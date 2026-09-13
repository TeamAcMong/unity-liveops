using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Chỗ gặp nhau giữa các assembly: composition root đăng ký, host (icon Home, màn event, popup kết quả, cheat) tra theo id.
    /// Chỉ dùng trên main thread; Reset trước khi reload assembly.
    /// </summary>
    public static class LiveOpsSystemRegistry
    {
        private static readonly Dictionary<string, LiveOpsSystem> Systems = new Dictionary<string, LiveOpsSystem>(StringComparer.Ordinal);

        public static event Action<LiveOpsSystem> SystemRegistered;

        public static void Register(LiveOpsSystem system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            Systems[system.SystemId] = system;
            SystemRegistered?.Invoke(system);
        }

        /// <summary>Chỉ gỡ khi đúng tham chiếu đang đăng ký.</summary>
        public static bool Unregister(LiveOpsSystem system)
        {
            if (system == null) return false;
            if (!Systems.TryGetValue(system.SystemId, out LiveOpsSystem existing) || !ReferenceEquals(existing, system)) return false;
            return Systems.Remove(system.SystemId);
        }

        public static bool TryGet(string systemId, out LiveOpsSystem system)
        {
            if (systemId == null)
            {
                system = null;
                return false;
            }
            return Systems.TryGetValue(systemId, out system);
        }

        public static void Reset()
        {
            Systems.Clear();
            SystemRegistered = null;
        }
    }
}
