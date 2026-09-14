using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Mọi thứ một lần Kiểm lịch được đọc — bất biến, dựng bằng <see cref="LiveEventCalendarCheckContextBuilder"/>. Tài liệu
    /// luôn đã ở thứ tự xuất (V-6) và <see cref="Compilation"/> luôn khớp đúng thứ tự đó, để luật trỏ từ outcome về mục
    /// bằng <see cref="LiveEventCalendarEntryOutcome.SourceIndex"/> mà không phải tìm lại theo chuỗi.
    /// </summary>
    public sealed class LiveEventCalendarCheckContext
    {
        internal LiveEventCalendarCheckContext(LiveEventCalendarDocument document, LiveEventCalendarCompilation compilation,
            DateTime nowUtc, LiveEventCalendarDocument publishedBaseline, LiveEventCalendarDocument remoteSnapshot,
            string remoteSnapshotSha256Hex, LiveEventCalendarValidationSettings settings, string onlyEventType)
        {
            Document = document;
            Compilation = compilation;
            NowUtc = nowUtc;
            PublishedBaseline = publishedBaseline;
            RemoteSnapshot = remoteSnapshot;
            RemoteSnapshotSha256Hex = remoteSnapshotSha256Hex;
            Settings = settings;
            OnlyEventType = onlyEventType;
        }

        /// <summary>Tài liệu nháp ĐÃ ở thứ tự xuất (<see cref="LiveEventCalendarExportOrder.Apply"/>).</summary>
        public LiveEventCalendarDocument Document { get; }

        public LiveEventCalendarCompilation Compilation { get; }
        public DateTime NowUtc { get; }

        /// <summary><c>null</c> = chưa có dấu đã đăng.</summary>
        public LiveEventCalendarDocument PublishedBaseline { get; }

        /// <summary><c>null</c> = chưa dán JSON đang chạy.</summary>
        public LiveEventCalendarDocument RemoteSnapshot { get; }

        /// <summary>"" khi chưa dán.</summary>
        public string RemoteSnapshotSha256Hex { get; }

        public LiveEventCalendarValidationSettings Settings { get; }

        /// <summary>"" = cả lịch; khác rỗng = kiểm nhanh một làn (luật chỉ phát hiện cho loại này).</summary>
        public string OnlyEventType { get; }

        public LiveEventCalendarCheckContextBuilder ToBuilder()
        {
            var builder = new LiveEventCalendarCheckContextBuilder(Document, NowUtc)
                .WithCompilation(Compilation)
                .WithSettings(Settings)
                .WithOnlyEventType(OnlyEventType);
            if (PublishedBaseline != null) builder.WithPublishedBaseline(PublishedBaseline);
            if (RemoteSnapshot != null) builder.WithRemoteSnapshot(RemoteSnapshot, RemoteSnapshotSha256Hex);
            return builder;
        }

        /// <summary>Luật dùng để lọc theo làn khi kiểm nhanh — một chỗ, để mười hai luật hiểu "cả lịch" giống nhau.</summary>
        internal bool IncludesEventType(string eventType)
        {
            return OnlyEventType.Length == 0 || string.Equals(OnlyEventType, eventType, StringComparison.Ordinal);
        }

        /// <summary>Mục của tài liệu ứng với outcome — đúng vị trí nhờ builder đã kiểm kết quả biên dịch cùng thứ tự xuất.</summary>
        internal FixedLiveEventEntry FixedEventOf(LiveEventCalendarEntryOutcome outcome)
        {
            return Document.FixedEvents[outcome.SourceIndex];
        }

        internal RecurringLiveEventRule RecurringRuleOf(LiveEventCalendarEntryOutcome outcome)
        {
            return Document.RecurringRules[outcome.SourceIndex];
        }

        /// <summary>Outcome của đợt cố định bị bỏ với đúng lý do chính cho trước, lọc theo làn — luật 1–5, 7 đều bắt đầu từ đây (V-7).</summary>
        internal List<LiveEventCalendarEntryOutcome> DroppedFixedOutcomes(LiveEventCalendarDropReason primaryReason)
        {
            return DroppedOutcomes(LiveEventCalendarEntryKind.FixedEvent, primaryReason, primaryReason);
        }

        internal List<LiveEventCalendarEntryOutcome> DroppedOutcomes(LiveEventCalendarEntryKind kind, LiveEventCalendarDropReason firstReason,
            LiveEventCalendarDropReason secondReason)
        {
            var matches = new List<LiveEventCalendarEntryOutcome>();
            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = Compilation.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = entries[index];
                if (outcome.Kind != kind || outcome.IsKept) continue;
                if (outcome.DropReason != firstReason && outcome.DropReason != secondReason) continue;
                if (!IncludesEventType(outcome.EventType)) continue;
                matches.Add(outcome);
            }
            return matches;
        }
    }

    public sealed class LiveEventCalendarCheckContextBuilder
    {
        private readonly LiveEventCalendarDocument _document;
        private readonly DateTime _nowUtc;
        private LiveEventCalendarCompilation _compilation;
        private LiveEventCalendarDocument _publishedBaseline;
        private LiveEventCalendarDocument _remoteSnapshot;
        private string _remoteSnapshotSha256Hex = string.Empty;
        private LiveEventCalendarValidationSettings _settings = LiveEventCalendarValidationSettings.Default;
        private string _onlyEventType = string.Empty;

        public LiveEventCalendarCheckContextBuilder(LiveEventCalendarDocument document, DateTime nowUtc)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        }

        /// <summary>
        /// Mặc định <see cref="LiveEventCalendarCompiler.CompileInExportOrder"/>. Truyền vào (phiên hub đã biên dịch sẵn) thì phải
        /// cùng thứ tự xuất — <see cref="Build"/> ném khi lệch, vì luật trỏ outcome về mục bằng vị trí.
        /// </summary>
        public LiveEventCalendarCheckContextBuilder WithCompilation(LiveEventCalendarCompilation compilation)
        {
            _compilation = compilation;
            return this;
        }

        public LiveEventCalendarCheckContextBuilder WithPublishedBaseline(LiveEventCalendarDocument baseline)
        {
            _publishedBaseline = baseline;
            return this;
        }

        public LiveEventCalendarCheckContextBuilder WithRemoteSnapshot(LiveEventCalendarDocument snapshot, string sha256Hex)
        {
            _remoteSnapshot = snapshot;
            _remoteSnapshotSha256Hex = snapshot != null ? sha256Hex ?? string.Empty : string.Empty;
            return this;
        }

        public LiveEventCalendarCheckContextBuilder WithSettings(LiveEventCalendarValidationSettings settings)
        {
            _settings = settings ?? LiveEventCalendarValidationSettings.Default;
            return this;
        }

        public LiveEventCalendarCheckContextBuilder WithOnlyEventType(string eventType)
        {
            _onlyEventType = eventType ?? string.Empty;
            return this;
        }

        public LiveEventCalendarCheckContext Build()
        {
            LiveEventCalendarDocument orderedDocument = LiveEventCalendarExportOrder.Apply(_document);
            LiveEventCalendarCompilation compilation = _compilation ?? LiveEventCalendarCompiler.Compile(orderedDocument);
            if (_compilation != null) EnsureCompilationMatches(orderedDocument, compilation);

            return new LiveEventCalendarCheckContext(orderedDocument, compilation, _nowUtc, _publishedBaseline, _remoteSnapshot,
                _remoteSnapshotSha256Hex, _settings, _onlyEventType);
        }

        /// <summary>
        /// Kết quả biên dịch truyền vào phải là của CHÍNH tài liệu này ở thứ tự xuất: luật lặp trước theo thứ tự asset, rồi đợt
        /// theo thứ tự xuất. Lệch là lỗi lập trình (vd biên dịch bằng <c>Compile</c> thay vì <c>CompileInExportOrder</c>) — ném
        /// ngay thay vì để luật gắn phát hiện nhầm mục một cách im lặng.
        /// </summary>
        private static void EnsureCompilationMatches(LiveEventCalendarDocument orderedDocument, LiveEventCalendarCompilation compilation)
        {
            IReadOnlyList<RecurringLiveEventRule> rules = orderedDocument.RecurringRules;
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = orderedDocument.FixedEvents;
            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = compilation.Entries;
            if (entries.Count != rules.Count + fixedEvents.Count)
            {
                throw new ArgumentException("Kết quả biên dịch có " + entries.Count + " mục, tài liệu có " +
                    (rules.Count + fixedEvents.Count) + " — không phải của tài liệu này.", "compilation");
            }
            for (int index = 0; index < rules.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = entries[index];
                if (outcome.Kind != LiveEventCalendarEntryKind.RecurringRule || outcome.SourceIndex != index ||
                    !string.Equals(outcome.EntryKey, rules[index].EventType, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Kết quả biên dịch lệch luật lặp thứ " + (index + 1) + ".", "compilation");
                }
            }
            for (int index = 0; index < fixedEvents.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = entries[rules.Count + index];
                if (outcome.Kind != LiveEventCalendarEntryKind.FixedEvent || outcome.SourceIndex != index ||
                    !string.Equals(outcome.EntryKey, fixedEvents[index].EntryKey, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Kết quả biên dịch không ở thứ tự xuất (lệch ở đợt thứ " + (index + 1) +
                        ") — dùng LiveEventCalendarCompiler.CompileInExportOrder.", "compilation");
                }
            }
        }
    }
}
