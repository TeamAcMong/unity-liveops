using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Luồng "Dán JSON đang chạy…" / "Thay nháp bằng JSON này…" / "Nhập JSON đang chạy…" ([SD2 §2.9], vá V-14) — adapter
    /// thật của <see cref="ILiveOpsHubActions"/>. Adapter tạm của bản dev đã được gỡ cùng gói này (sổ nhánh tạm mục 12, I-3).
    /// <para>
    /// Vì sao là MỘT lớp cho cả ba việc: cả ba bắt đầu từ cùng một ô dán và cùng một cái gate "parser của game đọc được
    /// chưa"; tách ra thì hai chỗ tự quyết "đọc được" theo hai cách. Bốn màn (Tổng quan, việc cần làm, cổng Xuất, hàng Chưa
    /// kiểm) chỉ gọi <c>services.Actions</c> nên không màn nào biết luồng này tồn tại.
    /// </para>
    /// <para>
    /// Ba bất biến: (1) chưa đọc được thì KHÔNG tạo asset, KHÔNG đụng nháp — nút chính của popover khoá kèm lý do in thành
    /// chữ (SPIKE-B SP-3); (2) thay nháp là thao tác phá huỷ nên phải qua hộp cấp 1 do <see cref="LiveOpsConfirmationPolicy"/>
    /// quyết mức, và áp trong ĐÚNG MỘT Undo group; (3) bản dán sống ở <see cref="LiveOpsHubRemoteSnapshot"/> (SessionState),
    /// không bao giờ làm asset bẩn — dán để so không phải là sửa lịch.
    /// </para>
    /// </summary>
    internal sealed class LiveOpsHubPasteRunningJsonAction : ILiveOpsHubActions
    {
        private const string ClauseSeparator = " · ";
        private const string IdSeparator = ", ";
        private const string ClauseEnd = ".";

        private readonly LiveOpsHubCalendarSession _session;
        private readonly LiveOpsHubSectionBus _bus;
        private readonly ILiveOpsHubConfirmationPresenter _confirmation;
        private readonly ILiveOpsHubFileDialog _fileDialog;
        private readonly ILiveOpsHubJsonReadBack _jsonReadBack;
        private readonly ILiveOpsHubLayoutLoader _layoutLoader;
        private readonly LiveOpsHubFormat _format;

        internal LiveOpsHubPasteRunningJsonAction(LiveOpsHubCalendarSession session, LiveOpsHubSectionBus bus,
            ILiveOpsHubConfirmationPresenter confirmation, ILiveOpsHubFileDialog fileDialog, ILiveOpsHubJsonReadBack jsonReadBack,
            ILiveOpsHubLayoutLoader layoutLoader, LiveOpsHubFormat format)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _confirmation = confirmation ?? throw new ArgumentNullException(nameof(confirmation));
            _fileDialog = fileDialog ?? throw new ArgumentNullException(nameof(fileDialog));
            _jsonReadBack = jsonReadBack ?? throw new ArgumentNullException(nameof(jsonReadBack));
            _layoutLoader = layoutLoader ?? throw new ArgumentNullException(nameof(layoutLoader));
            _format = format ?? throw new ArgumentNullException(nameof(format));
        }

        /// <summary>Luồng đã dựng nên nút luôn bật — lý do rỗng theo hợp đồng của port (V-20 C-2).</summary>
        public bool CanPasteRunningJson => true;

        public string PasteRunningJsonUnavailableReason => string.Empty;

        public bool CanImportRunningJson => true;

        public string ImportRunningJsonUnavailableReason => string.Empty;

        public void PasteRunningJson(Rect activatorWorldBound)
        {
            // Không có asset thì không có nháp để so hay để thay: mở đúng luồng nhập thay vì một popover không làm được gì.
            PasteRunningJsonMode mode = _session.Asset == null
                ? PasteRunningJsonMode.ImportIntoNewAsset
                : PasteRunningJsonMode.PasteIntoOpenCalendar;
            LiveOpsPopoverContent.ShowSingle(activatorWorldBound, CreatePopover(mode));
        }

        public void ImportRunningJsonIntoNewAsset(Rect activatorWorldBound)
        {
            // Đã có asset thì "nhập vào asset mới" là tạo cái thứ hai rồi bỏ rơi cái đang mở — dán vào asset đang mở mới đúng.
            PasteRunningJsonMode mode = _session.Asset == null
                ? PasteRunningJsonMode.ImportIntoNewAsset
                : PasteRunningJsonMode.PasteIntoOpenCalendar;
            LiveOpsPopoverContent.ShowSingle(activatorWorldBound, CreatePopover(mode));
        }

        /// <summary>
        /// Dựng popover với đúng callback của sản phẩm — test và kịch bản chụp gọi hàm này rồi <c>BuildForTest()</c> nên
        /// chúng đi qua CHÍNH cái gate "đọc được chưa" mà người dùng đi qua, không dựng lại một gate thứ hai.
        /// </summary>
        internal PasteRunningJsonPopover CreatePopover(PasteRunningJsonMode mode)
        {
            return new PasteRunningJsonPopover(mode, RemoteConfigKeyOfSession(), _jsonReadBack, _layoutLoader,
                CountUndeclaredEventTypes, Submit);
        }

        /// <summary>Việc sau khi người dùng bấm nút chính của popover — một chỗ rẽ nhánh duy nhất cho cả ba luồng.</summary>
        internal void Submit(PasteRunningJsonSubmission submission)
        {
            if (submission == null || submission.Document == null) return;
            if (_session.Asset == null)
            {
                ImportIntoNewAsset(submission);
                return;
            }
            if (submission.Choice == PasteRunningJsonChoice.ReplaceDraft)
            {
                ReplaceDraft(submission);
                return;
            }
            CompareOnly(submission);
        }

        /// <summary>
        /// "Chỉ so sánh với nháp": chỉ đặt bản remote của phiên. Luật 12 hết NotMeasured và luật 8 thấy loại lạ của bản
        /// remote — nên phải kiểm lại ngay, không đợi người dùng bấm F5 (bản vừa dán mà kết quả cũ là kết quả nói dối).
        /// </summary>
        private void CompareOnly(PasteRunningJsonSubmission submission)
        {
            _session.Remote.Set(submission.PastedText, _session.Clock.UtcNow);
            _session.StartCheck();
            _bus.ShowToast(LiveOpsToastModel.Info(LiveOpsHubStrings.PasteComparedToast));
            _bus.InvalidateHealth();
        }

        /// <summary>
        /// "Thay nháp bằng JSON này…": hộp cấp 1 nêu TỪNG id sẽ mất (bảng 7.0), rồi một Undo group. Bản dán cũng thành bản
        /// remote của phiên — người dùng vừa nói "đây là thứ đang chạy", giữ lại lời đó là điều kiện để luật 12 đo được.
        /// </summary>
        private void ReplaceDraft(PasteRunningJsonSubmission submission)
        {
            LiveEventCalendarDocument current = _session.Document;
            LiveEventCalendarDocument replaced = BuildReplacedDocument(current, submission.Document);

            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.ReplaceDraftWithJson,
                current, replaced, _session.Publish.ActiveBaseline, _session.Clock.UtcNow, string.Empty);
            if (decision.Requirement == LiveOpsConfirmRequirement.NotAllowed) return;

            LiveOpsConfirmRequest request = BuildReplaceConfirmRequest(submission.Document);
            if (_confirmation.Confirm(request) != LiveOpsConfirmResult.Destructive)
            {
                // Nút an toàn của hộp CHÍNH LÀ "Chỉ so sánh": bỏ luôn bản dán lúc này là bắt người dùng dán lại từ đầu.
                CompareOnly(submission);
                return;
            }

            LiveOpsHubEditOutcome outcome = _session.Apply(new ReplaceDocumentEdit(replaced), LiveOpsHubStrings.PasteReplaceUndoName);
            if (!outcome.Applied)
            {
                _bus.ShowOutcome(LiveOpsOutcomeRecord.Blocked(outcome.FailureText, string.Empty, _session.Clock.UtcNow));
                return;
            }
            _session.Remote.Set(submission.PastedText, _session.Clock.UtcNow);
            _session.StartCheck();
            _bus.ShowToast(LiveOpsToastModel.ForEdit(LiveOpsHubStrings.PasteReplaceUndoName, outcome.UndoGroup));
            _bus.InvalidateHealth();
        }

        /// <summary>
        /// Hộp cấp 1 của Hình 8 (ô 3) dựng từ NHÁP ĐANG MỞ và bản dán — kịch bản chụp gọi chính hàm này nên ảnh chứng minh
        /// câu mà sản phẩm ghép, không phải một chuỗi tiếng Việt viết cứng trong file kịch bản.
        /// </summary>
        internal LiveOpsConfirmRequest BuildReplaceConfirmRequest(LiveEventCalendarDocument pastedDocument)
        {
            if (pastedDocument == null) throw new ArgumentNullException(nameof(pastedDocument));
            List<string> replacedEventIds = new List<string>();
            List<string> droppedEventIds = new List<string>();
            List<string> changedRuleTypes = new List<string>();
            CollectReplaceImpact(_session.Document, pastedDocument, replacedEventIds, droppedEventIds, changedRuleTypes);
            int itemCount = replacedEventIds.Count + droppedEventIds.Count + changedRuleTypes.Count;
            return new LiveOpsConfirmRequest.Builder()
                .WithLevel(LiveOpsConfirmLevel.Level1)
                .WithTitle(LiveOpsHubStrings.PasteReplaceConfirmTitle)
                .WithBody(ReplaceConfirmBody(replacedEventIds, droppedEventIds, changedRuleTypes))
                .WithKeyHint(LiveOpsHubStrings.PasteReplaceConfirmKeyHint)
                .WithButtons(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteReplaceConfirmDestructiveFormat,
                        _format.Integer(itemCount)),
                    LiveOpsHubStrings.PasteReplaceConfirmSafe)
                .Build();
        }

        /// <summary>
        /// "Nhập JSON đang chạy…" khi chưa có asset (V-14 bước 3–6): chọn nơi lưu → tạo asset → một Undo group ghi tài liệu
        /// (kèm dấu đã đăng nếu Toggle bật) → lưu → outcome. Huỷ hộp lưu = không tạo gì; tạo asset thất bại = báo rồi dừng.
        /// </summary>
        private void ImportIntoNewAsset(PasteRunningJsonSubmission submission)
        {
            string chosenPath = _fileDialog.SaveFile(LiveOpsHubStrings.PasteImportSaveDialogTitle,
                LiveOpsHubPaths.ImportCalendarAssetDirectory, LiveOpsHubPaths.ImportCalendarAssetDefaultName,
                LiveOpsHubPaths.ImportCalendarAssetExtension);
            string assetPath = ToProjectRelativeAssetPath(chosenPath);
            if (assetPath.Length == 0) return;

            if (!_session.TryCreateAsset(assetPath))
            {
                // Hộp KHÔNG phá huỷ nên vẫn là DisplayDialog (7.0) — không dựng một LiveOpsConfirmWindow chỉ để nói "hỏng rồi".
                EditorUtility.DisplayDialog(LiveOpsHubStrings.PasteImportCreateFailedTitle,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteImportCreateFailedFormat, assetPath),
                    LiveOpsHubStrings.PasteImportCreateFailedCloseButton);
                return;
            }

            string assetFileName = _session.AssetFileName;
            LiveEventCalendarDocument imported = BuildImportedDocument(submission, assetFileName);
            string undoName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteImportUndoNameFormat, assetFileName);
            LiveOpsHubEditOutcome outcome = _session.Apply(new ReplaceDocumentEdit(imported), undoName);
            if (!outcome.Applied)
            {
                _bus.ShowOutcome(LiveOpsOutcomeRecord.Blocked(outcome.FailureText, string.Empty, _session.Clock.UtcNow));
                return;
            }

            _session.Remote.Set(submission.PastedText, _session.Clock.UtcNow);
            _session.Save();
            _session.StartCheck();

            // Tạo file asset KHÔNG nằm trong Undo của Unity: Undo trả nội dung về rỗng nhưng file vẫn còn — toast phải nói ra.
            string undoneStep = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteImportUndoneStepFormat, assetFileName);
            _bus.ShowToast(LiveOpsToastModel.ForEdit(undoName, outcome.UndoGroup, string.Empty, undoneStep));
            _bus.ShowOutcome(ImportOutcome(submission, assetFileName));
            _bus.InvalidateHealth();
        }

        private LiveOpsOutcomeRecord ImportOutcome(PasteRunningJsonSubmission submission, string assetFileName)
        {
            LiveEventCalendarDocument document = submission.Document;
            string headline = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteImportOutcomeHeadlineFormat,
                assetFileName, _format.Integer(document.RecurringRules.Count), _format.Integer(document.FixedEvents.Count));
            int undeclaredTypeCount = CountUndeclaredEventTypes(document);
            if (undeclaredTypeCount <= 0)
            {
                return LiveOpsOutcomeRecord.Ok(headline, LiveOpsHubStrings.PasteImportOutcomeReadyDetail, _session.Clock.UtcNow);
            }
            string detail = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteImportOutcomeUnknownTypesFormat,
                _format.Integer(undeclaredTypeCount));
            // ActionId là id MÀN: khung điều hướng theo id, nên "Mở Loại event" không cần luồng này biết gì về cửa sổ.
            return LiveOpsOutcomeRecord.Ok(headline, detail, _session.Clock.UtcNow, LiveOpsHubSections.Ids.EventTypes);
        }

        /// <summary>
        /// Tài liệu sau khi thay nháp: ĐỢT + LUẬT lấy từ bản dán; loại event, dấu đã đăng, cảnh báo đã bỏ qua và key remote
        /// GIỮ của nháp — JSON đang chạy không mang ba thứ đó, lấy của nó là xoá luôn lịch sử đăng (cùng luật với 7.6
        /// "Khôi phục vào nháp").
        /// <para>
        /// <c>entryKey</c> ghép theo ID ĐỢT (mục 6.2): mục trùng id giữ key cũ nên lựa chọn trên timeline, toast và Undo
        /// không nhảy mục; mục mới sinh key mới.
        /// </para>
        /// </summary>
        private static LiveEventCalendarDocument BuildReplacedDocument(LiveEventCalendarDocument current, LiveEventCalendarDocument pasted)
        {
            Dictionary<string, string> entryKeyByEventId = new Dictionary<string, string>(StringComparer.Ordinal);
            IReadOnlyList<FixedLiveEventEntry> currentEvents = current.FixedEvents;
            for (int index = 0; index < currentEvents.Count; index++)
            {
                FixedLiveEventEntry entry = currentEvents[index];
                if (entry.EventId.Length > 0 && !entryKeyByEventId.ContainsKey(entry.EventId)) entryKeyByEventId.Add(entry.EventId, entry.EntryKey);
            }

            LiveEventCalendarDocumentBuilder builder = new LiveEventCalendarDocumentBuilder()
                .WithRemoteConfigKey(current.RemoteConfigKey);
            foreach (LiveEventTypeDefinition type in current.EventTypes) builder.WithEventType(type);
            foreach (RecurringLiveEventRule rule in pasted.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in pasted.FixedEvents)
            {
                string entryKey;
                if (!entryKeyByEventId.TryGetValue(entry.EventId, out entryKey)) entryKey = FixedLiveEventEntry.CreateEntryKey();
                builder.WithFixedEvent(new FixedLiveEventEntry(entryKey, entry.EventId, entry.EventType, entry.StartUtcText,
                    entry.EndUtcText, entry.ConfigKey));
            }
            foreach (PublishedCalendarStamp stamp in current.PublishedStamps) builder.WithPublishedStamp(stamp);
            foreach (IgnoredCalendarWarning warning in current.IgnoredWarnings) builder.WithIgnoredWarning(warning);
            return builder.Build();
        }

        /// <summary>
        /// Tài liệu của asset vừa tạo (V-14 bước 4): <c>entryKey</c> sinh MỚI cho mọi đợt (không có nháp cũ để bám),
        /// key remote mặc định, <b>EventTypes rỗng</b> (PD-43 — hub không đoán hộ định nghĩa loại) và, khi Toggle bật, một
        /// dấu đã đăng tính trên đúng byte của bản dán.
        /// </summary>
        private LiveEventCalendarDocument BuildImportedDocument(PasteRunningJsonSubmission submission, string assetFileName)
        {
            LiveEventCalendarDocument pasted = submission.Document;
            LiveEventCalendarDocumentBuilder builder = new LiveEventCalendarDocumentBuilder()
                .WithRemoteConfigKey(LiveEventCalendarDocument.DefaultRemoteConfigKey);
            foreach (RecurringLiveEventRule rule in pasted.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in pasted.FixedEvents)
            {
                builder.WithFixedEvent(new FixedLiveEventEntry(FixedLiveEventEntry.CreateEntryKey(), entry.EventId, entry.EventType,
                    entry.StartUtcText, entry.EndUtcText, entry.ConfigKey));
            }
            if (submission.StampAsPublished) builder.WithPublishedStamp(BuildImportStamp(submission));
            return builder.Build();
        }

        private PublishedCalendarStamp BuildImportStamp(PasteRunningJsonSubmission submission)
        {
            byte[] pastedBytes = Encoding.UTF8.GetBytes(submission.PastedText);
            return new PublishedCalendarStamp(LiveEventUtcText.Format(_session.Clock.UtcNow),
                _session.PublisherIdentity.PublisherName, LiveEventCalendarSha256.ComputeHex(pastedBytes), pastedBytes.Length,
                submission.FormatVersion, LiveOpsHubStrings.PasteImportStampNote, submission.PastedText);
        }

        /// <summary>
        /// Ba vế của thân hộp ([SD2 §2.9]): đợt bị ghi đè (nêu từng id), đợt bị xoá hẳn, luật lặp bị đổi. Vế nào rỗng thì
        /// bỏ hẳn — "xoá 0 đợt" đọc như một cảnh báo về thứ không xảy ra.
        /// </summary>
        private static void CollectReplaceImpact(LiveEventCalendarDocument current, LiveEventCalendarDocument pasted,
            List<string> replacedEventIds, List<string> droppedEventIds, List<string> changedRuleTypes)
        {
            HashSet<string> pastedEventIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (FixedLiveEventEntry entry in pasted.FixedEvents) pastedEventIds.Add(entry.EventId);
            foreach (FixedLiveEventEntry entry in current.FixedEvents)
            {
                if (pastedEventIds.Contains(entry.EventId)) replacedEventIds.Add(entry.EventId);
                else droppedEventIds.Add(entry.EventId);
            }

            Dictionary<string, string> pastedRuleTextByType = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (RecurringLiveEventRule rule in pasted.RecurringRules) pastedRuleTextByType[rule.EventType] = rule.CanonicalText;
            HashSet<string> seenRuleTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (RecurringLiveEventRule rule in current.RecurringRules)
            {
                seenRuleTypes.Add(rule.EventType);
                string pastedText;
                bool kept = pastedRuleTextByType.TryGetValue(rule.EventType, out pastedText);
                if (!kept || !string.Equals(pastedText, rule.CanonicalText, StringComparison.Ordinal)) changedRuleTypes.Add(rule.EventType);
            }
            foreach (RecurringLiveEventRule rule in pasted.RecurringRules)
            {
                if (!seenRuleTypes.Contains(rule.EventType)) changedRuleTypes.Add(rule.EventType);
            }
        }

        private string ReplaceConfirmBody(List<string> replacedEventIds, List<string> droppedEventIds, List<string> changedRuleTypes)
        {
            List<string> clauses = new List<string>();
            if (replacedEventIds.Count > 0)
            {
                clauses.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteReplaceConfirmReplacedFormat,
                    _format.Integer(replacedEventIds.Count), string.Join(IdSeparator, replacedEventIds.ToArray())));
            }
            if (droppedEventIds.Count > 0)
            {
                clauses.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteReplaceConfirmDroppedFormat,
                    string.Join(IdSeparator, droppedEventIds.ToArray())));
            }
            if (changedRuleTypes.Count > 0)
            {
                clauses.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteReplaceConfirmRulesFormat,
                    string.Join(IdSeparator, changedRuleTypes.ToArray())));
            }
            if (_session.HasUnsavedChanges)
            {
                clauses.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteReplaceConfirmUnsavedFormat,
                    _session.AssetFileName, _format.Integer(_session.UnsavedDiff.ChangeCount)));
            }
            string sentence = string.Join(ClauseSeparator, clauses.ToArray());
            if (sentence.Length > 0) sentence += ClauseEnd;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteReplaceConfirmBodyFormat, sentence).Trim();
        }

        /// <summary>Loại có trong bản dán mà asset đang mở chưa khai báo — chưa có asset thì mọi loại đều chưa khai báo (PD-43).</summary>
        private int CountUndeclaredEventTypes(LiveEventCalendarDocument pasted)
        {
            if (pasted == null) return 0;
            HashSet<string> declaredTypes = new HashSet<string>(StringComparer.Ordinal);
            LiveEventCalendarDocument current = _session.Document;
            if (_session.Asset != null && current != null)
            {
                foreach (LiveEventTypeDefinition type in current.EventTypes) declaredTypes.Add(type.TypeId);
            }

            HashSet<string> undeclaredTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (FixedLiveEventEntry entry in pasted.FixedEvents)
            {
                if (entry.EventType.Length > 0 && !declaredTypes.Contains(entry.EventType)) undeclaredTypes.Add(entry.EventType);
            }
            foreach (RecurringLiveEventRule rule in pasted.RecurringRules)
            {
                if (rule.EventType.Length > 0 && !declaredTypes.Contains(rule.EventType)) undeclaredTypes.Add(rule.EventType);
            }
            return undeclaredTypes.Count;
        }

        private string RemoteConfigKeyOfSession()
        {
            LiveEventCalendarDocument document = _session.Document;
            if (_session.Asset == null || document == null || document.RemoteConfigKey.Length == 0)
            {
                return LiveEventCalendarDocument.DefaultRemoteConfigKey;
            }
            return document.RemoteConfigKey;
        }

        /// <summary>
        /// Hộp lưu của Editor trả đường dẫn TUYỆT ĐỐI, còn <see cref="LiveOpsHubCalendarSession.TryCreateAsset"/> chỉ nhận
        /// đường dẫn trong <c>Assets/</c>. Quy đổi ở đây chứ không sửa port: adapter Manual của test trả thẳng đường dẫn
        /// dự án, và cả hai đường phải về cùng một dạng trước khi tới phiên. Ngoài project = "" (không tạo gì).
        /// </summary>
        private static string ToProjectRelativeAssetPath(string chosenPath)
        {
            if (string.IsNullOrEmpty(chosenPath)) return string.Empty;
            string normalized = chosenPath.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.Ordinal)) return normalized;

            string dataPath = Application.dataPath.Replace('\\', '/');
            if (normalized.StartsWith(dataPath + "/", StringComparison.Ordinal))
            {
                return "Assets/" + normalized.Substring(dataPath.Length + 1);
            }
            // Người dùng chọn ra ngoài project: Unity không nạp được asset ở đó, báo bằng đường "không tạo gì" thay vì tạo file chết.
            return string.Empty;
        }

        /// <summary>Tên file asset của đường dẫn — dùng trong câu Undo và toast khi phiên chưa kịp nạp xong.</summary>
        internal static string AssetFileNameOf(string assetPath)
        {
            return string.IsNullOrEmpty(assetPath) ? string.Empty : Path.GetFileName(assetPath);
        }
    }
}
