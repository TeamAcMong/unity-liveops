using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class IconsTests
    {
        [SetUp]
        public void SetUp()
        {
            LiveOpsHubIcons.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubIcons.ClearCache();
        }

        [Test]
        public void Icons_AllNamesLoad()
        {
            List<string> missing = new List<string>();
            foreach (string name in LiveOpsHubIcons.AllDesignNames)
            {
                // Hai skin qua tham số: không đổi skin thật của Editor (ghi Preferences toàn máy, treo batchmode — SP-4).
                if (LiveOpsHubIcons.Get(name, false) == null) missing.Add(name + " (sáng)");
                if (LiveOpsHubIcons.Get(name, true) == null) missing.Add(name + " (tối)");
            }

            CollectionAssert.IsEmpty(missing, "icon trống trên nút ở bản Unity này: " + string.Join(", ", missing));
            CollectionAssert.Contains(LiveOpsHubIcons.AllDesignNames, "clear", "nút đóng dùng clear/d_clear — icon đóng cũ của Unity trả NULL ở 6000.6 (PD-14)");
            Assert.AreEqual(38, LiveOpsHubIcons.AllDesignNames.Count, "26 icon lưới thiết kế + 12 khung WaitSpin");
            // Log Error (IconContent "Unable to load the icon") làm fail ở bản thiếu icon — FindTexture không được log.
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Icons_ClipboardDark_UsesPackagePng()
        {
            Texture dark = LiveOpsHubIcons.Get("Clipboard", true);
            Texture light = LiveOpsHubIcons.Get("Clipboard", false);

            Assert.IsNotNull(dark);
            Assert.IsNotNull(light);
            string darkPath = AssetDatabase.GetAssetPath(dark);
            Assert.IsTrue(darkPath.StartsWith(LiveOpsHubPaths.IconsDirectory + LiveOpsHubPaths.DarkIconPrefix + LiveOpsHubPaths.ClipboardIconName, System.StringComparison.Ordinal),
                "Clipboard dựng sẵn thiếu d_ ở hai bản Unity — skin tối phải đọc PNG của package, không thì icon xám tối mất trên nền #383838");
            Assert.AreNotEqual(dark, light, "skin sáng vẫn dùng icon Clipboard của Unity");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Icons_PackageCalendarPngLoads()
        {
            Texture2D lightAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(LiveOpsHubPaths.CalendarIconPng);
            Texture2D darkAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(LiveOpsHubPaths.CalendarIconDarkPng);
            Assert.IsNotNull(lightAsset, "thiếu " + LiveOpsHubPaths.CalendarIconPng);
            Assert.IsNotNull(darkAsset, "thiếu " + LiveOpsHubPaths.CalendarIconDarkPng);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(LiveOpsHubPaths.CalendarIconPng2x));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(LiveOpsHubPaths.CalendarIconDarkPng2x));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(LiveOpsHubPaths.ClipboardIconDarkPng2x));

            Texture light = LiveOpsHubIcons.Get(LiveOpsHubPaths.CalendarIconName, false);
            Texture dark = LiveOpsHubIcons.Get(LiveOpsHubPaths.CalendarIconName, true);
            bool retina = EditorGUIUtility.pixelsPerPoint > 1f;
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(LiveOpsHubIcons.PackagePath(LiveOpsHubPaths.CalendarIconName, false, retina)), light);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(LiveOpsHubIcons.PackagePath(LiveOpsHubPaths.CalendarIconName, true, retina)), dark);
            Assert.AreNotEqual(light, dark, "icon lịch có hai bản theo skin");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CreateImage_FixedSizeClassAndScaleToFit()
        {
            UnityEngine.UIElements.Image image = LiveOpsHubIcons.CreateImage("Search Icon", 14);
            Assert.AreEqual(ScaleMode.ScaleToFit, image.scaleMode, "Search Icon là texture 64×64 — không ScaleToFit thì phình to");
            Assert.IsTrue(image.ClassListContains(LiveOpsHubClassNames.IconSize14));
            Assert.IsNotNull(image.image);
            Assert.AreEqual("WaitSpin00", LiveOpsHubIcons.SpinnerFrameName(12));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Spinner_StartStopAndAdvanceWrapsFrames()
        {
            // Nhịp 83 ms gắn với panel nên test gọi Advance trực tiếp — chờ lịch thật vừa chậm vừa phụ thuộc có cửa sổ.
            LiveOpsSpinner spinner = new LiveOpsSpinner();
            Assert.IsTrue(spinner.IsSpinning, "spinner mới tạo là đang quay — nơi dựng không phải gọi Start lần nữa");
            Assert.AreEqual(0, spinner.FrameIndex);
            Assert.AreEqual(LiveOpsHubIcons.Get(LiveOpsHubIcons.SpinnerFrameName(0)), spinner.image);
            Assert.AreEqual(PickingMode.Ignore, spinner.pickingMode, "spinner nằm trên nút/hàng — không được nuốt click");

            spinner.Stop();
            Assert.IsFalse(spinner.IsSpinning);
            spinner.Start();
            Assert.IsTrue(spinner.IsSpinning, "Start khi chưa có panel chỉ bật cờ, không ném vì chưa có lịch");

            spinner.Advance();
            Assert.AreEqual(1, spinner.FrameIndex);
            Assert.AreEqual(LiveOpsHubIcons.Get(LiveOpsHubIcons.SpinnerFrameName(1)), spinner.image, "mỗi nhịp phải đổi texture, không chỉ đổi chỉ số");
            for (int step = 1; step < LiveOpsHubIcons.SpinnerFrames; step++) spinner.Advance();
            Assert.AreEqual(0, spinner.FrameIndex, "sau khung 11 quay về WaitSpin00 — vượt 11 là tên icon không tồn tại");
            Assert.AreEqual(LiveOpsHubIcons.Get(LiveOpsHubIcons.SpinnerFrameName(0)), spinner.image);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
