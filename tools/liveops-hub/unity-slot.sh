#!/bin/bash
# unity-slot.sh — chạy một lệnh Unity khi lấy được một trong N slot của máy, có hạn giờ.
#
# Vì sao: nhiều gói việc chạy song song, mỗi gói mở Unity batchmode (import, test, chụp ảnh). Không chặn thì 8 Unity cùng
# import sẽ ăn hết RAM và làm mọi lượt chạy chậm tới mức hết hạn giờ. Khoá là thư mục vì `mkdir` là thao tác nguyên tử trên
# macOS: hai tiến trình không bao giờ cùng tạo được một thư mục, nên không cần flock (macOS không có sẵn).
# Hạn giờ bọc bằng `perl -e 'alarm N; exec @ARGV'` vì macOS không có `timeout` của coreutils; SIGALRM giết Unity treo
# (vd test yield WaitForEndOfFrame trong batchmode 2022.3) thay vì giữ slot mãi.
#
# Cách dùng:
#   unity-slot.sh [--slots N] [--timeout GIÂY] [--label CHỮ] -- <lệnh> [tham số...]
#   unity-slot.sh --status          # in slot đang giữ
#   unity-slot.sh --clean           # dọn khoá của tiến trình đã chết
#   unity-slot.sh --self-test       # tự kiểm: 3 lệnh sleep song song với N = 2, dọn khoá pid chết
#
# Biến môi trường:
#   LIVEOPS_UNITY_SLOTS             số slot (1–3, mặc định 2 — PD-26; chỉ đổi theo kết luận SP-17)
#   LIVEOPS_UNITY_SLOT_DIRECTORY    thư mục khoá (mặc định ~/.cache/unity-liveops/slots, dùng chung với G-SPIKE-A)
#
# Mỗi slot là thư mục slot-<số> chứa: `pid` (pid tiến trình giữ slot, một dòng), `command`, `project`, `started`.
# Thoát: mã thoát của lệnh; 142 khi hết hạn giờ (128 + SIGALRM); 2 khi dùng sai.

set -uo pipefail

readonly DEFAULT_SLOT_COUNT=2
readonly MAXIMUM_SLOT_COUNT=3
readonly DEFAULT_TIMEOUT_SECONDS=1800
readonly POLL_INTERVAL_SECONDS=2
# Khoá vừa mkdir mà chưa kịp ghi pid: chỉ coi là mồ côi khi đã quá khoảng này (tránh xoá khoá của tiến trình đang ghi).
readonly PID_WRITE_GRACE_SECONDS=30
readonly TIMEOUT_EXIT_CODE=142

slot_directory=${LIVEOPS_UNITY_SLOT_DIRECTORY:-$HOME/.cache/unity-liveops/slots}
slot_count=${LIVEOPS_UNITY_SLOTS:-$DEFAULT_SLOT_COUNT}
timeout_seconds=$DEFAULT_TIMEOUT_SECONDS
label=""
held_slot=""

print_usage() {
  sed -n '2,24p' "$0" | sed 's/^# \{0,1\}//'
}

fail_usage() {
  echo "unity-slot.sh: $1" >&2
  echo "Chạy 'unity-slot.sh --help' để xem cách dùng." >&2
  exit 2
}

validate_slot_count() {
  case "$slot_count" in
    ''|*[!0-9]*) fail_usage "số slot phải là số nguyên, nhận '$slot_count'";;
  esac
  if [ "$slot_count" -lt 1 ] || [ "$slot_count" -gt "$MAXIMUM_SLOT_COUNT" ]; then
    fail_usage "số slot phải trong 1–$MAXIMUM_SLOT_COUNT (PD-26), nhận $slot_count"
  fi
}

# Tuổi (giây) của một đường dẫn theo mtime; stat của macOS dùng -f %m.
path_age_seconds() {
  local modified
  modified=$(stat -f %m "$1" 2>/dev/null || echo 0)
  echo $(( $(date +%s) - modified ))
}

# Khoá mồ côi = có pid mà tiến trình đã chết, hoặc không có pid quá thời gian ân hạn. Xoá để slot không kẹt vĩnh viễn
# khi agent bị giết giữa chừng (trap EXIT không chạy khi nhận SIGKILL).
remove_slot_if_orphaned() {
  local slot_path=$1
  [ -d "$slot_path" ] || return 1
  local holder_pid=""
  if [ -f "$slot_path/pid" ]; then
    holder_pid=$(head -n 1 "$slot_path/pid" 2>/dev/null | tr -dc '0-9')
  fi
  if [ -n "$holder_pid" ]; then
    if kill -0 "$holder_pid" 2>/dev/null; then
      return 1
    fi
    echo "unity-slot.sh: dọn khoá mồ côi $(basename "$slot_path") (pid $holder_pid đã chết)" >&2
    rm -rf "$slot_path"
    return 0
  fi
  if [ "$(path_age_seconds "$slot_path")" -gt "$PID_WRITE_GRACE_SECONDS" ]; then
    echo "unity-slot.sh: dọn khoá mồ côi $(basename "$slot_path") (không có pid sau ${PID_WRITE_GRACE_SECONDS}s)" >&2
    rm -rf "$slot_path"
    return 0
  fi
  return 1
}

