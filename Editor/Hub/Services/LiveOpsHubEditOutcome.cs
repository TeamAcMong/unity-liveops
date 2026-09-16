namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Kết quả một lần sửa qua phiên (<see cref="LiveOpsHubCalendarSession.Apply"/>, kéo, ghi dấu). Màn đọc <see cref="UndoGroup"/>
    /// + <see cref="UndoName"/> để dựng toast có Hoàn tác (tên group = câu toast, 4.3), và <see cref="FailureText"/> để nói vì sao không
    /// áp — lệnh sửa trượt không được im lặng. Bất biến.
    /// </summary>
    internal sealed class LiveOpsHubEditOutcome
    {
        /// <summary>Không có Undo group (thất bại, hoặc thao tác không để lại bước Undo).</summary>
        internal const int NoUndoGroup = -1;

        private LiveOpsHubEditOutcome(bool applied, int undoGroup, string undoName, string failureText)
        {
            Applied = applied;
            UndoGroup = undoGroup;
            UndoName = undoName ?? string.Empty;
            FailureText = failureText ?? string.Empty;
        }

        public bool Applied { get; }

        /// <summary><see cref="NoUndoGroup"/> khi không áp.</summary>
        public int UndoGroup { get; }

        /// <summary>Tên Undo group đã đặt — cũng là câu toast; "" khi không áp.</summary>
        public string UndoName { get; }

        /// <summary>"" khi áp được; câu tiếng Việt nói vì sao khi không.</summary>
        public string FailureText { get; }

        internal static LiveOpsHubEditOutcome Success(int undoGroup, string undoName)
        {
            return new LiveOpsHubEditOutcome(true, undoGroup, undoName, string.Empty);
        }

        internal static LiveOpsHubEditOutcome Failure(string failureText)
        {
            return new LiveOpsHubEditOutcome(false, NoUndoGroup, string.Empty, failureText);
        }
    }
}
