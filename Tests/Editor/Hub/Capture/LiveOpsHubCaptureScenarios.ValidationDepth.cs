using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng ValidationDepth (G-VALIDATION-DEPTH, W5) — ba ô chiều sâu của Hình 17 ([SD2 §2.5, §2.7]):
    /// <list type="bullet">
    /// <item><c>h17-1-safe-bulk-preview</c>: card xem trước sửa hàng loạt chen dưới dải summary, mọi mục bật sẵn.</item>
    /// <item><c>h17-5-ignore-popover</c>: popover "Bỏ qua cảnh báo…" 300px với ghi chú đã gõ (nút chính mới mở).</item>
    /// <item><c>h17-6-ignored-group</c>: nhóm "Đã bỏ qua (1)" MỞ ra, có nút nhỏ "Bỏ bỏ qua" trên hàng.</item>
    /// </list>
    /// Menu chuột phải của hàng và menu "Quyết định… ▾" KHÔNG có ảnh: chúng là menu gốc của Unity, không chụp được (S-24) —
    /// nội dung của chúng khoá bằng <c>ValidationDepthTests.ContextMenu_Items</c> và <c>DecisionMenu_TwoChoices</c>.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        private const string ValidationDepthHuntType = "treasure-hunt";
        private const string ValidationDepthLavaType = "lava-quest";
        private const string ValidationDepthHuntConfigKey = "hunt_default";
        private const string ValidationDepthLavaConfigKey = "lava_quest_v2";

        /// <summary>Ghi chú của mockup ô 5 ([SD2 §2.7]).</summary>
        private const string ValidationDepthIgnoreNote = "lava-quest nghỉ đến mùa tháng 10";

        // Số đo bám hình: popover Bỏ qua 300px ([SD2 §2.7] "mockup 300px"), card "Đã bỏ qua" 170px ([SD2 §2.1]).
        private const float ValidationDepthIgnorePopoverWidth = 300f;

        /// <summary>
        /// Một hàng thay đổi cao tối thiểu 22 ([SD2 §2.5]). Bề ngang card KHÔNG đo trên ảnh: ô tick của hàng nằm đúng 4px
        /// trong viền card, mà <c>measure-capture.py</c> dò cạnh trong bán kính 4px và ở skin tối cạnh ô tick (56 → 33) mạnh
        /// hơn cạnh viền (56 → 36) nên nó bắt nhầm — giới hạn của công cụ, không phải lệch layout. Bề ngang, khe 6 và thụt
        /// 20px của dòng Undo được khoá bằng <c>ValidationDepthTests.BulkRepairPreview_GeometryMatchesDesign</c>.
        /// </summary>
        private const float ValidationDepthBulkPreviewRowHeight = 22f;

        /// <summary>
        /// Nhóm "Đã bỏ qua" MỞ chiếm cả bề ngang cột card (756px = 1064 − pane Chi tiết 300 − khe 8), không còn 170px của
        /// lúc thu gọn (soát W5 F-1).
        /// </summary>
        private const float ValidationDepthIgnoredOpenCardWidth = 756f;

        static partial void RegisterValidationDepth(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H171SafeBulkPreview, StandardWidth, StandardHeight,
                OpenValidationBulkPreview).WithExpectedFrames(ValidationDepthBulkPreviewFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H175IgnorePopover, StandardWidth, StandardHeight,
                OpenValidationIgnorePopover).WithExpectedFrames(ValidationDepthIgnorePopoverFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H176IgnoredGroup, StandardWidth, StandardHeight,
                OpenValidationIgnoredGroup).WithExpectedFrames(ValidationDepthIgnoredOpenFrames()));
        }

        /// <summary>
        /// Ô 1 đo CHÍNH thứ nó sinh ra để chụp (PD-36): chiều cao hai hàng thay đổi của card ([SD2 §2.5] "hàng min-height 22").
        /// Thiếu hai dòng này thì ảnh h17-1 chỉ chứng minh cái vỏ màn, không chứng minh cái card.
        /// </summary>
        private static LiveOpsHubCaptureExpectedFrame[] ValidationDepthBulkPreviewFrames()
        {
            List<LiveOpsHubCaptureExpectedFrame> frames = ShellFrames();
            frames.Add(new LiveOpsHubCaptureExpectedFrame(IgnoredCardElementName, ValidationIgnoredCardWidth, 0f));
            frames.Add(new LiveOpsHubCaptureExpectedFrame(BulkPreviewFirstRowElementName, 0f, ValidationDepthBulkPreviewRowHeight));
            frames.Add(new LiveOpsHubCaptureExpectedFrame(BulkPreviewSecondRowElementName, 0f, ValidationDepthBulkPreviewRowHeight));
            return frames.ToArray();
        }

        /// <summary>Ô 6 chụp nhóm ĐANG MỞ, nên bề ngang phải là bề ngang lúc mở, không phải 170px lúc thu gọn.</summary>
        private static LiveOpsHubCaptureExpectedFrame[] ValidationDepthIgnoredOpenFrames()
        {
            List<LiveOpsHubCaptureExpectedFrame> frames = ShellFrames();
            frames.Add(new LiveOpsHubCaptureExpectedFrame(IgnoredCardElementName, ValidationDepthIgnoredOpenCardWidth, 0f));
            return frames.ToArray();
        }

        private static readonly string BulkPreviewFirstRowElementName =
            LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewRowPrefix + "0";

        private static readonly string BulkPreviewSecondRowElementName =
            LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewRowPrefix + "1";

        private static LiveOpsHubCaptureExpectedFrame[] ValidationDepthIgnorePopoverFrames()
        {
            List<LiveOpsHubCaptureExpectedFrame> frames = ShellFrames();
            frames.Add(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.ValidationDepthElementNames.IgnoreBody,
                ValidationDepthIgnorePopoverWidth, 0f));
            return frames.ToArray();
        }

        /// <summary>
        /// Ô 1: hai đợt giờ hỏng chuẩn hoá được = hai hàng thay đổi, đủ để thấy Toggle bật tắt được từng mục. Lịch mẫu chỉ có
        /// MỘT lệnh sửa an toàn nên nó cho một card một hàng — không đọc được luật "nhãn đếm theo số mục đang bật".
        /// </summary>
        private static EditorWindow OpenValidationBulkPreview()
        {
            LiveOpsHubServices services = ValidationDepthServices(ValidationDepthTwoBrokenTimesDocument());
            EditorWindow window = OpenValidation(services, null);
            Button safeRepair = window.rootVisualElement.Q<Button>(ValidationSection.SafeRepairButtonElementName);
            if (safeRepair != null) SubmitValidationDepth(safeRepair);
            return window;
        }

        /// <summary>
        /// Ô 5: popover là cửa sổ HĐH riêng, không nằm trong ảnh của cửa sổ hub — gắn CHÍNH cây của popover vào root hub
        /// (dựng bằng cùng lớp, cùng dữ liệu). Ghi chú gõ sẵn để ảnh vẽ nút chính đã mở, đúng mockup.
        /// </summary>
        private static EditorWindow OpenValidationIgnorePopover()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            EditorWindow window = OpenValidation(services, null);
            LiveEventCalendarFinding finding = ValidationDepthIgnorableFinding(services);
            if (finding == null) return window;
            IgnoreWarningPopover popover = new IgnoreWarningPopover(finding, services.Format, services.LayoutLoader,
                services.Session.AssetFileName, warning => { });
            VisualElement built = popover.BuildForTest();
            window.rootVisualElement.Add(built);
            if (popover.NoteField != null) popover.NoteField.value = ValidationDepthIgnoreNote;
            return window;
        }

        /// <summary>Ô 6: một mục đã bỏ qua, nhóm MỞ sẵn — thu gọn thì ảnh chỉ còn cái tiêu đề đã có ở Hình 15.</summary>
        private static EditorWindow OpenValidationIgnoredGroup()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = ValidationDepthIgnorableFinding(services);
            if (finding != null)
            {
                var warning = new IgnoredCalendarWarning(finding.RuleId, finding.TargetId,
                    finding.RangeStartUtc.HasValue ? LiveEventUtcText.Format(finding.RangeStartUtc.Value) : string.Empty,
                    finding.RangeEndUtc.HasValue ? LiveEventUtcText.Format(finding.RangeEndUtc.Value) : string.Empty,
                    ValidationDepthIgnoreNote, string.Empty);
                services.Session.Apply(new AddIgnoredWarningEdit(warning), LiveOpsHubStrings.ValidationDepthIgnoreHeader);
                services.Session.RunCheckToCompletion();
            }

            EditorWindow window = OpenValidation(services, null);
            ValidationGroupCard ignoredCard = window.rootVisualElement.Q<ValidationGroupCard>(IgnoredCardElementName);
            if (ignoredCard != null) ignoredCard.SetCollapsed(false);
            return window;
        }

        private static LiveEventCalendarFinding ValidationDepthIgnorableFinding(LiveOpsHubServices services)
        {
            LiveEventCalendarCheckReport report = services.Session.Check.LastReport;
            if (report == null) return null;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                if (report.Findings[index].RepairKind == LiveEventCalendarRepairKind.Ignorable) return report.Findings[index];
            }
            return null;
        }

        private static void SubmitValidationDepth(Button button)
        {
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        private static LiveOpsHubServices ValidationDepthServices(LiveEventCalendarDocument document)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document)));
            services.Session.RunCheckToCompletion();
            return services;
        }

        private static LiveEventCalendarDocument ValidationDepthTwoBrokenTimesDocument()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(ValidationDepthHuntType, "Săn kho báu", 2, false, ValidationDepthHuntConfigKey))
                .WithEventType(new LiveEventTypeDefinition(ValidationDepthLavaType, "Nhiệm vụ dung nham", 0, false, ValidationDepthLavaConfigKey))
                .WithFixedEvent(new FixedLiveEventEntry("entry-hunt-broken", "hunt-1001", ValidationDepthHuntType,
                    "2026-10-01T00:00:00Z", "2026-10-3", ValidationDepthHuntConfigKey))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lava-broken", "lava-1101", ValidationDepthLavaType,
                    "2026-11-01T00:00:00Z", "2026-11-3", ValidationDepthLavaConfigKey))
                .Build();
        }
    }
}
