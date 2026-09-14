#!/bin/bash
# check-interim.sh — liệt kê nhánh tạm có chủ đích (INTERIM) và chặn khi gói gỡ còn để lại dấu (mục 12, PD-28).
#
# Vì sao: trạng thái dev "cố ý khác thiết kế" (section giữ chỗ, nút disabled lý do tạm) không được lọt thành cắt âm thầm.
# Dấu gồm comment `// INTERIM(<gói gỡ>): …` và định danh bắt đầu bằng `Interim` (hằng lý do tạm, lớp giữ chỗ).
#
# Cách dùng:
#   check-interim.sh [--repository <worktree>]              # liệt kê mọi dấu, thoát 0
#   check-interim.sh --for <gói> [--repository <worktree>]  # thoát 1 nếu còn INTERIM(<gói>) hoặc định danh Interim trong
#                                                           # file có INTERIM(<gói>) (gói gỡ chạy ở cổng gói)
#   check-interim.sh --none [--repository <worktree>]       # thoát 1 nếu còn BẤT KỲ dấu nào (G-ACCEPT)
# Phạm vi: Packages/com.dreamtech.liveops và Assets/Demo (.cs, .uxml, .uss).

set -euo pipefail

readonly PACKAGE_RELATIVE_PATH=Packages/com.dreamtech.liveops
readonly DEMO_RELATIVE_PATH=Assets/Demo

script_directory=$(cd "$(dirname "$0")" && pwd)
repository=${REPOSITORY:-}
mode=list
target_package=""

while [ "$#" -gt 0 ]; do
  case "$1" in
    --repository) [ "$#" -ge 2 ] || { echo "check-interim.sh: --repository cần đường dẫn" >&2; exit 2; }; repository=$2; shift 2;;
    --for) [ "$#" -ge 2 ] || { echo "check-interim.sh: --for cần tên gói" >&2; exit 2; }; mode=for; target_package=$2; shift 2;;
    --none) mode=none; shift;;
    --help|-h) sed -n '2,14p' "$0" | sed 's/^# \{0,1\}//'; exit 0;;
    *) echo "check-interim.sh: tham số lạ '$1'" >&2; exit 2;;
  esac
done

if [ -n "$repository" ]; then
  repository=$(cd "$repository" && pwd -P)
elif top_level=$(git rev-parse --show-toplevel 2>/dev/null) && [ -d "$top_level/$PACKAGE_RELATIVE_PATH" ]; then
  repository=$top_level
else
  repository=$(cd "$script_directory/../.." && pwd -P)
fi

python3 - "$repository" "$mode" "$target_package" "$PACKAGE_RELATIVE_PATH" "$DEMO_RELATIVE_PATH" <<'PYTHON'
import os
import re
import sys

repository, mode, target_package, package_path, demo_path = sys.argv[1:6]
mark_pattern = re.compile(r"INTERIM\(([^)]*)\)")
identifier_pattern = re.compile(r"\bInterim[A-Z]\w*")

marks = []        # (đường dẫn, dòng, gói, nội dung)
identifiers = []  # (đường dẫn, dòng, định danh)
files_by_package = {}
for root in (package_path, demo_path):
    absolute_root = os.path.join(repository, root)
    if not os.path.isdir(absolute_root):
        continue
    for directory, directory_names, names in os.walk(absolute_root):
        directory_names[:] = [name for name in directory_names if not name.startswith(".")]
        for name in sorted(names):
            if not name.endswith((".cs", ".uxml", ".uss")):
                continue
            absolute = os.path.join(directory, name)
            relative = os.path.relpath(absolute, repository).replace(os.sep, "/")
            with open(absolute, encoding="utf-8-sig", errors="replace") as handle:
                for number, line in enumerate(handle, start=1):
                    for match in mark_pattern.finditer(line):
                        owner = match.group(1).strip()
                        marks.append((relative, number, owner, line.strip()))
                        files_by_package.setdefault(owner, set()).add(relative)
                    for match in identifier_pattern.finditer(line):
                        identifiers.append((relative, number, match.group(0)))
            # Tên file Interim*.cs cũng là dấu (lớp giữ chỗ phải bị xoá cùng file).
            if name.startswith("Interim"):
                identifiers.append((relative, 0, name))

for relative, number, owner, text in marks:
    print("INTERIM(%s)  %s:%d  %s" % (owner, relative, number, text[:120]))
unique_identifiers = sorted(set((relative, number, name) for relative, number, name in identifiers))
for relative, number, name in unique_identifiers:
    print("Interim      %s:%d  %s" % (relative, number, name))
print("tổng: %d dấu INTERIM, %d định danh Interim" % (len(marks), len(unique_identifiers)))

if mode == "for":
    remaining_marks = [mark for mark in marks if mark[2] == target_package]
    owned_files = files_by_package.get(target_package, set())
    remaining_identifiers = [item for item in unique_identifiers if item[0] in owned_files]
    if remaining_marks or remaining_identifiers:
        print("INTERIM CHECK FAILED: còn %d dấu INTERIM(%s) và %d định danh Interim trong file của các dấu đó" % (
            len(remaining_marks), target_package, len(remaining_identifiers)))
        sys.exit(1)
    print("INTERIM CHECK OK: không còn dấu của %s" % target_package)
elif mode == "none":
    if marks or unique_identifiers:
        print("INTERIM CHECK FAILED: còn nhánh tạm — bản phát hành không được mang dấu nào (G-ACCEPT)")
        sys.exit(1)
    print("INTERIM CHECK OK: không còn nhánh tạm")
PYTHON
