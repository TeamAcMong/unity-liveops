using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class SectionBusTests
    {
        [Test]
        public void Navigate_RaisesNavigationRequestedWithSameObject()
        {
            var bus = new LiveOpsHubSectionBus();
            LiveOpsHubNavigation received = null;
            bus.NavigationRequested += navigation => received = navigation;
            LiveOpsHubNavigation sent = LiveOpsHubNavigation.To("validation").WithFilter(LiveOpsHubNavigation.FilterDropped);

            bus.Navigate(sent);

            Assert.AreSame(sent, received);
        }

        [Test]
        public void ShowToast_RaisesToastRequested()
        {
            var bus = new LiveOpsHubSectionBus();
            LiveOpsToastModel received = null;
            bus.ToastRequested += toast => received = toast;
            LiveOpsToastModel sent = LiveOpsToastModel.ForEdit("Đã xoá đợt hunt-0916-bonus", 5);

            bus.ShowToast(sent);

            Assert.AreSame(sent, received);
        }

        [Test]
        public void ShowOutcomeAndClear_RaiseInOrder()
        {
            var bus = new LiveOpsHubSectionBus();
            var calls = new List<string>();
            bus.OutcomeRequested += outcome => calls.Add("show:" + outcome.Headline);
            bus.OutcomeCleared += () => calls.Add("clear");

            bus.ShowOutcome(LiveOpsOutcomeRecord.Ok("Đã copy JSON", "", new DateTime(2026, 9, 13, 9, 2, 0, DateTimeKind.Utc)));
            bus.ClearOutcome();

            CollectionAssert.AreEqual(new[] { "show:Đã copy JSON", "clear" }, calls);
        }

        [Test]
        public void InvalidateHealth_RaisesHealthInvalidated()
        {
            var bus = new LiveOpsHubSectionBus();
            int count = 0;
            bus.HealthInvalidated += () => count++;

            bus.InvalidateHealth();
            bus.InvalidateHealth();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void SetContentClass_PassesNameAndEnabled()
        {
            var bus = new LiveOpsHubSectionBus();
            string receivedName = null;
            bool receivedEnabled = false;
            bus.ContentClassRequested += (className, enabled) =>
            {
                receivedName = className;
                receivedEnabled = enabled;
            };

            bus.SetContentClass(LiveOpsHubClassNames.ContentRaisedToast, true);

            Assert.AreEqual(LiveOpsHubClassNames.ContentRaisedToast, receivedName);
            Assert.IsTrue(receivedEnabled);
        }

        [Test]
        public void NoListeners_DoesNotThrow()
        {
            var bus = new LiveOpsHubSectionBus();

            Assert.DoesNotThrow(() =>
            {
                bus.Navigate(LiveOpsHubNavigation.To("overview"));
                bus.ShowToast(LiveOpsToastModel.Info("x"));
                bus.ClearOutcome();
                bus.InvalidateHealth();
                bus.SetContentClass(LiveOpsHubClassNames.ContentRaisedToast, false);
            });
        }

        [Test]
        public void NullArguments_Throw()
        {
            var bus = new LiveOpsHubSectionBus();

            Assert.Throws<ArgumentNullException>(() => bus.Navigate(null));
            Assert.Throws<ArgumentNullException>(() => bus.ShowToast(null));
            Assert.Throws<ArgumentNullException>(() => bus.ShowOutcome(null));
            Assert.Throws<ArgumentNullException>(() => bus.SetContentClass("", true));
        }

        [Test]
        public void ListenerUnsubscribingDuringRaise_OtherListenersStillCalled()
        {
            var bus = new LiveOpsHubSectionBus();
            int secondCalls = 0;
            Action first = null;
            first = () => bus.HealthInvalidated -= first;
            bus.HealthInvalidated += first;
            bus.HealthInvalidated += () => secondCalls++;

            bus.InvalidateHealth();
            bus.InvalidateHealth();

            Assert.AreEqual(2, secondCalls);
        }
    }
}
