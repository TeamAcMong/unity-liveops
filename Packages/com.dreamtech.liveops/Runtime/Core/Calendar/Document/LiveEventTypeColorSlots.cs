using System;
using System.Collections.Generic;
using System.Text;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Ô màu mặc định cho một loại event mới, và gợi ý khi hai loại trùng ô. Hub ghi ngay kết quả của
    /// <see cref="DefaultSlotFor"/> vào field <c>colorSlot</c> của loại lúc tạo — mọi màn sau đó chỉ đọc field đó, không
    /// gọi lại hàm này, nên đổi thuật toán băm ở bản sau không làm loại cũ đổi màu.
    /// </summary>
    public static class LiveEventTypeColorSlots
    {
        /// <summary>
        /// Murmur3Finalizer(FNV1a32(utf8(typeId))) % 8 — KHÔNG dùng <see cref="string.GetHashCode()"/> vì giá trị đó đổi
        /// giữa các lần chạy .NET (không tất định), làm màu loại nhảy mỗi lần mở Editor.
        /// </summary>
        public static int DefaultSlotFor(string typeId)
        {
            if (typeId == null) throw new ArgumentNullException(nameof(typeId));

            uint hash = Fnv1a32(typeId);
            hash = Murmur3Finalizer(hash);
            return (int)(hash % LiveEventTypeDefinition.ColorSlotCount);
        }

        /// <summary>Ô chưa loại nào dùng, tăng dần — chỉ để GỢI Ý khi trùng màu; hub không tự ghi (người dùng bấm chọn mới ghi).</summary>
        public static IReadOnlyList<int> FreeSlots(IReadOnlyList<LiveEventTypeDefinition> existingTypes)
        {
            var used = new bool[LiveEventTypeDefinition.ColorSlotCount];
            if (existingTypes != null)
            {
                for (int index = 0; index < existingTypes.Count; index++)
                {
                    LiveEventTypeDefinition type = existingTypes[index];
                    if (type != null) used[type.ColorSlot] = true;
                }
            }

            var free = new List<int>();
            for (int slot = 0; slot < used.Length; slot++)
            {
                if (!used[slot]) free.Add(slot);
            }
            return free;
        }

        /// <summary>Loại khác cùng ô màu với <paramref name="type"/> (để màn Loại event báo Warning trùng màu).</summary>
        public static IReadOnlyList<LiveEventTypeDefinition> TypesSharingSlot(LiveEventTypeDefinition type,
            IReadOnlyList<LiveEventTypeDefinition> allTypes)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var sharing = new List<LiveEventTypeDefinition>();
            if (allTypes == null) return sharing;
            for (int index = 0; index < allTypes.Count; index++)
            {
                LiveEventTypeDefinition candidate = allTypes[index];
                if (candidate == null || ReferenceEquals(candidate, type)) continue;
                if (string.Equals(candidate.TypeId, type.TypeId, StringComparison.Ordinal)) continue;
                if (candidate.ColorSlot == type.ColorSlot) sharing.Add(candidate);
            }
            return sharing;
        }

        private static uint Fnv1a32(string text)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            uint hash = offsetBasis;
            for (int index = 0; index < textBytes.Length; index++)
            {
                hash ^= textBytes[index];
                hash *= prime;
            }
            return hash;
        }

        /// <summary>fmix32 của MurmurHash3 — trộn thêm để bù cho bước nhân cuối yếu của FNV-1a.</summary>
        private static uint Murmur3Finalizer(uint hash)
        {
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;
            return hash;
        }
    }
}
