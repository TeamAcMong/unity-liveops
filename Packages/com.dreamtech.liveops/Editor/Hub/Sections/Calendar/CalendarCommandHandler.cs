using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Lệnh chiều sâu của màn Lịch (W5): nhân bản, copy id / copy đợt (JSON), dán tại con trỏ, ẩn / hiện / đưa làn, ⌘+kéo tạo và
    /// hoàn một mục về bản so. Một chỗ duy nhất cho CẢ HAI đường vào — phím Edit (<c>ExecuteCommandEvent</c> → ý định của
    /// timeline) và menu chuột phải (<see cref="CalendarContextMenus"/>) — nên hai đường không bao giờ làm khác nhau; menu chỉ
    /// dựng nhãn và hỏi <c>Can…</c>, việc thật nằm ở đây.
    /// <para>
    /// (V-10) Lớp này KHÔNG đụng <c>ED/Hub/Controls/Timeline/*</c>: nó nhận ý định control đã phát sẵn và trả lại
    /// <see cref="LiveEventCalendarEdit"/> + toast qua <see cref="CalendarTimelinePresenter.ApplyEdit"/>.
    /// </para>
    /// </summary>
    internal sealed class CalendarCommandHandler
    {
        /// <summary>Không có đợt cùng loại nào trước đó thì nhân bản nhảy 7 ngày [FD §3.9].</summary>
        internal static readonly TimeSpan DefaultDuplicateOffset = TimeSpan.FromDays(7);

        private readonly LiveOpsHubServices _services;
        private readonly CalendarTimelinePresenter _presenter;

        private FixedLiveEventEntry _copiedEntry;
        private string _copiedJson = string.Empty;

        public CalendarCommandHandler(LiveOpsHubServices services, CalendarTimelinePresenter presenter)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        /// <summary>Mở popover Thêm đợt tại làn + giờ đã biết (nhấp đúp, ⌘+kéo, mục menu "Thêm đợt bắt đầu …").</summary>
        public event Action<string, DateTime, DateTime?> AddEventRequested;

        /// <summary>Nhân bản: mở popover Thêm đợt ở BƯỚC XEM LẠI với id đề xuất, không tạo ngay [FD §3.9].</summary>
        public event Action<FixedLiveEventEntry, DateTime> DuplicateRequested;

        /// <summary>Danh sách làn ẩn vừa đổi — màn vẽ lại và cập nhật chip "Đang ẩn n làn".</summary>
        public event Action LanesChanged;

        /// <summary>Căn khung một đợt (mục "Căn khung", phím F đi qua chính element).</summary>
        public event Action<LiveOpsTimelineBarModel> FrameRequested;

        /// <summary>F8 / Shift+F8: ghim hover card lên phát hiện kế tiếp (+1) hoặc trước đó (−1).</summary>
        public event Action<int> FindingStepRequested;

        /// <summary>Vừa copy một đợt — timeline cần biết để bật lệnh Dán (⌘V) và mục menu tương ứng.</summary>
        public event Action CopiedEventChanged;

        /// <summary>Đợt vừa copy đang nằm trong clipboard hay không — timeline hỏi để bật/tắt lệnh Paste.</summary>
        public bool HasCopiedEvent
        {
            get
            {
                return _copiedEntry != null && _copiedJson.Length > 0
                       && string.Equals(_services.Clipboard.Text, _copiedJson, StringComparison.Ordinal);
            }
        }

        /// <summary>Đợt đang giữ trong clipboard của hub (null khi chưa copy hoặc clipboard đã bị thứ khác ghi đè).</summary>
        internal FixedLiveEventEntry CopiedEntry => HasCopiedEvent ? _copiedEntry : null;

        // ============================================================================================================ ý định

        /// <summary>Những loại ý định mà presenter không tự xử lý; loại lạ rơi xuống nhánh mặc định và bị bỏ qua có chủ ý.</summary>
        public void HandleIntent(LiveOpsTimelineIntent intent)
        {
            DuplicateBarIntent duplicate = intent as DuplicateBarIntent;
            if (duplicate != null)
            {
                RequestDuplicate(duplicate.BarKey);
                return;
            }
            CopyBarIntent copy = intent as CopyBarIntent;
            if (copy != null)
            {
                CopyEventJson(copy.BarKey);
                return;
            }
            PasteAtTimeIntent paste = intent as PasteAtTimeIntent;
            if (paste != null)
            {
                PasteAt(paste.LaneTypeId, paste.AtUtc);
                return;
            }
            CreateByDragIntent create = intent as CreateByDragIntent;
            if (create != null)
            {
                HandleCreateByDrag(create);
                return;
            }
            HideLaneIntent hide = intent as HideLaneIntent;
            if (hide != null)
            {
                HideLane(hide.TypeId);
                return;
            }
            if (intent is ShowAllLanesIntent)
            {
                ShowAllLanes();
                return;
            }
            MoveLaneIntent moveLane = intent as MoveLaneIntent;
            if (moveLane != null)
            {
                MoveLane(moveLane.TypeId, moveLane.Direction);
                return;
            }
            ShowFindingIntent finding = intent as ShowFindingIntent;
            if (finding != null) FindingStepRequested?.Invoke(finding.Direction);
        }

        /// <summary>
        /// ⌘/Ctrl + kéo chỗ trống: chỉ pha CHỐT mới mở popover. Pha xem trước đã có thanh ma do control vẽ; mở popover ở mỗi bước
        /// di chuột sẽ bật hàng chục cửa sổ.
        /// </summary>
        private void HandleCreateByDrag(CreateByDragIntent intent)
        {
            if (!intent.IsCommit || intent.LaneTypeId.Length == 0) return;
            if (Document.TryGetRecurringRule(intent.LaneTypeId, out RecurringLiveEventRule _)) return;
            AddEventRequested?.Invoke(intent.LaneTypeId, intent.StartUtc, intent.EndUtc);
        }

        // ============================================================================================================ copy / dán

        /// <summary>Copy id ra clipboard; không đụng đợt đang giữ để dán (id không dán lại thành một đợt được).</summary>
        public bool CopyEventId(string barKey)
        {
            FixedLiveEventEntry entry = FixedEntryOf(barKey);
            if (entry == null) return false;
            _services.Clipboard.Text = entry.EventId;
            ShowToast(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthCopyIdToastFormat, entry.EventId));
            return true;
        }

        /// <summary>
        /// Copy đợt dưới dạng object JSON đúng như mảng <c>events</c> của bản xuất — dán thẳng vào JSON remote được. Giữ luôn bản
        /// đợt trong bộ nhớ để lệnh Dán không phải đọc ngược JSON: <see cref="HasCopiedEvent"/> so lại clipboard từng byte nên
        /// người dùng copy thứ khác ở giữa thì Dán tự tắt.
        /// </summary>
        public bool CopyEventJson(string barKey)
        {
            FixedLiveEventEntry entry = FixedEntryOf(barKey);
            if (entry == null) return false;
            _copiedEntry = entry;
            _copiedJson = LiveEventCalendarJsonWriter.WriteFixedEventObject(Document, entry);
            _services.Clipboard.Text = _copiedJson;
            ShowToast(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthCopyJsonToastFormat, entry.EventId));
            CopiedEventChanged?.Invoke();
            return true;
        }

        /// <summary>Dán đợt đã copy vào làn tại giờ con trỏ; id mới do <see cref="LiveOpsEventIdSuggester"/> đề nghị.</summary>
        public bool PasteAt(string laneTypeId, DateTime atUtc)
        {
            FixedLiveEventEntry source = CopiedEntry;
            if (source == null || laneTypeId.Length == 0) return false;
            if (Document.TryGetRecurringRule(laneTypeId, out RecurringLiveEventRule _)) return false;
            if (!source.TryGetStartUtc(out DateTime sourceStartUtc) || !source.TryGetEndUtc(out DateTime sourceEndUtc)) return false;

            DateTime endUtc = LiveOpsTimelineGeometry.AddTicksClamped(atUtc, (sourceEndUtc - sourceStartUtc).Ticks);
            FixedLiveEventEntry pasted = new FixedLiveEventEntry(FixedLiveEventEntry.CreateEntryKey(),
                LiveOpsEventIdSuggester.Suggest(Document, laneTypeId, atUtc, source.EventId), laneTypeId,
                LiveEventUtcText.Format(atUtc), LiveEventUtcText.Format(endUtc), source.ConfigKey);
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthPasteToastFormat,
                pasted.EventId, _services.Format.ShortDateTime(atUtc));
            // (phiếu D-5) Tên bước Undo tách khỏi câu toast: toast sau ⌘Z đọc "Đã hoàn tác: Dán hunt-0916-bonus-2", không phải
            // "Đã hoàn tác: Đã dán …".
            string undoneStepName = string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarDepthPasteUndoStepFormat, pasted.EventId);
            if (!_presenter.ApplyEdit(new AddFixedEventEdit(pasted), LiveOpsEditOperation.AddFixedEvent, pasted.EntryKey, message,
                string.Empty, undoneStepName)) return false;
            _presenter.SetSelectedBarKey(pasted.EntryKey);
            return true;
        }

        // ============================================================================================================ nhân bản

        /// <summary>Đích của "Nhân bản sang …": giờ bắt đầu cộng khoảng cách tới đợt cùng loại TRƯỚC đó, không có thì 7 ngày.</summary>
        public bool TryGetDuplicateTarget(string barKey, out FixedLiveEventEntry entry, out DateTime targetStartUtc,
            out TimeSpan offset)
        {
            targetStartUtc = default(DateTime);
            offset = DefaultDuplicateOffset;
            entry = FixedEntryOf(barKey);
            if (entry == null || !entry.TryGetStartUtc(out DateTime startUtc)) return false;
            offset = OffsetFromPreviousSameType(entry, startUtc);
            targetStartUtc = LiveOpsTimelineGeometry.AddTicksClamped(startUtc, offset.Ticks);
            return true;
        }

        public bool RequestDuplicate(string barKey)
        {
            if (!TryGetDuplicateTarget(barKey, out FixedLiveEventEntry entry, out DateTime targetStartUtc, out TimeSpan _)) return false;
            DuplicateRequested?.Invoke(entry, targetStartUtc);
            return true;
        }

        /// <summary>Đợt cố định cùng loại gần nhất TRƯỚC mốc này; không có thì <see cref="DefaultDuplicateOffset"/>.</summary>
        private TimeSpan OffsetFromPreviousSameType(FixedLiveEventEntry entry, DateTime startUtc)
        {
            TimeSpan best = TimeSpan.Zero;
            IReadOnlyList<FixedLiveEventEntry> entries = Document.FixedEvents;
            for (int index = 0; index < entries.Count; index++)
            {
                FixedLiveEventEntry other = entries[index];
                if (string.Equals(other.EntryKey, entry.EntryKey, StringComparison.Ordinal)) continue;
                if (!string.Equals(other.EventType, entry.EventType, StringComparison.Ordinal)) continue;
                if (!other.TryGetStartUtc(out DateTime otherStartUtc) || otherStartUtc >= startUtc) continue;
                TimeSpan gap = startUtc - otherStartUtc;
                if (best == TimeSpan.Zero || gap < best) best = gap;
            }
            return best == TimeSpan.Zero ? DefaultDuplicateOffset : best;
        }

        // ============================================================================================================ làn

        public bool HideLane(string typeId)
        {
            if (typeId.Length == 0) return false;
            List<string> hidden = new List<string>(_presenter.HiddenLanes);
            if (hidden.Contains(typeId)) return false;
            hidden.Add(typeId);
            _presenter.SetHiddenLanes(hidden);
            ShowToast(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthHideLaneToastFormat, typeId));
            LanesChanged?.Invoke();
            return true;
        }

        public bool ShowAllLanes()
        {
            if (_presenter.HiddenLanes.Count == 0) return false;
            _presenter.SetHiddenLanes(Array.Empty<string>());
            ShowToast(LiveOpsHubStrings.CalendarDepthShowAllLanesToast);
            LanesChanged?.Invoke();
            return true;
        }

        /// <summary>Làn có đưa lên / xuống được không — mép danh sách thì không, và menu in lý do thành chữ.</summary>
        public bool CanMoveLane(string typeId, int direction)
        {
            int index = IndexOfEventType(typeId);
            if (index < 0) return false;
            int target = index + (direction < 0 ? -1 : 1);
            return target >= 0 && target < Document.EventTypes.Count;
        }

        /// <summary>
        /// (V-12) Đưa làn lên/xuống = <see cref="MoveEventTypeEdit"/> — MỘT Undo group, toast Hoàn tác, và KHÔNG đổi JSON nên sha
        /// xuất giữ nguyên: thứ tự làn là cách nhìn của người sửa lịch, không phải dữ liệu game đọc.
        /// </summary>
        public bool MoveLane(string typeId, int direction)
        {
            if (!CanMoveLane(typeId, direction)) return false;
            int target = IndexOfEventType(typeId) + (direction < 0 ? -1 : 1);
            string message = string.Format(CultureInfo.InvariantCulture, direction < 0
                ? LiveOpsHubStrings.CalendarDepthMoveLaneUpToastFormat
                : LiveOpsHubStrings.CalendarDepthMoveLaneDownToastFormat, typeId);
            // (phiếu D-5) Tên bước không nhắc hướng: "Đã hoàn tác: Đưa làn treasure-hunt" đủ để tìm lại bước, và sau khi hoàn tác
            // thì hướng cũ không còn nghĩa gì với người đọc.
            string undoneStepName = string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarDepthMoveLaneUndoStepFormat, typeId);
            return _presenter.ApplyEdit(new MoveEventTypeEdit(typeId, target), LiveOpsEditOperation.ReorderEventTypes, typeId,
                message, string.Empty, undoneStepName);
        }

        // ============================================================================================================ hoàn về bản so

        /// <summary>Mục này chỉ có nghĩa khi bản so CÓ mục đó — đợt mới thêm thì không có gì để hoàn về.</summary>
        public bool CanRevertToCompare(string entryKey)
        {
            return TryFindCompareEntry(entryKey, out FixedLiveEventEntry _, out FixedLiveEventEntry _);
        }

        /// <summary>
        /// Tìm mục tương ứng ở bản so. Khớp theo <c>EntryKey</c> TRƯỚC, rồi mới theo <c>EventId</c>: bản đã đăng được đọc lại từ
        /// JSON của dấu, mà JSON không mang EntryKey — mỗi lần parse sinh khoá mới. Khớp mỗi EntryKey thì "Hoàn về bản đã đăng"
        /// im lặng không bao giờ dùng được, đúng thứ đã làm test đầu tiên của lệnh này đỏ.
        /// </summary>
        private bool TryFindCompareEntry(string entryKey, out FixedLiveEventEntry draft, out FixedLiveEventEntry baseline)
        {
            draft = null;
            baseline = null;
            LiveEventCalendarDocument compare = CompareDocument;
            if (compare == null || string.IsNullOrEmpty(entryKey)) return false;
            if (!Document.TryGetFixedEvent(entryKey, out draft)) return false;
            if (compare.TryGetFixedEvent(entryKey, out baseline)) return true;
            IReadOnlyList<FixedLiveEventEntry> entries = compare.FixedEvents;
            for (int index = 0; index < entries.Count; index++)
            {
                if (!string.Equals(entries[index].EventId, draft.EventId, StringComparison.Ordinal)) continue;
                baseline = entries[index];
                return true;
            }
            baseline = null;
            return false;
        }

        /// <summary>
        /// "Hoàn về bản đã đăng" / (V-13) "Lấy bản trên đĩa cho mục này": thay ĐÚNG một mục bằng bản ở tài liệu so, một Undo group
        /// + toast. Không đụng mục nào khác — người dùng đang sửa cả lịch, không muốn mất mọi thay đổi vì một dòng.
        /// </summary>
        public bool RevertToCompare(string entryKey)
        {
            if (!TryFindCompareEntry(entryKey, out FixedLiveEventEntry draft, out FixedLiveEventEntry baseline)) return false;
            bool fromDisk = _services.Session.Publish.ActiveCompareSource == LiveOpsHubCompareSource.Disk;
            string message = string.Format(CultureInfo.InvariantCulture, fromDisk
                ? LiveOpsHubStrings.CalendarDepthTakeFromDiskToastFormat
                : LiveOpsHubStrings.CalendarDepthRevertToastFormat, baseline.EventId);
            // (phiếu D-5) Tên bước Undo theo đúng nguồn bản so đang dùng — cùng động từ với mục chuột phải người dùng vừa bấm.
            string undoneStepName = string.Format(CultureInfo.InvariantCulture, fromDisk
                ? LiveOpsHubStrings.CalendarDepthTakeFromDiskUndoStepFormat
                : LiveOpsHubStrings.CalendarDepthRevertUndoStepFormat, baseline.EventId);
            // Ghi vào ĐÚNG mục của nháp (giữ EntryKey của nháp) với giá trị của bản so — thay khoá là xoá rồi thêm, và mọi thứ
            // đang trỏ vào mục đó (lựa chọn, hover card, phát hiện) sẽ trỏ vào hư không.
            FixedLiveEventEntry reverted = new FixedLiveEventEntry(draft.EntryKey, baseline.EventId, baseline.EventType,
                baseline.StartUtcText, baseline.EndUtcText, baseline.ConfigKey);
            return _presenter.ApplyEdit(new ReplaceFixedEventEdit(reverted), LiveOpsEditOperation.ChangeFixedEventTimes, entryKey,
                message, string.Empty, undoneStepName);
        }

        // ============================================================================================================ dùng chung

        public void RequestFrame(string barKey)
        {
            LiveOpsTimelineBarModel bar = _presenter.FindBar(barKey);
            if (bar != null) FrameRequested?.Invoke(bar);
        }

        internal LiveEventCalendarDocument Document => _services.Session.Document ?? LiveEventCalendarDocument.Empty;

        private LiveEventCalendarDocument CompareDocument
        {
            get { return _services.Session.Publish == null ? null : _services.Session.Publish.CompareDocument; }
        }

        private int IndexOfEventType(string typeId)
        {
            IReadOnlyList<LiveEventTypeDefinition> types = Document.EventTypes;
            for (int index = 0; index < types.Count; index++)
            {
                if (string.Equals(types[index].TypeId, typeId, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        /// <summary>Thanh phải là đợt CỐ ĐỊNH thật: dải gom và đợt sinh từ luật không copy, không nhân bản, không xoá được.</summary>
        private FixedLiveEventEntry FixedEntryOf(string barKey)
        {
            LiveOpsTimelineBarModel bar = _presenter.FindBar(barKey);
            if (bar != null && (bar.IsStrip || bar.Source != LiveOpsTimelineBarSource.Fixed)) return null;
            return Document.TryGetFixedEvent(barKey, out FixedLiveEventEntry entry) ? entry : null;
        }

        private void ShowToast(string message)
        {
            _services.Bus.ShowToast(LiveOpsToastModel.Info(message));
        }
    }
}
