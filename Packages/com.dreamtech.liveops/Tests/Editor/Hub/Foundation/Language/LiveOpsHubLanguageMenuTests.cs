using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Menu chọn ngôn ngữ ở header (§1.4 đặc tả G-I18N): liệt kê đủ ngôn ngữ bằng TÊN GỐC, và đổi ngôn ngữ dựng lại cửa sổ
    /// ngay, không phải đóng mở lại.
    /// <para>
    /// Chứng cứ "đã dựng lại" là CHỮ THÂN cửa sổ đổi: nhãn rail (<c>ShellRailCaption</c>) và tiêu đề màn Tổng quan đọc catalog
    /// như mọi Label khác, nên nhãn menu đổi mà chữ thân không đổi là hỏng. Một test riêng đi đúng đường người dùng
    /// (<c>Set</c> → <c>Changed</c>) để chứng minh cửa sổ có nghe sự kiện, không chỉ có seam cho test.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class LiveOpsHubLanguageMenuTests
    {
        // Hai câu này là chữ NGƯỜI DÙNG ĐỌC, cố tình viết thẳng trong test: assert phải vỡ khi ai đó sửa bản dịch mà không
        // biết ảnh chụp/kỳ vọng nào đang phụ thuộc vào nó — so với LiveOpsHubStrings thì hai vế cùng đổi, test vô nghĩa.
        private const string VietnameseRailCaption = "ĐƯỜNG ĐI CỦA LỊCH";
        private const string EnglishRailCaption = "CALENDAR PATH";

        private LiveOpsHubWindowTestScope _scope;
        private bool _hadStoredPreference;
        private string _storedPreference;

        [SetUp]
        public void SetUp()
        {
            // Test đổi ngôn ngữ đi qua Set nên chạm pref THẬT của máy người chạy — chụp lại để TearDown trả nguyên trạng.
            _hadStoredPreference = EditorPrefs.HasKey(LiveOpsHubLanguage.EditorPreferenceKey);
            _storedPreference = EditorPrefs.GetString(LiveOpsHubLanguage.EditorPreferenceKey, string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _scope = null;
            LiveOpsHubTestServices.ReleaseAll();
            if (_hadStoredPreference) EditorPrefs.SetString(LiveOpsHubLanguage.EditorPreferenceKey, _storedPreference);
            else EditorPrefs.DeleteKey(LiveOpsHubLanguage.EditorPreferenceKey);
            LiveOpsHubLanguage.ReloadFromPreference();
        }

        [UnityTest]
        public IEnumerator Menu_ListsEveryAvailableLanguage_WithNativeName()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();

            ToolbarMenu menu = FindLanguageMenu(_scope.Window.rootVisualElement);
            Assert.IsNotNull(menu, "header phải có menu chọn ngôn ngữ (class " + LiveOpsHubClassNames.LanguageMenu + ")");

            List<string> itemNames = new List<string>();
            foreach (DropdownMenuItem item in menu.menu.MenuItems())
            {
                DropdownMenuAction action = item as DropdownMenuAction;
                if (action != null) itemNames.Add(action.name);
            }

            Assert.AreEqual(LiveOpsHubLanguage.Available.Count, itemNames.Count, "mỗi ngôn ngữ đúng một mục");
            foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
            {
                CollectionAssert.Contains(itemNames, LiveOpsHubLanguage.NativeName(language),
                    "mục menu phải là tên gốc của ngôn ngữ, không dịch");
            }

            Assert.AreEqual(LiveOpsHubLanguage.ShortCode(LiveOpsHubLanguage.Current), menu.text,
                "nhãn menu là mã ngắn của ngôn ngữ đang dùng");
        }

        [UnityTest]
        public IEnumerator ChangingLanguage_RebuildsWindowText_WithoutReopen()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();

            ToolbarMenu menuBefore = FindLanguageMenu(_scope.Window.rootVisualElement);
            Assert.IsNotNull(menuBefore);
            Assert.AreEqual(LiveOpsHubLanguage.ShortCode(LiveOpsHubLanguageId.Vietnamese), menuBefore.text,
                "fixture ghim Tiếng Việt nên cửa sổ mở ra đang ở VI");
            Label captionBefore = FindRailCaption(_scope.Window.rootVisualElement);
            Assert.IsNotNull(captionBefore, "rail phải có nhãn " + LiveOpsHubPaths.ShellElementNames.RailCaption);
            Assert.AreEqual(VietnameseRailCaption, captionBefore.text, "chữ thân cửa sổ đang là bản tiếng Việt");

            // Scope ghim của fixture đang thắng pref, nên đổi "ngôn ngữ đang thấy" bằng một scope lồng + tự dựng lại như
            // chính tay xử lý sự kiện của cửa sổ làm: test này kiểm chữ sau khi dựng lại, đường Set → Changed có test riêng.
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
            {
                _scope.Window.RebuildForLanguageChangeForTest();
                yield return _scope.WaitForLayout();

                ToolbarMenu menuAfter = FindLanguageMenu(_scope.Window.rootVisualElement);
                Assert.IsNotNull(menuAfter, "dựng lại xong header vẫn phải có menu ngôn ngữ");
                Assert.AreNotSame(menuBefore, menuAfter, "khung phải được dựng lại, không phải cái cũ còn đó");
                Assert.AreEqual(LiveOpsHubLanguage.ShortCode(LiveOpsHubLanguageId.English), menuAfter.text,
                    "nhãn menu phải theo ngôn ngữ mới ngay, không cần đóng mở lại cửa sổ");

                Label captionAfter = FindRailCaption(_scope.Window.rootVisualElement);
                Assert.IsNotNull(captionAfter);
                Assert.AreEqual(EnglishRailCaption, captionAfter.text,
                    "CHỮ THÂN phải đổi theo, không chỉ nhãn menu — nhãn menu đổi mà Label vẫn tiếng Việt là chưa dịch/chưa dựng lại");
                Assert.AreEqual("Overview", LiveOpsHubStrings.ShellOverviewTitle,
                    "chữ của màn cũng phải theo ngôn ngữ mới — không riêng gì nhãn rail");
            }
        }

        /// <summary>
        /// Đường người dùng thật: bấm mục menu → <see cref="LiveOpsHubLanguage.Set"/> → <c>Changed</c> → cửa sổ tự dựng lại.
        /// Fixture ghim Tiếng Việt nên chữ không đổi được ở đây (scope thắng pref); thứ test này chứng minh là cửa sổ CÓ nghe
        /// sự kiện — không có đăng ký thì khung cũ còn nguyên.
        /// </summary>
        [UnityTest]
        public IEnumerator SettingLanguage_RaisesChanged_AndWindowRebuildsItself()
        {
            EditorPrefs.SetString(LiveOpsHubLanguage.EditorPreferenceKey, LiveOpsHubLanguageId.English.ToString());
            LiveOpsHubLanguage.ReloadFromPreference();
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();

            ToolbarMenu menuBefore = FindLanguageMenu(_scope.Window.rootVisualElement);
            Assert.IsNotNull(menuBefore);

            LiveOpsHubLanguage.Set(LiveOpsHubLanguageId.Vietnamese);
            yield return _scope.WaitForLayout();

            Assert.AreEqual(LiveOpsHubLanguageId.Vietnamese, LiveOpsHubLanguage.PersistedLanguage, "Set phải ghi pref");
            ToolbarMenu menuAfter = FindLanguageMenu(_scope.Window.rootVisualElement);
            Assert.IsNotNull(menuAfter, "dựng lại xong header vẫn phải có menu ngôn ngữ");
            Assert.AreNotSame(menuBefore, menuAfter, "cửa sổ phải nghe LiveOpsHubLanguage.Changed và tự dựng lại");
        }

        [Test]
        public void PinnedVietnameseFixture_KeepsExistingGoldenText()
        {
            Assert.AreEqual(LiveOpsHubLanguageId.Vietnamese, LiveOpsHubLanguage.Current,
                "890 test đang có so đúng câu tiếng Việt — mất ghim là đỏ hàng loạt");
            Assert.AreEqual("Tổng quan", LiveOpsHubStrings.ShellOverviewTitle);
            Assert.AreEqual("Hoàn tác", LiveOpsHubStrings.FeedbackToastUndoLabel);
        }

        private static ToolbarMenu FindLanguageMenu(VisualElement root)
        {
            return root?.Q<ToolbarMenu>(null, LiveOpsHubClassNames.LanguageMenu);
        }

        private static Label FindRailCaption(VisualElement root)
        {
            return root?.Q<Label>(LiveOpsHubPaths.ShellElementNames.RailCaption);
        }
    }
}
