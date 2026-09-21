using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DreamTech.LiveOps.Unity
{
    /// <summary>
    /// Kết quả đọc lịch: lịch đã lọc và danh sách lý do các mục bị bỏ. Cùng một hình cho JSON remote và cho asset
    /// (<see cref="LiveEventCalendarAsset.ToParseResult"/>), để composition root của game chỉ có một nhánh.
    /// </summary>
    public sealed class LiveEventCalendarParseResult
    {
        /// <summary>Asset luôn mang được luật lặp nên kết quả của asset nói định dạng 2.</summary>
        internal const int AssetFormatVersion = 2;

        private static readonly IReadOnlyList<RecurringLiveEventCalendar> NoRecurringCalendars = Array.Empty<RecurringLiveEventCalendar>();
        private static readonly LiveEventCalendarCompilation EmptyCompilation = LiveEventCalendarCompiler.Compile(LiveEventCalendarDocument.Empty);

        /// <summary>Ctor 0.1.0 — giữ để không đổi bề mặt internal; kết quả là định dạng 1, chỉ phần cố định.</summary>
        internal LiveEventCalendarParseResult(FixedLiveEventCalendar calendar, IReadOnlyList<string> problems)
            : this(calendar, calendar, NoRecurringCalendars, problems, 1, CompileFixedInstances(calendar), false)
        {
        }

        private LiveEventCalendarParseResult(FixedLiveEventCalendar calendar, ILiveEventCalendar combinedCalendar,
            IReadOnlyList<RecurringLiveEventCalendar> recurringCalendars, IReadOnlyList<string> problems, int formatVersion,
            LiveEventCalendarCompilation compilation, bool cameFromDefaultCalendar)
        {
            Calendar = calendar;
            Problems = problems;
            CombinedCalendar = combinedCalendar;
            RecurringCalendars = recurringCalendars;
            FormatVersion = formatVersion;
            Compilation = compilation;
            CameFromDefaultCalendar = cameFromDefaultCalendar;
        }

        /// <summary>0.1.0: CHỈ phần đợt cố định. Muốn có luật lặp thì dùng <see cref="CombinedCalendar"/>.</summary>
        public FixedLiveEventCalendar Calendar { get; }

        public IReadOnlyList<string> Problems { get; }
        public bool HasProblems => Problems.Count > 0;

        /// <summary>Composite(mọi luật lặp…, đợt cố định) — lịch game nên truyền vào <c>WithCalendar</c>.</summary>
        public ILiveEventCalendar CombinedCalendar { get; }

        public IReadOnlyList<RecurringLiveEventCalendar> RecurringCalendars { get; }

        /// <summary>1, 2, hoặc số lớn hơn (đọc được một phần); 0 khi JSON trống hoặc không đọc được.</summary>
        public int FormatVersion { get; }

        /// <summary>Kết quả từng mục ("giữ 6/8") — hub dùng để giải thích mục nào bị bỏ vì sao.</summary>
        public LiveEventCalendarCompilation Compilation { get; }

        /// <summary><c>true</c> khi lịch lấy từ asset (<see cref="JsonLiveEventCalendarParser.ParseOrDefault(string, LiveEventCalendarAsset)"/>).</summary>
        public bool CameFromDefaultCalendar { get; }

        internal static LiveEventCalendarParseResult FromCompilation(LiveEventCalendarCompilation compilation, IReadOnlyList<string> problems,
            int formatVersion, bool cameFromDefaultCalendar)
        {
            return new LiveEventCalendarParseResult(compilation.FixedCalendar, compilation.Calendar, compilation.RecurringCalendars, problems,
                formatVersion, compilation, cameFromDefaultCalendar);
        }

        /// <summary>
        /// JSON trống / không đọc được: <see cref="Calendar"/> là đúng <see cref="FixedLiveEventCalendar.Empty"/> như 0.1.0
        /// (game so tham chiếu được), không phải một lịch rỗng mới.
        /// </summary>
        internal static LiveEventCalendarParseResult Empty(IReadOnlyList<string> problems, int formatVersion)
        {
            return new LiveEventCalendarParseResult(FixedLiveEventCalendar.Empty, FixedLiveEventCalendar.Empty, NoRecurringCalendars, problems,
                formatVersion, EmptyCompilation, false);
        }

        private static LiveEventCalendarCompilation CompileFixedInstances(FixedLiveEventCalendar calendar)
        {
            var builder = new LiveEventCalendarDocumentBuilder();
            if (calendar != null)
            {
                for (int index = 0; index < calendar.Instances.Count; index++)
                {
                    LiveEventInstance instance = calendar.Instances[index];
                    builder.WithFixedEvent(new FixedLiveEventEntry("instance-" + index.ToString(CultureInfo.InvariantCulture), instance.EventId,
                        instance.EventType, LiveEventUtcText.Format(instance.StartUtc), LiveEventUtcText.Format(instance.EndUtc), instance.ConfigKey));
                }
            }
            return LiveEventCalendarCompiler.Compile(builder.Build());
        }
    }

    /// <summary>
    /// Đọc lịch event từ JSON — thường là giá trị của một key remote config. Định dạng 2 (0.2.0):
    /// <code>
    /// { "version": 2,
    ///   "recurring": [
    ///     { "type": "sky-race", "anchorUtc": "2026-01-05T00:00:00Z", "idPrefix": "sky-race-",
    ///       "periodHours": 24, "activeHours": 20, "configKey": "sky_race_v4" } ],
    ///   "events": [
    ///     { "id": "lava-quest-2026-09", "type": "lava-quest",
    ///       "startUtc": "2026-09-14T00:00:00Z", "endUtc": "2026-09-17T00:00:00Z", "configKey": "lava_quest_v2" } ] }
    /// </code>
    /// Định dạng 1 (0.1.0) chỉ có <c>events</c> và vẫn đọc y như cũ. Giờ theo ISO 8601; không ghi múi giờ thì hiểu là UTC.
    /// <c>configKey</c> không bắt buộc.
    ///
    /// <para>Không ném exception: JSON sai trên remote config không được làm game crash. Mục hỏng bị bỏ và ghi lý do vào
    /// <see cref="LiveEventCalendarParseResult.Problems"/> (gồm cả trùng id, chồng giờ, luật lặp hỏng và đợt bị luật lặp che).
    /// Đọc và bỏ mục đi qua đúng bộ biên dịch mà hub dùng (<see cref="LiveEventCalendarCompiler"/>), nên "Kiểm lịch" nói
    /// giữ mục nào thì game giữ đúng mục đó.</para>
    /// </summary>
    public static class JsonLiveEventCalendarParser
    {
        /// <summary>
        /// Chính sách của overload hai tham số khi JSON remote có chữ nhưng không dùng được. Từ 0.2.0 là
        /// <see cref="LiveEventCalendarRemoteFailurePolicy.UseDefaultCalendar"/> (user chốt Q-9): một bản remote hỏng không
        /// được làm cả game mất sạch event, vì "không đợt nào chạy" là hỏng nặng hơn "lịch trong build có thể cũ".
        /// Không phải đổi hành vi của 0.1.0 (bản đó chưa có <c>ParseOrDefault</c>) — muốn cách cũ của <see cref="Parse"/>
        /// thì gọi overload ba tham số với <see cref="LiveEventCalendarRemoteFailurePolicy.KeepRemoteResult"/>.
        /// </summary>
        public const LiveEventCalendarRemoteFailurePolicy DefaultRemoteFailurePolicy = LiveEventCalendarRemoteFailurePolicy.UseDefaultCalendar;

        /// <summary>Định dạng lớn nhất parser này hiểu trọn.</summary>
        private const int NewestKnownFormatVersion = 2;

        private const string FixedEntryKeyPrefix = "json-";

#pragma warning disable 0649 // Field do JsonUtility gán.
        [Serializable]
        private sealed class CalendarDocument
        {
            public int version;
            public RecurringEntry[] recurring;
            public EventEntry[] events;
        }

        [Serializable]
        private sealed class RecurringEntry
        {
            public string type;
            public string anchorUtc;
            public string idPrefix;
            public int periodHours;
            public int activeHours;
            public string configKey;
        }

        [Serializable]
        private sealed class EventEntry
        {
            public string id;
            public string type;
            public string startUtc;
            public string endUtc;
            public string configKey;
        }
#pragma warning restore 0649

        /// <summary>0.1.0: giữ chữ ký; định dạng 1 ra đúng chuỗi Problems cũ (khoá bằng golden), định dạng 2 thêm luật lặp.</summary>
        public static LiveEventCalendarParseResult Parse(string json)
        {
            return CompileReadResult(ParseDocument(json));
        }

        /// <summary>
        /// JSON → tài liệu, KHÔNG biên dịch: giữ chuỗi hỏng và thứ tự nguồn để hub hiện đúng cái JSON viết. Không ném.
        /// </summary>
        public static LiveEventCalendarDocumentParseResult ParseDocument(string json)
        {
            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return new LiveEventCalendarDocumentParseResult(LiveEventCalendarDocument.Empty, 0, true, true, false, string.Empty, problems);
            }

            CalendarDocument source;
            try
            {
                source = JsonUtility.FromJson<CalendarDocument>(json);
            }
            catch (Exception exception)
            {
                // Đo ở hai bản (SP-11, JsonUtilityBehaviourTests): sai cú pháp, BOM, gốc không phải object đều ném
                // ArgumentException. Bắt mọi Exception vì lời hứa "dữ liệu remote hỏng không làm game crash" không được
                // phụ thuộc loại exception của một bản Unity chưa đo.
                problems.Add("JSON lịch event hỏng: " + exception.Message);
                return new LiveEventCalendarDocumentParseResult(LiveEventCalendarDocument.Empty, 0, false, false, false, exception.Message, problems);
            }

            // FromJson chỉ trả null với chuỗi rỗng/null (đã chặn ở trên) — vẫn kiểm null thay vì dựa exception (SP-11).
            if (source == null || (source.events == null && source.recurring == null))
            {
                int missingFormatVersion = source != null && source.version > 0 ? source.version : 1;
                problems.Add("JSON lịch event thiếu mảng \"events\".");
                return new LiveEventCalendarDocumentParseResult(LiveEventCalendarDocument.Empty, missingFormatVersion, true, false, true, string.Empty,
                    problems);
            }

            // Mảng VẮNG MẶT là null, còn "recurring": null / [] là mảng rỗng (đo SP-11) — nên "có recurring" = có ghi key.
            int formatVersion = source.version > 0 ? source.version : (source.recurring != null ? NewestKnownFormatVersion : 1);
            if (formatVersion > NewestKnownFormatVersion)
            {
                problems.Add("Lịch định dạng " + formatVersion.ToString(CultureInfo.InvariantCulture) +
                             " mới hơn parser này (đọc được 1 và 2) — chỉ đọc recurring và events.");
            }

            var builder = new LiveEventCalendarDocumentBuilder();
            if (source.recurring != null)
            {
                for (int index = 0; index < source.recurring.Length; index++)
                {
                    RecurringEntry entry = source.recurring[index] ?? new RecurringEntry();
                    builder.WithRecurringRule(new RecurringLiveEventRule(entry.type, entry.anchorUtc, entry.idPrefix, entry.periodHours,
                        entry.activeHours, entry.configKey));
                }
            }
            if (source.events != null)
            {
                for (int index = 0; index < source.events.Length; index++)
                {
                    // JsonUtility dựng instance cho phần tử null (đo SP-11) — field null được tài liệu chuẩn hoá thành "", nên
                    // câu Problems vẫn trùng 0.1.0 ("Mục thứ N (''): startUtc …").
                    EventEntry entry = source.events[index] ?? new EventEntry();
                    builder.WithFixedEvent(new FixedLiveEventEntry(FixedEntryKeyPrefix + index.ToString(CultureInfo.InvariantCulture), entry.id,
                        entry.type, entry.startUtc, entry.endUtc, entry.configKey));
                }
            }

            return new LiveEventCalendarDocumentParseResult(builder.Build(), formatVersion, true, false, false, string.Empty, problems);
        }

        /// <summary>
        /// (D1) JSON remote trống → lịch trong asset; JSON có chữ nhưng KHÔNG DÙNG ĐƯỢC → theo
        /// <see cref="DefaultRemoteFailurePolicy"/> (0.2.0: cũng là lịch trong asset, kèm Problem nói vì sao — Q-9).
        /// Đây là mặc định của MỘT API MỚI, không phải đổi hành vi của bản cũ: 0.1.0 không có <c>ParseOrDefault</c>
        /// (chỉ có <see cref="Parse"/>, và <see cref="Parse"/> giữ nguyên), nên game bump 0.1.0 → 0.2.0 không bị đổi gì.
        /// Chỉ các bản dựng trước của nhánh 0.2.0 — lúc mặc định còn là <c>KeepRemoteResult</c> — mới thấy khác.
        /// </summary>
        public static LiveEventCalendarParseResult ParseOrDefault(string json, LiveEventCalendarAsset defaultCalendar)
        {
            return ParseOrDefault(json, defaultCalendar, DefaultRemoteFailurePolicy);
        }

        /// <summary>
        /// JSON null/khoảng trắng → <c>defaultCalendar.ToParseResult()</c> (<c>CameFromDefaultCalendar</c> = true).
        /// <c>defaultCalendar</c> null → như <see cref="Parse"/>. JSON có chữ nhưng KHÔNG DÙNG ĐƯỢC (xem
        /// <see cref="IsRemoteCalendarUnusable"/>: <c>JsonUtility</c> không đọc nổi, HOẶC đọc được mà không mang mảng lịch
        /// nào) → theo <paramref name="remoteFailurePolicy"/>. JSON có mảng lịch nhưng vài mục hỏng → LUÔN dùng kết quả
        /// remote (mục hỏng bị bỏ + Problems), không bao giờ trộn với asset: trộn hai nguồn sinh lịch không ai đã đăng.
        /// </summary>
        public static LiveEventCalendarParseResult ParseOrDefault(string json, LiveEventCalendarAsset defaultCalendar,
            LiveEventCalendarRemoteFailurePolicy remoteFailurePolicy)
        {
            if (!Enum.IsDefined(typeof(LiveEventCalendarRemoteFailurePolicy), remoteFailurePolicy))
            {
                throw new ArgumentOutOfRangeException(nameof(remoteFailurePolicy));
            }
            if (defaultCalendar == null) return Parse(json);
            if (string.IsNullOrWhiteSpace(json)) return defaultCalendar.ToParseResult();

            LiveEventCalendarDocumentParseResult readResult = ParseDocument(json);
            if (remoteFailurePolicy == LiveEventCalendarRemoteFailurePolicy.UseDefaultCalendar && IsRemoteCalendarUnusable(readResult))
            {
                LiveEventCalendarParseResult assetResult = defaultCalendar.ToParseResult();
                var problems = new List<string>(assetResult.Problems.Count + 1)
                {
                    DescribeUnusableRemoteCalendar(readResult),
                };
                problems.AddRange(assetResult.Problems);
                return LiveEventCalendarParseResult.FromCompilation(assetResult.Compilation, problems, assetResult.FormatVersion, true);
            }
            return CompileReadResult(readResult);
        }

        /// <summary>
        /// Remote KHÔNG DÙNG ĐƯỢC = <c>JsonUtility</c> không đọc nổi (sai cú pháp, BOM, gốc không phải object), HOẶC đọc
        /// được nhưng không mang mảng lịch nào (<c>"{}"</c>, gõ sai tên mảng thành <c>"evets"</c>, một định dạng sau này
        /// đổi tên mảng). Hai ca cho ra CÙNG một hậu quả với người chơi — không đợt nào chạy — nên Q-9 xử như nhau, kẻo
        /// lời hứa "remote hỏng thì trong build vẫn còn lịch" có một lỗ đúng bằng ca dễ gặp nhất.
        /// <c>"events": []</c> KHÔNG rơi vào đây: mảng có mặt nghĩa là designer cố ý đăng một lịch rỗng.
        /// </summary>
        private static bool IsRemoteCalendarUnusable(LiveEventCalendarDocumentParseResult readResult)
        {
            return !readResult.IsReadable || readResult.IsMissingCalendarArrays;
        }

        /// <summary>
        /// Câu Problem đứng đầu khi quay về lịch trong asset. Hai ca nói hai câu KHÁC NHAU để dev đọc log biết ngay phải
        /// sửa gì: JSON sai cú pháp thì kèm được message gốc của <c>JsonUtility</c>, còn JSON đúng cú pháp mà thiếu mảng
        /// thì không có message nào để kèm — nói thẳng là thiếu mảng.
        /// </summary>
        private static string DescribeUnusableRemoteCalendar(LiveEventCalendarDocumentParseResult readResult)
        {
            if (!readResult.IsReadable)
            {
                return "JSON remote hỏng, dùng lịch mặc định trong asset: " + readResult.ReadErrorText;
            }
            return "JSON remote không có mảng lịch nào, dùng lịch mặc định trong asset.";
        }

        /// <summary>
        /// Biên dịch ĐÚNG thứ tự JSON (<see cref="LiveEventCalendarCompiler.Compile"/>, không sắp): trùng id của runtime giữ
        /// mục đứng trước trong thứ tự nhận, và "mục thứ N" của câu Problems phải là vị trí trong JSON như 0.1.0.
        /// </summary>
        private static LiveEventCalendarParseResult CompileReadResult(LiveEventCalendarDocumentParseResult readResult)
        {
            // Thiếu cả hai mảng cũng về Empty: 0.1.0 trả đúng tham chiếu FixedLiveEventCalendar.Empty cho "{}", còn biên dịch
            // tài liệu rỗng dựng một FixedLiveEventCalendar mới — game so tham chiếu sẽ đổi hành vi dù lịch vẫn rỗng.
            if (readResult.IsBlank || !readResult.IsReadable || readResult.IsMissingCalendarArrays)
            {
                return LiveEventCalendarParseResult.Empty(readResult.Problems, readResult.FormatVersion);
            }

            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(readResult.Document);
            var problems = new List<string>(readResult.Problems.Count + compilation.Problems.Count);
            problems.AddRange(readResult.Problems);
            problems.AddRange(compilation.Problems);
            return LiveEventCalendarParseResult.FromCompilation(compilation, problems, readResult.FormatVersion, false);
        }
    }
}
