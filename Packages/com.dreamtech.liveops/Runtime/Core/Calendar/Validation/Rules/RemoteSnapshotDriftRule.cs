using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 12 <c>remote-snapshot-drift</c> (PD-31): JSON đang chạy đã dán có khác bản đã đăng không — so NGỮ NGHĨA, vì Firebase
    /// console có thể đổi khoảng trắng/xuống dòng/thứ tự key mà game vẫn đọc ra đúng lịch cũ; báo lệch giả làm người dùng mất
    /// niềm tin vào dấu "Khớp dấu". Ba tầng, dừng ở tầng đầu tiên kết luận được:
    /// <list type="number">
    /// <item>Sha nguyên văn của bản dán = sha bộ ghi sinh ra cho bản so (đúng byte của dấu khi dấu do bộ ghi xuất) → Passed.</item>
    /// <item>Viết lại chuẩn cả hai bằng <see cref="LiveEventCalendarJsonWriter"/> (định dạng 2) và so sha → bằng = Passed.</item>
    /// <item>So từng mục theo object JSON chuẩn (đợt theo id, luật lặp theo loại — danh tính như diff, tự so không đi qua kiểu
    /// của diff để validator không phụ thuộc gói diff): không mục nào khác (chỉ khác thứ tự đợt cùng giờ) → Passed; có → Found.</item>
    /// </list>
    /// Vì sao so object JSON chuẩn thay vì <c>CanonicalText</c> nguyên văn: bản dán "2026-09-10T00:00Z" và bản so
    /// "2026-09-10T00:00:00Z" là cùng một giờ — tầng 2 đã coi là bằng, tầng 3 phải cùng thước đo, không thì "sha khác nhưng 0 mục
    /// khác" ngược lại thành "1 mục khác" tuỳ cách gõ giờ.
    /// <para>Phát hiện mang <see cref="LiveEventCalendarFinding.IsAboutRemoteSnapshot"/> = true (V-17): nói về bản đang chạy, không
    /// về nháp — không vào Cần xử lý của nháp, không chặn Copy JSON. Giá trị thô: <c>differs</c> Found = id/loại khác nối bằng
    /// <see cref="LiveEventCalendarFindingBuilder.ValueSeparator"/>, Expected = số mục khác; <c>no-stamp</c> Found = số mục của
    /// bản dán (luật + đợt), Expected = "".</para>
    /// </summary>
    internal sealed class RemoteSnapshotDriftRule : ILiveEventCalendarRule
    {
        public const string RemoteNotPastedReasonCode = "remote-not-pasted";

        public string RuleId => LiveEventCalendarRuleIds.RemoteSnapshotDrift;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.ShouldReview;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            LiveEventCalendarDocument remote = context.RemoteSnapshot;
            // Chưa dán thì chưa đo — không phải "khớp": luật này không bao giờ tính là đã qua khi thiếu bản remote.
            if (remote == null) return LiveEventCalendarRuleResult.NotMeasured(RuleId, RemoteNotPastedReasonCode);

            LiveEventCalendarDocument baseline = context.PublishedBaseline;
            if (baseline == null)
            {
                int itemCount = remote.RecurringRules.Count + remote.FixedEvents.Count;
                return Found(LiveEventCalendarDetailCodes.RemoteWithoutStamp, itemCount.ToString(CultureInfo.InvariantCulture), string.Empty);
            }

            // Tầng 1 — định dạng của dấu mới nhất khi có (bản so thường là dấu đó); không có thì định dạng 2.
            LiveEventCalendarJsonFormat stampFormat = StampFormatOf(context.Document);
            LiveEventCalendarJsonText baselineInStampFormat = LiveEventCalendarJsonWriter.Write(baseline, stampFormat);
            if (ShaEquals(context.RemoteSnapshotSha256Hex, baselineInStampFormat.Sha256Hex)) return LiveEventCalendarRuleResult.Passed(RuleId);

            // Tầng 2 — luôn định dạng 2: định dạng 1 không ghi luật lặp, nên bản dán có luật lặp mà bản so không có sẽ "bằng" giả.
            LiveEventCalendarJsonText baselineCanonical = stampFormat == LiveEventCalendarJsonFormat.Version2
                ? baselineInStampFormat
                : LiveEventCalendarJsonWriter.Write(baseline, LiveEventCalendarJsonFormat.Version2);
            LiveEventCalendarJsonText remoteCanonical = LiveEventCalendarJsonWriter.Write(remote, LiveEventCalendarJsonFormat.Version2);
            if (ShaEquals(remoteCanonical.Sha256Hex, baselineCanonical.Sha256Hex)) return LiveEventCalendarRuleResult.Passed(RuleId);

            // Tầng 3.
            List<string> differingItemIds = FindDifferingItems(baseline, remote);
            if (differingItemIds.Count == 0) return LiveEventCalendarRuleResult.Passed(RuleId);

            return Found(LiveEventCalendarDetailCodes.RemoteDiffers, string.Join(LiveEventCalendarFindingBuilder.ValueSeparator, differingItemIds),
                differingItemIds.Count.ToString(CultureInfo.InvariantCulture));
        }

        private LiveEventCalendarRuleResult Found(string detailCode, string foundText, string expectedText)
        {
            LiveEventCalendarFinding finding = new LiveEventCalendarFindingBuilder(RuleId, detailCode, Consequence,
                    LiveEventCalendarTargetKind.RemoteSnapshot, string.Empty)
                .WithTexts(foundText, expectedText)
                .WithRemoteSnapshotSubject(true)
                .Build();
            return LiveEventCalendarRuleResult.Found(RuleId, new[] { finding });
        }

        /// <summary>Luật lặp khác (theo thứ tự gặp: bản so rồi bản dán), rồi đợt khác (cùng thứ tự) — danh sách tất định.</summary>
        private static List<string> FindDifferingItems(LiveEventCalendarDocument baseline, LiveEventCalendarDocument remote)
        {
            var differing = new List<string>();

            var baselineRules = new ItemTexts();
            var remoteRules = new ItemTexts();
            for (int index = 0; index < baseline.RecurringRules.Count; index++)
            {
                RecurringLiveEventRule rule = baseline.RecurringRules[index];
                baselineRules.Add(rule.EventType, LiveEventCalendarJsonWriter.WriteRecurringRuleObject(baseline, rule));
            }
            for (int index = 0; index < remote.RecurringRules.Count; index++)
            {
                RecurringLiveEventRule rule = remote.RecurringRules[index];
                remoteRules.Add(rule.EventType, LiveEventCalendarJsonWriter.WriteRecurringRuleObject(remote, rule));
            }
            AppendDiffering(baselineRules, remoteRules, differing);

            // Thứ tự xuất cho cả hai phía: trùng id thì mục đứng trước thắng — so theo đúng thứ tự game nhận.
            LiveEventCalendarDocument orderedBaseline = LiveEventCalendarExportOrder.Apply(baseline);
            LiveEventCalendarDocument orderedRemote = LiveEventCalendarExportOrder.Apply(remote);
            var baselineEvents = new ItemTexts();
            var remoteEvents = new ItemTexts();
            for (int index = 0; index < orderedBaseline.FixedEvents.Count; index++)
            {
                FixedLiveEventEntry entry = orderedBaseline.FixedEvents[index];
                baselineEvents.Add(entry.EventId, LiveEventCalendarJsonWriter.WriteFixedEventObject(orderedBaseline, entry));
            }
            for (int index = 0; index < orderedRemote.FixedEvents.Count; index++)
            {
                FixedLiveEventEntry entry = orderedRemote.FixedEvents[index];
                remoteEvents.Add(entry.EventId, LiveEventCalendarJsonWriter.WriteFixedEventObject(orderedRemote, entry));
            }
            AppendDiffering(baselineEvents, remoteEvents, differing);

            return differing;
        }

        /// <summary>Một danh tính khác khi số mục mang nó hoặc object chuẩn của bất kỳ mục nào (theo thứ tự) khác nhau.</summary>
        private static void AppendDiffering(ItemTexts baselineItems, ItemTexts remoteItems, List<string> differing)
        {
            var identities = new List<string>(baselineItems.Identities);
            for (int index = 0; index < remoteItems.Identities.Count; index++)
            {
                if (!baselineItems.Contains(remoteItems.Identities[index])) identities.Add(remoteItems.Identities[index]);
            }

            for (int index = 0; index < identities.Count; index++)
            {
                string identity = identities[index];
                IReadOnlyList<string> baselineTexts = baselineItems.TextsOf(identity);
                IReadOnlyList<string> remoteTexts = remoteItems.TextsOf(identity);
                bool same = baselineTexts.Count == remoteTexts.Count;
                for (int textIndex = 0; same && textIndex < baselineTexts.Count; textIndex++)
                {
                    same = string.Equals(baselineTexts[textIndex], remoteTexts[textIndex], StringComparison.Ordinal);
                }
                if (!same) differing.Add(identity);
            }
        }

        private static LiveEventCalendarJsonFormat StampFormatOf(LiveEventCalendarDocument draft)
        {
            PublishedCalendarStamp stamp = draft.LatestStamp;
            return stamp != null && stamp.FormatVersion == (int)LiveEventCalendarJsonFormat.Version1
                ? LiveEventCalendarJsonFormat.Version1
                : LiveEventCalendarJsonFormat.Version2;
        }

        /// <summary>Sha dán từ nơi khác có thể viết hoa — hex không phân biệt hoa thường; sha rỗng không bao giờ khớp.</summary>
        private static bool ShaEquals(string left, string right)
        {
            return !string.IsNullOrEmpty(left) && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Object JSON chuẩn theo danh tính, giữ thứ tự gặp đầu tiên của danh tính.</summary>
        private sealed class ItemTexts
        {
            private static readonly string[] NoTexts = Array.Empty<string>();
            private readonly Dictionary<string, List<string>> _textsByIdentity = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            public List<string> Identities { get; } = new List<string>();

            public void Add(string identity, string text)
            {
                if (!_textsByIdentity.TryGetValue(identity, out List<string> texts))
                {
                    texts = new List<string>();
                    _textsByIdentity.Add(identity, texts);
                    Identities.Add(identity);
                }
                texts.Add(text);
            }

            public bool Contains(string identity) => _textsByIdentity.ContainsKey(identity);

            public IReadOnlyList<string> TextsOf(string identity)
            {
                return _textsByIdentity.TryGetValue(identity, out List<string> texts) ? texts : (IReadOnlyList<string>)NoTexts;
            }
        }
    }
}
