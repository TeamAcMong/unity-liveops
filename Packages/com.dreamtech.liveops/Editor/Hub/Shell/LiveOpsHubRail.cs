using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// View rail 196 px ([FD §3.5], 8.2): đọc <see cref="LiveOpsHubRailModel"/> và gắn class, không tự quyết gì. Dựng lại hàng CHỈ
    /// khi model đổi (chữ ký chuỗi) — cửa sổ làm mới health mỗi giây, dựng lại mỗi giây thì mất focus bàn phím và hover.
    /// Di chuyển bằng <c>NavigationMoveEvent</c>, mở bằng <c>NavigationSubmitEvent</c> như ListView gốc; focus theo vùng bằng
    /// <c>FocusIn/FocusOut</c> vì USS không có <c>:focus-within</c>.
    /// <para>
    /// Cửa sổ dưới 900 px ([FD §3.7], mục 12 I-8): rail còn 36 px — một ô icon cho MỖI TẦNG (36×32, icon 16, dấu 7 px góc phải
    /// trên), bấm ô mở <see cref="LiveOpsHubNarrowRailMenu"/>, đáy là ô icon lỗi của ô chặn. Cả hai hình dạng dựng SẴN trong cây
    /// và USS chọn bằng class trên root: đổi bề rộng cửa sổ không được dựng lại rail (mất focus, mất hover). Nút ‹› ghim mở
    /// lưu vào EditorPrefs nên lựa chọn sống qua domain reload và qua lần mở sau.
    /// </para>
    /// </summary>
    internal sealed class LiveOpsHubRail
    {
        /// <summary>Khoá EditorPrefs của nút ‹› ([FD §3.7] để trống tên khoá; 10.3 chốt đúng chuỗi này).</summary>
        internal const string PinnedOpenPreferenceKey = "LiveOpsHub.RailPinnedOpen";

        internal const string PinElementName = "hub-rail-pin";
        internal const string NarrowElementName = "hub-rail-narrow";
        internal const string NarrowBlockerElementName = "hub-rail-narrow-blocker";

        private const int NarrowIconSize = 16;

        private readonly VisualElement _hubRoot;
        private readonly VisualElement _railElement;
        private readonly Label _caption;
        private readonly ScrollView _scroll;
        private readonly VisualElement _blockerHost;
        private readonly Action<LiveOpsHubNavigation> _navigate;
        private readonly Dictionary<string, VisualElement> _rowsBySectionId = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
        private readonly List<VisualElement> _sectionRows = new List<VisualElement>();
        private readonly Dictionary<VisualElement, string> _sectionIdsByRow = new Dictionary<VisualElement, string>();
        private readonly VisualElement _narrowHost;
        private readonly List<VisualElement> _narrowCells = new List<VisualElement>();
        private readonly Button _pinButton;
        private bool _isPinnedOpen;
        private string _signature = string.Empty;
        private string _activeSectionId = string.Empty;
        private LiveOpsHubRailModel _model;

        public LiveOpsHubRail(VisualElement hubRoot, Action<LiveOpsHubNavigation> navigate)
        {
            if (hubRoot == null) throw new ArgumentNullException(nameof(hubRoot));
            _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
            _hubRoot = hubRoot;
            _railElement = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.Rail);
            _caption = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.RailCaption);
            _scroll = hubRoot.Q<ScrollView>(LiveOpsHubPaths.ShellElementNames.RailScroll);
            _blockerHost = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.RailBlockerHost);
            _caption.text = LiveOpsHubStrings.ShellRailCaption;

            // Hai phần dưới dựng bằng C# thay vì khai trong LiveOpsHub.uxml: cùng lối với dấu trạng thái của chip header — khung
            // UXML không phải biết tới control dual-path, và danh sách element sống còn của khung không đổi theo một breakpoint.
            _pinButton = new Button(TogglePinnedOpen)
            {
                name = PinElementName,
                tooltip = LiveOpsHubStrings.ShellRailPinTooltip,
            };
            _pinButton.AddToClassList(LiveOpsHubClassNames.RailPin);
            _pinButton.Add(new LiveOpsChevron(LiveOpsChevron.ChevronDirection.Left));
            _pinButton.Add(new LiveOpsChevron(LiveOpsChevron.ChevronDirection.Right));
            _railElement.Insert(0, _pinButton);

            _narrowHost = new VisualElement { name = NarrowElementName };
            _narrowHost.AddToClassList(LiveOpsHubClassNames.RailNarrow);
            _railElement.Add(_narrowHost);

            ApplyPinnedOpen(EditorPrefs.GetBool(PinnedOpenPreferenceKey, false));

            _railElement.RegisterCallback<FocusInEvent>(OnFocusIn);
            _railElement.RegisterCallback<FocusOutEvent>(OnFocusOut);
            _railElement.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _railElement.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }

        public VisualElement Element => _railElement;

        /// <summary>Model đang vẽ (null trước lần <see cref="Build"/> đầu).</summary>
        internal LiveOpsHubRailModel Model => _model;

        /// <summary>Ô chặn đáy rail; null khi model không có ô chặn.</summary>
        internal VisualElement BlockerElement { get; private set; }

        /// <summary>Số lần thật sự dựng lại hàng — test chứng minh làm mới health với model không đổi không dựng lại.</summary>
        internal int RebuildCount { get; private set; }

        internal IReadOnlyList<VisualElement> SectionRows => _sectionRows;

        /// <summary>Ô icon tầng của rail 36 px, theo đúng thứ tự tầng; rỗng trước lần <see cref="Build"/> đầu.</summary>
        internal IReadOnlyList<VisualElement> NarrowCells => _narrowCells;

        /// <summary>Ô icon lỗi ở đáy rail thu gọn; null khi model không có ô chặn.</summary>
        internal VisualElement NarrowBlockerElement { get; private set; }

        internal Button PinButton => _pinButton;

        /// <summary>Người dùng ghim rail mở ở cửa sổ hẹp (nút ‹›, nhớ trong EditorPrefs).</summary>
        internal bool IsPinnedOpen => _isPinnedOpen;

        /// <summary>Đổi trạng thái ghim và ghi EditorPrefs — đường duy nhất, để class trên root và khoá không bao giờ lệch nhau.</summary>
        internal void TogglePinnedOpen()
        {
            SetPinnedOpen(!_isPinnedOpen);
        }

        internal void SetPinnedOpen(bool pinnedOpen)
        {
            EditorPrefs.SetBool(PinnedOpenPreferenceKey, pinnedOpen);
            ApplyPinnedOpen(pinnedOpen);
        }

        private void ApplyPinnedOpen(bool pinnedOpen)
        {
            _isPinnedOpen = pinnedOpen;
            // Class nằm trên ROOT chứ không trên rail: nó phải thắng được .liveops-hub--narrow, mà class đó cũng ở root.
            _hubRoot.EnableInClassList(LiveOpsHubClassNames.RailPinnedOpen, pinnedOpen);
        }

        internal VisualElement GetRow(string sectionId)
        {
            return sectionId != null && _rowsBySectionId.TryGetValue(sectionId, out VisualElement row) ? row : null;
        }

        /// <summary>Vẽ model; bỏ qua khi model giống lần trước. Giữ focus bàn phím trên cùng màn sau khi dựng lại.</summary>
        public void Build(LiveOpsHubRailModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            string signature = SignatureOf(model);
            _model = model;
            if (string.Equals(signature, _signature, StringComparison.Ordinal)) return;
            _signature = signature;

            string focusedSectionId = FocusedSectionId();
            _scroll.Clear();
            _rowsBySectionId.Clear();
            _sectionIdsByRow.Clear();
            _sectionRows.Clear();
            RebuildCount++;

            for (int stageIndex = 0; stageIndex < model.Stages.Count; stageIndex++)
            {
                LiveOpsHubRailStageRow stage = model.Stages[stageIndex];
                bool isFirstStage = stageIndex == 0;
                bool isLastStage = stageIndex == model.Stages.Count - 1;
                _scroll.Add(CreateStageRow(stage, isFirstStage, isLastStage && stage.Rows.Count == 0));
                for (int rowIndex = 0; rowIndex < stage.Rows.Count; rowIndex++)
                {
                    bool isLastRow = isLastStage && rowIndex == stage.Rows.Count - 1;
                    VisualElement row = CreateSectionRow(stage, stage.Rows[rowIndex], isLastRow);
                    _scroll.Add(row);
                }
            }

            _blockerHost.Clear();
            BlockerElement = model.HasBlocker ? CreateBlocker(model) : null;
            if (BlockerElement != null) _blockerHost.Add(BlockerElement);

            BuildNarrow(model);

            SetActiveSection(_activeSectionId);
            VisualElement toFocus = GetRow(focusedSectionId);
            toFocus?.Focus();
        }

        public void SetActiveSection(string sectionId)
        {
            _activeSectionId = sectionId ?? string.Empty;
            foreach (KeyValuePair<string, VisualElement> pair in _rowsBySectionId)
            {
                pair.Value.EnableInClassList(LiveOpsHubClassNames.RailRowActive, string.Equals(pair.Key, _activeSectionId, StringComparison.Ordinal));
            }
            ApplyNarrowActiveCell();
        }

        /// <summary>
        /// Hình dạng 36 px ([FD §3.7]): một ô cho mỗi TẦNG (không phải mỗi màn — 36 px không đủ cho 6 hàng có nghĩa), dấu trạng
        /// thái của tầng ở góc phải trên, và ô icon lỗi ở đáy khi có ô chặn.
        /// </summary>
        private void BuildNarrow(LiveOpsHubRailModel model)
        {
            _narrowHost.Clear();
            _narrowCells.Clear();
            NarrowBlockerElement = null;

            for (int stageIndex = 0; stageIndex < model.Stages.Count; stageIndex++)
            {
                LiveOpsHubRailStageRow stage = model.Stages[stageIndex];
                VisualElement cell = new VisualElement { focusable = true, tabIndex = 0, tooltip = stage.Caption };
                cell.AddToClassList(LiveOpsHubClassNames.RailNarrowCell);
                cell.Add(LiveOpsHubIcons.CreateImage(PipelineStages.IconNameOf(stage.Stage), NarrowIconSize));

                LiveOpsStateMark mark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small, pickingMode = PickingMode.Ignore };
                mark.SetHealth(stage.State);
                mark.AddToClassList(LiveOpsHubClassNames.RailNarrowMark);
                cell.Add(mark);

                LiveOpsHubRailStageRow captured = stage;
                cell.RegisterCallback<ClickEvent>(clickEvent => OpenStageMenu(captured));
                cell.RegisterCallback<NavigationSubmitEvent>(submitEvent =>
                {
                    OpenStageMenu(captured);
                    submitEvent.StopPropagation();
                });
                _narrowHost.Add(cell);
                _narrowCells.Add(cell);
            }

            if (!model.HasBlocker) return;
            VisualElement blockerCell = new VisualElement
            {
                name = NarrowBlockerElementName,
                focusable = true,
                tabIndex = 0,
                tooltip = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellRailNarrowBlockerTooltipFormat,
                    model.BlockerTitle, model.BlockerDetail),
            };
            blockerCell.AddToClassList(LiveOpsHubClassNames.RailNarrowCell);
            blockerCell.AddToClassList(LiveOpsHubClassNames.RailNarrowCellBlocker);
            LiveOpsStateMark blockerMark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Regular, pickingMode = PickingMode.Ignore };
            blockerMark.SetHealth(HealthState.Blocked);
            blockerCell.Add(blockerMark);
            LiveOpsHubNavigation navigation = model.BlockerNavigation;
            blockerCell.RegisterCallback<ClickEvent>(clickEvent => _navigate(navigation));
            blockerCell.RegisterCallback<NavigationSubmitEvent>(submitEvent =>
            {
                _navigate(navigation);
                submitEvent.StopPropagation();
            });
            _narrowHost.Add(blockerCell);
            NarrowBlockerElement = blockerCell;
        }

        private void OpenStageMenu(LiveOpsHubRailStageRow stage)
        {
            int stageIndex = IndexOfStage(stage);
            Rect activator = stageIndex >= 0 && stageIndex < _narrowCells.Count ? _narrowCells[stageIndex].worldBound : _railElement.worldBound;
            LiveOpsHubNarrowRailMenu.Show(activator, stage, _activeSectionId, sectionId => _navigate(LiveOpsHubNavigation.To(sectionId)));
        }

        private int IndexOfStage(LiveOpsHubRailStageRow stage)
        {
            if (_model == null) return -1;
            for (int index = 0; index < _model.Stages.Count; index++)
            {
                if (ReferenceEquals(_model.Stages[index], stage)) return index;
            }
            return -1;
        }

        /// <summary>Ô tầng đang chứa màn đang mở — rail thu gọn tô ô đó, thay cho hàng active của rail 196 px.</summary>
        private void ApplyNarrowActiveCell()
        {
            if (_model == null) return;
            for (int index = 0; index < _narrowCells.Count && index < _model.Stages.Count; index++)
            {
                bool isActive = false;
                IReadOnlyList<LiveOpsHubRailSectionRow> rows = _model.Stages[index].Rows;
                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    if (!string.Equals(rows[rowIndex].SectionId, _activeSectionId, StringComparison.Ordinal)) continue;
                    isActive = true;
                    break;
                }
                _narrowCells[index].EnableInClassList(LiveOpsHubClassNames.RailNarrowCellActive, isActive);
            }
        }

        public void Dispose()
        {
            _railElement.UnregisterCallback<FocusInEvent>(OnFocusIn);
            _railElement.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            _railElement.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _railElement.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }

        private VisualElement CreateStageRow(LiveOpsHubRailStageRow stage, bool isFirstStage, bool isLastElement)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.RailStageRow);
            row.tooltip = stage.BadgeTooltip;

            VisualElement gutter = CreateGutter(stage.IsConnectorAboveDead, stage.IsConnectorDead, isFirstStage, isLastElement);
            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.SetHealth(stage.State);
            gutter.Add(mark);
            row.Add(gutter);

            Label caption = new Label(stage.Caption);
            caption.AddToClassList(LiveOpsHubClassNames.RailStageLabel);
            row.Add(caption);

            if (!string.IsNullOrEmpty(stage.Badge))
            {
                Label badge = new Label(stage.Badge);
                badge.AddToClassList(LiveOpsHubClassNames.RailBadge);
                badge.EnableInClassList(LiveOpsHubClassNames.RailBadgeStale, stage.IsBadgeStale);
                LiveOpsHubStyle.SetStateText(badge, stage.BadgeState);
                row.Add(badge);
            }
            return row;
        }

        private VisualElement CreateSectionRow(LiveOpsHubRailStageRow stage, LiveOpsHubRailSectionRow model, bool isLastElement)
        {
            VisualElement row = new VisualElement { focusable = true, tabIndex = 0 };
            row.AddToClassList(LiveOpsHubClassNames.RailRow);
            row.tooltip = model.Tooltip;
            row.Add(CreateGutter(stage.IsConnectorDead, stage.IsConnectorDead, false, isLastElement));

            Label label = new Label(model.Title);
            label.AddToClassList(LiveOpsHubClassNames.RailRowLabel);
            row.Add(label);

            LiveOpsStateMark mark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            mark.SetHealth(model.State);
            mark.AddToClassList(LiveOpsHubClassNames.RailRowMark);
            mark.EnableInClassList(LiveOpsHubClassNames.RailRowMarkHidden, model.IsMarkHidden);
            row.Add(mark);

            string sectionId = model.SectionId;
            row.RegisterCallback<ClickEvent>(clickEvent => _navigate(LiveOpsHubNavigation.To(sectionId)));
            _rowsBySectionId[sectionId] = row;
            _sectionIdsByRow[row] = sectionId;
            _sectionRows.Add(row);
            return row;
        }

        private static VisualElement CreateGutter(bool isTopDead, bool isBottomDead, bool hideTop, bool hideBottom)
        {
            VisualElement gutter = new VisualElement { pickingMode = PickingMode.Ignore };
            gutter.AddToClassList(LiveOpsHubClassNames.Gutter);
            gutter.Add(CreateGutterLine(LiveOpsHubClassNames.GutterLineTop, isTopDead, hideTop));
            gutter.Add(CreateGutterLine(LiveOpsHubClassNames.GutterLineBottom, isBottomDead, hideBottom));
            return gutter;
        }

        private static VisualElement CreateGutterLine(string halfClass, bool isDead, bool isHidden)
        {
            VisualElement line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList(LiveOpsHubClassNames.GutterLine);
            line.AddToClassList(halfClass);
            line.EnableInClassList(LiveOpsHubClassNames.GutterLineDead, isDead);
            line.EnableInClassList(LiveOpsHubClassNames.GutterLineHidden, isHidden);
            return line;
        }

        private VisualElement CreateBlocker(LiveOpsHubRailModel model)
        {
            VisualElement blocker = new VisualElement { focusable = true, tabIndex = 0, tooltip = model.BlockerDetail };
            blocker.AddToClassList(LiveOpsHubClassNames.RailBlocker);

            VisualElement head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.AddToClassList(LiveOpsHubClassNames.RailBlockerHead);
            LiveOpsStateMark mark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            mark.SetHealth(HealthState.Blocked);
            head.Add(mark);
            Label title = new Label(model.BlockerTitle);
            title.AddToClassList(LiveOpsHubClassNames.RailBlockerTitle);
            LiveOpsHubStyle.SetStateText(title, HealthState.Blocked);
            head.Add(title);
            head.Add(new LiveOpsChevron(LiveOpsChevron.ChevronDirection.Right));
            blocker.Add(head);

            Label detail = new Label(model.BlockerDetail) { pickingMode = PickingMode.Ignore };
            detail.AddToClassList(LiveOpsHubClassNames.RailBlockerDetail);
            blocker.Add(detail);

            LiveOpsHubNavigation navigation = model.BlockerNavigation;
            blocker.RegisterCallback<ClickEvent>(clickEvent => _navigate(navigation));
            return blocker;
        }

        private void OnFocusIn(FocusInEvent focusEvent)
        {
            _railElement.AddToClassList(LiveOpsHubClassNames.RailHasFocus);
        }

        private void OnFocusOut(FocusOutEvent focusEvent)
        {
            // Focus chuyển giữa hai hàng trong rail cũng bắn FocusOut trước FocusIn — chỉ tắt class khi focus thật sự rời rail.
            VisualElement next = focusEvent.relatedTarget as VisualElement;
            if (next != null && _railElement.Contains(next)) return;
            _railElement.RemoveFromClassList(LiveOpsHubClassNames.RailHasFocus);
        }

        private void OnNavigationMove(NavigationMoveEvent moveEvent)
        {
            int direction;
            switch (moveEvent.direction)
            {
                case NavigationMoveEvent.Direction.Up: direction = -1; break;
                case NavigationMoveEvent.Direction.Down: direction = 1; break;
                default: return;
            }

            List<VisualElement> focusables = FocusOrder();
            int current = PositionOf(focusables, moveEvent.target as VisualElement);
            if (current < 0) return;
            int next = current + direction;
            // Đầu/cuối rail: giữ focus tại chỗ và vẫn chặn sự kiện, để mũi tên không nhảy sang vùng nội dung ngoài ý người dùng.
            if (next >= 0 && next < focusables.Count) focusables[next].Focus();
            moveEvent.StopPropagation();
        }

        private void OnNavigationSubmit(NavigationSubmitEvent submitEvent)
        {
            VisualElement target = submitEvent.target as VisualElement;
            if (target == null) return;
            if (_sectionIdsByRow.TryGetValue(target, out string sectionId))
            {
                _navigate(LiveOpsHubNavigation.To(sectionId));
                submitEvent.StopPropagation();
                return;
            }
            if (target == BlockerElement && _model != null && _model.HasBlocker)
            {
                _navigate(_model.BlockerNavigation);
                submitEvent.StopPropagation();
            }
        }

        private List<VisualElement> FocusOrder()
        {
            List<VisualElement> order = new List<VisualElement>(_sectionRows);
            if (BlockerElement != null) order.Add(BlockerElement);
            return order;
        }

        private static int PositionOf(List<VisualElement> elements, VisualElement element)
        {
            for (int index = 0; index < elements.Count; index++)
            {
                if (elements[index] == element) return index;
            }
            return -1;
        }

        private string FocusedSectionId()
        {
            VisualElement focused = _railElement.panel?.focusController?.focusedElement as VisualElement;
            return focused != null && _sectionIdsByRow.TryGetValue(focused, out string sectionId) ? sectionId : null;
        }

        private static string SignatureOf(LiveOpsHubRailModel model)
        {
            StringBuilder builder = new StringBuilder();
            foreach (LiveOpsHubRailStageRow stage in model.Stages)
            {
                builder.Append((int)stage.Stage).Append('|').Append((int)stage.State).Append('|').Append(stage.Badge).Append('|')
                    .Append((int)stage.BadgeState).Append('|').Append(stage.IsBadgeStale).Append('|').Append(stage.BadgeTooltip).Append('|')
                    .Append(stage.IsConnectorDead).Append('|').Append(stage.IsConnectorAboveDead).Append('\n');
                foreach (LiveOpsHubRailSectionRow row in stage.Rows)
                {
                    builder.Append(row.SectionId).Append('|').Append(row.Title).Append('|').Append((int)row.State).Append('|').Append(row.Tooltip)
                        .Append('|').Append(row.IsMarkHidden).Append('\n');
                }
            }
            builder.Append(model.BlockerTitle).Append('|').Append(model.BlockerDetail).Append('|')
                .Append(model.BlockerNavigation == null ? string.Empty : model.BlockerNavigation.SectionId + "/" + model.BlockerNavigation.FilterConsequence);
            return builder.ToString();
        }
    }
}
