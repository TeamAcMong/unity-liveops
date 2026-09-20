using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Màn <c>event-types</c> ở tầng Cấu hình (7.2, Hình 10): bảng loại | inspector 300px, dòng loại chưa khai báo (V-17) và
    /// popover "Thêm loại". Màn chỉ đọc <see cref="EventTypesModel"/> và gọi phiên — mọi quyết định chữ/bật-tắt nằm ở model.
    /// <para>
    /// Mọi lệnh sửa đi qua đúng một chỗ (<see cref="ApplyEdit"/>): phiên dựng Undo group mang TÊN BẰNG câu toast, rồi màn phát
    /// toast Hoàn tác và báo health cũ lên bus. Xoá loại hỏi <see cref="LiveOpsConfirmationPolicy"/> — bảng 7.0 là nguồn duy nhất.
    /// </para>
    /// </summary>
    internal sealed class EventTypesSection : IHubSection, IHubHostAware, IHubSectionActions, IHubSectionViewState, IHubSectionNavigation
    {
        private readonly LiveOpsHubServices _services;

        private IHubHost _host;
        private VisualElement _root;
        private VisualElement _empty;
        private Label _emptyTitle;
        private Label _emptyBody;
        private VisualElement _emptyActions;
        private VisualElement _content;
        private Label _footer;
        private VisualElement _unknownBar;
        private VisualElement _columnsHiddenNote;
        private Label _columnsHiddenNoteText;
        private VisualElement _referenceCard;
        private EventTypeTable _table;
        private EventTypeInspector _inspector;
        private LiveOpsButtonSlot _addTypeSlot;

        private EventTypesModel _model;
        private string _selectedTypeId = string.Empty;
        private bool _isSubscribed;

        public EventTypesSection(LiveOpsHubServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public string Id
        {
            get { return LiveOpsHubSections.Ids.EventTypes; }
        }

        public string Title
        {
            get { return LiveOpsHubStrings.ShellEventTypesTitle; }
        }

        public string Subtitle
        {
            get { return LiveOpsHubStrings.ShellEventTypesSubtitle; }
        }

        public PipelineStage Stage
        {
            get { return PipelineStage.Configure; }
        }

        public IReadOnlyList<string> RequiredElementNames
        {
            get { return LiveOpsHubPaths.RequiredEventTypesElementNames; }
        }

        /// <summary>Services của cửa sổ (G-SESSION) — phiên lịch, bus, port đọc từ đây.</summary>
        internal LiveOpsHubServices Services
        {
            get { return _services; }
        }

        /// <summary>Loại đang chọn trong bảng; "" khi chưa chọn — trạng thái view sống qua domain reload.</summary>
        internal string SelectedTypeId
        {
            get { return _selectedTypeId; }
        }

        /// <summary>Model của lần vẽ gần nhất; null khi chưa dựng view — test đọc để khỏi dựng lại.</summary>
        internal EventTypesModel Model
        {
            get { return _model; }
        }

        /// <summary>Bảng loại của lần dựng gần nhất; null khi chưa dựng view — test đọc thứ tự hàng đang hiện (V-12).</summary>
        internal EventTypeTable Table
        {
            get { return _table; }
        }

        public SectionHealth GetHealth()
        {
            // Nguồn health chính thức của mọi màn là LiveOpsHubFindingRouting (6.4): nó lo cả "chưa có asset" và "kết quả cũ",
            // hai thứ mà model của màn không nhìn thấy.
            return LiveOpsHubFindingRouting.ForSection(Id, _services);
        }

        public VisualElement CreateView()
        {
            VisualTreeAsset tree = _services.LayoutLoader.LoadVisualTree(LiveOpsHubPaths.EventTypesSectionUxml);
            if (tree == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ShellMissingLayoutBodyFormat, LiveOpsHubPaths.EventTypesSectionUxml));
            }

            _root = tree.Instantiate();
            _root = _root.Q(LiveOpsHubPaths.EventTypesElementNames.Body) ?? _root;
            StyleSheet sheet = _services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.EventTypesSectionUss);
            if (sheet != null) _root.styleSheets.Add(sheet);

            _empty = _root.Q(LiveOpsHubPaths.EventTypesElementNames.Empty);
            _emptyTitle = _empty.Q<Label>("event-types-empty-title");
            _emptyBody = _empty.Q<Label>("event-types-empty-body");
            _emptyActions = _empty.Q("event-types-empty-actions");
            _content = _root.Q(LiveOpsHubPaths.EventTypesElementNames.Content);
            _footer = _root.Q<Label>(LiveOpsHubPaths.EventTypesElementNames.Footer);
            _unknownBar = _root.Q(LiveOpsHubPaths.EventTypesElementNames.UnknownBar);
            _referenceCard = _root.Q(LiveOpsHubPaths.EventTypesElementNames.ReferenceCard);

            _columnsHiddenNote = _root.Q(ColumnsHiddenNoteName);
            _columnsHiddenNoteText = _root.Q<Label>(ColumnsHiddenNoteTextName);
            // Icon dựng từ C# vì UXML không với tới LiveOpsHubIcons; chèn ở đầu hàng để nó đứng TRƯỚC câu chữ.
            Image columnsHiddenIcon = LiveOpsHubIcons.CreateImage(ColumnsHiddenNoteIconName, 14);
            columnsHiddenIcon.AddToClassList(LiveOpsHubClassNames.EventTypesColumnsHiddenNoteIcon);
            _columnsHiddenNote.Insert(0, columnsHiddenIcon);

            _table = new EventTypeTable();
            _table.SelectionChanged += OnTableSelectionChanged;
            _table.HiddenColumnsChanged += RefreshColumnsHiddenNote;
            _root.Q(LiveOpsHubPaths.EventTypesElementNames.TableHost).Add(_table.View);
            RefreshColumnsHiddenNote();

            _inspector = new EventTypeInspector(_root.Q(LiveOpsHubPaths.EventTypesElementNames.Inspector));
            _inspector.TypeIdCommitted += OnTypeIdCommitted;
            _inspector.DisplayNameCommitted += OnDisplayNameCommitted;
            _inspector.ColorSlotPicked += OnColorSlotPicked;
            _inspector.ResetColorRequested += OnResetColorRequested;
            _inspector.RequiresJoinChanged += OnRequiresJoinChanged;
            _inspector.ConfigKeyCommitted += OnConfigKeyCommitted;
            _inspector.ShowInCalendarRequested += OnShowInCalendarRequested;
            _inspector.MenuRequested += OnTypeMenuRequested;

            Subscribe();
            // Gỡ đăng ký ngay trên root của màn (TrickleDown, 7.0): cửa sổ dựng lại thân màn thì view cũ rời panel, còn phiên thì
            // sống tiếp — không gỡ ở đây là view chết vẫn nghe DocumentChanged và vẽ vào cây đã tháo.
            _root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel, TrickleDown.TrickleDown);
            Refresh();
            return _root;
        }

        public void OnShown()
        {
        }

        public void Bind(IHubHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public void PopulateHeaderActions(VisualElement container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            // Button LÀ TextElement: `text` được chính nút vẽ trên toàn bộ hộp nội dung, nên Image con nằm ĐÈ lên chữ chứ không
            // đẩy chữ sang phải (ảnh h10 của lần trước: dấu cộng đè chữ "h", chữ sát viền). Chữ phải là Label con đặt SAU Image.
            Button addButton = new Button(OnAddTypeClicked) { name = AddButtonName };
            addButton.AddToClassList(LiveOpsHubClassNames.Button);
            addButton.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            addButton.AddToClassList(LiveOpsHubClassNames.EventTypesAddButton);
            Image addIcon = LiveOpsHubIcons.CreateImage(AddIconName, 14);
            addIcon.name = AddButtonIconName;
            addIcon.AddToClassList(LiveOpsHubClassNames.EventTypesAddButtonIcon);
            addButton.Add(addIcon);
            Label addLabel = new Label(LiveOpsHubStrings.EventTypesAddTypeButton) { name = AddButtonLabelName };
            addLabel.AddToClassList(LiveOpsHubClassNames.EventTypesAddButtonLabel);
            addButton.Add(addLabel);

            _addTypeSlot = new LiveOpsButtonSlot(addButton);
            // Nút sống trong section header của shell, còn EventTypesSection.uss chỉ được nạp lên thân màn (cây khác) — không
            // nạp thêm ở đây thì ba class của nút không có style. Sheet dùng chung (components.uss) thuộc gói khác, không sửa.
            StyleSheet sheet = _services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.EventTypesSectionUss);
            if (sheet != null) _addTypeSlot.styleSheets.Add(sheet);
            _addTypeSlot.SetEnabledWithReason(HasAsset, HasAsset ? string.Empty : LiveOpsHubStrings.EventTypesEmptyNoAssetTitle);
            container.Add(_addTypeSlot);
        }

        public string CaptureViewState()
        {
            return JsonUtility.ToJson(new EventTypesViewState { selectedTypeId = _selectedTypeId });
        }

        public void RestoreViewState(string viewStateJson)
        {
            if (string.IsNullOrEmpty(viewStateJson)) return;
            try
            {
                EventTypesViewState state = JsonUtility.FromJson<EventTypesViewState>(viewStateJson);
                if (state == null) return;
                SelectType(state.selectedTypeId);
            }
            catch (ArgumentException)
            {
                // JSON của bản trước không được làm sập màn (7.0): trạng thái lạ = về mặc định, không chọn gì.
            }
        }

        public void ApplyNavigation(LiveOpsHubNavigation navigation)
        {
            if (navigation == null) throw new ArgumentNullException(nameof(navigation));
            if (navigation.EventType.Length > 0) SelectType(navigation.EventType);
        }

        /// <summary>Icon dấu cộng của nút chính ([SD1 §2.1]) — tên có trong <see cref="LiveOpsHubIcons.AllDesignNames"/>.</summary>
        internal const string AddIconName = "Toolbar Plus";

        /// <summary>Icon err 16px của hàng phát hiện trong card tham chiếu ([SD1 §2.2] "sọc Blocked, icon err").</summary>
        internal const string ReferenceIconName = "console.erroricon.sml";

        /// <summary>Icon info 14px của dòng khai báo cột bị ẩn (W9-05, soát R-01).</summary>
        internal const string ColumnsHiddenNoteIconName = "console.infoicon.sml";

        /// <summary>Dòng khai báo cột bị ẩn và câu chữ của nó — test đọc theo tên này để chứng minh thu gọn KHÔNG im lặng.</summary>
        internal const string ColumnsHiddenNoteName = "event-types-columns-hidden-note";

        internal const string ColumnsHiddenNoteTextName = "event-types-columns-hidden-note-text";

        /// <summary>Nút chính "Thêm loại" và hai con của nó — test đo icon đứng TRƯỚC chữ và không đè lên chữ.</summary>
        internal const string AddButtonName = "event-types-add";
        internal const string AddButtonIconName = "event-types-add-icon";
        internal const string AddButtonLabelName = "event-types-add-label";

        /// <summary>Tên element nút "Khai báo" của dòng loại chưa khai báo — test bấm đúng nút này.</summary>
        internal const string DeclareButtonName = "event-types-declare";

        /// <summary>Tên element icon err của hàng phát hiện trong card tham chiếu.</summary>
        internal const string ReferenceIconElementName = "event-types-reference-icon";

        private bool HasAsset
        {
            get { return _services.Session.Asset != null; }
        }

        private void Subscribe()
        {
            if (_isSubscribed) return;
            _services.Session.DocumentChanged += Refresh;
            _services.Session.CheckChanged += Refresh;
            _services.Session.Remote.Changed += Refresh;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;
            _services.Session.DocumentChanged -= Refresh;
            _services.Session.CheckChanged -= Refresh;
            _services.Session.Remote.Changed -= Refresh;
            _isSubscribed = false;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent detach)
        {
            Unsubscribe();
        }

        /// <summary>Dựng lại model rồi vẽ: bảng, footer, dòng loại chưa khai báo, card tham chiếu, inspector, trạng thái trống.</summary>
        internal void Refresh()
        {
            if (_root == null) return;
            LiveOpsHubCalendarSession session = _services.Session;
            _model = EventTypesModel.Build(session.Document, session.Check.LastReport, session.Remote);

            bool hasAsset = HasAsset;
            bool showContent = hasAsset && !_model.IsEmpty;
            EventTypesVisibility.SetHidden(_empty, showContent);
            EventTypesVisibility.SetHidden(_content, !showContent);
            _addTypeSlot?.SetEnabledWithReason(hasAsset, hasAsset ? string.Empty : LiveOpsHubStrings.EventTypesEmptyNoAssetTitle);
            if (!showContent)
            {
                BuildEmptyState(hasAsset);
                return;
            }

            _table.SetRows(_model.Rows);
            if (_selectedTypeId.Length == 0 && _model.Rows.Count > 0) _selectedTypeId = _model.Rows[0].TypeId;
            _table.SelectType(_selectedTypeId, false);
            _selectedTypeId = _table.SelectedTypeId;

            _footer.text = _model.FooterText;
            // Vẽ lại cả dòng khai báo cột bị ẩn: đổi ngôn ngữ dựng lại thân màn nhưng KHÔNG đổi bề rộng bảng, nên sự kiện
            // hình học không bắn và câu chữ sẽ kẹt ở ngôn ngữ cũ nếu chỉ trông vào HiddenColumnsChanged.
            RefreshColumnsHiddenNote();
            BuildUnknownBar();
            BuildReferenceCard();
            _inspector.Show(_model, _selectedTypeId, _services.Clock.UtcNow, _services.Format);
        }

        /// <summary>
        /// Vẽ lại dòng "bảng đang ẩn cột: …" theo bộ cột bảng vừa dựng (W9-05, soát R-01).
        /// <para>
        /// Vì sao là dòng chữ chứ không chỉ tooltip: tooltip chỉ hiện khi người dùng đã NGỜ có gì đó rồi rê chuột lên đúng
        /// chỗ, mà ở đây người dùng không có lý do để ngờ — bảng trông như một bảng bốn cột bình thường. Tooltip vẫn được
        /// đặt thêm trên chính dòng đó, cho người muốn đọc lại danh sách khi câu đã xuống nhiều dòng.
        /// </para>
        /// </summary>
        private void RefreshColumnsHiddenNote()
        {
            if (_columnsHiddenNote == null || _table == null) return;
            IReadOnlyList<string> hidden = _table.HiddenColumnTitles;
            EventTypesVisibility.SetHidden(_columnsHiddenNote, hidden.Count == 0);
            if (hidden.Count == 0)
            {
                _columnsHiddenNoteText.text = string.Empty;
                _columnsHiddenNote.tooltip = string.Empty;
                return;
            }

            string names = string.Join(LiveOpsHubStrings.EventTypesListSeparator, CopyToArray(hidden));
            string note = string.Format(CultureInfo.CurrentCulture, LiveOpsHubStrings.EventTypesHiddenColumnsNoteFormat, names);
            _columnsHiddenNoteText.text = note;
            _columnsHiddenNote.tooltip = note;
        }

        /// <summary><c>string.Join</c> không nhận <c>IReadOnlyList</c> ở C# 9 — chép ra mảng; danh sách nhiều nhất bốn tên.</summary>
        private static string[] CopyToArray(IReadOnlyList<string> values)
        {
            string[] result = new string[values.Count];
            for (int index = 0; index < values.Count; index++) result[index] = values[index];
            return result;
        }

        private void BuildEmptyState(bool hasAsset)
        {
            _emptyActions.Clear();
            if (!hasAsset)
            {
                _emptyTitle.text = LiveOpsHubStrings.EventTypesEmptyNoAssetTitle;
                EventTypesVisibility.SetHidden(_emptyBody, true);
                Button openOverview = new Button(() => _host?.Navigate(LiveOpsHubSections.Ids.Overview))
                {
                    text = LiveOpsHubStrings.EventTypesEmptyNoAssetButton,
                };
                openOverview.AddToClassList(LiveOpsHubClassNames.Button);
                openOverview.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
                _emptyActions.Add(openOverview);
                return;
            }

            _emptyTitle.text = LiveOpsHubStrings.EventTypesEmptyNoTypeTitle;
            EventTypesVisibility.SetHidden(_emptyBody, false);
            _emptyBody.text = LiveOpsHubStrings.EventTypesEmptyNoTypeBody;
            Button addType = new Button(OnAddTypeClicked) { text = LiveOpsHubStrings.EventTypesAddTypeButton };
            addType.AddToClassList(LiveOpsHubClassNames.Button);
            addType.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _emptyActions.Add(addType);
        }

        private void BuildUnknownBar()
        {
            _unknownBar.Clear();
            bool hasUndeclared = false;
            IReadOnlyList<EventTypeRow> rows = _model.Rows;
            for (int index = 0; index < rows.Count; index++)
            {
                EventTypeRow row = rows[index];
                if (row.IsDeclared) continue;
                _unknownBar.Add(BuildUndeclaredRow(row.TypeId, hasUndeclared));
                hasUndeclared = true;
            }
            EventTypesVisibility.SetHidden(_unknownBar, !hasUndeclared);
        }

        /// <param name="isStacked">Hàng thứ hai trở đi (USS không có <c>:first-child</c>) — chỉ nó mới cần khoảng cách trên.</param>
        private VisualElement BuildUndeclaredRow(string typeId, bool isStacked)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.EventTypesUnknownRow);
            if (isStacked) row.AddToClassList(LiveOpsHubClassNames.EventTypesUnknownRowStacked);

            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.SetHealth(HealthState.Blocked);
            row.Add(mark);

            Label text = new Label(_model.UndeclaredText(typeId));
            text.AddToClassList(LiveOpsHubClassNames.TextBlocked);
            text.AddToClassList(LiveOpsHubClassNames.EventTypesUnknownText);
            row.Add(text);

            Button declare = new Button(() => DeclareType(typeId))
            {
                name = DeclareButtonName,
                text = LiveOpsHubStrings.EventTypesDeclareButton,
            };
            declare.AddToClassList(LiveOpsHubClassNames.Button);
            row.Add(declare);
            return row;
        }

        private void BuildReferenceCard()
        {
            _referenceCard.Clear();
            string typeId = _model.FirstUndeclaredTypeId;
            LiveEventCalendarFinding finding = _model.ReferenceFinding(typeId);
            EventTypesVisibility.SetHidden(_referenceCard, finding == null);
            if (finding == null) return;

            VisualElement findingRow = new VisualElement();
            findingRow.AddToClassList(LiveOpsHubClassNames.FindingRow);
            findingRow.AddToClassList(LiveOpsHubClassNames.FindingStripeBlocked);

            // [SD1 §2.2] "card chứa hàng phát hiện (sọc Blocked, icon err)": thiếu icon thì hàng này và dòng Blocked ngay trên
            // nó (có LiveOpsStateMark) nói khác nhau về cùng một mức. Cỡ 16 là cỡ của hàng phát hiện ([FD §2.12]).
            Image findingIcon = LiveOpsHubIcons.CreateImage(ReferenceIconName, 16);
            findingIcon.name = ReferenceIconElementName;
            findingIcon.AddToClassList(LiveOpsHubClassNames.EventTypesReferenceIcon);
            findingRow.Add(findingIcon);

            // Hàng phát hiện là flex-row (sọc bên trái), nên hai dòng chữ phải nằm trong một cột riêng — không bọc thì headline
            // và dòng id luật dính vào nhau trên cùng một dòng.
            VisualElement textColumn = new VisualElement();
            textColumn.AddToClassList(LiveOpsHubClassNames.EventTypesReferenceText);

            Label headline = new Label(_model.ReferenceHeadline(typeId, _services.Format));
            headline.AddToClassList(LiveOpsHubClassNames.TextBlocked);
            textColumn.Add(headline);

            Label ruleLine = new Label(_model.ReferenceRuleIdLine(typeId));
            ruleLine.AddToClassList(LiveOpsHubClassNames.Caption);
            textColumn.Add(ruleLine);
            findingRow.Add(textColumn);
            _referenceCard.Add(findingRow);
        }

        private void SelectType(string typeId)
        {
            _selectedTypeId = typeId ?? string.Empty;
            if (_table == null) return;
            _table.SelectType(_selectedTypeId, false);
            _selectedTypeId = _table.SelectedTypeId;
            if (_model != null) _inspector.Show(_model, _selectedTypeId, _services.Clock.UtcNow, _services.Format);
        }

        private void OnTableSelectionChanged(string typeId)
        {
            _selectedTypeId = typeId ?? string.Empty;
            if (_model != null) _inspector.Show(_model, _selectedTypeId, _services.Clock.UtcNow, _services.Format);
        }

        private void OnAddTypeClicked()
        {
            if (!HasAsset) return;
            AddEventTypePopover popover = new AddEventTypePopover(_model != null ? _model.DeclaredTypeIds() : Array.Empty<string>(), AddType);
            Rect activator = _addTypeSlot != null ? _addTypeSlot.worldBound : _root.worldBound;
            LiveOpsPopoverContent.ShowSingle(activator, popover);
        }

        private void AddType(string typeId, string displayName)
        {
            LiveEventTypeDefinition type = new LiveEventTypeDefinition(typeId, displayName,
                LiveEventTypeColorSlots.DefaultSlotFor(typeId), false, string.Empty);
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesToastAddedFormat, typeId);
            if (ApplyEdit(null, typeId, new SetEventTypeEdit(type), message)) SelectType(typeId);
        }

        private void DeclareType(string typeId)
        {
            LiveEventTypeDefinition type = new LiveEventTypeDefinition(typeId, string.Empty,
                LiveEventTypeColorSlots.DefaultSlotFor(typeId), false, string.Empty);
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesToastDeclaredFormat, typeId);
            if (ApplyEdit(null, typeId, new SetEventTypeEdit(type), message)) SelectType(typeId);
        }

        private void OnTypeIdCommitted(string typeId, string newTypeId)
        {
            if (string.Equals(typeId, newTypeId, StringComparison.Ordinal)) return;
            // Ô id chỉ mở khi loại chưa có đợt/luật, nên đổi id là xoá định nghĩa cũ + thêm định nghĩa mới trong MỘT Undo group;
            // không mục nào phải đổi theo (đó chính là lý do ô bị khoá khi còn đợt).
            LiveEventTypeDefinition current = _model.DeclaredType(typeId);
            if (current == null || !_model.CanEditTypeId(typeId, out _)) return;
            LiveEventTypeDefinition renamed = new LiveEventTypeDefinition(newTypeId, current.DisplayName, current.ColorSlot,
                current.RequiresJoin, current.DefaultConfigKey);
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesToastTypeIdChangedFormat, typeId, newTypeId);
            LiveEventCalendarEdit edit = new CompositeCalendarEdit(new LiveEventCalendarEdit[]
            {
                new RemoveEventTypeEdit(typeId), new SetEventTypeEdit(renamed),
            });
            if (ApplyEdit(LiveOpsEditOperation.EditEventTypeFields, typeId, edit, message)) SelectType(newTypeId);
        }

        private void OnDisplayNameCommitted(string typeId, string displayName)
        {
            LiveEventTypeDefinition type = _model.DeclaredType(typeId);
            if (type == null || string.Equals(type.DisplayName, displayName, StringComparison.Ordinal)) return;
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesToastDisplayNameChangedFormat, typeId);
            ApplyEdit(LiveOpsEditOperation.EditEventTypeFields, typeId, new SetEventTypeEdit(type.WithDisplayName(displayName)), message);
        }

        private void OnColorSlotPicked(string typeId, int colorSlot)
        {
            LiveEventTypeDefinition type = _model.DeclaredType(typeId);
            if (type == null || type.ColorSlot == colorSlot) return;
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesToastColorChangedFormat, typeId,
                EventTypesModel.SlotNamedText(colorSlot));
            ApplyEdit(LiveOpsEditOperation.EditEventTypeFields, typeId, new SetEventTypeEdit(type.WithColorSlot(colorSlot)), message);
        }

        private void OnResetColorRequested(string typeId)
        {
            OnColorSlotPicked(typeId, _model.HashedSlotOf(typeId));
        }

        private void OnRequiresJoinChanged(string typeId, bool requiresJoin)
        {
            LiveEventTypeDefinition type = _model.DeclaredType(typeId);
            if (type == null || type.RequiresJoin == requiresJoin) return;
            string format = requiresJoin
                ? LiveOpsHubStrings.EventTypesToastRequiresJoinOnFormat
                : LiveOpsHubStrings.EventTypesToastRequiresJoinOffFormat;
            ApplyEdit(LiveOpsEditOperation.EditEventTypeFields, typeId, new SetEventTypeEdit(type.WithRequiresJoin(requiresJoin)),
                string.Format(CultureInfo.InvariantCulture, format, typeId));
        }

        private void OnConfigKeyCommitted(string typeId, string configKey)
        {
            LiveEventTypeDefinition type = _model.DeclaredType(typeId);
            if (type == null || string.Equals(type.DefaultConfigKey, configKey, StringComparison.Ordinal)) return;
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesToastConfigKeyChangedFormat, typeId);
            ApplyEdit(LiveOpsEditOperation.EditEventTypeFields, typeId, new SetEventTypeEdit(type.WithDefaultConfigKey(configKey)), message);
        }

        private void OnShowInCalendarRequested(string typeId)
        {
            LiveOpsHubNavigation navigation = LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Calendar).WithEventType(typeId, true);
            if (_model.TryFindNextFixedEvent(typeId, _services.Clock.UtcNow, out FixedLiveEventEntry nextEntry, out DateTime _))
            {
                navigation = navigation.WithEntry(nextEntry.EntryKey);
            }
            _services.Bus.Navigate(navigation);
        }

        private void OnTypeMenuRequested(string typeId, Rect activator)
        {
            GenericMenu menu = new GenericMenu();
            bool canDelete = _model.CanDeleteType(typeId, out string menuLabel, out string reason);
            GUIContent label = new GUIContent(menuLabel, reason);
            if (canDelete)
            {
                menu.AddItem(label, false, () => DeleteType(typeId));
            }
            else
            {
                menu.AddDisabledItem(label);
            }
            menu.DropDown(activator);
        }

        /// <summary>Xoá loại: bảng 7.0 cho xoá thẳng (toast Hoàn tác) khi không còn đợt/luật, và KHÔNG CHO khi còn.</summary>
        internal void DeleteType(string typeId)
        {
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesToastRemovedFormat, typeId);
            if (ApplyEdit(LiveOpsEditOperation.RemoveEventType, typeId, new RemoveEventTypeEdit(typeId), message)) SelectType(string.Empty);
        }

        /// <summary>
        /// Một chỗ duy nhất cho MỌI lệnh sửa của màn: hỏi <see cref="LiveOpsConfirmationPolicy"/> đúng một lần (7.0 "mức xác
        /// nhận — một nguồn duy nhất"), rồi phiên dựng Undo group tên bằng câu toast → toast Hoàn tác → health thành cũ.
        /// </summary>
        /// <param name="operation">
        /// Ô của bảng 7.0. <c>null</c> = lệnh không có ô nào trong bảng: "Thêm loại" / "Khai báo" tạo định nghĩa mới, không
        /// đụng mục nào đang có, và enum chưa khai <c>AddEventType</c> (mục 3 chỉ có <c>AddFixedEvent</c>/<c>AddRecurringRule</c>).
        /// </param>
        /// <param name="targetKey">Id loại mà lệnh đụng tới — policy đếm đợt/luật của đúng loại này.</param>
        private bool ApplyEdit(LiveOpsEditOperation? operation, string targetKey, LiveEventCalendarEdit edit, string undoName)
        {
            if (operation.HasValue)
            {
                LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(operation.Value, _services.Session.Document,
                    null, _services.Session.Publish.ActiveBaseline, _services.Clock.UtcNow, targetKey);
                // P1: màn này không có ô nào cần hộp xác nhận (sửa field = "luôn", xoá loại = "không cho" khi còn đợt), nên mọi
                // mức khác None là "không làm". Ô nào của bảng 7.0 đổi sang cấp 1/cấp 2 thì thêm nhánh mở hộp ở ĐÂY, một chỗ.
                if (decision.Requirement != LiveOpsConfirmRequirement.None) return false;
            }

            LiveOpsHubEditOutcome outcome = _services.Session.Apply(edit, undoName);
            if (!outcome.Applied) return false;
            _services.Bus.ShowToast(LiveOpsToastModel.ForEdit(undoName, outcome.UndoGroup));
            _services.Bus.InvalidateHealth();
            return true;
        }

        /// <summary>Trạng thái view sống qua domain reload (7.0): chỉ loại đang chọn — zoom/sort của bảng do control tự giữ.</summary>
        [Serializable]
        private sealed class EventTypesViewState
        {
            public string selectedTypeId;
        }
    }
}
