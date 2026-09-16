using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Pane "So với đã đăng" của màn Lịch [SD1 §3.4]: THAY CHỖ inspector đợt (hai pane loại trừ nhau, cùng rộng 280), nhóm theo
    /// cùng enum hậu quả với Kiểm lịch và Xuất JSON, hàng 22px có ký hiệu <c>+ ~</c>.
    /// <para>
    /// (V-22 CC-FT-1) Câu từng hàng KHÔNG viết ở đây: pane dựng <see cref="LiveOpsChangeTextContext"/> từ hai tài liệu nó vừa so
    /// rồi nhờ <see cref="ExportDiffViewModel"/> gom nhóm — cùng một nguồn câu, cùng một thứ tự nhóm với card diff của màn Xuất
    /// JSON. Nhân đôi phần gom nhóm ở đây là cách chắc chắn nhất để hai màn nói hai câu khác nhau về cùng một thay đổi.
    /// </para>
    /// <para>
    /// (V-13) Nguồn bản so theo <c>Publish.ActiveCompareSource</c>: <c>Published</c> (mặc định) hoặc <c>Disk</c> khi tới từ băng
    /// "asset đã đổi trên đĩa". Nguồn Disk đổi tiêu đề, đổi note và đổi động từ của mục chuột phải — không có "Hoàn về bản đã
    /// đăng" vì bản đang so không phải bản đã đăng.
    /// </para>
    /// </summary>
    internal sealed class CalendarComparePane
    {
        private readonly LiveOpsHubServices _services;
        private readonly VisualElement _root;
        private readonly Label _title;
        private readonly Label _count;
        private readonly VisualElement _body;
        private readonly Label _note;
        private readonly List<CalendarCompareRow> _rows = new List<CalendarCompareRow>();

        private string _selectedEntryKey = string.Empty;

        public CalendarComparePane(LiveOpsHubServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            _root = new VisualElement { name = LiveOpsHubPaths.CalendarDepthElementNames.ComparePane };
            _root.AddToClassList(LiveOpsHubClassNames.CalendarDepthComparePane);
            _root.AddToClassList(LiveOpsHubClassNames.Inspector);

            VisualElement header = new VisualElement();
            header.AddToClassList(LiveOpsHubClassNames.CalendarInspectorTitle);
            _title = new Label { name = LiveOpsHubPaths.CalendarDepthElementNames.CompareTitle };
            _title.AddToClassList(LiveOpsHubClassNames.CalendarDepthCompareTitle);
            header.Add(_title);
            _count = new Label();
            _count.AddToClassList(LiveOpsHubClassNames.CalendarDepthCompareCount);
            header.Add(_count);
            _root.Add(header);

            _body = new ScrollView { name = LiveOpsHubPaths.CalendarDepthElementNames.CompareBody };
            _body.AddToClassList(LiveOpsHubClassNames.CalendarDepthCompareBody);
            _root.Add(_body);

            _note = new Label { name = LiveOpsHubPaths.CalendarDepthElementNames.CompareNote };
            _note.AddToClassList(LiveOpsHubClassNames.CalendarDepthCompareNote);
            _root.Add(_note);
        }

        /// <summary>Bấm một dòng: chọn đợt và căn khung [SD1 §3.4]. "" khi hàng không trỏ tới đợt cố định nào.</summary>
        public event Action<string> RowActivated;

        /// <summary>Chuột phải một dòng — màn dựng menu từ <see cref="CalendarContextMenus.ForCompareRow"/>.</summary>
        public event Action<string, ContextualMenuPopulateEvent> RowContextRequested;

        public VisualElement Root => _root;

        internal Label Title => _title;
        internal Label Note => _note;
        internal IReadOnlyList<CalendarCompareRow> Rows => _rows;

        /// <summary>Số thay đổi đang vẽ — nút toolbar "So với đã đăng (n)" đếm đúng con số này, không đếm một diff khác.</summary>
        public int ChangeCount => _rows.Count;

        /// <summary>Đợt đang chọn trên trục — hàng tương ứng sáng nền highlight [SD1 §3.4].</summary>
        public void SetSelectedEntryKey(string entryKey)
        {
            _selectedEntryKey = entryKey ?? string.Empty;
            for (int index = 0; index < _rows.Count; index++)
            {
                _rows[index].Element.EnableInClassList(LiveOpsHubClassNames.CalendarDepthCompareRowSelected,
                    _rows[index].EntryKey.Length > 0 && string.Equals(_rows[index].EntryKey, _selectedEntryKey, StringComparison.Ordinal));
            }
        }

        /// <summary>Dựng lại toàn bộ pane từ phiên; gọi mỗi lần tài liệu, bản so hoặc nguồn so đổi.</summary>
        public void Refresh()
        {
            _body.Clear();
            _rows.Clear();

            LiveOpsHubPublishState publish = _services.Session.Publish;
            LiveEventCalendarDocument draft = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            LiveEventCalendarDocument baseline = publish == null ? null : publish.CompareDocument;
            LiveOpsHubCompareSource source = publish == null ? LiveOpsHubCompareSource.Published : publish.ActiveCompareSource;

            _title.text = TitleTextOf(source, baseline);
            _note.text = source == LiveOpsHubCompareSource.Disk
                ? LiveOpsHubStrings.CalendarDepthCompareDiskNote
                : LiveOpsHubStrings.CalendarDepthCompareNote;

            if (baseline == null)
            {
                _count.text = string.Empty;
                _body.Add(new Label(LiveOpsHubStrings.CalendarDepthCompareEmpty));
                return;
            }

            LiveEventCalendarDiffResult diff = publish.CompareDiff;
            LiveOpsChangeTextContext context = new LiveOpsChangeTextContext(baseline, draft, _services.Clock.UtcNow, diff, false);
            ExportDiffViewModel model = ExportDiffViewModel.Build(diff, context, _services.Format, source, string.Empty,
                LiveOpsHubStrings.CalendarDepthCompareEmpty, null);

            if (model.IsEmpty)
            {
                _count.text = string.Empty;
                _body.Add(new Label(LiveOpsHubStrings.CalendarDepthCompareEmpty));
                return;
            }
            for (int groupIndex = 0; groupIndex < model.Groups.Count; groupIndex++) AddGroup(model.Groups[groupIndex], diff);
            _count.text = _services.Format.Integer(_rows.Count);
            SetSelectedEntryKey(_selectedEntryKey);
        }

        private string TitleTextOf(LiveOpsHubCompareSource source, LiveEventCalendarDocument baseline)
        {
            if (source == LiveOpsHubCompareSource.Disk)
            {
                LiveOpsHubDiskConflict conflict = _services.Session.DiskConflict;
                string changedAt = conflict == null ? string.Empty : _services.Format.ShortDateTime(conflict.DetectedUtc);
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthCompareDiskTitleFormat, changedAt);
            }
            PublishedCalendarStamp stamp = baseline == null ? null : _services.Session.Document.LatestStamp;
            string stampText = stamp == null || !LiveEventUtcText.TryParse(stamp.PublishedUtcText, out DateTime publishedUtc)
                ? string.Empty
                : _services.Format.ShortDateTime(publishedUtc);
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthComparePublishedTitleFormat, stampText);
        }

        private void AddGroup(ExportDiffGroup group, LiveEventCalendarDiffResult diff)
        {
            Label groupLabel = new Label(group.HeaderText);
            groupLabel.AddToClassList(LiveOpsHubClassNames.CalendarDepthCompareGroupLabel);
            _body.Add(groupLabel);
            for (int index = 0; index < group.Rows.Count; index++) AddRow(group.Rows[index]);
        }

        /// <summary>
        /// Một hàng 22px: dấu 7px · ký hiệu mono 10px · câu (họ text theo hậu quả). Hàng mang <c>EntryKey</c> của mục để bấm là
        /// chọn được đợt trên trục; thay đổi của LOẠI hoặc của LUẬT không có EntryKey nên bấm chỉ chọn hàng, không căn khung.
        /// </summary>
        private void AddRow(ExportDiffRow diffRow)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarDepthCompareRow);

            LiveOpsStateMark mark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            mark.SetHealth(LiveOpsHubFindingRouting.StateOf(diffRow.Change.Consequence));
            row.Add(mark);

            Label symbol = new Label(diffRow.SymbolText);
            symbol.AddToClassList(LiveOpsHubClassNames.CalendarDepthCompareSymbol);
            symbol.AddToClassList(LiveOpsHubClassNames.Mono);
            row.Add(symbol);

            Label text = new Label(diffRow.TitleText) { tooltip = diffRow.ConsequenceText };
            text.AddToClassList(LiveOpsHubClassNames.CalendarDepthCompareText);
            LiveOpsHubStyle.SetStateText(text, LiveOpsHubFindingRouting.StateOf(diffRow.Change.Consequence));
            row.Add(text);

            string entryKey = diffRow.Change.EntryKey ?? string.Empty;
            row.RegisterCallback<ClickEvent>(_ => RowActivated?.Invoke(entryKey));
            row.AddManipulator(new ContextualMenuManipulator(menuEvent => RowContextRequested?.Invoke(entryKey, menuEvent)));
            _body.Add(row);
            _rows.Add(new CalendarCompareRow(entryKey, diffRow, row));
        }
    }

    /// <summary>Một hàng đã vẽ của pane So với — test đọc để chứng minh thứ tự nhóm, ký hiệu và mục chuột phải.</summary>
    internal sealed class CalendarCompareRow
    {
        public CalendarCompareRow(string entryKey, ExportDiffRow diffRow, VisualElement element)
        {
            EntryKey = entryKey ?? string.Empty;
            DiffRow = diffRow;
            Element = element;
        }

        public string EntryKey { get; }
        public ExportDiffRow DiffRow { get; }
        public VisualElement Element { get; }
    }
}
