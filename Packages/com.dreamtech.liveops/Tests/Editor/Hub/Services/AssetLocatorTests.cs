using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Nhớ asset lịch đang mở (PD-16). (SP-14) Chỗ nhớ là <c>EditorUserSettings</c> — theo project + người dùng, ghi ra
    /// <c>UserSettings/EditorUserSettings.asset</c>, sống qua khởi động lại Unity và qua xoá <c>Library/</c> ở cả hai bản; không
    /// <c>EditorPrefs</c> (theo máy, lẫn giữa các project) và không <c>SessionState</c> (mất khi đóng Unity).
    /// Test dùng KHOÁ RIÊNG để không đè lịch mà người dùng đang mở thật.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class AssetLocatorTests
    {
        private const string TestSettingsKey = "DreamTech.LiveOps.Hub.Tests.CalendarAssetGuid";

        [SetUp]
        public void SetUp()
        {
            EditorUserSettings.SetConfigValue(TestSettingsKey, null);
        }

        [TearDown]
        public void TearDown()
        {
            EditorUserSettings.SetConfigValue(TestSettingsKey, null);
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void AssetLocator_RemembersGuidPerProject()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile("LocatorMain.asset", LiveOpsDesignSample.Document);
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            LiveOpsHubAssetLocator locator = new LiveOpsHubAssetLocator(TestSettingsKey);
            Assert.IsEmpty(locator.RememberedGuid, "chưa nhớ gì");

            locator.Remember(asset);

            Assert.AreEqual(guid, locator.RememberedGuid, "nhớ bằng GUID, không phải instance ID hay đường dẫn");
            Assert.AreEqual(guid, EditorUserSettings.GetConfigValue(TestSettingsKey),
                "(SP-14) chỗ nhớ phải là EditorUserSettings — sống qua khởi động lại và qua xoá Library/");
            Assert.AreSame(asset, new LiveOpsHubAssetLocator(TestSettingsKey).FindRemembered(),
                "bộ tìm mới của lần mở sau đọc lại đúng asset đó");

            locator.Forget();
            Assert.IsEmpty(locator.RememberedGuid);
        }

        [Test]
        public void AssetLocator_MemoryAssetIsNotRemembered()
        {
            LiveOpsHubAssetLocator locator = new LiveOpsHubAssetLocator(TestSettingsKey);

            locator.Remember(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document));

            Assert.IsEmpty(locator.RememberedGuid, "asset chỉ trong bộ nhớ không có GUID — không có gì để tìm lại lần sau");
        }

        [Test]
        public void AssetLocator_CountsMultipleAssets()
        {
            LiveOpsHubAssetLocator locator = new LiveOpsHubAssetLocator(TestSettingsKey);
            int before = locator.CountAssets();

            LiveOpsHubTestServices.CreateAssetFile("LocatorOne.asset", LiveOpsDesignSample.Document);
            LiveOpsHubTestServices.CreateAssetFile("LocatorTwo.asset", LiveOpsDesignSample.Document);

            Assert.AreEqual(before + 2, locator.CountAssets(), "HelpBox 'Có N LiveEventCalendarAsset trong project' đếm cả project");
            IReadOnlyList<string> paths = locator.FindAssetPaths();
            CollectionAssert.Contains(paths, LiveOpsHubTestServices.TestFolder + "/LocatorOne.asset");
            CollectionAssert.Contains(paths, LiveOpsHubTestServices.TestFolder + "/LocatorTwo.asset");
            CollectionAssert.IsOrdered(paths, System.StringComparer.Ordinal, "thứ tự phải ổn định giữa các lần mở để lựa chọn mặc định không nhảy");
        }

        [Test]
        public void AssetLocator_ForgottenGuid_FallsBackToFirstAssetAndRemembersIt()
        {
            LiveOpsHubTestServices.CreateAssetFile("LocatorOne.asset", LiveOpsDesignSample.Document);
            LiveOpsHubAssetLocator locator = new LiveOpsHubAssetLocator(TestSettingsKey);
            IReadOnlyList<string> paths = locator.FindAssetPaths();
            Assert.Greater(paths.Count, 0, "project của test phải có ít nhất một lịch");

            LiveEventCalendarAsset found = locator.FindRemembered();

            Assert.IsNotNull(found, "mở hub lần đầu trong project có lịch thì không bắt người dùng đi chọn");
            Assert.AreEqual(paths[0], AssetDatabase.GetAssetPath(found), "lấy asset đầu tiên theo thứ tự đường dẫn");
            Assert.AreEqual(AssetDatabase.AssetPathToGUID(paths[0]), locator.RememberedGuid, "và nhớ luôn nó cho lần sau");
        }

        [Test]
        public void AssetLocator_RememberedAssetDeleted_FallsBackWithoutThrowing()
        {
            LiveEventCalendarAsset gone = LiveOpsHubTestServices.CreateAssetFile("LocatorGone.asset", LiveOpsDesignSample.Document);
            LiveOpsHubTestServices.CreateAssetFile("LocatorAlive.asset", LiveOpsDesignSample.Document);
            LiveOpsHubAssetLocator locator = new LiveOpsHubAssetLocator(TestSettingsKey);
            locator.Remember(gone);
            string goneGuid = locator.RememberedGuid;

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(gone));

            LiveEventCalendarAsset found = null;
            Assert.DoesNotThrow(() => found = locator.FindRemembered(), "GUID trỏ vào hư không không được làm hub ném");
            Assert.IsNotNull(found, "project vẫn còn lịch khác nên phải mở được một cái");
            Assert.AreNotEqual(goneGuid, locator.RememberedGuid, "nhớ lại asset còn sống thay cho GUID đã chết");
        }
    }
}
