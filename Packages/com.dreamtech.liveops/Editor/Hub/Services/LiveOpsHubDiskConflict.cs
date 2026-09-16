using System;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Asset lịch đổi trên đĩa (git pull, sửa tay YAML) trong lúc phiên còn thay đổi chưa lưu (4.3, R-5). Unity có thể đã đè
    /// instance bằng bản đĩa; phiên giữ bản chụp nháp nên không mất việc, và mang đủ ba tài liệu để băng Hình 28 khung 4 (G-SHELLPOLISH)
    /// hỏi đúng câu: "Tải lại" mất gì, "Xem khác biệt" so gì, "Giữ bản trong Editor" ghi lại gì. Bất biến.
    /// </summary>
    internal sealed class LiveOpsHubDiskConflict
    {
        internal LiveOpsHubDiskConflict(DateTime detectedUtc, LiveEventCalendarDocument diskDocument, LiveEventCalendarDocument editorDocument,
            LiveEventCalendarDocument savedDocument)
        {
            DetectedUtc = DateTime.SpecifyKind(detectedUtc, DateTimeKind.Utc);
            DiskDocument = diskDocument ?? throw new ArgumentNullException(nameof(diskDocument));
            EditorDocument = editorDocument ?? throw new ArgumentNullException(nameof(editorDocument));
            SavedDocument = savedDocument ?? LiveEventCalendarDocument.Empty;
            // Cả hai phía là tài liệu đầy đủ của CÙNG một asset → so theo EntryKey (đổi id là một thay đổi, có so loại), như "chưa lưu".
            DiskVersusEditor = LiveEventCalendarDiff.CompareByEntryKey(DiskDocument, EditorDocument, DetectedUtc);
            LostIfReload = LiveEventCalendarDiff.CompareByEntryKey(SavedDocument, EditorDocument, DetectedUtc);
        }

        public DateTime DetectedUtc { get; }

        /// <summary>Bản trên đĩa → bản trong Editor: pane "So với" nguồn Disk (V-13) đọc danh sách này.</summary>
        public LiveEventCalendarDiffResult DiskVersusEditor { get; }

        /// <summary>Thay đổi chưa lưu sẽ mất nếu bấm "Tải lại" (bản đã lưu lần trước → nháp).</summary>
        public LiveEventCalendarDiffResult LostIfReload { get; }

        /// <summary>Tài liệu đọc thẳng từ file lúc phát hiện.</summary>
        internal LiveEventCalendarDocument DiskDocument { get; }

        /// <summary>Bản chụp nháp của phiên ngay trước khi file đổi — thứ "Giữ bản trong Editor" ghi lại vào asset.</summary>
        internal LiveEventCalendarDocument EditorDocument { get; }

        internal LiveEventCalendarDocument SavedDocument { get; }
    }
}
