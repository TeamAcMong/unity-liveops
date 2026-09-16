using System;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Dữ liệu một toast (8.5) — bất biến, để section chỉ phát model lên <see cref="LiveOpsHubSectionBus"/> còn view toast
    /// (G-FEEDBACK) tự quyết cách vẽ, và test section assert được sự kiện trên bus mà không cần panel.
    /// Tên Undo group trùng câu toast (<see cref="UndoGroupName"/> = <see cref="Message"/>) để Edit → Undo History và toast
    /// nói cùng một câu; nhờ vậy người dùng tìm lại được bước đó khi toast đã tắt.
    /// </summary>
    internal sealed class LiveOpsToastModel
    {
        /// <summary>Giá trị <see cref="UndoGroup"/> của toast không có nút Hoàn tác.</summary>
        public const int NoUndoGroup = -1;

        private LiveOpsToastModel(string message, string tooltip, string undoneStepName, int undoGroup, bool isUndone)
        {
            Message = message;
            Tooltip = tooltip;
            UndoneStepName = undoneStepName;
            UndoGroup = undoGroup;
            IsUndone = isUndone;
        }

        public string Message { get; }

        /// <summary>Câu đủ cho tooltip — mặc định là chính câu toast vì toast cắt ellipsis ở cuối khi hẹp.</summary>
        public string Tooltip { get; }

        /// <summary>
        /// Tên NGẮN của bước, dùng sau khi hoàn tác: "Đã hoàn tác: Dời hunt-0916-bonus" [SD1 §3.8 khung 14] — ghép tiền tố vào
        /// nguyên câu toast thì ra hai lần "Đã" ("Đã hoàn tác: Đã dời hunt-0916-bonus …"). Để trống = dùng luôn câu toast
        /// (phiếu D-5 của cổng W4).
        /// </summary>
        public string UndoneStepName { get; }

        public int UndoGroup { get; }

        public string UndoGroupName => UndoGroup == NoUndoGroup ? string.Empty : Message;

        public bool IsUndone { get; }

        public bool HasUndo => UndoGroup != NoUndoGroup;

        /// <summary>
        /// Câu hiện trên toast: sau Hoàn tác thêm tiền tố "Đã hoàn tác: " trước TÊN BƯỚC (<see cref="UndoneStepName"/>, mặc định là
        /// chính câu toast), nút đổi thành Làm lại.
        /// </summary>
        public string DisplayMessage => IsUndone ? LiveOpsHubStrings.KitToastUndonePrefix + StepNameOrMessage : Message;

        private string StepNameOrMessage => string.IsNullOrEmpty(UndoneStepName) ? Message : UndoneStepName;

        /// <summary>
        /// Tooltip hiện trên toast: sau Hoàn tác cũng thêm tiền tố "Đã hoàn tác: " trước <see cref="Tooltip"/>. Tooltip riêng
        /// (vd khung giờ "14/9 00:00 → 15/9 00:00") không chứa câu toast, nên thiếu tiền tố thì người dùng rê chuột đọc tooltip
        /// sẽ tưởng thao tác vẫn còn hiệu lực; model ghép một chỗ để view không tự ghép (CC-FEEDBACK-2, V-22).
        /// </summary>
        public string DisplayTooltip => IsUndone ? LiveOpsHubStrings.KitToastUndonePrefix + Tooltip : Tooltip;

        public static LiveOpsToastModel ForEdit(string message, int undoGroup, string tooltip = "", string undoneStepName = "")
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentException(LiveOpsHubStrings.KitErrorToastMessageEmpty, nameof(message));
            if (undoGroup < 0) throw new ArgumentException(LiveOpsHubStrings.KitErrorToastUndoGroupNegative, nameof(undoGroup));
            return new LiveOpsToastModel(message, TooltipOrMessage(tooltip, message), undoneStepName ?? string.Empty, undoGroup, false);
        }

        public static LiveOpsToastModel Info(string message)
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentException(LiveOpsHubStrings.KitErrorToastMessageEmpty, nameof(message));
            return new LiveOpsToastModel(message, message, string.Empty, NoUndoGroup, false);
        }

        /// <summary>Bản sau khi bấm Hoàn tác (hoặc ⌘Z khớp group): giữ group để Làm lại gỡ đúng bước đó.</summary>
        public LiveOpsToastModel AsUndone()
        {
            // Toast Info không có bước Undo nào để gỡ — gọi AsUndone trên nó là lỗi của view, không phải trạng thái hợp lệ.
            if (!HasUndo) throw new InvalidOperationException(LiveOpsHubStrings.KitErrorToastWithoutUndo);
            return new LiveOpsToastModel(Message, Tooltip, UndoneStepName, UndoGroup, true);
        }

        public LiveOpsToastModel AsRedone()
        {
            if (!HasUndo) throw new InvalidOperationException(LiveOpsHubStrings.KitErrorToastWithoutUndo);
            return new LiveOpsToastModel(Message, Tooltip, UndoneStepName, UndoGroup, false);
        }

        private static string TooltipOrMessage(string tooltip, string message)
        {
            return string.IsNullOrEmpty(tooltip) ? message : tooltip;
        }
    }
}
