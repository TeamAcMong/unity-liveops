// lint-sample-path: Packages/com.dreamtech.liveops/Runtime/Unity/Calendar/LiveEventCalendarAssetSample.cs
// Mẫu vi phạm runtime: gọi UnityEditor, gọi SHA-256 trên đường game, Resources.Load.
// lint-expect: runtime-forbidden
// lint-expect: runtime-forbidden
// lint-expect: package-forbidden
using UnityEngine;

namespace DreamTech.LiveOps.Unity
{
    public sealed class LiveEventCalendarAssetSample : ScriptableObject
    {
        public string Save(string json)
        {
            UnityEditor.AssetDatabase.SaveAssets();
            TextAsset fallback = Resources.Load<TextAsset>("calendar");
            return LiveEventCalendarSha256.ComputeHex(json) + fallback.text;
        }
    }
}
