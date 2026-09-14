#!/bin/bash
# run-editmode.sh — chạy test Unity (EditMode mặc định) qua unity-slot.sh, đọc XML kết quả, thoát ≠ 0 khi không xanh.
#
# Vì sao: exit code của Unity -runTests không đủ tin (lỗi compile có thể làm chạy 0 test và vẫn "xong"; XML thiếu khi Unity
# chết). Script này là đường DUY NHẤT gói việc chạy test: luôn qua slot (trần Unity đồng thời PD-26), luôn có hạn giờ, và
# kết luận từ XML + log chứ không từ exit code.
# Category (mục 9.1): LiveOpsHub.Logic chạy được -nographics; LiveOpsHub.UI PHẢI chạy không -nographics (layout NaN, không
# vẽ, không nhận phím). Test cũ không gắn category tính là Logic. `all` chạy mọi test, không -nographics.
# Cú pháp lọc category của UTF 1.1.33 chốt ở SP-15: mặc định dùng -testCategory ("!LiveOpsHub.UI" cho Logic);
# --category-mode fixture là phương án dự phòng (lọc bằng tên fixture chứa ".Hub." thay category).
#
# Cách dùng:
#   run-editmode.sh --unity 6000|2022 [--project <đường dẫn>] [--repository <worktree>] [--category Logic|UI|all]
#                   [--filter <regex>] [--results <file.xml>] [--timeout GIÂY] [--allow-empty] [--category-mode category|fixture]
# Mặc định project: 6000 → worktree (git top-level); 2022 → ~/.cache/unity-liveops/temp-2022/<gói> (make-temp-project-2022.sh).
# In "N passed / M failed / K skipped" + tên test fail; thoát 1 khi fail, thiếu XML, total = 0 (trừ --allow-empty),
# hoặc log có "error CS"; 2 khi dùng sai; 3 khi ổ đĩa < 5 GB.

set -uo pipefail

readonly UNITY_2022=/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity
readonly UNITY_6000=/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity
readonly PACKAGE_RELATIVE_PATH=Packages/com.dreamtech.liveops
readonly UI_CATEGORY=LiveOpsHub.UI
readonly LOGIC_CATEGORY=LiveOpsHub.Logic
readonly DEFAULT_TEST_TIMEOUT_SECONDS=1500
readonly MINIMUM_FREE_GIGABYTES=5

script_directory=$(cd "$(dirname "$0")" && pwd)
repository=${REPOSITORY:-}
unity_version=""
project=""
category=all
filter=""
results=""
timeout_seconds=$DEFAULT_TEST_TIMEOUT_SECONDS
allow_empty=0
category_mode=${LIVEOPS_TEST_CATEGORY_MODE:-category}
platform=EditMode
graphics_mode=auto

fail_usage() { echo "run-editmode.sh: $1" >&2; exit 2; }

while [ "$#" -gt 0 ]; do
  case "$1" in
    --unity) [ "$#" -ge 2 ] || fail_usage "--unity cần 6000|2022"; unity_version=$2; shift 2;;
    --project) [ "$#" -ge 2 ] || fail_usage "--project cần đường dẫn"; project=$2; shift 2;;
    --repository) [ "$#" -ge 2 ] || fail_usage "--repository cần đường dẫn"; repository=$2; shift 2;;
    --category) [ "$#" -ge 2 ] || fail_usage "--category cần Logic|UI|all"; category=$2; shift 2;;
    --filter) [ "$#" -ge 2 ] || fail_usage "--filter cần regex"; filter=$2; shift 2;;
    --results) [ "$#" -ge 2 ] || fail_usage "--results cần đường dẫn"; results=$2; shift 2;;
    --timeout) [ "$#" -ge 2 ] || fail_usage "--timeout cần số giây"; timeout_seconds=$2; shift 2;;
    --allow-empty) allow_empty=1; shift;;
    --category-mode) [ "$#" -ge 2 ] || fail_usage "--category-mode cần category|fixture"; category_mode=$2; shift 2;;
    --platform) [ "$#" -ge 2 ] || fail_usage "--platform cần EditMode|PlayMode"; platform=$2; shift 2;;
    --graphics) graphics_mode=on; shift;;
    --nographics) graphics_mode=off; shift;;
    --help|-h) sed -n '2,20p' "$0" | sed 's/^# \{0,1\}//'; exit 0;;
    *) fail_usage "tham số lạ '$1'";;
  esac
