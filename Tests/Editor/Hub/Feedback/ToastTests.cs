using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Toast (8.5, [FD §3.9]) với stack Undo thật và đồng hồ tay: Hoàn tác chỉ bật khi group còn trên đỉnh, sau Hoàn tác đổi thành
    /// "Đã hoàn tác: …" + Làm lại, toast mới thay cũ, hover dừng đếm 6 giây, ⌘Z của Unity gỡ đúng group thì toast đổi chữ như bấm nút.
    /// Kèm outcome (không tự tắt, dòng chân "còn đến khi bạn làm việc khác"). Logic không cần panel: toast là VisualElement, hẹn giờ gọi
    /// thẳng Tick. Riêng nút đóng là UI: chỉ click thật qua cửa sổ mới chứng minh nút nối với Hide (Button.clicked không gọi được từ ngoài).
    /// </summary>
    [TestFixture]
    public sealed class ToastTests
    {
        private const string MoveMessage = "Đã dời kết thúc lava-quest-2026-09b 19/9 → 20/9 00:00 UTC";

        private LiveOpsHubUndoTracker _tracker;
        private UndoTestTarget _target;
        private double _nowSeconds;
        private LiveOpsToast _toast;

        [SetUp]
        public void SetUp()
        {
            _tracker = new LiveOpsHubUndoTracker();
            _target = new UndoTestTarget();
            _nowSeconds = 100.0;
            _toast = new LiveOpsToast(_tracker, () => _nowSeconds);
            _toast.ListenToUndoForTest();
        }

        [TearDown]
        public void TearDown()
        {
            if (_window != null) _window.Close();
            _window = null;
            _tracker.Dispose();
            _target.Dispose();
        }

        private const int MaximumFrames = 60;
        // Chờ theo cả khung lẫn giờ thật: batch chạy 60 khung trong ~60 ms, máy bận (một Unity khác chạy song song) thì focus/layout
        // chưa kịp và test đỏ giả — chỉ fail khi quá cả số khung lẫn số giây (cổng W2-CLOSE2 gặp đúng ca này ở 6000.6).
        private const double MaximumWaitSeconds = 5.0;

        private ToastProbeWindow _window;

        // Cửa sổ trống chỉ để toast có panel thật (click chuột cần layout); lồng trong fixture vì thư mục test Feedback không có file hỗ trợ riêng.
        private sealed class ToastProbeWindow : EditorWindow
        {
        }

        private LiveOpsToastModel RecordEdit(string message, string value)
        {
            int group = _tracker.BeginGroup(message);
            _target.SetKey(value, message);
            return LiveOpsToastModel.ForEdit(message, group);
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Toast_UndoEnabledOnlyWhenGroupOnTop()
        {
            _target.SetKey("hunt_default", "giá trị đầu");
            _toast.Show(RecordEdit(MoveMessage, "hunt_bonus"));

            Assert.IsTrue(_toast.IsVisible);
            Assert.AreEqual(MoveMessage, _toast.MessageLabel.text);
            Assert.AreEqual(MoveMessage, _toast.tooltip, "tooltip đủ câu vì câu cắt ellipsis khi hẹp");
            Assert.IsTrue(_toast.ActionButton.enabledSelf, "group của toast đang trên đỉnh → Hoàn tác bật");
            Assert.IsTrue(_toast.ActionButton.text.StartsWith(LiveOpsHubStrings.FeedbackToastUndoLabel, StringComparison.Ordinal), "bắt đầu bằng chuỗi mong đợi");
            Assert.IsFalse(_toast.ActionSlot.IsBlocked);

            // Người dùng sửa thứ khác (Inspector, màn khác): Hoàn tác lúc này sẽ gỡ nhầm thao tác đó.
            Undo.IncrementCurrentGroup();
            _target.SetKey("changed-elsewhere", "Sửa ở Inspector");
            _nowSeconds += 0.1;
            _toast.Tick();

            Assert.IsFalse(_toast.ActionButton.enabledSelf, "thao tác khác sau đó → Hoàn tác khoá");
            Assert.AreEqual(LiveOpsHubStrings.FeedbackToastUndoUnavailableReason, _toast.ActionSlot.Reason, "lý do ngắn in cạnh nút");
            Assert.AreEqual(LiveOpsHubStrings.FeedbackToastUndoUnavailableTooltip, _toast.ActionSlot.tooltip, "tooltip chỉ Edit → Undo History");

            _toast.ActionButton.SetEnabled(true);
            InvokeAction();
            Assert.AreEqual("changed-elsewhere", _target.Key, "bấm khi hết đỉnh (trạng thái nút cũ) không gỡ thao tác của người khác");
            Assert.IsFalse(_toast.Model.IsUndone);
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Toast_AfterUndo_ShowsRedo()
        {
            _target.SetKey("hunt_default", "giá trị đầu");
            LiveOpsToastModel model = RecordEdit(MoveMessage, "hunt_bonus");
            _toast.Show(model);
            _nowSeconds += 4.0;
            _toast.Tick();
            Assert.AreEqual(2.0, _toast.RemainingSeconds, 1e-9);

            InvokeAction();

            Assert.AreEqual("hunt_default", _target.Key, "Hoàn tác gỡ đúng thao tác");
            Assert.IsTrue(_toast.IsVisible, "toast không biến mất sau Hoàn tác");
            Assert.IsTrue(_toast.Model.IsUndone);
            Assert.AreEqual(LiveOpsHubStrings.KitToastUndonePrefix + MoveMessage, _toast.MessageLabel.text);
            Assert.IsTrue(_toast.ActionButton.text.StartsWith(LiveOpsHubStrings.FeedbackToastRedoLabel, StringComparison.Ordinal), "bắt đầu bằng chuỗi mong đợi");
            Assert.IsTrue(_toast.ActionButton.enabledSelf, "chưa có thao tác mới → Làm lại bật");
            Assert.AreEqual(LiveOpsToast.VisibleSeconds, _toast.RemainingSeconds, 1e-9, "đếm lại 6 giây");

            InvokeAction();
            Assert.AreEqual("hunt_bonus", _target.Key, "Làm lại trả thao tác");
            Assert.IsFalse(_toast.Model.IsUndone);
            Assert.AreEqual(MoveMessage, _toast.MessageLabel.text);

            // ⌘Z của Unity (không qua nút) gỡ đúng group này → toast đổi chữ như khi bấm nút.
            Undo.PerformUndo();
            Assert.AreEqual("hunt_default", _target.Key);
            Assert.IsTrue(_toast.Model.IsUndone, "⌘Z khớp group đổi toast sang Đã hoàn tác");
            Undo.PerformRedo();
            Assert.IsFalse(_toast.Model.IsUndone, "⌘⇧Z khớp group đổi toast về câu gốc");
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Toast_AfterUndo_TooltipUsesDisplayTooltip_ExplicitAndDefault()
        {
            // Tooltip riêng (khung giờ): view phải lấy nguyên DisplayTooltip của model, không tự ghép tiền tố (CC-FEEDBACK-2, G-FIX-W2-4).
            _target.SetKey("hunt_default", "giá trị đầu");
            int group = _tracker.BeginGroup(MoveMessage);
            _target.SetKey("hunt_bonus", MoveMessage);
            LiveOpsToastModel explicitTooltip = LiveOpsToastModel.ForEdit(MoveMessage, group, "19/9 → 20/9 00:00 UTC");
            _toast.Show(explicitTooltip);
            Assert.AreEqual("19/9 → 20/9 00:00 UTC", _toast.tooltip, "trước Hoàn tác: tooltip riêng giữ nguyên chữ gốc");

            InvokeAction();
            Assert.IsTrue(_toast.Model.IsUndone);
            Assert.AreEqual(LiveOpsHubStrings.KitToastUndonePrefix + "19/9 → 20/9 00:00 UTC", _toast.tooltip,
                "sau Hoàn tác: tooltip riêng cũng mang tiền tố \"Đã hoàn tác: \" — lấy từ model.DisplayTooltip, view không tự ghép");

            // Tooltip mặc định (không truyền, = Message): sau Hoàn tác cũng phải qua cùng đường DisplayTooltip.
            LiveOpsToastModel defaultTooltip = RecordEdit("Đã xoá hunt-0916-bonus", "second");
            _toast.Show(defaultTooltip);
            Assert.AreEqual("Đã xoá hunt-0916-bonus", _toast.tooltip, "trước Hoàn tác: tooltip mặc định là câu toast, chưa có tiền tố");
            InvokeAction();
            // Khẳng định lượt Hoàn tác thật sự chạy và chữ mong đợi viết tường minh: nếu chỉ so với DisplayTooltip/DisplayMessage
            // của chính model thì test vẫn xanh khi Hoàn tác không xảy ra (hai vế cùng là câu gốc).
            Assert.IsTrue(_toast.Model.IsUndone, "nút Hoàn tác phải gỡ group vừa ghi");
            Assert.AreEqual(LiveOpsHubStrings.KitToastUndonePrefix + "Đã xoá hunt-0916-bonus", _toast.tooltip,
                "sau Hoàn tác: tooltip mặc định mang tiền tố, trùng DisplayMessage vì Tooltip == Message");
            Assert.AreEqual(_toast.Model.DisplayTooltip, _toast.tooltip);
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Toast_UndoOlderStepAfterToastUndo_RedoDisabled()
        {
            _target.SetKey("value-0", "giá trị đầu");
            RecordEdit("Đã đổi màu loại hunt", "value-F");
            LiveOpsToastModel model = RecordEdit(MoveMessage, "value-G");
            _toast.Show(model);

            // ⌘Z lần một gỡ đúng thao tác của toast; ⌘Z lần hai gỡ thao tác CŨ hơn mà không ghi bản ghi mới nào.
            Undo.PerformUndo();
            Assert.IsTrue(_toast.Model.IsUndone);
            Assert.IsTrue(_toast.ActionButton.enabledSelf, "sau ⌘Z lần một Làm lại trả đúng thao tác của toast");
            Undo.PerformUndo();
            Assert.AreEqual("value-0", _target.Key);
            Assert.IsTrue(_toast.Model.IsUndone, "toast vẫn nói thao tác của nó đã hoàn tác");
            Assert.IsFalse(_toast.ActionButton.enabledSelf, "Làm lại kế tiếp là thao tác cũ hơn, không phải của toast → khoá");
            Assert.AreEqual(LiveOpsHubStrings.FeedbackToastUndoUnavailableReason, _toast.ActionSlot.Reason);

            // Nút cũ (trạng thái tới 100ms) vẫn bấm được: không được làm lại thao tác kia rồi nói câu của toast.
            _toast.ActionButton.SetEnabled(true);
            InvokeAction();
            Assert.AreEqual("value-0", _target.Key, "bấm khi Làm lại kế tiếp không phải của toast thì không chạy gì");
            Assert.IsTrue(_toast.Model.IsUndone);
            Assert.AreEqual(LiveOpsHubStrings.KitToastUndonePrefix + MoveMessage, _toast.MessageLabel.text);

            // ⌘⇧Z trả thao tác cũ hơn: thao tác của toast lại là Làm lại kế tiếp.
            Undo.PerformRedo();
            Assert.AreEqual("value-F", _target.Key);
            Assert.IsTrue(_toast.Model.IsUndone);
            Assert.IsTrue(_toast.ActionButton.enabledSelf, "thao tác cũ hơn đã làm lại → Làm lại của toast bật lại");
            InvokeAction();
            Assert.AreEqual("value-G", _target.Key, "Làm lại trả đúng thao tác của toast");
            Assert.IsFalse(_toast.Model.IsUndone);
            Assert.AreEqual(MoveMessage, _toast.MessageLabel.text);
            Assert.IsTrue(_toast.ActionButton.enabledSelf);
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Toast_UndoHistoryJumpPastGroup_UndoDisabled()
        {
            _target.SetKey("value-0", "giá trị đầu");
            int groupE = _tracker.BeginGroup("E");
            _target.SetKey("value-E", "E");
            RecordEdit("F", "value-F");
            LiveOpsToastModel model = RecordEdit(MoveMessage, "value-G");
            _toast.Show(model);

            // Edit → Undo History nhảy về sau E: gỡ cả thao tác của toast lẫn F trong một lần.
            Undo.RevertAllDownToGroup(groupE + 1);
            Assert.AreEqual("value-E", _target.Key, "nhảy lịch sử gỡ G và F");
            Assert.IsFalse(_toast.ActionButton.enabledSelf && !_toast.Model.IsUndone,
                "thao tác của toast đã bị gỡ — không được còn Hoàn tác bật với câu gốc (lần Undo/Redo cuối: group " + _tracker.LastUndoRedoGroup +
                ", toast group " + model.UndoGroup + ")");
            Assert.IsFalse(_toast.ActionButton.enabledSelf, "Làm lại kế tiếp là F, không phải thao tác của toast → khoá");
            _toast.ActionButton.SetEnabled(true);
            InvokeAction();
            Assert.AreEqual("value-E", _target.Key, "bấm nút cũ không gỡ E, không làm lại F");
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Toast_NewReplacesOld()
        {
            LiveOpsToastModel first = RecordEdit(MoveMessage, "first");
            _toast.Show(first);
            _nowSeconds += 5.0;
            _toast.Tick();

            LiveOpsToastModel second = RecordEdit("Đã xoá hunt-0916-bonus (16/9 12:00 → 18/9 00:00 UTC)", "second");
            _toast.Show(second);

            Assert.AreSame(second, _toast.Model, "toast mới thay toast cũ, không xếp chồng");
            Assert.AreEqual(second.Message, _toast.MessageLabel.text);
            Assert.AreEqual(LiveOpsToast.VisibleSeconds, _toast.RemainingSeconds, 1e-9, "toast mới đếm lại từ đầu");
            Assert.IsTrue(_toast.ActionButton.enabledSelf);

            LiveOpsToastModel info = LiveOpsToastModel.Info("Đã copy id weekly-pass-35");
            _toast.Show(info);
            Assert.IsTrue(_toast.ActionSlot.ClassListContains(LiveOpsHubClassNames.ToastActionHidden), "toast Info không có nút Hoàn tác");
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Toast_HoverPausesCountdown()
        {
            _toast.Show(LiveOpsToastModel.Info("Đã copy id weekly-pass-35"));
            _nowSeconds += 2.0;
            _toast.Tick();
            Assert.AreEqual(4.0, _toast.RemainingSeconds, 1e-9);

            _nowSeconds += 1.0;
            _toast.SetHovered(true);
            Assert.AreEqual(3.0, _toast.RemainingSeconds, 1e-9, "đoạn trước khi hover vẫn bị trừ");
            _nowSeconds += 30.0;
            _toast.Tick();
            Assert.IsTrue(_toast.IsVisible, "đang hover thì không tắt dù quá 6 giây");
            Assert.AreEqual(3.0, _toast.RemainingSeconds, 1e-9);

            _toast.SetHovered(false);
            _nowSeconds += 2.9;
            _toast.Tick();
            Assert.IsTrue(_toast.IsVisible, "rời chuột đếm tiếp phần còn lại, không đếm lại từ đầu");
            _nowSeconds += 0.2;
            _toast.Tick();
            Assert.IsFalse(_toast.IsVisible, "hết phần còn lại thì tắt");
            Assert.IsNull(_toast.Model);
            Assert.IsTrue(_toast.ClassListContains(LiveOpsHubClassNames.ToastHidden), "không có panel thì ẩn hẳn ngay (không chờ transition)");
        }

        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator Toast_CloseButton_Hides()
        {
            _window = ScriptableObject.CreateInstance<ToastProbeWindow>();
            _window.Show();
            _window.position = new Rect(0f, 0f, 640f, 120f);
            _window.rootVisualElement.Add(_toast);
            _toast.Show(LiveOpsToastModel.Info("Đã copy id weekly-pass-35"));
            Assert.IsFalse(_toast.ClassListContains(LiveOpsHubClassNames.ToastHidden));
            Assert.IsTrue(_toast.ClassListContains(LiveOpsHubClassNames.ToastVisible));

            int frames = 0;
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + MaximumWaitSeconds;
            while (float.IsNaN(_toast.CloseButton.worldBound.width) || _toast.CloseButton.worldBound.width <= 0f)
            {
                if (++frames > MaximumFrames && UnityEditor.EditorApplication.timeSinceStartup > deadline) Assert.Fail("nút đóng không có kích thước sau " + MaximumFrames + " khung và " + MaximumWaitSeconds + " giây");
                yield return null;
            }

            // Click chuột thật lên nút đóng (MouseDown + MouseUp tại tâm) — không gọi Hide trực tiếp.
            Vector2 center = _toast.CloseButton.worldBound.center;
            _window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = center, button = 0, clickCount = 1 });
            _window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = center, button = 0, clickCount = 1 });
            Assert.IsFalse(_toast.IsVisible, "click nút đóng tắt toast");
            Assert.IsNull(_toast.Model);
            Assert.IsFalse(_toast.ClassListContains(LiveOpsHubClassNames.ToastVisible));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Outcome_RecordStaysWithFootnote_BlockedUsesBlockedText()
        {
            DateTime createdUtc = new DateTime(2026, 9, 13, 9, 2, 0, DateTimeKind.Utc);
            LiveOpsOutcomeView outcome = new LiveOpsOutcomeView();
            Assert.IsTrue(outcome.ClassListContains(LiveOpsHubClassNames.OutcomeHidden), "chưa có kết quả thì ẩn");

            outcome.SetRecord(LiveOpsOutcomeRecord.Ok("Đã copy 1.612 byte vào clipboard lúc 09:02 UTC · sha 5b0d93",
                "Dán vào key liveops_calendar rồi bấm Đánh dấu đã đăng", createdUtc));
            Assert.IsTrue(outcome.IsShown);
            Assert.IsFalse(outcome.ClassListContains(LiveOpsHubClassNames.OutcomeHidden));
            Assert.IsNotNull(outcome.Q<Label>(className: LiveOpsHubClassNames.OutcomeFootnote));
            Assert.AreEqual(LiveOpsHubStrings.FeedbackOutcomeFootnote, outcome.Q<Label>(className: LiveOpsHubClassNames.OutcomeFootnote).text);
            Assert.IsTrue(outcome.ActionButton.ClassListContains(LiveOpsHubClassNames.OutcomeActionHidden), "không có hành động → không nút");

            string invokedAction = null;
            outcome.ActionInvoked += (actionId, argument) => invokedAction = actionId + "|" + argument;
            outcome.SetRecord(LiveOpsOutcomeRecord.Blocked("Không lưu được file", "Thư mục chỉ đọc", createdUtc, "reveal-file", "/tmp/x.json"), "Mở thư mục");
            Assert.IsTrue(outcome.ClassListContains(LiveOpsHubClassNames.OutcomeBlocked));
            Assert.IsTrue(outcome.HeadlineLabel.ClassListContains(LiveOpsHubClassNames.TextBlocked), "lỗi đọc bằng chữ blocked-text");
            Assert.IsFalse(outcome.ActionButton.ClassListContains(LiveOpsHubClassNames.OutcomeActionHidden));
            Assert.AreEqual("Mở thư mục", outcome.ActionButton.text);

            outcome.ClearRecord();
            Assert.IsFalse(outcome.IsShown);
            Assert.IsTrue(outcome.ClassListContains(LiveOpsHubClassNames.OutcomeHidden));
            Assert.IsNull(invokedAction);
        }

        /// <summary>Bấm nút toast không cần panel: gọi đúng handler mà Button.clicked gọi.</summary>
        private void InvokeAction()
        {
            _toast.HandleActionClicked();
        }
    }
}