clean_orphaned_slots() {
  mkdir -p "$slot_directory"
  local slot_path
  for slot_path in "$slot_directory"/slot-*; do
    [ -d "$slot_path" ] || continue
    remove_slot_if_orphaned "$slot_path" || true
  done
}

print_status() {
  mkdir -p "$slot_directory"
  local slot_path found=0
  for slot_path in "$slot_directory"/slot-*; do
    [ -d "$slot_path" ] || continue
    found=1
    local holder_pid state
    holder_pid=$(head -n 1 "$slot_path/pid" 2>/dev/null | tr -dc '0-9')
    if [ -n "$holder_pid" ] && kill -0 "$holder_pid" 2>/dev/null; then state="đang chạy"; else state="mồ côi"; fi
    echo "$(basename "$slot_path")  pid=${holder_pid:-?}  $state  từ $(cat "$slot_path/started" 2>/dev/null)  $(cat "$slot_path/label" 2>/dev/null)"
    echo "    project: $(cat "$slot_path/project" 2>/dev/null)"
  done
  [ "$found" = 1 ] || echo "không slot nào đang bị giữ ($slot_directory)"
}

# Lấy -projectPath trong tham số lệnh (Unity nhận không phân biệt hoa thường) để chặn hai Unity cùng mở một project:
# cả hai cùng sinh .meta cho file mới sẽ ra hai GUID (luật CLAUDE.md gốc).
extract_project_path() {
  local previous="" argument
  for argument in "$@"; do
    case "$previous" in
      -projectPath|-projectpath|-PROJECTPATH) echo "$argument"; return 0;;
    esac
    previous=$argument
  done
  echo ""
}

normalize_path() {
  if [ -d "$1" ]; then (cd "$1" && pwd -P); else echo "$1"; fi
}

# Project đang bị một Unity khác mở (GUI hoặc batch, kể cả tiến trình không qua slot)?
project_is_open_elsewhere() {
  local project=$1
  [ -n "$project" ] || return 1
  local slot_path
  for slot_path in "$slot_directory"/slot-*; do
    [ -d "$slot_path" ] || continue
    [ "$slot_path" = "$held_slot" ] && continue
    if [ "$(cat "$slot_path/project" 2>/dev/null)" = "$project" ]; then
      local holder_pid
      holder_pid=$(head -n 1 "$slot_path/pid" 2>/dev/null | tr -dc '0-9')
      if [ -n "$holder_pid" ] && kill -0 "$holder_pid" 2>/dev/null; then return 0; fi
    fi
  done
  # Unity mở không qua slot (Editor GUI của user, lệnh gọi thẳng): dò bảng tiến trình. Worker nhập asset
  # (-name AssetImportWorker) chạy cùng projectPath với tiến trình chính và tự tắt theo nó — không tính.
  local lowered_project line lowered_line
  lowered_project=$(printf '%s' "$project" | tr '[:upper:]' '[:lower:]')
  while IFS= read -r line; do
    # Chỉ tiến trình mà chính file thực thi là Unity: dòng lệnh của unity-slot.sh (bash) cũng chứa đường dẫn Unity
    # trong tham số nên phải loại, không thì script tự thấy chính mình và chờ mãi.
    case "$line" in
      *AssetImportWorker*) continue;;
      */Unity.app/Contents/MacOS/Unity\ *|*/Unity.app/Contents/MacOS/Unity) ;;
      *) continue;;
    esac
    case "${line%%/Unity.app/Contents/MacOS/Unity*}" in
      *" "*) continue;;
    esac
    lowered_line=$(printf '%s' "$line" | tr '[:upper:]' '[:lower:]')
    case "$lowered_line " in
      *"-projectpath $lowered_project "*|*"-projectpath $lowered_project/ "*) return 0;;
    esac
  done < <(ps -Ao command= | grep -F "Unity.app/Contents/MacOS/Unity")
  return 1
}

release_slot() {
  if [ -n "$held_slot" ] && [ -d "$held_slot" ]; then
    local holder_pid
    holder_pid=$(head -n 1 "$held_slot/pid" 2>/dev/null | tr -dc '0-9')
    # Chỉ xoá khoá của chính mình: nếu khoá đã bị dọn và tiến trình khác lấy lại cùng tên thì không đụng.
    if [ "$holder_pid" = "$$" ]; then
      rm -rf "$held_slot"
    fi
  fi
  held_slot=""
}