done

case "$unity_version" in
  6000|6000.6) unity_version=6000; unity_binary=$UNITY_6000; unity_label=6000.6;;
  2022|2022.3) unity_version=2022; unity_binary=$UNITY_2022; unity_label=2022.3;;
  *) fail_usage "--unity phải là 6000 hoặc 2022";;
esac
case "$category" in
  Logic|logic) category=Logic;;
  UI|ui) category=UI;;
  all|All) category=all;;
  *) fail_usage "--category phải là Logic, UI hoặc all";;
esac
case "$platform" in EditMode|PlayMode) ;; *) fail_usage "--platform phải là EditMode hoặc PlayMode";; esac
case "$category_mode" in category|fixture) ;; *) fail_usage "--category-mode phải là category hoặc fixture";; esac
[ -x "$unity_binary" ] || fail_usage "thiếu Unity ở $unity_binary"

resolve_repository() {
  if [ -n "$repository" ]; then (cd "$repository" && pwd -P); return; fi
  local top_level
  if top_level=$(git rev-parse --show-toplevel 2>/dev/null) && [ -d "$top_level/$PACKAGE_RELATIVE_PATH" ]; then
    echo "$top_level"; return
  fi
  return 1
}
if ! repository=$(resolve_repository); then
  echo "run-editmode.sh: thư mục hiện tại không thuộc repo có $PACKAGE_RELATIVE_PATH — truyền --repository <worktree>" >&2
  echo "  (không tự rơi về repo chứa script: gọi bằng đường dẫn tuyệt đối từ cwd khác sẽ kiểm nhầm worktree G-TOOLS)" >&2
  exit 2
fi
echo "run-editmode.sh: repository=$repository" >&2

