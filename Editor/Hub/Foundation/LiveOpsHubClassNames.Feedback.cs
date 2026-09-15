namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class thành phần của vùng Feedback (V-5, G-FEEDBACK) không có trong bảng 8.10: phần con của toast, outcome, palette, hover
    /// card, popover và hộp xác nhận. Ẩn/hiện bằng class <c>--hidden</c> riêng của từng phần vì [FD §2.14] không cho C# gán
    /// <c>style.display</c>; class khối (<c>liveops-hub-toast</c>, <c>-palette</c>, <c>-hover-card</c>…) đã có ở file gốc.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Toast — con cuối của cột nội dung, left 8 bottom 8 ([FD §3.9]).
        internal const string ToastHidden = "liveops-hub-toast--hidden";
        internal const string ToastIcon = "liveops-hub-toast-icon";
        internal const string ToastMessage = "liveops-hub-toast-message";
        internal const string ToastAction = "liveops-hub-toast-action";
        internal const string ToastActionHidden = "liveops-hub-toast-action--hidden";
        internal const string ToastActionButton = "liveops-hub-toast-action-button";
        internal const string ToastClose = "liveops-hub-toast-close";

        // Outcome — kết quả xuất/đăng ở lại tới khi làm việc khác.
        internal const string Outcome = "liveops-hub-outcome";
        internal const string OutcomeHidden = "liveops-hub-outcome--hidden";
        internal const string OutcomeBlocked = "liveops-hub-outcome--blocked";
        internal const string OutcomeText = "liveops-hub-outcome-text";
        internal const string OutcomeHeadline = "liveops-hub-outcome-headline";
        internal const string OutcomeDetail = "liveops-hub-outcome-detail";
        internal const string OutcomeFootnote = "liveops-hub-outcome-footnote";
        internal const string OutcomeAction = "liveops-hub-outcome-action";
        internal const string OutcomeActionHidden = "liveops-hub-outcome-action--hidden";

        // Palette ⌘K + scrim — con cuối của root, BringToFront khi mở ([FD §3.8]).
        internal const string ScrimHidden = "liveops-hub-scrim--hidden";
        internal const string PaletteHidden = "liveops-hub-palette--hidden";
        internal const string PaletteVisible = "liveops-hub-palette--visible";
        internal const string PaletteField = "liveops-hub-palette-field";
        internal const string PaletteList = "liveops-hub-palette-list";
        internal const string PaletteRowMark = "liveops-hub-palette-row-mark";
        internal const string PaletteRowTitle = "liveops-hub-palette-row-title";
        internal const string PaletteRowStage = "liveops-hub-palette-row-stage";
        internal const string PaletteEmpty = "liveops-hub-palette-empty";
        internal const string PaletteEmptyHidden = "liveops-hub-palette-empty--hidden";
        internal const string PaletteFooter = "liveops-hub-palette-footer";

        // Hover card — lớp nổi, vị trí left/top là chỗ 6 của [FD §2.14].
        internal const string HoverCardVisible = "liveops-hub-hover-card--visible";
        internal const string HoverCardPinned = "liveops-hub-hover-card--pinned";

        // Popover — gốc nội dung trong cửa sổ PopupWindow riêng (320×N).
        internal const string Popover = "liveops-hub-popover";
        internal const string PopoverClose = "liveops-hub-popover-close";

        // Hộp xác nhận cấp 1 / cấp 2 ([FD §3.10]) — sheet riêng LiveOpsConfirmWindow.uss.
        internal const string Confirm = "liveops-hub-confirm";
        internal const string ConfirmLevel2 = "liveops-hub-confirm--level2";
        internal const string ConfirmTitle = "liveops-hub-confirm-title";
        internal const string ConfirmBody = "liveops-hub-confirm-body";
        internal const string ConfirmBodyHidden = "liveops-hub-confirm-body--hidden";
        internal const string ConfirmWarning = "liveops-hub-confirm-warning";
        internal const string ConfirmWarningHidden = "liveops-hub-confirm-warning--hidden";
        internal const string ConfirmType = "liveops-hub-confirm-type";
        internal const string ConfirmTypeHidden = "liveops-hub-confirm-type--hidden";
        internal const string ConfirmTypeRow = "liveops-hub-confirm-type-row";
        internal const string ConfirmTypeId = "liveops-hub-confirm-type-id";
        internal const string ConfirmTypeField = "liveops-hub-confirm-type-field";
        internal const string ConfirmTypeHint = "liveops-hub-confirm-type-hint";
        internal const string ConfirmButtons = "liveops-hub-confirm-buttons";
        internal const string ConfirmKeyHint = "liveops-hub-confirm-key-hint";
        internal const string ConfirmMissingLayout = "liveops-hub-confirm-missing-layout";
    }
}
