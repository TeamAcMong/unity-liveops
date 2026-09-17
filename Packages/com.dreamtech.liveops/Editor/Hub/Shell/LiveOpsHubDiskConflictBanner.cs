using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Băng "asset đổi từ bên ngoài" (Hình 28 khung 4, 4.3, SPIKE-B SP-8b): HelpBox cảnh báo nêu SỐ MỤC khác nhau và HẬU QUẢ
    /// của cả hai lựa chọn, kèm ba nút Tải lại · Xem khác biệt · Giữ bản trong Editor (nút chính = nút an toàn).
    /// <para>
    /// Hub KHÔNG BAO GIỜ tự đè nháp: ba nút là ba quyết định của người dùng, không nút nào chạy ngầm. Vì thế băng phải nói
    /// đủ ba câu — đổi bao nhiêu mục, "Tải lại" mất gì, "Giữ bản trong Editor" sẽ ghi đè gì — chứ không chỉ "file đã đổi".
    /// </para>
    /// <para>
    /// Tách khỏi <see cref="LiveOpsHubWindow"/> vì đây là view thuần: nó dựng chữ từ <see cref="LiveOpsHubDiskConflict"/> và
    /// gọi lại ba callback, không biết phiên hay điều hướng. <see cref="BuildSentences"/> để test đọc thẳng câu mà không cần
    /// mở cửa sổ.
    /// </para>
    /// </summary>
    internal sealed class LiveOpsHubDiskConflictBanner
    {
        internal const string ElementName = "hub-disk-banner";
        internal const string TextElementName = "hub-disk-banner-text";
        internal const string ReloadButtonName = "hub-disk-banner-reload";
        internal const string DiffButtonName = "hub-disk-banner-diff";
        internal const string KeepButtonName = "hub-disk-banner-keep";

        private const string WarningIconName = "console.warnicon.sml";
        private const int WarningIconSize = 16;

        // Tên field JSON của giờ đợt — mục chỉ đổi giờ thì câu đọc là "Dời …" đúng như Hình 28 khung 4, còn lại là "Đổi …".
        private const string StartUtcFieldName = "startUtc";
        private const string EndUtcFieldName = "endUtc";

        private LiveOpsHubDiskConflictBanner(VisualElement element, Label text)
        {
            Element = element;
            TextLabel = text;
        }

        internal VisualElement Element { get; }

        internal Label TextLabel { get; }

        /// <param name="assetFileName">Tên file asset ("Main.asset") — câu luôn gọi tên file, không bao giờ "asset của bạn".</param>
        /// <param name="reload">"Tải lại" — lấy bản trên đĩa, mất thay đổi chưa lưu.</param>
        /// <param name="viewDifferences">"Xem khác biệt" — mở pane So với, nguồn là bản trên đĩa.</param>
        /// <param name="keepEditorVersion">"Giữ bản trong Editor" — nút AN TOÀN, và là nút chính.</param>
        internal static LiveOpsHubDiskConflictBanner Create(LiveOpsHubDiskConflict conflict, string assetFileName,
            LiveOpsHubFormat format, Action reload, Action viewDifferences, Action keepEditorVersion)
        {
            if (conflict == null) throw new ArgumentNullException(nameof(conflict));
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (reload == null) throw new ArgumentNullException(nameof(reload));
            if (viewDifferences == null) throw new ArgumentNullException(nameof(viewDifferences));
            if (keepEditorVersion == null) throw new ArgumentNullException(nameof(keepEditorVersion));

            VisualElement element = new VisualElement { name = ElementName };
            element.AddToClassList(LiveOpsHubClassNames.Note);
            element.AddToClassList(LiveOpsHubClassNames.DiskBanner);

            VisualElement head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.AddToClassList(LiveOpsHubClassNames.DiskBannerHead);
            head.Add(LiveOpsHubIcons.CreateImage(WarningIconName, WarningIconSize));

            Label text = new Label(BuildSentences(conflict, assetFileName, format)) { name = TextElementName };
            text.AddToClassList(LiveOpsHubClassNames.DiskBannerText);
            head.Add(text);
            element.Add(head);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(LiveOpsHubClassNames.DiskBannerActions);

            Button reloadButton = new Button(reload) { name = ReloadButtonName, text = LiveOpsHubStrings.ShellDiskBannerReloadButton };
            reloadButton.AddToClassList(LiveOpsHubClassNames.Button);
            // Q-W5-2: đi qua KHOÁ chứ không qua câu — dấu số ít/số nhiều phải chọn vế TRƯỚC string.Format, mà LiveOpsHubStrings.<Khoá>
            // đã bỏ dấu (StripToPlural) nên chỗ gọi cũ đọc "1 unsaved changes are lost".
            reloadButton.tooltip = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ShellDiskBannerReloadTooltipFormat),
                format.Integer(conflict.LostIfReload.ChangeCount));
            actions.Add(reloadButton);

            Button diffButton = new Button(viewDifferences) { name = DiffButtonName, text = LiveOpsHubStrings.ShellDiskBannerDiffButton };
            diffButton.AddToClassList(LiveOpsHubClassNames.Button);
            actions.Add(diffButton);

            // Nút chính = nút AN TOÀN (Hình 28 khung 4): hub không tự đè, nên hành động được mời sẵn là hành động không mất gì.
            Button keepButton = new Button(keepEditorVersion) { name = KeepButtonName, text = LiveOpsHubStrings.ShellDiskBannerKeepButton };
            keepButton.AddToClassList(LiveOpsHubClassNames.Button);
            keepButton.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            actions.Add(keepButton);

            element.Add(actions);
            return new LiveOpsHubDiskConflictBanner(element, text);
        }

        /// <summary>Ba câu của Hình 28 khung 4 nối liền: đổi bao nhiêu mục · Tải lại mất gì · Giữ bản trong Editor ghi đè gì.</summary>
        internal static string BuildSentences(LiveOpsHubDiskConflict conflict, string assetFileName, LiveOpsHubFormat format)
        {
            if (conflict == null) throw new ArgumentNullException(nameof(conflict));
            if (format == null) throw new ArgumentNullException(nameof(format));
            string fileName = assetFileName ?? string.Empty;
            string changedItems = ItemListOf(conflict.DiskVersusEditor);
            string changedCount = format.Integer(conflict.DiskVersusEditor.ChangeCount);
            string head = changedItems.Length == 0
                ? LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ShellDiskBannerHeadWithoutItemsFormat),
                    fileName, format.ShortDateTimeUtc(conflict.DetectedUtc), changedCount)
                : LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ShellDiskBannerHeadFormat),
                    fileName, format.ShortDateTimeUtc(conflict.DetectedUtc), changedCount, changedItems);

            string lostItems = ChangeListOf(conflict.LostIfReload);
            string lostCount = format.Integer(conflict.LostIfReload.ChangeCount);
            // Q-W5-2: đưa KHOÁ cho catalog (xem tooltip nút Tải lại ở trên) — câu tiếng Anh của hai khoá này mang dấu số ít/số nhiều.
            string reloadLine = lostItems.Length == 0
                ? LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ShellDiskBannerReloadLineWithoutItemsFormat), lostCount)
                : LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ShellDiskBannerReloadLineFormat), lostCount, lostItems);

            string keepLine = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ShellDiskBannerKeepLineFormat), changedCount);
            return head + LiveOpsHubStrings.ShellDiskBannerSentenceSeparator + reloadLine
                   + LiveOpsHubStrings.ShellDiskBannerSentenceSeparator + keepLine;
        }

        /// <summary>
        /// Tên các mục khác nhau, mỗi tên một lần; "" khi diff không nêu mục nào (vd chỉ khác key remote). Luật lặp mang tiền
        /// tố loại ("luật weekly-pass") và danh sách được SẮP theo tên, đúng chữ và đúng thứ tự của Hình 28 khung 4
        /// ("hunt-0914, lava-quest-2026-10, luật weekly-pass") — thứ tự duyệt diff phụ thuộc thứ tự mục trong tài liệu nên
        /// cùng một xung đột có thể đọc ra hai câu khác nhau.
        /// </summary>
        internal static string ItemListOf(LiveEventCalendarDiffResult diff)
        {
            return JoinSorted(diff, DisplayNameOf);
        }

        /// <summary>
        /// Như trên nhưng mỗi mục có ĐỘNG TỪ ("Dời lava-quest-2026-09b") — câu "Tải lại: mất 1 thay đổi chưa lưu (…)" của
        /// Hình 28 khung 4 nói thay đổi đó LÀ GÌ, id trần bắt người dùng tự đoán mình đã làm gì với nó.
        /// </summary>
        internal static string ChangeListOf(LiveEventCalendarDiffResult diff)
        {
            return JoinSorted(diff, ChangeTextOf);
        }

        private static string JoinSorted(LiveEventCalendarDiffResult diff, Func<LiveEventCalendarChange, string> textOf)
        {
            if (diff == null) return string.Empty;
            List<string> names = new List<string>();
            foreach (LiveEventCalendarChange change in diff.Changes)
            {
                if (change.Kind == LiveEventCalendarChangeKind.Kept) continue;
                string text = textOf(change);
                if (names.Contains(text)) continue;
                names.Add(text);
            }
            names.Sort(StringComparer.Ordinal);
            return string.Join(LiveOpsHubStrings.ShellDiskBannerItemSeparator, names.ToArray());
        }

        private static string DisplayNameOf(LiveEventCalendarChange change)
        {
            if (change.ItemKind != LiveEventCalendarItemKind.RecurringRule) return change.ItemId;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellDiskBannerRuleItemFormat, change.ItemId);
        }

        private static string ChangeTextOf(LiveEventCalendarChange change)
        {
            string format;
            if (change.Kind == LiveEventCalendarChangeKind.Added) format = LiveOpsHubStrings.ShellDiskBannerChangeAddedFormat;
            else if (change.Kind == LiveEventCalendarChangeKind.Removed) format = LiveOpsHubStrings.ShellDiskBannerChangeRemovedFormat;
            else if (HasTimeFieldChange(change)) format = LiveOpsHubStrings.ShellDiskBannerChangeMovedFormat;
            else format = LiveOpsHubStrings.ShellDiskBannerChangeEditedFormat;
            return string.Format(CultureInfo.InvariantCulture, format, DisplayNameOf(change));
        }

        private static bool HasTimeFieldChange(LiveEventCalendarChange change)
        {
            foreach (LiveEventCalendarFieldChange field in change.Fields)
            {
                if (string.Equals(field.FieldName, StartUtcFieldName, StringComparison.Ordinal)) return true;
                if (string.Equals(field.FieldName, EndUtcFieldName, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
