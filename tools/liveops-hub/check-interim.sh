#!/bin/bash
# check-interim.sh — liệt kê nhánh tạm có chủ đích (INTERIM) và chặn khi gói gỡ còn để lại dấu (mục 12, PD-28).
#
# Vì sao: trạng thái dev "cố ý khác thiết kế" (section giữ chỗ, nút disabled lý do tạm) không được lọt thành cắt âm thầm.
# Dấu gồm comment `// INTERIM(<gói gỡ>): …` và định danh bắt đầu bằng `Interim` (hằng lý do tạm, lớp giữ chỗ).
#
# Cách dùng:
#   check-interim.sh [--repository <worktree>]              # liệt kê mọi dấu, thoát 0
#   check-interim.sh --for <gói> [--repository <worktree>]  # thoát 1 nếu còn dấu của gói gỡ (gói gỡ chạy ở cổng gói):
#       - comment INTERIM(<gói>);
#       - file/định danh mục 12 giao cho gói xoá (sổ INTERIM_REGISTER bên dưới) còn ở BẤT KỲ đâu, dù comment đã bị xoá;
#       - file ownership.tsv ghi "<gói> xoá" còn tồn tại (cả .meta);
#       - định danh Interim* còn trong file có INTERIM(<gói>) hoặc file ownership.tsv cho gói ghi (trừ định danh sổ giao gói khác).
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
  echo "check-interim.sh: thư mục hiện tại không thuộc repo có $PACKAGE_RELATIVE_PATH — truyền --repository <worktree>" >&2
  echo "  (không tự rơi về repo chứa script: gọi bằng đường dẫn tuyệt đối từ cwd khác sẽ kiểm nhầm worktree G-TOOLS)" >&2
  exit 2
fi
echo "check-interim.sh: repository=$repository" >&2

python3 - "$repository" "$mode" "$target_package" "$PACKAGE_RELATIVE_PATH" "$DEMO_RELATIVE_PATH" "$script_directory" <<'PYTHON'
import os
import re
import sys

repository, mode, target_package, package_path, demo_path, script_directory = sys.argv[1:7]

# Sổ mục 12 của kế hoạch: gói gỡ → file phải bị xoá + định danh không được còn ở đâu. Vì sao cần sổ riêng: gói gỡ có thể xoá
# comment INTERIM mà giữ file/hằng (dấu còn nhưng không còn comment để nối về gói), và hằng chuỗi tạm nằm ở file Strings của
# gói khác (vd LiveOpsHubStrings.Kit.cs) mà gói gỡ không có quyền ghi — chỉ sổ theo tên mới bắt được.
EDITOR_HUB = package_path + "/Editor/Hub/"
INTERIM_REGISTER = {
    "G-VALIDATOR-B": {  # I-1
        "files": [package_path + "/Runtime/Core/Calendar/Validation/Rules/NotYetImplementedRule.cs"],
        "identifiers": ["NotYetImplementedRule"],
    },
    "G-SHELLPOLISH": {  # I-2, I-8, I-9
        "files": [EDITOR_HUB + "Sections/InterimPlaceholderSection.cs"],
        "identifiers": ["InterimPlaceholderSection", "InterimPlaceholderReason", "InterimDiskConflictLog"],
    },
    "G-PASTE": {  # I-3
        "files": [EDITOR_HUB + "Services/InterimUnavailableHubActions.cs"],
        "identifiers": ["InterimUnavailableHubActions", "InterimPasteNotBuiltReason"],
    },
    "G-VALIDATION-DEPTH": {"files": [], "identifiers": ["InterimBulkRepairReason"]},  # I-7
    "G-CALENDAR-DEPTH": {"files": [], "identifiers": []},  # I-4, I-5: chỉ comment
    "G-RECURRING-JSON": {"files": [], "identifiers": []},  # I-6: chỉ comment
}


def glob_to_regex(pattern):
    result = []
    index = 0
    while index < len(pattern):
        if pattern.startswith("/**", index) and index + 3 == len(pattern):
            result.append("(?:/.*)?")
            index += 3
            continue
        if pattern.startswith("**", index):
            result.append(".*")
            index += 2
            continue
        character = pattern[index]
        result.append("[^/]*" if character == "*" else "[^/]" if character == "?" else re.escape(character))
        index += 1
    return re.compile("^" + "".join(result) + "$")


