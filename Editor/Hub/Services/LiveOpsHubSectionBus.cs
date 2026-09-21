using System;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Kênh yêu cầu từ section lên cửa sổ: điều hướng có tham số, toast, outcome, làm mới health, class của cột nội dung.
    /// Section KHÔNG gọi thẳng cửa sổ — nó phát lên bus, cửa sổ lắng nghe. Nhờ vậy màn (W4) và phần UI của khung (G-HOSTUI)
    /// làm song song, và test section chỉ cần assert sự kiện trên bus mà không mở cửa sổ.
    /// </summary>
    internal sealed class LiveOpsHubSectionBus
    {
        public event Action<LiveOpsHubNavigation> NavigationRequested;
        public event Action<LiveOpsToastModel> ToastRequested;
        public event Action<LiveOpsOutcomeRecord> OutcomeRequested;
        public event Action OutcomeCleared;

        /// <summary>Cửa sổ: <c>LiveOpsHealthThrottle.InvalidateAll</c> + làm mới health ngay (không chờ nhịp 3 giây).</summary>
        public event Action HealthInvalidated;

        /// <summary>(tên class, bật/tắt) trên cột nội dung — vd <c>liveops-hub-content--raised-toast</c> của Lịch (PD-21).</summary>
        public event Action<string, bool> ContentClassRequested;

        /// <summary>
        /// (J2-01) Mép TRÊN — toạ độ WORLD — của phần chân màn mà toast KHÔNG được đè lên; <c>float.NaN</c> = màn này không đòi sàn nào.
        /// </summary>
        public event Action<float> ToastFloorRequested;

        public void Navigate(LiveOpsHubNavigation navigation)
        {
            if (navigation == null) throw new ArgumentNullException(nameof(navigation));
            NavigationRequested?.Invoke(navigation);
        }

        public void ShowToast(LiveOpsToastModel toast)
        {
            if (toast == null) throw new ArgumentNullException(nameof(toast));
            ToastRequested?.Invoke(toast);
        }

        public void ShowOutcome(LiveOpsOutcomeRecord outcome)
        {
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            OutcomeRequested?.Invoke(outcome);
        }

        public void ClearOutcome()
        {
            OutcomeCleared?.Invoke();
        }

        public void InvalidateHealth()
        {
            HealthInvalidated?.Invoke();
        }

        public void SetContentClass(string className, bool enabled)
        {
            if (string.IsNullOrEmpty(className)) throw new ArgumentNullException(nameof(className));
            ContentClassRequested?.Invoke(className, enabled);
        }

        /// <summary>
        /// (J2-01) Màn khai mép trên của chân màn (world y) để cửa sổ đậu toast trên đó. Màn ĐO chân của mình, cửa sổ sỞ HỮU toast —
        /// nên phép đổi sang <c>bottom</c> nằm ở cửa sổ, chỗ duy nhất biết hộp nào đang làm gốc toạ độ cho toast.
        /// <para>
        /// Vì sao là một con số ĐO ĐƯỢC chứ không phải một BẬC class: chân màn Lịch cao bao nhiêu là do dải chú giải gập mấy hàng,
        /// mà số hàng ấy đổi theo bề ngang CÒN LẠI sau khi ngăn kéo inspector mở — không có một tập bậc hữu hạn nào phủ hết (phiếu J2-01:
        /// ở 820 ngăn kéo mở, chú giải cao 68px và hai bậc 64/96 đều trượt, toast đè 452×24 lên hàng đầu chú giải).
        /// </para>
        /// </summary>
        public void SetToastFloor(float worldTopY)
        {
            ToastFloorRequested?.Invoke(worldTopY);
        }
    }
}
