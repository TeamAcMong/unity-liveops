using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// View mỏng của card "Đường đi của lịch" [SD1 §1.1]: 4 nút tầng P1 chia đều trong thân 48px, mỗi nút = dấu trạng thái
    /// lớn + tên tầng + ghi chú 10px; giữa hai nút là đường nối 2px. Đường nối sau tầng cổng chặn ĐẦU TIÊN "chết" — người
    /// đọc thấy lịch dừng ở đâu mà không phải đọc chữ. Tooltip nêu lý do của tầng (health đã mang lý do, 6.4).
    /// </summary>
    internal sealed class OverviewPipelineFlow
    {
        internal const string NodeElementNamePrefix = "overview-flow-node-";
        internal const string ConnectorElementNamePrefix = "overview-flow-connector-";

        private readonly VisualElement _container;

        internal OverviewPipelineFlow(VisualElement container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        internal void Rebuild(IReadOnlyList<OverviewFlowNode> nodes)
        {
            _container.Clear();
            if (nodes == null) return;
            foreach (OverviewFlowNode node in nodes)
            {
                _container.Add(BuildNode(node));
                if (node.HasConnector) _container.Add(BuildConnector(node));
            }
        }

        private static VisualElement BuildNode(OverviewFlowNode node)
        {
            VisualElement element = new VisualElement
            {
                name = NodeElementNamePrefix + node.Stage.ToString().ToLowerInvariant(),
                tooltip = node.Tooltip,
            };
            element.AddToClassList(LiveOpsHubClassNames.OverviewFlowNode);

            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.Size = LiveOpsStateMark.MarkSize.Large;
            mark.SetHealth(node.State);
            element.Add(mark);

            Label caption = new Label(node.Caption);
            caption.AddToClassList(LiveOpsHubClassNames.OverviewFlowCaption);
            element.Add(caption);

            Label note = new Label(node.Note);
            note.AddToClassList(LiveOpsHubClassNames.OverviewFlowNote);
            LiveOpsHubStyle.SetStateText(note, node.State);
            element.Add(note);
            return element;
        }

        private static VisualElement BuildConnector(OverviewFlowNode node)
        {
            VisualElement connector = new VisualElement
            {
                name = ConnectorElementNamePrefix + node.Stage.ToString().ToLowerInvariant(),
            };
            connector.AddToClassList(LiveOpsHubClassNames.OverviewFlowConnector);
            connector.EnableInClassList(LiveOpsHubClassNames.OverviewFlowConnectorDead, node.IsConnectorDead);
            return connector;
        }
    }
}
