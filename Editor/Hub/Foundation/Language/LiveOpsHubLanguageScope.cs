using System;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Ghim ngôn ngữ trong một đoạn code (test và lệnh chụp), xếp chồng LIFO: <see cref="Dispose"/> trả về ngôn ngữ của scope
    /// ngoài, hết scope thì về giá trị pref. Scope KHÔNG ghi pref và KHÔNG bắn <see cref="LiveOpsHubLanguage.Changed"/> — ghim
    /// chỉ để đọc chữ theo một ngôn ngữ cố định, không phải hành vi người dùng đổi ngôn ngữ.
    /// <para>Dispose hai lần không sao; dispose lệch thứ tự chỉ gỡ đúng scope đó ra khỏi chồng (không kéo theo scope khác).</para>
    /// </summary>
    internal sealed class LiveOpsHubLanguageScope : IDisposable
    {
        private bool _isDisposed;

        internal LiveOpsHubLanguageScope(LiveOpsHubLanguageId language, LiveOpsHubLanguageScope outer)
        {
            Language = language;
            Outer = outer;
        }

        internal LiveOpsHubLanguageId Language { get; }

        /// <summary>Scope bao ngoài scope này; null = scope ngoài cùng (dispose xong thì đọc pref).</summary>
        internal LiveOpsHubLanguageScope Outer { get; set; }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            LiveOpsHubLanguage.EndScope(this);
        }
    }
}
