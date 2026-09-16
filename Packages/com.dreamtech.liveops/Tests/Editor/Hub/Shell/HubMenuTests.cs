using System.Collections;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Bề mặt "chrome" của khung mà G-SHELLPOLISH (W5) dựng nốt: menu ⋮ của cửa sổ (8.3, [FD §3.3]), cửa sổ dữ liệu mẫu, HelpBox
    /// "Có 2 LiveEventCalendarAsset…" của Tổng quan (mục 12 I-9) và cảnh báo thiếu stylesheet sau khi ba sheet feedback/controls/
    /// timeline thành BẮT BUỘC (mục 12 I-10).
    /// <para>
    /// Vì sao I-10 nằm ở đây chứ không ở một file riêng: 10.3 giao cho gói đúng ba file test
    /// (<c>HubNarrowTests</c>, <c>HubDiskConflictTests</c>, <c>HubMenuTests</c>), và file này là file "chrome còn lại" của khung —
    /// cùng chỗ với <c>Overview_MultipleAssetsHelpBox</c> mà mục 12 I-9 đã đặt tên sẵn.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubMenuTests
    {
        private LiveOpsHubWindowTestScope _scope;

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _scope = null;
        }

        /// <summary>
        /// (mục 12 I-10) Ba stylesheet feedback/controls/timeline không còn là "chưa tới lượt": thiếu một cái là bản cài hỏng, nên
        /// cửa sổ phải <c>LogWarning</c> NÊU ĐÚNG ĐƯỜNG DẪN — im lặng thì người dùng chỉ thấy hub mất style mà không biết tìm ở đâu.
        /// </summary>
        [UnityTest]
        public IEnumerator MissingOptionalSheet_WarnsWithPath()
        {
            string missingPath = LiveOpsHubPaths.ControlsUss;
            LogAssert.Expect(LogType.Warning, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ShellMissingStyleSheetWarningFormat, missingPath));

            _scope = LiveOpsHubWindowTestScope.Open(layoutLoader: new MissingPathsLiveOpsHubLayoutLoader(new[] { missingPath }));
            yield return _scope.WaitForLayout();

            // Khung vẫn dựng được (cảnh báo, không phải card lỗi): thiếu style không được biến thành cửa sổ trắng.
            Assert.IsNotNull(_scope.Window.HubRoot, "thiếu một stylesheet không làm sập khung — chỉ cảnh báo");
            Assert.IsNotNull(_scope.Window.HubRoot.Q(LiveOpsHubPaths.ShellElementNames.Rail));
        }

        /// <summary>Ba sheet của gói khác đã vào nhóm bắt buộc — cờ là hợp đồng với probe CLI, không chỉ với cửa sổ.</summary>
        [Test]
        public void ShellStyleSheets_AllRequired()
        {
            foreach (LiveOpsHubPaths.ShellStyleSheet sheet in LiveOpsHubPaths.ShellStyleSheetLoadOrder)
            {
                Assert.IsTrue(sheet.IsRequired, "stylesheet " + sheet.Path + " phải bắt buộc từ W5 (mục 12 I-10)");
            }
        }
    }
}
