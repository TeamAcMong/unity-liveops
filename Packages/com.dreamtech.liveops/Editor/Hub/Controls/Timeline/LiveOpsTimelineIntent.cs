using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Ý định người dùng phát ra từ timeline — HỌ ĐÓNG (V-10). Control chỉ phát dữ liệu, không tự sửa tài liệu, không dựng menu hay
    /// hover card; presenter (G-CALENDAR W4, G-CALENDAR-DEPTH W5) đổi ý định thành <see cref="LiveEventCalendarEdit"/> hoặc mở
    /// popover. Họ đóng để presenter đối chiếu được "đã xử lý hoặc cố ý bỏ qua mọi loại": ctor <c>private protected</c> chặn
    /// lớp con ngoài file này, và <see cref="KnownIntentTypeNames"/> là danh sách tên cố định mà test khoá bằng reflection.
    /// </summary>
    internal abstract class LiveOpsTimelineIntent
    {
        private static readonly string[] IntentTypeNames =
        {
            nameof(SelectBarIntent),
            nameof(SelectManyIntent),
            nameof(MoveBarIntent),
            nameof(AddAtTimeIntent),
            nameof(DeleteBarIntent),
            nameof(DuplicateBarIntent),
            nameof(CopyBarIntent),
            nameof(PasteAtTimeIntent),
            nameof(OpenRuleIntent),
            nameof(ShowFindingIntent),
            nameof(CreateByDragIntent),
            nameof(HideLaneIntent),
            nameof(ShowAllLanesIntent),
            nameof(MoveLaneIntent),
            nameof(ToggleLaneCollapsedIntent),
        };

        private protected LiveOpsTimelineIntent()
        {
        }

        /// <summary>
        /// Tên mọi lớp ý định, theo thứ tự khai. Thêm ý định mới = thêm tên ở đây; test <c>Intents_ClosedFamily_AllHandledOrIgnoredByName</c>
        /// đỏ khi danh sách lệch với các lớp thật, và presenter W4/W5 đối chiếu danh sách này với nhánh xử lý của mình.
        /// </summary>
        public static IReadOnlyList<string> KnownIntentTypeNames => IntentTypeNames;
    }

    /// <summary>Pha của một thao tác kéo: xem trước mỗi lần di chuột, chốt khi thả, huỷ khi Esc — Esc không được tạo bước Undo.</summary>
    internal enum LiveOpsTimelineGesturePhase
    {
        Preview = 0,
        Commit = 1,
        Cancel = 2,
    }

    internal sealed class SelectBarIntent : LiveOpsTimelineIntent
    {
        public SelectBarIntent(string barKey, bool additive, bool range)
        {
            BarKey = barKey ?? string.Empty;
            Additive = additive;
            Range = range;
        }

        /// <summary>"" = bỏ chọn (bấm chỗ trống).</summary>
        public string BarKey { get; }

        /// <summary>⌘/Ctrl + click: bật tắt từng thanh.</summary>
        public bool Additive { get; }

        /// <summary>Shift + click: chọn dải trong làn.</summary>
        public bool Range { get; }
    }

    /// <summary>
    /// (G-OPT-TIMELINE) Lựa chọn NHIỀU thanh sau ⌘/Ctrl-click, Shift-click chọn dải hoặc khung chọn kéo trên chỗ trống
    /// (Hình 12 khung 9). Mang cả TẬP kết quả chứ không mang thao tác ("thêm khoá này"), để presenter không phải dựng lại luật
    /// bật tắt lần thứ hai và test đọc thẳng được tập cuối cùng.
    /// </summary>
    internal sealed class SelectManyIntent : LiveOpsTimelineIntent
    {
        private static readonly string[] EmptyKeys = new string[0];

        public SelectManyIntent(IReadOnlyList<string> barKeys, string primaryBarKey)
        {
            BarKeys = barKeys ?? EmptyKeys;
            PrimaryBarKey = primaryBarKey ?? string.Empty;
        }

        /// <summary>Tập thanh đang chọn sau thao tác, theo thứ tự trên trục; rỗng = bỏ chọn hết.</summary>
        public IReadOnlyList<string> BarKeys { get; }

        /// <summary>Thanh vừa chạm tới — neo của lần Shift-click sau và là thanh inspector lấy làm mốc; "" khi tập rỗng.</summary>
        public string PrimaryBarKey { get; }
    }

    /// <summary>(G-OPT-TIMELINE, Hình 12 khung 12) Thu gọn / mở một làn; presenter giữ danh sách trong view state như làn ẩn.</summary>
    internal sealed class ToggleLaneCollapsedIntent : LiveOpsTimelineIntent
    {
        public ToggleLaneCollapsedIntent(string typeId, bool collapsed)
        {
            TypeId = typeId ?? string.Empty;
            Collapsed = collapsed;
        }

        public string TypeId { get; }

        /// <summary>true = thu gọn làn, false = mở lại.</summary>
        public bool Collapsed { get; }
    }

    /// <summary>(G-OPT-TIMELINE) Một đợt bị kéo theo: giờ mới của đợt PHÍA SAU khi giữ Shift lúc đang kéo.</summary>
    internal sealed class LiveOpsTimelineBarMove
    {
        public LiveOpsTimelineBarMove(string barKey, DateTime newStartUtc, DateTime newEndUtc)
        {
            BarKey = barKey ?? string.Empty;
            NewStartUtc = newStartUtc;
            NewEndUtc = newEndUtc;
        }

        public string BarKey { get; }
        public DateTime NewStartUtc { get; }
        public DateTime NewEndUtc { get; }
    }

    internal sealed class MoveBarIntent : LiveOpsTimelineIntent
    {
        private static readonly LiveOpsTimelineBarMove[] NoFollowers = new LiveOpsTimelineBarMove[0];

        public MoveBarIntent(string barKey, DateTime newStartUtc, DateTime newEndUtc, LiveOpsTimelineGesturePhase phase)
            : this(barKey, newStartUtc, newEndUtc, phase, null)
        {
        }

        public MoveBarIntent(string barKey, DateTime newStartUtc, DateTime newEndUtc, LiveOpsTimelineGesturePhase phase,
            IReadOnlyList<LiveOpsTimelineBarMove> followers)
        {
            BarKey = barKey ?? string.Empty;
            NewStartUtc = newStartUtc;
            NewEndUtc = newEndUtc;
            Phase = phase;
            Followers = followers ?? NoFollowers;
        }

        public string BarKey { get; }
        public DateTime NewStartUtc { get; }
        public DateTime NewEndUtc { get; }
        public LiveOpsTimelineGesturePhase Phase { get; }

        /// <summary>
        /// (G-OPT-TIMELINE) Đợt phía sau bị kéo theo vì người dùng nhấn Shift SAU khi đã bắt đầu kéo. Rỗng ở mọi cử chỉ thường —
        /// một cử chỉ vẫn là MỘT bước Undo, nên đợt kéo theo đi cùng intent chứ không thành lệnh sửa riêng.
        /// </summary>
        public IReadOnlyList<LiveOpsTimelineBarMove> Followers { get; }
        public bool IsPreview => Phase == LiveOpsTimelineGesturePhase.Preview;
        public bool IsCommit => Phase == LiveOpsTimelineGesturePhase.Commit;
        public bool IsCancel => Phase == LiveOpsTimelineGesturePhase.Cancel;
    }

    internal sealed class AddAtTimeIntent : LiveOpsTimelineIntent
    {
        public AddAtTimeIntent(string laneTypeId, DateTime startUtc, DateTime? endUtc)
        {
            LaneTypeId = laneTypeId ?? string.Empty;
            StartUtc = startUtc;
            EndUtc = endUtc;
        }

        public string LaneTypeId { get; }
        public DateTime StartUtc { get; }

        /// <summary><c>null</c> = nhấp đúp (popover tự đề xuất độ dài); có giá trị = đã kéo ra khung.</summary>
        public DateTime? EndUtc { get; }
    }

    internal sealed class DeleteBarIntent : LiveOpsTimelineIntent
    {
        public DeleteBarIntent(string barKey)
        {
            BarKey = barKey ?? string.Empty;
        }

        public string BarKey { get; }
    }

    internal sealed class DuplicateBarIntent : LiveOpsTimelineIntent
    {
        public DuplicateBarIntent(string barKey)
        {
            BarKey = barKey ?? string.Empty;
        }

        public string BarKey { get; }
    }

    internal sealed class CopyBarIntent : LiveOpsTimelineIntent
    {
        public CopyBarIntent(string barKey)
        {
            BarKey = barKey ?? string.Empty;
        }

        public string BarKey { get; }
    }

    internal sealed class PasteAtTimeIntent : LiveOpsTimelineIntent
    {
        public PasteAtTimeIntent(string laneTypeId, DateTime atUtc)
        {
            LaneTypeId = laneTypeId ?? string.Empty;
            AtUtc = atUtc;
        }

        public string LaneTypeId { get; }
        public DateTime AtUtc { get; }
    }

    internal sealed class OpenRuleIntent : LiveOpsTimelineIntent
    {
        public OpenRuleIntent(string eventType)
        {
            EventType = eventType ?? string.Empty;
        }

        public string EventType { get; }
    }

    /// <summary>F8 / Shift+F8: lỗi kế tiếp / trước đó.</summary>
    internal sealed class ShowFindingIntent : LiveOpsTimelineIntent
    {
        public const int Next = 1;
        public const int Previous = -1;

        public ShowFindingIntent(int direction)
        {
            Direction = direction < 0 ? Previous : Next;
        }

        public int Direction { get; }
    }

    /// <summary>(V-10) ⌘/Ctrl + kéo chỗ trống làn cố định: thanh ma rồi mở popover Thêm đợt.</summary>
    internal sealed class CreateByDragIntent : LiveOpsTimelineIntent
    {
        public CreateByDragIntent(string laneTypeId, DateTime startUtc, DateTime endUtc, LiveOpsTimelineGesturePhase phase)
        {
            LaneTypeId = laneTypeId ?? string.Empty;
            StartUtc = startUtc;
            EndUtc = endUtc;
            Phase = phase;
        }

        public string LaneTypeId { get; }
        public DateTime StartUtc { get; }
        public DateTime EndUtc { get; }
        public LiveOpsTimelineGesturePhase Phase { get; }
        public bool IsPreview => Phase == LiveOpsTimelineGesturePhase.Preview;
        public bool IsCommit => Phase == LiveOpsTimelineGesturePhase.Commit;
        public bool IsCancel => Phase == LiveOpsTimelineGesturePhase.Cancel;
    }

    /// <summary>(V-10) Ẩn làn — presenter giữ danh sách làn ẩn trong view state và đưa lại qua <see cref="LiveOpsTimelineInput.WithHiddenLanes"/>.</summary>
    internal sealed class HideLaneIntent : LiveOpsTimelineIntent
    {
        public HideLaneIntent(string typeId)
        {
            TypeId = typeId ?? string.Empty;
        }

        public string TypeId { get; }
    }

    internal sealed class ShowAllLanesIntent : LiveOpsTimelineIntent
    {
    }

    /// <summary>(V-10, V-12) Đưa làn lên/xuống → <c>MoveEventTypeEdit</c>; không đổi JSON.</summary>
    internal sealed class MoveLaneIntent : LiveOpsTimelineIntent
    {
        public const int Up = -1;
        public const int Down = 1;

        public MoveLaneIntent(string typeId, int direction)
        {
            TypeId = typeId ?? string.Empty;
            Direction = direction < 0 ? Up : Down;
        }

        public string TypeId { get; }
        public int Direction { get; }
    }

    internal enum LiveOpsTimelineHitKind
    {
        None = 0,
        Bar = 1,
        LaneHeader = 2,
        EmptyLane = 3,
        Ruler = 4,
    }

    internal enum LiveOpsTimelineBarRegion
    {
        Body = 0,
        StartEdge = 1,
        EndEdge = 2,
    }

    /// <summary>(V-10) Kết quả bắt chuột của element (W3). Trường không áp dụng cho loại trúng thì để "" / <c>null</c>.</summary>
    internal sealed class LiveOpsTimelineHit
    {
        public static readonly LiveOpsTimelineHit None = new LiveOpsTimelineHit(LiveOpsTimelineHitKind.None, string.Empty,
            LiveOpsTimelineBarRegion.Body, string.Empty, null, Vector2.zero);

        public LiveOpsTimelineHit(LiveOpsTimelineHitKind kind, string barKey, LiveOpsTimelineBarRegion barRegion, string laneTypeId,
            DateTime? timeUtc, Vector2 localPosition)
        {
            Kind = kind;
            BarKey = barKey ?? string.Empty;
            BarRegion = barRegion;
            LaneTypeId = laneTypeId ?? string.Empty;
            TimeUtc = timeUtc;
            LocalPosition = localPosition;
        }

        public LiveOpsTimelineHitKind Kind { get; }
        public string BarKey { get; }
        public LiveOpsTimelineBarRegion BarRegion { get; }
        public string LaneTypeId { get; }

        /// <summary>EmptyLane: giờ đã bắt lưới theo bước hiện hành; Bar/Ruler: giờ tại con trỏ.</summary>
        public DateTime? TimeUtc { get; }

        public Vector2 LocalPosition { get; }
    }

    /// <summary>(V-10) Vào/rời thanh — không trễ; trễ 500/100 ms là việc của <c>LiveOpsHoverCardHost</c>.</summary>
    internal sealed class LiveOpsTimelineHover
    {
        public LiveOpsTimelineHover(string barKey, Rect barWorldBound)
        {
            BarKey = barKey ?? string.Empty;
            BarWorldBound = barWorldBound;
        }

        /// <summary>"" = rời thanh.</summary>
        public string BarKey { get; }

        public Rect BarWorldBound { get; }
        public bool IsLeave => BarKey.Length == 0;
    }

    /// <summary>(V-10) Chuột phải / phím Menu — element không dựng menu; presenter W5 dựng từ <c>CalendarContextMenus</c>.</summary>
    internal sealed class LiveOpsTimelineContextRequest
    {
        public LiveOpsTimelineContextRequest(LiveOpsTimelineHit hit, Vector2 worldPosition)
        {
            Hit = hit ?? LiveOpsTimelineHit.None;
            WorldPosition = worldPosition;
        }

        public LiveOpsTimelineHit Hit { get; }
        public Vector2 WorldPosition { get; }
    }
}