acquire_slot() {
  local project=$1 command_text=$2
  mkdir -p "$slot_directory"
  local announced_wait=0 announced_project_wait=0 wait_started
  wait_started=$(date +%s)
  while true; do
    # Dọn mọi khoá mồ côi (kể cả slot-<số> lớn hơn N của lần chạy N khác) trước khi thử lấy.
    clean_orphaned_slots
    local index
    for index in $(seq 1 "$slot_count"); do
      local slot_path="$slot_directory/slot-$index"
      if mkdir "$slot_path" 2>/dev/null; then
        echo "$$" > "$slot_path/pid"
        held_slot=$slot_path
        if project_is_open_elsewhere "$project"; then
          # Project đang mở ở chỗ khác: trả slot để người khác dùng, chờ vòng sau.
          release_slot
          if [ "$announced_project_wait" = 0 ]; then
            echo "unity-slot.sh: project $project đang được một Unity khác mở — chờ nó đóng..." >&2
            announced_project_wait=1
          fi
          break
        fi
        printf '%s\n' "$project" > "$slot_path/project"
        printf '%s\n' "$command_text" > "$slot_path/command"
        printf '%s\n' "$label" > "$slot_path/label"
        date '+%Y-%m-%d %H:%M:%S' > "$slot_path/started"
        return 0
      fi
    done
    if [ "$announced_wait" = 0 ] && [ "$announced_project_wait" = 0 ]; then
      echo "unity-slot.sh: đang chờ slot Unity (tối đa $slot_count đồng thời; xem --status)..." >&2
      announced_wait=1
    fi
    sleep "$POLL_INTERVAL_SECONDS"
    if [ $(( $(date +%s) - wait_started )) -gt 0 ] && [ $(( ($(date +%s) - wait_started) % 300 )) -lt "$POLL_INTERVAL_SECONDS" ]; then
      echo "unity-slot.sh: vẫn chờ slot sau $(( $(date +%s) - wait_started ))s" >&2
    fi
  done
}

run_with_slot() {
  validate_slot_count
  case "$timeout_seconds" in
    ''|*[!0-9]*) fail_usage "--timeout phải là số giây, nhận '$timeout_seconds'";;
  esac
  [ "$#" -gt 0 ] || fail_usage "thiếu lệnh sau --"
  local project
  project=$(extract_project_path "$@")
  [ -n "$project" ] && project=$(normalize_path "$project")

  trap 'release_slot' EXIT
  trap 'release_slot; exit 130' INT
  trap 'release_slot; exit 143' TERM

  acquire_slot "$project" "$*"
  echo "unity-slot.sh: giữ $(basename "$held_slot") (pid $$, hạn ${timeout_seconds}s)${label:+ — $label}" >&2
  local started exit_code
  started=$(date +%s)
  perl -e 'alarm shift @ARGV; exec @ARGV or die "không chạy được lệnh: $!\n"' "$timeout_seconds" "$@"
  exit_code=$?
  release_slot
  if [ "$exit_code" = "$TIMEOUT_EXIT_CODE" ]; then
    echo "unity-slot.sh: HẾT HẠN GIỜ sau ${timeout_seconds}s — lệnh bị dừng bằng SIGALRM: $*" >&2
  fi
  echo "unity-slot.sh: trả slot sau $(( $(date +%s) - started ))s, mã thoát $exit_code" >&2
  return "$exit_code"
}

