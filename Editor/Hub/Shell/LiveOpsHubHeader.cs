using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// View header 26 px ([FD §3.3]): tiêu đề · ô "Đi tới màn…" + nhãn phím · giãn · chip asset · chip nháp. View mỏng — chữ của
    /// hai chip do <see cref="LiveOpsHubHeaderChipModel"/> dựng, header chỉ đổ và ẩn phần rỗng.
    /// <para>
    /// Ô "Đi tới màn…" là một <c>VisualElement</c> chứ không phải <c>Button</c>: nó trông như ô nhập (chữ mờ + nhãn phím) nhưng
    /// không nhận chữ — gõ vào đó là gõ vào palette. Dùng Button sẽ mang nền/viền nút của Editor và mời người dùng gõ vào một ô
    /// không nhận gì.
    /// </para>
    /// G-I18N thêm menu chọn ngôn ngữ ở mép phải (D-L2).
    /// </summary>
    internal sealed class LiveOpsHubHeader
    {
        private readonly Label _goToKey;
        private readonly Label _assetChipKey;
        private readonly Label _assetChipText;
        private readonly Label _draftLeft;
        private readonly VisualElement _draftDivider;
        private readonly Label _draftRight;

        public LiveOpsHubHeader(VisualElement hubRoot)
        {
            if (hubRoot == null) throw new ArgumentNullException(nameof(hubRoot));
            Element = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.Header);
            TitleLabel = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.HeaderTitle);
            TitleLabel.text = LiveOpsHubStrings.ShellWindowTitle;

            GoTo = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.HeaderGoTo);
            Label goToLabel = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.HeaderGoToLabel);
            goToLabel.text = LiveOpsHubStrings.ShellGoToPlaceholder;
            GoTo.tooltip = LiveOpsHubStrings.ShellGoToTooltip;
            _goToKey = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.HeaderGoToKey);

            AssetChip = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.HeaderAssetChip);
            _assetChipKey = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.HeaderAssetChipKey);
            _assetChipText = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.HeaderAssetChipText);

            DraftChip = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.HeaderDraftChip);
            _draftLeft = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.HeaderDraftChipLeft);
            _draftDivider = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.HeaderDraftChipDivider);
            _draftRight = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.HeaderDraftChipRight);

            LanguageMenu = new LiveOpsHubLanguageMenu(Element);
        }

        public VisualElement Element { get; }
        public Label TitleLabel { get; }
        public VisualElement GoTo { get; }
        public VisualElement AssetChip { get; }
        public VisualElement DraftChip { get; }
        public LiveOpsHubLanguageMenu LanguageMenu { get; }

        /// <summary>Phần trái của chip nháp — nơi nhận bấm "Lưu"; test đọc chữ từ đây.</summary>
        internal Label DraftLeftLabel => _draftLeft;

        /// <summary>Phần phải của chip nháp — nơi nhận bấm "sang Xuất JSON".</summary>
        internal Label DraftRightLabel => _draftRight;

        internal Label AssetChipTextLabel => _assetChipText;

        internal Label GoToKeyLabel => _goToKey;

        /// <summary>Nhãn phím ⌘K cạnh ô "Đi tới màn…"; "" = người dùng gỡ phím → chỉ còn chữ, không có nhãn cụt.</summary>
        public void SetGoToKeyLabel(string keyLabel)
        {
            _goToKey.text = keyLabel ?? string.Empty;
        }

        /// <summary>Đổ cả hai chip từ model; phần rỗng ẩn bằng class (không setter visible — [FD §2.14]).</summary>
        public void SetChips(LiveOpsHubHeaderChipModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            bool hasAsset = model.DraftChipForm != LiveOpsHubHeaderChipModel.FormNoAsset;
            _assetChipKey.text = LiveOpsHubStrings.ShellChipCalendarKey;
            _assetChipKey.EnableInClassList(LiveOpsHubClassNames.ChipHidden, !hasAsset);
            _assetChipText.text = model.AssetChipText;
            AssetChip.tooltip = hasAsset
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellChipAssetTooltipFormat, model.AssetChipTooltip)
                : LiveOpsHubStrings.ShellChipNoCalendarTooltip;
            AssetChip.EnableInClassList(LiveOpsHubClassNames.ChipClickable, hasAsset);

            _draftLeft.text = model.DraftLeftText;
            _draftRight.text = model.DraftRightText;
            bool hasRight = model.DraftRightText.Length > 0;
            _draftDivider.EnableInClassList(LiveOpsHubClassNames.ChipHidden, !hasRight);
            _draftRight.EnableInClassList(LiveOpsHubClassNames.ChipHidden, !hasRight);
            // Chỉ dạng (a) có nửa trái bấm được (Lưu); mọi dạng khác chip chỉ là chữ nên không mời bấm.
            _draftLeft.EnableInClassList(LiveOpsHubClassNames.ChipClickable, model.DraftChipForm == LiveOpsHubHeaderChipModel.FormUnsaved);
            _draftRight.EnableInClassList(LiveOpsHubClassNames.ChipClickable, hasRight);
            _draftLeft.tooltip = model.DraftChipForm == LiveOpsHubHeaderChipModel.FormUnsaved
                ? LiveOpsHubStrings.ShellChipUnsavedTooltip
                : string.Empty;
            _draftRight.tooltip = hasRight ? LiveOpsHubStrings.ShellChipPublishedDiffTooltip : string.Empty;
        }
    }
}
