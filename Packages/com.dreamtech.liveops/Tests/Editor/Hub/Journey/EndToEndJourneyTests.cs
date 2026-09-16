using System;
using System.Collections;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hành trình J3 chạy bằng máy (mục 9.2, tiêu chí "Xong khi" của P1): designer mở asset trống, dựng lịch ba tuần, để lọt
    /// hai lỗi, sửa hết bằng đúng hai đường của màn Kiểm lịch, xuất JSON qua cổng, rồi GAME đọc lại chính chuỗi đó và trả về
    /// đúng đợt ở năm mốc giờ.
    /// <para>
    /// Vì sao một test dài thay vì năm test ngắn: từng mảnh đã có test riêng ở W1–W4; cái chưa ai chứng minh là hai đầu
    /// NỐI được với nhau — chuỗi rời khỏi hub bằng clipboard phải là chuỗi parser của game hiểu, và nội dung sau hai lệnh sửa
    /// phải là nội dung người chơi thấy. Cắt nhỏ là mất đúng phép nối đó.
    /// </para>
    /// <para>
    /// Mọi bước đi qua đường thật của người dùng: lệnh sửa qua <c>Session.Apply</c> (một bước Undo mỗi lệnh), "Kiểm lại" và
    /// "Sửa" bấm bằng chuột thật trong cửa sổ hub, "Đề xuất…" áp bằng cú click vào nút Áp trong popover (SPIKE-B: phím không
    /// bao giờ áp), Copy bấm ở màn Xuất JSON. Không gọi cửa sau vào trạng thái nào.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class EndToEndJourneyTests
    {
        private const string LavaQuestType = "lava-quest";
        private const string TreasureHuntType = "treasure-hunt";
        private const string SkyRaceType = "sky-race";

        private const string LavaQuestConfigKey = "lava_quest_v2";
        private const string TreasureHuntConfigKey = "hunt_v1";
        private const string SkyRaceConfigKey = "sky_race_v4";

        private const string LavaQuestFirstEventId = "lava-quest-0914";
        private const string LavaQuestSecondEventId = "lava-quest-0918";
        private const string LavaQuestOverlappingEventId = "lava-quest-0920";
        private const string TreasureHuntFirstEventId = "hunt-0915";
        private const string TreasureHuntSecondEventId = "hunt-0922";
        private const string TreasureHuntUnreadableEventId = "hunt-0928";

        /// <summary>
        /// Lần lặp kế tiếp của luật sky-race tại mốc giờ thứ năm. Id do lịch lặp sinh = tiền tố + SỐ THỨ TỰ tính từ neo
        /// (neo 14/9, chu kỳ 24 giờ) — 22:00 ngày 14/9 nằm trong lần lặp 0 đã đóng, nên đợt kế là lần lặp 1.
        /// </summary>
        private const string SkyRaceNextOccurrenceEventId = "sky-race-1";

        /// <summary>Giờ bắt đầu gõ sai của đợt săn kho báu cuối — thiếu số 0 ở ngày, chuẩn hoá được nên là "Sửa an toàn".</summary>
        private const string UnreadableStartUtcText = "2026-9-28T00:00:00Z";

        /// <summary>Đúng giờ mà lệnh Sửa an toàn phải viết lại.</summary>
        private const string RepairedStartUtcText = "2026-09-28T00:00:00Z";

        /// <summary>Đầu đợt chồng giờ sau khi áp đề xuất "dời đầu, giữ cuối" — bằng đúng lúc đợt giữ lại kết thúc.</summary>
        private const string ShiftedStartUtcText = "2026-09-21T00:00:00Z";

        /// <summary>Số luật của bộ kiểm — dùng để chứng minh lần kiểm chạy hết, không dừng giữa chừng.</summary>
        private const int MaximumCheckFrames = 240;

        private SectionTestScope _sectionScope;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            LiveOpsPopoverContent.CloseCurrent();
            if (_sectionScope != null) _sectionScope.Dispose();
            _sectionScope = null;
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        [UnityTest]
        public IEnumerator Journey_DesignerCreatesThreeWeeks_FixesErrors_CopiesJson_GameReadsSame()
        {
            ManualLiveOpsClock hubClock = new ManualLiveOpsClock(LiveOpsDesignSample.NowUtc);
            LiveEventCalendarAsset calendarAsset = LiveOpsHubTestServices.CreateMemoryAsset(LiveEventCalendarDocument.Empty);
            InMemoryLiveOpsHubClipboard clipboard = new InMemoryLiveOpsHubClipboard();
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(hubClock)
                .WithCalendarAsset(calendarAsset)
                .WithClipboard(clipboard));

            // ---- 1. Designer dựng lịch ba tuần trên asset trống: ba loại, một luật lặp, sáu đợt cố định.
            DeclareThreeEventTypes(services);
            DeclareOneRecurringRule(services);
            AddSixFixedEvents(services);

            Assert.AreEqual(3, services.Session.Document.EventTypes.Count, "hành trình khai đúng ba loại");
            Assert.AreEqual(1, services.Session.Document.RecurringRules.Count, "hành trình khai đúng một luật lặp");
            Assert.AreEqual(6, services.Session.Document.FixedEvents.Count, "hành trình đặt đúng sáu đợt trong ba tuần");

            // ---- 2. F5: bấm "Kiểm lại" trong màn Kiểm lịch, chờ chạy hết mười hai luật.
            ValidationSection validation = new ValidationSection(services);
            _sectionScope = SectionTestScope.Open(validation);
            yield return _sectionScope.WaitForLayout();

            yield return ClickInWindow(_sectionScope.Window, HeaderButton(ValidationSection.RecheckButtonElementName));
            Assert.IsTrue(services.Session.Check.IsRunning, "bấm \"Kiểm lại\" phải bắt đầu một lần kiểm thật");
            yield return WaitForCheckToFinish(services);

            Assert.IsNotNull(FindRow(validation, LiveEventCalendarRuleIds.UtcTimeFormat), "giờ gõ sai phải ra phát hiện utc-time-format");
            Assert.IsNotNull(FindRow(validation, LiveEventCalendarRuleIds.OverlapSameType), "hai đợt lava-quest chồng giờ phải ra phát hiện overlap-same-type");

            // ---- 3. "Sửa": đúng một lỗi sửa an toàn nên nút bật; sửa xong hub tự kiểm lại (PD-10).
            Assert.IsTrue(validation.SafeRepairSlot.Button.enabledSelf,
                "hành trình để lại đúng MỘT lỗi sửa an toàn — nút \"Sửa\" phải bật, nếu không lịch mẫu của test đã lệch");
            yield return ClickInWindow(_sectionScope.Window, HeaderButton(ValidationSection.SafeRepairButtonElementName));
            yield return WaitForCheckToFinish(services);

            Assert.AreEqual(RepairedStartUtcText, StartTextOf(services, TreasureHuntUnreadableEventId),
                "Sửa an toàn viết lại giờ gõ sai mà KHÔNG đổi thời điểm");
            Assert.IsNull(FindRow(validation, LiveEventCalendarRuleIds.UtcTimeFormat), "sửa xong thì phát hiện giờ sai phải biến mất");

            // ---- 4. "Đề xuất…": đổi điều người chơi thấy nên chỉ chạy khi có cú click thật vào nút Áp (SPIKE-B SP-2).
            ValidationFindingRow overlapRow = FindRow(validation, LiveEventCalendarRuleIds.OverlapSameType);
            Assert.IsNotNull(overlapRow, "phát hiện chồng giờ vẫn phải còn sau lần sửa an toàn");
            Assert.AreEqual(ValidationRowAction.Proposal, overlapRow.Row.Action, "chồng giờ là việc của người, hàng phải mời \"Đề xuất…\"");
            yield return ClickInWindow(_sectionScope.Window, overlapRow.ActionButton);

            ProposalPopover proposal = LiveOpsPopoverContent.Current as ProposalPopover;
            Assert.IsNotNull(proposal, "bấm \"Đề xuất…\" phải mở popover đề xuất");
            Assert.IsTrue(proposal.editorWindow != null, "popover phải mở thật — không thì cú click bên dưới không kiểm gì");
            yield return WaitForLayoutOf(proposal.ApplyButton);
            yield return ClickInWindow(proposal.editorWindow, proposal.ApplyButton);
            yield return WaitForCheckToFinish(services);

            Assert.AreEqual(ShiftedStartUtcText, StartTextOf(services, LavaQuestOverlappingEventId),
                "áp đề xuất \"dời đầu, giữ cuối\" đẩy đợt chồng giờ sang đúng lúc đợt giữ lại kết thúc");
            Assert.AreEqual(0, services.Session.Check.LastReport.Summary.DroppedCount,
                "sửa hết hai lỗi thì không còn đợt nào bị bỏ — đây là điều kiện chặn của cổng xuất");

            // ---- 5. Cổng xuất xanh rồi Copy: clipboard nhận ĐÚNG chuỗi sẽ dán lên remote.
            _sectionScope.Dispose();
            _sectionScope = null;
            ExportSection export = new ExportSection(services);
            _sectionScope = SectionTestScope.Open(export);
            yield return _sectionScope.WaitForLayout();

            Assert.AreEqual(ExportGateStatusCode.Ready, export.Gate.StatusCode,
                "lịch sạch + kiểm mới + parser đọc lại được = cổng ở trạng thái (b), nút Copy mở khoá");
            LiveEventCalendarJsonText exportedJson = services.Session.Publish.CurrentJson;
            yield return ClickInWindow(_sectionScope.Window, HeaderButton(ExportSection.CopyButtonElementName));

            Assert.AreEqual(exportedJson.Text, clipboard.Text, "clipboard nhận đúng chuỗi của cổng, không thêm bớt ký tự nào");
            Assert.AreEqual(exportedJson.Sha256Hex, services.Session.Publish.LastExportedSha256Hex, "Copy ghi lastExportedSha của đúng bản này");

            // ---- 6. Game đọc lại: parser thật + lịch ghép (đợt cố định + luật lặp) + đồng hồ tay.
            LiveEventCalendarParseResult parsed = JsonLiveEventCalendarParser.Parse(clipboard.Text);
            Assert.IsFalse(parsed.HasProblems, "chuỗi hub xuất phải vào parser của game không một lời than: " + string.Join(" · ", parsed.Problems));
            Assert.AreEqual((int)LiveEventCalendarJsonFormat.Version2, parsed.FormatVersion, "hub xuất định dạng 2 để mang được luật lặp");

            ManualLiveOpsClock gameClock = new ManualLiveOpsClock(LiveOpsDesignSample.NowUtc);
            LiveOpsSystem system = new LiveOpsSystemBuilder("journey")
                .WithClock(gameClock)
                .WithCalendar(parsed.CombinedCalendar)
                .WithEventTypesFrom(calendarAsset)
                .Build();

            // ---- 7. Năm mốc giờ: hai đợt bình thường, một đợt do đề xuất dời, một đợt do sửa an toàn cứu, một nhịp luật lặp.
            AssertActiveEvent(system, gameClock, new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc), LavaQuestType, LavaQuestFirstEventId,
                "mốc 1 — đợt lava-quest đầu tiên đang chạy");
            AssertActiveEvent(system, gameClock, new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc), TreasureHuntType, TreasureHuntFirstEventId,
                "mốc 2 — đợt săn kho báu đầu tiên đang chạy");
            AssertUpcomingEvent(system, gameClock, new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc), LavaQuestType, LavaQuestSecondEventId,
                TimeSpan.FromHours(12), "mốc 2 — lava-quest đang nghỉ giữa hai đợt, game phải nói đợt kế và còn bao lâu");
            AssertActiveEvent(system, gameClock, new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc), LavaQuestType, LavaQuestOverlappingEventId,
                "mốc 3 — đợt vừa được đề xuất dời đang chạy đúng chỗ mới; nếu đề xuất không tới được game thì đây là ô trống");
            AssertActiveEvent(system, gameClock, new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc), TreasureHuntType, TreasureHuntUnreadableEventId,
                "mốc 4 — đợt từng gõ sai giờ đã chạy được; nếu Sửa an toàn không tới được game thì đợt này vẫn bị bỏ");
            AssertUpcomingEvent(system, gameClock, new DateTime(2026, 9, 14, 22, 0, 0, DateTimeKind.Utc), SkyRaceType, SkyRaceNextOccurrenceEventId,
                TimeSpan.FromHours(2), "mốc 5 — luật lặp 20 giờ/ngày: 22:00 là giờ nghỉ, đợt kế mở lúc 00:00 hôm sau");

            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------ dựng lịch

        /// <summary>Ba loại của hành trình. Loại thứ ba chỉ chạy bằng luật lặp nên không có đợt cố định nào.</summary>
        private void DeclareThreeEventTypes(LiveOpsHubServices services)
        {
            ApplyOrFail(services, new SetEventTypeEdit(new LiveEventTypeDefinition(LavaQuestType, "Lava Quest", 0, false, LavaQuestConfigKey)),
                "Khai loại " + LavaQuestType);
            ApplyOrFail(services, new SetEventTypeEdit(new LiveEventTypeDefinition(TreasureHuntType, "Treasure Hunt", 1, false, TreasureHuntConfigKey)),
                "Khai loại " + TreasureHuntType);
            ApplyOrFail(services, new SetEventTypeEdit(new LiveEventTypeDefinition(SkyRaceType, "Sky Race", 2, false, SkyRaceConfigKey)),
                "Khai loại " + SkyRaceType);
        }

        /// <summary>Luật lặp hằng ngày: chu kỳ 24 giờ, chạy 20 giờ — bốn giờ nghỉ mỗi ngày là mốc giờ thứ năm của phép so.</summary>
        private void DeclareOneRecurringRule(LiveOpsHubServices services)
        {
            ApplyOrFail(services, new SetRecurringRuleEdit(new RecurringLiveEventRule(SkyRaceType, "2026-09-14T00:00:00Z", "sky-race-", 24, 20,
                    SkyRaceConfigKey)),
                "Đặt luật lặp " + SkyRaceType);
        }

        /// <summary>
        /// Sáu đợt trong ba tuần (14/9 → 1/10) với đúng hai khuyết điểm của hành trình: đợt lava-quest thứ ba chồng giờ lên đợt
        /// thứ hai, và đợt săn kho báu cuối gõ thiếu số 0 ở ngày. Mọi khoảng trống cùng loại đều dưới bảy ngày để luật 11 không
        /// thêm cảnh báo lạ vào phép đếm của test.
        /// </summary>
        private void AddSixFixedEvents(LiveOpsHubServices services)
        {
            AddFixedEvent(services, LavaQuestFirstEventId, LavaQuestType, "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", LavaQuestConfigKey);
            AddFixedEvent(services, LavaQuestSecondEventId, LavaQuestType, "2026-09-18T00:00:00Z", "2026-09-21T00:00:00Z", LavaQuestConfigKey);
            AddFixedEvent(services, LavaQuestOverlappingEventId, LavaQuestType, "2026-09-20T00:00:00Z", "2026-09-23T00:00:00Z", LavaQuestConfigKey);
            AddFixedEvent(services, TreasureHuntFirstEventId, TreasureHuntType, "2026-09-15T00:00:00Z", "2026-09-18T00:00:00Z", TreasureHuntConfigKey);
            AddFixedEvent(services, TreasureHuntSecondEventId, TreasureHuntType, "2026-09-22T00:00:00Z", "2026-09-25T00:00:00Z", TreasureHuntConfigKey);
            AddFixedEvent(services, TreasureHuntUnreadableEventId, TreasureHuntType, UnreadableStartUtcText, "2026-10-01T00:00:00Z", TreasureHuntConfigKey);
        }

        private void AddFixedEvent(LiveOpsHubServices services, string eventId, string eventType, string startUtcText, string endUtcText,
            string configKey)
        {
            ApplyOrFail(services, new AddFixedEventEdit(new FixedLiveEventEntry("entry-" + eventId, eventId, eventType, startUtcText, endUtcText,
                configKey)), "Thêm đợt " + eventId);
        }

        private void ApplyOrFail(LiveOpsHubServices services, LiveEventCalendarEdit edit, string undoName)
        {
            LiveOpsHubEditOutcome outcome = services.Session.Apply(edit, undoName);
            Assert.IsTrue(outcome.Applied, "dựng lịch: lệnh \"" + undoName + "\" không áp được — " + outcome.FailureText);
        }

        // ------------------------------------------------------------------ đọc lại lịch

        private static string StartTextOf(LiveOpsHubServices services, string eventId)
        {
            foreach (FixedLiveEventEntry entry in services.Session.Document.FixedEvents)
            {
                if (string.Equals(entry.EventId, eventId, StringComparison.Ordinal)) return entry.StartUtcText;
            }
            Assert.Fail("lịch không còn đợt '" + eventId + "' — lệnh sửa đã xoá nhầm mục");
            return string.Empty;
        }

        private static ValidationFindingRow FindRow(ValidationSection section, string ruleId)
        {
            for (int index = 0; index < section.VisibleRows.Count; index++)
            {
                ValidationFindingRow row = section.VisibleRows[index];
                if (row.Row.Finding != null && string.Equals(row.Row.Finding.RuleId, ruleId, StringComparison.Ordinal)) return row;
            }
            return null;
        }

        // ------------------------------------------------------------------ phép so ở mốc giờ

        private static void AssertActiveEvent(LiveOpsSystem system, ManualLiveOpsClock clock, DateTime nowUtc, string eventType,
            string expectedEventId, string reason)
        {
            clock.Set(nowUtc);
            LiveEventStatus status = system.GetStatus(eventType);
            Assert.IsTrue(status.IsActive, reason + " — nhưng loại '" + eventType + "' không có đợt nào đang chạy");
            Assert.AreEqual(expectedEventId, status.Instance.EventId, reason);
        }

        private static void AssertUpcomingEvent(LiveOpsSystem system, ManualLiveOpsClock clock, DateTime nowUtc, string eventType,
            string expectedEventId, TimeSpan expectedTimeUntilStart, string reason)
        {
            clock.Set(nowUtc);
            LiveEventStatus status = system.GetStatus(eventType);
            Assert.IsTrue(status.IsUpcoming, reason + " — nhưng loại '" + eventType + "' không ở trạng thái chờ");
            Assert.AreEqual(expectedEventId, status.Instance.EventId, reason);
            Assert.AreEqual(expectedTimeUntilStart, status.TimeUntilStart, reason + " — còn lại tới lúc mở phải khớp từng giờ");
        }

        // ------------------------------------------------------------------ bấm và chờ

        private Button HeaderButton(string elementName)
        {
            Button button = _sectionScope.Window.rootVisualElement.Q<Button>(elementName);
            Assert.IsNotNull(button, "section header thiếu nút '" + elementName + "'");
            return button;
        }

        /// <summary>Bấm chuột thật vào giữa một element trong cửa sổ đang chứa nó — gọi thẳng handler thì không chứng minh nút đã nối vào luồng.</summary>
        private static IEnumerator ClickInWindow(EditorWindow window, VisualElement element)
        {
            Assert.IsNotNull(element, "không có element để bấm");
            yield return WaitForLayoutOf(element);
            Vector2 center = element.worldBound.center;
            window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = center, button = 0, clickCount = 1 });
            window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = center, button = 0, clickCount = 1 });
            yield return null;
        }

        private static IEnumerator WaitForLayoutOf(VisualElement element)
        {
            yield return LiveOpsHubWindowTestScope.WaitForLayout(element);
        }

        /// <summary>
        /// Chờ lần kiểm đang chạy xong bằng chính nhịp <c>EditorApplication.update</c> của phiên — không gọi cửa sau
        /// <c>RunCheckToCompletion</c>, vì hành trình phải chứng minh cái nhịp thật cũng về đích.
        /// </summary>
        private static IEnumerator WaitForCheckToFinish(LiveOpsHubServices services)
        {
            int frames = 0;
            while (services.Session.Check.IsRunning)
            {
                if (++frames > MaximumCheckFrames) Assert.Fail("lần kiểm không xong sau " + MaximumCheckFrames + " khung — nhịp kiểm đã chết giữa chừng");
                yield return null;
            }
            yield return null;
        }
    }
}
