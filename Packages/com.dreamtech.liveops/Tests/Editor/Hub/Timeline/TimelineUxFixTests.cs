using System;
using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Đợt W8-UX, gói C (`plan/w8-ux/UX-FIX-PLAN.md` mục 1): test KHOÁ LỖI của control timeline — mỗi test dựng đúng cảnh mà
    /// ảnh chụp / người dùng giả đã bắt được, nên nó ĐỎ trên code trước đợt và chỉ xanh khi lỗi thật sự hết.
    ///
    /// UX-01 (thân làn cao 0 ở 2022.3 + minimap dính làn cuối) · UX-13 (chevron không bấm được) · UX-14 (xem trước kéo còn
    /// giữ dấu "bị bỏ" và nhãn chồng cũ) · UX-16 (meta làn bị chip nuốt chỗ) · UX-17 (cờ "bây giờ" che nhãn tháng, nhãn ngày
    /// cụt) · UX-18 (readout dính dấu ngăn, đè hàng thước) · UX-19 (mẫu chú giải trùng nền) · UX-31 (làn thu gọn, thanh hẹp
    /// bị bỏ, nhãn dải, câu gợi ý làn trống).
    ///
    /// Cảnh tương tác đi qua sự kiện chuột THẬT của cửa sổ (<see cref="TimelineTestPanel.SendMouse"/>) đúng luật đợt: không
    /// gọi thẳng handler hay model để gây hành vi, chỉ đọc lại trạng thái để assert.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class TimelineUxFixTests
    {
        /// <summary>Cỡ nhỏ nhất của ma trận cổng W8 — track hẹp nhất nên nhãn thước cụt sớm nhất (UX-17).</summary>
        private const float NarrowWindowWidth = 700f;

        private const float NarrowWindowHeight = 560f;

        /// <summary>Sai số một pixel khi so hình học đã qua layout (làm tròn của Yoga).</summary>
        private const float LayoutTolerance = 1f;

        /// <summary>Bề rộng tối thiểu để một nhãn còn đọc được trong track (R-09): dưới mức này coi như mất hẳn.</summary>
        private const float MinimumReadableLabelWidth = 16f;

        /// <summary>
        /// (R-07) Ma trận cỡ cửa sổ của cổng W8-UX. Gói này đụng layout (sàn thân, meta xuống dòng, chevron rộng thêm, khe
        /// minimap, chỗ đặt readout) nên phải có bằng chứng co giãn ở CẢ SÁU cỡ, không chỉ cỡ mặc định — R-01 lọt qua đúng vì
        /// thiếu ma trận này.
        /// </summary>
        private static readonly Vector2[] WindowSizeMatrix =
        {
            new Vector2(700f, 560f),
            new Vector2(820f, 560f),
            new Vector2(1024f, 700f),
            new Vector2(1280f, 760f),
            new Vector2(1440f, 900f),
            new Vector2(1920f, 1040f),
        };

        private TimelineTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        private TimelineHarness Harness => _panel.Harness;
        private LiveOpsTimelineElement Element => _panel.Harness.Element;

        private IEnumerator Open(LiveEventCalendarDocument document = null, LiveOpsTimelineZoom zoom = LiveOpsTimelineZoom.ThreeWeeks,
            float width = TimelineTestPanel.DefaultWidth, float height = TimelineTestPanel.DefaultHeight, DateTime? rangeStartUtc = null,
            string[] collapsedLanes = null, DateTime? nowUtc = null)
        {
            _panel = TimelineTestPanel.Open(width, height);
            VisualElement root = _panel.CreateRoot(false);
            Func<LiveOpsTimelineInput> inputFactory = TimelineViewInputs.Checked(document ?? LiveOpsDesignSample.Document);
            if (collapsedLanes != null)
            {
                Func<LiveOpsTimelineInput> baseFactory = inputFactory;
                inputFactory = () => baseFactory().WithCollapsedLanes(collapsedLanes);
            }
            if (nowUtc.HasValue)
            {
                Func<LiveOpsTimelineInput> baseFactory = inputFactory;
                DateTime overriddenNowUtc = nowUtc.Value;
                inputFactory = () => baseFactory().WithNowUtc(overriddenNowUtc);
            }
            TimelineHarness harness = TimelineHarness.Create(root, inputFactory);
            harness.Start(rangeStartUtc ?? LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, zoom), zoom);
            _panel.Harness = harness;
            yield return harness.WaitReady();
        }

        /// <summary>Đóng cửa sổ thử giữa chừng — test quét nhiều cỡ cửa sổ mở lại panel cho từng cỡ.</summary>
        private void Close()
        {
            _panel?.Dispose();
            _panel = null;
        }

        // ================================================================================================ UX-01 · UX-19 khung

        /// <summary>
        /// (UX-01, UJ-26 / C1 / C2) Thân timeline phải tự có sàn chiều cao đủ MỘT làn: ở 2022.3 ScrollView không góp chiều cao
        /// nội tại, nên khi chuỗi cha chưa giãn thì vùng làn tụt về 0 và không còn thanh nào để bấm. (UX-19, V1) Minimap phải
        /// có khe tách trên, nếu không nó dính sát làn cuối và đọc thành một dải tràn của làn đó.
        /// </summary>
        [UnityTest]
        public IEnumerator Body_HasOneLaneFloor_AndMinimapHasTopGap()
        {
            yield return Open();

            float floor = Element.Body.resolvedStyle.minHeight.value;
            Assert.GreaterOrEqual(floor, LiveOpsTimelineGeometry.LanePaddingTop + LiveOpsTimelineGeometry.RowPitch,
                "thân timeline phải có min-height đủ một làn mở — không có sàn thì 2022.3 vẽ vùng làn cao 0 (UJ-26)");
            Assert.GreaterOrEqual(Element.Minimap.resolvedStyle.marginTop, 6f - LayoutTolerance,
                "minimap phải tách khỏi làn cuối ≥ 6px (V1)");
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ UX-13 chevron

        /// <summary>
        /// (UX-13, UJ-08) Mũi tên ▾ cạnh tên làn là thứ DUY NHẤT trông như nút thu gọn trên header; trước đợt nó
        /// <c>PickingMode.Ignore</c> và không có handler nên bấm rơi vào hư không, người dùng phải tìm ra menu chuột phải.
        /// Bấm bằng chuột thật phải phát đúng một <see cref="ToggleLaneCollapsedIntent"/> và KHÔNG mở cử chỉ kéo/khung chọn.
        /// </summary>
        [UnityTest]
        public IEnumerator LaneChevron_Click_TogglesCollapse()
        {
            yield return Open();
            LiveOpsTimelineLaneHeader header = Element.FindHeader("treasure-hunt");
            Assert.IsNotNull(header, "tiền đề: có làn treasure-hunt");
            Assert.IsFalse(header.Model.IsCollapsed, "tiền đề: làn đang mở");
            Rect chevron = header.Chevron.worldBound;
            Assert.GreaterOrEqual(chevron.width, 14f, "vùng bấm chevron phải đủ rộng cho chuột thật (≥ 14px)");
            Assert.GreaterOrEqual(chevron.height, 14f, "vùng bấm chevron phải đủ cao cho chuột thật (≥ 14px)");

            _panel.SendMouse(EventType.MouseDown, chevron.center);
            _panel.SendMouse(EventType.MouseUp, chevron.center);
            yield return null;

            List<ToggleLaneCollapsedIntent> toggles = Harness.IntentsOf<ToggleLaneCollapsedIntent>();
            Assert.AreEqual(1, toggles.Count, "bấm chevron phải phát đúng một lệnh thu gọn làn (UJ-08)");
            Assert.AreEqual("treasure-hunt", toggles[0].TypeId);
            Assert.IsTrue(toggles[0].Collapsed, "làn đang mở thì bấm = thu gọn");
            Assert.AreEqual(0, Harness.IntentsOf<MoveBarIntent>().Count, "bấm chevron không được mở cử chỉ kéo");
            Assert.AreEqual(0, Harness.IntentsOf<SelectBarIntent>().Count, "bấm chevron không chọn thanh nào");
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ UX-14 xem trước kéo

        /// <summary>
        /// (UX-14, UJ-13 / V10) Kéo một đợt ĐANG bị bỏ vì chồng giờ ra khỏi vùng chồng: trước đợt, thanh vẫn gạch ngang, tag
        /// "bị bỏ" vẫn treo và nhãn "chồng 12 giờ" của model cũ vẫn nằm giữa làn — ba dấu hiệu nói ngược với thứ người dùng vừa
        /// làm. Xem trước phải đặt lại cả ba theo kết quả xem trước, và trả về trạng thái model khi huỷ.
        /// </summary>
        [UnityTest]
        public IEnumerator DragOutOfOverlap_PreviewClearsDroppedStateAndOverlapLabel()
        {
            yield return Open();
            LiveOpsTimelineBar bonus = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.IsNotNull(bonus, "tiền đề: có thanh hunt-0916-bonus");
            Assert.IsTrue(bonus.Model.IsDropped, "tiền đề: bonus bị bỏ vì chồng giờ trong dữ liệu mẫu");
            LiveOpsTimelineLane lane = Element.FindLaneElement("treasure-hunt");
            Assert.AreEqual(1, lane.DrawnOverlaps.Count, "tiền đề: làn có một vùng chồng");
            Assert.AreEqual(1, VisibleOverlapLabelCount(lane), "tiền đề: có nhãn chồng giờ");

            Vector2 start = bonus.worldBound.center;
            float threeDays = (float)(Element.PixelsPerHour * 72d);
            _panel.SendMouse(EventType.MouseDown, start);
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(threeDays / 2f, 0f));
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(threeDays, 0f));
            yield return null;

            Assert.IsTrue(Element.DragController.IsDragging, "tiền đề: đang kéo");
            Assert.AreEqual(0, Element.DragController.WillDropBarKeys.Count, "tiền đề: kéo đủ xa thì không còn đợt nào bị bỏ");
            Assert.IsFalse(bonus.ClassListContains(LiveOpsHubClassNames.TimelineBarDropped),
                "thanh đang kéo hết chồng giờ phải bỏ dấu \"bị bỏ\" ngay trong lúc xem trước (UJ-13)");
            Assert.IsTrue(bonus.Strike.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "gạch ngang phải tắt theo");
            Assert.AreEqual(0, VisibleDroppedTagCount(lane), "tag \"bị bỏ\" cũ không được treo lại khi xem trước nói hết chồng");
            Assert.AreEqual(0, VisibleOverlapLabelCount(lane), "nhãn \"chồng 12 giờ\" của model cũ phải biến mất theo vùng chồng");

            _panel.SendKey("escape");
            yield return null;
            Assert.IsTrue(bonus.ClassListContains(LiveOpsHubClassNames.TimelineBarDropped), "huỷ kéo thì trả về đúng trạng thái model");
            Assert.AreEqual(1, VisibleOverlapLabelCount(lane), "huỷ kéo thì nhãn chồng giờ của model trở lại");
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ UX-16 meta làn

        /// <summary>
        /// (UX-16, V2 / UJ-19) Header làn rộng 168px: chip "Không đặt được (n)" không co, nên hàng meta chỉ còn ~33px và câu
        /// nhịp của làn cụt thành "cố đị…". Chip phải nhường chỗ (xuống dòng) để meta còn đủ bề rộng đọc được vế nhịp.
        /// </summary>
        [UnityTest]
        public IEnumerator LaneMeta_KeepsRoom_WhenUnplaceableChipShows()
        {
            yield return Open();
            LiveOpsTimelineLaneHeader header = Element.FindHeader("lava-quest");
            Assert.IsNotNull(header, "tiền đề: có làn lava-quest");
            Assert.IsFalse(header.UnplaceableChip.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                "tiền đề: lava-quest có đợt không đặt được nên chip hiện");

            yield return TimelineTestPanel.WaitUntil(() => TimelineTestPanel.HasLayout(header.MetaLabel),
                "nhãn meta phải có layout thật");
            Assert.GreaterOrEqual(header.MetaLabel.layout.width, 96f,
                "meta làn phải còn ≥ 96px khi có chip — chip nuốt chỗ là câu nhịp cụt thành \"cố đị…\" (V2)");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (R-01) Hồi quy của chính UX-16: chip xuống dòng đẩy header lên ba dòng (~49px) trong khi làn bị ép
        /// <c>style.height = LaneHeight</c> (~30px). Nền làn vẽ theo <c>contentRect</c> nên đáy hàng ~18px không được vẽ — mất cột
        /// cuối tuần, vạch ngày và vạch "bây giờ" ở dải đó, đúng loại "dải lạ" mà đợt này đang diệt. Nền làn phải phủ HẾT hàng.
        /// </summary>
        [UnityTest]
        public IEnumerator LaneTrack_FillsWholeRow_WhenHeaderIsTaller()
        {
            yield return Open();
            for (int index = 0; index < Element.LaneCount; index++)
            {
                LiveOpsTimelineLane lane = Element.LaneAt(index);
                VisualElement row = Element.RowAt(index);
                LiveOpsTimelineLaneHeader header = Element.HeaderAt(index);
                yield return TimelineTestPanel.WaitUntil(() => TimelineTestPanel.HasLayout(lane) && TimelineTestPanel.HasLayout(row),
                    "làn và hàng phải có layout thật");
                Assert.GreaterOrEqual(lane.layout.height, row.layout.height - LayoutTolerance,
                    "nền làn \"" + lane.Model.TypeId + "\" cao " + lane.layout.height + "px trong hàng " + row.layout.height +
                    "px — đáy hàng không được vẽ (R-01)");
                Assert.GreaterOrEqual(lane.layout.height, header.layout.height - LayoutTolerance,
                    "nền làn phải cao ít nhất bằng header của chính nó (R-01)");
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (R-04, UJ-19) Vế nhịp của làn lặp KHÔNG có chip: <c>flex-wrap</c> và <c>min-width</c> của UX-16 không chạm tới nó, mà
        /// câu tiếng Anh "repeats every 24 hours · runs 20 hours" (~200px) dài gấp rưỡi chỗ trống của header 168px. Chữ phải
        /// xuống dòng chứ không bị cắt — soát ở CẢ HAI ngôn ngữ vì bản tiếng Việt cũng dài hơn chỗ trống.
        /// </summary>
        [UnityTest]
        public IEnumerator LaneMeta_IsNotCut_OnLaneWithoutChip_BothLanguages()
        {
            foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
            {
                using (LiveOpsHubLanguage.Override(language))
                {
                    yield return Open();
                    LiveOpsTimelineLaneHeader header = Element.FindHeader("sky-race");
                    Assert.IsNotNull(header, language + ": tiền đề có làn sky-race");
                    Assert.IsTrue(header.UnplaceableChip.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                        language + ": tiền đề làn sky-race không có chip \"Không đặt được\"");
                    Assert.Greater(header.MetaLabel.text.Length, 0, language + ": tiền đề meta có chữ");

                    Label meta = header.MetaLabel;
                    yield return TimelineTestPanel.WaitUntil(() => TimelineTestPanel.HasLayout(meta), "nhãn meta phải có layout thật");
                    // (R-11) Bề rộng chữ đo bằng chính bộ chữ của panel — một Label song sinh không giới hạn bề rộng — chứ KHÔNG
                    // bằng ước lượng 5,6px/ký tự mà code cũng đang dùng: đo lại bằng cùng công thức thì code và test cùng sai một
                    // hướng và vẫn xanh. Tiếng Việt ký tự hẹp hơn nên ước lượng đó báo tràn ở chỗ thật ra vẫn vừa.
                    Label probe = CreateSingleLineProbe(meta);
                    yield return TimelineTestPanel.WaitUntil(() => TimelineTestPanel.HasLayout(probe), "nhãn đo phải có layout thật");
                    float needed = probe.layout.width;
                    probe.RemoveFromHierarchy();
                    // Chiều cao MỘT dòng đo lại từ chính panel (nhãn meta thấp nhất trong khung), không gõ cứng: cỡ chữ 10px ra
                    // chiều cao dòng khác nhau giữa hai bản Unity và hai phông.
                    float lines = Mathf.Max(1f, Mathf.Round(meta.layout.height / ShortestMetaHeight()));
                    float room = meta.layout.width * lines;
                    Assert.LessOrEqual(needed, room + LayoutTolerance,
                        language + ": meta làn \"" + meta.text + "\" cần " + needed + "px mà chỉ có " + room + "px (" + lines +
                        " dòng × " + meta.layout.width + "px) — phải xuống dòng, không được cắt (R-04)");
                    Close();
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (R-06, UJ-08) Làn "chưa ghi loại" không thu gọn được (không có TypeId để nhớ trạng thái). Để lại chevron nhận chuột,
        /// có <c>cursor: link</c> và tooltip "Thu gọn làn" rồi nuốt cú bấm chính là UJ-08 chỉ đổi chỗ — nút hứa hành động rồi
        /// không làm gì. Chevron của làn đó phải biến mất như swatch.
        /// </summary>
        [UnityTest]
        public IEnumerator UntypedLane_HasNoChevronToClick()
        {
            LiveEventCalendarDocumentBuilder builder = TimelineDesignSampleInput.CopyOf(LiveOpsDesignSample.Document);
            builder.WithFixedEvent(new FixedLiveEventEntry("entry-untyped", "quest-0915", string.Empty,
                "2026-09-15T00:00:00Z", "2026-09-16T00:00:00Z", string.Empty));
            yield return Open(builder.Build());

            LiveOpsTimelineLaneHeader untyped = Element.FindHeader(string.Empty);
            Assert.IsNotNull(untyped, "tiền đề: có làn chưa ghi loại");
            Assert.IsTrue(untyped.Chevron.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                "chevron của làn chưa ghi loại phải ẩn — nó không thu gọn được (R-06)");

            LiveOpsTimelineLaneHeader typed = Element.FindHeader("treasure-hunt");
            Assert.IsNotNull(typed, "tiền đề: vẫn có làn có loại để so");
            Assert.IsFalse(typed.Chevron.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                "làn có loại vẫn phải giữ chevron bấm được (UX-13)");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (R-09) Nhãn tháng né cờ "bây giờ" bằng cách dời sang PHẢI; khi cờ rơi sát mốc tháng gần mép phải track, nhãn duy nhất
        /// nói NĂM bị đẩy hẳn ra ngoài khung và mất sạch — tệ hơn lúc chưa né (trước đó còn ló một phần). Nhãn còn hiện thì phải
        /// còn đọc được trong track; hết chỗ thì bỏ hẳn theo luật chồng nhãn, chứ không vẽ ngoài khung.
        /// </summary>
        [UnityTest]
        public IEnumerator RulerMonthLabel_StaysInsideTrack_WhenNowFlagSitsOnIt()
        {
            // Khung bắt đầu 22/8 nên mốc tháng 10 rơi ở ~92% bề rộng track, và "bây giờ" đứng ngay trên mốc đó: né sang phải là
            // nhãn ra hẳn ngoài khung.
            yield return Open(zoom: LiveOpsTimelineZoom.Month,
                rangeStartUtc: new DateTime(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc),
                nowUtc: new DateTime(2026, 10, 1, 2, 0, 0, DateTimeKind.Utc));
            Assert.IsFalse(Element.Ruler.NowFlag.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "tiền đề: cờ bây giờ hiện");

            Rect track = Element.Ruler.Track.worldBound;
            IReadOnlyList<Label> months = Element.Ruler.VisibleMonthLabels;
            foreach (Label label in months)
            {
                Rect box = label.worldBound;
                float insideWidth = Mathf.Min(box.xMax, track.xMax) - Mathf.Max(box.xMin, track.xMin);
                Assert.GreaterOrEqual(insideWidth, 0.5f * box.width,
                    "nhãn tầng tháng \"" + label.text + "\" chỉ còn " + insideWidth + "/" + box.width +
                    "px trong track — né cờ mà tràn khỏi khung là mất hẳn (R-09)");
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (R-12) Kéo VÀO chỗ chồng giờ: thanh phải mang viền "sẽ bị bỏ" của cử chỉ đang làm, KHÔNG mang gạch ngang "bị bỏ" —
        /// đó là kết luận của bản kiểm, chưa có lúc này (xem <c>LiveOpsTimelineBar.SetDroppedPreview</c>). Tag bên phải thanh là
        /// chỗ nói "sẽ bị bỏ", không phải gạch ngang.
        /// </summary>
        [UnityTest]
        public IEnumerator DragIntoOverlap_ShowsWillDropWithoutStrike()
        {
            yield return Open();
            LiveOpsTimelineBar early = Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey);
            Assert.IsNotNull(early, "tiền đề: có thanh hunt-0914");
            Assert.IsFalse(early.Model.IsDropped, "tiền đề: hunt-0914 KHÔNG bị bỏ trong model");

            Vector2 start = early.worldBound.center;
            float threeDays = (float)(Element.PixelsPerHour * 72d);
            _panel.SendMouse(EventType.MouseDown, start);
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(threeDays / 2f, 0f));
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(threeDays, 0f));
            yield return null;

            Assert.IsTrue(Element.DragController.IsDragging, "tiền đề: đang kéo");
            CollectionAssert.Contains(Element.DragController.WillDropBarKeys, LiveOpsDesignSample.HuntEarlyEntryKey,
                "tiền đề: kéo hunt-0914 qua sau bonus thì chính nó là đợt muộn hơn nên sẽ bị bỏ");

            Assert.IsTrue(early.ClassListContains(LiveOpsHubClassNames.TimelineBarWillDrop), "thanh phải mang viền \"sẽ bị bỏ\"");
            Assert.IsFalse(early.ClassListContains(LiveOpsHubClassNames.TimelineBarDropped),
                "\"sẽ bị bỏ\" và \"bị bỏ\" là hai thứ khác nhau — xem trước không được nói trước kết luận của bản kiểm (R-12)");
            Assert.IsTrue(early.Strike.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                "gạch ngang \"bị bỏ\" không được bật lúc mới kéo vào chỗ chồng (R-12)");

            _panel.SendKey("escape");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ UX-17 thước

        /// <summary>
        /// (UX-17, V9) Zoom Tháng, khung bắt đầu đúng ngày hôm nay: cờ "08:47" nằm ngay trên nhãn tháng ở x = 0 và che mất
        /// "THÁNG 9 2026" — nhãn tháng là thứ DUY NHẤT nói ra năm. Nhãn tháng phải né cờ chứ không nằm dưới nó.
        /// </summary>
        [UnityTest]
        public IEnumerator RulerMonthLabel_DoesNotSitUnderNowFlag()
        {
            yield return Open(zoom: LiveOpsTimelineZoom.Month,
                rangeStartUtc: new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc));
            Assert.IsFalse(Element.Ruler.NowFlag.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "tiền đề: cờ bây giờ hiện");
            Rect flag = Element.Ruler.NowFlag.worldBound;

            IReadOnlyList<Label> months = Element.Ruler.VisibleMonthLabels;
            Assert.Greater(months.Count, 0, "tiền đề: thước có nhãn tầng tháng");
            foreach (Label label in months)
            {
                Rect box = label.worldBound;
                Assert.IsFalse(box.Overlaps(flag),
                    "nhãn tầng tháng \"" + label.text + "\" nằm dưới cờ bây giờ — phải né cờ hoặc bỏ nhãn (V9)");
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (UX-17, UJ-19) Nhãn ngày cắt theo bề rộng ô của nó. Ở cửa sổ 700px + tiếng Anh, ô một ngày chỉ ~25px mà "Mon 14"
        /// cần ~35px, nên nhãn đọc ra "Mon 1" — sai NGÀY chứ không chỉ xấu. Nhãn phải đo chữ thật và rút về số ngày khi thiếu chỗ.
        /// </summary>
        [UnityTest]
        public IEnumerator RulerDayLabels_FitTheirCell_InEnglish()
        {
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
            {
                yield return Open(width: NarrowWindowWidth, height: NarrowWindowHeight);
                IReadOnlyList<Label> days = Element.Ruler.VisibleDayLabels;
                Assert.Greater(days.Count, 0, "tiền đề: thước có nhãn ngày");
                AssertDayLabelsFitTheirCell("tiếng Anh, 700px");
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (R-08) Bậc rút gọn CUỐI của nhãn ngày ("14") cũng phải được soát: bản trước trả thẳng bậc cuối không kiểm, nên ô hẹp
        /// hơn ~11px vẫn vẽ rồi để <c>overflow: hidden</c> cắt "14" thành "1" — đọc ra một NGÀY KHÁC, đúng lỗi UX-17 muốn diệt.
        /// Quét cả ma trận cỡ cửa sổ × hai mức zoom × hai ngôn ngữ để ô hẹp nhất chắc chắn có mặt.
        /// </summary>
        [UnityTest]
        public IEnumerator RulerDayLabels_FitOrHide_AcrossWindowSizesAndZooms()
        {
            LiveOpsTimelineZoom[] zooms = { LiveOpsTimelineZoom.ThreeWeeks, LiveOpsTimelineZoom.Month };
            foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
            {
                using (LiveOpsHubLanguage.Override(language))
                {
                    foreach (Vector2 size in WindowSizeMatrix)
                    {
                        foreach (LiveOpsTimelineZoom zoom in zooms)
                        {
                            yield return Open(zoom: zoom, width: size.x, height: size.y);
                            AssertDayLabelsFitTheirCell(language + ", " + size.x + "×" + size.y + ", " + zoom);
                            Close();
                        }
                    }
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (R-07) Kiểm bố cục ở cả sáu cỡ cửa sổ cho phần timeline: nền làn phủ hết hàng, nhãn tên làn còn bề rộng đọc được
        /// (chevron rộng thêm 7px ăn vào đó), thân timeline và minimap không tụt về 0, track không âm.
        /// </summary>
        [UnityTest]
        public IEnumerator TimelineLayout_HoldsAcrossSixWindowSizes()
        {
            foreach (Vector2 size in WindowSizeMatrix)
            {
                yield return Open(width: size.x, height: size.y);
                string where = size.x + "×" + size.y;
                Assert.Greater(Element.Body.layout.height, 0f, where + ": thân timeline cao 0 — không còn thanh nào bấm được");
                Assert.Greater(Element.Minimap.layout.height, 0f, where + ": minimap cao 0");
                Assert.Greater(Element.LaneCount, 0, where + ": không vẽ làn nào");
                for (int index = 0; index < Element.LaneCount; index++)
                {
                    LiveOpsTimelineLane lane = Element.LaneAt(index);
                    VisualElement row = Element.RowAt(index);
                    LiveOpsTimelineLaneHeader header = Element.HeaderAt(index);
                    Assert.GreaterOrEqual(lane.layout.height, row.layout.height - LayoutTolerance,
                        where + ": nền làn \"" + lane.Model.TypeId + "\" không phủ hết hàng (R-01)");
                    Assert.Greater(lane.layout.width, 0f, where + ": track của làn rộng 0");
                    Assert.GreaterOrEqual(header.NameLabel.layout.width, MinimumReadableLabelWidth,
                        where + ": nhãn tên làn chỉ còn " + header.NameLabel.layout.width + "px — chevron 16px ăn hết chỗ (R-07)");
                }
                Close();
            }
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ UX-18 readout

        /// <summary>
        /// (UX-18, T2) Vế chồng giờ của readout là một Label riêng bắt đầu bằng dấu ngăn " · "; UI Toolkit bỏ khoảng trắng ĐẦU
        /// khi dựng chữ nên người dùng đọc ra "UTC· chồng". Dấu ngăn phải dính vào chữ (không phải khoảng trắng đầu) và khe
        /// trái phải do USS giữ.
        /// </summary>
        [UnityTest]
        public IEnumerator ReadoutOverlap_SeparatorSurvivesLayout()
        {
            yield return Open();
            LiveOpsTimelineBar early = Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey);
            Assert.IsNotNull(early, "tiền đề: có thanh hunt-0914");
            Vector2 start = early.worldBound.center;
            float oneHour = (float)Element.PixelsPerHour;
            _panel.SendMouse(EventType.MouseDown, start);
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(oneHour * 6f, 0f));
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(oneHour * 12f, 0f));
            yield return null;

            Assert.IsFalse(Element.ReadoutOverlap.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                "tiền đề: kéo hunt-0914 chồng lên bonus nên có vế chồng giờ");
            string text = Element.ReadoutOverlap.text;
            Assert.IsFalse(text.StartsWith(" ", StringComparison.Ordinal),
                "vế chồng giờ không được bắt đầu bằng khoảng trắng — UI Toolkit bỏ nó và người dùng đọc \"UTC· chồng\" (T2)");
            Assert.IsTrue(text.StartsWith("·", StringComparison.Ordinal), "vế chồng giờ phải mở đầu bằng dấu ngăn");
            Assert.GreaterOrEqual(Element.ReadoutOverlap.resolvedStyle.marginLeft, 4f - LayoutTolerance,
                "khe trước dấu ngăn phải do USS giữ khi chữ không giữ được");

            _panel.SendKey("escape");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (UX-18, T3 · R-02, R-03) Readout bám mép đang kéo và đặt PHÍA TRÊN thanh. Ở làn TRÊN CÙNG chỗ "phía trên" là hàng dấu
        /// của thước (khung 6); ở làn giữa khung nó là làn BÊN TRÊN, và readout che thanh của làn đó cùng nhãn "chồng 12 giờ"
        /// (khung 5, 7). Test kéo thanh chồng giờ của <c>treasure-hunt</c> — cảnh có thanh ở nhiều làn quanh nó — rồi soát ba
        /// điều mà CHỈ nhánh chọn chỗ mới cho: không đè thước, không đè thanh đang kéo, không đè thanh nào khác đang vẽ.
        ///
        /// (R-02) Bản trước dùng <c>FirstDraggableBarOfTopLane</c> nên cử chỉ kéo không mở được trên code cũ (chết ở dòng tiền
        /// đề, chưa chạy tới assert lỗi), và câu assert duy nhất của nó — <c>readout.yMin >= ruler.yMax</c> — lại đúng bằng
        /// dòng kẹp <c>Math.Max(rulerBottom, top)</c> nên không thể đỏ dù xoá hẳn nhánh lật. Ba câu dưới đây đỏ ngay khi bỏ
        /// nhánh chọn chỗ: readout rơi về ngay trên thanh đang kéo.
        /// </summary>
        [UnityTest]
        public IEnumerator Readout_CoversNeitherRulerNorAnyBar()
        {
            yield return Open();
            // Kéo bonus LÙI 3 ngày: nó nằm ở hàng phụ thứ hai của làn treasure-hunt, nên chỗ "trên thanh" của thiết kế rơi đúng
            // vào hàng phụ thứ nhất — chỗ thanh hunt-0914 đang đứng. Đây là cảnh Hình 12 khung 5 và 7: readout che thanh bên cạnh.
            LiveOpsTimelineBar dragged = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.IsNotNull(dragged, "tiền đề: có thanh hunt-0916-bonus");
            Vector2 start = dragged.worldBound.center;
            float threeDays = (float)(Element.PixelsPerHour * 72d);
            _panel.SendMouse(EventType.MouseDown, start);
            _panel.SendMouse(EventType.MouseDrag, start - new Vector2(threeDays / 2f, 0f));
            _panel.SendMouse(EventType.MouseDrag, start - new Vector2(threeDays, 0f));
            yield return null;

            Assert.IsTrue(Element.DragController.IsDragging, "tiền đề: đang kéo");
            Assert.IsFalse(Element.Readout.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "tiền đề: readout hiện");
            yield return TimelineTestPanel.WaitUntil(() => TimelineTestPanel.HasLayout(Element.Readout),
                "readout phải có layout thật mới đo được chỗ đặt");

            Rect readout = Element.Readout.worldBound;
            Assert.GreaterOrEqual(readout.yMin, Element.Ruler.worldBound.yMax - LayoutTolerance,
                "readout đè hàng thước — phải lật xuống dưới thanh khi phía trên là thước (T3, khung 6)");
            Assert.IsFalse(Overlaps(readout, dragged.worldBound),
                "readout đè chính thanh đang kéo — thanh là thứ người dùng đang nhìn (T3)");
            foreach (LiveOpsTimelineBar bar in VisibleBars())
            {
                Assert.IsFalse(Overlaps(readout, bar.worldBound),
                    "readout đè thanh \"" + bar.Model.BarKey + "\" của làn khác — phải né hoặc ở lại trong hàng đang kéo (T3, khung 5/7)");
            }

            _panel.SendKey("escape");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ UX-19 chú giải

        /// <summary>
        /// (UX-19, V13) Skin sáng: mẫu "nhạt = đã khép" và mẫu "thanh phẳng" chỉ khác nền cửa sổ bằng nền nút mờ, đo ra
        /// 1,00:1 — người dùng thấy chữ mà không thấy ký hiệu. Mỗi mẫu phải mang dải màu đáy như thanh thật để luôn có một
        /// vệt đọc được trên cả hai skin.
        /// </summary>
        [UnityTest]
        public IEnumerator LegendSamples_HaveColorBand()
        {
            yield return Open();
            List<VisualElement> samples = Element.Legend.Query<VisualElement>(className: LiveOpsHubClassNames.TimelineLegendSample).ToList();
            Assert.AreEqual(LiveOpsTimelineLegend.ItemCount - 1, samples.Count, "tiền đề: chú giải có đủ mẫu (mục cuối dùng dấu, không dùng mẫu)");
            int overlapSamples = 0;
            foreach (VisualElement sample in samples)
            {
                VisualElement stripe = sample.Q<VisualElement>(className: LiveOpsHubClassNames.TimelineBarStripe);
                if (sample.ClassListContains(LiveOpsHubClassNames.TimelineLegendSampleOverlap))
                {
                    // (R-10) Mẫu vùng chồng giờ giải thích MẢNG NỀN gạch chéo, không phải thanh: mang dải màu loại là nói sai
                    // thứ nó giải thích. Nó đọc được nhờ viền + nền blocked của chính nó.
                    overlapSamples++;
                    Assert.IsNull(stripe, "mẫu vùng chồng giờ không được mang dải màu loại của thanh (R-10)");
                    continue;
                }
                Assert.IsNotNull(stripe, "mẫu chú giải hình thanh phải có dải màu đáy như thanh thật (V13)");
                Assert.Greater(stripe.resolvedStyle.backgroundColor.a, 0.5f, "dải màu đáy của mẫu phải đặc, không trong suốt");
                Assert.GreaterOrEqual(stripe.resolvedStyle.height, 2f, "dải màu đáy phải cao ≥ 2px mới đọc được");
            }
            Assert.AreEqual(1, overlapSamples, "tiền đề: chú giải có đúng một mẫu vùng chồng giờ");
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ UX-31

        /// <summary>
        /// (UX-31, T11) Làn thu gọn phải vẽ DẢI đặc 6px — trước đợt thanh giữ nguyên viền 1px của thanh thường nên 6px đó
        /// gần như chỉ còn viền, đọc thành "thanh mini" chứ không phải dải trạng thái.
        /// </summary>
        [UnityTest]
        public IEnumerator CollapsedLaneBar_IsSolidBand_WithoutBorder()
        {
            yield return Open(collapsedLanes: new[] { "treasure-hunt" });

            LiveOpsTimelineLane lane = Element.FindLaneElement("treasure-hunt");
            Assert.IsNotNull(lane, "tiền đề: có làn treasure-hunt");
            Assert.IsTrue(lane.Model.IsCollapsed, "tiền đề: làn đang thu gọn");
            Assert.Greater(lane.BarCount, 0, "tiền đề: làn thu gọn vẫn có thanh");
            LiveOpsTimelineBar bar = lane.BarAt(0);
            Assert.IsTrue(bar.IsCollapsed, "tiền đề: thanh ở dạng thu gọn");

            Assert.LessOrEqual(bar.resolvedStyle.borderTopWidth, 0f, "dải làn thu gọn không được có viền (T11)");
            Assert.LessOrEqual(bar.resolvedStyle.borderBottomWidth, 0f, "dải làn thu gọn không được có viền (T11)");
            Assert.GreaterOrEqual(bar.Stripe.resolvedStyle.height, LiveOpsTimelineLane.CollapsedBarHeight - LayoutTolerance,
                "dải màu phải lấp hết 6px của làn thu gọn");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (UX-31, UJ-24) Thanh bị bỏ hẹp (hunt-0916-bonus ~45px) không đủ chỗ cho nhãn id nên chỉ còn gạch ngang: người dùng
        /// thấy "một đợt nào đó bị bỏ" mà không biết đợt nào. Tag "bị bỏ" bên phải thanh phải nói ra id.
        /// </summary>
        [UnityTest]
        public IEnumerator NarrowDroppedBar_TagCarriesEventId()
        {
            yield return Open();
            LiveOpsTimelineBar bonus = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.IsTrue(bonus.Model.IsDropped, "tiền đề: bonus bị bỏ");
            Assert.AreEqual(string.Empty, bonus.Label.text, "tiền đề: thanh hẹp nên không có nhãn id trong thân");

            LiveOpsTimelineLane lane = Element.FindLaneElement("treasure-hunt");
            List<VisualElement> tags = VisibleDroppedTags(lane);
            Assert.AreEqual(1, tags.Count, "tiền đề: có đúng một tag bị bỏ");
            StringAssert.Contains(bonus.Model.EventId, tags[0].Q<Label>().text,
                "tag bị bỏ của thanh hẹp phải nói ra id đợt (UJ-24)");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (UX-31, T11) Dải gom ở zoom Tháng: câu đầy đủ "sky-race · 42 đợt · 20 giờ/ngày" không vừa nên nhãn về rỗng và số
        /// đợt biến mất hẳn. Phải còn ít nhất CON SỐ — đó là thứ duy nhất nói dải này gom bao nhiêu đợt.
        /// </summary>
        [Test]
        public void StripLabel_KeepsCount_WhenTooNarrowForFullText()
        {
            LiveOpsTimelineBarModel strip = new LiveOpsTimelineBarModel("sky-race#strip#0", "sky-race-252…293", "sky-race",
                LiveOpsTimelineBarSource.RecurringStrip, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc), 0, 42, LiveEventPhase.Upcoming, false, false, false, false,
                string.Empty, HealthState.Ok, 400d);

            string wide = LiveOpsTimelineBar.LabelTextFor(strip, 300f);
            StringAssert.Contains("42", wide, "tiền đề: đủ chỗ thì nhãn có số đợt");

            string narrow = LiveOpsTimelineBar.LabelTextFor(strip, 40f);
            StringAssert.Contains("42", narrow, "hẹp tới đâu cũng phải giữ số đợt của dải (T11)");
        }

        /// <summary>
        /// (UX-31, T7) Dòng gợi ý dựng bằng hai Label: id làn (đậm) rồi phần mô tả. Mọi câu mô tả khác đều mở đầu bằng "· ";
        /// riêng câu làn trống thiếu dấu nên đọc liền thành "star-tournament14/9 → 19/9".
        /// </summary>
        [Test]
        public void HintEmptyLane_StartsWithSeparator_BothLanguages()
        {
            foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
            {
                using (LiveOpsHubLanguage.Override(language))
                {
                    string text = LiveOpsHubStrings.TimelineHintEmptyLaneFormat;
                    Assert.IsTrue(text.StartsWith("· ", StringComparison.Ordinal),
                        language + ": câu gợi ý làn trống phải mở đầu bằng dấu ngăn như mọi câu mô tả khác (T7) — đang là \"" + text + "\"");
                }
            }
        }

        // ================================================================================================ phụ trợ

        /// <summary>
        /// Label song sinh của <paramref name="source"/>: cùng class, đặt tuyệt đối ngoài luồng layout và ép một dòng, nên
        /// <c>layout.width</c> của nó là bề rộng THẬT mà chữ cần — đo bằng chính bộ chữ đang vẽ, không bằng ước lượng.
        /// </summary>
        private Label CreateSingleLineProbe(Label source)
        {
            Label probe = new Label(source.text) { pickingMode = PickingMode.Ignore };
            foreach (string className in source.GetClasses()) probe.AddToClassList(className);
            probe.style.position = Position.Absolute;
            probe.style.whiteSpace = WhiteSpace.NoWrap;
            probe.style.left = 0f;
            probe.style.top = 0f;
            probe.style.minWidth = 0f;
            probe.style.maxWidth = StyleKeyword.None;
            Element.Add(probe);
            return probe;
        }

        /// <summary>Chiều cao nhãn meta thấp nhất trong khung = chiều cao MỘT dòng; dùng để suy meta nào đã xuống dòng (R-04).</summary>
        private float ShortestMetaHeight()
        {
            float shortest = float.PositiveInfinity;
            for (int index = 0; index < Element.LaneCount; index++)
            {
                float height = Element.HeaderAt(index).MetaLabel.layout.height;
                if (height > 0f && height < shortest) shortest = height;
            }
            Assert.IsFalse(float.IsInfinity(shortest), "tiền đề: có ít nhất một nhãn meta đã layout");
            return shortest;
        }

        /// <summary>Mọi nhãn ngày đang hiện phải lọt ô của nó — bậc nào cũng không lọt thì thước phải ẩn nhãn, không vẽ nửa con số.</summary>
        private void AssertDayLabelsFitTheirCell(string where)
        {
            foreach (Label label in Element.Ruler.VisibleDayLabels)
            {
                float room = label.contentRect.width;
                if (float.IsNaN(room) || room <= 0f) continue;
                Assert.Greater(label.text.Length, 0, where + ": nhãn ngày đang hiện mà rỗng — phải ẩn hẳn (R-08)");
                // Đo bằng CÙNG công thức thước đang dùng cho tầng tháng (LabelCharacterWidth): một con số cho cả hai bản Unity
                // và cả hai skin, thay vì MeasureTextSize — API đó không cùng tên kiểu ở 2022.3 và 6000.6. Cổng L của gói G đo
                // lại phần cắt chữ TRÊN ẢNH (measure-capture.py) để không cùng sai một hướng với code (R-11).
                float needed = label.text.Length * LiveOpsTimelineGeometry.LabelCharacterWidth;
                Assert.LessOrEqual(needed, room + LayoutTolerance,
                    where + ": nhãn ngày \"" + label.text + "\" cần " + needed + "px trong ô " + room +
                    "px — bị cắt thành số ngày sai (UJ-19, R-08)");
            }
        }

        /// <summary>Mọi thanh đang vẽ trên mọi làn — readout là lớp phủ nên nó che được cả thanh của làn khác (R-03).</summary>
        private List<LiveOpsTimelineBar> VisibleBars()
        {
            List<LiveOpsTimelineBar> bars = new List<LiveOpsTimelineBar>();
            for (int laneIndex = 0; laneIndex < Element.LaneCount; laneIndex++)
            {
                LiveOpsTimelineLane lane = Element.LaneAt(laneIndex);
                for (int barIndex = 0; barIndex < lane.BarCount; barIndex++) bars.Add(lane.BarAt(barIndex));
            }
            return bars;
        }

        /// <summary>Đè nhau THẬT: chạm mép nhau (chênh dưới một pixel làm tròn của Yoga) không tính là đè.</summary>
        private static bool Overlaps(Rect first, Rect second)
        {
            Rect shrunk = new Rect(second.x + LayoutTolerance, second.y + LayoutTolerance,
                Mathf.Max(0f, second.width - 2f * LayoutTolerance), Mathf.Max(0f, second.height - 2f * LayoutTolerance));
            return shrunk.width > 0f && shrunk.height > 0f && first.Overlaps(shrunk);
        }

        private static int VisibleOverlapLabelCount(LiveOpsTimelineLane lane)
        {
            int count = 0;
            foreach (Label label in lane.Query<Label>(className: LiveOpsHubClassNames.TimelineOverlapLabel).ToList())
            {
                if (!label.ClassListContains(LiveOpsHubClassNames.TimelineHidden)) count++;
            }
            return count;
        }

        private static List<VisualElement> VisibleDroppedTags(LiveOpsTimelineLane lane)
        {
            List<VisualElement> visible = new List<VisualElement>();
            foreach (VisualElement tag in lane.Query<VisualElement>(className: LiveOpsHubClassNames.TimelineDroppedTag).ToList())
            {
                if (!tag.ClassListContains(LiveOpsHubClassNames.TimelineHidden)) visible.Add(tag);
            }
            return visible;
        }

        private static int VisibleDroppedTagCount(LiveOpsTimelineLane lane)
        {
            return VisibleDroppedTags(lane).Count;
        }
    }
}
