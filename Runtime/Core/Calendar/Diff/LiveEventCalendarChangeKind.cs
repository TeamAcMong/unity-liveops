namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một mục khác gì giữa bản so và nháp. <see cref="Kept"/> chỉ được ĐẾM (chip "Giữ 3"), không bao giờ nằm trong
    /// <see cref="LiveEventCalendarDiffResult.Changes"/> — liệt kê mục không đổi chỉ làm loãng danh sách cần xem.
    /// </summary>
    public enum LiveEventCalendarChangeKind
    {
        Added = 0,
        Changed = 1,
        Removed = 2,
        Kept = 3,
    }
}
