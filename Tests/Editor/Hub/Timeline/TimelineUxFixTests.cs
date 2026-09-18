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
            string[] collapsedLanes = null)
        {
            _panel = TimelineTestPanel.Open(width, height);
            VisualElement root = _panel.CreateRoot(false);
            Func<LiveOpsTimelineInput> inputFactory = TimelineViewInputs.Checked(document ?? LiveOpsDesignSample.Document);
            if (collapsedLanes != null)
            {
                Func<LiveOpsTimelineInput> baseFactory = inputFactory;
                inputFactory = () => baseFactory().WithCollapsedLanes(collapsedLanes);
            }
            TimelineHarness harness = TimelineHarness.Create(root, inputFactory);
            harness.Start(rangeStartUtc ?? LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, zoom), zoom);
            _panel.Harness = harness;
            yield return harness.WaitReady();
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
                foreach (Label label in days)
                {
                    float room = label.contentRect.width;
                    if (float.IsNaN(room) || room <= 0f) continue;
                    // Đo bằng CÙNG công thức thước đang dùng cho tầng tháng (LabelCharacterWidth): một con số cho cả hai bản Unity
                    // và cả hai skin, thay vì MeasureTextSize — API đó không cùng tên kiểu ở 2022.3 và 6000.6.
                    float needed = label.text.Length * LiveOpsTimelineGeometry.LabelCharacterWidth;
                    Assert.LessOrEqual(needed, room + LayoutTolerance,
                        "nhãn ngày \"" + label.text + "\" cần " + needed + "px trong ô " + room + "px — bị cắt thành số ngày sai (UJ-19)");
                }
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
        /// (UX-18, T3) Readout bám mép đang kéo và đặt PHÍA TRÊN thanh; với làn đầu tiên, chỗ "phía trên" là hàng dấu của
        /// thước, nên readout đè lên thước. Nó phải lật xuống dưới thanh khi trên không còn chỗ.
        /// </summary>
        [UnityTest]
        public IEnumerator Readout_DoesNotCoverRuler()
        {
            yield return Open();
            LiveOpsTimelineBar firstLaneBar = FirstDraggableBarOfTopLane();
            Assert.IsNotNull(firstLaneBar, "tiền đề: làn trên cùng có một thanh kéo được");
            Vector2 start = firstLaneBar.worldBound.center;
            float sixHours = (float)(Element.PixelsPerHour * 6d);
            _panel.SendMouse(EventType.MouseDown, start);
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(sixHours / 2f, 0f));
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(sixHours, 0f));
            yield return null;

            Assert.IsTrue(Element.DragController.IsDragging, "tiền đề: đang kéo");
            Assert.IsFalse(Element.Readout.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "tiền đề: readout hiện");
            Rect readout = Element.Readout.worldBound;
            Rect ruler = Element.Ruler.worldBound;
            Assert.GreaterOrEqual(readout.yMin, ruler.yMax - LayoutTolerance,
                "readout đè hàng thước — phải lật xuống dưới thanh khi phía trên là thước (T3)");

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
            foreach (VisualElement sample in samples)
            {
                VisualElement stripe = sample.Q<VisualElement>(className: LiveOpsHubClassNames.TimelineBarStripe);
                Assert.IsNotNull(stripe, "mẫu chú giải phải có dải màu đáy như thanh thật (V13)");
                Assert.Greater(stripe.resolvedStyle.backgroundColor.a, 0.5f, "dải màu đáy của mẫu phải đặc, không trong suốt");
                Assert.GreaterOrEqual(stripe.resolvedStyle.height, 2f, "dải màu đáy phải cao ≥ 2px mới đọc được");
            }
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

        /// <summary>Thanh kéo được đầu tiên của làn TRÊN CÙNG — chỗ duy nhất mà "phía trên thanh" rơi đúng vào hàng thước.</summary>
        private LiveOpsTimelineBar FirstDraggableBarOfTopLane()
        {
            if (Element.LaneCount == 0) return null;
            LiveOpsTimelineLane lane = Element.LaneAt(0);
            for (int barIndex = 0; barIndex < lane.BarCount; barIndex++)
            {
                LiveOpsTimelineBar bar = lane.BarAt(barIndex);
                if (bar.IsDraggable) return bar;
            }
            return null;
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
