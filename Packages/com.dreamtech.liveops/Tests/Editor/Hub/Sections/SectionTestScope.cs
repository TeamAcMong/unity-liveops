using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Dựng MỘT màn trong cửa sổ hub thật (đủ token hai skin, stylesheet khung, host đã Bind) rồi chờ layout — cho test UI của
    /// từng màn (W4). Vì sao không dựng view trần: token chỉ khai trên <c>.liveops-hub-root</c> và custom property không kế thừa
    /// ([API §12.2]), nên view dựng ngoài cửa sổ hub ra màu sai và <c>resolvedStyle</c> không đáng tin.
    /// </summary>
    internal sealed class SectionTestScope : IDisposable
    {
        private readonly LiveOpsHubWindowTestScope _windowScope;

        private SectionTestScope(LiveOpsHubWindowTestScope windowScope, IHubSection section)
        {
            _windowScope = windowScope;
            Section = section;
        }

        public IHubSection Section { get; }
        public LiveOpsHubWindow Window => _windowScope.Window;

        /// <summary>View của màn đang nằm trong thân (null khi màn ném — test đọc <see cref="IsFailed"/>).</summary>
        public VisualElement View => Window.SectionBody == null || Window.SectionBody.childCount == 0 ? null : Window.SectionBody[0];

        public bool IsFailed => Window.IsSectionFailed(Section.Id);

        public static SectionTestScope Open(IHubSection section, int width = LiveOpsHubWindowTestScope.StandardWidth,
            int height = LiveOpsHubWindowTestScope.StandardHeight)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            LiveOpsHubWindowTestScope windowScope = LiveOpsHubWindowTestScope.Open(new List<IHubSection> { section }, false, null, section.Id, width, height);
            return new SectionTestScope(windowScope, section);
        }

        public IEnumerator WaitForLayout()
        {
            yield return _windowScope.WaitForLayout();
            if (View != null) yield return LiveOpsHubWindowTestScope.WaitForLayout(View);
        }

        /// <summary>Mọi tên trong <see cref="IHubSection.RequiredElementNames"/> Q được trong view; trượt thì fail nêu đúng tên.</summary>
        public void AssertRequiredElementsPresent()
        {
            Assert.IsFalse(IsFailed, "màn '" + Section.Id + "' ném khi dựng — không có view để tìm element");
            foreach (string elementName in Section.RequiredElementNames)
            {
                Assert.IsNotNull(View.Q(elementName), "màn '" + Section.Id + "' thiếu element '" + elementName + "' — UXML và RequiredElementNames lệch nhau");
            }
        }

        public void Dispose()
        {
            _windowScope.Dispose();
        }
    }
}
