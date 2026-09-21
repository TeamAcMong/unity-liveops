using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// CA CHỨNG MINH cho từng mục của <see cref="UxTruncationExemption"/>: mỗi mục miễn trừ phải có đúng một ca ở đây, và ca
    /// ấy phải đo được CẢ HAI điều kiện user chốt 21/9/2026 — (a) tooltip hiện đủ chữ, (b) có chỗ khác trong màn hiện đủ chữ.
    /// <para>
    /// Vì sao ca nằm RIÊNG chứ không nhét vào <see cref="UxLayoutAuditTests"/>: ma trận bố cục đo 38 màn × 7 cỡ × 2 ngôn ngữ
    /// và gom mọi phát hiện vào một câu assert — một miễn trừ hỏng ở đó chỉ làm con số đổi, không ai biết điều kiện nào trượt.
    /// Ca ở đây dựng ĐÚNG trạng thái làm ô bị cắt rồi hỏi từng điều kiện một, nên khi đỏ thì câu assert nói thẳng thiếu gì.
    /// </para>
    /// <para>
    /// Vì sao ca này KHÔNG thừa so với phép đo tại lượt kiểm: lượt kiểm chỉ đo được điều kiện (a) — tooltip là thứ đọc được
    /// từ chính phần tử bị phát hiện. Điều kiện (b) đòi biết "chỗ khác" là chỗ nào và phải THAO TÁC để tới đó (chọn hàng cho
    /// inspector vẽ), nên nó chỉ đo được ở một ca dựng trạng thái như ca này.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxTruncationExemptionTests
    {
        /// <summary>
        /// Cỡ cửa sổ của ca: rộng hơn bậc <c>--medium</c> để inspector Loại event là một CỘT luôn hiện — điều kiện (b) nói về
        /// chỗ đọc đủ, nên chỗ ấy phải đang ở trên màn hình chứ không nằm sau một drawer đóng.
        /// </summary>
        private static readonly UxWindowSize WideSize = new UxWindowSize(1280, 760);

        private UxHubWindowFixture _fixture;

        [TearDown]
        public void TearDown()
        {
            if (_fixture == null) return;
            _fixture.Dispose();
            _fixture = null;
        }

        /// <summary>
        /// Ô cột "Id loại" của bảng Loại event. Cột khai cứng 140px (<c>EventTypeTable.AddColumns</c>) nên id dài bằng
        /// <see cref="LiveOpsIdentifierLimits.MaxIdentifierLength"/> của mẫu xấu nhất không thể vừa ô ở cỡ cửa sổ nào — đây
        /// đúng là chỗ user cho phép rút chữ CÓ ĐIỀU KIỆN.
        /// </summary>
        [UnityTest]
        public IEnumerator EventTypesTypeIdCell_TruncatesWithTooltipAndInspectorShowsFullText()
        {
            yield return RunCellExemption(UxTruncationExemption.EventTypesTypeIdCellName,
                LiveOpsWorstCaseSample.LongTypeId, UxTruncationExemption.EventTypeInspectorTypeIdSurface);
        }

        /// <summary>
        /// Ô cột "Config key". Cột khai cứng 140px và là cột PHỤ — ở cửa sổ hẹp nó bị bỏ hẳn, nên chỗ đọc đủ ở inspector là
        /// đường duy nhất người dùng còn lại kể cả khi cột không được vẽ.
        /// </summary>
        [UnityTest]
        public IEnumerator EventTypesConfigKeyCell_TruncatesWithTooltipAndInspectorShowsFullText()
        {
            yield return RunCellExemption(UxTruncationExemption.EventTypesConfigKeyCellName,
                LiveOpsWorstCaseSample.LongTypeConfigKey, UxTruncationExemption.EventTypeInspectorConfigKeySurface);
        }

        /// <summary>
        /// Vế NGƯỢC của luật, đo trên chính bảng ấy: ô cột "Tên hiển thị" là cột GIÃN (<c>stretchable = true</c>), nên nó
        /// KHÔNG thuộc hai loại ô user cho phép rút chữ. Không có ca này thì bảng miễn trừ đọc thành "ô bảng nào cũng được
        /// tha", đúng cái tha rộng hơn ý định mà luật sinh ra để chặn.
        /// </summary>
        [UnityTest]
        public IEnumerator EventTypesDisplayNameCell_IsNotExempt_BecauseItsColumnStretches()
        {
            yield return OpenEventTypes();

            // Tìm theo CHỮ chứ không theo tên element: đúng vì ô của cột giãn CỐ Ý không mang tên — chỉ hai cột được miễn
            // mới cần nhận ra được từ bên ngoài (EventTypeTable.IdentifiedCellColumnNames).
            Label cell = CellWithText(LiveOpsWorstCaseSample.LongTypeDisplayName);
            Assert.IsNotNull(cell, "bảng phải vẽ ô 'Tên hiển thị' của loại có tên dài nhất");
            Assert.IsEmpty(cell.name, "ô của cột GIÃN không được mang tên element — tên chỉ dành cho cột được miễn");
            Assert.IsFalse(
                UxTruncationExemption.Allows("event-types-worst-data", UxLayoutFindingKinds.TextCut, cell, out _),
                "cột 'Tên hiển thị' GIÃN theo bề rộng bảng nên chữ bị cắt ở đó là lỗi bố cục thật — luật rút gọn có điều "
                + "kiện chỉ nói về cột đã khai bề rộng CỐ ĐỊNH");
        }

        /// <summary>
        /// Gỡ tooltip khỏi ô được miễn thì miễn trừ phải BIẾN MẤT ngay trong cùng một lượt. Đây là vế làm luật này khác một
        /// danh sách miễn trừ thường: mục vẫn còn trong bảng, nhưng điều kiện (a) không đạt nên phát hiện quay lại đỏ.
        /// </summary>
        [UnityTest]
        public IEnumerator ExemptCell_LosesExemption_WhenTooltipIsRemoved()
        {
            yield return OpenEventTypes();

            Label cell = CellNamed(UxTruncationExemption.EventTypesTypeIdCellName, LiveOpsWorstCaseSample.LongTypeId);
            Assert.IsNotNull(cell, "bảng phải vẽ ô 'Id loại' của loại có id dài nhất");
            Assert.IsTrue(UxTruncationExemption.Allows("event-types-worst-data", UxLayoutFindingKinds.TextCut, cell, out _),
                "ô đang mang tooltip đủ chữ nên nó phải được miễn");

            cell.tooltip = string.Empty;
            bool allowed = UxTruncationExemption.Allows("event-types-worst-data", UxLayoutFindingKinds.TextCut, cell,
                out string note);
            Assert.IsFalse(allowed, "gỡ tooltip là mất điều kiện (a) — miễn trừ phải biến mất, không được sống bằng lời khai");
            StringAssert.Contains("không mang tooltip", note,
                "dòng chẩn đoán phải nói THIẾU GÌ, không chỉ nói 'lỗi' — người sửa cần biết điều kiện nào trượt");
        }

        /// <summary>
        /// (J2-03) Nhãn thanh đợt trên trục. Bề rộng thanh bằng khoảng thời gian của đợt nhân tỉ lệ zoom, nên nhãn bị rút ở
        /// MỌI cỡ cửa sổ — không có bề rộng nào nới được để chữ vừa. Ca này đo đủ hai điều kiện: (a) thanh mang tooltip mở
        /// đầu bằng id ĐẦY ĐỦ, (b) chọn thanh thì dòng id trên tiêu đề inspector giữ nguyên chuỗi đầy đủ và đang hiện.
        /// </summary>
        [UnityTest]
        public IEnumerator TimelineBarLabel_ShortensWithTooltipAndInspectorShowsFullText()
        {
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, WideSize,
                LiveOpsHubLanguageId.Vietnamese, WorstCaseServices());
            yield return _fixture.WaitForLayout();

            LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsWorstCaseSample.LongEntryKey);
            Assert.IsNotNull(bar, "trục phải vẽ thanh của đợt có id dài nhất — không có thanh thì ca này không đo được gì");
            Label label = bar.Label;
            Assert.AreNotEqual(LiveOpsWorstCaseSample.LongEventId, label.text,
                "ca này chỉ có nghĩa khi nhãn ĐANG bị rút — thanh đang đủ chỗ cho cả id thì phải đổi mốc zoom của ca");
            StringAssert.Contains(LiveOpsHubStrings.TimelineBarLabelEllipsis, label.text,
                "hub tự rút nhãn thì phải để lại dấu '…' ở phía bị bỏ, nếu không người đọc tưởng đó là id thật");

            // So bằng StringComparison.Ordinal chứ không bằng StringAssert.StartsWith: tooltip là chuỗi MÁY đối chiếu
            // (id + khoảng UTC), máy chạy cổng đổi ngôn ngữ hệ thống thì phép so phải cho cùng kết quả.
            Assert.IsTrue(bar.tooltip.StartsWith(LiveOpsWorstCaseSample.LongEventId, StringComparison.Ordinal),
                "điều kiện (a): tooltip của thanh phải mở đầu bằng id ĐẦY ĐỦ, vì mẩu chữ còn lại trên nhãn không đủ tra "
                + "— tooltip đang là '" + bar.tooltip + "'");
            Assert.IsTrue(
                UxTruncationExemption.Allows("calendar-worst-data", UxLayoutFindingKinds.TextCut, label, out _),
                "nhãn thanh đang có tooltip đủ chữ nên luật rút gọn có điều kiện phải miễn cho nó");

            yield return UxEventSender.Click(_fixture.Window, bar);
            yield return UxEventSender.Settle(UxEventSender.SettleFrames * 2, UxEventSender.SettleMilliseconds * 2);

            Label titleId = InspectorTitleId();
            Assert.IsNotNull(titleId, "điều kiện (b): chọn một thanh thì tiêu đề inspector phải vẽ dòng id");
            Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(titleId),
                "điều kiện (b): chỗ đọc đủ phải đang HIỆN trên màn, không nằm sau một drawer đóng");
            Assert.AreEqual(LiveOpsWorstCaseSample.LongEventId, titleId.text,
                "điều kiện (b): dòng id của inspector phải giữ chuỗi ĐẦY ĐỦ, không phải bản đã rút của nhãn thanh");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Vế NGƯỢC của mục J2-03: gỡ tooltip khỏi THANH thì nhãn mất miễn trừ ngay, y như ô bảng. Không có ca này thì mục
        /// J2-03 đọc thành "nhãn thanh thì rút thoải mái" — mà quyết định của user là rút CÓ ĐIỀU KIỆN.
        /// </summary>
        [UnityTest]
        public IEnumerator TimelineBarLabel_LosesExemption_WhenBarTooltipIsRemoved()
        {
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, WideSize,
                LiveOpsHubLanguageId.Vietnamese, WorstCaseServices());
            yield return _fixture.WaitForLayout();

            LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsWorstCaseSample.LongEntryKey);
            Assert.IsNotNull(bar, "trục phải vẽ thanh của đợt có id dài nhất");

            bar.tooltip = string.Empty;
            bool allowed = UxTruncationExemption.Allows("calendar-worst-data", UxLayoutFindingKinds.TextCut, bar.Label,
                out string note);
            Assert.IsFalse(allowed, "gỡ tooltip là mất điều kiện (a) — miễn trừ của nhãn thanh phải biến mất theo");
            StringAssert.Contains("không mang tooltip", note,
                "dòng chẩn đoán phải nói THIẾU GÌ để người sửa biết điều kiện nào trượt");
        }

        /// <summary>
        /// (soát W11 R-01) Nhãn thanh rút kiểu HẬU TỐ ("…mega-final-round-2") là hình dạng mà
        /// <c>LiveOpsTimelineGeometry.ShortenedIdentifier</c> ưu tiên trả về, nên nó phải là hình dạng được đo kỹ nhất.
        /// Ba vế ở đây đóng đúng ba đường mà một phép so lỏng sẽ cho lọt.
        /// <para>
        /// Vì sao là ca THUẦN LOGIC chứ không mở hub: ba vế này nói về phép so chuỗi, không về bố cục. Dựng tay cây
        /// thanh + nhãn cho phép đặt tooltip SAI — thứ sản phẩm không bao giờ tự sinh ra, mà lại đúng là thứ cần chặn.
        /// </para>
        /// </summary>
        [Test]
        public void SuffixShortenedBarLabel_NeedsTooltipNamingTheSameIdentifier()
        {
            const string shownSuffix = "…mega-final-round-2";
            VisualElement bar = BuildBarWithLabel(shownSuffix, out Label label);

            bar.tooltip = LiveOpsWorstCaseSample.LongEventId + BarTooltipTail;
            Assert.IsTrue(UxTruncationExemption.CarriesFullTextTooltip(label, bar, out string _),
                "tooltip mở đầu bằng id ĐẦY ĐỦ của chính đợt ấy là đúng điều kiện (a) — phép so phải nhận");

            bar.tooltip = LiveOpsWorstCaseSample.BrokenEndEventId + BarTooltipTail;
            Assert.IsFalse(UxTruncationExemption.CarriesFullTextTooltip(label, bar, out string otherNote),
                "tooltip nói về một đợt KHÁC thì chữ bị rút vẫn không đọc lại được — bản đầu nhận nó vì mẩu giữ lại của "
                + "hình dạng hậu tố là chuỗi rỗng và mọi tooltip đều 'bắt đầu bằng chuỗi rỗng'");
            StringAssert.Contains("hậu tố", otherNote,
                "dòng chẩn đoán phải nói rõ phép so nào trượt, kẻo người sửa đi tìm nhầm điều kiện");

            bar.tooltip = LiveOpsWorstCaseSample.LongEventId + "0" + BarTooltipTail;
            Assert.IsFalse(UxTruncationExemption.CarriesFullTextTooltip(label, bar, out string _),
                "'…mega-final-round-2' nằm gọn trong 'mega-final-round-20' mà hai chuỗi ấy là hai đợt khác nhau — phép so "
                + "phải đòi BIÊN ngay sau mẩu giữ lại, không được dùng Contains suông");
        }

        /// <summary>
        /// (soát W11 R-02) Khoảng dư mỏng (<c>textTight</c>, W9-25) của nhãn thanh KHÔNG được miễn suông. Hai loại ô đầu
        /// của bảng miễn trừ được miễn hẳn vế ấy vì bề rộng của chúng là con số do bố cục chọn; nhãn thanh thì chỉ được
        /// user cho phép RÚT chữ, nên textTight ở đây vẫn phải đi qua điều kiện (a).
        /// <para>
        /// Không có ca này thì một dòng <c>if (isTextTight) return true;</c> lặng lẽ nuốt 4 chỗ textTight thật của hai màn
        /// và con số nợ W11 tụt đi 4 mà không ai biết vì sao.
        /// </para>
        /// </summary>
        [Test]
        public void BarLabelTextTight_StillGoesThroughTooltipCondition()
        {
            // Nhãn dải gom: không có "…" nào vì nó rút theo BẬC (câu đầy đủ → "loại · số đợt" → chỉ số đợt), nên phép so
            // đi vào nhánh "tooltip phải CHỨA đủ chữ của ô".
            const string stripLabel = "lava-quest · 21 đợt · 20 giờ/ngày";
            VisualElement bar = BuildBarWithLabel(stripLabel, out Label label);

            bar.tooltip = "lava-quest · 21 đợt · bấm để zoom vào 13/9 08:47 → 20/9 08:47 UTC";
            Assert.IsFalse(
                UxTruncationExemption.Allows("calendar-worst-data", UxLayoutFindingKinds.TextTight, label, out string note),
                "tooltip của dải nói câu KHÁC với nhãn, nên nhãn chật 97,5% bề rộng vẫn là một phát hiện thật");
            StringAssert.Contains("không chứa đủ chữ", note,
                "dòng chẩn đoán phải nói thiếu gì — mục miễn trừ có tồn tại, chỉ là điều kiện (a) không đạt");

            bar.tooltip = stripLabel + " · 13/9 08:47 → 20/9 08:47 UTC";
            Assert.IsTrue(
                UxTruncationExemption.Allows("calendar-worst-data", UxLayoutFindingKinds.TextTight, label, out string _),
                "tooltip có chứa đủ chữ của nhãn thì điều kiện (a) đạt — luật vẫn là rút CÓ ĐIỀU KIỆN, không phải cấm hẳn");
        }

        /// <summary>
        /// (soát W11 R-02) Mục J2-03 khai selector là class của CẢ THANH chứ không phải class của nhãn, vì tooltip nằm
        /// trên thanh. Lời khai đi kèm là "trong một thanh chỉ có đúng MỘT phần tử mang chữ" — câu này đo được, nên đo.
        /// Thêm một Label thứ hai vào thanh mà quên bảng miễn trừ là tha rộng hơn ý định, và ca này đỏ ngay.
        /// </summary>
        [UnityTest]
        public IEnumerator TimelineBar_CarriesExactlyOneTextElement()
        {
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, WideSize,
                LiveOpsHubLanguageId.Vietnamese, WorstCaseServices());
            yield return _fixture.WaitForLayout();

            LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsWorstCaseSample.LongEntryKey);
            Assert.IsNotNull(bar, "trục phải vẽ thanh của đợt có id dài nhất");

            List<TextElement> texts = new List<TextElement>();
            bar.Query<TextElement>().ToList(texts);
            Assert.AreEqual(1, texts.Count,
                "thanh đợt phải chỉ có ĐÚNG một phần tử mang chữ (cái nhãn) — thêm phần tử chữ thứ hai là mục miễn trừ "
                + "khai theo class CẢ THANH bỗng tha luôn cho nó, rộng hơn hẳn điều user đã duyệt");
            Assert.AreSame(bar.Label, texts[0], "phần tử mang chữ duy nhất của thanh phải là cái nhãn");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Đuôi tooltip của một thanh đợt, đúng khuôn <c>TimelineBarTooltipFormat</c> ("{0} · {1} → {2} UTC").</summary>
        private const string BarTooltipTail = " · 12/9 00:00 → 15/9 00:00 UTC";

        /// <summary>Cây thanh + nhãn tối thiểu mà bảng miễn trừ nhận ra: class của thanh để khớp selector, class nhãn để đọc chữ.</summary>
        private static VisualElement BuildBarWithLabel(string labelText, out Label label)
        {
            VisualElement bar = new VisualElement();
            bar.AddToClassList(LiveOpsHubClassNames.TimelineBar);
            label = new Label { text = labelText };
            label.AddToClassList(LiveOpsHubClassNames.TimelineBarLabel);
            bar.Add(label);
            return bar;
        }

        /// <summary>Dòng id trên tiêu đề inspector Lịch; null = inspector chưa vẽ (chưa chọn đợt nào).</summary>
        private Label InspectorTitleId()
        {
            List<Label> labels = new List<Label>();
            _fixture.Root.Query<Label>(className: UxTruncationExemption.CalendarInspectorTitleIdSurface).ToList(labels);
            return labels.Count == 0 ? null : labels[0];
        }

        // ============================================================================================ hạ tầng ca

        /// <summary>
        /// Một mục miễn trừ, đo đủ bốn vế: ô có thật · ô THẬT SỰ không chứa hết chữ · (a) tooltip đủ chữ · (b) chỗ khai ở
        /// inspector giữ đủ chữ và đang hiện trên màn.
        /// </summary>
        private IEnumerator RunCellExemption(string cellName, string fullText, string surfaceName)
        {
            yield return OpenEventTypes();

            Label cell = CellNamed(cellName, fullText);
            Assert.IsNotNull(cell, "bảng Loại event phải vẽ ô '" + cellName + "' mang chuỗi dài nhất của mẫu xấu nhất");

            float available = cell.contentRect.width;
            float needed = cell.MeasureTextSize(cell.text, 0f, VisualElement.MeasureMode.Undefined, 0f,
                VisualElement.MeasureMode.Undefined).x;
            Assert.Greater(needed, available,
                "ca này chỉ có nghĩa khi ô ĐANG không chứa hết chữ — ô '" + cellName + "' cần "
                + needed.ToString("0.#", CultureInfo.InvariantCulture) + "px, có "
                + available.ToString("0.#", CultureInfo.InvariantCulture) + "px");

            Assert.AreEqual(fullText, cell.tooltip,
                "điều kiện (a): ô bị rút chữ phải mang tooltip hiện ĐỦ chữ, không phải một câu tóm tắt khác");

            yield return SelectTypeRow(cell);

            TextField surface = _fixture.Root.Q<TextField>(surfaceName);
            Assert.IsNotNull(surface, "điều kiện (b): màn phải có chỗ khai '" + surfaceName + "' để đọc đủ chữ");
            Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(surface),
                "điều kiện (b): chỗ đọc đủ phải đang HIỆN trên màn, không nằm sau một pane đóng");
            Assert.AreEqual(fullText, surface.value,
                "điều kiện (b): chỗ khai phải giữ đúng chuỗi ĐẦY ĐỦ, không phải bản đã rút");
            LogAssert.NoUnexpectedReceived();
        }

        private IEnumerator OpenEventTypes()
        {
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.EventTypes, WideSize,
                LiveOpsHubLanguageId.Vietnamese, WorstCaseServices());
            yield return _fixture.WaitForLayout();
        }

        /// <summary>
        /// Chọn hàng của ô đang xét như người dùng BẤM vào nó — inspector chỉ vẽ loại đang chọn, nên không chọn thì điều
        /// kiện (b) đo trên một inspector rỗng và ca sẽ xanh vì lý do sai.
        /// </summary>
        private IEnumerator SelectTypeRow(VisualElement cell)
        {
            yield return UxEventSender.Click(_fixture.Window, cell);
            yield return UxEventSender.Settle(UxEventSender.SettleFrames * 2, UxEventSender.SettleMilliseconds * 2);
        }

        /// <summary>Ô bảng mang đúng tên cột và đúng chuỗi cần đo; null = bảng chưa vẽ ô ấy.</summary>
        private Label CellNamed(string cellName, string text)
        {
            return FindCell(cellName, text);
        }

        /// <summary>Ô bảng mang đúng chuỗi cần đo, KHÔNG hỏi tên — dùng cho ô của cột không được đặt tên.</summary>
        private Label CellWithText(string text)
        {
            return FindCell(null, text);
        }

        private Label FindCell(string cellName, string text)
        {
            List<Label> labels = new List<Label>();
            if (cellName == null) _fixture.Root.Query<Label>(className: LiveOpsHubClassNames.EventTypesCell).ToList(labels);
            else _fixture.Root.Query<Label>(cellName).ToList(labels);
            for (int index = 0; index < labels.Count; index++)
            {
                if (!labels[index].ClassListContains(LiveOpsHubClassNames.EventTypesCell)) continue;
                if (string.Equals(labels[index].text, text, StringComparison.Ordinal)) return labels[index];
            }
            return null;
        }

        private static LiveOpsHubServices WorstCaseServices()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsWorstCaseSample.Document)));
            services.Session.RunCheckToCompletion();
            return services;
        }
    }
}
