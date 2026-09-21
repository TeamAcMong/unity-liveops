using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Đầu vào của <see cref="LiveOpsTimelineModel.Build"/> — builder <c>With…</c> theo khuôn composition root của package. Gom đúng
    /// những gì presenter đã có trong phiên (tài liệu nháp, bản biên dịch, báo cáo kiểm có thể cũ, diff với bản đã đăng, giờ, khoảng,
    /// bề rộng track, làn ẩn) để model không tự đi tìm dữ liệu và test dựng được bằng dữ liệu mẫu. Mọi thứ trừ tài liệu, khoảng và
    /// bề rộng track đều tuỳ chọn: thiếu báo cáo thì không tô lỗi, thiếu diff thì không có vuông "khác bản đã đăng".
    /// </summary>
    internal sealed class LiveOpsTimelineInput
    {
        private readonly HashSet<string> _hiddenLanes = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _collapsedLanes = new HashSet<string>(StringComparer.Ordinal);

        public LiveEventCalendarDocument Document { get; private set; }

        /// <summary><c>null</c> = model tự gọi <see cref="LiveEventCalendarCompiler.CompileInExportOrder"/> (V-6) — cùng thứ tự game đọc JSON đã xuất.</summary>
        public LiveEventCalendarCompilation Compilation { get; private set; }

        /// <summary>Có thể là báo cáo cũ (trước lần sửa cuối) — timeline vẫn tô theo nó như rail tô "cũ" (6.4).</summary>
        public LiveEventCalendarCheckReport CheckReport { get; private set; }

        /// <summary><c>LiveEventCalendarDiff.Compare(bản đã đăng, nháp, now)</c>; <c>null</c> khi chưa đăng lần nào.</summary>
        public LiveEventCalendarDiffResult PublishedDiff { get; private set; }

        public DateTime NowUtc { get; private set; }
        public DateTime RangeStartUtc { get; private set; }
        public DateTime RangeEndUtc { get; private set; }
        public bool HasRange { get; private set; }
        public float TrackWidth { get; private set; }
        public IReadOnlyCollection<string> HiddenLanes => _hiddenLanes;

        /// <summary>(G-OPT-TIMELINE, Hình 12 khung 12) Làn thu gọn — vẫn vẽ, nhưng cao 22px và thanh rút còn dải 6px không nhãn.</summary>
        public IReadOnlyCollection<string> CollapsedLanes => _collapsedLanes;

        public bool IsLaneHidden(string typeId) => typeId != null && _hiddenLanes.Contains(typeId);

        public bool IsLaneCollapsed(string typeId) => typeId != null && _collapsedLanes.Contains(typeId);

        public LiveOpsTimelineInput WithDocument(LiveEventCalendarDocument document)
        {
            Document = document;
            return this;
        }

        public LiveOpsTimelineInput WithCompilation(LiveEventCalendarCompilation compilation)
        {
            Compilation = compilation;
            return this;
        }

        public LiveOpsTimelineInput WithCheckReport(LiveEventCalendarCheckReport checkReport)
        {
            CheckReport = checkReport;
            return this;
        }

        public LiveOpsTimelineInput WithPublishedDiff(LiveEventCalendarDiffResult publishedDiff)
        {
            PublishedDiff = publishedDiff;
            return this;
        }

        public LiveOpsTimelineInput WithNowUtc(DateTime nowUtc)
        {
            NowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            return this;
        }

        /// <summary>Khoảng tự do — zoom liên tục (⌘ + bánh xe) đưa vào khoảng = bề rộng track ÷ px/giờ.</summary>
        public LiveOpsTimelineInput WithRange(DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            RangeStartUtc = DateTime.SpecifyKind(rangeStartUtc, DateTimeKind.Utc);
            RangeEndUtc = DateTime.SpecifyKind(rangeEndUtc, DateTimeKind.Utc);
            HasRange = true;
            return this;
        }

        /// <summary>Khoảng của preset zoom bắt đầu tại <paramref name="rangeStartUtc"/> (24 giờ · 21 ngày · 42 ngày).</summary>
        public LiveOpsTimelineInput WithRange(DateTime rangeStartUtc, LiveOpsTimelineZoom zoom)
        {
            DateTime start = DateTime.SpecifyKind(rangeStartUtc, DateTimeKind.Utc);
            return WithRange(start, LiveOpsTimelineGeometry.AddTicksClamped(start, LiveOpsTimelineGeometry.RangeLengthOf(zoom).Ticks));
        }

        public LiveOpsTimelineInput WithTrackWidth(float trackWidth)
        {
            TrackWidth = trackWidth;
            return this;
        }

        /// <summary>(V-10) Làn ẩn do presenter giữ trong view state; thay toàn bộ danh sách cũ.</summary>
        public LiveOpsTimelineInput WithHiddenLanes(IEnumerable<string> typeIds)
        {
            _hiddenLanes.Clear();
            if (typeIds == null) return this;
            foreach (string typeId in typeIds)
            {
                if (typeId != null) _hiddenLanes.Add(typeId);
            }
            return this;
        }

        /// <summary>
        /// (Hình 12 khung 12) Làn thu gọn do presenter giữ trong view state, y như làn ẩn; thay toàn bộ danh sách cũ. Thu gọn KHÁC
        /// ẩn: làn thu gọn vẫn có mặt trên trục nên người dùng thấy loại đó còn sống, chỉ là không chiếm 28px mỗi hàng phụ.
        /// </summary>
        public LiveOpsTimelineInput WithCollapsedLanes(IEnumerable<string> typeIds)
        {
            _collapsedLanes.Clear();
            if (typeIds == null) return this;
            foreach (string typeId in typeIds)
            {
                if (typeId != null) _collapsedLanes.Add(typeId);
            }
            return this;
        }

        public LiveOpsTimelineModel Build()
        {
            return LiveOpsTimelineModel.Build(this);
        }
    }
}
