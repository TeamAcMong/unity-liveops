using System;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Trạng thái view của màn Xuất JSON sống qua domain reload (7.0): tab "Đã định dạng | Một dòng" và dấu vừa khôi phục
    /// (trạng thái (i)).
    /// <para>
    /// Định dạng JSON, nguồn bản so và DẤU ĐANG LÀ BẢN SO không ở đây: chúng nằm trong SessionState của phiên
    /// (<c>LiveOpsHubPublishState.SelectedFormat</c> / <c>ActiveCompareSource</c> / <c>ActiveStamp</c>, ghi qua
    /// <c>SelectActiveStamp</c>) vì màn khác cũng đọc — Tổng quan đọc cổng xuất, Lịch đọc pane "So với". Hai chỗ nhớ cùng một
    /// thứ là hai chỗ lệch nhau; vì vậy màn không giữ thêm một khoá "dấu đang chọn" của riêng nó.
    /// </para>
    /// </summary>
    [Serializable]
    internal sealed class ExportSessionState
    {
        [SerializeField] private bool jsonSingleLine;
        [SerializeField] private string restoredStampKey;
        [SerializeField] private int restoreUndoGroup;

        public ExportSessionState()
        {
            restoredStampKey = string.Empty;
            restoreUndoGroup = LiveOpsHubEditOutcome.NoUndoGroup;
        }

        /// <summary>Tab "Một dòng" của JSON viewer (PD-12).</summary>
        public bool JsonSingleLine
        {
            get { return jsonSingleLine; }
            set { jsonSingleLine = value; }
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
