using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Paste (G-PASTE, W5) — ba ảnh của luồng Dán / Nhập JSON đang chạy:
    /// <list type="bullet">
    /// <item><c>h08c-confirm-replace-draft</c>: hộp cấp 1 "Thay lịch nháp bằng JSON đã dán?" (Hình 8 ô 3).</item>
    /// <item><c>h17-9-remote-drift</c>: màn Kiểm lịch sau khi dán một bản remote LỆCH với dấu đã đăng ([SD2 §2.8]).</item>
    /// <item><c>h09f-overview-import-json</c>: Tổng quan trạng thái (a) với popover "Nhập JSON đang chạy" đã dán, Toggle
    /// ghi dấu bật (vá V-14).</item>
    /// </list>
    /// Cả ba đi qua ĐƯỜNG THẬT: hộp dựng bằng <c>LiveOpsHubPasteRunningJsonAction.BuildReplaceConfirmRequest</c>, bản remote
    /// đặt bằng chính phiên, popover dựng bằng <c>CreatePopover</c> — ảnh chứng minh sản phẩm, không chứng minh file kịch bản.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>Bề rộng cửa sổ popover của hub ([FD §7]) — đo trên GỐC popover, chỗ duy nhất mang đúng 320px.</summary>
        private const float PastePopoverWidth = 320f;

        /// <summary>Header cửa sổ hub cao 26 — giữ một mỏ neo của khung sườn trong ảnh có popover phủ lên.</summary>
        private const float PasteHubHeaderHeight = 26f;

        /// <summary>Ô dán cao ĐÚNG 96px (USS của popover) — ô có viền nên đây là số đo được bằng dò cạnh trên chính ảnh.</summary>
        private const float PasteInputScrollHeight = 96f;

        /// <summary>
        /// Chỗ đặt popover trong ảnh: ngay DƯỚI nút "Nhập JSON đang chạy…" của trạng thái (a), đúng chỗ
        /// <c>PopupWindow</c> thật mở ra — không đặt ĐÈ lên nút, vì ảnh phải cho thấy cả nút nào đã mở ra nó. Số lấy từ
        /// <c>worldBound</c> của <c>overview-import-json</c> trong chính JSON số đo (x = 327, y = 227, cao 18): viết thành số
        /// vì lúc kịch bản chạy chưa có layout để đọc.
        /// </summary>
        private const float PastePopoverLeft = 327f;
        private const float PastePopoverTop = 247f;

        /// <summary>Tên khuôn — để đọc được trong JSON số đo khi cần truy ngược một ảnh.</summary>
        private const string PastePopoverHostName = "paste-running-json-popover-host";

        /// <summary>
        /// Bản remote LỆCH: đúng bản đã đăng 11/9 16:20, sửa hai mục — hunt-0914 đổi configKey và lava-quest-2026-09b đổi giờ
        /// kết thúc. Hai mục là con số của câu thiết kế "Bản remote khác dấu 11/9 16:20: 2 đợt".
        /// </summary>
        private static string PasteDriftedRemoteJson()
        {
            return LiveOpsDesignSample.PublishedSnapshotJson
                .Replace("\"configKey\": \"hunt_v1\"", "\"configKey\": \"hunt_v3\"")
                .Replace("\"endUtc\": \"2026-09-19T00:00:00Z\"", "\"endUtc\": \"2026-09-21T00:00:00Z\"");
        }

        static partial void RegisterPaste(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H08cConfirmReplaceDraft,
                    (int)LiveOpsConfirmWindow.Width, (int)LiveOpsConfirmWindow.Level1Height,
                    () => OpenConfirm(PasteReplaceConfirmRequest(), string.Empty),
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(ConfirmRootExpectedFrame()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H179RemoteDrift, StandardWidth, StandardHeight,
                OpenValidationRemoteDrift));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H09fOverviewImportJson, StandardWidth, StandardHeight,
                    OpenOverviewImportPopover)
                .WithExpectedFrames(PasteImportPopoverFrames()));
        }

        /// <summary>
        /// Ảnh h09f đo CHÍNH cái popover nó sinh ra để chụp (PD-36): bề rộng 320px của gốc popover
        /// (<c>hub-popover-root</c>) — thiếu dòng này thì ảnh chỉ chứng minh cái vỏ màn Tổng quan (đã có ở h09a).
        /// <para>
        /// Thêm chiều cao 96px của khung cuộn ô dán: đây là số ĐO ĐƯỢC bằng dò cạnh (ô có viền), và nó chính là thứ đã sai ở
        /// lượt trước — ô nở theo nội dung đẩy cả Toggle ghi dấu và hai nút ra khỏi khung (soát W5 P-1/P-3). Bề rộng 320 thì
        /// công cụ chỉ đọc được <c>worldBound</c> (thân popover không viền, nền trùng nền cửa sổ) nên một mình nó không đủ.
        /// </para>
        /// <para>
        /// KHÔNG lấy bảng khung mặc định (rail 196 / cột nội dung 1084): popover phủ lên đúng đường ranh rail ↔ nội dung,
        /// làm phép dò cạnh của <c>measure-capture.py</c> ở skin TỐI đọc 194 / 1086 trong khi <c>worldBound</c> vẫn đúng
        /// 196 / 1084 — giới hạn của công cụ, không phải lệch bố cục. Hai số đó đã khoá ở h09a và ở
        /// <c>OverviewSectionTests</c>.
        /// </para>
        /// </summary>
        private static LiveOpsHubCaptureExpectedFrame[] PasteImportPopoverFrames()
        {
            return new[]
            {
                new LiveOpsHubCaptureExpectedFrame(LiveOpsPopoverContent.RootElementName, PastePopoverWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.PasteElementNames.InputScroll, 0f, PasteInputScrollHeight),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Header, 0f, PasteHubHeaderHeight),
            };
        }

        /// <summary>
        /// Hộp ô 3 của Hình 8 — dựng từ nháp mẫu và bản đã đăng làm "JSON đã dán": đúng năm đợt trùng id, một đợt bị xoá
        /// (hunt-0916-bonus) và một luật đổi (weekly-pass), tức câu mẫu của [SD2 §2.9].
        /// </summary>
        internal static LiveOpsConfirmRequest PasteReplaceConfirmRequest()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            LiveOpsHubPasteRunningJsonAction action = (LiveOpsHubPasteRunningJsonAction)services.Actions;
            LiveEventCalendarDocumentParseResult parsed = services.JsonReadBack.ReadBackDocument(PasteDriftedRemoteJson());
            return action.BuildReplaceConfirmRequest(parsed.Document);
        }

        /// <summary>
        /// Trạng thái "đã dán JSON và lệch bản" ([SD2 §2.8]): nhóm Chưa kiểm của luật 12 biến mất, thay bằng hàng Warning
        /// "Bản remote khác dấu …" — đặt bản remote bằng chính phiên rồi kiểm lại như người dùng bấm.
        /// </summary>
        private static EditorWindow OpenValidationRemoteDrift()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            services.Session.Remote.Set(PasteDriftedRemoteJson(), LiveOpsDesignSample.NowUtc);
            services.Session.RunCheckToCompletion();
            return OpenValidation(services, null);
        }

        /// <summary>
        /// Tổng quan (a) + popover "Nhập JSON đang chạy" đã dán. Popover là cửa sổ HĐH riêng nên không nằm trong ảnh của
        /// cửa sổ hub: gắn CHÍNH cây của nó vào root hub (cùng lớp, cùng dữ liệu, cùng gate đọc-lại) — cùng cách Hình 17 ô 5
        /// và ô 7 đã làm.
        /// </summary>
        private static EditorWindow OpenOverviewImportPopover()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);
            EditorWindow window = OpenOverview(services);
            LiveOpsHubPasteRunningJsonAction action = (LiveOpsHubPasteRunningJsonAction)services.Actions;
            PasteRunningJsonPopover popover = action.CreatePopover(PasteRunningJsonMode.ImportIntoNewAsset);
            window.rootVisualElement.Add(PastePopoverHost(popover));
            // Dán sẵn bản đang chạy: ảnh phải vẽ dòng "Đọc được: …" và nút chính ĐÃ MỞ, đúng khung của vá V-14.
            popover.PasteForTest(LiveOpsDesignSample.PublishedSnapshotJson);
            return window;
        }

        /// <summary>
        /// Khuôn của cửa sổ popover thật, lấy từ chính <c>GetWindowSize()</c> mà <c>PopupWindow</c> dùng. Bắt buộc phải chặn:
        /// gắn cây popover vào root hub mà không chặn chiều cao thì ô dán nở theo nội dung (lượt trước: quá 550px) và dòng
        /// trạng thái, Toggle ghi dấu, hai nút đều rơi khỏi ảnh — tức là ảnh vẽ một popover KHÔNG TỒN TẠI (soát W5 P-1).
        /// </summary>
        private static VisualElement PastePopoverHost(PasteRunningJsonPopover popover)
        {
            Vector2 popoverSize = popover.GetWindowSize();
            VisualElement host = new VisualElement { name = PastePopoverHostName };
            host.style.position = Position.Absolute;
            host.style.left = PastePopoverLeft;
            host.style.top = PastePopoverTop;
            host.style.width = popoverSize.x;
            host.style.height = popoverSize.y;
            host.style.overflow = Overflow.Hidden;
            host.Add(popover.BuildForTest());
            return host;
        }
    }
}
