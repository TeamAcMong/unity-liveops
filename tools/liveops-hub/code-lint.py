#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""code-lint.py — lint luật package com.dreamtech.liveops cho file .cs và .uxml, biết nhánh #if.

Vì sao: lỗi quy ước (tên viết tắt, lớp không sealed, API chỉ có từ 2023+, chuỗi UI ngoài LiveOpsHubStrings) sửa rẻ nhất ở
gói sinh ra nó (PD-29). grep thường không hiểu `#if UNITY_2023_2_OR_NEWER`, nên script theo dõi ngăn xếp tiền xử lý và
biết mỗi dòng nằm trong nhánh define nào; comment và chuỗi được bỏ khi tìm định danh để không báo nhầm.

Cách dùng:
  code-lint.py [--repository <worktree>] [--report-only] [--json] <file hoặc thư mục...>
  code-lint.py --self-test          # chạy bộ mẫu trong tools/liveops-hub/lint-samples/
Không truyền đường dẫn = quét toàn bộ Packages/com.dreamtech.liveops.
Thoát 1 khi có lỗi (cảnh báo không làm đỏ); --report-only luôn thoát 0 (cổng đợt dùng cho file 0.1.0 cũ).
Ngoại lệ có chủ đích ghi ở tools/liveops-hub/lint-allow.tsv: <luật>\\t<glob đường dẫn>\\t<ký hiệu hoặc *>\\t<lý do>.
"""

import argparse
import fnmatch
import itertools
import json
import os
import re
import subprocess
import sys

PACKAGE_PREFIX = "Packages/com.dreamtech.liveops/"
CORE_PREFIX = PACKAGE_PREFIX + "Runtime/Core/"
CORE_CALENDAR_PREFIX = PACKAGE_PREFIX + "Runtime/Core/Calendar/"
RUNTIME_UNITY_PREFIX = PACKAGE_PREFIX + "Runtime/Unity/"
EDITOR_PREFIX = PACKAGE_PREFIX + "Editor/"
TESTS_PREFIX = PACKAGE_PREFIX + "Tests/"
HUB_SECTIONS_PREFIX = PACKAGE_PREFIX + "Editor/Hub/Sections/"

ERROR = "error"
WARNING = "warning"

# Định danh cấm viết tắt (mục 9.4). Chỉ bắt ở vị trí khai báo (biến, tham số, tham số lambda, catch, foreach) để không
# báo nhầm khi gọi API ngoài có tên như vậy.
ABBREVIATED_NAMES = {
    "evt", "ve", "cc", "ex", "idx", "btn", "lbl", "el", "cfg", "tmp", "msg", "str", "cnt", "ctx", "mgr", "obj", "val",
    "args", "e", "x",
}

# API đã chứng minh hỏng hoặc không tồn tại ở một trong hai bản Unity ([API §11], [API §12]).
BANNED_IDENTIFIERS = [
    "GetInstanceID", "ScheduleRefresh", "windowFocusChanged", "TabView", "ToggleButtonGroup", "placeholderText",
    "iconImage", "dataSource", "RegisterContext", "SetDashPattern", "fillGradient",
]
BANNED_PATTERNS = [
    (re.compile(r"\bIsDirty\s*\(\s*[0-9]"), "IsDirty(int) bị gỡ ở 6000.6 (CS0619)"),
    (re.compile(r"\btextEdition\s*\.\s*placeholder\b"), "textEdition.placeholder không có ở 2022.3 — dùng Label phủ"),
    (re.compile(r"\bColumn\b[^;]*\.\s*comparison\b|\bcolumn\s*\.\s*comparison\b"), "Column.comparison không có ở 2022.3"),
    (re.compile(r"winbtn_win_close"), "icon winbtn_win_close trả NULL + log Error ở 6000.6 — dùng clear / d_clear (PD-14)"),
]

# 12 id luật (mục 6.1). Chuỗi id chỉ được viết ở nơi khai hằng và nguồn câu duy nhất (V-8).
RULE_IDS = [
    "utc-time-format", "end-before-start", "invalid-identifier", "duplicate-event-id", "overlap-same-type",
    "recurring-rule-invalid", "shadowed-by-recurring", "unknown-event-type", "running-event-id-changed",
    "config-key-missing", "long-gap-between-events", "remote-snapshot-drift",
]
RULE_ID_LITERAL_ALLOWED_FILES = [
    PACKAGE_PREFIX + "Runtime/Core/Calendar/Validation/LiveEventCalendarRuleIds.cs",
    PACKAGE_PREFIX + "Runtime/Core/Calendar/Validation/LiveEventCalendarDetailCodes.cs",
    PACKAGE_PREFIX + "Editor/Hub/Services/LiveOpsFindingText.cs",
    PACKAGE_PREFIX + "Editor/Hub/Services/LiveOpsChangeText.cs",
]

VIETNAMESE_CHARACTER = re.compile(u"[À-ỹĐđ]")
STYLE_INLINE_ALLOWED = re.compile(r"//\s*style-inline-allowed:\s*(\d+)")
INTERIM_MARK = re.compile(r"INTERIM\(([^)]*)\)")

# Tập bản Unity dùng để suy nhánh #if: define phiên bản đi theo một biến "bản" duy nhất nên không xét tổ hợp vô lý
# (vd UNITY_6000_0_OR_NEWER đúng mà UNITY_2023_2_OR_NEWER sai).
VERSION_CANDIDATES = [(2022, 3), (2023, 1), (2023, 2), (6000, 0), (6000, 6)]
VERSION_SYMBOL = re.compile(r"^UNITY_(\d+)_(\d+)_OR_NEWER$")


class Finding(object):
    def __init__(self, path, line, rule, severity, message):
        self.path = path
        self.line = line
        self.rule = rule
        self.severity = severity
        self.message = message

    def as_dictionary(self):
        return {"path": self.path, "line": self.line, "rule": self.rule, "severity": self.severity, "message": self.message}


# ---------------------------------------------------------------------------------------------------------------- #if

class ConditionParser(object):
    """Phân tích biểu thức tiền xử lý C#: định danh, true/false, !, &&, ||, ==, !=, ngoặc."""

    TOKEN = re.compile(r"\s*(\|\||&&|==|!=|!|\(|\)|[A-Za-z_][A-Za-z0-9_]*)")

    def __init__(self, text):
        self.tokens = []
        position = 0
        text = text.split("//")[0].strip()
        while position < len(text):
            match = self.TOKEN.match(text, position)
            if not match:
                raise ValueError("biểu thức #if không đọc được: %s" % text)
            self.tokens.append(match.group(1))
            position = match.end()
            while position < len(text) and text[position].isspace():
                position += 1
        self.index = 0

    def parse(self):
        expression = self.parse_or()
        if self.index != len(self.tokens):
            raise ValueError("thừa token trong #if: %s" % " ".join(self.tokens))
        return expression

    def peek(self):
        return self.tokens[self.index] if self.index < len(self.tokens) else None

    def take(self):
        token = self.tokens[self.index]
        self.index += 1
        return token

    def parse_or(self):
        left = self.parse_and()
        while self.peek() == "||":
            self.take()
            left = ("or", left, self.parse_and())
        return left

    def parse_and(self):
        left = self.parse_equality()
        while self.peek() == "&&":
            self.take()
            left = ("and", left, self.parse_equality())
        return left

    def parse_equality(self):
        left = self.parse_unary()
        while self.peek() in ("==", "!="):
            operator = self.take()
            right = self.parse_unary()
            left = ("equal", left, right) if operator == "==" else ("not", ("equal", left, right))
        return left

    def parse_unary(self):
        token = self.peek()
        if token == "!":
            self.take()
            return ("not", self.parse_unary())
        if token == "(":
            self.take()
            inner = self.parse_or()
            if self.peek() != ")":
                raise ValueError("thiếu ')' trong #if")
            self.take()
            return inner
        if token is None:
            raise ValueError("#if rỗng")
        self.take()
        if token == "true":
            return ("constant", True)
        if token == "false":
            return ("constant", False)
        return ("symbol", token)


