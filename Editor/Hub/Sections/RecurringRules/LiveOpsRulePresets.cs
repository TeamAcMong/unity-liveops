using System;
using System.Collections.Generic;
using UnityEditor;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Nguồn mẫu luật lặp cho dropdown "Mẫu" ([SD1 §4.1]): mọi <see cref="LiveOpsRulePresetLibrary"/> trong dự án, rồi tới ba
    /// mẫu dựng sẵn của package. Dự án đứng trước vì nhịp riêng của game là thứ người vận hành chọn hằng ngày; mẫu dựng sẵn
    /// luôn còn ở cuối nên dropdown không bao giờ rỗng.
    /// <para>
    /// Tên mẫu dựng sẵn lấy từ catalog MỖI LẦN gọi, không cache: đổi ngôn ngữ hub xong mở lại màn thì dropdown phải đổi theo.
    /// </para>
    /// </summary>
    internal static class LiveOpsRulePresets
    {
        /// <summary>Thứ Hai 5/1/2026 00:00 UTC — mốc của mọi mẫu dựng sẵn, trùng neo trong dữ liệu mẫu thiết kế.</summary>
        internal const string BuiltInAnchorUtcText = "2026-01-05T00:00:00Z";

        internal const int HoursPerDay = 24;
        internal const int HoursPerWeek = 168;
        internal const int HoursPerTwoWeeks = 336;
        internal const int DailyActiveHours = 20;

        /// <summary>Không tìm thấy mẫu nào khớp — dropdown hiện "Tùy chỉnh…".</summary>
        internal const int CustomIndex = -1;

        private const string PresetLibraryFilter = "t:LiveOpsRulePresetLibrary";

        public static IReadOnlyList<LiveOpsRulePresetLibrary.Preset> BuiltIn
        {
            get
            {
                return new[]
                {
                    new LiveOpsRulePresetLibrary.Preset(LiveOpsHubStrings.RecurringPresetWeeklyMonday, BuiltInAnchorUtcText, HoursPerWeek, HoursPerWeek),
                    new LiveOpsRulePresetLibrary.Preset(LiveOpsHubStrings.RecurringPresetDailyRunTwenty, BuiltInAnchorUtcText, HoursPerDay, DailyActiveHours),
                    new LiveOpsRulePresetLibrary.Preset(LiveOpsHubStrings.RecurringPresetBiweeklyMonday, BuiltInAnchorUtcText, HoursPerTwoWeeks, HoursPerWeek),
                };
            }
        }

        /// <summary>
        /// Mẫu của dự án + mẫu dựng sẵn. Mẫu hỏng (neo không đọc được, chu kỳ ≤ 0, chạy ngoài (0, chu kỳ]) bị bỏ im lặng: một
        /// asset sai của game không được làm hỏng dropdown của hub, và người dùng vẫn còn mẫu dựng sẵn để chọn.
        /// </summary>
        public static IReadOnlyList<LiveOpsRulePresetLibrary.Preset> Resolve()
        {
            List<LiveOpsRulePresetLibrary.Preset> presets = new List<LiveOpsRulePresetLibrary.Preset>();
            // Tên hiển thị là thứ DUY NHẤT dropdown cho người dùng thấy: hai mẫu trùng tên thì mẫu sau không có cách nào
            // chọn được (ô chỉ giữ chuỗi), nên bỏ nó đi còn thật hơn là để một mục bấm vào lại ra mẫu khác.
            HashSet<string> usedDisplayNames = new HashSet<string>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets(PresetLibraryFilter);
            for (int index = 0; index < guids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[index]);
                if (string.IsNullOrEmpty(assetPath)) continue;
                LiveOpsRulePresetLibrary library = AssetDatabase.LoadAssetAtPath<LiveOpsRulePresetLibrary>(assetPath);
                if (library == null) continue;
                IReadOnlyList<LiveOpsRulePresetLibrary.Preset> libraryPresets = library.Presets;
                for (int presetIndex = 0; presetIndex < libraryPresets.Count; presetIndex++)
                {
                    LiveOpsRulePresetLibrary.Preset preset = libraryPresets[presetIndex];
                    if (preset == null || !preset.IsUsable) continue;
                    if (!usedDisplayNames.Add(preset.DisplayName)) continue;
                    presets.Add(preset);
                }
            }
            IReadOnlyList<LiveOpsRulePresetLibrary.Preset> builtIn = BuiltIn;
            for (int index = 0; index < builtIn.Count; index++)
            {
                if (usedDisplayNames.Add(builtIn[index].DisplayName)) presets.Add(builtIn[index]);
            }
            return presets;
        }

        /// <summary>Mẫu đầu tiên khớp cả neo, chu kỳ và thời gian chạy của luật; <see cref="CustomIndex"/> khi sửa tay.</summary>
        public static int IndexMatching(RecurringLiveEventRule rule, IReadOnlyList<LiveOpsRulePresetLibrary.Preset> presets)
        {
            if (rule == null || presets == null) return CustomIndex;
            for (int index = 0; index < presets.Count; index++)
            {
                LiveOpsRulePresetLibrary.Preset preset = presets[index];
                if (preset == null) continue;
                if (preset.PeriodHours != rule.PeriodHours || preset.ActiveHours != rule.ActiveHours) continue;
                if (!SameInstant(preset.AnchorUtcText, rule.AnchorUtcText)) continue;
                return index;
            }
            return CustomIndex;
        }

        /// <summary>So theo MỐC, không theo chữ: "2026-01-05T00:00:00Z" và một cách viết khác cùng mốc vẫn là một mẫu.</summary>
        private static bool SameInstant(string leftText, string rightText)
        {
            DateTime left;
            DateTime right;
            if (!LiveEventUtcText.TryParse(leftText, out left) || !LiveEventUtcText.TryParse(rightText, out right)) return false;
            return left == right;
        }
    }
}
