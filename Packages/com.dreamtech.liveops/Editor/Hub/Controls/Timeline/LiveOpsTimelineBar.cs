using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Một thanh đợt trong pool của <see cref="LiveOpsTimelineLane"/> [SD1 §3.5]. Thanh là element (không vẽ bằng Painter2D của làn)
    /// vì ba thứ chỉ element có: con trỏ chuột theo vùng bằng USS <c>cursor</c> (V-11 — thân <c>move-arrow</c>, mép 8px
    /// <c>resize-horizontal</c>), tooltip riêng từng thanh, và trạng thái hover/chọn/kéo bằng class thay vì vẽ lại cả làn. Làn vẫn
    /// tính thanh vào ước lượng vertex (<see cref="LiveOpsTimelineVertexBudget.EstimateLane"/>) nên model gom dải như cũ — số element
    /// thanh mỗi làn vì vậy bị chặn bởi cùng ngân sách, không phình theo dữ liệu một năm.
    ///
    /// Thanh không có logic nghiệp vụ: mọi quyết định (nhãn, cắt mép, đang chạy, bị bỏ, dải) đọc từ <see cref="LiveOpsTimelineBarModel"/>
    /// và hình học; nơi dùng chỉ bật trạng thái tương tác.
    /// </summary>
    internal sealed class LiveOpsTimelineBar : VisualElement
    {
        private const string LoopIconName = "preAudioLoopOff";
        private const string LockIconName = "InspectorLock";
        private const int SmallIconSize = 10;

        public LiveOpsTimelineBar()
        {
            AddToClassList(LiveOpsHubClassNames.TimelineBar);
            pickingMode = PickingMode.Position;

            Stripe = CreatePart(LiveOpsHubClassNames.TimelineBarStripe);
            LockIcon = LiveOpsHubIcons.CreateImage(LockIconName, SmallIconSize);
            LockIcon.AddToClassList(LiveOpsHubClassNames.TimelineBarLockIcon);
            Add(LockIcon);
            LoopIcon = LiveOpsHubIcons.CreateImage(LoopIconName, SmallIconSize);
            LoopIcon.AddToClassList(LiveOpsHubClassNames.TimelineBarLoopIcon);
            Add(LoopIcon);
            Label = new Label { pickingMode = PickingMode.Ignore };
            Label.AddToClassList(LiveOpsHubClassNames.TimelineBarLabel);
            Label.AddToClassList(LiveOpsHubClassNames.Mono);
            Add(Label);
            ChangedSquare = CreatePart(LiveOpsHubClassNames.TimelineBarChangedSquare);
            ChangedSquare.pickingMode = PickingMode.Position;
            Strike = CreatePart(LiveOpsHubClassNames.TimelineBarStrike);
            ClipStart = CreateClipMarker(LiveOpsHubClassNames.TimelineBarClipStart, LiveOpsHubStrings.TimelineClippedStartTooltip);
            ClipEnd = CreateClipMarker(LiveOpsHubClassNames.TimelineBarClipEnd, LiveOpsHubStrings.TimelineClippedEndTooltip);
            StartEdge = CreateEdge(LiveOpsHubClassNames.TimelineBarEdgeStart);
            EndEdge = CreateEdge(LiveOpsHubClassNames.TimelineBarEdgeEnd);
        }

        public LiveOpsTimelineBarModel Model { get; private set; }

        /// <summary>Toạ độ đã vẽ trong track (trước translate xem trước) — HitTest và readout đọc lại đúng số này.</summary>
        public float Left { get; private set; }

        public float Width { get; private set; }
        public float Top { get; private set; }
        public bool IsClippedStart { get; private set; }
        public bool IsClippedEnd { get; private set; }

        internal VisualElement Stripe { get; }
        internal Image LoopIcon { get; }
        internal Image LockIcon { get; }
        internal Label Label { get; }
        internal VisualElement ChangedSquare { get; }
        internal VisualElement Strike { get; }
        internal VisualElement ClipStart { get; }
        internal VisualElement ClipEnd { get; }
        internal VisualElement StartEdge { get; }
        internal VisualElement EndEdge { get; }

        /// <summary>Thanh cố định kéo được: không phải dải, chưa khép. Đợt sinh từ luật và dải chỉ đọc (CC-TLMODEL-1).</summary>
        public bool IsDraggable => Model != null && Model.Source == LiveOpsTimelineBarSource.Fixed && !Model.IsEnded;

        /// <summary>Mép đầu kéo được: thanh kéo được và chưa chạy (đợt đang chạy khoá mép đầu [SD1 §3.5 --running]).</summary>
        public bool IsStartEdgeDraggable => IsDraggable && !Model.IsRunning;

        internal void Bind(LiveOpsTimelineBarModel model, LiveOpsTimelineGeometry geometry, float top, int colorSlot, string tooltipText,
            LiveOpsHubFormat format)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            // Tên element = BarKey: chỉ để nơi ngoài gọi đúng MỘT thanh theo tên — kịch bản chụp khai khung mong đợi cho thanh mốc
            // (đo được bằng máy thay vì đối chiếu tay), và Q(name) trong test. Không có USS nào bám tên.
            name = model.BarKey;
            (float left, float width, bool clippedStart, bool clippedEnd) rectangle = geometry.BarRect(model.StartUtc, model.EndUtc);
            Left = rectangle.left;
            Width = rectangle.width;
            Top = top;
            IsClippedStart = rectangle.clippedStart;
            IsClippedEnd = rectangle.clippedEnd;

            style.left = Left; // style-inline-allowed: 3
            style.width = Width; // style-inline-allowed: 3
            style.top = Top; // style-inline-allowed: 3
            style.translate = StyleKeyword.Null; // style-inline-allowed: 4

            bool isRecurring = model.Source == LiveOpsTimelineBarSource.Recurring || model.Source == LiveOpsTimelineBarSource.RecurringStrip;
            EnableInClassList(LiveOpsHubClassNames.TimelineBarRecurring, isRecurring);
            EnableInClassList(LiveOpsHubClassNames.TimelineBarMerged, model.IsStrip);
            EnableInClassList(LiveOpsHubClassNames.TimelineBarEnded, model.IsEnded);
            EnableInClassList(LiveOpsHubClassNames.TimelineBarRunning, model.IsRunning && model.Source == LiveOpsTimelineBarSource.Fixed);
            EnableInClassList(LiveOpsHubClassNames.TimelineBarDropped, model.IsDropped);
            EnableInClassList(LiveOpsHubClassNames.TimelineBarChanged, model.IsChangedSincePublished);
            EnableInClassList(LiveOpsHubClassNames.TimelineBarClippedStart, IsClippedStart);
            EnableInClassList(LiveOpsHubClassNames.TimelineBarClippedEnd, IsClippedEnd);
            LiveOpsHubStyle.SetEventColor(Stripe, colorSlot);

            string labelText = LabelTextFor(model, Width);
            Label.text = labelText;
            SetVisible(LoopIcon, isRecurring && !model.IsStrip && Width >= LiveOpsTimelineGeometry.LabelFullIdMinimumBarWidth);
            SetVisible(LockIcon, model.IsRunning && model.Source == LiveOpsTimelineBarSource.Fixed && Width >= LiveOpsTimelineGeometry.LabelMinimumBarWidth);
            SetVisible(ChangedSquare, model.IsChangedSincePublished);
            ChangedSquare.tooltip = model.ChangedTooltip;
            SetVisible(Strike, model.IsDropped);
            SetVisible(ClipStart, IsClippedStart);
            SetVisible(ClipEnd, IsClippedEnd);
            SetVisible(StartEdge, IsStartEdgeDraggable);
            SetVisible(EndEdge, IsDraggable);
            LockIcon.tooltip = model.IsRunning
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineRunningLockTooltipFormat, format.ShortDateTime(model.StartUtc))
                : string.Empty;
            tooltip = tooltipText ?? string.Empty;

            SetInteractionState(false, false, false);
            SetDragPreview(false, false);
        }

        /// <summary>Nhãn theo thuật toán barLabel [SD1 §3.5]; dải gom ghi "sky-race · 42 đợt · 20 giờ/ngày" khi đủ chỗ, không thì để trống (tooltip nói).</summary>
        internal static string LabelTextFor(LiveOpsTimelineBarModel model, float width)
        {
            if (model.IsStrip)
            {
                string stripText = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineStripLabelFormat), model.EventType,
                    model.StripCount, model.StripHoursPerDay);
                float room = width - LiveOpsTimelineGeometry.LabelHorizontalPadding;
                return stripText.Length * LiveOpsTimelineGeometry.LabelCharacterWidth <= room ? stripText : string.Empty;
            }
            bool isRecurring = model.Source == LiveOpsTimelineBarSource.Recurring;
            string identifier = model.RenamedFromId.Length > 0
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineRenamedLabelFormat, model.RenamedFromId, model.EventId)
                : model.EventId;
            return LiveOpsTimelineGeometry.BarLabel(identifier, width, isRecurring, model.IsChangedSincePublished);
        }

        /// <summary>
        /// Trạng thái tương tác [SD1 §3.5]: hover hiện tay nắm 2×12; chọn = highlight (chữ trắng khi timeline có focus — class trên
        /// root timeline lo phần đó); focus bàn phím = viền focus. Viền sẽ bị bỏ do làn bật khi xem trước kéo.
        /// </summary>
        internal void SetInteractionState(bool hover, bool selected, bool focused)
        {
            EnableInClassList(LiveOpsHubClassNames.TimelineBarHover, hover);
            EnableInClassList(LiveOpsHubClassNames.TimelineBarSelected, selected);
            EnableInClassList(LiveOpsHubClassNames.TimelineBarFocused, focused);
        }

        internal void SetDragPreview(bool dragging, bool willDrop)
        {
            EnableInClassList(LiveOpsHubClassNames.TimelineBarDragging, dragging);
            EnableInClassList(LiveOpsHubClassNames.TimelineBarWillDrop, willDrop);
        }

        /// <summary>Xem trước khi kéo: dời bằng translate, đổi bề rộng khi kéo mép; thả hoặc huỷ thì <see cref="ClearPreviewGeometry"/>.</summary>
        internal void SetPreviewGeometry(float left, float width)
        {
            style.translate = new Translate(left - Left, 0f); // style-inline-allowed: 4
            style.width = Math.Max(LiveOpsTimelineGeometry.MinimumBarWidth, width); // style-inline-allowed: 3
            Label.text = LabelTextFor(Model, width);
        }

        internal void ClearPreviewGeometry()
        {
            style.translate = StyleKeyword.Null; // style-inline-allowed: 4
            style.width = Width; // style-inline-allowed: 3
            if (Model != null) Label.text = LabelTextFor(Model, Width);
        }

        /// <summary>Vùng của thanh tại toạ độ x trong track: mép 8px (4 trong, 4 ngoài) chỉ khi mép đó kéo được; thanh hẹp chọn mép gần hơn.</summary>
        internal LiveOpsTimelineBarRegion RegionAt(float trackPosition)
        {
            float halfGrab = LiveOpsTimelineGeometry.EdgeGrabWidth / 2f;
            float right = Left + Width;
            float distanceToStart = Math.Abs(trackPosition - Left);
            float distanceToEnd = Math.Abs(trackPosition - right);
            bool nearStart = IsStartEdgeDraggable && distanceToStart <= halfGrab;
            bool nearEnd = IsDraggable && distanceToEnd <= halfGrab;
            if (nearStart && nearEnd) return distanceToStart < distanceToEnd ? LiveOpsTimelineBarRegion.StartEdge : LiveOpsTimelineBarRegion.EndEdge;
            if (nearStart) return LiveOpsTimelineBarRegion.StartEdge;
            if (nearEnd) return LiveOpsTimelineBarRegion.EndEdge;
            return LiveOpsTimelineBarRegion.Body;
        }

        /// <summary>Có trúng thanh không (kể cả 4px ngoài mép kéo được).</summary>
        internal bool Contains(float trackPosition, float lanePosition)
        {
            if (!IsInsideRow(lanePosition)) return false;
            float halfGrab = LiveOpsTimelineGeometry.EdgeGrabWidth / 2f;
            float extraStart = IsStartEdgeDraggable ? halfGrab : 0f;
            float extraEnd = IsDraggable ? halfGrab : 0f;
            return trackPosition >= Left - extraStart && trackPosition <= Left + Width + extraEnd;
        }

        /// <summary>
        /// Có trúng thanh KHÔNG tính 4px lề bắt mép. Hai đợt sát nhau (đợt sau bắt đầu đúng lúc đợt trước khép — Hình 12 khung 7)
        /// có lề bắt mép chồng lên nhau; làn phải ưu tiên thanh mà điểm bấm nằm hẳn bên trong, nếu không người dùng bấm mép cuối
        /// đợt trước lại tóm phải mép đầu đợt sau.
        /// </summary>
        internal bool ContainsWithoutEdgeSlack(float trackPosition, float lanePosition)
        {
            return IsInsideRow(lanePosition) && trackPosition >= Left && trackPosition <= Left + Width;
        }

        private bool IsInsideRow(float lanePosition)
        {
            return lanePosition >= Top && lanePosition <= Top + LiveOpsTimelineGeometry.BarHeight;
        }

        private VisualElement CreatePart(string className)
        {
            VisualElement part = new VisualElement { pickingMode = PickingMode.Ignore };
            part.AddToClassList(className);
            Add(part);
            return part;
        }

        private VisualElement CreateClipMarker(string className, string tooltipText)
        {
            // Dấu ◂ ▸ là nửa hình vuông 7×7 xoay 45° trong cha overflow hidden [SD1 §3.5] — không dùng ký tự vì font Editor không chắc có glyph.
            VisualElement marker = new VisualElement { pickingMode = PickingMode.Position, tooltip = tooltipText };
            marker.AddToClassList(className);
            VisualElement shape = new VisualElement { pickingMode = PickingMode.Ignore };
            shape.AddToClassList(LiveOpsHubClassNames.TimelineBarClipShape);
            marker.Add(shape);
            Add(marker);
            return marker;
        }

        private VisualElement CreateEdge(string variantClassName)
        {
            // Vùng bắt 8px là element riêng để USS gắn cursor resize-horizontal (V-11); tay nắm 2×12 bên trong chỉ hiện khi hover.
            VisualElement edge = new VisualElement { pickingMode = PickingMode.Position };
            edge.AddToClassList(LiveOpsHubClassNames.TimelineBarEdge);
            edge.AddToClassList(variantClassName);
            VisualElement handle = new VisualElement { pickingMode = PickingMode.Ignore };
            handle.AddToClassList(LiveOpsHubClassNames.TimelineBarHandle);
            edge.Add(handle);
            Add(edge);
            return edge;
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            element.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !visible);
        }
    }
}