def expression_symbols(expression, collected):
    kind = expression[0]
    if kind == "symbol":
        collected.add(expression[1])
    elif kind == "constant":
        pass
    else:
        for child in expression[1:]:
            expression_symbols(child, collected)
    return collected


def evaluate(expression, assignment):
    kind = expression[0]
    if kind == "constant":
        return expression[1]
    if kind == "symbol":
        return assignment(expression[1])
    if kind == "not":
        return not evaluate(expression[1], assignment)
    if kind == "and":
        return evaluate(expression[1], assignment) and evaluate(expression[2], assignment)
    if kind == "or":
        return evaluate(expression[1], assignment) or evaluate(expression[2], assignment)
    if kind == "equal":
        return evaluate(expression[1], assignment) == evaluate(expression[2], assignment)
    raise ValueError(kind)


def branch_implies(conditions, symbol, expected_value):
    """True khi mọi tổ hợp define làm các điều kiện đúng đều cho `symbol` == expected_value (và tổ hợp đó có tồn tại)."""
    symbols = set([symbol])
    for condition in conditions:
        expression_symbols(condition, symbols)
    free_symbols = sorted(name for name in symbols if not VERSION_SYMBOL.match(name))
    satisfiable = False
    for version in VERSION_CANDIDATES:
        for values in itertools.product([False, True], repeat=len(free_symbols)):
            free_values = dict(zip(free_symbols, values))

            def assignment(name, version=version, free_values=free_values):
                match = VERSION_SYMBOL.match(name)
                if match:
                    return version >= (int(match.group(1)), int(match.group(2)))
                return free_values[name]

            if all(evaluate(condition, assignment) for condition in conditions):
                satisfiable = True
                if assignment(symbol) != expected_value:
                    return False
    return satisfiable


# ------------------------------------------------------------------------------------------------ tách code/comment/chuỗi

