using System;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Cổng Xuất JSON (7.6, V-9): mọi trạng thái (a)–(k) + parser lệch của bảng [SD2 §3.10] dựng từ dữ liệu THẬT — nháp mẫu 13/9
    /// 08:47, validator mặc định, bộ ghi JSON, parser của game đọc lại qua đúng adapter phiên dùng (V-16), diff với dấu 11/9 16:20.
    /// Không giả kết quả parser hay báo cáo: cổng sai chỗ nào thì test đỏ ở đúng dòng đó.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class ExportGateTests
    {
        private static readonly DateTime CheckedAtUtc = new DateTime(2026, 9, 13, 8, 46, 58, DateTimeKind.Utc);
        private static readonly DateTime StaleCheckedAtUtc = new DateTime(2026, 9, 13, 8, 46, 30, DateTimeKind.Utc);
        private static readonly DateTime DraftChangedAtUtc = new DateTime(2026, 9, 13, 8, 46, 50, DateTimeKind.Utc);
        private static readonly DateTime RemoteVerifiedUtc = new DateTime(2026, 9, 13, 9, 10, 0, DateTimeKind.Utc);

        private const string DesignUnreviewedItems = "weekly-pass tiền tố";

        [TestCase("a")]
        [TestCase("b")]
        [TestCase("c")]
        [TestCase("d")]
        [TestCase("e")]
        [TestCase("f")]
        [TestCase("g")]
        [TestCase("h")]
        [TestCase("i")]
        [TestCase("j")]
        [TestCase("k")]
        [TestCase("parser-mismatch")]
        public void GateModel_States_a_to_k(string letter)
        {
            switch (letter)
            {
                case "a": AssertStateBlocked(); break;
                case "b": AssertStateReady(); break;
                case "c": AssertStateExported(); break;
                case "d": AssertStateStaleCheck(); break;
                case "e": AssertStateMarkedPublished(); break;
                case "f": AssertStateNoChanges(); break;
                case "g": AssertStateFirstPublish(); break;
                case "h": AssertStateReadBackFailed(); break;
                case "i": AssertStateRestoring(); break;
                case "j": AssertStateFormat1(); break;
                case "k": AssertStateRequiredReviewOnly(); break;
                default: AssertStateParserMismatch(); break;
            }
        }

        [Test]
        public void CompactGate_Counts4Conditions()
        {
            ExportGateInput input = ReadyInput(FixedDraft(), 1);
            ExportGateState state = ExportGateModel.Evaluate(input);

            Assert.IsTrue(state.IsCompact, "(b) đủ điều kiện, chưa copy → card gọn");
            Assert.AreEqual(4, state.CompactConditionCount, "PD-11: chỉ đếm dòng 1, 2, 3, 5 — dòng Bản remote không chặn nên không đếm");
            Assert.AreEqual("4 điều kiện đạt · sha " + input.Json.ShortSha + " · " + Format.Bytes(input.Json.ByteCount), state.CompactText);
            Assert.AreEqual("Bản remote vẫn chưa kiểm", state.CompactRemoteText);
            Assert.AreEqual("đủ điều kiện để copy", state.CardMetaText);

            // Dán bản remote khớp dấu vẫn không đổi số điều kiện — dòng remote luôn tách bên phải.
            ExportGateState remoteMatched = ExportGateModel.Evaluate(input.WithRemote(true, true, RemoteVerifiedUtc));
            Assert.IsTrue(remoteMatched.IsCompact);
            Assert.AreEqual(4, remoteMatched.CompactConditionCount);
            Assert.AreEqual("Bản remote khớp dấu 11/9 16:20", remoteMatched.CompactRemoteText);

            // Còn một dòng chặn thì không gọn; đã copy đúng sha thì hiện đủ 5 dòng như (c).
            Assert.IsFalse(ExportGateModel.Evaluate(ReadyInput(FixedDraft(), 0)).IsCompact);
            Assert.IsFalse(ExportGateModel.Evaluate(input.WithLastExportedSha256Hex(input.Json.Sha256Hex)).IsCompact);
            Assert.AreEqual(string.Empty, ExportGateModel.Evaluate(input.WithLastExportedSha256Hex(input.Json.Sha256Hex)).CompactText);
        }

        [Test]
        public void CompactGate_FirstPublish_CountsOnlyVisibleConditions()
        {
            ExportGateInput input = ReadyInput(DocumentWithoutStamps(FixedDraft()), 0);
            ExportGateState state = ExportGateModel.Evaluate(input);

            Assert.IsTrue(state.IsCompact);
            Assert.AreEqual(3, state.CompactConditionCount, "(g) dòng 5 ẩn ở lần đăng đầu nên không còn là điều kiện");
            AssertStartsWith("3 điều kiện đạt · sha ", state.CompactText);
        }

        [Test]
        public void ParserMismatch_SameCountDifferentKeptEntry_Blocks()
        {
            // Hai đợt trùng id khác loại: nháp (thứ tự xuất) giữ đợt lava-quest, bỏ đợt treasure-hunt. Bộ đọc lại "lỗi" đổi loại của
            // hai đợt cho nhau — parser vẫn giữ 1/2, cùng số với Kiểm lịch, nhưng giữ nhầm mục. So số lượng thôi sẽ báo khớp giả (V-6).
            LiveEventCalendarDocument draft = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("lava-quest", "Nhiệm vụ dung nham", 0, false, "lava_quest_v2"))
                .WithEventType(new LiveEventTypeDefinition("treasure-hunt", "Săn kho báu", 7, true, "hunt_default"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-first", "dup-0915", "lava-quest", "2026-09-15T00:00:00Z", "2026-09-16T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-second", "dup-0915", "treasure-hunt", "2026-09-17T00:00:00Z", "2026-09-18T00:00:00Z", "hunt_default"))
                .Build();
            const string placeholder = "\"type-swap-placeholder\"";
            ExportGateInput inputWithoutCheck = InputFor(draft, LiveEventCalendarJsonFormat.Version2,
                text => text.Replace("\"lava-quest\"", placeholder).Replace("\"treasure-hunt\"", "\"lava-quest\"").Replace(placeholder, "\"treasure-hunt\""));
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(new LiveEventCalendarCheckContextBuilder(draft, LiveOpsDesignSample.NowUtc).Build());
            Assert.AreEqual(1, report.Summary.DroppedCount, "tiền điều kiện: Kiểm lịch báo đúng một đợt bị bỏ vì trùng id");
            ExportGateInput input = inputWithoutCheck.WithCheck(report, false, false, report.RuleResults.Count, report.RuleResults.Count, CheckedAtUtc);

            Assert.AreEqual(input.DraftCompilation.EntryCount, input.ReadBack.Compilation.EntryCount, "tiền điều kiện: cùng số mục");
            Assert.AreEqual(input.DraftCompilation.KeptCount, input.ReadBack.Compilation.KeptCount, "tiền điều kiện: cùng số giữ");
            Assert.AreEqual(1, input.ReadBack.Compilation.KeptCount);

            ExportGateState state = ExportGateModel.Evaluate(input);

            Assert.AreEqual(ExportGateStatusCode.ReadBackMismatch, state.StatusCode);
            ExportGateRow parserRow = state.Row(ExportGateRowKind.ParserReadBack);
            Assert.AreEqual(ExportGateRowState.Blocked, parserRow.State);
            Assert.AreEqual("Parser giữ 1/2, Kiểm lịch cũng báo 1 bị bỏ nhưng khác mục — lỗi của hub", parserRow.Text);
            Assert.AreEqual(ExportGateRowAction.CopyReadBackError, parserRow.Action);
            Assert.AreEqual("Copy lỗi", parserRow.ActionText);
            AssertStartsWith("mục thứ 1: parser giữ dup-0915 · treasure-hunt, Kiểm lịch giữ dup-0915 · lava-quest", parserRow.MetaText);
            StringAssert.Contains("mục thứ 2:", state.ReadBackErrorReportText);
            AssertAllButtonsDisabled(state);
            Assert.AreEqual(HealthState.Blocked, state.Health.State);

            // Chưa kiểm lịch thì dòng 1 "chưa biết đợt nào bị bỏ" — dòng 2 không được mượn lời Kiểm lịch, vẫn chặn vì lệch dãy mục.
            ExportGateState withoutCheck = ExportGateModel.Evaluate(inputWithoutCheck);
            Assert.AreEqual(ExportGateStatusCode.ReadBackMismatch, withoutCheck.StatusCode);
            Assert.AreEqual("Parser giữ 1/2, bộ biên dịch của hub cũng bỏ 1 nhưng khác mục — lỗi của hub",
                withoutCheck.Row(ExportGateRowKind.ParserReadBack).Text);
        }

        [Test]
        public void ParserRow_SaysMatchesCheckOnlyWhenDroppedCountsAgree()
        {
            // Loại lạ trong nháp (luật 8): Kiểm lịch báo Bị bỏ, còn bộ biên dịch không xét loại nên parser giữ đủ mục. Dòng 2 nói
            // "khớp Kiểm lịch" lúc này sẽ mâu thuẫn với dòng 1 "1 đợt bị bỏ".
            LiveEventCalendarDocument draft = WithExtraEvent(FixedDraft(), new FixedLiveEventEntry("draft-lucky-spin", "lucky-spin-0920", "lucky-spin",
                "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z", "lucky_spin_v1"), true);
            ExportGateInput input = ReadyInput(draft, 1);
            Assert.AreEqual(1, input.LastReport.Summary.DroppedCount, "tiền điều kiện: loại lạ trong nháp là Bị bỏ (V-17)");
            Assert.AreEqual(0, input.DraftCompilation.DroppedCount, "tiền điều kiện: bộ biên dịch không bỏ đợt vì loại lạ");

            ExportGateState state = ExportGateModel.Evaluate(input);

            Assert.AreEqual("Kiểm lịch: 1 đợt bị bỏ", state.Row(ExportGateRowKind.NoDroppedEntries).Text);
            ExportGateRow parser = state.Row(ExportGateRowKind.ParserReadBack);
            Assert.AreEqual(ExportGateRowState.Ok, parser.State);
            string counts = input.DraftCompilation.KeptCount + "/" + input.DraftCompilation.EntryCount;
            Assert.AreEqual("Parser của game xác nhận: giữ " + counts + " mục, khớp bộ biên dịch của hub", parser.Text);
            Assert.AreEqual("Parser của game xác nhận: giữ " + counts + " mục", parser.NarrowText);
            Assert.AreEqual(ExportGateStatusCode.Blocked, state.StatusCode);
        }

        [Test]
        public void SameCalendarDifferentJson_IsNotNoChanges()
        {
            // Nháp trùng tài liệu bản đã đăng nhưng chọn định dạng 1: diff rỗng, còn byte game nhận khác sha của dấu.
            LiveEventCalendarDocument draft = NoChangeDraft();
            ExportGateInput input = WithDesignCheckAndStamp(InputFor(draft, LiveEventCalendarJsonFormat.Version1, null), 0, draft);
            Assert.IsTrue(input.PublishedDiff.IsEmpty, "tiền điều kiện: diff tài liệu rỗng");
            Assert.AreNotEqual(input.ActiveStamp.Sha256Hex, input.Json.Sha256Hex, "tiền điều kiện: JSON khác sha của dấu");

            ExportGateState state = ExportGateModel.Evaluate(input);

            Assert.AreEqual(ExportGateStatusCode.Ready, state.StatusCode, "không phải (f): thứ sắp copy khác bản đã đăng");
            Assert.IsFalse(state.HasStatus(ExportGateStatusCode.NoChanges));
            Assert.IsTrue(state.Copy.IsEnabled);
            Assert.AreEqual("Còn thiếu cho Đánh dấu: copy hoặc lưu file bản này", state.ReasonText);
            Assert.AreEqual("Lịch không đổi so với 11/9 16:20, nhưng JSON sắp xuất khác bản đã đăng: sha " + input.ActiveStamp.ShortSha + " → "
                + input.Json.ShortSha, state.DiffEmptyText);

            ExportGateState exported = ExportGateModel.Evaluate(input.WithLastExportedSha256Hex(input.Json.Sha256Hex));
            Assert.AreEqual(ExportGateStatusCode.Exported, exported.StatusCode);
            Assert.IsTrue(exported.MarkPublished.IsEnabled, "dấu phải mang sha mới, không thì dòng Bản remote báo khác dấu");
            Assert.AreEqual("Ghi dấu đã đăng cho sha " + input.Json.ShortSha, exported.MarkPublished.Tooltip);
            Assert.AreEqual("Bản remote: chưa dán — không biết Firebase đang giữ gì", exported.Row(ExportGateRowKind.RemoteSnapshot).Text,
                "không phải (e) vừa ghi dấu");
        }

        [Test]
        public void Input_InvalidArguments_ThrowWithParameterName()
        {
            ExportGateInput input = ReadyInput(FixedDraft(), 0);

            ArgumentOutOfRangeException negativeRuleCount = Assert.Throws<ArgumentOutOfRangeException>(() => input.WithCheck(null, false, false, 0, -1, null));
            Assert.AreEqual("ruleCount", negativeRuleCount.ParamName);
            ArgumentOutOfRangeException negativeCompleted = Assert.Throws<ArgumentOutOfRangeException>(() => input.WithCheck(null, false, false, -1, 12, null));
            Assert.AreEqual("completedRuleCount", negativeCompleted.ParamName);

            ArgumentNullException missingInput = Assert.Throws<ArgumentNullException>(() => ExportGateModel.Evaluate(null));
            AssertStartsWith("Cổng xuất cần đầu vào để đánh giá.", missingInput.Message);
            ArgumentNullException missingReadBack = Assert.Throws<ArgumentNullException>(() =>
                new ExportGateInput(input.Json, input.DraftCompilation, null, true, string.Empty, Format));
            Assert.AreEqual("readBack", missingReadBack.ParamName);
            AssertStartsWith("Cổng xuất cần kết quả parser đọc lại khi JSON đọc được.", missingReadBack.Message);
        }

        [Test]
        public void UnknownTypeOnlyInRemote_CopyNotBlocked()
        {
            LiveEventCalendarDocument draft = FixedDraft();
            LiveEventCalendarDocument remote = WithExtraEvent(DocumentWithoutStamps(draft), new FixedLiveEventEntry("remote-lucky-spin", "lucky-spin-0913",
                "lucky-spin", "2026-09-13T00:00:00Z", "2026-09-14T00:00:00Z", "lucky_spin_v1"), false);
            string remoteSha = LiveEventCalendarJsonWriter.Write(remote, LiveEventCalendarJsonFormat.Version2).Sha256Hex;
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(
                new LiveEventCalendarCheckContextBuilder(draft, LiveOpsDesignSample.NowUtc)
                    .WithPublishedBaseline(BaselineOf(draft.LatestStamp))
                    .WithRemoteSnapshot(remote, remoteSha)
                    .Build());

            LiveEventCalendarFinding remoteUnknownType = null;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                LiveEventCalendarFinding finding = report.Findings[index];
                if (finding.RuleId == LiveEventCalendarRuleIds.UnknownEventType && finding.IsAboutRemoteSnapshot) remoteUnknownType = finding;
            }
            Assert.IsNotNull(remoteUnknownType, "tiền điều kiện: lucky-spin chỉ có trong JSON đang chạy phải ra một phát hiện về bản remote");
            Assert.AreEqual(0, report.Summary.DroppedCount, "V-17: phát hiện về bản remote không vào số Bị bỏ của nháp");

            ExportGateInput input = InputFor(draft, LiveEventCalendarJsonFormat.Version2, null)
                .WithCheck(report, false, false, report.RuleResults.Count, report.RuleResults.Count, CheckedAtUtc)
                .WithPublished(draft.LatestStamp, DiffOf(draft), 1)
                .WithRemote(true, false, RemoteVerifiedUtc);
            ExportGateState state = ExportGateModel.Evaluate(input);

            ExportGateRow droppedRow = state.Row(ExportGateRowKind.NoDroppedEntries);
            Assert.AreEqual(ExportGateRowState.Ok, droppedRow.State);
            Assert.AreEqual("Kiểm lịch: không có đợt bị bỏ", droppedRow.Text);
            Assert.IsTrue(state.Copy.IsEnabled, "loại lạ chỉ trong JSON đang chạy không khoá Copy JSON của nháp (PD-42)");
            Assert.IsTrue(state.SaveFile.IsEnabled);

            ExportGateRow remoteRow = state.Row(ExportGateRowKind.RemoteSnapshot);
            Assert.AreEqual(ExportGateRowState.Warning, remoteRow.State);
            Assert.AreEqual("Bản remote khác dấu 11/9 16:20 · đã đối chiếu 09:10", remoteRow.Text);
            Assert.IsFalse(remoteRow.BlocksCopy, "dòng Bản remote không bao giờ chặn");
            Assert.AreEqual(LiveOpsHubCompareSource.Remote, remoteRow.Navigation.CompareSource);
            Assert.AreEqual(string.Empty, state.CopyBlockColumnTextFor(remoteUnknownType.Consequence, true),
                "Tổng quan: hàng lucky-spin không có cột chặn Copy JSON (V-17)");
            Assert.AreEqual(HealthState.Ok, state.Health.State);
        }

        [Test]
        public void NoChanges_CopyEnabled_MarkDisabled()
        {
            ExportGateInput input = ReadyInput(NoChangeDraft(), 0);
            Assert.IsTrue(input.PublishedDiff.IsEmpty, "tiền điều kiện: nháp trùng bản đã đăng");

            ExportGateState state = ExportGateModel.Evaluate(input);

            Assert.AreEqual(ExportGateStatusCode.NoChanges, state.StatusCode);
            Assert.IsTrue(state.Copy.IsEnabled, "không có thay đổi vẫn copy được — dán lại đúng bản đang chạy không hại gì");
            Assert.IsFalse(state.Copy.IsPrimary);
            Assert.IsTrue(state.SaveFile.IsEnabled);
            Assert.IsFalse(state.MarkPublished.IsEnabled);
            Assert.IsTrue(state.MarkPublished.IsPrimary, "(f) Đánh dấu đã đăng… chính-disabled, lý do cạnh nút");
            Assert.AreEqual("Không có thay đổi để ghi", state.MarkPublished.Tooltip);
            Assert.AreEqual("Không có thay đổi để ghi", state.ReasonText);
            Assert.IsFalse(state.ReasonIsBlocked);
            Assert.AreEqual("Không có gì mới so với 11/9 16:20 · sha " + input.ActiveStamp.ShortSha, state.DiffEmptyText);

            // Copy xong vẫn không có gì để ghi: nút Đánh dấu vẫn tắt.
            ExportGateState exported = ExportGateModel.Evaluate(input.WithLastExportedSha256Hex(input.Json.Sha256Hex));
            Assert.IsFalse(exported.MarkPublished.IsEnabled);
        }

        [Test]
        public void Input_WithReturnsCopy_OriginalUnchanged()
        {
            ExportGateInput original = ReadyInput(FixedDraft(), 0);
            ExportGateInput changed = original.WithLastExportedSha256Hex("abc").WithRemote(true, true, RemoteVerifiedUtc);

            Assert.AreEqual(string.Empty, original.LastExportedSha256Hex);
            Assert.IsFalse(original.RemotePasted);
            Assert.AreEqual("abc", changed.LastExportedSha256Hex);
            Assert.IsTrue(changed.RemotePasted);
            Assert.AreEqual(LiveEventCalendarJsonFormat.Version2, changed.JsonFormat, "định dạng đọc từ chính JSON, không nhận riêng");
        }

        [Test]
        public void Rows_AlwaysFiveInDrawOrder()
        {
            ExportGateState state = ExportGateModel.Evaluate(ReadyInput(DocumentWithoutStamps(FixedDraft()), 0));

            Assert.AreEqual(5, state.Rows.Count);
            for (int index = 0; index < state.Rows.Count; index++) Assert.AreEqual((ExportGateRowKind)index, state.Rows[index].Kind);
            Assert.IsFalse(state.Row(ExportGateRowKind.RequiredChangesReviewed).IsVisible);
        }

        // ------------------------------------------------------------------------------------------------- từng trạng thái

        private static void AssertStateBlocked()
        {
            ExportGateState state = ExportGateModel.Evaluate(ReadyInput(LiveOpsDesignSample.Document, 0));

            Assert.AreEqual(ExportGateStatusCode.Blocked, state.StatusCode);
            Assert.AreEqual("a", ExportGateModel.LetterOf(state.StatusCode));
            Assert.IsFalse(state.Copy.IsEnabled);
            Assert.IsTrue(state.Copy.IsPrimary, "Copy JSON là nút chính kể cả khi disabled — bước tiếp theo thật");
            Assert.AreEqual("Copy JSON: còn 2 đợt game sẽ bỏ, và 1 thay đổi đụng đợt đang chạy chưa xem (" + DesignUnreviewedItems + ")", state.Copy.Tooltip);
            Assert.IsFalse(state.SaveFile.IsEnabled);
            Assert.IsFalse(state.SaveFile.IsPrimary);
            Assert.AreEqual("Lưu file: cùng điều kiện với Copy JSON", state.SaveFile.Tooltip);
            Assert.IsFalse(state.MarkPublished.IsEnabled);
            Assert.IsFalse(state.MarkPublished.IsPrimary);
            Assert.AreEqual("Chưa copy hay lưu file JSON này", state.MarkPublished.Tooltip);
            Assert.AreEqual("Chặn: 2 đợt bị bỏ · 1 thay đổi bắt buộc chưa xem", state.ReasonText);
            Assert.IsTrue(state.ReasonIsBlocked);
            Assert.IsFalse(state.IsCompact);
            Assert.AreEqual("bấm một dòng để tới màn liên quan", state.CardMetaText);

            ExportGateRow dropped = state.Row(ExportGateRowKind.NoDroppedEntries);
            Assert.AreEqual(ExportGateRowState.Blocked, dropped.State);
            Assert.AreEqual("Kiểm lịch: 2 đợt bị bỏ", dropped.Text);
            Assert.AreEqual(LiveOpsHubSections.Ids.Validation, dropped.Navigation.SectionId);
            Assert.AreEqual(LiveOpsHubNavigation.FilterDropped, dropped.Navigation.FilterConsequence);

            ExportGateRow parser = state.Row(ExportGateRowKind.ParserReadBack);
            Assert.AreEqual(ExportGateRowState.Ok, parser.State, "parser khớp Kiểm lịch là bằng chứng → chấm Ok dù còn đợt bị bỏ");
            Assert.AreEqual("Parser của game xác nhận: giữ 6/8 mục, khớp Kiểm lịch", parser.Text);
            Assert.AreEqual("Parser của game xác nhận: giữ 6/8 mục", parser.NarrowText);
            Assert.AreEqual("1 không đọc được · 1 bị bỏ vì chồng giờ", parser.MetaText);

            Assert.AreEqual("Kiểm lịch chạy sau lần sửa cuối (08:46:58 UTC)", state.Row(ExportGateRowKind.CheckFreshness).Text);

            ExportGateRow remote = state.Row(ExportGateRowKind.RemoteSnapshot);
            Assert.AreEqual(ExportGateRowState.NotMeasured, remote.State);
            Assert.AreEqual("Bản remote: chưa dán — không biết Firebase đang giữ gì", remote.Text);
            Assert.AreEqual("Bản remote: chưa dán", remote.NarrowText);
            Assert.AreEqual("Dán JSON đang chạy…", remote.ActionText);
            Assert.IsFalse(remote.BlocksCopy);

            ExportGateRow reviewed = state.Row(ExportGateRowKind.RequiredChangesReviewed);
            Assert.AreEqual(ExportGateRowState.Blocked, reviewed.State);
            Assert.AreEqual("Chưa xem 1 thay đổi làm người chơi mất tiến độ (" + DesignUnreviewedItems + ")", reviewed.Text);
            Assert.AreEqual("Chưa xem 1 thay đổi làm người chơi mất tiến độ", reviewed.NarrowText);

            Assert.AreEqual(HealthState.Blocked, state.Health.State);
            Assert.AreEqual("chặn", state.Health.Badge);
            Assert.AreEqual(state.ReasonText, state.Health.Reason);
            Assert.IsTrue(state.Health.Counts.IsEmpty, "màn Xuất không đếm phát hiện (CC-SHELL-5 b)");
            Assert.AreEqual("chặn Copy JSON", state.CopyBlockColumnTextFor(LiveEventCalendarConsequence.Dropped, false));
            Assert.AreEqual("chặn Copy JSON", state.CopyBlockColumnTextFor(LiveEventCalendarConsequence.ProgressLost, false));
            Assert.AreEqual(string.Empty, state.CopyBlockColumnTextFor(LiveEventCalendarConsequence.ShouldReview, false));
            // V-17: phát hiện về JSON đang chạy không bao giờ có cột chặn — kiểm ở đây vì dòng 1 và 5 đều Blocked, nên chỉ nhánh
            // isAboutRemoteSnapshot mới trả được chuỗi rỗng cho Bị bỏ/Mất tiến độ.
            Assert.AreEqual(string.Empty, state.CopyBlockColumnTextFor(LiveEventCalendarConsequence.Dropped, true));
            Assert.AreEqual(string.Empty, state.CopyBlockColumnTextFor(LiveEventCalendarConsequence.ProgressLost, true));
        }

        private static void AssertStateReady()
        {
            ExportGateInput input = ReadyInput(FixedDraft(), 1);
            ExportGateState state = ExportGateModel.Evaluate(input);

            Assert.AreEqual(ExportGateStatusCode.Ready, state.StatusCode);
            Assert.IsTrue(state.Copy.IsEnabled);
            Assert.IsTrue(state.Copy.IsPrimary);
            Assert.AreEqual("Copy " + Format.Bytes(input.Json.ByteCount) + " vào clipboard · sha " + input.Json.ShortSha, state.Copy.Tooltip);
            Assert.IsTrue(state.SaveFile.IsEnabled);
            Assert.IsFalse(state.SaveFile.IsPrimary, "Lưu file không bao giờ là nút chính");
            Assert.IsFalse(state.MarkPublished.IsEnabled);
            Assert.IsFalse(state.MarkPublished.IsPrimary);
            Assert.AreEqual("Còn thiếu cho Đánh dấu: copy hoặc lưu file bản này", state.ReasonText);
            Assert.IsFalse(state.ReasonIsBlocked);
            Assert.IsTrue(state.IsCompact);
            Assert.AreEqual("Parser của game xác nhận: giữ 8/8 mục, khớp Kiểm lịch", state.Row(ExportGateRowKind.ParserReadBack).Text);
            Assert.AreEqual(string.Empty, state.Row(ExportGateRowKind.ParserReadBack).MetaText);
            Assert.AreEqual("Đã xem 1/1 thay đổi bắt buộc", state.Row(ExportGateRowKind.RequiredChangesReviewed).Text);
            Assert.AreEqual(HealthState.Ok, state.Health.State);
            Assert.IsFalse(state.HasStatus(ExportGateStatusCode.FirstPublish));
        }

        private static void AssertStateExported()
        {
            ExportGateInput ready = ReadyInput(FixedDraft(), 1);
            ExportGateState state = ExportGateModel.Evaluate(ready.WithLastExportedSha256Hex(ready.Json.Sha256Hex));

            Assert.AreEqual(ExportGateStatusCode.Exported, state.StatusCode);
            Assert.AreEqual("c", ExportGateModel.LetterOf(state.StatusCode));
            Assert.IsTrue(state.Copy.IsEnabled);
            Assert.IsFalse(state.Copy.IsPrimary);
            Assert.IsTrue(state.SaveFile.IsEnabled);
            Assert.IsTrue(state.MarkPublished.IsEnabled);
            Assert.IsTrue(state.MarkPublished.IsPrimary, "copy đúng sha xong thì Đánh dấu đã đăng… thành nút đậm");
            Assert.AreEqual("Ghi dấu đã đăng cho sha " + ready.Json.ShortSha, state.MarkPublished.Tooltip);
            Assert.AreEqual(string.Empty, state.ReasonText);
            Assert.IsFalse(state.IsCompact, "(c) vẽ đủ 5 dòng Ok + remote chưa kiểm");
            for (int index = 0; index < state.Rows.Count; index++)
            {
                if (state.Rows[index].CanBlockCopy) Assert.AreEqual(ExportGateRowState.Ok, state.Rows[index].State, state.Rows[index].Kind.ToString());
            }

            // Nháp đổi sau lần copy: sha khác → quay về (b), Đánh dấu nói rõ vì sao tắt.
            ExportGateState draftChanged = ExportGateModel.Evaluate(ready.WithLastExportedSha256Hex("91aa02" + new string('0', 58)));
            Assert.AreEqual(ExportGateStatusCode.Ready, draftChanged.StatusCode);
            Assert.IsFalse(draftChanged.MarkPublished.IsEnabled);
            Assert.AreEqual("Nháp đã đổi sau lần copy 91aa02 — copy bản mới rồi dán lại", draftChanged.MarkPublished.Tooltip);
        }

        private static void AssertStateStaleCheck()
        {
            ExportGateInput fresh = ReadyInput(FixedDraft(), 1);
            LiveEventCalendarCheckReport report = fresh.LastReport;

            ExportGateState stale = ExportGateModel.Evaluate(fresh
                .WithCheck(report, true, false, report.RuleResults.Count, report.RuleResults.Count, StaleCheckedAtUtc)
                .WithStaleCause(DraftChangedAtUtc, null));
            Assert.AreEqual(ExportGateStatusCode.StaleCheck, stale.StatusCode);
            AssertCopyAndSaveDisabled(stale);
            ExportGateRow freshness = stale.Row(ExportGateRowKind.CheckFreshness);
            Assert.AreEqual(ExportGateRowState.NotMeasured, freshness.State);
            Assert.AreEqual("Kiểm lịch cũ: lịch đổi lúc 08:46:50, sau lần kiểm 08:46:30", freshness.Text);
            Assert.AreEqual(ExportGateRowAction.StartCheck, freshness.Action);
            Assert.AreEqual("Kiểm lại (F5)", freshness.ActionText);
            Assert.AreEqual("tự chạy khi mở màn", freshness.MetaText);
            Assert.AreEqual(ExportGateRowState.NotMeasured, stale.Row(ExportGateRowKind.NoDroppedEntries).State, "kết quả cũ không được trông như vẫn đúng (PD-23)");
            Assert.AreEqual("Chặn: Kiểm lịch cũ", stale.ReasonText);
            Assert.AreEqual(HealthState.NotMeasured, stale.Health.State);
            Assert.AreEqual(freshness.Text, stale.Health.Reason);

            ExportGateState milestone = ExportGateModel.Evaluate(fresh
                .WithCheck(report, true, false, report.RuleResults.Count, report.RuleResults.Count, StaleCheckedAtUtc)
                .WithStaleCause(null, new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual("Kiểm lịch cũ: đã qua mốc 14/9 00:00 UTC sau lần kiểm 08:46:30", milestone.Row(ExportGateRowKind.CheckFreshness).Text);

            // Không rõ vì sao cũ (vd domain reload cắt ngang): câu trung tính, không khẳng định lịch đã đổi.
            ExportGateState unknownCause = ExportGateModel.Evaluate(fresh
                .WithCheck(report, true, false, report.RuleResults.Count, report.RuleResults.Count, StaleCheckedAtUtc));
            Assert.AreEqual("Kiểm lịch cũ: kết quả lúc 08:46:30 có thể không còn đúng", unknownCause.Row(ExportGateRowKind.CheckFreshness).Text);

            ExportGateState running = ExportGateModel.Evaluate(fresh.WithCheck(report, true, true, 7, 12, StaleCheckedAtUtc));
            Assert.AreEqual(ExportGateStatusCode.StaleCheck, running.StatusCode);
            Assert.AreEqual(ExportGateRowState.Running, running.Row(ExportGateRowKind.CheckFreshness).State);
            Assert.AreEqual("Đang kiểm lại 7/12 luật…", running.Row(ExportGateRowKind.CheckFreshness).Text);
            Assert.AreEqual("Chặn: đang kiểm lại lịch", running.ReasonText);
            AssertCopyAndSaveDisabled(running);

            ExportGateState neverChecked = ExportGateModel.Evaluate(fresh.WithCheck(null, false, false, 0, 12, null));
            Assert.AreEqual(ExportGateStatusCode.StaleCheck, neverChecked.StatusCode);
            Assert.AreEqual("Kiểm lịch chưa chạy lần nào", neverChecked.Row(ExportGateRowKind.CheckFreshness).Text);
            Assert.AreEqual(ExportGateRowState.NotMeasured, neverChecked.Row(ExportGateRowKind.NoDroppedEntries).State);
            AssertCopyAndSaveDisabled(neverChecked);
        }

        private static void AssertStateMarkedPublished()
        {
            ExportGateInput input = ReadyInput(NoChangeDraft(), 0);
            Assert.AreEqual(input.ActiveStamp.Sha256Hex, input.Json.Sha256Hex, "tiền điều kiện: nháp đúng byte bản chụp của dấu");

            ExportGateState state = ExportGateModel.Evaluate(input.WithLastExportedSha256Hex(input.Json.Sha256Hex));

            Assert.AreEqual(ExportGateStatusCode.MarkedPublished, state.StatusCode);
            Assert.AreEqual("e", ExportGateModel.LetterOf(state.StatusCode));
            Assert.IsFalse(state.MarkPublished.IsEnabled, "vừa ghi dấu: không còn gì để ghi");
            Assert.IsTrue(state.MarkPublished.IsPrimary);
            Assert.IsTrue(state.Copy.IsEnabled);
            Assert.AreEqual("Chưa đối chiếu với Firebase", state.Row(ExportGateRowKind.RemoteSnapshot).Text);
            Assert.AreEqual("Dán JSON đang chạy…", state.Row(ExportGateRowKind.RemoteSnapshot).ActionText);
            Assert.AreEqual(HealthState.Ok, state.Health.State);
        }

        private static void AssertStateNoChanges()
        {
            ExportGateState state = ExportGateModel.Evaluate(ReadyInput(NoChangeDraft(), 0));

            Assert.AreEqual(ExportGateStatusCode.NoChanges, state.StatusCode);
            Assert.AreEqual("f", ExportGateModel.LetterOf(state.StatusCode));
            Assert.IsTrue(state.Copy.IsEnabled);
            Assert.IsFalse(state.MarkPublished.IsEnabled);
            Assert.IsTrue(state.MarkPublished.IsPrimary);
            Assert.AreEqual("Không có thay đổi để ghi", state.ReasonText);
            AssertStartsWith("Không có gì mới so với 11/9 16:20 · sha ", state.DiffEmptyText);
            Assert.AreEqual("Không có thay đổi bắt buộc cần xem", state.Row(ExportGateRowKind.RequiredChangesReviewed).Text);
        }

        private static void AssertStateFirstPublish()
        {
            ExportGateInput input = ReadyInput(DocumentWithoutStamps(FixedDraft()), 0);
            ExportGateState state = ExportGateModel.Evaluate(input);

            Assert.AreEqual(ExportGateStatusCode.Ready, state.StatusCode);
            Assert.IsTrue(state.HasStatus(ExportGateStatusCode.FirstPublish));
            Assert.AreEqual(ExportGateStatusCode.FirstPublish, state.StatusCodes[1]);
            Assert.AreEqual("g", ExportGateModel.LetterOf(ExportGateStatusCode.FirstPublish));
            Assert.AreEqual(ExportGateRowState.Hidden, state.Row(ExportGateRowKind.RequiredChangesReviewed).State);
            Assert.IsFalse(state.BlocksCopy(ExportGateRowKind.RequiredChangesReviewed));
            Assert.AreEqual("Chưa có dấu đã đăng nào — lần này sẽ là bản so đầu tiên", state.DiffEmptyText);
            Assert.IsTrue(state.Copy.IsEnabled);

            // Lần đăng đầu luôn có thứ để ghi: copy xong là Đánh dấu bật.
            ExportGateState exported = ExportGateModel.Evaluate(input.WithLastExportedSha256Hex(input.Json.Sha256Hex));
            Assert.IsTrue(exported.MarkPublished.IsEnabled);
            Assert.IsTrue(exported.HasStatus(ExportGateStatusCode.FirstPublish));

            // Lần đăng đầu mà còn đợt bị bỏ: mã chính (a), (g) đi kèm.
            ExportGateState blocked = ExportGateModel.Evaluate(ReadyInput(DocumentWithoutStamps(LiveOpsDesignSample.Document), 0));
            Assert.AreEqual(ExportGateStatusCode.Blocked, blocked.StatusCode);
            Assert.IsTrue(blocked.HasStatus(ExportGateStatusCode.FirstPublish));
            Assert.AreEqual("Chặn: 2 đợt bị bỏ", blocked.ReasonText);
        }

        private static void AssertStateReadBackFailed()
        {
            LiveEventCalendarDocument draft = FixedDraft();
            LiveEventCalendarJsonText written = LiveEventCalendarJsonWriter.Write(draft, LiveEventCalendarJsonFormat.Version2);
            string truncated = written.Text.Substring(0, written.Text.Length - 1);
            string runtimeMessage = JsonLiveEventCalendarParser.ParseDocument(truncated).ReadErrorText;
            Assert.IsNotEmpty(runtimeMessage, "tiền điều kiện: JSON cụt phải làm parser của game báo lỗi");

            ExportGateInput input = WithDesignCheckAndStamp(InputFor(draft, LiveEventCalendarJsonFormat.Version2, text => text.Substring(0, text.Length - 1)), 1, draft);
            Assert.IsFalse(input.ReadBackIsReadable);
            ExportGateState state = ExportGateModel.Evaluate(input.WithLastExportedSha256Hex(input.Json.Sha256Hex));

            Assert.AreEqual(ExportGateStatusCode.ReadBackFailed, state.StatusCode);
            Assert.AreEqual("h", ExportGateModel.LetterOf(state.StatusCode));
            AssertAllButtonsDisabled(state);
            ExportGateRow parser = state.Row(ExportGateRowKind.ParserReadBack);
            Assert.AreEqual(ExportGateRowState.Blocked, parser.State);
            Assert.AreEqual("Parser của game không đọc lại được JSON này — lỗi của hub", parser.Text);
            Assert.AreEqual("Copy lỗi", parser.ActionText);
            AssertStartsWith("Chặn: parser không đọc lại được JSON", state.ReasonText);

            ExportGateReadBackFailure failure = state.ReadBackFailure;
            Assert.IsNotNull(failure);
            Assert.AreEqual("JSON do hub tạo không đọc lại được — lỗi của hub, không phải của lịch", failure.SymptomText);
            Assert.AreEqual(runtimeMessage, failure.RuntimeMessage, "message runtime nguyên văn");
            Assert.AreEqual("Báo dev kèm lỗi; không dán JSON này lên Firebase. Mọi nút xuất disabled cho tới khi parser đọc được.", failure.NoteText);
            StringAssert.Contains(runtimeMessage, failure.ErrorReportText);
            Assert.AreEqual(failure.ErrorReportText, state.ReadBackErrorReportText);
            // JSON thật hợp lệ nên bộ dò không tìm được vị trí: không bịa "dòng 65", không vẽ nút mở dòng.
            Assert.AreEqual(0, failure.ErrorLine);
            Assert.AreEqual(string.Empty, failure.PositionText);
            Assert.AreEqual(string.Empty, failure.OpenLineButtonText);

            ExportGateReadBackFailure located = ExportGateModel.Evaluate(input.WithReadBackErrorLocation(65, 1)).ReadBackFailure;
            Assert.AreEqual("ở dòng 65, cột 1", located.PositionText);
            Assert.AreEqual("Mở JSON ở dòng 65", located.OpenLineButtonText);
            Assert.AreEqual(HealthState.Blocked, state.Health.State);
        }

        private static void AssertStateRestoring()
        {
            ExportGateInput input = ReadyInput(FixedDraft(), 1);
            PublishedCalendarStamp restoredFrom = input.ActiveStamp;
            ExportGateState state = ExportGateModel.Evaluate(input.WithRestoring(restoredFrom, true));

            Assert.AreEqual(ExportGateStatusCode.Ready, state.StatusCode, "(i) chỉ thêm note, không đổi ba nút");
            Assert.IsTrue(state.HasStatus(ExportGateStatusCode.Restoring));
            Assert.AreEqual("i", ExportGateModel.LetterOf(ExportGateStatusCode.Restoring));
            Assert.AreEqual("Nháp đang là bản khôi phục từ 11/9 16:20", state.RestoringNoticeText);
            Assert.AreEqual("Hoàn tác khôi phục", state.UndoRestoreText);
            Assert.IsTrue(state.CanUndoRestore);

            Assert.IsFalse(ExportGateModel.Evaluate(input.WithRestoring(restoredFrom, false)).CanUndoRestore, "Undo group không còn trên đỉnh → disabled");
            ExportGateState notRestoring = ExportGateModel.Evaluate(input);
            Assert.IsFalse(notRestoring.HasStatus(ExportGateStatusCode.Restoring));
            Assert.AreEqual(string.Empty, notRestoring.UndoRestoreText);
        }

        private static void AssertStateFormat1()
        {
            ExportGateInput input = WithDesignCheckAndStamp(InputFor(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version1, null), 0,
                LiveOpsDesignSample.Document);
            ExportGateState state = ExportGateModel.Evaluate(input);

            Assert.IsTrue(state.HasStatus(ExportGateStatusCode.Format1));
            Assert.AreEqual("j", ExportGateModel.LetterOf(ExportGateStatusCode.Format1));
            Assert.AreEqual(ExportGateStatusCode.Blocked, state.StatusCode, "định dạng 1 vẫn bị chặn bởi 2 đợt bị bỏ");
            ExportGateRow parser = state.Row(ExportGateRowKind.ParserReadBack);
            Assert.AreEqual(ExportGateRowState.Ok, parser.State, "định dạng 1 bỏ luật lặp theo thiết kế — không phải lệch");
            Assert.AreEqual("Parser của game xác nhận: giữ 4/6 mục, khớp Kiểm lịch", parser.Text);
            Assert.AreEqual("Định dạng 1 không có recurring: 2 luật lặp sẽ không được xuất, game chỉ thấy 6 đợt cố định", state.Format1NoticeText);
            Assert.AreEqual(string.Empty, ExportGateModel.Evaluate(ReadyInput(FixedDraft(), 1)).Format1NoticeText);
        }

        private static void AssertStateRequiredReviewOnly()
        {
            ExportGateState state = ExportGateModel.Evaluate(ReadyInput(FixedDraft(), 0));

            Assert.AreEqual(ExportGateStatusCode.RequiredReviewOnly, state.StatusCode);
            Assert.AreEqual("k", ExportGateModel.LetterOf(state.StatusCode));
            Assert.IsFalse(state.Copy.IsEnabled);
            Assert.IsTrue(state.Copy.IsPrimary, "(k) Copy JSON chính-disabled");
            Assert.AreEqual("Chặn: 1 thay đổi bắt buộc chưa xem", state.ReasonText);
            Assert.AreEqual(ExportGateRowState.Ok, state.Row(ExportGateRowKind.NoDroppedEntries).State);
            Assert.AreEqual(ExportGateRowState.Blocked, state.Row(ExportGateRowKind.RequiredChangesReviewed).State);
            Assert.AreEqual(ExportGateRowAction.ShowPublishedDiff, state.Row(ExportGateRowKind.RequiredChangesReviewed).Action);
            Assert.AreEqual(HealthState.Blocked, state.Health.State);
            Assert.AreEqual("1 cần xem", state.Health.Badge);
            Assert.AreEqual(string.Empty, state.CopyBlockColumnTextFor(LiveEventCalendarConsequence.Dropped, false));
            Assert.AreEqual("chặn Copy JSON", state.CopyBlockColumnTextFor(LiveEventCalendarConsequence.ProgressLost, false));
        }

        private static void AssertStateParserMismatch()
        {
            // Bộ đọc lại "sửa hộ" giờ hỏng của lava-quest-2026-10: parser giữ 7/8 trong khi Kiểm lịch báo 2 bị bỏ — đúng câu Hình 19.
            ExportGateInput input = WithDesignCheckAndStamp(InputFor(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2,
                text => text.Replace("\"2026-10-3\"", "\"2026-10-03T00:00:00Z\"")), 0, LiveOpsDesignSample.Document);
            ExportGateState state = ExportGateModel.Evaluate(input);

            Assert.AreEqual(ExportGateStatusCode.ReadBackMismatch, state.StatusCode);
            Assert.AreEqual("parser-mismatch", ExportGateModel.LetterOf(state.StatusCode));
            ExportGateRow parser = state.Row(ExportGateRowKind.ParserReadBack);
            Assert.AreEqual(ExportGateRowState.Blocked, parser.State);
            Assert.AreEqual("Parser giữ 7/8 nhưng Kiểm lịch báo 2 bị bỏ — lỗi của hub", parser.Text);
            Assert.AreEqual("Copy lỗi", parser.ActionText);
            StringAssert.Contains("lava-quest-2026-10", parser.MetaText);
            StringAssert.Contains("không đọc được", parser.MetaText);
            AssertAllButtonsDisabled(state);
            AssertStartsWith("Chặn: parser lệch Kiểm lịch", state.ReasonText);
            Assert.AreEqual(HealthState.Blocked, state.Health.State);
            Assert.IsNull(state.ReadBackFailure, "lệch không phải (h): parser vẫn đọc được");
        }

        // ------------------------------------------------------------------------------------------------------- dựng dữ liệu

        private static LiveOpsHubFormat Format => new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset);

        /// <summary>Nháp → JSON → parser của game đọc lại qua adapter phiên dùng (V-16); <paramref name="rewriteBeforeRead"/> null = đọc nguyên văn.</summary>
        private static ExportGateInput InputFor(LiveEventCalendarDocument draft, LiveEventCalendarJsonFormat jsonFormat, Func<string, string> rewriteBeforeRead)
        {
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(draft, jsonFormat);
            var readBackPort = new RewritingLiveOpsHubJsonReadBack(rewriteBeforeRead ?? (text => text));
            LiveEventCalendarDocumentParseResult readDocument = readBackPort.ReadBackDocument(json.Text);
            LiveEventCalendarParseResult readBack = readBackPort.ReadBack(json.Text);
            return new ExportGateInput(json, LiveEventCalendarCompiler.CompileInExportOrder(draft), readBack, readDocument.IsReadable,
                readDocument.ReadErrorText, Format);
        }

        /// <summary>Đầu vào như phiên dựng lúc mở màn: kiểm mới (08:46:58), dấu mới nhất là bản so, <paramref name="reviewedRequiredCount"/> mục bắt buộc đã tick.</summary>
        private static ExportGateInput ReadyInput(LiveEventCalendarDocument draft, int reviewedRequiredCount)
        {
            return WithDesignCheckAndStamp(InputFor(draft, LiveEventCalendarJsonFormat.Version2, null), reviewedRequiredCount, draft);
        }

        private static ExportGateInput WithDesignCheckAndStamp(ExportGateInput input, int reviewedRequiredCount, LiveEventCalendarDocument draft)
        {
            PublishedCalendarStamp stamp = draft.LatestStamp;
            var contextBuilder = new LiveEventCalendarCheckContextBuilder(draft, LiveOpsDesignSample.NowUtc);
            if (stamp != null) contextBuilder.WithPublishedBaseline(BaselineOf(stamp));
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(contextBuilder.Build());

            ExportGateInput checkedInput = input.WithCheck(report, false, false, report.RuleResults.Count, report.RuleResults.Count, CheckedAtUtc);
            return stamp == null ? checkedInput : checkedInput.WithPublished(stamp, DiffOf(draft), reviewedRequiredCount);
        }

        private static LiveEventCalendarDiffResult DiffOf(LiveEventCalendarDocument draft)
        {
            LiveEventCalendarDocument baseline = BaselineOf(draft.LatestStamp);
            return LiveEventCalendarDiff.Compare(baseline, draft, LiveOpsDesignSample.NowUtc);
        }

        /// <summary>Bản so như phiên dựng: đọc bản chụp JSON của dấu bằng parser game (V-3: không có định nghĩa loại).</summary>
        private static LiveEventCalendarDocument BaselineOf(PublishedCalendarStamp stamp)
        {
            return JsonLiveEventCalendarParser.ParseDocument(stamp.SnapshotJson).Document;
        }

        /// <summary>Nháp mẫu sau khi sửa cả hai đợt bị bỏ [SD2 §3.4] "…giữ 8/8 mục": dời hunt-0916-bonus hết chồng, viết đúng endUtc lava-quest-2026-10.</summary>
        private static LiveEventCalendarDocument FixedDraft()
        {
            LiveEventCalendarDocument sample = LiveOpsDesignSample.Document;
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(sample.RemoteConfigKey);
            for (int index = 0; index < sample.EventTypes.Count; index++) builder.WithEventType(sample.EventTypes[index]);
            for (int index = 0; index < sample.RecurringRules.Count; index++) builder.WithRecurringRule(sample.RecurringRules[index]);
            for (int index = 0; index < sample.FixedEvents.Count; index++)
            {
                FixedLiveEventEntry entry = sample.FixedEvents[index];
                if (entry.EntryKey == LiveOpsDesignSample.HuntBonusEntryKey)
                {
                    entry = new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType, "2026-09-17T00:00:00Z", "2026-09-18T00:00:00Z", entry.ConfigKey);
                }
                else if (entry.EntryKey == LiveOpsDesignSample.LavaQuestLateEntryKey)
                {
                    entry = new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType, entry.StartUtcText, "2026-10-03T00:00:00Z", entry.ConfigKey);
                }
                builder.WithFixedEvent(entry);
            }
            for (int index = 0; index < sample.PublishedStamps.Count; index++) builder.WithPublishedStamp(sample.PublishedStamps[index]);
            return builder.Build();
        }

        /// <summary>Nháp trùng bản đã đăng 11/9 16:20 (đợt + luật đọc từ bản chụp, loại + dấu từ mẫu) — trạng thái (e)/(f).</summary>
        private static LiveEventCalendarDocument NoChangeDraft()
        {
            LiveEventCalendarDocument sample = LiveOpsDesignSample.Document;
            LiveEventCalendarDocument baseline = BaselineOf(sample.LatestStamp);
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(sample.RemoteConfigKey);
            for (int index = 0; index < sample.EventTypes.Count; index++) builder.WithEventType(sample.EventTypes[index]);
            for (int index = 0; index < baseline.RecurringRules.Count; index++) builder.WithRecurringRule(baseline.RecurringRules[index]);
            for (int index = 0; index < baseline.FixedEvents.Count; index++) builder.WithFixedEvent(baseline.FixedEvents[index]);
            for (int index = 0; index < sample.PublishedStamps.Count; index++) builder.WithPublishedStamp(sample.PublishedStamps[index]);
            return builder.Build();
        }

        private static LiveEventCalendarDocument DocumentWithoutStamps(LiveEventCalendarDocument document)
        {
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(document.RemoteConfigKey);
            for (int index = 0; index < document.EventTypes.Count; index++) builder.WithEventType(document.EventTypes[index]);
            for (int index = 0; index < document.RecurringRules.Count; index++) builder.WithRecurringRule(document.RecurringRules[index]);
            for (int index = 0; index < document.FixedEvents.Count; index++) builder.WithFixedEvent(document.FixedEvents[index]);
            return builder.Build();
        }

        /// <summary>Bản remote như JSON dán vào: không có định nghĩa loại (<paramref name="keepEventTypes"/> false), thêm một đợt.</summary>
        private static LiveEventCalendarDocument WithExtraEvent(LiveEventCalendarDocument document, FixedLiveEventEntry extra, bool keepEventTypes)
        {
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(document.RemoteConfigKey);
            if (keepEventTypes)
            {
                for (int index = 0; index < document.EventTypes.Count; index++) builder.WithEventType(document.EventTypes[index]);
            }
            for (int index = 0; index < document.RecurringRules.Count; index++) builder.WithRecurringRule(document.RecurringRules[index]);
            for (int index = 0; index < document.FixedEvents.Count; index++) builder.WithFixedEvent(document.FixedEvents[index]);
            builder.WithFixedEvent(extra);
            return builder.Build();
        }

        private static void AssertStartsWith(string expectedPrefix, string actual)
        {
            Assert.IsTrue(actual.StartsWith(expectedPrefix, StringComparison.Ordinal), "mong bắt đầu bằng \"" + expectedPrefix + "\", thực tế \"" + actual + "\"");
        }

        private static void AssertAllButtonsDisabled(ExportGateState state)
        {
            AssertCopyAndSaveDisabled(state);
            Assert.IsFalse(state.MarkPublished.IsEnabled, "Đánh dấu đã đăng…");
        }

        private static void AssertCopyAndSaveDisabled(ExportGateState state)
        {
            Assert.IsFalse(state.Copy.IsEnabled, "Copy JSON");
            Assert.IsFalse(state.SaveFile.IsEnabled, "Lưu file…");
            Assert.IsTrue(state.IsCopyBlocked);
        }
    }
}
