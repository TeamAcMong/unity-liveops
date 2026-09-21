using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phần vùng Shell của <see cref="LiveOpsHubPaths"/> (V-5): tên element viết tay trong <c>LiveOpsHub.uxml</c> và thứ tự nạp
    /// stylesheet vào root clone. Đổi cùng nhịp với UXML (G-SHELL → G-HOSTUI → G-SHELLPOLISH). Cửa sổ, probe CLI và test dùng
    /// CHUNG danh sách này — sửa UXML mà quên đây thì lỗi lộ ở cổng, không thành cửa sổ trắng.
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        /// <summary>Tên element của khung (thuộc tính <c>name</c> trong UXML) — khác tên class để Q theo tên không trúng phần tử khác cùng class.</summary>
        internal static class ShellElementNames
        {
            internal const string Root = "hub-root";
            internal const string Header = "hub-header";
            internal const string HeaderTitle = "hub-header-title";

            // Header của G-HOSTUI (W4): ô "Đi tới màn…" + hai chip.
            internal const string HeaderGoTo = "hub-header-goto";
            internal const string HeaderGoToLabel = "hub-header-goto-label";
            internal const string HeaderGoToKey = "hub-header-goto-key";
            internal const string HeaderSpacer = "hub-header-spacer";
            internal const string HeaderAssetChip = "hub-header-asset-chip";
            internal const string HeaderAssetChipKey = "hub-header-asset-chip-key";
            internal const string HeaderAssetChipText = "hub-header-asset-chip-text";
            internal const string HeaderDraftChip = "hub-header-draft-chip";
            internal const string HeaderDraftChipLeft = "hub-header-draft-chip-left";
            internal const string HeaderDraftChipDivider = "hub-header-draft-chip-divider";
            internal const string HeaderDraftChipRight = "hub-header-draft-chip-right";

            internal const string Main = "hub-main";
            internal const string Rail = "hub-rail";
            internal const string RailCaption = "hub-rail-caption";
            internal const string RailScroll = "hub-rail-scroll";
            internal const string RailBlockerHost = "hub-rail-blocker-host";
            internal const string Content = "hub-content";
            internal const string SectionHeader = "hub-section-header";
            internal const string SectionTitle = "hub-section-title";
            internal const string SectionSubtitle = "hub-section-subtitle";
            internal const string SectionActions = "hub-section-actions";
            internal const string ShellNotes = "hub-shell-notes";
            internal const string SectionBody = "hub-section-body";

            /// <summary>Chỗ cắm view outcome (8.1 bước 7) — con cuối cột nội dung, dưới thân màn.</summary>
            internal const string OutcomeHost = "hub-outcome-host";

            internal const string StatusBar = "hub-status";
            internal const string StatusLeft = "hub-status-left";
            internal const string StatusLeftText = "hub-status-left-text";
            internal const string StatusRight = "hub-status-right";
        }

        /// <summary>Element sống còn của khung — thiếu một cái thì cửa sổ hiện màn lỗi thay vì dựng nửa vời (8.1 bước 3).</summary>
        internal static readonly IReadOnlyList<string> RequiredShellElementNames = Array.AsReadOnly(new[]
        {
            ShellElementNames.Root, ShellElementNames.Header, ShellElementNames.HeaderTitle, ShellElementNames.Main, ShellElementNames.Rail,
            ShellElementNames.RailCaption, ShellElementNames.RailScroll, ShellElementNames.RailBlockerHost, ShellElementNames.Content,
            ShellElementNames.SectionHeader, ShellElementNames.SectionTitle, ShellElementNames.SectionSubtitle, ShellElementNames.SectionActions,
            ShellElementNames.ShellNotes, ShellElementNames.SectionBody, ShellElementNames.OutcomeHost, ShellElementNames.StatusBar,
            ShellElementNames.StatusLeft, ShellElementNames.StatusLeftText, ShellElementNames.StatusRight,
            ShellElementNames.HeaderGoTo, ShellElementNames.HeaderGoToLabel, ShellElementNames.HeaderGoToKey, ShellElementNames.HeaderSpacer,
            ShellElementNames.HeaderAssetChip, ShellElementNames.HeaderAssetChipKey, ShellElementNames.HeaderAssetChipText,
            ShellElementNames.HeaderDraftChip, ShellElementNames.HeaderDraftChipLeft, ShellElementNames.HeaderDraftChipDivider,
            ShellElementNames.HeaderDraftChipRight,
        });

        /// <summary>
        /// Thứ tự nạp vào root clone (mục 2.3): theme → components → shell → feedback → controls → timeline → motion. Motion luôn
        /// cuối vì biến thể <c>--no-motion</c> thắng nhờ thứ tự khi cùng specificity ([FD §2.13]).
        /// </summary>
        internal static readonly IReadOnlyList<ShellStyleSheet> ShellStyleSheetLoadOrder = Array.AsReadOnly(new[]
        {
            new ShellStyleSheet(ThemeUss, true),
            new ShellStyleSheet(ComponentsUss, true),
            new ShellStyleSheet(ShellUss, true),
            // Ba sheet dưới do gói khác tạo (feedback G-FEEDBACK W2, controls G-CONTROLS W2, timeline G-TIMELINE-VIEW W3). Từ W5
            // cả ba đã có trên đĩa nên chúng BẮT BUỘC như sheet khung: thiếu một cái là bản cài hỏng (mất toast, mất tab, mất
            // timeline) — cửa sổ LogWarning đúng đường dẫn (8.1 bước 2) và probe CLI báo lỗi thay vì chỉ ghi chú.
            new ShellStyleSheet(FeedbackUss, true),
            new ShellStyleSheet(ControlsUss, true),
            new ShellStyleSheet(TimelineUss, true),
            new ShellStyleSheet(MotionUss, true),
        });

        /// <summary>
        /// Một stylesheet của khung. <see cref="IsRequired"/> = thiếu trên đĩa là package hỏng: cửa sổ <c>LogWarning</c> nêu đường
        /// dẫn, probe CLI báo lỗi. Từ W5 mọi sheet của thứ tự nạp đều bắt buộc; cờ giữ lại để bản dev sau này thêm sheet mới của
        /// một gói chưa tới lượt vẫn dựng được cửa sổ mà không cảnh báo giả.
        /// </summary>
        internal sealed class ShellStyleSheet
        {
            public ShellStyleSheet(string path, bool isRequired)
            {
                Path = path;
                IsRequired = isRequired;
            }

            public string Path { get; }
            public bool IsRequired { get; }
        }
    }
}