def split_code_and_strings(lines):
    """Trả về (code_lines, string_lines, comment_lines): code bỏ comment + nội dung chuỗi (thay bằng ""),
    string_lines là nội dung các literal chuỗi trên dòng, comment_lines là chữ trong comment."""
    code_lines = []
    string_lines = []
    comment_lines = []
    in_block_comment = False
    in_verbatim_string = False
    verbatim_buffer = []
    for line in lines:
        code = []
        strings = []
        comments = []
        index = 0
        length = len(line)
        while index < length:
            character = line[index]
            if in_block_comment:
                end = line.find("*/", index)
                if end < 0:
                    comments.append(line[index:])
                    index = length
                else:
                    comments.append(line[index:end])
                    index = end + 2
                    in_block_comment = False
                continue
            if in_verbatim_string:
                if character == '"':
                    if index + 1 < length and line[index + 1] == '"':
                        verbatim_buffer.append('"')
                        index += 2
                        continue
                    in_verbatim_string = False
                    strings.append("".join(verbatim_buffer))
                    verbatim_buffer = []
                    code.append('""')
                    index += 1
                    continue
                verbatim_buffer.append(character)
                index += 1
                continue
            if line.startswith("//", index):
                comments.append(line[index + 2:])
                break
            if line.startswith("/*", index):
                in_block_comment = True
                index += 2
                continue
            if character in ("@", "$") and line.startswith(('@"', '$@"', '@$"'), index):
                prefix_length = 2 if line.startswith('@"', index) else 3
                in_verbatim_string = True
                verbatim_buffer = []
                index += prefix_length
                continue
            if character == '"' or (character == "$" and index + 1 < length and line[index + 1] == '"'):
                if character == "$":
                    index += 1
                index += 1
                buffer = []
                while index < length:
                    if line[index] == "\\" and index + 1 < length:
                        buffer.append(line[index:index + 2])
                        index += 2
                        continue
                    if line[index] == '"':
                        index += 1
                        break
                    buffer.append(line[index])
                    index += 1
                strings.append("".join(buffer))
                code.append('""')
                continue
            if character == "'":
                end = index + 1
                while end < length:
                    if line[end] == "\\":
                        end += 2
                        continue
                    if line[end] == "'":
                        break
                    end += 1
                code.append("' '")
                index = end + 1
                continue
            code.append(character)
            index += 1
        if in_verbatim_string:
            verbatim_buffer.append("\n")
        code_lines.append("".join(code))
        string_lines.append(strings)
        comment_lines.append(" ".join(comments))
    return code_lines, string_lines, comment_lines


# --------------------------------------------------------------------------------------------------------- ngoại lệ

class AllowList(object):
    def __init__(self, path):
        self.entries = []
        if path and os.path.isfile(path):
            with open(path, encoding="utf-8") as handle:
                for raw_line in handle:
                    line = raw_line.rstrip("\n")
                    if not line.strip() or line.startswith("#"):
                        continue
                    columns = line.split("\t")
                    if len(columns) < 4 or not columns[3].strip():
                        raise ValueError("lint-allow.tsv: mỗi dòng cần 4 cột <luật> <glob> <ký hiệu|*> <lý do>: %r" % line)
                    self.entries.append((columns[0], columns[1], columns[2]))

    def allows(self, rule, path, symbol):
        for entry_rule, pattern, entry_symbol in self.entries:
            if entry_rule != rule:
                continue
            if not fnmatch.fnmatch(path, pattern):
                continue
            if entry_symbol == "*" or entry_symbol == symbol:
                return True
        return False


# ------------------------------------------------------------------------------------------------------------ lint C#

CLASS_DECLARATION = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*((?:(?:public|internal|private|protected|static|sealed|abstract|partial|unsafe|new|readonly)\s+)*)"
    r"class\s+([A-Za-z_][A-Za-z0-9_]*)")
RECORD_DECLARATION = re.compile(r"^\s*(?:(?:public|internal|private|protected|sealed|abstract|partial|readonly)\s+)*record\s+(?:struct\s+|class\s+)?[A-Z]\w*\s*[({<:;]")
INIT_ACCESSOR = re.compile(r"\binit\s*;")
FILE_SCOPED_NAMESPACE = re.compile(r"^\s*namespace\s+[A-Za-z_][\w.]*\s*;")
GLOBAL_USING = re.compile(r"^\s*global\s+using\b")
FIELD_M_PREFIX = re.compile(r"\b[A-Za-z_][\w.]*(?:<[^<>;()]*>)?(?:\[\])*\??\s+m_[A-Z]\w*\s*(?:=(?!=)|;|,)")
VAR_DECLARATION = re.compile(r"\bvar\s+([A-Za-z_]\w*)\s*=")
DECLARATION_PATTERNS = [
    # kiểu (có thể generic/mảng/nullable) + tên + (= ; , ) in)
    re.compile(r"\b(?P<type>[A-Za-z_][\w.]*(?:<[^<>;()]*>)?(?:\[\])*\??)\s+(?P<name>[A-Za-z_]\w*)\s*(?==(?!=)|;|,|\)|\bin\b)"),
    # tham số lambda đơn: x =>
    re.compile(r"(?:^|[(,=\s])([A-Za-z_]\w*)\s*=>"),
    # tham số lambda trong ngoặc: (x, y) =>
    re.compile(r"\(\s*([A-Za-z_]\w*(?:\s*,\s*[A-Za-z_]\w*)*)\s*\)\s*=>"),
]
NOT_DECLARATION_KEYWORDS = {"return", "new", "throw", "await", "yield", "case", "goto", "using", "else", "is", "as", "in", "out", "ref", "typeof", "nameof", "default", "const", "static", "readonly", "public", "private", "protected", "internal", "operator", "where", "select", "from", "orderby", "group", "into", "let", "join", "on", "equals", "by", "ascending", "descending"}
DATETIME_NOW = re.compile(r"\bDateTime\s*\.\s*(?:Now|UtcNow)\b")
STRING_COMPARISON_CALL = re.compile(r"\.\s*(Equals|StartsWith|EndsWith|IndexOf|Compare)\s*\(\s*(\"|[A-Za-z_])")
PARSE_WITHOUT_CULTURE = re.compile(r"\b(?:int|long|short|double|float|decimal|DateTime|DateTimeOffset|TimeSpan)\s*\.\s*(?:Parse|TryParse|ParseExact|TryParseExact)\s*\(")
FORMAT_TO_STRING = re.compile(r"\.\s*ToString\s*\(\s*\"")
STYLE_ASSIGNMENT = re.compile(r"\.\s*style\s*\.\s*\w+\s*(?:=(?!=)|\+=|-=)")
UI_TEST_TRIGGER = re.compile(r"\bEditorWindow\b|\bpanel\b|\bSendEvent\s*\(")
UI_CATEGORY_MARK = re.compile(r"Category\s*\(\s*LiveOpsHubTestCategories\s*\.\s*UI\s*\)")
INTERIM_IDENTIFIER_DECLARATION = re.compile(r"\b(?:const\s+\w+|static\s+readonly\s+\w+|class|struct)\s+(Interim\w*)")


