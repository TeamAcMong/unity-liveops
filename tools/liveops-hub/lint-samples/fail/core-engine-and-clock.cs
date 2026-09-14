// lint-sample-path: Packages/com.dreamtech.liveops/Runtime/Core/Calendar/Document/EngineLeak.cs
// Mẫu vi phạm: core tham chiếu UnityEngine và đọc đồng hồ máy trực tiếp.
// lint-expect: core-engine-reference
// lint-expect: clock-direct
using System;
using UnityEngine;

namespace DreamTech.LiveOps
{
    public sealed class EngineLeak
    {
        public DateTime Stamp() => DateTime.UtcNow;
    }
}
