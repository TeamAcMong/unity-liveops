using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Unity
{
    /// <summary>Lắp loại event từ <see cref="LiveEventCalendarAsset"/> vào <see cref="LiveOpsSystemBuilder"/>.</summary>
    public static class LiveOpsSystemBuilderCalendarAssetExtensions
    {
        /// <summary>
        /// Đăng ký MỌI loại trong asset theo thứ tự làn: <c>requiresJoin</c> → <see cref="LiveEventJoinPolicy.ExplicitJoin"/>,
        /// còn lại <see cref="LiveEventJoinPolicy.JoinOnFirstProgress"/>. Luật quà và điều kiện theo loại do game cấp qua
        /// hai hàm (trả <c>null</c> = mặc định của builder) — asset không mang được code của game.
        ///
        /// <para>Vì sao đọc loại qua <see cref="LiveEventCalendarAsset.ToDocument"/>: loại có id sai quy tắc đã bị bỏ ở đó,
        /// nên asset sửa tay hỏng không làm <c>Build()</c> của game ném. Loại đã đăng ký tay trước đó mà trùng id thì builder
        /// vẫn ném như <c>WithEventType</c> — đó là lỗi lắp ráp của game, không phải dữ liệu từ xa.</para>
        /// </summary>
        public static LiveOpsSystemBuilder WithEventTypesFrom(this LiveOpsSystemBuilder builder, LiveEventCalendarAsset asset,
            Func<string, ILiveEventCompletionRule> completionRuleForType = null,
            Func<string, ILiveEventEligibility> eligibilityForType = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (asset == null) throw new ArgumentNullException(nameof(asset));

            IReadOnlyList<LiveEventTypeDefinition> types = asset.ToDocument().EventTypes;
            for (int index = 0; index < types.Count; index++)
            {
                LiveEventTypeDefinition type = types[index];
                ILiveEventCompletionRule completionRule = completionRuleForType != null ? completionRuleForType(type.TypeId) : null;
                ILiveEventEligibility eligibility = eligibilityForType != null ? eligibilityForType(type.TypeId) : null;
                LiveEventJoinPolicy joinPolicy = type.RequiresJoin ? LiveEventJoinPolicy.ExplicitJoin : LiveEventJoinPolicy.JoinOnFirstProgress;
                builder.WithEventType(type.TypeId, completionRule, eligibility, joinPolicy);
            }
            return builder;
        }
    }
}
