using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Màn chưa dựng: tiêu đề và subtitle thật trên section header, health NotMeasured có lý do (rail đủ 6 màn, mỗi màn vòng
    /// rỗng kèm câu nói vì sao — không bao giờ trông như "ổn"), thân là trạng thái trống ([FD §4]): dòng đầu nói vì sao trống,
    /// dòng sau là subtitle thật của màn.
    /// </summary>
    // INTERIM(G-SHELLPOLISH): lớp gốc của 6 màn giữ chỗ ở bản dev (mục 12 I-2) — mỗi gói màn W4 thay lớp gốc bằng màn thật,
    // G-SHELLPOLISH (W5) xoá file này; màn nào còn kế thừa thì compile đỏ, không lọt thành màn trống âm thầm.
    internal abstract class InterimPlaceholderSection : IHubSection, IHubHostAware, IHubSectionActions, IHubSectionViewState
    {
        /// <summary>Tên element viết tay của thân giữ chỗ — probe và test Q theo đúng các tên này.</summary>
        internal const string BodyElementName = "placeholder-body";
        internal const string TitleElementName = "placeholder-title";
        internal const string ReasonElementName = "placeholder-reason";

        private static readonly IReadOnlyList<string> PlaceholderElementNames = Array.AsReadOnly(new[] { BodyElementName, TitleElementName, ReasonElementName });

        protected InterimPlaceholderSection(string id, string title, string subtitle, PipelineStage stage)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle));
            Stage = stage;
        }

        public string Id { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public PipelineStage Stage { get; }
        public IReadOnlyList<string> RequiredElementNames => PlaceholderElementNames;

        /// <summary>Host đã Bind — màn thật dùng để đọc services; màn giữ chỗ chỉ giữ lại để hợp đồng Bind trước khi hiện kiểm được.</summary>
        internal IHubHost Host { get; private set; }

        public SectionHealth GetHealth()
        {
            return SectionHealth.NotMeasured(LiveOpsHubStrings.InterimPlaceholderReason);
        }

        public VisualElement CreateView()
        {
            VisualElement body = new VisualElement { name = BodyElementName };
            body.AddToClassList(LiveOpsHubClassNames.Empty);

            Label title = new Label(LiveOpsHubStrings.InterimPlaceholderReason) { name = TitleElementName };
            title.AddToClassList(LiveOpsHubClassNames.EmptyTitle);
            body.Add(title);

            // Dòng dưới là subtitle thật của màn (mục 12 I-2: thân rỗng có tiêu đề + subtitle thật) — nói màn này sẽ làm gì mà không
            // thêm câu tạm nào ngoài sổ INTERIM (hằng tạm duy nhất là InterimPlaceholderReason, G-SHELLPOLISH gỡ theo sổ).
            Label reason = new Label(Subtitle) { name = ReasonElementName };
            reason.AddToClassList(LiveOpsHubClassNames.EmptyBody);
            body.Add(reason);
            return body;
        }

        public void OnShown()
        {
        }

        public void Bind(IHubHost host)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>Không có nút: nút của màn thật (vd "Thêm đợt") chưa có hành động để làm — không vẽ nút disabled trỏ tới thứ chưa có.</summary>
        public void PopulateHeaderActions(VisualElement container)
        {
        }

        public string CaptureViewState()
        {
            return string.Empty;
        }

        public void RestoreViewState(string viewStateJson)
        {
        }
    }
}
