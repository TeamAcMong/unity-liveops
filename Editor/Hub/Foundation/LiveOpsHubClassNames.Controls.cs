namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class thành phần của vùng Controls (V-5, G-CONTROLS) không có trong bảng 8.10: phần con của ô ngày giờ UTC, tab tự làm,
    /// thanh chu kỳ và dòng JSON. Tách file vùng vì file gốc do G-SKELETON đóng từ W0; thiếu hằng thì <c>check-class-names.py</c>
    /// chặn class gõ tay trong USS/UXML/C#. Class gốc <c>liveops-hub-cycle-bar</c>, <c>-json-view</c>, <c>-json-gutter</c>,
    /// <c>-json-overview</c> đã có ở bảng 8.10 nên không khai lại.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Ô ngày giờ UTC — ô ngày 76 + ô giờ 44 + nhãn "UTC" + dòng giờ máy + dòng lỗi ([FD §2.16] hàng Field).
        internal const string UtcField = "liveops-hub-utc-field";
        internal const string UtcFieldInput = "liveops-hub-utc-field__input";
        internal const string UtcFieldRow = "liveops-hub-utc-field__row";
        internal const string UtcFieldDate = "liveops-hub-utc-field__date";
        internal const string UtcFieldTime = "liveops-hub-utc-field__time";
        internal const string UtcFieldPartError = "liveops-hub-utc-field__part--error";
        internal const string UtcFieldZone = "liveops-hub-utc-field__zone";
        internal const string UtcFieldErrorIcon = "liveops-hub-utc-field__error-icon";
        internal const string UtcFieldDeviceLine = "liveops-hub-utc-field__device-line";
        internal const string UtcFieldError = "liveops-hub-utc-field__error";
        internal const string UtcFieldHidden = "liveops-hub-utc-field--hidden";

        // Tab tự làm = Toolbar + ToolbarToggle loại trừ nhau (TabView chỉ có từ Unity 6).
        internal const string TabStrip = "liveops-hub-tab-strip";
        internal const string TabStripSlot = "liveops-hub-tab-strip__slot";
        internal const string TabStripTab = "liveops-hub-tab-strip__tab";

        // Thanh chu kỳ luật lặp — ba con theo flex-grow bằng số giờ: chạy (màu loại) · nghỉ · tràn (blocked-fill).
        internal const string CycleBarRun = "liveops-hub-cycle-bar__run";
        internal const string CycleBarRest = "liveops-hub-cycle-bar__rest";
        internal const string CycleBarOverflow = "liveops-hub-cycle-bar__overflow";
        internal const string CycleBarPartHidden = "liveops-hub-cycle-bar__part--hidden";

        // JSON viewer — ListView dòng 16px ([FD §2.6], [SD2 §3.6]).
        internal const string JsonViewTabs = "liveops-hub-json-view__tabs";
        internal const string JsonViewBody = "liveops-hub-json-view__body";
        internal const string JsonViewList = "liveops-hub-json-view__list";
        internal const string JsonLine = "liveops-hub-json-line";
        internal const string JsonLineBlocked = "liveops-hub-json-line--blocked";
        internal const string JsonLineWarning = "liveops-hub-json-line--warning";
        internal const string JsonLineNumber = "liveops-hub-json-line-number";
        internal const string JsonGutterIcon = "liveops-hub-json-gutter-icon";
        internal const string JsonGutterMark = "liveops-hub-json-gutter-mark";
        internal const string JsonCode = "liveops-hub-json-code";
        internal const string JsonAnnotation = "liveops-hub-json-annotation";
        internal const string JsonPartHidden = "liveops-hub-json-view__part--hidden";
        internal const string JsonOverviewMarker = "liveops-hub-json-overview-marker";
        internal const string JsonOverviewMarkerDropped = "liveops-hub-json-overview-marker--dropped";
        internal const string JsonOverviewMarkerWarning = "liveops-hub-json-overview-marker--warning";
        internal const string JsonOverviewMarkerChange = "liveops-hub-json-overview-marker--change";
        internal const string JsonOverviewViewport = "liveops-hub-json-overview-viewport";

        // Cắt giữa chữ dài (id đợt, sha, đường dẫn): USS -unity-text-overflow-position: middle là đường chính (SP-6 kiểm chứng).
        internal const string TextElideMiddle = "liveops-hub-text--elide-middle";
    }
}
