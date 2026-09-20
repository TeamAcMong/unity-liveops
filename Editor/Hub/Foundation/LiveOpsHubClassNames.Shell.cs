namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class thành phần của vùng Shell (V-5, G-SHELL → G-HOSTUI → G-SHELLPOLISH) không có trong bảng 8.10: bố cục cột nội dung,
    /// nửa đường nối của gutter rail, các phần của ô chặn, card lỗi và note đầu thân. Tách file vùng vì file gốc do G-SKELETON
    /// đóng từ W0; thiếu hằng thì <c>check-class-names.py</c> chặn class gõ tay trong USS/UXML/C#.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        /// <summary>
        /// Bậc cửa sổ hẹp THỨ TƯ, dưới 1000px (W9-20). Vì sao thêm: bậc <c>--medium</c> trải từ 900 tới 1099 nên mọi thứ nhường
        /// ở đó nhường CHO CẢ dải; thanh công cụ màn Lịch ở 950 còn chật thêm một control nữa mà không có bậc nào nói được
        /// điều đó. Khai ở file vùng Shell chứ không ở bảng 8.10 vì bảng ấy do G-SKELETON đóng từ W0 (V-5) — cùng chỗ với
        /// <see cref="RailPinnedOpen"/>, cũng là một class trên root.
        /// </summary>
        internal const string Snug = "liveops-hub--snug";

        // Bố cục khung — "liveops-hub-content" là tên measure-capture.py đo bề rộng cột nội dung 1084.
        internal const string Main = "liveops-hub-main";
        internal const string Content = "liveops-hub-content";
        internal const string HeaderTitle = "liveops-hub-header-title";
        internal const string SectionHeading = "liveops-hub-section-heading";
        internal const string SectionActionsEmpty = "liveops-hub-section-actions--empty";
        internal const string ShellNotes = "liveops-hub-shell-notes";

        // Header của G-HOSTUI (W4): ô "Đi tới màn…", khoảng giãn, phần chữ của chip.
        internal const string HeaderSpacer = "liveops-hub-header-spacer";
        internal const string GotoLabel = "liveops-hub-goto-label";
        internal const string GotoKey = "liveops-hub-goto-key";
        internal const string ChipText = "liveops-hub-chip-text";
        internal const string ChipHidden = "liveops-hub-chip--hidden";
        internal const string ChipClickable = "liveops-hub-chip--clickable";

        /// <summary>Dấu trạng thái 7 px đầu chip nháp — dạng (c) của [FD §3.3] là một vòng RỖNG trước chữ "Chưa có dấu đã đăng".</summary>
        internal const string ChipMark = "liveops-hub-chip-mark";

        /// <summary>Nửa trái chip nháp dạng (a) in đậm ([FD §3.3]) — chip duy nhất mời làm một việc.</summary>
        internal const string ChipTextStrong = "liveops-hub-chip-text--strong";

        // Chỗ cắm outcome ở cuối cột nội dung (8.1 bước 7).
        internal const string OutcomeHost = "liveops-hub-outcome-host";

        // Băng "tệp đã đổi trên đĩa" (4.3, SPIKE-B SP-8b) — dựng trong hub-shell-notes như note đang biên dịch.
        internal const string DiskBanner = "liveops-hub-disk-banner";
        internal const string DiskBannerHead = "liveops-hub-disk-banner-head";
        internal const string DiskBannerText = "liveops-hub-disk-banner-text";
        internal const string DiskBannerActions = "liveops-hub-disk-banner-actions";

        // Menu chọn ngôn ngữ ở góc phải header (G-I18N).
        internal const string LanguageMenu = "liveops-hub-language-menu";

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

        // Rail 36 px của cửa sổ hẹp ([FD §3.7], mục 12 I-8) — một ô icon cho MỖI TẦNG, dấu 7px góc phải trên, ô icon lỗi ở đáy.
        internal const string RailPin = "liveops-hub-rail-pin";
        internal const string RailNarrow = "liveops-hub-rail-narrow";
        internal const string RailNarrowCell = "liveops-hub-rail-narrow-cell";
        internal const string RailNarrowCellActive = "liveops-hub-rail-narrow-cell--active";
        internal const string RailNarrowCellBlocker = "liveops-hub-rail-narrow-cell--blocker";
        internal const string RailNarrowMark = "liveops-hub-rail-narrow-mark";

        /// <summary>Người dùng ghim rail mở ở cửa sổ hẹp (EditorPrefs) — class trên root vì nó thắng cả <c>--narrow</c>.</summary>
        internal const string RailPinnedOpen = "liveops-hub--rail-pinned-open";
        internal const string RailBlockerHead = "liveops-hub-rail-blocker-head";
        internal const string RailBlockerTitle = "liveops-hub-rail-blocker-title";
        internal const string RailBlockerDetail = "liveops-hub-rail-blocker-detail";

        // Status bar
        internal const string StatusMark = "liveops-hub-status-mark";
        internal const string StatusMarkHidden = "liveops-hub-status-mark--hidden";
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
