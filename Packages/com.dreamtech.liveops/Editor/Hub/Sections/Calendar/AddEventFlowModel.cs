using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Một dòng loại ở bước 1 của popover Thêm đợt [SD1 §3.11]; loại có luật lặp bị tắt và nói rõ sửa ở đâu.</summary>
    internal sealed class AddEventTypeChoice
    {
        internal AddEventTypeChoice(string typeId, string displayName, int colorSlot, bool isEnabled, string tagText)
        {
            TypeId = typeId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ColorSlot = colorSlot;
            IsEnabled = isEnabled;
            TagText = tagText ?? string.Empty;
        }

        public string TypeId { get; }
        public string DisplayName { get; }
        public int ColorSlot { get; }

        /// <summary><c>false</c> cho loại sinh từ luật — Lịch không tạo đợt cố định trên làn lặp (7.3).</summary>
        public bool IsEnabled { get; }

        /// <summary>Nhãn phụ căn phải: "cố định" · "phải bấm tham gia" · "(sinh từ luật — sửa ở Luật lặp)".</summary>
        public string TagText { get; }
    }

    /// <summary>
    /// Ba bước của popover Thêm đợt [SD1 §3.11] — bất biến, mỗi <c>With…</c> trả một bản mới, nên popover giữ đúng một tham chiếu
    /// và Quay lại chỉ là giữ lại bản trước. Kiểm nhanh (<see cref="QuickCheck"/>) chỉ chạy trên LÀN của loại đang chọn: nó trả
    /// lời "đợt này có bị bỏ không", không thay cho lần kiểm đủ 12 luật ở màn Kiểm lịch (7.3).
    /// </summary>
    internal sealed class AddEventFlowModel
    {
        internal const int StepChooseType = 1;
        internal const int StepChooseTimes = 2;
        internal const int StepReview = 3;

        internal const int DefaultDurationHours = 72;

        private readonly LiveOpsHubCalendarSession _session;
        private readonly DateTime _nowUtc;
        private readonly string _entryKey;

        private AddEventFlowModel(LiveOpsHubCalendarSession session, DateTime nowUtc, string entryKey, int step, string eventType,
            string startDateText, string startTimeText, int durationHours, string eventId, string configKey)
        {
            _session = session;
            _nowUtc = nowUtc;
            _entryKey = entryKey;
            Step = step;
            EventType = eventType ?? string.Empty;
            StartDateText = startDateText ?? string.Empty;
            StartTimeText = startTimeText ?? string.Empty;
            DurationHours = durationHours;
            EventId = eventId ?? string.Empty;
            ConfigKey = configKey ?? string.Empty;
        }

        public int Step { get; }
        public string EventType { get; }
        public string StartDateText { get; }
        public string StartTimeText { get; }
        public int DurationHours { get; }

        /// <summary>Id người dùng đang giữ ở bước 3; rỗng nghĩa là chưa sửa nên dùng <see cref="SuggestedEventId"/>.</summary>
        public string EventId { get; }

        public string ConfigKey { get; }

        /// <summary>Giờ bắt đầu đọc được từ hai ô; <c>null</c> khi chuỗi chưa hợp lệ (nút Tiếp tắt).</summary>
        public DateTime? StartUtc
        {
            get
            {
                if (!LiveOpsUtcDateTimeField.TryParseParts(StartDateText, StartTimeText, out DateTime startUtc)) return null;
                return startUtc;
            }
        }

        public DateTime? EndUtc
        {
            get
            {
                DateTime? startUtc = StartUtc;
                if (startUtc == null || DurationHours <= 0) return null;
                return startUtc.Value.AddHours(DurationHours);
            }
        }

        /// <summary>Id đề nghị theo quy ước của lịch (PD-20); người dùng sửa được ở bước 3.</summary>
        public string SuggestedEventId
        {
            get
            {
                if (EventId.Length > 0) return EventId;
                DateTime? startUtc = StartUtc;
                if (EventType.Length == 0 || startUtc == null) return string.Empty;
                return LiveOpsEventIdSuggester.Suggest(_session.Document ?? LiveEventCalendarDocument.Empty, EventType,
                    startUtc.Value, string.Empty);
            }
        }

        /// <summary>Config key sẽ ghi ra JSON khi ô để trống — chữ dẫn nghiêng của bước 3 nêu đúng giá trị này.</summary>
        public string EffectiveConfigKey
        {
            get
            {
                if (ConfigKey.Length > 0) return ConfigKey;
                LiveEventCalendarDocument document = _session.Document ?? LiveEventCalendarDocument.Empty;
                return document.TryGetEventType(EventType, out LiveEventTypeDefinition type) ? type.DefaultConfigKey : string.Empty;
            }
        }

        /// <summary>Kiểm nhanh làn của loại đang chọn với đợt sắp thêm; <c>null</c> khi chưa đủ dữ liệu để dựng đợt.</summary>
        public LiveEventCalendarCheckReport QuickCheck
        {
            get
            {
                LiveEventCalendarDocument preview = BuildPreviewDocument();
                return preview == null ? null : _session.CheckLane(EventType, preview);
            }
        }

        /// <summary>Đợt sắp thêm sẽ bị game bỏ — bước 3 đổi sang biến thể "Quay lại sửa giờ" / "Vẫn thêm".</summary>
        public bool WillBeDropped
        {
            get
            {
                LiveEventCalendarFinding finding = DropFinding;
                return finding != null;
            }
        }

        /// <summary>Phát hiện Bị bỏ của chính đợt sắp thêm; <c>null</c> khi không có. Câu chữ lấy từ <see cref="LiveOpsFindingText"/> (V-8).</summary>
        public LiveEventCalendarFinding DropFinding
        {
            get
            {
                LiveEventCalendarCheckReport report = QuickCheck;
                if (report == null) return null;
                string eventId = SuggestedEventId;
                IReadOnlyList<LiveEventCalendarFinding> findings = report.Findings;
                for (int index = 0; index < findings.Count; index++)
                {
                    LiveEventCalendarFinding finding = findings[index];
                    if (finding.Consequence != LiveEventCalendarConsequence.Dropped) continue;
                    bool isThisEntry = string.Equals(finding.TargetEntryKey, _entryKey, StringComparison.Ordinal)
                        || string.Equals(finding.TargetId, eventId, StringComparison.Ordinal);
                    if (isThisEntry) return finding;
                }
                return null;
            }
        }

        /// <summary>Bước 1 mở từ nút header: chưa có loại, giờ mặc định là 00:00 UTC của ngày mai theo đồng hồ của phiên.</summary>
        public static AddEventFlowModel Create(LiveOpsHubCalendarSession session, DateTime nowUtc)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            DateTime now = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            DateTime start = now.Date.AddDays(1);
            return new AddEventFlowModel(session, now, FixedLiveEventEntry.CreateEntryKey(), StepChooseType, string.Empty,
                DateText(start), TimeText(start), DefaultDurationHours, string.Empty, string.Empty);
        }

        /// <summary>Nhấp đúp chỗ trống trên làn cố định: loại và giờ đã biết nên vào thẳng bước 2 (7.3).</summary>
        public static AddEventFlowModel CreateAt(LiveOpsHubCalendarSession session, DateTime nowUtc, string eventType, DateTime startUtc,
            int durationHours)
        {
            AddEventFlowModel model = Create(session, nowUtc);
            DateTime start = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
            int duration = durationHours > 0 ? durationHours : DefaultDurationHours;
            return new AddEventFlowModel(model._session, model._nowUtc, model._entryKey, StepChooseTimes, eventType ?? string.Empty,
                DateText(start), TimeText(start), duration, string.Empty, string.Empty);
        }

        /// <summary>Loại lọc theo chuỗi gõ; loại có luật lặp vẫn hiện (disabled) để người dùng biết nó tồn tại và sửa ở đâu.</summary>
        public IReadOnlyList<AddEventTypeChoice> TypeChoices(string filterText)
        {
            LiveEventCalendarDocument document = _session.Document ?? LiveEventCalendarDocument.Empty;
            string filter = filterText == null ? string.Empty : filterText.Trim();
            List<AddEventTypeChoice> choices = new List<AddEventTypeChoice>();
            IReadOnlyList<LiveEventTypeDefinition> types = document.EventTypes;
            for (int index = 0; index < types.Count; index++)
            {
                LiveEventTypeDefinition type = types[index];
                if (filter.Length > 0 && type.TypeId.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                    && type.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
                bool isRecurring = document.TryGetRecurringRule(type.TypeId, out RecurringLiveEventRule _);
                string tag = isRecurring
                    ? LiveOpsHubStrings.CalendarAddTypeRecurringTag
                    : type.RequiresJoin ? LiveOpsHubStrings.CalendarAddTypeOptInTag : LiveOpsHubStrings.CalendarAddTypeFixedTag;
                choices.Add(new AddEventTypeChoice(type.TypeId, type.DisplayName, type.ColorSlot, !isRecurring, tag));
            }
            return choices;
        }

        public AddEventFlowModel WithType(string typeId)
        {
            return new AddEventFlowModel(_session, _nowUtc, _entryKey, StepChooseTimes, typeId, StartDateText, StartTimeText,
                DurationHours, EventId, ConfigKey);
        }

        public AddEventFlowModel WithTimes(string startDateText, string startTimeText, int durationHours)
        {
            return new AddEventFlowModel(_session, _nowUtc, _entryKey, StepReview, EventType, startDateText, startTimeText,
                durationHours, EventId, ConfigKey);
        }

        public AddEventFlowModel WithEventId(string eventId)
        {
            return new AddEventFlowModel(_session, _nowUtc, _entryKey, Step, EventType, StartDateText, StartTimeText, DurationHours,
                eventId, ConfigKey);
        }

        public AddEventFlowModel WithConfigKey(string configKey)
        {
            return new AddEventFlowModel(_session, _nowUtc, _entryKey, Step, EventType, StartDateText, StartTimeText, DurationHours,
                EventId, configKey);
        }

        /// <summary>Lùi một bước; ở bước 1 trả chính nó (Quay lại không đóng popover).</summary>
        public AddEventFlowModel Back()
        {
            int step = Step <= StepChooseType ? StepChooseType : Step - 1;
            return new AddEventFlowModel(_session, _nowUtc, _entryKey, step, EventType, StartDateText, StartTimeText, DurationHours,
                EventId, ConfigKey);
        }

        /// <summary>Đủ dữ liệu để sang bước sau chưa — nút Tiếp đọc giá trị này thay vì tự đoán.</summary>
        public bool CanAdvance()
        {
            if (Step == StepChooseType) return EventType.Length > 0;
            if (Step == StepChooseTimes) return StartUtc != null && DurationHours > 0;
            return SuggestedEventId.Length > 0;
        }

        /// <summary>Lệnh thêm đợt; <c>null</c> khi chưa đủ dữ liệu (nút bị tắt nên không gọi tới đây).</summary>
        public AddFixedEventEdit ToEdit()
        {
            FixedLiveEventEntry entry = BuildEntry();
            return entry == null ? null : new AddFixedEventEdit(entry);
        }

        private FixedLiveEventEntry BuildEntry()
        {
            DateTime? startUtc = StartUtc;
            DateTime? endUtc = EndUtc;
            string eventId = SuggestedEventId;
            if (startUtc == null || endUtc == null || eventId.Length == 0 || EventType.Length == 0) return null;
            return new FixedLiveEventEntry(_entryKey, eventId, EventType, LiveEventUtcText.Format(startUtc.Value),
                LiveEventUtcText.Format(endUtc.Value), ConfigKey);
        }

        private LiveEventCalendarDocument BuildPreviewDocument()
        {
            FixedLiveEventEntry entry = BuildEntry();
            if (entry == null) return null;
            LiveEventCalendarDocument document = _session.Document ?? LiveEventCalendarDocument.Empty;
            return LiveEventCalendarEdits.TryApply(document, new AddFixedEventEdit(entry), out LiveEventCalendarDocument preview)
                ? preview
                : null;
        }

        private static string DateText(DateTime utc)
        {
            return utc.ToString(LiveOpsUtcDateTimeField.DateFormat, CultureInfo.InvariantCulture);
        }

        private static string TimeText(DateTime utc)
        {
            return utc.ToString(TimeTextFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>Ô giờ của <see cref="LiveOpsUtcDateTimeField"/> — HH:mm, không giây.</summary>
        private const string TimeTextFormat = "HH:mm";
    }
}
