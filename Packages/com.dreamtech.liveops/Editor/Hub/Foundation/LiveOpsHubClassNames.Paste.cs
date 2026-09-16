namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class USS của vùng Paste (G-PASTE, W5) — popover "Dán / Nhập JSON đang chạy" ([SD2 §2.9], V-14). Popover là cửa sổ
    /// panel riêng nên bố cục của nó không dùng chung class nào của màn; mọi class ở đây có selector trong
    /// <c>Dialogs/PasteRunningJsonPopover.uss</c> (check-class-names --strict).
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        /// <summary>Thân popover: rộng 320px như mọi popover của hub ([FD §7]), xếp dọc, padding 10.</summary>
        internal const string Paste = "liveops-hub-paste";

        /// <summary>Tiêu đề động từ + đối tượng ở đầu popover.</summary>
        internal const string PasteHeader = "liveops-hub-paste-header";

        /// <summary>Ô dán nhiều dòng, chữ mono — người dùng phải thấy đúng chuỗi mình dán, kể cả khoảng trắng.</summary>
        internal const string PasteInput = "liveops-hub-paste-input";

        /// <summary>Dòng trạng thái ngay dưới ô: "Đọc được: …" hoặc câu lỗi cú pháp có dòng/ký tự.</summary>
        internal const string PasteStatus = "liveops-hub-paste-status";

        /// <summary>Dòng "Key remote: liveops_calendar (đổi được sau ở Tổng quan)" của chế độ nhập (V-14).</summary>
        internal const string PasteRemoteKey = "liveops-hub-paste-remote-key";

        /// <summary>
        /// Ẩn khối không thuộc chế độ đang mở (dòng key remote + Toggle ghi dấu ở chế độ dán; nhóm lựa chọn ở chế độ nhập).
        /// Ẩn bằng CLASS chứ không <c>style.display</c>: trạng thái không được gán style inline ([FD §2.14]).
        /// </summary>
        internal const string PasteHidden = "liveops-hub-paste-hidden";

        /// <summary>Hàng nút dính đáy popover — Huỷ bên trái nút chính, cùng khuôn với các popover khác.</summary>
        internal const string PasteFooter = "liveops-hub-paste-footer";
    }
}
