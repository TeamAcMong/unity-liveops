using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Kết quả so một bản so với nháp: danh sách thay đổi đã sắp (hậu quả → loại mục → thời gian) và số đếm cho chip
    /// "Thêm 1 · Đổi 4 · Xoá 0 · Giữ 3". Mục giữ nguyên chỉ có trong <see cref="KeptCount"/>.
    /// </summary>
    public sealed class LiveEventCalendarDiffResult
    {
        public LiveEventCalendarDiffResult(IReadOnlyList<LiveEventCalendarChange> changes, int keptCount)
        {
            if (keptCount < 0) throw new ArgumentOutOfRangeException(nameof(keptCount), "Số mục giữ nguyên không được âm.");

            var copiedChanges = new List<LiveEventCalendarChange>(changes != null ? changes.Count : 0);
            if (changes != null)
            {
                for (int index = 0; index < changes.Count; index++)
                {
                    LiveEventCalendarChange change = changes[index];
                    if (change == null) throw new ArgumentException("Danh sách thay đổi có phần tử null.", nameof(changes));

                    copiedChanges.Add(change);
                    switch (change.Kind)
                    {
                        case LiveEventCalendarChangeKind.Added: AddedCount++; break;
                        case LiveEventCalendarChangeKind.Changed: ChangedCount++; break;
                        case LiveEventCalendarChangeKind.Removed: RemovedCount++; break;
                    }
                    if (change.IsReviewRequired) ReviewRequiredCount++;
                }
            }

            Changes = copiedChanges;
            KeptCount = keptCount;
        }

        /// <summary>Không gồm mục giữ nguyên; sắp: hậu quả nặng trước → loại mục → thời gian.</summary>
        public IReadOnlyList<LiveEventCalendarChange> Changes { get; }

        public int AddedCount { get; }
        public int ChangedCount { get; }
        public int RemovedCount { get; }
        public int KeptCount { get; }

        /// <summary>Thêm + Đổi + Xoá — "5 thay đổi".</summary>
        public int ChangeCount => AddedCount + ChangedCount + RemovedCount;

        public int ReviewRequiredCount { get; }

        public bool IsEmpty => ChangeCount == 0;
    }
}
