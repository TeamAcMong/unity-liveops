using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Card diff của màn Xuất JSON [SD2 §3.8] trên đúng bộ mẫu thiết kế: nháp khác bản đã đăng 5 chỗ, chia theo hậu quả.
    /// Hai luật quan trọng nhất ở đây là "hàng Bị bỏ không có Toggle" (tick không mở khoá gì) và (V-13) "so với bản remote
    /// thì không hàng nào có Toggle" — "Đã xem" luôn nói về dấu đã đăng.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class ExportDiffViewModelTests
    {
        private const string HeaderText = "So với bản đã đăng";
        private const string EmptyText = "Không có gì mới";

        private readonly LiveOpsHubFormat _format = new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset);

        [Test]
        public void Groups_FollowConsequenceOrder_AndSkipRemovedAndKept()
        {
            ExportDiffViewModel model = Build(LiveOpsHubCompareSource.Published, null);

            Assert.Greater(model.Groups.Count, 0, "mẫu thiết kế có thay đổi để vẽ");
            LiveEventCalendarConsequence previous = LiveEventCalendarConsequence.Dropped;
            for (int index = 0; index < model.Groups.Count; index++)
            {
                if (index > 0) Assert.Less((int)previous, (int)model.Groups[index].Consequence, "nhóm xếp theo mức nặng giảm dần");
                previous = model.Groups[index].Consequence;
                foreach (ExportDiffRow row in model.Groups[index].Rows)
                {
                    Assert.AreNotEqual(LiveEventCalendarChangeKind.Removed, row.Change.Kind, "Xoá chỉ đếm ở chip, không thành hàng");
                    Assert.AreNotEqual(LiveEventCalendarChangeKind.Kept, row.Change.Kind, "Giữ chỉ đếm ở chip");
                }
            }
        }

        [Test]
        public void DroppedRows_HaveNoReviewToggle()
        {
            ExportDiffViewModel model = Build(LiveOpsHubCompareSource.Published, null);

            bool sawDropped = false;
            foreach (ExportDiffGroup group in model.Groups)
            {
                foreach (ExportDiffRow row in group.Rows)
                {
                    if (row.Consequence != LiveEventCalendarConsequence.Dropped) continue;
                    sawDropped = true;
                    Assert.IsFalse(row.HasReviewToggle, "tick một mục bị bỏ không mở khoá cổng — hàng đó có link Sửa ở Kiểm lịch");
                }
            }
            Assert.IsTrue(sawDropped, "mẫu thiết kế có mục bị game bỏ");
        }

        [Test]
        public void ProgressLostRow_IsRequiredAndUsesWarningSymbol()
        {
            ExportDiffViewModel model = Build(LiveOpsHubCompareSource.Published, null);

            ExportDiffRow row = FindRow(model, LiveEventCalendarConsequence.ProgressLost);
            Assert.IsNotNull(row, "mẫu thiết kế có một thay đổi làm người chơi mất tiến độ");
            Assert.IsTrue(row.IsReviewRequired, "mục mất tiến độ mang tag bắt buộc");
            Assert.IsTrue(row.HasReviewToggle);
            Assert.AreEqual(LiveOpsHubStrings.ExportDiffSymbolProgressLost, row.SymbolText, "hàng mất tiến độ luôn là '!'");
            Assert.AreEqual(1, model.RequiredCount);
            Assert.AreEqual(0, model.ReviewedRequiredCount);
        }

        [Test]
        public void ReviewedChange_CountsInMetaText()
        {
            ExportDiffViewModel firstPass = Build(LiveOpsHubCompareSource.Published, null);
            ExportDiffRow required = FindRow(firstPass, LiveEventCalendarConsequence.ProgressLost);
            List<string> reviewed = new List<string> { required.Change.Fingerprint };

            ExportDiffViewModel model = Build(LiveOpsHubCompareSource.Published, change => reviewed.Contains(change.Fingerprint));

            Assert.AreEqual(1, model.ReviewedCount);
            Assert.AreEqual(1, model.ReviewedRequiredCount);
            Assert.IsTrue(model.MetaText.Length > 0, "có mục đánh dấu được thì meta đếm 'Đã xem …/… · bắt buộc …/…'");
        }

        [Test]
        public void CompareSourceRemote_DropsEveryReviewToggle()
        {
            ExportDiffViewModel model = Build(LiveOpsHubCompareSource.Remote, null);

            Assert.IsFalse(model.HasReviewToggles, "'Đã xem' luôn so với dấu đã đăng, không với bản remote đã dán (V-13)");
            foreach (ExportDiffGroup group in model.Groups)
            {
                foreach (ExportDiffRow row in group.Rows) Assert.IsFalse(row.HasReviewToggle);
            }
            Assert.AreEqual(string.Empty, model.MetaText, "không có mục đánh dấu được thì không in '0/0'");
        }

        [Test]
        public void Chips_AlwaysShowFourCounters()
        {
            ExportDiffViewModel model = Build(LiveOpsHubCompareSource.Published, null);

            Assert.AreEqual(4, model.ChipTexts.Count, "Thêm · Đổi · Xoá · Giữ luôn hiện đủ, kể cả khi bằng 0");
        }

        [Test]
        public void NoChanges_UsesEmptySentenceFromGate()
        {
            LiveEventCalendarDocument draft = LiveOpsDesignSample.Document;
            LiveEventCalendarDiffResult diff = LiveEventCalendarDiff.Compare(draft, draft, LiveOpsDesignSample.NowUtc);
            ExportDiffViewModel model = ExportDiffViewModel.Build(diff, Context(draft, draft, diff), _format,
                LiveOpsHubCompareSource.Published, HeaderText, EmptyText, null);

            Assert.IsTrue(model.IsEmpty);
            Assert.AreEqual(EmptyText, model.EmptyText, "câu 'không có gì mới' do cổng xuất viết, card chỉ hiện lại");
        }

        [Test]
        public void Build_NullFormat_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ExportDiffViewModel.Build(null, null, null,
                LiveOpsHubCompareSource.Published, HeaderText, EmptyText, null));
        }

        private ExportDiffViewModel Build(LiveOpsHubCompareSource source, Func<LiveEventCalendarChange, bool> isReviewed)
        {
            LiveEventCalendarDocument baseline = JsonLiveEventCalendarParser.ParseDocument(LiveOpsDesignSample.PublishedSnapshotJson).Document;
            LiveEventCalendarDocument draft = LiveOpsDesignSample.Document;
            LiveEventCalendarDiffResult diff = LiveEventCalendarDiff.Compare(baseline, draft, LiveOpsDesignSample.NowUtc);
            return ExportDiffViewModel.Build(diff, Context(baseline, draft, diff), _format, source, HeaderText, EmptyText, isReviewed);
        }

        private static LiveOpsChangeTextContext Context(LiveEventCalendarDocument baseline, LiveEventCalendarDocument draft,
            LiveEventCalendarDiffResult diff)
        {
            return new LiveOpsChangeTextContext(baseline, draft, LiveOpsDesignSample.NowUtc, diff, false);
        }

        private static ExportDiffRow FindRow(ExportDiffViewModel model, LiveEventCalendarConsequence consequence)
        {
            foreach (ExportDiffGroup group in model.Groups)
            {
                if (group.Consequence != consequence) continue;
                if (group.Rows.Count > 0) return group.Rows[0];
            }
            return null;
        }
    }
}
