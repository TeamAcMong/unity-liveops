using System;
using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kiểm bố cục tự động của cổng W8-UX (§3.3): MỖI màn × 6 cỡ cửa sổ × vi/en. Một test cho một màn; bên trong mở hub một lần
    /// cho mỗi ngôn ngữ rồi đổi cỡ như người kéo mép cửa sổ, kiểm sau mỗi lần.
    /// <para>
    /// Vì sao gộp 12 lượt vào một test thay vì 12 test tham số: <c>[UnityTest]</c> có tham số chạy khác nhau giữa UTF 1.1.33
    /// (2022.3) và 1.8 (6000.6) — cổng phải cho CÙNG kết quả hai bản. Đổi lại, mỗi test gom mọi lỗi của 12 lượt vào MỘT câu
    /// assert kèm đường dẫn JSON, nên vẫn thấy hết một lần chứ không phải sửa từng cái rồi chạy lại.
    /// </para>
    /// <para>
    /// Test này ĐỎ trên code trước đợt W8 là có chủ đích — đó là 63 lỗi mà 900 test cũ không thấy. Chỗ cắt có chủ đích khai ở
    /// <see cref="UxLayoutAllowList"/> kèm lý do, không sửa ngưỡng ở đây.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxLayoutAuditTests
    {
        /// <summary>Thân màn phải lấp gần hết chiều cao khung nội dung; 95% chừa chỗ cho margin/viền của màn.</summary>
        private const float StretchRatio = 0.95f;

        private UxHubWindowFixture _fixture;

        [TearDown]
        public void TearDown()
        {
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
        }

        [UnityTest]
        public IEnumerator Overview_LayoutIsUsable_AtEverySize()
        {
            yield return RunScreen(OverviewScreen());
        }

        [UnityTest]
        public IEnumerator EventTypes_LayoutIsUsable_AtEverySize()
        {
            yield return RunScreen(EventTypesScreen());
        }

        [UnityTest]
        public IEnumerator Calendar_LayoutIsUsable_AtEverySize_NoSelection()
        {
            yield return RunScreen(CalendarScreen("calendar-no-selection", null));
        }

        [UnityTest]
        public IEnumerator Calendar_LayoutIsUsable_AtEverySize_WithSelection()
        {
            yield return RunScreen(CalendarScreen("calendar-selection", SelectFirstBar));
        }

        [UnityTest]
        public IEnumerator Calendar_LayoutIsUsable_AtEverySize_MultiSelection()
        {
            yield return RunScreen(CalendarScreen("calendar-multi-selection", SelectTwoBars));
        }

        /// <summary>
        /// UX-01: chuỗi <c>calendar-root → calendar-main</c> phải lấp chiều cao <c>hub-section-body</c> ở MỌI cỡ. Test riêng vì
        /// đây là lỗi gốc kéo theo "820 màn trắng" và "vùng làn cao 0" — tách ra để cổng chỉ đúng một nguyên nhân.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_BodyFillsSection()
        {
            UxLayoutScreen screen = new UxLayoutScreen("calendar-body-fills", LiveOpsHubSections.Ids.Calendar, null,
                new[] { LiveOpsHubPaths.CalendarElementNames.Root, LiveOpsHubPaths.CalendarElementNames.Main },
                CalendarStretchRules(), null);
            yield return RunScreen(screen);
        }

        /// <summary>
        /// UX-01: thân làn của trục phải có chiều cao khi CHƯA chọn đợt nào — ở 2022.3 vùng này cao 0 ở mọi cỡ, nên không có
        /// thanh nào để bấm và người dùng không vào được màn.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_LaneViewportHasHeight_NoSelection()
        {
            UxLayoutScreen screen = new UxLayoutScreen("calendar-lane-viewport", LiveOpsHubSections.Ids.Calendar, null,
                new[] { "." + LiveOpsHubClassNames.TimelineBody, "." + LiveOpsHubClassNames.TimelineLaneRow },
                new[]
                {
                    new UxLayoutStretchRule("." + LiveOpsHubClassNames.TimelineBody, "." + LiveOpsHubClassNames.TimelineMain, true, 0.5f),
                },
                null);
            yield return RunScreen(screen);
        }

        /// <summary>UX-03: track của thước phải bám bề rộng cột timeline ở mọi cỡ — không kẹt ở bề rộng dự phòng 635px.</summary>
        [UnityTest]
        public IEnumerator Timeline_RulerTrackFillsColumn_AtEverySize()
        {
            UxLayoutScreen screen = new UxLayoutScreen("timeline-ruler-track", LiveOpsHubSections.Ids.Calendar, null,
                new[] { "." + LiveOpsHubClassNames.TimelineRulerTrack },
                new[]
                {
                    new UxLayoutStretchRule("." + LiveOpsHubClassNames.TimelineRulerTrack,
                        LiveOpsHubPaths.CalendarElementNames.TimelineColumn, false, 0.5f),
                },
                null);
            yield return RunScreen(screen);
        }

        [UnityTest]
        public IEnumerator Recurring_LayoutIsUsable_AtEverySize()
        {
            yield return RunScreen(RecurringScreen());
        }

        [UnityTest]
        public IEnumerator Validation_LayoutIsUsable_AtEverySize()
        {
            yield return RunScreen(new UxLayoutScreen("validation", LiveOpsHubSections.Ids.Validation, null,
                ShellRequiredElements(), SectionStretchRules(LiveOpsHubPaths.ValidationElementNames.Body), null));
        }

        [UnityTest]
        public IEnumerator Export_LayoutIsUsable_AtEverySize()
        {
            yield return RunScreen(new UxLayoutScreen("export", LiveOpsHubSections.Ids.Export, null,
                ShellRequiredElements(), null, null));
        }

        /// <summary>UX-20/UX-27: rail và status bar là lối đi chung — ở cỡ hẹp chúng vẫn phải bấm và đọc được.</summary>
        [UnityTest]
        public IEnumerator Shell_RailAndStatusBar_AreUsable_AtEverySize()
        {
            UxLayoutScreen screen = new UxLayoutScreen("shell-rail-status", LiveOpsHubSections.Ids.Overview, null,
                new[]
                {
                    LiveOpsHubPaths.ShellElementNames.Rail, LiveOpsHubPaths.ShellElementNames.Content,
                    LiveOpsHubPaths.ShellElementNames.SectionBody, LiveOpsHubPaths.ShellElementNames.StatusBar,
                    LiveOpsHubPaths.ShellElementNames.StatusLeftText, LiveOpsHubPaths.ShellElementNames.StatusRight,
                },
                null, null);
            yield return RunScreen(screen);
        }

        /// <summary>Danh sách miễn trừ phải luôn có lý do đọc được — chỗ duy nhất cổng im lặng không được thành bãi rác.</summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void AllowList_EveryEntry_NamesAKindAndAReason()
        {
            foreach (UxLayoutAllowEntry entry in UxLayoutAllowList.All)
            {
                Assert.IsNotEmpty(entry.Selector, "mục miễn trừ không có selector");
                Assert.IsNotEmpty(entry.Kind, "mục miễn trừ '" + entry.Selector + "' không nói nó tha loại phát hiện nào");
                Assert.Greater(entry.Reason.Length, 40,
                    "mục miễn trừ '" + entry.Selector + "' có lý do quá ngắn — phải nói RÕ vì sao cắt chỗ đó là thiết kế");
            }
        }

        // ================================================================================================ chạy một màn

        private IEnumerator RunScreen(UxLayoutScreen screen)
        {
            List<string> problems = new List<string>();
            List<string> jsonPaths = new List<string>();
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                _fixture = UxHubWindowFixture.Open(screen.SectionId, UxHubWindowFixture.AllSizes[0], language, screen.CreateServices());
                yield return _fixture.WaitForLayout();
                if (screen.AfterOpen != null) yield return screen.AfterOpen(_fixture);
                foreach (UxWindowSize size in UxHubWindowFixture.AllSizes)
                {
                    yield return _fixture.Resize(size);
                    UxLayoutAuditResult result = UxLayoutAuditor.Audit(_fixture.Window, screen.Id, size, language,
                        screen.RequiredElements, screen.StretchRules);
                    jsonPaths.Add(UxLayoutAuditor.WriteJson(result));
                    foreach (string problem in result.Problems())
                    {
                        problems.Add(screen.Id + " " + size + " " + LanguageTag(language) + " — " + problem);
                    }
                }
                _fixture.Dispose();
                _fixture = null;
            }
            Assert.IsEmpty(problems, "Kiểm bố cục màn '" + screen.Id + "' thấy " + problems.Count + " chỗ người dùng không dùng được."
                + "\nJSON chẩn đoán: " + jsonPaths[0] + " (và " + (jsonPaths.Count - 1) + " file cùng thư mục)"
                + "\n - " + string.Join("\n - ", problems.ToArray()));
        }

        private static string LanguageTag(LiveOpsHubLanguageId language)
        {
            return language == LiveOpsHubLanguageId.Vietnamese ? "vi" : "en";
        }

        // ================================================================================================ bảng màn

        private static UxLayoutScreen OverviewScreen()
        {
            return new UxLayoutScreen("overview", LiveOpsHubSections.Ids.Overview, null, ShellRequiredElements(), null, null);
        }

        private static UxLayoutScreen EventTypesScreen()
        {
            return new UxLayoutScreen("event-types", LiveOpsHubSections.Ids.EventTypes, null, ShellRequiredElements(), null, null);
        }

        private static UxLayoutScreen CalendarScreen(string screenId, Func<UxHubWindowFixture, IEnumerator> afterOpen)
        {
            List<string> required = new List<string>(ShellRequiredElements())
            {
                LiveOpsHubPaths.CalendarElementNames.Root,
                LiveOpsHubPaths.CalendarElementNames.Toolbar,
                LiveOpsHubPaths.CalendarElementNames.Main,
                LiveOpsHubPaths.CalendarElementNames.TimelineColumn,
                LiveOpsHubPaths.CalendarElementNames.Timeline,
            };
            return new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Calendar, null, required, CalendarStretchRules(), afterOpen);
        }

        private static UxLayoutScreen RecurringScreen()
        {
            List<string> required = new List<string>(ShellRequiredElements())
            {
                RecurringRulesSection.BodyElementName,
                RecurringRulesSection.ListElementName,
                RecurringRuleForm.SentenceElementName,
            };
            return new UxLayoutScreen("recurring-default", LiveOpsHubSections.Ids.RecurringRules, null, required,
                SectionStretchRules(RecurringRulesSection.BodyElementName), null);
        }

        /// <summary>Phần tử của khung có ở MỌI màn — mất một cái là mất lối đi, không phải lỗi riêng của màn nào.</summary>
        private static string[] ShellRequiredElements()
        {
            return new[]
            {
                LiveOpsHubPaths.ShellElementNames.Root,
                LiveOpsHubPaths.ShellElementNames.Main,
                LiveOpsHubPaths.ShellElementNames.Content,
                LiveOpsHubPaths.ShellElementNames.SectionHeader,
                LiveOpsHubPaths.ShellElementNames.SectionTitle,
                LiveOpsHubPaths.ShellElementNames.SectionBody,
                LiveOpsHubPaths.ShellElementNames.StatusBar,
            };
        }

        private static UxLayoutStretchRule[] SectionStretchRules(string sectionRootSelector)
        {
            return new[]
            {
                new UxLayoutStretchRule(sectionRootSelector, LiveOpsHubPaths.ShellElementNames.SectionBody, true, StretchRatio),
            };
        }

        private static UxLayoutStretchRule[] CalendarStretchRules()
        {
            return new[]
            {
                new UxLayoutStretchRule(LiveOpsHubPaths.CalendarElementNames.Root, LiveOpsHubPaths.ShellElementNames.SectionBody, true, StretchRatio),
                new UxLayoutStretchRule(LiveOpsHubPaths.CalendarElementNames.Main, LiveOpsHubPaths.CalendarElementNames.Root, true, 0.6f),
                new UxLayoutStretchRule("." + LiveOpsHubClassNames.TimelineRulerTrack, LiveOpsHubPaths.CalendarElementNames.TimelineColumn, false, 0.5f),
            };
        }

        // ================================================================================================ thao tác dựng trạng thái

        /// <summary>Chọn đợt bằng cú BẤM THẬT lên thanh — không gọi API chọn, vì chính đường bấm mới là chỗ UX-07 hỏng.</summary>
        private static IEnumerator SelectFirstBar(UxHubWindowFixture fixture)
        {
            LiveOpsTimelineBar bar = fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
            yield return UxEventSender.Click(fixture.Window, bar);
        }

        private static IEnumerator SelectTwoBars(UxHubWindowFixture fixture)
        {
            yield return SelectFirstBar(fixture);
            LiveOpsTimelineBar second = fixture.BarOf(LiveOpsDesignSample.HuntEarlyEntryKey);
            yield return UxEventSender.Click(fixture.Window, second, UxEventSender.ActionModifier);
        }

        /// <summary>Một màn của ma trận kiểm bố cục: mở ở đâu, phần tử nào bắt buộc dùng được, vùng nào phải giãn.</summary>
        private sealed class UxLayoutScreen
        {
            public UxLayoutScreen(string id, string sectionId, Func<LiveOpsHubServices> createServices,
                IReadOnlyList<string> requiredElements, IReadOnlyList<UxLayoutStretchRule> stretchRules,
                Func<UxHubWindowFixture, IEnumerator> afterOpen)
            {
                Id = id;
                SectionId = sectionId;
                _createServices = createServices;
                RequiredElements = requiredElements;
                StretchRules = stretchRules;
                AfterOpen = afterOpen;
            }

            private readonly Func<LiveOpsHubServices> _createServices;

            public string Id { get; }
            public string SectionId { get; }
            public IReadOnlyList<string> RequiredElements { get; }
            public IReadOnlyList<UxLayoutStretchRule> StretchRules { get; }

            /// <summary>Thao tác THẬT chạy sau khi cửa sổ có layout (chọn đợt, mở nháp) — null = trạng thái mở mặc định.</summary>
            public Func<UxHubWindowFixture, IEnumerator> AfterOpen { get; }

            public LiveOpsHubServices CreateServices()
            {
                return _createServices == null ? null : _createServices();
            }
        }
    }
}
