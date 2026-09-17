using System;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Màn <c>overview</c> (7.1) — "Lịch đang kẹt ở đâu và việc gì cần làm trước khi đăng". Thân dựng từ
    /// <see cref="OverviewModel"/> (thuần) nên view chỉ đổ chữ và bật class; mọi câu của phát hiện đến từ
    /// <see cref="LiveOpsFindingText"/> và dòng "chặn Copy JSON" đến từ <see cref="ExportGateState"/> (V-9) — Tổng quan
    /// không tự kết luận gì, health của màn luôn Ok (6.4: tự tô sẽ đếm đôi với màn gốc).
    /// </summary>
    internal sealed class OverviewSection : IHubSection, IHubHostAware, IHubSectionActions, IHubSectionViewState
    {
        internal const string BodyElementName = "overview-body";
        internal const string EmptyElementName = "overview-empty";
        internal const string DefaultBodyElementName = "overview-default";
        internal const string ScrollElementName = "overview-scroll";
        internal const string MetricsElementName = "overview-metrics";
        internal const string NeedsActionBodyElementName = "overview-needs-action-body";
        internal const string FlowElementName = "overview-flow";
        internal const string UpcomingElementName = "overview-upcoming";
        internal const string UpcomingTabsElementName = "overview-upcoming-tabs";
        internal const string FooterNoteElementName = "overview-footer-note";
        internal const string NeedsActionTitleElementName = "overview-needs-action-title";
        internal const string NeedsActionSubtitleElementName = "overview-needs-action-subtitle";
        internal const string FlowTitleElementName = "overview-flow-title";
        internal const string FlowSubtitleElementName = "overview-flow-subtitle";
        internal const string UpcomingTitleElementName = "overview-upcoming-title";
        internal const string UpcomingSubtitleElementName = "overview-upcoming-subtitle";
        internal const string EmptyTitleElementName = "overview-empty-title";
        internal const string EmptyBodyElementName = "overview-empty-body";
        internal const string EmptyStepsElementName = "overview-empty-steps";
        internal const string EmptyActionsElementName = "overview-empty-actions";
        internal const string RecheckAllButtonElementName = "overview-recheck-all";
        internal const string RecheckAllLabelElementName = "overview-recheck-all-label";
        internal const string CreateAssetButtonElementName = "overview-create-asset";
        internal const string ImportJsonButtonElementName = "overview-import-json";
        internal const string SelectAssetButtonElementName = "overview-select-asset";
        internal const string MultipleAssetsElementName = "overview-multiple-assets";
        internal const string MultipleAssetsTextElementName = "overview-multiple-assets-text";
        internal const string SwitchAssetButtonElementName = "overview-switch-asset";
        internal const string SectionMenuButtonElementName = "overview-section-menu";

        internal const int DraftTabIndex = 0;
        internal const int PublishedTabIndex = 1;

        /// <summary>Icon của nút "Kiểm lại tất cả" — 7.1 viết "[Refresh] Kiểm lại tất cả", cùng lối với "[Toolbar Plus] Thêm loại" của 7.2.</summary>
        private const string RecheckIconName = "Refresh";
        private const int RecheckIconSize = 12;

        /// <summary>Icon của HelpBox info [SD1 §1.3] và của nút ⋮ — cùng lưới icon [FD §2.12].</summary>
        private const string InfoIconName = "console.infoicon.sml";
        private const string SectionMenuIconName = "_Menu";
        private const int NoticeIconSize = 16;
        private const int SectionMenuIconSize = 12;

        /// <summary>Kiểu asset mà menu "Đổi…" liệt kê — chuỗi filter của <c>AssetDatabase.FindAssets</c>.</summary>
        private const string CalendarAssetSearchFilter = "t:LiveEventCalendarAsset";

        // <see cref="BodyElementName"/> KHÔNG có trong danh sách: chính nó là root của view, mà Q() chỉ tìm con — probe 9.3
        // và HubWindowTests Q trên view nên để tên root vào đây sẽ luôn báo thiếu.
        private static readonly IReadOnlyList<string> ElementNames = Array.AsReadOnly(new[]
        {
            EmptyElementName, DefaultBodyElementName, MetricsElementName, NeedsActionBodyElementName,
            FlowElementName, UpcomingElementName, UpcomingTabsElementName, FooterNoteElementName,
        });

        private readonly LiveOpsHubServices _services;

        private IHubHost _host;
        private VisualElement _root;
        private VisualElement _emptyElement;
        private VisualElement _defaultBodyElement;
        private ScrollView _scrollElement;
        private VisualElement _metricsElement;
        private Label _needsActionSubtitle;
        private Label _upcomingTitle;
        private Label _upcomingSubtitle;
        private LiveOpsTabStrip _upcomingTabs;
        private VisualElement _multipleAssetsElement;
        private Label _multipleAssetsText;
        private OverviewNeedsActionList _needsActionList;
        private OverviewPipelineFlow _pipelineFlow;
        private OverviewUpcomingTable _upcomingTable;
        private OverviewModel _model;
        private StyleSheet _sectionStyleSheet;
        private bool _showPublishedSource;
        private bool _isSubscribed;
        private int _objectPickerControlId;

        public OverviewSection(LiveOpsHubServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public string Id => LiveOpsHubSections.Ids.Overview;
        public string Title => LiveOpsHubStrings.ShellOverviewTitle;
        public string Subtitle => LiveOpsHubStrings.ShellOverviewSubtitle;
        public PipelineStage Stage => PipelineStage.Configure;
        public IReadOnlyList<string> RequiredElementNames => ElementNames;

        /// <summary>
        /// Services của cửa sổ (G-SESSION). Giữ nguyên tên thành viên mà màn giữ chỗ W2 đã có: <c>CalendarSessionTests</c>
        /// (quyền ghi của G-SESSION) đọc property này để chứng minh mọi màn nhận ĐÚNG services của phiên.
        /// </summary>
        internal LiveOpsHubServices Services => _services;

        /// <summary>Luôn Ok (6.4): mọi dấu đã có ở màn gốc, Tổng quan tự kết luận sẽ đếm đôi.</summary>
        public SectionHealth GetHealth()
        {
            return LiveOpsHubFindingRouting.OverviewHealth();
        }

        public void Bind(IHubHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public VisualElement CreateView()
        {
            VisualElement root = LoadLayout();
            _root = root;
            _emptyElement = root.Q(EmptyElementName);
            _defaultBodyElement = root.Q(DefaultBodyElementName);
            _scrollElement = root.Q<ScrollView>(ScrollElementName);
            ConfigureBodyScroll();
            _metricsElement = root.Q(MetricsElementName);
            _needsActionSubtitle = root.Q<Label>(NeedsActionSubtitleElementName);
            _upcomingTitle = root.Q<Label>(UpcomingTitleElementName);
            _upcomingSubtitle = root.Q<Label>(UpcomingSubtitleElementName);
            _upcomingTabs = root.Q<LiveOpsTabStrip>(UpcomingTabsElementName);
            _multipleAssetsElement = root.Q(MultipleAssetsElementName);
            _multipleAssetsText = root.Q<Label>(MultipleAssetsTextElementName);
            BuildMultipleAssetsNotice();

            root.Q<Label>(NeedsActionTitleElementName).text = LiveOpsHubStrings.OverviewNeedsActionCardTitle;
            root.Q<Label>(FlowTitleElementName).text = LiveOpsHubStrings.OverviewFlowCardTitle;
            root.Q<Label>(FlowSubtitleElementName).text = LiveOpsHubStrings.OverviewFlowCardSubtitle;
            root.Q<Label>(FooterNoteElementName).text = LiveOpsHubStrings.OverviewFooterNote;

            _needsActionList = new OverviewNeedsActionList(root.Q(NeedsActionBodyElementName), OnNeedsActionRowActivated, Navigate,
                DisabledReasonOf);
            _pipelineFlow = new OverviewPipelineFlow(root.Q(FlowElementName));
            _upcomingTable = new OverviewUpcomingTable(root.Q(UpcomingElementName));

            _upcomingTabs.Choices = LiveOpsHubStrings.OverviewUpcomingTabDraft + LiveOpsTabStrip.ChoiceSeparator
                + LiveOpsHubStrings.OverviewUpcomingTabPublished;
            _upcomingTabs.SelectedIndexChanged += OnUpcomingTabChanged;

            BuildEmptyState(root.Q(EmptyElementName));
            Subscribe();
            // Gỡ đăng ký khi cây rời panel (đổi màn, đóng cửa sổ): TrickleDown để bắt được cả lúc chính root này bị gỡ.
            root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel, TrickleDown.TrickleDown);
            root.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            root.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            Refresh();
            return root;
        }

        /// <summary>
        /// Hướng cuộn của thân màn (Q-W5-1). Đặt từ C# chứ không từ thuộc tính UXML: tên thuộc tính của ScrollView lệch giữa
        /// 2022.3 và 6000.6, còn property thì không — một đường đặt cho cả hai bản. Ngang <c>Hidden</c> vì bố cục đã co theo
        /// breakpoint <c>--narrow</c>, không bao giờ có chữ nằm ngoài bề ngang.
        /// </summary>
        private void ConfigureBodyScroll()
        {
            if (_scrollElement == null) return;
            _scrollElement.mode = ScrollViewMode.Vertical;
            _scrollElement.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _scrollElement.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        public void OnShown()
        {
            Refresh();
        }

        /// <summary>Nút chính của header: kiểm lại toàn bộ rồi bỏ cache health để rail đổi ngay, không đợi nhịp 3 giây.</summary>
        public void PopulateHeaderActions(VisualElement container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            // BẪY: `Button` là TextElement — nó tự đo theo `text` CHỈ KHI không có con. Thêm Image mà vẫn để `text` thì nút co
            // về min-width 54px và icon đè lên chữ. Nên chữ đi vào một Label con, và nút xếp hàng ngang bằng class riêng.
            Button recheck = new Button(StartCheck) { name = RecheckAllButtonElementName };
            recheck.AddToClassList(LiveOpsHubClassNames.Button);
            recheck.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            recheck.AddToClassList(LiveOpsHubClassNames.OverviewRecheckAllButton);
            // Nút này sống trong header của CỬA SỔ, ngoài thân màn — stylesheet của màn gắn ở thân không với tới nó, nên gắn
            // thêm vào chính nút. Không gán style inline ([FD §2.14] chỉ cho C# gán hình học suy từ dữ liệu).
            StyleSheet sheet = LoadSectionStyleSheet();
            if (sheet != null) recheck.styleSheets.Add(sheet);
            recheck.Add(LiveOpsHubIcons.CreateImage(RecheckIconName, RecheckIconSize));
            Label recheckLabel = new Label(LiveOpsHubStrings.OverviewRecheckAllButton)
            {
                name = RecheckAllLabelElementName,
            };
            recheckLabel.AddToClassList(LiveOpsHubClassNames.OverviewRecheckAllLabel);
            recheck.Add(recheckLabel);

            // Nút ⋮ đứng TRƯỚC nút chính: [FD §3.6] chốt "nút chính đứng cuối bên phải", việc ít dùng không được chen vào chỗ đó.
            Button menuButton = new Button(ShowSectionMenu)
            {
                name = SectionMenuButtonElementName,
                tooltip = LiveOpsHubStrings.OverviewSectionMenuTooltip,
            };
            menuButton.AddToClassList(LiveOpsHubClassNames.Button);
            if (sheet != null) menuButton.styleSheets.Add(sheet);
            menuButton.Add(LiveOpsHubIcons.CreateImage(SectionMenuIconName, SectionMenuIconSize));
            container.Add(menuButton);

            container.Add(recheck);
        }

        // ============================================================================================================ (b) nhiều asset

        /// <summary>Icon + nút "Đổi…" của HelpBox [SD1 §1.3] — UXML chỉ giữ khung và câu, icon theo skin nên phải dựng từ C#.</summary>
        private void BuildMultipleAssetsNotice()
        {
            if (_multipleAssetsElement == null) return;
            _multipleAssetsElement.Insert(0, LiveOpsHubIcons.CreateImage(InfoIconName, NoticeIconSize));
            Button switchButton = new Button(ShowSwitchAssetMenu)
            {
                name = SwitchAssetButtonElementName,
                text = LiveOpsHubStrings.OverviewSwitchAssetButton,
            };
            switchButton.AddToClassList(LiveOpsHubClassNames.Button);
            _multipleAssetsElement.Add(switchButton);
        }

        private void RefreshMultipleAssetsNotice()
        {
            if (_multipleAssetsElement == null) return;
            string notice = _model.MultipleAssetsNotice;
            _multipleAssetsText.text = notice;
            // Một asset thì không có gì để nói: HelpBox biến mất hẳn thay vì thành một dòng rỗng chiếm chỗ.
            _multipleAssetsElement.EnableInClassList(LiveOpsHubClassNames.OverviewHidden, notice.Length == 0);
        }

        /// <summary>
        /// Menu "Đổi…" liệt kê ĐƯỜNG DẪN từng asset lịch [SD1 §1.3] — tên file trùng nhau là chuyện thường, nên đường dẫn mới
        /// phân biệt được. Hỏi AssetDatabase tại đây chứ không qua phiên: phiên chỉ đếm số asset (chữ ký đóng băng PD-35).
        /// </summary>
        private void ShowSwitchAssetMenu()
        {
            GenericMenu menu = new GenericMenu();
            string currentPath = _services.Session.AssetPath;
            string[] guids = AssetDatabase.FindAssets(CalendarAssetSearchFilter);
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                if (string.IsNullOrEmpty(path)) continue;
                string assetPath = path;
                bool isCurrent = string.Equals(assetPath, currentPath, StringComparison.Ordinal);
                menu.AddItem(new GUIContent(assetPath), isCurrent, () => SelectAssetAtPath(assetPath));
            }
            menu.ShowAsContext();
        }

        private void SelectAssetAtPath(string assetPath)
        {
            LiveEventCalendarAsset asset = AssetDatabase.LoadAssetAtPath<LiveEventCalendarAsset>(assetPath);
            if (asset == null) return;
            if (_services.Session.TrySelectAsset(asset)) Refresh();
        }

        // ============================================================================================================ menu ⋮ của màn

        /// <summary>Menu ⋮ của section header (7.1): việc ít dùng, hôm nay đúng một mục "Đổi key remote…" (Q-1).</summary>
        private void ShowSectionMenu()
        {
            GenericMenu menu = new GenericMenu();
            GUIContent remoteKeyItem = new GUIContent(LiveOpsHubStrings.OverviewRemoteKeyMenuItem);
            // Chưa có asset thì không có field remoteConfigKey nào để đổi — mục xám, không mở popover trỏ vào hư không.
            if (_services.Session.Asset != null) menu.AddItem(remoteKeyItem, false, ShowRemoteKeyPopover);
            else menu.AddDisabledItem(remoteKeyItem);
            menu.ShowAsContext();
        }

        private void ShowRemoteKeyPopover()
        {
            Rect activator = _root != null ? _root.worldBound : new Rect();
            LiveOpsPopoverContent.ShowSingle(activator,
                new OverviewRemoteKeyPopover(_services.Session.Document.RemoteConfigKey, ApplyRemoteKey));
        }

        private void ApplyRemoteKey(string remoteConfigKey)
        {
            string undoName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewRemoteKeyUndoNameFormat, remoteConfigKey);
            LiveOpsHubEditOutcome outcome = _services.Session.Apply(new SetRemoteConfigKeyEdit(remoteConfigKey), undoName);
            if (!outcome.Applied)
            {
                _services.Bus.ShowToast(LiveOpsToastModel.Info(outcome.FailureText));
                return;
            }
            _services.Bus.ShowToast(LiveOpsToastModel.ForEdit(undoName, outcome.UndoGroup));
            _services.Bus.InvalidateHealth();
            Refresh();
        }

        public string CaptureViewState()
        {
            return JsonUtility.ToJson(new OverviewViewState { showPublishedSource = _showPublishedSource });
        }

        /// <summary>JSON rỗng hoặc hỏng → mặc định (tab Nháp): trạng thái view của bản cũ không được làm sập màn.</summary>
        public void RestoreViewState(string viewStateJson)
        {
            _showPublishedSource = false;
            if (string.IsNullOrEmpty(viewStateJson)) return;
            try
            {
                OverviewViewState state = JsonUtility.FromJson<OverviewViewState>(viewStateJson);
                if (state != null) _showPublishedSource = state.showPublishedSource;
            }
            catch (ArgumentException)
            {
                _showPublishedSource = false;
            }
        }

        // ============================================================================================================ dựng lại

        /// <summary>Stylesheet của màn; nạp lại là tra AssetDatabase đã cache, nhưng giữ một tham chiếu cho chỗ dùng thứ hai (header).</summary>
        private StyleSheet LoadSectionStyleSheet()
        {
            if (_sectionStyleSheet == null) _sectionStyleSheet = _services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.OverviewSectionUss);
            return _sectionStyleSheet;
        }

        private VisualElement LoadLayout()
        {
            VisualTreeAsset tree = _services.LayoutLoader.LoadVisualTree(LiveOpsHubPaths.OverviewSectionUxml);
            if (tree == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.OverviewMissingLayoutFormat, LiveOpsHubPaths.OverviewSectionUxml));
            }
            VisualElement clone = tree.CloneTree();
            // CloneTree bọc thêm một TemplateContainer: trả đúng thân màn để tên element và class của màn nằm ngay dưới thân.
            VisualElement body = clone.Q(BodyElementName) ?? clone;
            // Stylesheet gắn vào CHÍNH element trả về, không vào TemplateContainer: cái bọc bị bỏ lại nên sheet gắn ở đó không
            // bao giờ vào panel — màn vẫn dựng nhưng mất sạch số đo (cột 96/150, thân flow 48, cột bảng 170/250/140/130).
            StyleSheet sheet = LoadSectionStyleSheet();
            if (sheet != null) body.styleSheets.Add(sheet);
            return body;
        }

        private void Subscribe()
        {
            if (_isSubscribed) return;
            _services.Session.DocumentChanged += Refresh;
            _services.Session.CheckChanged += Refresh;
            // Ghi dấu / gỡ dấu / đổi bản so KHÔNG phát sự kiện riêng (LiveOpsHubCalendarSession.NotifyPublishStateChanged chỉ
            // tăng StateVersion, và chữ ký đó đóng băng theo PD-35). Bus.InvalidateHealth là tín hiệu "có thứ vật chất vừa đổi"
            // của cả hub, nên nghe nó thay vì để metric ĐÃ ĐĂNG, hàng "Chưa có dấu đã đăng" và tab "Bản đã đăng" giữ số cũ.
            _services.Bus.HealthInvalidated += Refresh;
            _isSubscribed = true;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            if (!_isSubscribed) return;
            _services.Session.DocumentChanged -= Refresh;
            _services.Session.CheckChanged -= Refresh;
            _services.Bus.HealthInvalidated -= Refresh;
            _isSubscribed = false;
        }

        private void Refresh()
        {
            if (_root == null) return;
            ExportGateState gate = _services.Session.Asset != null
                ? _services.Session.EvaluateExportGate(_services.JsonReadBack, _services.Format)
                : null;
            _model = OverviewModel.Build(_services.Session, CollectSectionHealths(), gate,
                _services.Actions.CanPasteRunningJson, _services.Actions.CanImportRunningJson, _services.Format);

            bool noAsset = _model.BodyState == OverviewModel.BodyStateNoAsset;
            // Ẩn bằng class (display:none) chứ không style inline: [FD §2.14] chỉ cho C# gán hình học suy từ dữ liệu.
            _emptyElement.EnableInClassList(LiveOpsHubClassNames.OverviewHidden, !noAsset);
            _defaultBodyElement.EnableInClassList(LiveOpsHubClassNames.OverviewHidden, noAsset);
            // Khung cuộn ẩn cùng thân: để lại một ScrollView rỗng flex-grow 1 là empty (a) bị đẩy lên mép trên [SD1 §1.2].
            if (_scrollElement != null) _scrollElement.EnableInClassList(LiveOpsHubClassNames.OverviewHidden, noAsset);
            if (noAsset) return;

            RefreshMultipleAssetsNotice();
            RebuildMetrics();
            _needsActionSubtitle.text = _model.BodyState == OverviewModel.BodyStateNoBlockers
                ? LiveOpsHubStrings.OverviewNeedsActionCardNoBlockersSubtitle
                : LiveOpsHubStrings.OverviewNeedsActionCardSubtitle;
            _needsActionList.Rebuild(_model.NeedsActionRows, _model.BodyState == OverviewModel.BodyStateNoBlockers,
                _model.NotMeasuredRuleCount);
            _pipelineFlow.Rebuild(_model.FlowNodes);
            RefreshUpcoming();
        }

        private IReadOnlyList<SectionHealth> CollectSectionHealths()
        {
            List<SectionHealth> healths = new List<SectionHealth>();
            if (_host == null) return healths;
            foreach (IHubSection section in _host.Sections) healths.Add(section.GetHealth());
            return healths;
        }

        private void RebuildMetrics()
        {
            _metricsElement.Clear();
            int index = 0;
            foreach (OverviewMetric metric in _model.Metrics)
            {
                _metricsElement.Add(BuildMetric(metric, index == _model.Metrics.Count - 1));
                index++;
            }
        }

        private static VisualElement BuildMetric(OverviewMetric metric, bool isLast)
        {
            VisualElement element = new VisualElement { tooltip = metric.Tooltip };
            element.AddToClassList(LiveOpsHubClassNames.Metric);
            if (isLast) element.AddToClassList(LiveOpsHubClassNames.OverviewMetricLast);

            Label caption = new Label(metric.Caption);
            caption.AddToClassList(LiveOpsHubClassNames.MetricCaption);
            element.Add(caption);

            VisualElement valueRow = new VisualElement();
            valueRow.AddToClassList(LiveOpsHubClassNames.OverviewMetricValueRow);
            if (metric.Value == null)
            {
                // Vòng rỗng = "chưa đo được", KHÔNG phải số 0 ([FD §2.4]); câu lý do nằm ngay dòng dưới.
                LiveOpsStateMark mark = new LiveOpsStateMark();
                mark.Size = LiveOpsStateMark.MarkSize.Large;
                mark.SetHealth(HealthState.NotMeasured);
                valueRow.Add(mark);
            }
            else
            {
                Label value = new Label(metric.Value);
                value.AddToClassList(LiveOpsHubClassNames.MetricValue);
                if (metric.ValueState.HasValue) LiveOpsHubStyle.SetStateText(value, metric.ValueState.Value);
                valueRow.Add(value);
                if (metric.Unit.Length > 0)
                {
                    Label unit = new Label(metric.Unit);
                    unit.AddToClassList(LiveOpsHubClassNames.MetricUnit);
                    valueRow.Add(unit);
                }
            }
            element.Add(valueRow);

            VisualElement footRow = new VisualElement();
            footRow.AddToClassList(LiveOpsHubClassNames.OverviewMetricFootRow);
            if (metric.IsChecking)
            {
                LiveOpsSpinner spinner = new LiveOpsSpinner();
                spinner.Start();
                footRow.Add(spinner);
            }
            Label foot = new Label(metric.Foot);
            foot.AddToClassList(LiveOpsHubClassNames.MetricFoot);
            footRow.Add(foot);
            element.Add(footRow);
            return element;
        }

        private void RefreshUpcoming()
        {
            bool canUsePublished = _model.HasPublishedStamp;
            if (!canUsePublished) _showPublishedSource = false;
            _upcomingTabs.SetChoiceEnabled(PublishedTabIndex, canUsePublished,
                LiveOpsHubStrings.OverviewUpcomingTabPublishedDisabledReason);
            _upcomingTabs.SetSelectedIndexWithoutNotify(_showPublishedSource ? PublishedTabIndex : DraftTabIndex);
            _upcomingTitle.text = _showPublishedSource
                ? LiveOpsHubStrings.OverviewUpcomingCardTitlePublished
                : LiveOpsHubStrings.OverviewUpcomingCardTitleDraft;
            _upcomingSubtitle.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewUpcomingRangeFormat,
                _services.Format.ShortDateTime(_model.RangeStartUtc), _services.Format.ShortDateTime(_model.RangeEndUtc));
            _upcomingTable.Rebuild(_model.UpcomingRows(_showPublishedSource));
        }

        private void OnUpcomingTabChanged(int index)
        {
            _showPublishedSource = index == PublishedTabIndex;
            if (_model != null) RefreshUpcoming();
        }

        // ============================================================================================================ hành động

        private void OnNeedsActionRowActivated(OverviewNeedsActionRow row)
        {
            switch (row.Action)
            {
                case OverviewRowAction.Navigate:
                    Navigate(row.Navigation);
                    break;
                case OverviewRowAction.StartCheck:
                    StartCheck();
                    break;
                case OverviewRowAction.PasteRunningJson:
                    _services.Actions.PasteRunningJson(_root != null ? _root.worldBound : new Rect());
                    break;
            }
        }

        /// <summary>
        /// Lý do in cạnh nút khoá của một hàng. Model chỉ biết "được hay không" (cờ của mục 3) — câu vì sao là của chính port,
        /// nên view hỏi port thay vì để model chép lại một câu nó không sở hữu.
        /// </summary>
        private string DisabledReasonOf(OverviewNeedsActionRow row)
        {
            if (row.DisabledReason.Length > 0) return row.DisabledReason;
            return row.Action == OverviewRowAction.PasteRunningJson
                ? _services.Actions.PasteRunningJsonUnavailableReason
                : string.Empty;
        }

        private void Navigate(LiveOpsHubNavigation navigation)
        {
            if (navigation == null) return;
            _services.Bus.Navigate(navigation);
        }

        private void StartCheck()
        {
            _services.Session.StartCheck();
            // Kiểm lại xong health của MỌI màn đổi: bỏ cache 3 giây để rail không hiện số cũ thêm một nhịp.
            LiveOpsHealthThrottle.InvalidateAll();
            _services.Bus.InvalidateHealth();
        }

        // ============================================================================================================ (a) chưa có asset

        private void BuildEmptyState(VisualElement empty)
        {
            empty.Q<Label>(EmptyTitleElementName).text = LiveOpsHubStrings.OverviewNoAssetTitle;
            empty.Q<Label>(EmptyBodyElementName).text = LiveOpsHubStrings.OverviewNoAssetBody;

            VisualElement steps = empty.Q(EmptyStepsElementName);
            steps.Clear();
            steps.Add(BuildStep(1, LiveOpsHubStrings.OverviewNoAssetStepCreate));
            steps.Add(BuildStep(2, LiveOpsHubStrings.OverviewNoAssetStepDeclareTypes));
            steps.Add(BuildStep(3, LiveOpsHubStrings.OverviewNoAssetStepFirstCheck));

            VisualElement actions = empty.Q(EmptyActionsElementName);
            actions.Clear();

            Button create = new Button(CreateCalendarAsset)
            {
                name = CreateAssetButtonElementName,
                text = LiveOpsHubStrings.OverviewCreateAssetButton,
            };
            create.AddToClassList(LiveOpsHubClassNames.Button);
            create.AddToClassList(LiveOpsHubClassNames.ButtonFirst);
            create.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            actions.Add(create);

            Button import = new Button(ImportRunningJson)
            {
                name = ImportJsonButtonElementName,
                text = LiveOpsHubStrings.OverviewImportRunningJsonButton,
            };
            import.AddToClassList(LiveOpsHubClassNames.Button);
            LiveOpsButtonSlot importSlot = new LiveOpsButtonSlot(import);
            LiveOpsHubStyle.SetEnabledWithReason(importSlot, _services.Actions.CanImportRunningJson,
                _services.Actions.ImportRunningJsonUnavailableReason);
            actions.Add(importSlot);

            Button select = new Button(ShowAssetPicker)
            {
                name = SelectAssetButtonElementName,
                text = LiveOpsHubStrings.OverviewSelectAssetButton,
            };
            select.AddToClassList(LiveOpsHubClassNames.Button);
            actions.Add(select);
        }

        private static VisualElement BuildStep(int number, string label)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.OverviewStepRow);
            Label marker = new Label(number.ToString(CultureInfo.InvariantCulture));
            marker.AddToClassList(LiveOpsHubClassNames.OverviewStepMarker);
            row.Add(marker);
            Label text = new Label(label);
            text.AddToClassList(LiveOpsHubClassNames.OverviewStepLabel);
            row.Add(text);
            return row;
        }

        private void CreateCalendarAsset()
        {
            string path = _services.FileDialog.SaveFile(LiveOpsHubStrings.OverviewCreateAssetDialogTitle,
                LiveOpsHubPaths.DefaultCalendarAssetDirectory, LiveOpsHubPaths.DefaultCalendarAssetName,
                LiveOpsHubPaths.CalendarAssetExtension);
            if (string.IsNullOrEmpty(path)) return;
            if (_services.Session.TryCreateAsset(path))
            {
                Refresh();
                return;
            }
            // Hộp lưu đã trả một đường dẫn mà tạo vẫn hỏng (ngoài Assets/, đã có file): nói ra, không im lặng về màn trống.
            _services.Bus.ShowToast(LiveOpsToastModel.Info(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.OverviewCreateAssetFailedFormat, path)));
        }

        private void ImportRunningJson()
        {
            if (!_services.Actions.CanImportRunningJson) return;
            _services.Actions.ImportRunningJsonIntoNewAsset(_root != null ? _root.worldBound : new Rect());
        }

        private void ShowAssetPicker()
        {
            _objectPickerControlId = GUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.ShowObjectPicker<LiveEventCalendarAsset>(null, false, string.Empty, _objectPickerControlId);
        }

        /// <summary>Hộp chọn asset gửi lệnh qua hệ lệnh của Editor — phải nhận ValidateCommandEvent thì ExecuteCommandEvent mới tới.</summary>
        private void OnValidateCommand(ValidateCommandEvent commandEvent)
        {
            if (!IsObjectPickerCommand(commandEvent.commandName)) return;
            commandEvent.StopPropagation();
        }

        private void OnExecuteCommand(ExecuteCommandEvent commandEvent)
        {
            if (!IsObjectPickerCommand(commandEvent.commandName)) return;
            if (EditorGUIUtility.GetObjectPickerControlID() != _objectPickerControlId) return;
            commandEvent.StopPropagation();
            LiveEventCalendarAsset asset = EditorGUIUtility.GetObjectPickerObject() as LiveEventCalendarAsset;
            if (asset == null) return;
            if (_services.Session.TrySelectAsset(asset)) Refresh();
        }

        private static bool IsObjectPickerCommand(string commandName)
        {
            return string.Equals(commandName, "ObjectSelectorUpdated", StringComparison.Ordinal)
                || string.Equals(commandName, "ObjectSelectorClosed", StringComparison.Ordinal);
        }

        /// <summary>Trạng thái view sống qua domain reload: đúng một cờ (tab nguồn của bảng 7 ngày tới).</summary>
        [Serializable]
        private sealed class OverviewViewState
        {
            public bool showPublishedSource;
        }
    }
}
