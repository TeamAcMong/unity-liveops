// lint-sample-path: Packages/com.dreamtech.liveops/Runtime/Unity/LiveEventVariantSample.cs
// Mẫu vi phạm biến thể bị bỏ sót trước F11: Resources.LoadAll (không phải Resources.Load trần) và
// System.Linq.Enumerable.Count viết đủ đường dẫn (không có `using System.Linq;`).
// Đặt ở Runtime/Unity (không phải Runtime/Core) để tách riêng luật package-forbidden khỏi
// core-engine-reference — Core cấm UnityEngine hoàn toàn nên dùng UnityEngine.Resources ở đó sẽ
// kéo thêm lỗi core-engine-reference, làm mẫu không còn cô lập đúng 2 biến thể F11.
// lint-expect: package-forbidden
// lint-expect: package-forbidden
namespace DreamTech.LiveOps
{
    internal sealed class LiveEventVariantSample
    {
        public int Count(object[] items)
        {
            UnityEngine.Object[] all = UnityEngine.Resources.LoadAll("calendar");
            return System.Linq.Enumerable.Count(items) + all.Length;
        }
    }
}
