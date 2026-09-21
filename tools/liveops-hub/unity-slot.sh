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
#   unity-slot.sh --status          # in slot và khoá project đang giữ
#   unity-slot.sh --clean           # dọn khoá của tiến trình đã chết
#   unity-slot.sh --self-test       # tự kiểm: N = 2, pid chết, wrapper bị SIGKILL, hai lệnh cùng project, hạn giờ
#
# Biến môi trường:
#   LIVEOPS_UNITY_SLOTS             số slot (1–3, mặc định 2 — PD-26; chỉ đổi theo kết luận SP-17)
#   LIVEOPS_UNITY_SLOT_DIRECTORY    thư mục khoá (mặc định ~/.cache/unity-liveops/slots, dùng chung với G-SPIKE-A)
#
# Khoá: slot-<số>/ (tối đa N) và projects/<băm đường dẫn project>/ (một lệnh mỗi project, giữ suốt lệnh). Mỗi khoá chứa
# `pid` + `pid_started` (bash wrapper), `child_pid` + `child_started` (tiến trình lệnh — perl exec thành Unity), `project`,
# `command`, `label`, `started`. Khoá còn sống khi wrapper HOẶC tiến trình lệnh còn sống (so cả giờ bắt đầu để pid bị tái
# sử dụng không giữ khoá mãi). Khoá tự tạo tay (G-SPIKE-A) chỉ có `pid` vẫn được hiểu.
# Thoát: mã thoát của lệnh; 142 khi hết hạn giờ (128 + SIGALRM); 2 khi dùng sai.

set -uo pipefail

readonly DEFAULT_SLOT_COUNT=2
readonly MAXIMUM_SLOT_COUNT=3
readonly DEFAULT_TIMEOUT_SECONDS=1800
readonly POLL_INTERVAL_SECONDS=2
# Khoá vừa mkdir mà chưa kịp ghi pid: chỉ coi là mồ côi khi đã quá khoảng này (tránh xoá khoá của tiến trình đang ghi).
readonly PID_WRITE_GRACE_SECONDS=30
# Nhận INT/TERM thì chuyển TERM cho lệnh con và chờ tối đa khoảng này rồi mới KILL — không trả khoá khi Unity còn chạy.
readonly CHILD_STOP_GRACE_SECONDS=30
readonly TIMEOUT_EXIT_CODE=142

slot_directory=${LIVEOPS_UNITY_SLOT_DIRECTORY:-$HOME/.cache/unity-liveops/slots}
slot_count=${LIVEOPS_UNITY_SLOTS:-$DEFAULT_SLOT_COUNT}
timeout_seconds=$DEFAULT_TIMEOUT_SECONDS
label=""
held_slot=""
held_project_lock=""
child_process_id=""

