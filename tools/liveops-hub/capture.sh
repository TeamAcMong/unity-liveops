#!/bin/bash
# capture.sh — chụp ảnh cửa sổ LiveOps Hub thật theo kịch bản × skin × bản Unity (mục 9.5), rồi đo ảnh.
#
# Vì sao: nghiệm thu giao diện phải bằng ảnh cửa sổ Editor thật ở cả hai skin và hai bản Unity, không phải mockup; ảnh ở
# cổng đợt là ảnh của sản phẩm đã ghép (PD-36). Lệnh chụp là `LiveOpsHubCaptureCommand.CaptureFromCommandLine` (assembly
# test Editor, G-SHELL); script này chỉ lo slot, hạn giờ, thư mục ra và chạy đo. KHÔNG -nographics (layout NaN, ảnh một
# màu [API §12.5]) và KHÔNG -quit (lệnh tự EditorApplication.Exit sau khi chụp xong).
#
# Skin (SP-4, cổng W0 lượt 2): gọi `InternalEditorUtility.SwitchSkinAndRepaintAllViews()` giữa phiên batch làm Editor treo
# ở cả hai bản và ghi đè skin của máy — lệnh chụp KHÔNG được đổi skin. Mỗi skin là MỘT lượt Unity riêng; trước lượt, script
# đặt EditorPrefs `UserSkin` (1 = dark, 0 = light) rồi khôi phục giá trị cũ ngay sau lượt và trong trap. Pref này nằm trong
# plist dùng chung cho MỌI bản Unity trên máy (kể cả Unity GUI user đang mở), nên mọi lượt đặt skin chạy tuần tự dưới một khoá
# riêng `<thư mục slot>/skin-preference/` (không song song, kể cả khi hai capture.sh chạy ở hai slot); khoá ghi giá trị gốc để
# lượt sau gặp khoá mồ côi (capture.sh bị kill -9) vẫn trả được skin cho user.
#
# Cách dùng:
#   capture.sh --unity 6000|2022|all --scenarios "<id,id,…>|registered" --label <nhãn>
#              [--project <đường dẫn>] [--repository <worktree>] [--skins dark,light] [--output <thư mục gốc>]
#              [--timeout GIÂY] [--no-measure] [--dry-run]
# Ra: <output>/<nhãn>/<bản Unity>/<id>-<skin>.png + <id>-<skin>.json (+ capture-<skin>.log, measurements.json)
# Mặc định output ~/.cache/unity-liveops/captures; project 6000 = worktree, 2022 = ~/.cache/unity-liveops/temp-2022/<gói>.
# --dry-run in đúng lệnh sẽ chạy rồi thoát 0. Thoát: 0 đạt; 1 Unity lỗi, thiếu ảnh hoặc số đo lệch; 2 dùng sai; 3 đĩa < 5 GB.

set -uo pipefail

readonly UNITY_2022=/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity
readonly UNITY_6000=/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity
readonly PACKAGE_RELATIVE_PATH=Packages/com.dreamtech.liveops
readonly CAPTURE_METHOD=DreamTech.LiveOps.Editor.Tests.LiveOpsHubCaptureCommand.CaptureFromCommandLine
readonly DEFAULT_CAPTURE_TIMEOUT_SECONDS=900
readonly MINIMUM_FREE_GIGABYTES=5
readonly SKIN_LOCK_POLL_SECONDS=2
# Biến môi trường chỉ để tự kiểm công cụ trên một domain giả — không đổi khi chụp thật (Unity đọc đúng domain này).
readonly UNITY_PREFERENCES_DOMAIN=${LIVEOPS_UNITY_PREFERENCES_DOMAIN:-com.unity3d.UnityEditor5.x}
readonly SKIN_LOCK_DIRECTORY=${LIVEOPS_UNITY_SLOT_DIRECTORY:-$HOME/.cache/unity-liveops/slots}/skin-preference

