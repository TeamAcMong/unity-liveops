using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chú giải dưới minimap [SD1 §3.6]: 8 mục giải mã mọi ký hiệu đang có trên trục (thanh nổi, thanh phẳng, nhạt, gạch chéo đỏ, vuông
    /// góc phải, gạch ngang + tag, dấu ở mép, "Không đặt được"). Mẫu 18×10 dựng bằng class của chính thanh/vùng chồng để chú giải đổi
    /// màu cùng skin với thứ nó giải thích; flex-wrap xuống hai dòng khi thiếu chỗ. Cửa sổ hẹp ẩn dải này (chú giải vào menu ⋮, W5).
    /// </summary>
    internal sealed class LiveOpsTimelineLegend : VisualElement
    {
        internal const int ItemCount = 8;
        private const string LoopIconName = "preAudioLoopOff";
        private const int LoopIconSize = 10;

        /// <summary>Màu loại của dải mẫu — khe 0, đúng màu làn đầu tiên trên trục nên mắt nối được mẫu với thanh thật.</summary>
        private const int SampleColorSlot = 0;

        public LiveOpsTimelineLegend()
        {
            AddToClassList(LiveOpsHubClassNames.TimelineLegend);
            pickingMode = PickingMode.Ignore;

            AddItem(Sample(LiveOpsHubClassNames.TimelineLegendSampleFixed), LiveOpsHubStrings.TimelineLegendFixed);

            VisualElement recurring = Sample(LiveOpsHubClassNames.TimelineLegendSampleRecurring);
            Image loopIcon = LiveOpsHubIcons.CreateImage(LoopIconName, LoopIconSize);
            AddItem(recurring, LiveOpsHubStrings.TimelineLegendRecurring, loopIcon);

            AddItem(Sample(LiveOpsHubClassNames.TimelineLegendSampleEnded), LiveOpsHubStrings.TimelineLegendEnded);
            // (R-10) Mẫu "vùng chồng giờ" giải thích MẢNG NỀN gạch chéo trên trục, không phải một thanh. Gắn dải màu loại cho nó
            // là mẫu nói sai thứ nó giải thích — người đọc tưởng vùng chồng cũng là một đợt. Nó đã có viền + nền blocked riêng.
            AddItem(Sample(LiveOpsHubClassNames.TimelineLegendSampleOverlap, false), LiveOpsHubStrings.TimelineLegendOverlap);

            VisualElement changed = Sample(LiveOpsHubClassNames.TimelineLegendSampleFixed);
            changed.Add(SamplePart(LiveOpsHubClassNames.TimelineBarChangedSquare));
            AddItem(changed, LiveOpsHubStrings.TimelineLegendChanged);

            VisualElement dropped = Sample(LiveOpsHubClassNames.TimelineLegendSampleFixed);
            dropped.Add(SamplePart(LiveOpsHubClassNames.TimelineBarStrike));
            AddItem(dropped, LiveOpsHubStrings.TimelineLegendDropped);

            VisualElement clipped = Sample(LiveOpsHubClassNames.TimelineLegendSampleFixed);
            VisualElement clipMarker = SamplePart(LiveOpsHubClassNames.TimelineBarClipStart);
            clipMarker.Add(SamplePart(LiveOpsHubClassNames.TimelineBarClipShape));
            clipped.Add(clipMarker);
            AddItem(clipped, LiveOpsHubStrings.TimelineLegendClipped);

            LiveOpsStateMark mark = new LiveOpsStateMark { Kind = LiveOpsStateMark.MarkKind.Health, Size = LiveOpsStateMark.MarkSize.Small };
            mark.SetHealth(HealthState.Blocked);
            mark.AddToClassList(LiveOpsHubClassNames.TimelineLegendSampleMark);
            AddItem(mark, LiveOpsHubStrings.TimelineLegendUnplaceable);
        }

        private void AddItem(VisualElement sample, string text, VisualElement icon = null)
        {
            VisualElement item = new VisualElement { pickingMode = PickingMode.Ignore };
            item.AddToClassList(LiveOpsHubClassNames.TimelineLegendItem);
            item.Add(sample);
            if (icon != null) item.Add(icon);
            Label label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(LiveOpsHubClassNames.TimelineLegendText);
            item.Add(label);
            Add(item);
        }

        /// <summary>
        /// (UX-19, V13) Mẫu mang CẢ dải màu đáy 3px của thanh thật, không chỉ nền + viền: ở skin sáng nền nút và nền cửa sổ đo
        /// ra 1,00:1, nên mẫu "nhạt = đã khép" và "thanh phẳng" biến mất hẳn và chú giải chỉ còn chữ. Dải lấy màu loại 0 —
        /// cùng token với thanh trên trục, nên mẫu đổi màu theo skin cùng nhịp thứ nó giải thích.
        /// </summary>
        /// <param name="withStripe">Chỉ mẫu hình THANH mới mang dải màu loại; mẫu vùng chồng giờ là mảng nền nên không (R-10).</param>
        private static VisualElement Sample(string variantClassName, bool withStripe = true)
        {
            VisualElement sample = new VisualElement { pickingMode = PickingMode.Ignore };
            sample.AddToClassList(LiveOpsHubClassNames.TimelineLegendSample);
            sample.AddToClassList(variantClassName);
            if (!withStripe) return sample;
            VisualElement stripe = SamplePart(LiveOpsHubClassNames.TimelineBarStripe);
            LiveOpsHubStyle.SetEventColor(stripe, SampleColorSlot);
            sample.Add(stripe);
            return sample;
        }

        private static VisualElement SamplePart(string className)
        {
            VisualElement part = new VisualElement { pickingMode = PickingMode.Ignore };
            part.AddToClassList(className);
            return part;
        }
    }
}