print_usage() {
  sed -n '2,26p' "$0" | sed 's/^# \{0,1\}//'
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

# Tuổi (giây) của một khoá theo GIỜ TẠO (stat -f %B của macOS), không theo mtime: ghi file vào khoá — kể cả dấu `reaping`
# của tiến trình dọn — làm mới mtime và khoá không pid sẽ không bao giờ quá hạn.
path_age_seconds() {
  local created
  created=$(stat -f %B "$1" 2>/dev/null || echo 0)
  echo $(( $(date +%s) - created ))
}

# Giờ bắt đầu của tiến trình theo ps; rỗng khi tiến trình không còn. exec (perl → Unity) giữ nguyên giờ này.
process_start_time() {
  ps -o lstart= -p "$1" 2>/dev/null | sed 's/^ *//; s/ *$//'
}

# Ghi <tên>_started rồi <tên> qua file tạm + mv: người đọc không bao giờ thấy file pid rỗng hoặc pid thiếu giờ bắt đầu.
write_process_record() {
  local lock_path=$1 name=$2 process_id=$3
  process_start_time "$process_id" > "$lock_path/${name}_started.$$" && mv "$lock_path/${name}_started.$$" "$lock_path/${name}_started"
  printf '%s\n' "$process_id" > "$lock_path/$name.$$" && mv "$lock_path/$name.$$" "$lock_path/$name"
}

recorded_process_id() {
  head -n 1 "$1/$2" 2>/dev/null | tr -dc '0-9'
}

# Tiến trình ghi trong khoá còn sống? Khoá tay không có <tên>_started thì chỉ dựa vào kill -0.
recorded_process_is_alive() {
  local lock_path=$1 name=$2 process_id recorded_start
  process_id=$(recorded_process_id "$lock_path" "$name")
  [ -n "$process_id" ] || return 1
  kill -0 "$process_id" 2>/dev/null || return 1
  recorded_start=$(cat "$lock_path/${name}_started" 2>/dev/null)
  if [ -n "$recorded_start" ] && [ "$(process_start_time "$process_id")" != "$recorded_start" ]; then
    return 1
  fi
  return 0
}

lock_is_alive() {
  recorded_process_is_alive "$1" pid || recorded_process_is_alive "$1" child_pid
}

# Mồ côi = có pid (wrapper hoặc lệnh) mà cả hai đã chết, hoặc chưa có pid nào sau thời gian ân hạn tính từ lúc tạo khoá.
lock_is_orphaned() {
  local lock_path=$1
  [ -d "$lock_path" ] || return 1
  lock_is_alive "$lock_path" && return 1
  if [ -n "$(recorded_process_id "$lock_path" pid)" ] || [ -n "$(recorded_process_id "$lock_path" child_pid)" ]; then
    return 0
  fi
  [ "$(path_age_seconds "$lock_path")" -gt "$PID_WRITE_GRACE_SECONDS" ]
}

# Dọn khoá mồ côi để slot không kẹt vĩnh viễn khi agent bị giết giữa chừng (trap EXIT không chạy khi nhận SIGKILL).
# Hai tiến trình cùng thấy một khoá mồ côi thì chỉ một được dọn: dấu `reaping` tạo bằng mkdir nguyên tử; kiểm lại sau khi
# có dấu (khoá có thể đã bị người khác dọn và tạo lại — khoá mới còn sống hoặc còn trong ân hạn nên không bị xoá); rồi
# mv nguyên tử sang tên .stale trước khi rm để tên slot biến mất một lần, không lộ khoá đang xoá dở cho người khác.
remove_lock_if_orphaned() {
  local lock_path=$1
  lock_is_orphaned "$lock_path" || return 1
  if ! mkdir "$lock_path/reaping" 2>/dev/null; then
    # Tiến trình dọn trước bị giết giữa chừng thì dấu của nó cũng mồ côi — gỡ dấu, vòng sau dọn tiếp.
    if [ -d "$lock_path/reaping" ] && lock_is_orphaned "$lock_path/reaping"; then
      rm -rf "$lock_path/reaping"
    fi
    return 1
  fi
  write_process_record "$lock_path/reaping" pid "$$"
  if ! lock_is_orphaned "$lock_path"; then
    rm -rf "$lock_path/reaping"
    return 1
  fi
  local holder_description stale_path
  holder_description="pid $(recorded_process_id "$lock_path" pid || true), lệnh $(recorded_process_id "$lock_path" child_pid || true)"
  stale_path="$lock_path.stale.$$.$RANDOM"
  if mv "$lock_path" "$stale_path" 2>/dev/null; then
    rm -rf "$stale_path"
    echo "unity-slot.sh: dọn khoá mồ côi ${lock_path#"$slot_directory"/} ($holder_description đã chết hoặc chưa từng ghi)" >&2
    return 0
  fi
  rm -rf "$lock_path/reaping"
  return 1
}

clean_orphaned_locks() {
  mkdir -p "$slot_directory/projects"
  local lock_path
  for lock_path in "$slot_directory"/slot-* "$slot_directory"/projects/*; do
    [ -d "$lock_path" ] || continue
    case "$lock_path" in
      # Bản .stale còn sót khi tiến trình dọn bị giết giữa mv và rm: không ai giữ nữa.
      *.stale.*) rm -rf "$lock_path"; continue;;
    esac
    remove_lock_if_orphaned "$lock_path" || true
  done
}

print_lock_status() {
  local lock_path=$1 state
  if lock_is_alive "$lock_path"; then state="đang chạy"; else state="mồ côi"; fi
  echo "${lock_path#"$slot_directory"/}  pid=$(recorded_process_id "$lock_path" pid)  lệnh=$(recorded_process_id "$lock_path" child_pid)  $state  từ $(cat "$lock_path/started" 2>/dev/null)  $(cat "$lock_path/label" 2>/dev/null)"
  echo "    project: $(cat "$lock_path/project" 2>/dev/null)"
}

print_status() {
  mkdir -p "$slot_directory/projects"
  local lock_path found=0
  for lock_path in "$slot_directory"/slot-* "$slot_directory"/projects/*; do
    [ -d "$lock_path" ] || continue
    case "$lock_path" in *.stale.*) continue;; esac
    found=1
    print_lock_status "$lock_path"
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

# Tên khoá project: băm đường dẫn chữ thường (APFS mặc định không phân biệt hoa thường — hai cách viết là một project).
project_lock_path() {
  local lowered_project digest
  lowered_project=$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')
  digest=$(printf '%s' "$lowered_project" | shasum -a 256 | cut -c1-24)
  echo "$slot_directory/projects/$digest"
}

# Project đang bị một Unity khác mở mà khoá project không thấy: slot tay có file `project` (G-SPIKE-A), Editor GUI của
# user, lệnh gọi thẳng không qua slot.
project_is_open_elsewhere() {
  local project=$1
  [ -n "$project" ] || return 1
  local slot_path
  for slot_path in "$slot_directory"/slot-*; do
    [ -d "$slot_path" ] || continue
    [ "$slot_path" = "$held_slot" ] && continue
    if [ "$(cat "$slot_path/project" 2>/dev/null)" = "$project" ] && lock_is_alive "$slot_path"; then
      return 0
    fi
  done
  # Worker nhập asset (-name AssetImportWorker) chạy cùng projectPath với tiến trình chính và tự tắt theo nó — không tính.
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

# Chỉ xoá khoá của chính mình: nếu khoá đã bị dọn và tiến trình khác lấy lại cùng tên thì không đụng.
release_lock() {
  local lock_path=$1
  if [ -n "$lock_path" ] && [ -d "$lock_path" ] && [ "$(recorded_process_id "$lock_path" pid)" = "$$" ]; then
    rm -rf "$lock_path"
  fi
}

release_slot() {
  release_lock "$held_slot"
  held_slot=""
}

release_all_locks() {
  release_slot
  release_lock "$held_project_lock"
  held_project_lock=""
}

write_lock_details() {
  local lock_path=$1 project=$2 command_text=$3
  printf '%s\n' "$project" > "$lock_path/project"
  printf '%s\n' "$command_text" > "$lock_path/command"
  printf '%s\n' "$label" > "$lock_path/label"
  date '+%Y-%m-%d %H:%M:%S' > "$lock_path/started"
}

# Khoá project lấy TRƯỚC slot và giữ suốt lệnh. Vì sao: kiểm "project đang mở" rồi mới ghi file `project` vào slot để
# hở một khe — hai lệnh cùng project phát cùng lúc đều thấy "chưa ai mở" và cùng chạy. mkdir khoá project là nguyên tử nên
# chỉ một lệnh qua; chờ slot trong lúc giữ khoá project không phí gì vì lệnh cùng project đằng nào cũng phải chờ.
acquire_project_lock() {
  local project=$1 command_text=$2
  [ -n "$project" ] || return 0
  local lock_path announced_wait=0
  lock_path=$(project_lock_path "$project")
  mkdir -p "$slot_directory/projects"
  while true; do
    remove_lock_if_orphaned "$lock_path" || true
    if mkdir "$lock_path" 2>/dev/null; then
      write_process_record "$lock_path" pid "$$"
      held_project_lock=$lock_path
      write_lock_details "$lock_path" "$project" "$command_text"
      if ! project_is_open_elsewhere "$project"; then
        return 0
      fi
      release_lock "$held_project_lock"
      held_project_lock=""
    fi
    if [ "$announced_wait" = 0 ]; then
      echo "unity-slot.sh: project $project đang được một Unity khác mở — chờ nó đóng..." >&2
      announced_wait=1
    fi
    sleep "$POLL_INTERVAL_SECONDS"
  done
}

acquire_slot() {
  local project=$1 command_text=$2
  mkdir -p "$slot_directory"
  local announced_wait=0 announced_project_wait=0 wait_started
  wait_started=$(date +%s)
  while true; do
    # Dọn mọi khoá mồ côi (kể cả slot-<số> lớn hơn N của lần chạy N khác) trước khi thử lấy.
    clean_orphaned_locks
    local index
    for index in $(seq 1 "$slot_count"); do
      local slot_path="$slot_directory/slot-$index"
      if mkdir "$slot_path" 2>/dev/null; then
        write_process_record "$slot_path" pid "$$"
        held_slot=$slot_path
        # Kiểm lại lúc đã có slot: Editor GUI có thể được mở trong lúc chờ slot.
        if project_is_open_elsewhere "$project"; then
          release_slot
          if [ "$announced_project_wait" = 0 ]; then
            echo "unity-slot.sh: project $project đang được một Unity khác mở — chờ nó đóng..." >&2
            announced_project_wait=1
          fi
          break
        fi
        write_lock_details "$slot_path" "$project" "$command_text"
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

# INT/TERM/HUP: dừng lệnh con TRƯỚC rồi mới trả khoá — trả khoá khi Unity còn chạy là mở chỗ cho Unity thứ ba.
stop_child_and_exit() {
  local exit_code=$1
  if [ -n "$child_process_id" ] && kill -0 "$child_process_id" 2>/dev/null; then
    kill -TERM "$child_process_id" 2>/dev/null
    local waited=0
    while kill -0 "$child_process_id" 2>/dev/null && [ "$waited" -lt "$CHILD_STOP_GRACE_SECONDS" ]; do
      sleep 1
      waited=$((waited + 1))
    done
    kill -0 "$child_process_id" 2>/dev/null && kill -KILL "$child_process_id" 2>/dev/null
    wait "$child_process_id" 2>/dev/null
  fi
  release_all_locks
  exit "$exit_code"
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

  trap 'release_all_locks' EXIT
  trap 'stop_child_and_exit 130' INT
  trap 'stop_child_and_exit 143' TERM
  trap 'stop_child_and_exit 129' HUP

  acquire_project_lock "$project" "$*"
  acquire_slot "$project" "$*"
  echo "unity-slot.sh: giữ $(basename "$held_slot") (pid $$, hạn ${timeout_seconds}s)${label:+ — $label}" >&2
  local started exit_code
  started=$(date +%s)
  # Chạy nền rồi wait (không exec/foreground) để ghi được pid của lệnh vào khoá: wrapper bị SIGKILL thì khoá vẫn sống
  # theo lệnh con, không bị dọn trong lúc Unity còn chạy. Lệnh nền của bash không tương tác nhận stdin /dev/null — Unity
  # batchmode không đọc stdin.
  perl -e 'alarm shift @ARGV; exec @ARGV or die "không chạy được lệnh: $!\n"' "$timeout_seconds" "$@" &
  child_process_id=$!
  write_process_record "$held_slot" child_pid "$child_process_id"
  [ -n "$held_project_lock" ] && write_process_record "$held_project_lock" child_pid "$child_process_id"
  wait "$child_process_id"
  exit_code=$?
  child_process_id=""
  release_all_locks
  if [ "$exit_code" = "$TIMEOUT_EXIT_CODE" ]; then
    echo "unity-slot.sh: HẾT HẠN GIỜ sau ${timeout_seconds}s — lệnh bị dừng bằng SIGALRM: $*" >&2
  fi
  echo "unity-slot.sh: trả slot sau $(( $(date +%s) - started ))s, mã thoát $exit_code" >&2
  return "$exit_code"
}

# Chờ tới khi điều kiện (một lệnh bash) đúng, tối đa N giây; thoát 1 khi hết hạn.
wait_for_condition() {
  local limit_seconds=$1 condition=$2 waited=0
  until bash -c "$condition" 2>/dev/null; do
    [ "$waited" -ge $((limit_seconds * 5)) ] && return 1
    sleep 0.2
    waited=$((waited + 1))
  done
  return 0
}

run_self_test() {
  local script_path test_directory
  script_path=$(cd "$(dirname "$0")" && pwd)/$(basename "$0")
  test_directory=$(mktemp -d "${TMPDIR:-/tmp}/unity-slot-self-test.XXXXXX")
  local failures=0
  export LIVEOPS_UNITY_SLOT_DIRECTORY="$test_directory/slots"
  export LIVEOPS_UNITY_SLOTS=2
  local slots="$LIVEOPS_UNITY_SLOT_DIRECTORY"
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
  if ls -d "$slots"/slot-* "$slots"/projects/* >/dev/null 2>&1; then
    echo "LỖI: còn khoá sau khi mọi lệnh xong"; failures=$((failures + 1))
  else
    echo "   OK: mọi khoá đã trả"
  fi

  echo "== self-test 2: khoá của pid đã chết bị dọn"
  bash -c 'exit 0' & local dead_pid=$!
  wait "$dead_pid"
  mkdir -p "$slots/slot-1" "$slots/slot-2"
  echo "$dead_pid" > "$slots/slot-1/pid"
  echo "$dead_pid" > "$slots/slot-2/pid"
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

  echo "== self-test 3: wrapper bị SIGKILL mà lệnh con còn chạy — khoá vẫn giữ, lệnh thứ ba chờ lệnh con xong"
  bash "$script_path" --timeout 60 --label self-test-keep -- bash -c "sleep 9" 2>>"$test_directory/log-keep" &
  local keep_wrapper=$!
  bash "$script_path" --timeout 60 --label self-test-killed -- \
      bash -c "echo \$(date +%s) > '$test_directory/start-killed-child'; sleep 6; echo \$(date +%s) > '$test_directory/end-killed-child'" \
      2>>"$test_directory/log-killed" &
  local killed_wrapper=$!
  # Chờ theo file bắt đầu của lệnh (không theo child_pid) để bài kiểm vẫn bắt được bản script không ghi child_pid.
  wait_for_condition 10 "[ -d '$slots/slot-1' ] && [ -d '$slots/slot-2' ] && [ -f '$test_directory/start-killed-child' ]" || true
  sleep 0.5
  if [ ! -s "$slots/slot-1/child_pid" ] || [ ! -s "$slots/slot-2/child_pid" ]; then
    echo "LỖI: khoá không ghi child_pid"; failures=$((failures + 1))
  fi
  kill -9 "$killed_wrapper" 2>/dev/null
  wait "$killed_wrapper" 2>/dev/null
  local killed_at
  killed_at=$(date +%s)
  if bash "$script_path" --status 2>/dev/null | grep -q "mồ côi"; then
    echo "LỖI: --status báo mồ côi trong khi lệnh con của wrapper bị giết còn chạy"; failures=$((failures + 1))
  fi
  bash "$script_path" --timeout 60 --label self-test-third -- \
      bash -c "echo \$(date +%s) > '$test_directory/start-third'" 2>>"$test_directory/log-third"
  if [ ! -f "$test_directory/start-third" ]; then
    echo "LỖI: lệnh thứ ba không chạy"; failures=$((failures + 1))
  elif [ ! -f "$test_directory/end-killed-child" ]; then
    echo "LỖI: lệnh thứ ba đã chạy xong khi lệnh con của wrapper bị giết còn chạy — khoá bị dọn sớm, 3 lệnh đồng thời"
    failures=$((failures + 1))
  else
    local third_start child_end
    third_start=$(cat "$test_directory/start-third")
    child_end=$(cat "$test_directory/end-killed-child")
    if [ "$third_start" -lt "$child_end" ]; then
      echo "LỖI: lệnh thứ ba bắt đầu ($third_start) trước khi lệnh con mồ côi xong ($child_end) — 3 lệnh đồng thời"
      failures=$((failures + 1))
    else
      echo "   OK: lệnh thứ ba chờ $(( third_start - killed_at ))s sau khi wrapper bị giết, bắt đầu sau khi lệnh con xong"
    fi
  fi
  wait "$keep_wrapper" 2>/dev/null
  bash "$script_path" --clean >/dev/null 2>&1
  if ls -d "$slots"/slot-* "$slots"/projects/* >/dev/null 2>&1; then
    echo "LỖI: còn khoá sau self-test 3"; ls -la "$slots" "$slots/projects"; failures=$((failures + 1))
  fi

  echo "== self-test 4: hai lệnh cùng -projectPath phát cùng lúc chạy nối tiếp (N = 2 còn trống)"
  mkdir -p "$test_directory/project"
  for index in 1 2; do
    bash "$script_path" --timeout 60 --label "self-test-project-$index" -- \
        bash -c "echo \$(date +%s) > '$test_directory/project-start-$index'; sleep 4; echo \$(date +%s) > '$test_directory/project-end-$index'" \
        _ -projectPath "$test_directory/project" 2>>"$test_directory/log-project-$index" &
  done
  wait
  if [ -f "$test_directory/project-start-1" ] && [ -f "$test_directory/project-start-2" ]; then
    local start_one start_two end_one end_two
    start_one=$(cat "$test_directory/project-start-1"); start_two=$(cat "$test_directory/project-start-2")
    end_one=$(cat "$test_directory/project-end-1"); end_two=$(cat "$test_directory/project-end-2")
    if { [ "$start_two" -ge "$end_one" ] || [ "$start_one" -ge "$end_two" ]; }; then
      echo "   OK: không chồng nhau (1: $start_one→$end_one, 2: $start_two→$end_two)"
    else
      echo "LỖI: hai lệnh cùng project chạy chồng nhau (1: $start_one→$end_one, 2: $start_two→$end_two)"; failures=$((failures + 1))
    fi
  else
    echo "LỖI: lệnh cùng project không chạy"; failures=$((failures + 1))
  fi
  if ls -d "$slots"/slot-* "$slots"/projects/* >/dev/null 2>&1; then
    echo "LỖI: còn khoá project sau self-test 4"; failures=$((failures + 1))
  fi

  echo "== self-test 5: hết hạn giờ trả mã $TIMEOUT_EXIT_CODE và trả slot"
  bash "$script_path" --timeout 1 -- sleep 5 2>"$test_directory/log-timeout"
  local timeout_exit=$?
  if [ "$timeout_exit" = "$TIMEOUT_EXIT_CODE" ] && ! ls -d "$slots"/slot-* >/dev/null 2>&1; then
    echo "   OK: mã $timeout_exit, slot đã trả"
  else
    echo "LỖI: mã thoát $timeout_exit (cần $TIMEOUT_EXIT_CODE) hoặc còn khoá"; failures=$((failures + 1))
  fi

  echo "== self-test 6: TERM gửi wrapper thì lệnh con bị dừng trước khi trả khoá"
  bash "$script_path" --timeout 60 --label self-test-term -- bash -c "sleep 30" 2>>"$test_directory/log-term" &
  local term_wrapper=$!
  wait_for_condition 10 "[ -s '$slots/slot-1/child_pid' ]" || true
  local term_child
  term_child=$(recorded_process_id "$slots/slot-1" child_pid)
  kill -TERM "$term_wrapper"
  wait "$term_wrapper" 2>/dev/null
  if [ -n "$term_child" ] && ! kill -0 "$term_child" 2>/dev/null && ! ls -d "$slots"/slot-* >/dev/null 2>&1; then
    echo "   OK: lệnh con $term_child đã dừng, khoá đã trả"
  else
    echo "LỖI: lệnh con '$term_child' còn chạy hoặc còn khoá sau TERM"; failures=$((failures + 1))
    [ -n "$term_child" ] && kill -KILL "$term_child" 2>/dev/null
  fi

  echo "== self-test 7: số slot ngoài 1–3 bị từ chối"
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
      --clean) clean_orphaned_locks; print_status; exit 0;;
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