script_directory=$(cd "$(dirname "$0")" && pwd)
repository=${REPOSITORY:-}
unity_selection=""
project_override=""
scenarios=""
label=""
skins=dark,light
output_root=$HOME/.cache/unity-liveops/captures
timeout_seconds=$DEFAULT_CAPTURE_TIMEOUT_SECONDS
measure=1
dry_run=0

fail_usage() { echo "capture.sh: $1" >&2; exit 2; }

while [ "$#" -gt 0 ]; do
  case "$1" in
    --unity) [ "$#" -ge 2 ] || fail_usage "--unity cần 6000|2022|all"; unity_selection=$2; shift 2;;
    --project) [ "$#" -ge 2 ] || fail_usage "--project cần đường dẫn"; project_override=$2; shift 2;;
    --repository) [ "$#" -ge 2 ] || fail_usage "--repository cần đường dẫn"; repository=$2; shift 2;;
    --scenarios) [ "$#" -ge 2 ] || fail_usage "--scenarios cần danh sách id hoặc registered"; scenarios=$2; shift 2;;
    --label) [ "$#" -ge 2 ] || fail_usage "--label cần giá trị"; label=$2; shift 2;;
    --skins) [ "$#" -ge 2 ] || fail_usage "--skins cần dark,light"; skins=$2; shift 2;;
    --output) [ "$#" -ge 2 ] || fail_usage "--output cần đường dẫn"; output_root=$2; shift 2;;
    --timeout) [ "$#" -ge 2 ] || fail_usage "--timeout cần số giây"; timeout_seconds=$2; shift 2;;
    --no-measure) measure=0; shift;;
    --dry-run) dry_run=1; shift;;
    --help|-h) sed -n '2,18p' "$0" | sed 's/^# \{0,1\}//'; exit 0;;
    *) fail_usage "tham số lạ '$1'";;
  esac
done

