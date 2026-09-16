using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Menu chọn ngôn ngữ ở header (§1.4 đặc tả G-I18N): liệt kê đủ ngôn ngữ bằng TÊN GỐC, và đổi ngôn ngữ dựng lại cửa sổ
    /// ngay, không phải đóng mở lại.
    /// <para>
    /// Ở bước 1, bản tiếng Anh còn trống nên chữ thân cửa sổ vẫn rơi về bản gốc — chứng cứ "đã dựng lại" là nhãn menu đổi
    /// (khoá dùng chung, có đủ hai ngôn ngữ) và element khung là instance mới. Sau bước dịch, siết thêm assert so chữ Label.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class LiveOpsHubLanguageMenuTests
    {
        private LiveOpsHubWindowTestScope _scope;

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _scope = null;
            LiveOpsHubTestServices.ReleaseAll();
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
        public IEnumerator ChangingLanguage_RebuildsWindow_WithoutReopen()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();

            ToolbarMenu menuBefore = FindLanguageMenu(_scope.Window.rootVisualElement);
            Assert.IsNotNull(menuBefore);
            Assert.AreEqual(LiveOpsHubLanguage.ShortCode(LiveOpsHubLanguageId.Vietnamese), menuBefore.text,
                "fixture ghim Tiếng Việt nên cửa sổ mở ra đang ở VI");

            // Scope ghim của fixture đang thắng pref, nên đổi "ngôn ngữ đang thấy" bằng một scope lồng + tự dựng lại như
            // chính tay xử lý sự kiện của cửa sổ làm: test này kiểm đường dựng lại, không kiểm đường ghi pref (đã có test riêng).
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
            {
                _scope.Window.RebuildForLanguageChangeForTest();
                yield return _scope.WaitForLayout();

                ToolbarMenu menuAfter = FindLanguageMenu(_scope.Window.rootVisualElement);
                Assert.IsNotNull(menuAfter, "dựng lại xong header vẫn phải có menu ngôn ngữ");
                Assert.AreNotSame(menuBefore, menuAfter, "khung phải được dựng lại, không phải cái cũ còn đó");
                Assert.AreEqual(LiveOpsHubLanguage.ShortCode(LiveOpsHubLanguageId.English), menuAfter.text,
                    "nhãn menu phải theo ngôn ngữ mới ngay, không cần đóng mở lại cửa sổ");
            }
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
    }
}
