using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một lệnh sửa tài liệu lịch. Họ đóng có chủ đích (ctor <c>internal</c>): mọi lệnh sửa sống trong file này, để
    /// <see cref="LiveEventCalendarEdits.TryApply"/> xử lý được hết mà không sợ thiếu case ở bản build khác.
    /// </summary>
    public abstract class LiveEventCalendarEdit
    {
        internal LiveEventCalendarEdit()
        {
        }
    }

    public sealed class AddFixedEventEdit : LiveEventCalendarEdit
    {
        public AddFixedEventEdit(FixedLiveEventEntry entry) => Entry = entry;
        public FixedLiveEventEntry Entry { get; }
    }

    /// <summary>Thay đợt theo <see cref="FixedLiveEventEntry.EntryKey"/>.</summary>
    public sealed class ReplaceFixedEventEdit : LiveEventCalendarEdit
    {
        public ReplaceFixedEventEdit(FixedLiveEventEntry entry) => Entry = entry;
        public FixedLiveEventEntry Entry { get; }
    }

    public sealed class RemoveFixedEventEdit : LiveEventCalendarEdit
    {
        public RemoveFixedEventEdit(string entryKey) => EntryKey = entryKey;
        public string EntryKey { get; }
    }

    /// <summary>Thêm luật mới, hoặc thay luật hiện có của cùng loại.</summary>
    public sealed class SetRecurringRuleEdit : LiveEventCalendarEdit
    {
        public SetRecurringRuleEdit(RecurringLiveEventRule rule) => Rule = rule;
        public RecurringLiveEventRule Rule { get; }
    }

    public sealed class RemoveRecurringRuleEdit : LiveEventCalendarEdit
    {
        public RemoveRecurringRuleEdit(string eventType) => EventType = eventType;
        public string EventType { get; }
    }

    public sealed class SetEventTypeEdit : LiveEventCalendarEdit
    {
        public SetEventTypeEdit(LiveEventTypeDefinition type) => Type = type;
        public LiveEventTypeDefinition Type { get; }
    }

    public sealed class RemoveEventTypeEdit : LiveEventCalendarEdit
    {
        public RemoveEventTypeEdit(string typeId) => TypeId = typeId;
        public string TypeId { get; }
    }

    /// <summary>(V-12) "Đưa lên / Đưa xuống" làn; <see cref="NewIndex"/> kẹp vào [0, số loại − 1]. Không đổi JSON/sha.</summary>
    public sealed class MoveEventTypeEdit : LiveEventCalendarEdit
    {
        public MoveEventTypeEdit(string typeId, int newIndex)
        {
            TypeId = typeId;
            NewIndex = newIndex;
        }

        public string TypeId { get; }
        public int NewIndex { get; }
    }

    public sealed class SetRemoteConfigKeyEdit : LiveEventCalendarEdit
    {
        public SetRemoteConfigKeyEdit(string remoteConfigKey) => RemoteConfigKey = remoteConfigKey;
        public string RemoteConfigKey { get; }
    }

    public sealed class AddPublishedStampEdit : LiveEventCalendarEdit
    {
        public AddPublishedStampEdit(PublishedCalendarStamp stamp) => Stamp = stamp;
        public PublishedCalendarStamp Stamp { get; }
    }

    public sealed class RemoveLatestPublishedStampEdit : LiveEventCalendarEdit
    {
    }

    public sealed class AddIgnoredWarningEdit : LiveEventCalendarEdit
    {
        public AddIgnoredWarningEdit(IgnoredCalendarWarning warning) => Warning = warning;
        public IgnoredCalendarWarning Warning { get; }
    }

    public sealed class RemoveIgnoredWarningEdit : LiveEventCalendarEdit
    {
        public RemoveIgnoredWarningEdit(IgnoredCalendarWarning warning) => Warning = warning;
        public IgnoredCalendarWarning Warning { get; }
    }

    /// <summary>Thay nháp, Khôi phục.</summary>
    public sealed class ReplaceDocumentEdit : LiveEventCalendarEdit
    {
        public ReplaceDocumentEdit(LiveEventCalendarDocument document) => Document = document;
        public LiveEventCalendarDocument Document { get; }
    }

    /// <summary>Sửa hàng loạt = một Undo group.</summary>
    public sealed class CompositeCalendarEdit : LiveEventCalendarEdit
    {
        public CompositeCalendarEdit(IEnumerable<LiveEventCalendarEdit> edits)
        {
            var list = new List<LiveEventCalendarEdit>();
            if (edits != null)
            {
                foreach (LiveEventCalendarEdit edit in edits)
                {
                    if (edit != null) list.Add(edit);
                }
            }
            Edits = list;
        }

        public IReadOnlyList<LiveEventCalendarEdit> Edits { get; }
    }
}
