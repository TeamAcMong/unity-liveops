using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps
{
    /// <summary>Một dòng quà: id vật phẩm theo cách game đặt tên ("coin", "booster.magnet") và số lượng.</summary>
    public sealed class LiveOpsRewardItem : IEquatable<LiveOpsRewardItem>
    {
        public LiveOpsRewardItem(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId)) throw new ArgumentException("Item id không được rỗng.", nameof(itemId));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số lượng quà phải lớn hơn 0.");
            ItemId = itemId;
            Amount = amount;
        }

        public string ItemId { get; }
        public int Amount { get; }

        public bool Equals(LiveOpsRewardItem other)
        {
            return !ReferenceEquals(other, null) && string.Equals(ItemId, other.ItemId, StringComparison.Ordinal) && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LiveOpsRewardItem);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ItemId.GetHashCode() * 397) ^ Amount;
            }
        }

        public override string ToString()
        {
            return ItemId + " x" + Amount.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Gói quà. Package không hiểu id vật phẩm — granter của game đổi sang kho đồ thật. <see cref="PresentationId"/> là gợi ý hiển
    /// thị cho UI (vd "chest.gold" để chọn hình rương), để trống nếu không cần. Gói chỉ có <see cref="PresentationId"/> mà không
    /// có dòng quà vẫn hợp lệ: game tự quyết nội dung lúc mở rương.
    /// </summary>
    public sealed class LiveOpsRewardBundle : IEquatable<LiveOpsRewardBundle>
    {
        public static readonly LiveOpsRewardBundle None = new LiveOpsRewardBundle(string.Empty, null);

        public LiveOpsRewardBundle(string presentationId, IEnumerable<LiveOpsRewardItem> items)
        {
            PresentationId = presentationId ?? string.Empty;
            var list = new List<LiveOpsRewardItem>();
            if (items != null)
            {
                foreach (LiveOpsRewardItem item in items)
                {
                    if (item != null) list.Add(item);
                }
            }
            Items = list.AsReadOnly();
        }

        public string PresentationId { get; }
        public IReadOnlyList<LiveOpsRewardItem> Items { get; }
        public bool IsEmpty => Items.Count == 0 && PresentationId.Length == 0;

        public bool Equals(LiveOpsRewardBundle other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (!string.Equals(PresentationId, other.PresentationId, StringComparison.Ordinal) || Items.Count != other.Items.Count) return false;
            for (int index = 0; index < Items.Count; index++)
            {
                if (!Items[index].Equals(other.Items[index])) return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LiveOpsRewardBundle);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PresentationId.GetHashCode();
                foreach (LiveOpsRewardItem item in Items) hash = (hash * 397) ^ item.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (IsEmpty) return "(không có quà)";
            var builder = new StringBuilder();
            if (PresentationId.Length > 0) builder.Append('[').Append(PresentationId).Append(']');
            for (int index = 0; index < Items.Count; index++)
            {
                builder.Append(index == 0 && PresentationId.Length == 0 ? string.Empty : index == 0 ? " " : ", ");
                builder.Append(Items[index]);
            }
            return builder.ToString();
        }
    }
}
