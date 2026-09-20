using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Bằng chứng đo tương phản màu ĐÃ HỢP THÀNH ở một skin Editor — file JSON do lượt chụp ghi ra, lượt EditMode đọc lại.
    /// <para>
    /// Vì sao phải đi vòng qua FILE thay vì đo thẳng trong lượt cổng (phiếu W9-27): cửa sổ hub thật vẽ theo skin ĐANG CHẠY
    /// của Editor, mà đổi <c>EditorPrefs UserSkin</c> là việc riêng của <c>capture.sh</c> (SP-4) — mọi lượt cổng chạy skin
    /// TỐI, nên luật màu-đã-hợp-thành chưa từng có số đo ở skin SÁNG. Ép class <c>liveops-hub--skin-light</c> trong tiến
    /// trình KHÔNG thay được: skin sáng thật đổi cả biến của chính Unity (nền hàng đang chọn, viền, tint của
    /// <c>:disabled</c>), nên ép class chỉ đổi token CỦA HUB và đo ra một cửa sổ không có thật; hơn nữa
    /// <c>LiveOpsHubSkin</c> nghe <c>CustomStyleResolvedEvent</c> và bật lại class theo probe nền thật, tức muốn giữ ép
    /// phải mở một cửa hậu test-only trong mã sản phẩm.
    /// </para>
    /// <para>
    /// Giá phải trả, khai thẳng: cổng phụ thuộc vào một FILE sinh ngoài lượt EditMode. Vì vậy dấu của file phải chặt —
    /// <see cref="UxContrastEvidenceData.sourceDigest"/> băm TOÀN BỘ mã nguồn của hub (<c>Editor/Hub/**</c> .cs + .uss), nên
    /// bất kỳ sửa đổi nào ở thứ đang được đo đều làm bằng chứng cũ LỆCH và test đỏ. Bằng chứng cũ mà cổng vẫn xanh thì đó là
    /// cổng tự lừa mình.
    /// </para>
    /// </summary>
    internal static class UxContrastEvidence
    {
        internal const string LightSkin = "light";
        internal const string DarkSkin = "dark";

        /// <summary>Phiên bản khuôn JSON — đổi phép đo thì tăng số này để bằng chứng cũ không còn đọc được.</summary>
        internal const int SchemaVersion = 1;

        private const string EvidenceRelativeDirectory = ".cache/unity-liveops/ux-contrast";

        /// <summary>Thư mục của hub được băm vào dấu — đúng thứ phép đo nói về.</summary>
        private const string HubSourceRelativeDirectory = "Editor/Hub";

        /// <summary>
        /// Đường dẫn bằng chứng của một skin trên bản Unity ĐANG CHẠY:
        /// <c>~/.cache/unity-liveops/ux-contrast/&lt;bản Unity&gt;/composed-&lt;skin&gt;.json</c>.
        /// <para>
        /// Tách theo bản Unity vì cửa sổ hub của hai bản không dựng ra cùng một cây element (header màn cao 36 hay 61 tuỳ
        /// bản), nên một bằng chứng dùng chung sẽ nói về một cây khác với cây đang chạy.
        /// </para>
        /// </summary>
        internal static string PathFor(string skin)
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string directory = Path.Combine(Path.Combine(home, EvidenceRelativeDirectory), Application.unityVersion);
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "composed-" + skin + ".json");
        }

        /// <summary>Gốc package trên đĩa; null khi không hỏi được (assembly không chạy như một phần của package).</summary>
        internal static string PackageDirectory()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UxContrastEvidence).Assembly);
            return package == null ? null : Path.GetFullPath(package.resolvedPath);
        }

        /// <summary>
        /// Dấu của mã nguồn hub: SHA-256 của (đường dẫn tương đối + nội dung) mọi file <c>.cs</c> và <c>.uss</c> dưới
        /// <c>Editor/Hub</c>, sắp theo đường dẫn.
        /// <para>
        /// Vì sao băm CÂY NGUỒN chứ không ghi sha commit: bước đo chạy trên cây LÀM VIỆC, thường còn chưa commit. Một sha
        /// commit vì thế nói về một cây khác với cây vừa đo, và bằng chứng "đúng commit" vẫn có thể là bằng chứng của mã cũ.
        /// Băm cây thì chỉ cần một dòng USS đổi là dấu lệch — đúng câu hỏi cần hỏi.
        /// </para>
        /// </summary>
        /// <returns>Hex 64 ký tự, hoặc chuỗi rỗng khi không hỏi được gốc package.</returns>
        internal static string ComputeSourceDigest()
        {
            string packageDirectory = PackageDirectory();
            if (string.IsNullOrEmpty(packageDirectory)) return string.Empty;
            string hubDirectory = Path.Combine(packageDirectory, HubSourceRelativeDirectory);
            if (!Directory.Exists(hubDirectory)) return string.Empty;

            List<string> paths = new List<string>();
            paths.AddRange(Directory.GetFiles(hubDirectory, "*.cs", SearchOption.AllDirectories));
            paths.AddRange(Directory.GetFiles(hubDirectory, "*.uss", SearchOption.AllDirectories));
            paths.Sort(StringComparer.Ordinal);

            StringBuilder seed = new StringBuilder();
            for (int index = 0; index < paths.Count; index++)
            {
                seed.Append(paths[index].Substring(packageDirectory.Length).Replace('\\', '/'));
                seed.Append('\n');
                seed.Append(File.ReadAllText(paths[index]));
                seed.Append('\n');
            }

            return LiveEventCalendarSha256.ComputeHex(new UTF8Encoding(false).GetBytes(seed.ToString()));
        }

        internal static void Write(string path, UxContrastEvidenceData data)
        {
            File.WriteAllText(path, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
        }

        /// <summary>Đọc bằng chứng; null khi file không có hoặc JSON hỏng.</summary>
        internal static UxContrastEvidenceData Read(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                return JsonUtility.FromJson<UxContrastEvidenceData>(File.ReadAllText(path));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Nội dung file bằng chứng. Trường công khai vì <c>JsonUtility</c> chỉ đọc/ghi trường — đây là khuôn dữ liệu, không
    /// phải một đối tượng có hành vi.
    /// </summary>
    [Serializable]
    internal sealed class UxContrastEvidenceData
    {
        public int schema;
        public string unityVersion;
        public string skin;
        public bool proSkin;
        public string sourceDigest;
        public int measuredCount;
        public int sceneCount;
        public float minimumRatio;
        public string[] failures;
    }
}
