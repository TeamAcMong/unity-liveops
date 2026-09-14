#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""contact-sheet.py — dựng contact-sheet.html cho một bộ ảnh chụp LiveOps Hub: lưới 2022.3 | 6000.6 × dark | light.

Vì sao: cổng user duyệt ảnh (W4, W5, W7) cần một trang nhìn được mọi kịch bản cạnh hình thiết kế tương ứng, kèm bảng
đạt/lệch từ measure-capture.py. Ảnh trong trang chỉ để định vị — mỗi ô link tới PNG gốc; trang ghi rõ không kết luận
lệch/đạt bằng mắt trên ảnh thu nhỏ (mục 9.5).

Đầu vào: <thư mục nhãn> chứa <bản Unity>/<id>-<skin>.png (+ .json, measurements.json) như capture.sh ghi.
Cách dùng:
  contact-sheet.py <thư mục nhãn> [--output contact-sheet.html] [--title "W4 — J3"] [--scenarios id,id]
In đường dẫn trang; thoát 0 (trang vẫn dựng khi thiếu ảnh — ô thiếu ghi "chưa chụp").
"""

import argparse
import html
import json
import os
import sys
from urllib.parse import quote

VERSIONS = ["2022.3", "6000.6"]
SKINS = ["dark", "light"]

# Id kịch bản → hình thiết kế, khung/trạng thái, cửa sổ, gói, đợt (IMPLEMENTATION_PLAN mục 9.5, gồm vá V-11/V-13/V-14).
# Cổng đợt sinh lại khối này khi ma trận đổi (LiveOpsHubCaptureScenarioIds là nguồn id trong code).
SCENARIO_FIGURES = {
    "h01-calendar-default": {"figure": "1", "frame": "Lịch mặc định Dark: chọn hunt-0916-bonus, con trỏ 16/9 12:00, minimap, chú giải, gợi ý", "window": "1280×760", "package": "G-CALENDAR (.Calendar)", "wave": "W4"},
    "h01a-inspector-empty": {"figure": "1", "frame": "inspector (a) chưa chọn · (b) đợt sinh từ luật · (d) giờ không đọc được", "window": "1280×760", "package": "G-CALENDAR", "wave": "W4"},
    "h01b-inspector-recurring": {"figure": "1", "frame": "inspector (a) chưa chọn · (b) đợt sinh từ luật · (d) giờ không đọc được", "window": "1280×760", "package": "G-CALENDAR", "wave": "W4"},
    "h01d-inspector-unreadable": {"figure": "1", "frame": "inspector (a) chưa chọn · (b) đợt sinh từ luật · (d) giờ không đọc được", "window": "1280×760", "package": "G-CALENDAR", "wave": "W4"},
    "h02-state-marks": {"figure": "2", "frame": "4 HealthState × hàng tầng / section / việc cần làm + họ giai đoạn", "window": "640×300", "package": "G-SHELL (.Shell)", "wave": "W2"},
    "h03a-components-controls": {"figure": "3", "frame": "Button (nghỉ, hover, nhấn, focus, disabled + lý do, chính, nguy hiểm) · ToolbarToggle · Field (nghỉ, focus, lỗi) · Card (mở, thu gọn, header hover)", "window": "1080×420", "package": "G-CONTROLS (.Controls)", "wave": "W2"},
    "h03b-components-shell": {"figure": "3", "frame": "Chip (trung tính, hover, nháp a, nháp c) · Hàng rail (nghỉ, hover, active, active mất focus, focus bàn phím) · Hàng bảng (nghỉ, hover, selected, selected mất fo", "window": "1080×300", "package": "G-HOSTUI (.Shell)", "wave": "W4"},
    "h04-overview-default": {"figure": "4 / 5", "frame": "Khung + Tổng quan mặc định (Dark = Hình 4, Light = Hình 5)", "window": "1280×760", "package": "G-OVERVIEW (.Overview)", "wave": "W4"},
    "h06a-palette-empty": {"figure": "6", "frame": "palette mở trống · đang gõ \"kiem\" · không khớp", "window": "1280×760", "package": "G-HOSTUI (.Shell)", "wave": "W4"},
    "h06b-palette-typing": {"figure": "6", "frame": "palette mở trống · đang gõ \"kiem\" · không khớp", "window": "1280×760", "package": "G-HOSTUI (.Shell)", "wave": "W4"},
    "h06c-palette-no-match": {"figure": "6", "frame": "palette mở trống · đang gõ \"kiem\" · không khớp", "window": "1280×760", "package": "G-HOSTUI (.Shell)", "wave": "W4"},
    "h07a-toast-undo": {"figure": "7", "frame": "toast có Hoàn tác · Hoàn tác disabled \"Đã có thao tác khác sau đó\" · \"Đã hoàn tác…\" + Làm lại", "window": "1280×760", "package": "G-HOSTUI (.Shell); bản dựng sớm trong cửa sổ trần hf-toast-bare = G-FEEDBACK (.F", "wave": "W4"},
    "h07b-toast-undo-disabled": {"figure": "7", "frame": "toast có Hoàn tác · Hoàn tác disabled \"Đã có thao tác khác sau đó\" · \"Đã hoàn tác…\" + Làm lại", "window": "1280×760", "package": "G-HOSTUI (.Shell); bản dựng sớm trong cửa sổ trần hf-toast-bare = G-FEEDBACK (.F", "wave": "W4"},
    "h07c-toast-redo": {"figure": "7", "frame": "toast có Hoàn tác · Hoàn tác disabled \"Đã có thao tác khác sau đó\" · \"Đã hoàn tác…\" + Làm lại", "window": "1280×760", "package": "G-HOSTUI (.Shell); bản dựng sớm trong cửa sổ trần hf-toast-bare = G-FEEDBACK (.F", "wave": "W4"},
    "h07d-outcome-copied": {"figure": "7", "frame": "outcome sau Copy · outcome sau Ghi dấu", "window": "1280×760", "package": "G-EXPORT (.Export)", "wave": "W4"},
    "h07e-outcome-marked": {"figure": "7", "frame": "outcome sau Copy · outcome sau Ghi dấu", "window": "1280×760", "package": "G-EXPORT (.Export)", "wave": "W4"},
    "h08a-confirm-shorten-running": {"figure": "8", "frame": "hộp 1 rút ngắn đợt đang chạy · hộp 6 biến thể không có số", "window": "400×212", "package": "G-CALENDAR", "wave": "W4"},
    "h08f-confirm-shorten-running-no-number": {"figure": "8", "frame": "hộp 1 rút ngắn đợt đang chạy · hộp 6 biến thể không có số", "window": "400×212", "package": "G-CALENDAR", "wave": "W4"},
    "h08b-confirm-delete-published": {"figure": "8", "frame": "hộp 2 xoá đợt đã đăng chưa bắt đầu", "window": "400×212", "package": "G-CALENDAR", "wave": "W4"},
    "h08c-confirm-replace-draft": {"figure": "8", "frame": "hộp 3 thay nháp bằng JSON đã dán", "window": "400×212", "package": "G-PASTE (.Paste)", "wave": "W5"},
    "h08d-confirm-restore-published": {"figure": "8", "frame": "hộp 4 khôi phục bản đăng vào nháp", "window": "400×212", "package": "G-EXPORT", "wave": "W4"},
    "h08e-confirm-prefix-type-to-confirm": {"figure": "8", "frame": "hộp 5 cấp 2 đổi tiền tố (đang gõ thiếu một ký tự)", "window": "400×290", "package": "G-RECURRING (.Recurring)", "wave": "W4"},
    "h08g-confirm-remove-stamp": {"figure": "(S-19)", "frame": "hộp Gỡ dấu đã đăng", "window": "400×212", "package": "G-EXPORT", "wave": "W4"},
    "hf-confirm-level1-layout": {"figure": "(bố cục sớm)", "frame": "hộp cấp 1 / cấp 2 với chữ mẫu của test", "window": "400×212 · 400×290", "package": "G-FEEDBACK", "wave": "W2"},
    "hf-confirm-level2-layout": {"figure": "(bố cục sớm)", "frame": "hộp cấp 1 / cấp 2 với chữ mẫu của test", "window": "400×212 · 400×290", "package": "G-FEEDBACK", "wave": "W2"},
    "hf-toast-bare": {"figure": "7", "frame": "toast dựng sớm trong cửa sổ trần (bản W2 của Hình 7)", "window": "1280×760", "package": "G-FEEDBACK (.Feedback)", "wave": "W2"},
    "h09a-overview-no-asset": {"figure": "9", "frame": "(a) chưa có asset · (c) không còn việc chặn", "window": "1280×760", "package": "G-OVERVIEW", "wave": "W4"},
    "h09c-overview-no-blockers": {"figure": "9", "frame": "(a) chưa có asset · (c) không còn việc chặn", "window": "1280×760", "package": "G-OVERVIEW", "wave": "W4"},
    "h09b-overview-multiple-assets": {"figure": "9", "frame": "(b) nhiều asset", "window": "1280×760", "package": "G-SHELLPOLISH (.ShellPolish)", "wave": "W5"},
    "h09d-overview-never-checked": {"figure": "(chưa vẽ)", "frame": "Tổng quan chưa kiểm lần nào · đang kiểm", "window": "1280×760", "package": "G-OVERVIEW", "wave": "W4"},
    "h09e-overview-checking": {"figure": "(chưa vẽ)", "frame": "Tổng quan chưa kiểm lần nào · đang kiểm", "window": "1280×760", "package": "G-OVERVIEW", "wave": "W4"},
    "h10-event-types-default": {"figure": "10", "frame": "Loại event mặc định (chọn star-tournament, ghi đè ô 6 steel) · biến thể trùng màu chưa xử lý", "window": "1280×760", "package": "G-EVENTTYPES (.EventTypes)", "wave": "W4"},
    "h10c-event-types-color-collision": {"figure": "10", "frame": "Loại event mặc định (chọn star-tournament, ghi đè ô 6 steel) · biến thể trùng màu chưa xử lý", "window": "1280×760", "package": "G-EVENTTYPES (.EventTypes)", "wave": "W4"},
    "h10b-event-types-unknown-type": {"figure": "10b", "frame": "loại chưa khai báo sau khi dán JSON (đặt bản remote qua API phiên, không cần popover)", "window": "1280×760", "package": "G-EVENTTYPES", "wave": "W4"},
    "h11-calendar-compare-pane": {"figure": "11", "frame": "Lịch Light + pane So với đã đăng + chip nháp (a) + toast sau kéo", "window": "1280×760", "package": "G-CALENDAR-DEPTH (.CalendarDepth)", "wave": "W5"},
    "h12-frame-01": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-03a": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-03b": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-04": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-05": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-06": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-07": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-08": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-11": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-13": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-14": {"figure": "12", "frame": "khung 1 nghỉ · 3 đã chọn có focus / mất focus · 4 focus bàn phím · 5 kéo thân · 6 kéo mép cuối · 7 kéo sẽ chồng · 8 ⌘⌫ xoá + toast · 11 zoom Tháng · 13 chip đợt", "window": "803×420", "package": "G-CALENDAR", "wave": "W4"},
    "h12-frame-02": {"figure": "12", "frame": "khung 2 hover (tay nắm 2×12 + hover card 500ms)", "window": "803×420", "package": "G-CALENDAR-DEPTH", "wave": "W5"},
    "h12-frame-09": {"figure": "12", "frame": "khung 9 chọn nhiều + khung chọn · khung 12 làn thu gọn", "window": "803×420", "package": "G-OPT-TIMELINE (.OptTimeline)", "wave": "W6 (tuỳ chọn)"},
    "h12-frame-12": {"figure": "12", "frame": "khung 9 chọn nhiều + khung chọn · khung 12 làn thu gọn", "window": "803×420", "package": "G-OPT-TIMELINE (.OptTimeline)", "wave": "W6 (tuỳ chọn)"},
    "ht-timeline-figure1": {"figure": "(sớm)", "frame": "timeline trần theo toạ độ Hình 1 · zoom Tháng · kéo sẽ chồng", "window": "803×420", "package": "G-TIMELINE-VIEW (.Timeline)", "wave": "W3"},
    "ht-timeline-month": {"figure": "(sớm)", "frame": "timeline trần theo toạ độ Hình 1 · zoom Tháng · kéo sẽ chồng", "window": "803×420", "package": "G-TIMELINE-VIEW (.Timeline)", "wave": "W3"},
    "ht-timeline-drag-overlap": {"figure": "(sớm)", "frame": "timeline trần theo toạ độ Hình 1 · zoom Tháng · kéo sẽ chồng", "window": "803×420", "package": "G-TIMELINE-VIEW (.Timeline)", "wave": "W3"},
    "h13-calendar-narrow-drawer": {"figure": "13", "frame": "cửa sổ 820×560: drawer inspector, rail 36px, toolbar rút gọn", "window": "820×560", "package": "G-CALENDAR-DEPTH", "wave": "W5"},
    "h13b-add-event-step1": {"figure": "13b", "frame": "popover Thêm đợt bước 1 · 2 · 3 · biến thể chồng giờ", "window": "320×N", "package": "G-CALENDAR", "wave": "W4"},
    "h13b-add-event-step2": {"figure": "13b", "frame": "popover Thêm đợt bước 1 · 2 · 3 · biến thể chồng giờ", "window": "320×N", "package": "G-CALENDAR", "wave": "W4"},
    "h13b-add-event-step3": {"figure": "13b", "frame": "popover Thêm đợt bước 1 · 2 · 3 · biến thể chồng giờ", "window": "320×N", "package": "G-CALENDAR", "wave": "W4"},
    "h13b-add-event-overlap": {"figure": "13b", "frame": "popover Thêm đợt bước 1 · 2 · 3 · biến thể chồng giờ", "window": "320×N", "package": "G-CALENDAR", "wave": "W4"},
    "h14a-recurring-default": {"figure": "14", "frame": "Luật lặp mặc định · (b) đang gõ tiền tố mới · sau khi ghi (HelpBox ở lại) · chạy lâu hơn chu kỳ", "window": "1280×760", "package": "G-RECURRING", "wave": "W4"},
    "h14b-recurring-prefix-draft": {"figure": "14", "frame": "Luật lặp mặc định · (b) đang gõ tiền tố mới · sau khi ghi (HelpBox ở lại) · chạy lâu hơn chu kỳ", "window": "1280×760", "package": "G-RECURRING", "wave": "W4"},
    "h14c-recurring-after-write": {"figure": "14", "frame": "Luật lặp mặc định · (b) đang gõ tiền tố mới · sau khi ghi (HelpBox ở lại) · chạy lâu hơn chu kỳ", "window": "1280×760", "package": "G-RECURRING", "wave": "W4"},
    "h14d-recurring-active-longer": {"figure": "14", "frame": "Luật lặp mặc định · (b) đang gõ tiền tố mới · sau khi ghi (HelpBox ở lại) · chạy lâu hơn chu kỳ", "window": "1280×760", "package": "G-RECURRING", "wave": "W4"},
    "h14e-recurring-json-error": {"figure": "(W5)", "frame": "Foldout JSON sửa được + lỗi \"Dòng 3, ký tự 18: thiếu dấu phẩy\"", "window": "1280×760", "package": "G-RECURRING-JSON (.RecurringJson)", "wave": "W5"},
    "h15-validation-default": {"figure": "15 / 16", "frame": "Kiểm lịch mặc định (Dark = Hình 15, Light = Hình 16)", "window": "1280×760", "package": "G-VALIDATION (.Validation)", "wave": "W4"},
    "h17-2a-stale-edited": {"figure": "17", "frame": "ô 2 kết quả cũ (do sửa) + biến thể cũ do mốc · ô 3 đang kiểm · ô 4 không còn lỗi · ô 7 popover Đề xuất… · ô 8 toast sau Áp", "window": "1280×760", "package": "G-VALIDATION", "wave": "W4"},
    "h17-2b-stale-milestone": {"figure": "17", "frame": "ô 2 kết quả cũ (do sửa) + biến thể cũ do mốc · ô 3 đang kiểm · ô 4 không còn lỗi · ô 7 popover Đề xuất… · ô 8 toast sau Áp", "window": "1280×760", "package": "G-VALIDATION", "wave": "W4"},
    "h17-3-running": {"figure": "17", "frame": "ô 2 kết quả cũ (do sửa) + biến thể cũ do mốc · ô 3 đang kiểm · ô 4 không còn lỗi · ô 7 popover Đề xuất… · ô 8 toast sau Áp", "window": "1280×760", "package": "G-VALIDATION", "wave": "W4"},
    "h17-4-no-errors": {"figure": "17", "frame": "ô 2 kết quả cũ (do sửa) + biến thể cũ do mốc · ô 3 đang kiểm · ô 4 không còn lỗi · ô 7 popover Đề xuất… · ô 8 toast sau Áp", "window": "1280×760", "package": "G-VALIDATION", "wave": "W4"},
    "h17-7-proposal-popover": {"figure": "17", "frame": "ô 2 kết quả cũ (do sửa) + biến thể cũ do mốc · ô 3 đang kiểm · ô 4 không còn lỗi · ô 7 popover Đề xuất… · ô 8 toast sau Áp", "window": "1280×760", "package": "G-VALIDATION", "wave": "W4"},
    "h17-8-toast-after-apply": {"figure": "17", "frame": "ô 2 kết quả cũ (do sửa) + biến thể cũ do mốc · ô 3 đang kiểm · ô 4 không còn lỗi · ô 7 popover Đề xuất… · ô 8 toast sau Áp", "window": "1280×760", "package": "G-VALIDATION", "wave": "W4"},
    "h17-1-safe-bulk-preview": {"figure": "17", "frame": "ô 1 xem trước sửa hàng loạt · ô 5 popover Bỏ qua cảnh báo · ô 6 nhóm Đã bỏ qua", "window": "1280×760", "package": "G-VALIDATION-DEPTH (.ValidationDepth)", "wave": "W5"},
    "h17-5-ignore-popover": {"figure": "17", "frame": "ô 1 xem trước sửa hàng loạt · ô 5 popover Bỏ qua cảnh báo · ô 6 nhóm Đã bỏ qua", "window": "1280×760", "package": "G-VALIDATION-DEPTH (.ValidationDepth)", "wave": "W5"},
    "h17-6-ignored-group": {"figure": "17", "frame": "ô 1 xem trước sửa hàng loạt · ô 5 popover Bỏ qua cảnh báo · ô 6 nhóm Đã bỏ qua", "window": "1280×760", "package": "G-VALIDATION-DEPTH (.ValidationDepth)", "wave": "W5"},
    "h17-10-rule-failed": {"figure": "(chưa vẽ)", "frame": "luật ném lỗi · chỉ còn Nên xem", "window": "1280×760", "package": "G-VALIDATION", "wave": "W4"},
    "h17-11-should-review-only": {"figure": "(chưa vẽ)", "frame": "luật ném lỗi · chỉ còn Nên xem", "window": "1280×760", "package": "G-VALIDATION", "wave": "W4"},
    "h17-9-remote-drift": {"figure": "(chưa vẽ)", "frame": "đã dán JSON và lệch bản", "window": "1280×760", "package": "G-PASTE", "wave": "W5"},
    "h18-export-blocked": {"figure": "18", "frame": "Xuất JSON bị chặn (a)", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-b-ready": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-c-copied": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-cprime-saved-file": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-d-stale": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-e-marked": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-f-no-changes": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-g-first-publish": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-h-parser-failed": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-i-restoring": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-j-format1": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-k-review-required": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h19-parser-mismatch": {"figure": "19", "frame": "(b) sẵn sàng · (c) vừa copy · (c′) vừa lưu file · (d) kiểm cũ · (e) đã ghi dấu · (f) không có thay đổi · (g) lần đăng đầu · (h) parser đọc lại thất bại · (i) đa", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h20a-mark-published-ready": {"figure": "20", "frame": "trái đủ điều kiện · giữa thiếu xác nhận + ghi chú · phải nháp đổi sau lần copy", "window": "410×330", "package": "G-EXPORT", "wave": "W4"},
    "h20b-mark-published-missing": {"figure": "20", "frame": "trái đủ điều kiện · giữa thiếu xác nhận + ghi chú · phải nháp đổi sau lần copy", "window": "410×330", "package": "G-EXPORT", "wave": "W4"},
    "h20c-mark-published-draft-changed": {"figure": "20", "frame": "trái đủ điều kiện · giữa thiếu xác nhận + ghi chú · phải nháp đổi sau lần copy", "window": "410×330", "package": "G-EXPORT", "wave": "W4"},
    "h21-export-json-top": {"figure": "21", "frame": "JSON viewer Light, dòng 1–24, cao 385px", "window": "1280×760", "package": "G-EXPORT", "wave": "W4"},
    "h28a-failure-section": {"figure": "28", "frame": "khung 1 màn ném lỗi · khung 2 thiếu UXML · khung 3 đang biên dịch", "window": "1280×760", "package": "G-SHELL", "wave": "W2"},
    "h28b-missing-uxml": {"figure": "28", "frame": "khung 1 màn ném lỗi · khung 2 thiếu UXML · khung 3 đang biên dịch", "window": "1280×760", "package": "G-SHELL", "wave": "W2"},
    "h28c-compiling": {"figure": "28", "frame": "khung 1 màn ném lỗi · khung 2 thiếu UXML · khung 3 đang biên dịch", "window": "1280×760", "package": "G-SHELL", "wave": "W2"},
    "h28d-disk-changed-banner": {"figure": "28", "frame": "khung 4 asset đổi từ bên ngoài · hộp \"Ghi đè 3 mục vừa đổi trên đĩa?\"", "window": "1280×760 · 400×212", "package": "G-SHELLPOLISH", "wave": "W5"},
    "h28e-confirm-overwrite-disk": {"figure": "28", "frame": "khung 4 asset đổi từ bên ngoài · hộp \"Ghi đè 3 mục vừa đổi trên đĩa?\"", "window": "1280×760 · 400×212", "package": "G-SHELLPOLISH", "wave": "W5"},
    "hs-shell-skeleton": {"figure": "(khung)", "frame": "khung với section giữ chỗ · khung có phiên thật · hẹp 820 rail 36px · compact 700", "window": "1280×760 · 820×560 · 700×520", "package": "như cột trước", "wave": "W2–W5"},
    "hs-shell-with-session": {"figure": "(khung)", "frame": "khung với section giữ chỗ · khung có phiên thật · hẹp 820 rail 36px · compact 700", "window": "1280×760 · 820×560 · 700×520", "package": "như cột trước", "wave": "W2–W5"},
    "hs-shell-narrow-820": {"figure": "(khung)", "frame": "khung với section giữ chỗ · khung có phiên thật · hẹp 820 rail 36px · compact 700", "window": "1280×760 · 820×560 · 700×520", "package": "như cột trước", "wave": "W2–W5"},
    "hs-shell-compact-700": {"figure": "(khung)", "frame": "khung với section giữ chỗ · khung có phiên thật · hẹp 820 rail 36px · compact 700", "window": "1280×760 · 820×560 · 700×520", "package": "như cột trước", "wave": "W2–W5"},
    "hc-controls-gallery": {"figure": "(control)", "frame": "gallery control dùng chung", "window": "1080×600", "package": "G-CONTROLS", "wave": "W2"},
    "ht-timeline-day-ruler": {"figure": "(vá V-11)", "frame": "timeline zoom Ngày: thước 3 tầng (tầng giờ máy) + bubble giờ tại con trỏ", "window": "803×420", "package": "G-TIMELINE-VIEW (.Timeline)", "wave": "W3"},
    "h09f-overview-import-json": {"figure": "(vá V-14)", "frame": "Tổng quan (a) popover \"Nhập JSON đang chạy…\" (đã dán, toggle ghi dấu bật)", "window": "1280×760", "package": "G-PASTE (.Paste)", "wave": "W5"},
    "h19-l-compare-remote": {"figure": "(vá V-13)", "frame": "Xuất JSON card diff với nguồn bản remote đã dán", "window": "1280×760", "package": "G-EXPORT (.Export)", "wave": "W4"},
    "h28f-calendar-compare-disk": {"figure": "28 khung 4 → (vá V-13)", "frame": "\"Xem khác biệt\": pane So với nguồn bản trên đĩa", "window": "1280×760", "package": "G-CALENDAR-DEPTH (.CalendarDepth)", "wave": "W5"},
    "ho-shortcut-help": {"figure": "(P1-lùi)", "frame": "popover hướng dẫn phím tắt", "window": "320×N", "package": "G-OPT-SHORTCUTHELP (.ShortcutHelp)", "wave": "W6 (tuỳ chọn)"},
}


def load_measurements(directory):
    path = os.path.join(directory, "measurements.json")
    if not os.path.isfile(path):
        return {}
    with open(path, encoding="utf-8") as handle:
        reports = json.load(handle)
    return {report["file"]: report for report in reports}


def discover(root, scenario_filter):
    found = set()
    for version in VERSIONS:
        directory = os.path.join(root, version)
        if not os.path.isdir(directory):
            continue
        for name in os.listdir(directory):
            if not name.endswith(".png"):
                continue
            stem = name[:-len(".png")]
            for skin in SKINS:
                if stem.endswith("-" + skin):
                    found.add(stem[:-len(skin) - 1])
    if scenario_filter:
        found |= set(scenario_filter)
        found &= set(scenario_filter)
    ordered = [identifier for identifier in SCENARIO_FIGURES if identifier in found]
    ordered.extend(sorted(found - set(ordered)))
    return ordered


def cell_html(root, version, identifier, skin, measurements):
    file_name = "%s-%s.png" % (identifier, skin)
    relative = "%s/%s" % (version, file_name)
    if not os.path.isfile(os.path.join(root, version, file_name)):
        return '<td class="missing">chưa chụp</td>', "missing"
    report = measurements.get(version, {}).get(file_name)
    if report is None:
        status, badge = "unmeasured", '<span class="badge unmeasured">chưa đo</span>'
    elif report.get("errors"):
        status, badge = "deviation", '<span class="badge deviation">LỆCH %d</span>' % len(report["errors"])
    else:
        status, badge = "pass", '<span class="badge pass">ĐẠT</span>'
    details = ""
    if report:
        size = report.get("imageSize") or {}
        lines = ["%s×%s px" % (size.get("width", "?"), size.get("height", "?"))]
        lines.extend("lệch: " + error for error in report.get("errors", []))
        lines.extend("cảnh báo: " + warning for warning in report.get("warnings", []))
        details = "<ul>%s</ul>" % "".join("<li>%s</li>" % html.escape(line) for line in lines)
    link = quote(relative)
    return ('<td class="%s"><a href="%s" target="_blank" title="Mở ảnh gốc"><img loading="lazy" src="%s" alt="%s"></a>'
            '<div class="meta">%s %s</div>%s</td>' % (status, link, link, html.escape(file_name), badge,
                                                        '<a href="%s">ảnh gốc</a>' % link, details)), status


def build(root, output, title, scenario_filter):
    measurements = {version: load_measurements(os.path.join(root, version)) for version in VERSIONS}
    identifiers = discover(root, scenario_filter)
    totals = {(version, skin): {"pass": 0, "deviation": 0, "missing": 0, "unmeasured": 0} for version in VERSIONS for skin in SKINS}
    rows = []
    for identifier in identifiers:
        figure = SCENARIO_FIGURES.get(identifier, {})
        cells = []
        for version in VERSIONS:
            for skin in SKINS:
                cell, status = cell_html(root, version, identifier, skin, measurements)
                totals[(version, skin)][status] += 1
                cells.append(cell)
        heading = ('<th scope="row"><code>%s</code><div class="figure">Hình %s · %s</div>'
                   '<div class="frame">%s</div><div class="owner">%s · %s</div></th>') % (
            html.escape(identifier), html.escape(figure.get("figure", "?")), html.escape(figure.get("window", "")),
            html.escape(figure.get("frame", "không có trong ma trận 9.5")), html.escape(figure.get("package", "")),
            html.escape(figure.get("wave", "")))
        rows.append("<tr>%s%s</tr>" % (heading, "".join(cells)))
    summary_cells = "".join(
        "<td>%d đạt · %d lệch · %d chưa đo · %d chưa chụp</td>" % (
            totals[(version, skin)]["pass"], totals[(version, skin)]["deviation"],
            totals[(version, skin)]["unmeasured"], totals[(version, skin)]["missing"])
        for version in VERSIONS for skin in SKINS)
    header_cells = "".join("<th>%s · %s</th>" % (version, skin) for version in VERSIONS for skin in SKINS)
    page = """<!doctype html>
