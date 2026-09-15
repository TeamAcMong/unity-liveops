#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""measure-capture.py — đo ảnh chụp cửa sổ LiveOps Hub ở độ phân giải gốc: kích thước khung, ΔE màu token, tương phản WCAG.

Vì sao: nhìn ảnh thu nhỏ để kết luận "lệch 2px" hay "màu đúng" là sai (thu nhỏ trộn điểm ảnh). Kết luận chỉ từ số đo trên PNG
gốc (mục 9.5). Script chỉ dùng thư viện chuẩn Python 3 (đọc PNG bằng zlib) để chạy được trên mọi máy không cần pip.

Đầu vào: thư mục chứa cặp <id>-<skin>.png + <id>-<skin>.json do LiveOpsHubCaptureCommand ghi. JSON:
  {
    "scenario": "hs-shell-skeleton", "skin": "dark", "unityVersion": "6000.6.0f1", "pixelsPerPoint": 1,
    "window": {"width": 1280, "height": 760},
    "elements": [ {"name": "liveops-hub-rail", "classes": ["liveops-hub-rail"],
                   "worldBound": {"x": 0, "y": 26, "width": 196, "height": 714}} ],
    "expectedFrames": [ {"element": "liveops-hub-rail", "width": 196} ],          (tuỳ chọn — ghi đè bảng mặc định)
    "colorAnchors": [ {"name": "rail-blocked-text", "point": {"x": 40, "y": 100}, "expected": "#FF8080",
                       "maximumDeltaE": 3, "background": {"x": 12, "y": 100}, "minimumContrast": 4.54} ]
  }
Toạ độ theo point của UI Toolkit, gốc trên-trái (ảnh đã lật dọc khi chụp); pixel = point × pixelsPerPoint.
Bảng khung mặc định (khớp theo tên hoặc class): rail rộng 196, header cao 26, section header cao 36, status bar cao 20,
cột nội dung rộng 1084 — sai số ±1 px.

Cách dùng:
  measure-capture.py <thư mục…> [--tolerance 1] [--delta-e 3] [--output measurements.json]
  measure-capture.py --self-test
