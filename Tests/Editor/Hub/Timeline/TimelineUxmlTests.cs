using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Đăng ký dual-path của control timeline (SP-1): MỘT file UXML đọc ra cùng giá trị ở 2022.3 (<c>UxmlFactory</c>/<c>UxmlTraits</c>
    /// với thuộc tính khai tay tên <c>zoom-level</c>) và 6000.6 (<c>[UxmlAttribute]</c>, tên kebab-case Unity sinh từ property
    /// <c>ZoomLevel</c>). Cổng đợt chạy fixture này ở cả hai bản: lệch tên thuộc tính giữa hai nhánh <c>#if</c> thì UXML của màn Lịch
    /// đọc ra zoom khác nhau theo bản Unity mà không ai báo lỗi.
    ///
    /// Không cần panel: <c>CloneTree</c> đủ để kiểm giá trị thuộc tính đã vào property (giống <c>ControlsUxmlTests</c>).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class TimelineUxmlTests
    {
        private const string ZoomLevelUxmlPath = "Packages/com.dreamtech.liveops/Tests/Editor/Hub/Timeline/Uxml/TimelineZoomLevel.uxml";
        private const double ScaleTolerance = 0.0005;

        [Test]
        public void UxmlZoomLevel2_ReadOnBothVersions()
        {
            VisualElement container = CloneUxml(ZoomLevelUxmlPath);

            LiveOpsTimelineElement month = container.Q<LiveOpsTimelineElement>("month");
            Assert.IsNotNull(month, "thẻ liveops:LiveOpsTimelineElement không dựng được — đăng ký dual-path hỏng ở bản Unity này");
            Assert.AreEqual((int)LiveOpsTimelineZoom.Month, month.ZoomLevel, "zoom-level=\"2\" không đọc được");
            AssertPresetScale(month, LiveOpsTimelineZoom.Month, "month");

            LiveOpsTimelineElement day = container.Q<LiveOpsTimelineElement>("day");
            Assert.IsNotNull(day);
            Assert.AreEqual((int)LiveOpsTimelineZoom.Day, day.ZoomLevel, "zoom-level=\"0\" phải ra Ngày, không bị coi là 'vắng mặt'");
            AssertPresetScale(day, LiveOpsTimelineZoom.Day, "day");

            LiveOpsTimelineElement defaults = container.Q<LiveOpsTimelineElement>("defaults");
            Assert.IsNotNull(defaults);
            Assert.AreEqual((int)LiveOpsTimelineZoom.ThreeWeeks, defaults.ZoomLevel,
                "thuộc tính vắng mặt phải giữ mặc định 3 tuần, không phải default(int) = 0 = Ngày");
            AssertPresetScale(defaults, LiveOpsTimelineZoom.ThreeWeeks, "defaults");

            LiveOpsTimelineElement outOfRange = container.Q<LiveOpsTimelineElement>("out-of-range");
            Assert.IsNotNull(outOfRange);
            Assert.AreEqual((int)LiveOpsTimelineZoom.Month, outOfRange.ZoomLevel, "zoom-level ngoài 0–2 phải kẹp, không đẩy enum lạ vào hình học");

            foreach (LiveOpsTimelineElement element in new[] { month, day, defaults, outOfRange })
            {
                Assert.IsFalse(element.IsContinuousScale, "element dựng từ UXML luôn ở preset — zoom liên tục chỉ đến từ ⌘/Ctrl + bánh xe");
                Assert.IsNotNull(element.Ruler, "thước phải dựng trong constructor, kể cả khi element đến từ UXML");
                Assert.IsNotNull(element.Minimap);
                Assert.IsNotNull(element.Legend);
                Assert.IsNotNull(element.HintLine);
                Assert.IsTrue(element.ClassListContains(LiveOpsHubClassNames.Timeline));
                Assert.AreEqual(string.Empty, element.SelectedBarKey, "element mới dựng chưa chọn thanh nào");
            }

            Assert.IsNotNull(container.Q<LiveOpsTimelineLane>("lane"), "thẻ liveops:LiveOpsTimelineLane không dựng được");
            Assert.IsNotNull(container.Q<LiveOpsTimelineMinimap>("minimap"), "thẻ liveops:LiveOpsTimelineMinimap không dựng được");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Không có layout thì px/giờ tính theo <c>FallbackTrackWidth</c> — chứng minh zoom đã vào hình học, không chỉ vào property.</summary>
        private static void AssertPresetScale(LiveOpsTimelineElement element, LiveOpsTimelineZoom zoom, string context)
        {
            double expected = LiveOpsTimelineElement.FallbackTrackWidth / LiveOpsTimelineGeometry.RangeLengthOf(zoom).TotalHours;
            Assert.AreEqual(expected, element.PixelsPerHour, ScaleTolerance, context + ": px/giờ phải theo preset vừa đọc từ UXML");
        }

        private static VisualElement CloneUxml(string path)
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.IsNotNull(tree, "thiếu " + path);
            VisualElement container = new VisualElement();
            tree.CloneTree(container);
            return container;
        }
    }
}
