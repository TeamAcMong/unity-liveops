using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Một hover card dùng chung cho cả cửa sổ ([FD §2.13], 8.1 bước 7): con cuối của root, hiện sau 500ms đứng yên trên đích, ẩn
    /// sau 100ms rời đích (đi chuột từ đích sang thẻ không làm thẻ tắt), F8 ghim thẻ tới khi <see cref="Hide"/>.
    /// <para>
    /// Chờ và ẩn do C# hẹn giờ theo đồng hồ tiêm được, không dùng <c>transition-delay</c>: rời chuột trong 500ms phải huỷ được việc
    /// hiện, và test đếm được 500ms không cần chờ thời gian thật. USS chỉ lo opacity 80ms (liveops-hub-motion.uss).
    /// </para>
    /// Một thẻ cho mọi đích: hai thẻ chồng nhau khi lướt chuột qua hai thanh sát nhau là nhiễu, và root chỉ có một lớp nổi trên cùng.
    /// </summary>
    internal sealed class LiveOpsHoverCardHost
    {
        public const int ShowDelayMilliseconds = 500, HideDelayMilliseconds = 100;

        internal const string CardElementName = "hub-hover-card";

        /// <summary>Nhịp hẹn giờ — đủ mịn để 500ms lệch không quá một nhịp.</summary>
        internal const long TickMilliseconds = 25;

        /// <summary>Khoảng cách từ mép dưới đích tới thẻ và lề giữ thẻ trong root.</summary>
        internal const float CardOffset = 4f;

        /// <summary>Sai số so hạn giờ (giây): cộng dồn số thực làm 10,0 + 0,5 lệch một ulp so với đồng hồ đọc 10,5 — không được lệch một nhịp.</summary>
        private const double DeadlineToleranceSeconds = 1e-6;

        private readonly VisualElement _root;
        private readonly Func<double> _clockSeconds;
        private readonly VisualElement _card;
        private readonly Dictionary<VisualElement, Func<VisualElement>> _contentBuilders = new Dictionary<VisualElement, Func<VisualElement>>();

        private VisualElement _pendingTarget;
        private double _showAtSeconds = double.NaN;
        private double _hideAtSeconds = double.NaN;
        private IVisualElementScheduledItem _ticker;

        public LiveOpsHoverCardHost(VisualElement root) : this(root, () => EditorApplication.timeSinceStartup)
        {
        }

        internal LiveOpsHoverCardHost(VisualElement root, Func<double> clockSeconds)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _clockSeconds = clockSeconds ?? throw new ArgumentNullException(nameof(clockSeconds));
            _card = new VisualElement { name = CardElementName, pickingMode = PickingMode.Ignore };
            _card.AddToClassList(LiveOpsHubClassNames.HoverCard);
            _card.RegisterCallback<PointerEnterEvent>(pointerEvent => CancelHide());
            _card.RegisterCallback<PointerLeaveEvent>(pointerEvent => ScheduleHide());
            _card.RegisterCallback<GeometryChangedEvent>(geometryEvent => KeepCardInsideRoot());
            _root.Add(_card);
            // Hẹn giờ chỉ chạy khi root có panel: đích được hover trước khi root gắn panel thì bắt đầu đếm lúc gắn.
            _root.RegisterCallback<AttachToPanelEvent>(panelEvent =>
            {
                if (_pendingTarget != null || !double.IsNaN(_hideAtSeconds)) EnsureTicker();
            });
        }

        public bool IsVisible { get; private set; }
        public bool IsPinned { get; private set; }

        /// <summary>
        /// (UX-05, UJ-05) Thẻ đang bị tắt vì một cử chỉ đang chạy (kéo thanh trên trục). Vì sao cần cổng riêng thay vì dựa vào
        /// PointerLeave: lúc kéo, element giữ pointer capture và mỗi bước xem trước dựng lại làn — thanh đang hover bị thay bằng
        /// element MỚI, nên Leave của thanh cũ không bao giờ tới và thẻ treo lại che đúng chỗ người dùng đang nhắm.
        /// </summary>
        public bool IsSuppressed { get; private set; }

        /// <summary>Số đích còn đăng ký — test dọn đích chết đọc ở đây.</summary>
        internal int AttachedTargetCount => _contentBuilders.Count;

        /// <summary>Đích đang có thẻ (hoặc đang chờ 500ms); null khi không có.</summary>
        public VisualElement CurrentTarget { get; private set; }

        internal VisualElement Card => _card;

        /// <summary>Gắn hover cho <paramref name="target"/>; nội dung dựng lúc hiện (dữ liệu có thể đã đổi từ lúc gắn).</summary>
        public void Attach(VisualElement target, Func<VisualElement> buildContent)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (buildContent == null) throw new ArgumentNullException(nameof(buildContent));
            PruneDetachedTargets();
            bool isNew = !_contentBuilders.ContainsKey(target);
            _contentBuilders[target] = buildContent;
            if (!isNew) return;
            target.RegisterCallback<PointerEnterEvent>(pointerEvent => HandlePointerEnter(target));
            target.RegisterCallback<PointerLeaveEvent>(pointerEvent => HandlePointerLeave(target));
        }

        /// <summary>
        /// (UX-05) Bật/tắt cổng khi một cử chỉ bắt đầu và kết thúc. Bật: ẩn thẻ đang hiện và huỷ thẻ đang chờ. Tắt: KHÔNG tự
        /// hiện lại — người dùng vừa nhả chuột thì cái họ nhìn là thanh vừa thả, không phải một thẻ bật lên đè lên nó.
        /// </summary>
        public void Suppress(bool isSuppressed)
        {
            if (IsSuppressed == isSuppressed) return;
            IsSuppressed = isSuppressed;
            if (isSuppressed) Hide();
        }

        /// <summary>
        /// (UX-05) Dọn đích đã rời cây: mỗi lần vẽ lại làn thay thanh bằng element mới, builder của element cũ ở lại mãi trong
        /// từ điển và thẻ của nó có thể còn đang hiện. Gọi sau mỗi lần gắn lại hover card cho tập thanh mới.
        /// </summary>
        public void PruneDetachedTargets()
        {
            List<VisualElement> dead = null;
            foreach (KeyValuePair<VisualElement, Func<VisualElement>> pair in _contentBuilders)
            {
                if (pair.Key.panel != null) continue;
                if (dead == null) dead = new List<VisualElement>();
                dead.Add(pair.Key);
            }
            if (dead == null) return;
            for (int index = 0; index < dead.Count; index++)
            {
                VisualElement target = dead[index];
                _contentBuilders.Remove(target);
                if (_pendingTarget == target)
                {
                    _pendingTarget = null;
                    _showAtSeconds = double.NaN;
                }
                if (CurrentTarget == target) HideVisibleCard();
            }
        }

        /// <summary>F8: hiện ngay và ghim — rời chuột không tắt, chỉ <see cref="Hide"/> (Esc/F8 lần nữa/điều hướng) mới tắt.</summary>
        public void ShowPinned(VisualElement target, VisualElement content)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (content == null) throw new ArgumentNullException(nameof(content));
            ShowCard(target, content);
            IsPinned = true;
            _card.AddToClassList(LiveOpsHubClassNames.HoverCardPinned);
        }

        /// <summary>Tắt hẳn: thẻ đang hiện, thẻ đang chờ 500ms và ghim (Esc/F8 lần nữa/điều hướng).</summary>
        public void Hide()
        {
            _pendingTarget = null;
            _showAtSeconds = double.NaN;
            HideVisibleCard();
            _ticker?.Pause();
        }

        /// <summary>
        /// Chỉ bỏ thẻ đang hiện, giữ đích đang chờ 500ms: lướt từ thanh A sang thanh B sát bên thì hạn ẩn A (100ms) tới trước hạn hiện B
        /// (500ms) — xoá luôn việc chờ của B ở đây thì chuột đứng yên trên B không bao giờ thấy thẻ, vì không có PointerEnter mới.
        /// </summary>
        private void HideVisibleCard()
        {
            _hideAtSeconds = double.NaN;
            IsVisible = false;
            IsPinned = false;
            CurrentTarget = null;
            _card.RemoveFromClassList(LiveOpsHubClassNames.HoverCardVisible);
            _card.RemoveFromClassList(LiveOpsHubClassNames.HoverCardPinned);
            _card.pickingMode = PickingMode.Ignore;
            _card.Clear();
        }

        internal void HandlePointerEnter(VisualElement target)
        {
            if (IsPinned || IsSuppressed) return;
            if (IsVisible && CurrentTarget == target)
            {
                CancelHide();
                return;
            }
            _pendingTarget = target;
            _showAtSeconds = _clockSeconds() + ShowDelayMilliseconds / 1000.0;
            EnsureTicker();
        }

        internal void HandlePointerLeave(VisualElement target)
        {
            if (IsPinned) return;
            if (_pendingTarget == target)
            {
                // Rời trước 500ms: huỷ, không để thẻ bật lên sau khi chuột đã ở chỗ khác.
                _pendingTarget = null;
                _showAtSeconds = double.NaN;
            }
            if (IsVisible && CurrentTarget == target) ScheduleHide();
        }

        /// <summary>Một nhịp hẹn giờ: tới hạn thì hiện thẻ đang chờ hoặc ẩn thẻ đang hiện.</summary>
        internal void Tick()
        {
            double now = _clockSeconds();
            if (_pendingTarget != null && !double.IsNaN(_showAtSeconds) && now + DeadlineToleranceSeconds >= _showAtSeconds)
            {
                VisualElement target = _pendingTarget;
                _pendingTarget = null;
                _showAtSeconds = double.NaN;
                Func<VisualElement> build;
                if (_contentBuilders.TryGetValue(target, out build))
                {
                    VisualElement content = build();
                    if (content == null) throw new InvalidOperationException(LiveOpsHubStrings.FeedbackErrorHoverCardContentMissing);
                    ShowCard(target, content);
                }
            }
            if (!double.IsNaN(_hideAtSeconds) && now + DeadlineToleranceSeconds >= _hideAtSeconds) HideVisibleCard();
            if (_pendingTarget == null && double.IsNaN(_hideAtSeconds)) _ticker?.Pause();
        }

        private void ShowCard(VisualElement target, VisualElement content)
        {
            _pendingTarget = null;
            _showAtSeconds = double.NaN;
            _hideAtSeconds = double.NaN;
            _card.Clear();
            _card.Add(content);
            CurrentTarget = target;
            IsVisible = true;
            _card.pickingMode = PickingMode.Position;
            _card.AddToClassList(LiveOpsHubClassNames.HoverCardVisible);
            // Chỗ 10 của [FD §2.14]: lớp nổi đưa lên trên cùng khi hiện (USS không có z-index).
            _card.BringToFront();
            PlaceNear(target);
        }

        private void ScheduleHide()
        {
            if (!IsVisible || IsPinned) return;
            _hideAtSeconds = _clockSeconds() + HideDelayMilliseconds / 1000.0;
            EnsureTicker();
        }

        private void CancelHide()
        {
            _hideAtSeconds = double.NaN;
        }

        private void PlaceNear(VisualElement target)
        {
            Rect targetBound = target.worldBound;
            Rect rootBound = _root.worldBound;
            if (float.IsNaN(targetBound.x) || float.IsNaN(rootBound.x)) return;
            _card.style.left = targetBound.x - rootBound.x; // style-inline-allowed: 6
            _card.style.top = targetBound.yMax - rootBound.y + CardOffset; // style-inline-allowed: 6
        }

        /// <summary>Thẻ vừa có kích thước thật: đẩy vào trong root (thanh sát mép phải/đáy không được đẩy thẻ ra ngoài cửa sổ).</summary>
        private void KeepCardInsideRoot()
        {
            if (!IsVisible || CurrentTarget == null) return;
            Rect card = _card.layout;
            Rect root = _root.layout;
            if (float.IsNaN(card.width) || float.IsNaN(root.width)) return;
            float left = Mathf.Min(card.x, root.width - card.width - CardOffset);
            float top = card.y;
            if (top + card.height > root.height - CardOffset)
            {
                // Không đủ chỗ dưới đích thì đặt lên trên đích.
                Rect targetBound = CurrentTarget.worldBound;
                top = targetBound.y - _root.worldBound.y - card.height - CardOffset;
            }
            left = Mathf.Max(CardOffset, left);
            top = Mathf.Max(CardOffset, top);
            if (Mathf.Approximately(left, card.x) && Mathf.Approximately(top, card.y)) return;
            _card.style.left = left; // style-inline-allowed: 6
            _card.style.top = top; // style-inline-allowed: 6
        }

        private void EnsureTicker()
        {
            if (_root.panel == null) return;
            if (_ticker == null) _ticker = _root.schedule.Execute(Tick).Every(TickMilliseconds);
            else _ticker.Resume();
        }
    }
}
