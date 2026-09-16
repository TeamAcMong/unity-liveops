using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bốn tile metric của màn Xuất JSON [SD2 §3.5]: KÍCH THƯỚC (byte + số dòng) · ĐỊNH DẠNG (1 hoặc 2, đổi bằng
    /// ToolbarMenu → trạng thái (j)) · SHA (6 ký tự mono, tooltip đủ 64) · SO VỚI (giờ dấu + chỉ loại thay đổi khác 0).
    /// <para>
    /// Tile dựng lại mỗi lần <see cref="Bind"/> thay vì giữ tham chiếu từng Label: bốn tile là bốn hình khác nhau (tile SHA
    /// không có footer, tile ĐỊNH DẠNG có menu) nên giữ tham chiếu sẽ thành bốn nhánh if trong mỗi lần cập nhật.
    /// </para>
    /// </summary>
    internal sealed class ExportMetrics : VisualElement
    {
        internal const string ElementName = "export-metrics";
        internal const string FormatMenuElementName = "export-metric-format-menu";

        private const string FootSeparator = " · ";

        private readonly LiveOpsHubFormat _format;

        public ExportMetrics(LiveOpsHubFormat format)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format), LiveOpsHubStrings.ExportGateErrorFormatMissing);
            name = ElementName;
            AddToClassList(LiveOpsHubClassNames.ExportMetricRow);
        }

        /// <summary>Người dùng chọn định dạng khác trong ToolbarMenu của tile ĐỊNH DẠNG.</summary>
        public event Action<LiveEventCalendarJsonFormat> FormatSelected;

        /// <param name="compareStamp">Dấu đang là bản so; null = chưa đăng lần nào (tile SO VỚI in "chưa đăng").</param>
        public void Bind(LiveEventCalendarJsonText json, LiveEventCalendarDiffResult diff, PublishedCalendarStamp compareStamp)
        {
            Clear();
            if (json == null) return;

            Add(BuildMetric(LiveOpsHubStrings.ExportMetricSizeCaption, _format.Integer(json.ByteCount), LiveOpsHubStrings.ExportMetricSizeUnit,
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportMetricSizeFootFormat, _format.Integer(json.LineCount)),
                string.Empty, false));

            VisualElement formatTile = BuildMetric(LiveOpsHubStrings.ExportMetricFormatCaption, FormatValueText(json.Format),
                FormatUnitText(json.Format), string.Empty, string.Empty, false);
            formatTile.Add(BuildFormatMenu(json.Format));
            Add(formatTile);

            Add(BuildMetric(LiveOpsHubStrings.ExportMetricShaCaption, json.ShortSha, string.Empty, string.Empty, json.Sha256Hex, true));

            // Chưa đăng lần nào (g): KHÔNG đếm thay đổi. Diff lúc đó so với tài liệu rỗng nên mọi mục thành "Thêm", và tile
            // sẽ nói "Thêm 8" ngay cạnh card diff vừa nói "chưa có dấu đã đăng nào" — hai câu chọi nhau trên cùng một màn.
            string compareValue = compareStamp == null ? LiveOpsHubStrings.ExportMetricCompareNone : StampTimeText(compareStamp);
            VisualElement compareTile = BuildMetric(LiveOpsHubStrings.ExportMetricCompareCaption, compareValue, string.Empty,
                CompareFootText(compareStamp == null ? null : diff), string.Empty, false);
            compareTile.AddToClassList(LiveOpsHubClassNames.ExportMetricLast);
            Add(compareTile);
        }

        /// <summary>Footer tile SO VỚI: CHỈ loại thay đổi khác 0 ("Thêm 1 · Đổi 4") — số 0 ở đây là nhiễu, chip trên card diff mới đếm đủ.</summary>
        internal string CompareFootText(LiveEventCalendarDiffResult diff)
        {
            if (diff == null || diff.ChangeCount == 0) return LiveOpsHubStrings.ExportMetricCompareFootEmpty;
            List<string> parts = new List<string>();
            if (diff.AddedCount > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportDiffChipAddedFormat, _format.Integer(diff.AddedCount)));
            }
            if (diff.ChangedCount > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportDiffChipChangedFormat, _format.Integer(diff.ChangedCount)));
            }
            if (diff.RemovedCount > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportDiffChipRemovedFormat, _format.Integer(diff.RemovedCount)));
            }
            return string.Join(FootSeparator, parts.ToArray());
        }

        private string StampTimeText(PublishedCalendarStamp stamp)
        {
            DateTime publishedUtc;
            return stamp.TryGetPublishedUtc(out publishedUtc) ? _format.ShortDateTime(publishedUtc) : stamp.PublishedUtcText;
        }

        private ToolbarMenu BuildFormatMenu(LiveEventCalendarJsonFormat current)
        {
            // Menu KHÔNG mang chữ: giá trị đã in ở dòng value của tile ("2" + "có recurring"), đặt thêm text vào ToolbarMenu là
            // in con số hai lần trên cùng một tile [SD2 §3.5]. Menu chỉ còn mũi tên để bấm đổi định dạng.
            ToolbarMenu menu = new ToolbarMenu { name = FormatMenuElementName };
            menu.menu.AppendAction(LiveOpsHubStrings.ExportMetricFormatChoice2,
                action => RaiseFormatSelected(LiveEventCalendarJsonFormat.Version2),
                action => current == LiveEventCalendarJsonFormat.Version2 ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            menu.menu.AppendAction(LiveOpsHubStrings.ExportMetricFormatChoice1,
                action => RaiseFormatSelected(LiveEventCalendarJsonFormat.Version1),
                action => current == LiveEventCalendarJsonFormat.Version1 ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            return menu;
        }

        private void RaiseFormatSelected(LiveEventCalendarJsonFormat format)
        {
            Action<LiveEventCalendarJsonFormat> handler = FormatSelected;
            if (handler != null) handler(format);
        }

        private static string FormatValueText(LiveEventCalendarJsonFormat format)
        {
            return format == LiveEventCalendarJsonFormat.Version1
                ? LiveOpsHubStrings.ExportMetricFormatChoice1
                : LiveOpsHubStrings.ExportMetricFormatChoice2;
        }

        private static string FormatUnitText(LiveEventCalendarJsonFormat format)
        {
            return format == LiveEventCalendarJsonFormat.Version1
                ? LiveOpsHubStrings.ExportMetricFormatUnitVersion1
                : LiveOpsHubStrings.ExportMetricFormatUnitVersion2;
        }

        private static VisualElement BuildMetric(string caption, string value, string unit, string foot, string tooltipText, bool valueIsMono)
        {
            VisualElement tile = new VisualElement { tooltip = tooltipText };
            tile.AddToClassList(LiveOpsHubClassNames.Metric);

            Label captionLabel = new Label(caption);
            captionLabel.AddToClassList(LiveOpsHubClassNames.MetricCaption);
            tile.Add(captionLabel);

            VisualElement valueRow = new VisualElement();
            valueRow.AddToClassList(LiveOpsHubClassNames.ExportMetricValueRow);
            Label valueLabel = new Label(value);
            valueLabel.AddToClassList(LiveOpsHubClassNames.MetricValue);
            if (valueIsMono) valueLabel.AddToClassList(LiveOpsHubClassNames.Mono);
            valueRow.Add(valueLabel);
            if (unit.Length > 0)
            {
                Label unitLabel = new Label(unit);
                unitLabel.AddToClassList(LiveOpsHubClassNames.MetricUnit);
                valueRow.Add(unitLabel);
            }
            tile.Add(valueRow);

            VisualElement footRow = new VisualElement();
            footRow.AddToClassList(LiveOpsHubClassNames.ExportMetricFootRow);
            if (foot.Length > 0)
            {
                Label footLabel = new Label(foot);
                footLabel.AddToClassList(LiveOpsHubClassNames.MetricFoot);
                footRow.Add(footLabel);
            }
            tile.Add(footRow);
            return tile;
        }
    }
}
