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
        private readonly LiveOpsStateMark _draftMark;
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
            // Dấu dựng bằng C# rồi chèn lên đầu chip — cùng cách LiveOpsHubStatusBar làm, để UXML khung không phụ thuộc thẻ
            // control dual-path (chỗ 8 [FD §2.14]).
            _draftMark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            _draftMark.AddToClassList(LiveOpsHubClassNames.ChipMark);
            DraftChip.Insert(0, _draftMark);
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

        /// <summary>Dấu trạng thái đầu chip nháp; chỉ hiện ở dạng (c) "Chưa có dấu đã đăng" ([FD §3.3]).</summary>
        internal LiveOpsStateMark DraftMark => _draftMark;

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
            // Màu link đặt trên NHÃN CHỮ, không đặt trên chip: class ở container thừa kế xuống cả "Lịch" (khoá chip), mà [FD §3.3]
            // tả khoá là chữ mờ 10px chứ không phải chữ link (L-3).
            _assetChipText.EnableInClassList(LiveOpsHubClassNames.ChipClickable, hasAsset);

            // Dạng (c) mang vòng RỖNG trước chữ ([FD §3.3]); mọi dạng khác không có bằng chứng nào để đeo dấu.
            bool isNeverPublished = model.DraftChipForm == LiveOpsHubHeaderChipModel.FormNeverPublished;
            _draftMark.EnableInClassList(LiveOpsHubClassNames.ChipHidden, !isNeverPublished);
            if (isNeverPublished) _draftMark.SetHealth(HealthState.NotMeasured);

            _draftLeft.text = model.DraftLeftText;
            _draftRight.text = model.DraftRightText;
            bool hasRight = model.DraftRightText.Length > 0;
            _draftDivider.EnableInClassList(LiveOpsHubClassNames.ChipHidden, !hasRight);
            _draftRight.EnableInClassList(LiveOpsHubClassNames.ChipHidden, !hasRight);
            // Chỉ dạng (a) có nửa trái bấm được (Lưu); mọi dạng khác chip chỉ là chữ nên không mời bấm.
            bool isUnsaved = model.DraftChipForm == LiveOpsHubHeaderChipModel.FormUnsaved;
            _draftLeft.EnableInClassList(LiveOpsHubClassNames.ChipClickable, isUnsaved);
            // Chỉ dạng (a) in đậm ([FD §3.3]) — chip duy nhất mời làm một việc.
            _draftLeft.EnableInClassList(LiveOpsHubClassNames.ChipTextStrong, isUnsaved);
            _draftRight.EnableInClassList(LiveOpsHubClassNames.ChipClickable, hasRight);
            _draftLeft.tooltip = model.DraftChipForm == LiveOpsHubHeaderChipModel.FormUnsaved
                ? LiveOpsHubStrings.ShellChipUnsavedTooltip
                : string.Empty;
            _draftRight.tooltip = hasRight ? LiveOpsHubStrings.ShellChipPublishedDiffTooltip : string.Empty;
        }
    }
}
