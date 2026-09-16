using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Thư viện mẫu luật lặp của một dự án ([SD1 §4.1]): mỗi mẫu là một bộ (neo, chu kỳ, thời gian chạy) đặt tên sẵn để người
    /// vận hành chọn thay vì gõ lại ba con số. Là ScriptableObject để mỗi game tự thêm nhịp riêng mà không sửa package; hub
    /// gom mọi thư viện trong dự án cộng với mẫu dựng sẵn qua <see cref="LiveOpsRulePresets.Resolve"/>.
    /// <para>
    /// Mẫu KHÔNG mang loại event và tiền tố id: hai thứ đó là danh tính của luật (đổi = người chơi mất tiến độ), nên chúng
    /// luôn do người dùng đặt trên form, không bao giờ do một mẫu áp vào.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "DreamTech/LiveOps/Recurring rule template library", fileName = "LiveOpsRulePresets")]
    public sealed class LiveOpsRulePresetLibrary : ScriptableObject
    {
        [SerializeField] private List<Preset> _presets = new List<Preset>();

        public IReadOnlyList<Preset> Presets => _presets ?? (_presets = new List<Preset>());

        /// <summary>Một mẫu. Giờ ghi bằng chữ ISO như trong asset lịch, để mẫu hỏng cũng chỉ hỏng một mẫu (không ném lúc nạp).</summary>
        [Serializable]
        public sealed class Preset
        {
            [SerializeField] private string _displayName;
            [SerializeField] private string _anchorUtcText;
            [SerializeField] private int _periodHours;
            [SerializeField] private int _activeHours;

            /// <summary>Unity cần hàm dựng không tham số để nạp danh sách đã serialize.</summary>
            public Preset()
            {
                _displayName = string.Empty;
                _anchorUtcText = string.Empty;
            }

            public Preset(string displayName, string anchorUtcText, int periodHours, int activeHours)
            {
                _displayName = displayName ?? string.Empty;
                _anchorUtcText = anchorUtcText ?? string.Empty;
                _periodHours = periodHours;
                _activeHours = activeHours;
            }

            public string DisplayName => _displayName ?? string.Empty;
            public string AnchorUtcText => _anchorUtcText ?? string.Empty;
            public int PeriodHours => _periodHours;
            public int ActiveHours => _activeHours;

            /// <summary>Mẫu dùng được: có tên, neo đọc được, chu kỳ dương và thời gian chạy nằm trong (0, chu kỳ].</summary>
            public bool IsUsable
            {
                get
                {
                    if (DisplayName.Length == 0 || PeriodHours <= 0) return false;
                    if (ActiveHours <= 0 || ActiveHours > PeriodHours) return false;
                    DateTime anchorUtc;
                    return LiveEventUtcText.TryParse(AnchorUtcText, out anchorUtc);
                }
            }
        }
    }
}