def collect_partial_modifiers(package_root):
    """Tên lớp partial → tập modifier gặp ở mọi phần (một phần có sealed/static/abstract là đủ cho cả lớp)."""
    modifiers_by_class = {}
    if not os.path.isdir(package_root):
        return modifiers_by_class
    for directory, _, files in os.walk(package_root):
        for file_name in files:
            if not file_name.endswith(".cs"):
                continue
            try:
                with open(os.path.join(directory, file_name), encoding="utf-8-sig") as handle:
                    for line in handle:
                        match = CLASS_DECLARATION.match(line)
                        if match and "partial" in match.group(1).split():
                            modifiers_by_class.setdefault(match.group(2), set()).update(match.group(1).split())
            except (OSError, UnicodeDecodeError):
                continue
    return modifiers_by_class


def load_known_packages(ownership_path):
    packages = set()
    if ownership_path and os.path.isfile(ownership_path):
        with open(ownership_path, encoding="utf-8") as handle:
            for line in handle:
                columns = line.rstrip("\n").split("\t")
                if len(columns) >= 2 and columns[0] == "package":
                    packages.add(columns[1])
    return packages


def package_matches(name, known_packages):
    if name in known_packages:
        return True
    # Gói sửa sinh ở cổng đợt (G-FIX-W<n>-k) không có sẵn trong bảng.
    return re.match(r"^G-FIX-W\d+-\d+$", name) is not None


