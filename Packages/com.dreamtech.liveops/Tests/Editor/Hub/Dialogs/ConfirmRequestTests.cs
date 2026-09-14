using System;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class ConfirmRequestTests
    {
        [Test]
        public void Level1_DefaultsSafeLabelAndKeyHint()
        {
            LiveOpsConfirmRequest request = new LiveOpsConfirmRequest.Builder()
                .WithTitle("Xoá đợt lava-quest-2026-09b?")
                .WithBody("Đợt đã đăng, bắt đầu 17/9 00:00 UTC.")
                .WithButtons("Xoá đợt", LiveOpsHubStrings.KitConfirmKeepLabel)
                .Build();

            Assert.AreEqual(LiveOpsConfirmLevel.Level1, request.Level);
            Assert.AreEqual("Giữ lại", request.SafeLabel);
            Assert.AreEqual("Xoá đợt", request.DestructiveLabel);
            Assert.AreEqual("Enter / Esc: Giữ lại", request.KeyHint);
            Assert.AreEqual(HelpBoxMessageType.None, request.BodyStyle);
            Assert.AreEqual(string.Empty, request.TypeToConfirmText);
        }

        [Test]
        public void TypeToConfirm_SetsLevel2WarningAndKeyHint()
        {
            LiveOpsConfirmRequest request = new LiveOpsConfirmRequest.Builder()
                .WithTitle("Xoá đợt đang chạy weekly-pass-35?")
                .WithBody(LiveOpsHubStrings.KitUnknownPlayerCountSentence + " " + LiveOpsHubStrings.KitNoTestDataSentence)
                .WithButtons("Xoá đợt", "Giữ lại")
                .WithTypeToConfirm("weekly-pass-35")
                .Build();

            Assert.AreEqual(LiveOpsConfirmLevel.TypeToConfirm, request.Level);
            Assert.AreEqual(HelpBoxMessageType.Warning, request.BodyStyle);
            Assert.AreEqual("weekly-pass-35", request.TypeToConfirmText);
            Assert.AreEqual("Esc: Giữ lại · Enter không đổi gì", request.KeyHint);
        }

        [Test]
        public void ExplicitKeyHintAndTooltip_Kept()
        {
            LiveOpsConfirmRequest request = new LiveOpsConfirmRequest.Builder()
                .WithTitle("Thay lịch nháp bằng JSON đã dán?")
                .WithButtons("Thay 7 mục", "Chỉ so sánh")
                .WithKeyHint("Enter / Esc: Chỉ so sánh")
                .WithBodyTooltip("5 đợt, 1 luật")
                .WithHelpBoxWarning()
                .Build();

            Assert.AreEqual("Enter / Esc: Chỉ so sánh", request.KeyHint);
            Assert.AreEqual("5 đợt, 1 luật", request.BodyTooltip);
            Assert.AreEqual(HelpBoxMessageType.Warning, request.BodyStyle);
        }

        [Test]
        public void MissingTitle_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LiveOpsConfirmRequest.Builder().WithButtons("Xoá", "Giữ lại").Build());
        }

        [Test]
        public void MissingDestructiveLabel_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LiveOpsConfirmRequest.Builder().WithTitle("Xoá đợt?").Build());
        }

        [Test]
        public void TypeToConfirmWithoutText_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LiveOpsConfirmRequest.Builder()
                .WithTitle("Xoá đợt?").WithButtons("Xoá", "Giữ lại").WithTypeToConfirm("").Build());
        }

        [Test]
        public void Level1WithTypeText_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LiveOpsConfirmRequest.Builder()
                .WithTitle("Xoá đợt?").WithButtons("Xoá", "Giữ lại").WithTypeToConfirm("hunt-0914")
                .WithLevel(LiveOpsConfirmLevel.Level1).Build());
        }

        [Test]
        public void BuiltRequest_NotChangedByLaterBuilderCalls()
        {
            LiveOpsConfirmRequest.Builder builder = new LiveOpsConfirmRequest.Builder().WithTitle("Gỡ dấu đã đăng?").WithButtons("Gỡ dấu", "Giữ lại");
            LiveOpsConfirmRequest first = builder.Build();

            builder.WithTitle("Khác");

            Assert.AreEqual("Gỡ dấu đã đăng?", first.Title);
        }
    }
}
