using System;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Nháp tại ô của màn Luật lặp (mục 7.4, [SD1 §4.2]): gõ một giá trị làm đổi id hay giờ khép của lần lặp ĐANG CHẠY thì
    /// giá trị đó chưa được ghi — nó chỉ là nháp, và mức hỏi do <see cref="LiveOpsConfirmationPolicy"/> quyết.
    /// <para>
    /// Điểm đắt nhất ở đây là C-6: lần lặp "đang chạy" lấy từ BẢN ĐÃ ĐĂNG khi bản so có luật đó, nên chuỗi phải gõ là id
    /// người chơi đang giữ (<c>weekly-pass-35</c>), không phải id của nháp (<c>pass-35</c>) — và hoàn tiền tố về đúng bản đã
    /// đăng thì không bị hỏi lần nữa.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class RecurringPrefixDraftTests
    {
        private const string WeeklyPassType = "weekly-pass";
        private const string PublishedPrefix = "weekly-pass-";
        private const string DraftPrefix = "pass-";

        /// <summary>Tiền tố thứ BA: nháp đã lệch bản đã đăng, nên id phải gõ chỉ đúng khi thật sự đọc bản so (C-6).</summary>
        private const string TypedPrefix = "bonus-";

        private const string AnchorUtcText = "2026-01-05T00:00:00Z";
        private const string AssetFileName = "Main.asset";
        private const int WeekHours = 168;
        private const string RunningEventId = "weekly-pass-35";
        private const string DraftRunningEventId = "pass-35";
        private const string TypedRunningEventId = "bonus-35";

        /// <summary>Rút ngắn 168 → 120 giờ: đợt 35 mở 7/9 00:00 nên khép 12/9 00:00, TRƯỚC "lúc này" 13/9 08:47.</summary>
        private const int ShortenedActiveHours = 120;

        private const string ShortenedEndText = "12/9 00:00";

        private static readonly LiveOpsHubFormat Format = new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset);

        [Test]
        public void PrefixChangeWithRunning_DraftUntilTypedConfirm()
        {
            // BA tiền tố khác nhau: nháp đang là pass-, bản đã đăng còn weekly-pass-, người dùng gõ bonus-. Nếu code lấy lần
            // lặp đang chạy từ NHÁP (bỏ C-6) thì chuỗi phải gõ sẽ là pass-35 và khẳng định dưới đây đỏ ngay.
            LiveEventCalendarDocument draftDocument = DocumentWithPrefix(DraftPrefix, WeekHours, WeekHours);
            LiveEventCalendarDocument published = DocumentWithPrefix(PublishedPrefix, WeekHours, WeekHours);
            RecurringLiveEventRule candidate = RuleOf(draftDocument).WithIdPrefix(TypedPrefix);

            RecurringPrefixDraft draft = RecurringPrefixDraft.For(draftDocument, published, LiveOpsDesignSample.NowUtc,
                AssetFileName, RecurringRuleFields.IdPrefix, LiveOpsEditOperation.ChangeRecurringIdentity, candidate, Format);

            Assert.IsTrue(draft.NeedsConfirmation, "đổi tiền tố khi weekly-pass-35 đang chạy phải đi qua hai bước");
            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, draft.Requirement, "đổi id đợt đang chạy = hộp cấp 2 gõ tên (bảng 7.0)");
            Assert.IsTrue(draft.ChangesRunningId);
            Assert.AreEqual(RunningEventId, draft.RunningEventId, "id người chơi đang giữ lấy từ bản đã đăng (C-6)");
            Assert.AreNotEqual(DraftRunningEventId, draft.RunningEventId, "id của NHÁP không phải id người chơi đang giữ");
            Assert.AreEqual(TypedRunningEventId, draft.NewRunningEventId);

            LiveOpsConfirmRequest request = draft.BuildConfirmRequest();
            Assert.AreEqual(LiveOpsConfirmLevel.TypeToConfirm, request.Level);
            Assert.AreEqual(RunningEventId, request.TypeToConfirmText, "chuỗi phải gõ là id người chơi đang giữ, không phải id của nháp");
            StringAssert.Contains(RunningEventId, request.Body);
            StringAssert.Contains(TypedRunningEventId, request.Body);

            // Nháp KHÔNG đụng tài liệu: Lịch, Kiểm lịch và Xuất JSON vẫn thấy giá trị cũ tới khi người dùng đi hết bước hai.
            Assert.AreEqual(DraftPrefix, RuleOf(draftDocument).EffectiveIdPrefix);
            StringAssert.Contains(AssetFileName, draft.CellNotice);
            StringAssert.Contains(DraftPrefix, draft.CellNotice);
        }

        /// <summary>
        /// Không có bản so: lần lặp đang chạy lấy từ chính nháp — cùng một lối gọi phải ra id KHÁC. Đây là vế còn lại của
        /// C-6, và là lý do khẳng định ở test trên không thể xanh nhờ trùng chữ.
        /// </summary>
        [Test]
        public void PrefixChangeWithoutBaseline_TypesDraftRunningId()
        {
            LiveEventCalendarDocument draftDocument = DocumentWithPrefix(DraftPrefix, WeekHours, WeekHours);
            RecurringLiveEventRule candidate = RuleOf(draftDocument).WithIdPrefix(TypedPrefix);

            RecurringPrefixDraft draft = RecurringPrefixDraft.For(draftDocument, null, LiveOpsDesignSample.NowUtc,
                AssetFileName, RecurringRuleFields.IdPrefix, LiveOpsEditOperation.ChangeRecurringIdentity, candidate, Format);

            Assert.AreEqual(DraftRunningEventId, draft.RunningEventId, "chưa có dấu đã đăng thì nháp trước khi sửa là nguồn duy nhất");
            Assert.AreEqual(DraftRunningEventId, draft.BuildConfirmRequest().TypeToConfirmText);
        }

        [Test]
        public void ActiveHoursShortenRunning_Level1()
        {
            LiveEventCalendarDocument draftDocument = DocumentWithPrefix(PublishedPrefix, WeekHours, WeekHours);
            RecurringLiveEventRule candidate = RuleOf(draftDocument).WithActiveHours(ShortenedActiveHours);

            RecurringPrefixDraft draft = RecurringPrefixDraft.For(draftDocument, null, LiveOpsDesignSample.NowUtc,
                AssetFileName, RecurringRuleFields.ActiveHours, LiveOpsEditOperation.ChangeRecurringActiveHours, candidate, Format);

            Assert.IsTrue(draft.NeedsConfirmation, "rút ngắn đợt đang chạy vẫn phải hỏi");
            Assert.AreEqual(LiveOpsConfirmRequirement.Level1, draft.Requirement, "id giữ nguyên nên là hộp cấp 1, không bắt gõ tên");
            Assert.IsFalse(draft.ChangesRunningId);
            LiveOpsConfirmRequest request = draft.BuildConfirmRequest();
            Assert.AreEqual(LiveOpsConfirmLevel.Level1, request.Level);
            Assert.AreEqual(string.Empty, request.TypeToConfirmText, "cấp 1 không vẽ ô gõ");
            StringAssert.Contains(RunningEventId, request.Body);
            // Giờ khép MỚI (12/9 00:00) nằm TRƯỚC "lúc này": lần lặp mới không còn ở giai đoạn Active nên tìm theo giai đoạn
            // sẽ ra rỗng — đúng chỗ hộp "rút ngắn" mất luôn con số mà nó sinh ra để nói (mục 7.4).
            StringAssert.Contains(ShortenedEndText, request.Body, "hộp cấp 1 phải nêu giờ khép mới, không để chỗ trống");
            StringAssert.Contains(ShortenedEndText, draft.ConsequenceText);
        }

        /// <summary>
        /// Ca không có gì thay chỗ: dời neo vào khoảng nghỉ nên luật mới KHÔNG có lần lặp nào đang chạy. Câu hậu quả và thân
        /// hộp phải nói đợt biến mất, chứ không in một id rỗng vào "sẽ mang id {2}".
        /// </summary>
        [Test]
        public void IdentityChangeWithoutReplacement_SaysOccurrenceDisappears()
        {
            LiveEventCalendarDocument draftDocument = DocumentWithPrefix(PublishedPrefix, 24, 20);
            // Kiểu sky-race 24/20: neo 00:00 thì 08:47 nằm trong đợt. Dời neo sang 10:00 đẩy khoảng nghỉ (06:00–10:00)
            // trùm đúng "lúc này", nên luật MỚI không có lần lặp nào đang chạy — không ai thay chỗ đợt cũ.
            RecurringLiveEventRule candidate = RuleOf(draftDocument).WithAnchor("2026-01-05T10:00:00Z");

            RecurringPrefixDraft draft = RecurringPrefixDraft.For(draftDocument, null, LiveOpsDesignSample.NowUtc,
                AssetFileName, RecurringRuleFields.Anchor, LiveOpsEditOperation.ChangeRecurringIdentity, candidate, Format);

            Assert.IsTrue(draft.NeedsConfirmation);
            Assert.IsFalse(draft.HasReplacement, "luật mới không có lần lặp nào đang chạy lúc này");
            Assert.AreEqual(string.Empty, draft.NewRunningEventId);
            LiveOpsConfirmRequest request = draft.BuildConfirmRequest();
            StringAssert.Contains(draft.RunningEventId, request.Body);
            StringAssert.Contains(LiveOpsHubStrings.KitUnknownPlayerCountSentence, request.Body);
            StringAssert.Contains(NoReplacementMarker(LiveOpsHubStrings.RecurringConfirmNoReplacementBodyFormat), request.Body,
                "thân hộp phải là câu 'đợt biến mất', không phải câu 'sẽ mang id ___'");
            StringAssert.Contains(NoReplacementMarker(LiveOpsHubStrings.RecurringNoReplacementConsequenceFormat), draft.ConsequenceText,
                "câu hậu quả dưới ô cũng vậy");
        }

        /// <summary>
        /// Mẩu chữ nằm giữa <c>{1}</c> và <c>{0}</c> của câu "không ai thay chỗ" — đoạn chỉ câu này mới có, nên khẳng định
        /// bắt đúng câu mà không phải chép lại chữ tiếng Việt vào test (đổi câu ở catalog thì test theo, không đỏ giả).
        /// </summary>
        private static string NoReplacementMarker(string format)
        {
            int start = format.IndexOf("{1}", StringComparison.Ordinal) + "{1}".Length;
            int end = format.IndexOf("{0}", start, StringComparison.Ordinal);
            return end > start ? format.Substring(start, end - start) : format.Substring(start);
        }

        [Test]
        public void RevertToPublishedPrefix_NeedsNoConfirmation()
        {
            LiveEventCalendarDocument draftDocument = DocumentWithPrefix(DraftPrefix, WeekHours, WeekHours);
            LiveEventCalendarDocument published = DocumentWithPrefix(PublishedPrefix, WeekHours, WeekHours);
            RecurringLiveEventRule candidate = RuleOf(draftDocument).WithIdPrefix(PublishedPrefix);

            RecurringPrefixDraft draft = RecurringPrefixDraft.For(draftDocument, published, LiveOpsDesignSample.NowUtc,
                AssetFileName, RecurringRuleFields.IdPrefix, LiveOpsEditOperation.ChangeRecurringIdentity, candidate, Format);

            Assert.IsFalse(draft.NeedsConfirmation,
                "hoàn về đúng tiền tố đã đăng là trả id người chơi đang giữ về chỗ cũ — hỏi nữa là hỏi thừa (C-6)");
        }

        [Test]
        public void NoRunningOccurrence_NeedsNoConfirmation()
        {
            // Chu kỳ 24 giờ, chạy 4 giờ: 08:47 UTC nằm trong khoảng nghỉ nên không ai đang chơi dở.
            LiveEventCalendarDocument draftDocument = DocumentWithPrefix(PublishedPrefix, 24, 4);
            RecurringLiveEventRule candidate = RuleOf(draftDocument).WithIdPrefix(DraftPrefix);

            RecurringPrefixDraft draft = RecurringPrefixDraft.For(draftDocument, null, LiveOpsDesignSample.NowUtc,
                AssetFileName, RecurringRuleFields.IdPrefix, LiveOpsEditOperation.ChangeRecurringIdentity, candidate, Format);

            Assert.IsFalse(draft.NeedsConfirmation, "không có lần lặp đang chạy thì ghi ngay khi commit field (mục 7.4)");
        }

        [Test]
        public void ActiveHoursExtendRunning_NeedsNoConfirmation()
        {
            LiveEventCalendarDocument draftDocument = DocumentWithPrefix(PublishedPrefix, 240, WeekHours);
            RecurringLiveEventRule candidate = RuleOf(draftDocument).WithActiveHours(200);

            RecurringPrefixDraft draft = RecurringPrefixDraft.For(draftDocument, null, LiveOpsDesignSample.NowUtc,
                AssetFileName, RecurringRuleFields.ActiveHours, LiveOpsEditOperation.ChangeRecurringActiveHours, candidate, Format);

            Assert.IsFalse(draft.NeedsConfirmation, "kéo dài đợt đang chạy không lấy đi gì của người chơi");
        }

        [Test]
        public void AnchorChangeWithRunning_IsIdentityAndTypesRunningId()
        {
            LiveEventCalendarDocument draftDocument = DocumentWithPrefix(PublishedPrefix, WeekHours, WeekHours);
            // Dời neo một TUẦN: chu kỳ 168 giờ nên dời ít hơn thế vẫn ra cùng số thứ tự — id không đổi thì không có gì để hỏi.
            RecurringLiveEventRule candidate = RuleOf(draftDocument).WithAnchor("2026-01-12T00:00:00Z");

            RecurringPrefixDraft draft = RecurringPrefixDraft.For(draftDocument, null, LiveOpsDesignSample.NowUtc,
                AssetFileName, RecurringRuleFields.Anchor, LiveOpsEditOperation.ChangeRecurringIdentity, candidate, Format);

            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, draft.Requirement, "dời neo cũng đổi id của lần lặp đang chạy");
            Assert.AreEqual(RunningEventId, draft.BuildConfirmRequest().TypeToConfirmText);
        }

        [Test]
        public void None_HasNoDraftAndNoText()
        {
            Assert.IsFalse(RecurringPrefixDraft.None.HasDraft);
            Assert.IsFalse(RecurringPrefixDraft.None.NeedsConfirmation);
            Assert.AreEqual(string.Empty, RecurringPrefixDraft.None.CellNotice);
            Assert.AreEqual(string.Empty, RecurringPrefixDraft.None.ConsequenceText);
            Assert.Throws<InvalidOperationException>(() => RecurringPrefixDraft.None.BuildConfirmRequest());
        }

        private static LiveEventCalendarDocument DocumentWithPrefix(string idPrefix, int periodHours, int activeHours)
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(WeeklyPassType, "Pass tuần", 1, false, "weekly_pass_s3"))
                .WithRecurringRule(new RecurringLiveEventRule(WeeklyPassType, AnchorUtcText, idPrefix, periodHours, activeHours, string.Empty))
                .Build();
        }

        private static RecurringLiveEventRule RuleOf(LiveEventCalendarDocument document)
        {
            RecurringLiveEventRule rule;
            Assert.IsTrue(document.TryGetRecurringRule(WeeklyPassType, out rule));
            return rule;
        }
    }
}
