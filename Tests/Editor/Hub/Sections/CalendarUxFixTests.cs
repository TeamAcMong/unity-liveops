using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Đợt W8-UX, gói A (G-UX-CALENDAR): mỗi test ở đây KHOÁ một lỗi có thật mà người dùng giả gặp trong Unity GUI
    /// (<c>plan/uiux/USER-JOURNEY-FINDINGS.md</c>) hoặc soát ảnh (<c>plan/uiux/SCHEDULE-UI-AUDIT.md</c>). Tên test mang mã UX của
    /// bảng lỗi gộp (<c>plan/w8-ux/UX-FIX-PLAN.md</c> mục 1) để người đọc lần ngược được từ test về triệu chứng.
    /// <para>
    /// Test bố cục dựng cửa sổ hub THẬT (không <c>-nographics</c>): mọi lỗi của đợt này là lỗi bề rộng/chiều cao thật, đọc
    /// <c>resolvedStyle</c> trên panel giả sẽ ra NaN và test xanh giả.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class CalendarUxFixTests
    {
        private const int MaximumLayoutFrames = 60;
        private const int MaximumLayoutMilliseconds = 5000;
        private const float DesignTrackWidth = 635f;

        private static readonly DateTime RangeStartUtc = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime WhileMidQuestRunningUtc = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);

        private LiveOpsHubWindow _window;
        private ScriptedLiveOpsHubConfirmationPresenter _confirmation;
        private ManualLiveOpsClock _clock;

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

        // ============================================================================================ UX-01 gốc màn phải giãn

        /// <summary>
        /// (UX-01 · C1, C2, UJ-26) Chuỗi gốc màn → main → split → cột timeline phải lấp chiều cao thân màn KỂ CẢ khi chưa chọn
        /// đợt. Ở 820px inspector thành drawer <c>position:absolute</c>, nên nếu một mắt xích không <c>flex-grow</c> thì cả thân
        /// sụp về 0 và người dùng thấy một màn trắng — đúng thứ UJ-26 chụp được.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux01_RootChain_FillsSectionHeight_WithoutSelection()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 820, 560);
            VisualElement view = SectionView();
            float bodyHeight = _window.SectionBody.resolvedStyle.height;
            Assert.Greater(bodyHeight, 100f, "thân màn của cửa sổ phải có chiều cao thật trước khi đo màn Lịch");

            AssertFillsHeight(view, bodyHeight, "gốc màn (view của section)");
            AssertFillsHeight(view.Q(LiveOpsHubPaths.CalendarElementNames.Main), bodyHeight, "calendar-main");
            AssertFillsHeight(view.Q(LiveOpsHubPaths.CalendarElementNames.Split), bodyHeight, "calendar-split");
            AssertFillsHeight(view.Q(LiveOpsHubPaths.CalendarElementNames.TimelineColumn), bodyHeight, "calendar-timeline-column");
        }

        // ============================================================================================ UX-02/UX-03 dựng lại trục

        /// <summary>
        /// (UX-02 · UJ-01, UJ-23) ⌘+lăn đổi thang đo của element; màn phải DỰNG LẠI model theo khoảng mới và KHÔNG được ép
        /// element về preset. Trước khi sửa, <c>OnTimelineRangeChanged</c> chỉ lưu giờ bắt đầu nên thước và thanh đứng yên.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux02_ContinuousZoom_RebuildsModelAndKeepsScale()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 1280, 760);
            CalendarSection section = Calendar();
            LiveOpsTimelineElement timeline = section.Timeline;
            double pixelsPerHourBeforeWheel = timeline.PixelsPerHour;

            // (R-04) Đi qua SỰ KIỆN bánh xe thật kèm phím lệnh, không gọi SetContinuousScale: đoạn mã mà UJ-01 tố nằm trong
            // `OnWheel` (đọc modifier, quy delta ra nấc, neo theo x của chuột) — gọi thẳng hàm là bỏ qua đúng đoạn đó.
            SendActionWheel(timeline, -LiveOpsTimelineElement.WheelDeltaPerNotch * 2f);
            yield return null;

            Assert.AreNotEqual(pixelsPerHourBeforeWheel, timeline.PixelsPerHour,
                "⌘+lăn phải đổi thang đo của trục — không đổi nghĩa là sự kiện bánh xe không tới nơi");
            Assert.IsTrue(timeline.IsContinuousScale, "màn vẽ lại KHÔNG được ép trục về preset sau khi người dùng ⌘+lăn");
            LiveOpsTimelineModel model = section.Presenter.Model;
            Assert.IsNotNull(model, "vẽ lại phải dựng model mới");
            AssertSameMinute(timeline.RangeStartUtc, model.Geometry.RangeStartUtc, "mốc trái của model phải là mốc của trục");
            AssertSameMinute(timeline.RangeEndUtc, model.Geometry.RangeEndUtc, "mốc phải của model phải là mốc của trục");
        }

        /// <summary>
        /// (UX-03 · UJ-02) Kéo rộng cửa sổ: bề rộng track đổi ⇒ model phải dựng lại theo bề rộng MỚI. Trước khi sửa, model giữ
        /// nguyên 635px nên ở 1440 trục chỉ vẽ trong hai phần ba màn hình.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux03_WiderWindow_RebuildsModelToTrackWidth()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 820, 560);
            CalendarSection section = Calendar();
            float narrowTrackWidth = section.Timeline.TrackWidth;

            _window.position = new Rect(0, 0, 1440, 900);
            yield return WaitForLayout(_window.rootVisualElement);
            yield return null;
            yield return null;

            float trackWidth = section.Timeline.TrackWidth;
            Assert.Greater(trackWidth, narrowTrackWidth + 100f,
                "cửa sổ rộng thêm 620px thì track phải rộng theo, không giữ số đo của lần dựng đầu");
            Assert.AreEqual(trackWidth, section.Presenter.Model.Geometry.TrackWidth, 1f,
                "model phải dựng theo bề rộng track THẬT, không giữ số đo của lần dựng đầu");
        }

        // ============================================================================================ UX-04 toast giờ hỏng

        /// <summary>
        /// (UX-04 · UJ-04) Giờ không đọc được thì câu toast phải NÓI điều đó, không in mốc mặc định 1/1 00:00 — câu cũ tự mâu
        /// thuẫn với chính lỗi mà hub vừa báo.
        /// </summary>
        [Test]
        public void Ux04_ToastForUnreadableTime_NeverShowsJanuaryFirst()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey,
                out FixedLiveEventEntry before));
            FixedLiveEventEntry broken = before.WithTimes("2026-09-1", string.Empty);

            string message = presenter.DragToastMessage(before, broken);

            StringAssert.DoesNotContain(services.Format.ShortDateTime(default(DateTime)), message,
                "giờ không đọc được không được in thành 1/1 00:00");
            StringAssert.Contains(LiveOpsHubStrings.CalendarDepthUnreadableTimeText, message,
                "câu toast phải nói thẳng là giờ chưa đọc được");
        }

        // ============================================================================================ UX-05 hover card khi kéo

        /// <summary>
        /// (UX-05 · UJ-05) Đang kéo thanh thì hover card phải TẮT hẳn và bật lại khi nhả: thẻ bật giữa lúc kéo che đúng thanh
        /// người dùng đang nhắm, và vì thanh cũ bị thay bởi element mới nên PointerLeave của nó không bao giờ tới.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux05_HoverCard_SuppressedWhileDragging()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 1280, 760);
            CalendarSection section = Calendar();
            LiveOpsHoverCardHost host = section.HoverCardHost;
            Assert.IsFalse(host.IsSuppressed, "chưa kéo thì hover card vẫn làm việc bình thường");

            DateTime newStartUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
            section.Presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, newStartUtc,
                newStartUtc.AddHours(36), LiveOpsTimelineGesturePhase.Preview));
            Assert.IsTrue(host.IsSuppressed, "bắt đầu kéo thì hover card phải tắt");

            section.Presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, newStartUtc,
                newStartUtc.AddHours(36), LiveOpsTimelineGesturePhase.Commit));
            Assert.IsFalse(host.IsSuppressed, "nhả chuột xong hover card phải làm việc lại");
            Assert.IsFalse(host.IsVisible, "và không được treo lại một thẻ của thanh đã bị thay");
        }

        // ============================================================================================ UX-06 hỏi trước khi áp

        /// <summary>
        /// (UX-06 · UJ-11) Rút ngắn đợt đang chạy: hộp phải hỏi TRƯỚC khi trục/toast/status bar nói "Đã dời". Trước khi sửa,
        /// presenter commit + toast rồi mới hỏi, nên người dùng đọc kết quả trong lúc hộp còn đang chờ.
        /// </summary>
        [Test]
        public void Ux06_ShortenRunningDrag_AsksBeforeApplying()
        {
            LiveOpsHubServices services = CreateServices();
            _clock.Set(WhileMidQuestRunningUtc);
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            List<Action> deferred = new List<Action>();
            List<LiveOpsToastModel> toasts = new List<LiveOpsToastModel>();
            presenter.DeferConfirmation = action => deferred.Add(action);
            presenter.ToastRequested += toast => toasts.Add(toast);
            _confirmation.Enqueue(LiveOpsConfirmResult.Destructive);

            DragShorter(presenter);

            Assert.AreEqual(1, deferred.Count, "hộp vẫn chỉ mở SAU khi chuột đã nhả (SP-2 (a))");
            Assert.AreEqual(0, toasts.Count, "chưa trả lời hộp thì chưa được báo 'Đã dời'");
            // Nháp continuous edit CÒN MỞ = chưa có bước Undo nào, chưa gộp gì. Tài liệu vẫn mang giá trị xem trước vì đó chính
            // là bản nháp người dùng đang nhìn — thứ phải chưa xảy ra là COMMIT, không phải bản xem trước.
            Assert.IsTrue(services.Session.IsContinuousEditOpen, "chưa trả lời hộp thì nháp chưa được gộp thành bước Undo");

            deferred[0]();

            Assert.AreEqual(1, _confirmation.Requests.Count);
            Assert.IsFalse(services.Session.IsContinuousEditOpen, "trả lời xong thì nháp phải đóng");
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry afterAnswer));
            Assert.AreEqual("2026-09-18T12:00:00Z", afterAnswer.EndUtcText, "chọn nút phá huỷ thì mới ghi");
            Assert.AreEqual(1, toasts.Count, "và toast chỉ chạy sau lựa chọn");
        }

        /// <summary>(UX-06 · UJ-11) Chọn "Giữ": không toast, không bước Undo, tài liệu y như trước khi kéo.</summary>
        [Test]
        public void Ux06_ShortenRunningDrag_KeepLeavesNoToastNoUndoStep()
        {
            LiveOpsHubServices services = CreateServices();
            _clock.Set(WhileMidQuestRunningUtc);
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            List<Action> deferred = new List<Action>();
            List<LiveOpsToastModel> toasts = new List<LiveOpsToastModel>();
            presenter.DeferConfirmation = action => deferred.Add(action);
            presenter.ToastRequested += toast => toasts.Add(toast);
            _confirmation.Enqueue(LiveOpsConfirmResult.Safe);

            DragShorter(presenter);
            deferred[0]();

            Assert.AreEqual(0, toasts.Count, "chọn Giữ thì không có toast nào để người dùng phải đọc rồi bỏ qua");
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry kept));
            Assert.AreEqual("2026-09-20T00:00:00Z", kept.EndUtcText, "và giờ kết thúc trở về đúng giá trị cũ");
        }

        // ============================================================================================ UX-07 drawer ở --medium

        /// <summary>(UX-07 · UJ-06) 820px chưa chọn đợt: drawer inspector phải ẨN, không đứng chắn 280px cạnh trục.</summary>
        [UnityTest]
        public IEnumerator Ux07_Medium_NoSelection_HidesInspectorDrawer()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 820, 560);
            VisualElement inspector = SectionView().Q(LiveOpsHubPaths.CalendarElementNames.Inspector);

            Assert.IsTrue(inspector.ClassListContains(LiveOpsHubClassNames.CalendarHidden),
                "chưa chọn đợt thì drawer không có gì để hiện");
        }

        /// <summary>
        /// (UX-07, lệch thiết kế V-39) Drawer mở thì cột timeline nhận lề phải bằng bề rộng drawer: mọi chip/nhãn neo phải còn
        /// nhìn thấy và bấm được, thay vì bị drawer phủ lên.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux07_Medium_DrawerOpen_TimelineColumnKeepsRoom()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 820, 560);
            CalendarSection section = Calendar();
            section.Presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            yield return null;
            yield return null;

            VisualElement inspector = SectionView().Q(LiveOpsHubPaths.CalendarElementNames.Inspector);
            VisualElement split = SectionView().Q(LiveOpsHubPaths.CalendarElementNames.Split);
            Assert.IsFalse(inspector.ClassListContains(LiveOpsHubClassNames.CalendarHidden), "chọn đợt thì drawer mở");
            Assert.IsTrue(split.ClassListContains(LiveOpsHubClassNames.CalendarDepthDrawerOpen),
                "split phải mang class 'drawer đang mở' để USS chừa chỗ");
            Assert.GreaterOrEqual(split.resolvedStyle.marginRight, 200f,
                "cột timeline phải chừa chỗ cho drawer, không để drawer phủ lên chip neo phải");
        }

        // ============================================================================================ UX-11 nâng toast

        /// <summary>
        /// (UX-11 · C8, V8) Dựng lại view màn Lịch: view CŨ detach SAU khi view mới đã bật class nâng toast, nên tắt class theo
        /// detach của bất kỳ gốc nào là xoá mất yêu cầu của view đang sống — toast rơi xuống dưới minimap.
        /// </summary>
        [Test]
        public void Ux11_RaisedToastClass_SurvivesSectionRebuild()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            bool isRaised = false;
            services.Bus.ContentClassRequested += (className, enabled) =>
            {
                if (string.Equals(className, LiveOpsHubClassNames.ContentRaisedToast, StringComparison.Ordinal)) isRaised = enabled;
            };
            CalendarSection section = new CalendarSection(services);
            VisualElement firstRoot = section.CreateView();
            section.CreateView();

            section.HandleDetachForTest(firstRoot);

            Assert.IsTrue(isRaised, "view cũ rời panel không được tắt class nâng toast của view đang sống");
        }

        // ============================================================================================ UX-12 chip làn ẩn

        /// <summary>(UX-12 · UJ-07) Chip "Đang ẩn n làn · Hiện" phải có NÚT bấm được, không phải một nhãn trông như nút.</summary>
        [UnityTest]
        public IEnumerator Ux12_HiddenLanesChip_ShowButtonRestoresLanes()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 1280, 760);
            CalendarSection section = Calendar();
            section.CommandHandler.HideLane("lava-quest");
            yield return null;

            Assert.AreEqual(1, section.Presenter.HiddenLanes.Count, "ẩn một làn trước đã");
            Button showButton = section.Toolbar.HiddenLanesShowButton;
            Assert.IsNotNull(showButton, "chip phải có nút 'Hiện'");
            // (R-04) Trước khi bấm phải chắc nút NHẬN được chuột: gửi thẳng sự kiện vào Button vẫn xanh kể cả khi nút bị một lớp
            // khác phủ hoặc để pickingMode = Ignore — tức vẫn xanh trong đúng ca UJ-07 "bấm mãi không ăn".
            AssertPickable(showButton, "nút 'Hiện' của chip làn ẩn");
            ClickWithPointer(showButton);
            yield return null;

            Assert.AreEqual(0, section.Presenter.HiddenLanes.Count, "bấm 'Hiện' phải đưa mọi làn trở lại");
        }

        // ============================================================================================ UX-14 kiểm nhanh khi kéo

        /// <summary>
        /// (UX-14 · UJ-13) Kiểm nhanh của readout chỉ được nêu phát hiện DÍNH đợt đang kéo và MỚI sinh ra vì bước kéo này. Trước
        /// khi sửa nó lấy phát hiện Bị bỏ đầu tiên của cả làn, nên kéo lava-quest-2026-09b lại đọc lỗi của lava-quest-2026-10.
        /// </summary>
        [Test]
        public void Ux14_QuickCheck_NamesOnlyDraggedEvent()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            List<string> tags = new List<string>();
            presenter.QuickCheckTagChanged += (text, health) => tags.Add(text);

            DateTime newStartUtc = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, newStartUtc,
                newStartUtc.AddDays(2), LiveOpsTimelineGesturePhase.Preview));

            Assert.Greater(tags.Count, 0, "mỗi bước xem trước phải phát một câu kiểm nhanh");
            string tag = tags[tags.Count - 1];
            StringAssert.DoesNotContain("lava-quest-2026-10", tag,
                "kiểm nhanh không được nêu lỗi của một đợt khác đã hỏng sẵn trên cùng làn");
            Assert.AreEqual(LiveOpsHubStrings.CalendarQuickCheckOkTag, tag,
                "bước kéo này không sinh lỗi mới nên tag phải là câu Ok");
        }

        // ============================================================================================ UX-15 menu chuột phải

        /// <summary>(UX-15 · UJ-18) Làn lặp không thêm được đợt cố định, nên mục "Thêm đợt cố định…" phải KHOÁ và nói lý do.</summary>
        [Test]
        public void Ux15_LaneHeaderMenu_RecurringLane_DisablesAddFixedEvent()
        {
            CalendarMenuContext context = new CalendarMenuContext
            {
                LaneTypeId = "sky-race",
                IsRecurringLane = true,
                HiddenLaneCount = 0,
            };

            CalendarMenuItem add = FindItem(CalendarContextMenus.ForLaneHeader(context), CalendarMenuItemId.AddForLane);

            Assert.IsFalse(add.IsEnabled, "làn lặp sinh đợt từ luật — không thêm đợt cố định vào đó được");
        }

        /// <summary>(UX-15 · UJ-18) Không làn nào ẩn thì "Hiện tất cả làn" không có gì để làm.</summary>
        [Test]
        public void Ux15_LaneHeaderMenu_NoHiddenLane_DisablesShowAll()
        {
            CalendarMenuContext context = new CalendarMenuContext { LaneTypeId = "lava-quest", HiddenLaneCount = 0 };

            CalendarMenuItem showAll = FindItem(CalendarContextMenus.ForLaneHeader(context), CalendarMenuItemId.ShowAllLanes);

            Assert.IsFalse(showAll.IsEnabled, "không có làn ẩn thì mục này là một lời hứa suông");
        }

        /// <summary>
        /// (UX-15 · UJ-18) Có dấu đã đăng nhưng đợt này CHƯA có trong bản đã đăng: lý do phải nói đúng điều đó, không nói
        /// "Chưa có dấu đã đăng".
        /// </summary>
        [Test]
        public void Ux15_RevertItem_NotInPublished_SaysTheRightReason()
        {
            CalendarMenuContext context = new CalendarMenuContext
            {
                BarKey = LiveOpsDesignSample.HuntBonusEntryKey,
                HasCompareDocument = true,
                IsInCompareDocument = false,
                CanRevertToCompare = false,
            };

            CalendarMenuItem revert = FindItem(CalendarContextMenus.ForFixedBar(context), CalendarMenuItemId.RevertToCompare);

            Assert.IsFalse(revert.IsEnabled);
            StringAssert.Contains(LiveOpsHubStrings.CalendarDepthMenuNotInPublishedReason, revert.Text,
                "có dấu đã đăng rồi thì lý do phải là 'đợt này chưa có trong bản đã đăng'");
        }

        /// <summary>(UX-15 · UJ-18) Nhãn nhân bản nêu khoảng nhảy bằng CHỮ ĐỦ ("7 ngày"), không viết tắt "(+7n)".</summary>
        [UnityTest]
        public IEnumerator Ux15_DuplicateMenuLabel_UsesFullWords()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 1280, 760);
            CalendarSection section = Calendar();

            CalendarMenuContext context = section.BuildMenuContextForTest(new LiveOpsTimelineHit(LiveOpsTimelineHitKind.Bar,
                LiveOpsDesignSample.HuntBonusEntryKey, LiveOpsTimelineBarRegion.Body, "treasure-hunt", null, Vector2.zero));

            Assert.IsTrue(context.CanDuplicate, "đợt mẫu này nhân bản được");
            // hunt-0914 bắt đầu 14/9 00:00, hunt-0916-bonus bắt đầu 16/9 12:00 ⇒ khoảng nhảy đề xuất là 2 ngày 12 giờ.
            Assert.AreEqual(section.Services.Format.Duration(TimeSpan.FromHours(60), false), context.DuplicateOffsetText,
                "menu dùng dạng đủ chữ, không dùng dạng viết tắt của readout");
        }

        // ============================================================================================ UX-26 tên bước kéo

        /// <summary>
        /// (UX-26 · UJ-10, UJ-22) Tên bước Undo phải nói ĐÚNG mép đã đổi — "Đổi …" chung chung làm status bar và toast nói hai
        /// việc khác nhau về cùng một thao tác.
        /// </summary>
        [Test]
        public void Ux26_DragUndoStepName_NamesTheEdgeMoved()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey,
                out FixedLiveEventEntry before));
            before.TryGetStartUtc(out DateTime startUtc);
            before.TryGetEndUtc(out DateTime endUtc);

            FixedLiveEventEntry endMoved = before.WithTimes(LiveEventUtcText.Format(startUtc),
                LiveEventUtcText.Format(endUtc.AddHours(6)));
            FixedLiveEventEntry startMoved = before.WithTimes(LiveEventUtcText.Format(startUtc.AddHours(-6)),
                LiveEventUtcText.Format(endUtc));

            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.CalendarDepthMoveEndEdgeUndoStepFormat, before.EventId),
                presenter.DragUndoStepName(before, endMoved), "kéo mép cuối = \"Đổi mép cuối …\"");
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.CalendarDepthMoveStartEdgeUndoStepFormat, before.EventId),
                presenter.DragUndoStepName(before, startMoved), "kéo mép đầu = \"Đổi mép đầu …\"");
        }

        // ============================================================================================ UX-28 hover card

        /// <summary>(UX-28 · V11, UJ-20) Hover card có cả thẻ để viết: thời lượng in dạng đủ chữ, không "1n 12g".</summary>
        [Test]
        public void Ux28_HoverCardDuration_NotAbbreviated()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            LiveOpsTimelineBarModel bar = presenter.FindBar(LiveOpsDesignSample.LavaQuestMidEntryKey);
            Assert.IsNotNull(bar);

            VisualElement card = CalendarHoverCardContent.Build(bar, null, services, services.Session.Document.LatestStamp);

            string expected = services.Format.Duration(bar.EndUtc - bar.StartUtc, false);
            Assert.IsTrue(ContainsText(card, expected),
                "hover card phải in thời lượng dạng đủ chữ '" + expected + "'");
        }

        // ============================================================================================ UX-29 toolbar hẹp

        /// <summary>
        /// (UX-29 · V5) Ở cửa sổ hẹp toolbar không còn chỗ cho menu Bắt lưới và nút "So với đã đăng (n)" — hai mục đó đã có
        /// đường lệnh trong menu ⋮.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux29_NarrowToolbar_HidesSnapAndCompare()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 820, 560);
            CalendarToolbar toolbar = Calendar().Toolbar;

            Assert.AreEqual(DisplayStyle.None, toolbar.SnapMenu.resolvedStyle.display, "menu Bắt lưới vào menu ⋮ khi hẹp");
            Assert.AreEqual(DisplayStyle.None, toolbar.CompareToggle.resolvedStyle.display, "nút So với vào menu ⋮ khi hẹp");
            Assert.AreEqual(DisplayStyle.Flex, toolbar.OverflowMenu.resolvedStyle.display, "và menu ⋮ phải hiện");
        }

        // ====================================================== UX-11 · R-01/R-05 nâng toast trên CỬA SỔ THẬT

        /// <summary>
        /// (UX-11 · C8, R-01) Đọc class trên <c>hub-content</c> của CỬA SỔ THẬT ở hai bề rộng. Test cũ chỉ nghe sự kiện của bus
        /// bằng một lambda nên khoá đúng giả thuyết chứ không khoá triệu chứng: ảnh chụp sau lượt sửa trước vẫn thiếu
        /// <c>liveops-hub-content--raised-toast</c> ở CẢ 10 ảnh, và ở 820×560 thì không còn lớp nâng nào nên toast rơi về 8px.
        /// Gốc thật: <c>LiveOpsHubWindow.CreateGUI</c> gọi <c>ShowSection</c> (bước 6) TRƯỚC <c>SubscribeServices</c> (bước 8),
        /// nên lần bật lớp trong <c>CreateView</c> rơi vào một bus chưa ai nghe.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux11_RaisedToastClass_IsOnRealWindow_AtBothWidths()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 1280, 760);
            VisualElement content = HubContent();

            Assert.IsTrue(content.ClassListContains(LiveOpsHubClassNames.ContentRaisedToast),
                "màn Lịch đang hiện thì cột nội dung phải mang lớp nâng toast, không thì toast nằm dưới minimap");
            Assert.IsTrue(content.ClassListContains(LiveOpsHubClassNames.CalendarDepthContentRaisedToastTall),
                "ở 1280 chân màn (minimap + chú giải + gợi ý) cao hơn 64px nên phải lên bậc cao");

            _window.position = new Rect(0, 0, 820, 560);
            yield return WaitForLayout(_window.rootVisualElement);
            yield return null;

            Assert.IsTrue(content.ClassListContains(LiveOpsHubClassNames.ContentRaisedToast),
                "thu hẹp cửa sổ không được làm mất lớp nâng toast — đây đúng là ảnh h13 của lượt trước");
        }

        /// <summary>
        /// (UX-11 · R-01/R-05) Và số đo thật: toast HIỆN ở 820 phải cách đáy ít nhất một bậc nâng. Class đúng mà USS thua độ đặc
        /// hiệu thì bảng class vẫn xanh trong khi người dùng vẫn thấy toast đè lên chú giải.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux11_VisibleToast_KeepsRaisedOffsetAtNarrowWidth()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 820, 560);
            CalendarSection section = Calendar();

            section.CommandHandler.HideLane("lava-quest");
            yield return null;
            yield return null;

            VisualElement toast = _window.HubRoot.Q(LiveOpsToast.ElementName);
            Assert.IsNotNull(toast, "ẩn làn phải bắn một toast");
            Assert.IsTrue(toast.ClassListContains(LiveOpsHubClassNames.ToastVisible), "toast đó phải đang hiện");
            // Đo bằng BỐ CỤC (`layout`, toạ độ trong cột nội dung) chứ không bằng `worldBound`: `worldBound` cộng cả `translate`
            // của hiệu ứng trượt lên 8px nên đo sớm vài khung sẽ hụt. Cũng không đọc `resolvedStyle.bottom` — Yoga trả số ÂM cho
            // `bottom` của element neo tuyệt đối.
            float lift = HubContent().layout.height - toast.layout.yMax;
            Assert.GreaterOrEqual(lift, CalendarSection.DefaultToastRaisePixels - 2f,
                "toast của màn Lịch phải nằm trên chân màn, không rơi về 8px (nâng đo được: " + lift + "px)");
        }

        // ====================================================== UX-11 · V8/R-02 ghi chú pane So với

        /// <summary>
        /// (UX-11 · V8, R-02) Ghi chú của pane "So với đã đăng" phải nằm NGAY DƯỚI danh sách. Trước khi sửa, thân pane
        /// <c>flex-grow: 1</c> đẩy ghi chú xuống tận đáy: số đo ảnh h11 là dòng cuối ở y≈276 còn ghi chú ở y=715 — 425px trống.
        /// </summary>
        [UnityTest]
        public IEnumerator V8_ComparePaneNote_SitsRightUnderTheList()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 1280, 760);
            CalendarSection section = Calendar();
            // Pane chỉ có dòng khi nháp KHÁC bản đã đăng — sửa một đợt trước, đúng như Hình 11 dựng trạng thái.
            Assert.IsTrue(section.Services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry entry));
            section.Services.Session.Apply(new ReplaceFixedEventEdit(entry.WithTimes(entry.StartUtcText, "2026-09-21T00:00:00Z")),
                "Dời mép cuối " + entry.EventId);
            yield return null;

            section.Toolbar.CompareToggle.value = true;
            yield return null;
            yield return null;

            Assert.Greater(section.ComparePane.Rows.Count, 0, "phải có ít nhất một dòng khác biệt để đo");
            VisualElement lastRow = section.ComparePane.Rows[section.ComparePane.Rows.Count - 1].Element;
            float gap = section.ComparePane.Note.worldBound.yMin - lastRow.worldBound.yMax;
            Assert.GreaterOrEqual(gap, 0f, "ghi chú không được chồng lên dòng cuối");
            Assert.LessOrEqual(gap, 40f,
                "ghi chú phải ngay dưới danh sách; khoảng trống lớn làm người đọc tưởng nó thuộc về thứ khác (đo được: " + gap + "px)");
        }

        // ====================================================== UX-02 · R-05 tab zoom sau khi lăn

        /// <summary>
        /// (UX-02 · UJ-23, R-05) Bấm tab zoom sau khi đã ⌘+lăn: khung MỚI phải lấy TÂM khung đang xem làm neo, không nhảy về một
        /// mốc mặc định. Đoạn <c>ApplyZoomPreset</c>/<c>CurrentAnchorUtc</c> thêm ở lượt trước chưa có test nào chạm tới.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux02_ZoomPresetTab_AnchorsOnTheViewCentre()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 1280, 760);
            CalendarSection section = Calendar();
            LiveOpsTimelineElement timeline = section.Timeline;
            // Dời khung ra XA "bây giờ" để phân biệt được hai lời giải (neo theo tâm khung hay rơi về bây giờ).
            DateTime farStartUtc = new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            timeline.SetContinuousScale(timeline.PixelsPerHour, farStartUtc);
            yield return null;
            DateTime anchorUtc = timeline.RangeStartUtc.AddTicks((timeline.RangeEndUtc - timeline.RangeStartUtc).Ticks / 2L);

            AssertPickable(section.Toolbar.ZoomTabs.TabAt(0), "tab zoom 'Ngày'");
            ClickWithPointer(section.Toolbar.ZoomTabs.TabAt(0));
            yield return null;
            yield return null;

            Assert.IsFalse(timeline.IsContinuousScale, "bấm tab preset thì trục trở lại preset");
            DateTime newCentreUtc = timeline.RangeStartUtc.AddTicks((timeline.RangeEndUtc - timeline.RangeStartUtc).Ticks / 2L);
            AssertSameMinute(anchorUtc, newCentreUtc, "tâm khung sau khi đổi preset phải là tâm khung người dùng đang nhìn");
        }

        // ====================================================== UX-14 · R-05 nhánh "đợt bị đè"

        /// <summary>
        /// (UX-14 · UJ-13, R-05) Nhánh thứ hai của bộ lọc kiểm nhanh: kéo đè lên một đợt khác làm ĐỢT ĐÓ bị bỏ — lỗi MỚI đó là
        /// hậu quả của chính bước kéo này nên readout phải nêu nó (nhánh này chưa có test nào).
        /// </summary>
        [Test]
        public void Ux14_QuickCheck_NamesTheEventThisDragBroke()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            List<string> tags = new List<string>();
            presenter.QuickCheckTagChanged += (text, health) => tags.Add(text);

            // lava-quest-2026-09b (17→20/9) kéo về 9→12/9: nó mở TRƯỚC lava-quest-2026-09a (10→13/9) nên 09a thành đợt bị bỏ.
            DateTime newStartUtc = new DateTime(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, newStartUtc,
                newStartUtc.AddDays(3), LiveOpsTimelineGesturePhase.Preview));

            Assert.Greater(tags.Count, 0, "mỗi bước xem trước phải phát một câu kiểm nhanh");
            string tag = tags[tags.Count - 1];
            Assert.AreNotEqual(LiveOpsHubStrings.CalendarQuickCheckOkTag, tag,
                "bước kéo này làm một đợt khác bị bỏ nên KHÔNG được báo Ok");
            StringAssert.Contains("lava-quest-2026-09a", tag, "và phải nêu đúng đợt vừa bị đè");
            StringAssert.DoesNotContain("lava-quest-2026-10", tag, "vẫn không được nêu lỗi có sẵn của đợt khác");
        }

        // ====================================================== UX-05 · R-06 dọn đích đã rời panel

        /// <summary>
        /// (UX-05 · UJ-05, R-06) Vẽ lại làn THAY cả làn: thanh cũ vẫn còn cha (làn cũ) nhưng cả cụm đã rời panel. Luật dọn cũ
        /// ("không cha VÀ không panel") bỏ lọt đúng ca này, nên thẻ của thanh đã chết treo lại che chỗ người dùng đang nhắm.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux05_HoverCard_PrunesBarWhoseLaneLeftThePanel()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 1280, 760);
            double nowSeconds = 10d;
            VisualElement host = new VisualElement();
            _window.HubRoot.Add(host);
            VisualElement lane = new VisualElement();
            host.Add(lane);
            VisualElement bar = new VisualElement();
            lane.Add(bar);
            LiveOpsHoverCardHost cardHost = new LiveOpsHoverCardHost(host, () => nowSeconds);
            cardHost.Attach(bar, () => new Label("lava-quest-2026-09b"));
            yield return null;

            cardHost.HandlePointerEnter(bar);
            nowSeconds += LiveOpsHoverCardHost.ShowDelayMilliseconds / 1000d;
            cardHost.Tick();
            Assert.IsTrue(cardHost.IsVisible, "thẻ phải đang hiện trước khi làn bị thay");

            host.Remove(lane);
            cardHost.PruneDetachedTargets();

            Assert.IsFalse(cardHost.IsVisible, "làn bị thay thì thẻ của thanh cũ phải tắt, dù thanh vẫn còn cha");
            Assert.AreEqual(0, cardHost.AttachedTargetCount, "và builder của thanh chết phải được dọn");
        }

        // ====================================================== R-12 sáu cỡ cửa sổ

        /// <summary>
        /// (R-12) Màn Lịch đụng đúng chỗ phụ thuộc breakpoint (<c>--medium</c> là dưới 1100px, không chỉ 820), nên đo ở CẢ SÁU cỡ
        /// của cổng đợt. Ca nặng nhất là 1024×700: vẫn là <c>--medium</c> nên drawer chiếm 280px, và nếu pane Danh sách 240px cũng
        /// mở thì track còn rất ít chỗ.
        /// </summary>
        [UnityTest]
        public IEnumerator Layout_SixWindowSizes_KeepTimelineUsable()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario, 1280, 760);
            CalendarSection section = Calendar();
            int[] widths = { 700, 820, 1024, 1280, 1440, 1920 };
            int[] heights = { 560, 560, 700, 760, 900, 1040 };

            for (int index = 0; index < widths.Length; index++)
            {
                _window.position = new Rect(0, 0, widths[index], heights[index]);
                yield return WaitForLayout(_window.rootVisualElement);
                yield return null;
                string what = widths[index] + "×" + heights[index];

                VisualElement view = SectionView();
                float bodyHeight = _window.SectionBody.resolvedStyle.height;
                Assert.Greater(bodyHeight, 100f, what + ": thân màn phải có chiều cao thật");
                AssertFillsHeight(view.Q(LiveOpsHubPaths.CalendarElementNames.Main), bodyHeight, what + " calendar-main");
                AssertFillsHeight(view.Q(LiveOpsHubPaths.CalendarElementNames.TimelineColumn), bodyHeight,
                    what + " calendar-timeline-column");
                Assert.Greater(section.Timeline.TrackWidth, 120f,
                    what + ": track phải còn chỗ vẽ — dưới 120px thì thước không đọc được");
                Assert.LessOrEqual(view.worldBound.xMax, _window.SectionBody.worldBound.xMax + 1f,
                    what + ": màn không được tràn ra ngoài thân cửa sổ");
            }
        }

        // ============================================================================================ tiện ích

        private static void AssertFillsHeight(VisualElement element, float bodyHeight, string what)
        {
            Assert.IsNotNull(element, what + " phải có trong cây");
            // (R-13) Ngưỡng 0,85 chứ không phải 0,5: số đo thật ở 820×560 là 457/514 ≈ 89% ở cả hai bản Unity, nên 0,5 cho lọt
            // một hồi quy làm cột timeline còn 60% chiều cao — đúng loại hồi quy mà test này sinh ra để chặn.
            Assert.GreaterOrEqual(element.resolvedStyle.height, bodyHeight * 0.85f,
                what + " phải lấp chiều cao thân màn, không sụp về chiều cao nội tại");
        }

        private static void AssertSameMinute(DateTime expected, DateTime actual, string because)
        {
            Assert.LessOrEqual(Math.Abs((expected - actual).TotalMinutes), 1d,
                because + " (chờ " + expected.ToString("O", CultureInfo.InvariantCulture) + ", nhận "
                + actual.ToString("O", CultureInfo.InvariantCulture) + ")");
        }

        private static bool ContainsText(VisualElement element, string text)
        {
            foreach (Label label in element.Query<Label>().ToList())
            {
                if (label.text != null && label.text.Contains(text)) return true;
            }
            return false;
        }

        private static CalendarMenuItem FindItem(IReadOnlyList<CalendarMenuItem> items, CalendarMenuItemId id)
        {
            for (int index = 0; index < items.Count; index++)
            {
                if (items[index].Id == id) return items[index];
            }
            Assert.Fail("menu thiếu mục " + id);
            return null;
        }

        /// <summary>
        /// (R-04) Bấm bằng SỰ KIỆN CHUỘT thật: <c>Clickable</c> của Button nghe PointerDown/PointerUp, nên đường này chạy đúng
        /// đoạn mã mà người dùng chạm tới. <c>NavigationSubmitEvent</c> (đường cũ) gửi thẳng vào Button nên vẫn xanh kể cả khi nút
        /// bị phủ — dùng kèm <see cref="AssertPickable"/> để bắt đúng ca "bấm mãi không ăn".
        /// </summary>
        private static void ClickWithPointer(VisualElement element)
        {
            Vector2 point = element.worldBound.center;
            Assert.IsFalse(float.IsNaN(point.x), "phần tử phải có layout thật trước khi bấm");
            SendPointer<PointerDownEvent>(element, EventType.MouseDown, point);
            SendPointer<PointerUpEvent>(element, EventType.MouseUp, point);
        }

        /// <summary>
        /// Dựng sự kiện con trỏ từ một <c>Event</c> IMGUI: có vậy <c>button</c>, <c>position</c> và <c>clickCount</c> mới đúng —
        /// <c>GetPooled()</c> không tham số để <c>button = -1</c> nên <c>Clickable</c> bỏ qua.
        /// </summary>
        private static void SendPointer<TEvent>(VisualElement element, EventType eventType, Vector2 point)
            where TEvent : PointerEventBase<TEvent>, new()
        {
            Event systemEvent = new Event
            {
                type = eventType,
                mousePosition = point,
                button = 0,
                clickCount = 1,
            };
            using (TEvent pointerEvent = PointerEventBase<TEvent>.GetPooled(systemEvent))
            {
                pointerEvent.target = element;
                element.SendEvent(pointerEvent);
            }
        }

        /// <summary>(R-04) Panel có trả về đúng phần tử này (hoặc con của nó) khi bấm vào giữa nó không.</summary>
        private static void AssertPickable(VisualElement element, string what)
        {
            Assert.IsNotNull(element.panel, what + " phải nằm trong panel thật");
            Assert.Greater(element.worldBound.width, 0f, what + " phải có bề rộng thật");
            VisualElement picked = element.panel.Pick(element.worldBound.center);
            Assert.IsNotNull(picked, what + " không nhận được chuột — có gì đó phủ lên hoặc pickingMode = Ignore");
            for (VisualElement walk = picked; walk != null; walk = walk.parent)
            {
                if (ReferenceEquals(walk, element)) return;
            }
            Assert.Fail(what + " bị '" + picked.name + "' phủ mất: bấm vào giữa nút lại trúng phần tử khác");
        }

        /// <summary>
        /// (R-04) ⌘+lăn THẬT trên trục: dựng <c>Event</c> IMGUI dạng ScrollWheel kèm phím lệnh rồi gửi vào element, đúng đường mà
        /// <c>LiveOpsTimelineElement.OnWheel</c> đọc (modifier + delta + toạ độ chuột để neo).
        /// </summary>
        private static void SendActionWheel(LiveOpsTimelineElement timeline, float deltaY)
        {
            Vector2 point = timeline.Ruler.Track.worldBound.center;
            Event systemEvent = new Event
            {
                type = EventType.ScrollWheel,
                mousePosition = point,
                delta = new Vector2(0f, deltaY),
                modifiers = Application.platform == RuntimePlatform.OSXEditor ? EventModifiers.Command : EventModifiers.Control,
            };
            using (WheelEvent wheel = WheelEvent.GetPooled(systemEvent))
            {
                wheel.target = timeline;
                timeline.SendEvent(wheel);
            }
        }

        private VisualElement HubContent()
        {
            VisualElement content = _window.HubRoot.Q(LiveOpsHubPaths.ShellElementNames.Content);
            Assert.IsNotNull(content, "cửa sổ hub phải có cột nội dung 'hub-content'");
            return content;
        }

        private static void DragShorter(CalendarTimelinePresenter presenter)
        {
            DateTime startUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
            DateTime shorterEndUtc = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, startUtc, shorterEndUtc,
                LiveOpsTimelineGesturePhase.Preview));
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, startUtc, shorterEndUtc,
                LiveOpsTimelineGesturePhase.Commit));
        }

        private LiveOpsHubServices CreateServices()
        {
            _clock = LiveOpsHubTestServices.CreateClock();
            _confirmation = new ScriptedLiveOpsHubConfirmationPresenter();
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(_clock)
                .WithConfirmation(_confirmation)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            services.Session.RunCheckToCompletion();
            return services;
        }

        private static CalendarTimelinePresenter CreatePresenter(LiveOpsHubServices services)
        {
            CalendarTimelinePresenter presenter = new CalendarTimelinePresenter(services);
            DateTime rangeEndUtc = LiveOpsTimelineGeometry.AddTicksClamped(RangeStartUtc,
                LiveOpsTimelineGeometry.RangeLengthOf(LiveOpsTimelineZoom.ThreeWeeks).Ticks);
            presenter.BuildModel(RangeStartUtc, rangeEndUtc, DesignTrackWidth);
            return presenter;
        }

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

        private VisualElement SectionView()
        {
            return _window.SectionBody == null || _window.SectionBody.childCount == 0 ? null : _window.SectionBody[0];
        }

        private IEnumerator OpenCalendar(string scenarioId, int width, int height)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(scenarioId);
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            _window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
            // SP-16: đặt kích thước SAU Show.
            _window.position = new Rect(0, 0, width, height);
            yield return WaitForLayout(_window.rootVisualElement);
        }

        private static IEnumerator WaitForLayout(VisualElement element)
        {
            int frames = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!LiveOpsHubWindowTestScope.HasLayout(element))
            {
                frames++;
                if (frames > MaximumLayoutFrames || stopwatch.ElapsedMilliseconds > MaximumLayoutMilliseconds)
                {
                    Assert.Fail("layout không ổn định sau " + frames + " khung");
                }
                yield return null;
            }
        }
    }
}
