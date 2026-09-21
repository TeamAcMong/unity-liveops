using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Dấu trạng thái/giai đoạn dùng ở mọi nơi ([FD §2.4]). Hình dạng trước, màu sau: sức khoẻ là họ tròn (Blocked tròn
    /// đặc + vạch cắt, Warning thoi, NotMeasured vòng rỗng viền 2px, Ok chấm nhỏ), giai đoạn là họ vuông/gạch — in đen
    /// trắng vẫn phân biệt được. Hình vẽ hoàn toàn bằng class USS; C# chỉ đổi class và giữ con vạch cắt
    /// <c>liveops-hub-state-mark__bar</c> luôn tồn tại (ẩn bằng USS) để test và view tái dùng không phải thêm/bớt con.
    ///
    /// Dual-path ([FD §2.15], SP-1 đã kiểm chứng hai bản): Unity 6 đọc thuộc tính UXML bằng <c>[UxmlAttribute]</c>
    /// (tên kebab-case sinh từ property), 2022.3 bằng <c>UxmlFactory/UxmlTraits</c> với cùng tên — một file UXML chạy hai bản.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    public sealed partial class LiveOpsStateMark : VisualElement
    {
        public enum MarkKind
        {
            Health = 0,
            Phase = 1,
        }

        /// <summary>Số giá trị = bề ngang danh nghĩa (px). Blocked luôn ≥ 8 để vạch cắt còn đọc được (USS giữ cỡ).</summary>
        public enum MarkSize
        {
            Small = 7,
            Regular = 8,
            Large = 12,
        }

        internal const string HealthOk = "ok";
        internal const string HealthWarning = "warning";
        internal const string HealthBlocked = "blocked";
        internal const string HealthNotMeasured = "not-measured";

        private static readonly string[] HealthClasses =
        {
            LiveOpsHubClassNames.StateMarkOk, LiveOpsHubClassNames.StateMarkWarning, LiveOpsHubClassNames.StateMarkBlocked, LiveOpsHubClassNames.StateMarkNotMeasured,
        };

        private static readonly string[] PhaseClasses =
        {
            LiveOpsHubClassNames.PhaseRunning, LiveOpsHubClassNames.PhaseUpcoming, LiveOpsHubClassNames.PhasePending, LiveOpsHubClassNames.PhaseEnded,
        };

        private MarkKind _kind;
        private MarkSize _size;
        // Mặc định NotMeasured: dấu chưa được đặt trạng thái không bao giờ được trông như Ok.
        private HealthState _health = HealthState.NotMeasured;
        private LiveEventPhase _phase = LiveEventPhase.None;
        private bool _isPendingResult;

        public LiveOpsStateMark()
        {
            AddToClassList(LiveOpsHubClassNames.StateMark);
            Bar = new VisualElement { pickingMode = PickingMode.Ignore };
            Bar.AddToClassList(LiveOpsHubClassNames.StateMarkBar);
            Add(Bar);
            _kind = MarkKind.Health;
            _size = MarkSize.Regular;
            ApplyClasses();
        }

        /// <summary>Con vạch cắt (Blocked) / lõi 2px (Chờ hiện kết quả) — luôn có, hiện hay ẩn do USS.</summary>
        internal VisualElement Bar { get; }

        /// <summary>Thuộc tính UXML <c>kind</c> ("health" | "phase"). Chỉ chọn họ class; không đổi trạng thái đã lưu.</summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public MarkKind Kind
        {
            get => _kind;
            set
            {
                _kind = value;
                ApplyClasses();
            }
        }

        /// <summary>Thuộc tính UXML <c>size</c> ("small" | "regular" | "large").</summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public MarkSize Size
        {
            get => _size;
            set
            {
                _size = value;
                ApplyClasses();
            }
        }

        /// <summary>
        /// Thuộc tính UXML <c>health</c> ("ok" | "warning" | "blocked" | "not-measured") cho dấu tĩnh trong UXML — chuỗi vì
        /// <see cref="HealthState"/> là internal, không được lộ trên chữ ký public (CS0053). Setter chỉ lưu trạng thái, không
        /// tự chuyển <see cref="Kind"/>: Unity 6 gán mọi thuộc tính theo thứ tự khai báo nên đổi Kind ở đây sẽ đè
        /// <c>kind="phase"</c> đã đọc. Chuỗi lạ bị bỏ qua (giữ trạng thái cũ).
        /// </summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public string Health
        {
            get => HealthNameOf(_health);
            set
            {
                if (!TryParseHealth(value, out HealthState parsed)) return;
                _health = parsed;
                ApplyClasses();
            }
        }

        internal HealthState HealthValue => _health;

        internal void SetHealth(HealthState state)
        {
            _health = state;
            _kind = MarkKind.Health;
            ApplyClasses();
        }

        public void SetPhase(LiveEventPhase phase, bool isPendingResult)
        {
            _phase = phase;
            _isPendingResult = isPendingResult;
            _kind = MarkKind.Phase;
            ApplyClasses();
        }

        internal static bool TryParseHealth(string text, out HealthState state)
        {
            state = HealthState.NotMeasured;
            if (string.IsNullOrEmpty(text)) return false;
            string trimmed = text.Trim();
            if (string.Equals(trimmed, HealthOk, StringComparison.OrdinalIgnoreCase)) { state = HealthState.Ok; return true; }
            if (string.Equals(trimmed, HealthWarning, StringComparison.OrdinalIgnoreCase)) { state = HealthState.Warning; return true; }
            if (string.Equals(trimmed, HealthBlocked, StringComparison.OrdinalIgnoreCase)) { state = HealthState.Blocked; return true; }
            if (string.Equals(trimmed, HealthNotMeasured, StringComparison.OrdinalIgnoreCase)) { state = HealthState.NotMeasured; return true; }
            return false;
        }

        internal static string HealthNameOf(HealthState state)
        {
            switch (state)
            {
                case HealthState.Ok: return HealthOk;
                case HealthState.Warning: return HealthWarning;
                case HealthState.Blocked: return HealthBlocked;
                default: return HealthNotMeasured;
            }
        }

        private void ApplyClasses()
        {
            foreach (string className in HealthClasses) RemoveFromClassList(className);
            foreach (string className in PhaseClasses) RemoveFromClassList(className);

            EnableInClassList(LiveOpsHubClassNames.StateMarkSmall, _size == MarkSize.Small);
            EnableInClassList(LiveOpsHubClassNames.StateMarkLarge, _size == MarkSize.Large);

            if (_kind == MarkKind.Health)
            {
                AddToClassList(StateClassOf(_health));
                return;
            }
            string phaseClass = LiveOpsHubStyle.PhaseClassOf(_phase, _isPendingResult);
            if (phaseClass != null) AddToClassList(phaseClass);
        }

        private static string StateClassOf(HealthState state)
        {
            switch (state)
            {
                case HealthState.Ok: return LiveOpsHubClassNames.StateMarkOk;
                case HealthState.Warning: return LiveOpsHubClassNames.StateMarkWarning;
                case HealthState.Blocked: return LiveOpsHubClassNames.StateMarkBlocked;
                default: return LiveOpsHubClassNames.StateMarkNotMeasured;
            }
        }

#if !UNITY_2023_2_OR_NEWER
        public new sealed class UxmlFactory : UxmlFactory<LiveOpsStateMark, UxmlTraits>
        {
        }

        public new sealed class UxmlTraits : VisualElement.UxmlTraits
        {
            // Tên thuộc tính phải trùng tên kebab-case mà [UxmlAttribute] của Unity 6 sinh từ property (Kind → "kind").
            private readonly UxmlEnumAttributeDescription<MarkKind> _kind =
                new UxmlEnumAttributeDescription<MarkKind> { name = "kind", defaultValue = MarkKind.Health };

            // Mặc định enum của mô tả là default(T) = 0 — không phải giá trị hợp lệ của MarkSize, nên ghi rõ Regular.
            private readonly UxmlEnumAttributeDescription<MarkSize> _size =
                new UxmlEnumAttributeDescription<MarkSize> { name = "size", defaultValue = MarkSize.Regular };

            private readonly UxmlStringAttributeDescription _health =
                new UxmlStringAttributeDescription { name = "health", defaultValue = HealthNotMeasured };

            public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext context)
            {
                base.Init(visualElement, bag, context);
                LiveOpsStateMark mark = (LiveOpsStateMark)visualElement;
                mark.Kind = _kind.GetValueFromBag(bag, context);
                mark.Size = _size.GetValueFromBag(bag, context);
                mark.Health = _health.GetValueFromBag(bag, context);
            }
        }
#endif
    }
}
