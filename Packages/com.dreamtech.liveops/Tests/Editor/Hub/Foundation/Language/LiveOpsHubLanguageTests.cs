using System;
using NUnit.Framework;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Dịch vụ ngôn ngữ (§1.3 đặc tả G-I18N): mặc định English, pref rác vẫn ra English, <c>Set</c> chỉ đụng đúng một khoá và
    /// chỉ bắn sự kiện khi thật sự đổi, scope ghim xếp chồng LIFO và không ghi pref.
    /// <para>
    /// Mọi test ở đây đọc/ghi pref THẬT của máy người chạy nên <see cref="TearDown"/> khôi phục nguyên trạng — kể cả khi assert
    /// ném giữa chừng. <see cref="LiveOpsHubLanguagePinFixture"/> đang ghim Tiếng Việt cho cả assembly, nên đường pref được soi
    /// qua <see cref="LiveOpsHubLanguage.PersistedLanguage"/> (không xét scope) thay vì qua <c>Current</c>.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class LiveOpsHubLanguageTests
    {
        private const string UnityUserSkinPreferenceKey = "UserSkin";
        private const string ProbePreferenceKey = "LiveOpsHub.LanguageTests.Probe";
        private const string ProbeValue = "untouched";

        private bool _hadStoredPreference;
        private string _storedPreference;

        [SetUp]
        public void SetUp()
        {
            _hadStoredPreference = EditorPrefs.HasKey(LiveOpsHubLanguage.EditorPreferenceKey);
            _storedPreference = EditorPrefs.GetString(LiveOpsHubLanguage.EditorPreferenceKey, string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadStoredPreference) EditorPrefs.SetString(LiveOpsHubLanguage.EditorPreferenceKey, _storedPreference);
            else EditorPrefs.DeleteKey(LiveOpsHubLanguage.EditorPreferenceKey);
            EditorPrefs.DeleteKey(ProbePreferenceKey);
            LiveOpsHubLanguage.ReloadFromPreference();
        }

        [Test]
        public void DefaultLanguage_IsEnglish_WhenPreferenceMissing()
        {
            EditorPrefs.DeleteKey(LiveOpsHubLanguage.EditorPreferenceKey);
            LiveOpsHubLanguage.ReloadFromPreference();

            Assert.AreEqual(LiveOpsHubLanguageId.English, LiveOpsHubLanguage.PersistedLanguage,
                "chưa chọn bao giờ thì hub nói tiếng Anh (D-L1)");
        }

        [Test]
        public void UnknownPreferenceValue_FallsBackToEnglish()
        {
            foreach (string stored in new[] { string.Empty, "  ", "Klingon", "7", "vietnamese" })
            {
                EditorPrefs.SetString(LiveOpsHubLanguage.EditorPreferenceKey, stored);
                LiveOpsHubLanguage.ReloadFromPreference();

                Assert.AreEqual(LiveOpsHubLanguageId.English, LiveOpsHubLanguage.PersistedLanguage,
                    "giá trị pref '" + stored + "' không đọc được — phải về English, không ném và không log Error");
            }
        }

        [Test]
        public void Set_PersistsUnderHubKeyOnly_AndRaisesChanged()
        {
            EditorPrefs.SetString(LiveOpsHubLanguage.EditorPreferenceKey, LiveOpsHubLanguageId.English.ToString());
            EditorPrefs.SetString(ProbePreferenceKey, ProbeValue);
            int skinBefore = EditorPrefs.GetInt(UnityUserSkinPreferenceKey, int.MinValue);
            LiveOpsHubLanguage.ReloadFromPreference();
            int raised = 0;
            Action handler = () => raised++;

            LiveOpsHubLanguage.Changed += handler;
            try
            {
                LiveOpsHubLanguage.Set(LiveOpsHubLanguageId.Vietnamese);
            }
            finally
            {
                LiveOpsHubLanguage.Changed -= handler;
            }

            Assert.AreEqual(1, raised, "đổi ngôn ngữ phải bắn Changed đúng một lần để cửa sổ dựng lại");
            Assert.AreEqual(LiveOpsHubLanguageId.Vietnamese.ToString(),
                EditorPrefs.GetString(LiveOpsHubLanguage.EditorPreferenceKey, string.Empty),
                "lựa chọn phải nằm dưới đúng khoá LiveOpsHub.Language");
            Assert.AreEqual(ProbeValue, EditorPrefs.GetString(ProbePreferenceKey, string.Empty),
                "Set không được đụng khoá pref nào khác (không DeleteAll)");
            Assert.AreEqual(skinBefore, EditorPrefs.GetInt(UnityUserSkinPreferenceKey, int.MinValue),
                "Set không được đụng pref UserSkin của Unity");
        }

        [Test]
        public void Set_SameValue_DoesNotRaiseChanged()
        {
            EditorPrefs.SetString(LiveOpsHubLanguage.EditorPreferenceKey, LiveOpsHubLanguageId.Vietnamese.ToString());
            LiveOpsHubLanguage.ReloadFromPreference();
            int raised = 0;
            Action handler = () => raised++;

            LiveOpsHubLanguage.Changed += handler;
            try
            {
                LiveOpsHubLanguage.Set(LiveOpsHubLanguageId.Vietnamese);
            }
            finally
            {
                LiveOpsHubLanguage.Changed -= handler;
            }

            Assert.AreEqual(0, raised, "đặt lại giá trị đang dùng không đổi gì — dựng lại cửa sổ lúc đó là nháy màn vô cớ");
        }

        [Test]
        public void Override_PinsLanguage_AndRestoresOnDispose()
        {
            LiveOpsHubLanguageId beforeScope = LiveOpsHubLanguage.Current;
            EditorPrefs.SetString(LiveOpsHubLanguage.EditorPreferenceKey, LiveOpsHubLanguageId.English.ToString());
            EditorPrefs.SetString(ProbePreferenceKey, ProbeValue);
            LiveOpsHubLanguage.ReloadFromPreference();
            int raised = 0;
            Action handler = () => raised++;
            LiveOpsHubLanguage.Changed += handler;

            try
            {
                LiveOpsHubLanguageScope scope = LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English);
                Assert.AreEqual(LiveOpsHubLanguageId.English, LiveOpsHubLanguage.Current);
                Assert.AreEqual(LiveOpsHubLanguageId.English.ToString(),
                    EditorPrefs.GetString(LiveOpsHubLanguage.EditorPreferenceKey, string.Empty),
                    "scope ghim không được ghi pref — ghim chỉ để đọc chữ, không phải người dùng đổi ngôn ngữ");

                scope.Dispose();
                Assert.AreEqual(beforeScope, LiveOpsHubLanguage.Current, "Dispose trả về ngôn ngữ của scope ngoài");
                Assert.DoesNotThrow(scope.Dispose, "Dispose hai lần không được ném");
                Assert.AreEqual(beforeScope, LiveOpsHubLanguage.Current);
            }
            finally
            {
                LiveOpsHubLanguage.Changed -= handler;
            }

            Assert.AreEqual(0, raised, "scope ghim không bắn Changed");
        }

        [Test]
        public void Override_Nested_RestoresOuterLanguage()
        {
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.Vietnamese))
            {
                Assert.AreEqual(LiveOpsHubLanguageId.Vietnamese, LiveOpsHubLanguage.Current);
                using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
                {
                    Assert.AreEqual(LiveOpsHubLanguageId.English, LiveOpsHubLanguage.Current);
                }

                Assert.AreEqual(LiveOpsHubLanguageId.Vietnamese, LiveOpsHubLanguage.Current,
                    "scope trong đóng thì ngôn ngữ về đúng scope ngoài (LIFO)");
            }
        }

        [Test]
        public void Available_ListsEnglishFirst()
        {
            Assert.AreEqual(2, LiveOpsHubLanguage.Available.Count, "P1 có đúng hai ngôn ngữ (D-L1)");
            Assert.AreEqual(LiveOpsHubLanguageId.English, LiveOpsHubLanguage.Available[0], "mặc định đứng đầu menu");
            Assert.AreEqual(LiveOpsHubLanguageId.Vietnamese, LiveOpsHubLanguage.Available[1]);
        }

        [Test]
        public void NativeNameAndShortCode_AreNotTranslated()
        {
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
            {
                Assert.AreEqual("Tiếng Việt", LiveOpsHubLanguage.NativeName(LiveOpsHubLanguageId.Vietnamese),
                    "người đang thấy giao diện tiếng Anh vẫn phải nhận ra dòng tiếng của mình");
                Assert.AreEqual("English", LiveOpsHubLanguage.NativeName(LiveOpsHubLanguageId.English));
                Assert.AreEqual("VI", LiveOpsHubLanguage.ShortCode(LiveOpsHubLanguageId.Vietnamese));
                Assert.AreEqual("EN", LiveOpsHubLanguage.ShortCode(LiveOpsHubLanguageId.English));
            }
        }
    }
}
