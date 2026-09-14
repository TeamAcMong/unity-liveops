using System;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Định nghĩa một LOẠI event (vd "lava-quest"), không phải một đợt cụ thể — nhiều đợt cố định và tối đa một luật
    /// lặp có thể cùng mang loại này. Bất biến; các phương thức <c>With…</c> trả bản sao đã đổi.
    /// </summary>
    public sealed class LiveEventTypeDefinition
    {
        public const int ColorSlotCount = 8;

        public LiveEventTypeDefinition(string typeId, string displayName, int colorSlot, bool requiresJoin, string defaultConfigKey)
        {
            LiveEventInstance.ValidateIdentifier(typeId, nameof(typeId), "Id loại event");
            if (colorSlot < 0 || colorSlot >= ColorSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(colorSlot), "Ô màu phải trong [0, " + (ColorSlotCount - 1) + "].");
            }

            TypeId = typeId;
            DisplayName = displayName ?? string.Empty;
            ColorSlot = colorSlot;
            RequiresJoin = requiresJoin;
            DefaultConfigKey = defaultConfigKey ?? string.Empty;
        }

        public string TypeId { get; }
        public string DisplayName { get; }
        public int ColorSlot { get; }
        public bool RequiresJoin { get; }
        public string DefaultConfigKey { get; }

        public LiveEventTypeDefinition WithDisplayName(string displayName)
        {
            return new LiveEventTypeDefinition(TypeId, displayName, ColorSlot, RequiresJoin, DefaultConfigKey);
        }

        public LiveEventTypeDefinition WithColorSlot(int colorSlot)
        {
            return new LiveEventTypeDefinition(TypeId, DisplayName, colorSlot, RequiresJoin, DefaultConfigKey);
        }

        public LiveEventTypeDefinition WithRequiresJoin(bool requiresJoin)
        {
            return new LiveEventTypeDefinition(TypeId, DisplayName, ColorSlot, requiresJoin, DefaultConfigKey);
        }

        public LiveEventTypeDefinition WithDefaultConfigKey(string defaultConfigKey)
        {
            return new LiveEventTypeDefinition(TypeId, DisplayName, ColorSlot, RequiresJoin, defaultConfigKey);
        }
    }
}
