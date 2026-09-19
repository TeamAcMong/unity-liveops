using System;
using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hành trình ô giờ UTC của inspector Lịch (UX-04) — phần mà <see cref="UxCalendarJourneyTests"/> chưa chạm tới. Cùng luật với
    /// mọi lớp UxGate khác: chuột/phím THẬT qua <see cref="UxEventSender"/>, test chỉ ĐỌC trạng thái để assert.
    /// <para>
    /// Vì sao tách file: lớp hành trình Lịch giữ các ca kéo/thu phóng và đang do gói khác của lượt W8-UX2 sửa song song. Hai ca ở
    /// đây khoá hai chỗ mà cả cổng W8 lẫn lượt soát đều chỉ ra là CHƯA CÓ VÙNG PHỦ bằng sự kiện thật:
    /// </para>
    /// <para>
    /// (1) lối chốt "gõ xong rồi RỜI Ô" — mọi ca cũ chỉ chốt bằng Enter, nên câu "rời ô thì ghi" của báo cáo cổng mới chỉ là suy
    /// luận đọc code chứ chưa có số đo; và lối rời ô còn đi qua nhánh riêng <c>_pendingIncompleteInput</c> /
    /// <c>_focusMovingWithinField</c> của <see cref="LiveOpsUtcDateTimeField"/>, lại còn khác nhau giữa hai bản Unity (2022.3 ghi ô
    /// <c>isDelayed</c> lúc <c>BlurEvent</c>, 6000.6 lúc <c>FocusOutEvent</c>).
    /// </para>
    /// <para>
    /// (2) trạng thái "đợt ĐÃ KHÉP": bảng 7.0 cấm đổi giờ của đợt đã khép, nhưng trước lượt W8-UX2 hai ô giờ vẫn BẬT — người dùng
    /// gõ được, Enter được, ô hiện giờ mới, rồi lệnh ghi bị bỏ lặng lẽ và asset giữ nguyên giờ cũ. Hai ca hành trình cũ từng đứng
    /// trên đợt đã khép nay chuyển sang đợt chưa bắt đầu, nên vùng phủ của trạng thái này nằm ở đây.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxCalendarTimeFieldJourneyTests
    {
        /// <summary>Cùng mốc "bây giờ" với mẫu thiết kế của <see cref="UxCalendarJourneyTests"/> — 13/9/2026 08:47 giờ máy (+7).</summary>
        private static readonly DateTime DesignNowUtc = new DateTime(2026, 9, 13, 1, 47, 0, DateTimeKind.Utc);

        /// <summary>hunt-0914 (14/9 → 17/9): CHƯA BẮT ĐẦU ở mốc trên nên đổi giờ được thật.</summary>
        private const string NotStartedEntryKey = LiveOpsDesignSample.HuntEarlyEntryKey;

        /// <summary>lava-quest-2026-09a (10/9 → 13/9): ĐÃ KHÉP trước mốc trên 1 giờ 47 phút.</summary>
        private const string EndedEntryKey = LiveOpsDesignSample.LavaQuestEarlyEntryKey;

        /// <summary>Giờ mới người dùng gõ vào ô giờ Bắt đầu — khác giờ đang có (00:00) nên một lần ghi là thấy được.</summary>
        private const string TypedStartTime = "23:00";

        /// <summary>Chuỗi asset mong đợi sau khi ghi: đúng ngày cũ của hunt-0914, chỉ phần giờ đổi.</summary>
        private const string ExpectedStartUtcText = "2026-09-14T23:00:00Z";

        /// <summary>Cỡ cửa sổ rộng nhất có inspector mở sẵn (không phải drawer) — cùng cỡ mà ca UX-04 cũ dùng.</summary>
        private static readonly UxWindowSize InspectorSize = UxHubWindowFixture.AllSizes[4];

        private UxHubWindowFixture _fixture;

        [TearDown]
        public void TearDown()
        {
            DisposeFixture();
        }

        /// <summary>
        /// UX-04 lối RỜI Ô: gõ giờ mới rồi bấm sang một ô KHÁC của inspector (không Enter) thì tài liệu phải đổi. Đây là lối mà
        /// người dùng dùng nhiều nhất — gõ xong đi tiếp — và là lối duy nhất đi qua nhánh <c>FocusOutEvent</c>/<c>BlurEvent</c> của
        /// ô <c>isDelayed</c>, chỗ hai bản Unity ghi vào hai thời điểm khác nhau.
        /// </summary>
        [UnityTest]
        public IEnumerator TimeField_TypeThenClickAnotherField_Commits()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendarWithSelection(language, NotStartedEntryKey);
                LiveOpsUtcDateTimeField field = FirstDateTimeField();
                IntegerField durationField = DurationField();
                int revisionBefore = _fixture.Services.Session.DocumentRevision;

                yield return UxEventSender.ReplaceText(_fixture.Window, field.TimeInput, TypedStartTime);
                // Đợt đợi điều kiện trước rồi mới đo lối rời ô: nếu cú gõ không tới được ô (cửa sổ hub bị mất focus vì một Unity khác
                // đang chạy cùng máy) thì "tài liệu không đổi" là hệ quả, không phải triệu chứng — câu này nói thẳng nguyên nhân thay vì để người đọc
                // đi tra một lỗi không có thật.
                Assert.AreEqual(TypedStartTime, field.TimeInput.text,
                    "cú gõ không tới được ô giờ — cửa sổ hub không nhận được phím (máy đang chạy một lượt Unity khác?), chưa đo được UX-04 ("
                    + language + ")");
                // Rời ô bằng cú bấm THẬT vào vùng nhập của ô "Dài" — không Enter, không Tab: hai lối đó đã có ca riêng và Tab còn phụ
                // thuộc cách từng bản Unity định tuyến phím theo focus. Bấm đúng vùng nhập (không phải tâm cả hàng, vì tâm hàng rơi vào nhãn
                // và nhãn dài ngắn khác nhau theo ngôn ngữ) — cùng cách UxEventSender.ReplaceText nhắm ô.
                yield return UxEventSender.Click(_fixture.Window, InputPartOf(durationField));

                Assert.AreNotEqual(revisionBefore, _fixture.Services.Session.DocumentRevision,
                    "gõ giờ rồi bấm sang ô khác mà tài liệu không đổi — lối rời ô của ô giờ không ghi (UX-04, " + language + ")");
                Assert.IsTrue(_fixture.Services.Session.Document.TryGetFixedEvent(NotStartedEntryKey,
                    out FixedLiveEventEntry entry), "đợt đang sửa phải còn trong tài liệu");
                Assert.AreEqual(ExpectedStartUtcText, entry.StartUtcText,
                    "tài liệu đổi nhưng KHÔNG đổi thành giờ vừa gõ — lối rời ô ghi nhầm giá trị (UX-04, " + language + ")");
                DisposeFixture();
            }
        }

        /// <summary>
        /// UX-04 trạng thái ĐÃ KHÉP: hai ô giờ và ô "Dài" của đợt đã khép phải KHOÁ và NÓI LÝ DO. Trước lượt W8-UX2 chúng vẫn bật,
        /// nhận chữ, hiện giờ mới, rồi <c>CalendarTimelinePresenter.ApplyEdit</c> trả false không một lời nào — người dùng đọc giờ
        /// mới trong ô mà asset còn giờ cũ. Ca này khoá cả ba ô cùng lúc vì cả ba đi chung một lệnh ghi
        /// (<c>LiveOpsEditOperation.ChangeFixedEventTimes</c>) và cùng bị chính sách từ chối.
        /// </summary>
        [UnityTest]
        public IEnumerator EndedEntry_TimeFieldsAndDuration_AreLockedWithReason()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendarWithSelection(language, EndedEntryKey);
                List<LiveOpsUtcDateTimeField> fields = new List<LiveOpsUtcDateTimeField>();
                _fixture.Root.Query<LiveOpsUtcDateTimeField>().ToList(fields);
                Assert.AreEqual(2, fields.Count, "inspector của đợt cố định phải có đúng hai ô giờ (Bắt đầu, Kết thúc)");
                int revisionBefore = _fixture.Services.Session.DocumentRevision;

                for (int index = 0; index < fields.Count; index++)
                {
                    Assert.IsFalse(fields[index].enabledInHierarchy,
                        "ô giờ thứ " + (index + 1) + " của đợt ĐÃ KHÉP vẫn bật — người dùng gõ được một thứ mà bảng 7.0 cấm ghi (UX-04, "
                        + language + ")");
                    Assert.IsNotEmpty(fields[index].tooltip ?? string.Empty,
                        "ô giờ thứ " + (index + 1) + " khoá mà không nói vì sao — khoá im lặng cũng là nuốt thao tác (UX-04, " + language + ")");
                }
                IntegerField durationField = DurationField();
                Assert.IsFalse(durationField.enabledInHierarchy,
                    "ô Dài của đợt ĐÃ KHÉP vẫn bật — nó ghi vào mép cuối nên cũng bị chính sách từ chối (UX-04, " + language + ")");
                Assert.IsNotEmpty(durationField.tooltip ?? string.Empty,
                    "ô Dài khoá mà không nói vì sao (UX-04, " + language + ")");

                // Cú bấm THẬT vào ô đã khoá: ô không được nhận focus và tài liệu không được đổi — khoá phải là khoá thật, không
                // phải chỉ đổi màu chữ.
                yield return UxEventSender.Click(_fixture.Window, InputPartOf(fields[0].TimeInput));

                Assert.AreNotSame(fields[0].TimeInput, FocusedElement(),
                    "bấm vào ô giờ đã khoá vẫn đặt được con trỏ vào đó — người dùng sẽ gõ tiếp (UX-04, " + language + ")");
                Assert.AreEqual(revisionBefore, _fixture.Services.Session.DocumentRevision,
                    "bấm vào ô giờ của đợt đã khép mà tài liệu đổi — khoá không giữ được gì (UX-04, " + language + ")");
                DisposeFixture();
            }
        }

        // ================================================================================================ trợ giúp

        private void DisposeFixture()
        {
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
        }

        private IEnumerator OpenCalendarWithSelection(LiveOpsHubLanguageId language, string entryKey)
        {
            LiveOpsHubServices services = UxHubWindowFixture.DesignServices(new ScriptedLiveOpsHubConfirmationPresenter(), DesignNowUtc);
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, InspectorSize, language, services);
            yield return _fixture.WaitForLayout();
            yield return UxEventSender.Click(_fixture.Window, _fixture.BarOf(entryKey));
        }

        private LiveOpsUtcDateTimeField FirstDateTimeField()
        {
            LiveOpsUtcDateTimeField field = _fixture.Root.Q<LiveOpsUtcDateTimeField>();
            Assert.IsNotNull(field, "inspector không có ô ngày-giờ UTC nào để gõ");
            return field;
        }

        /// <summary>
        /// Ô "Dài" của inspector. Tìm theo NHÃN chứ không theo class: class <c>--number</c> còn nằm trên ô "Dời (giờ)" của bảng
        /// nhiều-lựa-chọn và trên popover Thêm đợt, nên lấy ô đầu tiên mang class đó là dễ bắt nhầm ô.
        /// </summary>
        private IntegerField DurationField()
        {
            List<IntegerField> fields = new List<IntegerField>();
            _fixture.Root.Query<IntegerField>().ToList(fields);
            for (int index = 0; index < fields.Count; index++)
            {
                if (string.Equals(fields[index].label, LiveOpsHubStrings.CalendarFieldDurationLabel, StringComparison.Ordinal))
                {
                    return fields[index];
                }
            }
            Assert.Fail("inspector của đợt cố định phải có ô \"" + LiveOpsHubStrings.CalendarFieldDurationLabel + "\"");
            return null;
        }

        /// <summary>
        /// Vùng nhập của một field UIElements — cùng class mà <see cref="UxEventSender.ReplaceText"/> nhắm. Bấm vào tâm cả hàng là
        /// bấm vào NHÃN, mà nhãn dài ngắn khác nhau theo ngôn ngữ nên chỗ rơi xuống đổi theo bản dịch — không đủ chắc cho một ca kiểm.
        /// </summary>
        private static VisualElement InputPartOf(VisualElement field)
        {
            return field.Q(className: UnityBaseFieldInputClassName) ?? field;
        }

        /// <summary>Class vùng nhập của Unity (không phải class của hub nên không nằm trong <c>LiveOpsHubClassNames</c>).</summary>
        private const string UnityBaseFieldInputClassName = "unity-base-field__input";

        private VisualElement FocusedElement()
        {
            return _fixture.Root.focusController == null ? null : _fixture.Root.focusController.focusedElement as VisualElement;
        }
    }
}
