using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class LiveEventTypeColorSlotsTests
    {
        [Test]
        public void DefaultSlot_MatchesDesignTable()
        {
            // [SD1 §0.1] bảng 8 màu: lava-quest 0, weekly-pass 1, sky-race 4, treasure-hunt/star-tournament 7 (trùng —
            // star-tournament được ghi đè thành 6 trong asset, nhưng hàm này luôn trả giá trị BĂM, chưa ghi đè).
            Assert.AreEqual(0, LiveEventTypeColorSlots.DefaultSlotFor("lava-quest"));
            Assert.AreEqual(1, LiveEventTypeColorSlots.DefaultSlotFor("weekly-pass"));
            Assert.AreEqual(4, LiveEventTypeColorSlots.DefaultSlotFor("sky-race"));
            Assert.AreEqual(7, LiveEventTypeColorSlots.DefaultSlotFor("treasure-hunt"));
            Assert.AreEqual(7, LiveEventTypeColorSlots.DefaultSlotFor("star-tournament"));
        }

        [Test]
        public void DefaultSlot_IsDeterministic_AcrossCalls()
        {
            Assert.AreEqual(LiveEventTypeColorSlots.DefaultSlotFor("lucky-spin"), LiveEventTypeColorSlots.DefaultSlotFor("lucky-spin"));
        }

        [Test]
        public void DefaultSlot_AlwaysInRange()
        {
            string[] sampleIds = { "", "a", "z-race-999", "loại-có-dấu" };
            foreach (string typeId in sampleIds)
            {
                int slot = LiveEventTypeColorSlots.DefaultSlotFor(typeId);
                Assert.GreaterOrEqual(slot, 0);
                Assert.Less(slot, LiveEventTypeDefinition.ColorSlotCount);
            }
        }

        [Test]
        public void FreeSlots_ExcludesUsedSlots_AscendingOrder()
        {
            var types = new List<LiveEventTypeDefinition>
            {
                new LiveEventTypeDefinition("a", "A", 0, false, ""),
                new LiveEventTypeDefinition("b", "B", 3, false, ""),
                new LiveEventTypeDefinition("c", "C", 7, false, ""),
            };

            IReadOnlyList<int> free = LiveEventTypeColorSlots.FreeSlots(types);

            CollectionAssert.AreEqual(new[] { 1, 2, 4, 5, 6 }, free);
        }

        [Test]
        public void FreeSlots_EmptyList_ReturnsAllEightSlots()
        {
            IReadOnlyList<int> free = LiveEventTypeColorSlots.FreeSlots(new List<LiveEventTypeDefinition>());
            Assert.AreEqual(8, free.Count);
        }

        [Test]
        public void TypesSharingSlot_ReturnsOthersWithSameSlot_ExcludesSelf()
        {
            var treasureHunt = new LiveEventTypeDefinition("treasure-hunt", "Săn kho báu", 7, true, "");
            var starTournament = new LiveEventTypeDefinition("star-tournament", "Giải ngôi sao", 7, false, "");
            var lavaQuest = new LiveEventTypeDefinition("lava-quest", "Nhiệm vụ dung nham", 0, false, "");
            var allTypes = new List<LiveEventTypeDefinition> { treasureHunt, starTournament, lavaQuest };

            IReadOnlyList<LiveEventTypeDefinition> sharing = LiveEventTypeColorSlots.TypesSharingSlot(treasureHunt, allTypes);

            Assert.AreEqual(1, sharing.Count);
            Assert.AreEqual("star-tournament", sharing[0].TypeId);
        }

        [Test]
        public void TypesSharingSlot_NoCollision_ReturnsEmpty()
        {
            var lavaQuest = new LiveEventTypeDefinition("lava-quest", "Nhiệm vụ dung nham", 0, false, "");
            var skyRace = new LiveEventTypeDefinition("sky-race", "Đua trên trời", 4, false, "");

            IReadOnlyList<LiveEventTypeDefinition> sharing = LiveEventTypeColorSlots.TypesSharingSlot(lavaQuest,
                new List<LiveEventTypeDefinition> { lavaQuest, skyRace });

            Assert.AreEqual(0, sharing.Count);
        }
    }
}
