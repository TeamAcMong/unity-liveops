using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Validation (G-VALIDATION, W4) — Hình 15/16 và các ô của Hình 17 ([SD2 §2.1–2.8]):
    /// <list type="bullet">
    /// <item><c>h15-validation-default</c>: mẫu thiết kế lúc 08:47 — 5 phát hiện + 1 luật chưa kiểm + 6 luật đã qua.</item>
    /// <item><c>h17-2a-stale-edited</c> / <c>h17-2b-stale-milestone</c>: hai LÝ DO cũ khác nhau, hai câu khác nhau.</item>
    /// <item><c>h17-3-running</c>: đang kiểm 7/12 luật — hộp spinner + thanh tiến trình, cửa sổ không bị khoá.</item>
    /// <item><c>h17-4-no-errors</c>: không còn lỗi, nhưng hàng "chưa kiểm" vẫn ở lại (Q-12).</item>
    /// <item><c>h17-7-proposal-popover</c> / <c>h17-8-toast-after-apply</c>: popover Đề xuất… và toast sau khi Áp.</item>
    /// <item><c>h17-10-rule-failed</c>: một luật ném — hàng NotMeasured + "Copy lỗi".</item>
    /// <item><c>h17-11-should-review-only</c>: hết Bị bỏ / Mất tiến độ mà còn Nên xem (thiết kế chưa vẽ, [SD2 §4 mục 23]).</item>
    /// </list>
    /// Mọi kịch bản mở cửa sổ hub thật bằng <c>OpenWithServices</c> trên CÙNG một services với registry, và đặt trạng thái
    /// TRƯỚC khi mở: view của màn dựng trong CreateGUI nên sửa sau khi Show là một cuộc đua với lượt layout đầu tiên.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        private const string ValidationHuntType = "treasure-hunt";
        private const string ValidationHuntDefaultConfigKey = "hunt_default";

        /// <summary>Đợt được chọn sẵn trong Hình 15 (hàng đầu nhóm Bị bỏ, pane Chi tiết vẽ đúng phát hiện của nó).</summary>
        private const string ValidationSelectedEventId = "hunt-0916-bonus";

        // Số đo của màn theo [SD2 §2.1–2.6]; khai ở đây để ảnh chụp kiểm được phần của gói, không chỉ vỏ hub (9.5, PD-36).
        private const float ValidationRailWidth = 196f;
        private const float ValidationContentWidth = 1084f;
        private const float ValidationSummaryHeight = 24f;
        private const float ValidationProgressHeight = 24f;
        private const float ValidationDetailWidth = 300f;
        private const float ValidationIgnoredCardWidth = 170f;
        private const float ValidationProposalWidth = 320f;
        private const float ValidationToastHeight = 24f;

        /// <summary>Đủ xa để vượt mốc mở/khép gần nhất của lịch mẫu — đúng lý do cũ "đã qua mốc" (PD-23).</summary>
        private const int ValidationMilestoneScanDays = 30;

        static partial void RegisterValidation(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H15ValidationDefault, StandardWidth, StandardHeight,
                OpenValidationDefault).WithExpectedFrames(GroupsStateFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H172aStaleEdited, StandardWidth, StandardHeight,
                OpenValidationStaleEdited).WithExpectedFrames(GroupsStateFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H172bStaleMilestone, StandardWidth, StandardHeight,
                OpenValidationStaleMilestone).WithExpectedFrames(GroupsStateFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H173Running, StandardWidth, StandardHeight,
                OpenValidationRunning).WithExpectedFrames(RunningStateFrames(false)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H174NoErrors, StandardWidth, StandardHeight,
                OpenValidationNoErrors).WithExpectedFrames(NoErrorsStateFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H177ProposalPopover, StandardWidth, StandardHeight,
                OpenValidationProposalPopover).WithExpectedFrames(ProposalFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H178ToastAfterApply, StandardWidth, StandardHeight,
                OpenValidationToastAfterApply).WithExpectedFrames(RunningStateFrames(true)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H1710RuleFailed, StandardWidth, StandardHeight,
                OpenValidationRuleFailed).WithExpectedFrames(GroupsStateFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H1711ShouldReviewOnly, StandardWidth, StandardHeight,
                OpenValidationShouldReviewOnly).WithExpectedFrames(GroupsStateFrames()));
        }

        /// <summary>Hai khung vỏ hub — mọi kịch bản của màn đều mở cửa sổ 1280×760 nên rail và cột nội dung luôn đo được.</summary>
        private static List<LiveOpsHubCaptureExpectedFrame> ShellFrames()
        {
            return new List<LiveOpsHubCaptureExpectedFrame>
            {
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Rail, ValidationRailWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Content, ValidationContentWidth, 0f),
            };
        }

        /// <summary>
        /// Trạng thái có card: dải summary 24, pane Chi tiết 300 (KHÔNG drawer ở cửa sổ 1280 — [SD2 §2.1] chỉ đổi sang drawer
        /// dưới 1100), card "Đã bỏ qua" 170.
        /// <para>
        /// KHÔNG khai hai số sau, có lý do đo được:
        /// • sọc mức độ 3px của hàng ([SD2 §2.2]) là <c>border-left-width</c> của <c>liveops-hub-finding-stripe--*</c>, không
        ///   phải bề rộng element: hộp của nó rộng 8px (3 viền + 5 khe tới icon), nên khai 3 sẽ đỏ dù ảnh đúng. Số 3 khoá bằng
        ///   USS dùng chung + <c>check-class-names.py</c>.
        /// • ô tìm 190px: <c>worldBound</c> đúng 190 ở cả hai skin, nhưng dò cạnh ở skin SÁNG ra 188 (viền ô nhạt hơn nền một
        ///   mức xám) — số đo của công cụ, không phải lệch của màn. Bề rộng 190 khoá bằng USS.
        /// </para>
        /// </summary>
        private static LiveOpsHubCaptureExpectedFrame[] GroupsStateFrames()
        {
            List<LiveOpsHubCaptureExpectedFrame> frames = ShellFrames();
            frames.Add(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.ValidationElementNames.Summary, 0f, ValidationSummaryHeight));
            frames.Add(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.ValidationElementNames.Detail, ValidationDetailWidth, 0f));
            frames.Add(new LiveOpsHubCaptureExpectedFrame(IgnoredCardElementName, ValidationIgnoredCardWidth, 0f));
            return frames.ToArray();
        }

        /// <summary>"Không còn lỗi" vẫn là trạng thái CÓ card (hàng Chưa kiểm ở lại) — chỉ khác là không còn hàng phát hiện.</summary>
        private static LiveOpsHubCaptureExpectedFrame[] NoErrorsStateFrames()
        {
            List<LiveOpsHubCaptureExpectedFrame> frames = ShellFrames();
            frames.Add(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.ValidationElementNames.Summary, 0f, ValidationSummaryHeight));
            frames.Add(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.ValidationElementNames.Detail, ValidationDetailWidth, 0f));
            frames.Add(new LiveOpsHubCaptureExpectedFrame(IgnoredCardElementName, ValidationIgnoredCardWidth, 0f));
            return frames.ToArray();
        }

        /// <summary>Đang kiểm: chỉ hộp 24px của [SD2 §2.8]; thêm toast 24px cho ô 8 (sửa xong là tự kiểm lại, PD-10).</summary>
        private static LiveOpsHubCaptureExpectedFrame[] RunningStateFrames(bool withToast)
        {
            List<LiveOpsHubCaptureExpectedFrame> frames = ShellFrames();
            frames.Add(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.ValidationElementNames.Progress, 0f, ValidationProgressHeight));
            if (withToast) frames.Add(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Toast, 0f, ValidationToastHeight));
            return frames.ToArray();
        }

        private static LiveOpsHubCaptureExpectedFrame[] ProposalFrames()
        {
            List<LiveOpsHubCaptureExpectedFrame> frames = ShellFrames();
            frames.Add(new LiveOpsHubCaptureExpectedFrame(ProposalPopover.BodyElementName, ValidationProposalWidth, 0f));
            return frames.ToArray();
        }

        private const string IgnoredCardElementName = LiveOpsHubPaths.ValidationElementNames.GroupCardPrefix + "ignored";

        private static EditorWindow OpenValidationDefault()
        {
            // Hình 15 vẽ hàng hunt-0916-bonus ĐANG CHỌN: pane Chi tiết bên phải là của chính hàng đó, nên ảnh không chọn sẵn
            // thì nửa phải của hình chỉ còn câu mời "Chọn một phát hiện…".
            return OpenValidation(LiveOpsHubTestServices.FromDesignSample(), ValidationSelectedEventId);
        }

        private static EditorWindow OpenValidationStaleEdited()
        {
            return OpenValidation(LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.StaleCheckScenario), null);
        }

        private static EditorWindow OpenValidationStaleMilestone()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            // Không sửa lịch: chỉ để thời gian trôi qua một mốc — luật 9 và 11 phụ thuộc "bây giờ" nên kết quả hết là bằng chứng.
            services.Session.Check.Reevaluate(LiveOpsDesignSample.NowUtc.AddDays(ValidationMilestoneScanDays));
            return OpenValidation(services, null);
        }

        private static EditorWindow OpenValidationRunning()
        {
            return OpenValidation(LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.RunningCheckScenario), null);
        }

        private static EditorWindow OpenValidationNoErrors()
        {
            LiveOpsHubServices services = ServicesFor(CleanDocument(), null);
            services.Session.RunCheckToCompletion();
            return OpenValidation(services, null);
        }

        private static EditorWindow OpenValidationShouldReviewOnly()
        {
            LiveOpsHubServices services = ServicesFor(ShouldReviewOnlyDocument(), null);
            services.Session.RunCheckToCompletion();
            return OpenValidation(services, null);
        }

        private static EditorWindow OpenValidationRuleFailed()
        {
            LiveOpsHubServices services = ServicesFor(LiveOpsDesignSample.Document, ValidatorWithFailingLongGapRule());
            services.Session.RunCheckToCompletion();
            return OpenValidation(services, null);
        }

        private static EditorWindow OpenValidationProposalPopover()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            EditorWindow window = OpenValidation(services, ValidationSelectedEventId);
            LiveEventCalendarFinding finding = FirstProposalFinding(services);
            if (finding == null) return window;
            // PopupWindow là cửa sổ HĐH riêng, không nằm trong ảnh của cửa sổ hub: gắn CHÍNH cây của popover vào root hub để ô 7
            // chụp được đúng nội dung đó (dựng bằng cùng lớp, cùng dữ liệu — không nặn lại bằng tay). Bề rộng 320px đến từ USS
            // của popover chứ không phải từ GetWindowSize, nên cây gắn rời vẫn đúng khung của Hình 17 ô 7.
            ProposalPopover popover = new ProposalPopover(finding, services.Format, services.LayoutLoader, null, repair => { }, 0);
            VisualElement built = popover.BuildForTest();
            window.rootVisualElement.Add(built);
            return window;
        }

        private static EditorWindow OpenValidationToastAfterApply()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            EditorWindow window = OpenValidation(services, null);
            // Đi đúng động từ "Sửa" của HÀNG: toast phải là hệ quả của một lần sửa thật. Nút section header từ W5 mở card xem
            // trước ([SD2 §2.5]) nên nó không còn dẫn thẳng tới toast — ô 1 của Hình 17 mới là ảnh của card đó.
            Button safeRepair = SafeRepairRowButton(window);
            if (safeRepair != null)
            {
                using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
                {
                    submit.target = safeRepair;
                    safeRepair.SendEvent(submit);
                }
            }
            return window;
        }

        /// <summary>Nút "Sửa" của hàng sửa nhanh được đầu tiên; null khi lịch không có lệnh sửa an toàn nào.</summary>
        private static Button SafeRepairRowButton(EditorWindow window)
        {
            List<ValidationFindingRow> rows = window.rootVisualElement.Query<ValidationFindingRow>().ToList();
            for (int index = 0; index < rows.Count; index++)
            {
                if (rows[index].Row.Action == ValidationRowAction.SafeRepair) return rows[index].ActionButton;
            }
            return null;
        }

        private static LiveEventCalendarFinding FirstProposalFinding(LiveOpsHubServices services)
        {
            LiveEventCalendarCheckReport report = services.Session.Check.LastReport;
            if (report == null) return null;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                if (report.Findings[index].RepairKind == LiveEventCalendarRepairKind.Proposal) return report.Findings[index];
            }
            return null;
        }

        /// <summary>Lịch sạch: một loại, một đợt tương lai TỰ khai config key — không luật nào có gì để nói.</summary>
        private static LiveEventCalendarDocument CleanDocument()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(ValidationHuntType, "Săn kho báu", 2, false, ValidationHuntDefaultConfigKey))
                .WithFixedEvent(new FixedLiveEventEntry("entry-hunt-clean", "hunt-1001", ValidationHuntType,
                    "2026-10-01T00:00:00Z", "2026-10-02T00:00:00Z", "hunt_v1"))
                .Build();
        }

        /// <summary>Cùng lịch đó nhưng đợt KHÔNG tự khai config key: chỉ còn một phát hiện Nên xem, không Bị bỏ, không Mất tiến độ.</summary>
        private static LiveEventCalendarDocument ShouldReviewOnlyDocument()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(ValidationHuntType, "Săn kho báu", 2, false, ValidationHuntDefaultConfigKey))
                .WithFixedEvent(new FixedLiveEventEntry("entry-hunt-inherit", "hunt-1001", ValidationHuntType,
                    "2026-10-01T00:00:00Z", "2026-10-02T00:00:00Z", string.Empty))
                .Build();
        }

        private static LiveEventCalendarValidator ValidatorWithFailingLongGapRule()
        {
            var rules = new List<ILiveEventCalendarRule>();
            IReadOnlyList<ILiveEventCalendarRule> defaults = LiveEventCalendarValidator.Default.Rules;
            for (int index = 0; index < defaults.Count; index++)
            {
                bool isLongGap = string.Equals(defaults[index].RuleId, LiveEventCalendarRuleIds.LongGapBetweenEvents, StringComparison.Ordinal);
                rules.Add(isLongGap ? new ValidationFailingRule(defaults[index]) : defaults[index]);
            }
            return new LiveEventCalendarValidator(rules);
        }

        private static LiveOpsHubServices ServicesFor(LiveEventCalendarDocument document, LiveEventCalendarValidator validator)
        {
            LiveOpsHubServicesBuilder builder = LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document));
            if (validator != null) builder = builder.WithValidator(validator);
            return LiveOpsHubTestServices.Build(builder);
        }

        /// <param name="selectedEventId">Đích của hàng chọn sẵn sau khi cửa sổ dựng xong; "" = không chọn hàng nào.</param>
        private static EditorWindow OpenValidation(LiveOpsHubServices services, string selectedEventId)
        {
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            EditorWindow window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Validation);
            // Chọn SAU khi mở: hàng chỉ tồn tại khi view của màn đã dựng, mà view dựng trong CreateGUI của cửa sổ.
            if (!string.IsNullOrEmpty(selectedEventId))
            {
                for (int index = 0; index < sections.Count; index++)
                {
                    ValidationSection validation = sections[index] as ValidationSection;
                    if (validation != null) validation.TrySelectFinding(selectedEventId);
                }
            }
            return window;
        }
    }

    /// <summary>Luật ném để dựng ca "luật không chạy được" ([SD2 §2.8]) — giữ nguyên id và hậu quả của luật thật.</summary>
    internal sealed class ValidationFailingRule : ILiveEventCalendarRule
    {
        private readonly ILiveEventCalendarRule _original;

        internal ValidationFailingRule(ILiveEventCalendarRule original)
        {
            _original = original;
        }

        public string RuleId => _original.RuleId;
        public LiveEventCalendarConsequence Consequence => _original.Consequence;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            throw new NullReferenceException(FailureMessage);
        }

        /// <summary>Câu của EXCEPTION, không phải chữ hiển thị: nó đi vào <c>ExceptionMessage</c> rồi mới qua LiveOpsFindingText.</summary>
        private const string FailureMessage = "giả lập luật hỏng";
    }
}
