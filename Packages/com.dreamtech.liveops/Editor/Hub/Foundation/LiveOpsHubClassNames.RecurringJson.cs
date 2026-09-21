namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class USS của vùng RecurringJson (G-RECURRING-JSON, W5) — phần SỬA ĐƯỢC của foldout "JSON của luật này" (mục 7.4)
    /// và nhãn lý do khi một ô của form bị khoá vì ô khác đang giữ nháp (Q-W4-4, user duyệt 16/9).
    /// <para>
    /// Vì sao tên vẫn mang tiền tố <c>liveops-hub-recurring-…</c> mà file lại là <c>RecurringJson</c>: class là class riêng
    /// của MÀN <c>recurring</c> (check-class-names.py luật 3 nhận cả hai file vùng <c>Recurring</c> và <c>RecurringJson</c>
    /// cho màn đó), còn file thì theo QUYỀN GHI — <c>LiveOpsHubClassNames.Recurring.cs</c> là của G-RECURRING, gói này chỉ
    /// được ghi file vùng RecurringJson (V-5, bảng quyền ghi mục 2).
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        /// <summary>Ô nhập JSON nhiều dòng trong foldout — mono, cao tối thiểu để thấy cả object luật mà không cuộn.</summary>
        internal const string RecurringJsonEditor = "liveops-hub-recurring-json-editor";

        /// <summary>Dòng lỗi cú pháp/đọc lại ngay dưới ô ("Dòng 3, ký tự 18: thiếu dấu phẩy").</summary>
        internal const string RecurringJsonError = "liveops-hub-recurring-json-error";

        /// <summary>
        /// Nhãn lý do in THÀNH CHỮ cạnh một ô đang bị khoá (SPIKE-B SP-3: lý do khoá luôn là chữ, tooltip chỉ phụ, không
        /// test nào assert tooltip). Dùng cho ba ô bị khoá khi ô thứ tư đang giữ nháp (Q-W4-4).
        /// </summary>
        internal const string RecurringFieldLockReason = "liveops-hub-recurring-field-lock-reason";
    }
}
