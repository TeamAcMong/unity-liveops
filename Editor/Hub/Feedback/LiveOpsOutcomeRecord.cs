using System;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Kết quả của một hành động xuất/đăng (8.5) — "còn đến khi bạn làm việc khác". Lớp <c>[Serializable]</c> với field
    /// <c>[SerializeField]</c> vì cửa sổ giữ nó trong <c>LiveOpsHubWindowState</c>: domain reload sau khi sửa script không
    /// được xoá mất dòng "Đã copy JSON 5b0d93" người dùng vừa đọc.
    /// <para>
    /// <see cref="State"/> lưu dạng số theo đúng số của <c>HealthState</c> (Ok = 0, Blocked = 2) thay vì kiểu enum đó: enum
    /// thuộc Foundation (G-HUBBASE) cùng đợt W1 nên gói này không được tham chiếu (V-9); view ép kiểu
    /// <c>(HealthState)record.State</c> được vì số khớp.
    /// </para>
    /// </summary>
    [Serializable]
    internal sealed class LiveOpsOutcomeRecord
    {
        /// <summary>= <c>(int)HealthState.Ok</c>.</summary>
        public const int OkState = 0;

        /// <summary>= <c>(int)HealthState.Blocked</c>.</summary>
        public const int BlockedState = 2;

        [SerializeField] private int state;
        [SerializeField] private string headline;
        [SerializeField] private string detail;
        [SerializeField] private string actionId;
        [SerializeField] private string actionArgument;
        [SerializeField] private string createdUtc;

        // Serializer của Unity (JsonUtility, [SerializeField] trong state cửa sổ) dựng instance bằng hàm dựng không tham số.
        private LiveOpsOutcomeRecord()
        {
        }

        private LiveOpsOutcomeRecord(int state, string headline, string detail, string actionId, string actionArgument, DateTime createdUtc)
        {
            this.state = state;
            this.headline = headline ?? string.Empty;
            this.detail = detail ?? string.Empty;
            this.actionId = actionId ?? string.Empty;
            this.actionArgument = actionArgument ?? string.Empty;
            // Giờ lưu dạng chuỗi ISO chuẩn: serializer của Unity không lưu DateTime, và chuỗi đọc được trong file state.
            this.createdUtc = LiveEventUtcText.Format(createdUtc);
        }

        public int State => state;
        public bool IsBlocked => state == BlockedState;
        public string Headline => headline ?? string.Empty;
        public string Detail => detail ?? string.Empty;

        /// <summary>Id nút hành động kèm theo (vd "reveal-file"), "" khi không có nút.</summary>
        public string ActionId => actionId ?? string.Empty;

        public string ActionArgument => actionArgument ?? string.Empty;
        public string CreatedUtcText => createdUtc ?? string.Empty;

        public bool TryGetCreatedUtc(out DateTime created) => LiveEventUtcText.TryParse(CreatedUtcText, out created);

        /// <param name="createdUtc">Giờ lấy từ đồng hồ của services (không đọc giờ máy ở model — test đóng băng được giờ).</param>
        public static LiveOpsOutcomeRecord Ok(string headline, string detail, DateTime createdUtc, string actionId = "", string actionArgument = "")
        {
            return new LiveOpsOutcomeRecord(OkState, headline, detail, actionId, actionArgument, createdUtc);
        }

        public static LiveOpsOutcomeRecord Blocked(string headline, string detail, DateTime createdUtc, string actionId = "", string actionArgument = "")
        {
            return new LiveOpsOutcomeRecord(BlockedState, headline, detail, actionId, actionArgument, createdUtc);
        }
    }
}
