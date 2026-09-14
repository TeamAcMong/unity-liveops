using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    // Adapter mặc định của hub: đường thật của Editor. Mỗi adapter mỏng tới mức không cần test riêng — logic nằm ở model thuần,
    // adapter chỉ chuyển lời gọi sang API Unity/hệ điều hành.

    public sealed class EditorLiveOpsHubClipboard : ILiveOpsHubClipboard
    {
        public string Text
        {
            get => EditorGUIUtility.systemCopyBuffer ?? string.Empty;
            set => EditorGUIUtility.systemCopyBuffer = value ?? string.Empty;
        }
    }

    public sealed class EditorLiveOpsHubFileDialog : ILiveOpsHubFileDialog
    {
        public string SaveFile(string title, string directory, string defaultName, string extension)
        {
            return EditorUtility.SaveFilePanel(title, directory, defaultName, extension) ?? string.Empty;
        }

        public void Reveal(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            EditorUtility.RevealInFinder(path);
        }
    }

    /// <summary>
    /// Tên người đăng = <c>git config user.name</c> (khớp tên trong lịch sử commit của team), hạn <see cref="GitTimeoutMilliseconds"/>;
    /// git không có, treo hay trả rỗng thì rơi về tên tài khoản máy. Đọc một lần rồi giữ: hộp Đánh dấu đã đăng mở nhiều lần
    /// trong phiên không được mỗi lần treo tới 2 giây.
    /// </summary>
    public sealed class GitLiveOpsHubPublisherIdentity : ILiveOpsHubPublisherIdentity
    {
        public const int GitTimeoutMilliseconds = 2000;

        private bool _resolved;
        private string _publisherName = string.Empty;
        private string _sourceLabel = string.Empty;

        public string PublisherName
        {
            get
            {
                Resolve();
                return _publisherName;
            }
        }

        public string SourceLabel
        {
            get
            {
                Resolve();
                return _sourceLabel;
            }
        }

        private void Resolve()
        {
            if (_resolved) return;
            _resolved = true;
            string gitName = ReadGitUserName();
            if (gitName.Length > 0)
            {
                _publisherName = gitName;
                _sourceLabel = LiveOpsHubStrings.KitPublisherSourceGit;
                return;
            }
            _publisherName = Environment.UserName ?? string.Empty;
            _sourceLabel = LiveOpsHubStrings.KitPublisherSourceAccount;
        }

        private static string ReadGitUserName()
        {
            var startInfo = new ProcessStartInfo("git", "config user.name")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                // Thư mục làm việc của Editor là gốc project: đọc được cả user.name riêng của repo game, không chỉ bản toàn máy.
                WorkingDirectory = Directory.GetCurrentDirectory(),
            };
            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null) return string.Empty;
                    // Đọc stdout bất đồng bộ trước khi chờ: đọc đồng bộ sau WaitForExit có thể kẹt khi bộ đệm ống đầy.
                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    if (!process.WaitForExit(GitTimeoutMilliseconds))
                    {
                        TryKill(process);
                        return string.Empty;
                    }
                    if (process.ExitCode != 0 || !outputTask.Wait(GitTimeoutMilliseconds)) return string.Empty;
                    return (outputTask.Result ?? string.Empty).Trim();
                }
            }
            // Máy không cài git (Win32Exception), thư mục làm việc lạ, hay ống bị đóng — đều rơi về tên tài khoản máy.
            catch (Win32Exception)
            {
                return string.Empty;
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (AggregateException)
            {
                return string.Empty;
            }
        }

        private static void TryKill(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (InvalidOperationException)
            {
                // Tiến trình vừa tự thoát giữa lúc hết hạn và lúc kill — không còn gì để dọn.
            }
            catch (Win32Exception)
            {
                // Không kill được thì bỏ: tên đã rơi về tài khoản máy, git treo không giữ Editor lại.
            }
        }
    }

    public sealed class DeviceLiveOpsHubTimeZone : ILiveOpsHubTimeZone
    {
        public TimeSpan DeviceOffsetAt(DateTime utc)
        {
            return TimeZoneInfo.Local.GetUtcOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
        }
    }

    public sealed class EditorLiveOpsHubCompilationState : ILiveOpsHubCompilationState
    {
        public bool IsCompiling => EditorApplication.isCompiling;
    }

    /// <summary>(V-16) Mặc định: parser của game đọc lại đúng chuỗi sẽ đăng.</summary>
    internal sealed class GameParserLiveOpsHubJsonReadBack : ILiveOpsHubJsonReadBack
    {
        public LiveEventCalendarParseResult ReadBack(string json)
        {
            return JsonLiveEventCalendarParser.Parse(json);
        }

        public LiveEventCalendarDocumentParseResult ReadBackDocument(string json)
        {
            return JsonLiveEventCalendarParser.ParseDocument(json);
        }
    }

    /// <summary>(V-16) Mặc định: nạp UXML/USS từ AssetDatabase theo đường dẫn của <c>LiveOpsHubPaths</c>.</summary>
    internal sealed class AssetDatabaseLiveOpsHubLayoutLoader : ILiveOpsHubLayoutLoader
    {
        public VisualTreeAsset LoadVisualTree(string assetPath)
        {
            return string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
        }

        public StyleSheet LoadStyleSheet(string assetPath)
        {
            return string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
        }
    }
}
