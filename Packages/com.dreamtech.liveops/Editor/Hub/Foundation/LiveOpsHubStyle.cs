using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Gắn class trạng thái lên element — nơi DUY NHẤT view đổi màu theo dữ liệu ([FD §2.14]: màu/trạng thái luôn bằng
    /// class, không style inline). Mỗi hàm gỡ cả họ class trước khi gắn một class, vì element tái dùng (hàng ListView,
    /// pool thanh đợt) mà sót class cũ thì một dấu mang hai màu cùng lúc và thứ tự rule USS quyết định màu thắng — lỗi
    /// không log, chỉ thấy trên ảnh.
    /// </summary>
    internal static class LiveOpsHubStyle
    {
        private static readonly string[] FillClasses =
        {
            LiveOpsHubClassNames.FillOk, LiveOpsHubClassNames.FillWarning, LiveOpsHubClassNames.FillBlocked, LiveOpsHubClassNames.FillNotMeasured,
        };

        private static readonly string[] TextClasses =
        {
            LiveOpsHubClassNames.TextOk, LiveOpsHubClassNames.TextWarning, LiveOpsHubClassNames.TextBlocked, LiveOpsHubClassNames.TextQuiet,
        };

        private static readonly string[] PhaseClasses =
        {
            LiveOpsHubClassNames.PhaseRunning, LiveOpsHubClassNames.PhaseUpcoming, LiveOpsHubClassNames.PhasePending, LiveOpsHubClassNames.PhaseEnded,
        };

        private static readonly string[] StripeClasses =
        {
            LiveOpsHubClassNames.FindingStripeBlocked, LiveOpsHubClassNames.FindingStripeWarning, LiveOpsHubClassNames.FindingStripeNotMeasured,
        };

        private static readonly string[] EventColorClasses =
        {
            LiveOpsHubClassNames.EventColor0, LiveOpsHubClassNames.EventColor1, LiveOpsHubClassNames.EventColor2, LiveOpsHubClassNames.EventColor3,
            LiveOpsHubClassNames.EventColor4, LiveOpsHubClassNames.EventColor5, LiveOpsHubClassNames.EventColor6, LiveOpsHubClassNames.EventColor7,
        };

        /// <summary>Họ fill (chấm, thanh, swatch). Gỡ cả họ text: một element chỉ mang một họ ([FD §2.4]).</summary>
        public static void SetState(VisualElement element, HealthState state)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            RemoveAll(element, TextClasses);
            RemoveAll(element, FillClasses);
            element.AddToClassList(FillClassOf(state));
        }

        /// <summary>Họ text (chỉ Label). NotMeasured dùng màu quiet — không có màu riêng để không trông như một mức lỗi.</summary>
        public static void SetStateText(Label label, HealthState state)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            RemoveAll(label, FillClasses);
            RemoveAll(label, TextClasses);
            label.AddToClassList(TextClassOf(state));
        }

        /// <summary>
        /// Họ giai đoạn đợt (vuông/gạch, vẽ bằng màu chữ). Đợt đã khép mà game chưa hiện kết quả là "Chờ hiện kết quả",
        /// không phải "Đã khép"; <see cref="LiveEventPhase.None"/> không gắn class nào.
        /// </summary>
        public static void SetPhase(VisualElement element, LiveEventPhase phase, bool isPendingResult)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            RemoveAll(element, PhaseClasses);
            string phaseClass = PhaseClassOf(phase, isPendingResult);
            if (phaseClass != null) element.AddToClassList(phaseClass);
        }

        /// <summary>Sọc 3px bên trái hàng phát hiện; <c>null</c> hoặc Ok = không sọc.</summary>
        public static void SetSeverityStripe(VisualElement element, HealthState? severity)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            RemoveAll(element, StripeClasses);
            if (!severity.HasValue) return;
            switch (severity.Value)
            {
                case HealthState.Blocked: element.AddToClassList(LiveOpsHubClassNames.FindingStripeBlocked); break;
                case HealthState.Warning: element.AddToClassList(LiveOpsHubClassNames.FindingStripeWarning); break;
                case HealthState.NotMeasured: element.AddToClassList(LiveOpsHubClassNames.FindingStripeNotMeasured); break;
            }
        }

        /// <summary>
        /// Màu loại event từ field <c>colorSlot</c> (0–7). Ô ngoài khoảng (asset sửa tay) quay vòng về 0–7 thay vì ném:
        /// view không được sập vì dữ liệu hỏng — ô sai do màn Loại event báo, không phải do swatch.
        /// </summary>
        public static void SetEventColor(VisualElement swatch, int colorSlot)
        {
            if (swatch == null) throw new ArgumentNullException(nameof(swatch));
            RemoveAll(swatch, EventColorClasses);
            int wrapped = ((colorSlot % EventColorClasses.Length) + EventColorClasses.Length) % EventColorClasses.Length;
            swatch.AddToClassList(EventColorClasses[wrapped]);
        }

        /// <summary>Khoá/mở nút kèm lý do — lý do bắt buộc khi khoá (<see cref="LiveOpsButtonSlot.SetEnabledWithReason"/>).</summary>
        public static void SetEnabledWithReason(LiveOpsButtonSlot slot, bool enabled, string reason)
        {
            if (slot == null) throw new ArgumentNullException(nameof(slot));
            slot.SetEnabledWithReason(enabled, reason);
        }

        internal static string FillClassOf(HealthState state)
        {
            switch (state)
            {
                case HealthState.Warning: return LiveOpsHubClassNames.FillWarning;
                case HealthState.Blocked: return LiveOpsHubClassNames.FillBlocked;
                case HealthState.NotMeasured: return LiveOpsHubClassNames.FillNotMeasured;
                default: return LiveOpsHubClassNames.FillOk;
            }
        }

        internal static string TextClassOf(HealthState state)
        {
            switch (state)
            {
                case HealthState.Warning: return LiveOpsHubClassNames.TextWarning;
                case HealthState.Blocked: return LiveOpsHubClassNames.TextBlocked;
                case HealthState.NotMeasured: return LiveOpsHubClassNames.TextQuiet;
                default: return LiveOpsHubClassNames.TextOk;
            }
        }

        internal static string PhaseClassOf(LiveEventPhase phase, bool isPendingResult)
        {
            switch (phase)
            {
                case LiveEventPhase.Upcoming: return LiveOpsHubClassNames.PhaseUpcoming;
                case LiveEventPhase.Active: return LiveOpsHubClassNames.PhaseRunning;
                case LiveEventPhase.Ended: return isPendingResult ? LiveOpsHubClassNames.PhasePending : LiveOpsHubClassNames.PhaseEnded;
                default: return null;
            }
        }

        private static void RemoveAll(VisualElement element, string[] classNames)
        {
            foreach (string className in classNames)
            {
                element.RemoveFromClassList(className);
            }
        }
    }
}
