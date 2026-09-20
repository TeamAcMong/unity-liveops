#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""check-ownership.py — tập file một gói đã chạm phải nằm trong quyền ghi của gói (mục 2 + 10.4 của kế hoạch).

Vì sao: gói trong một đợt chạy song song trên worktree riêng rồi merge ở cổng đợt. Hai gói sửa chung một file = xung đột
merge; hai gói cùng tạo một thư mục mới = hai GUID .meta cho cùng thư mục. Bảng quyền ghi là hợp đồng; script là cổng máy
của hợp đồng đó (PD-27, PD-29, R-19, R-31).

Cách dùng:
  check-ownership.py <gói> [--repository <worktree>] [--base <ref>] [--table <ownership.tsv>]
      so `git diff` từ merge-base với feature/liveops-hub-p1 (gồm file chưa commit và chưa track) với bảng
  check-ownership.py --wave-deps [--table <ownership.tsv>]
      báo lỗi khi một gói phụ thuộc gói cùng đợt hoặc đợt sau (V-9)
Thoát 0 khi sạch, 1 khi vi phạm, 2 khi dùng sai.
"""

import argparse
import os
import re
import subprocess
import sys

DEFAULT_BASE_BRANCH = "feature/liveops-hub-p1"
PACKAGE_PREFIX = "Packages/com.dreamtech.liveops/"

# Nhãn đợt: "W" + số (lẻ được: W3.5 của G-I18N, W9.5 của G-W9-GATE) + hậu tố tuỳ chọn của một LƯỢT bên trong đợt
# ("W8-UX2" = lượt 2 của đợt sửa UI/UX W8). Có hậu tố vì một đợt dài chạy nhiều lượt sửa nối nhau, mà mỗi lượt vẫn
# phải xếp SAU lượt trước — ghi chung "W8" thì bảng không diễn tả được thứ tự đó, còn đánh số lẻ W8.1/W8.2 thì mọi
# báo cáo cũ (viết "W8-UX2") trỏ sai đợt.
WAVE_PATTERN = re.compile(r"^W(\d+(?:\.\d+)?)(?:-([A-Za-z0-9]+))?$")


def wave_sort_key(wave):
    """Khoá xếp thứ tự của một nhãn đợt: (số đợt, hậu tố lượt).

    So bằng TUPLE chứ không bằng một số thực: hậu tố là chữ ("UX2") nên không nhét vào phần thập phân được, mà bản cũ
    gọi thẳng float(wave[1:]) nên đổi cột đợt sang "W8-UX2" là vỡ ngay ở dòng đầu (ValueError: could not convert
    '8-UX2'). Chuỗi rỗng đứng trước mọi chuỗi khác, nên "W8" < "W8-UX2" < "W9" — đúng nghĩa "lượt 2 nằm trong đợt 8
    nhưng sau phần gốc của đợt 8".
    """
    match = WAVE_PATTERN.match(wave or "")
    if match is None:
        raise ValueError("nhãn đợt lạ '%s' — phải dạng W<số> hoặc W<số>-<lượt>, ví dụ W8, W3.5, W8-UX2" % wave)
    return (float(match.group(1)), match.group(2) or "")


def glob_to_regex(pattern):
    # `**` khớp mọi tầng, `*` trong một tầng; "X/**" khớp cả chính X (thư mục gốc của nhánh).
    index = 0
    result = []
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
        if character == "*":
            result.append("[^/]*")
        elif character == "?":
            result.append("[^/]")
        else:
            result.append(re.escape(character))
        index += 1
    return re.compile("^" + "".join(result) + "$")


def package_pattern_matches(pattern, package):
    if pattern == "*":
        return True
    return glob_to_regex(pattern).match(package) is not None


class OwnershipTable(object):
    def __init__(self, path):
        self.packages = {}
        self.package_order = []
        self.file_rows = []       # (regex, pattern, [gói], ghi chú)
        self.directory_rows = []  # (regex, pattern, gói, đợt)
        with open(path, encoding="utf-8") as handle:
            for number, raw_line in enumerate(handle, start=1):
                line = raw_line.rstrip("\n")
                if not line or line.startswith("#"):
                    continue
                columns = line.split("\t")
                kind = columns[0]
                if kind == "package":
                    dependencies = [] if columns[4] == "-" else columns[4].split(",")
                    self.packages[columns[1]] = {"wave": columns[2], "size": columns[3], "dependencies": dependencies}
                    self.package_order.append(columns[1])
                elif kind == "file":
                    self.file_rows.append((glob_to_regex(columns[1]), columns[1], columns[2].split(","), columns[3] if len(columns) > 3 else ""))
                elif kind == "directory":
                    self.directory_rows.append((glob_to_regex(columns[1]), columns[1], columns[2], columns[3]))
                elif kind == "region":
                    continue
                else:
                    raise ValueError("%s:%d: loại dòng lạ '%s'" % (path, number, kind))

    def wave_of(self, package):
        if package in self.packages:
            return self.packages[package]["wave"]
        match = re.match(r"^G-FIX-(W\d+)-\d+$", package)
        if match:
            return match.group(1)
        return None

    def file_packages(self, path):
        owners = []
        for regex, _, packages, _ in self.file_rows:
            if regex.match(path):
                owners.extend(packages)
        return owners

    def allows_file(self, package, path):
        return any(package_pattern_matches(pattern, package) for pattern in self.file_packages(path))


def git(repository, *arguments):
    return subprocess.check_output(["git", "-C", repository] + list(arguments), stderr=subprocess.DEVNULL).decode("utf-8")


def resolve_repository(explicit):
    if explicit:
        return os.path.realpath(explicit)
    if os.environ.get("REPOSITORY"):
        return os.path.realpath(os.environ["REPOSITORY"])
    try:
        top_level = subprocess.check_output(["git", "rev-parse", "--show-toplevel"], stderr=subprocess.DEVNULL).decode().strip()
        if top_level and os.path.isdir(os.path.join(top_level, PACKAGE_PREFIX)):
            return os.path.realpath(top_level)
    except (subprocess.CalledProcessError, OSError):
        pass
    # Không rơi về repo chứa script (hoặc repo git bất kỳ của cwd): diff nhầm worktree thì báo xanh giả cho gói khác.
    print("check-ownership.py: thư mục hiện tại không thuộc repo có %s — truyền --repository <worktree>" % PACKAGE_PREFIX,
          file=sys.stderr)
    sys.exit(2)


def changed_paths(repository, base):
    paths = set()
    for line in git(repository, "diff", "--name-only", "--no-renames", base).splitlines():
        if line:
            paths.add(line)
    for line in git(repository, "ls-files", "--others", "--exclude-standard").splitlines():
        if line:
            paths.add(line)
    return sorted(paths)


def check_package(table, package, repository, base):
    if table.wave_of(package) is None:
        print("LỖI: gói '%s' không có trong ownership.tsv" % package)
        return 1
    package_wave = table.wave_of(package)
    paths = changed_paths(repository, base)
    base_directories = set(line for line in git(repository, "ls-tree", "-d", "-r", "--name-only", base).splitlines() if line)
    changed_set = set(paths)
    violations = []
    warnings = []

    new_directories = set()
    for path in paths:
        parent = os.path.dirname(path)
        while parent and parent not in base_directories:
            new_directories.add(parent)
            parent = os.path.dirname(parent)

    for path in paths:
        if path.endswith(".meta"):
            asset = path[:-len(".meta")]
            if asset in new_directories or (os.path.isdir(os.path.join(repository, asset)) and asset in base_directories):
                continue  # .meta thư mục: xét ở luật thư mục mới bên dưới
            if asset in changed_set and table.allows_file(package, asset):
                continue
            if table.allows_file(package, asset):
                continue
            owners = table.file_packages(asset)
            violations.append("%s — .meta của tài sản không thuộc %s%s" % (path, package, (" (chủ: %s)" % ",".join(owners)) if owners else ""))
            continue
        if table.allows_file(package, path):
            continue
        owners = table.file_packages(path)
        if owners:
            violations.append("%s — thuộc %s, không thuộc %s" % (path, ",".join(owners), package))
        else:
            violations.append("%s — không có trong bảng quyền ghi (mục 2); gói cần thêm file phải báo điều phối" % path)

    for directory in sorted(new_directories):
        rows = [row for row in table.directory_rows if row[0].match(directory)]
        if rows:
            owners = sorted(set(row[2] for row in rows))
            if package not in owners:
                violations.append("thư mục mới %s — chủ theo 10.4 là %s, không phải %s" % (directory, ",".join(owners), package))
            continue
        # Không có trong 10.4: suy chủ từ các file trong bảng nằm dưới thư mục đó thuộc gói cùng đợt.
        same_wave_owners = set()
        prefix = directory + "/"
        for regex, pattern, packages, _ in table.file_rows:
            literal_part = pattern.split("*")[0]
            if not literal_part.startswith(prefix):
                continue
            first_package = packages[0]
            if table.wave_of(first_package) == package_wave:
                same_wave_owners.add(first_package)
        others = sorted(same_wave_owners - {package})
        if others:
            violations.append("thư mục mới %s — gói cùng đợt %s cũng có file trong đó; cần chủ duy nhất ở 10.4" % (directory, ",".join(others)))
        else:
            warnings.append("thư mục mới %s không có trong 10.4 — suy chủ = %s (không gói cùng đợt nào khác có file ở đó)" % (directory, package))

    for warning in warnings:
        print("cảnh báo: " + warning)
    if violations:
        for violation in violations:
            print("LỖI: " + violation)
        print("OWNERSHIP CHECK FAILED (%s, %d vi phạm / %d đường dẫn, base %s)" % (package, len(violations), len(paths), base))
        return 1
    print("OWNERSHIP CHECK OK (%s, %d đường dẫn, %d thư mục mới, base %s)" % (package, len(paths), len(new_directories), base))
    return 0


def check_wave_dependencies(table):
    errors = []
    for package in table.package_order:
        wave = table.packages[package]["wave"]
        # Đợt có thể lẻ (W3.5 = gói G-I18N chen giữa W3 và W4) và có thể mang hậu tố lượt (W8-UX2), nên so bằng
        # wave_sort_key chứ không bằng float của phần sau chữ W.
        try:
            wave_key = wave_sort_key(wave)
        except ValueError as error:
            errors.append("%s: %s" % (package, error))
            continue
        for dependency in table.packages[package]["dependencies"]:
            if WAVE_PATTERN.match(dependency):
                dependency_wave = dependency
            else:
                dependency_wave = table.wave_of(dependency)
                if dependency_wave is None:
                    errors.append("%s phụ thuộc '%s' không có trong bảng" % (package, dependency))
                    continue
            try:
                dependency_key = wave_sort_key(dependency_wave)
            except ValueError as error:
                errors.append("%s phụ thuộc %s: %s" % (package, dependency, error))
                continue
            if dependency_key >= wave_key:
                errors.append("%s (%s) phụ thuộc %s (%s) — phải ở đợt trước" % (package, wave, dependency, dependency_wave))
    if errors:
        for error in errors:
            print("LỖI: " + error)
        print("WAVE DEPENDENCIES FAILED (%d)" % len(errors))
        return 1
    print("WAVE DEPENDENCIES OK (%d gói)" % len(table.package_order))
    return 0


def main():
    parser = argparse.ArgumentParser(description="Kiểm quyền ghi file của gói theo ownership.tsv.")
    parser.add_argument("package", nargs="?")
    parser.add_argument("--repository")
    parser.add_argument("--base", help="ref so sánh (mặc định merge-base HEAD với %s)" % DEFAULT_BASE_BRANCH)
    parser.add_argument("--table", help="ownership.tsv (mặc định cạnh script)")
    parser.add_argument("--wave-deps", action="store_true")
    arguments = parser.parse_args()

    table_path = arguments.table or os.path.join(os.path.dirname(os.path.abspath(__file__)), "ownership.tsv")
    table = OwnershipTable(table_path)
    if arguments.wave_deps:
        return check_wave_dependencies(table)
    if not arguments.package:
        parser.error("cần <gói> hoặc --wave-deps")
    repository = resolve_repository(arguments.repository)
    print("check-ownership.py: repository=%s" % repository, file=sys.stderr)
    base = arguments.base
    if not base:
        try:
            base = git(repository, "merge-base", "HEAD", DEFAULT_BASE_BRANCH).strip()
        except subprocess.CalledProcessError:
            print("LỖI: không tìm được merge-base với %s — truyền --base" % DEFAULT_BASE_BRANCH)
            return 2
    return check_package(table, arguments.package, repository, base)


if __name__ == "__main__":
    sys.exit(main())
