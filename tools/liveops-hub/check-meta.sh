#!/bin/bash
# check-meta.sh — kiểm .meta của package và demo: đủ, không mồ côi, không trùng GUID, không bị sửa (PD-27).
#
# Vì sao: package cài bằng git là chỉ đọc — file thiếu .meta bị Unity của game bỏ qua và GUID đổi giữa các máy; hai gói
# cùng tạo một thư mục ra hai GUID; lượt chạy Unity 2022.3 có thể serialize lại .meta (uss/uxml/png) mà không ai để ý.
# Kiểm:
#   1. mọi file và thư mục trong Packages/com.dreamtech.liveops (trừ gốc package) và Assets/Demo (gồm Assets/Demo) có .meta
#   2. không .meta mồ côi (tài sản đã xoá mà .meta còn)
#   3. không hai .meta cùng GUID trong Assets/ và Packages/
#   4. `git status --porcelain -- '*.meta'` không có dòng sửa (M) — .meta chỉ được thêm mới (??/A)
# Bỏ qua như Unity: tên bắt đầu bằng '.', thư mục kết thúc '~'. Chỉ xét file git thấy (tracked + untracked không ignore).
#
# Cách dùng: check-meta.sh [<worktree>] [--repository <worktree>]
# In "META CHECK OK" và thoát 0 khi sạch; thoát 1 khi có lỗi.

set -euo pipefail

readonly PACKAGE_RELATIVE_PATH=Packages/com.dreamtech.liveops
readonly DEMO_RELATIVE_PATH=Assets/Demo

repository=${REPOSITORY:-}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --repository) repository=$2; shift 2;;
    --help|-h) sed -n '2,16p' "$0" | sed 's/^# \{0,1\}//'; exit 0;;
    -*) echo "check-meta.sh: tham số lạ '$1'" >&2; exit 2;;
    *) repository=$1; shift;;
  esac
done

if [ -n "$repository" ]; then
  repository=$(cd "$repository" && pwd -P)
elif top_level=$(git rev-parse --show-toplevel 2>/dev/null) && [ -d "$top_level/$PACKAGE_RELATIVE_PATH" ]; then
  repository=$top_level
else
  echo "check-meta.sh: thư mục hiện tại không thuộc repo có $PACKAGE_RELATIVE_PATH — truyền --repository <worktree>" >&2
  echo "  (không tự rơi về repo chứa script: gọi bằng đường dẫn tuyệt đối từ cwd khác sẽ kiểm nhầm worktree G-TOOLS)" >&2
  exit 2
fi
echo "check-meta.sh: repository=$repository" >&2

python3 - "$repository" "$PACKAGE_RELATIVE_PATH" "$DEMO_RELATIVE_PATH" <<'PYTHON'
import os
import re
import subprocess
import sys

repository, package_path, demo_path = sys.argv[1], sys.argv[2], sys.argv[3]


def git_files(*paths):
    output = subprocess.check_output(
        ["git", "-C", repository, "ls-files", "--cached", "--others", "--exclude-standard", "-z", "--"] + list(paths))
    return [path for path in output.decode("utf-8").split("\0") if path]


def ignored_by_unity(relative_path):
    for part in relative_path.split("/"):
        if part.startswith(".") or part.endswith("~"):
            return True
    return False


errors = []
# .meta của chính Assets/Demo nằm ngoài thư mục đó nên phải xin git riêng.
files = [path for path in git_files(package_path, demo_path, demo_path + ".meta") if os.path.exists(os.path.join(repository, path))]
file_set = set(files)

assets = set()
directories = set()
for path in files:
    if ignored_by_unity(path):
        continue
    if path.endswith(".meta"):
        continue
    assets.add(path)
    parent = os.path.dirname(path)
    while parent:
        if parent == package_path or parent in ("Packages", "Assets", ""):
            break
        directories.add(parent)
        parent = os.path.dirname(parent)
if any(path.startswith(demo_path + "/") for path in assets):
    directories.add(demo_path)

for asset in sorted(assets | directories):
    if asset + ".meta" not in file_set:
        kind = "thư mục" if asset in directories else "file"
        errors.append("thiếu .meta cho %s %s" % (kind, asset))

for path in sorted(file_set):
    if not path.endswith(".meta") or ignored_by_unity(path):
        continue
    target = path[:-len(".meta")]
    if not os.path.exists(os.path.join(repository, target)):
        errors.append(".meta mồ côi %s (không còn %s)" % (path, target))
    elif os.path.isdir(os.path.join(repository, target)) and target not in directories:
        # Thư mục rỗng (git không theo dõi) vẫn có .meta: Unity giữ được, nhưng máy khác clone về sẽ thiếu thư mục.
        errors.append(".meta của thư mục rỗng %s (git không mang thư mục rỗng — thêm file thật hoặc xoá .meta)" % path)

guid_pattern = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.M)
owners_by_guid = {}
for path in git_files("Assets", "Packages"):
    if not path.endswith(".meta"):
        continue
    absolute = os.path.join(repository, path)
    if not os.path.isfile(absolute):
        continue
    with open(absolute, encoding="utf-8", errors="replace") as handle:
        match = guid_pattern.search(handle.read())
    if not match:
        errors.append(".meta không có guid: %s" % path)
        continue
    owners_by_guid.setdefault(match.group(1), []).append(path)
for guid, owners in sorted(owners_by_guid.items()):
    if len(owners) > 1:
        errors.append("trùng GUID %s: %s" % (guid, ", ".join(owners)))

status = subprocess.check_output(["git", "-C", repository, "status", "--porcelain", "--", "*.meta"]).decode("utf-8")
for line in status.splitlines():
    code = line[:2]
    if "M" in code:
        errors.append(".meta bị sửa (lượt Unity serialize lại?): %s — git checkout -- '<file>' rồi ghi rủi ro vào báo cáo gói" % line[3:])
    elif "D" in code:
        print("thông tin: .meta bị xoá %s (hợp lệ khi gói xoá tài sản tương ứng)" % line[3:])

checked = len(assets) + len(directories)
if errors:
    for error in errors:
        print("LỖI: " + error)
    print("META CHECK FAILED (%d lỗi, %d tài sản/thư mục)" % (len(errors), checked))
    sys.exit(1)
print("META CHECK OK (%d tài sản/thư mục, %d GUID)" % (checked, len(owners_by_guid)))
PYTHON
