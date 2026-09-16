using System;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Model thuần của màn Tổng quan (7.1) trên phiên THẬT dựng từ lịch mẫu: metric đếm đúng bộ tổng hợp của Kiểm lịch, hàng
    /// việc cần làm xếp xấu nhất trước, dòng "chặn Copy JSON" đến từ cổng xuất (V-9), 4 nút tầng P1 với đường nối chết từ tầng
    /// cổng chặn đầu tiên, và bảng 7 ngày tới gom loại lặp dày thành một dòng.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class OverviewModelTests
    {
        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        /// <summary>
        /// Hình 4: CẦN XỬ LÝ = 5 = 2 bị bỏ + 1 mất tiến độ + 2 nên xem, và hàng việc cần làm đi từ xấu nhất xuống — Blocked trước
        /// Warning, Warning trước NotMeasured. Con số phải bằng bộ tổng hợp của Kiểm lịch, không phải phép đếm riêng của màn.
        /// </summary>
        [Test]
        public void NeedsAction_WorstFirst_NeedsAction5()
        {
            OverviewModel model = BuildDesignSampleModel(out LiveOpsHubServices services);

            OverviewMetric needsAction = model.Metrics[1];
            Assert.AreEqual(LiveOpsHubStrings.OverviewMetricNeedsActionCaption, needsAction.Caption);
            Assert.AreEqual("5", needsAction.Value, "5 = 2 bị bỏ + 1 mất tiến độ + 2 nên xem (DesignSampleCheckTests)");
            Assert.AreEqual(HealthState.Blocked, needsAction.ValueState);
            StringAssert.Contains(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewDroppedCountFormat, 2), needsAction.Foot);
            StringAssert.Contains(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewProgressLostCountFormat, 1), needsAction.Foot);
            StringAssert.Contains(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewShouldReviewCountFormat, 2), needsAction.Foot);

            Assert.AreEqual(OverviewModel.BodyStateDefault, model.BodyState);
            Assert.GreaterOrEqual(model.NeedsActionRows.Count, 4, "2 bị bỏ gộp 1 hàng · 1 mất tiến độ · 2 nên xem gộp 1 hàng · bản remote chưa dán");

            int previousRank = -1;
            foreach (OverviewNeedsActionRow row in model.NeedsActionRows)
            {
                int rank = SortRankOf(row.State);
                Assert.GreaterOrEqual(rank, previousRank, "hàng việc cần làm phải xếp xấu nhất trước: " + row.Title);
                previousRank = rank;
            }

            OverviewNeedsActionRow worst = model.NeedsActionRows[0];
            Assert.AreEqual(HealthState.Blocked, worst.State);
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewRowDroppedTitleFormat, 2), worst.Title);
            Assert.AreEqual(LiveOpsHubStrings.OverviewOpenValidationButton, worst.ButtonText);
            Assert.AreEqual(LiveOpsHubSections.Ids.Validation, worst.Navigation.SectionId);
            Assert.AreEqual(LiveOpsHubNavigation.FilterDropped, worst.Navigation.FilterConsequence);
            Assert.IsTrue(worst.BlocksCopy, "còn đợt bị bỏ thì cổng chặn Copy JSON — trạng thái lấy từ ExportGateModel (V-9)");
            Assert.AreSame(services.Session.Document.LatestStamp, services.Session.Document.LatestStamp);
        }

        /// <summary>
        /// Chưa kiểm lần nào: metric CẦN XỬ LÝ là VÒNG RỖNG kèm câu "chưa kiểm" — không bao giờ in 0 hay "—" thay cho
        /// "không đo được" ([FD §2.4]); 0 sẽ đọc thành "đã kiểm, không có gì".
        /// </summary>
        [Test]
        public void NeverChecked_MetricShowsEmptyRingNotZero()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.EmptyAssetScenario);
            OverviewModel model = Build(services);

            Assert.AreEqual(OverviewModel.BodyStateNeverChecked, model.BodyState);
            OverviewMetric needsAction = model.Metrics[1];
            Assert.IsNull(needsAction.Value, "chưa kiểm lần nào = vòng rỗng, KHÔNG phải số 0");
            Assert.AreEqual(LiveOpsHubStrings.OverviewMetricNeverCheckedFoot, needsAction.Foot);
            Assert.IsFalse(needsAction.IsChecking);

            OverviewMetric notChecked = model.Metrics[2];
            Assert.IsNull(notChecked.Value, "chưa có báo cáo thì số luật chưa kiểm cũng là chưa đo được");
            Assert.AreEqual(LiveOpsHubStrings.OverviewMetricNeverCheckedFoot, notChecked.Foot);
        }

        [Test]
        public void NoAsset_BodyStateIsNoAsset_AndNothingIsRendered()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);
            OverviewModel model = Build(services);

            Assert.AreEqual(OverviewModel.BodyStateNoAsset, model.BodyState);
            Assert.AreEqual(0, model.Metrics.Count);
            Assert.AreEqual(0, model.NeedsActionRows.Count);
            Assert.AreEqual(0, model.FlowNodes.Count);
            Assert.AreEqual(0, model.UpcomingRows(false).Count);
        }

        /// <summary>Đang kiểm: metric có spinner + "Đang kiểm 7/12 luật…", vẫn không in số cũ như thể vừa đo xong.</summary>
        [Test]
        public void Checking_MetricShowsSpinnerAndProgress()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.RunningCheckScenario);
            OverviewModel model = Build(services);

            Assert.AreEqual(OverviewModel.BodyStateChecking, model.BodyState);
            OverviewMetric needsAction = model.Metrics[1];
            Assert.IsTrue(needsAction.IsChecking);
            Assert.IsNull(needsAction.Value);
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewMetricCheckingFormat,
                LiveOpsHubTestServices.RunningCompletedRuleCount, services.Session.Check.RuleCount), needsAction.Foot);
        }

        /// <summary>PD-1: P1 không có màn tầng CHẠY nên dòng phụ CHƯA KIỂM chỉ còn "1 luật (bản remote)", không có mảnh "2 màn".</summary>
        [Test]
        public void NotCheckedMetric_CountsRulesOnly_RemoteSnapshotRule()
        {
            OverviewModel model = BuildDesignSampleModel(out LiveOpsHubServices services);

            OverviewMetric notChecked = model.Metrics[2];
            Assert.AreEqual("1", notChecked.Value);
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.OverviewMetricNotCheckedRemoteRulesFormat, 1), notChecked.Foot);
            Assert.AreEqual(1, model.NotMeasuredRuleCount);
            Assert.IsNotNull(services);
        }

        /// <summary>Hàng bản remote: nút "Dán JSON đang chạy…" khoá khi action chưa có (INTERIM(G-PASTE)) — lý do đến từ chính port.</summary>
        [Test]
        public void RemoteRow_PasteUnavailable_RowIsDisabled()
        {
            OverviewModel model = BuildDesignSampleModel(out LiveOpsHubServices services);

            OverviewNeedsActionRow remoteRow = FindRow(model, LiveOpsHubStrings.OverviewPasteRunningJsonButton);
            Assert.IsNotNull(remoteRow, "chưa dán JSON đang chạy thì phải có hàng riêng cho bản remote");
            Assert.AreEqual(HealthState.NotMeasured, remoteRow.State);
            Assert.AreEqual(OverviewRowAction.PasteRunningJson, remoteRow.Action);
            Assert.AreEqual(services.Actions.CanPasteRunningJson, remoteRow.IsButtonEnabled);
            StringAssert.Contains(LiveEventCalendarRuleIds.RemoteSnapshotDrift, remoteRow.Detail);
        }

        /// <summary>Bốn tầng P1 (PD-1); đường nối chết từ tầng cổng chặn ĐẦU TIÊN trở đi, tầng cuối không có đường nối.</summary>
        [Test]
        public void FlowNodes_FourStages_ConnectorDiesFromFirstBlockingGate()
        {
            OverviewModel model = BuildDesignSampleModel(out LiveOpsHubServices services);

            Assert.AreEqual(4, model.FlowNodes.Count);
            Assert.AreEqual(PipelineStage.Configure, model.FlowNodes[0].Stage);
            Assert.AreEqual(PipelineStage.Schedule, model.FlowNodes[1].Stage);
            Assert.AreEqual(PipelineStage.Check, model.FlowNodes[2].Stage);
            Assert.AreEqual(PipelineStage.Export, model.FlowNodes[3].Stage);
            Assert.IsFalse(model.FlowNodes[3].HasConnector, "P1 dừng ở tầng XUẤT — không vẽ đường nối sang tầng CHẠY");

            Assert.AreEqual(HealthState.Blocked, model.FlowNodes[2].State, "lịch mẫu còn 2 đợt bị bỏ → Kiểm lịch Blocked");
            Assert.AreEqual(LiveOpsHubStrings.OverviewFlowNoteStopsHere, model.FlowNodes[2].Note);
            Assert.IsFalse(model.FlowNodes[0].IsConnectorDead);
            Assert.IsFalse(model.FlowNodes[1].IsConnectorDead);
            Assert.IsTrue(model.FlowNodes[2].IsConnectorDead);
            Assert.AreEqual(LiveOpsHubStrings.OverviewFlowNoteBlocked, model.FlowNodes[3].Note);
            Assert.IsNotNull(services);
        }

        /// <summary>sky-race chạy mỗi ngày: bảy dòng gần giống nhau gom MỘT dòng, còn đợt đang chạy vẫn có dòng khép riêng.</summary>
        [Test]
        public void Upcoming_DenseRecurringType_CollapsesIntoOneRow()
        {
            OverviewModel model = BuildDesignSampleModel(out LiveOpsHubServices services);

            IReadOnlyList<OverviewUpcomingRow> rows = model.UpcomingRows(false);
            int groupedSkyRace = 0;
            int skyRaceOpenRows = 0;
            foreach (OverviewUpcomingRow row in rows)
            {
                if (!string.Equals(row.EventType, "sky-race", StringComparison.Ordinal)) continue;
                if (row.Kind == OverviewUpcomingKind.Grouped) groupedSkyRace++;
                if (row.Kind == OverviewUpcomingKind.Open) skyRaceOpenRows++;
            }
            Assert.AreEqual(1, groupedSkyRace, "loại lặp dày gom đúng một dòng");
            Assert.AreEqual(0, skyRaceOpenRows, "dòng gom đã nói hết phần mở — không liệt kê lại từng đợt");
            Assert.IsNotNull(services);
        }

        /// <summary>Đợt bị game bỏ có dòng riêng trong bảng 7 ngày, mang dấu Blocked và câu lý do từ nguồn câu duy nhất (V-8).</summary>
        [Test]
        public void Upcoming_DroppedEntry_HasBlockedRowWithReason()
        {
            OverviewModel model = BuildDesignSampleModel(out LiveOpsHubServices services);

            OverviewUpcomingRow dropped = null;
            foreach (OverviewUpcomingRow row in model.UpcomingRows(false))
            {
                if (row.Kind == OverviewUpcomingKind.Dropped) dropped = row;
            }
            Assert.IsNotNull(dropped, "hunt-0916-bonus chồng giờ nên game bỏ — bảng phải nói ra");
            Assert.AreEqual("hunt-0916-bonus", dropped.EventIdText);
            Assert.AreEqual(HealthState.Blocked, dropped.NoteState);
            Assert.IsNotEmpty(dropped.NoteText);
            Assert.IsNotNull(services);
        }

        /// <summary>Chưa có dấu đã đăng: không có bảng "bản đã đăng" để đọc, và Tổng quan phải nói ra thay vì để bảng trống.</summary>
        [Test]
        public void Upcoming_WithoutPublishedStamp_PublishedSourceIsEmptyAndRowSaysWhy()
        {
            LiveOpsHubServices services = BuildServices(LiveOpsDesignSample.DocumentWithoutPublishedStamp());
            services.Session.RunCheckToCompletion();
            OverviewModel model = Build(services);

            Assert.IsFalse(model.HasPublishedStamp);
            Assert.AreEqual(0, model.UpcomingRows(true).Count);
            Assert.Greater(model.UpcomingRows(false).Count, 0);
            Assert.IsNotNull(FindRow(model, LiveOpsHubStrings.OverviewOpenExportButton),
                "chưa có dấu đã đăng là một việc cần làm, không phải trạng thái im lặng");
        }

        /// <summary>Lịch sạch: card việc cần làm thành (c) "0 việc chặn" nhưng hàng NotMeasured vẫn còn — chưa kiểm không phải là ổn.</summary>
        [Test]
        public void NoBlockers_BodyStateIsNoBlockers_NotMeasuredRowsStay()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.EmptyAssetScenario);
            services.Session.RunCheckToCompletion();
            OverviewModel model = Build(services);

            Assert.AreEqual(OverviewModel.BodyStateNoBlockers, model.BodyState);
            foreach (OverviewNeedsActionRow row in model.NeedsActionRows)
            {
                Assert.AreEqual(HealthState.NotMeasured, row.State, "(c) chỉ còn hàng chưa đo được: " + row.Title);
            }
            Assert.Greater(model.NeedsActionRows.Count, 0, "(c) vẫn liệt kê hàng chưa kiểm [SD1 §1.4]");
        }

        // ============================================================================================================ hạ tầng

        private static int SortRankOf(HealthState state)
        {
            switch (state)
            {
                case HealthState.Blocked: return 0;
                case HealthState.Warning: return 1;
                default: return 2;
            }
        }

        private static OverviewNeedsActionRow FindRow(OverviewModel model, string buttonText)
        {
            foreach (OverviewNeedsActionRow row in model.NeedsActionRows)
            {
                if (string.Equals(row.ButtonText, buttonText, StringComparison.Ordinal)) return row;
            }
            return null;
        }

        private static OverviewModel BuildDesignSampleModel(out LiveOpsHubServices services)
        {
            services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            return Build(services);
        }

        private static LiveOpsHubServices BuildServices(LiveEventCalendarDocument document)
        {
            return LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document)));
        }

        /// <summary>Health của 6 màn lấy từ CHÍNH phiên (<see cref="LiveOpsHubFindingRouting.ForSection"/>) — đúng thứ màn thật truyền vào.</summary>
        private static OverviewModel Build(LiveOpsHubServices services)
        {
            List<SectionHealth> healths = new List<SectionHealth>();
            foreach (IHubSection section in LiveOpsHubSections.Create(services))
            {
                healths.Add(LiveOpsHubFindingRouting.ForSection(section.Id, services));
            }
            ExportGateState gate = services.Session.Asset != null
                ? services.Session.EvaluateExportGate(services.JsonReadBack, services.Format)
                : null;
            return OverviewModel.Build(services.Session, healths, gate, services.Actions.CanPasteRunningJson,
                services.Actions.CanImportRunningJson, services.Format);
        }
    }
}
