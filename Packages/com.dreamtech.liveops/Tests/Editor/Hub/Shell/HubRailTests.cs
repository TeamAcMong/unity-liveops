using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// View rail trên cửa sổ thật (UI): class đường nối chết, dấu Ok ẩn mà nhãn còn, badge cũ, ô chặn dẫn tới Kiểm lịch lọc Bị bỏ,
    /// điều hướng bàn phím bằng NavigationMove/Submit ([FD §3.5], 8.2). Quyết định nằm ở <see cref="LiveOpsHubRailModel"/> — ở đây
    /// chỉ kiểm view gắn đúng class và nối đúng sự kiện.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubRailTests
    {
        private static readonly DateTime CheckedAtUtc = new DateTime(2026, 9, 13, 8, 46, 30, DateTimeKind.Utc);

        private LiveOpsHubWindowTestScope _scope;

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _scope = null;
        }

        [UnityTest]
        public IEnumerator DeadConnector_FromFirstBlockedGate()
        {
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(FakeHubSection.CreateDesignSampleShaped()));
            yield return _scope.WaitForLayout();
            LiveOpsHubRail rail = _scope.Window.Rail;
            List<VisualElement> stageRows = rail.Element.Query(className: LiveOpsHubClassNames.RailStageRow).ToList();
            Assert.AreEqual(4, stageRows.Count);

            AssertLines(stageRows[1], false, false, "LÊN LỊCH Blocked nhưng không phải cổng");
            AssertLines(stageRows[2], false, true, "KIỂM là cổng Blocked đầu tiên: nửa trên sống, từ dấu trở xuống chết");
            AssertLines(stageRows[3], true, true, "XUẤT sau cổng chặn");
            AssertLines(rail.GetRow(LiveOpsHubSections.Ids.Calendar), false, false, "hàng màn trước cổng chặn còn sống");
            AssertLines(rail.GetRow(LiveOpsHubSections.Ids.Validation), true, true, "hàng màn của tầng cổng chặn chết");

            Label calendarLabel = rail.GetRow(LiveOpsHubSections.Ids.Calendar).Q<Label>(className: LiveOpsHubClassNames.RailRowLabel);
            Label exportLabel = rail.GetRow(LiveOpsHubSections.Ids.Export).Q<Label>(className: LiveOpsHubClassNames.RailRowLabel);
            Assert.AreEqual(calendarLabel.resolvedStyle.opacity, exportLabel.resolvedStyle.opacity, 0.001f, "nhãn sau đường nối chết không bị làm mờ");
            VisualElement deadLine = stageRows[3].Q(className: LiveOpsHubClassNames.GutterLineBottom);
            VisualElement liveLine = stageRows[1].Q(className: LiveOpsHubClassNames.GutterLineBottom);
            Assert.Less(deadLine.resolvedStyle.opacity, liveLine.resolvedStyle.opacity, "đường nối chết mờ hơn đường sống (0,12 vs 0,35)");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OkSection_HidesMark_KeepsLabel()
        {
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(FakeHubSection.CreateDesignSampleShaped()), sectionId: LiveOpsHubSections.Ids.Calendar);
            yield return _scope.WaitForLayout();
            LiveOpsHubRail rail = _scope.Window.Rail;

            VisualElement overview = rail.GetRow(LiveOpsHubSections.Ids.Overview);
            LiveOpsStateMark okMark = overview.Q<LiveOpsStateMark>();
            Label okLabel = overview.Q<Label>(className: LiveOpsHubClassNames.RailRowLabel);
            Assert.AreEqual(Visibility.Hidden, okMark.resolvedStyle.visibility, "màn Ok ẩn dấu");
            Assert.AreEqual(LiveOpsHubStrings.ShellOverviewTitle, okLabel.text, "ẩn dấu nhưng giữ nhãn");
            Assert.AreEqual(Visibility.Visible, okLabel.resolvedStyle.visibility);

            VisualElement calendar = rail.GetRow(LiveOpsHubSections.Ids.Calendar);
            LiveOpsStateMark blockedMark = calendar.Q<LiveOpsStateMark>();
            Assert.AreEqual(Visibility.Visible, blockedMark.resolvedStyle.visibility);
            Assert.IsTrue(blockedMark.ClassListContains(LiveOpsHubClassNames.StateMarkBlocked));
            Assert.GreaterOrEqual(blockedMark.resolvedStyle.width, 8f, "Blocked trên hàng màn vẫn 8 px để vạch cắt đọc được");
            StringAssert.Contains("hunt-0916-bonus", calendar.tooltip, "tooltip hàng là lý do (WHY)");
            Assert.IsTrue(calendar.ClassListContains(LiveOpsHubClassNames.RailRowActive));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator StaleBadge_PrefixAndTooltip()
        {
            List<FakeHubSection> fakes = FakeHubSection.CreateDesignSampleShaped();
            fakes[4].Health = fakes[4].Health.AsStale(CheckedAtUtc);
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(fakes));
            yield return _scope.WaitForLayout();
            LiveOpsHubRail rail = _scope.Window.Rail;

            // Cửa sổ W2 chưa có bộ tổng hợp: badge cũ lấy con số từ badge đã giữ của màn (SectionHealth.AsStale).
            VisualElement checkRow = rail.Element.Query(className: LiveOpsHubClassNames.RailStageRow).AtIndex(2);
            Label badge = checkRow.Q<Label>(className: LiveOpsHubClassNames.RailBadge);
            Assert.IsNotNull(badge);
            Assert.AreEqual("cũ · 2 bị bỏ", badge.text);
            Assert.IsTrue(badge.ClassListContains(LiveOpsHubClassNames.RailBadgeStale));
            Assert.IsTrue(checkRow.tooltip.StartsWith("Kết quả kiểm lúc 08:46:30, lịch đã đổi sau đó: ", StringComparison.Ordinal),
                "tooltip badge cũ phải nêu giờ kiểm: " + checkRow.tooltip);
            Assert.IsTrue(checkRow.Q<LiveOpsStateMark>().ClassListContains(LiveOpsHubClassNames.StateMarkNotMeasured), "tầng có kết quả cũ mang vòng rỗng");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Blocker_Click_NavigatesValidationFilteredDropped()
        {
            List<FakeHubSection> fakes = FakeHubSection.CreateDesignSampleShaped();
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(fakes), sectionId: LiveOpsHubSections.Ids.Overview);
            yield return _scope.WaitForLayout();
            LiveOpsHubWindow window = _scope.Window;
            VisualElement blocker = window.Rail.BlockerElement;
            Assert.IsNotNull(blocker, "có tầng cổng Blocked thì phải có ô chặn đáy rail");
            Assert.AreEqual("Dừng ở Kiểm lịch", blocker.Q<Label>(className: LiveOpsHubClassNames.RailBlockerTitle).text);
            yield return LiveOpsHubWindowTestScope.WaitForLayout(blocker);

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = blocker;
                blocker.SendEvent(click);
            }
            yield return null;

            Assert.AreEqual(LiveOpsHubSections.Ids.Validation, window.ActiveSectionId, "bấm ô chặn mở Kiểm lịch");
            Assert.IsNotNull(fakes[4].LastNavigation, "màn Kiểm lịch phải nhận điều hướng có tham số");
            Assert.AreEqual(LiveOpsHubNavigation.FilterDropped, fakes[4].LastNavigation.FilterConsequence, "Kiểm lịch lọc sẵn nhóm Bị bỏ");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RailNavigation_ArrowsAndEnter()
        {
            _scope = LiveOpsHubWindowTestScope.Open(sectionId: LiveOpsHubSections.Ids.Overview);
            yield return _scope.WaitForLayout();
            LiveOpsHubWindow window = _scope.Window;
            LiveOpsHubRail rail = window.Rail;
            window.Focus();
            VisualElement overview = rail.GetRow(LiveOpsHubSections.Ids.Overview);
            overview.Focus();
            yield return null;
            Assert.AreSame(overview, overview.panel.focusController.focusedElement, "hàng rail phải nhận focus bàn phím");
            Assert.IsTrue(rail.Element.ClassListContains(LiveOpsHubClassNames.RailHasFocus), "focus trong rail bật liveops-hub-rail--has-focus");

            SendMove(overview, NavigationMoveEvent.Direction.Down);
            VisualElement eventTypes = rail.GetRow(LiveOpsHubSections.Ids.EventTypes);
            Assert.AreSame(eventTypes, eventTypes.panel.focusController.focusedElement, "mũi tên xuống sang hàng màn kế, bỏ qua hàng tầng");
            Assert.AreEqual(LiveOpsHubSections.Ids.Overview, window.ActiveSectionId, "di chuyển focus chưa mở màn");

            SendMove(eventTypes, NavigationMoveEvent.Direction.Down);
            VisualElement calendar = rail.GetRow(LiveOpsHubSections.Ids.Calendar);
            Assert.AreSame(calendar, calendar.panel.focusController.focusedElement, "xuống qua ranh tầng CẤU HÌNH → LÊN LỊCH");

            SendMove(calendar, NavigationMoveEvent.Direction.Up);
            Assert.AreSame(eventTypes, eventTypes.panel.focusController.focusedElement, "mũi tên lên quay lại");

            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled(EventModifiers.None))
            {
                submit.target = eventTypes;
                eventTypes.SendEvent(submit);
            }
            yield return null;
            Assert.AreEqual(LiveOpsHubSections.Ids.EventTypes, window.ActiveSectionId, "Enter (NavigationSubmit) mở màn đang focus");
            Assert.IsTrue(rail.GetRow(LiveOpsHubSections.Ids.EventTypes).ClassListContains(LiveOpsHubClassNames.RailRowActive));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ActiveRow_WhiteLabelOnlyWhenRailHasFocus()
        {
            _scope = LiveOpsHubWindowTestScope.Open(sectionId: LiveOpsHubSections.Ids.Calendar);
            yield return _scope.WaitForLayout();
            LiveOpsHubRail rail = _scope.Window.Rail;
            Label activeLabel = rail.GetRow(LiveOpsHubSections.Ids.Calendar).Q<Label>(className: LiveOpsHubClassNames.RailRowLabel);
            Label idleLabel = rail.GetRow(LiveOpsHubSections.Ids.Overview).Q<Label>(className: LiveOpsHubClassNames.RailRowLabel);
            rail.Element.RemoveFromClassList(LiveOpsHubClassNames.RailHasFocus);
            yield return LiveOpsHubWindowTestScope.WaitFrames(2);

            // Mất focus (nền highlight-inactive): chữ màu thường — ở Light trắng trên #AEAEAE chỉ 2,22:1 (Hình 3, [FD §2.3]).
            Assert.AreNotEqual(Color.white, activeLabel.resolvedStyle.color, "hàng active khi rail mất focus không được chữ trắng");
            Assert.AreEqual(idleLabel.resolvedStyle.color, activeLabel.resolvedStyle.color, "mất focus: chữ hàng active cùng màu chữ thường");
            Assert.AreEqual(FontStyle.Bold, activeLabel.resolvedStyle.unityFontStyleAndWeight, "hàng active vẫn đậm khi mất focus");

            rail.Element.AddToClassList(LiveOpsHubClassNames.RailHasFocus);
            yield return LiveOpsHubWindowTestScope.WaitFrames(2);
            Assert.AreEqual(Color.white, activeLabel.resolvedStyle.color, "rail có focus (nền highlight): chữ trắng");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RefreshHealth_SameModel_DoesNotRebuildRows()
        {
            List<FakeHubSection> fakes = FakeHubSection.CreateRegistryShaped();
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(fakes));
            yield return _scope.WaitForLayout();
            LiveOpsHubWindow window = _scope.Window;
            int rebuilds = window.Rail.RebuildCount;

            window.RefreshHealth();
            window.RefreshHealth();
            Assert.AreEqual(rebuilds, window.Rail.RebuildCount, "làm mới mỗi giây với model không đổi không được dựng lại hàng (mất focus, hover)");

            fakes[3].Health = SectionHealth.Warning("1 mất tiến độ", "weekly-pass đổi tiền tố");
            LiveOpsHealthThrottle.InvalidateAll();
            window.RefreshHealth();
            Assert.AreEqual(rebuilds + 1, window.Rail.RebuildCount, "health đổi thì rail dựng lại");
            Assert.IsTrue(window.Rail.GetRow(LiveOpsHubSections.Ids.RecurringRules).Q<LiveOpsStateMark>().ClassListContains(LiveOpsHubClassNames.StateMarkWarning));
            LogAssert.NoUnexpectedReceived();
        }

        private static void SendMove(VisualElement target, NavigationMoveEvent.Direction direction)
        {
            using (NavigationMoveEvent move = NavigationMoveEvent.GetPooled(direction, EventModifiers.None))
            {
                move.target = target;
                target.SendEvent(move);
            }
        }

        private static void AssertLines(VisualElement row, bool topDead, bool bottomDead, string message)
        {
            Assert.IsNotNull(row, message + ": không có hàng");
            VisualElement top = row.Q(className: LiveOpsHubClassNames.GutterLineTop);
            VisualElement bottom = row.Q(className: LiveOpsHubClassNames.GutterLineBottom);
            Assert.AreEqual(topDead, top.ClassListContains(LiveOpsHubClassNames.GutterLineDead), message + " (nửa trên)");
            Assert.AreEqual(bottomDead, bottom.ClassListContains(LiveOpsHubClassNames.GutterLineDead), message + " (nửa dưới)");
        }
    }
}
