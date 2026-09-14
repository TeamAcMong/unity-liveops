using System;
using DreamTech.LiveOps.Unity;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    // Port của hub: mọi tác dụng phụ của Editor (clipboard, hộp chọn file, hộp modal, git, múi giờ máy, trạng thái biên dịch,
    // đọc lại JSON, nạp layout) đi qua đây để test và kịch bản chụp ảnh thay bằng adapter Manual/InMemory/Scripted mà không
    // mở cửa sổ hệ thống. Port public theo luật adapter của package (công cụ dự án khác lắp được); port nào lộ kiểu internal
    // của hub thì internal theo.

    /// <summary>Clipboard hệ thống. Copy JSON đi qua đây để test so đúng từng byte đã đưa người dùng.</summary>
    public interface ILiveOpsHubClipboard
    {
        string Text { get; set; }
    }

    /// <summary>Hộp lưu file và "Mở thư mục". <see cref="SaveFile"/> trả "" khi người dùng huỷ.</summary>
    public interface ILiveOpsHubFileDialog
    {
        string SaveFile(string title, string directory, string defaultName, string extension);
        void Reveal(string path);
    }

    /// <summary>Tên người ghi dấu đã đăng và nguồn của tên đó ("git user.name" | "tên tài khoản máy").</summary>
    public interface ILiveOpsHubPublisherIdentity
    {
        string PublisherName { get; }
        string SourceLabel { get; }
    }

    /// <summary>Độ lệch giờ máy tại một thời điểm (DST đổi độ lệch theo ngày, nên hỏi theo từng mốc, không một hằng).</summary>
    public interface ILiveOpsHubTimeZone
    {
        TimeSpan DeviceOffsetAt(DateTime utc);
    }

    /// <summary>Unity có đang biên dịch script — khung 3 của Hình 28 dựng được bằng adapter Manual.</summary>
    public interface ILiveOpsHubCompilationState
    {
        bool IsCompiling { get; }
    }

    /// <summary>
    /// Hành động mở luồng lớn từ nhiều màn (Tổng quan, việc cần làm, cổng Xuất, Kiểm lịch). Màn chỉ đọc
    /// <c>services.Actions</c> để bật/tắt nút và lấy lý do cạnh nút — không tự biết luồng đã dựng hay chưa.
    /// </summary>
    public interface ILiveOpsHubActions
    {
        void PasteRunningJson(Rect activatorWorldBound);
        bool CanPasteRunningJson { get; }

        /// <summary>Lý do in cạnh nút khi <see cref="CanPasteRunningJson"/> = false; "" khi dùng được.</summary>
        string PasteRunningJsonUnavailableReason { get; }

        /// <summary>(V-14) Tổng quan (a) "Nhập JSON đang chạy…" khi CHƯA có asset.</summary>
        void ImportRunningJsonIntoNewAsset(Rect activatorWorldBound);
        bool CanImportRunningJson { get; }

        /// <summary>Lý do in cạnh nút khi <see cref="CanImportRunningJson"/> = false; "" khi dùng được.</summary>
        string ImportRunningJsonUnavailableReason { get; }
    }

    /// <summary>
    /// Hộp xác nhận. Internal vì <see cref="LiveOpsConfirmRequest"/> là internal (hub không phải API của game);
    /// adapter modal (G-FEEDBACK) gọi <c>ShowModalUtility</c>, test dùng <see cref="ScriptedLiveOpsHubConfirmationPresenter"/>.
    /// </summary>
    internal interface ILiveOpsHubConfirmationPresenter
    {
        LiveOpsConfirmResult Confirm(LiveOpsConfirmRequest request);
    }

    /// <summary>
    /// (V-16) Đọc lại JSON bằng đúng parser của game. Seam để kịch bản (h) "parser không đọc được" và "parser lệch" dựng được
    /// bằng cách viết lại ĐẦU VÀO rồi vẫn chạy parser thật (<see cref="RewritingLiveOpsHubJsonReadBack"/>) — không giả kết
    /// quả parser, vì giả kết quả thì không còn kiểm đường game.
    /// </summary>
    internal interface ILiveOpsHubJsonReadBack
    {
        /// <summary>Đọc và biên dịch như game: lịch game sẽ chạy + lý do bỏ từng mục.</summary>
        LiveEventCalendarParseResult ReadBack(string json);

        /// <summary>
        /// Đọc thành tài liệu, KHÔNG biên dịch: luồng Dán/đọc lại cần thấy đúng cái JSON viết (kể cả mục hỏng) và biết
        /// JSON có đọc được không, mà vẫn phải đi qua đúng bước đọc của parser game.
        /// </summary>
        LiveEventCalendarDocumentParseResult ReadBackDocument(string json);
    }

    /// <summary>(V-16) Nạp UXML/USS của hub — kịch bản h28b "thiếu UXML" thay bằng <see cref="MissingPathsLiveOpsHubLayoutLoader"/>.</summary>
    internal interface ILiveOpsHubLayoutLoader
    {
        VisualTreeAsset LoadVisualTree(string assetPath);
        StyleSheet LoadStyleSheet(string assetPath);
    }
}
