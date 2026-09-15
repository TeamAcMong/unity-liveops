using System;
using System.Collections.Generic;
using UnityEditor;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bọc stack Undo chung của Editor cho toast và status bar (8.5, [FD §3.9]): mỗi thao tác sửa lịch là một group mang đúng câu
    /// toast, một lần kéo gộp thành một bước, và nút Hoàn tác/Làm lại chỉ bật khi bước của toast đúng là bước Unity sẽ gỡ/làm lại kế tiếp.
    /// <para>
    /// "Kế tiếp" không đọc được từ Unity: <c>Undo.GetCurrentGroup()</c> tự tăng khi có mouse down / phím và sau mỗi lần Undo/Redo (đo ở
    /// 6000.6: PerformUndo đẩy số group lên 1) mà không ghi gì vào stack, còn <c>GetCurrentGroupName</c> trả tên group có bản ghi gần
    /// nhất kể cả khi group hiện tại rỗng. Vì vậy tracker tự dựng lại hai ngăn từ sự kiện thật: ngăn đã làm (mỗi bản ghi mới đọc ở
    /// <c>Undo.willFlushUndoRecord</c> — bắn đúng một lần mỗi bản ghi, lúc số group hiện tại là group của bản ghi; bản ghi mới xoá ngăn
    /// làm lại như Unity) và ngăn làm lại; <c>Undo.undoRedoEvent</c> chuyển bước giữa hai ngăn theo số group và chiều. Chỉ nhìn "có bản
    /// ghi mới hơn không" là chưa đủ: ⌘Z lần hai gỡ bước CŨ hơn toast mà không thêm bản ghi nào, và nút Làm lại khi đó sẽ làm lại bước
    /// kia trong khi toast nói câu của mình.
    /// </para>
    /// <para>
    /// Giới hạn: thao tác của code khác ghi Undo mà không đi qua flush (nếu có), hay <c>Undo.ClearUndo</c> xoá bản ghi, thì tracker
    /// không thấy. Sự kiện Undo/Redo của bước tracker không biết vẫn được đặt vào ngăn tương ứng nên nút của toast tắt (an toàn); chỉ
    /// bước bị xoá khỏi stack mà không có sự kiện mới làm nút bật sai. Mọi thao tác sửa lịch của hub đi qua <c>Undo.RecordObject</c>
    /// nên luôn được thấy; phiên lịch gọi <c>Undo.FlushUndoRecordObjects</c> ngay sau khi sửa ([API §2]) để bản ghi được đẩy khi group
    /// của thao tác còn là group hiện tại — flush dồn tới cuối frame sau một mouse down khác sẽ ghi nhận bản ghi ở group mới hơn.
    /// </para>
    /// <para>
    /// <see cref="Dispose"/> là bắt buộc: <c>Undo.undoRedoEvent</c> và <c>Undo.willFlushUndoRecord</c> là field delegate tĩnh, tracker
    /// không gỡ thì Unity giữ nó (và mọi toast nghe nó) sống tới domain reload kế tiếp.
    /// </para>
    /// </summary>
    internal sealed class LiveOpsHubUndoTracker : IDisposable
    {
        // Group đã gộp (CollapseGroup) → số group cao nhất đã nằm trong nó: mọi bản ghi tới số đó là chính thao tác kéo.
        private readonly Dictionary<int, int> _collapsedThrough = new Dictionary<int, int>();

        // Hai ngăn dựng lại từ sự kiện, phần tử cuối là đỉnh. Mỗi phần tử là số group mở đầu một bước (bước gộp: group có bản ghi đầu tiên).
        private readonly List<int> _doneSteps = new List<int>();
        private readonly List<int> _redoSteps = new List<int>();

        private int _newestRecordedGroup = -1;
        private bool _isDisposed;

        public LiveOpsHubUndoTracker()
        {
            // Mỗi instance gắn một lần ở đây và gỡ đúng delegate của chính nó trong Dispose; không có cặp -= trước += vì -= bằng method
            // của instance mới không gỡ được delegate của instance cũ, và sau domain reload hai field tĩnh này đã trắng.
            Undo.undoRedoEvent += OnUndoRedoEvent;
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
            // Các bước từ group trở lên trong ngăn đã làm giờ là một bước của Unity: giữ số nhỏ nhất (group có bản ghi đầu tiên).
            int firstStep = -1;
            while (_doneSteps.Count > 0 && Top(_doneSteps) >= group)
            {
                firstStep = Top(_doneSteps);
                _doneSteps.RemoveAt(_doneSteps.Count - 1);
            }
            if (firstStep >= 0) _doneSteps.Add(firstStep);
        }

        /// <summary>
        /// true khi lần Undo/Redo vừa chạy (<see cref="LastUndoRedoGroup"/>) là chính bước của <paramref name="group"/> — kể cả khi bước gộp
        /// mang số của group có bản ghi đầu tiên sau <paramref name="group"/> (group mở lúc nhấn chuột chưa kịp có bản ghi).
        /// </summary>
        public bool IsLastUndoRedoOf(int group)
        {
            if (group < 0 || LastUndoRedoGroup < 0) return false;
            return BelongsTo(LastUndoRedoGroup, group);
        }

        /// <summary>
        /// true khi bước của <paramref name="group"/> đang là bước kế tiếp ở một trong hai chiều: Hoàn tác sẽ gỡ đúng nó (<see cref="IsNextUndo"/>)
        /// hoặc Làm lại sẽ trả đúng nó (<see cref="IsNextRedo"/>). Group tự tăng không có bản ghi (mouse down, sau Undo/Redo) không làm mất
        /// trạng thái này; bản ghi mới hay Undo/Redo một bước khác thì mất.
        /// </summary>
        public bool IsGroupOnTop(int group)
        {
            return IsNextUndo(group) || IsNextRedo(group);
        }

        /// <summary>Hoàn tác (⌘Z) lúc này gỡ đúng bước của <paramref name="group"/>.</summary>
        internal bool IsNextUndo(int group)
        {
            // Group mở mà chưa có bản ghi thì chưa phải bước nào: Hoàn tác lúc đó gỡ bước cũ hơn, nên không coi là của toast.
            return group >= 0 && _doneSteps.Count > 0 && BelongsTo(Top(_doneSteps), group);
        }

        /// <summary>Làm lại (⌘⇧Z) lúc này trả đúng bước của <paramref name="group"/>.</summary>
        internal bool IsNextRedo(int group)
        {
            return group >= 0 && _redoSteps.Count > 0 && BelongsTo(Top(_redoSteps), group);
        }

        /// <summary>Nút Hoàn tác của toast. Không kiểm group ở đây — toast kiểm <see cref="IsNextUndo"/> trước khi gọi.</summary>
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
            int group = Undo.GetCurrentGroup();
            _newestRecordedGroup = Math.Max(_newestRecordedGroup, group);
            // Bản ghi mới làm Unity bỏ ngăn làm lại; nhiều bản ghi cùng một group (hay trong khoảng đã gộp) vẫn là một bước.
            _redoSteps.Clear();
            if (_doneSteps.Count == 0 || !IsSameStep(Top(_doneSteps), group)) _doneSteps.Add(group);
        }

        private void OnUndoRedoEvent(in UndoRedoInfo info)
        {
            LastUndoRedoGroupName = info.undoName ?? string.Empty;
            LastUndoRedoWasRedo = info.isRedo;
            LastUndoRedoGroup = info.undoGroup;
            if (info.isRedo) MoveRedone(info.undoGroup);
            else MoveUndone(info.undoGroup);
            UndoRedoPerformed?.Invoke();
        }

        /// <summary>
        /// Undo của bước <paramref name="group"/>: mọi bước từ đỉnh xuống tới nó sang ngăn làm lại (bước mới hơn còn trong ngăn đã làm nghĩa
        /// là đã bị gỡ theo đường tracker không thấy). Bước tracker không biết vẫn được đặt lên đỉnh ngăn làm lại để nút của toast tắt.
        /// </summary>
        private void MoveUndone(int group)
        {
            bool found = false;
            while (_doneSteps.Count > 0 && (Top(_doneSteps) >= group || IsSameStep(Top(_doneSteps), group)))
            {
                int step = Top(_doneSteps);
                _doneSteps.RemoveAt(_doneSteps.Count - 1);
                _redoSteps.Add(step);
                if (IsSameStep(step, group)) found = true;
            }
            if (!found) _redoSteps.Add(group);
        }

        /// <summary>Redo của bước <paramref name="group"/>: đối xứng với <see cref="MoveUndone"/> (ngăn làm lại, đỉnh là bước cũ nhất đã gỡ).</summary>
        private void MoveRedone(int group)
        {
            bool found = false;
            while (_redoSteps.Count > 0 && (Top(_redoSteps) <= group || IsSameStep(Top(_redoSteps), group)))
            {
                int step = Top(_redoSteps);
                _redoSteps.RemoveAt(_redoSteps.Count - 1);
                _doneSteps.Add(step);
                if (IsSameStep(step, group)) found = true;
            }
            if (!found) _doneSteps.Add(group);
        }

        /// <summary>Bước mang số <paramref name="stepGroup"/> là thao tác của <paramref name="ownerGroup"/> (chính nó, hoặc nằm trong khoảng đã gộp).</summary>
        private bool BelongsTo(int stepGroup, int ownerGroup)
        {
            if (stepGroup == ownerGroup) return true;
            int lastIncluded;
            return _collapsedThrough.TryGetValue(ownerGroup, out lastIncluded) && stepGroup > ownerGroup && stepGroup <= lastIncluded;
        }

        /// <summary>Hai số group thuộc cùng một bước của Unity: bằng nhau, hoặc cùng nằm trong một khoảng đã gộp.</summary>
        private bool IsSameStep(int firstGroup, int secondGroup)
        {
            if (firstGroup == secondGroup) return true;
            foreach (KeyValuePair<int, int> collapsed in _collapsedThrough)
            {
                bool firstInside = firstGroup >= collapsed.Key && firstGroup <= collapsed.Value;
                bool secondInside = secondGroup >= collapsed.Key && secondGroup <= collapsed.Value;
                if (firstInside && secondInside) return true;
            }
            return false;
        }

        private static int Top(List<int> steps)
        {
            return steps[steps.Count - 1];
        }
    }
}
