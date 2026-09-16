using System;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Trạng thái view của màn Xuất JSON sống qua domain reload (7.0): tab "Đã định dạng | Một dòng", dấu đã đăng đang chọn
    /// trong card lịch sử và dấu vừa khôi phục (trạng thái (i)).
    /// <para>
    /// Định dạng JSON và nguồn bản so KHÔNG ở đây: chúng nằm trong SessionState của phiên
    /// (<c>LiveOpsHubPublishState.SelectedFormat</c> / <c>ActiveCompareSource</c>) vì màn khác cũng đọc — Tổng quan đọc cổng
    /// xuất, Lịch đọc pane "So với". Hai chỗ nhớ cùng một thứ là hai chỗ lệch nhau.
    /// </para>
    /// </summary>
    [Serializable]
    internal sealed class ExportSessionState
    {
        [SerializeField] private bool jsonSingleLine;
        [SerializeField] private string selectedStampKey;
        [SerializeField] private string restoredStampKey;
        [SerializeField] private int restoreUndoGroup;

        public ExportSessionState()
        {
            selectedStampKey = string.Empty;
            restoredStampKey = string.Empty;
            restoreUndoGroup = LiveOpsHubEditOutcome.NoUndoGroup;
        }

        /// <summary>Tab "Một dòng" của JSON viewer (PD-12).</summary>
        public bool JsonSingleLine
        {
            get { return jsonSingleLine; }
            set { jsonSingleLine = value; }
        }

        /// <summary>Hàng lịch sử đang chọn (khoá = giờ đăng + sha); "" = chưa chọn hàng nào.</summary>
        public string SelectedStampKey
        {
            get { return selectedStampKey ?? string.Empty; }
            set { selectedStampKey = value ?? string.Empty; }
        }

        /// <summary>Dấu vừa khôi phục vào nháp — nguồn của note (i); "" = không ở trạng thái khôi phục.</summary>
        public string RestoredStampKey
        {
            get { return restoredStampKey ?? string.Empty; }
            set { restoredStampKey = value ?? string.Empty; }
        }

        /// <summary>Undo group của lần khôi phục, để "Hoàn tác khôi phục" gọi đúng bước; <c>NoUndoGroup</c> = không có.</summary>
        public int RestoreUndoGroup
        {
            get { return restoreUndoGroup; }
            set { restoreUndoGroup = value; }
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>Chuỗi rỗng hoặc JSON của bản trước không đọc được → trạng thái mặc định, không ném (hợp đồng IHubSectionViewState).</summary>
        public static ExportSessionState FromJson(string viewStateJson)
        {
            if (string.IsNullOrEmpty(viewStateJson)) return new ExportSessionState();
            try
            {
                ExportSessionState state = JsonUtility.FromJson<ExportSessionState>(viewStateJson);
                return state ?? new ExportSessionState();
            }
            catch (ArgumentException)
            {
                return new ExportSessionState();
            }
        }

        /// <summary>Khoá của một dấu — giờ đăng + sha đủ để phân biệt, và đọc được trong file state.</summary>
        public static string KeyOf(PublishedCalendarStamp stamp)
        {
            return stamp == null ? string.Empty : stamp.PublishedUtcText + "|" + stamp.Sha256Hex;
        }
    }
}