[ -n "$unity_selection" ] || fail_usage "thiếu --unity"
[ -n "$scenarios" ] || fail_usage "thiếu --scenarios"
[ -n "$label" ] || fail_usage "thiếu --label"
case "$label" in */*|.*) fail_usage "nhãn không được chứa '/' hoặc bắt đầu bằng '.'";; esac
case "$skins" in dark|light|dark,light|light,dark) ;; *) fail_usage "--skins chỉ nhận dark, light hoặc dark,light";; esac
case "$scenarios" in
  registered) ;;
  *[!a-z0-9,_-]*) fail_usage "--scenarios chỉ gồm id [a-z0-9_-] cách nhau bằng dấu phẩy, hoặc 'registered'";;
esac
case "$unity_selection" in
  6000|6000.6) versions="6000";;
  2022|2022.3) versions="2022";;
  all) versions="6000 2022";;
  *) fail_usage "--unity phải là 6000, 2022 hoặc all";;
esac

if [ -n "$repository" ]; then
  repository=$(cd "$repository" && pwd -P)
elif top_level=$(git rev-parse --show-toplevel 2>/dev/null) && [ -d "$top_level/$PACKAGE_RELATIVE_PATH" ]; then
  repository=$top_level
else
  echo "capture.sh: thư mục hiện tại không thuộc repo có $PACKAGE_RELATIVE_PATH — truyền --repository <worktree>" >&2
  echo "  (không tự rơi về repo chứa script: gọi bằng đường dẫn tuyệt đối từ cwd khác sẽ kiểm nhầm worktree G-TOOLS)" >&2
  exit 2
fi
echo "capture.sh: repository=$repository" >&2

package_name() {
  local branch
  branch=$(git -C "$repository" branch --show-current 2>/dev/null || true)
  case "$branch" in
    wt/*) echo "${branch#wt/}";;
    *) basename "$repository";;
  esac
}

quote_command() {
  local argument quoted=""
  for argument in "$@"; do
    quoted="$quoted $(printf '%q' "$argument")"
  done
  echo "${quoted# }"
}

# ---- Khoá + pref skin ------------------------------------------------------------------------------------------------
# Giá trị pref gốc lưu dạng chữ: "<số>" hoặc "absent" (khoá chưa có — khôi phục bằng cách xoá khoá, không ghi 0).
read_user_skin() {
  defaults read "$UNITY_PREFERENCES_DOMAIN" UserSkin 2>/dev/null || echo absent
}

write_user_skin() {
  if [ "$1" = absent ]; then
    defaults delete "$UNITY_PREFERENCES_DOMAIN" UserSkin >/dev/null 2>&1 || true
  else
    defaults write "$UNITY_PREFERENCES_DOMAIN" UserSkin -int "$1"
  fi
}

holding_skin_lock=0
release_skin_lock() {
  [ "$holding_skin_lock" = 1 ] || return 0
  local original
  original=$(cat "$SKIN_LOCK_DIRECTORY/original-user-skin" 2>/dev/null || echo "")
  if [ -n "$original" ]; then
    write_user_skin "$original"
    echo "capture.sh: trả UserSkin về $original" >&2
  fi
  rm -rf "$SKIN_LOCK_DIRECTORY"
  holding_skin_lock=0
}

# Chờ khoá tới khi lấy được; khoá của pid đã chết thì khôi phục pref gốc nó ghi lại rồi mới dọn (không để user kẹt skin sai).
acquire_skin_lock() {
  mkdir -p "$(dirname "$SKIN_LOCK_DIRECTORY")"
  local waited=0 owner_pid stale_original
  while ! mkdir "$SKIN_LOCK_DIRECTORY" 2>/dev/null; do
    owner_pid=$(cat "$SKIN_LOCK_DIRECTORY/pid" 2>/dev/null || echo "")
    # Khoá không có pid mà đã quá 1 phút = tiến trình chết giữa mkdir và ghi pid (lúc đó pref chưa bị đổi).
    if { [ -n "$owner_pid" ] && ! kill -0 "$owner_pid" 2>/dev/null; } ||
       { [ -z "$owner_pid" ] && [ -n "$(find "$SKIN_LOCK_DIRECTORY" -maxdepth 0 -mmin +1 2>/dev/null)" ]; }; then
      stale_original=$(cat "$SKIN_LOCK_DIRECTORY/original-user-skin" 2>/dev/null || echo "")
      if [ -n "$stale_original" ]; then
        write_user_skin "$stale_original"
        echo "capture.sh: khoá skin mồ côi của pid $owner_pid — đã trả UserSkin về $stale_original" >&2
      fi
      rm -rf "$SKIN_LOCK_DIRECTORY"
      continue
    fi
    if [ "$waited" = 0 ]; then
      echo "capture.sh: chờ khoá skin (đang giữ bởi pid ${owner_pid:-?})" >&2
    fi
    sleep "$SKIN_LOCK_POLL_SECONDS"
    waited=$((waited + SKIN_LOCK_POLL_SECONDS))
  done
  holding_skin_lock=1
  echo "$$" > "$SKIN_LOCK_DIRECTORY/pid"
  read_user_skin > "$SKIN_LOCK_DIRECTORY/original-user-skin"
}

on_signal() {
  release_skin_lock
  exit 130
}
trap release_skin_lock EXIT
trap on_signal INT TERM

overall_status=0
IFS=',' read -r -a skin_list <<< "$skins"
for version in $versions; do
  if [ "$version" = 6000 ]; then
    unity_binary=$UNITY_6000; unity_label=6000.6; default_project=$repository
  else
    unity_binary=$UNITY_2022; unity_label=2022.3; default_project=$HOME/.cache/unity-liveops/temp-2022/$(package_name)
  fi
  project=${project_override:-$default_project}
  output_directory=$output_root/$label/$unity_label

  if [ "$dry_run" = 0 ]; then
    [ -x "$unity_binary" ] || fail_usage "thiếu Unity ở $unity_binary"
    [ -d "$project" ] || fail_usage "không thấy project $project"
    free_gigabytes=$(df -g /System/Volumes/Data | awk 'NR==2 {print $4}')
    if [ "${free_gigabytes:-0}" -lt "$MINIMUM_FREE_GIGABYTES" ]; then
      echo "LỖI: ổ đĩa còn ${free_gigabytes} GB (< $MINIMUM_FREE_GIGABYTES GB) — không mở Unity" >&2
      exit 3
    fi
    mkdir -p "$output_directory"
    # Dọn ảnh/đo/log cũ trước khi chụp — nhãn = tên gói (9.6) nên chạy lại sau khi sửa dùng cùng thư mục; nếu không dọn,
    # ảnh cũ của lần chụp trước vẫn đủ để qua bước kiểm "đủ ảnh" dù lần chụp này thật ra lỗi (F8).
    rm -f "$output_directory"/*.png "$output_directory"/*.json "$output_directory"/capture*.log
  else
    echo "# capture $unity_label → $output_directory"
    echo "mkdir -p $(printf '%q' "$output_directory")"
  fi

  version_failed=0
  for skin in "${skin_list[@]}"; do
    if [ "$skin" = dark ]; then skin_value=1; else skin_value=0; fi
    log_file=$output_directory/capture-$skin.log
    capture_command=("$unity_binary" -batchmode -projectPath "$project"
      -executeMethod "$CAPTURE_METHOD"
      -liveopsCaptureOut "$output_directory" -liveopsCaptureScenarios "$scenarios" -liveopsCaptureSkins "$skin"
      -logFile "$log_file")
    slot_command=("$script_directory/unity-slot.sh" --timeout "$timeout_seconds" --label "capture $label $unity_label $skin" -- "${capture_command[@]}")

    if [ "$dry_run" = 1 ]; then
      echo "# khoá $SKIN_LOCK_DIRECTORY; lưu UserSkin cũ; khôi phục sau lượt + trong trap"
      quote_command defaults write "$UNITY_PREFERENCES_DOMAIN" UserSkin -int "$skin_value"
      quote_command "${slot_command[@]}"
      continue
    fi

    acquire_skin_lock
    write_user_skin "$skin_value"
    unity_status=0
    "${slot_command[@]}" || unity_status=$?
    release_skin_lock
    if [ "$unity_status" != 0 ]; then
      echo "LỖI: chụp $unity_label skin $skin thoát $unity_status — xem $log_file"
      grep -n "LIVEOPS CAPTURE\|error CS\|Exception" "$log_file" 2>/dev/null | head -n 20 | sed 's/^/   /'
      version_failed=1
    fi
  done

  if [ "$dry_run" = 1 ]; then
    if [ "$measure" = 1 ]; then
      quote_command python3 "$script_directory/measure-capture.py" "$output_directory"
    fi
    continue
  fi
  if [ "$version_failed" = 1 ]; then
    overall_status=1
    continue
  fi
  if [ "$scenarios" != registered ]; then
    missing=0
    IFS=',' read -r -a scenario_list <<< "$scenarios"
    for scenario in "${scenario_list[@]}"; do
      for skin in "${skin_list[@]}"; do
        if [ ! -s "$output_directory/$scenario-$skin.png" ]; then
          echo "LỖI: thiếu ảnh $output_directory/$scenario-$skin.png"
          missing=$((missing + 1))
        fi
      done
    done
    [ "$missing" = 0 ] || overall_status=1
  fi
  echo "ảnh $unity_label: $(ls "$output_directory"/*.png 2>/dev/null | wc -l | tr -d ' ') file → $output_directory"
  if [ "$measure" = 1 ]; then
    python3 "$script_directory/measure-capture.py" "$output_directory" || overall_status=1
  fi
done

if [ "$dry_run" = 1 ]; then
  exit 0
fi
if [ "$overall_status" = 0 ]; then
  echo "CAPTURE OK ($label)"
else
  echo "CAPTURE FAILED ($label)"
fi
exit "$overall_status"