Thoát 0 khi mọi số đo đạt; 1 khi có lệch hoặc thiếu ảnh; khung không đo được bằng dò cạnh chỉ là cảnh báo.
"""

import argparse
import json
import math
import os
import struct
import sys
import tempfile
import zlib

DEFAULT_FRAME_EXPECTATIONS = [
    {"element": "liveops-hub-rail", "width": 196},
    {"element": "liveops-hub-header", "height": 26},
    {"element": "liveops-hub-section-header", "height": 36},
    {"element": "liveops-hub-status", "height": 20},
    {"element": "liveops-hub-content", "width": 1084},
]
CONTRAST_ALLOWANCE = 0.3
EDGE_SEARCH_RADIUS_PIXELS = 4
EDGE_MINIMUM_DELTA_E = 1.0  # nền Editor tối khác nhau vài mức xám (#383838 vs #3C3C3C ≈ ΔE 1,5)
SAMPLE_FRACTIONS = (0.25, 0.5, 0.75)
# Cạnh bạn của đường viền mảnh phải mạnh ít nhất ngần này so với cạnh mạnh nhất (viền toast skin sáng: 23,1 / 31,0 ≈ 0,75;
# viền section header skin tối: 9,3 / 11,1 ≈ 0,84) — dưới mức này coi là nhiễu khử răng cưa, không phải mép viền.
THIN_LINE_PARTNER_STRENGTH_RATIO = 0.5


# ------------------------------------------------------------------------------------------------------------ PNG

class Image(object):
    def __init__(self, width, height, pixels):
        self.width = width
        self.height = height
        self.pixels = pixels  # bytes RGB, hàng trên cùng trước

    def rgb(self, column, row):
        column = min(max(int(column), 0), self.width - 1)
        row = min(max(int(row), 0), self.height - 1)
        offset = (row * self.width + column) * 3
        return self.pixels[offset], self.pixels[offset + 1], self.pixels[offset + 2]

    def average_rgb(self, column, row, radius=1):
        totals = [0, 0, 0]
        count = 0
        for delta_y in range(-radius, radius + 1):
            for delta_x in range(-radius, radius + 1):
                red, green, blue = self.rgb(column + delta_x, row + delta_y)
                totals[0] += red
                totals[1] += green
                totals[2] += blue
                count += 1
        return tuple(total / count for total in totals)


def read_png(path):
    with open(path, "rb") as handle:
        data = handle.read()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("%s không phải PNG" % path)
    position = 8
    width = height = bit_depth = color_type = interlace = None
    compressed = bytearray()
    while position < len(data):
        length = struct.unpack(">I", data[position:position + 4])[0]
        chunk_type = data[position + 4:position + 8]
        chunk = data[position + 8:position + 8 + length]
        position += 12 + length
        if chunk_type == b"IHDR":
            width, height, bit_depth, color_type, _, _, interlace = struct.unpack(">IIBBBBB", chunk)
        elif chunk_type == b"IDAT":
            compressed.extend(chunk)
        elif chunk_type == b"IEND":
            break
    if bit_depth != 8 or interlace != 0 or color_type not in (0, 2, 4, 6):
        raise ValueError("%s: chỉ đọc PNG 8-bit không interlace (gray/RGB/RGBA), nhận depth=%s type=%s interlace=%s"
                         % (path, bit_depth, color_type, interlace))
    channels = {0: 1, 2: 3, 4: 2, 6: 4}[color_type]
    stride = width * channels
    raw = zlib.decompress(bytes(compressed))
    previous = bytearray(stride)
    output = bytearray(width * height * 3)
    for row in range(height):
        start = row * (stride + 1)
        filter_type = raw[start]
        current = bytearray(raw[start + 1:start + 1 + stride])
        for index in range(stride):
            left = current[index - channels] if index >= channels else 0
            up = previous[index]
            up_left = previous[index - channels] if index >= channels else 0
            if filter_type == 1:
                current[index] = (current[index] + left) & 0xFF
            elif filter_type == 2:
                current[index] = (current[index] + up) & 0xFF
            elif filter_type == 3:
                current[index] = (current[index] + ((left + up) >> 1)) & 0xFF
            elif filter_type == 4:
                estimate = left + up - up_left
                distance_left = abs(estimate - left)
                distance_up = abs(estimate - up)
                distance_up_left = abs(estimate - up_left)
                if distance_left <= distance_up and distance_left <= distance_up_left:
                    predictor = left
                elif distance_up <= distance_up_left:
                    predictor = up
                else:
                    predictor = up_left
                current[index] = (current[index] + predictor) & 0xFF
        for column in range(width):
            source = column * channels
            target = (row * width + column) * 3
            if channels >= 3:
                output[target:target + 3] = current[source:source + 3]
            else:
                output[target] = output[target + 1] = output[target + 2] = current[source]
        previous = current
    return Image(width, height, bytes(output))


def write_png(path, width, height, pixel_function):
    raw = bytearray()
    for row in range(height):
        raw.append(0)
        for column in range(width):
            raw.extend(pixel_function(column, row))
    def chunk(chunk_type, payload):
        return struct.pack(">I", len(payload)) + chunk_type + payload + struct.pack(">I", zlib.crc32(chunk_type + payload) & 0xFFFFFFFF)
    with open(path, "wb") as handle:
        handle.write(b"\x89PNG\r\n\x1a\n")
        handle.write(chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0)))
        handle.write(chunk(b"IDAT", zlib.compress(bytes(raw), 6)))
        handle.write(chunk(b"IEND", b""))


# ------------------------------------------------------------------------------------------------------------ màu

def parse_hex(text):
    value = text.strip().lstrip("#")
    if len(value) == 3:
        value = "".join(character * 2 for character in value)
    if len(value) not in (6, 8):
        raise ValueError("màu hex không hợp lệ: %s" % text)
    return tuple(int(value[index:index + 2], 16) for index in (0, 2, 4))


def srgb_to_linear(channel):
    normalized = channel / 255.0
    return normalized / 12.92 if normalized <= 0.04045 else ((normalized + 0.055) / 1.055) ** 2.4


def relative_luminance(rgb):
    red, green, blue = (srgb_to_linear(channel) for channel in rgb)
    return 0.2126 * red + 0.7152 * green + 0.0722 * blue


def contrast_ratio(first, second):
    lighter, darker = sorted((relative_luminance(first), relative_luminance(second)), reverse=True)
    return (lighter + 0.05) / (darker + 0.05)


def to_lab(rgb):
    red, green, blue = (srgb_to_linear(channel) for channel in rgb)
    cie_x = (0.4124 * red + 0.3576 * green + 0.1805 * blue) / 0.95047
    cie_y = (0.2126 * red + 0.7152 * green + 0.0722 * blue) / 1.0
    cie_z = (0.0193 * red + 0.1192 * green + 0.9505 * blue) / 1.08883

    def pivot(value):
        return value ** (1.0 / 3.0) if value > 0.008856 else (7.787 * value) + (16.0 / 116.0)

    pivot_x, pivot_y, pivot_z = pivot(cie_x), pivot(cie_y), pivot(cie_z)
    return 116.0 * pivot_y - 16.0, 500.0 * (pivot_x - pivot_y), 200.0 * (pivot_y - pivot_z)


def delta_e(first, second):
    # CIE76 — đủ để bắt token sai skin (khác nhau hàng chục đơn vị); ngưỡng mặc định 3 ≈ khác biệt mắt thường vừa thấy.
    lab_first, lab_second = to_lab(first), to_lab(second)
    return math.sqrt(sum((first_component - second_component) ** 2 for first_component, second_component in zip(lab_first, lab_second)))


# ------------------------------------------------------------------------------------------------------------ đo

def find_edge(image, fixed_coordinate, expected_position, horizontal, outward_sign, scale=1.0):
    """Vị trí cạnh của khung gần expected_position (mép worldBound); None khi không có cạnh rõ.

    outward_sign = -1 cho mép đầu (ngoài khung là phía toạ độ nhỏ), +1 cho mép cuối.

    Vì sao không lấy thẳng cạnh có ΔE lớn nhất: đường viền 1 px tạo HAI cạnh kề nhau (nền→viền, viền→nền). Bước mạnh hơn
    tuỳ skin — skin sáng #C8C8C8→#8A8A8A→#DEDEDE làm bước viền→thân mạnh hơn, lấy cạnh mạnh nhất là lấy mép TRONG của viền
    nên toast 24 px đo 22 và header/section header/status hụt 1 px (CC-FEEDBACK-1, L-1). Điểm ảnh một mình không biết đường
    viền thuộc khung này hay khung kề (header #CBCBCB, viền #939393 ở hàng 25, section header #CBCBCB từ hàng 26 — hai phía
    giống hệt nhau), nên cặp cạnh bao một đường mảnh được phân xử bằng mép worldBound: đường nằm TRONG khung là viền của
    chính khung → lấy mép NGOÀI của viền; đường nằm ngoài khung là viền của khung kề → lấy cạnh sát khung. Cả hai ca đều là
    cạnh gần mép worldBound nhất. Chỉ phân xử trong phạm vi một đường mảnh (≤ 1 point) nên mơ hồ tối đa 1 px = sai số; khung
    thật lệch ≥ 2 px vẫn bị bắt.
    """
    limit = image.width if horizontal else image.height
    strengths = {}
    for position in range(int(round(expected_position)) - EDGE_SEARCH_RADIUS_PIXELS, int(round(expected_position)) + EDGE_SEARCH_RADIUS_PIXELS + 1):
        if position <= 0 or position >= limit:
            continue
        if horizontal:
            before, after = image.rgb(position - 1, fixed_coordinate), image.rgb(position, fixed_coordinate)
        else:
            before, after = image.rgb(fixed_coordinate, position - 1), image.rgb(fixed_coordinate, position)
        strengths[position] = delta_e(before, after)
    if not strengths:
        return None
    best_strength = max(strengths.values())
    if best_strength < EDGE_MINIMUM_DELTA_E:
        return None
    best_positions = [position for position, strength in strengths.items() if strength == best_strength]
    strongest = min(best_positions, key=lambda position: (abs(position - expected_position), -outward_sign * position))
    # Cạnh bạn của một đường mảnh: cách cạnh mạnh nhất không quá bề dày đường (1 point = scale px), và đủ mạnh để là cạnh
    # thật chứ không phải nhiễu khử răng cưa.
    line_thickness = max(1, int(round(scale)))
    partner_minimum = max(EDGE_MINIMUM_DELTA_E, best_strength * THIN_LINE_PARTNER_STRENGTH_RATIO)
    candidates = [position for position, strength in strengths.items()
                  if abs(position - strongest) <= line_thickness and strength >= partner_minimum]
    # Hoà khoảng cách (mép worldBound lẻ nửa px) thì lấy cạnh ngoài khung.
    return min(candidates, key=lambda position: (abs(position - expected_position), -outward_sign * position))


def matching_elements(elements, key):
    return [element for element in elements if element.get("name") == key or key in (element.get("classes") or [])]


def measure_frame(image, element, expectation, scale, tolerance):
    results = []
    bound = element["worldBound"]
    for dimension in ("width", "height"):
        if dimension not in expectation:
            continue
        expected = float(expectation[dimension])
        reported = float(bound[dimension])
        entry = {"element": element.get("name"), "dimension": dimension, "expected": expected, "reported": reported}
        entry["reportedPass"] = abs(reported - expected) <= tolerance
        horizontal = dimension == "width"
        start = (bound["x"] if horizontal else bound["y"]) * scale
        end = start + reported * scale
        across_start = (bound["y"] if horizontal else bound["x"]) * scale
        across_length = (bound["height"] if horizontal else bound["width"]) * scale
        samples = []
        for fraction in SAMPLE_FRACTIONS:
            fixed = int(across_start + across_length * fraction)
            first_edge = find_edge(image, fixed, start, horizontal, -1, scale) if start > 0 else 0
            second_limit = image.width if horizontal else image.height
            second_edge = find_edge(image, fixed, end, horizontal, +1, scale) if end < second_limit else second_limit
            if first_edge is None or second_edge is None:
                continue
            samples.append((second_edge - first_edge) / scale)
        entry["measuredSamples"] = samples
        agreeing = [sample for sample in samples if abs(sample - expected) <= tolerance]
        if len(samples) >= 2:
            entry["measured"] = sorted(samples)[len(samples) // 2]
            entry["measuredPass"] = len(agreeing) >= 2
        else:
            entry["measured"] = None
            entry["measuredPass"] = None  # không đủ cạnh rõ để đo — cảnh báo, dựa vào worldBound
        entry["pass"] = entry["reportedPass"] and entry["measuredPass"] is not False
        results.append(entry)
    return results


def measure_color(image, anchor, scale, maximum_delta_e):
    point = anchor["point"]
    actual = image.average_rgb(point["x"] * scale, point["y"] * scale)
    entry = {"name": anchor.get("name"), "actual": "#%02X%02X%02X" % tuple(int(round(channel)) for channel in actual)}
    passed = True
    if anchor.get("expected"):
        expected = parse_hex(anchor["expected"])
        difference = delta_e(actual, expected)
        limit = float(anchor.get("maximumDeltaE", maximum_delta_e))
        entry.update({"expected": anchor["expected"], "deltaE": round(difference, 2), "maximumDeltaE": limit})
        passed = passed and difference <= limit
    if anchor.get("background") and anchor.get("minimumContrast") is not None:
        background_point = anchor["background"]
        background = image.average_rgb(background_point["x"] * scale, background_point["y"] * scale)
        ratio = contrast_ratio(actual, background)
        minimum = float(anchor["minimumContrast"]) - CONTRAST_ALLOWANCE
        entry.update({"background": "#%02X%02X%02X" % tuple(int(round(channel)) for channel in background),
                      "contrast": round(ratio, 2), "minimumContrast": round(minimum, 2)})
        passed = passed and ratio >= minimum
    entry["pass"] = passed
    return entry


def measure_capture(json_path, tolerance, maximum_delta_e):
    with open(json_path, encoding="utf-8") as handle:
        description = json.load(handle)
    png_path = json_path[:-len(".json")] + ".png"
    report = {"file": os.path.basename(png_path), "scenario": description.get("scenario"), "skin": description.get("skin"),
              "unityVersion": description.get("unityVersion"), "frames": [], "colors": [], "warnings": [], "errors": []}
    if not os.path.isfile(png_path):
        report["errors"].append("thiếu ảnh %s" % os.path.basename(png_path))
        return report
    image = read_png(png_path)
    scale = float(description.get("pixelsPerPoint", 1) or 1)
    report["imageSize"] = {"width": image.width, "height": image.height}
    window = description.get("window")
    if window:
        expected_width, expected_height = window["width"] * scale, window["height"] * scale
        if abs(image.width - expected_width) > tolerance or abs(image.height - expected_height) > tolerance:
            report["warnings"].append("ảnh %dx%d khác cửa sổ khai báo %dx%d (SP-16: dùng kích thước thật)" % (
                image.width, image.height, expected_width, expected_height))
    elements = description.get("elements") or []
    expectations = description.get("expectedFrames") or DEFAULT_FRAME_EXPECTATIONS
    for expectation in expectations:
        for element in matching_elements(elements, expectation["element"]):
            for entry in measure_frame(image, element, expectation, scale, tolerance):
                report["frames"].append(entry)
                if not entry["pass"]:
                    report["errors"].append("%s %s: thiết kế %.0f, worldBound %.1f, đo %s" % (
                        entry["element"], entry["dimension"], entry["expected"], entry["reported"],
                        "%.1f" % entry["measured"] if entry["measured"] is not None else "—"))
                elif entry["measuredPass"] is None:
                    report["warnings"].append("%s %s: không dò được cạnh rõ, chỉ có worldBound" % (entry["element"], entry["dimension"]))
    for anchor in description.get("colorAnchors") or []:
        entry = measure_color(image, anchor, scale, maximum_delta_e)
        report["colors"].append(entry)
        if not entry["pass"]:
            report["errors"].append("màu %s: %s" % (entry["name"], json.dumps(entry, ensure_ascii=False)))
    return report


def collect_json_files(paths):
    found = []
    for path in paths:
        if os.path.isdir(path):
            for name in sorted(os.listdir(path)):
                if name.endswith(".json") and name != "measurements.json":
                    found.append(os.path.join(path, name))
        elif path.endswith(".json"):
            found.append(path)
    return found


def run(paths, tolerance, maximum_delta_e, output):
    json_files = collect_json_files(paths)
    if not json_files:
        print("LỖI: không có file số đo .json trong %s" % ", ".join(paths))
        return 1
    reports = [measure_capture(path, tolerance, maximum_delta_e) for path in json_files]
    failure_count = 0
    for report in reports:
        status = "ĐẠT" if not report["errors"] else "LỆCH"
        print("%-5s %s (%d khung, %d màu)" % (status, report["file"], len(report["frames"]), len(report["colors"])))
        for error in report["errors"]:
            print("   lệch: " + error)
        for warning in report["warnings"]:
            print("   cảnh báo: " + warning)
        if report["errors"]:
            failure_count += 1
    destination = output
    if destination is None and len(paths) == 1 and os.path.isdir(paths[0]):
        destination = os.path.join(paths[0], "measurements.json")
    if destination:
        with open(destination, "w", encoding="utf-8") as handle:
            json.dump(reports, handle, ensure_ascii=False, indent=2)
        print("số đo → %s" % destination)
    print("MEASURE %s (%d/%d ảnh đạt)" % ("OK" if failure_count == 0 else "FAILED", len(reports) - failure_count, len(reports)))
    return 1 if failure_count else 0


def self_test_thin_borders(directory):
    """Ca viền 1 px (CC-FEEDBACK-1, L-1): số màu chép từ ảnh chụp thật, mong đợi là số thiết kế đọc tay trên pixel gốc."""
    failures = 0

    def measured_samples(stem, height, rows, elements, expected_frames):
        # rows: danh sách (hàng đầu, hàng cuối, màu) — hàng không phủ lấy màu cửa sổ của ca.
        def pixel(column, row):
            for first_row, last_row, color in rows:
                if first_row <= row <= last_row:
                    return bytes(color)
            return bytes(rows[0][2])
        case_directory = os.path.join(directory, stem)
        os.makedirs(case_directory)
        write_png(os.path.join(case_directory, stem + ".png"), 400, height, pixel)
        description = {"scenario": stem, "skin": "self-test", "unityVersion": "self-test", "pixelsPerPoint": 1,
                       "window": {"width": 400, "height": height}, "elements": elements, "expectedFrames": expected_frames}
        json_path = os.path.join(case_directory, stem + ".json")
        with open(json_path, "w", encoding="utf-8") as handle:
            json.dump(description, handle)
        report = measure_capture(json_path, 1, 3)
        return dict(((frame["element"], frame["dimension"]), frame) for frame in report["frames"]), report

    def toast_rows(window, border, body, top_row, bottom_row):
        return [(0, top_row - 1, window), (top_row, top_row, border), (top_row + 1, bottom_row - 1, body),
                (bottom_row, bottom_row, border), (bottom_row + 1, 759, window)]

    toast_element = [{"name": "hub-toast", "classes": ["liveops-hub-toast"], "worldBound": {"x": 0, "y": 708, "width": 400, "height": 24}}]
    toast_expectation = [{"element": "liveops-hub-toast", "height": 24}]
    # Pixel gốc hf-toast-bare, hàng 708 và 731 là viền tooltip: sáng #C8C8C8/#8A8A8A/#DEDEDE, tối #383838/#191919/#373737.
    for skin, window, border, body in (("light", (0xC8, 0xC8, 0xC8), (0x8A, 0x8A, 0x8A), (0xDE, 0xDE, 0xDE)),
                                       ("dark", (0x38, 0x38, 0x38), (0x19, 0x19, 0x19), (0x37, 0x37, 0x37))):
        frames, _ = measured_samples("toast-" + skin, 760, toast_rows(window, border, body, 708, 731), toast_element, toast_expectation)
        samples = frames[("hub-toast", "height")]["measuredSamples"]
        if samples != [24.0, 24.0, 24.0]:
            print("FAIL toast viền 1 px skin %s đo %s (pixel gốc 24: mép ngoài hai viền)" % (skin, samples))
            failures += 1
    # Toast vẽ thật chỉ 22 px (viền hàng 709 và 730) trong khi worldBound nói 24: phân xử đường mảnh không được che lệch này.
    frames, report = measured_samples("toast-drawn-22", 760, toast_rows((0xC8, 0xC8, 0xC8), (0x8A, 0x8A, 0x8A), (0xDE, 0xDE, 0xDE), 709, 730),
                                      toast_element, toast_expectation)
    if not report["errors"]:
        print("FAIL toast vẽ 22 px (worldBound 24) không bị bắt, đo %s" % frames[("hub-toast", "height")]["measuredSamples"])
        failures += 1

    # Khung hs-shell-skeleton skin sáng cột x = 900: header #CBCBCB viền #939393 hàng 25; section header viền hàng 61; nội
    # dung #C8C8C8; status viền trên hàng 740. Viền hàng 25 thuộc header nên section header bắt đầu ở hàng 26.
    header_background = (0xCB, 0xCB, 0xCB)
    border = (0x93, 0x93, 0x93)
    content = (0xC8, 0xC8, 0xC8)
    rows = [(0, 24, header_background), (25, 25, border), (26, 60, header_background), (61, 61, border),
            (62, 739, content), (740, 740, border), (741, 759, header_background)]
    elements = [
        {"name": "hub-header", "classes": ["liveops-hub-header"], "worldBound": {"x": 0, "y": 0, "width": 400, "height": 26}},
        {"name": "hub-section-header", "classes": ["liveops-hub-section-header"], "worldBound": {"x": 0, "y": 26, "width": 400, "height": 36}},
        {"name": "hub-status", "classes": ["liveops-hub-status"], "worldBound": {"x": 0, "y": 740, "width": 400, "height": 20}},
    ]
    frames, _ = measured_samples("shell-light", 760, rows, elements, DEFAULT_FRAME_EXPECTATIONS)
    for element, expected in (("hub-header", 26.0), ("hub-section-header", 36.0), ("hub-status", 20.0)):
        samples = frames[(element, "height")]["measuredSamples"]
        if samples != [expected] * 3:
            print("FAIL %s viền 1 px đo %s (pixel gốc %.0f)" % (element, samples, expected))
            failures += 1
    # h28b: header viền hàng 25 kề nền cửa sổ #C8C8C8 (không có section header).
    frames, _ = measured_samples("header-over-window", 760, [(0, 24, header_background), (25, 25, border), (26, 759, content)],
                                 elements[:1], DEFAULT_FRAME_EXPECTATIONS)
    if frames[("hub-header", "height")]["measuredSamples"] != [26.0] * 3:
        print("FAIL header kề nền cửa sổ đo %s (pixel gốc 26)" % frames[("hub-header", "height")]["measuredSamples"])
        failures += 1
    print("%s   ca viền 1 px (toast sáng/tối, toast vẽ 22 px, header/section header/status)" % ("ok" if not failures else "FAIL"))
    return failures


def self_test():
    window_background = (0x38, 0x38, 0x38)
    rail_background = (0x33, 0x33, 0x33)
    header_background = (0x3C, 0x3C, 0x3C)
    blocked_text = (0xFF, 0x80, 0x80)

    def build(directory, stem, rail_width):
        def pixel(column, row):
            if row < 26:
                return bytes(header_background)
            if column < rail_width:
                if 90 <= row <= 110 and 30 <= column <= 50:
                    return bytes(blocked_text)
                return bytes(rail_background)
            return bytes(window_background)
        write_png(os.path.join(directory, stem + ".png"), 400, 200, pixel)
        description = {
            "scenario": stem, "skin": "dark", "unityVersion": "self-test", "pixelsPerPoint": 1,
            "window": {"width": 400, "height": 200},
            "elements": [
                {"name": "liveops-hub-rail", "classes": ["liveops-hub-rail"], "worldBound": {"x": 0, "y": 26, "width": 196, "height": 174}},
                {"name": "header", "classes": ["liveops-hub-header"], "worldBound": {"x": 0, "y": 0, "width": 400, "height": 26}},
            ],
            "colorAnchors": [
                {"name": "blocked-text-on-rail", "point": {"x": 40, "y": 100}, "expected": "#FF8080",
                 "background": {"x": 10, "y": 150}, "minimumContrast": 4.54},
            ],
        }
        with open(os.path.join(directory, stem + ".json"), "w", encoding="utf-8") as handle:
            json.dump(description, handle)

    failures = 0
    with tempfile.TemporaryDirectory() as directory:
        good = os.path.join(directory, "good")
        bad = os.path.join(directory, "bad")
        os.makedirs(good)
        os.makedirs(bad)
        build(good, "self-dark", 196)
        build(bad, "self-dark", 199)
        image = read_png(os.path.join(good, "self-dark.png"))
        if image.rgb(10, 100) != rail_background or image.rgb(300, 100) != window_background:
            print("FAIL đọc PNG: màu điểm ảnh sai")
            failures += 1
        ratio = contrast_ratio(parse_hex("#FF8080"), parse_hex("#383838"))
        if abs(ratio - 4.83) > 0.02:
            print("FAIL tương phản #FF8080/#383838 = %.2f (thiết kế 4,83)" % ratio)
            failures += 1
        if run([good], 1, 3, None) != 0:
            print("FAIL ảnh đúng thiết kế bị báo lệch")
            failures += 1
        if run([bad], 1, 3, None) == 0:
            print("FAIL rail 199px (worldBound nói 196) không bị bắt")
            failures += 1
        failures += self_test_thin_borders(directory)
    print("MEASURE SELF-TEST %s" % ("OK" if not failures else "FAILED"))
    return 1 if failures else 0


def main():
    parser = argparse.ArgumentParser(description="Đo ảnh chụp LiveOps Hub ở độ phân giải gốc.")
    parser.add_argument("paths", nargs="*")
    parser.add_argument("--tolerance", type=float, default=1.0, help="sai số kích thước (px), mặc định 1")
    parser.add_argument("--delta-e", type=float, default=3.0, help="ΔE CIE76 tối đa cho điểm neo màu, mặc định 3")
    parser.add_argument("--output", help="ghi JSON số đo (mặc định <thư mục>/measurements.json khi đo một thư mục)")
    parser.add_argument("--self-test", action="store_true")
    arguments = parser.parse_args()
    if arguments.self_test:
        return self_test()
    if not arguments.paths:
        parser.error("cần thư mục ảnh hoặc --self-test")
    return run(arguments.paths, arguments.tolerance, arguments.delta_e, arguments.output)


if __name__ == "__main__":
    sys.exit(main())
