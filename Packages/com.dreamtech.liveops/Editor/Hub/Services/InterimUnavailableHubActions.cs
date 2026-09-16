using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Action tạm của bản dev: báo không dùng được thay vì ẩn nút, để màn W4 vẽ đúng chỗ nút + lý do cạnh nút và luồng J3
    /// không bị chặn. Màn chỉ đọc <c>services.Actions</c> nên không màn nào mang dấu nhánh tạm; gỡ chỉ là thay adapter.
    /// </summary>
    // INTERIM(G-PASTE): "Dán/Nhập JSON đang chạy…" chưa dựng tới W5 (sổ nhánh tạm mục 12, I-3) — G-PASTE xoá file này
    // và cho builder dùng LiveOpsHubPasteRunningJsonAction thật.
    internal sealed class InterimUnavailableHubActions : ILiveOpsHubActions
    {
        // INTERIM(G-PASTE): lý do tạm hiện cạnh nút; câu nằm ở LiveOpsHubStrings (chữ UI chỉ ở file chuỗi).
        internal static string InterimPasteNotBuiltReason => LiveOpsHubStrings.KitActionNotBuiltInDevBuild;

        public bool CanPasteRunningJson => false;
        public string PasteRunningJsonUnavailableReason => InterimPasteNotBuiltReason;
        public bool CanImportRunningJson => false;
        public string ImportRunningJsonUnavailableReason => InterimPasteNotBuiltReason;

        public void PasteRunningJson(Rect activatorWorldBound)
        {
            // Nút đã tắt; lời gọi lọt (phím tắt, palette) không làm gì thay vì mở một luồng chưa có.
        }

        public void ImportRunningJsonIntoNewAsset(Rect activatorWorldBound)
        {
        }
    }
}
