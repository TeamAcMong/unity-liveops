using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Thanh chu kỳ của luật lặp ([SD1 §4.1]): cao 14, viền, bo 2; ba con chia bề ngang bằng <c>flex-grow</c> = số giờ — con 1 phần
    /// chạy (màu loại), con 2 phần nghỉ, con 3 phần tràn khi chạy lâu hơn chu kỳ (blocked-fill: đợt sau mở trước khi đợt trước
    /// khép). Chỉ flex-grow, không tính pixel: thanh co giãn theo cột form mà không cần nghe GeometryChangedEvent.
    ///
    /// Ba con luôn tồn tại (ẩn bằng class) để test và view tái dùng không thêm/bớt con. Khi tràn, thanh vẽ thêm vạch 1px ở mốc hết
    /// chu kỳ bằng Painter2D — màu đọc từ token khai lại trên chính <c>.liveops-hub-cycle-bar</c> (custom property không kế thừa
    /// từ root [API §12.2]), để vạch đổi màu theo skin cùng nhịp với phần còn lại.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    public sealed partial class LiveOpsRuleCycleBar : VisualElement
    {
        internal static readonly CustomStyleProperty<Color> PeriodEndColorProperty = new CustomStyleProperty<Color>("--liveops-hub-cycle-bar-period-end");

        private const float PeriodEndLineWidth = 1f;

        private int _periodHours;
        private int _activeHours;
        private int _colorSlot;
        private Color _periodEndColor = Color.clear;

        public LiveOpsRuleCycleBar()
        {
            AddToClassList(LiveOpsHubClassNames.CycleBar);
            Run = CreatePart(LiveOpsHubClassNames.CycleBarRun);
            Rest = CreatePart(LiveOpsHubClassNames.CycleBarRest);
            Overflow = CreatePart(LiveOpsHubClassNames.CycleBarOverflow);
            Overflow.AddToClassList(LiveOpsHubClassNames.FillBlocked);
            LiveOpsHubStyle.SetEventColor(Run, _colorSlot);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            generateVisualContent += DrawPeriodEnd;
            ApplyParts();
        }

        /// <summary>Thuộc tính UXML <c>period-hours</c>: chu kỳ (giờ). ≤ 0 = luật chưa hợp lệ → thanh rỗng (không chia cho 0).</summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public int PeriodHours
        {
            get => _periodHours;
            set
            {
                _periodHours = value;
                ApplyParts();
            }
        }

        /// <summary>Thuộc tính UXML <c>active-hours</c>: số giờ chạy mỗi đợt. Lớn hơn chu kỳ → con thứ 3 blocked-fill.</summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public int ActiveHours
        {
            get => _activeHours;
            set
            {
                _activeHours = value;
                ApplyParts();
            }
        }

        /// <summary>Ô màu loại event (0–7) của phần chạy; ngoài khoảng quay vòng như swatch.</summary>
        public void SetColorSlot(int colorSlot)
        {
            _colorSlot = colorSlot;
            LiveOpsHubStyle.SetEventColor(Run, colorSlot);
        }

        internal VisualElement Run { get; }
        internal VisualElement Rest { get; }
        internal VisualElement Overflow { get; }

        /// <summary>Chạy lâu hơn chu kỳ (có phần tràn).</summary>
        public bool IsOverflowing => _periodHours > 0 && _activeHours > _periodHours;

        /// <summary>Màu vạch mốc hết chu kỳ đã đọc từ token; <see cref="Color.clear"/> khi stylesheet controls chưa nạp.</summary>
        internal Color PeriodEndColor => _periodEndColor;

        private VisualElement CreatePart(string className)
        {
            VisualElement part = new VisualElement { pickingMode = PickingMode.Ignore };
            part.AddToClassList(className);
            Add(part);
            return part;
        }

        private void ApplyParts()
        {
            int period = Math.Max(0, _periodHours);
            int active = Math.Max(0, _activeHours);
            int runHours = Math.Min(active, period);
            int restHours = Math.Max(0, period - active);
            int overflowHours = period > 0 ? Math.Max(0, active - period) : 0;

            SetPart(Run, runHours);
            SetPart(Rest, restHours);
            SetPart(Overflow, overflowHours);
            MarkDirtyRepaint();
        }

        private static void SetPart(VisualElement part, int hours)
        {
            part.style.flexGrow = hours; // style-inline-allowed: 5
            part.EnableInClassList(LiveOpsHubClassNames.CycleBarPartHidden, hours <= 0);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent resolvedEvent)
        {
            if (customStyle.TryGetValue(PeriodEndColorProperty, out Color color)) _periodEndColor = color;
            MarkDirtyRepaint();
        }

        private void DrawPeriodEnd(MeshGenerationContext context)
        {
            if (!IsOverflowing || _periodEndColor.a <= 0f) return;
            Rect content = contentRect;
            if (float.IsNaN(content.width) || content.width <= 0f) return;
            float periodEndX = content.xMin + content.width * _periodHours / _activeHours;
            Painter2D painter = context.painter2D;
            painter.strokeColor = _periodEndColor;
            painter.lineWidth = PeriodEndLineWidth;
            painter.BeginPath();
            painter.MoveTo(new Vector2(periodEndX, content.yMin));
            painter.LineTo(new Vector2(periodEndX, content.yMax));
            painter.Stroke();
        }

#if !UNITY_2023_2_OR_NEWER
        public new sealed class UxmlFactory : UxmlFactory<LiveOpsRuleCycleBar, UxmlTraits>
        {
        }

        public new sealed class UxmlTraits : VisualElement.UxmlTraits
        {
            // Tên thuộc tính trùng tên kebab-case mà [UxmlAttribute] của Unity 6 sinh từ property (PeriodHours → "period-hours").
            private readonly UxmlIntAttributeDescription _periodHours = new UxmlIntAttributeDescription { name = "period-hours", defaultValue = 0 };
            private readonly UxmlIntAttributeDescription _activeHours = new UxmlIntAttributeDescription { name = "active-hours", defaultValue = 0 };

            public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext context)
            {
                base.Init(visualElement, bag, context);
                LiveOpsRuleCycleBar bar = (LiveOpsRuleCycleBar)visualElement;
                bar.PeriodHours = _periodHours.GetValueFromBag(bag, context);
                bar.ActiveHours = _activeHours.GetValueFromBag(bag, context);
            }
        }
#endif
    }
}
