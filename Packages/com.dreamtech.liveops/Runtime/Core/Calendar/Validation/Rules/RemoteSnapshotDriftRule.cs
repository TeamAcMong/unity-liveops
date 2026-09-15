using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 12 <c>remote-snapshot-drift</c> (PD-31): JSON đang chạy đã dán có khác bản đã đăng không — so NGỮ NGHĨA, vì Firebase
    /// console có thể đổi khoảng trắng/xuống dòng/thứ tự key mà game vẫn đọc ra đúng lịch cũ; báo lệch giả làm người dùng mất
    /// niềm tin vào dấu "Khớp dấu". Thước đo là dấu đã đăng MỚI NHẤT (<see cref="LiveEventCalendarDocument.LatestStamp"/>), không
    /// phải bản so đang chọn: <see cref="LiveEventCalendarCheckContext.PublishedBaseline"/> có thể là dấu cũ người dùng chọn để so
    /// trong phiên, và so bản đang chạy với dấu cũ thì Firebase chạy bản cũ mà hub vẫn nói "khớp". Ba tầng, dừng ở tầng đầu tiên
    /// kết luận được:
    /// <list type="number">
    /// <item>Sha nguyên văn của bản dán = sha của dấu mới nhất → Passed.</item>
    /// <item>Viết lại chuẩn bản dán bằng <see cref="LiveEventCalendarJsonWriter"/> ra đúng sha của dấu (dấu định dạng 2 do bộ ghi
    /// xuất) → Passed; rồi viết lại chuẩn cả bản dán lẫn tài liệu của dấu ở định dạng 2 và so sha → bằng = Passed.</item>
    /// <item>So từng mục theo object JSON chuẩn VÀ kết quả biên dịch (đợt theo id, luật lặp theo loại — danh tính như diff, tự so
    /// không đi qua kiểu của diff để validator không phụ thuộc gói diff): không mục nào khác → Passed; có → Found.</item>
    /// </list>
    /// Tầng 2b/3 cần TÀI LIỆU của dấu mới nhất, mà core không có parser: phiên hub đọc <c>SnapshotJson</c> của dấu rồi truyền vào
    /// <see cref="LiveEventCalendarCheckContext.LatestStampBaseline"/> (V-21 CC-VALB-1) — có thì dùng thẳng. Thiếu (phiên chưa đọc
    /// được dấu, context dựng tay) thì chỉ dùng bản so khi bộ ghi viết bản so ra đúng sha của dấu (tức bản so chính là dấu đó);
    /// không chứng minh được thì NotMeasured <c>latest-stamp-not-loaded</c> (CC-VALB-2) — "chưa đo" thật thà hơn một kết luận so
    /// với sai bản.
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
        public const string LatestStampNotLoadedReasonCode = "latest-stamp-not-loaded";

        public string RuleId => LiveEventCalendarRuleIds.RemoteSnapshotDrift;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.ShouldReview;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            LiveEventCalendarDocument remote = context.RemoteSnapshot;
            // Chưa dán thì chưa đo — không phải "khớp": luật này không bao giờ tính là đã qua khi thiếu bản remote.
            if (remote == null) return LiveEventCalendarRuleResult.NotMeasured(RuleId, RemoteNotPastedReasonCode);

            PublishedCalendarStamp latestStamp = context.Document.LatestStamp;
            LiveEventCalendarDocument baseline = context.PublishedBaseline;
            LiveEventCalendarDocument latestStampBaseline = context.LatestStampBaseline;
            if (latestStamp == null && baseline == null && latestStampBaseline == null)
            {
                int itemCount = remote.RecurringRules.Count + remote.FixedEvents.Count;
                return Found(LiveEventCalendarDetailCodes.RemoteWithoutStamp, itemCount.ToString(CultureInfo.InvariantCulture), string.Empty);
            }

            LiveEventCalendarJsonText remoteCanonical = LiveEventCalendarJsonWriter.Write(remote, LiveEventCalendarJsonFormat.Version2);
            LiveEventCalendarDocument stampDocument;
            if (latestStamp != null)
            {
                // Tầng 1.
                if (ShaEquals(context.RemoteSnapshotSha256Hex, latestStamp.Sha256Hex)) return LiveEventCalendarRuleResult.Passed(RuleId);

                // Tầng 2a — không cần tài liệu của dấu: dấu định dạng 2 là đúng byte bộ ghi xuất, nên bản dán viết lại chuẩn ra đúng
                // sha đó là cùng lịch. Không làm với dấu định dạng 1: định dạng 1 không ghi luật lặp, bản dán thêm luật lặp sẽ "bằng" giả.
                LiveEventCalendarJsonFormat stampFormat = StampFormatOf(latestStamp);
                if (stampFormat == LiveEventCalendarJsonFormat.Version2 && ShaEquals(remoteCanonical.Sha256Hex, latestStamp.Sha256Hex))
                {
                    return LiveEventCalendarRuleResult.Passed(RuleId);
                }

                stampDocument = latestStampBaseline ?? StampDocumentFromBaseline(baseline, latestStamp, stampFormat);
                if (stampDocument == null) return LiveEventCalendarRuleResult.NotMeasured(RuleId, LatestStampNotLoadedReasonCode);
            }
            else
            {
                // Tài liệu không kèm dấu (context dựng tay ở công cụ/test): tài liệu dấu truyền vào nếu có, không thì bản so — là
                // thước đo duy nhất có.
                stampDocument = latestStampBaseline ?? baseline;
                if (ShaEquals(context.RemoteSnapshotSha256Hex, LiveEventCalendarJsonWriter.Write(stampDocument, LiveEventCalendarJsonFormat.Version2).Sha256Hex))
                {
                    return LiveEventCalendarRuleResult.Passed(RuleId);
                }
            }

            // Tầng 2b — luôn định dạng 2 (cùng lý do định dạng 1 ở trên).
            LiveEventCalendarJsonText stampCanonical = LiveEventCalendarJsonWriter.Write(stampDocument, LiveEventCalendarJsonFormat.Version2);
            if (ShaEquals(remoteCanonical.Sha256Hex, stampCanonical.Sha256Hex)) return LiveEventCalendarRuleResult.Passed(RuleId);

            // Tầng 3.
            List<string> differingItemIds = FindDifferingItems(stampDocument, remote);
            if (differingItemIds.Count == 0) return LiveEventCalendarRuleResult.Passed(RuleId);

            return Found(LiveEventCalendarDetailCodes.RemoteDiffers, string.Join(LiveEventCalendarFindingBuilder.ValueSeparator, differingItemIds),
                differingItemIds.Count.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Không có tài liệu dấu truyền vào: bản so chỉ được làm thước đo khi bộ ghi viết nó ra ĐÚNG sha của dấu mới nhất — bản so
        /// là dấu cũ người dùng chọn thì không, kẻo bản dán = dấu cũ lại báo "khớp". <c>null</c> = không chứng minh được.
        /// </summary>
        private static LiveEventCalendarDocument StampDocumentFromBaseline(LiveEventCalendarDocument baseline, PublishedCalendarStamp latestStamp,
            LiveEventCalendarJsonFormat stampFormat)
        {
            if (baseline == null) return null;
            return ShaEquals(LiveEventCalendarJsonWriter.Write(baseline, stampFormat).Sha256Hex, latestStamp.Sha256Hex) ? baseline : null;
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

            // Thứ tự xuất cho cả hai phía: trùng id thì mục đứng trước thắng — so theo đúng thứ tự game nhận.
            LiveEventCalendarDocument orderedBaseline = LiveEventCalendarExportOrder.Apply(baseline);
            LiveEventCalendarDocument orderedRemote = LiveEventCalendarExportOrder.Apply(remote);
            ItemVersions baselineRules = new ItemVersions();
            ItemVersions baselineEvents = new ItemVersions();
            CollectItems(orderedBaseline, baselineRules, baselineEvents);
            ItemVersions remoteRules = new ItemVersions();
            ItemVersions remoteEvents = new ItemVersions();
            CollectItems(orderedRemote, remoteRules, remoteEvents);

            AppendDiffering(baselineRules, remoteRules, differing);
            AppendDiffering(baselineEvents, remoteEvents, differing);
            return differing;
        }

        /// <summary>
        /// Mỗi mục = object JSON chuẩn + game có giữ nó không (biên dịch thứ tự xuất). Vì sao cần cờ giữ: hai đợt cùng loại cùng giờ
        /// bắt đầu chồng nhau, đảo thứ tự asset thì object từng id y nguyên nhưng game giữ đợt KHÁC (hoà giờ theo thứ tự danh sách,
        /// đợt sau bị bỏ) — người chơi thấy lịch khác mà so object thì "không lệch".
        /// </summary>
        private static void CollectItems(LiveEventCalendarDocument orderedDocument, ItemVersions rules, ItemVersions events)
        {
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(orderedDocument);
            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = compilation.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = entries[index];
                if (outcome.Kind == LiveEventCalendarEntryKind.RecurringRule)
                {
                    RecurringLiveEventRule rule = orderedDocument.RecurringRules[outcome.SourceIndex];
                    rules.Add(rule.EventType, LiveEventCalendarJsonWriter.WriteRecurringRuleObject(orderedDocument, rule), outcome.IsKept);
                }
                else
                {
                    FixedLiveEventEntry entry = orderedDocument.FixedEvents[outcome.SourceIndex];
                    events.Add(entry.EventId, LiveEventCalendarJsonWriter.WriteFixedEventObject(orderedDocument, entry), outcome.IsKept);
                }
            }
        }

        /// <summary>Một danh tính khác khi số mục mang nó, hoặc object chuẩn / cờ giữ của bất kỳ mục nào (theo thứ tự) khác nhau.</summary>
        private static void AppendDiffering(ItemVersions baselineItems, ItemVersions remoteItems, List<string> differing)
        {
            var identities = new List<string>(baselineItems.Identities);
            for (int index = 0; index < remoteItems.Identities.Count; index++)
            {
                if (!baselineItems.Contains(remoteItems.Identities[index])) identities.Add(remoteItems.Identities[index]);
            }

            for (int index = 0; index < identities.Count; index++)
            {
                string identity = identities[index];
                IReadOnlyList<ItemVersion> baselineVersions = baselineItems.VersionsOf(identity);
                IReadOnlyList<ItemVersion> remoteVersions = remoteItems.VersionsOf(identity);
                bool same = baselineVersions.Count == remoteVersions.Count;
                for (int versionIndex = 0; same && versionIndex < baselineVersions.Count; versionIndex++)
                {
                    same = baselineVersions[versionIndex].IsKept == remoteVersions[versionIndex].IsKept &&
                        string.Equals(baselineVersions[versionIndex].ObjectText, remoteVersions[versionIndex].ObjectText, StringComparison.Ordinal);
                }
                if (!same) differing.Add(identity);
            }
        }

        private static LiveEventCalendarJsonFormat StampFormatOf(PublishedCalendarStamp stamp)
        {
            return stamp.FormatVersion == (int)LiveEventCalendarJsonFormat.Version1
                ? LiveEventCalendarJsonFormat.Version1
                : LiveEventCalendarJsonFormat.Version2;
        }

        /// <summary>Sha dán từ nơi khác có thể viết hoa — hex không phân biệt hoa thường; sha rỗng không bao giờ khớp.</summary>
        private static bool ShaEquals(string left, string right)
        {
            return !string.IsNullOrEmpty(left) && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class ItemVersion
        {
            public ItemVersion(string objectText, bool isKept)
            {
                ObjectText = objectText;
                IsKept = isKept;
            }

            public string ObjectText { get; }
            public bool IsKept { get; }
        }

        /// <summary>Các mục theo danh tính, giữ thứ tự gặp đầu tiên của danh tính.</summary>
        private sealed class ItemVersions
        {
            private static readonly ItemVersion[] NoVersions = Array.Empty<ItemVersion>();
            private readonly Dictionary<string, List<ItemVersion>> _versionsByIdentity = new Dictionary<string, List<ItemVersion>>(StringComparer.Ordinal);

            public List<string> Identities { get; } = new List<string>();

            public void Add(string identity, string objectText, bool isKept)
            {
                if (!_versionsByIdentity.TryGetValue(identity, out List<ItemVersion> versions))
                {
                    versions = new List<ItemVersion>();
                    _versionsByIdentity.Add(identity, versions);
                    Identities.Add(identity);
                }
                versions.Add(new ItemVersion(objectText, isKept));
            }

            public bool Contains(string identity) => _versionsByIdentity.ContainsKey(identity);

            public IReadOnlyList<ItemVersion> VersionsOf(string identity)
            {
                return _versionsByIdentity.TryGetValue(identity, out List<ItemVersion> versions) ? versions : (IReadOnlyList<ItemVersion>)NoVersions;
            }
        }
    }
}