run_self_test() {
  local script_path test_directory
  script_path=$(cd "$(dirname "$0")" && pwd)/$(basename "$0")
  test_directory=$(mktemp -d "${TMPDIR:-/tmp}/unity-slot-self-test.XXXXXX")
  local failures=0
  export LIVEOPS_UNITY_SLOT_DIRECTORY="$test_directory/slots"
  export LIVEOPS_UNITY_SLOTS=2
  echo "== self-test 1: 3 lệnh 'sleep 5' song song với N = 2 (thư mục khoá tạm $test_directory)"
  local suite_started index
  suite_started=$(date +%s)
  for index in 1 2 3; do
    ( bash "$script_path" --timeout 60 --label "self-test-$index" -- \
        bash -c "echo \$(date +%s) > '$test_directory/start-$index'; sleep 5; echo \$(date +%s) > '$test_directory/end-$index'" \
        2>>"$test_directory/log-$index" ) &
    sleep 0.2
  done
  wait
  local starts=() index_value
  for index in 1 2 3; do
    if [ ! -f "$test_directory/start-$index" ]; then
      echo "LỖI: lệnh $index không chạy"; failures=$((failures + 1)); continue
    fi
    index_value=$(cat "$test_directory/start-$index")
    starts+=("$(( index_value - suite_started ))")
  done
  if [ "${#starts[@]}" = 3 ]; then
    local sorted
    sorted=$(printf '%s\n' "${starts[@]}" | sort -n | tr '\n' ' ')
    echo "   giây bắt đầu tương đối (đã sắp): $sorted"
    local first second third
    read -r first second third <<< "$sorted"
    if [ "$second" -ge 5 ]; then echo "LỖI: hai lệnh đầu phải chạy song song (lệnh thứ hai bắt đầu ở giây $second)"; failures=$((failures + 1)); fi
    if [ $(( third - first )) -lt 5 ]; then echo "LỖI: lệnh thứ ba bắt đầu sớm hơn 5 giây sau lệnh đầu ($first → $third)"; failures=$((failures + 1)); fi
    [ $(( third - first )) -ge 5 ] && [ "$second" -lt 5 ] && echo "   OK: 2 chạy ngay, lệnh thứ ba chờ $(( third - first ))s"
  fi
  if ls "$LIVEOPS_UNITY_SLOT_DIRECTORY"/slot-* >/dev/null 2>&1; then
    echo "LỖI: còn khoá sau khi mọi lệnh xong"; failures=$((failures + 1))
  else
    echo "   OK: mọi khoá đã trả"
  fi

  echo "== self-test 2: khoá của pid đã chết bị dọn"
  bash -c 'exit 0' & local dead_pid=$!
  wait "$dead_pid"
  mkdir -p "$LIVEOPS_UNITY_SLOT_DIRECTORY/slot-1" "$LIVEOPS_UNITY_SLOT_DIRECTORY/slot-2"
  echo "$dead_pid" > "$LIVEOPS_UNITY_SLOT_DIRECTORY/slot-1/pid"
  echo "$dead_pid" > "$LIVEOPS_UNITY_SLOT_DIRECTORY/slot-2/pid"
  local cleanup_started
  cleanup_started=$(date +%s)
  if bash "$script_path" --timeout 30 --label self-test-dead -- true 2>"$test_directory/log-dead"; then
    if [ $(( $(date +%s) - cleanup_started )) -le 10 ] && grep -q "mồ côi" "$test_directory/log-dead"; then
      echo "   OK: khoá mồ côi được dọn, lệnh chạy ngay"
    else
      echo "LỖI: không thấy dọn khoá mồ côi"; cat "$test_directory/log-dead"; failures=$((failures + 1))
    fi
  else
    echo "LỖI: lệnh sau khi dọn khoá thất bại"; failures=$((failures + 1))
  fi

  echo "== self-test 3: hết hạn giờ trả mã $TIMEOUT_EXIT_CODE và trả slot"
  bash "$script_path" --timeout 1 -- sleep 5 2>"$test_directory/log-timeout"
  local timeout_exit=$?
  if [ "$timeout_exit" = "$TIMEOUT_EXIT_CODE" ] && ! ls "$LIVEOPS_UNITY_SLOT_DIRECTORY"/slot-* >/dev/null 2>&1; then
    echo "   OK: mã $timeout_exit, slot đã trả"
  else
    echo "LỖI: mã thoát $timeout_exit (cần $TIMEOUT_EXIT_CODE) hoặc còn khoá"; failures=$((failures + 1))
  fi

  echo "== self-test 4: số slot ngoài 1–3 bị từ chối"
  if LIVEOPS_UNITY_SLOTS=4 bash "$script_path" -- true 2>/dev/null; then
    echo "LỖI: N = 4 phải bị từ chối"; failures=$((failures + 1))
  else
    echo "   OK"
  fi

  rm -rf "$test_directory"
  if [ "$failures" = 0 ]; then
    echo "UNITY SLOT SELF-TEST OK"
    return 0
  fi
  echo "UNITY SLOT SELF-TEST FAILED ($failures lỗi)"
  return 1
}

main() {
  if [ "$#" = 0 ]; then print_usage; exit 2; fi
  while [ "$#" -gt 0 ]; do
    case "$1" in
      --help|-h) print_usage; exit 0;;
      --status) print_status; exit 0;;
      --clean) clean_orphaned_slots; print_status; exit 0;;
      --self-test) run_self_test; exit $?;;
      --slots) [ "$#" -ge 2 ] || fail_usage "--slots cần giá trị"; slot_count=$2; shift 2;;
      --timeout) [ "$#" -ge 2 ] || fail_usage "--timeout cần giá trị"; timeout_seconds=$2; shift 2;;
      --label) [ "$#" -ge 2 ] || fail_usage "--label cần giá trị"; label=$2; shift 2;;
      --) shift; run_with_slot "$@"; exit $?;;
      *) fail_usage "tham số lạ '$1' (lệnh phải đứng sau --)";;
    esac
  done
  fail_usage "thiếu -- <lệnh>"
}

main "$@"
