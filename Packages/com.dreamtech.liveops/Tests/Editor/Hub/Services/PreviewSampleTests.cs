using System;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// "Hiện dữ liệu mẫu" (mục 8) dựng từ bản sao Editor của fixture thiết kế. Hai điều phải giữ: bản sao không trôi khỏi
    /// <see cref="LiveOpsDesignSample"/>, và asset xem thử không bao giờ thành file trong project của người dùng.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class PreviewSampleTests
    {
        [Test]
        public void PreviewSample_EqualsDesignSampleFixture()
        {
            LiveOpsDocumentAssert.AssertDocumentsEqual(LiveOpsDesignSample.Document, LiveOpsHubPreviewSample.Document);

            Assert.AreEqual(LiveOpsDesignSample.NowUtc, LiveOpsHubPreviewSample.NowUtc);
            Assert.AreEqual(DateTimeKind.Utc, LiveOpsHubPreviewSample.NowUtc.Kind);
            Assert.AreEqual(LiveOpsDesignSample.DeviceOffset, LiveOpsHubPreviewSample.DeviceOffset);

            ManualLiveOpsClock clock = LiveOpsHubPreviewSample.CreateClock();
            Assert.AreEqual(LiveOpsDesignSample.NowUtc, clock.UtcNow);
            Assert.IsTrue(clock.IsTrusted, "đồng hồ mẫu chưa tin được thì hub sẽ vẽ trạng thái 'chưa đồng bộ giờ' thay cho mẫu thiết kế");

            ManualLiveOpsHubTimeZone timeZone = LiveOpsHubPreviewSample.CreateTimeZone();
            Assert.AreEqual(LiveOpsDesignSample.DeviceOffset, timeZone.DeviceOffsetAt(LiveOpsDesignSample.NowUtc));

            // Cửa sổ mẫu đọc lịch qua asset, không qua Document — so cả đường khứ hồi asset để một field asset làm rơi (vd entryKey,
            // dấu đã đăng) cũng đỏ ở đây chứ không chỉ lộ ra trong ảnh chụp.
            LiveEventCalendarAsset asset = LiveOpsHubPreviewSample.CreateAsset();
            try
            {
                LiveOpsDocumentAssert.AssertDocumentsEqual(LiveOpsDesignSample.Document, asset.ToDocument());
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void PreviewSample_Document_IsFreshEachCall()
        {
            LiveEventCalendarDocument first = LiveOpsHubPreviewSample.Document;
            LiveEventCalendarDocument second = LiveOpsHubPreviewSample.Document;

            Assert.AreNotSame(first, second);
            LiveOpsDocumentAssert.AssertDocumentsEqual(first, second);
        }

        [Test]
        public void PreviewAsset_HideFlagsDontSave_NeverOnDisk()
        {
            string typeFilter = "t:" + nameof(LiveEventCalendarAsset);
            string[] calendarAssetGuidsBefore = AssetDatabase.FindAssets(typeFilter);

            LiveEventCalendarAsset asset = LiveOpsHubPreviewSample.CreateAsset();
            try
            {
                Assert.AreEqual(HideFlags.DontSave, asset.hideFlags & HideFlags.DontSave,
                    "thiếu cờ DontSave thì tham chiếu lỡ gán vào scene/prefab sẽ kéo dữ liệu mẫu ra đĩa");
                Assert.AreEqual(LiveOpsHubPreviewSample.AssetName, asset.name);
                AssertNotOnDisk(asset);

                // Giả lập đúng thứ phiên làm sau mỗi lần sửa (SetDirty) rồi ⌘S (lưu asset bẩn) — asset mẫu vẫn không có đường dẫn.
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
                AssertNotOnDisk(asset);

                CollectionAssert.AreEquivalent(calendarAssetGuidsBefore, AssetDatabase.FindAssets(typeFilter),
                    "tạo asset mẫu không được thêm asset lịch nào vào project");
                // Lọc theo ScriptableObject: tìm theo tên trơn khớp cả chính file LiveOpsHubPreviewSample.cs (MonoScript).
                CollectionAssert.IsEmpty(AssetDatabase.FindAssets(LiveOpsHubPreviewSample.AssetName + " t:" + nameof(ScriptableObject)),
                    "project không được có asset mang tên asset mẫu");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        private static void AssertNotOnDisk(LiveEventCalendarAsset asset)
        {
            Assert.IsFalse(EditorUtility.IsPersistent(asset), "asset mẫu đã thành asset lưu trên đĩa");
            Assert.IsFalse(AssetDatabase.Contains(asset), "AssetDatabase đang theo dõi asset mẫu");
            Assert.AreEqual(string.Empty, AssetDatabase.GetAssetPath(asset));
        }
    }
}
