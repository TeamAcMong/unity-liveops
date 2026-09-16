using DreamTech.LiveOps.Editor;
using NUnit.Framework;

/// <summary>
/// Ghim TIẾNG VIỆT cho toàn bộ assembly test hub (<c>[SetUpFixture]</c> ngoài mọi namespace áp cho mọi fixture).
/// <para>
/// Vì sao: hub đổi ngôn ngữ mặc định sang English ở G-I18N, nhưng 890 test đang có so đúng câu tiếng Việt — ghim ở đây giữ
/// chúng xanh mà không phải sửa một dòng câu chữ nào, và cũng tách test khỏi pref thật của máy người chạy. Test nào cần
/// tiếng Anh thì tự mở scope lồng (<see cref="LiveOpsHubLanguage.Override"/> xếp chồng LIFO).
/// </para>
/// </summary>
[SetUpFixture]
public sealed class LiveOpsHubLanguagePinFixture
{
    private static LiveOpsHubLanguageScope _pinnedVietnamese;

    /// <summary>Ngôn ngữ mà mọi test chạy dưới, trừ khi tự mở scope lồng.</summary>
    internal static LiveOpsHubLanguageId PinnedLanguage
    {
        get { return LiveOpsHubLanguageId.Vietnamese; }
    }

    [OneTimeSetUp]
    public void PinVietnamese()
    {
        _pinnedVietnamese = LiveOpsHubLanguage.Override(PinnedLanguage);
    }

    [OneTimeTearDown]
    public void ReleasePin()
    {
        if (_pinnedVietnamese != null) _pinnedVietnamese.Dispose();
        _pinnedVietnamese = null;
    }
}
