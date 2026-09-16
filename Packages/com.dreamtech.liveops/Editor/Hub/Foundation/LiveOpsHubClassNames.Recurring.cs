namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class USS riêng của màn Luật lặp (<c>recurring</c>, G-RECURRING) — tiền tố <c>liveops-hub-recurring-…</c> theo V-5:
    /// class chỉ màn này dùng khai ở đây, không ở file gốc. Class dùng chung mà màn này chỉ mượn (câu token
    /// <c>liveops-hub-rule-sentence</c>, thanh chu kỳ <c>liveops-hub-cycle-bar</c>, card, tag, mono) vẫn nằm ở
    /// <see cref="LiveOpsHubClassNames"/> gốc — hai nơi khai cùng một giá trị là lỗi mà <c>check-class-names.py</c> bắt.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Bố cục hai pane [SD1 §4.1]: trái 240px danh sách luật · phải form max-width 640.
        internal const string RecurringSplit = "liveops-hub-recurring-split";
        internal const string RecurringList = "liveops-hub-recurring-list";
        internal const string RecurringListRow = "liveops-hub-recurring-list-row";
        internal const string RecurringListRowSelected = "liveops-hub-recurring-list-row--selected";
        internal const string RecurringListName = "liveops-hub-recurring-list-name";
        internal const string RecurringListMeta = "liveops-hub-recurring-list-meta";
        internal const string RecurringForm = "liveops-hub-recurring-form";
        internal const string RecurringFormScroll = "liveops-hub-recurring-form-scroll";

        // Ẩn/hiện một khối của màn bằng class (luật style inline [FD §2.14] chỉ cho hình học suy từ dữ liệu, không cho display).
        internal const string RecurringHidden = "liveops-hub-recurring-hidden";

        // Một hàng field: nhãn 96px · control · chữ phụ ("= 7 ngày", "thứ Hai · 07:00 giờ máy").
        internal const string RecurringFieldRow = "liveops-hub-recurring-field-row";
        internal const string RecurringFieldLabel = "liveops-hub-recurring-field-label";
        internal const string RecurringFieldLabelFocused = "liveops-hub-recurring-field-label--focused";
        internal const string RecurringFieldInput = "liveops-hub-recurring-field-input";
        internal const string RecurringFieldSuffix = "liveops-hub-recurring-field-suffix";
        internal const string RecurringFieldDrafting = "liveops-hub-recurring-field-input--drafting";
        internal const string RecurringFieldNumber = "liveops-hub-recurring-field-number";

        // Khối cảnh báo tại chỗ dưới ô đang sửa [SD1 §4.2]: dòng phụ "Nháp tại ô này…" + HelpBox + hàng nút.
        internal const string RecurringDraftBlock = "liveops-hub-recurring-draft-block";
        internal const string RecurringDraftNotice = "liveops-hub-recurring-draft-notice";
        internal const string RecurringDraftActions = "liveops-hub-recurring-draft-actions";
        internal const string RecurringAfterWrite = "liveops-hub-recurring-after-write";
        internal const string RecurringAfterWriteActions = "liveops-hub-recurring-after-write-actions";
        internal const string RecurringLink = "liveops-hub-recurring-link";

        // Thanh chu kỳ + câu đọc "chạy 7 ngày · liền mạch, không nghỉ".
        internal const string RecurringCycleRow = "liveops-hub-recurring-cycle-row";
        internal const string RecurringCycleText = "liveops-hub-recurring-cycle-text";

        // Card "5 đợt kế tiếp": bảng 190 / 96 / 96 / 70 / giãn.
        internal const string RecurringOccurrences = "liveops-hub-recurring-occurrences";
        internal const string RecurringOccurrencesHead = "liveops-hub-recurring-occurrences-head";
        internal const string RecurringOccurrencesRow = "liveops-hub-recurring-occurrences-row";
        internal const string RecurringOccurrencesFoot = "liveops-hub-recurring-occurrences-foot";
        internal const string RecurringCellId = "liveops-hub-recurring-cell-id";
        internal const string RecurringCellArrow = "liveops-hub-recurring-cell-arrow";
        internal const string RecurringCellStart = "liveops-hub-recurring-cell-start";
        internal const string RecurringCellEnd = "liveops-hub-recurring-cell-end";
        internal const string RecurringCellDevice = "liveops-hub-recurring-cell-device";
        internal const string RecurringCellNow = "liveops-hub-recurring-cell-now";

        // Popover "Thêm luật" (chọn loại chưa có luật + mẫu) và foldout JSON chỉ đọc của W4.
        internal const string RecurringAdd = "liveops-hub-recurring-add";
        internal const string RecurringAddActions = "liveops-hub-recurring-add-actions";
        internal const string RecurringJson = "liveops-hub-recurring-json";
        internal const string RecurringJsonActions = "liveops-hub-recurring-json-actions";
    }
}
