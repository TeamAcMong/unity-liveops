using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Mở một cửa sổ LiveOps Hub thật cho test UI (9.1): <c>OpenForTest</c> (CreateInstance + Show, không GetWindow), đặt kích thước
    /// SAU khi Show (SP-16: đặt trước bị kẹp ≈ 401×202), chờ layout bằng vòng đếm khung thay vì số khung cố định, và đóng cửa sổ
    /// ở <see cref="Dispose"/>. Test gọi <c>Dispose</c> trong <c>[TearDown]</c> để cửa sổ không sót qua test sau khi assert fail.
    /// </summary>
    internal sealed class LiveOpsHubWindowTestScope : IDisposable
    {
        internal const int StandardWidth = 1280;
        internal const int StandardHeight = 760;
        internal const int MaximumLayoutFrames = 60;

        private LiveOpsHubWindowTestScope(LiveOpsHubWindow window, ManualLiveOpsHubCompilationState compilationState, IReadOnlyList<IHubSection> sections)
        {
            Window = window;
            CompilationState = compilationState;
            Sections = sections;
        }

        public LiveOpsHubWindow Window { get; private set; }
        public ManualLiveOpsHubCompilationState CompilationState { get; }
        public IReadOnlyList<IHubSection> Sections { get; }

        /// <param name="sections">null = registry thật (<see cref="LiveOpsHubSections.Create"/>).</param>
        public static LiveOpsHubWindowTestScope Open(IReadOnlyList<IHubSection> sections = null, bool isCompiling = false,
            ILiveOpsHubLayoutLoader layoutLoader = null, string sectionId = null, int width = StandardWidth, int height = StandardHeight)
        {
            IReadOnlyList<IHubSection> chosen = sections ?? LiveOpsHubSections.Create();
            ManualLiveOpsHubCompilationState compilationState = new ManualLiveOpsHubCompilationState(isCompiling);
            LiveOpsHubWindow window = LiveOpsHubWindow.OpenForTest(chosen, compilationState, layoutLoader, sectionId);
            window.position = new Rect(0, 0, width, height);
            return new LiveOpsHubWindowTestScope(window, compilationState, chosen);
        }

        /// <summary>Chờ tới khi root có bề rộng thật; quá 60 khung là fail kèm lý do hay gặp (chạy -nographics).</summary>
        public IEnumerator WaitForLayout()
        {
            int frames = 0;
            while (!HasLayout(Window.rootVisualElement))
            {
                if (++frames > MaximumLayoutFrames)
                {
                    Assert.Fail("cửa sổ hub không có layout sau 60 khung — test UI phải chạy không -nographics");
                }
                yield return null;
            }
            // Thêm hai khung để GeometryChangedEvent (breakpoints) và CustomStyleResolvedEvent (probe skin) đã bắn.
            yield return null;
            yield return null;
        }

        /// <summary>Chờ tới khi <paramref name="element"/> có layout (vd hàng rail vừa dựng lại).</summary>
        public static IEnumerator WaitForLayout(VisualElement element)
        {
            int frames = 0;
            while (!HasLayout(element))
            {
                if (++frames > MaximumLayoutFrames) Assert.Fail("element '" + element.name + "' không có layout sau 60 khung");
                yield return null;
            }
        }

        public static IEnumerator WaitFrames(int count)
        {
            for (int frame = 0; frame < count; frame++) yield return null;
        }

        public static bool HasLayout(VisualElement element)
        {
            if (element == null) return false;
            Rect layout = element.layout;
            return !float.IsNaN(layout.width) && layout.width > 0f && !float.IsNaN(layout.height) && layout.height > 0f;
        }

        public void Dispose()
        {
            if (Window != null) Window.Close();
            Window = null;
        }
    }
}
