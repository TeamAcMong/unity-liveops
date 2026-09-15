using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Màn giả cho test khung và kịch bản chụp: health đặt tay, đếm lần gọi, ghi lại host/điều hướng/trạng thái view, và biến thể
    /// ném ở <see cref="CreateView"/> hoặc <see cref="GetHealth"/> (Hình 28 khung 1). Cài đủ giao diện tuỳ chọn của màn thật để
    /// test kiểm cửa sổ gọi đúng thứ tự Bind → CreateView → RestoreViewState → OnShown.
    /// </summary>
    internal sealed class FakeHubSection : IHubSection, IHubHostAware, IHubSectionActions, IHubSectionViewState, IHubSectionNavigation
    {
        internal const string BodyElementName = "fake-section-body";
        internal const string BodyLabelElementName = "fake-section-label";

        private static readonly IReadOnlyList<string> DefaultRequiredNames = Array.AsReadOnly(new[] { BodyElementName, BodyLabelElementName });

        private readonly List<string> _calls = new List<string>();

        public FakeHubSection(string id, string title, string subtitle, PipelineStage stage)
        {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            Stage = stage;
            Health = SectionHealth.Ok();
            RequiredElementNames = DefaultRequiredNames;
        }

        public string Id { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public PipelineStage Stage { get; }
        public IReadOnlyList<string> RequiredElementNames { get; set; }

        public SectionHealth Health { get; set; }

        /// <summary>Khác null → CreateView ném exception này (màn ném khi dựng).</summary>
        public Exception CreateViewException { get; set; }

        /// <summary>Khác null → GetHealth ném exception này (throttle phải đổi thành NotMeasured có lý do).</summary>
        public Exception HealthException { get; set; }

        /// <summary>Khác null → thân màn do hàm này dựng (kịch bản chụp đặt nội dung riêng).</summary>
        public Func<VisualElement> ViewFactory { get; set; }

        /// <summary>Nút thêm vào section header; null = không nút.</summary>
        public Action<VisualElement> HeaderActions { get; set; }

        /// <summary>JSON trả ra ở CaptureViewState — test đặt để chứng minh trạng thái đi qua cửa sổ.</summary>
        public string ViewStateToCapture { get; set; } = string.Empty;

        public string LastRestoredViewState { get; private set; }
        public IHubHost BoundHost { get; private set; }
        public LiveOpsHubNavigation LastNavigation { get; private set; }
        public int CreateViewCount { get; private set; }
        public int ShownCount { get; private set; }
        public int GetHealthCount { get; private set; }

        /// <summary>Nhật ký thứ tự gọi ("Bind", "CreateView", "Restore", "Shown", "Navigation").</summary>
        public IReadOnlyList<string> Calls => _calls;

        public VisualElement LastView { get; private set; }

        public SectionHealth GetHealth()
        {
            GetHealthCount++;
            if (HealthException != null) throw HealthException;
            return Health;
        }

        public VisualElement CreateView()
        {
            CreateViewCount++;
            _calls.Add("CreateView");
            if (CreateViewException != null) throw CreateViewException;
            VisualElement view;
            if (ViewFactory != null)
            {
                view = ViewFactory();
            }
            else
            {
                view = new VisualElement { name = BodyElementName };
                view.Add(new Label(Title) { name = BodyLabelElementName });
            }
            LastView = view;
            return view;
        }

        public void OnShown()
        {
            ShownCount++;
            _calls.Add("Shown");
        }

        public void Bind(IHubHost host)
        {
            BoundHost = host;
            _calls.Add("Bind");
        }

        public void PopulateHeaderActions(VisualElement container)
        {
            HeaderActions?.Invoke(container);
        }

        public string CaptureViewState()
        {
            return ViewStateToCapture;
        }

        public void RestoreViewState(string viewStateJson)
        {
            LastRestoredViewState = viewStateJson;
            _calls.Add("Restore");
        }

        public void ApplyNavigation(LiveOpsHubNavigation navigation)
        {
            LastNavigation = navigation;
            _calls.Add("Navigation");
        }

        /// <summary>6 màn giả cùng id/tiêu đề/subtitle/tầng với registry thật, health Ok — nền cho test đặt health từng màn.</summary>
        public static List<FakeHubSection> CreateRegistryShaped()
        {
            return new List<FakeHubSection>
            {
                new FakeHubSection(LiveOpsHubSections.Ids.Overview, LiveOpsHubStrings.ShellOverviewTitle, LiveOpsHubStrings.ShellOverviewSubtitle, PipelineStage.Configure),
                new FakeHubSection(LiveOpsHubSections.Ids.EventTypes, LiveOpsHubStrings.ShellEventTypesTitle, LiveOpsHubStrings.ShellEventTypesSubtitle, PipelineStage.Configure),
                new FakeHubSection(LiveOpsHubSections.Ids.Calendar, LiveOpsHubStrings.ShellCalendarTitle, LiveOpsHubStrings.ShellCalendarSubtitle, PipelineStage.Schedule),
                new FakeHubSection(LiveOpsHubSections.Ids.RecurringRules, LiveOpsHubStrings.ShellRecurringRulesTitle, LiveOpsHubStrings.ShellRecurringRulesSubtitle, PipelineStage.Schedule),
                new FakeHubSection(LiveOpsHubSections.Ids.Validation, LiveOpsHubStrings.ShellValidationTitle, LiveOpsHubStrings.ShellValidationSubtitle, PipelineStage.Check),
                new FakeHubSection(LiveOpsHubSections.Ids.Export, LiveOpsHubStrings.ShellExportTitle, LiveOpsHubStrings.ShellExportSubtitle, PipelineStage.Export),
            };
        }

        /// <summary>
        /// Health mặc định của Hình 4 ([FD §3.1]): Lịch Blocked, Luật lặp Warning, Kiểm lịch Blocked, Xuất JSON Blocked — chữ lý do
        /// lấy nguyên văn thiết kế để rail và ảnh chụp so được với hình. Số đếm theo đích 6.4 (V-21 CC-SHELL-5 (b)): Lịch 2 bị bỏ ·
        /// 2 nên xem, Luật lặp 1 mất tiến độ, Kiểm lịch toàn lịch 2 · 1 · 2 · 1 chưa kiểm; Xuất JSON không đếm phát hiện — rail cộng các
        /// số này, không đọc chữ badge.
        /// </summary>
        public static List<FakeHubSection> CreateDesignSampleShaped()
        {
            List<FakeHubSection> sections = CreateRegistryShaped();
            sections[2].Health = SectionHealth.Blocked("2 bị bỏ",
                "2 đợt bị bỏ: hunt-0916-bonus (chồng giờ), lava-quest-2026-10 (giờ kết thúc sai định dạng) · 2 nên xem: hunt-0914 không tự khai configKey, lava-quest trống 11 ngày")
                .WithCounts(new LiveOpsHubFindingCounts(2, 0, 2, 0));
            sections[3].Health = SectionHealth.Warning("1 mất tiến độ", "weekly-pass đổi tiền tố khi weekly-pass-35 đang chạy")
                .WithCounts(new LiveOpsHubFindingCounts(0, 1, 0, 0));
            sections[4].Health = SectionHealth.Blocked("2 bị bỏ", "2 đợt sẽ bị game bỏ khi đọc lịch").WithCounts(new LiveOpsHubFindingCounts(2, 1, 2, 1));
            sections[5].Health = SectionHealth.Blocked("chặn", "Copy JSON bị khoá: 2 đợt bị bỏ, 1 thay đổi bắt buộc chưa xem");
            return sections;
        }

        public static IReadOnlyList<IHubSection> AsSections(IEnumerable<FakeHubSection> fakes)
        {
            List<IHubSection> sections = new List<IHubSection>();
            foreach (FakeHubSection fake in fakes) sections.Add(fake);
            return sections;
        }
    }
}
