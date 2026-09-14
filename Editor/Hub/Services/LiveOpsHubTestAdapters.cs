using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Unity;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    // Adapter cho test, kịch bản chụp ảnh và lắp nhanh — nằm trong assembly Editor (không phải assembly test) theo luật adapter
    // Manual/InMemory của package, để công cụ của dự án khác cũng dựng được hub không cần cửa sổ hệ thống. Adapter của port
    // internal thì internal theo (V-16).

    public sealed class InMemoryLiveOpsHubClipboard : ILiveOpsHubClipboard
    {
        private string _text = string.Empty;

        public InMemoryLiveOpsHubClipboard(string initialText = "")
        {
            _text = initialText ?? string.Empty;
        }

        public string Text
        {
            get => _text;
            set => _text = value ?? string.Empty;
        }
    }

    /// <summary>Trả đường dẫn đặt trước (thay hộp lưu file của hệ thống) và ghi lại từng lần gọi để test assert tiêu đề, tên mặc định.</summary>
    public sealed class ManualLiveOpsHubFileDialog : ILiveOpsHubFileDialog
    {
        private readonly List<SaveFileCall> _saveFileCalls = new List<SaveFileCall>();
        private readonly List<string> _revealedPaths = new List<string>();

        /// <param name="pathToReturn">"" = giả lập người dùng bấm Huỷ.</param>
        public ManualLiveOpsHubFileDialog(string pathToReturn = "")
        {
            PathToReturn = pathToReturn ?? string.Empty;
        }

        public string PathToReturn { get; set; }

        public IReadOnlyList<SaveFileCall> SaveFileCalls => _saveFileCalls.ToArray();
        public IReadOnlyList<string> RevealedPaths => _revealedPaths.ToArray();

        public string SaveFile(string title, string directory, string defaultName, string extension)
        {
            _saveFileCalls.Add(new SaveFileCall(title, directory, defaultName, extension));
            return PathToReturn ?? string.Empty;
        }

        public void Reveal(string path)
        {
            _revealedPaths.Add(path ?? string.Empty);
        }

        public sealed class SaveFileCall
        {
            public SaveFileCall(string title, string directory, string defaultName, string extension)
            {
                Title = title ?? string.Empty;
                Directory = directory ?? string.Empty;
                DefaultName = defaultName ?? string.Empty;
                Extension = extension ?? string.Empty;
            }

            public string Title { get; }
            public string Directory { get; }
            public string DefaultName { get; }
            public string Extension { get; }
        }
    }

    /// <summary>
    /// Trả kết quả theo hàng đợi đã xếp và ghi lại mọi request (test đọc câu hộp). Hết hàng đợi thì trả
    /// <see cref="LiveOpsConfirmResult.Safe"/> — đúng thứ hộp thật trả khi người dùng Esc/đóng, nên test quên xếp kết quả
    /// không bao giờ vô tình áp thay đổi phá huỷ.
    /// </summary>
    internal sealed class ScriptedLiveOpsHubConfirmationPresenter : ILiveOpsHubConfirmationPresenter
    {
        private readonly Queue<LiveOpsConfirmResult> _results = new Queue<LiveOpsConfirmResult>();
        private readonly List<LiveOpsConfirmRequest> _requests = new List<LiveOpsConfirmRequest>();

        public ScriptedLiveOpsHubConfirmationPresenter(params LiveOpsConfirmResult[] results)
        {
            if (results == null) return;
            for (int index = 0; index < results.Length; index++) _results.Enqueue(results[index]);
        }

        public IReadOnlyList<LiveOpsConfirmRequest> Requests => _requests.ToArray();
        public int PendingResultCount => _results.Count;

        public void Enqueue(LiveOpsConfirmResult result)
        {
            _results.Enqueue(result);
        }

        public LiveOpsConfirmResult Confirm(LiveOpsConfirmRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            _requests.Add(request);
            return _results.Count > 0 ? _results.Dequeue() : LiveOpsConfirmResult.Safe;
        }
    }

    public sealed class ManualLiveOpsHubPublisherIdentity : ILiveOpsHubPublisherIdentity
    {
        public ManualLiveOpsHubPublisherIdentity(string publisherName, string sourceLabel)
        {
            PublisherName = publisherName ?? string.Empty;
            SourceLabel = sourceLabel ?? string.Empty;
        }

        public string PublisherName { get; }
        public string SourceLabel { get; }
    }

    /// <summary>Độ lệch cố định (mẫu thiết kế dùng +7) để chữ "giờ máy" trong test và ảnh chụp không phụ thuộc máy chạy.</summary>
    public sealed class ManualLiveOpsHubTimeZone : ILiveOpsHubTimeZone
    {
        public ManualLiveOpsHubTimeZone(TimeSpan offset)
        {
            Offset = offset;
        }

        public TimeSpan Offset { get; set; }

        public TimeSpan DeviceOffsetAt(DateTime utc) => Offset;
    }

    public sealed class ManualLiveOpsHubCompilationState : ILiveOpsHubCompilationState
    {
        public ManualLiveOpsHubCompilationState(bool isCompiling = false)
        {
            IsCompiling = isCompiling;
        }

        public bool IsCompiling { get; set; }
    }

    /// <summary>
    /// (V-16) Viết lại chuỗi TRƯỚC khi đưa parser thật đọc — vd thay cả chuỗi bằng "{" để dựng (h) "parser không đọc được",
    /// hoặc sửa "2026-10-3" thành giờ đúng để parser giữ 7/8 ("parser lệch"). Parser thật vẫn chạy nên kịch bản không nói dối
    /// về đường game.
    /// </summary>
    internal sealed class RewritingLiveOpsHubJsonReadBack : ILiveOpsHubJsonReadBack
    {
        private readonly Func<string, string> _rewriteBeforeRead;

        public RewritingLiveOpsHubJsonReadBack(Func<string, string> rewriteBeforeRead)
        {
            _rewriteBeforeRead = rewriteBeforeRead ?? throw new ArgumentNullException(nameof(rewriteBeforeRead));
        }

        /// <summary>Chuỗi parser đã thật sự đọc ở lần gọi gần nhất — test kiểm seam không bỏ qua bước viết lại.</summary>
        public string LastRewrittenText { get; private set; } = string.Empty;

        public LiveEventCalendarParseResult ReadBack(string json)
        {
            string rewritten = _rewriteBeforeRead(json);
            LastRewrittenText = rewritten ?? string.Empty;
            return JsonLiveEventCalendarParser.Parse(rewritten);
        }

        public LiveEventCalendarDocumentParseResult ReadBackDocument(string json)
        {
            // Cùng bước viết lại với ReadBack: kịch bản "parser không đọc được" phải hỏng ở cả hai đường, không thì luồng Dán
            // thấy JSON đọc được trong khi bước kiểm xuất báo hỏng.
            string rewritten = _rewriteBeforeRead(json);
            LastRewrittenText = rewritten ?? string.Empty;
            return JsonLiveEventCalendarParser.ParseDocument(rewritten);
        }
    }

    /// <summary>
    /// (V-16) Trả null cho đúng các đường dẫn chỉ định (giả lập UXML/USS bị xoá khỏi package — kịch bản h28b), còn lại nạp qua
    /// loader bên trong (mặc định AssetDatabase) để phần khác của hub vẫn dựng thật.
    /// </summary>
    internal sealed class MissingPathsLiveOpsHubLayoutLoader : ILiveOpsHubLayoutLoader
    {
        private readonly HashSet<string> _missingPaths;
        private readonly ILiveOpsHubLayoutLoader _innerLoader;

        public MissingPathsLiveOpsHubLayoutLoader(IReadOnlyCollection<string> missingPaths)
            : this(missingPaths, new AssetDatabaseLiveOpsHubLayoutLoader())
        {
        }

        /// <param name="innerLoader">Loader cho đường dẫn không bị giả mất — test truyền loader giả để không cần asset thật trên đĩa.</param>
        public MissingPathsLiveOpsHubLayoutLoader(IReadOnlyCollection<string> missingPaths, ILiveOpsHubLayoutLoader innerLoader)
        {
            _missingPaths = new HashSet<string>(StringComparer.Ordinal);
            if (missingPaths != null)
            {
                foreach (string path in missingPaths)
                {
                    if (path != null) _missingPaths.Add(path);
                }
            }
            _innerLoader = innerLoader ?? throw new ArgumentNullException(nameof(innerLoader));
        }

        public VisualTreeAsset LoadVisualTree(string assetPath)
        {
            return IsMissing(assetPath) ? null : _innerLoader.LoadVisualTree(assetPath);
        }

        public StyleSheet LoadStyleSheet(string assetPath)
        {
            return IsMissing(assetPath) ? null : _innerLoader.LoadStyleSheet(assetPath);
        }

        private bool IsMissing(string assetPath)
        {
            return assetPath != null && _missingPaths.Contains(assetPath);
        }
    }
}
