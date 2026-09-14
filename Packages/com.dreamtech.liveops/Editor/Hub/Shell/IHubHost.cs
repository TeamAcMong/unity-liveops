using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Host tối thiểu mà màn được thấy. Màn KHÔNG gọi thẳng cửa sổ để hiện toast, điều hướng có tham số hay làm mới health:
    /// chúng phát yêu cầu lên <see cref="LiveOpsHubSectionBus"/>, cửa sổ lắng nghe — nhờ vậy màn (W4) và phần UI của khung
    /// (G-HOSTUI) làm song song, và test màn chỉ cần assert sự kiện trên bus. G-SESSION thêm đúng một property
    /// <c>Services</c>; chữ ký này đóng băng khi cổng W3 xanh (PD-35).
    /// </summary>
    internal interface IHubHost
    {
        /// <summary>Thứ tự registry = thứ tự rail = ⌘1…6.</summary>
        IReadOnlyList<IHubSection> Sections { get; }

        /// <summary>Id sai → cảnh báo nêu id rồi hiện Tổng quan (không ném: id có thể đến từ SessionState của bản cũ).</summary>
        void Navigate(string sectionId);

        /// <summary>Đọc từ <see cref="LiveOpsHubWindowState"/>; "" khi màn chưa lưu gì.</summary>
        string GetSectionViewState(string sectionId);

        /// <summary>Màn gọi khi rời màn; cửa sổ cũng tự lưu mọi <see cref="IHubSectionViewState"/> ở OnDisable (trước domain reload).</summary>
        void SetSectionViewState(string sectionId, string viewStateJson);
    }
}
