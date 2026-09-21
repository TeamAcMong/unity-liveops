using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DreamTech.LiveOps.Unity
{
    /// <summary>
    /// Lịch LiveOps lưu trong project (D1): định nghĩa loại, luật lặp, đợt cố định, lịch sử đăng và cảnh báo đã bỏ qua —
    /// MỘT asset review được bằng git. Game dùng nó làm lịch mặc định khi remote config trống
    /// (<see cref="JsonLiveEventCalendarParser.ParseOrDefault(string, LiveEventCalendarAsset)"/>) và để đăng ký loại
    /// (<see cref="LiveOpsSystemBuilderCalendarAssetExtensions.WithEventTypesFrom"/>); hub sửa nó qua
    /// <see cref="ApplyDocument"/>.
    ///
    /// <para>Asset chỉ là nơi lưu: mọi phán hợp lệ nằm ở bộ biên dịch core, để "game đọc asset", "game đọc JSON đã xuất"
    /// và "Kiểm lịch của hub" không bao giờ tự đoán luật riêng.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "DreamTech/LiveOps/Lịch LiveOps", fileName = "Main", order = 100)]
    public sealed class LiveEventCalendarAsset : ScriptableObject
    {
        public const int CurrentSchemaVersion = 1;

        // Tiền tố entryKey thay thế cho đợt thiếu/trùng khoá trong YAML (sửa tay, xung đột merge). Tất định theo vị trí để
        // gọi ToDocument nhiều lần (Undo, postprocessor) ra cùng tài liệu — khoá ngẫu nhiên sẽ làm hub báo "chưa lưu" giả.
        private const string ReplacementEntryKeyPrefix = "asset-fixed-";

        // Thứ tự field = thứ tự dòng YAML (mục 4.1). Tên field là định dạng file của người dùng — không đổi tên.
        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string remoteConfigKey = LiveEventCalendarDocument.DefaultRemoteConfigKey;
        [SerializeField] private List<LiveEventTypeAssetEntry> eventTypes = new List<LiveEventTypeAssetEntry>();
        [SerializeField] private List<RecurringRuleAssetEntry> recurringRules = new List<RecurringRuleAssetEntry>();
        [SerializeField] private List<FixedEventAssetEntry> fixedEvents = new List<FixedEventAssetEntry>();
        [SerializeField] private List<PublishedStampAssetEntry> publishedStamps = new List<PublishedStampAssetEntry>();
        [SerializeField] private List<IgnoredWarningAssetEntry> ignoredWarnings = new List<IgnoredWarningAssetEntry>();

        /// <summary>Lớn hơn <see cref="CurrentSchemaVersion"/> = asset do bản package mới hơn lưu; vẫn đọc field quen.</summary>
        public int SchemaVersion => schemaVersion;

        /// <summary>
        /// Đọc asset thành tài liệu. Không ném: YAML sửa tay hoặc xung đột merge không được làm hub/game hỏng. Field null
        /// coi như rỗng; loại có id sai quy tắc hoặc trùng id bị bỏ (tài liệu không chứa được loại đó); ô màu ngoài
        /// [0, 7] lấy ô mặc định của loại; đợt thiếu/trùng entryKey nhận khoá thay thế tất định. Chuỗi giờ hỏng giữ
        /// nguyên văn (PD-2) — bộ biên dịch mới là nơi báo.
        /// </summary>
        public LiveEventCalendarDocument ToDocument()
        {
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(remoteConfigKey ?? string.Empty);

            var seenTypeIds = new HashSet<string>(StringComparer.Ordinal);
            if (eventTypes != null)
            {
                for (int index = 0; index < eventTypes.Count; index++)
                {
                    LiveEventTypeAssetEntry entry = eventTypes[index];
                    if (entry == null || !IsValidIdentifier(entry.TypeId) || !seenTypeIds.Add(entry.TypeId)) continue;

                    int colorSlot = entry.ColorSlot >= 0 && entry.ColorSlot < LiveEventTypeDefinition.ColorSlotCount
                        ? entry.ColorSlot
                        : LiveEventTypeColorSlots.DefaultSlotFor(entry.TypeId);
                    builder.WithEventType(new LiveEventTypeDefinition(entry.TypeId, entry.DisplayName, colorSlot, entry.RequiresJoin,
                        entry.DefaultConfigKey));
                }
            }

            if (recurringRules != null)
            {
                for (int index = 0; index < recurringRules.Count; index++)
                {
                    RecurringRuleAssetEntry entry = recurringRules[index];
                    if (entry == null) continue;
                    builder.WithRecurringRule(new RecurringLiveEventRule(entry.EventType, entry.AnchorUtc, entry.IdPrefix,
                        entry.PeriodHours, entry.ActiveHours, entry.ConfigKey));
                }
            }

            if (fixedEvents != null)
            {
                var usedEntryKeys = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < fixedEvents.Count; index++)
                {
                    FixedEventAssetEntry entry = fixedEvents[index];
                    if (entry != null && entry.EntryKey.Length > 0) usedEntryKeys.Add(entry.EntryKey);
                }

                var assignedEntryKeys = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < fixedEvents.Count; index++)
                {
                    FixedEventAssetEntry entry = fixedEvents[index];
                    if (entry == null) continue;

                    string entryKey = entry.EntryKey;
                    // Mục đầu tiên giữ khoá của mình; mục trùng khoá đứng sau (hai nhánh cùng thêm một đợt rồi merge) nhận
                    // khoá thay thế, để builder không ném và cả hai đợt vẫn hiện cho người dùng tự gỡ.
                    if (entryKey.Length == 0 || assignedEntryKeys.Contains(entryKey))
                    {
                        entryKey = CreateReplacementEntryKey(index, usedEntryKeys);
                    }
                    assignedEntryKeys.Add(entryKey);
                    usedEntryKeys.Add(entryKey);

                    builder.WithFixedEvent(new FixedLiveEventEntry(entryKey, entry.EventId, entry.EventType, entry.StartUtc, entry.EndUtc,
                        entry.ConfigKey));
                }
            }

            if (publishedStamps != null)
            {
                for (int index = 0; index < publishedStamps.Count; index++)
                {
                    PublishedStampAssetEntry entry = publishedStamps[index];
                    if (entry == null) continue;
                    builder.WithPublishedStamp(new PublishedCalendarStamp(entry.PublishedUtc, entry.Publisher, entry.Sha256, entry.ByteCount,
                        entry.FormatVersion, entry.Note, entry.SnapshotJson));
                }
            }

            if (ignoredWarnings != null)
            {
                for (int index = 0; index < ignoredWarnings.Count; index++)
                {
                    IgnoredWarningAssetEntry entry = ignoredWarnings[index];
                    if (entry == null) continue;
                    builder.WithIgnoredWarning(new IgnoredCalendarWarning(entry.RuleId, entry.TargetId, entry.RangeStartUtc, entry.RangeEndUtc,
                        entry.Note, entry.ExpiresUtc));
                }
            }

            return builder.Build();
        }

        /// <summary>
        /// Ghi TOÀN BỘ field theo tài liệu (thay danh sách, không trộn). KHÔNG gọi Undo, KHÔNG SetDirty: runtime không có
        /// API Editor, và phiên của hub phải tự <c>Undo.RecordObject</c> TRƯỚC mỗi lần gọi (SP-9) rồi <c>SetDirty</c> sau.
        /// </summary>
        public void ApplyDocument(LiveEventCalendarDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            // Không hạ số schema: asset do bản mới hơn lưu vẫn phải báo "lưu bởi bản mới hơn" sau khi bản này ghi đè field
            // quen; chỉ nâng asset cũ/hỏng (0) lên số bản này hiểu.
            if (schemaVersion < CurrentSchemaVersion) schemaVersion = CurrentSchemaVersion;
            remoteConfigKey = document.RemoteConfigKey;

            var newEventTypes = new List<LiveEventTypeAssetEntry>(document.EventTypes.Count);
            for (int index = 0; index < document.EventTypes.Count; index++)
            {
                newEventTypes.Add(new LiveEventTypeAssetEntry(document.EventTypes[index]));
            }

            var newRecurringRules = new List<RecurringRuleAssetEntry>(document.RecurringRules.Count);
            for (int index = 0; index < document.RecurringRules.Count; index++)
            {
                newRecurringRules.Add(new RecurringRuleAssetEntry(document.RecurringRules[index]));
            }

            var newFixedEvents = new List<FixedEventAssetEntry>(document.FixedEvents.Count);
            for (int index = 0; index < document.FixedEvents.Count; index++)
            {
                newFixedEvents.Add(new FixedEventAssetEntry(document.FixedEvents[index]));
            }

            var newPublishedStamps = new List<PublishedStampAssetEntry>(document.PublishedStamps.Count);
            for (int index = 0; index < document.PublishedStamps.Count; index++)
            {
                newPublishedStamps.Add(new PublishedStampAssetEntry(document.PublishedStamps[index]));
            }

            var newIgnoredWarnings = new List<IgnoredWarningAssetEntry>(document.IgnoredWarnings.Count);
            for (int index = 0; index < document.IgnoredWarnings.Count; index++)
            {
                newIgnoredWarnings.Add(new IgnoredWarningAssetEntry(document.IgnoredWarnings[index]));
            }

            eventTypes = newEventTypes;
            recurringRules = newRecurringRules;
            fixedEvents = newFixedEvents;
            publishedStamps = newPublishedStamps;
            ignoredWarnings = newIgnoredWarnings;
        }

        /// <summary>
        /// Biên dịch ở THỨ TỰ XUẤT (V-6, <see cref="LiveEventCalendarCompiler.CompileInExportOrder"/>): trùng id giữ đúng
        /// đợt mà game sẽ giữ khi đọc JSON đã xuất, không phải đợt đứng trước trong asset.
        /// </summary>
        public LiveEventCalendarCompilation Compile()
        {
            return LiveEventCalendarCompiler.CompileInExportOrder(ToDocument());
        }

        /// <summary>
        /// Cùng hình kết quả với <see cref="JsonLiveEventCalendarParser.Parse"/> để composition root của game chỉ có một
        /// nhánh dù lịch đến từ remote hay từ asset. <c>FormatVersion</c> = 2 (asset mang được luật lặp),
        /// <c>CameFromDefaultCalendar</c> = true.
        /// </summary>
        public LiveEventCalendarParseResult ToParseResult()
        {
            LiveEventCalendarCompilation compilation = Compile();
            return LiveEventCalendarParseResult.FromCompilation(compilation, new List<string>(compilation.Problems),
                LiveEventCalendarParseResult.AssetFormatVersion, true);
        }

        private static string CreateReplacementEntryKey(int index, HashSet<string> usedEntryKeys)
        {
            string baseKey = ReplacementEntryKeyPrefix + index.ToString(CultureInfo.InvariantCulture);
            string candidate = baseKey;
            int suffix = 1;
            while (usedEntryKeys.Contains(candidate))
            {
                suffix++;
                candidate = baseKey + "-" + suffix.ToString(CultureInfo.InvariantCulture);
            }
            return candidate;
        }

        // Cùng luật id của LiveEventInstance (không rỗng, không '#', không xuống dòng) — hàm đó internal ở core, và ctor
        // LiveEventTypeDefinition ném khi sai, nên kiểm trước để ToDocument giữ lời hứa không ném.
        private static bool IsValidIdentifier(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(LiveEventInstance.ReservedSeparator) < 0 &&
                   value.IndexOf('\n') < 0 &&
                   value.IndexOf('\r') < 0;
        }
    }
}
