using System;
using System.Collections;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// (V-21 D-3) Nửa lane + minimap của <c>SelfDrawnElements_ResolveTokens_BothSkinClasses</c> (nửa JSON view + cycle bar ở
    /// <c>ControlsSkinTokenTests</c> của G-CONTROLS). R-11: custom property KHÔNG kế thừa từ root ([API §12.2]) — element tự vẽ chỉ đọc
    /// được token khi <c>liveops-hub-timeline.uss</c> khai lại token trên chính selector của làn/minimap bằng <c>var()</c>. Quên khai lại
    /// thì <c>TryGetValue</c> trả false, nền cuối tuần / vùng chồng / dải màu loại về màu dự phòng của C# — không log, chỉ thấy trên ảnh.
    ///
    /// Kiểm ở root thường và root <c>liveops-hub--skin-light</c> (đổi class, không đổi skin thật của Editor — SP-4), và giá trị phải theo
    /// đúng skin của root. Dùng <see cref="TimelineTestPanel"/> (cửa sổ thử của thư mục Timeline) — không mở cửa sổ hub, không dùng phiên.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class TimelineSkinTokenTests
    {
        // Giá trị của liveops-hub-theme.uss ([FD §2.3, §2.6]) — theme đổi thì test này đổi cùng commit.
        private static readonly Color DarkLaneGrid = new Color(1f, 1f, 1f, 0.06f);          // rgba(255, 255, 255, 0.06)
        private static readonly Color LightLaneGrid = new Color(0f, 0f, 0f, 0.08f);         // rgba(0, 0, 0, 0.08)
        private static readonly Color DarkLaneOverlap = Hex("#F05454");
        private static readonly Color LightLaneOverlap = Hex("#A80000");
        private static readonly Color DarkMinimapNow = Hex("#E6E6E6");
        private static readonly Color LightMinimapNow = Hex("#1A1A1A");
        private static readonly Color DarkMinimapBlocked = Hex("#F05454");
        private static readonly Color LightMinimapBlocked = Hex("#A80000");
        private static readonly Color DarkMinimapEventColor0 = Hex("#5096DE");
        private static readonly Color LightMinimapEventColor0 = Hex("#2F6DB5");
        private const float ColorTolerance = 1.5f / 255f;
        private const float LaneTrackWidth = 635f;
        private const string TreasureHuntTypeId = "treasure-hunt";

        private TimelineTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        [UnityTest]
        public IEnumerator SelfDrawnElements_ResolveTokens_BothSkinClasses()
        {
            LiveOpsTimelineModel model = TimelineDesignSampleInput.ThreeWeeks(LaneTrackWidth).Build();
            LiveOpsTimelineLaneModel laneModel = model.FindLane(TreasureHuntTypeId);
            Assert.IsNotNull(laneModel, "tiền đề: dữ liệu mẫu phải có làn treasure-hunt (làn có vùng chồng)");

            _panel = TimelineTestPanel.Open();
            VisualElement darkRoot = _panel.CreateRoot(false);
            VisualElement lightRoot = _panel.CreateRoot(true);
            LiveOpsTimelineLane darkLane = AddLane(darkRoot, laneModel, model.Geometry);
            LiveOpsTimelineLane lightLane = AddLane(lightRoot, laneModel, model.Geometry);
            LiveOpsTimelineMinimap darkMinimap = AddMinimap(darkRoot, model);
            LiveOpsTimelineMinimap lightMinimap = AddMinimap(lightRoot, model);
            yield return TimelineTestPanel.WaitUntil(
                () => TimelineTestPanel.HasLayout(darkLane) && TimelineTestPanel.HasLayout(lightLane) &&
                      TimelineTestPanel.HasLayout(darkMinimap) && TimelineTestPanel.HasLayout(lightMinimap),
                "làn/minimap chưa có layout");
            yield return null;
            yield return null;

            AssertLaneTokens(darkLane, "root thường", DarkLaneGrid, DarkLaneOverlap);
            AssertLaneTokens(lightLane, "root liveops-hub--skin-light", LightLaneGrid, LightLaneOverlap);
            AssertMinimapTokens(darkMinimap, "root thường", DarkMinimapNow, DarkMinimapBlocked, DarkMinimapEventColor0);
            AssertMinimapTokens(lightMinimap, "root liveops-hub--skin-light", LightMinimapNow, LightMinimapBlocked, LightMinimapEventColor0);

            // Đổi class skin lúc đang mở (probe skin của hub bật/tắt class): token và màu đã đọc phải theo ngay.
            lightRoot.RemoveFromClassList(LiveOpsHubClassNames.SkinLight);
            yield return null;
            yield return null;
            AssertLaneTokens(lightLane, "root vừa tắt liveops-hub--skin-light", DarkLaneGrid, DarkLaneOverlap);
            AssertMinimapTokens(lightMinimap, "root vừa tắt liveops-hub--skin-light", DarkMinimapNow, DarkMinimapBlocked, DarkMinimapEventColor0);
            LogAssert.NoUnexpectedReceived();
        }

        private static LiveOpsTimelineLane AddLane(VisualElement root, LiveOpsTimelineLaneModel laneModel, LiveOpsTimelineGeometry geometry)
        {
            LiveOpsTimelineLane lane = new LiveOpsTimelineLane();
            lane.style.width = LaneTrackWidth;
            lane.style.height = 80f;
            lane.style.flexGrow = 0f;
            lane.style.flexShrink = 0f;
            root.Add(lane);
            lane.SetLane(laneModel, geometry);
            return lane;
        }

        private static LiveOpsTimelineMinimap AddMinimap(VisualElement root, LiveOpsTimelineModel model)
        {
            LiveOpsTimelineMinimap minimap = new LiveOpsTimelineMinimap();
            minimap.style.width = LaneTrackWidth;
            minimap.style.flexGrow = 0f;
            minimap.style.flexShrink = 0f;
            root.Add(minimap);
            minimap.SetMinimap(model.Minimap);
            return minimap;
        }

        private static void AssertLaneTokens(LiveOpsTimelineLane lane, string context, Color grid, Color overlap)
        {
            AssertToken(lane.customStyle, LiveOpsTimelineLane.GridColorProperty, grid, context + " · vạch ngày của làn");
            AssertToken(lane.customStyle, LiveOpsTimelineLane.WeekendColorProperty, null, context + " · cột cuối tuần");
            AssertToken(lane.customStyle, LiveOpsTimelineLane.TodayColorProperty, null, context + " · cột hôm nay");
            AssertToken(lane.customStyle, LiveOpsTimelineLane.GridMondayColorProperty, null, context + " · vạch thứ Hai");
            AssertToken(lane.customStyle, LiveOpsTimelineLane.OverlapColorProperty, overlap, context + " · vùng chồng");
            AssertColor(grid, lane.GridColor, context + ": màu vạch đã lưu trong làn");
            AssertColor(overlap, lane.OverlapColor, context + ": màu vùng chồng đã lưu trong làn");
        }

        private static void AssertMinimapTokens(LiveOpsTimelineMinimap minimap, string context, Color now, Color blocked, Color eventColor0)
        {
            AssertToken(minimap.customStyle, LiveOpsTimelineMinimap.NowColorProperty, now, context + " · vạch bây giờ");
            AssertToken(minimap.customStyle, LiveOpsTimelineMinimap.BlockedColorProperty, blocked, context + " · vạch đỏ");
            AssertToken(minimap.customStyle, LiveOpsTimelineMinimap.WarningColorProperty, null, context + " · vạch vàng");
            AssertToken(minimap.customStyle, LiveOpsTimelineMinimap.TextColorProperty, null, context + " · chữ hai đầu dải");
            for (int slot = 0; slot < LiveOpsTimelineMinimap.EventColorProperties.Length; slot++)
            {
                AssertToken(minimap.customStyle, LiveOpsTimelineMinimap.EventColorProperties[slot], slot == 0 ? (Color?)eventColor0 : null,
                    context + " · dải màu loại " + slot);
            }
            AssertColor(blocked, minimap.BlockedColor, context + ": màu vạch đỏ đã lưu trong minimap");
            AssertColor(eventColor0, minimap.EventColor(0), context + ": màu dải loại 0 đã lưu trong minimap");
        }

        /// <summary><paramref name="expected"/> null = chỉ đòi token resolve được (giá trị đã khoá ở test khác), khác null = kiểm cả màu.</summary>
        private static void AssertToken(ICustomStyle style, CustomStyleProperty<Color> property, Color? expected, string context)
        {
            Assert.IsTrue(style.TryGetValue(property, out Color actual),
                context + ": TryGetValue(" + property.name + ") = false — USS phải khai lại token trên selector của chính element");
            if (expected.HasValue) AssertColor(expected.Value, actual, context);
        }

        private static void AssertColor(Color expected, Color actual, string context)
        {
            Assert.AreEqual(expected.r, actual.r, ColorTolerance, context + " (r) — token sai skin?");
            Assert.AreEqual(expected.g, actual.g, ColorTolerance, context + " (g)");
            Assert.AreEqual(expected.b, actual.b, ColorTolerance, context + " (b)");
            Assert.AreEqual(expected.a, actual.a, ColorTolerance, context + " (a) — alpha của token nền là phần hồn của nó");
        }

        private static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color)) throw new ArgumentException("hằng màu hỏng: " + hex, nameof(hex));
            return color;
        }
    }
}
