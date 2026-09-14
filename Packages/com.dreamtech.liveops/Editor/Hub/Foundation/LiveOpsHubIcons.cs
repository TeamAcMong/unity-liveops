using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Nguồn icon duy nhất của hub ([FD §2.12], [API §7.1]). Vì sao không gọi thẳng <c>EditorGUIUtility.IconContent</c>:
    /// (1) tên thiếu trên một bản Unity làm IconContent log Error → fail test ở bản đó (<c>winbtn_win_close</c> NULL ở
    /// 6000.6, PD-14), còn <c>FindTexture</c> trả null không log; (2) cache của IconContent theo tên, không xoá khi đổi
    /// skin, nên helper tự chọn <c>d_</c> và cache theo (tên, skin) rồi <see cref="ClearCache"/> khi probe skin bắn;
    /// (3) icon không có sẵn (lịch, Clipboard bản tối) đọc PNG của package.
    /// </summary>
    internal static class LiveOpsHubIcons
    {
        private const string SpinnerFramePrefix = "WaitSpin";
        private const int SpinnerFrameCount = 12;

        // Icon dựng sẵn không có bản d_ ([API §7.1]): gắn d_ vào sẽ trượt, nên gọi thẳng tên gốc ở skin tối.
        private static readonly HashSet<string> NamesWithoutDarkVariant = new HashSet<string>(StringComparer.Ordinal)
        {
            "Clipboard", "TestPassed", "TestFailed", "Error", "Info", "UpArrow",
        };

        private static readonly Dictionary<IconCacheKey, Texture> Cache = new Dictionary<IconCacheKey, Texture>();

        private static readonly string[] DesignNames = BuildDesignNames();

        /// <summary>
        /// Mọi tên icon trong lưới thiết kế ([FD §2.12]) — nút đóng là <c>clear</c> thay cho <c>winbtn_win_close</c> (PD-14),
        /// đủ 12 khung spinner, icon lịch của package. Test <c>Icons_AllNamesLoad</c> và probe CLI duyệt danh sách này.
        /// </summary>
        public static IReadOnlyList<string> AllDesignNames { get; } = Array.AsReadOnly(DesignNames);

        /// <summary>Texture theo skin hiện tại; null khi không tìm được ở mọi nguồn (không log — nơi gọi tự quyết).</summary>
        public static Texture Get(string name)
        {
            return Get(name, EditorGUIUtility.isProSkin);
        }

        /// <summary>
        /// Bản chọn skin tường minh: test kiểm nhánh skin tối/sáng mà không đổi skin của Editor (đổi skin bằng code ghi
        /// Preferences toàn máy của người dùng và treo batchmode — SP-4).
        /// </summary>
        internal static Texture Get(string name, bool darkSkin)
        {
            if (string.IsNullOrEmpty(name)) return null;
            IconCacheKey key = new IconCacheKey(name, darkSkin);
            if (Cache.TryGetValue(key, out Texture cached) && cached != null) return cached;

            Texture texture = Load(name, darkSkin);
            if (texture != null) Cache[key] = texture;
            return texture;
        }

        /// <summary>Gọi khi skin đổi (probe <c>--liveops-hub-skin-probe</c> hoặc OnFocus) rồi dựng lại icon đang hiện.</summary>
        public static void ClearCache()
        {
            Cache.Clear();
        }

        /// <summary>
        /// Image cỡ cố định bằng class (10/12/14/16) + <c>ScaleToFit</c>: icon dựng sẵn có cỡ lệch (Search Icon 64×64,
        /// FolderOpened 256×256, InspectorLock 16×14) nên không để texture tự quyết kích thước.
        /// </summary>
        public static Image CreateImage(string name, int size)
        {
            Image image = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.AddToClassList(LiveOpsHubClassNames.Icon);
            image.AddToClassList(SizeClassOf(size));
            image.image = Get(name);
            return image;
        }

        /// <summary>Tên khung spinner thứ <paramref name="frameIndex"/> (vòng 12 khung WaitSpin00…11).</summary>
        internal static string SpinnerFrameName(int frameIndex)
        {
            int wrapped = ((frameIndex % SpinnerFrameCount) + SpinnerFrameCount) % SpinnerFrameCount;
            return SpinnerFramePrefix + wrapped.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static int SpinnerFrames => SpinnerFrameCount;

        internal static string SizeClassOf(int size)
        {
            switch (size)
            {
                case 10: return LiveOpsHubClassNames.IconSize10;
                case 12: return LiveOpsHubClassNames.IconSize12;
                case 14: return LiveOpsHubClassNames.IconSize14;
                case 16: return LiveOpsHubClassNames.IconSize16;
                default: throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }
        }

        private static Texture Load(string name, bool darkSkin)
        {
            // Clipboard dựng sẵn thiếu bản tối ở cả hai bản Unity → skin tối dùng PNG tự vẽ của package.
            if (darkSkin && string.Equals(name, "Clipboard", StringComparison.Ordinal))
            {
                Texture packageClipboard = LoadPackagePng(LiveOpsHubPaths.ClipboardIconName, true);
                if (packageClipboard != null) return packageClipboard;
            }

            if (name.StartsWith(LiveOpsHubPaths.PackageIconNamePrefix, StringComparison.Ordinal))
            {
                return LoadPackagePng(name, darkSkin);
            }

            // FindTexture không log khi trượt (khác IconContent) — tên thiếu ở một bản không làm fail test của bản đó.
            if (darkSkin && !NamesWithoutDarkVariant.Contains(name))
            {
                Texture dark = EditorGUIUtility.FindTexture(LiveOpsHubPaths.DarkIconPrefix + name);
                if (dark != null) return dark;
            }
            return EditorGUIUtility.FindTexture(name);
        }

        private static Texture LoadPackagePng(string name, bool darkSkin)
        {
            bool retina = EditorGUIUtility.pixelsPerPoint > 1f;
            // Thứ tự thử: đúng skin + đúng mật độ → đúng skin 1x → skin còn lại (icon chỉ có bản tối vẫn hiện thay vì trống).
            string[] candidates = darkSkin
                ? new[] { PackagePath(name, true, retina), PackagePath(name, true, false), PackagePath(name, false, retina), PackagePath(name, false, false) }
                : new[] { PackagePath(name, false, retina), PackagePath(name, false, false), PackagePath(name, true, retina), PackagePath(name, true, false) };
            foreach (string path in candidates)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null) return texture;
            }
            return null;
        }

        internal static string PackagePath(string name, bool darkSkin, bool retina)
        {
            return LiveOpsHubPaths.IconsDirectory
                   + (darkSkin ? LiveOpsHubPaths.DarkIconPrefix : string.Empty)
                   + name
                   + (retina ? LiveOpsHubPaths.RetinaIconSuffix : string.Empty)
                   + LiveOpsHubPaths.IconFileExtension;
        }

        private static string[] BuildDesignNames()
        {
            List<string> names = new List<string>
            {
                "Toolbar Plus", "TreeEditor.Trash", "Refresh", "Search Icon", "_Menu", "Settings",
                "console.erroricon.sml", "console.warnicon.sml", "console.infoicon.sml",
                "PlayButton", "PauseButton", "Animation.PrevKey", "Animation.NextKey",
                "Clipboard", "SaveAs", "FolderOpened Icon", "UnityEditor.ConsoleWindow", "InspectorLock",
                "FilterByType", "UnityEditor.HistoryWindow", "preAudioLoopOff", "Valid", "VisibilityOn", "_Help",
                "clear", LiveOpsHubPaths.CalendarIconName,
            };
            for (int frameIndex = 0; frameIndex < SpinnerFrameCount; frameIndex++)
            {
                names.Add(SpinnerFrameName(frameIndex));
            }
            return names.ToArray();
        }

        private readonly struct IconCacheKey : IEquatable<IconCacheKey>
        {
            private readonly string _name;
            private readonly bool _darkSkin;

            public IconCacheKey(string name, bool darkSkin)
            {
                _name = name;
                _darkSkin = darkSkin;
            }

            public bool Equals(IconCacheKey other)
            {
                return _darkSkin == other._darkSkin && string.Equals(_name, other._name, StringComparison.Ordinal);
            }

            public override bool Equals(object other)
            {
                return other is IconCacheKey key && Equals(key);
            }

            public override int GetHashCode()
            {
                return StringComparer.Ordinal.GetHashCode(_name) * 2 + (_darkSkin ? 1 : 0);
            }
        }
    }
}
