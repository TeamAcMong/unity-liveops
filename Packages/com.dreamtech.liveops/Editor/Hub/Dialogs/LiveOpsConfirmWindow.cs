using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Cửa sổ modal của hộp xác nhận phá huỷ (8.6, [FD §3.10]): 400×212 (cấp 1) hoặc 400×290 (cấp 2), tiêu đề "LiveOps Hub", nội dung
    /// <see cref="LiveOpsConfirmContent"/>. Mọi section gọi qua <c>services.Confirmation</c> (<see cref="ModalLiveOpsHubConfirmationPresenter"/>),
    /// không gọi thẳng lớp này — test dùng <c>ScriptedLiveOpsHubConfirmationPresenter</c> vì <c>ShowModalUtility</c> chặn batchmode.
    /// Đóng bằng nút × của hệ điều hành = <see cref="LiveOpsConfirmResult.Safe"/> (kết quả mặc định không bao giờ là phá huỷ).
    /// </summary>
    internal sealed class LiveOpsConfirmWindow : EditorWindow
    {
        internal const float Width = 400f;
        internal const float Level1Height = 212f;
        internal const float TypeToConfirmHeight = 290f;

        /// <summary>
        /// Hộp không bao giờ cao hơn thế. Phần vượt trần do <see cref="LiveOpsConfirmContent"/> nuốt bằng ô cuộn của thân
        /// (<see cref="LiveOpsConfirmContent.ScrollElementName"/>) — không có ô cuộn đó thì câu hậu quả dài đẩy hàng nút
        /// (flex-shrink: 0) xuống dưới mép cửa sổ và người dùng mất cả hai nút lẫn gợi ý phím.
        /// </summary>
        internal const float MaximumHeight = 420f;

        /// <summary>
        /// Sàn chiều cao — chỉ để chặn ca đo hỏng (0 px), KHÔNG phải để đặt dáng hộp: hộp cấp 1 câu ngắn đo được 87 px, nên một
        /// sàn 120 px sẽ tự đẻ lại đúng 33 px trống mà UX-27 đang gỡ. 64 px = tiêu đề + một dòng + hàng nút + padding.
        /// </summary>
        internal const float MinimumHeight = 64f;

        /// <summary>
        /// Cửa sổ chủ của hộp: cửa sổ hub đang mở tự ghi tên mình vào đây (<see cref="RegisterOwnerWindow"/>). Vì sao KHÔNG
        /// đọc <c>EditorWindow.focusedWindow</c> nữa (soát W8-UX R4): focus là thứ Unity và người dùng đổi liên tục (popover
        /// vừa đóng, Console vừa bật, cửa sổ Project vừa click) nên chỗ đặt hộp phụ thuộc một thứ không ai kiểm được — và
        /// không có đường nào chứng minh nó rơi đúng vào hub. Hộp luôn mở TỪ một thao tác trong hub nên chủ PHẢI là hub.
        /// Tĩnh vì lớp này được dựng từ <c>ModalLiveOpsHubConfirmationPresenter</c>, file nằm ngoài quyền ghi của gói nên
        /// không truyền chủ xuống qua constructor được — ghi ở mục 5 báo cáo gói.
        /// </summary>
        private static EditorWindow _ownerWindow;

        private LiveOpsConfirmContent _content;
        private int _heightPasses;

        internal LiveOpsConfirmContent Content => _content;
        internal LiveOpsConfirmResult Result { get; private set; } = LiveOpsConfirmResult.Safe;

        /// <summary>
        /// Rect của cửa sổ chủ đã DÙNG THẬT để đặt hộp (rỗng khi không tìm được chủ). Có để test đọc được: khoá chỗ đặt bằng
        /// mỗi hàm thuần <see cref="PlacementFor"/> thì đường nối "hộp ↔ cửa sổ hub" vẫn có thể trơ hoàn toàn mà cổng xanh.
        /// </summary>
        internal Rect ResolvedOwnerPosition { get; private set; }

        /// <summary>Cửa sổ hub gọi lúc bật để nhận làm chủ của hộp xác nhận — xem <see cref="_ownerWindow"/>.</summary>
        internal static void RegisterOwnerWindow(EditorWindow owner)
        {
            if (owner != null) _ownerWindow = owner;
        }

        /// <summary>Cửa sổ hub gọi lúc tắt. Chỉ xoá khi chính nó đang là chủ: hai hub mở cùng lúc thì cái đóng không cướp chủ của cái còn lại.</summary>
        internal static void UnregisterOwnerWindow(EditorWindow owner)
        {
            if (ReferenceEquals(_ownerWindow, owner)) _ownerWindow = null;
        }

        /// <summary>
        /// Rect cửa sổ chủ: hub đã đăng ký → cửa sổ đang focus (không tính chính hộp) → cửa sổ Editor chính. Hub đã đóng là
        /// null giả của Unity nên <c>== null</c> bắt được và rơi tiếp xuống bậc sau.
        /// </summary>
        internal static Rect ResolveOwnerPosition(EditorWindow exclude)
        {
            EditorWindow owner = _ownerWindow;
            if (owner == null || ReferenceEquals(owner, exclude)) owner = focusedWindow;
            if (owner != null && !ReferenceEquals(owner, exclude)) return owner.position;
            return EditorGUIUtility.GetMainWindowPosition();
        }

        /// <summary>Mở modal và chờ người dùng; trả Safe khi Enter/Esc/nút an toàn/đóng cửa sổ. Không gọi trong test (chặn batchmode).</summary>
        public static LiveOpsConfirmResult Show(LiveOpsConfirmRequest request)
        {
            LiveOpsConfirmWindow window = Create(request, null);
            window.ShowModalUtility();
            // Cửa sổ đã đóng (Unity object == null) nhưng instance C# vẫn giữ Result đã chốt.
            return window.Result;
        }

        /// <summary>
        /// Mở KHÔNG modal (test UI + kịch bản chụp <c>hf-confirm-level*-layout</c>): cùng nội dung, cùng kích thước, cửa sổ thường để
        /// batchmode không bị chặn. Người gọi đóng cửa sổ.
        /// </summary>
        /// <param name="placeOnOwnerWindow">
        /// true = đặt hộp bằng ĐÚNG đường production (<see cref="ResolveOwnerPosition"/> + <see cref="PlacementFor"/>) để test
        /// đo được chỗ đặt thật trên cửa sổ hub thật; false (mặc định, mọi kịch bản chụp) = ghim ở (0,0) cho ảnh không phụ
        /// thuộc chỗ cửa sổ chủ đang nằm.
        /// </param>
        internal static LiveOpsConfirmWindow OpenForTest(LiveOpsConfirmRequest request, ILiveOpsHubLayoutLoader layoutLoader = null,
            bool placeOnOwnerWindow = false)
        {
            LiveOpsConfirmWindow window = Create(request, layoutLoader);
            window.Show();
            // SP-16: kích thước đặt SAU Show mới giữ (đặt trước bị kẹp ≈ 401×202); chiều cao vẫn do ApplyMeasuredHeight chốt
            // lại theo nội dung.
            Vector2 size = SizeFor(request.Level);
            window.position = placeOnOwnerWindow
                ? window.PlaceOnOwnerWindow(size)
                : new Rect(0f, 0f, size.x, size.y);
            window.Focus();
            return window;
        }

        internal static Vector2 SizeFor(LiveOpsConfirmLevel level)
        {
            return new Vector2(Width, level == LiveOpsConfirmLevel.TypeToConfirm ? TypeToConfirmHeight : Level1Height);
        }

        /// <summary>
        /// Chỗ đặt hộp theo CỬA SỔ CHỦ (hub đang focus lúc mở), không theo cửa sổ Editor chính (UX-27 / UJ-12): hub nổi ở
        /// (40,52) làm hộp căn giữa màn hình rơi đúng lên inspector và nửa phải trục — che chính chỗ người dùng đang đọc để
        /// quyết. Hàm thuần (không đụng EditorWindow) để test được chỗ đặt mà không phải mở cửa sổ thật.
        /// <para>
        /// Hai luật: (1) hub đủ rộng (từ hai lần bề rộng hộp) thì hộp lệch sang TRÁI khỏi tâm — cột inspector của màn Lịch
        /// nằm bên phải; (2) hộp luôn nằm gọn trong cửa sổ chủ, chừa lề <see cref="OwnerMargin"/>. Cửa sổ chủ hẹp hơn hộp +
        /// hai lề thì bỏ lề và căn giữa: nằm lệch ra ngoài còn tệ hơn sát mép.
        /// </para>
        /// </summary>
        internal static Rect PlacementFor(Rect owner, Vector2 size)
        {
            if (owner.width <= 0f || owner.height <= 0f) return new Rect(owner.x, owner.y, size.x, size.y);

            float horizontalShift = owner.width >= size.x * 2f ? owner.width * OwnerHorizontalShiftRatio : 0f;
            float left = Clamp(owner.center.x - size.x * 0.5f - horizontalShift, owner.xMin, owner.width, size.x);
            // Cao hơn tâm một chút: đáy màn là chỗ toast và hàng nút của màn, đè lên đó là che luôn câu vừa báo.
            float top = Clamp(owner.yMin + (owner.height - size.y) * OwnerVerticalRatio, owner.yMin, owner.height, size.y);
            return new Rect(left, top, size.x, size.y);
        }

        /// <summary>Lề tối thiểu giữa hộp và mép cửa sổ chủ.</summary>
        private const float OwnerMargin = 12f;

        /// <summary>Phần bề rộng cửa sổ chủ mà hộp lệch sang trái để tránh cột inspector bên phải.</summary>
        private const float OwnerHorizontalShiftRatio = 0.12f;

        /// <summary>Hộp đặt ở 38% chiều cao cửa sổ chủ (trên tâm) — xem <see cref="PlacementFor"/>.</summary>
        private const float OwnerVerticalRatio = 0.38f;

        private static float Clamp(float value, float ownerMinimum, float ownerLength, float boxLength)
        {
            float lower = ownerMinimum + OwnerMargin;
            float upper = ownerMinimum + ownerLength - boxLength - OwnerMargin;
            if (upper < lower) return ownerMinimum + (ownerLength - boxLength) * 0.5f;
            return Mathf.Clamp(value, lower, upper);
        }

        private static LiveOpsConfirmWindow Create(LiveOpsConfirmRequest request, ILiveOpsHubLayoutLoader layoutLoader)
        {
            if (request == null) throw new ArgumentNullException(nameof(request), LiveOpsHubStrings.FeedbackErrorConfirmRequestMissing);
            LiveOpsConfirmWindow window = CreateInstance<LiveOpsConfirmWindow>();
            window.titleContent = new GUIContent(LiveOpsHubStrings.ShellWindowTitle);
            Vector2 size = SizeFor(request.Level);
            // Cho phép co lại theo nội dung đo được (UX-27): khoá cứng min = max ở đây thì ApplyMeasuredHeight không đổi được gì.
            window.minSize = new Vector2(Width, MinimumHeight);
            window.maxSize = new Vector2(Width, MaximumHeight);
            window.PlaceOnOwnerWindow(size);
            window._content = layoutLoader == null ? new LiveOpsConfirmContent(request) : new LiveOpsConfirmContent(request, layoutLoader);
            window._content.Completed += window.OnCompleted;
            window.rootVisualElement.Add(window._content);
            // Focus chỉ giữ khi element đã có panel + layout: chờ GeometryChangedEvent đầu tiên rồi mới focus nút an toàn / ô gõ.
            window._content.RegisterCallback<GeometryChangedEvent>(window.FocusInitialOnce);
            window._content.RegisterCallback<GeometryChangedEvent>(window.ApplyMeasuredHeightOnce);
            return window;
        }

        /// <summary>
        /// Đặt hộp lên cửa sổ chủ (hub đã đăng ký; xem <see cref="ResolveOwnerPosition"/>) và nhớ lại Rect đã dùng. Vì sao
        /// không dùng thẳng cửa sổ chính: hub nổi ở (40,52) làm hộp căn giữa màn hình rơi đúng lên inspector và nửa phải
        /// trục — che chính chỗ người dùng đang đọc để quyết (UX-27 / UJ-12).
        /// <para>
        /// Gọi trong <see cref="Create"/> tức TRƯỚC <c>ShowModalUtility</c> là CỐ Ý: vòng modal chặn luồng tới khi hộp đóng
        /// nên không còn chỗ nào đặt sau Show. Cảnh báo SP-16 (hình học đặt trước Show bị kẹp) không chạm ca này vì
        /// <c>minSize</c>/<c>maxSize</c> đã gán ngay phía trên, nên khung kẹp là 400×[64…420] chứ không phải mặc định.
        /// </para>
        /// </summary>
        private Rect PlaceOnOwnerWindow(Vector2 size)
        {
            Rect ownerPosition = ResolveOwnerPosition(this);
            ResolvedOwnerPosition = ownerPosition;
            if (ownerPosition.width <= 0f || ownerPosition.height <= 0f) return position;
            position = PlacementFor(ownerPosition, size);
            return position;
        }

        private void FocusInitialOnce(GeometryChangedEvent geometryEvent)
        {
            _content.UnregisterCallback<GeometryChangedEvent>(FocusInitialOnce);
            _content.FocusInitial();
        }

        /// <summary>
        /// (UX-27 / UJ-12) Chiều cao theo NỘI DUNG đo được, không theo hằng 212/290: với câu ngắn, hằng để lại một khoảng trống
        /// lớn giữa thân và hàng nút và người đọc hiểu thành "còn thứ gì chưa hiện". Đo sau khi layout đầu tiên xong (lúc đó mọi
        /// Label đã có chiều cao thật, kể cả câu xuống dòng), rồi giữ nguyên: đổi chiều cao làm layout chạy lại và bắn tiếp
        /// GeometryChangedEvent — không có chốt một lần thì thành vòng lặp.
        /// </summary>
        private void ApplyMeasuredHeightOnce(GeometryChangedEvent geometryEvent)
        {
            // Đo LẠI sau mỗi lần layout đổi, tới khi khớp: lần layout đầu tiên các Label (và HelpBox cấp 2) chưa có chiều cao
            // cuối, nên chốt ngay ở lần đó để lại đúng 33 px dư — chính thứ đang sửa. Chặn số lượt để không thành vòng lặp
            // khi một con nào đó không bao giờ đứng yên.
            float measured = _content.MeasureContentHeight();
            if (measured <= 0f) return;
            float height = Mathf.Clamp(measured, MinimumHeight, MaximumHeight);
            Rect current = position;
            if (Mathf.Abs(current.height - height) < 1f || ++_heightPasses > MaximumHeightPasses)
            {
                _content.UnregisterCallback<GeometryChangedEvent>(ApplyMeasuredHeightOnce);
                return;
            }
            minSize = new Vector2(Width, height);
            maxSize = new Vector2(Width, height);
            position = new Rect(current.x, current.y, Width, height);
        }

        /// <summary>Số lượt đo lại tối đa — xem <see cref="ApplyMeasuredHeightOnce"/>.</summary>
        private const int MaximumHeightPasses = 6;

        private void OnCompleted(LiveOpsConfirmResult result)
        {
            Result = result;
            // Đóng ngay (không delayCall): vòng modal của ShowModalUtility không chắc chạy delayCall, và hộp còn mở sau khi đã chọn
            // là người dùng bấm được lần nữa. Nút Close() trong callback click là cách cửa sổ UI Toolkit của Unity vẫn làm.
            Close();
        }

        private void OnDisable()
        {
            if (_content != null) _content.Completed -= OnCompleted;
        }
    }
}
