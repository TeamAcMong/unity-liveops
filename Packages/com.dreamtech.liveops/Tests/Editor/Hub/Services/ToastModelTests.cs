using System;
using NUnit.Framework;
using UnityEngine;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>Model toast + outcome (8.5) — thuần, không cần panel.</summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class ToastModelTests
    {
        [Test]
        public void ForEdit_UndoGroupNameFallsBackToMessage_TooltipFallsBackToMessage()
        {
            LiveOpsToastModel toast = LiveOpsToastModel.ForEdit("Đã xoá đợt hunt-0916-bonus", 42);

            Assert.AreEqual(42, toast.UndoGroup);
            Assert.IsTrue(toast.HasUndo);
            Assert.AreEqual(toast.Message, toast.UndoGroupName);
            Assert.AreEqual(toast.Message, toast.Tooltip);
            Assert.IsFalse(toast.IsUndone);
            Assert.AreEqual(toast.Message, toast.DisplayMessage);
        }

        [Test]
        public void ForEdit_ExplicitTooltip_Kept()
        {
            LiveOpsToastModel toast = LiveOpsToastModel.ForEdit("Đã dời hunt-0914", 3, "14/9 00:00 → 15/9 00:00");
            Assert.AreEqual("14/9 00:00 → 15/9 00:00", toast.Tooltip);
        }

        [Test]
        public void DisplayTooltip_BeforeUndo_EqualsTooltip()
        {
            LiveOpsToastModel withTooltip = LiveOpsToastModel.ForEdit("Đã dời hunt-0914", 3, "14/9 00:00 → 15/9 00:00");
            LiveOpsToastModel withoutTooltip = LiveOpsToastModel.ForEdit("Đã xoá đợt hunt-0916-bonus", 7);

            Assert.AreEqual("14/9 00:00 → 15/9 00:00", withTooltip.DisplayTooltip);
            Assert.AreEqual(withoutTooltip.DisplayMessage, withoutTooltip.DisplayTooltip);
        }

        [Test]
        public void DisplayTooltip_AfterUndo_ExplicitTooltipPrefixed_RedoRemovesPrefix()
        {
            LiveOpsToastModel toast = LiveOpsToastModel.ForEdit("Đã dời hunt-0914", 3, "14/9 00:00 → 15/9 00:00");

            LiveOpsToastModel undone = toast.AsUndone();

            Assert.AreEqual("Đã hoàn tác: 14/9 00:00 → 15/9 00:00", undone.DisplayTooltip);
            Assert.AreEqual("14/9 00:00 → 15/9 00:00", undone.Tooltip, "Tooltip giữ chữ gốc, tiền tố chỉ ở DisplayTooltip");
            Assert.AreEqual("14/9 00:00 → 15/9 00:00", undone.AsRedone().DisplayTooltip);
        }

        [Test]
        public void DisplayTooltip_AfterUndo_DefaultTooltip_EqualsDisplayMessage()
        {
            LiveOpsToastModel undone = LiveOpsToastModel.ForEdit("Đã xoá đợt hunt-0916-bonus", 7).AsUndone();

            Assert.AreEqual("Đã hoàn tác: Đã xoá đợt hunt-0916-bonus", undone.DisplayTooltip);
            Assert.AreEqual(undone.DisplayMessage, undone.DisplayTooltip);
        }

        [Test]
        public void DisplayMessage_AfterUndo_UsesStepName()
        {
            LiveOpsToastModel toast = LiveOpsToastModel.ForEdit("Đã dời hunt-0916-bonus 17/9 00:00 → 18/9 12:00 UTC", 9,
                undoneStepName: "Dời hunt-0916-bonus");

            Assert.AreEqual("Đã dời hunt-0916-bonus 17/9 00:00 → 18/9 12:00 UTC", toast.DisplayMessage, "chưa hoàn tác thì vẫn là câu toast");
            Assert.AreEqual("Đã hoàn tác: Dời hunt-0916-bonus", toast.AsUndone().DisplayMessage,
                "[SD1 §3.8 khung 14] — không đọc hai lần \"Đã\" (phiếu D-5 của cổng W4)");
            // Q-W5-5 (user chốt 17/9/2026): Undo History đọc CÂU NGẮN, toast giữ câu dài — trái luật 8.5 cũ, user đã đồng ý.
            Assert.AreEqual("Dời hunt-0916-bonus", toast.UndoGroupName, "tên bước Undo là câu ngắn của thiết kế (Q-W5-5)");
            Assert.AreEqual("Dời hunt-0916-bonus", toast.StepName);
            Assert.AreEqual("Đã dời hunt-0916-bonus 17/9 00:00 → 18/9 12:00 UTC", toast.AsUndone().AsRedone().DisplayMessage);
        }

        [Test]
        public void Info_HasNoUndoGroup()
        {
            LiveOpsToastModel toast = LiveOpsToastModel.Info("Không còn bản trên đĩa để so — đang so với bản đã đăng");

            Assert.AreEqual(LiveOpsToastModel.NoUndoGroup, toast.UndoGroup);
            Assert.IsFalse(toast.HasUndo);
            Assert.AreEqual(string.Empty, toast.UndoGroupName);
        }

        [Test]
        public void AsUndone_PrefixesDisplay_KeepsGroupAndMessage()
        {
            LiveOpsToastModel toast = LiveOpsToastModel.ForEdit("Đã xoá đợt hunt-0916-bonus", 7);

            LiveOpsToastModel undone = toast.AsUndone();

            Assert.IsTrue(undone.IsUndone);
            Assert.AreEqual(7, undone.UndoGroup);
            Assert.AreEqual(toast.Message, undone.Message);
            Assert.AreEqual("Đã hoàn tác: Đã xoá đợt hunt-0916-bonus", undone.DisplayMessage);
            Assert.IsFalse(toast.IsUndone, "model bất biến: bản gốc không đổi");
        }

        [Test]
        public void AsRedone_ReturnsToOriginalDisplay()
        {
            LiveOpsToastModel redone = LiveOpsToastModel.ForEdit("Đã xoá đợt hunt-0916-bonus", 7).AsUndone().AsRedone();

            Assert.IsFalse(redone.IsUndone);
            Assert.AreEqual("Đã xoá đợt hunt-0916-bonus", redone.DisplayMessage);
        }

        [Test]
        public void Info_AsUndone_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => LiveOpsToastModel.Info("x").AsUndone());
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentException>(() => LiveOpsToastModel.ForEdit("", 1));
            Assert.Throws<ArgumentException>(() => LiveOpsToastModel.ForEdit("Đã xoá", -1));
            Assert.Throws<ArgumentException>(() => LiveOpsToastModel.Info(null));
        }

        [Test]
        public void OutcomeRecord_OkAndBlocked_StateMatchesHealthStateNumbers()
        {
            DateTime created = new DateTime(2026, 9, 13, 9, 2, 0, DateTimeKind.Utc);

            LiveOpsOutcomeRecord ok = LiveOpsOutcomeRecord.Ok("Đã copy JSON 5b0d93", "1.601 byte", created, "reveal-file", "/tmp/a.json");
            LiveOpsOutcomeRecord blocked = LiveOpsOutcomeRecord.Blocked("Main.asset đã bị xoá khỏi project", "", created);

            Assert.AreEqual(0, ok.State);
            Assert.IsFalse(ok.IsBlocked);
            Assert.AreEqual(2, blocked.State);
            Assert.IsTrue(blocked.IsBlocked);
            Assert.AreEqual("reveal-file", ok.ActionId);
            Assert.AreEqual(string.Empty, blocked.ActionId);
            Assert.AreEqual("2026-09-13T09:02:00Z", ok.CreatedUtcText);
            Assert.IsTrue(ok.TryGetCreatedUtc(out DateTime parsed));
            Assert.AreEqual(created, parsed);
        }

        [Test]
        public void OutcomeRecord_JsonUtilityRoundTrip_KeepsFields()
        {
            // Outcome sống qua domain reload bằng serializer của Unity — JsonUtility đi cùng đường field [SerializeField].
            LiveOpsOutcomeRecord original = LiveOpsOutcomeRecord.Blocked("Không copy được", "Clipboard bận",
                new DateTime(2026, 9, 13, 9, 2, 0, DateTimeKind.Utc), "retry-copy", "json");

            LiveOpsOutcomeRecord restored = JsonUtility.FromJson<LiveOpsOutcomeRecord>(JsonUtility.ToJson(original));

            Assert.AreEqual(original.State, restored.State);
            Assert.AreEqual(original.Headline, restored.Headline);
            Assert.AreEqual(original.Detail, restored.Detail);
            Assert.AreEqual(original.ActionId, restored.ActionId);
            Assert.AreEqual(original.ActionArgument, restored.ActionArgument);
            Assert.AreEqual(original.CreatedUtcText, restored.CreatedUtcText);
        }
    }
}
