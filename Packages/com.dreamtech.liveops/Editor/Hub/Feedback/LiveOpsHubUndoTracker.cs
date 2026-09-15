using System;
using System.Collections.Generic;
using UnityEditor;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bọc stack Undo chung của Editor cho toast và status bar (8.5, [FD §3.9]): mỗi thao tác sửa lịch là một group mang đúng câu
    /// toast, một lần kéo gộp thành một bước, và nút Hoàn tác chỉ bật khi group của toast còn là thao tác mới nhất.
    /// <para>
    /// "Còn trên đỉnh" không chỉ là <c>Undo.GetCurrentGroup() == group</c>: Unity tự tăng số group khi có mouse down / phím và cả sau
    /// mỗi lần Undo/Redo (đo ở 6000.6: PerformUndo đẩy số group lên 1) mà không ghi gì vào stack. So bằng đúng số group thì nút Hoàn
    /// tác tắt ngay khi người dùng đưa chuột bấm nó, và Làm lại không bao giờ bật. Vì vậy tracker nhớ group MỚI NHẤT có bản ghi thật,
    /// đọc ở <c>Undo.willFlushUndoRecord</c> (bắn đúng một lần mỗi bản ghi, lúc số group hiện tại là group của bản ghi; không bắn khi
    /// chỉ tăng số group). <c>GetCurrentGroupName</c> không dùng được cho việc này: nó trả tên group có bản ghi gần nhất, kể cả khi
    /// group hiện tại rỗng.
    /// </para>
    /// <para>
    /// Giới hạn: thao tác của code khác ghi Undo mà không đi qua flush (nếu có) thì tracker không thấy — nút vẫn bật và Undo gỡ thao tác
    /// đó. Mọi thao tác sửa lịch của hub đi qua <c>Undo.RecordObject</c> nên luôn được thấy; phiên lịch gọi
    /// <c>Undo.FlushUndoRecordObjects</c> ngay sau khi sửa ([API §2]) để bản ghi được đẩy khi group của thao tác còn là group hiện tại
    /// — flush dồn tới cuối frame sau một mouse down khác sẽ ghi nhận bản ghi ở group mới hơn.
    /// </para>
    /// </summary>
    internal sealed class LiveOpsHubUndoTracker : IDisposable
    {
        // Group đã gộp (CollapseGroup) → số group cao nhất đã nằm trong nó: mọi bản ghi tới số đó là chính thao tác kéo.
        private readonly Dictionary<int, int> _collapsedThrough = new Dictionary<int, int>();
        private int _newestRecordedGroup = -1;
        private bool _isDisposed;

        public LiveOpsHubUndoTracker()
        {
            // Field delegate (không phải event): gỡ trước rồi gắn để tracker tạo lại sau domain reload không gắn hai lần.
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            Undo.undoRedoEvent += OnUndoRedoEvent;
            Undo.willFlushUndoRecord -= OnWillFlushUndoRecord;
            Undo.willFlushUndoRecord += OnWillFlushUndoRecord;
        }

        /// <summary>
        /// Undo/Redo vừa chạy (nút toast, ⌘Z của Unity, Edit → Undo History). Bọc <c>Undo.undoRedoEvent</c> thay cho
        /// <c>undoRedoPerformed</c> vì sự kiện đó mang tên group và chiều (undo/redo) — toast cần biết đúng thao tác của mình vừa bị
        /// gỡ hay là thao tác khác (<see cref="LastUndoRedoGroupName"/>, <see cref="LastUndoRedoWasRedo"/>).
        /// </summary>
        public event Action UndoRedoPerformed;

        /// <summary>Tên group của lần Undo/Redo gần nhất ("" khi chưa có).</summary>
        public string LastUndoRedoGroupName { get; private set; } = string.Empty;

        public bool LastUndoRedoWasRedo { get; private set; }

        /// <summary>Group của lần Undo/Redo gần nhất (<see cref="LiveOpsToastModel.NoUndoGroup"/> khi chưa có).</summary>
        public int LastUndoRedoGroup { get; private set; } = LiveOpsToastModel.NoUndoGroup;

        /// <summary>Tên group gần nhất tracker mở — status bar "Vừa làm: …" (G-HOSTUI); "" khi chưa có thao tác nào.</summary>
        public string LastActionText { get; private set; } = string.Empty;

        /// <summary>Group của <see cref="LastActionText"/>; <see cref="LiveOpsToastModel.NoUndoGroup"/> khi chưa có.</summary>
        public int LastActionGroup { get; private set; } = LiveOpsToastModel.NoUndoGroup;

        /// <summary>Mở group mới mang tên = câu toast (Edit → Undo History và toast nói cùng một câu). Trả số group để toast giữ.</summary>
        public int BeginGroup(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException(LiveOpsHubStrings.KitErrorToastMessageEmpty, nameof(name));
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(name);
            int group = Undo.GetCurrentGroup();
            LastActionText = name;
            LastActionGroup = group;
            return group;
        }

        /// <summary>
        /// Gộp mọi group sau <paramref name="group"/> vào nó — một lần kéo (nhiều khung, mỗi khung có thể một group) thành một bước
        /// Undo. Gọi lúc thả chuột. Bước gộp mang tên của group có bản ghi đầu tiên (đo ở 6000.6), nên presenter ghi bản ghi đầu tiên
        /// NGAY trong group vừa mở; đặt tên sau khi gộp không có tác dụng (đổi tên group hiện tại, không phải bước đã gộp).
        /// </summary>
        public void CollapseGroup(int group)
        {
            int newest = Math.Max(Undo.GetCurrentGroup(), _newestRecordedGroup);
            Undo.CollapseUndoOperations(group);
            _collapsedThrough[group] = newest;
        }

        /// <summary>
        /// true khi lần Undo/Redo vừa chạy (<see cref="LastUndoRedoGroup"/>) là chính bước của <paramref name="group"/> — kể cả khi bước gộp
        /// mang số của group có bản ghi đầu tiên sau <paramref name="group"/> (group mở lúc nhấn chuột chưa kịp có bản ghi).
        /// </summary>
        public bool IsLastUndoRedoOf(int group)
        {
            if (group < 0 || LastUndoRedoGroup < 0) return false;
            if (LastUndoRedoGroup == group) return true;
            int lastIncluded;
            return _collapsedThrough.TryGetValue(group, out lastIncluded) && LastUndoRedoGroup > group && LastUndoRedoGroup <= lastIncluded;
        }

        /// <summary>
        /// true khi chưa có thao tác nào ghi vào stack sau <paramref name="group"/> — bấm Hoàn tác sẽ gỡ đúng thao tác của toast
        /// (hoặc Làm lại đúng thao tác vừa gỡ). Group tự tăng không có bản ghi (mouse down, sau Undo/Redo) không làm mất trạng thái này.
        /// </summary>
        public bool IsGroupOnTop(int group)
        {
            if (group < 0) return false;
            if (Undo.GetCurrentGroup() == group) return true;
            int lastIncluded;
            if (!_collapsedThrough.TryGetValue(group, out lastIncluded)) lastIncluded = group;
            return _newestRecordedGroup <= lastIncluded;
        }

        /// <summary>Nút Hoàn tác của toast. Không kiểm group ở đây — toast kiểm <see cref="IsGroupOnTop"/> trước khi gọi.</summary>
        internal void PerformUndo()
        {
            Undo.PerformUndo();
        }

        internal void PerformRedo()
        {
            Undo.PerformRedo();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            Undo.willFlushUndoRecord -= OnWillFlushUndoRecord;
            UndoRedoPerformed = null;
        }

        private void OnWillFlushUndoRecord()
        {
            // Bắn lúc số group hiện tại là group của bản ghi đang đẩy vào stack — đúng thứ cần để biết "có thao tác sau group này".
            _newestRecordedGroup = Math.Max(_newestRecordedGroup, Undo.GetCurrentGroup());
        }

        private void OnUndoRedoEvent(in UndoRedoInfo info)
        {
            LastUndoRedoGroupName = info.undoName ?? string.Empty;
            LastUndoRedoWasRedo = info.isRedo;
            LastUndoRedoGroup = info.undoGroup;
            UndoRedoPerformed?.Invoke();
        }
    }
}
