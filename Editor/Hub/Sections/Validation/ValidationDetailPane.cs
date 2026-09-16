using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Pane "Chi tiết" 300px của màn Kiểm lịch ([SD2 §2.3]): key–value Đợt/Loại/Luật → "ĐIỀU XẢY RA VỚI NGƯỜI CHƠI" →
    /// "VÌ SAO" → "CÁCH SỬA" (nút full-width, mỗi lệnh sửa một nút) → link tài liệu luật. Thứ tự đó là cố ý: người đọc cần
    /// biết hậu quả với NGƯỜI CHƠI trước, cơ chế sau.
    /// <para>
    /// Pane là sibling NGOÀI <c>TwoPaneSplitView</c> để ở <c>--medium</c> (&lt; 1100px) nó thành drawer phủ bên phải; vì vậy
    /// element luôn nằm trong cây (probe và <c>HubWindowTests</c> Q được ở mọi trạng thái) và chỉ đổi class.
    /// </para>
    /// </summary>
    internal sealed class ValidationDetailPane : VisualElement
    {
        internal ValidationDetailPane()
        {
            name = LiveOpsHubPaths.ValidationElementNames.Detail;
            AddToClassList(LiveOpsHubClassNames.ValidationDetail);

            VisualElement header = new VisualElement();
            header.AddToClassList(LiveOpsHubClassNames.CardHeader);
            Label title = new Label(LiveOpsHubStrings.ValidationDetailTitle);
            title.AddToClassList(LiveOpsHubClassNames.ValidationDetailTitle);
            header.Add(title);

            CloseButton = new Button(OnCloseClicked) { tooltip = LiveOpsHubStrings.ValidationDetailCloseTooltip };
            CloseButton.AddToClassList(LiveOpsHubClassNames.ValidationDetailClose);
            CloseButton.Add(LiveOpsHubIcons.CreateImage(CloseIconName, CloseIconSize));
            header.Add(CloseButton);
            Add(header);

            Body = new VisualElement();
            Add(Body);
            Show(null, null, null);
        }

        /// <summary>Người dùng chọn một cách sửa trong "CÁCH SỬA" — màn mở popover Đề xuất… với lựa chọn đó chọn sẵn.</summary>
        internal event Action<LiveEventCalendarFinding, int> RepairRequested;

        /// <summary>Nút đóng của drawer ở bề rộng hẹp.</summary>
        internal event Action CloseRequested;

        internal Button CloseButton { get; }
        internal VisualElement Body { get; }
        internal LiveEventCalendarFinding Finding { get; private set; }
        internal IReadOnlyList<Button> RepairButtons => _repairButtons;

        private readonly List<Button> _repairButtons = new List<Button>();
        private const string CloseIconName = "clear";
        private const int CloseIconSize = 12;

        /// <summary>Vẽ lại pane cho một phát hiện; <paramref name="finding"/> null = câu mời chọn một hàng.</summary>
        internal void Show(LiveEventCalendarFinding finding, LiveOpsHubFormat format, LiveEventCalendarDocument document)
        {
            Finding = finding;
            _repairButtons.Clear();
            Body.Clear();
            if (finding == null || format == null)
            {
                Label empty = new Label(LiveOpsHubStrings.ValidationDetailEmpty);
                empty.AddToClassList(LiveOpsHubClassNames.ValidationDetailParagraph);
                empty.AddToClassList(LiveOpsHubClassNames.TextQuiet);
                Body.Add(empty);
                return;
            }

            if (finding.TargetId.Length > 0) Body.Add(KeyValueRow(LiveOpsHubStrings.ValidationDetailEventLabel, finding.TargetId, true, -1));
            string eventType = EventTypeOf(finding, document);
            if (eventType.Length > 0)
            {
                Body.Add(KeyValueRow(LiveOpsHubStrings.ValidationDetailTypeLabel, eventType, false, ColorSlotOf(eventType, document)));
            }
            Body.Add(KeyValueRow(LiveOpsHubStrings.ValidationDetailRuleLabel, finding.RuleId, true, -1));

            AddSection(LiveOpsHubStrings.ValidationDetailWhatHappensHeading, LiveOpsFindingText.ConsequenceSentence(finding, format));
            AddSection(LiveOpsHubStrings.ValidationDetailWhyHeading, LiveOpsFindingText.WhySentence(finding.RuleId));

            string manualFix = LiveOpsFindingText.ManualFixSentence(finding, format);
            if (finding.Repairs.Count > 0 || manualFix.Length > 0)
            {
                Body.Add(Heading(LiveOpsHubStrings.ValidationDetailHowToFixHeading));
                // (V-21 CC-VALB-3) Không lệnh sửa nào: câu chỉ cách sửa tay thay chỗ các nút, chứ không để trống mục "CÁCH SỬA".
                if (finding.Repairs.Count == 0) Body.Add(Paragraph(manualFix));
                for (int index = 0; index < finding.Repairs.Count; index++)
                {
                    int repairIndex = index;
                    Button repairButton = new Button(() => RepairRequested?.Invoke(finding, repairIndex))
                    {
                        text = LiveOpsFindingText.RepairOptionText(finding, finding.Repairs[index], format),
                    };
                    repairButton.AddToClassList(LiveOpsHubClassNames.Button);
                    repairButton.AddToClassList(LiveOpsHubClassNames.ValidationDetailFixButton);
                    Body.Add(repairButton);
                    _repairButtons.Add(repairButton);
                }
            }

            VisualElement helpRow = new VisualElement();
            helpRow.AddToClassList(LiveOpsHubClassNames.ValidationDetailHelpRow);
            helpRow.Add(LiveOpsHubIcons.CreateImage(HelpIconName, HelpIconSize));
            DocumentationButton = new Button(() => Application.OpenURL(LiveOpsHubPaths.RuleDocumentationUrl(finding.RuleId)))
            {
                text = LiveOpsHubStrings.ValidationDetailDocumentationLink,
            };
            DocumentationButton.AddToClassList(LiveOpsHubClassNames.ValidationRowLink);
            helpRow.Add(DocumentationButton);
            Body.Add(helpRow);
        }

        internal Button DocumentationButton { get; private set; }

        /// <summary>Drawer (&lt; 1100px) phủ bên phải và đóng được bằng Esc; ở bề rộng đủ, pane là cột thường.</summary>
        internal void SetDrawer(bool isDrawer)
        {
            EnableInClassList(LiveOpsHubClassNames.ValidationDetailDrawer, isDrawer);
            EnableInClassList(LiveOpsHubClassNames.Drawer, isDrawer);
            CloseButton.EnableInClassList(LiveOpsHubClassNames.ValidationHidden, !isDrawer);
        }

        private const string HelpIconName = "_Help";
        private const int HelpIconSize = 14;

        private void OnCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void AddSection(string heading, string sentence)
        {
            if (string.IsNullOrEmpty(sentence)) return;
            Body.Add(Heading(heading));
            Body.Add(Paragraph(sentence));
        }

        private static Label Heading(string text)
        {
            Label label = new Label(text);
            label.AddToClassList(LiveOpsHubClassNames.ValidationDetailHeading);
            return label;
        }

        private static Label Paragraph(string text)
        {
            Label label = new Label(text) { enableRichText = true };
            label.AddToClassList(LiveOpsHubClassNames.ValidationDetailParagraph);
            return label;
        }

        private static VisualElement KeyValueRow(string key, string value, bool isMono, int colorSlot)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.KeyValue);
            Label keyLabel = new Label(key);
            keyLabel.AddToClassList(LiveOpsHubClassNames.KeyValueKey);
            row.Add(keyLabel);
            if (colorSlot >= 0)
            {
                VisualElement swatch = new VisualElement();
                swatch.AddToClassList(LiveOpsHubClassNames.Swatch);
                LiveOpsHubStyle.SetEventColor(swatch, colorSlot);
                row.Add(swatch);
            }
            Label valueLabel = new Label(value) { enableRichText = false };
            if (isMono) valueLabel.AddToClassList(LiveOpsHubClassNames.Mono);
            row.Add(valueLabel);
            return row;
        }

        private static string EventTypeOf(LiveEventCalendarFinding finding, LiveEventCalendarDocument document)
        {
            if (document == null) return string.Empty;
            return new ValidationTextContext(document, default(DateTime), string.Empty).EventTypeOf(finding);
        }

        private static int ColorSlotOf(string eventType, LiveEventCalendarDocument document)
        {
            LiveEventTypeDefinition definition;
            return document != null && document.TryGetEventType(eventType, out definition) ? definition.ColorSlot : -1;
        }
    }
}
