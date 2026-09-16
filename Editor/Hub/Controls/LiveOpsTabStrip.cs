using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Tab tự làm: <see cref="Toolbar"/> + <see cref="ToolbarToggle"/> loại trừ nhau ([FD §2.15] chốt vai trò TabStrip là wrapper).
    /// Vì sao không <c>TabView</c>/<c>ToggleButtonGroup</c>: chỉ có từ Unity 6. Vì sao không RadioButtonGroup: thiết kế vẽ tab trên
    /// toolbar (nền <c>toolbar_button-checked</c> khi bật), RadioButton vẽ nút tròn.
    ///
    /// Bất biến: khi có ít nhất một lựa chọn thì luôn đúng MỘT tab bật — bấm lại tab đang bật không tắt nó (người dùng không bao giờ
    /// thấy "Nháp | Bản đã đăng" không chọn gì). Bấm lại tab đang bật báo riêng qua <see cref="SelectedTabPressedAgain"/> để timeline
    /// về đúng preset zoom khi đang zoom tự do (V-11).
    ///
    /// Mỗi tab nằm trong một slot mang tooltip lý do: toggle disabled có thể không nhận hover nên tooltip đặt trên chính toggle không
    /// hiện (R-16, cùng lý do <see cref="LiveOpsButtonSlot"/>). Slot còn mang một <see cref="Label"/> IN LÝ DO THÀNH CHỮ cạnh tab
    /// (SPIKE-B SP-3): tooltip là đường phụ, vì cả tab lẫn slot đều có thể không nhận hover ở một bản Unity và khi đó người dùng
    /// chỉ thấy một tab xám không nói vì sao.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    public sealed partial class LiveOpsTabStrip : Toolbar
    {
        internal const char ChoiceSeparator = '|';

        private readonly List<ToolbarToggle> _tabs = new List<ToolbarToggle>();
        private readonly List<VisualElement> _slots = new List<VisualElement>();
        private readonly List<Label> _reasonLabels = new List<Label>();
        private string _choices = string.Empty;
        private int _selectedIndex = -1;

        public LiveOpsTabStrip()
        {
            AddToClassList(LiveOpsHubClassNames.TabStrip);
        }

        /// <summary>Thuộc tính UXML <c>choices</c>: nhãn các tab ngăn bằng '|' ("Nháp|Bản đã đăng"). Gán lại dựng lại toàn bộ tab.</summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public string Choices
        {
            get => _choices;
            set
            {
                _choices = value ?? string.Empty;
                RebuildTabs();
            }
        }

        /// <summary>Số tab hiện có.</summary>
        internal int ChoiceCount => _tabs.Count;

        /// <summary>
        /// Tab đang bật; -1 chỉ khi không có lựa chọn nào. Gán ngoài khoảng bị kẹp về khoảng hợp lệ (không bao giờ bỏ chọn hết).
        /// Gán đổi tab bắn <see cref="SelectedIndexChanged"/>; gán đúng tab đang bật thì không bắn.
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => Select(value, true);
        }

        public event Action<int> SelectedIndexChanged;

        /// <summary>Người dùng bấm lại tab đang bật (không đổi chọn) — tham số là chỉ số tab đó.</summary>
        public event Action<int> SelectedTabPressedAgain;

        /// <summary>Đổi tab mà không bắn sự kiện — presenter khôi phục trạng thái view.</summary>
        public void SetSelectedIndexWithoutNotify(int index)
        {
            Select(index, false);
        }

        /// <summary>
        /// Khoá/mở một tab. Khoá bắt buộc có <paramref name="reason"/> (tab disabled không nói vì sao là người dùng kẹt); lý do nằm
        /// trên slot của tab. Khoá tab đang bật không đổi chọn — người gọi quyết định chuyển tab, control không đoán.
        /// </summary>
        public void SetChoiceEnabled(int index, bool enabled, string reason)
        {
            if (index < 0 || index >= _tabs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TabStripChoiceOutOfRangeFormat, index, _tabs.Count));
            }
            if (!enabled && string.IsNullOrEmpty(reason))
            {
                throw new ArgumentException(LiveOpsHubStrings.DisabledReasonRequired, nameof(reason));
            }
            _tabs[index].SetEnabled(enabled);
            string shownReason = enabled ? string.Empty : reason;
            _slots[index].tooltip = shownReason;
            // Lý do là CHỮ cạnh tab (SP-3); tooltip trên slot và trên chính nhãn chỉ là đường phụ khi chữ bị cắt.
            _reasonLabels[index].text = shownReason;
            _reasonLabels[index].tooltip = shownReason;
            _slots[index].EnableInClassList(LiveOpsHubClassNames.TabStripSlotBlocked, !enabled);
        }

        /// <summary>Toggle của tab thứ <paramref name="index"/> — cho test và kịch bản chụp ép trạng thái hover/nhấn.</summary>
        internal ToolbarToggle TabAt(int index)
        {
            return _tabs[index];
        }

        /// <summary>Slot bọc tab (mang tooltip lý do khi tab bị khoá).</summary>
        internal VisualElement SlotAt(int index)
        {
            return _slots[index];
        }

        /// <summary>Nhãn in lý do cạnh tab thứ <paramref name="index"/>; chữ rỗng khi tab đang mở (SP-3).</summary>
        internal Label ReasonLabelAt(int index)
        {
            return _reasonLabels[index];
        }

        private void RebuildTabs()
        {
            foreach (VisualElement slot in _slots) slot.RemoveFromHierarchy();
            _slots.Clear();
            _tabs.Clear();
            _reasonLabels.Clear();

            string[] labels = _choices.Length == 0 ? Array.Empty<string>() : _choices.Split(ChoiceSeparator);
            for (int index = 0; index < labels.Length; index++)
            {
                VisualElement slot = new VisualElement();
                slot.AddToClassList(LiveOpsHubClassNames.TabStripSlot);
                // Nhãn lý do đứng TRƯỚC tab, cùng lối "lý do ngắn màu blocked-text ngay bên trái" của nút chính ([FD §3.6]);
                // dựng sẵn cho mọi tab để bật/tắt là đổi class, không dựng lại cây khi khoá.
                Label reason = new Label();
                reason.AddToClassList(LiveOpsHubClassNames.TabStripReason);
                reason.AddToClassList(LiveOpsHubClassNames.TextBlocked);
                slot.Add(reason);
                ToolbarToggle tab = new ToolbarToggle { text = labels[index] };
                tab.AddToClassList(LiveOpsHubClassNames.TabStripTab);
                int tabIndex = index;
                tab.RegisterValueChangedCallback(changeEvent => OnTabValueChanged(tabIndex, changeEvent));
                slot.Add(tab);
                Add(slot);
                _slots.Add(slot);
                _reasonLabels.Add(reason);
                _tabs.Add(tab);
            }

            int previous = _selectedIndex;
            _selectedIndex = -1;
            if (_tabs.Count == 0) return;
            // Giữ chỉ số đã chọn khi dựng lại (UXML đổi nhãn không được làm nhảy tab); không có thì tab đầu.
            ApplyToggleValues(Clamp(previous < 0 ? 0 : previous));
        }

        private void OnTabValueChanged(int tabIndex, ChangeEvent<bool> changeEvent)
        {
            // ChangeEvent<bool> của toggle con không được nổi lên thành "giá trị" của cả dải — nơi nghe chỉ dùng SelectedIndexChanged.
            changeEvent.StopPropagation();
            if (changeEvent.newValue)
            {
                Select(tabIndex, true);
                return;
            }
            if (tabIndex == _selectedIndex)
            {
                // Bấm lại tab đang bật: toggle tự tắt — bật lại ngay (không bao giờ rỗng) và báo "bấm lại".
                _tabs[tabIndex].SetValueWithoutNotify(true);
                SelectedTabPressedAgain?.Invoke(tabIndex);
            }
        }

        private void Select(int index, bool notify)
        {
            if (_tabs.Count == 0)
            {
                _selectedIndex = -1;
                return;
            }
            int clamped = Clamp(index);
            bool changed = clamped != _selectedIndex;
            ApplyToggleValues(clamped);
            if (changed && notify) SelectedIndexChanged?.Invoke(clamped);
        }

        private void ApplyToggleValues(int selected)
        {
            _selectedIndex = selected;
            for (int index = 0; index < _tabs.Count; index++)
            {
                _tabs[index].SetValueWithoutNotify(index == selected);
            }
        }

        private int Clamp(int index)
        {
            if (index < 0) return 0;
            return index >= _tabs.Count ? _tabs.Count - 1 : index;
        }

#if !UNITY_2023_2_OR_NEWER
        public new sealed class UxmlFactory : UxmlFactory<LiveOpsTabStrip, UxmlTraits>
        {
        }

        public new sealed class UxmlTraits : VisualElement.UxmlTraits
        {
            // Tên thuộc tính trùng tên kebab-case mà [UxmlAttribute] của Unity 6 sinh từ property (Choices → "choices").
            private readonly UxmlStringAttributeDescription _choices = new UxmlStringAttributeDescription { name = "choices", defaultValue = string.Empty };

            public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext context)
            {
                base.Init(visualElement, bag, context);
                ((LiveOpsTabStrip)visualElement).Choices = _choices.GetValueFromBag(bag, context);
            }
        }
#endif
    }
}
