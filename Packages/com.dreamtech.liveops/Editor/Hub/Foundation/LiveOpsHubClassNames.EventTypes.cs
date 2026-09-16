namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class riêng của màn Loại event (V-5, G-EVENTTYPES) — mọi giá trị mang tiền tố <c>liveops-hub-event-types-</c> theo id màn,
    /// nên <c>check-class-names.py</c> buộc chúng chỉ được khai ở đúng file này. Thành phần dùng chung (card, swatch, dấu trạng
    /// thái, nút, hàng phát hiện) lấy từ bảng 8.10 — file này chỉ khai thứ chỉ màn này có.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        /// <summary>Gốc thân màn (bọc cả trạng thái trống lẫn bảng + inspector).</summary>
        internal const string EventTypesBody = "liveops-hub-event-types-body";

        /// <summary>Hàng ngang: cột bảng <c>flex:1</c> | inspector 300px (flex none, viền trái) [SD1 §2.1].</summary>
        internal const string EventTypesContent = "liveops-hub-event-types-content";
        internal const string EventTypesTableColumn = "liveops-hub-event-types-table-column";
        internal const string EventTypesTable = "liveops-hub-event-types-table";
        internal const string EventTypesFooter = "liveops-hub-event-types-footer";

        /// <summary>Ô của bảng; biến thể căn phải cho cột "Đợt".</summary>
        internal const string EventTypesCell = "liveops-hub-event-types-cell";
        internal const string EventTypesCellRight = "liveops-hub-event-types-cell--right";

        /// <summary>Hàng loại chưa khai báo (Hình 10b): mờ 0,7, swatch viền đứt không màu.</summary>
        internal const string EventTypesRowUndeclared = "liveops-hub-event-types-row--undeclared";
        internal const string EventTypesSwatchUndeclared = "liveops-hub-event-types-swatch--undeclared";

        /// <summary>Dòng dưới bảng của loại chưa khai báo: dấu Blocked + câu + nút "Khai báo".</summary>
        internal const string EventTypesUnknownBar = "liveops-hub-event-types-unknown-bar";

        /// <summary>Card tham chiếu phát hiện của Kiểm lịch (sọc Blocked) nằm trong cột bảng.</summary>
        internal const string EventTypesReferenceCard = "liveops-hub-event-types-reference-card";

        // Inspector 300px.
        internal const string EventTypesInspector = "liveops-hub-event-types-inspector";
        internal const string EventTypesInspectorTitle = "liveops-hub-event-types-inspector-title";
        internal const string EventTypesField = "liveops-hub-event-types-field";
        internal const string EventTypesFieldLabel = "liveops-hub-event-types-field-label";
        internal const string EventTypesFieldSub = "liveops-hub-event-types-field-sub";

        /// <summary>Dãy 8 <c>ToolbarToggle</c> 18×18 dính liền nhau như một nhóm toolbar.</summary>
        internal const string EventTypesColorSlots = "liveops-hub-event-types-color-slots";
        internal const string EventTypesColorSlot = "liveops-hub-event-types-color-slot";

        /// <summary>Ô trống được gợi ý khi còn trùng màu: viền focus 1px, KHÔNG tự ghi (PD Q1 [SD1 Q1]).</summary>
        internal const string EventTypesColorSlotSuggested = "liveops-hub-event-types-color-slot--suggested";

        internal const string EventTypesUsageCard = "liveops-hub-event-types-usage-card";
    }
}
