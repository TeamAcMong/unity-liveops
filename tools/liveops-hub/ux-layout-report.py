#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""ux-layout-report.py — gom JSON chẩn đoán của kiểm bố cục W8-UX thành một bảng cho cổng người.

Vì sao: UxLayoutAuditTests ghi một file JSON cho MỖI (màn × cỡ × ngôn ngữ × bản Unity) — một lượt cổng ra hơn hai trăm file.
Đọc từng file thì không thấy được hình dạng của lỗi ("chỉ hỏng ở 700 và 820", "chỉ hỏng ở bản 2022.3"), mà đó mới là thứ
quyết định sửa ở đâu. Script gom lại theo màn × cỡ, đếm từng loại phát hiện, và (tuỳ chọn) so hai lượt TRƯỚC/SAU.

Cách dùng:
  ux-layout-report.py <thư mục lượt>                     # ~/.cache/unity-liveops/ux-gate/<nhãn>
  ux-layout-report.py <thư mục lượt> --html <file.html>   # thêm bản HTML để dán vào báo cáo
  ux-layout-report.py <sau> --before <trước>              # cột chênh lệch: số lỗi giảm/tăng theo màn
  ux-layout-report.py <thư mục lượt> --details            # in nguyên văn từng câu chẩn đoán
Thoát 0 luôn khi đọc được thư mục (đây là công cụ BÁO CÁO, không phải cổng — cổng là test); thoát 2 khi dùng sai.
"""

import argparse
import html
import json
import os
import sys

# Thứ tự hiển thị = thứ tự nặng dần khi soát: thiếu lối vào trước, rồi chữ không đọc được, rồi bố cục.
FINDING_KINDS = [
    ("missingElement", "thiếu/không dùng được"),
    ("textCut", "chữ bị cắt"),
    ("childOverflow", "con tràn khỏi cha"),
    ("notStretched", "không giãn theo cửa sổ"),
    ("siblingOverlap", "anh em chồng nhau"),
    ("absoluteOverText", "lớp nổi đè chữ"),
    ("pairOverlap", "hai khối cấm chồng lại chồng"),
    ("lowContrast", "dấu màu không đủ tương phản"),
]

# Hai khoá CHẨN ĐOÁN, không tính vào tổng: zeroSizeNamed (khung hub có spacer/vùng rỗng cao 0 đúng thiết kế — ô quan trọng
# đã được missingElement lo) và scrollViews (liệt kê mọi ScrollView; chỉ dòng mang một trong các dấu hiệu dưới mới là lỗi).
DIAGNOSTIC_KINDS = [("zeroSizeNamed", "cao hoặc rộng 0 (chẩn đoán)")]
# scrollViews liệt kê MỌI ScrollView (chẩn đoán); chỉ dòng mang một trong các dấu hiệu này mới là lỗi.
SCROLL_VIEW_PROBLEM_MARKS = ("TRÀN DỌC KHÔNG CÓ THANH CUỘN", "TRÀN NGANG KHÔNG CÓ THANH CUỘN", "CÓ THANH CUỘN NGANG")


def read_run(directory):
    """Đọc mọi *.json dưới <thư mục>/<bản Unity>/ và trả danh sách bản ghi đã chuẩn hoá."""
    records = []
    for root, _, file_names in os.walk(directory):
        for file_name in sorted(file_names):
            if not file_name.endswith(".json"):
                continue
            path = os.path.join(root, file_name)
            try:
                with open(path, encoding="utf-8") as handle:
                    document = json.load(handle)
            except (OSError, ValueError) as failure:
                print("BỎ QUA %s: %s" % (path, failure), file=sys.stderr)
                continue
            records.append(normalise(document, path))
    return records


def normalise(document, path):
    counts = {}
    entries = {}
    for kind, _ in FINDING_KINDS + DIAGNOSTIC_KINDS:
        values = document.get(kind) or []
        counts[kind] = len(values)
        entries[kind] = values
    scroll_problems = [line for line in (document.get("scrollViews") or [])
                       if any(mark in line for mark in SCROLL_VIEW_PROBLEM_MARKS)]
    counts["scrollViews"] = len(scroll_problems)
    entries["scrollViews"] = scroll_problems
    # Cờ "cỡ này CHƯA đo đúng" và "loại này đã bị cắt bớt dòng" phải hiện ra trong bảng: một lượt xanh mà nửa số cỡ bị hệ
    # điều hành kẹp, hoặc một loại 1.176 dòng chỉ còn 200, đều là báo cáo nói dối nếu chỉ in con số (R-08, R-14).
    window_position = document.get("windowPosition") or {}
    requested_size = document.get("requestedSize") or {}
    truncated = document.get("truncated") or {}
    return {
        "path": path,
        "screen": document.get("screen", "?"),
        "size": document.get("size", "?"),
        "language": document.get("language", "?"),
        "unityVersion": document.get("unityVersion", "?"),
        "clamped": bool(document.get("clamped")),
        "clampNote": document.get("clampNote", ""),
        "actualSize": "%dx%d" % (int(window_position.get("width", 0)), int(window_position.get("height", 0)))
                      if window_position else "?",
        "requestedSize": "%dx%d" % (int(requested_size.get("width", 0)), int(requested_size.get("height", 0)))
                         if requested_size else "?",
        "truncated": truncated,
        "counts": counts,
        "entries": entries,
        "total": sum(count for kind, count in counts.items() if kind not in [item[0] for item in DIAGNOSTIC_KINDS]),
    }


def truncation_text(record):
    """Mô tả các loại đã chạm trần dòng của một lượt; rỗng thì trả "-"."""
    truncated = record["truncated"]
    if not truncated:
        return "-"
    return "; ".join("%s=%s" % (kind, count) for kind, count in sorted(truncated.items()))


def kind_labels():
    return FINDING_KINDS + [("scrollViews", "cuộn hỏng")] + DIAGNOSTIC_KINDS


def group_totals(records, key):
    totals = {}
    for record in records:
        totals[record[key]] = totals.get(record[key], 0) + record["total"]
    return totals


def markdown_table(records, before_records):
    lines = []
    labels = kind_labels()
    header = ["màn", "cỡ", "cỡ thật", "ngôn ngữ", "bản Unity"] + [label for _, label in labels] + ["cắt bớt", "tổng"]
    lines.append("| " + " | ".join(header) + " |")
    lines.append("|" + "---|" * len(header))
    for record in sorted(records, key=lambda item: (item["screen"], item["size"], item["language"], item["unityVersion"])):
        actual = record["actualSize"] + (" KẸP" if record["clamped"] else "")
        row = [record["screen"], record["size"], actual, record["language"], record["unityVersion"]]
        row += [str(record["counts"].get(kind, 0)) for kind, _ in labels]
        row.append(truncation_text(record))
        row.append(str(record["total"]))
        lines.append("| " + " | ".join(row) + " |")

    clamped_records = [record for record in records if record["clamped"]]
    if clamped_records:
        lines.append("")
        lines.append("### Cỡ CHƯA đo đúng trên máy chạy (cửa sổ bị hệ điều hành kẹp)")
        lines.append("")
        for record in sorted(clamped_records, key=lambda item: (item["screen"], item["size"])):
            lines.append("- %s · %s · %s — %s" % (record["screen"], record["size"], record["language"],
                                                  record["clampNote"] or "cửa sổ nhỏ hơn cỡ yêu cầu"))

    truncated_records = [record for record in records if record["truncated"]]
    if truncated_records:
        lines.append("")
        lines.append("### Loại bị CẮT BỚT dòng (JSON chỉ giữ phần đầu — con số dưới đây mới là số thật)")
        lines.append("")
        for record in sorted(truncated_records, key=lambda item: (item["screen"], item["size"])):
            lines.append("- %s · %s · %s — %s" % (record["screen"], record["size"], record["language"],
                                                  truncation_text(record)))

    if before_records:
        lines.append("")
        lines.append("### Chênh lệch theo màn (âm = đã sửa bớt)")
        lines.append("")
        lines.append("| màn | trước | sau | chênh |")
        lines.append("|---|---|---|---|")
        after_totals = group_totals(records, "screen")
        before_totals = group_totals(before_records, "screen")
        for screen in sorted(set(list(after_totals.keys()) + list(before_totals.keys()))):
            before_count = before_totals.get(screen, 0)
            after_count = after_totals.get(screen, 0)
            lines.append("| %s | %d | %d | %+d |" % (screen, before_count, after_count, after_count - before_count))
    return "\n".join(lines)


def detail_lines(records):
    lines = []
    for record in sorted(records, key=lambda item: (item["screen"], item["size"], item["language"])):
        if record["total"] == 0:
            continue
        lines.append("")
        lines.append("#### %s · %s · %s · %s (%d)" % (record["screen"], record["size"], record["language"],
                                                      record["unityVersion"], record["total"]))
        for kind, label in kind_labels():
            for entry in record["entries"].get(kind, []):
                lines.append("- **%s** — %s" % (label, entry))
    return "\n".join(lines)


def write_html(path, title, body_markdown):
    # HTML tối giản, không phụ thuộc mạng: báo cáo phải mở được trên máy không có internet.
    rows = html.escape(body_markdown)
    with open(path, "w", encoding="utf-8") as handle:
        handle.write("<!doctype html>\n<html lang=\"vi\"><head><meta charset=\"utf-8\">\n")
        handle.write("<title>%s</title>\n" % html.escape(title))
        handle.write("<style>body{font:13px/1.5 -apple-system,Segoe UI,sans-serif;margin:24px;max-width:1200px}"
                     "pre{white-space:pre-wrap;background:#f6f6f6;padding:12px;border-radius:6px}</style>\n")
        handle.write("</head><body>\n<h1>%s</h1>\n<pre>%s</pre>\n</body></html>\n" % (html.escape(title), rows))


def main():
    parser = argparse.ArgumentParser(description="Gom JSON kiểm bố cục W8-UX thành bảng")
    parser.add_argument("run_directory", help="thư mục lượt (~/.cache/unity-liveops/ux-gate/<nhãn>)")
    parser.add_argument("--before", default="", help="thư mục lượt TRƯỚC để so chênh lệch")
    parser.add_argument("--html", default="", help="ghi thêm bản HTML ra file này")
    parser.add_argument("--details", action="store_true", help="in nguyên văn từng câu chẩn đoán")
    options = parser.parse_args()

    if not os.path.isdir(options.run_directory):
        print("ux-layout-report.py: không thấy thư mục %s" % options.run_directory, file=sys.stderr)
        return 2
    records = read_run(options.run_directory)
    if not records:
        print("ux-layout-report.py: không có JSON nào trong %s — lượt UxGate đã chạy chưa?" % options.run_directory)
        return 0
    before_records = read_run(options.before) if options.before and os.path.isdir(options.before) else []

    title = "Kiểm bố cục W8-UX — %s" % os.path.basename(os.path.normpath(options.run_directory))
    body = markdown_table(records, before_records)
    if options.details:
        body += "\n" + detail_lines(records)
    print("# %s\n" % title)
    print(body)
    clamped_count = sum(1 for item in records if item["clamped"])
    truncated_count = sum(1 for item in records if item["truncated"])
    print("\nTổng: %d chỗ không dùng được trên %d lượt kiểm (%d lượt bị kẹp cỡ, %d lượt bị cắt bớt dòng)."
          % (sum(item["total"] for item in records), len(records), clamped_count, truncated_count))
    if options.html:
        write_html(options.html, title, body)
        print("HTML: %s" % options.html)
    return 0


if __name__ == "__main__":
    sys.exit(main())