class CSharpLinter(object):
    def __init__(self, allow_list, partial_modifiers, known_packages):
        self.allow_list = allow_list
        self.partial_modifiers = partial_modifiers
        self.known_packages = known_packages

    def lint(self, relative_path, text):
        findings = []
        lines = text.split("\n")
        code_lines, string_lines, comment_lines = split_code_and_strings(lines)
        in_package = relative_path.startswith(PACKAGE_PREFIX)
        in_core = relative_path.startswith(CORE_PREFIX)
        in_core_calendar = relative_path.startswith(CORE_CALENDAR_PREFIX)
        in_runtime_unity = relative_path.startswith(RUNTIME_UNITY_PREFIX)
        in_editor = relative_path.startswith(EDITOR_PREFIX)
        in_tests = relative_path.startswith(TESTS_PREFIX)
        in_hub_sections = relative_path.startswith(HUB_SECTIONS_PREFIX)
        is_strings_file = os.path.basename(relative_path).startswith("LiveOpsHubStrings")

        def report(line_number, rule, severity, message, symbol="*"):
            if self.allow_list.allows(rule, relative_path, symbol):
                return
            findings.append(Finding(relative_path, line_number, rule, severity, message))

        condition_stack = []  # mỗi phần tử: [danh sách nhánh trước, điều kiện nhánh hiện tại]

        def current_conditions():
            return [frame[1] for frame in condition_stack]

        statement_buffer = []
        statement_start_line = 1

        for index, code in enumerate(code_lines):
            line_number = index + 1
            stripped = code.strip()
            directive = re.match(r"^#\s*(if|elif|else|endif)\b(.*)$", stripped)
            if directive:
                keyword, rest = directive.group(1), directive.group(2)
                try:
                    if keyword == "if":
                        condition = ConditionParser(rest).parse()
                        condition_stack.append([[condition], condition])
                    elif keyword == "elif":
                        previous, _ = condition_stack[-1]
                        condition = ConditionParser(rest).parse()
                        negated_previous = [("not", item) for item in previous]
                        branch = condition
                        for item in negated_previous:
                            branch = ("and", item, branch)
                        condition_stack[-1] = [previous + [condition], branch]
                    elif keyword == "else":
                        previous, _ = condition_stack[-1]
                        branch = ("constant", True)
                        for item in previous:
                            branch = ("and", ("not", item), branch)
                        condition_stack[-1] = [previous, branch]
                    elif keyword == "endif":
                        condition_stack.pop()
                except (ValueError, IndexError) as problem:
                    report(line_number, "preprocessor", ERROR, "tiền xử lý không đọc được: %s" % problem)
                continue
            if stripped.startswith("#"):
                continue

            conditions = current_conditions()
            string_literals = string_lines[index]
            raw_line = lines[index]

            # C# ≤ 9
            if RECORD_DECLARATION.search(code):
                report(line_number, "csharp-version", ERROR, "`record` là C# 9+ của .NET 5 — package giới hạn C# 9 không có record trên Unity 2022.3")
            if INIT_ACCESSOR.search(code):
                report(line_number, "csharp-version", ERROR, "`init` accessor không dùng được trên Unity 2022.3 (thiếu IsExternalInit)")
            if FILE_SCOPED_NAMESPACE.search(code):
                report(line_number, "csharp-version", ERROR, "namespace kiểu file (C# 10) — dùng khối namespace { }")
            if GLOBAL_USING.search(code):
                report(line_number, "csharp-version", ERROR, "`global using` là C# 10")

            # Kiến trúc
            if in_core and re.search(r"\busing\s+Unity(Engine|Editor)\b|\bUnity(Engine|Editor)\s*\.", code):
                report(line_number, "core-engine-reference", ERROR, "core là noEngineReferences — không tham chiếu UnityEngine/UnityEditor")
            if in_runtime_unity and re.search(r"\bLiveEventCalendarSha256\b", code):
                report(line_number, "runtime-forbidden", ERROR, "đường game không gọi SHA-256 (IL2CPP có thể strip — R-28)")
            if in_runtime_unity and re.search(r"\bUnityEditor\b", code):
                report(line_number, "runtime-forbidden", ERROR, "runtime Unity không được đụng UnityEditor (build game sẽ lỗi)")
            if in_package:
                if re.search(r"\bResources\s*\.\s*Load\b", code):
                    report(line_number, "package-forbidden", ERROR, "package không Resources.Load (đường dẫn thuộc game)")
                if re.search(r"\busing\s+System\s*\.\s*Linq\b", code):
                    report(line_number, "package-forbidden", ERROR, "không dùng System.Linq trong package (cấp phát ẩn, luật package)")
                if re.search(r"\bFindAnyObjectByType\b", code):
                    report(line_number, "package-forbidden", ERROR, "FindAnyObjectByType không có ở mọi bản 2022.3")
                if re.search(r"\bAwaitable\b", code):
                    report(line_number, "package-forbidden", ERROR, "Awaitable chỉ có từ Unity 2023.1")

            # API cấm
            if in_package:
                for identifier in BANNED_IDENTIFIERS:
                    if re.search(r"\b%s\b" % re.escape(identifier), code):
                        report(line_number, "banned-api", ERROR, "API cấm `%s` (hỏng hoặc không có ở một bản Unity)" % identifier, identifier)
                for pattern, message in BANNED_PATTERNS:
                    # Tên icon nằm trong chuỗi, còn API nằm trong code — tìm cả hai, không tìm trong comment.
                    search_text = code + " " + " ".join(string_literals)
                    if pattern.search(search_text):
                        report(line_number, "banned-api", ERROR, message)
                if re.search(r"\bEditorUtility\s*\.\s*DisplayDialog\w*\b", code):
                    report(line_number, "display-dialog", ERROR, "EditorUtility.DisplayDialog chặn test/batchmode — đi qua ILiveOpsHubConfirmationPresenter (thêm lint-allow.tsv nếu cố ý)", "DisplayDialog")

            # Nhánh #if
            self.check_branches(code, conditions, line_number, report)

            # Quy ước
            class_match = CLASS_DECLARATION.match(code)
            if class_match:
                modifiers = set(class_match.group(1).split())
                class_name = class_match.group(2)
                if "partial" in modifiers:
                    modifiers = modifiers | self.partial_modifiers.get(class_name, set())
                if not modifiers & {"sealed", "static", "abstract"}:
                    report(line_number, "class-not-sealed", ERROR, "lớp `%s` phải sealed/static/abstract (sealed mặc định)" % class_name, class_name)
            if FIELD_M_PREFIX.search(code):
                report(line_number, "field-m-prefix", ERROR, "không đặt tên field kiểu m_X — dùng _camelCase")
            self.check_abbreviations(code, line_number, report)
            if (in_core_calendar or in_editor) and DATETIME_NOW.search(code):
                report(line_number, "clock-direct", ERROR, "đọc đồng hồ máy trực tiếp — đi qua port đồng hồ (test không đóng băng được giờ)")
            comparison_match = STRING_COMPARISON_CALL.search(code)
            if comparison_match and "StringComparison" not in code and "StringComparer" not in code:
                # IndexOf('c') với ký tự không có overload StringComparison — code đã thay chuỗi ký tự bằng "' '".
                if not re.search(r"\.\s*IndexOf\s*\(\s*' '", code):
                    report(line_number, "string-comparison", WARNING, "`%s` không có StringComparison — kết quả phụ thuộc culture" % comparison_match.group(1))
            if (PARSE_WITHOUT_CULTURE.search(code) or FORMAT_TO_STRING.search(code)) and "InvariantCulture" not in code:
                report(line_number, "invariant-culture", WARNING, "đọc/ghi số-ngày không CultureInfo.InvariantCulture — máy locale vi-VN đổi dấu thập phân")

            # Test
            if in_tests:
                if re.search(r"\basync\s+Task\b", code):
                    report(line_number, "test-forbidden", ERROR, "test async Task không có ở UTF 1.1.33 (2022.3)")
                for token, why in (("UnityOneTimeSetUp", "chỉ có từ UTF 1.5"), ("WaitForEndOfFrame", "treo batchmode ở UTF 1.1.33"),
                                   ("ShowModalUtility", "chặn batchmode"), ("SaveFilePanel", "chặn batchmode"),
                                   ("DisplayDialog", "chặn batchmode")):
                    if re.search(r"\b%s\b" % token, code):
                        report(line_number, "test-forbidden", ERROR, "`%s` trong test: %s" % (token, why), token)

            # UI
            if in_editor and not is_strings_file:
                for literal in string_literals:
                    if VIETNAMESE_CHARACTER.search(literal):
                        report(line_number, "ui-vietnamese-string", ERROR, "chuỗi tiếng Việt ngoài LiveOpsHubStrings.*.cs — chữ UI phải ở một chỗ")
                        break
            if in_editor and STYLE_ASSIGNMENT.search(code):
                allowed = STYLE_INLINE_ALLOWED.search(raw_line)
                if not allowed or not (1 <= int(allowed.group(1)) <= 10):
                    report(line_number, "ui-inline-style", ERROR, "gán style inline ngoài 10 chỗ được phép [FD §2.14] — thêm `// style-inline-allowed: <1–10>` nếu đúng một chỗ trong danh sách")
            if in_hub_sections and re.search(r"\bJsonLiveEventCalendarParser\s*\.\s*Parse\s*\(", code):
                report(line_number, "hub-parser-direct", ERROR, "section không gọi thẳng JsonLiveEventCalendarParser.Parse — đi qua ILiveOpsHubJsonReadBack (V-16)")
            if in_package and relative_path not in RULE_ID_LITERAL_ALLOWED_FILES and not in_tests:
                for literal in string_literals:
                    if literal in RULE_IDS:
                        report(line_number, "rule-id-literal", ERROR, "id luật `%s` viết dạng chuỗi — dùng LiveEventCalendarRuleIds (V-8)" % literal, literal)

            # Nhánh tạm
            for match in INTERIM_MARK.finditer(comment_lines[index]):
                owner = match.group(1).strip()
                if not package_matches(owner, self.known_packages):
                    report(line_number, "interim-owner", ERROR, "INTERIM(%s) không trỏ tới gói có trong ownership.tsv" % owner, owner)
            declaration = INTERIM_IDENTIFIER_DECLARATION.search(code)
            if declaration:
                window_start = max(0, index - 3)
                nearby_comments = " ".join(comment_lines[window_start:index + 1])
                if "INTERIM(" not in nearby_comments:
                    report(line_number, "interim-constant", ERROR, "`%s` là nhánh tạm nhưng không có comment INTERIM(<gói gỡ>) trong 3 dòng trên" % declaration.group(1), declaration.group(1))

            # Câu lệnh (var)
            if not statement_buffer:
                statement_start_line = line_number
            statement_buffer.append(code)
            if ";" in code or "{" in code or "}" in code:
                statement = " ".join(statement_buffer)
                statement_buffer = []
                for part in re.split(r"[;{}]", statement):
                    var_match = VAR_DECLARATION.search(part)
                    if var_match and not re.search(r"\bnew\b", part):
                        report(statement_start_line, "var-without-new", ERROR, "`var %s` không đi với `new` — viết kiểu tường minh khi vế phải không lộ kiểu" % var_match.group(1), var_match.group(1))

        if in_tests and not relative_path.endswith("LiveOpsHubTestCategories.cs"):
            code_text = "\n".join(code_lines)
            trigger = UI_TEST_TRIGGER.search(code_text)
            if trigger and not UI_CATEGORY_MARK.search(code_text):
                line_number = code_text[:trigger.start()].count("\n") + 1
                report(line_number, "test-ui-category", ERROR, "test chạm EditorWindow/panel/SendEvent phải có [Category(LiveOpsHubTestCategories.UI)] (chạy không -nographics)")
        return findings

    def check_branches(self, code, conditions, line_number, report):
        def must_be_inside(symbol, expected, message):
            if not branch_implies(conditions, symbol, expected):
                report(line_number, "if-branch", ERROR, message, symbol)

        if re.search(r"\bUxmlFactory\b|\bUxmlTraits\b", code):
            must_be_inside("UNITY_2023_2_OR_NEWER", False, "UxmlFactory/UxmlTraits chỉ trong nhánh `#if !UNITY_2023_2_OR_NEWER` (đã gỡ ở Unity 6)")
        if re.search(r"\[\s*Uxml(Element|Attribute)\b", code):
            must_be_inside("UNITY_2023_2_OR_NEWER", True, "[UxmlElement]/[UxmlAttribute] chỉ trong nhánh `#if UNITY_2023_2_OR_NEWER` (không có ở 2022.3)")
        if re.search(r"\bfocusController\s*\.\s*IgnoreEvent\b", code):
            must_be_inside("UNITY_2023_2_OR_NEWER", True, "focusController.IgnoreEvent chỉ trong nhánh `#if UNITY_2023_2_OR_NEWER`")
        if re.search(r"\bsortingMode\b", code):
            must_be_inside("UNITY_6000_0_OR_NEWER", True, "sortingMode chỉ trong nhánh `#if UNITY_6000_0_OR_NEWER`")
        if re.search(r"\bsortingEnabled\b", code):
            must_be_inside("UNITY_6000_0_OR_NEWER", False, "sortingEnabled chỉ trong nhánh `#if !UNITY_6000_0_OR_NEWER` (CS0618 ở 6000.6)")
        if re.search(r"\bPreventDefault\s*\(", code):
            must_be_inside("UNITY_2023_2_OR_NEWER", False, "PreventDefault chỉ trong nhánh `#if !UNITY_2023_2_OR_NEWER` (CS0618 ở 6000.6)")

    def check_abbreviations(self, code, line_number, report):
        names = set()
        for pattern in DECLARATION_PATTERNS:
            for match in pattern.finditer(code):
                if "type" in pattern.groupindex:
                    # "return x;", "throw ex;" có dạng giống khai báo — vế trước là từ khoá thì không phải khai báo.
                    if match.group("type") in NOT_DECLARATION_KEYWORDS:
                        continue
                    candidates = [match.group("name")]
                else:
                    candidates = re.split(r"\s*,\s*", match.group(1))
                for name in candidates:
                    names.add(name.strip())
        catch_match = re.search(r"\bcatch\s*\(\s*[\w.]+\s+([A-Za-z_]\w*)\s*\)", code)
        if catch_match:
            names.add(catch_match.group(1))
        for name in names:
            if name in ABBREVIATED_NAMES and name not in NOT_DECLARATION_KEYWORDS:
                # `x` trong "is x" / "out x"? chỉ vị trí khai báo bị bắt ở trên; bỏ trường hợp là tên kiểu (theo sau là định danh).
                report(line_number, "abbreviated-name", ERROR, "tên viết tắt `%s` — đặt tên đầy đủ (luật package)" % name, name)


