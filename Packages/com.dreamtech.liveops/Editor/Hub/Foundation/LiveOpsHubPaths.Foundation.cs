namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phần vùng Foundation của <see cref="LiveOpsHubPaths"/> (V-5, G-HUBBASE): tên icon PNG của package và đường dẫn
    /// font trong bundle Editor. Tách khỏi file gốc vì file gốc do G-SKELETON đóng từ W0.
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        /// <summary>Thư mục PNG icon tự vẽ; tên file = [d_]tên[@2x].png — cùng quy ước với icon dựng sẵn của Unity.</summary>
        internal const string IconsDirectory = HubRoot + "/Icons/";

        /// <summary>Tiền tố tên icon của package — tên bắt đầu bằng tiền tố này đọc PNG trong <see cref="IconsDirectory"/>.</summary>
        internal const string PackageIconNamePrefix = "liveops-hub-";

        internal const string CalendarIconName = "liveops-hub-calendar";

        /// <summary>
        /// Chỉ có bản tối: icon <c>Clipboard</c> dựng sẵn thiếu <c>d_</c> ở cả hai bản Unity (skin tối trả bản sáng, gần như
        /// mất trên nền #383838) — skin sáng vẫn dùng icon của Unity.
        /// </summary>
        internal const string ClipboardIconName = "liveops-hub-clipboard";

        internal const string DarkIconPrefix = "d_";
        internal const string RetinaIconSuffix = "@2x";
        internal const string IconFileExtension = ".png";

        /// <summary>
        /// Font mono của bundle Editor: phải là FontAsset SDF (đuôi .asset) và gán bằng <c>-unity-font-definition</c>;
        /// gán Font .ttf qua <c>-unity-font</c> thì chữ không đổi mà không báo lỗi ([FD §2.9], [API §7.3]).
        /// </summary>
        internal const string MonoFontResource = "Fonts/RobotoMono/RobotoMono-Regular SDF.asset";

        /// <summary>
        /// File font gốc của font Label mặc định (Inter-Regular SDF). Bản SDF không <c>Load</c> được theo đường dẫn ở cả hai
        /// bản Unity ([API §12.3]) nên kiểm glyph ngoài panel đọc thẳng file .ttf mà SDF được dựng từ đó.
        /// </summary>
        internal const string DefaultEditorFontResource = "Fonts/Inter/Inter-Regular.ttf";
    }
}
