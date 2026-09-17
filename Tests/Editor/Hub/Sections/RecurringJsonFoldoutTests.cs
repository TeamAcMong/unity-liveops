using System;
using System.Collections;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Foldout "JSON của luật này" ở bản SỬA ĐƯỢC của W5 (mục 7.4, [SD1 §4.1]; gỡ INTERIM I-6 của mục 12). Ba điều phải
    /// đúng: câu lỗi nói được VỊ TRÍ ("Dòng 3, ký tự 18: thiếu dấu phẩy" — PD-13), "Áp" chỉ bật khi parser của game đọc
    /// được, và bấm "Áp" đi qua ĐÚNG luật hỏi của bảng 7.0 — JSON rút ngắn đợt đang chạy vẫn phải qua hộp cấp 1.
    /// <para>
    /// Test dựng màn thật trong cửa sổ hub và bấm chuột thật (như <see cref="RecurringRulesSectionTests"/>): gọi thẳng
    /// handler thì không chứng minh được ô nhập và nút đã nối vào luồng ghi.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class RecurringJsonFoldoutTests
    {
        private const string WeeklyPassType = "weekly-pass";
        private const string OtherType = "sky-race";
        private const string PublishedPrefix = "weekly-pass-";
        private const string AnchorUtcText = "2026-01-05T00:00:00Z";

        /// <summary>Luật chạy liền mạch 24/24 neo 00:00 UTC — 08:47 của mẫu thiết kế nằm giữa đợt đang chạy.</summary>
        private const int SeamlessDayHours = 24;

        /// <summary>Rút thời gian chạy còn 20 giờ: đợt đang chạy khép sớm 4 giờ ⇒ bảng 7.0 đòi hộp cấp 1.</summary>
        private const int ShortenedActiveHours = 20;

        private const string SeamlessActiveHoursLine = "\"activeHours\": 24,";
        private const string ShortenedActiveHoursLine = "\"activeHours\": 20,";

        private const string NewConfigKey = "pass_v1";
        private const string EmptyConfigKeyLine = "\"configKey\": \"\"";
        private const string NewConfigKeyLine = "\"configKey\": \"" + NewConfigKey + "\"";

        /// <summary>
        /// JSON thiếu dấu phẩy cuối dòng 3. Dòng 3 dài đúng 17 ký tự nên chỗ phải chèn dấu phẩy là ký tự thứ 18 — câu lỗi
        /// ra đúng ví dụ của thiết kế. Ghép bằng "\n" chứ không dùng chuỗi verbatim: file nguồn checkout ở Windows sẽ có
        /// "\r\n" và số ký tự của dòng đổi theo, test thành đỏ vì chuyện chẳng liên quan.
        /// </summary>
        private static string MissingCommaOnLineThreeJson()
        {
            return string.Join("\n", new[]
            {
                "{",
                "  \"type\": \"" + WeeklyPassType + "\",",
                "  \"idPrefix\": \"x\"",
                "  \"periodHours\": 168,",
                "  \"activeHours\": 168,",
                "  \"configKey\": \"\"",
                "}",
            });
        }

        /// <summary>Câu lỗi nguyên văn của thiết kế (mục 7.4) — ghim cả vị trí lẫn lời, không chỉ "có lỗi gì đó".</summary>
        private const string MissingCommaSentence = "Dòng 3, ký tự 18: thiếu dấu phẩy";

        /// <summary>Chuỗi mà seam viết lại đưa cho parser THẬT: cú pháp hỏng nên parser của game không đọc được (V-16).</summary>
        private const string UnreadableRewrite = "{ not json";

        /// <summary>Đầu câu đầy đủ của dòng lỗi — phần đuôi là nguyên văn lời parser, không ghim vào test.</summary>
        private const string UnreadableFullSentencePrefix = "Parser của game không đọc được JSON này: ";

        /// <summary>Vòng chờ của test UI (V-23): chỉ fail khi quá CẢ 60 khung LẪN 5 giây.</summary>
        private const int MaximumWaitFrames = 60;

        private const double MaximumWaitSeconds = 5d;

        private SectionTestScope _scope;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scope != null) _scope.Dispose();
            _scope = null;
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        /// <summary>
        /// Test gác của INTERIM I-6 (mục 12): bản W4 chỉ có Label + Copy, nên chừng nào foldout còn chưa sửa được thì test
        /// này đỏ. Đo đúng ba thứ làm nên "sửa được": ô nhập không read-only, có nút "Áp", và ô mang sẵn JSON của luật.
        /// </summary>
        [UnityTest]
        public IEnumerator Foldout_IsEditable()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());
            yield return OpenJsonFoldout();

            TextField editor = _scope.View.Q<TextField>(RecurringRuleJsonFoldout.EditorElementName);
            Assert.IsNotNull(editor, "foldout JSON phải có ô NHẬP, không phải Label chỉ đọc");
            Assert.IsFalse(editor.isReadOnly, "ô JSON của W5 sửa được");
            Assert.IsNotNull(_scope.View.Q<Button>(RecurringRuleJsonFoldout.ApplyElementName), "foldout thiếu nút Áp");
            StringAssert.Contains("\"type\": \"" + WeeklyPassType + "\"", editor.value, "ô mở ra đã có sẵn JSON của luật đang chọn");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Câu lỗi phải nói CHỖ phải sửa. "JSON không hợp lệ" bắt người dùng tự dò cả object; "Dòng 3, ký tự 18" đưa họ
        /// thẳng tới dấu phẩy thiếu (PD-13).
        /// </summary>
        [UnityTest]
        public IEnumerator BrokenJson_ShowsLineAndColumn_AndBlocksApply()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());
            yield return OpenJsonFoldout();

            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.Vietnamese))
            {
                Foldout.Editor.value = MissingCommaOnLineThreeJson();
                yield return null;

                Assert.AreEqual(MissingCommaSentence, Foldout.ErrorText, "câu lỗi phải nêu đúng dòng, ký tự và lý do");
                Assert.IsTrue(Foldout.ApplySlot.IsBlocked, "JSON hỏng thì Áp phải khoá");
                Assert.AreEqual(MissingCommaSentence, Foldout.ApplySlot.Reason,
                    "lý do khoá in THÀNH CHỮ cạnh nút (SPIKE-B SP-3), không phải chỉ tooltip");
                Assert.IsNull(Foldout.Candidate, "JSON hỏng không đẻ ra luật ứng viên nào");
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Ô vừa mở chưa ai sửa: Áp khoá — nhưng vẫn phải nói vì sao, không đứng im.</summary>
        [UnityTest]
        public IEnumerator UnchangedJson_BlocksApplyWithReason()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());
            yield return OpenJsonFoldout();

            Assert.IsTrue(Foldout.ApplySlot.IsBlocked);
            Assert.AreEqual(LiveOpsHubStrings.RecurringJsonUnchangedReason, Foldout.ApplySlot.Reason);
            Assert.AreEqual(string.Empty, Foldout.ErrorText, "chưa sửa gì thì không phải LỖI — không tô dòng đỏ dưới ô");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Đổi <c>"type"</c> trong JSON = luật của loại khác hẳn. Ô "Loại event" trên form khoá sau khi tạo vì đúng lý do
        /// đó; cho đổi vòng qua JSON thì khoá kia thành trang trí.
        /// </summary>
        [UnityTest]
        public IEnumerator JsonChangingEventType_BlocksApplyWithReason()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());
            yield return OpenJsonFoldout();

            Foldout.Editor.value = Foldout.Editor.value.Replace("\"" + WeeklyPassType + "\"", "\"" + OtherType + "\"");
            yield return null;

            Assert.IsTrue(Foldout.ApplySlot.IsBlocked, "đổi loại trong JSON không áp được từ đây");
            StringAssert.Contains(WeeklyPassType, Foldout.ErrorText, "câu lỗi nói rõ luật này là của loại nào");
            Assert.IsNull(Foldout.Candidate);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Dán cả mảng <c>recurring</c> vào ô một-luật: nói thẳng chỗ này nhận đúng một object.</summary>
        [UnityTest]
        public IEnumerator JsonWithTwoRules_BlocksApplyWithReason()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());
            yield return OpenJsonFoldout();

            string ruleObject = Foldout.Editor.value;
            Foldout.Editor.value = ruleObject + "," + ruleObject;
            yield return null;

            Assert.IsTrue(Foldout.ApplySlot.IsBlocked);
            Assert.AreEqual(LiveOpsHubStrings.RecurringJsonNotOneRuleReason, Foldout.ErrorText);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Dán CẢ MẢNG <c>recurring</c> (có ngoặc vuông) vào ô một-luật: câu hiện ra phải là câu thân thiện đã hứa sẵn
        /// "không phải mảng", không phải câu trần của <c>JsonUtility</c> — mảng bọc thêm một lớp nữa sẽ rơi vào nhánh
        /// "parser không đọc được" và người dùng nhận một câu chẳng chỉ ra chỗ nào sai.
        /// </summary>
        [UnityTest]
        public IEnumerator JsonWithArray_BlocksApplyWithReason()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());
            yield return OpenJsonFoldout();

            Foldout.Editor.value = "[" + Foldout.Editor.value + "]";
            yield return null;

            Assert.IsTrue(Foldout.ApplySlot.IsBlocked);
            Assert.AreEqual(LiveOpsHubStrings.RecurringJsonNotOneRuleReason, Foldout.ErrorText);
            Assert.IsNull(Foldout.Candidate);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// JSON đọc được và KHÔNG đụng gì của người chơi (chỉ đổi <c>configKey</c>: id giữ nguyên, giờ khép giữ nguyên) thì
        /// ghi thẳng kèm toast có Hoàn tác (bảng 7.0). Đây cũng là chỗ chứng minh nút Áp thật sự nối vào lệnh sửa của phiên.
        /// </summary>
        [UnityTest]
        public IEnumerator ApplyHarmlessChange_WritesImmediatelyWithToast()
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter = new ScriptedLiveOpsHubConfirmationPresenter();
            yield return OpenSeamlessDailySample(presenter);
            yield return OpenJsonFoldout();
            LiveOpsToastModel toast = null;
            Section.Services.Bus.ToastRequested += model => toast = model;

            StringAssert.Contains(EmptyConfigKeyLine, Foldout.Editor.value, "mẫu 24/24 chưa đặt configKey riêng");
            Foldout.Editor.value = Foldout.Editor.value.Replace(EmptyConfigKeyLine, NewConfigKeyLine);
            yield return null;
            Assert.IsFalse(Foldout.ApplySlot.IsBlocked, "JSON đọc được thì Áp phải bật");

            yield return ClickButton(RecurringRuleJsonFoldout.ApplyElementName);

            Assert.AreEqual(0, presenter.Requests.Count, "đổi configKey không lấy đi gì của người chơi — không hỏi (bảng 7.0)");
            Assert.AreEqual(NewConfigKey, WrittenRule().ConfigKey, "Áp ghi thẳng vào asset");
            Assert.IsNotNull(toast, "mọi lệnh ghi thẳng đều có toast kèm Hoàn tác");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Nghiệm thu 10.3: JSON rút ngắn thời gian chạy của lần lặp ĐANG CHẠY → hộp cấp 1. Suy thao tác theo một ô duy
        /// nhất (JSON đổi nhiều field cùng lúc nên trông như đổi danh tính) thì policy trả None và đợt đang chạy bị khép
        /// sớm mà KHÔNG hộp nào — đúng thứ bảng 7.0 cấm.
        /// </summary>
        [UnityTest]
        public IEnumerator ApplyShorteningRunningActiveHours_AsksLevel1()
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter = new ScriptedLiveOpsHubConfirmationPresenter();
            yield return OpenSeamlessDailySample(presenter);
            yield return OpenJsonFoldout();

            Foldout.Editor.value = WithActiveHoursLine(Foldout.Editor.value, ShortenedActiveHoursLine);
            yield return null;
            yield return ClickButton(RecurringRuleJsonFoldout.ApplyElementName);

            Assert.IsTrue(Section.Draft.NeedsConfirmation, "bước một: giữ nháp tại ô, Main.asset chưa đổi");
            Assert.AreEqual(LiveOpsConfirmRequirement.Level1, Section.Draft.Requirement, "rút ngắn đợt đang chạy = hộp cấp 1");
            Assert.AreEqual(SeamlessDayHours, WrittenRule().ActiveHours, "chưa xác nhận thì chưa ghi");

            presenter.Enqueue(LiveOpsConfirmResult.Destructive);
            yield return ClickButton(RecurringRuleForm.DraftWriteElementName);

            Assert.AreEqual(1, presenter.Requests.Count, "đúng một hộp, và là hộp của bước hai");
            Assert.AreEqual(LiveOpsConfirmLevel.Level1, presenter.Requests[0].Level);
            Assert.AreEqual(ShortenedActiveHours, WrittenRule().ActiveHours, "xác nhận xong mới ghi");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Phiên phát <c>DocumentChanged</c>/<c>CheckChanged</c> liên tục (kiểm chạy nền, Undo, đổi đĩa) và mỗi lần là một
        /// lần vẽ lại form. Đè chữ trong ô mỗi lượt thì JSON đang gõ dở biến mất giữa chừng mà không ai bấm gì.
        /// </summary>
        [UnityTest]
        public IEnumerator EditedJson_SurvivesRefresh()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());
            yield return OpenJsonFoldout();

            Foldout.Editor.value = MissingCommaOnLineThreeJson();
            yield return null;
            Section.Refresh();
            yield return null;

            Assert.AreEqual(MissingCommaOnLineThreeJson(), Foldout.Editor.value, "vẽ lại form không được nuốt JSON đang gõ dở");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Đổi sang luật khác thì phần gõ dở PHẢI mất: nó là JSON của luật cũ, giữ lại là mời áp nhầm luật.</summary>
        [UnityTest]
        public IEnumerator SelectingAnotherRule_DropsEditedJson()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());
            yield return OpenJsonFoldout();
            Foldout.Editor.value = MissingCommaOnLineThreeJson();
            yield return null;

            Section.ApplyNavigation(LiveOpsHubNavigation.To(Section.Id).WithEventType(OtherType, false));
            yield return null;

            StringAssert.Contains("\"type\": \"" + OtherType + "\"", Foldout.Editor.value, "ô phải hiện JSON của luật vừa chọn");
            Assert.IsTrue(Foldout.ApplySlot.IsBlocked, "luật mới, chưa ai sửa gì — Áp khoá lại");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Q-W5-3 (user chốt 17/9/2026): khi parser của game không đọc được JSON, cạnh nút "Áp" chỉ in CÂU NGẮN
        /// ("JSON chưa đọc được") còn CÂU ĐẦY ĐỦ — có nguyên văn lời của parser — ở lại dòng lỗi dưới ô. Một khung nhìn
        /// không in hai lần cùng một câu; test đếm số lần câu đầy đủ xuất hiện trong CẢ cây element để luật đó không lặng lẽ
        /// mất đi khi ai đó nối thêm một nhãn nữa.
        /// </summary>
        [UnityTest]
        public IEnumerator UnreadableJson_ReasonBesideApply_IsShortSentence_ErrorLineKeepsFullSentence()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter(),
                new RewritingLiveOpsHubJsonReadBack(text => UnreadableRewrite));
            yield return OpenJsonFoldout();

            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.Vietnamese))
            {
                // Vẫn là JSON hợp lệ, chỉ khác chuỗi đang ghi — bộ dò cú pháp và cửa "phải là object" đều qua, nên ca rơi
                // đúng vào nhánh "parser không đọc được".
                Foldout.Editor.value = Foldout.Editor.value + "\n";
                yield return null;

                Assert.IsTrue(Foldout.ApplySlot.IsBlocked, "parser không đọc được thì Áp phải khoá");
                Assert.AreEqual(LiveOpsHubStrings.RecurringJsonUnreadableShortReason, Foldout.ApplySlot.Reason,
                    "cạnh nút chỉ đủ chỗ cho câu ngắn (Q-W5-3)");
                StringAssert.StartsWith(UnreadableFullSentencePrefix, Foldout.ErrorText,
                    "dòng lỗi dưới ô giữ câu đầy đủ, kèm nguyên văn lời của parser");
                Assert.AreNotEqual(Foldout.ApplySlot.Reason, Foldout.ErrorText, "hai chỗ phải là hai câu khác nhau");
                Assert.AreEqual(1, CountLabelsWithText(_scope.Window.rootVisualElement, Foldout.ErrorText),
                    "câu đầy đủ chỉ được xuất hiện ĐÚNG MỘT lần trong cây element");
                Assert.IsNull(Foldout.Candidate, "parser không đọc được thì không đẻ ra luật ứng viên nào");
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Đếm nhãn mang đúng một câu — dùng để chặn ca "in hai lần cùng một câu trong một khung nhìn".</summary>
        private static int CountLabelsWithText(VisualElement root, string text)
        {
            int count = 0;
            foreach (Label label in root.Query<Label>().ToList())
            {
                if (string.Equals(label.text, text, StringComparison.Ordinal)) count++;
            }
            return count;
        }

        private RecurringRulesSection Section => (RecurringRulesSection)_scope.Section;

        private RecurringRuleJsonFoldout Foldout => Section.Form.JsonFoldout;

        /// <summary>Thay đúng dòng activeHours của object JSON; assert tại chỗ để test không âm thầm đo một JSON không đổi.</summary>
        private static string WithActiveHoursLine(string json, string replacement)
        {
            StringAssert.Contains(SeamlessActiveHoursLine, json, "mẫu 24/24 phải ghi activeHours 24 trong JSON");
            return json.Replace(SeamlessActiveHoursLine, replacement);
        }

        private RecurringLiveEventRule WrittenRule()
        {
            RecurringLiveEventRule rule;
            Assert.IsTrue(Section.Services.Session.Document.TryGetRecurringRule(WeeklyPassType, out rule));
            return rule;
        }

        /// <summary>Mở foldout rồi chờ layout: nội dung của Foldout đóng không có khung, bấm chuột lên nó không trúng gì.</summary>
        private IEnumerator OpenJsonFoldout()
        {
            Foldout.value = true;
            yield return _scope.WaitForLayout();
            yield return LiveOpsHubWindowTestScope.WaitForLayout(Foldout.Editor);
        }

        /// <summary>Chuột thật lên một nút của màn: chờ nút có layout rồi gửi MouseDown/MouseUp vào cửa sổ.</summary>
        private IEnumerator ClickButton(string elementName)
        {
            Button button = _scope.View.Q<Button>(elementName);
            Assert.IsNotNull(button, "màn thiếu nút '" + elementName + "'");
            yield return LiveOpsHubWindowTestScope.WaitForLayout(button);
            yield return WaitForButtonBounds(button);
            Vector2 center = button.worldBound.center;
            _scope.Window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = center, button = 0, clickCount = 1 });
            _scope.Window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = center, button = 0, clickCount = 1 });
            yield return null;
        }

        /// <summary>
        /// Chờ nút có bề rộng thật (V-23: quá CẢ 60 khung LẪN 5 giây mới fail). Foldout vừa mở ra thì lượt layout đầu tiên
        /// có thể còn trả khung 0×0, và bấm vào khung 0×0 là bấm trượt mà test lại đọc thành "nút không nối".
        /// </summary>
        private static IEnumerator WaitForButtonBounds(VisualElement button)
        {
            int frames = 0;
            double startedAt = EditorApplication.timeSinceStartup;
            while (button.worldBound.width <= 0f || button.worldBound.height <= 0f)
            {
                bool framesExhausted = ++frames > MaximumWaitFrames;
                bool secondsExhausted = EditorApplication.timeSinceStartup - startedAt > MaximumWaitSeconds;
                if (framesExhausted && secondsExhausted) Assert.Fail("nút trong foldout không có khung sau 60 khung và 5 giây");
                yield return null;
            }
        }

        private IEnumerator OpenDesignSample(ScriptedLiveOpsHubConfirmationPresenter presenter)
        {
            return OpenDesignSample(presenter, null);
        }

        /// <param name="jsonReadBack">
        /// (V-16) Kịch bản "parser của game không đọc được" dựng bằng cách VIẾT LẠI đầu vào rồi vẫn chạy parser thật, chứ
        /// không giả kết quả — hub và game phải hỏng ở cùng một chỗ.
        /// </param>
        private IEnumerator OpenDesignSample(ScriptedLiveOpsHubConfirmationPresenter presenter, ILiveOpsHubJsonReadBack jsonReadBack)
        {
            LiveOpsHubServicesBuilder builder = LiveOpsHubTestServices.CreateBuilder(null)
                .WithConfirmation(presenter)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document));
            if (jsonReadBack != null) builder.WithJsonReadBack(jsonReadBack);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(builder);
            _scope = SectionTestScope.Open(new RecurringRulesSection(services));
            yield return _scope.WaitForLayout();
        }

        /// <summary>
        /// Một luật DUY NHẤT chạy liền mạch 24/24 neo 00:00 UTC: 08:47 nằm giữa đợt đang chạy, nên rút "Chạy mỗi đợt" là
        /// hậu quả đo được ("khép sớm 4 giờ"), không lẫn với đổi id.
        /// </summary>
        private IEnumerator OpenSeamlessDailySample(ScriptedLiveOpsHubConfirmationPresenter presenter)
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(WeeklyPassType, "Pass tuần", 1, false, string.Empty))
                .WithRecurringRule(new RecurringLiveEventRule(WeeklyPassType, AnchorUtcText, PublishedPrefix, SeamlessDayHours,
                    SeamlessDayHours, string.Empty))
                .Build();
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithConfirmation(presenter)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document)));
            _scope = SectionTestScope.Open(new RecurringRulesSection(services));
            yield return _scope.WaitForLayout();
        }
    }
}
