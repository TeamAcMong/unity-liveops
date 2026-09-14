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
            ShellElementNames.ShellNotes, ShellElementNames.SectionBody, ShellElementNames.StatusBar, ShellElementNames.StatusLeft,
            ShellElementNames.StatusLeftText, ShellElementNames.StatusRight,
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
            // INTERIM(G-SHELLPOLISH): ba sheet của gói khác (feedback G-FEEDBACK W2, controls G-CONTROLS W2, timeline G-TIMELINE-VIEW
            // W3) chưa bắt buộc — bản dev dựng dở chưa có file nên cửa sổ không cảnh báo (mọi test UI assert không log lạ), probe chỉ
            // ghi chú. G-SHELLPOLISH (W5, khi cả ba đã có) đổi ba cờ thành true: bản cài thiếu sheet nào cũng LogWarning đúng đường
            // dẫn (8.1 bước 2) và probe báo lỗi.
            new ShellStyleSheet(FeedbackUss, false),
            new ShellStyleSheet(ControlsUss, false),
            new ShellStyleSheet(TimelineUss, false),
            new ShellStyleSheet(MotionUss, true),
        });

        /// <summary>
        /// Một stylesheet của khung. <see cref="IsRequired"/> = thiếu trên đĩa là package hỏng: cửa sổ <c>LogWarning</c> nêu đường
        /// dẫn, probe CLI báo lỗi. false chỉ cho sheet chưa tới lượt gói tạo ở bản dev (nhánh tạm có dấu INTERIM ở trên).
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
