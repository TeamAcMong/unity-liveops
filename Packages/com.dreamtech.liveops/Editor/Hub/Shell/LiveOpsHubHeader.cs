using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// View header 26 px ([FD §3.3]). G-SHELL chỉ đổ tiêu đề "LiveOps Hub" — ô "Đi tới màn…" và chip asset/nháp cần palette và
    /// phiên lịch nên do G-HOSTUI (W4) thêm vào đúng view này; header không tự bịa chip khi chưa có dữ liệu để nói.
    /// G-I18N thêm menu chọn ngôn ngữ ở mép phải (D-L2).
    /// </summary>
    internal sealed class LiveOpsHubHeader
    {
        public LiveOpsHubHeader(VisualElement hubRoot)
        {
            if (hubRoot == null) throw new ArgumentNullException(nameof(hubRoot));
            Element = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.Header);
            TitleLabel = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.HeaderTitle);
            TitleLabel.text = LiveOpsHubStrings.ShellWindowTitle;
            LanguageMenu = new LiveOpsHubLanguageMenu(Element);
        }

        public VisualElement Element { get; }
        public Label TitleLabel { get; }
        public LiveOpsHubLanguageMenu LanguageMenu { get; }
    }
}