package_name() {
  local branch
  branch=$(git -C "$repository" branch --show-current 2>/dev/null || true)
  case "$branch" in
    wt/*) echo "${branch#wt/}";;
    *) basename "$repository";;
  esac
}

if [ -z "$project" ]; then
  if [ "$unity_version" = 6000 ]; then
    project=$repository
  else
    project=$HOME/.cache/unity-liveops/temp-2022/$(package_name)
    [ -f "$project/Packages/manifest.json" ] || fail_usage "chưa có project tạm 2022.3 ở $project — chạy make-temp-project-2022.sh trước"
  fi
fi
[ -d "$project" ] || fail_usage "không thấy project $project"
project=$(cd "$project" && pwd -P)

free_gigabytes=$(df -g /System/Volumes/Data | awk 'NR==2 {print $4}')
if [ "${free_gigabytes:-0}" -lt "$MINIMUM_FREE_GIGABYTES" ]; then
  echo "LỖI: ổ đĩa còn ${free_gigabytes} GB (< $MINIMUM_FREE_GIGABYTES GB) — không mở Unity" >&2
  exit 3
fi

timestamp=$(date +%Y%m%d-%H%M%S)
if [ -z "$results" ]; then
  results_directory=$HOME/.cache/unity-liveops/test-results/$(package_name)
  results=$results_directory/$(echo "$platform" | tr '[:upper:]' '[:lower:]')-$unity_label-$category-$timestamp.xml
fi
mkdir -p "$(dirname "$results")"
results=$(cd "$(dirname "$results")" && pwd -P)/$(basename "$results")
log_file=${results%.xml}.log
rm -f "$results"

unity_arguments=(-batchmode -projectPath "$project" -runTests -testPlatform "$platform" -testResults "$results" -logFile "$log_file")
# -nographics: Logic mặc định có (chạy được CI headless); UI và all không có. PlayMode theo ma trận CLAUDE.md gốc (có).
use_nographics=0
case "$graphics_mode" in
  on) use_nographics=0;;
  off) use_nographics=1;;
  auto)
    if [ "$category" = Logic ]; then use_nographics=1; fi
    if [ "$platform" = PlayMode ]; then use_nographics=1; fi;;
esac
[ "$use_nographics" = 1 ] && unity_arguments+=(-nographics)

combined_filter=$filter
if [ "$category_mode" = category ]; then
  case "$category" in
    Logic) unity_arguments+=(-testCategory "!$UI_CATEGORY");;
    UI) unity_arguments+=(-testCategory "$UI_CATEGORY");;
  esac
else
  # Dự phòng SP-15: test UI chỉ nằm trong assembly Editor.Tests (namespace DreamTech.LiveOps.Editor.Tests) và mang
  # category trong XML; lọc bằng regex tên. Logic = mọi test không thuộc fixture UI — lọc sau khi đọc XML.
  case "$category" in
    UI) combined_filter=${filter:-.*};;
  esac
fi
[ -n "$combined_filter" ] && unity_arguments+=(-testFilter "$combined_filter")

echo "run-editmode.sh: $platform $unity_label category=$category filter='${filter}' project=$project"
echo "   results: $results"
echo "   log:     $log_file"
unity_status=0
"$script_directory/unity-slot.sh" --timeout "$timeout_seconds" --label "$platform $unity_label $category $(package_name)" -- \
  "$unity_binary" "${unity_arguments[@]}" || unity_status=$?

failures=0
if [ -f "$log_file" ] && grep -q "error CS[0-9]" "$log_file"; then
  echo "LỖI: log có lỗi compile:"
  grep "error CS[0-9]" "$log_file" | sort -u | head -n 30 | sed 's/^/   /'
  failures=$((failures + 1))
fi
if [ ! -s "$results" ]; then
  echo "LỖI: không có XML kết quả (Unity thoát $unity_status) — xem $log_file"
  [ -f "$log_file" ] && tail -n 30 "$log_file" | sed 's/^/   /'
  exit 1
fi

summary_status=0
python3 - "$results" "$category" "$category_mode" "$UI_CATEGORY" "$allow_empty" <<'PYTHON' || summary_status=$?
import sys
import xml.etree.ElementTree as ElementTree

results_path, category, category_mode, ui_category, allow_empty = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4], sys.argv[5] == "1"
root = ElementTree.parse(results_path).getroot()

def categories_of(element, inherited):
    names = set(inherited)
    properties = element.find("properties")
    if properties is not None:
        for property_element in properties.findall("property"):
            if property_element.get("name") == "Category":
                names.add(property_element.get("value"))
    return names

cases = []
def walk(element, inherited):
    names = categories_of(element, inherited)
    if element.tag == "test-case":
        cases.append((element, names))
        return
    for child in element:
        if child.tag in ("test-suite", "test-case"):
            walk(child, names)

for child in root:
    if child.tag in ("test-suite", "test-case"):
        walk(child, set())

# Chế độ fixture: Unity đã chạy mọi test khớp regex; lọc Logic/UI ở đây theo category ghi trong XML.
if category_mode == "fixture" and category != "all":
    if category == "UI":
        cases = [(case, names) for case, names in cases if ui_category in names]
    else:
        cases = [(case, names) for case, names in cases if ui_category not in names]

passed = failed = skipped = 0
failed_cases = []
for case, _ in cases:
    result = case.get("result", "")
    if result == "Passed":
        passed += 1
    elif result == "Failed":
        failed += 1
        message_element = case.find("failure/message")
        message = (message_element.text or "").strip() if message_element is not None else ""
        failed_cases.append((case.get("fullname", case.get("name", "?")), message))
    else:
        skipped += 1
total = passed + failed + skipped
print("%d passed / %d failed / %d skipped (total %d)" % (passed, failed, skipped, total))
for name, message in failed_cases:
    first_line = message.splitlines()[0] if message else ""
    print("   FAIL %s — %s" % (name, first_line))
if failed:
    sys.exit(1)
if total == 0 and not allow_empty:
    print("LỖI: không test nào chạy (bộ lọc sai hoặc assembly test không compile) — dùng --allow-empty nếu cố ý")
    sys.exit(1)
sys.exit(0)
PYTHON

if [ "$summary_status" != 0 ]; then failures=$((failures + 1)); fi
if [ "$unity_status" = 142 ]; then
  echo "LỖI: hết hạn giờ ${timeout_seconds}s"
  failures=$((failures + 1))
fi
if [ "$failures" != 0 ]; then
  echo "TESTS FAILED ($platform $unity_label $category)"
  exit 1
fi
echo "TESTS OK ($platform $unity_label $category)"
exit 0
