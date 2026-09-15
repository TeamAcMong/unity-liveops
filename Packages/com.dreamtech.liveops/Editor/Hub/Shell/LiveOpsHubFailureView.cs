using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// "Lỗi của hub không bao giờ hiện thành cửa sổ trắng" ([FD §4.1] Hình 28): mỗi khung nói triệu chứng bằng chữ thường,
    /// nguyên nhân bằng chữ mono và cho một hành động. Khung 1 (màn ném khi dựng) thay thân của MỘT màn — rail vẫn mở màn khác
    /// được. Khung 2 (thiếu UXML hoặc element sống còn) dựng bằng C# thay cho cả cửa sổ vì không còn bố cục để đặt vào.
    /// </summary>
    internal static class LiveOpsHubFailureView
    {
        internal const string CardElementName = "hub-failure";
        internal const string TitleElementName = "hub-failure-title";
        internal const string BodyElementName = "hub-failure-body";
        internal const string CopyButtonName = "hub-failure-copy";
        internal const string RetryButtonName = "hub-failure-retry";
        internal const string RevealButtonName = "hub-failure-reveal";

        /// <summary>Icon lỗi 16 px đầu card — tên có trong <see cref="LiveOpsHubIcons.AllDesignNames"/> (probe kiểm nạp được).</summary>
        internal const string ErrorIconName = "console.erroricon.sml";

        /// <summary>
        /// Card lỗi cho màn <paramref name="section"/> đặt vào thân màn. "Copy lỗi" chép đủ loại + thông điệp + stack; "Thử dựng
        /// lại" gọi <paramref name="retry"/> (cửa sổ dựng lại chính màn đó).
        /// </summary>
        public static VisualElement ForSection(IHubSection section, Exception exception, Action retry, ILiveOpsHubClipboard clipboard)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            if (clipboard == null) throw new ArgumentNullException(nameof(clipboard));

            string errorText = DescribeException(exception);
            VisualElement card = CreateCard(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellSectionFailureTitleFormat, section.Title));

            Label body = new Label(errorText) { name = BodyElementName, enableRichText = false };
            body.AddToClassList(LiveOpsHubClassNames.FailureBody);
            body.AddToClassList(LiveOpsHubClassNames.Mono);
            card.Add(body);

            VisualElement actions = CreateActions();
            Button copyButton = CreateButton(CopyButtonName, LiveOpsHubStrings.ShellCopyErrorButton, () => clipboard.Text = errorText, true);
            actions.Add(copyButton);
            if (retry != null) actions.Add(CreateButton(RetryButtonName, LiveOpsHubStrings.ShellRetryBuildButton, retry, false));
            card.Add(actions);

            Label footnote = new Label(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellSectionFailureFootnoteFormat, section.Title,
                LiveOpsHubStrings.ShellSectionFailedHealthReason));
            footnote.AddToClassList(LiveOpsHubClassNames.FailureFootnote);
            card.Add(footnote);
            return card;
        }

        /// <summary>
        /// Khung 2: không nạp được <paramref name="missingPath"/>. Xoá cả cửa sổ và dựng root hub tối thiểu (header chỉ còn "LiveOps
        /// Hub" + card lỗi). Stylesheet nạp qua cùng <paramref name="layoutLoader"/> nếu còn — thiếu cả sheet thì chữ vẫn hiện
        /// không style, không bao giờ trắng.
        /// </summary>
        public static VisualElement ShowMissingLayout(VisualElement windowRoot, string missingPath, ILiveOpsHubLayoutLoader layoutLoader,
            ILiveOpsHubClipboard clipboard, ILiveOpsHubFileDialog fileDialog)
        {
            string title = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellMissingLayoutTitleFormat, Path.GetFileName(missingPath));
            string body = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellMissingLayoutBodyFormat, missingPath);
            return ShowWindowFailure(windowRoot, title, body, missingPath, layoutLoader, clipboard, fileDialog);
        }

        /// <summary>UXML nạp được nhưng thiếu element sống còn (UXML sửa tay lệch <see cref="LiveOpsHubPaths.RequiredShellElementNames"/>).</summary>
        public static VisualElement ShowMissingElements(VisualElement windowRoot, IReadOnlyList<string> missingNames, ILiveOpsHubLayoutLoader layoutLoader,
            ILiveOpsHubClipboard clipboard, ILiveOpsHubFileDialog fileDialog)
        {
            string body = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellMissingElementsBodyFormat,
                string.Join(", ", missingNames ?? Array.Empty<string>()), LiveOpsHubPaths.ShellUxml);
            return ShowWindowFailure(windowRoot, LiveOpsHubStrings.ShellMissingElementsTitle, body, LiveOpsHubPaths.ShellUxml, layoutLoader, clipboard,
                fileDialog);
        }

        /// <summary>Loại + thông điệp + stack (kể cả inner exception) — thứ người dùng dán vào báo lỗi.</summary>
        internal static string DescribeException(Exception exception)
        {
            StringBuilder builder = new StringBuilder();
            Exception current = exception;
            while (current != null)
            {
                if (builder.Length > 0) builder.Append('\n');
                builder.Append(current.GetType().Name).Append(": ").Append(current.Message);
                if (!string.IsNullOrEmpty(current.StackTrace)) builder.Append('\n').Append(current.StackTrace.TrimEnd());
                current = current.InnerException;
            }
            return builder.ToString();
        }

        /// <summary>
        /// Chỗ mở trong Finder/Explorer cho nút "Mở thư mục package": file nếu còn trên đĩa, không thì thư mục gần nhất còn tồn tại
        /// chứa nó (thường là <c>Editor/Hub/UI</c> — đúng chỗ file phải nằm). Vì sao không đưa thẳng đường dẫn dự án: card thiếu
        /// UXML hiện ĐÚNG lúc file không có, và đường ảo <c>Packages/…</c> không có trên đĩa khi package cài bằng <c>file:</c> hay
        /// git — reveal đường đó thì nút không mở gì.
        /// </summary>
        internal static string ResolveRevealPath(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath)) return string.Empty;
            string physicalPath = FileUtil.GetPhysicalPath(projectPath);
            string candidate = Path.GetFullPath(string.IsNullOrEmpty(physicalPath) ? projectPath : physicalPath);
            if (File.Exists(candidate)) return candidate;
            while (!string.IsNullOrEmpty(candidate) && !Directory.Exists(candidate))
            {
                candidate = Path.GetDirectoryName(candidate);
            }
            return candidate ?? string.Empty;
        }

        private static VisualElement ShowWindowFailure(VisualElement windowRoot, string title, string body, string pathToReveal,
            ILiveOpsHubLayoutLoader layoutLoader, ILiveOpsHubClipboard clipboard, ILiveOpsHubFileDialog fileDialog)
        {
            if (windowRoot == null) throw new ArgumentNullException(nameof(windowRoot));
            if (clipboard == null) throw new ArgumentNullException(nameof(clipboard));
            if (fileDialog == null) throw new ArgumentNullException(nameof(fileDialog));

            windowRoot.Clear();
            VisualElement hubRoot = new VisualElement { name = LiveOpsHubPaths.ShellElementNames.Root };
            hubRoot.AddToClassList(LiveOpsHubClassNames.Root);
            hubRoot.EnableInClassList(LiveOpsHubClassNames.SkinLight, !EditorGUIUtility.isProSkin);
            if (layoutLoader != null)
            {
                foreach (LiveOpsHubPaths.ShellStyleSheet sheet in LiveOpsHubPaths.ShellStyleSheetLoadOrder)
                {
                    // Nạp mọi sheet còn có (không lọc theo IsRequired): card lỗi chỉ cần token + khung, sheet thiếu thì bỏ qua im
                    // lặng — cửa sổ đang ở màn lỗi, thêm cảnh báo stylesheet chỉ che mất lỗi thật.
                    StyleSheet loaded = layoutLoader.LoadStyleSheet(sheet.Path);
                    if (loaded != null) hubRoot.styleSheets.Add(loaded);
                }
            }

            VisualElement header = new VisualElement { name = LiveOpsHubPaths.ShellElementNames.Header };
            header.AddToClassList(LiveOpsHubClassNames.Header);
            Label headerTitle = new Label(LiveOpsHubStrings.ShellWindowTitle) { name = LiveOpsHubPaths.ShellElementNames.HeaderTitle };
            headerTitle.AddToClassList(LiveOpsHubClassNames.HeaderTitle);
            header.Add(headerTitle);
            hubRoot.Add(header);

            VisualElement card = CreateCard(title);
            card.AddToClassList(LiveOpsHubClassNames.FailureWindow);
            Label bodyLabel = new Label(body) { name = BodyElementName, enableRichText = false };
            bodyLabel.AddToClassList(LiveOpsHubClassNames.FailureBody);
            card.Add(bodyLabel);

            string errorText = title + "\n" + body;
            VisualElement actions = CreateActions();
            actions.Add(CreateButton(RevealButtonName, LiveOpsHubStrings.ShellOpenPackageFolderButton, () => fileDialog.Reveal(ResolveRevealPath(pathToReveal)), true));
            actions.Add(CreateButton(CopyButtonName, LiveOpsHubStrings.ShellCopyErrorButton, () => clipboard.Text = errorText, false));
            card.Add(actions);
            hubRoot.Add(card);
            windowRoot.Add(hubRoot);
            return card;
        }

        private static VisualElement CreateCard(string title)
        {
            VisualElement card = new VisualElement { name = CardElementName };
            card.AddToClassList(LiveOpsHubClassNames.Failure);
            VisualElement head = new VisualElement();
            head.AddToClassList(LiveOpsHubClassNames.FailureHead);
            head.Add(LiveOpsHubIcons.CreateImage(ErrorIconName, 16));
            Label titleLabel = new Label(title) { name = TitleElementName, enableRichText = false };
            titleLabel.AddToClassList(LiveOpsHubClassNames.FailureTitle);
            titleLabel.AddToClassList(LiveOpsHubClassNames.TextBlocked);
            head.Add(titleLabel);
            card.Add(head);
            return card;
        }

        private static VisualElement CreateActions()
        {
            VisualElement actions = new VisualElement();
            actions.AddToClassList(LiveOpsHubClassNames.FailureActions);
            return actions;
        }

        private static Button CreateButton(string name, string text, Action clicked, bool isFirst)
        {
            Button button = new Button(clicked) { name = name, text = text };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            button.EnableInClassList(LiveOpsHubClassNames.ButtonFirst, isFirst);
            return button;
        }
    }
}
