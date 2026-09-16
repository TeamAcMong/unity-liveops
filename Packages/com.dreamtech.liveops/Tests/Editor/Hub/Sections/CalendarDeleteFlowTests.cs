using System;
using NUnit.Framework;

using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Ba mức xoá đợt cố định [FD §3.10] và hai dạng câu toast. Chạy trên lịch mẫu để con số trong câu đúng hình thiết kế;
    /// mức xác nhận đọc từ <see cref="LiveOpsConfirmationPolicy"/> nên test này khoá phần LUỒNG (nhãn nút, câu, lệnh sửa),
    /// không lặp lại bảng 7.0.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class CalendarDeleteFlowTests
    {
        /// <summary>18/9 10:00 UTC: lava-quest-2026-09b (17/9 → 20/9) đang chạy — đúng mốc giả định của hình hộp xác nhận [SD1 §3.15].</summary>
        private static readonly DateTime WhileMidQuestRunningUtc = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void Delete_ThreeLevels()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            DateTime nowUtc = services.Clock.UtcNow;

            CalendarDeleteFlow unpublished = CalendarDeleteFlow.For(services.Session, LiveOpsDesignSample.HuntBonusEntryKey, nowUtc,
                services.Format);
            Assert.AreEqual(LiveOpsConfirmRequirement.None, unpublished.Requirement,
                "hunt-0916-bonus chưa bắt đầu và chưa có trong bản đã đăng — xoá ngay, toast có Hoàn tác");
            Assert.IsFalse(unpublished.AsksBeforeDeleting);
            Assert.AreEqual(LiveOpsHubStrings.CalendarDeleteButton, unpublished.ButtonText, "nhãn nút không có '…' khi không hỏi");
            Assert.IsNull(unpublished.BuildConfirmRequest(LiveOpsHubKeyLabels.Undo), "mức không hỏi thì không dựng hộp");

            CalendarDeleteFlow published = CalendarDeleteFlow.For(services.Session, LiveOpsDesignSample.HuntEarlyEntryKey, nowUtc,
                services.Format);
            Assert.AreEqual(LiveOpsConfirmRequirement.Level1, published.Requirement,
                "hunt-0914 đã có trong bản đăng 11/9 nhưng chưa bắt đầu — hộp cấp 1");
            Assert.AreEqual(LiveOpsHubStrings.CalendarDeleteButtonWithDialog, published.ButtonText);
            LiveOpsConfirmRequest publishedRequest = published.BuildConfirmRequest(LiveOpsHubKeyLabels.Undo);
            Assert.IsNotNull(publishedRequest);
            Assert.AreEqual(LiveOpsConfirmLevel.Level1, publishedRequest.Level);
            StringAssert.Contains("hunt-0914", publishedRequest.Title, "tiêu đề nêu đúng đợt sắp xoá");
            Assert.AreEqual(string.Empty, publishedRequest.TypeToConfirmText, "cấp 1 không bắt gõ id");

            LiveOpsHubServices runningServices = LiveOpsHubTestServices.FromDesignSample();
            ((ManualLiveOpsClock)runningServices.Clock).Set(WhileMidQuestRunningUtc);
            CalendarDeleteFlow running = CalendarDeleteFlow.For(runningServices.Session, LiveOpsDesignSample.LavaQuestMidEntryKey,
                WhileMidQuestRunningUtc, runningServices.Format);
            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, running.Requirement,
                "đợt đang chạy — hộp cấp 2 gõ id vì người chơi đang ở trong đợt");
            LiveOpsConfirmRequest runningRequest = running.BuildConfirmRequest(LiveOpsHubKeyLabels.Undo);
            Assert.IsNotNull(runningRequest);
            Assert.AreEqual("lava-quest-2026-09b", runningRequest.TypeToConfirmText, "chuỗi phải gõ là id người chơi đang giữ");
            StringAssert.Contains(LiveOpsHubStrings.CalendarUnknownPlayerCountSentence, runningRequest.Body,
                "câu hậu quả P1 luôn nói hub không biết số người chơi toàn cục trước mọi số đo (7.0)");
        }

        [Test]
        public void DeleteToast_ShortFormBelow1100()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarDeleteFlow flow = CalendarDeleteFlow.For(services.Session, LiveOpsDesignSample.HuntBonusEntryKey,
                services.Clock.UtcNow, services.Format);

            string wideMessage = flow.ToastMessage(1280f);
            string narrowMessage = flow.ToastMessage(CalendarDeleteFlow.ShortToastWidthThreshold - 100f);
            StringAssert.Contains("hunt-0916-bonus", wideMessage);
            StringAssert.Contains("12:00", wideMessage, "cửa sổ rộng: toast nêu đủ giờ");
            StringAssert.Contains("hunt-0916-bonus", narrowMessage);
            Assert.IsFalse(narrowMessage.Contains("12:00"), "dưới 1100px toast rút còn ngày — giờ đủ nằm ở tooltip");
            StringAssert.Contains("12:00", flow.ToastTooltip(), "tooltip luôn giữ giờ đủ để lấy lại được bằng chuột");
        }

        [Test]
        public void Delete_BuildsRemoveEditForTarget()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarDeleteFlow flow = CalendarDeleteFlow.For(services.Session, LiveOpsDesignSample.HuntBonusEntryKey,
                services.Clock.UtcNow, services.Format);
            RemoveFixedEventEdit edit = flow.BuildEdit() as RemoveFixedEventEdit;
            Assert.IsNotNull(edit, "luồng xoá trả đúng lệnh RemoveFixedEventEdit");
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, edit.EntryKey);
        }

        [Test]
        public void Delete_MissingEntry_HasNoTarget()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarDeleteFlow flow = CalendarDeleteFlow.For(services.Session, "entry-khong-co", services.Clock.UtcNow,
                services.Format);
            Assert.IsFalse(flow.HasTarget, "đợt vừa bị xoá ở nơi khác: luồng không có đích, không ném");
            Assert.IsNull(flow.BuildEdit());
            Assert.AreEqual(string.Empty, flow.ToastMessage(1280f));
        }
    }
}