<html lang="vi"><head><meta charset="utf-8"><title>%(title)s</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
:root { --ground: #f4f4f2; --ink: #1d1d1b; --muted: #5d5d58; --line: #d4d4cf; --pass: #1f7a3a; --deviation: #b3261e; --unmeasured: #8a6100; }
@media (prefers-color-scheme: dark) { :root { --ground: #1b1b1a; --ink: #ecece8; --muted: #a3a39c; --line: #3a3a37; --pass: #6fcf8a; --deviation: #ff8a80; --unmeasured: #f4bc02; } }
body { background: var(--ground); color: var(--ink); font: 14px/1.45 -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; margin: 24px; }
h1 { font-size: 20px; margin: 0 0 4px; }
.warning { border-left: 4px solid var(--unmeasured); padding: 8px 12px; margin: 12px 0 20px; max-width: 900px; }
.table-scroll { overflow-x: auto; }
table { border-collapse: collapse; min-width: 1100px; }
th, td { border: 1px solid var(--line); vertical-align: top; padding: 8px; text-align: left; }
th[scope="row"] { width: 220px; }
td { width: 22%%; }
img { max-width: 100%%; display: block; border: 1px solid var(--line); }
.figure, .owner { color: var(--muted); font-size: 12px; }
.frame { font-size: 12px; margin: 4px 0; }
.meta { margin-top: 6px; font-size: 12px; }
ul { margin: 6px 0 0; padding-left: 16px; font-size: 12px; }
.badge { font-weight: 600; margin-right: 8px; }
.badge.pass { color: var(--pass); } .badge.deviation { color: var(--deviation); } .badge.unmeasured { color: var(--unmeasured); }
td.missing { color: var(--muted); font-style: italic; }
</style></head><body>
<h1>%(title)s</h1>
<div>Thư mục: <code>%(root)s</code> · %(count)d kịch bản</div>
<p class="warning">Ảnh trong lưới chỉ để định vị. <strong>Không kết luận lệch/đạt bằng mắt trên ảnh thu nhỏ</strong> — dùng cột số đo
(measure-capture.py, đo trên PNG gốc) hoặc mở “ảnh gốc” ở độ phân giải thật.</p>
<div class="table-scroll"><table>
<thead><tr><th>Tổng</th>%(summary)s</tr><tr><th>Kịch bản</th>%(headers)s</tr></thead>
<tbody>%(rows)s</tbody></table></div>
</body></html>
""" % {"title": html.escape(title), "root": html.escape(root), "count": len(identifiers), "summary": summary_cells,
       "headers": header_cells, "rows": "\n".join(rows)}
    with open(output, "w", encoding="utf-8") as handle:
        handle.write(page)
    return identifiers, totals


def main():
    parser = argparse.ArgumentParser(description="Dựng contact-sheet.html cho ảnh chụp LiveOps Hub.")
    parser.add_argument("root", help="thư mục nhãn (chứa 2022.3/ và 6000.6/)")
    parser.add_argument("--output")
    parser.add_argument("--title")
    parser.add_argument("--scenarios", help="chỉ các id này, phân tách bằng dấu phẩy")
    arguments = parser.parse_args()
    root = os.path.realpath(arguments.root)
    if not os.path.isdir(root):
        print("LỖI: không có thư mục %s" % root)
        return 2
    output = arguments.output or os.path.join(root, "contact-sheet.html")
    title = arguments.title or "LiveOps Hub — %s" % os.path.basename(root)
    scenario_filter = [item for item in (arguments.scenarios or "").split(",") if item]
    identifiers, totals = build(root, output, title, scenario_filter)
    deviations = sum(value["deviation"] for value in totals.values())
    print("contact sheet: %s (%d kịch bản, %d ô lệch)" % (output, len(identifiers), deviations))
    return 0


if __name__ == "__main__":
    sys.exit(main())