# ---------------------------------------------------------------------------------------------------------- lint UXML

UXML_ATTRIBUTE = re.compile(r"\b(text|tooltip|label)\s*=\s*\"([^\"]*)\"")


def lint_uxml(relative_path, text, allow_list):
    findings = []
    if not relative_path.startswith(EDITOR_PREFIX):
        return findings
    for index, line in enumerate(text.split("\n")):
        for match in UXML_ATTRIBUTE.finditer(line):
            if VIETNAMESE_CHARACTER.search(match.group(2)) and not allow_list.allows("uxml-vietnamese-text", relative_path, match.group(1)):
                findings.append(Finding(relative_path, index + 1, "uxml-vietnamese-text", ERROR,
                                        "thuộc tính %s có tiếng Việt trong UXML — chữ đặt từ C# qua LiveOpsHubStrings" % match.group(1)))
    return findings


# ------------------------------------------------------------------------------------------------------------- chạy

def resolve_repository(explicit):
    if explicit:
        return os.path.realpath(explicit)
    environment_repository = os.environ.get("REPOSITORY")
    if environment_repository:
        return os.path.realpath(environment_repository)
    try:
        top_level = subprocess.check_output(["git", "rev-parse", "--show-toplevel"], stderr=subprocess.DEVNULL).decode().strip()
        if os.path.isdir(os.path.join(top_level, PACKAGE_PREFIX)):
            return os.path.realpath(top_level)
    except (subprocess.CalledProcessError, OSError):
        pass
    return os.path.realpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))


