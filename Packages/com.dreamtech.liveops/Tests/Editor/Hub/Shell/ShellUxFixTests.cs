using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Test khoá lỗi UI/UX của gói G-UX-SHELL (đợt W8-UX): UX-20 (status bar đè nhau ở 820, badge rail giữ chữ ngôn ngữ cũ,
    /// icon tầng KIỂM nhạt), UX-26 (sau Hoàn tác status vẫn mời ⌘Z), UX-27 (hộp xác nhận đặt theo cửa sổ Editor chính, cao
    /// hơn nội dung, gợi ý phím bị cắt), UX-30 (nút ⋮ rộng rỗng, hàng bảng "7 ngày tới" bị co dưới chiều cao tối thiểu).
    /// <para>
    /// Mỗi test viết để ĐỎ trên code trước khi sửa: đó là bằng chứng nó khoá đúng lỗi chứ không chỉ mô tả lại code mới.
    /// Nhóm này chạm layout/cửa sổ thật nên ở category UI (9.1) — chạy KHÔNG có cờ <c>-nographics</c>.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class ShellUxFixTests
    {
        /// <summary>Cỡ cửa sổ của ảnh hành trình J10-02-820 — chỗ status bar tiếng Anh chạy vào nhau.</summary>
        private const int NarrowWidth = 820;
        private const int NarrowHeight = 560;

        /// <summary>Sai số một pixel khi so mép: layout UI Toolkit làm tròn theo dpi, so bằng == sẽ đỏ giả.</summary>
        private const float LayoutTolerance = 1f;

        private const double MaximumWaitSeconds = 5.0;

        /// <summary>Khoảng cách tối thiểu giữa câu trái đã cắt và giờ UTC bên phải — xem <c>liveops-hub-status-right</c>.</summary>
        private const float MinimumStatusGap = 10f;

        /// <summary>Câu trái dài như câu thật ở hành trình ("Kiểm lúc … · Vừa làm: …").</summary>
        private const string LongLeftText =
            "Checked at 08:46:58 · 12 rules · 3 need action · Just did: Move hunt-0916-bonus end 17/9 12:00 → 19/9 00:00 (⌘Z)";

        private const string RightText = "13/9 08:47 UTC · published 11/9 16:20 · sha 4f2a91c";

        private const string UndoStepName = "Dời kết thúc lava-quest-2026-09b";
        private const string MovedEndUtc = "2026-09-21T00:00:00Z";
        private const string UndoAssetFileName = "ShellUxFixUndo.asset";

        private LiveOpsHubWindowTestScope _scope;
        private LiveOpsHubWindow _sessionWindow;
        private LiveOpsConfirmWindow _confirmWindow;

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _scope = null;
            if (_sessionWindow != null)
            {
                _sessionWindow.Close();
                _sessionWindow = null;
            }
            if (_confirmWindow != null)
            {
                _confirmWindow.Close();
                _confirmWindow = null;
            }
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        // ──────────────────────────────────────────────────────────────────────────────────── UX-20 · status bar 820

        /// <summary>
        /// (UX-20 / UJ-19) Ở 820 px câu trái dài phải CO rồi cắt "…", không được vẽ đè lên giờ UTC bên phải. UI Toolkit đặt
        /// <c>flex-shrink</c> mặc định là 0 (khác web), nên một Label có <c>text-overflow: ellipsis</c> mà không khai co vẫn
        /// giữ nguyên bề rộng chữ và tràn ra ngoài cha — đúng ca "…Move hunt-091…13/9 08:47 UTC" của ảnh J10-02-820.
        /// </summary>
        [UnityTest]
        public IEnumerator StatusBar_LongLeftText_DoesNotOverlapRightText_At820()
        {
            _scope = LiveOpsHubWindowTestScope.Open(width: NarrowWidth, height: NarrowHeight);
            yield return _scope.WaitForLayout();

            LiveOpsHubStatusBar statusBar = _scope.Window.StatusBar;
            statusBar.SetLeft(null, LongLeftText, string.Empty);
            statusBar.SetRight(RightText, string.Empty);
            yield return LiveOpsHubWindowTestScope.WaitFrames(3);

            Rect left = statusBar.LeftLabel.worldBound;
            Rect right = statusBar.RightLabel.worldBound;
            Rect bar = statusBar.Element.worldBound;

            Assert.LessOrEqual(left.xMax, right.xMin + LayoutTolerance,
                "câu trái phải cắt trước giờ UTC bên phải, không được đè lên (UX-20): trái tới " + left.xMax + ", phải bắt đầu " + right.xMin);
            Assert.GreaterOrEqual(right.xMin - left.xMax, MinimumStatusGap,
                "hai vế phải cách nhau đủ để đọc ra hai câu: 6 px cũ làm '…Move hunt-091…13/9 08:47 UTC' dính thành một (UX-20)");
            Assert.LessOrEqual(left.xMax, bar.xMax + LayoutTolerance, "câu trái không được tràn khỏi status bar");
            Assert.AreEqual(LongLeftText, statusBar.LeftLabel.tooltip, "cắt chữ thì tooltip phải giữ đủ câu");
        }

        // ──────────────────────────────────────────────────────────────────────────────────── UX-20 · icon tầng KIỂM

        /// <summary>
        /// (UX-20 / V14) Icon tầng KIỂM ở rail hẹp đo được 1,67:1 trên nền rail skin sáng (dấu ✓ 125 trên nền 165) — dưới
        /// ngưỡng 3:1 của [FD §2.3] và đọc thành "tầng bị tắt". Icon khác cùng rail (bánh răng, đĩa) đạt 3,03:1, nên lỗi
        /// nằm ở chính glyph "Valid" chứ không ở màu nền. Test khoá tên icon: không còn là "Valid", vẫn nằm trong lưới
        /// icon đã đo của hub và nạp được ở CẢ hai skin (tên thiếu ở một skin làm rail mất icon im lặng).
        /// </summary>
        [Test]
        public void RailCheckStageIcon_IsNotThePaleValidGlyph()
        {
            string iconName = PipelineStages.IconNameOf(PipelineStage.Check);

            Assert.AreNotEqual("Valid", iconName,
                "glyph 'Valid' chỉ 1,67:1 trên nền rail ở skin sáng — trông như tầng KIỂM bị tắt (UX-20/V14)");
            CollectionAssert.Contains(LiveOpsHubIcons.AllDesignNames, iconName,
                "icon của tầng phải nằm trong lưới icon đã đo ([FD §2.12]) để probe icon còn gác được");
            Assert.IsNotNull(LiveOpsHubIcons.Get(iconName, darkSkin: false), "icon tầng KIỂM phải nạp được ở skin sáng");
            Assert.IsNotNull(LiveOpsHubIcons.Get(iconName, darkSkin: true), "icon tầng KIỂM phải nạp được ở skin tối");

            // (soát W8-UX R10) Chốt đúng TIÊU CHÍ đã làm đổi glyph, không chỉ chốt "khác Valid": đổi sang một glyph nhạt khác
            // mà vẫn xanh thì test này vô nghĩa. Số đo lấy từ probe icon của measure-capture.py trên nền rail skin sáng — ca
            // xấu nhất vì nền nhạt hơn. Glyph chưa có trong bảng = chưa đo, và chưa đo thì không được dùng cho tầng.
            float measuredContrast;
            Assert.IsTrue(MeasuredIconContrastOnRail.TryGetValue(iconName, out measuredContrast),
                "glyph '" + iconName + "' chưa có số đo tương phản trên nền rail — đo bằng probe icon rồi thêm vào bảng trước khi dùng");
            Assert.GreaterOrEqual(measuredContrast, MinimumIconContrast,
                "icon tầng phải đạt ngưỡng " + MinimumIconContrast + ":1 của [FD §2.3] trên nền rail skin sáng — '" + iconName
                + "' đo được " + measuredContrast);
        }

        /// <summary>Ngưỡng tương phản của [FD §2.3] cho hình đơn sắc trên nền.</summary>
        private const float MinimumIconContrast = 3f;

        /// <summary>
        /// Tỉ lệ tương phản ĐO ĐƯỢC của từng glyph trên nền rail ở skin sáng (probe icon của <c>measure-capture.py</c>, đợt
        /// W8-UX). Bảng này là lý do của <see cref="RailCheckStageIcon_IsNotThePaleValidGlyph"/>: "Valid" trượt ngưỡng, ba
        /// glyph còn lại đạt. Thêm glyph mới thì phải đo và thêm số vào đây.
        /// </summary>
        private static readonly Dictionary<string, float> MeasuredIconContrastOnRail = new Dictionary<string, float>
        {
            { "Valid", 1.67f },
            { "Search Icon", 3.03f },
            { "Settings", 3.03f },
            { "SaveAs", 3.03f },
        };

        // ──────────────────────────────────────────────────────────────────────────────────── UX-20 · đổi ngôn ngữ

        /// <summary>
        /// (UX-20 / UJ-20) Sau khi đổi ngôn ngữ, badge tầng XUẤT vẫn ghi "chặn" trong khi cả hub đã sang English. Nguyên
        /// nhân không nằm ở rail: phiên lịch giữ <c>ExportGateState</c> ĐÃ DỰNG THÀNH CHỮ trong một cache khoá theo
        /// <c>StateVersion</c> + format + readBack — đổi ngôn ngữ không đụng ba thứ đó nên cổng xuất trả lại đúng bản chữ cũ
        /// cho health, và rail chỉ chép lại.
        /// <para>
        /// (soát W8-UX R6) Test đọc CHỮ BADGE người dùng thấy trên rail, không chỉ so danh tính object nội bộ: hồi quy làm
        /// rơi cache mà vẫn vẽ badge tiếng Việt thì phải ĐỎ. Đổi ngôn ngữ bằng scope ghim chứ không bằng
        /// <c>LiveOpsHubLanguage.Set</c> vì Set ghi thẳng vào EditorPrefs của MÁY người chạy test; scope không bắn
        /// <c>LiveOpsHubLanguage.Changed</c> nên phải gọi tay đúng đường dựng lại mà handler đó gọi.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator LanguageChange_RebuildsRailBadge()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();

            ExportGateState beforeState;
            string vietnameseBadge;
            string vietnameseBlocked;
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.Vietnamese))
            {
                yield return OpenSessionWindow(services);
                beforeState = services.Session.EvaluateExportGate(services.JsonReadBack, services.Format);
                vietnameseBadge = ExportStageBadgeText();
                vietnameseBlocked = LiveOpsHubStrings.ExportGateHealthBlockedBadge;
            }

            Assert.AreEqual(vietnameseBlocked, vietnameseBadge,
                "ngữ cảnh test phải là cổng XUẤT đang chặn ở tiếng Việt — badge đang là '" + vietnameseBadge + "'");

            string englishBadge;
            string englishBlocked;
            ExportGateState afterState;
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
            {
                _sessionWindow.RebuildForLanguageChangeForTest();
                yield return LiveOpsHubWindowTestScope.WaitFrames(2);
                englishBadge = ExportStageBadgeText();
                englishBlocked = LiveOpsHubStrings.ExportGateHealthBlockedBadge;
                afterState = services.Session.EvaluateExportGate(services.JsonReadBack, services.Format);
            }

            Assert.AreNotEqual(vietnameseBadge, englishBadge,
                "badge tầng XUẤT phải đổi theo ngôn ngữ — vẫn là '" + englishBadge + "' giữa một hub đã sang English (UX-20/UJ-20)");
            Assert.AreEqual(englishBlocked, englishBadge,
                "badge tầng XUẤT sau khi đổi ngôn ngữ phải là chữ 'chặn' của tiếng Anh, không phải một câu khác");
            Assert.AreNotSame(beforeState, afterState,
                "đổi ngôn ngữ phải bỏ bản cổng xuất đã dựng thành chữ — đó là cơ chế làm badge đổi được (UX-20)");
        }

        /// <summary>Chữ badge của tầng XUẤT trên rail — bề mặt người dùng thật sự đọc.</summary>
        private string ExportStageBadgeText()
        {
            List<VisualElement> stageRows = _sessionWindow.Rail.Element.Query(className: LiveOpsHubClassNames.RailStageRow).ToList();
            Assert.AreEqual(PipelineStageRowCount, stageRows.Count,
                "rail P1 vẽ 4 tầng — số tầng đổi thì test đang soi nhầm hàng");
            Label badge = stageRows[ExportStageRowIndex].Q<Label>(className: LiveOpsHubClassNames.RailBadge);
            return badge == null ? string.Empty : badge.text;
        }

        /// <summary>Rail P1 vẽ 4 tầng (CẤU HÌNH, LÊN LỊCH, KIỂM, XUẤT — tầng CHẠY để dành P2/P3).</summary>
        private const int PipelineStageRowCount = 4;

        /// <summary>Chỉ số hàng tầng XUẤT trong rail.</summary>
        private const int ExportStageRowIndex = 3;

        // ──────────────────────────────────────────────────────────────────────────────────── UX-26 · status sau ⌘Z

        /// <summary>
        /// (UX-26 / UJ-10) Sau khi Hoàn tác, câu "Vừa làm: X (⌘Z)" vẫn mời bấm ⌘Z — bấm tiếp sẽ gỡ thao tác của người khác,
        /// và người dùng không được mời phím Làm lại để lấy lại bước vừa bỏ. Câu phải đổi hẳn sang thể "vừa hoàn tác" kèm
        /// phím Làm lại ([SD1 §3.8] khung 14). Test đọc chữ THẬT trên status bar sau một lần Undo thật của Editor.
        /// </summary>
        [UnityTest]
        public IEnumerator StatusBar_AfterUndo_OffersRedoKeyInsteadOfUndoKey()
        {
            string undoKeyLabel = LiveOpsHubKeyLabels.Undo;
            string redoKeyLabel = LiveOpsHubKeyLabels.Redo;
            if (undoKeyLabel.Length == 0 || redoKeyLabel.Length == 0)
            {
                Assert.Ignore("máy chạy test không gán phím Undo/Redo — câu status bỏ luôn phần phím, không có gì để khoá");
            }

            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(UndoAssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null).WithCalendarAsset(asset));
            yield return OpenSessionWindow(services);

            services.Session.Apply(CalendarSessionTests.MoveLavaQuestEnd(services.Session.Document, MovedEndUtc), UndoStepName);
            int undoGroup = Undo.GetCurrentGroup();
            services.Bus.ShowToast(LiveOpsToastModel.ForEdit(UndoStepName, undoGroup, string.Empty, UndoStepName));
            yield return LiveOpsHubWindowTestScope.WaitFrames(2);

            string textBeforeUndo = _sessionWindow.StatusBar.LeftLabel.text;
            Assume.That(textBeforeUndo, Does.Contain("(" + undoKeyLabel + ")"),
                "trước khi Hoàn tác câu phải mời ⌘Z — nếu không, ngữ cảnh test chưa dựng được bước Undo của thao tác");

            _sessionWindow.UndoTracker.PerformUndo();
            yield return LiveOpsHubWindowTestScope.WaitFrames(2);

            string textAfterUndo = _sessionWindow.StatusBar.LeftLabel.text;
            Assert.IsFalse(textAfterUndo.Contains("(" + undoKeyLabel + ")"),
                "sau Hoàn tác câu không được mời ⌘Z nữa — bước đó đã bị gỡ (UX-26): " + textAfterUndo);
            StringAssert.Contains(redoKeyLabel, textAfterUndo,
                "sau Hoàn tác câu phải mời phím Làm lại để lấy lại bước vừa bỏ (UX-26): " + textAfterUndo);
            StringAssert.Contains(UndoStepName, textAfterUndo, "câu vẫn phải nêu tên bước để biết vừa bỏ cái gì");
        }

        // ──────────────────────────────────────────────────────────────────────────────────── UX-27 · hộp xác nhận

        /// <summary>
        /// (UX-27 / UJ-12) Hộp mở giữa cửa sổ Editor CHÍNH nên với hub nổi ở (40,52) nó rơi đúng lên inspector và nửa phải
        /// trục — che chính chỗ người dùng đang quyết. Chỗ đặt phải tính theo cửa sổ chủ (hub đang focus lúc mở) và phải
        /// nằm gọn trong cửa sổ đó. Hàm đặt chỗ tách riêng để test được không cần mở cửa sổ thật.
        /// </summary>
        [Test]
        public void ConfirmWindow_Placement_StaysInsideOwnerWindow()
        {
            Rect owner = new Rect(40f, 52f, 1280f, 814f);
            Vector2 size = LiveOpsConfirmWindow.SizeFor(LiveOpsConfirmLevel.Level1);

            Rect placement = LiveOpsConfirmWindow.PlacementFor(owner, size);

            Assert.AreEqual(size.x, placement.width, LayoutTolerance, "bề rộng hộp không đổi theo cửa sổ chủ");
            Assert.AreEqual(size.y, placement.height, LayoutTolerance, "chiều cao hộp do nội dung quyết, không do cửa sổ chủ");
            Assert.GreaterOrEqual(placement.xMin, owner.xMin, "hộp phải nằm trong cửa sổ chủ, không tràn sang trái");
            Assert.LessOrEqual(placement.xMax, owner.xMax, "hộp phải nằm trong cửa sổ chủ, không tràn sang phải");
            Assert.GreaterOrEqual(placement.yMin, owner.yMin, "hộp phải nằm trong cửa sổ chủ, không tràn lên trên");
            Assert.LessOrEqual(placement.yMax, owner.yMax, "hộp phải nằm trong cửa sổ chủ, không tràn xuống dưới");
            Assert.Less(placement.center.x, owner.center.x,
                "hub đủ rộng thì hộp lệch khỏi cột inspector bên phải — chỗ người dùng đang đọc để quyết (UX-27)");
        }

        /// <summary>
        /// (UX-27 / UJ-12 · soát W8-UX R4) Hàm thuần ở trên có thể đúng hoàn toàn mà đường nối vẫn TRƠ: hộp thật vẫn đặt theo
        /// cửa sổ đang focus, hoặc <c>PlaceOnOwnerWindow</c> không bao giờ chạy, và mọi cổng vẫn xanh. Test này mở CỬA SỔ HUB
        /// THẬT rồi mở hộp bằng đúng đường production, đo Rect thật của hai cửa sổ — đúng cách UJ-12 đã đo trên máy
        /// (hộp 760,203 400×240 so với hub 40,52 1280×814).
        /// </summary>
        [UnityTest]
        public IEnumerator ConfirmWindow_RealWindow_OpensOverTheHubWindow()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            yield return OpenSessionWindow(services);

            Rect hub = _sessionWindow.position;
            _confirmWindow = LiveOpsConfirmWindow.OpenForTest(ShortBodyRequest(), null, placeOnOwnerWindow: true);
            yield return WaitForLayout(_confirmWindow.rootVisualElement);
            yield return LiveOpsHubWindowTestScope.WaitFrames(3);

            Assert.AreEqual(hub, _confirmWindow.ResolvedOwnerPosition,
                "hộp phải lấy CỬA SỔ HUB làm chủ, không lấy cửa sổ đang focus (UX-27): chủ đã dùng là "
                + _confirmWindow.ResolvedOwnerPosition);

            Rect box = _confirmWindow.position;
            Assert.GreaterOrEqual(box.xMin, hub.xMin - LayoutTolerance, "hộp thật phải nằm trong hub, không tràn sang trái: " + box);
            Assert.LessOrEqual(box.xMax, hub.xMax + LayoutTolerance, "hộp thật phải nằm trong hub, không tràn sang phải: " + box);
            Assert.GreaterOrEqual(box.yMin, hub.yMin - LayoutTolerance, "hộp thật phải nằm trong hub, không tràn lên trên: " + box);
            Assert.LessOrEqual(box.yMax, hub.yMax + LayoutTolerance, "hộp thật phải nằm trong hub, không tràn xuống dưới: " + box);
        }

        /// <summary>
        /// (UX-27 / UJ-12) Gợi ý phím góc trái bị cắt thành "Enter không đổi gi": một dòng + ellipsis trong hàng nút 400 px
        /// không đủ chỗ cho câu hai vế. Câu này nói ĐÚNG hai phím thoát hiểm của hộp phá huỷ nên không được cắt — cho xuống
        /// dòng thay vì cắt.
        /// </summary>
        [UnityTest]
        public IEnumerator ConfirmWindow_KeyHint_WrapsInsteadOfBeingCut()
        {
            _confirmWindow = LiveOpsConfirmWindow.OpenForTest(PrefixTypeToConfirmRequest());
            yield return WaitForLayout(_confirmWindow.rootVisualElement);

            Label keyHint = _confirmWindow.Content.Q<Label>(LiveOpsConfirmContent.KeyHintElementName);
            Assert.IsNotNull(keyHint, "hộp phải có gợi ý phím");
            Assert.AreEqual(WhiteSpace.Normal, keyHint.resolvedStyle.whiteSpace,
                "gợi ý phím phải xuống dòng thay vì cắt — câu nêu hai phím thoát hiểm (UX-27)");

            Vector2 needed = keyHint.MeasureTextSize(keyHint.text, keyHint.contentRect.width, VisualElement.MeasureMode.AtMost,
                0f, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(needed.y, keyHint.contentRect.height + LayoutTolerance,
                "cả câu gợi ý phím phải vẽ đủ trong chỗ của nó: cần " + needed.y + " có " + keyHint.contentRect.height);
        }

        /// <summary>
        /// (UX-27 / UJ-12) Hộp cao theo hằng cố định (212/290) nên với câu ngắn nó để lại một khoảng trống lớn giữa thân và
        /// hàng nút — người dùng đọc thành "còn thứ gì chưa hiện". Chiều cao phải theo nội dung đo được.
        /// </summary>
        [UnityTest]
        public IEnumerator ConfirmWindow_Height_HasNoDeadSpaceAboveButtons()
        {
            _confirmWindow = LiveOpsConfirmWindow.OpenForTest(ShortBodyRequest());
            yield return WaitForLayout(_confirmWindow.rootVisualElement);
            yield return LiveOpsHubWindowTestScope.WaitFrames(3);

            // (soát W8-UX R3) Đo bằng HÌNH HỌC ĐỘC LẬP, không bằng MeasureContentHeight: chính hàm đó là điều kiện dừng của
            // vòng lặp chỉnh chiều cao, nên lấy nó làm thước là khẳng định lại điều kiện dừng bằng chính nó — đo hụt thì cả
            // hai sai cùng chiều và test vẫn xanh trong khi nội dung bị cắt. Ở đây chỉ đọc worldBound của thân và của hàng
            // nút: khoảng cách giữa hai mép đó chính là mảng trống mà người dùng nhìn thấy.
            Label body = _confirmWindow.Content.Q<Label>(LiveOpsConfirmContent.BodyElementName);
            Label keyHint = _confirmWindow.Content.Q<Label>(LiveOpsConfirmContent.KeyHintElementName);
            Assert.IsNotNull(body, "hộp phải có câu thân");
            Assert.IsNotNull(keyHint, "hộp phải có gợi ý phím");
            VisualElement buttons = keyHint.hierarchy.parent;
            float deadSpace = buttons.worldBound.yMin - body.worldBound.yMax;

            Assert.LessOrEqual(deadSpace, MaximumDeadSpace,
                "hộp phải cao theo nội dung: đang dư " + deadSpace + " px giữa thân và hàng nút (UX-27)");
            // Vế thứ hai của cùng hình học: hàng nút phải nằm TRONG cửa sổ. Đây là chỗ ô cuộn của thân gác (soát R5) — thân
            // dài quá trần 420 px mà không cuộn được thì hàng nút bị đẩy ra ngoài và hộp phá huỷ hết đường thoát bằng chuột.
            Assert.LessOrEqual(buttons.worldBound.yMax, _confirmWindow.rootVisualElement.worldBound.yMax + LayoutTolerance,
                "hàng nút phải nằm trong vùng vẽ của cửa sổ — " + DescribeConfirmGeometry());
        }

        /// <summary>Mọi số đo của hộp trong một câu: assert đỏ phải nói được sai ở đâu mà không phải chạy lại Unity.</summary>
        private string DescribeConfirmGeometry()
        {
            VisualElement root = _confirmWindow.rootVisualElement;
            VisualElement frame = _confirmWindow.Content.Q(LiveOpsConfirmContent.ContentElementName);
            VisualElement scroll = _confirmWindow.Content.Q(LiveOpsConfirmContent.ScrollElementName);
            Label body = _confirmWindow.Content.Q<Label>(LiveOpsConfirmContent.BodyElementName);
            Label title = _confirmWindow.Content.Q<Label>(LiveOpsConfirmContent.TitleElementName);
            Label keyHint = _confirmWindow.Content.Q<Label>(LiveOpsConfirmContent.KeyHintElementName);
            VisualElement buttons = keyHint == null ? null : keyHint.hierarchy.parent;
            return "position=" + _confirmWindow.position + " root=" + root.worldBound + " frame=" + frame.worldBound
                + " measured=" + _confirmWindow.Content.MeasureContentHeight()
                + " title=" + (title == null ? "-" : title.worldBound.ToString())
                + " scroll=" + (scroll == null ? "-" : scroll.worldBound.ToString())
                + " body=" + (body == null ? "-" : body.worldBound.ToString())
                + " buttons=" + (buttons == null ? "-" : buttons.worldBound.ToString());
        }

        /// <summary>
        /// Chỗ dư tối đa giữa đáy thân và đỉnh hàng nút — quá mức này là người đọc thấy một mảng trống ("còn thứ gì chưa
        /// hiện"). Lỗi gốc UJ-12 đo được 125 px.
        /// </summary>
        private const float MaximumDeadSpace = 8f;

        /// <summary>
        /// (soát W8-UX R5) Thân hộp phải CUỘN được chứ không đẩy hàng nút ra ngoài khung: câu hậu quả do nơi gọi dựng và
        /// không có trần (danh sách id của "dán JSON đang chạy"), còn cửa sổ hộp có trần 420 px.
        /// </summary>
        [UnityTest]
        public IEnumerator ConfirmWindow_VeryLongBody_ScrollsAndKeepsButtonsInsideWindow()
        {
            _confirmWindow = LiveOpsConfirmWindow.OpenForTest(VeryLongBodyRequest());
            yield return WaitForLayout(_confirmWindow.rootVisualElement);
            yield return LiveOpsHubWindowTestScope.WaitFrames(4);

            ScrollView scroll = _confirmWindow.Content.Q<ScrollView>(LiveOpsConfirmContent.ScrollElementName);
            Assert.IsNotNull(scroll, "thân hộp phải nằm trong một ô cuộn");
            // Không có vế này thì test rỗng nghĩa: câu thân phải THẬT SỰ tràn khỏi ô cuộn, nếu không nó chỉ đang chứng minh
            // rằng một hộp vừa khít thì nút nằm trong khung.
            Assert.Greater(scroll.contentContainer.worldBound.height, scroll.contentViewport.worldBound.height + LayoutTolerance,
                "ngữ cảnh test phải thật sự tràn: nội dung thân cao " + scroll.contentContainer.worldBound.height
                + " px, ô cuộn cao " + scroll.contentViewport.worldBound.height + " px");

            Label keyHint = _confirmWindow.Content.Q<Label>(LiveOpsConfirmContent.KeyHintElementName);
            VisualElement buttons = keyHint.hierarchy.parent;
            Assert.LessOrEqual(_confirmWindow.position.height, LiveOpsConfirmWindow.MaximumHeight + LayoutTolerance,
                "hộp không được cao quá trần " + LiveOpsConfirmWindow.MaximumHeight + " px");
            Assert.LessOrEqual(buttons.worldBound.yMax, _confirmWindow.rootVisualElement.worldBound.yMax + LayoutTolerance,
                "câu hậu quả dài không được đẩy hàng nút ra ngoài vùng vẽ — người dùng mất cả hai nút lẫn gợi ý phím: "
                + DescribeConfirmGeometry());
            Assert.Greater(buttons.worldBound.height, 0f, "hàng nút phải còn chiều cao thật, không bị co về 0");
        }

        // ──────────────────────────────────────────────────────────────────────────────────── UX-30 · Tổng quan 820

        /// <summary>
        /// (UX-30 / UJ-25) Nút "⋮" dùng class nút CHỮ nên rộng 54 px với một icon 12 px nằm lệch trái — trông như nút hỏng.
        /// Nút chỉ có icon phải vuông và vừa icon.
        /// </summary>
        [UnityTest]
        public IEnumerator Overview_SectionMenuButton_IsSquareIconButton()
        {
            yield return OpenOverviewNarrow();

            Button menuButton = _sessionWindow.rootVisualElement.Q<Button>(OverviewSection.SectionMenuButtonElementName);
            Assert.IsNotNull(menuButton, "màn Tổng quan phải có nút ⋮ ở section header");

            Rect bounds = menuButton.worldBound;
            Assert.LessOrEqual(bounds.width, MaximumIconButtonWidth,
                "nút chỉ có icon phải vuông, không giữ min-width của nút chữ (UX-30): đang rộng " + bounds.width);
            Assert.GreaterOrEqual(bounds.width, bounds.height - LayoutTolerance,
                "nút icon không được hẹp hơn chiều cao của chính nó");
        }

        /// <summary>Nút icon vuông theo chiều cao nút 18 px + đệm — rộng hơn thế là đang giữ min-width của nút chữ.</summary>
        private const float MaximumIconButtonWidth = 26f;

        /// <summary>
        /// (UX-30 / UJ-25) Bảng "7 ngày tới" nằm trong thân co được: khi thân cao hơn cửa sổ, flexbox co từng hàng xuống
        /// dưới chiều cao nội dung nên chữ dòng đầu đè nửa chữ và đáy bảng cụt. Hàng bảng phải giữ chiều cao tối thiểu và
        /// để ScrollView của thân làm việc cuộn.
        /// </summary>
        [UnityTest]
        public IEnumerator Overview_UpcomingRows_KeepMinimumHeight_At820()
        {
            yield return OpenOverviewNarrow();

            UQueryBuilder<VisualElement> rows = _sessionWindow.rootVisualElement.Query(className: LiveOpsHubClassNames.OverviewUpcomingRow);
            int checkedRows = 0;
            rows.ForEach(row =>
            {
                checkedRows++;
                Assert.GreaterOrEqual(row.worldBound.height, MinimumUpcomingRowHeight - LayoutTolerance,
                    "hàng bảng 7 ngày tới bị co còn " + row.worldBound.height + " px — chữ sẽ đè nhau (UX-30)");
            });
            Assert.Greater(checkedRows, 0, "ngữ cảnh test phải có ít nhất một hàng trong bảng 7 ngày tới");
        }

        /// <summary>min-height của hàng bảng trong <c>OverviewSection.uss</c>.</summary>
        private const float MinimumUpcomingRowHeight = 26f;

        // ──────────────────────────────────────────────────────────────────────────────────── hạ tầng test

        private IEnumerator OpenOverviewNarrow()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            services.Session.RunCheckToCompletion();
            yield return OpenSessionWindow(services, LiveOpsHubSections.Ids.Overview, NarrowWidth, NarrowHeight);
        }

        private IEnumerator OpenSessionWindow(LiveOpsHubServices services)
        {
            return OpenSessionWindow(services, null, LiveOpsHubWindowTestScope.StandardWidth, LiveOpsHubWindowTestScope.StandardHeight);
        }

        private IEnumerator OpenSessionWindow(LiveOpsHubServices services, string sectionId, int width, int height)
        {
            if (_sessionWindow != null) _sessionWindow.Close();
            _sessionWindow = LiveOpsHubWindow.OpenWithServices(services, sectionId);
            // SP-16: đặt kích thước SAU Show.
            _sessionWindow.position = new Rect(0, 0, width, height);
            yield return WaitForLayout(_sessionWindow.rootVisualElement);
            yield return null;
            yield return null;
        }

        /// <summary>V-23: chỉ fail khi quá CẢ 60 khung LẪN 5 giây — máy bận (hai Unity song song) không được làm test đỏ giả.</summary>
        private static IEnumerator WaitForLayout(VisualElement element)
        {
            int frames = 0;
            double deadline = EditorApplication.timeSinceStartup + MaximumWaitSeconds;
            while (!LiveOpsHubWindowTestScope.HasLayout(element))
            {
                if (++frames > LiveOpsHubWindowTestScope.MaximumLayoutFrames && EditorApplication.timeSinceStartup > deadline)
                {
                    Assert.Fail("cửa sổ không có layout sau 60 khung và 5 giây — test UI phải chạy không -nographics");
                }
                yield return null;
            }
        }

        private static LiveOpsConfirmRequest PrefixTypeToConfirmRequest()
        {
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle("Đổi tiền tố id của đợt đang chạy")
                .WithBody("weekly-pass-35 → pass-35. Đợt đang chạy tới 14/9 00:00 UTC. Hub không biết số người chơi toàn cục đang có điểm.")
                .WithTypeToConfirm("weekly-pass-35")
                .WithButtons("Đổi tiền tố", "Giữ tiền tố cũ")
                .WithKeyHint("Esc: Giữ tiền tố cũ · Enter không đổi gì")
                .Build();
        }

        /// <summary>
        /// Câu hậu quả dài như ca thật của "dán JSON đang chạy": danh sách id bị thay + id bị bỏ nối thành một câu, không có
        /// trần số lượng. Dài hơn hẳn trần 420 px của cửa sổ hộp.
        /// </summary>
        private static LiveOpsConfirmRequest VeryLongBodyRequest()
        {
            StringBuilder body = new StringBuilder();
            for (int index = 0; index < LongBodyIdCount; index++)
            {
                if (index > 0) body.Append(", ");
                body.Append("weekly-pass-").Append(index.ToString("00", CultureInfo.InvariantCulture)).Append("-bonus-hunt");
            }
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle("Thay lịch đang chạy bằng JSON vừa dán?")
                .WithBody(body.ToString())
                .WithButtons("Thay lịch", "Giữ lịch cũ")
                .WithKeyHint("Esc: Giữ lịch cũ · Enter không đổi gì")
                .Build();
        }

        /// <summary>Đủ id để câu thân vượt trần 420 px của cửa sổ hộp ở bề rộng 400 px.</summary>
        private const int LongBodyIdCount = 60;

        private static LiveOpsConfirmRequest ShortBodyRequest()
        {
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle("Xoá hunt-0914?")
                .WithBody("Đợt chưa bắt đầu.")
                .WithButtons("Xoá đợt", "Giữ lại")
                .Build();
        }
    }
}
