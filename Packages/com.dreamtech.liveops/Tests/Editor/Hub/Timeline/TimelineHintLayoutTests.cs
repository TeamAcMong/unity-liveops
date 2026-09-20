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
    /// (W9-29 + W9-25 chỗ #2 và #7) Ma trận BỐ CỤC của dòng gợi ý đáy trục và của nhãn tầng tháng trên thước — dựng đúng
    /// những trạng thái DỮ LIỆU XẤU NHẤT mà ma trận cổng W9 chưa bao giờ dựng tới, nên ba lỗi ảnh của đợt trước lọt lưới.
    /// <para>
    /// Vì sao phải mở cửa sổ hub THẬT chứ không dùng <see cref="TimelineTestPanel"/> cho phần gợi ý: trong panel thử, trục
    /// chiếm trọn bề rộng cửa sổ (700px ⇒ trục 700px), còn trong hub thật trục còn 384px ở cùng cỡ (rail + đệm màn). Câu
    /// gợi ý "đợt bị bỏ" cần 402px (vi) / 502px (en) — nó vừa ở trục 700 và KHÔNG vừa ở trục 384. Đo trong panel là đo một
    /// cửa sổ không có thật và không lỗi nào hiện ra.
    /// </para>
    /// <para>
    /// CHÍN biến thể — bản đầu chỉ dựng bốn, phiếu soát W10 F3 bắt đúng chỗ đó: NGHỈ (câu DÀI NHẤT của hub hôm nay — bản
    /// tiếng Anh 120 ký tự, dài hơn cả câu "đợt bị bỏ" 100 ký tự), LÀN TRỐNG (chủ ngữ = TÊN LÀN, lấy làn có id dài nhất),
    /// ĐANG KÉO (câu có Shift/Alt/Esc), đợt cố định sắp diễn ra (câu có ⌘D), đợt BỊ BỎ (câu có ⌘⌦), đợt ĐÃ KHÉP (chủ ngữ
    /// `lava-quest-2026-09a` — đúng chỗ #2 của phiếu W9-25), ID DÀI NHẤT (54 ký tự) và CHỌN NHIỀU (câu có ⌘-click ·
    /// Shift-click, chủ ngữ là tên làn dài nhất). LÀN TRỐNG đo HAI lần — một lần khi chưa chọn gì (dòng gợi ý rộng) và một
    /// lần khi đang chọn một đợt (pane inspector mở ra, dòng gợi ý hẹp lại). Mọi biến thể chạy ở bảy cỡ × hai ngôn ngữ.
    /// </para>
    /// <para>
    /// Ba nhánh còn lại của <see cref="LiveOpsTimelineHintLine.ShowSelected"/> — đang chạy, sinh từ luật, dải gom — KHÔNG
    /// dựng riêng, và đây là lý do chứ không phải bỏ sót: cả ba dùng chủ ngữ cùng loại (id đợt, đã đo ở bản DÀI NHẤT) và câu
    /// mô tả của chúng là ba câu NGẮN NHẤT của dòng gợi ý (26–33 ký tự, không có vế phím tắt), tức luôn nằm trong cái mà
    /// biến thể dài nhất đã phủ. Thêm chúng chỉ làm lượt chạy dài ra mà không đo thêm bề rộng nào.
    /// </para>
    /// <para>
    /// Cả chín biến thể dùng CHUNG một cửa sổ cho mỗi cặp (ngôn ngữ, cỡ) và dựng trạng thái bằng đúng đường của người dùng
    /// (bấm, ⌘-bấm, kéo thật) hoặc bằng đường pose sẵn có của control (<see cref="LiveOpsTimelineElement.SetCursor"/> —
    /// internal từ trước cho kịch bản chụp, vì batchmode không bắt được con trỏ thật). Mở lại cửa sổ cho từng biến thể tốn
    /// gấp chín lần thời gian mà không đo thêm được gì: thứ đang đo là HỘP của dòng gợi ý ở một cỡ cửa sổ, không phải việc
    /// mở cửa sổ.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class TimelineHintLayoutTests
    {
        /// <summary>
        /// Id dài nhất của mẫu xấu nhất: 54 ký tự, dài hơn mọi id thật trong <see cref="LiveOpsDesignSample"/>. Dùng cho CẢ
        /// id đợt lẫn id LOẠI (tên làn) — hai thứ đi cùng một bảng chữ và cùng một ô nhập, nên trần độ dài là một.
        /// <para>
        /// (phiếu soát W10 F9) Sau khi gộp đợt W10, hằng này phải trỏ về <c>LiveOpsWorstCaseSample</c> của gói G-W10-MATRIX
        /// — file đó là chỗ đăng ký "dữ liệu xấu nhất" của cả hub. Chưa trỏ được ở gói này vì file thuộc quyền ghi của gói
        /// kia và chưa tồn tại trên nền <c>d797065</c>; giữ ở đây thì thành hai nguồn sự thật, nên đã ghi vào báo cáo đợt.
        /// </para>
        /// </summary>
        private const string LongestId = "treasure-hunt-autumn-festival-2026-week-38-bonus-round";

        private const string LongEntryKey = "entry-" + LongestId;
        private const string LongLaneFirstEntryKey = "entry-long-lane-first";
        private const string LongLaneSecondEntryKey = "entry-long-lane-second";

        /// <summary>Sai số một pixel của Yoga khi so hình học đã qua layout; phép đo chữ lệch nhiều hơn nên dùng riêng.</summary>
        private const float LayoutTolerance = 1f;

        /// <summary>
        /// Sai số RIÊNG cho nhánh đo chữ: nhãn đo (Label song sinh) và nhãn thật đi qua cùng bộ chữ nhưng khác lượt làm tròn
        /// theo pixelsPerPoint, nên 3px là nhiễu của phép đo chứ không phải chữ người dùng đọc thiếu.
        /// </summary>
        private const float TextMeasureTolerance = 3f;

        /// <summary>
        /// Trần chiều cao của dòng gợi ý — CÙNG MỘT SỐ với <c>max-height</c> của <c>.liveops-hub-timeline-hint</c>. Dòng gợi
        /// ý được phép cao thêm để giữ cụm phím (W9-29), nhưng không được nuốt trục. Test đòi CHƯA CHẠM trần (so sánh ngặt):
        /// chạm trần nghĩa là USS đã bắt đầu cắt chữ, và chữ bị cắt thì phải đỏ ở đây chứ không phải chỉ lộ trên ảnh.
        /// </summary>
        private const float MaximumHintHeight = 56f;

        /// <summary>Khoảng kéo của biến thể ĐANG KÉO: quá ngưỡng 4px của drag controller nhiều lần mà vẫn nằm trong trục.</summary>
        private const float DragDistance = 30f;

        /// <summary>Chừa mép khi kéo sang phải: điểm chuột ra ngoài cửa sổ thì sự kiện không tới được control.</summary>
        private const float DragEdgeMargin = 8f;

        /// <summary>
        /// Biến môi trường chứa ĐƯỜNG DẪN file để ghi số đo; rỗng (mặc định) = không ghi gì. Tắt mặc định vì cổng đợt chạy cả
        /// nghìn ca, thêm trăm dòng mỗi lượt là rác. Ghi ra FILE chứ không <c>Debug.Log</c>: <c>LogAssert.NoUnexpectedReceived</c>
        /// coi mọi dòng log không khai trước là lỗi, nên bật probe sẽ làm chính test đỏ. Dùng khi cần bằng chứng số — vd so số
        /// nhãn tuần trước/sau khi đổi bề rộng ô nhãn tầng 1 (phiếu soát W10 F7).
        /// </summary>
        private const string ProbeEnvironmentVariable = "LIVEOPS_HINT_PROBE";

        /// <summary>
        /// Bảy cỡ của ma trận kiểm bố cục (giống <c>UxHubWindowFixture.AllLayoutSizes</c>, chép lại để test này không phụ
        /// thuộc thứ tự mảng của cổng — cỡ ở đây là dữ liệu của gói, không phải hợp đồng chỉ mục).
        /// </summary>
        private static readonly UxWindowSize[] SizeMatrix =
        {
            new UxWindowSize(700, 560),
            new UxWindowSize(820, 560),
            new UxWindowSize(950, 700),
            new UxWindowSize(1024, 700),
            new UxWindowSize(1280, 760),
            new UxWindowSize(1440, 900),
            new UxWindowSize(1920, 1040),
        };

        private static readonly LiveOpsHubLanguageId[] Languages = { LiveOpsHubLanguageId.Vietnamese, LiveOpsHubLanguageId.English };

        private UxHubWindowFixture _fixture;
        private TimelineTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _fixture?.Dispose();
            _fixture = null;
            _panel?.Dispose();
            _panel = null;
        }

        // ================================================================================================ W9-29 dòng gợi ý

        /// <summary>
        /// (W9-29) Dòng gợi ý phải đọc được TRỌN VẸN ở cả bảy cỡ, cả hai ngôn ngữ, ở cả CHÍN trạng thái — kể cả biến thể dài
        /// nhất. Trước gói này, câu "đợt bị bỏ" bị ellipsis cắt từ cuối ở bốn cỡ, và thứ đứng cuối câu đúng là cụm phím
        /// ⌘⌦ / ⌘D — thứ mà dòng gợi ý là chỗ DUY NHẤT trong hub nói ra.
        /// </summary>
        [UnityTest]
        public IEnumerator HintLine_ReadsWhole_AtEverySize_EveryWorstCaseVariant_BothLanguages()
        {
            foreach (LiveOpsHubLanguageId language in Languages)
            {
                foreach (UxWindowSize size in SizeMatrix)
                {
                    yield return OpenHub(language, size, WorstCaseServices());
                    foreach (HintVariant variant in AllVariants)
                    {
                        yield return AssertHintVariant(language, size, variant);
                    }
                    CloseHub();
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (W9-29) Cụm phím là vế CUỐI câu, nên nó là thứ đầu tiên mất khi câu bị cắt. Test này khẳng định HAI vế cùng lúc ở
        /// cỡ hẹp nhất: câu ĐÃ GHÉP nhãn phím thật (đọc từ profile phím của Editor), và câu ấy ĐỌC ĐƯỢC TRỌN VẸN trong ô của
        /// nó. Vế sau mới là vế canh được phiếu W9-29 — chuỗi mô hình vốn vẫn đủ chữ ở bản cũ, bản cũ chỉ cắt lúc VẼ.
        /// <para>
        /// Không có binding thì không có vế phím nào để kiểm (<see cref="LiveOpsHubKeyLabels"/> trả "" khi id không tồn tại
        /// hoặc chưa gán phím): khi đó test BỎ QUA ra mặt bằng <c>Assert.Ignore</c>, không xanh im lặng.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator HintLine_KeepsShortcutCluster_AtNarrowestSize_BothLanguages()
        {
            string duplicateKey = LiveOpsHubKeyLabels.For("Main Menu/Edit/Duplicate");
            string deleteKey = LiveOpsHubKeyLabels.For("Main Menu/Edit/Delete");
            if (duplicateKey.Length == 0 && deleteKey.Length == 0)
            {
                Assert.Ignore("Editor này không gán phím cho cả Edit/Duplicate lẫn Edit/Delete — câu gợi ý không ghép vế phím "
                    + "nào để mà canh (LiveOpsHubKeyLabels trả \"\" đúng luật S-8).");
            }

            foreach (LiveOpsHubLanguageId language in Languages)
            {
                yield return OpenHub(language, SizeMatrix[0], null);
                LiveOpsTimelineElement timeline = TimelineOf(_fixture);
                if (duplicateKey.Length > 0)
                {
                    yield return SelectBar(LiveOpsDesignSample.HuntEarlyEntryKey, "hunt-0914");
                    StringAssert.Contains(duplicateKey + " " + LiveOpsHubStrings.TimelineHintDuplicate, timeline.HintLine.Description.text,
                        language + ": câu gợi ý của đợt cố định phải ghép cụm phím nhân bản");
                    yield return AssertLabelReadsWhole(timeline.HintLine.Description, timeline,
                        language + " cụm phím nhân bản ở cỡ hẹp nhất");
                }
                if (deleteKey.Length > 0)
                {
                    yield return SelectBar(LiveOpsDesignSample.HuntBonusEntryKey, "hunt-0916-bonus");
                    StringAssert.Contains(deleteKey + " " + LiveOpsHubStrings.TimelineHintDelete, timeline.HintLine.Description.text,
                        language + ": câu gợi ý của đợt bị bỏ phải ghép cụm phím xoá");
                    yield return AssertLabelReadsWhole(timeline.HintLine.Description, timeline,
                        language + " cụm phím xoá ở cỡ hẹp nhất");
                }
                CloseHub();
            }
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ W9-25 chỗ #7 nhãn tháng

        /// <summary>
        /// (W9-25 chỗ #7) Nhãn tầng tháng ("THÁNG 11 2026") ôm khít ô của nó — ô tự co theo chữ nên chỉ hụt 1px là cắt, và
        /// một mốc tháng bị cắt đọc ra "THÁNG 3 20", một mốc KHÔNG có thật. Quét bảy cỡ × hai ngôn ngữ với khoảng đang xem
        /// bắc qua ranh giới tháng 11 — đúng khoảng mà nhãn dài nhất của bản tiếng Việt xuất hiện.
        /// <para>
        /// Quét CẢ HAI mức thu nhỏ sinh tick tầng 1 (phiếu soát W10 F7): mức Tháng chỉ có nhãn tháng, còn mức Ba tuần mới có
        /// thêm nhãn "Tuần n" — nhãn tuần cũng đi qua <c>BindMonthTierLabels</c>, nên đổi số của tầng 1 là đổi cả nhãn tuần.
        /// Hai assert thêm cho chuyện đó: (1) ô nhãn KHÔNG thò khỏi mép track — điều kiện để ô được phép rộng hơn chỗ giữ;
        /// (2) mức Ba tuần phải còn ít nhất MỘT nhãn tuần.
        /// </para>
        /// <para>
        /// Vì sao sàn là 1 chứ không phải "số thứ Hai trừ một": số nhãn tuần hiện ra phụ thuộc bề rộng track và chỗ mà nhãn
        /// THÁNG chiếm — ở cửa sổ hẹp, bản trước gói cũng chỉ vẽ 1/3 nhãn tuần (đo trong plan/w10/logs/weekcount-old.txt),
        /// nên mọi sàn cao hơn đều là con số bịa. Bảo đảm "không mất nhãn tuần nào" là bảo đảm CẤU TRÚC: luật nhường chỗ vẫn
        /// tính bằng <c>MonthTierLabelCharacterWidth</c> y như trước W10, và đã đo đối chiếu 14/14 dòng
        /// (weekcount-old.txt so với weekcount-fixed.txt). Sàn 1 ở đây chỉ để chặn trường hợp tầng tuần biến mất sạch.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator RulerMonthLabels_FitTheirBox_AtEverySize_BothZooms_BothLanguages()
        {
            DateTime rangeStartUtc = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
            LiveOpsTimelineZoom[] zooms = { LiveOpsTimelineZoom.Month, LiveOpsTimelineZoom.ThreeWeeks };
            foreach (LiveOpsHubLanguageId language in Languages)
            {
                using (LiveOpsHubLanguage.Override(language))
                {
                    foreach (LiveOpsTimelineZoom zoom in zooms)
                    {
                        foreach (UxWindowSize size in SizeMatrix)
                        {
                            yield return OpenPanel(size, zoom, rangeStartUtc);
                            IReadOnlyList<Label> months = _panel.Harness.Element.Ruler.VisibleMonthLabels;
                            string context = language + " " + zoom + " " + size;
                            Assert.Greater(months.Count, 0, context + ": tiền đề thước có nhãn tầng tháng");
                            bool sawMonthMark = false;
                            int weekLabelCount = 0;
                            float trackRight = _panel.Harness.Element.Ruler.Track.worldBound.xMax;
                            for (int index = 0; index < months.Count; index++)
                            {
                                yield return AssertLabelReadsWhole(months[index], _panel.Harness.Element, context + " nhãn thước");
                                // (W9-25 chỗ #7) Ô của nhãn tầng 1 rộng hơn chỗ giữ, nên phải canh riêng việc nó KHÔNG thò khỏi
                                // track: track cắt con, phần thò ra là childOverflow thật và cổng bố cục sẽ đỏ vì nó.
                                Assert.LessOrEqual(months[index].worldBound.xMax, trackRight + LayoutTolerance,
                                    context + " nhãn \"" + months[index].text + "\": ô nhãn thò "
                                    + (months[index].worldBound.xMax - trackRight) + "px khỏi mép phải của track");
                                if (months[index].ClassListContains(LiveOpsHubClassNames.TimelineRulerLabelEmphasized)) sawMonthMark = true;
                                else weekLabelCount++;
                            }
                            Assert.IsTrue(sawMonthMark,
                                context + ": tiền đề khoảng đang xem phải bắc qua một mốc THÁNG (nhãn nhấn mạnh)");
                            if (zoom == LiveOpsTimelineZoom.ThreeWeeks)
                            {
                                Assert.GreaterOrEqual(weekLabelCount, 1, context + ": mức Ba tuần mà KHÔNG còn nhãn tuần nào "
                                    + "— luật nhường chỗ của tầng 1 đã nuốt sạch vạch tuần");
                            }
                            LogProbe("[HINTGATE] " + context + " nhãn tầng 1: tổng=" + months.Count + " tuần=" + weekLabelCount);
                            ClosePanel();
                        }
                    }
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ phụ trợ

        private enum HintVariant
        {
            Idle,
            EmptyLane,
            Dragging,
            FixedUpcoming,
            Dropped,
            Ended,
            LongId,
            EmptyLaneWhileSelected,
            MultiSelected,
        }

        /// <summary>
        /// Thứ tự CÓ Ý NGHĨA: mỗi biến thể dựng tiếp trên trạng thái biến thể trước nên không phải mở lại cửa sổ. Quan trọng
        /// hơn, thứ tự này đo dòng gợi ý ở CẢ HAI bề rộng mà hub thật có: chưa chọn gì thì không có pane inspector nên trục
        /// rộng 648px ở cửa sổ 700 (NGHỈ và LÀN TRỐNG), chọn rồi thì pane inspector mở ra và dòng gợi ý chỉ còn 368px (mọi
        /// biến thể sau). LÀN TRỐNG xuất hiện ở cả hai bề rộng — <c>UpdateHintLine</c> xét con trỏ TRƯỚC tập chọn, nên rê chuột
        /// qua chỗ trống khi đang chọn một đợt vẫn ra câu làn trống, lúc đó chỉ còn 368px — nên nó được đo hai lần.
        /// </summary>
        private static readonly HintVariant[] AllVariants =
        {
            HintVariant.Idle,
            HintVariant.EmptyLane,
            HintVariant.Dragging,
            HintVariant.FixedUpcoming,
            HintVariant.Dropped,
            HintVariant.Ended,
            HintVariant.LongId,
            HintVariant.EmptyLaneWhileSelected,
            HintVariant.MultiSelected,
        };

        private IEnumerator AssertHintVariant(LiveOpsHubLanguageId language, UxWindowSize size, HintVariant variant)
        {
            string context = language + " " + size + " " + variant;
            LiveOpsTimelineElement timeline = TimelineOf(_fixture);
            switch (variant)
            {
                case HintVariant.Idle:
                    break;
                case HintVariant.EmptyLane:
                case HintVariant.EmptyLaneWhileSelected:
                    yield return PoseCursorOnEmptyLane(timeline);
                    break;
                case HintVariant.Dragging:
                    yield return BeginDragAndHold(timeline);
                    break;
                case HintVariant.FixedUpcoming:
                    yield return SelectBar(LiveOpsDesignSample.HuntEarlyEntryKey, "hunt-0914");
                    break;
                case HintVariant.Dropped:
                    yield return SelectBar(LiveOpsDesignSample.HuntBonusEntryKey, "hunt-0916-bonus");
                    break;
                // (W9-25 chỗ #2) Đợt ĐÃ KHÉP: câu mô tả ngắn nhưng chủ ngữ là `lava-quest-2026-09a` — đúng nhãn mà cổng bố
                // cục W9 báo "chữ chiếm 95,0% bề rộng ô" trên 2022.3.
                case HintVariant.Ended:
                    yield return SelectBar(LiveOpsDesignSample.LavaQuestEarlyEntryKey, "lava-quest-2026-09a");
                    break;
                case HintVariant.LongId:
                    yield return SelectBar(LongEntryKey, LongestId);
                    break;
                default:
                    yield return SelectBar(LongLaneFirstEntryKey, "long-lane-first");
                    yield return SelectBar(LongLaneSecondEntryKey, string.Empty, UxEventSender.ActionModifier);
                    break;
            }

            LiveOpsTimelineHintLine hint = timeline.HintLine;
            Assert.Greater(hint.Description.text.Length, 0, context + ": dòng gợi ý phải có câu mô tả");

            yield return AssertLabelReadsWhole(hint.Description, timeline, context + " mô tả");
            if (hint.Subject.text.Length > 0) yield return AssertLabelReadsWhole(hint.Subject, timeline, context + " chủ ngữ");
            // Hai nhãn phải NẰM TRONG hộp gợi ý: "đọc trọn vẹn trong ô của mình" mà cái ô ấy lại thò ra khỏi dòng gợi ý (dòng
            // này có overflow: hidden) thì chữ vẫn mất — đúng luật childOverflow của cổng bố cục, canh ngay tại đây.
            AssertInsideHost(hint.Description, hint, context + " mô tả");
            AssertInsideHost(hint.Subject, hint, context + " chủ ngữ");

            Assert.Less(hint.layout.height, MaximumHintHeight,
                context + ": dòng gợi ý cao " + hint.layout.height + "px, chạm trần " + MaximumHintHeight
                + "px của USS — từ trần trở lên là chữ bị cắt chứ không còn là gập");
            Assert.GreaterOrEqual(timeline.Body.layout.height,
                LiveOpsTimelineGeometry.LanePaddingTop + LiveOpsTimelineGeometry.RowPitch - LayoutTolerance,
                context + ": dòng gợi ý gập xuống không được lấy mất chỗ của làn đầu tiên");
            LogProbe("[HINTGATE] " + context + " hint=" + hint.contentRect.width + "x" + hint.layout.height
                + " subjBox=" + hint.Subject.contentRect.width + "x" + hint.Subject.contentRect.height
                + " descBox=" + hint.Description.contentRect.width + "x" + hint.Description.contentRect.height
                + " subj='" + hint.Subject.text + "' desc='" + hint.Description.text + "'");

            if (variant == HintVariant.EmptyLane || variant == HintVariant.EmptyLaneWhileSelected)
            {
                timeline.SetCursor(null, string.Empty, false);
            }
            if (variant == HintVariant.Dragging) yield return EndHeldDrag(timeline);
        }

        /// <summary>
        /// Trạng thái LÀN TRỐNG: con trỏ đứng trên chỗ trống của làn có id DÀI NHẤT. Dựng bằng
        /// <see cref="LiveOpsTimelineElement.SetCursor"/> — đường pose mà control đã mở sẵn cho kịch bản chụp, vì batchmode
        /// không giữ được con trỏ thật qua nhiều khung hình. Câu gợi ý vẫn do chính <c>UpdateHintLine</c> dựng (kể cả đoạn
        /// khoảng đang xem), nên chữ đo được là chữ người dùng đọc.
        /// </summary>
        private IEnumerator PoseCursorOnEmptyLane(LiveOpsTimelineElement timeline)
        {
            timeline.SetCursor(LiveOpsDesignSample.NowUtc, LongestId, true);
            yield return UxEventSender.Settle(UxEventSender.SettleFrames, UxEventSender.SettleMilliseconds);
            StringAssert.Contains(LongestId, timeline.HintLine.Subject.text,
                "tiền đề: gợi ý làn trống phải nêu tên làn dài nhất (làn không dựng được thì phép đo vô nghĩa)");
        }

        /// <summary>
        /// Trạng thái ĐANG KÉO: cú kéo THẬT (Move → Down → các bước Drag) và GIỮ chuột để đọc bố cục giữa chừng, đúng cách
        /// <see cref="UxEventSender.Drag"/> mở ra cho test đọc trạng thái nửa chừng. Kéo sang phải khi còn chỗ, hết chỗ thì
        /// sang trái — ở cửa sổ 700 thanh có thể đã nằm sát mép phải của trục.
        /// </summary>
        private IEnumerator BeginDragAndHold(LiveOpsTimelineElement timeline)
        {
            LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.HuntEarlyEntryKey);
            Assert.IsNotNull(bar, "tiền đề: trục phải vẽ thanh hunt-0914 để kéo");
            if (!TryPointThatClicksThrough(bar).HasValue)
            {
                timeline.Body.ScrollTo(bar);
                yield return UxEventSender.Settle(UxEventSender.SettleFrames, UxEventSender.SettleMilliseconds);
                bar = _fixture.BarOf(LiveOpsDesignSample.HuntEarlyEntryKey);
            }
            Vector2? grip = TryPointThatClicksThrough(bar);
            Assert.IsTrue(grip.HasValue, "tiền đề: cú kéo phải tới được thanh hunt-0914 — nó đang bị che hoặc ngoài tầm nhìn");
            Vector2 from = grip.Value;
            bool roomOnTheRight = from.x + DragDistance + DragEdgeMargin <= timeline.worldBound.xMax;
            float delta = roomOnTheRight ? DragDistance : -DragDistance;
            yield return UxEventSender.Drag(_fixture.Window, from, from + new Vector2(delta, 0f), 3, EventModifiers.None,
                null, false);
            Assert.IsTrue(timeline.DragController.IsDragging,
                "tiền đề: cú kéo phải THẬT SỰ bắt đầu (ngưỡng " + LiveOpsTimelineDragController.DragThreshold + "px)");
        }

        /// <summary>
        /// Điểm bấm THẬT SỰ tới được thanh. Tâm thanh không dùng được ở mọi cảnh: thanh "bị bỏ" ở cửa sổ hẹp chỉ rộng 21px
        /// và NHÃN CHỮ của thanh bên cạnh tràn đè lên nó, nên <c>panel.Pick(tâm)</c> trả về nhãn 'hunt-0914' — bấm ở đó là
        /// chọn nhầm đợt bên cạnh, và trước khi có khẳng định chủ ngữ thì lỗi ấy ĐI QUA IM LẶNG.
        /// <para>
        /// Vì vậy dò theo đúng thứ tự người dùng nhắm: lưới điểm trên thân thanh trước (tâm → các điểm sát mép trong), rồi
        /// tới TÂM CỦA TỪNG PHẦN TỬ CON của thanh — tag "· bị bỏ" nằm ngoài thân thanh nhưng vẫn là con của nó, nên bấm vào
        /// tag là bấm vào thanh, đúng cách người dùng bấm khi thanh quá nhỏ. Không điểm nào tới được thì trả <c>null</c>:
        /// nơi gọi cuộn thân trục rồi thử lại, và nếu vẫn không tới thì <c>SelectBar</c> dựng trạng thái bằng đường presenter
        /// (có ghi ra file probe), còn cú KÉO thì ĐỎ vì nó không có đường thay thế. Không bao giờ bấm bừa một điểm.
        /// </para>
        /// </summary>
        private static Vector2? TryPointThatClicksThrough(LiveOpsTimelineBar bar)
        {
            Rect bound = bar.worldBound;
            var candidates = new List<Vector2>
            {
                bound.center,
                new Vector2(bound.xMin + ClickEdgeInset, bound.center.y),
                new Vector2(bound.xMax - ClickEdgeInset, bound.center.y),
                new Vector2(bound.center.x, bound.yMin + ClickEdgeInset),
                new Vector2(bound.center.x, bound.yMax - ClickEdgeInset),
                new Vector2(bound.xMin + ClickEdgeInset, bound.yMax - ClickEdgeInset),
                new Vector2(bound.xMax - ClickEdgeInset, bound.yMax - ClickEdgeInset),
                new Vector2(bound.xMin + ClickEdgeInset, bound.yMin + ClickEdgeInset),
                new Vector2(bound.xMax - ClickEdgeInset, bound.yMin + ClickEdgeInset),
            };
            foreach (VisualElement child in bar.Children())
            {
                Rect childBound = child.worldBound;
                if (childBound.width > 0f && childBound.height > 0f) candidates.Add(childBound.center);
            }
            for (int index = 0; index < candidates.Count; index++)
            {
                for (VisualElement current = bar.panel.Pick(candidates[index]); current != null; current = current.hierarchy.parent)
                {
                    if (current == bar) return candidates[index];
                }
            }
            return null;
        }

        /// <summary>Lùi 2px vào trong khi dò điểm bấm: ngay trên mép thanh là đã có thể rơi sang phần tử bên cạnh.</summary>
        private const float ClickEdgeInset = 2f;

        /// <summary>Huỷ cú kéo đang giữ: Esc (đường huỷ của chính hub) rồi nhả chuột, nên không đợt nào bị dời thật.</summary>
        private IEnumerator EndHeldDrag(LiveOpsTimelineElement timeline)
        {
            yield return UxEventSender.PressEscape(_fixture.Window);
            UxEventSender.MouseUp(_fixture.Window, timeline.worldBound.center, 0, EventModifiers.None, 1);
            yield return UxEventSender.Settle(UxEventSender.SettleFrames, UxEventSender.SettleMilliseconds);
            Assert.IsFalse(timeline.DragController.IsActive, "cú kéo phải được huỷ sạch trước biến thể kế");
        }

        private static void LogProbe(string line)
        {
            string path = Environment.GetEnvironmentVariable(ProbeEnvironmentVariable);
            if (string.IsNullOrEmpty(path)) return;
            System.IO.File.AppendAllText(path, line + Environment.NewLine);
        }

        /// <summary>Nhãn con không được thò khỏi hộp cha đang CẮT, cả bốn mép — cùng luật <c>childOverflow</c> của cổng bố cục.</summary>
        private static void AssertInsideHost(Label label, VisualElement host, string context)
        {
            Rect labelBound = label.worldBound;
            Rect hostBound = host.worldBound;
            Assert.LessOrEqual(labelBound.xMax, hostBound.xMax + LayoutTolerance,
                context + ": nhãn thò " + (labelBound.xMax - hostBound.xMax) + "px khỏi mép phải dòng gợi ý");
            Assert.LessOrEqual(labelBound.yMax, hostBound.yMax + LayoutTolerance,
                context + ": nhãn thò " + (labelBound.yMax - hostBound.yMax) + "px khỏi mép dưới dòng gợi ý");
            Assert.GreaterOrEqual(labelBound.xMin, hostBound.xMin - LayoutTolerance,
                context + ": nhãn thò " + (hostBound.xMin - labelBound.xMin) + "px khỏi mép trái dòng gợi ý");
            Assert.GreaterOrEqual(labelBound.yMin, hostBound.yMin - LayoutTolerance,
                context + ": nhãn thò " + (hostBound.yMin - labelBound.yMin) + "px khỏi mép trên dòng gợi ý");
        }

        /// <summary>
        /// Nhãn đọc được TRỌN VẸN: (1) mẩu chữ liền KHÔNG ngắt được (từ dài nhất, gạch nối tính là chỗ ngắt) phải vừa bề
        /// rộng ô — nếu không thì dù có gập vẫn tràn ngang; (2) chữ đã gập phải vừa CHIỀU CAO ô. Hai vế cùng bắt buộc: vế
        /// một mình bắt được "chữ xuống dòng nhưng ô không cao thêm", vế hai mình bắt được "ô đủ cao nhưng một từ dài hơn ô".
        /// </summary>
        private IEnumerator AssertLabelReadsWhole(Label label, VisualElement host, string context)
        {
            Rect content = label.contentRect;
            Assert.Greater(content.width, 0f, context + ": nhãn phải có bề rộng thật (test này phải chạy KHÔNG -nographics)");

            Label runProbe = CreateProbe(host, label, LongestUnbreakableRun(label.text), float.NaN);
            Label wrapProbe = CreateProbe(host, label, label.text, content.width);
            yield return UxEventSender.WaitUntil(() => runProbe.layout.width > 0f && wrapProbe.layout.height > 0f,
                context + ": nhãn đo chưa có layout");
            float runWidth = runProbe.layout.width;
            float wrappedHeight = wrapProbe.layout.height;
            runProbe.RemoveFromHierarchy();
            wrapProbe.RemoveFromHierarchy();

            Assert.LessOrEqual(runWidth, content.width + TextMeasureTolerance,
                context + ": mẩu chữ liền dài nhất của \"" + label.text + "\" cần " + runWidth + "px mà ô chỉ rộng "
                + content.width + "px — gập cũng không cứu được, chữ sẽ tràn ngang rồi bị cắt");
            Assert.LessOrEqual(wrappedHeight, content.height + TextMeasureTolerance,
                context + ": chữ \"" + label.text + "\" gập lại cần " + wrappedHeight + "px chiều cao mà ô chỉ cao "
                + content.height + "px — phần cuối chữ bị cắt mất");
        }

        /// <summary>
        /// Mẩu chữ liền KHÔNG ngắt được dài nhất. Chỗ ngắt = khoảng trắng và NGAY SAU dấu gạch nối (UI Toolkit ngắt được ở
        /// gạch nối, nên "treasure-hunt-…" gập thật chứ không tràn). Trả "" khi chuỗi rỗng.
        /// </summary>
        internal static string LongestUnbreakableRun(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string longest = string.Empty;
            int runStart = 0;
            for (int index = 0; index < text.Length; index++)
            {
                char current = text[index];
                bool breakAfter = current == '-';
                bool breakBefore = char.IsWhiteSpace(current);
                if (!breakAfter && !breakBefore) continue;
                int runEnd = breakBefore ? index : index + 1;
                if (runEnd - runStart > longest.Length) longest = text.Substring(runStart, runEnd - runStart);
                runStart = breakBefore ? index + 1 : index + 1;
            }
            if (text.Length - runStart > longest.Length) longest = text.Substring(runStart);
            return longest;
        }

        /// <summary>
        /// Label song sinh: cùng class nên cùng bộ chữ và cùng cỡ, đặt tuyệt đối ngoài luồng layout. <paramref name="width"/>
        /// NaN = đo bề rộng tự nhiên một hàng; có số = ép đúng bề rộng ô rồi đọc chiều cao đã gập.
        /// </summary>
        private static Label CreateProbe(VisualElement host, Label source, string text, float width)
        {
            Label probe = new Label(text) { pickingMode = PickingMode.Ignore };
            foreach (string className in source.GetClasses()) probe.AddToClassList(className);
            probe.style.position = Position.Absolute;
            probe.style.left = 0f;
            probe.style.top = 0f;
            // Nhãn nguồn có thể là absolute căng theo cha (nhãn thước dùng top:0/bottom:0): không gỡ hai mép kia thì nhãn
            // đo cao bằng cả khung và phép đo vô nghĩa.
            probe.style.right = StyleKeyword.Auto;
            probe.style.bottom = StyleKeyword.Auto;
            probe.style.minWidth = 0f;
            probe.style.maxWidth = StyleKeyword.None;
            probe.style.maxHeight = StyleKeyword.None;
            probe.style.height = StyleKeyword.Auto;
            if (float.IsNaN(width))
            {
                probe.style.whiteSpace = WhiteSpace.NoWrap;
            }
            else
            {
                probe.style.whiteSpace = WhiteSpace.Normal;
                probe.style.width = width;
            }
            host.Add(probe);
            return probe;
        }

        private static LiveOpsTimelineElement TimelineOf(UxHubWindowFixture fixture)
        {
            LiveOpsTimelineElement timeline = fixture.Root.Q<LiveOpsTimelineElement>();
            Assert.IsNotNull(timeline, "màn Lịch phải có control timeline");
            return timeline;
        }

        private IEnumerator OpenHub(LiveOpsHubLanguageId language, UxWindowSize size, LiveOpsHubServices services)
        {
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, size, language, services);
            yield return _fixture.WaitForLayout();
        }

        private void CloseHub()
        {
            _fixture?.Dispose();
            _fixture = null;
        }

        private IEnumerator SelectBar(string entryKey, string expectedSubject)
        {
            yield return SelectBar(entryKey, expectedSubject, EventModifiers.None);
        }

        /// <summary>
        /// Chọn một đợt bằng cú BẤM THẬT, nhưng chỉ sau khi đã CUỘN thanh vào tầm nhìn và kiểm thanh thật sự nhận được cú
        /// bấm — rồi khẳng định dòng gợi ý đã đổi sang đúng đợt ấy.
        /// <para>
        /// Vì sao cần cả ba bước: mẫu xấu nhất có sáu làn, ở cửa sổ cao 560px làn dưới cùng nằm DƯỚI mép ScrollView. Bấm vào
        /// toạ độ của một thanh nằm ngoài tầm nhìn là bấm trúng chỗ khác, và lỗi đó ĐI QUA IM LẶNG: dòng gợi ý giữ nguyên
        /// đợt chọn trước, test vẫn xanh vì câu nào cũng đọc được — chỉ là đo nhầm câu. Lượt đo ngày 20/9/2026 dính đúng
        /// chuyện đó (biến thể "đợt bị bỏ" đo ra câu của "đợt cố định"). Nay sai là ĐỎ ngay tại tiền đề.
        /// </para>
        /// </summary>
        private IEnumerator SelectBar(string entryKey, string expectedSubject, EventModifiers modifiers)
        {
            LiveOpsTimelineElement timeline = TimelineOf(_fixture);
            LiveOpsTimelineBar bar = _fixture.BarOf(entryKey);
            Assert.IsNotNull(bar, "tiền đề: trục phải vẽ thanh '" + entryKey + "'");
            Vector2? point = TryPointThatClicksThrough(bar);
            if (!point.HasValue)
            {
                // Thanh nằm ngoài tầm nhìn của thân trục (mẫu xấu nhất có sáu làn, cửa sổ cao 560px không đủ chỗ): cuộn tới
                // rồi thử lại. Chỉ cuộn khi CẦN — cuộn vô cớ đẩy thanh lên sát mép và chui xuống dưới header làn.
                timeline.Body.ScrollTo(bar);
                yield return UxEventSender.Settle(UxEventSender.SettleFrames, UxEventSender.SettleMilliseconds);
                bar = _fixture.BarOf(entryKey);
                Assert.IsNotNull(bar, "tiền đề: thanh '" + entryKey + "' phải còn trong cây sau khi cuộn");
                point = TryPointThatClicksThrough(bar);
            }
            if (point.HasValue)
            {
                yield return UxEventSender.ClickAt(_fixture.Window, point.Value, modifiers);
            }
            else
            {
                // Thanh KHÔNG bấm tới được ở cỡ này và đó là chuyện có thật của sản phẩm, không phải lỗi test: thanh "bị bỏ"
                // ở 1024x700 bản tiếng Anh chỉ rộng 26px và NHÃN CHỮ của thanh liền kề tràn đè kín nó (pick ở mọi điểm đều
                // trả nhãn 'hunt-0914'). Đã ghi vào sổ rủi ro của đợt. Ở đây dựng trạng thái bằng đường PRESENTER
                // (LiveOpsTimelineElement.Select) để vẫn đo được câu gợi ý của đợt bị bỏ: pane inspector đã mở từ biến thể
                // trước nên bề rộng đang đo vẫn là bề rộng HẸP — đúng cảnh xấu nhất. Không im lặng: câu assert chủ ngữ ngay
                // dưới vẫn phải đúng, và biến thể này vẫn đo bằng cú bấm thật ở mọi cỡ khác.
                LogProbe("[HINTGATE] dựng bằng Select() vì không bấm tới được thanh '" + entryKey + "' " + bar.worldBound
                    + " — tâm thanh rơi vào " + UxLayoutAuditor.Describe(bar.panel.Pick(bar.worldBound.center)));
                timeline.Select(entryKey, false);
            }
            yield return UxEventSender.Settle(UxEventSender.SettleFrames * 2, UxEventSender.SettleMilliseconds * 2);
            Assert.AreEqual(expectedSubject, timeline.HintLine.Subject.text,
                "tiền đề: bấm thanh '" + entryKey + "' phải đưa dòng gợi ý sang đúng đợt đó");
        }

        /// <summary>
        /// Mẫu thiết kế + ba thứ xấu nhất mà mẫu thiết kế không có: MỘT đợt id dài nhất, MỘT loại (làn) id dài nhất, và HAI
        /// đợt trong làn đó để dựng được trạng thái chọn nhiều CÙNG một loại (câu gợi ý khi đó nêu tên loại, tức nêu chuỗi
        /// dài nhất). KHÔNG sửa <see cref="LiveOpsDesignSample"/> — 1.601 byte JSON đã đăng ghim theo mẫu cũ, sửa mẫu là làm
        /// đỏ một test hợp đồng chẳng liên quan gì tới bố cục. Giờ của ba đợt thêm vào KHÔNG chồng nhau: chồng giờ sinh phát
        /// hiện mới và đổi cả banner của màn, tức đổi chính cái bố cục đang đo.
        /// </summary>
        private static LiveOpsHubServices WorstCaseServices()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(document.RemoteConfigKey);
            for (int index = 0; index < document.EventTypes.Count; index++) builder.WithEventType(document.EventTypes[index]);
            // Ô màu 3: ô còn trống trong [0, 7] của mẫu thiết kế (0, 1, 4, 6, 7 đã dùng) — LiveEventTypeDefinition ném khi ra ngoài.
            builder.WithEventType(new LiveEventTypeDefinition(LongestId, "Làn tên dài nhất", 3, false, "star_tournament_v1"));
            for (int index = 0; index < document.RecurringRules.Count; index++) builder.WithRecurringRule(document.RecurringRules[index]);
            for (int index = 0; index < document.FixedEvents.Count; index++) builder.WithFixedEvent(document.FixedEvents[index]);
            builder.WithFixedEvent(new FixedLiveEventEntry(LongEntryKey, LongestId, "star-tournament",
                "2026-09-15T00:00:00Z", "2026-09-19T00:00:00Z", "star_tournament_v1"));
            builder.WithFixedEvent(new FixedLiveEventEntry(LongLaneFirstEntryKey, "long-lane-first", LongestId,
                "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z", "star_tournament_v1"));
            builder.WithFixedEvent(new FixedLiveEventEntry(LongLaneSecondEntryKey, "long-lane-second", LongestId,
                "2026-09-16T00:00:00Z", "2026-09-17T00:00:00Z", "star_tournament_v1"));
            for (int index = 0; index < document.PublishedStamps.Count; index++) builder.WithPublishedStamp(document.PublishedStamps[index]);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(new ManualLiveOpsClock(LiveOpsDesignSample.NowUtc, true))
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(builder.Build())));
            services.Session.RunCheckToCompletion();
            return services;
        }

        private IEnumerator OpenPanel(UxWindowSize size, LiveOpsTimelineZoom zoom, DateTime rangeStartUtc)
        {
            _panel = TimelineTestPanel.Open(size.Width, size.Height);
            VisualElement root = _panel.CreateRoot(false);
            TimelineHarness harness = TimelineHarness.Create(root, TimelineViewInputs.Checked(LiveOpsDesignSample.Document));
            harness.Start(rangeStartUtc, zoom);
            _panel.Harness = harness;
            yield return harness.WaitReady();
        }

        private void ClosePanel()
        {
            _panel?.Dispose();
            _panel = null;
        }
    }
}
