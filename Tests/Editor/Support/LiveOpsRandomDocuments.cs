using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps.Tests
{
    /// <summary>
    /// Bộ sinh tài liệu lịch ngẫu nhiên có seed cố định cho các fuzz test (khứ hồi JSON V-4, thứ tự xuất V-6, mỗi mục bị bỏ
    /// đúng một phát hiện V-7, cú pháp bộ ghi). Không tham chiếu NUnit (asmdef hỗ trợ không có nunit) và không dùng
    /// <see cref="Random"/>: thuật toán của <see cref="Random"/> không được hứa giống nhau giữa Mono 2022.3 và runtime của
    /// 6000.6, mà một tài liệu hỏng ở một bản phải tái hiện được đúng từng byte ở bản kia — nên dùng SplitMix64 viết tay.
    ///
    /// <para>Mỗi tài liệu CHỦ Ý chứa thứ bộ ghi/parser/validator dễ làm sai: giờ có phần lẻ giây, giờ có múi khác Z, giờ hỏng
    /// (kể cả chuỗi chỉ có khoảng trắng), mục nhiều lỗi (id có '#' + kết thúc trước bắt đầu; giờ hỏng + id rỗng), trùng id mà
    /// đợt bắt đầu MUỘN đứng trước trong asset, trùng id với đợt ĐỨNG TRƯỚC trong thứ tự xuất đã bị bỏ vì lỗi cấp mục, trùng id
    /// + chồng giờ, luật lặp hỏng và luật thứ hai cùng loại, id có dạng lần lặp của luật (bị che theo id), ký tự cần escape
    /// (nháy, gạch chéo ngược, tab, xuống dòng, ký tự điều khiển) và ký tự không ASCII (tiếng Việt, emoji = cặp surrogate) —
    /// hai thứ sau nằm ở configKey, field bộ ghi thật sự ghi ra JSON, không chỉ ở tên hiển thị của loại.</para>
    /// </summary>
    public static class LiveOpsRandomDocuments
    {
        /// <summary>Seed dùng chung cho mọi fuzz của gói — đổi số này là đổi bộ dữ liệu mọi test đang khoá.</summary>
        public const int FixedSeed = 20260913;

        /// <summary>Số tài liệu mỗi fuzz chạy (mục 5.6, 6.1: 200 tài liệu seed cố định).</summary>
        public const int DocumentCount = 200;

        /// <summary>
        /// Tiền tố id của cặp đợt ở kịch bản bắt buộc "trùng id với đợt đứng trước đã bị bỏ". Test phủ dùng nó để đòi chính kịch
        /// bản này đóng góp ca đó — đếm chung mọi tài liệu thì ca có thể chỉ đạt nhờ đợt ngẫu nhiên tình cờ trùng id.
        /// </summary>
        public const string DuplicateOfDroppedEventIdPrefix = "forced-after-dropped-";

        // Số kịch bản bắt buộc xoay vòng theo chỉ số tài liệu: mỗi kịch bản xuất hiện đều trong 200 tài liệu, không phụ thuộc may rủi.
        private const int ForcedScenarioCount = 6;

        // Mỗi 40 tài liệu có một tài liệu không có đợt lẫn luật — ca "recurring": [] / "events": [] của bộ ghi và parser.
        private const int EmptyDocumentInterval = 40;
        private const int EmptyDocumentRemainder = 39;

        private const int MaximumRecurringRuleCount = 3;
        private const int MaximumRandomFixedEventCount = 8;
        private const int MaximumPublishedStampCount = 2;
        private const int MaximumIgnoredWarningCount = 2;

        // Đợt rải trong ~4 tháng quanh "bây giờ" của mẫu (13/9/2026), để khung so lần lặp một năm quanh neo chứa hết.
        private static readonly DateTime FirstEventDayUtc = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        private const int EventDaySpread = 120;
        private static readonly TimeSpan DeviceOffset = TimeSpan.FromHours(7);

        private static readonly string[] ValidTypeIds = { "lava-quest", "sky-race", "treasure-hunt", "weekly-pass", "star-tournament", "lucky-spin" };

        // Loại sai quy tắc: rỗng, có '#', có xuống dòng — bộ biên dịch bỏ đợt (InvalidIdentifier) và luật (InvalidRecurringRule).
        private static readonly string[] InvalidTypeIds = { string.Empty, "bad#type", "line\nbreak" };

        private static readonly string[] InvalidEventIds = { string.Empty, "has#hash", "two\nlines", "tab\r" };

        private static readonly string[] UnreadableTimeTexts = { "2026-10-3", string.Empty, "   ", "not a time", "2026-13-01T00:00:00Z", "3/10/2026", "2026-09-10 08:00" };

        private static readonly string[] RemoteConfigKeys = { LiveEventCalendarDocument.DefaultRemoteConfigKey, string.Empty, "calendar_v2", "lịch \"chính\"" };

        private static readonly string[] DisplayNames = { string.Empty, "Nhiệm vụ dung nham", "Đua \"trên\" trời", "Săn\tkho báu 🔥", "back\\slash" };

        // Tab và emoji (cặp surrogate) phải nằm ở đây chứ không chỉ ở DisplayNames: bộ ghi JSON không ghi loại, nên chỉ field được
        // ghi ra (configKey của luật và đợt) mới đưa chúng qua bộ ghi, bộ dò cú pháp và JsonUtility.
        private static readonly string[] ConfigKeys =
            { string.Empty, string.Empty, "lava_quest_v2", "hunt_bonus", "khoá\\riêng", "quote\"key", "chuông\u0007", "săn\tkho", "lửa 🔥" };

        private static readonly string[] IdPrefixes = { string.Empty, string.Empty, string.Empty, "pass-", "race-", "bad#", "new\nline-" };

        private static readonly int[] PeriodHourChoices = { 24, 24, 168, 6, 12, 1, 0, -24, 1000000000 };

        private static readonly int[] DurationHourChoices = { 24, 48, 72, 36, 12, 1, 0, -12 };

        private static readonly string[] Publishers = { "DatHoUnityDev", string.Empty, "người đăng" };

        /// <summary>Tài liệu thứ <paramref name="documentIndex"/> của <see cref="FixedSeed"/>.</summary>
        public static LiveEventCalendarDocument Create(int documentIndex)
        {
            return Create(FixedSeed, documentIndex);
        }

        /// <summary>Cùng (seed, chỉ số) luôn ra cùng tài liệu, ở mọi bản Unity và mọi culture.</summary>
        public static LiveEventCalendarDocument Create(int seed, int documentIndex)
        {
            if (documentIndex < 0) throw new ArgumentOutOfRangeException(nameof(documentIndex), "Chỉ số tài liệu phải ≥ 0.");
            return new DocumentGenerator(new DeterministicRandom(seed, documentIndex), documentIndex).Generate();
        }

        /// <summary>Nhãn đặt đầu thông điệp lỗi — đủ để tái hiện đúng tài liệu: <c>Create(seed, chỉ số)</c>.</summary>
        public static string Label(int documentIndex)
        {
            return "Tài liệu ngẫu nhiên #" + documentIndex.ToString(CultureInfo.InvariantCulture) + " (seed " +
                   FixedSeed.ToString(CultureInfo.InvariantCulture) + ")";
        }

        /// <summary>
        /// Liệt kê field thô của tài liệu (giờ nguyên văn, configKey riêng, EntryKey) để thông điệp lỗi của fuzz tự đủ, không
        /// phải chạy lại bằng tay mới biết tài liệu hỏng trông thế nào. Ký tự điều khiển in dạng <c>\uXXXX</c> cho đọc được.
        /// </summary>
        public static string Describe(LiveEventCalendarDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            var builder = new StringBuilder();
            builder.Append("remoteConfigKey=").Append(Visible(document.RemoteConfigKey)).Append('\n');
            for (int index = 0; index < document.EventTypes.Count; index++)
            {
                LiveEventTypeDefinition type = document.EventTypes[index];
                builder.Append("type[").Append(index.ToString(CultureInfo.InvariantCulture)).Append("] ").Append(Visible(type.TypeId))
                    .Append(" default=").Append(Visible(type.DefaultConfigKey)).Append('\n');
            }
            for (int index = 0; index < document.RecurringRules.Count; index++)
            {
                RecurringLiveEventRule rule = document.RecurringRules[index];
                builder.Append("rule[").Append(index.ToString(CultureInfo.InvariantCulture)).Append("] ").Append(Visible(rule.EventType))
                    .Append(" anchor=").Append(Visible(rule.AnchorUtcText))
                    .Append(" prefix=").Append(Visible(rule.IdPrefix))
                    .Append(" period=").Append(rule.PeriodHours.ToString(CultureInfo.InvariantCulture))
                    .Append(" active=").Append(rule.ActiveHours.ToString(CultureInfo.InvariantCulture))
                    .Append(" configKey=").Append(Visible(rule.ConfigKey)).Append('\n');
            }
            for (int index = 0; index < document.FixedEvents.Count; index++)
            {
                FixedLiveEventEntry entry = document.FixedEvents[index];
                builder.Append("event[").Append(index.ToString(CultureInfo.InvariantCulture)).Append("] ").Append(Visible(entry.EventId))
                    .Append(" type=").Append(Visible(entry.EventType))
                    .Append(" start=").Append(Visible(entry.StartUtcText))
                    .Append(" end=").Append(Visible(entry.EndUtcText))
                    .Append(" configKey=").Append(Visible(entry.ConfigKey))
                    .Append(" key=").Append(entry.EntryKey).Append('\n');
            }
            builder.Append("stamps=").Append(document.PublishedStamps.Count.ToString(CultureInfo.InvariantCulture))
                .Append(" ignoredWarnings=").Append(document.IgnoredWarnings.Count.ToString(CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static string Visible(string value)
        {
            var builder = new StringBuilder(value.Length + 2);
            builder.Append('\'');
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (character < 0x20) builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                else builder.Append(character);
            }
            builder.Append('\'');
            return builder.ToString();
        }

        /// <summary>
        /// Dựng một tài liệu. Tách thành lớp có trạng thái (bộ ngẫu nhiên, đếm EntryKey) thay vì truyền tham số qua mọi hàm tĩnh.
        /// </summary>
        private sealed class DocumentGenerator
        {
            private readonly DeterministicRandom _random;
            private readonly int _documentIndex;
            private readonly List<FixedLiveEventEntry> _fixedEvents = new List<FixedLiveEventEntry>();
            private readonly List<RecurringLiveEventRule> _recurringRules = new List<RecurringLiveEventRule>();

            public DocumentGenerator(DeterministicRandom random, int documentIndex)
            {
                _random = random;
                _documentIndex = documentIndex;
            }

            public LiveEventCalendarDocument Generate()
            {
                var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(_random.Pick(RemoteConfigKeys));

                // Chỉ khai loại hợp lệ (ctor LiveEventTypeDefinition ném với id sai) và bỏ ngẫu nhiên vài loại — đợt thuộc loại
                // chưa khai giữ nguyên trong JSON (luật 8 báo), còn configKey hiệu lực rơi về "" thay vì mặc định của loại.
                for (int index = 0; index < ValidTypeIds.Length; index++)
                {
                    if (!_random.Chance(70)) continue;
                    builder.WithEventType(new LiveEventTypeDefinition(ValidTypeIds[index], _random.Pick(DisplayNames),
                        _random.NextInt(LiveEventTypeDefinition.ColorSlotCount), _random.Chance(50), _random.Pick(ConfigKeys)));
                }

                bool isEmptyDocument = _documentIndex % EmptyDocumentInterval == EmptyDocumentRemainder;
                if (!isEmptyDocument)
                {
                    AddRecurringRules();
                    AddRandomFixedEvents();
                    AddForcedScenario(_documentIndex % ForcedScenarioCount);
                }

                for (int index = 0; index < _recurringRules.Count; index++) builder.WithRecurringRule(_recurringRules[index]);
                for (int index = 0; index < _fixedEvents.Count; index++) builder.WithFixedEvent(_fixedEvents[index]);
                AddPublishedStamps(builder);
                AddIgnoredWarnings(builder);
                return builder.Build();
            }

            // ----- Luật lặp -----

            private void AddRecurringRules()
            {
                int ruleCount = _random.NextInt(MaximumRecurringRuleCount + 1);
                for (int index = 0; index < ruleCount; index++)
                {
                    // Lặp lại loại của luật trước → luật thứ hai cùng loại (DuplicateRecurringType nếu cả hai hợp lệ).
                    string eventType = index > 0 && _random.Chance(25) ? _recurringRules[index - 1].EventType : PickEventType();
                    int periodHours = _random.Pick(PeriodHourChoices);
                    int activeHours = PickActiveHours(periodHours);
                    string anchorText = _random.Chance(8)
                        ? _random.Pick(UnreadableTimeTexts)
                        : RenderReadableTime(AnchorFor(), allowFraction: true);
                    _recurringRules.Add(new RecurringLiveEventRule(eventType, anchorText, _random.Pick(IdPrefixes), periodHours, activeHours,
                        _random.Pick(ConfigKeys)));
                }
            }

            private DateTime AnchorFor()
            {
                // Neo đầu năm (như mẫu) hoặc giữa khoảng đợt — neo giữa khoảng làm lần lặp chồng lên đợt cố định cùng loại (bị che).
                DateTime anchor = _random.Chance(50)
                    ? new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)
                    : FirstEventDayUtc.AddDays(_random.NextInt(EventDaySpread)).AddHours(_random.NextInt(24));
                return AddFractionMaybe(anchor);
            }

            private int PickActiveHours(int periodHours)
            {
                switch (_random.NextInt(6))
                {
                    case 0: return periodHours;
                    case 1: return periodHours > 4 ? periodHours - 4 : 1;
                    case 2: return 1;
                    case 3: return 0;
                    case 4: return periodHours > 0 && periodHours < int.MaxValue - 6 ? periodHours + 6 : 30;
                    default: return -1;
                }
            }

            // ----- Đợt cố định -----

            private void AddRandomFixedEvents()
            {
                int eventCount = _random.NextInt(MaximumRandomFixedEventCount + 1);
                for (int index = 0; index < eventCount; index++)
                {
                    string eventType = PickEventType();
                    DateTime startUtc = AddFractionMaybe(RandomStartUtc());
                    DateTime endUtc = AddFractionMaybe(startUtc.AddHours(_random.Pick(DurationHourChoices)));
                    string startText = _random.Chance(7) ? _random.Pick(UnreadableTimeTexts) : RenderReadableTime(startUtc, allowFraction: true);
                    string endText = _random.Chance(7) ? _random.Pick(UnreadableTimeTexts) : RenderReadableTime(endUtc, allowFraction: true);
                    InsertAtRandomPosition(new FixedLiveEventEntry(NextEntryKey(), PickEventId(eventType), eventType, startText, endText,
                        _random.Pick(ConfigKeys)));
                }
            }

            private string PickEventId(string eventType)
            {
                int choice = _random.NextInt(100);
                if (choice < 8) return _random.Pick(InvalidEventIds);
                if (choice < 30) return "dup-" + _random.NextInt(3).ToString(CultureInfo.InvariantCulture);
                // Id có dạng lần lặp của luật cùng loại → Composite che đợt theo id dù không chồng giờ (DropReason.ShadowedByRecurring).
                if (choice < 40 && _recurringRules.Count > 0)
                {
                    RecurringLiveEventRule rule = _random.Pick(_recurringRules);
                    return rule.EffectiveIdPrefix + _random.NextInt(40).ToString(CultureInfo.InvariantCulture);
                }
                return (eventType.Length > 0 ? eventType : "event") + "-" + _random.NextInt(6).ToString(CultureInfo.InvariantCulture);
            }

            /// <summary>
            /// Kịch bản bắt buộc của mục 6.1/V-6/V-7 — xoay vòng theo chỉ số để 200 tài liệu chắc chắn chạm đủ, còn vị trí chèn
            /// vẫn ngẫu nhiên để thứ tự asset không trùng thứ tự xuất.
            /// </summary>
            private void AddForcedScenario(int scenario)
            {
                string eventType = _random.Pick(ValidTypeIds);
                DateTime baseUtc = AddFractionMaybe(RandomStartUtc());
                switch (scenario)
                {
                    case 0:
                        // Id có '#' + kết thúc trước bắt đầu: lý do chính InvalidIdentifier, EndNotAfterStart vào AdditionalItemReasons.
                        InsertAtRandomPosition(Entry("forced#hash", eventType, baseUtc, baseUtc.AddHours(-6)));
                        break;
                    case 1:
                        // Giờ hỏng + id rỗng: lý do chính giờ hỏng, InvalidIdentifier đi kèm.
                        InsertAtRandomPosition(new FixedLiveEventEntry(NextEntryKey(), string.Empty, eventType,
                            RenderReadableTime(baseUtc, allowFraction: true), _random.Pick(UnreadableTimeTexts), _random.Pick(ConfigKeys)));
                        break;
                    case 2:
                    {
                        // Trùng id, đợt bắt đầu MUỘN đứng TRƯỚC trong asset (hai loại khác nhau để không chồng giờ): thứ tự xuất giữ
                        // đợt bắt đầu sớm — hub, asset và JSON phải giữ cùng một đợt (V-6).
                        string duplicateId = "forced-late-first-" + _documentIndex.ToString(CultureInfo.InvariantCulture);
                        FixedLiveEventEntry later = Entry(duplicateId, eventType, baseUtc.AddDays(10), baseUtc.AddDays(11));
                        FixedLiveEventEntry earlier = Entry(duplicateId, OtherTypeThan(eventType), baseUtc, baseUtc.AddDays(1));
                        int laterPosition = _random.NextInt(_fixedEvents.Count + 1);
                        _fixedEvents.Insert(laterPosition, later);
                        _fixedEvents.Insert(laterPosition + 1 + _random.NextInt(_fixedEvents.Count - laterPosition), earlier);
                        break;
                    }
                    case 3:
                    {
                        // Trùng id với đợt ĐỨNG TRƯỚC (theo thứ tự xuất) đã bị bỏ vì lỗi cấp mục: không phải trùng (runtime không
                        // thấy đợt hỏng), đợt sau được giữ. Lỗi đặt ở endUtc chứ không ở startUtc: đợt có startUtc không đọc được bị
                        // thứ tự xuất xếp CUỐI, tức đứng SAU đợt được giữ — ca "đứng trước" sẽ không bao giờ xảy ra. Vị trí trong
                        // asset để ngẫu nhiên: thứ tự xuất theo startUtc quyết định ai đứng trước, không phải thứ tự asset.
                        string sharedId = DuplicateOfDroppedEventIdPrefix + _documentIndex.ToString(CultureInfo.InvariantCulture);
                        FixedLiveEventEntry dropped = new FixedLiveEventEntry(NextEntryKey(), sharedId, eventType,
                            RenderReadableTime(baseUtc, allowFraction: true), _random.Pick(UnreadableTimeTexts), string.Empty);
                        FixedLiveEventEntry kept = Entry(sharedId, eventType, baseUtc.AddDays(40), baseUtc.AddDays(41));
                        InsertAtRandomPosition(dropped);
                        InsertAtRandomPosition(kept);
                        break;
                    }
                    case 4:
                    {
                        // Trùng id + chồng giờ cùng loại: đợt sau bị bỏ vì trùng id TRƯỚC khi xét chồng giờ (thứ tự lý do V-7).
                        string sharedId = "forced-duplicate-overlap-" + _documentIndex.ToString(CultureInfo.InvariantCulture);
                        InsertAtRandomPosition(Entry(sharedId, eventType, baseUtc, baseUtc.AddHours(48)));
                        InsertAtRandomPosition(Entry(sharedId, eventType, baseUtc.AddHours(12), baseUtc.AddHours(60)));
                        break;
                    }
                    default:
                    {
                        // Đợt nửa giây .2 → .7 trong cùng một giây + đợt cùng loại mở ở .5 (chồng giờ, bị bỏ). Bộ ghi lỡ cắt phần lẻ
                        // thì đợt nửa giây thành bắt đầu = kết thúc: nháp giữ, JSON bỏ — phép so 3 bắt được.
                        long halfSecondTicks = TimeSpan.TicksPerSecond / 2;
                        DateTime startUtc = baseUtc.AddTicks(TimeSpan.TicksPerSecond / 5 - baseUtc.Ticks % TimeSpan.TicksPerSecond);
                        InsertAtRandomPosition(Entry("forced-subsecond-" + _documentIndex.ToString(CultureInfo.InvariantCulture), eventType,
                            startUtc, startUtc.AddTicks(halfSecondTicks)));
                        InsertAtRandomPosition(Entry("forced-overlap-" + _documentIndex.ToString(CultureInfo.InvariantCulture), eventType,
                            startUtc.AddTicks(TimeSpan.TicksPerSecond * 3 / 10), startUtc.AddTicks(TimeSpan.TicksPerHour)));
                        break;
                    }
                }
            }

            private FixedLiveEventEntry Entry(string eventId, string eventType, DateTime startUtc, DateTime endUtc)
            {
                return new FixedLiveEventEntry(NextEntryKey(), eventId, eventType, RenderReadableTime(startUtc, allowFraction: true),
                    RenderReadableTime(endUtc, allowFraction: true), _random.Pick(ConfigKeys));
            }

            private string OtherTypeThan(string eventType)
            {
                for (int attempt = 0; attempt < ValidTypeIds.Length; attempt++)
                {
                    string candidate = _random.Pick(ValidTypeIds);
                    if (!string.Equals(candidate, eventType, StringComparison.Ordinal)) return candidate;
                }
                return string.Equals(eventType, ValidTypeIds[0], StringComparison.Ordinal) ? ValidTypeIds[1] : ValidTypeIds[0];
            }

            private void InsertAtRandomPosition(FixedLiveEventEntry entry)
            {
                _fixedEvents.Insert(_random.NextInt(_fixedEvents.Count + 1), entry);
            }

            private DateTime RandomStartUtc()
            {
                return FirstEventDayUtc.AddDays(_random.NextInt(EventDaySpread)).AddHours(_random.NextInt(24));
            }

            private string PickEventType()
            {
                return _random.Chance(8) ? _random.Pick(InvalidTypeIds) : _random.Pick(ValidTypeIds);
            }

            private string NextEntryKey()
            {
                // 32 hex như FixedLiveEventEntry.CreateEntryKey nhưng tất định — Guid mới mỗi lần làm tài liệu không tái hiện được.
                return _random.NextUInt64().ToString("x16", CultureInfo.InvariantCulture) +
                       _random.NextUInt64().ToString("x16", CultureInfo.InvariantCulture);
            }

            // ----- Giờ -----

            private DateTime AddFractionMaybe(DateTime utc)
            {
                // Phần lẻ tới từng tick (7 chữ số) — bộ ghi phải giữ đủ, phép so 2 phải so theo tick (V-19).
                return _random.Chance(20) ? utc.AddTicks(1 + _random.NextInt((int)TimeSpan.TicksPerSecond - 1)) : utc;
            }

            /// <summary>Một thời điểm đọc được viết theo nhiều cách parser game chấp nhận — bộ ghi phải chuẩn hoá về cùng một chuỗi.</summary>
            private string RenderReadableTime(DateTime utc, bool allowFraction)
            {
                if (!allowFraction) utc = utc.AddTicks(-(utc.Ticks % TimeSpan.TicksPerSecond));
                string fraction = FractionText(utc);
                switch (_random.NextInt(10))
                {
                    case 0:
                        // Múi +07:00 (giờ máy của designer) — cùng thời điểm, bộ ghi đổi về Z.
                        return utc.Add(DeviceOffset).ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + fraction + "+07:00";
                    case 1:
                        // Không ghi múi — parser hiểu là UTC (AssumeUniversal).
                        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + fraction;
                    case 2:
                        // Khoảng trắng hai đầu — TryParse cắt, bộ ghi chuẩn hoá.
                        return "  " + utc.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + fraction + "Z ";
                    default:
                        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + fraction + "Z";
                }
            }

            private static string FractionText(DateTime utc)
            {
                long fractionTicks = utc.Ticks % TimeSpan.TicksPerSecond;
                if (fractionTicks == 0) return string.Empty;
                return "." + fractionTicks.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
            }

            // ----- Dấu đã đăng, cảnh báo đã bỏ qua (chỉ để khứ hồi asset; JSON không mang) -----

            private void AddPublishedStamps(LiveEventCalendarDocumentBuilder builder)
            {
                int stampCount = _random.NextInt(MaximumPublishedStampCount + 1);
                for (int index = 0; index < stampCount; index++)
                {
                    string sha256Hex = _random.NextUInt64().ToString("x16", CultureInfo.InvariantCulture) +
                                 _random.NextUInt64().ToString("x16", CultureInfo.InvariantCulture) +
                                 _random.NextUInt64().ToString("x16", CultureInfo.InvariantCulture) +
                                 _random.NextUInt64().ToString("x16", CultureInfo.InvariantCulture);
                    builder.WithPublishedStamp(new PublishedCalendarStamp(RenderReadableTime(RandomStartUtc(), allowFraction: false),
                        _random.Pick(Publishers), sha256Hex, 100 + _random.NextInt(5000), 1 + _random.NextInt(2), _random.Pick(DisplayNames),
                        "{\n  \"version\": 2,\n  \"recurring\": [],\n  \"events\": []\n}"));
                }
            }

            private void AddIgnoredWarnings(LiveEventCalendarDocumentBuilder builder)
            {
                IReadOnlyList<string> ruleIds = LiveEventCalendarRuleIds.All;
                int warningCount = _random.NextInt(MaximumIgnoredWarningCount + 1);
                for (int index = 0; index < warningCount; index++)
                {
                    DateTime rangeStartUtc = RandomStartUtc();
                    builder.WithIgnoredWarning(new IgnoredCalendarWarning(_random.Pick(ruleIds), _random.Pick(ValidTypeIds),
                        RenderReadableTime(rangeStartUtc, allowFraction: false), RenderReadableTime(rangeStartUtc.AddDays(9), allowFraction: false),
                        "nghỉ giữa mùa", _random.Chance(50) ? string.Empty : RenderReadableTime(rangeStartUtc.AddDays(9), allowFraction: false)));
                }
            }
        }

        /// <summary>
        /// SplitMix64 (Steele, Lea, Flood 2014): 64 bit trạng thái, phân bố đủ đều cho fuzz, và quan trọng hơn là cùng dãy số
        /// ở mọi runtime vì chỉ dùng phép toán ulong không tràn-kiểm.
        /// </summary>
        private sealed class DeterministicRandom
        {
            private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
            private ulong _state;

            public DeterministicRandom(int seed, int stream)
            {
                unchecked
                {
                    // Trộn seed và chỉ số tài liệu qua một vòng SplitMix: tài liệu kề nhau không bắt đầu từ trạng thái kề nhau.
                    _state = (ulong)(uint)seed * GoldenGamma ^ ((ulong)(uint)stream + 0x632BE59BD9B4E019UL);
                    _state = NextUInt64();
                }
            }

            public ulong NextUInt64()
            {
                unchecked
                {
                    _state += GoldenGamma;
                    ulong mixed = _state;
                    mixed = (mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9UL;
                    mixed = (mixed ^ (mixed >> 27)) * 0x94D049BB133111EBUL;
                    return mixed ^ (mixed >> 31);
                }
            }

            /// <summary>[0, exclusiveMaximum). Lệch modulo không đáng kể với các khoảng nhỏ ở đây.</summary>
            public int NextInt(int exclusiveMaximum)
            {
                if (exclusiveMaximum <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
                return (int)(NextUInt64() % (ulong)exclusiveMaximum);
            }

            public bool Chance(int percent)
            {
                return NextInt(100) < percent;
            }

            public T Pick<T>(IReadOnlyList<T> items)
            {
                return items[NextInt(items.Count)];
            }
        }
    }
}
