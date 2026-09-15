using System;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Adapter thật của port <see cref="ILiveOpsHubConfirmationPresenter"/>: mở <see cref="LiveOpsConfirmWindow"/> modal. Services mặc định
    /// dùng lớp này; test và kịch bản chụp dùng <see cref="ScriptedLiveOpsHubConfirmationPresenter"/> (modal chặn batchmode).
    /// <para>
    /// Mức hỏi KHÔNG do presenter hay view tự suy (PD-24, bảng 7.0): section gọi <see cref="LiveOpsConfirmationPolicy.Decide"/> rồi
    /// <see cref="ConfirmAsPolicyDecides"/>. Hàm đó là điểm duy nhất nối quyết định với hộp, và từ chối request nhẹ hơn chính sách —
    /// vd policy đòi gõ id đang chạy (cấp 2) mà section dựng hộp cấp 1, hoặc chữ phải gõ khác id người chơi đang giữ. Với đổi tiền
    /// tố/neo/chu kỳ và xoá luật lặp, chính sách lấy lần lặp đang chạy từ BẢN SO khi bản so có luật đó (V-20 C-6, 7.4 thắng) — nên chữ
    /// phải gõ là id của bản đã đăng (<c>weekly-pass-35</c>), không phải id nháp; kiểm khớp <see cref="LiveOpsConfirmDecision.TypeToConfirmText"/>
    /// giữ đúng điều đó ở mọi màn.
    /// </para>
    /// </summary>
    internal sealed class ModalLiveOpsHubConfirmationPresenter : ILiveOpsHubConfirmationPresenter
    {
        private readonly Func<LiveOpsConfirmRequest, LiveOpsConfirmResult> _showWindow;

        public ModalLiveOpsHubConfirmationPresenter() : this(LiveOpsConfirmWindow.Show)
        {
        }

        /// <param name="showWindow">Test thay đường mở modal để kiểm adapter chuyển đúng request và kết quả.</param>
        internal ModalLiveOpsHubConfirmationPresenter(Func<LiveOpsConfirmRequest, LiveOpsConfirmResult> showWindow)
        {
            _showWindow = showWindow ?? throw new ArgumentNullException(nameof(showWindow));
        }

        public LiveOpsConfirmResult Confirm(LiveOpsConfirmRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request), LiveOpsHubStrings.FeedbackErrorConfirmRequestMissing);
            return _showWindow(request);
        }

        /// <summary>
        /// Nối quyết định của <see cref="LiveOpsConfirmationPolicy"/> với hộp (bảng 7.0):
        /// <list type="bullet">
        /// <item><see cref="LiveOpsConfirmRequirement.None"/> → <see cref="LiveOpsConfirmResult.Destructive"/> ngay, không mở hộp (thao tác chạy,
        /// toast Hoàn tác lo đường lui) — <paramref name="buildRequest"/> không được gọi.</item>
        /// <item><see cref="LiveOpsConfirmRequirement.Level1"/> → request phải là cấp 1.</item>
        /// <item><see cref="LiveOpsConfirmRequirement.TypeToConfirm"/> → request phải là cấp 2 và chữ phải gõ bằng đúng id đang chạy của quyết định (Ordinal).</item>
        /// <item><see cref="LiveOpsConfirmRequirement.DedicatedDialog"/> / <see cref="LiveOpsConfirmRequirement.NotAllowed"/> → ném: nút phải
        /// khoá kèm lý do hoặc mở hộp riêng, tới đây là lỗi lập trình (không được lặng lẽ cho chạy).</item>
        /// </list>
        /// </summary>
        internal static LiveOpsConfirmResult ConfirmAsPolicyDecides(ILiveOpsHubConfirmationPresenter presenter, LiveOpsConfirmDecision decision,
            Func<LiveOpsConfirmRequest> buildRequest)
        {
            if (presenter == null) throw new ArgumentNullException(nameof(presenter));
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            if (buildRequest == null) throw new ArgumentNullException(nameof(buildRequest));
            switch (decision.Requirement)
            {
                case LiveOpsConfirmRequirement.None:
                    return LiveOpsConfirmResult.Destructive;
                case LiveOpsConfirmRequirement.NotAllowed:
                    throw new InvalidOperationException(LiveOpsHubStrings.FeedbackErrorPolicyNotAllowed);
                case LiveOpsConfirmRequirement.DedicatedDialog:
                    throw new InvalidOperationException(LiveOpsHubStrings.FeedbackErrorPolicyDedicatedDialog);
            }

            LiveOpsConfirmRequest request = buildRequest();
            if (request == null) throw new InvalidOperationException(LiveOpsHubStrings.FeedbackErrorConfirmRequestMissing);
            LiveOpsConfirmLevel expectedLevel = decision.Requirement == LiveOpsConfirmRequirement.TypeToConfirm
                ? LiveOpsConfirmLevel.TypeToConfirm
                : LiveOpsConfirmLevel.Level1;
            if (request.Level != expectedLevel)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.FeedbackErrorPolicyLevelMismatchFormat,
                    expectedLevel, request.Level));
            }
            if (expectedLevel == LiveOpsConfirmLevel.TypeToConfirm
                && !string.Equals(request.TypeToConfirmText, decision.TypeToConfirmText, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.FeedbackErrorPolicyTypeTextMismatchFormat,
                    decision.TypeToConfirmText, request.TypeToConfirmText));
            }
            return presenter.Confirm(request);
        }
    }
}
