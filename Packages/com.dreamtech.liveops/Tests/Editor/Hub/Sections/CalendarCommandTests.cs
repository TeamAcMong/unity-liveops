using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Lệnh chiều sâu của màn Lịch (W5): copy id / copy đợt (JSON), dán tại con trỏ, nhân bản, ẩn / hiện / đưa làn và hoàn một
    /// mục về bản so. Hai đường vào đều được kiểm: <c>ExecuteCommandEvent</c> (phím Edit, R-15) trên cửa sổ hub THẬT, và lời gọi
    /// thẳng <see cref="CalendarCommandHandler"/> cho những lệnh mở popover (mở cửa sổ popover trong một lượt test batchmode là
    /// thứ duy nhất không kiểm bằng sự kiện được).
    /// </summary>
    [TestFixture]
    public sealed class CalendarCommandTests
    {
        private const int MaximumLayoutFrames = 60;
        private const int MaximumLayoutMilliseconds = 5000;
        private static readonly DateTime RangeStartUtc = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
        private const float DesignTrackWidth = 635f;

        private LiveOpsHubWindow _window;

        /// <summary>
        /// (V-23) Đóng cửa sổ rồi NHƯỜNG một khung trước khi test kế tiếp chạy: ở 2022.3 batchmode, cửa sổ vừa đóng còn giữ
        /// quyền nhận phím thêm một khung, nên test sau gọi <c>Focus()</c> một lần là rơi vào hư không — đúng cách lượt đầu của
        /// gói này làm <c>CalendarSectionTests.Frame03a_SelectedBarKeepsTimelineFocus</c> đỏ dù nó không đụng gì tới W5.
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
                yield return null;
            }
            LiveOpsHubTestServices.ReleaseAll();
            yield return null;
        }

        // ------------------------------------------------------------------------------------------------ lệnh Edit (R-15)

        /// <summary>
        /// ⌘C rồi ⌘V: clipboard nhận đúng object JSON của đợt, và dán vào chỗ trống làn tại con trỏ tạo một đợt mới với id đề
        /// xuất. Đi qua <c>ValidateCommand</c> + <c>ExecuteCommand</c> như Edit menu của Unity, không gọi tay handler.
        /// </summary>
        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator CopyThenPasteCommand_AddsEventAtCursor()
        {
            yield return OpenCalendar();
            CalendarSection section = Calendar();
            LiveOpsHubServices services = section.Services;
            int before = services.Session.Document.FixedEvents.Count;

            section.Presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            yield return ClaimFocus(section.Timeline);
            SendCommand(section.Timeline, LiveOpsTimelineElement.CopyCommand);

            Assert.IsTrue(section.CommandHandler.HasCopiedEvent, "⌘C trên một thanh cố định phải đưa đợt vào clipboard");
            StringAssert.Contains("hunt-0916-bonus", services.Clipboard.Text, "clipboard giữ object JSON của chính đợt đó");

            DateTime cursorUtc = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);
            section.Timeline.SetCursor(cursorUtc, "lava-quest", true);
            yield return null;
            SendCommand(section.Timeline, LiveOpsTimelineElement.PasteCommand);

            Assert.AreEqual(before + 1, services.Session.Document.FixedEvents.Count, "⌘V tại con trỏ thêm đúng một đợt");
            Assert.AreNotEqual(string.Empty, section.Presenter.SelectedBarKey, "đợt vừa dán được chọn để sửa tiếp");
        }

        /// <summary>⌘⌫ trên đợt chưa đăng, chưa bắt đầu: xoá ngay, không hộp (Hình 12 khung 8) — đường phím phải giống đường menu.</summary>
        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator DeleteCommand_RemovesUnpublishedUpcomingEvent()
        {
            yield return OpenCalendar();
            CalendarSection section = Calendar();
            section.Presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            section.Timeline.Focus();
            yield return null;

            SendCommand(section.Timeline, LiveOpsTimelineElement.DeleteCommand);

            Assert.IsFalse(section.Services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey,
                out FixedLiveEventEntry _), "⌘⌫ xoá ngay đợt chưa đăng và chưa bắt đầu");
        }

        /// <summary>⌘D không tạo đợt ngay: nó mở popover Thêm đợt ở bước xem lại với đích đã tính [FD §3.9].</summary>
        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator DuplicateCommand_RequestsDuplicateWithoutEditingDocument()
        {
            yield return OpenCalendar();
            CalendarSection section = Calendar();
            int before = section.Services.Session.Document.FixedEvents.Count;
            List<DateTime> targets = new List<DateTime>();
            section.CommandHandler.DuplicateRequested += (entry, targetStartUtc) => targets.Add(targetStartUtc);

            section.Presenter.SetSelectedBarKey(LiveOpsDesignSample.LavaQuestMidEntryKey);
            yield return ClaimFocus(section.Timeline);
            SendCommand(section.Timeline, LiveOpsTimelineElement.DuplicateCommand);

            Assert.AreEqual(1, targets.Count, "⌘D hỏi nhân bản đúng một lần");
            Assert.AreEqual(before, section.Services.Session.Document.FixedEvents.Count,
                "nhân bản KHÔNG tự thêm đợt — người dùng còn phải xem lại id ở bước 3");
        }

        /// <summary>(V-10) ⌘+kéo chỗ trống: pha CHỐT mở popover Thêm đợt ở bước xem lại, pha xem trước thì không.</summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void CommandDragCreate_OpensAddEventStep3()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarCommandHandler handler = CreateHandler(services, out CalendarTimelinePresenter _);
            List<DateTime> requested = new List<DateTime>();
            handler.AddEventRequested += (laneTypeId, startUtc, endUtc) => requested.Add(startUtc);

            DateTime startUtc = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);
            handler.HandleIntent(new CreateByDragIntent("treasure-hunt", startUtc, startUtc.AddHours(24),
                LiveOpsTimelineGesturePhase.Preview));
            Assert.AreEqual(0, requested.Count, "mỗi bước di chuột mở một popover là hàng chục cửa sổ — chỉ pha chốt mới mở");

            handler.HandleIntent(new CreateByDragIntent("treasure-hunt", startUtc, startUtc.AddHours(24),
                LiveOpsTimelineGesturePhase.Commit));
            Assert.AreEqual(1, requested.Count, "thả chuột mở popover Thêm đợt đúng một lần");
            Assert.AreEqual(startUtc, requested[0]);

            handler.HandleIntent(new CreateByDragIntent("weekly-pass", startUtc, startUtc.AddHours(24),
                LiveOpsTimelineGesturePhase.Commit));
            Assert.AreEqual(1, requested.Count, "làn lặp không tạo đợt cố định được — Làn lặp sửa ở Luật lặp");
        }

        // ------------------------------------------------------------------------------------------------ làn (V-12)

        /// <summary>
        /// (V-12) Đưa làn lên/xuống là MỘT Undo group và KHÔNG đổi JSON: thứ tự làn là cách nhìn của người sửa lịch, game không
        /// đọc nó. sha của bản xuất phải giữ nguyên từng ký tự — đổi thứ tự làn mà sha nhảy là một lần đăng lại vô nghĩa.
        /// </summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void LaneHeaderMenu_MoveUpDown_OneUndoGroup_ShaUnchanged()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarCommandHandler handler = CreateHandler(services, out CalendarTimelinePresenter presenter);
            List<LiveOpsToastModel> toasts = new List<LiveOpsToastModel>();
            presenter.ToastRequested += toast => toasts.Add(toast);
            string shaBefore = services.Session.Publish.CurrentJson.Sha256Hex;
            string second = services.Session.Document.EventTypes[1].TypeId;

            Assert.IsTrue(handler.MoveLane(second, MoveLaneIntent.Up), "làn thứ hai đưa lên được");

            Assert.AreEqual(second, services.Session.Document.EventTypes[0].TypeId, "làn đã lên đầu");
            Assert.AreEqual(1, toasts.Count, "một lần đưa làn = một toast");
            Assert.IsTrue(toasts[0].HasUndo, "và đúng một bước Undo");
            StringAssert.Contains(second, toasts[0].Message);
            Assert.AreEqual(shaBefore, services.Session.Publish.CurrentJson.Sha256Hex,
                "đưa làn KHÔNG đổi JSON nên sha xuất giữ nguyên (V-12)");

            Assert.IsTrue(handler.MoveLane(second, MoveLaneIntent.Down), "và đưa xuống lại được");
            Assert.AreEqual(second, services.Session.Document.EventTypes[1].TypeId);
            Assert.AreEqual(shaBefore, services.Session.Publish.CurrentJson.Sha256Hex);
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void MoveLane_AtEdge_IsRefusedSoMenuCanSayWhy()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarCommandHandler handler = CreateHandler(services, out CalendarTimelinePresenter _);
            string first = services.Session.Document.EventTypes[0].TypeId;
            int last = services.Session.Document.EventTypes.Count - 1;
            string bottom = services.Session.Document.EventTypes[last].TypeId;

            Assert.IsFalse(handler.CanMoveLane(first, MoveLaneIntent.Up));
            Assert.IsFalse(handler.MoveLane(first, MoveLaneIntent.Up));
            Assert.IsFalse(handler.CanMoveLane(bottom, MoveLaneIntent.Down));
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void HideLaneThenShowAll_KeepsOneListOfHiddenLanes()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarCommandHandler handler = CreateHandler(services, out CalendarTimelinePresenter presenter);

            Assert.IsTrue(handler.HideLane("lava-quest"));
            Assert.IsFalse(handler.HideLane("lava-quest"), "ẩn hai lần không đếm thành hai làn ẩn");
            Assert.AreEqual(1, presenter.HiddenLanes.Count);

            Assert.IsTrue(handler.ShowAllLanes());
            Assert.AreEqual(0, presenter.HiddenLanes.Count);
            Assert.IsFalse(handler.ShowAllLanes(), "không còn làn ẩn thì không phát toast rỗng");
        }

        // ------------------------------------------------------------------------------------------------ hoàn về bản so

        /// <summary>Hoàn về bản đã đăng thay ĐÚNG một mục — mọi thay đổi khác của nháp phải còn nguyên.</summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void RevertToCompare_ReplacesOnlyThatEntry()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarCommandHandler handler = CreateHandler(services, out CalendarTimelinePresenter presenter);
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry original));
            // Bản đã đăng đọc lại từ JSON của dấu nên EntryKey của nó KHÁC nháp — mục tương ứng tìm theo eventId.
            FixedLiveEventEntry baseline = BaselineOf(services, original.EventId);
            Assert.IsNotNull(baseline, "lịch mẫu đã có dấu đã đăng chứa lava-quest-2026-09b");
            string otherEndBefore = OtherEndTextOf(services);
            presenter.ApplyEdit(new ReplaceFixedEventEdit(original.WithTimes(original.StartUtcText, "2026-09-25T00:00:00Z")),
                LiveOpsEditOperation.ChangeFixedEventTimes, original.EntryKey, "test", string.Empty);

            Assert.IsTrue(handler.CanRevertToCompare(LiveOpsDesignSample.LavaQuestMidEntryKey));
            Assert.IsTrue(handler.RevertToCompare(LiveOpsDesignSample.LavaQuestMidEntryKey));

            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry reverted), "mục giữ nguyên EntryKey — hoàn về không phải là xoá rồi thêm");
            Assert.AreEqual(baseline.EndUtcText, reverted.EndUtcText, "mục đã về đúng bản đã đăng");
            Assert.AreEqual(otherEndBefore, OtherEndTextOf(services), "và KHÔNG đụng mục nào khác của nháp");
        }

        private static FixedLiveEventEntry BaselineOf(LiveOpsHubServices services, string eventId)
        {
            LiveEventCalendarDocument compare = services.Session.Publish.CompareDocument;
            if (compare == null) return null;
            for (int index = 0; index < compare.FixedEvents.Count; index++)
            {
                if (string.Equals(compare.FixedEvents[index].EventId, eventId, StringComparison.Ordinal))
                {
                    return compare.FixedEvents[index];
                }
            }
            return null;
        }

        private static string OtherEndTextOf(LiveOpsHubServices services)
        {
            return services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey,
                out FixedLiveEventEntry other) ? other.EndUtcText : string.Empty;
        }

        /// <summary>Đợt chỉ có trong nháp thì không có gì để hoàn về — menu phải khoá mục, không im lặng không làm gì.</summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void RevertToCompare_UnknownEntryIsRefused()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarCommandHandler handler = CreateHandler(services, out CalendarTimelinePresenter _);

            Assert.IsFalse(handler.CanRevertToCompare("entry-không-có-trong-bản-đăng"));
            Assert.IsFalse(handler.RevertToCompare("entry-không-có-trong-bản-đăng"));
        }

        // ------------------------------------------------------------------------------------------------ nhân bản

        /// <summary>Đích nhân bản = khoảng cách tới đợt cùng loại TRƯỚC đó (09a → 09b là 7 ngày), không phải một hằng số [FD §3.9].</summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void DuplicateTarget_UsesGapToPreviousSameTypeEvent()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarCommandHandler handler = CreateHandler(services, out CalendarTimelinePresenter _);

            Assert.IsTrue(handler.TryGetDuplicateTarget(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry entry, out DateTime targetStartUtc, out TimeSpan offset));
            entry.TryGetStartUtc(out DateTime startUtc);
            Assert.AreEqual(startUtc + offset, targetStartUtc);
            Assert.AreEqual(TimeSpan.FromDays(7), offset, "lava-quest-2026-09a → 09b cách nhau 7 ngày");
        }

        /// <summary>Clipboard bị thứ khác ghi đè giữa chừng: Dán phải TẮT, không dán lại một đợt mà người dùng không còn giữ.</summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void HasCopiedEvent_FalseWhenClipboardChanged()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarCommandHandler handler = CreateHandler(services, out CalendarTimelinePresenter _);

            Assert.IsTrue(handler.CopyEventJson(LiveOpsDesignSample.HuntBonusEntryKey));
            Assert.IsTrue(handler.HasCopiedEvent);

            services.Clipboard.Text = "một đoạn chữ khác";
            Assert.IsFalse(handler.HasCopiedEvent);
            Assert.IsFalse(handler.PasteAt("lava-quest", RangeStartUtc));
        }

        // ------------------------------------------------------------------------------------------------ dựng

        private CalendarSection Calendar()
        {
            foreach (IHubSection section in ((IHubHost)_window).Sections)
            {
                CalendarSection calendar = section as CalendarSection;
                if (calendar != null) return calendar;
            }
            Assert.Fail("cửa sổ hub không có màn Lịch");
            return null;
        }

        /// <summary>
        /// (V-23) Đòi lại focus MỖI KHUNG tới khi element thật sự giữ nó: ở 2022.3 batchmode lần <c>Focus()</c> đầu có thể rơi
        /// vào hư không khi cửa sổ thử chưa là cửa sổ nhận phím, và lệnh Edit gửi sau đó sẽ không tới được timeline.
        /// </summary>
        private IEnumerator ClaimFocus(VisualElement target)
        {
            int frames = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (target.panel == null || target.panel.focusController.focusedElement != target)
            {
                target.Focus();
                frames++;
                if (frames > MaximumLayoutFrames && stopwatch.ElapsedMilliseconds > MaximumLayoutMilliseconds)
                {
                    Assert.Fail("timeline không nhận được focus sau 60 khung và 5 giây — lệnh Edit sẽ không tới nơi");
                }
                yield return null;
            }
        }

        /// <summary>
        /// Validate rồi Execute như Edit menu của Unity ([API §12.1]), gửi THẲNG vào timeline. Không gửi qua
        /// <c>EditorWindow.SendEvent</c>: ở 2022.3 batchmode việc cửa sổ thử có phải cửa sổ nhận lệnh hay không phụ thuộc cửa sổ
        /// vừa đóng ở test trước, nên cùng một test xanh ở 6000.6 và đỏ ở 2022.3 (đúng thứ lượt đầu của gói này gặp). Đường đi
        /// vào element vẫn là <c>ValidateCommandEvent</c>/<c>ExecuteCommandEvent</c> thật của R-15, không gọi tay handler.
        /// </summary>
        private static void SendCommand(VisualElement target, string commandName)
        {
            Event commandEvent = UnityEditor.EditorGUIUtility.CommandEvent(commandName);
            using (ValidateCommandEvent validate = ValidateCommandEvent.GetPooled(
                new Event { type = EventType.ValidateCommand, commandName = commandName }))
            {
                validate.target = target;
                target.SendEvent(validate);
            }
            using (ExecuteCommandEvent execute = ExecuteCommandEvent.GetPooled(commandEvent))
            {
                execute.target = target;
                target.SendEvent(execute);
            }
        }

        private IEnumerator OpenCalendar()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            _window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
            _window.position = new Rect(0, 0, 1280, 760);
            yield return WaitForLayout(_window.rootVisualElement);
        }

        private static IEnumerator WaitForLayout(VisualElement element)
        {
            int frames = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!LiveOpsHubWindowTestScope.HasLayout(element))
            {
                frames++;
                if (frames > MaximumLayoutFrames && stopwatch.ElapsedMilliseconds > MaximumLayoutMilliseconds)
                {
                    Assert.Fail("cửa sổ hub không có layout sau 60 khung và 5 giây — test UI phải chạy không -nographics");
                }
                yield return null;
            }
            yield return null;
            yield return null;
        }

        private static LiveOpsHubServices CreateServices()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.RunCheckToCompletion();
            return services;
        }

        private static CalendarCommandHandler CreateHandler(LiveOpsHubServices services, out CalendarTimelinePresenter presenter)
        {
            presenter = new CalendarTimelinePresenter(services);
            DateTime rangeEndUtc = LiveOpsTimelineGeometry.AddTicksClamped(RangeStartUtc,
                LiveOpsTimelineGeometry.RangeLengthOf(LiveOpsTimelineZoom.ThreeWeeks).Ticks);
            presenter.BuildModel(RangeStartUtc, rangeEndUtc, DesignTrackWidth);
            CalendarCommandHandler handler = new CalendarCommandHandler(services, presenter);
            presenter.CommandHandler = handler;
            return handler;
        }
    }
}
