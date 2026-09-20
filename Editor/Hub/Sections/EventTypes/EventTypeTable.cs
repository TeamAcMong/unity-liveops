using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng loại của màn Loại event ([SD1 §2.1]): <see cref="MultiColumnListView"/> bảy cột 44 / 140 / giãn / 120 / 140 / 90 / 84.
    /// Bảng chỉ VẼ <see cref="EventTypeRow"/> do <see cref="EventTypesModel"/> dựng — không tự tính chữ, không đọc tài liệu.
    /// <para>
    /// (W9-31) Cột thứ tám 36px KHÔNG CÒN. Nó mang tiêu đề rỗng và chỉ vẽ dấu trạng thái cho loại CHƯA KHAI BÁO, mà mẫu
    /// thiết kế không có loại nào chưa khai — nên ở mọi ảnh, mọi cỡ, nó là một cột trống có đường kẻ chia cột riêng và người
    /// đọc thấy đúng một bảng vỡ cột. Dấu ấy nay nằm trong ô MÀU, ngay cạnh swatch rỗng viền quiet vốn đã nói "chưa khai" —
    /// không mất thông tin nào, và 36px trả lại cho các cột chữ nên cửa sổ hẹp giữ được nhiều cột phụ hơn.
    /// </para>
    /// <para>
    /// Thứ tự mặc định là thứ tự làn trên Lịch (V-12); bấm tiêu đề cột mới sắp lại, và bỏ sắp thì về đúng thứ tự làn. Kéo hàng để
    /// đổi thứ tự làn KHÔNG có ở P1 (V-12: chỉ menu header làn của màn Lịch), nên bảng không đăng ký đường kéo nào.
    /// </para>
    /// </summary>
    internal sealed class EventTypeTable
    {
        internal const string ColorColumnName = "color";
        internal const string TypeIdColumnName = "type-id";
        internal const string DisplayNameColumnName = "display-name";
        internal const string EntryColumnName = "entry";
        internal const string ConfigKeyColumnName = "config-key";
        internal const string SourceColumnName = "source";
        internal const string EventCountColumnName = "event-count";

        /// <summary>
        /// (W9-31) Tên của cột 36px ĐÃ GỠ. Hằng còn lại vì kịch bản chụp vùng EventTypes khai một khung mong đợi theo tên này
        /// và file đó thuộc quyền ghi của gói khác trong cùng đợt; khung ấy nay không khớp phần tử nào nên không kiểm gì.
        /// Xoá CẶP (hằng ở đây + khung trong kịch bản chụp) ở cổng đợt, kẻo hằng thừa ở lại rồi có người dựng lại cột trống.
        /// </summary>
        internal const string StateColumnName = "state";

        /// <summary>
        /// (W9-05) Bề rộng TỐI THIỂU đọc được của từng cột — bề rộng CHỮ dài nhất của dữ liệu mẫu cộng đệm ô 8px, rồi cộng
        /// thêm khoảng dư để luật "chữ chiếm > 95% bề rộng ô" của W9-25 không kêu. Không khai số này thì
        /// <see cref="MultiColumnListView"/> chia đều bề rộng còn lại và bóp MỌI cột xuống 27px ở cửa sổ 700 — tám cột đều
        /// thành chữ cụt, đúng 76 chỗ của phiếu W9-05.
        /// <para>
        /// Ba số dưới đây được SỬA LẠI sau lượt đo với cổng W9 mới (bộ cỡ có 950x700, và bậc breakpoint ~1000px của
        /// G-W9-CALENDAR trả thêm chỗ cho bảng ở 1024 nên cột phụ nào cũng rơi đúng vào bề rộng tối thiểu của nó):
        /// khoá config "star_tournament_v1" cần 119px chữ — 112 là CẮT THẬT, đo được ở 1024 trên cả vi và en;
        /// "Joins automatically" cần 100px và "Recurring rule" cần 73px — hai cái đó vừa khít 96,2% và 96,1% ô, tức
        /// đúng loại "chưa cắt nhưng sắp cắt" mà W9-25 dựng lên để bắt. Nới bề rộng ĐỌC ĐƯỢC là cách chữa, không phải
        /// nới ngưỡng của luật. Khoá config lấy 140 chứ không 132: ở 132 nó vẫn còn 96% ô — hết CẮT nhưng chưa hết
        /// CHẬT, mà cảnh báo của W9-25 nói đúng chỗ đó.
        /// </para>
        /// </summary>
        private const float ColorColumnMinimumWidth = 44f;

        private const float TypeIdColumnMinimumWidth = 128f;

        /// <summary>
        /// (W9-31) 120 → 128px. Tên hiển thị dài nhất của mẫu ("Nhiệm vụ dung nham") cần 111px chữ; 120 cho đúng 112px vùng
        /// nội dung sau 8px đệm ô — dư 1px, tức 99,1%, đúng thứ luật dư 5% của W9-25 dựng lên để bắt. Con số 120 chưa bao
        /// giờ bị lộ vì cột này GIÃN và trước W9-31 nó không bao giờ rơi xuống bề rộng tối thiểu; gỡ cột trạng thái 36px trả
        /// chỗ cho một cột phụ nữa ở cửa sổ 700px, và lúc đó cột tên hiển thị mới chạm đáy của chính nó. 128 cho 120px vùng
        /// nội dung, dư 9px (92,5%).
        /// </summary>
        private const float DisplayNameColumnMinimumWidth = 128f;
        private const float EntryColumnMinimumWidth = 116f;
        private const float ConfigKeyColumnMinimumWidth = 140f;
        private const float SourceColumnMinimumWidth = 88f;
        private const float EventCountColumnMinimumWidth = 56f;

        /// <summary>
        /// Chỗ dành cho thanh cuộn dọc của bảng. Trừ sẵn thì tổng bề rộng tối thiểu của các cột đang hiện không bao giờ
        /// vượt viewport, tức bảng không bao giờ mọc thanh cuộn NGANG — cuộn ngang trong một bảng là cách giấu cột đi mà
        /// không nói, cổng bố cục tính nó là lỗi.
        /// </summary>
        private const float VerticalScrollerReserve = 14f;

        /// <summary>
        /// Ba cột luôn hiện: màu (đã mang cả dấu trạng thái từ W9-31), id loại, tên hiển thị. Đây là phần trả lời "hàng này
        /// là loại nào và nó đang thế nào" — bỏ cột nào trong ba cột này thì bảng thôi là bảng loại.
        /// </summary>
        private const float AlwaysVisibleColumnsWidth = ColorColumnMinimumWidth + TypeIdColumnMinimumWidth
            + DisplayNameColumnMinimumWidth;

        /// <summary>
        /// Bốn cột phụ theo thứ tự GIỮ LẠI: cách vào → khoá config → nguồn → số đợt. Cửa sổ hẹp dần thì bỏ từ cuối danh
        /// sách. Bỏ theo TIỀN TỐ (gặp cột đầu tiên không vừa là dừng) chứ không nhặt cột nào vừa thì lấy: nhảy cóc một cột
        /// làm thứ tự cột đổi theo bề rộng cửa sổ và người dùng mất mốc đọc.
        /// </summary>
        private static readonly float[] OptionalColumnMinimumWidths =
        {
            EntryColumnMinimumWidth, ConfigKeyColumnMinimumWidth, SourceColumnMinimumWidth, EventCountColumnMinimumWidth,
        };

        /// <summary>Ba cột đầu (màu, id loại, tên hiển thị) — luôn nằm trước bốn cột phụ.</summary>
        private readonly List<Column> _leadingColumns = new List<Column>();

        /// <summary>Bốn cột phụ theo đúng thứ tự giữ lại của <see cref="OptionalColumnMinimumWidths"/>.</summary>
        private readonly List<Column> _optionalColumns = new List<Column>();

        /// <summary>Số cột phụ đang hiện; -1 = chưa dựng lần nào. Chỉ dựng lại bảng cột khi con số này ĐỔI.</summary>
        private int _visibleOptionalColumnCount = -1;

        /// <summary>
        /// Số cột phụ đã ĐẶT LỊCH áp nhưng lượt <c>schedule</c> chưa chạy; -1 = không có lịch nào đang chờ.
        /// <para>
        /// Vì sao phải có trường riêng thay vì so với <see cref="_visibleOptionalColumnCount"/> (soát W9 R-06): hai lượt
        /// layout trong CÙNG một khung hình là chuyện thường ở đây — <c>LiveOpsHubBreakpoints.Apply</c> bật/tắt class
        /// <c>--medium</c> làm inspector đổi 300 ↔ 240px, nên bảng đo lại ngay trong khung đó. Nếu so với số ĐÃ ÁP thì
        /// lượt 1 (muốn 3) đặt lịch, lượt 2 (muốn 2, bằng đúng số đang áp) lại thoát sớm — và cái lịch cũ vẫn áp 3 cột
        /// vào một bề rộng chỉ chứa nổi 2, rồi bảng KẸT ở đó tới lần đổi hình học sau.
        /// </para>
        /// </summary>
        private int _pendingOptionalColumnCount = -1;

        /// <summary>Tiêu đề các cột phụ đang bị ẩn, theo đúng thứ tự cột — rỗng khi bảng đủ tám cột.</summary>
        private readonly List<string> _hiddenColumnTitles = new List<string>();

        private readonly List<EventTypeRow> _laneOrder = new List<EventTypeRow>();
        private readonly List<EventTypeRow> _visibleOrder = new List<EventTypeRow>();

        internal EventTypeTable()
        {
            View = new MultiColumnListView { name = LiveOpsHubPaths.EventTypesElementNames.Table, itemsSource = _visibleOrder };
            View.AddToClassList(LiveOpsHubClassNames.EventTypesTable);
            View.selectionType = SelectionType.Single;
            AddColumns();
            // Bề rộng bảng đổi (kéo mép cửa sổ, inspector co lại) thì tính lại xem giữ được bao nhiêu cột phụ.
            View.RegisterCallback<GeometryChangedEvent>(OnViewGeometryChanged);
            LiveOpsTableSorting.EnableCustomSorting(View);
            View.columnSortingChanged += ApplySorting;
            View.selectedIndicesChanged += OnSelectedIndicesChanged;
        }

        /// <summary>Loại đang chọn đổi (bấm hàng, ↑↓) — "" khi bỏ chọn.</summary>
        internal event Action<string> SelectionChanged;

        /// <summary>
        /// Bộ cột phụ đang bị ẩn vừa đổi — màn vẽ lại dòng khai báo dưới bảng (W9-05, soát R-01).
        /// <para>
        /// Thu gọn có chủ đích chỉ hợp lệ khi người dùng ĐƯỢC BÁO là có thứ đang bị giấu; giấu im lặng thì người đọc
        /// tưởng bảng chỉ có bốn cột và không bao giờ biết phải nới cửa sổ hay nhìn sang pane chi tiết ([SPIKE-B SP-3]).
        /// </para>
        /// </summary>
        internal event Action HiddenColumnsChanged;

        internal MultiColumnListView View { get; }

        /// <summary>
        /// Số cột phụ ĐÃ ÁP xong (−1 khi chưa có số đo nào). Khác <see cref="OptionalColumnCountThatFits"/> ở chỗ hàm kia
        /// nói "bề rộng này chứa được mấy cột" còn cái này nói "bảng đã dựng lại xong theo con số đó chưa" — khoảng giữa
        /// hai cái là một lượt <c>schedule.Execute</c>. Test cần đọc được nó để CHỜ THEO ĐIỀU KIỆN thay vì đếm khung
        /// hình (W9-16): ba khung là đủ hay không tuỳ máy đang bận tới đâu.
        /// </summary>
        internal int AppliedOptionalColumnCount
        {
            get { return _visibleOptionalColumnCount; }
        }

        /// <summary>Tiêu đề các cột phụ đang bị ẩn ở bề rộng hiện tại — rỗng khi bảng còn đủ tám cột.</summary>
        internal IReadOnlyList<string> HiddenColumnTitles
        {
            get { return _hiddenColumnTitles; }
        }

        /// <summary>Loại đang chọn; "" khi không có hàng nào được chọn.</summary>
        internal string SelectedTypeId
        {
            get
            {
                int index = View.selectedIndex;
                return index >= 0 && index < _visibleOrder.Count ? _visibleOrder[index].TypeId : string.Empty;
            }
        }

        /// <summary>Nạp hàng mới rồi giữ nguyên loại đang chọn nếu nó còn (sửa một field không được nhảy mất lựa chọn).</summary>
        internal void SetRows(IReadOnlyList<EventTypeRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            string previousTypeId = SelectedTypeId;
            _laneOrder.Clear();
            for (int index = 0; index < rows.Count; index++) _laneOrder.Add(rows[index]);
            ApplySorting();
            SelectType(previousTypeId, false);
        }

        /// <summary>Chọn một loại theo id; id không còn trong bảng thì bỏ chọn.</summary>
        internal void SelectType(string typeId, bool notify)
        {
            int index = IndexOf(typeId);
            if (index < 0)
            {
                View.ClearSelection();
            }
            else
            {
                View.SetSelection(index);
            }
            if (notify) SelectionChanged?.Invoke(SelectedTypeId);
        }

        /// <summary>Thứ tự đang hiện — test đọc để chứng minh bảng theo thứ tự làn khi chưa bật sort cột (V-12).</summary>
        internal IReadOnlyList<EventTypeRow> VisibleRows
        {
            get { return _visibleOrder; }
        }

        private void AddColumns()
        {
            _leadingColumns.Add(BuildColumn(ColorColumnName, LiveOpsHubStrings.EventTypesColumnColor, 44, ColorColumnMinimumWidth,
                MakeSwatchCell, BindSwatchCell, false));
            _leadingColumns.Add(BuildColumn(TypeIdColumnName, LiveOpsHubStrings.EventTypesColumnTypeId, 140, TypeIdColumnMinimumWidth,
                MakeMonoCell, BindTypeIdCell, true));
            Column displayName = BuildColumn(DisplayNameColumnName, LiveOpsHubStrings.EventTypesColumnDisplayName, 180,
                DisplayNameColumnMinimumWidth, MakeTextCell, BindDisplayNameCell, true);
            displayName.stretchable = true;
            _leadingColumns.Add(displayName);
            _optionalColumns.Add(BuildColumn(EntryColumnName, LiveOpsHubStrings.EventTypesColumnEntry, 120, EntryColumnMinimumWidth,
                MakeTextCell, BindEntryCell, true));
            _optionalColumns.Add(BuildColumn(ConfigKeyColumnName, LiveOpsHubStrings.EventTypesColumnConfigKey, 140, ConfigKeyColumnMinimumWidth,
                MakeMonoCell, BindConfigKeyCell, true));
            _optionalColumns.Add(BuildColumn(SourceColumnName, LiveOpsHubStrings.EventTypesColumnSource, 90, SourceColumnMinimumWidth,
                MakeTextCell, BindSourceCell, true));
            _optionalColumns.Add(BuildColumn(EventCountColumnName, LiveOpsHubStrings.EventTypesColumnEventCount, 84,
                EventCountColumnMinimumWidth, MakeCountCell, BindCountCell, true));
            ApplyVisibleColumnCount(_optionalColumns.Count);
        }

        private static Column BuildColumn(string columnName, string title, float width, float minimumWidth,
            Func<VisualElement> makeCell, Action<VisualElement, int> bindCell, bool sortable)
        {
            return new Column
            {
                name = columnName,
                title = title,
                width = width,
                minWidth = minimumWidth,
                sortable = sortable,
                makeCell = makeCell,
                bindCell = bindCell,
            };
        }

        private void OnViewGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            int wanted = OptionalColumnCountThatFits(geometryEvent.newRect.width);
            if (wanted < 0) return;
            // So với số ĐANG CHỜ ÁP chứ không với số đã áp (soát W9 R-06) — xem chú thích của _pendingOptionalColumnCount.
            int current = _pendingOptionalColumnCount >= 0 ? _pendingOptionalColumnCount : _visibleOptionalColumnCount;
            if (wanted == current) return;
            _pendingOptionalColumnCount = wanted;
            // Dựng lại bảng cột là thay đổi CẤU TRÚC cây; làm ngay bên trong một lượt layout là sửa cái đang được đo.
            // Hoãn sang lượt sau — cổng bố cục chờ layout ổn định 9 khung nên vẫn đo đúng bộ cột mới.
            // Callback TỰ ĐO LẠI thay vì dùng lại `wanted` đã bắt được: giữa lúc đặt lịch và lúc chạy, bề rộng có thể đã
            // đổi thêm lần nữa, và áp một con số cũ là cách chắc chắn nhất để bảng kẹt ở bộ cột sai.
            View.schedule.Execute(ApplyColumnCountThatFitsNow);
        }

        /// <summary>Đo lại bề rộng THẬT tại lúc lượt hoãn chạy rồi áp; bỏ qua khi cây chưa có số đo.</summary>
        private void ApplyColumnCountThatFitsNow()
        {
            _pendingOptionalColumnCount = -1;
            int wanted = OptionalColumnCountThatFits(View.resolvedStyle.width);
            if (wanted < 0) return;
            ApplyVisibleColumnCount(wanted);
        }

        /// <summary>
        /// Giữ lại nhiều cột phụ nhất mà bề rộng bảng còn CHỨA ĐƯỢC ở bề rộng tối thiểu đọc được của chúng; trả về -1 khi
        /// chưa có số đo. Đây là "thu gọn có chủ đích": cột bị bỏ không biến mất khỏi hub — mọi giá trị của loại đang chọn
        /// vẫn đọc đủ ở inspector bên phải, vốn là nơi thiết kế đặt chi tiết ([SD1] §2.1). Ngược lại, để tám cột cùng ở lại
        /// trong 423px thì CẢ TÁM đều còn 27px và không cột nào đọc nổi — mất nhiều hơn hẳn.
        /// <para>
        /// Thu gọn KHÔNG ĐƯỢC im lặng: mỗi lần con số này đổi, <see cref="RefreshHiddenColumnTitles"/> khai tên các cột
        /// vừa bị bỏ và màn in một dòng chữ (vi + en) ngay dưới bảng, kèm tooltip liệt kê cột (soát W9 R-01).
        /// </para>
        /// <para>
        /// <c>static</c> và <c>internal</c> vì đây là HÀM THUẦN — bậc cột theo bề rộng là luật đáng được khoá bằng test
        /// đơn vị, không phải thứ chỉ chứng minh được bằng một lượt dựng cửa sổ thật.
        /// </para>
        /// </summary>
        internal static int OptionalColumnCountThatFits(float viewWidth)
        {
            if (float.IsNaN(viewWidth) || viewWidth <= 0f) return -1;
            float usableWidth = viewWidth - VerticalScrollerReserve;
            float used = AlwaysVisibleColumnsWidth;
            int count = 0;
            while (count < OptionalColumnMinimumWidths.Length && used + OptionalColumnMinimumWidths[count] <= usableWidth)
            {
                used += OptionalColumnMinimumWidths[count];
                count++;
            }

            return count;
        }

        /// <summary>
        /// Dựng lại danh sách cột với <paramref name="optionalCount"/> cột phụ đầu tiên. Dựng LẠI chứ không dùng
        /// <c>Column.visible</c>: ở Unity 2022.3, một cột ẩn đứng TRƯỚC một cột hiện làm
        /// <c>MultiColumnController.OnColumnResized</c> tra ô theo chỉ số của danh sách ĐẦY ĐỦ trong khi hàng chỉ dựng ô cho
        /// cột đang hiện — ném <c>ArgumentOutOfRangeException</c> ngay lượt layout đầu (đã gặp thật ở lượt EditMode 2022.3
        /// của gói này). Cột phụ bị bỏ luôn là các cột CUỐI từ W9-31, nhưng cách dựng lại vẫn giữ: nó là cách duy nhất đã
        /// chạy xanh trên cả hai bản, và một lượt sắp xếp đang trỏ cột vừa bỏ vẫn phải được gỡ trước khi bảng đổi cột.
        /// </summary>
        private void ApplyVisibleColumnCount(int optionalCount)
        {
            if (optionalCount == _visibleOptionalColumnCount) return;
            _visibleOptionalColumnCount = optionalCount;
            DropSortingOfHiddenColumns(optionalCount);
            View.columns.Clear();
            foreach (Column column in _leadingColumns) View.columns.Add(column);
            for (int index = 0; index < optionalCount; index++) View.columns.Add(_optionalColumns[index]);
            RefreshHiddenColumnTitles(optionalCount);
            // Danh sách cột vừa dựng lại thì thứ tự hàng phải được TÍNH LẠI theo bộ cột còn lại (soát W9 R-07): đang sắp
            // theo một cột phụ mà cột đó biến mất thì bảng phải về thứ tự làn, chứ không giữ một thứ tự mà người dùng
            // không còn nhìn thấy lý do.
            ApplySorting();
        }

        /// <summary>
        /// Gỡ mọi mô tả sắp xếp đang trỏ vào cột sắp bị bỏ, TRƯỚC khi <c>columns.Clear()</c> chạy. Không gỡ thì
        /// <c>View.sortedColumns</c> còn giữ tham chiếu tới những <see cref="Column"/> đã rời bảng, và lượt
        /// <see cref="ApplySorting"/> ngay sau đó sắp theo một cột không còn hiện trên màn.
        /// </summary>
        private void DropSortingOfHiddenColumns(int optionalCount)
        {
            for (int index = View.sortColumnDescriptions.Count - 1; index >= 0; index--)
            {
                if (IsColumnVisibleAt(View.sortColumnDescriptions[index].columnName, optionalCount)) continue;
                View.sortColumnDescriptions.RemoveAt(index);
            }
        }

        /// <summary>Cột tên <paramref name="columnName"/> có nằm trong bộ cột hiện khi giữ <paramref name="optionalCount"/> cột phụ không.</summary>
        private bool IsColumnVisibleAt(string columnName, int optionalCount)
        {
            for (int index = optionalCount; index < _optionalColumns.Count; index++)
            {
                if (string.Equals(_optionalColumns[index].name, columnName, StringComparison.Ordinal)) return false;
            }

            return true;
        }

        /// <summary>Ghi lại tiêu đề các cột phụ bị bỏ rồi báo cho màn — dòng khai báo dưới bảng đọc đúng danh sách này.</summary>
        private void RefreshHiddenColumnTitles(int optionalCount)
        {
            _hiddenColumnTitles.Clear();
            for (int index = optionalCount; index < _optionalColumns.Count; index++)
            {
                _hiddenColumnTitles.Add(_optionalColumns[index].title);
            }

            Action changed = HiddenColumnsChanged;
            if (changed != null) changed();
        }

        /// <summary>
        /// Ô MÀU: swatch 9x9 + dấu trạng thái của chính hàng (W9-31). Hai thứ đứng cạnh nhau trong 44px vì chúng nói cùng một
        /// chuyện về loại này — swatch rỗng viền quiet nghĩa là "hub chưa biết màu vì loại chưa khai", dấu Blocked nói thẳng
        /// điều đó. Dấu luôn có trong cây (ẩn bằng <c>visible</c>) để hàng không nhảy một pixel khi đổi trạng thái.
        /// </summary>
        private static VisualElement MakeSwatchCell()
        {
            VisualElement cell = new VisualElement();
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCell);
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCellCenter);
            VisualElement swatch = new VisualElement();
            swatch.AddToClassList(LiveOpsHubClassNames.Swatch);
            cell.Add(swatch);
            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.Size = LiveOpsStateMark.MarkSize.Small;
            cell.Add(mark);
            return cell;
        }

        private static VisualElement MakeTextCell()
        {
            Label cell = new Label();
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCell);
            return cell;
        }

        private static VisualElement MakeMonoCell()
        {
            Label cell = new Label();
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCell);
            cell.AddToClassList(LiveOpsHubClassNames.Mono);
            return cell;
        }

        private static VisualElement MakeCountCell()
        {
            Label cell = new Label();
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCell);
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCellRight);
            return cell;
        }

        private void BindSwatchCell(VisualElement cell, int rowIndex)
        {
            EventTypeRow row = _visibleOrder[rowIndex];
            ApplyRowClasses(cell, row);
            VisualElement swatch = cell[0];
            for (int slot = 0; slot < LiveEventTypeDefinition.ColorSlotCount; slot++)
            {
                swatch.EnableInClassList(EventTypeColorClassNames.Of(slot), slot == row.ColorSlot);
            }
            swatch.EnableInClassList(LiveOpsHubClassNames.EventTypesSwatchUndeclared, !row.IsDeclared);
            LiveOpsStateMark mark = (LiveOpsStateMark)cell[1];
            mark.SetHealth(HealthState.Blocked);
            mark.visible = !row.IsDeclared;
        }

        private void BindTypeIdCell(VisualElement cell, int rowIndex)
        {
            BindLabel(cell, rowIndex, _visibleOrder[rowIndex].TypeId);
        }

        private void BindDisplayNameCell(VisualElement cell, int rowIndex)
        {
            BindLabel(cell, rowIndex, _visibleOrder[rowIndex].DisplayName);
        }

        private void BindEntryCell(VisualElement cell, int rowIndex)
        {
            BindLabel(cell, rowIndex, _visibleOrder[rowIndex].EntryText);
        }

        private void BindConfigKeyCell(VisualElement cell, int rowIndex)
        {
            BindLabel(cell, rowIndex, _visibleOrder[rowIndex].ConfigKey);
        }

        private void BindSourceCell(VisualElement cell, int rowIndex)
        {
            BindLabel(cell, rowIndex, _visibleOrder[rowIndex].SourceText);
        }

        private void BindCountCell(VisualElement cell, int rowIndex)
        {
            EventTypeRow row = _visibleOrder[rowIndex];
            BindLabel(cell, rowIndex, row.EventCountText);
            // "luật" là chữ thay cho số nên hạ sáng (opacity 0,7 của class quiet) — không so chuỗi, đọc cờ của model.
            cell.EnableInClassList(LiveOpsHubClassNames.TextQuiet, row.IsEventCountRuleText);
        }

        private void BindLabel(VisualElement cell, int rowIndex, string text)
        {
            EventTypeRow row = _visibleOrder[rowIndex];
            ApplyRowClasses(cell, row);
            ((Label)cell).text = text;
        }

        private static void ApplyRowClasses(VisualElement cell, EventTypeRow row)
        {
            cell.EnableInClassList(LiveOpsHubClassNames.EventTypesRowUndeclared, !row.IsDeclared);
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> indices)
        {
            SelectionChanged?.Invoke(SelectedTypeId);
        }

        /// <summary>
        /// Sắp lại theo cột đang chọn; không cột nào đang sắp thì về THỨ TỰ LÀN. Bảng tự sắp <c>itemsSource</c> rồi
        /// <c>RefreshItems</c> thay vì <c>Column.comparison</c> — property đó không có ở 2022.3 ([API §5]).
        /// </summary>
        private void ApplySorting()
        {
            string previousTypeId = SelectedTypeId;
            _visibleOrder.Clear();
            for (int index = 0; index < _laneOrder.Count; index++) _visibleOrder.Add(_laneOrder[index]);

            List<SortColumnDescription> sorted = new List<SortColumnDescription>();
            foreach (SortColumnDescription description in View.sortedColumns) sorted.Add(description);
            if (sorted.Count > 0)
            {
                _visibleOrder.Sort((left, right) => CompareBySortedColumns(left, right, sorted));
            }
            View.RefreshItems();
            SelectType(previousTypeId, false);
        }

        private static int CompareBySortedColumns(EventTypeRow left, EventTypeRow right, List<SortColumnDescription> sorted)
        {
            for (int index = 0; index < sorted.Count; index++)
            {
                SortColumnDescription description = sorted[index];
                int comparison = CompareByColumn(left, right, description.columnName);
                if (comparison == 0) continue;
                return description.direction == SortDirection.Descending ? -comparison : comparison;
            }
            // Cột bằng nhau thì giữ thứ tự làn: sắp "ổn định" để hai lần bấm cùng cột không đảo hàng ngẫu nhiên.
            return left.LaneIndex.CompareTo(right.LaneIndex);
        }

        private static int CompareByColumn(EventTypeRow left, EventTypeRow right, string columnName)
        {
            switch (columnName)
            {
                case TypeIdColumnName: return string.CompareOrdinal(left.TypeId, right.TypeId);
                case DisplayNameColumnName: return string.CompareOrdinal(left.DisplayName, right.DisplayName);
                case EntryColumnName: return string.CompareOrdinal(left.EntryText, right.EntryText);
                case ConfigKeyColumnName: return string.CompareOrdinal(left.ConfigKey, right.ConfigKey);
                case SourceColumnName: return string.CompareOrdinal(left.SourceText, right.SourceText);
                case EventCountColumnName: return left.UsageCount.CompareTo(right.UsageCount);
                default: return 0;
            }
        }

        private int IndexOf(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return -1;
            for (int index = 0; index < _visibleOrder.Count; index++)
            {
                if (string.Equals(_visibleOrder[index].TypeId, typeId, StringComparison.Ordinal)) return index;
            }
            return -1;
        }
    }

    /// <summary>
    /// Ẩn/hiện một phần của màn Loại event bằng class (UI Toolkit không có <c>VisualElement.hidden</c>, còn
    /// <c>style.display</c> là style inline ngoài 10 chỗ được phép [FD §2.14]).
    /// </summary>
    internal static class EventTypesVisibility
    {
        internal static void SetHidden(VisualElement element, bool isHidden)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.EnableInClassList(LiveOpsHubClassNames.EventTypesHidden, isHidden);
        }
    }

    /// <summary>Ô màu → class nền của bảng 8.10; gom một chỗ để bảng và inspector không tự nối chuỗi tên class.</summary>
    internal static class EventTypeColorClassNames
    {
        /// <summary>"" khi số ô nằm ngoài [0, 7] — hàng chưa khai báo (-1) không gắn class màu nào.</summary>
        internal static string Of(int colorSlot)
        {
            switch (colorSlot)
            {
                case 0: return LiveOpsHubClassNames.EventColor0;
                case 1: return LiveOpsHubClassNames.EventColor1;
                case 2: return LiveOpsHubClassNames.EventColor2;
                case 3: return LiveOpsHubClassNames.EventColor3;
                case 4: return LiveOpsHubClassNames.EventColor4;
                case 5: return LiveOpsHubClassNames.EventColor5;
                case 6: return LiveOpsHubClassNames.EventColor6;
                case 7: return LiveOpsHubClassNames.EventColor7;
                default: return string.Empty;
            }
        }
    }
}
