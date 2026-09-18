using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Tự kiểm CỔNG (không kiểm sản phẩm): mọi đường reflection của cổng W8-UX phải tra được trên bản Unity đang chạy.
    /// <para>
    /// Vì sao cần một fixture riêng cho việc này: ba đường reflection của cổng đều "hỏng thì im lặng" —
    /// <c>MeasureTextSize</c> không tra được thì mọi phép đo chữ trả 0 và loại <c>textCut</c> biến mất;
    /// <c>VisualElement.ShouldClip</c> không hỏi được thì "bị cha cắt" luôn false; <c>Internal_CallDelayFunctions</c> không tra
    /// được thì nhịp <c>delayCall</c> không bao giờ chạy trong một cử chỉ. Cả ba đều biến "cổng hỏng" thành "cổng XANH", đúng cái
    /// hạng lỗi mà đợt W8 mở ra để diệt (R-07).
    /// </para>
    /// <para>
    /// Im lặng trả 0 vẫn là hành vi đúng của <see cref="UxLayoutAuditor"/> khi đang chạy (làm đỏ giữa một lượt kiểm thì mất cả
    /// lượt); chỗ để làm ĐỎ là ở đây, một lần cho mỗi lượt cổng.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxGateSelfCheckTests
    {
        /// <summary>Chuỗi đo thử — dài hơn một ký tự để bề rộng đo được phải lớn hơn 0 trên mọi font.</summary>
        private const string ProbeText = "LiveOps Hub — đo chữ";

        private UxHubWindowFixture _fixture;

        [TearDown]
        public void TearDown()
        {
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
            UxHubWindowFixture.CloseStrayWindows();
        }

        [UnityTest]
        public IEnumerator Reflection_EveryProbe_ResolvesOnThisUnityVersion()
        {
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, UxHubWindowFixture.AllSizes[4],
                LiveOpsHubLanguageId.Vietnamese);
            yield return _fixture.WaitForLayout();

            Assert.IsTrue(UxLayoutAuditor.CanMeasureText(),
                "không tra được TextElement.MeasureTextSize — mọi phép đo chữ trả 0 và cổng MẤT loại phát hiện textCut");
            Assert.IsTrue(UxLayoutAuditor.CanReadIsElided(),
                "không đọc được TextElement.isElided — chữ rút gọn bằng '…' không còn bị phát hiện");
            Assert.IsTrue(UxLayoutAuditor.CanDetectClipping(),
                "không hỏi được VisualElement.ShouldClip — 'bị cha cắt' luôn false và IsClippedByAncestor thành code chết");
            Assert.IsTrue(UxEventSender.CanPumpDelayCalls(),
                "không tra được EditorApplication.Internal_CallDelayFunctions — nhịp delayCall không chạy trong một cử chỉ (UJ-11)");

            Label probe = new Label(ProbeText);
            _fixture.Root.Add(probe);
            yield return UxEventSender.Settle(UxEventSender.SettleFrames, UxEventSender.SettleMilliseconds);
            float measured = UxLayoutAuditor.MeasuredWidthOf(probe, ProbeText);
            probe.RemoveFromHierarchy();

            Assert.Greater(measured, 0f,
                "MeasureTextSize tra được nhưng đo ra 0 — phép đo chữ của cổng không nói gì về bề rộng thật");
        }
    }
}
