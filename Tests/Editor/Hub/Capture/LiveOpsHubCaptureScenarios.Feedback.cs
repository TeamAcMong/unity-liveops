using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Feedback (G-FEEDBACK, W2) — bố cục sớm trước khi có phiên lịch:
    /// <list type="bullet">
    /// <item><c>hf-toast-bare</c>: cửa sổ hub trần (một màn giả, không phiên) với toast Hoàn tác là con cuối của cột nội dung, đúng chỗ G-HOSTUI sẽ
    /// gắn ở W4 (Hình 7). Nhóm Undo là thao tác thật trên asset trong bộ nhớ, để nút Hoàn tác bật vì group còn trên đỉnh — không ép trạng thái nút.</item>
    /// <item><c>hf-confirm-level1-layout</c> / <c>hf-confirm-level2-layout</c>: hộp 400×212 / 400×290 với chữ mẫu của test, mở KHÔNG modal
    /// (modal chặn batchmode) — cùng nội dung, cùng kích thước với hộp thật.</item>
    /// </list>
    /// Chuyển động tắt trong ảnh (class <c>--no-motion</c>) và đồng hồ toast đứng yên: ảnh chụp trạng thái đã hiện, không phải một khung
    /// giữa transition 120ms hay toast đã tự tắt khi lượt chụp chậm quá 6 giây.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        private const string ToastSectionId = "toast-bare";
        private const float ToastHeight = 24f;
        private const string ToastMessage = "Đã dời kết thúc lava-quest-2026-09b 19/9 → 20/9 00:00 UTC";

        static partial void RegisterFeedback(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HfToastBare, StandardWidth, StandardHeight, OpenToastBare)
                .WithExpectedFrames(
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Rail, 196f, 0f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Header, 0f, 26f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Content, 1084f, 0f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Toast, 0f, ToastHeight)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HfConfirmLevel1Layout, (int)LiveOpsConfirmWindow.Width,
                    (int)LiveOpsConfirmWindow.Level1Height, () => OpenConfirm(ConfirmLevel1SampleRequest(), string.Empty),
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsConfirmContent.RootElementName, LiveOpsConfirmWindow.Width,
                    LiveOpsConfirmWindow.Level1Height)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HfConfirmLevel2Layout, (int)LiveOpsConfirmWindow.Width,
                    (int)LiveOpsConfirmWindow.TypeToConfirmHeight, () => OpenConfirm(ConfirmLevel2SampleRequest(), "weekly-pass-3"),
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsConfirmContent.RootElementName, LiveOpsConfirmWindow.Width,
                    LiveOpsConfirmWindow.TypeToConfirmHeight)));
        }

        /// <summary>Hộp 2 của Hình 8 (cấp 1 — xoá đợt đã đăng chưa bắt đầu), chữ nguyên văn [FD §3.10].</summary>
        internal static LiveOpsConfirmRequest ConfirmLevel1SampleRequest()
        {
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle("Xoá hunt-0914?")
                .WithBody("Đợt chưa bắt đầu (14/9 00:00 → 17/9 00:00 UTC) nhưng đã có trong bản đăng 11/9 16:20: lần đăng tới người chơi sẽ " +
                          "không thấy đợt này. hunt-0916-bonus hết chồng giờ và sẽ mở từ 16/9 12:00. Hoàn tác được bằng ⌘Z tới khi đóng Unity.")
                .WithButtons("Xoá đợt", "Giữ lại")
                .Build();
        }

        /// <summary>Hộp 5 của Hình 8 (cấp 2 — đổi tiền tố id của đợt đang chạy), đang gõ thiếu một ký tự.</summary>
        internal static LiveOpsConfirmRequest ConfirmLevel2SampleRequest()
        {
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle("Đổi tiền tố id của đợt đang chạy")
                .WithBody("weekly-pass-35 → pass-35. Đợt đang chạy tới 14/9 00:00 UTC. Người chơi đã có điểm ở weekly-pass-35 sẽ bắt đầu lại từ 0 " +
                          "ở pass-35. " + LiveOpsHubStrings.KitUnknownPlayerCountSentence + " " + LiveOpsHubStrings.KitNoTestDataSentence)
                .WithTypeToConfirm("weekly-pass-35")
                .WithButtons("Đổi tiền tố", "Giữ tiền tố cũ")
                .Build();
        }

        private static EditorWindow OpenConfirm(LiveOpsConfirmRequest request, string typedText)
        {
            LiveOpsConfirmWindow window = LiveOpsConfirmWindow.OpenForTest(request);
            if (window.Content.TypeField != null) window.Content.TypeField.value = typedText;
            return window;
        }

        private static EditorWindow OpenToastBare()
        {
            // Toast phải là con cuối của cột nội dung — cột đó chỉ có sau CreateGUI, nên gắn toast lúc thân màn giả vào panel.
            FakeHubSection section = new FakeHubSection(ToastSectionId, LiveOpsHubStrings.ShellCalendarTitle, "Hình 7", PipelineStage.Schedule)
            {
                ViewFactory = BuildToastHostView,
                RequiredElementNames = new[] { ToastBodyElementName },
            };
            return LiveOpsHubWindow.OpenForTest(new List<IHubSection> { section }, new ManualLiveOpsHubCompilationState(false), null, section.Id);
        }

        private const string ToastBodyElementName = "capture-toast-body";

        private static VisualElement BuildToastHostView()
        {
            VisualElement body = new VisualElement { name = ToastBodyElementName };
            body.RegisterCallback<AttachToPanelEvent>(AttachToastToContentColumn);
            return body;
        }

        private static void AttachToastToContentColumn(AttachToPanelEvent attachEvent)
        {
            VisualElement body = (VisualElement)attachEvent.target;
            body.UnregisterCallback<AttachToPanelEvent>(AttachToastToContentColumn);
            VisualElement content = FindAncestor(body, LiveOpsHubPaths.ShellElementNames.Content);
            VisualElement root = FindAncestorWithClass(body, LiveOpsHubClassNames.Root);
            if (content == null || root == null) return;
            root.AddToClassList(LiveOpsHubClassNames.NoMotion);
            // Từ W4 cửa sổ tự dựng toast của mình (G-HOSTUI) — để nguyên thì khung có HAI element `hub-toast`, cái đầu đang ẩn,
            // và công cụ đo đọc nhầm cái ẩn (cao 0). Kịch bản này cần đồng hồ đứng yên nên gỡ toast thật rồi gắn toast của mình.
            content.Q<LiveOpsToast>()?.RemoveFromHierarchy();

            // Thao tác thật trên asset bộ nhớ: group mang đúng câu toast, còn trên đỉnh → Hoàn tác bật như sau một lần kéo.
            LiveOpsHubUndoTracker tracker = new LiveOpsHubUndoTracker();
            UndoTestTarget target = new UndoTestTarget();
            int group = tracker.BeginGroup(ToastMessage);
            target.SetKey("hunt_bonus", ToastMessage);

            LiveOpsToast toast = new LiveOpsToast(tracker, () => 0.0);
            content.Add(toast);
            toast.Show(LiveOpsToastModel.ForEdit(ToastMessage, group));
            // Lệnh chụp đóng cửa sổ sau mỗi kịch bản: dọn tracker (field delegate của Undo) và asset bộ nhớ theo.
            toast.RegisterCallback<DetachFromPanelEvent>(detachEvent =>
            {
                tracker.Dispose();
                target.Dispose();
            });
        }

        private static VisualElement FindAncestor(VisualElement element, string elementName)
        {
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                if (current.name == elementName) return current;
            }
            return null;
        }

        private static VisualElement FindAncestorWithClass(VisualElement element, string className)
        {
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                if (current.ClassListContains(className)) return current;
            }
            return null;
        }
    }
}