def load_ownership(path):
    """(dòng file: (regex, pattern, [gói], ghi chú)), tập tên gói."""
    rows = []
    packages = set()
    if not os.path.isfile(path):
        return rows, packages
    with open(path, encoding="utf-8") as handle:
        for raw_line in handle:
            columns = raw_line.rstrip("\n").split("\t")
            if columns[0] == "package" and len(columns) > 1:
                packages.add(columns[1])
            elif columns[0] == "file" and len(columns) > 2:
                rows.append((glob_to_regex(columns[1]), columns[1], columns[2].split(","), columns[3] if len(columns) > 3 else ""))
    return rows, packages
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
    ownership_path = os.path.join(repository, "tools", "liveops-hub", "ownership.tsv")
    if not os.path.isfile(ownership_path):
        ownership_path = os.path.join(script_directory, "ownership.tsv")
    ownership_rows, known_packages = load_ownership(ownership_path)
    if target_package not in known_packages and target_package not in INTERIM_REGISTER and not re.match(r"^G-FIX-W\d+-\d+$", target_package):
        print("check-interim.sh: gói lạ '%s' (không có trong %s)" % (target_package, ownership_path), file=sys.stderr)
        sys.exit(2)
    problems = []
    for relative, number, owner, text in marks:
        if owner == target_package:
            problems.append("%s:%d  còn comment INTERIM(%s)" % (relative, number, owner))

    register = INTERIM_REGISTER.get(target_package, {"files": [], "identifiers": []})
    files_to_delete = set(register["files"])
    note_pattern = re.compile(r"(?:^|[;,\s])%s\s+xoá\b" % re.escape(target_package))
    for _, pattern, _, note in ownership_rows:
        if note_pattern.search(note) and not any(character in pattern for character in "*?"):
            files_to_delete.add(pattern)
    for relative in sorted(files_to_delete):
        for candidate in (relative, relative + ".meta"):
            if os.path.exists(os.path.join(repository, candidate)):
                problems.append("%s  file phải bị %s xoá (mục 12 / ownership.tsv) vẫn còn" % (candidate, target_package))

    registered_names = set(register["identifiers"])
    names_of_other_packages = set()
    for package, entry in INTERIM_REGISTER.items():
        if package != target_package:
            names_of_other_packages.update(entry["identifiers"])
    registered_pattern = re.compile(r"\b(%s)\b" % "|".join(sorted(registered_names))) if registered_names else None
    marked_files = files_by_package.get(target_package, set())

    def writable_by_target(relative):
        return any(regex.match(relative) and (target_package in owners or "*" in owners) for regex, _, owners, _ in ownership_rows)

    for relative, number, name in unique_identifiers:
        base_name = os.path.splitext(name)[0] if number == 0 else name
        if base_name in names_of_other_packages:
            continue
        if base_name in registered_names:
            continue  # báo ở vòng quét theo sổ bên dưới (mọi file, kể cả dạng không bắt đầu bằng Interim)
        if relative in marked_files or writable_by_target(relative):
            problems.append("%s:%d  còn định danh %s trong file của %s" % (relative, number, name, target_package))
    if registered_pattern:
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
                            for match in registered_pattern.finditer(line):
                                problems.append("%s:%d  còn định danh %s (mục 12 giao %s gỡ)" % (relative, number, match.group(1), target_package))

    if problems:
        for problem in problems:
            print("  " + problem)
        print("INTERIM CHECK FAILED: còn %d dấu của %s" % (len(problems), target_package))
        sys.exit(1)
    print("INTERIM CHECK OK: không còn dấu của %s" % target_package)
elif mode == "none":
    if marks or unique_identifiers:
        print("INTERIM CHECK FAILED: còn nhánh tạm — bản phát hành không được mang dấu nào (G-ACCEPT)")
        sys.exit(1)
    print("INTERIM CHECK OK: không còn nhánh tạm")
PYTHON
