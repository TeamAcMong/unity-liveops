using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Hợp đồng một màn của hub. Cửa sổ chỉ biết màn qua giao diện này — thêm màn (P2/P3) là thêm một mục vào
    /// <see cref="LiveOpsHubSections"/>, không sửa khung. Mọi thành viên phải rẻ và không ném: rail gọi
    /// <see cref="GetHealth"/> mỗi giây (qua <see cref="LiveOpsHealthThrottle"/>), còn <see cref="CreateView"/> ném thì cửa sổ
    /// bắt và hiện <see cref="LiveOpsHubFailureView"/> thay cho màn — rail vẫn dùng được.
    /// </summary>
    internal interface IHubSection
    {
        /// <summary>kebab-case, không đổi sau phát hành: id nằm trong SessionState, trạng thái view đã lưu và điều hướng.</summary>
        string Id { get; }

        string Title { get; }

        /// <summary>Bắt buộc, dưới 60 ký tự ([FD §3.6]) — section header luôn có một dòng nói màn này để làm gì.</summary>
        string Subtitle { get; }

        PipelineStage Stage { get; }

        /// <summary>Rẻ, không ném, chỉ đọc trạng thái sẵn có (không biên dịch, không kiểm, không đọc đĩa — 8.4).</summary>
        SectionHealth GetHealth();

        VisualElement CreateView();

        void OnShown();

        /// <summary>
        /// Tên element viết tay trong UXML của màn. Probe CLI và <c>HubWindowTests</c> Q&lt;&gt; từng tên sau khi dựng — đổi tên
        /// trong UXML mà quên C# thì lỗi lộ ngay ở cổng, không im lặng thành màn trống.
        /// </summary>
        IReadOnlyList<string> RequiredElementNames { get; }
    }

    /// <summary>
    /// Trạng thái view sống qua domain reload (lựa chọn, zoom, khoảng, pane mở, nháp tại ô). Cửa sổ giữ chuỗi JSON trong
    /// <see cref="LiveOpsHubWindowState"/> (<c>[SerializeField]</c>) vì <c>viewDataKey</c> chỉ lưu được control dựng sẵn.
    /// </summary>
    internal interface IHubSectionViewState
    {
        /// <summary>JSON nhỏ (JsonUtility trên một lớp [Serializable] của màn).</summary>
        string CaptureViewState();

        /// <summary>Chuỗi rỗng hoặc hỏng → trạng thái mặc định, không ném: JSON cũ của bản trước không được làm sập màn.</summary>
        void RestoreViewState(string viewStateJson);
    }

    /// <summary>Nhãn ngắn hơn <see cref="IHubSection.Title"/> cho hàng rail khi tiêu đề quá dài cho cột 196px.</summary>
    internal interface IHubRailLabel
    {
        string RailLabel { get; }
    }

    /// <summary>Màn tự thêm nút vào section header; container rỗng thì cửa sổ ẩn nó (không để khoảng trống lệch tiêu đề).</summary>
    internal interface IHubSectionActions
    {
        void PopulateHeaderActions(VisualElement container);
    }

    /// <summary>Cửa sổ gọi <see cref="Bind"/> cho mọi màn TRƯỚC khi hiện màn nào (8.1 bước 5).</summary>
    internal interface IHubHostAware
    {
        void Bind(IHubHost host);
    }
}
