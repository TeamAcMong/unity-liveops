namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class thành phần của vùng Shell (V-5, G-SHELL → G-HOSTUI → G-SHELLPOLISH) không có trong bảng 8.10: bố cục cột nội dung,
    /// nửa đường nối của gutter rail, các phần của ô chặn, card lỗi và note đầu thân. Tách file vùng vì file gốc do G-SKELETON
    /// đóng từ W0; thiếu hằng thì <c>check-class-names.py</c> chặn class gõ tay trong USS/UXML/C#.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Bố cục khung — "liveops-hub-content" là tên measure-capture.py đo bề rộng cột nội dung 1084.
        internal const string Main = "liveops-hub-main";
        internal const string Content = "liveops-hub-content";
        internal const string HeaderTitle = "liveops-hub-header-title";
        internal const string SectionHeading = "liveops-hub-section-heading";
        internal const string SectionActionsEmpty = "liveops-hub-section-actions--empty";
        internal const string ShellNotes = "liveops-hub-shell-notes";

        // Rail
        internal const string RailScroll = "liveops-hub-rail-scroll";
        internal const string RailStageLabel = "liveops-hub-rail-stage-label";
        internal const string RailRowLabel = "liveops-hub-rail-row-label";
        internal const string RailRowMark = "liveops-hub-rail-row-mark";
        internal const string RailRowMarkHidden = "liveops-hub-rail-row-mark--hidden";
        internal const string GutterLineTop = "liveops-hub-gutter-line--top";
        internal const string GutterLineBottom = "liveops-hub-gutter-line--bottom";
        internal const string GutterLineHidden = "liveops-hub-gutter-line--hidden";
        internal const string RailBlockerHost = "liveops-hub-rail-blocker-host";
        internal const string RailBlockerHead = "liveops-hub-rail-blocker-head";
        internal const string RailBlockerTitle = "liveops-hub-rail-blocker-title";
        internal const string RailBlockerDetail = "liveops-hub-rail-blocker-detail";

        // Status bar
        internal const string StatusMark = "liveops-hub-status-mark";
        internal const string StatusText = "liveops-hub-status-text";

        // Note đầu thân (đang biên dịch)
        internal const string NoteText = "liveops-hub-note-text";

        // Card lỗi (Hình 28 khung 1–2)
        internal const string FailureWindow = "liveops-hub-failure--window";
        internal const string FailureHead = "liveops-hub-failure-head";
        internal const string FailureTitle = "liveops-hub-failure-title";
        internal const string FailureBody = "liveops-hub-failure-body";
        internal const string FailureActions = "liveops-hub-failure-actions";
        internal const string FailureFootnote = "liveops-hub-failure-footnote";

        // Trống
        internal const string EmptyTitle = "liveops-hub-empty-title";
        internal const string EmptyBody = "liveops-hub-empty-body";
    }
}