def collect_files(repository, paths):
    collected = []
    targets = paths or [os.path.join(repository, PACKAGE_PREFIX)]
    for target in targets:
        absolute = target if os.path.isabs(target) else os.path.join(os.getcwd(), target)
        if not os.path.exists(absolute):
            absolute = os.path.join(repository, target)
        if os.path.isdir(absolute):
            for directory, directory_names, files in os.walk(absolute):
                directory_names[:] = [name for name in directory_names if not name.startswith(".") and not name.endswith("~")]
                for file_name in sorted(files):
                    if file_name.endswith((".cs", ".uxml")):
                        collected.append(os.path.join(directory, file_name))
        elif os.path.isfile(absolute) and absolute.endswith((".cs", ".uxml")):
            collected.append(absolute)
        elif not os.path.exists(absolute):
            print("code-lint.py: bỏ qua đường dẫn không tồn tại %s" % target, file=sys.stderr)
    return sorted(set(os.path.realpath(path) for path in collected))


def relative_to_repository(repository, path):
    relative = os.path.relpath(path, repository)
    return relative.replace(os.sep, "/")


def lint_text(relative_path, text, linter, allow_list):
    if relative_path.endswith(".uxml"):
        return lint_uxml(relative_path, text, allow_list)
    return linter.lint(relative_path, text)


def print_findings(findings, as_json):
    if as_json:
        print(json.dumps([finding.as_dictionary() for finding in findings], ensure_ascii=False, indent=2))
        return
    for finding in findings:
        print("%s:%d: %s [%s] %s" % (finding.path, finding.line, finding.severity, finding.rule, finding.message))


