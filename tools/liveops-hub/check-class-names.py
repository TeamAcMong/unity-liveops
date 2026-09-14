#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""check-class-names.py — mọi class `liveops-hub-*` dùng trong USS/UXML/C# phải là hằng trong LiveOpsHubClassNames*.cs.

Vì sao: class là hợp đồng giữa C# (EnableInClassList), UXML và USS. Gõ sai một ký tự không lỗi compile, không log — chỉ mất
style, tới ảnh chụp mới thấy. Gom mọi tên vào hằng (file gốc G-SKELETON + file vùng mỗi gói, V-5) thì một nguồn, và script
này đối chiếu máy:
  1. literal `liveops-hub-*` trong .uss (selector), .uxml (class="…"), .cs (chuỗi) phải bằng giá trị một hằng
  2. không hai hằng cùng giá trị (hai gói đặt trùng tên cho hai thứ khác nhau)
  3. giá trị `liveops-hub-<id màn>-…` chỉ khai trong file vùng của màn đó
  4. --strict (từ cổng W5): mọi hằng có ít nhất một selector USS
Chạy trên worktree của gói: chỉ thấy file gốc + file vùng của gói — đủ để xanh độc lập.

Cách dùng: check-class-names.py [--repository <worktree>] [--strict]
Thoát 0 khi sạch, 1 khi vi phạm.
"""

import argparse
import os
import re
import subprocess
import sys

PACKAGE_PREFIX = "Packages/com.dreamtech.liveops/"
CLASS_NAMES_DIRECTORY = PACKAGE_PREFIX + "Editor/Hub/Foundation/"
SCAN_ROOTS = [PACKAGE_PREFIX + "Editor", PACKAGE_PREFIX + "Tests/Editor/Hub"]
CLASS_LITERAL = re.compile(r"^liveops-hub(?:-[a-z0-9_]+)*(?:--[a-z0-9-]+)?$")
CONSTANT_DECLARATION = re.compile(r"\b(?:const\s+string|static\s+readonly\s+string)\s+(\w+)\s*=\s*\"([^\"]*)\"")
# Id màn (LiveOpsHubSections.Ids, mục 7) → file vùng được khai class riêng của màn.
SCREEN_REGIONS = {
    "overview": ["Overview"],
    "event-types": ["EventTypes"],
    "calendar": ["Calendar", "CalendarDepth"],
    "recurring": ["Recurring", "RecurringJson"],
    "validation": ["Validation", "ValidationDepth"],
    "export": ["Export"],
}


def resolve_repository(explicit):
    if explicit:
        return os.path.realpath(explicit)
    if os.environ.get("REPOSITORY"):
        return os.path.realpath(os.environ["REPOSITORY"])
    try:
        top_level = subprocess.check_output(["git", "rev-parse", "--show-toplevel"], stderr=subprocess.DEVNULL).decode().strip()
        if os.path.isdir(os.path.join(top_level, PACKAGE_PREFIX)):
            return os.path.realpath(top_level)
    except (subprocess.CalledProcessError, OSError):
        pass
    return os.path.realpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))


def strip_css_comments(text):
    return re.sub(r"/\*.*?\*/", lambda match: re.sub(r"[^\n]", " ", match.group(0)), text, flags=re.S)


def csharp_string_literals(text):
    # Quét tuần tự: chuỗi thường, chuỗi @"…", comment // và /* */ — tên class trong comment giải thích không tính.
    token = re.compile(r'@"(?:[^"]|"")*"|"(?:[^"\\\n]|\\.)*"|//[^\n]*|/\*.*?\*/', re.S)
    results = []
    for match in token.finditer(text):
        value = match.group(0)
        if value.startswith("//") or value.startswith("/*"):
            continue
        literal = value[2:-1] if value.startswith("@") else value[1:-1]
        results.append((text[:match.start()].count("\n") + 1, literal))
    return results


def region_of(file_name):
    match = re.match(r"^LiveOpsHubClassNames(?:\.(\w+))?\.cs$", file_name)
    if not match:
        return None
    return match.group(1) or ""


def main():
    parser = argparse.ArgumentParser(description="Đối chiếu class liveops-hub-* với LiveOpsHubClassNames*.cs.")
    parser.add_argument("--repository")
    parser.add_argument("--strict", action="store_true")
    arguments = parser.parse_args()
    repository = resolve_repository(arguments.repository)

    constants = []  # (file, dòng, tên, giá trị, vùng)
    class_names_directory = os.path.join(repository, CLASS_NAMES_DIRECTORY)
    if os.path.isdir(class_names_directory):
        for file_name in sorted(os.listdir(class_names_directory)):
            region = region_of(file_name)
            if region is None:
                continue
            relative = CLASS_NAMES_DIRECTORY + file_name
            with open(os.path.join(class_names_directory, file_name), encoding="utf-8-sig") as handle:
                for number, line in enumerate(handle, start=1):
                    for match in CONSTANT_DECLARATION.finditer(line.split("//")[0]):
                        constants.append((relative, number, match.group(1), match.group(2), region))

    errors = []
    values = {}
    for relative, number, name, value, region in constants:
        values.setdefault(value, []).append((relative, number, name, region))
        for screen_id, regions in SCREEN_REGIONS.items():
            if value == "liveops-hub-" + screen_id or value.startswith("liveops-hub-%s-" % screen_id) or value.startswith("liveops-hub-%s--" % screen_id):
                if region not in regions:
                    errors.append("%s:%d: %s = \"%s\" là class riêng màn '%s' — chỉ khai ở LiveOpsHubClassNames.{%s}.cs" % (
                        relative, number, name, value, screen_id, ",".join(regions)))
    for value, declarations in sorted(values.items()):
        if len(declarations) > 1:
            errors.append("giá trị \"%s\" khai trùng: %s" % (value, "; ".join("%s:%d %s" % (item[0], item[1], item[2]) for item in declarations)))

    known_values = set(values)
    used_in_selectors = set()
    literal_count = 0
    for scan_root in SCAN_ROOTS:
        absolute_root = os.path.join(repository, scan_root)
        if not os.path.isdir(absolute_root):
            continue
        for directory, directory_names, file_names in os.walk(absolute_root):
            directory_names[:] = [name for name in directory_names if not name.startswith(".")]
            for file_name in sorted(file_names):
                absolute = os.path.join(directory, file_name)
                relative = os.path.relpath(absolute, repository).replace(os.sep, "/")
                if file_name.endswith(".uss"):
                    with open(absolute, encoding="utf-8-sig") as handle:
                        text = strip_css_comments(handle.read())
                    for block in re.finditer(r"([^{}]*)\{", text):
                        selector = block.group(1)
                        line_number = text[:block.start()].count("\n") + 1 + (len(selector) - len(selector.lstrip("\n")))
                        for match in re.finditer(r"(?<![-\w])\.(liveops-hub[\w-]*)", selector):
                            literal_count += 1
                            used_in_selectors.add(match.group(1))
                            if match.group(1) not in known_values:
                                errors.append("%s:%d: selector .%s không có hằng trong LiveOpsHubClassNames*.cs" % (relative, line_number, match.group(1)))
                elif file_name.endswith(".uxml"):
                    with open(absolute, encoding="utf-8-sig") as handle:
                        for number, line in enumerate(handle, start=1):
                            for attribute in re.finditer(r"\bclass\s*=\s*\"([^\"]*)\"", line):
                                for token in attribute.group(1).split():
                                    if token.startswith("liveops-hub"):
                                        literal_count += 1
                                        if token not in known_values:
                                            errors.append("%s:%d: class=\"%s\" không có hằng trong LiveOpsHubClassNames*.cs" % (relative, number, token))
                elif file_name.endswith(".cs"):
                    if region_of(file_name) is not None or file_name.startswith("LiveOpsHubPaths"):
                        continue  # nơi khai hằng class / tên element, không phải nơi dùng
                    with open(absolute, encoding="utf-8-sig") as handle:
                        text = handle.read()
                    for number, literal in csharp_string_literals(text):
                        if CLASS_LITERAL.match(literal):
                            literal_count += 1
                            if literal not in known_values:
                                errors.append("%s:%d: chuỗi \"%s\" trông như class nhưng không có hằng — dùng LiveOpsHubClassNames.<Tên>" % (relative, number, literal))

    if arguments.strict:
        for relative, number, name, value, region in constants:
            if value not in used_in_selectors:
                errors.append("%s:%d: %s = \"%s\" không có selector USS nào (--strict)" % (relative, number, name, value))

    if errors:
        for error in errors:
            print("LỖI: " + error)
        print("CLASS NAMES CHECK FAILED (%d lỗi, %d hằng, %d chỗ dùng)" % (len(errors), len(constants), literal_count))
        return 1
    note = "" if constants else " — chưa có LiveOpsHubClassNames*.cs (trước G-SKELETON)"
    print("CLASS NAMES CHECK OK (%d hằng, %d chỗ dùng%s)" % (len(constants), literal_count, note))
    return 0


if __name__ == "__main__":
    sys.exit(main())
