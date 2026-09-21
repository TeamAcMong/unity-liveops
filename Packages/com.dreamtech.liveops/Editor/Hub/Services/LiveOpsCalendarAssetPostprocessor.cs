using System;
using UnityEditor;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Báo phiên khi file asset đổi trên đĩa (git pull, sửa tay, xoá, đổi tên) — 4.3. Chỉ chuyển tiếp đường dẫn; so hash để phân biệt
    /// lần tự lưu của hub (SP-8a: <c>SaveAssetIfDirty</c> CÓ bắn postprocessor một lần) là việc của phiên. Không có phiên nghe thì
    /// không làm gì, để mọi import của project không tốn gì vì hub.
    /// </summary>
    internal sealed class LiveOpsCalendarAssetPostprocessor : AssetPostprocessor
    {
        /// <summary>(imported, deleted, moved, movedFrom) — cùng bốn mảng Unity đưa, không lọc: phiên tự tìm đường dẫn của mình.</summary>
        internal static event Action<string[], string[], string[], string[]> AssetsChanged;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            Action<string[], string[], string[], string[]> handlers = AssetsChanged;
            if (handlers == null) return;
            handlers(importedAssets ?? Array.Empty<string>(), deletedAssets ?? Array.Empty<string>(), movedAssets ?? Array.Empty<string>(),
                movedFromAssetPaths ?? Array.Empty<string>());
        }
    }
}
