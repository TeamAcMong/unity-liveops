using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// View mỏng của section header 36 px ([FD §3.6]): tiêu đề 12 px đậm, subtitle bắt buộc (cắt "…" kèm tooltip đủ câu), nút
    /// của màn bên phải. Element lấy từ <c>LiveOpsHub.uxml</c> theo tên — lớp này không dựng bố cục, chỉ đổ chữ và nút.
    /// </summary>
    internal sealed class LiveOpsHubSectionHeader
    {
        public LiveOpsHubSectionHeader(VisualElement hubRoot)
        {
            if (hubRoot == null) throw new ArgumentNullException(nameof(hubRoot));
            Element = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.SectionHeader);
            TitleLabel = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.SectionTitle);
            SubtitleLabel = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.SectionSubtitle);
            ActionsContainer = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.SectionActions);
        }

        public VisualElement Element { get; }
        public Label TitleLabel { get; }
        public Label SubtitleLabel { get; }
        public VisualElement ActionsContainer { get; }

        /// <summary>
        /// Đổ tiêu đề + subtitle rồi xin nút từ màn. Màn ném khi thêm nút thì exception đi tiếp lên cửa sổ (cửa sổ coi như màn ném
        /// khi dựng) — header vẫn giữ tiêu đề thật và container nút đã rỗng, không để nửa bộ nút của màn lỗi.
        /// </summary>
        public void Show(IHubSection section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            TitleLabel.text = section.Title;
            SubtitleLabel.text = section.Subtitle;
            SubtitleLabel.tooltip = section.Subtitle;
            ActionsContainer.Clear();
            try
            {
                if (section is IHubSectionActions actions) actions.PopulateHeaderActions(ActionsContainer);
            }
            catch
            {
                ActionsContainer.Clear();
                throw;
            }
            finally
            {
                UpdateActionsVisibility();
            }
        }

        /// <summary>Ẩn container nút khi rỗng: container trống vẫn chiếm margin làm subtitle bị cắt sớm.</summary>
        public void UpdateActionsVisibility()
        {
            ActionsContainer.EnableInClassList(LiveOpsHubClassNames.SectionActionsEmpty, ActionsContainer.childCount == 0);
        }
    }
}