def run_self_test(tools_directory):
    samples_directory = os.path.join(tools_directory, "lint-samples")
    ownership_path = os.path.join(tools_directory, "ownership.tsv")
    known_packages = load_known_packages(ownership_path) or {"G-TOOLS", "G-PASTE", "G-SHELLPOLISH"}
    failures = 0
    checked = 0
    sample_files = []
    for directory, _, files in os.walk(samples_directory):
        for file_name in files:
            if file_name.endswith((".cs", ".uxml")):
                sample_files.append(os.path.join(directory, file_name))
    if not sample_files:
        print("LỖI: không có mẫu trong %s" % samples_directory)
        return 1
    for sample_path in sorted(sample_files):
        with open(sample_path, encoding="utf-8") as handle:
            text = handle.read()
        virtual_path_match = re.search(r"lint-sample-path:\s*(\S+)", text)
        if not virtual_path_match:
            print("LỖI: %s thiếu dòng lint-sample-path" % sample_path)
            failures += 1
            continue
        virtual_path = virtual_path_match.group(1)
        expected = sorted(re.findall(r"lint-expect:\s*([a-z0-9-]+)", text))
        allow_list = AllowList(None)
        partial_modifiers = {}
        for line in text.split("\n"):
            match = CLASS_DECLARATION.match(line)
            if match and "partial" in match.group(1).split():
                partial_modifiers.setdefault(match.group(2), set()).update(match.group(1).split())
        linter = CSharpLinter(allow_list, partial_modifiers, known_packages)
        actual = sorted(finding.rule for finding in lint_text(virtual_path, text, linter, allow_list))
        checked += 1
        name = os.path.relpath(sample_path, samples_directory)
        if actual != expected:
            failures += 1
            print("FAIL %s\n   mong đợi: %s\n   thực tế:  %s" % (name, expected, actual))
            for finding in lint_text(virtual_path, text, linter, allow_list):
                print("      dòng %d [%s] %s" % (finding.line, finding.rule, finding.message))
        else:
            print("ok   %s (%s)" % (name, ", ".join(expected) if expected else "sạch"))

    # Kiểm riêng suy luận nhánh #if (không phụ thuộc file mẫu).
    implication_cases = [
        ("UNITY_2023_2_OR_NEWER", ["UNITY_2023_2_OR_NEWER"], "UNITY_2023_2_OR_NEWER", True, True),
        ("!UNITY_2023_2_OR_NEWER", ["!UNITY_2023_2_OR_NEWER"], "UNITY_2023_2_OR_NEWER", False, True),
        ("6000 ⇒ 2023.2", ["UNITY_6000_0_OR_NEWER"], "UNITY_2023_2_OR_NEWER", True, True),
        ("2023.2 không ⇒ 6000", ["UNITY_2023_2_OR_NEWER"], "UNITY_6000_0_OR_NEWER", True, False),
        ("UNITY_EDITOR && !2023.2", ["UNITY_EDITOR && !UNITY_2023_2_OR_NEWER"], "UNITY_2023_2_OR_NEWER", False, True),
        ("ngoài #if", [], "UNITY_2023_2_OR_NEWER", False, False),
    ]
    for label, texts, symbol, expected_value, expected_result in implication_cases:
        conditions = [ConditionParser(text).parse() for text in texts]
        result = branch_implies(conditions, symbol, expected_value)
        checked += 1
        if result != expected_result:
            failures += 1
            print("FAIL suy nhánh '%s': %s == %s → %s (mong đợi %s)" % (label, symbol, expected_value, result, expected_result))
        else:
            print("ok   suy nhánh '%s'" % label)

    if failures:
        print("CODE LINT SELF-TEST FAILED (%d/%d)" % (failures, checked))
        return 1
    print("CODE LINT SELF-TEST OK (%d ca)" % checked)
    return 0


def main():
    parser = argparse.ArgumentParser(description="Lint luật package com.dreamtech.liveops (biết nhánh #if).")
    parser.add_argument("paths", nargs="*", help="file hoặc thư mục; mặc định toàn bộ package")
    parser.add_argument("--repository", help="worktree (mặc định git top-level hoặc repo chứa script)")
    parser.add_argument("--report-only", action="store_true", help="in phát hiện nhưng luôn thoát 0 (file 0.1.0 cũ ở cổng đợt)")
    parser.add_argument("--json", action="store_true", help="in JSON")
    parser.add_argument("--self-test", action="store_true", help="chạy bộ mẫu lint-samples/")
    arguments = parser.parse_args()

    tools_directory = os.path.dirname(os.path.abspath(__file__))
    if arguments.self_test:
        return run_self_test(tools_directory)

    repository = resolve_repository(arguments.repository)
    repository_tools = os.path.join(repository, "tools", "liveops-hub")
    allow_path = os.path.join(repository_tools, "lint-allow.tsv")
    ownership_path = os.path.join(repository_tools, "ownership.tsv")
    if not os.path.isfile(allow_path):
        allow_path = os.path.join(tools_directory, "lint-allow.tsv")
    if not os.path.isfile(ownership_path):
        ownership_path = os.path.join(tools_directory, "ownership.tsv")
    allow_list = AllowList(allow_path)
    known_packages = load_known_packages(ownership_path)
    partial_modifiers = collect_partial_modifiers(os.path.join(repository, PACKAGE_PREFIX))
    linter = CSharpLinter(allow_list, partial_modifiers, known_packages)

    findings = []
    files = collect_files(repository, arguments.paths)
    for path in files:
        relative_path = relative_to_repository(repository, path)
        with open(path, encoding="utf-8-sig") as handle:
            text = handle.read()
        findings.extend(lint_text(relative_path, text, linter, allow_list))
    print_findings(findings, arguments.json)
    error_count = sum(1 for finding in findings if finding.severity == ERROR)
    warning_count = len(findings) - error_count
    if not arguments.json:
        print("code-lint: %d file, %d lỗi, %d cảnh báo" % (len(files), error_count, warning_count))
    if error_count and not arguments.report_only:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
