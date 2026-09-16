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
            OverviewModel model = BuildDesignSampleModel();

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
            Assert.AreEqual(LiveOpsHubStrings.OverviewRowDroppedDetail, worst.Detail,
                "dòng 'ở đâu' của hàng bị bỏ nói đúng màn đích và điều kiện mở Copy JSON");
        }

        /// <summary>
        /// Câu của hàng Mất tiến độ là câu của <see cref="LiveOpsFindingText"/> với DẤU ĐÃ ĐĂNG (V-22 CC-FT-1), không phải câu
        /// màn tự ghép — headline đổi theo bản đã đăng nên gọi thiếu ngữ cảnh sẽ im lặng ra câu khác.
        /// </summary>
        [Test]
        public void ProgressLostRow_TitleComesFromFindingTextWithLatestStamp()
        {
            OverviewModel model = BuildDesignSampleModel(out LiveOpsHubServices services);

            LiveEventCalendarFinding progressLost = null;
            foreach (LiveEventCalendarFinding finding in services.Session.Check.LastReport.Findings)
            {
                if (finding.IsIgnored || finding.IsAboutRemoteSnapshot) continue;
                if (finding.Consequence == LiveEventCalendarConsequence.ProgressLost) progressLost = finding;
            }
            Assert.IsNotNull(progressLost, "lịch mẫu có đúng một phát hiện Mất tiến độ (DesignSampleCheckTests)");

            string expected = LiveOpsFindingText.Headline(progressLost, services.Format, services.Session.Document.LatestStamp);
            OverviewNeedsActionRow row = null;
            foreach (OverviewNeedsActionRow candidate in model.NeedsActionRows)
            {
                if (candidate.State == HealthState.Warning && string.Equals(candidate.Title, expected, StringComparison.Ordinal)) row = candidate;
            }
            Assert.IsNotNull(row, "hàng Mất tiến độ phải mang đúng headline của nguồn câu duy nhất: " + expected);
            Assert.AreEqual(LiveOpsFindingText.PlainText(LiveOpsFindingText.Meta(progressLost, services.Format, services.Session.Clock.UtcNow)),
                row.DetailTooltip, "câu meta đầy đủ (có đồng hồ) đi vào tooltip của dòng 'ở đâu'");
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
            OverviewModel model = BuildDesignSampleModel();

            OverviewMetric notChecked = model.Metrics[2];
            Assert.AreEqual("1", notChecked.Value);
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.OverviewMetricNotCheckedRemoteRulesFormat, 1), notChecked.Foot);
            Assert.AreEqual(1, model.NotMeasuredRuleCount);
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
            OverviewModel model = BuildDesignSampleModel();

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
        }

        /// <summary>sky-race chạy mỗi ngày: bảy dòng gần giống nhau gom MỘT dòng, còn đợt đang chạy vẫn có dòng khép riêng.</summary>
        [Test]
        public void Upcoming_DenseRecurringType_CollapsesIntoOneRow()
        {
            OverviewModel model = BuildDesignSampleModel();

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
        }

        /// <summary>Đợt bị game bỏ có dòng riêng trong bảng 7 ngày, mang dấu Blocked và câu lý do từ nguồn câu duy nhất (V-8).</summary>
        [Test]
        public void Upcoming_DroppedEntry_HasBlockedRowWithReason()
        {
            OverviewModel model = BuildDesignSampleModel();

            OverviewUpcomingRow dropped = null;
            foreach (OverviewUpcomingRow row in model.UpcomingRows(false))
            {
                if (row.Kind == OverviewUpcomingKind.Dropped) dropped = row;
            }
            Assert.IsNotNull(dropped, "hunt-0916-bonus chồng giờ nên game bỏ — bảng phải nói ra");
            Assert.AreEqual("hunt-0916-bonus", dropped.EventIdText);
            Assert.AreEqual(HealthState.Blocked, dropped.NoteState);
            Assert.IsNotEmpty(dropped.NoteText);
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

        /// <summary>
        /// (V-9) Cột "chặn Copy JSON" hỏi đúng <see cref="ExportGateState.CopyBlockColumnTextFor"/> của cổng xuất: hàng chỉ mang
        /// chữ khi nó LÀ lý do cổng đang chặn. Kết quả kiểm đã cũ chỉ làm cổng "chưa đo được" — Hình 4 để "-" ở hàng đó, và
        /// hàng "2 đợt bị bỏ" dựng từ báo cáo cũ cũng không được tự nhận là đang chặn.
        /// </summary>
        [Test]
        public void StaleCheck_NoRowClaimsToBlockCopy()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.StaleCheckScenario);
            OverviewModel model = Build(services);
            ExportGateState gate = services.Session.EvaluateExportGate(services.JsonReadBack, services.Format);

            OverviewNeedsActionRow staleRow = FindRow(model, LiveOpsHubStrings.OverviewRecheckButton);
            Assert.IsNotNull(staleRow, "kiểm đã cũ phải có hàng Kiểm lại (F5)");
            Assert.IsFalse(staleRow.BlocksCopy,
                "hàng 'Kết quả kiểm đã cũ' chỉ làm cổng CHƯA ĐO ĐƯỢC — Hình 4 để '-' ở cột chặn Copy JSON");

            Assert.AreEqual(string.Empty, gate.CopyBlockColumnTextFor(LiveEventCalendarConsequence.Dropped, false),
                "cổng xuất: kiểm đã cũ thì dòng 'không còn đợt bị bỏ' là CHƯA ĐO ĐƯỢC, không phải đang chặn");
            OverviewNeedsActionRow droppedRow = FindRow(model, LiveOpsHubStrings.OverviewOpenValidationButton);
            Assert.IsNotNull(droppedRow, "báo cáo cũ vẫn còn đợt bị bỏ nên hàng đó vẫn được liệt kê");
            Assert.IsFalse(droppedRow.BlocksCopy,
                "hàng dựng từ báo cáo CŨ không được tự nhận là lý do cổng đang chặn — màn Xuất đang để trống ô đó");
        }

        /// <summary>Dòng phụ metric ĐANG CHẠY là một DANH SÁCH id nên nối bằng dấu phẩy ("weekly-pass-35, sky-race-251" — [SD1 §1.1]).</summary>
        [Test]
        public void RunningMetric_FootJoinsEventIdsWithComma()
        {
            OverviewModel model = BuildDesignSampleModel();

            OverviewMetric running = model.Metrics[0];
            Assert.AreEqual("2", running.Value, "lịch mẫu có 2 đợt đang chạy");
            StringAssert.Contains(LiveOpsHubStrings.OverviewIdListSeparator, running.Foot);
            Assert.IsFalse(running.Foot.Contains(LiveOpsHubStrings.OverviewPartSeparator),
                "' · ' ngăn các MẢNH khác loại, không dùng cho danh sách id: " + running.Foot);
        }

        /// <summary>
        /// Dòng gom của bảng 7 ngày: cột Lúc chỉ in khoảng ("14/9 00:00 → 20/9 00:00"). Số đợt đã nằm ở cột Sự kiện — nói hai
        /// lần trong cột rộng 170px có cắt chữ là cách chắc chắn mất luôn mốc cuối. Ghi chú dùng dạng đầy đủ "20 giờ mỗi ngày",
        /// không dạng gọn "20g mỗi ngày".
        /// </summary>
        [Test]
        public void Upcoming_GroupedRow_TimeShowsRangeOnly_AndNoteSpellsDuration()
        {
            OverviewModel model = BuildDesignSampleModel(out LiveOpsHubServices services);

            OverviewUpcomingRow grouped = null;
            foreach (OverviewUpcomingRow row in model.UpcomingRows(false))
            {
                if (row.Kind == OverviewUpcomingKind.Grouped) grouped = row;
            }
            Assert.IsNotNull(grouped, "sky-race lặp dày phải có dòng gom");
            Assert.IsFalse(grouped.TimeText.Contains(grouped.KindText),
                "cột Lúc không lặp lại số đợt của cột Sự kiện: " + grouped.TimeText);

            Assert.IsTrue(services.Session.Document.TryGetRecurringRule(grouped.EventType, out RecurringLiveEventRule rule));
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewUpcomingGroupedNoteFormat,
                    services.Format.Duration(TimeSpan.FromHours(rule.ActiveHours), false)), grouped.NoteText,
                "ghi chú dòng gom dùng dạng đầy đủ ('20 giờ'), không dạng gọn ('20g')");
        }

        /// <summary>
        /// (V-8) Ghi chú dòng "bị bỏ" dùng dạng CHI TIẾT của nguồn câu duy nhất: "chồng 12 giờ với hunt-0914". Nhãn ngắn
        /// "chồng giờ" là để xếp cạnh nhãn khác trong một danh sách, đứng một mình nó giấu mất khoảng chồng và id đối thủ.
        /// </summary>
        [Test]
        public void Upcoming_DroppedOverlapRow_NoteNamesOverlapLengthAndRival()
        {
            OverviewModel model = BuildDesignSampleModel(out LiveOpsHubServices services);

            OverviewUpcomingRow dropped = null;
            foreach (OverviewUpcomingRow row in model.UpcomingRows(false))
            {
                if (row.Kind == OverviewUpcomingKind.Dropped) dropped = row;
            }
            Assert.IsNotNull(dropped);

            LiveEventCalendarEntryOutcome outcome = null;
            foreach (LiveEventCalendarEntryOutcome candidate in services.Session.Compilation.Entries)
            {
                if (!candidate.IsKept && candidate.DropReason == LiveEventCalendarDropReason.OverlapsSameType) outcome = candidate;
            }
            Assert.IsNotNull(outcome, "lịch mẫu có một mục bị bỏ vì chồng giờ");
            Assert.IsTrue(outcome.OverlapStartUtc.HasValue && outcome.OverlapEndUtc.HasValue);

            Assert.AreEqual(LiveOpsFindingText.Format(LiveOpsHubStrings.FindingOverlapDetailFormat,
                    services.Format.Duration(outcome.OverlapEndUtc.Value - outcome.OverlapStartUtc.Value, false),
                    LiveOpsFindingText.IdText(outcome.RelatedEventId)), dropped.NoteText);
            Assert.AreNotEqual(LiveOpsHubStrings.FindingOverlapShortLabel, dropped.NoteText,
                "nhãn ngắn 'chồng giờ' bỏ mất khoảng chồng và id đối thủ mà Hình 4 yêu cầu");
        }

        /// <summary>
        /// Ghim thứ tự: mảng tầng của model song song VỊ TRÍ với registry. Thêm hay đổi chỗ một màn mà quên mảng này thì health
        /// gắn nhầm tầng và không có gì kêu (<c>Math.Min</c> nuốt sai lệch) — đó là lý do thứ tự phải có test chứ không chỉ comment.
        /// </summary>
        [Test]
        public void SectionStages_MatchRegistryOrderAndCount()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.EmptyAssetScenario);
            List<IHubSection> sections = LiveOpsHubSections.Create(services);

            Assert.AreEqual(sections.Count, OverviewModel.SectionStagesByRegistryOrder.Count,
                "registry và mảng tầng phải cùng số màn");
            for (int index = 0; index < sections.Count; index++)
            {
                Assert.AreEqual(sections[index].Stage, OverviewModel.SectionStagesByRegistryOrder[index],
                    "màn '" + sections[index].Id + "' ở vị trí " + index.ToString(CultureInfo.InvariantCulture)
                    + " không khớp tầng đã ghim");
            }
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

        private static OverviewModel BuildDesignSampleModel()
        {
            return BuildDesignSampleModel(out LiveOpsHubServices _);
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
