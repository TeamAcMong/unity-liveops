#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""uss-lint.py — chặn thuộc tính/selector USS mà UI Toolkit (2022.3 và 6000.6) không có hoặc hub cấm.

Vì sao: USS sai không lỗi compile — Unity chỉ log cảnh báo lúc import (hoặc im lặng bỏ qua), nên `gap`, `box-shadow`,
`:last-child`, `@media` quen tay từ CSS web sẽ lọt tới ảnh chụp mới lộ. Màu trần ngoài liveops-hub-theme.uss phá hai skin
(mọi màu phải đi qua token có số tương phản [FD §2.3]).

Cách dùng:
  uss-lint.py [--repository <worktree>] [file.uss hoặc thư mục...]   # mặc định: mọi .uss trong package
  uss-lint.py --self-test
Thoát 1 khi có lỗi.
"""

import argparse
import os
import re
import subprocess
import sys

PACKAGE_PREFIX = "Packages/com.dreamtech.liveops/"
THEME_FILE_NAME = "liveops-hub-theme.uss"
MONO_FONT_DEFINITION = 'resource("Fonts/RobotoMono/RobotoMono-Regular SDF.asset")'

PROPERTY_RULES = [
    (re.compile(r"^(?:row-|column-)?gap$"), "`gap` không có trong USS — dùng margin + class `--first`/`--last`"),
    (re.compile(r"^box-shadow$"), "`box-shadow` không có trong USS"),
    (re.compile(r"^text-transform$"), "`text-transform` không có — viết HOA sẵn trong LiveOpsHubStrings"),
    (re.compile(r"^z-index$"), "`z-index` không có — thứ tự vẽ theo cây, dùng BringToFront (chỗ 10 của [FD §2.14])"),
    (re.compile(r"^grid(?:-.*)?$"), "CSS grid không có trong USS — dùng flex"),
    (re.compile(r"^-unity-font$"), "`-unity-font:` (font cũ) — mono dùng `-unity-font-definition: %s`" % MONO_FONT_DEFINITION),
]
VALUE_RULES = [
    (re.compile(r"!important"), "`!important` không có trong USS"),
    (re.compile(r"var\(\s*--unity-icons-"), "`var(--unity-icons-…)` không ổn định giữa hai bản — icon lấy qua LiveOpsHubIcons trong C#"),
]
SELECTOR_RULES = [
    (re.compile(r":last-child\b"), "`:last-child` không có — bật class `liveops-hub-row--last` từ C#"),
    (re.compile(r":nth-child\b"), "`:nth-child` không có — bật class từ C#"),
    (re.compile(r"(?:^|[\s>+~,])\*(?=$|[\s.:#\[>+~,])"), "selector `*` làm chậm style resolve toàn panel — nêu class cụ thể"),
]
BARE_COLOR = re.compile(r"#[0-9A-Fa-f]{3,8}\b|\brgba?\s*\(")


def strip_comments(text):
    # Giữ số dòng: thay comment bằng khoảng trắng cùng số ký tự xuống dòng.
    return re.sub(r"/\*.*?\*/", lambda match: re.sub(r"[^\n]", " ", match.group(0)), text, flags=re.S)


def lint_uss(relative_path, text):
    findings = []
    is_theme = os.path.basename(relative_path) == THEME_FILE_NAME
    cleaned = strip_comments(text)
    for match in re.finditer(r"@media\b", cleaned):
        findings.append((cleaned[:match.start()].count("\n") + 1, "`@media` không có — breakpoint bằng class `--medium/--narrow/--compact` từ C#"))
    for block in re.finditer(r"([^{}]*)\{([^{}]*)\}", cleaned):
        selector_text, body = block.group(1), block.group(2)
        leading = len(selector_text) - len(selector_text.lstrip())
        selector_line = cleaned[:block.start(1) + leading].count("\n") + 1
        stripped_selector = selector_text.strip()
        for pattern, message in SELECTOR_RULES:
            if pattern.search(stripped_selector):
                findings.append((selector_line, message))
        consumed = block.start(2)
        for declaration in body.split(";"):
            leading = len(declaration) - len(declaration.lstrip())
            line_number = cleaned[:consumed + leading].count("\n") + 1
            consumed += len(declaration) + 1
            if ":" not in declaration:
                continue
            name, value = declaration.split(":", 1)
            name = name.strip()
            value = value.strip()
            for pattern, message in PROPERTY_RULES:
                if pattern.match(name):
                    findings.append((line_number, message))
            for pattern, message in VALUE_RULES:
                if pattern.search(value):
                    findings.append((line_number, message))
            if name == "display" and value.startswith("grid"):
                findings.append((line_number, "`display: grid` không có trong USS — dùng flex"))
            if BARE_COLOR.search(value) and not is_theme:
                if name.startswith("--"):
                    findings.append((line_number, "khai token màu `%s` ngoài %s — token hai skin chỉ khai ở theme" % (name, THEME_FILE_NAME)))
                else:
                    findings.append((line_number, "màu trần `%s` ngoài %s — dùng token var(--liveops-hub-color-…) hoặc --unity-colors-*" % (value, THEME_FILE_NAME)))
    return sorted(findings)


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


def collect(repository, paths):
    targets = paths or [os.path.join(repository, PACKAGE_PREFIX)]
    files = []
    for target in targets:
        absolute = target if os.path.isabs(target) else os.path.abspath(target)
        if not os.path.exists(absolute):
            absolute = os.path.join(repository, target)
        if os.path.isdir(absolute):
            for directory, directory_names, names in os.walk(absolute):
                directory_names[:] = [name for name in directory_names if not name.startswith(".")]
                files.extend(os.path.join(directory, name) for name in names if name.endswith(".uss"))
        elif absolute.endswith(".uss") and os.path.isfile(absolute):
            files.append(absolute)
    return sorted(set(files))


SELF_TEST_BAD = """
.liveops-hub-row { gap: 4px; box-shadow: 0 1px 2px black; }
.liveops-hub-row:last-child { margin-bottom: 0; }
@media (max-width: 900px) { .liveops-hub-rail { width: 36px; } }
.liveops-hub-title { text-transform: uppercase; z-index: 2; color: #FF8080 !important; }
* { margin: 0; }
.liveops-hub-json { -unity-font: resource("x"); background-image: var(--unity-icons-clear); display: grid; }
.liveops-hub-card { --liveops-hub-color-blocked-text: #FF8080; border-color: rgba(0, 0, 0, 0.5); }
"""
SELF_TEST_GOOD = """
/* gap: 4px; comment không tính */
.liveops-hub-row { margin-left: 4px; flex-direction: row; }
.liveops-hub-row--last { margin-bottom: 0; }
.liveops-hub--narrow .liveops-hub-rail { width: 36px; }
.liveops-hub-json { -unity-font-definition: resource("Fonts/RobotoMono/RobotoMono-Regular SDF.asset"); color: var(--liveops-hub-color-blocked-text); }
.liveops-hub-card > .liveops-hub-card-header:hover { background-color: var(--unity-colors-button-background-hover); }
"""
SELF_TEST_THEME = """
.liveops-hub-root { --liveops-hub-color-blocked-text: #FF8080; }
.liveops-hub--skin-light { --liveops-hub-color-blocked-text: #780000; }
"""


def self_test():
    failures = 0
    bad = lint_uss("Packages/com.dreamtech.liveops/Editor/Hub/UI/bad.uss", SELF_TEST_BAD)
    expected_bad_count = 14
    if len(bad) != expected_bad_count:
        failures += 1
        print("FAIL mẫu vi phạm: %d phát hiện (mong đợi %d)" % (len(bad), expected_bad_count))
        for line_number, message in bad:
            print("   dòng %d: %s" % (line_number, message))
    else:
        print("ok   mẫu vi phạm: %d phát hiện" % len(bad))
    good = lint_uss("Packages/com.dreamtech.liveops/Editor/Hub/UI/good.uss", SELF_TEST_GOOD)
    theme = lint_uss("Packages/com.dreamtech.liveops/Editor/Hub/UI/" + THEME_FILE_NAME, SELF_TEST_THEME)
    for label, found in (("mẫu đạt", good), ("theme", theme)):
        if found:
            failures += 1
            print("FAIL %s có phát hiện: %s" % (label, found))
        else:
            print("ok   %s sạch" % label)
    print("USS LINT SELF-TEST %s" % ("OK" if not failures else "FAILED"))
    return 1 if failures else 0


def main():
    parser = argparse.ArgumentParser(description="Lint USS của LiveOps Hub.")
    parser.add_argument("paths", nargs="*")
    parser.add_argument("--repository")
    parser.add_argument("--self-test", action="store_true")
    arguments = parser.parse_args()
    if arguments.self_test:
        return self_test()
    repository = resolve_repository(arguments.repository)
    files = collect(repository, arguments.paths)
    error_count = 0
    for path in files:
        relative_path = os.path.relpath(path, repository).replace(os.sep, "/")
        with open(path, encoding="utf-8-sig") as handle:
            findings = lint_uss(relative_path, handle.read())
        for line_number, message in findings:
            print("%s:%d: error %s" % (relative_path, line_number, message))
        error_count += len(findings)
    print("uss-lint: %d file, %d lỗi" % (len(files), error_count))
    return 1 if error_count else 0


if __name__ == "__main__":
    sys.exit(main())
