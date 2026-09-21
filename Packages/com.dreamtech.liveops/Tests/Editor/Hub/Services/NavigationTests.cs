using System;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class NavigationTests
    {
        [Test]
        public void To_SetsSectionOnly_DefaultsEmpty()
        {
            LiveOpsHubNavigation navigation = LiveOpsHubNavigation.To("calendar");

            Assert.AreEqual("calendar", navigation.SectionId);
            Assert.AreEqual(string.Empty, navigation.FilterConsequence);
            Assert.AreEqual(string.Empty, navigation.RuleId);
            Assert.AreEqual(string.Empty, navigation.EntryKey);
            Assert.AreEqual(string.Empty, navigation.EventType);
            Assert.IsFalse(navigation.Flash);
            Assert.AreEqual(LiveOpsHubCompareSource.None, navigation.CompareSource);
        }

        [Test]
        public void With_ReturnsNewObject_KeepsOriginalAndOtherFields()
        {
            LiveOpsHubNavigation original = LiveOpsHubNavigation.To("validation");

            LiveOpsHubNavigation filtered = original.WithFilter(LiveOpsHubNavigation.FilterDropped).WithRule("overlap-same-type")
                .WithEntry("entry-key-1").WithEventType("treasure-hunt", true);

            Assert.AreEqual(string.Empty, original.FilterConsequence, "bất biến: bản gốc không đổi");
            Assert.AreEqual("validation", filtered.SectionId);
            Assert.AreEqual("dropped", filtered.FilterConsequence);
            Assert.AreEqual("overlap-same-type", filtered.RuleId);
            Assert.AreEqual("entry-key-1", filtered.EntryKey);
            Assert.AreEqual("treasure-hunt", filtered.EventType);
            Assert.IsTrue(filtered.Flash);
        }

        [Test]
        public void Navigation_WithCompareSource_Roundtrip()
        {
            LiveOpsHubNavigation toDisk = LiveOpsHubNavigation.To("calendar").WithEntry("entry-key-1").WithCompareSource(LiveOpsHubCompareSource.Disk);
            LiveOpsHubNavigation toRemote = toDisk.WithCompareSource(LiveOpsHubCompareSource.Remote);
            LiveOpsHubNavigation back = toRemote.WithCompareSource(LiveOpsHubCompareSource.None);

            Assert.AreEqual(LiveOpsHubCompareSource.Disk, toDisk.CompareSource);
            Assert.AreEqual(LiveOpsHubCompareSource.Remote, toRemote.CompareSource);
            Assert.AreEqual(LiveOpsHubCompareSource.None, back.CompareSource);
            Assert.AreEqual("calendar", back.SectionId);
            Assert.AreEqual("entry-key-1", back.EntryKey, "đổi nguồn so không làm mất tham số khác");
            // Các With… khác giữ nguyên nguồn so đã chọn.
            Assert.AreEqual(LiveOpsHubCompareSource.Disk, toDisk.WithRule("x").WithFilter(LiveOpsHubNavigation.FilterShouldReview).CompareSource);
        }

        [Test]
        public void CompareSource_NumbersStable()
        {
            Assert.AreEqual(0, (int)LiveOpsHubCompareSource.None);
            Assert.AreEqual(1, (int)LiveOpsHubCompareSource.Published);
            Assert.AreEqual(2, (int)LiveOpsHubCompareSource.Disk);
            Assert.AreEqual(3, (int)LiveOpsHubCompareSource.Remote);
        }

        [Test]
        public void WithFilter_EveryKnownConsequenceAccepted()
        {
            LiveOpsHubNavigation navigation = LiveOpsHubNavigation.To("validation");
            foreach (string consequence in new[] { "", "dropped", "progress-lost", "should-review", "not-measured" })
            {
                Assert.AreEqual(consequence, navigation.WithFilter(consequence).FilterConsequence);
            }
            Assert.AreEqual(string.Empty, navigation.WithFilter(null).FilterConsequence);
        }

        [Test]
        public void WithFilter_UnknownConsequence_Throws()
        {
            Assert.Throws<ArgumentException>(() => LiveOpsHubNavigation.To("validation").WithFilter("Dropped"));
        }

        [Test]
        public void To_EmptySection_Throws()
        {
            Assert.Throws<ArgumentException>(() => LiveOpsHubNavigation.To(""));
            Assert.Throws<ArgumentException>(() => LiveOpsHubNavigation.To(null));
        }
    }
}
