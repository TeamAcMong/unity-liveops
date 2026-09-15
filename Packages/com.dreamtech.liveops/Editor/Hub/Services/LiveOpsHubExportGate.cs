using System;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Unity;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Năm dòng của card "Cổng xuất" theo đúng thứ tự vẽ [SD2 §3.4]. Số giá trị = vị trí trong <see cref="ExportGateState.Rows"/>.</summary>
    internal enum ExportGateRowKind
    {
        NoDroppedEntries = 0,
        ParserReadBack = 1,
        CheckFreshness = 2,
        RemoteSnapshot = 3,
        RequiredChangesReviewed = 4,
    }

    /// <summary>
    /// Trạng thái một dòng cổng. Tách khỏi <see cref="HealthState"/> vì cổng có thêm "đang chạy" (spinner trong dòng) và "ẩn"
    /// (dòng 5 ở lần đăng đầu) — hai trạng thái không có nghĩa với rail.
    /// </summary>
    internal enum ExportGateRowState
    {
        Hidden = 0,
        Ok = 1,
        Warning = 2,
        Blocked = 3,
        NotMeasured = 4,
        Running = 5,
    }

    /// <summary>Việc của nút/chevron trên một dòng cổng — card (G-EXPORT) nối việc thật; model không biết cửa sổ.</summary>
    internal enum ExportGateRowAction
    {
        None = 0,
        OpenValidationDropped = 1,
        CopyReadBackError = 2,
        StartCheck = 3,
        PasteRunningJson = 4,
        CompareWithRemote = 5,
        ShowPublishedDiff = 6,
    }

    /// <summary>
    /// Mã trạng thái màn Xuất JSON theo bảng [SD2 §3.10]. Mã chính (<see cref="ExportGateState.StatusCode"/>) quyết định ba nút;
    /// (g), (i), (j) là trạng thái kèm — chỉ thay chữ trong cùng bố cục nên đi cùng một mã chính (vd lần đăng đầu mà còn đợt bị bỏ
    /// là (a) + (g)), đọc bằng <see cref="ExportGateState.HasStatus"/>.
    /// </summary>
    internal enum ExportGateStatusCode
    {
        /// <summary>(a) còn đợt bị bỏ (có thể kèm thay đổi bắt buộc chưa xem).</summary>
        Blocked = 0,

        /// <summary>(b) đủ điều kiện, chưa copy hay lưu file đúng sha đang hiện.</summary>
        Ready = 1,

        /// <summary>(c)/(c′) đã copy hoặc lưu file đúng sha đang hiện — hai mã chỉ khác outcome, ba nút như nhau.</summary>
        Exported = 2,

        /// <summary>(d) Kiểm lịch cũ, chưa chạy lần nào, hoặc đang chạy.</summary>
        StaleCheck = 3,

        /// <summary>(e) vừa ghi dấu: nháp trùng dấu đang là bản so và đúng bản đã copy.</summary>
        MarkedPublished = 4,

        /// <summary>(f) không có thay đổi so với bản so.</summary>
        NoChanges = 5,

        /// <summary>(g) chưa có dấu đã đăng — trạng thái kèm.</summary>
        FirstPublish = 6,

        /// <summary>(h) parser của game không đọc lại được JSON do hub tạo.</summary>
        ReadBackFailed = 7,

        /// <summary>(i) nháp đang là bản khôi phục từ lịch sử — trạng thái kèm.</summary>
        Restoring = 8,

        /// <summary>(j) đang chọn định dạng 1 — trạng thái kèm.</summary>
        Format1 = 9,

        /// <summary>(k) hết đợt bị bỏ, còn thay đổi bắt buộc chưa xem.</summary>
        RequiredReviewOnly = 10,

        /// <summary>Parser đọc lại lệch dãy mục của Kiểm lịch (V-6) — lỗi của hub.</summary>
        ReadBackMismatch = 11,
    }

    /// <summary>Một dòng của card "Cổng xuất": dấu + chữ (+ meta / nút / chevron) [SD2 §3.4]. Bất biến.</summary>
    internal sealed class ExportGateRow
    {
        internal ExportGateRow(ExportGateRowKind kind, ExportGateRowState state, string text, string narrowText, string metaText,
            ExportGateRowAction action, string actionText, LiveOpsHubNavigation navigation)
        {
            Kind = kind;
            State = state;
            Text = text ?? string.Empty;
            NarrowText = string.IsNullOrEmpty(narrowText) ? Text : narrowText;
            MetaText = metaText ?? string.Empty;
            Action = action;
            ActionText = actionText ?? string.Empty;
            Navigation = navigation;
        }

        public ExportGateRowKind Kind { get; }
        public ExportGateRowState State { get; }
        public string Text { get; }

        /// <summary>Chữ khi cửa sổ hẹp ("Bản remote: chưa dán") — bằng <see cref="Text"/> khi thiết kế không có bản rút gọn.</summary>
        public string NarrowText { get; }

        /// <summary>Meta 10px sau chữ ("1 không đọc được · 1 bị bỏ vì chồng giờ"); "" khi không có.</summary>
        public string MetaText { get; }

        public ExportGateRowAction Action { get; }

        /// <summary>Nhãn nút trong dòng ("Kiểm lại (F5)", "Copy lỗi"); "" = dòng chỉ có chevron hoặc không bấm được.</summary>
        public string ActionText { get; }

        /// <summary>Điều hướng khi bấm dòng (Kiểm lịch lọc Bị bỏ, Xuất so với remote); null khi dòng không dẫn tới màn khác.</summary>
        public LiveOpsHubNavigation Navigation { get; }

        public bool IsVisible => State != ExportGateRowState.Hidden;

        /// <summary>Dòng thuộc nhóm có thể chặn Copy (1, 2, 3, 5 — dòng Bản remote không bao giờ chặn) và đang hiện.</summary>
        public bool CanBlockCopy => Kind != ExportGateRowKind.RemoteSnapshot && IsVisible;

        /// <summary>Dòng đang chặn Copy JSON: thuộc nhóm chặn được và chưa đạt (Blocked, chưa kiểm, hay đang chạy đều chưa là bằng chứng).</summary>
        public bool BlocksCopy => CanBlockCopy && State != ExportGateRowState.Ok;
    }

    /// <summary>Trạng thái một nút header (Copy JSON · Lưu file… · Đánh dấu đã đăng…). Tooltip đặt trên slot vì nút disabled không nhận hover.</summary>
    internal sealed class ExportGateButton
    {
        internal ExportGateButton(bool isEnabled, bool isPrimary, string tooltip)
        {
            IsEnabled = isEnabled;
            IsPrimary = isPrimary;
            Tooltip = tooltip ?? string.Empty;
        }

        public bool IsEnabled { get; }

        /// <summary>Nút đậm — "bước tiếp theo thật", nên có thể đậm cả khi đang disabled [SD2 §3.2].</summary>
        public bool IsPrimary { get; }

        public string Tooltip { get; }
    }

    /// <summary>Card lỗi đầu thân của trạng thái (h) [SD2 §3.10]: triệu chứng, message runtime nguyên văn, vị trí, hành động, note.</summary>
    internal sealed class ExportGateReadBackFailure
    {
        internal ExportGateReadBackFailure(string runtimeMessage, int errorLine, int errorColumn, string errorReportText)
        {
            RuntimeMessage = runtimeMessage ?? string.Empty;
            ErrorLine = errorLine > 0 ? errorLine : 0;
            ErrorColumn = errorLine > 0 && errorColumn > 0 ? errorColumn : 0;
            PositionText = ErrorLine > 0
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateFailurePositionFormat, ErrorLine, ErrorColumn)
                : string.Empty;
            OpenLineButtonText = ErrorLine > 0
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateFailureOpenLineFormat, ErrorLine)
                : string.Empty;
            ErrorReportText = errorReportText ?? string.Empty;
        }

        public string SymptomText => LiveOpsHubStrings.ExportGateFailureSymptom;

        /// <summary>Message của runtime nguyên văn (vẽ mono, bọc noparse ở view) — không dịch, dev cần đúng chữ để tìm.</summary>
        public string RuntimeMessage { get; }

        /// <summary>1-based; 0 khi không tìm được vị trí (JsonUtility không trả vị trí — chỉ bộ dò cú pháp tìm được).</summary>
        public int ErrorLine { get; }

        public int ErrorColumn { get; }

        /// <summary>"ở dòng 65, cột 1"; "" khi không có vị trí.</summary>
        public string PositionText { get; }

        public string CopyErrorButtonText => LiveOpsHubStrings.ExportGateCopyErrorAction;

        /// <summary>"Mở JSON ở dòng 65"; "" khi không có vị trí — không vẽ nút mở tới một dòng không biết.</summary>
        public string OpenLineButtonText { get; }

        public string NoteText => LiveOpsHubStrings.ExportGateFailureNote;

        /// <summary>Chuỗi đưa vào clipboard khi bấm "Copy lỗi".</summary>
        public string ErrorReportText { get; }
    }

    /// <summary>
    /// Đầu vào của <see cref="ExportGateModel.Evaluate"/> (V-9). CHỈ nhận kiểu core/Unity và giá trị thuần — không nhận kiểu của
    /// phiên (<c>LiveOpsHubCheckState</c>, <c>LiveOpsHubPublishState</c>) để gói cổng không phải chờ G-SESSION và để test dựng
    /// mọi trạng thái (a)–(k) từ dữ liệu thật mà không cần cửa sổ. Bất biến: mỗi <c>With…</c> trả bản sao, nên phiên giữ một đầu
    /// vào cũ để so mà không sợ bị sửa tại chỗ.
    /// </summary>
    internal sealed class ExportGateInput
    {
        /// <param name="json">JSON đúng như Copy/Lưu file sẽ đưa đi (bản đã định dạng — PD-12).</param>
        /// <param name="draftCompilation">Nháp biên dịch theo thứ tự xuất (<c>CompileInExportOrder</c>, V-6) — dãy mục Kiểm lịch nói.</param>
        /// <param name="readBack">Kết quả parser của game đọc lại <paramref name="json"/> qua <c>ILiveOpsHubJsonReadBack</c> (V-16).</param>
        /// <param name="readBackIsReadable">false khi parser không đọc được JSON (trạng thái h).</param>
        /// <param name="readBackErrorText">Message runtime khi không đọc được; "" khi đọc được.</param>
        /// <param name="displayFormat">Bộ định dạng chữ ngày giờ/số của hub.</param>
        public ExportGateInput(LiveEventCalendarJsonText json, LiveEventCalendarCompilation draftCompilation, LiveEventCalendarParseResult readBack,
            bool readBackIsReadable, string readBackErrorText, LiveOpsHubFormat displayFormat)
        {
            Json = json ?? throw new ArgumentNullException(nameof(json), LiveOpsHubStrings.ExportGateErrorJsonMissing);
            DraftCompilation = draftCompilation ?? throw new ArgumentNullException(nameof(draftCompilation), LiveOpsHubStrings.ExportGateErrorCompilationMissing);
            DisplayFormat = displayFormat ?? throw new ArgumentNullException(nameof(displayFormat), LiveOpsHubStrings.ExportGateErrorFormatMissing);
            // Đọc được mà không có kết quả là lỗi lập trình của nơi gọi — coi như không đọc được sẽ giấu lỗi đó sau card (h).
            if (readBackIsReadable && readBack == null) throw new ArgumentNullException(nameof(readBack), LiveOpsHubStrings.ExportGateErrorReadBackMissing);
            ReadBack = readBack;
            ReadBackIsReadable = readBackIsReadable;
            ReadBackErrorText = readBackErrorText ?? string.Empty;
            LastExportedSha256Hex = string.Empty;
        }

        public LiveEventCalendarJsonText Json { get; private set; }

        /// <summary>Định dạng đang chọn — đọc từ chính JSON để không bao giờ lệch với chuỗi đang được copy.</summary>
        public LiveEventCalendarJsonFormat JsonFormat => Json.Format;

        public LiveEventCalendarCompilation DraftCompilation { get; private set; }
        public LiveEventCalendarParseResult ReadBack { get; private set; }
        public bool ReadBackIsReadable { get; private set; }
        public string ReadBackErrorText { get; private set; }
        public int ReadBackErrorLine { get; private set; }
        public int ReadBackErrorColumn { get; private set; }
        public LiveOpsHubFormat DisplayFormat { get; private set; }

        /// <summary>null = chưa kiểm lần nào.</summary>
        public LiveEventCalendarCheckReport LastReport { get; private set; }

        public bool IsCheckStale { get; private set; }
        public bool IsCheckRunning { get; private set; }
        public int CompletedRuleCount { get; private set; }
        public int RuleCount { get; private set; }

        /// <summary>Giờ của lần kiểm gần nhất; mặc định lấy từ báo cáo.</summary>
        public DateTime? CheckedAtUtc { get; private set; }

        /// <summary>Lúc lịch đổi sau lần kiểm (câu "lịch đổi lúc 08:46:50"); null khi không biết.</summary>
        public DateTime? DraftChangedAtUtc { get; private set; }

        /// <summary>Mốc thời gian đã qua sau lần kiểm (PD-23 <c>MilestonePassed</c>); null khi kết quả cũ vì sửa.</summary>
        public DateTime? PassedMilestoneUtc { get; private set; }

        /// <summary>Dấu đang là bản so; null = lần đăng đầu (g).</summary>
        public PublishedCalendarStamp ActiveStamp { get; private set; }

        public LiveEventCalendarDiffResult PublishedDiff { get; private set; }
        public int ReviewedRequiredCount { get; private set; }

        /// <summary>sha đủ 64 ký tự của lần Copy/Lưu file gần nhất trong phiên; "" khi chưa.</summary>
        public string LastExportedSha256Hex { get; private set; }

        public bool RemotePasted { get; private set; }
        public bool RemoteMatchesStamp { get; private set; }
        public DateTime? RemoteVerifiedUtc { get; private set; }

        /// <summary>Dấu mà nháp vừa được khôi phục từ đó (i); null khi không khôi phục.</summary>
        public PublishedCalendarStamp RestoredFromStamp { get; private set; }

        /// <summary>Undo group khôi phục còn trên đỉnh — "Hoàn tác khôi phục" disabled khi false.</summary>
        public bool CanUndoRestore { get; private set; }

        /// <summary>Kết quả Kiểm lịch. <paramref name="lastReport"/> null = chưa kiểm lần nào; đang chạy giữ báo cáo cũ (nếu có).</summary>
        public ExportGateInput WithCheck(LiveEventCalendarCheckReport lastReport, bool isStale, bool isRunning, int completedRuleCount, int ruleCount,
            DateTime? checkedAtUtc)
        {
            // Kiểm riêng từng số để ParamName chỉ đúng tham số âm — nơi gọi đọc tên đó để biết bộ đếm nào của phiên hỏng.
            if (completedRuleCount < 0) throw new ArgumentOutOfRangeException(nameof(completedRuleCount), LiveOpsHubStrings.ExportGateErrorNegativeCount);
            if (ruleCount < 0) throw new ArgumentOutOfRangeException(nameof(ruleCount), LiveOpsHubStrings.ExportGateErrorNegativeCount);
            ExportGateInput copy = Copy();
            copy.LastReport = lastReport;
            copy.IsCheckStale = isStale;
            copy.IsCheckRunning = isRunning;
            copy.CompletedRuleCount = completedRuleCount;
            copy.RuleCount = ruleCount;
            copy.CheckedAtUtc = checkedAtUtc ?? (lastReport != null ? lastReport.CheckedAtUtc : (DateTime?)null);
            return copy;
        }

        /// <summary>Vì sao kết quả cũ — chỉ để câu dòng 3 nói đúng ("lịch đổi lúc …" hay "đã qua mốc …").</summary>
        public ExportGateInput WithStaleCause(DateTime? draftChangedAtUtc, DateTime? passedMilestoneUtc)
        {
            ExportGateInput copy = Copy();
            copy.DraftChangedAtUtc = draftChangedAtUtc;
            copy.PassedMilestoneUtc = passedMilestoneUtc;
            return copy;
        }

        /// <summary>
        /// Vị trí lỗi đọc lại khi nơi gọi biết (bộ dò cú pháp chạy trên đúng chuỗi parser đã đọc). Không gọi thì model dò trên
        /// <see cref="Json"/> — đúng cho đường thật, vì parser đọc chính chuỗi đó.
        /// </summary>
        public ExportGateInput WithReadBackErrorLocation(int line, int column)
        {
            ExportGateInput copy = Copy();
            copy.ReadBackErrorLine = line > 0 ? line : 0;
            copy.ReadBackErrorColumn = column > 0 ? column : 0;
            return copy;
        }

        /// <summary>Bản so: dấu đang Active, diff của nháp với dấu đó, số thay đổi bắt buộc đã tick "Đã xem".</summary>
        public ExportGateInput WithPublished(PublishedCalendarStamp activeStamp, LiveEventCalendarDiffResult publishedDiff, int reviewedRequiredCount)
        {
            if (reviewedRequiredCount < 0) throw new ArgumentOutOfRangeException(nameof(reviewedRequiredCount), LiveOpsHubStrings.ExportGateErrorNegativeCount);
            ExportGateInput copy = Copy();
            copy.ActiveStamp = activeStamp;
            copy.PublishedDiff = publishedDiff;
            copy.ReviewedRequiredCount = reviewedRequiredCount;
            return copy;
        }

        public ExportGateInput WithLastExportedSha256Hex(string lastExportedSha256Hex)
        {
            ExportGateInput copy = Copy();
            copy.LastExportedSha256Hex = lastExportedSha256Hex ?? string.Empty;
            return copy;
        }

        public ExportGateInput WithRemote(bool remotePasted, bool remoteMatchesStamp, DateTime? remoteVerifiedUtc)
        {
            ExportGateInput copy = Copy();
            copy.RemotePasted = remotePasted;
            copy.RemoteMatchesStamp = remotePasted && remoteMatchesStamp;
            copy.RemoteVerifiedUtc = remoteVerifiedUtc;
            return copy;
        }

        public ExportGateInput WithRestoring(PublishedCalendarStamp restoredFromStamp, bool canUndoRestore)
        {
            ExportGateInput copy = Copy();
            copy.RestoredFromStamp = restoredFromStamp;
            copy.CanUndoRestore = restoredFromStamp != null && canUndoRestore;
            return copy;
        }

        private ExportGateInput Copy()
        {
            return (ExportGateInput)MemberwiseClone();
        }
    }

    /// <summary>
    /// Kết quả của <see cref="ExportGateModel.Evaluate"/>: 5 dòng cổng, dạng gọn, 3 nút, lý do cạnh nút, mã (a)–(k), health màn
    /// Xuất và cột "chặn Copy JSON" cho Tổng quan. Bất biến — card (G-EXPORT), rail/health (G-SESSION) và Tổng quan (G-OVERVIEW)
    /// đọc cùng một đối tượng nên không màn nào tự suy lại điều kiện Copy.
    /// </summary>
    internal sealed class ExportGateState
    {
        private readonly HashSet<ExportGateStatusCode> _statusCodes;

        internal ExportGateState(ExportGateStatusCode statusCode, IReadOnlyList<ExportGateStatusCode> statusCodes, IReadOnlyList<ExportGateRow> rows,
            ExportGateButton copy, ExportGateButton saveFile, ExportGateButton markPublished, string reasonText, bool reasonIsBlocked,
            bool isCompact, int compactConditionCount, string compactText, string compactRemoteText, string cardMetaText, SectionHealth health,
            ExportGateReadBackFailure readBackFailure, string readBackErrorReportText, int droppedCount, int unreviewedRequiredCount,
            string diffEmptyText, string restoringNoticeText, bool canUndoRestore, string format1NoticeText)
        {
            StatusCode = statusCode;
            StatusCodes = statusCodes;
            _statusCodes = new HashSet<ExportGateStatusCode>(statusCodes);
            Rows = rows;
            Copy = copy;
            SaveFile = saveFile;
            MarkPublished = markPublished;
            ReasonText = reasonText ?? string.Empty;
            ReasonIsBlocked = reasonIsBlocked;
            IsCompact = isCompact;
            CompactConditionCount = compactConditionCount;
            CompactText = compactText ?? string.Empty;
            CompactRemoteText = compactRemoteText ?? string.Empty;
            CardMetaText = cardMetaText ?? string.Empty;
            Health = health;
            ReadBackFailure = readBackFailure;
            ReadBackErrorReportText = readBackErrorReportText ?? string.Empty;
            DroppedCount = droppedCount;
            UnreviewedRequiredCount = unreviewedRequiredCount;
            DiffEmptyText = diffEmptyText ?? string.Empty;
            RestoringNoticeText = restoringNoticeText ?? string.Empty;
            CanUndoRestore = canUndoRestore;
            Format1NoticeText = format1NoticeText ?? string.Empty;
        }

        /// <summary>Mã chính — quyết định ba nút.</summary>
        public ExportGateStatusCode StatusCode { get; }

        /// <summary>Mã chính trước, rồi mã kèm (g), (i), (j) theo thứ tự đó.</summary>
        public IReadOnlyList<ExportGateStatusCode> StatusCodes { get; }

        /// <summary>Luôn đủ 5 dòng theo <see cref="ExportGateRowKind"/> (dòng ẩn vẫn có mặt, <see cref="ExportGateRow.IsVisible"/> = false).</summary>
        public IReadOnlyList<ExportGateRow> Rows { get; }

        public ExportGateButton Copy { get; }
        public ExportGateButton SaveFile { get; }
        public ExportGateButton MarkPublished { get; }

        /// <summary>Chữ cạnh nút chính ("Chặn: 2 đợt bị bỏ · …", "Không có thay đổi để ghi"); "" khi không có.</summary>
        public string ReasonText { get; }

        /// <summary>true = chữ blocked-text; false = chữ quiet.</summary>
        public bool ReasonIsBlocked { get; }

        /// <summary>(PD-11) Card gọn một dòng: mọi dòng chặn được đều đạt và chưa copy/lưu đúng sha đang hiện.</summary>
        public bool IsCompact { get; }

        /// <summary>Số dòng có thể chặn đang hiện — 4, hoặc 3 ở lần đăng đầu khi dòng 5 ẩn. Dòng Bản remote không bao giờ đếm.</summary>
        public int CompactConditionCount { get; }

        /// <summary>"4 điều kiện đạt · sha 5b0d93 · 1.612 byte"; "" khi không gọn.</summary>
        public string CompactText { get; }

        /// <summary>Chữ phải của dạng gọn ("Bản remote vẫn chưa kiểm"); "" khi không gọn.</summary>
        public string CompactRemoteText { get; }

        /// <summary>Meta header card ("bấm một dòng để tới màn liên quan" / "đủ điều kiện để copy").</summary>
        public string CardMetaText { get; }

        /// <summary>Health màn Xuất cho <c>LiveOpsHubFindingRouting</c> (6.4) — không mang số đếm phát hiện.</summary>
        public SectionHealth Health { get; }

        /// <summary>Card lỗi (h); null ở mọi trạng thái khác.</summary>
        public ExportGateReadBackFailure ReadBackFailure { get; }

        /// <summary>Chuỗi "Copy lỗi" của dòng parser (h hoặc lệch); "" khi parser khớp.</summary>
        public string ReadBackErrorReportText { get; }

        /// <summary>Đợt bị bỏ của nháp theo báo cáo gần nhất (V-17: không gồm phát hiện về bản remote).</summary>
        public int DroppedCount { get; }

        public int UnreviewedRequiredCount { get; }

        /// <summary>Chữ empty của card diff: (f) "Không có gì mới so với …", (g) "Chưa có dấu đã đăng nào — …"; "" khi có diff.</summary>
        public string DiffEmptyText { get; }

        /// <summary>(i) "Nháp đang là bản khôi phục từ 11/9 16:20"; "" khi không khôi phục.</summary>
        public string RestoringNoticeText { get; }

        public string UndoRestoreText => RestoringNoticeText.Length > 0 ? LiveOpsHubStrings.ExportGateUndoRestoreAction : string.Empty;
        public bool CanUndoRestore { get; }

        /// <summary>(j) HelpBox warning khi định dạng 1 làm mất luật lặp; "" khi không mất gì.</summary>
        public string Format1NoticeText { get; }

        public bool IsCopyBlocked => !Copy.IsEnabled;

        public bool HasStatus(ExportGateStatusCode code)
        {
            return _statusCodes.Contains(code);
        }

        public ExportGateRow Row(ExportGateRowKind kind)
        {
            return Rows[(int)kind];
        }

        /// <summary>Dòng cổng này có đang chặn Copy JSON không.</summary>
        public bool BlocksCopy(ExportGateRowKind kind)
        {
            return Row(kind).BlocksCopy;
        }

        /// <summary>
        /// Chữ cột 96px "chặn Copy JSON" của một hàng Việc cần làm ở Tổng quan [SD1 §1.1]: có khi hàng đó là lý do cổng đang
        /// chặn — Bị bỏ của nháp (dòng 1 Blocked) hoặc Mất tiến độ còn thay đổi bắt buộc chưa xem (dòng 5 Blocked). Phát hiện về
        /// JSON đang chạy đã dán không bao giờ chặn Copy của nháp (V-17), nên luôn "".
        /// </summary>
        public string CopyBlockColumnTextFor(LiveEventCalendarConsequence consequence, bool isAboutRemoteSnapshot)
        {
            if (isAboutRemoteSnapshot) return string.Empty;
            switch (consequence)
            {
                case LiveEventCalendarConsequence.Dropped:
                    return Row(ExportGateRowKind.NoDroppedEntries).State == ExportGateRowState.Blocked ? LiveOpsHubStrings.ExportGateCopyBlockedColumn : string.Empty;
                case LiveEventCalendarConsequence.ProgressLost:
                    return Row(ExportGateRowKind.RequiredChangesReviewed).State == ExportGateRowState.Blocked ? LiveOpsHubStrings.ExportGateCopyBlockedColumn : string.Empty;
                default:
                    return string.Empty;
            }
        }
    }

    /// <summary>
    /// Máy trạng thái cổng Xuất JSON (7.6, [SD2 §3.2–§3.4], [SD2 §3.10]) — thuần, không đụng cửa sổ, clipboard hay phiên. "Copy
    /// cần ba điều kiện, Đánh dấu cần một": Copy/Lưu file cần không còn đợt bị bỏ của nháp, Kiểm lịch mới, đã xem mọi thay đổi
    /// bắt buộc, cộng parser của game đọc lại khớp Kiểm lịch; Đánh dấu chỉ cần đúng sha đang hiện đã được copy/lưu và có thay đổi.
    /// </summary>
    internal static class ExportGateModel
    {
        private const string ClockWithSecondsPattern = "HH:mm:ss";
        private const string ClockPattern = "HH:mm";

        // Tên field JSON do bộ so sinh (LiveEventCalendarDiff giữ hằng private) — chỉ để gắn nhãn tiếng Việt ở dòng 5.
        private const string FieldNameIdPrefix = "idPrefix";
        private const string FieldNameAnchor = "anchorUtc";
        private const string FieldNamePeriod = "periodHours";
        private const string FieldNameActiveDuration = "activeHours";
        private const string FieldNameStart = "startUtc";
        private const string FieldNameEnd = "endUtc";
        private const string FieldNameType = "type";

        private const string LineBreak = "\n";

        private enum ReadBackStatus
        {
            Confirmed = 0,
            Failed = 1,
            Mismatch = 2,
        }

        /// <summary>Chữ cái của mã trong bảng thiết kế ("a"…"k"; "parser-mismatch" cho hàng không có chữ cái).</summary>
        public static string LetterOf(ExportGateStatusCode code)
        {
            switch (code)
            {
                case ExportGateStatusCode.Blocked: return "a";
                case ExportGateStatusCode.Ready: return "b";
                case ExportGateStatusCode.Exported: return "c";
                case ExportGateStatusCode.StaleCheck: return "d";
                case ExportGateStatusCode.MarkedPublished: return "e";
                case ExportGateStatusCode.NoChanges: return "f";
                case ExportGateStatusCode.FirstPublish: return "g";
                case ExportGateStatusCode.ReadBackFailed: return "h";
                case ExportGateStatusCode.Restoring: return "i";
                case ExportGateStatusCode.Format1: return "j";
                case ExportGateStatusCode.RequiredReviewOnly: return "k";
                default: return "parser-mismatch";
            }
        }

        public static ExportGateState Evaluate(ExportGateInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input), LiveOpsHubStrings.ExportGateErrorInputMissing);
            LiveOpsHubFormat format = input.DisplayFormat;
            LiveEventCalendarJsonText json = input.Json;

            // ---- Kiểm lịch
            bool hasReport = input.LastReport != null;
            bool isRunning = input.IsCheckRunning;
            bool isStale = hasReport && input.IsCheckStale;
            bool isFresh = hasReport && !isStale && !isRunning;
            int droppedCount = hasReport ? input.LastReport.Summary.DroppedCount : 0;

            // ---- Parser đọc lại
            List<EntrySignature> expectedEntries = ExpectedEntries(input.DraftCompilation, input.JsonFormat);
            List<EntrySignature> readBackEntries = input.ReadBackIsReadable ? SignaturesOf(input.ReadBack.Compilation) : new List<EntrySignature>();
            ReadBackStatus readBackStatus = !input.ReadBackIsReadable
                ? ReadBackStatus.Failed
                : (SameSequence(expectedEntries, readBackEntries) ? ReadBackStatus.Confirmed : ReadBackStatus.Mismatch);
            bool readBackConfirmed = readBackStatus == ReadBackStatus.Confirmed;
            // Dòng 2 so với dãy mục của bộ biên dịch, còn dòng 1 đếm phát hiện Bị bỏ của Kiểm lịch — hai số lệch được (luật 8 loại
            // lạ bỏ đợt mà bộ biên dịch không xét loại; phát hiện đã bỏ qua). Chỉ nói "Kiểm lịch" ở dòng 2 khi báo cáo mới và cùng
            // số, kẻo dòng 1 "1 đợt bị bỏ" đứng cạnh dòng 2 "giữ 8/8 mục, khớp Kiểm lịch".
            int expectedDroppedCount = expectedEntries.Count - KeptCountOf(expectedEntries);
            bool checkAgreesWithCompiler = isFresh && droppedCount == expectedDroppedCount;

            // ---- Bản so
            PublishedCalendarStamp stamp = input.ActiveStamp;
            bool hasStamp = stamp != null;
            LiveEventCalendarDiffResult diff = input.PublishedDiff;
            bool diffMissing = hasStamp && diff == null;
            int requiredCount = hasStamp && diff != null ? diff.ReviewRequiredCount : 0;
            int reviewedCount = Math.Min(input.ReviewedRequiredCount, requiredCount);
            int unreviewedCount = requiredCount - reviewedCount;
            bool draftMatchesStamp = hasStamp && string.Equals(stamp.Sha256Hex, json.Sha256Hex, StringComparison.OrdinalIgnoreCase);
            bool calendarUnchanged = hasStamp && diff != null && diff.IsEmpty;
            // Diff so hai TÀI LIỆU, còn game nhận BYTE: chọn định dạng 1 (mất luật lặp) hay bộ ghi đổi cách viết cho diff rỗng mà
            // JSON khác bản đã đăng. Khi đó vẫn có thứ để ghi — dấu phải mang sha mới, không thì dòng Bản remote báo "khác dấu".
            bool noChanges = calendarUnchanged && draftMatchesStamp;
            bool sameCalendarDifferentJson = calendarUnchanged && !draftMatchesStamp;
            bool markHasChanges = !hasStamp || (diff != null && !noChanges);

            bool exportedCurrent = input.LastExportedSha256Hex.Length > 0 &&
                                   string.Equals(input.LastExportedSha256Hex, json.Sha256Hex, StringComparison.OrdinalIgnoreCase);

            // ---- Năm dòng
            var rows = new ExportGateRow[5];
            rows[(int)ExportGateRowKind.NoDroppedEntries] = DroppedRow(hasReport, isFresh, isRunning, droppedCount, format);
            string readBackErrorReportText;
            ExportGateReadBackFailure readBackFailure;
            rows[(int)ExportGateRowKind.ParserReadBack] = ReadBackRow(input, readBackStatus, expectedEntries, readBackEntries, checkAgreesWithCompiler,
                format, out readBackErrorReportText, out readBackFailure);
            rows[(int)ExportGateRowKind.CheckFreshness] = FreshnessRow(input, hasReport, isStale, isRunning, format);
            bool justMarked = noChanges && exportedCurrent;
            rows[(int)ExportGateRowKind.RemoteSnapshot] = RemoteRow(input, stamp, justMarked, format);
            rows[(int)ExportGateRowKind.RequiredChangesReviewed] = ReviewedRow(hasStamp, diff, reviewedCount, requiredCount, unreviewedCount, format);

            bool copyAllowed = true;
            for (int index = 0; index < rows.Length; index++)
            {
                if (rows[index].BlocksCopy) copyAllowed = false;
            }

            // ---- Mã trạng thái: mã chính theo thứ tự "cái chặn sâu nhất trước" — parser hỏng thì kết quả kiểm cũ hay mới không
            // còn nghĩa; kiểm cũ thì số bị bỏ có thể đã khác.
            ExportGateStatusCode statusCode;
            if (readBackStatus == ReadBackStatus.Failed) statusCode = ExportGateStatusCode.ReadBackFailed;
            else if (readBackStatus == ReadBackStatus.Mismatch) statusCode = ExportGateStatusCode.ReadBackMismatch;
            else if (!isFresh) statusCode = ExportGateStatusCode.StaleCheck;
            else if (droppedCount > 0) statusCode = ExportGateStatusCode.Blocked;
            else if (unreviewedCount > 0 || diffMissing) statusCode = ExportGateStatusCode.RequiredReviewOnly;
            else if (noChanges) statusCode = justMarked ? ExportGateStatusCode.MarkedPublished : ExportGateStatusCode.NoChanges;
            else if (exportedCurrent) statusCode = ExportGateStatusCode.Exported;
            else statusCode = ExportGateStatusCode.Ready;

            var statusCodes = new List<ExportGateStatusCode> { statusCode };
            if (!hasStamp) statusCodes.Add(ExportGateStatusCode.FirstPublish);
            if (input.RestoredFromStamp != null) statusCodes.Add(ExportGateStatusCode.Restoring);
            if (input.JsonFormat == LiveEventCalendarJsonFormat.Version1) statusCodes.Add(ExportGateStatusCode.Format1);

            // ---- Ba nút. Không có gì để ghi thì "Đánh dấu" vẫn là nút chính (disabled + lý do cạnh nút) như (f) — Copy lại cùng
            // JSON không phải bước tiếp theo.
            bool markIsPrimary = exportedCurrent || (noChanges && copyAllowed);
            string shortSha = json.ShortSha;
            string bytesText = format.Bytes(json.ByteCount);
            string copyBlockedTooltip = CopyBlockedTooltip(rows, readBackStatus, hasReport, isRunning, droppedCount, unreviewedCount, diffMissing,
                UnreviewedItemsText(diff, reviewedCount), format);
            var copyButton = new ExportGateButton(copyAllowed, !markIsPrimary,
                copyAllowed ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateCopyTooltipReadyFormat, bytesText, shortSha) : copyBlockedTooltip);
            var saveButton = new ExportGateButton(copyAllowed, false,
                copyAllowed ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateSaveTooltipReadyFormat, bytesText, shortSha)
                    : LiveOpsHubStrings.ExportGateSaveTooltipBlocked);
            bool markEnabled = exportedCurrent && markHasChanges && readBackConfirmed;
            var markButton = new ExportGateButton(markEnabled, markIsPrimary,
                MarkTooltip(input, exportedCurrent, noChanges, diffMissing, readBackConfirmed, shortSha));

            // ---- Lý do cạnh nút
            string reasonText;
            bool reasonIsBlocked;
            if (!copyAllowed)
            {
                reasonText = BlockedReason(readBackStatus, hasReport, isStale, isRunning, droppedCount, unreviewedCount, diffMissing, format);
                reasonIsBlocked = true;
            }
            else if (noChanges)
            {
                reasonText = LiveOpsHubStrings.ExportGateReasonNoChanges;
                reasonIsBlocked = false;
            }
            else if (!exportedCurrent)
            {
                reasonText = LiveOpsHubStrings.ExportGateReasonMissingForMark;
                reasonIsBlocked = false;
            }
            else
            {
                reasonText = string.Empty;
                reasonIsBlocked = false;
            }

            // ---- Dạng gọn (PD-11): đếm dòng có thể chặn đang hiện; dòng remote tách riêng bên phải.
            int compactConditionCount = 0;
            for (int index = 0; index < rows.Length; index++)
            {
                if (rows[index].CanBlockCopy) compactConditionCount++;
            }
            bool isCompact = copyAllowed && !exportedCurrent;
            string compactText = isCompact
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateCompactFormat, format.Integer(compactConditionCount), shortSha, bytesText)
                : string.Empty;
            ExportGateRow remoteRow = rows[(int)ExportGateRowKind.RemoteSnapshot];
            string compactRemoteText = !isCompact
                ? string.Empty
                : (remoteRow.State == ExportGateRowState.NotMeasured ? LiveOpsHubStrings.ExportGateRemoteStillUnchecked : remoteRow.NarrowText);
            string cardMetaText = copyAllowed ? LiveOpsHubStrings.ExportGateCardMetaReady : LiveOpsHubStrings.ExportGateCardMetaBlocked;

            // ---- Health màn Xuất (6.4): Blocked khi cổng chặn Copy, NotMeasured khi kiểm cũ/chưa kiểm/đang kiểm, Ok khi sẵn sàng.
            SectionHealth health;
            switch (statusCode)
            {
                case ExportGateStatusCode.ReadBackFailed:
                case ExportGateStatusCode.ReadBackMismatch:
                case ExportGateStatusCode.Blocked:
                    health = SectionHealth.Blocked(LiveOpsHubStrings.ExportGateHealthBlockedBadge, reasonText);
                    break;
                case ExportGateStatusCode.StaleCheck:
                    health = SectionHealth.NotMeasured(rows[(int)ExportGateRowKind.CheckFreshness].Text);
                    break;
                case ExportGateStatusCode.RequiredReviewOnly:
                    health = diffMissing
                        ? SectionHealth.Blocked(LiveOpsHubStrings.ExportGateHealthBlockedBadge, reasonText)
                        : SectionHealth.Blocked(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateHealthNeedsReviewBadgeFormat,
                            format.Integer(unreviewedCount)), reasonText);
                    break;
                default:
                    health = SectionHealth.Ok();
                    break;
            }

            // ---- Thân
            string diffEmptyText = string.Empty;
            if (!hasStamp) diffEmptyText = LiveOpsHubStrings.ExportGateFirstPublish;
            else if (noChanges)
            {
                diffEmptyText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateNoChangesFormat, StampTimeText(stamp, format), stamp.ShortSha);
            }
            else if (sameCalendarDifferentJson)
            {
                // Card diff rỗng nhưng không được nói "Không có gì mới": thứ được copy khác bản đã đăng, nêu cả hai sha để thấy vì sao.
                diffEmptyText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateSameCalendarDifferentJsonFormat, StampTimeText(stamp, format),
                    stamp.ShortSha, shortSha);
            }

            string restoringNoticeText = input.RestoredFromStamp != null
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateRestoringFormat, StampTimeText(input.RestoredFromStamp, format))
                : string.Empty;

            string format1NoticeText = string.Empty;
            if (input.JsonFormat == LiveEventCalendarJsonFormat.Version1)
            {
                int recurringCount = 0;
                int fixedCount = 0;
                IReadOnlyList<LiveEventCalendarEntryOutcome> draftEntries = input.DraftCompilation.Entries;
                for (int index = 0; index < draftEntries.Count; index++)
                {
                    if (draftEntries[index].Kind == LiveEventCalendarEntryKind.RecurringRule) recurringCount++;
                    else fixedCount++;
                }
                if (recurringCount > 0)
                {
                    format1NoticeText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateFormat1NoticeFormat,
                        format.Integer(recurringCount), format.Integer(fixedCount));
                }
            }

            return new ExportGateState(statusCode, statusCodes, rows, copyButton, saveButton, markButton, reasonText, reasonIsBlocked, isCompact,
                compactConditionCount, compactText, compactRemoteText, cardMetaText, health, readBackFailure, readBackErrorReportText, droppedCount,
                unreviewedCount, diffEmptyText, restoringNoticeText, input.CanUndoRestore, format1NoticeText);
        }

        // ------------------------------------------------------------------------------------------------------------ dòng

        private static ExportGateRow DroppedRow(bool hasReport, bool isFresh, bool isRunning, int droppedCount, LiveOpsHubFormat format)
        {
            if (!hasReport)
            {
                return new ExportGateRow(ExportGateRowKind.NoDroppedEntries, ExportGateRowState.NotMeasured, LiveOpsHubStrings.ExportGateDroppedNeverChecked,
                    string.Empty, string.Empty, ExportGateRowAction.None, string.Empty, null);
            }
            if (isRunning)
            {
                return new ExportGateRow(ExportGateRowKind.NoDroppedEntries, ExportGateRowState.NotMeasured, LiveOpsHubStrings.ExportGateDroppedRunning,
                    string.Empty, string.Empty, ExportGateRowAction.None, string.Empty, null);
            }
            if (!isFresh)
            {
                // Kết quả cũ không được trông như vẫn đúng (PD-23): vòng rỗng, số cũ chỉ còn ở meta.
                string staleMeta = droppedCount > 0
                    ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateDroppedStaleMetaFormat, format.Integer(droppedCount))
                    : string.Empty;
                return new ExportGateRow(ExportGateRowKind.NoDroppedEntries, ExportGateRowState.NotMeasured, LiveOpsHubStrings.ExportGateDroppedStale,
                    string.Empty, staleMeta, ExportGateRowAction.None, string.Empty, null);
            }
            if (droppedCount > 0)
            {
                return new ExportGateRow(ExportGateRowKind.NoDroppedEntries, ExportGateRowState.Blocked,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateDroppedBlockedFormat, format.Integer(droppedCount)),
                    string.Empty, string.Empty, ExportGateRowAction.OpenValidationDropped, string.Empty,
                    LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Validation).WithFilter(LiveOpsHubNavigation.FilterDropped));
            }
            return new ExportGateRow(ExportGateRowKind.NoDroppedEntries, ExportGateRowState.Ok, LiveOpsHubStrings.ExportGateDroppedOk,
                string.Empty, string.Empty, ExportGateRowAction.None, string.Empty, null);
        }

        private static ExportGateRow ReadBackRow(ExportGateInput input, ReadBackStatus status, List<EntrySignature> expectedEntries,
            List<EntrySignature> readBackEntries, bool checkAgreesWithCompiler, LiveOpsHubFormat format, out string errorReportText,
            out ExportGateReadBackFailure failure)
        {
            LiveEventCalendarJsonText json = input.Json;
            if (status == ReadBackStatus.Failed)
            {
                int errorLine = input.ReadBackErrorLine;
                int errorColumn = input.ReadBackErrorColumn;
                if (errorLine == 0 && LiveOpsJsonSyntaxLocator.TryFindFirstError(json.Text, out LiveOpsJsonSyntaxLocator.SyntaxError syntaxError))
                {
                    errorLine = syntaxError.Line;
                    errorColumn = syntaxError.Column;
                }
                var reportLines = new List<string>
                {
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateFailureReportHeaderFormat, json.ShortSha),
                    input.ReadBackErrorText,
                };
                if (errorLine > 0)
                {
                    reportLines.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateFailurePositionFormat, errorLine, errorColumn));
                }
                errorReportText = string.Join(LineBreak, reportLines);
                failure = new ExportGateReadBackFailure(input.ReadBackErrorText, errorLine, errorColumn, errorReportText);
                return new ExportGateRow(ExportGateRowKind.ParserReadBack, ExportGateRowState.Blocked, LiveOpsHubStrings.ExportGateReadBackFailed,
                    LiveOpsHubStrings.ExportGateReadBackFailedNarrow, string.Empty, ExportGateRowAction.CopyReadBackError,
                    LiveOpsHubStrings.ExportGateCopyErrorAction, null);
            }

            failure = null;
            int readBackKept = KeptCountOf(readBackEntries);
            int readBackTotal = readBackEntries.Count;
            string keptText = format.Integer(readBackKept);
            string totalText = format.Integer(readBackTotal);

            if (status == ReadBackStatus.Mismatch)
            {
                int expectedKept = KeptCountOf(expectedEntries);
                int expectedDropped = expectedEntries.Count - expectedKept;
                string text;
                if (readBackTotal != expectedEntries.Count)
                {
                    text = string.Format(CultureInfo.InvariantCulture, checkAgreesWithCompiler
                            ? LiveOpsHubStrings.ExportGateReadBackMismatchEntryCountFormat
                            : LiveOpsHubStrings.ExportGateReadBackMismatchEntryCountCompilerFormat, totalText,
                        format.Integer(expectedEntries.Count));
                }
                else if (readBackKept != expectedKept)
                {
                    text = string.Format(CultureInfo.InvariantCulture, checkAgreesWithCompiler
                            ? LiveOpsHubStrings.ExportGateReadBackMismatchKeptFormat
                            : LiveOpsHubStrings.ExportGateReadBackMismatchKeptCompilerFormat, keptText, totalText,
                        format.Integer(expectedDropped));
                }
                else
                {
                    // (V-6) Cùng số mà khác mục: ca hai đợt trùng id giữ lệch nhau — so số lượng thôi sẽ báo "khớp" giả.
                    text = string.Format(CultureInfo.InvariantCulture, checkAgreesWithCompiler
                            ? LiveOpsHubStrings.ExportGateReadBackMismatchSameCountFormat
                            : LiveOpsHubStrings.ExportGateReadBackMismatchSameCountCompilerFormat, keptText, totalText,
                        format.Integer(expectedDropped));
                }

                var reportLines = new List<string>
                {
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateMismatchReportHeaderFormat, json.ShortSha),
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateMismatchReportCountsFormat, keptText, totalText,
                        format.Integer(expectedKept), format.Integer(expectedEntries.Count)),
                };
                string firstDifference = string.Empty;
                int longest = Math.Max(expectedEntries.Count, readBackEntries.Count);
                for (int index = 0; index < longest; index++)
                {
                    EntrySignature readBackEntry = index < readBackEntries.Count ? readBackEntries[index] : null;
                    EntrySignature expectedEntry = index < expectedEntries.Count ? expectedEntries[index] : null;
                    if (readBackEntry != null && expectedEntry != null && readBackEntry.SameAs(expectedEntry)) continue;
                    string line = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateReadBackMismatchMetaFormat,
                        format.Integer(index + 1), DescribeEntry(readBackEntry), DescribeEntry(expectedEntry));
                    if (firstDifference.Length == 0) firstDifference = line;
                    reportLines.Add(line);
                }
                errorReportText = string.Join(LineBreak, reportLines);
                return new ExportGateRow(ExportGateRowKind.ParserReadBack, ExportGateRowState.Blocked, text,
                    LiveOpsHubStrings.ExportGateReadBackMismatchNarrow, firstDifference, ExportGateRowAction.CopyReadBackError,
                    LiveOpsHubStrings.ExportGateCopyErrorAction, null);
            }

            errorReportText = string.Empty;
            return new ExportGateRow(ExportGateRowKind.ParserReadBack, ExportGateRowState.Ok,
                string.Format(CultureInfo.InvariantCulture,
                    checkAgreesWithCompiler ? LiveOpsHubStrings.ExportGateReadBackOkFormat : LiveOpsHubStrings.ExportGateReadBackOkCompilerFormat, keptText, totalText),
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateReadBackOkNarrowFormat, keptText, totalText),
                DropReasonSummary(readBackEntries, format), ExportGateRowAction.None, string.Empty, null);
        }

        private static ExportGateRow FreshnessRow(ExportGateInput input, bool hasReport, bool isStale, bool isRunning, LiveOpsHubFormat format)
        {
            if (isRunning)
            {
                return new ExportGateRow(ExportGateRowKind.CheckFreshness, ExportGateRowState.Running,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateFreshnessRunningFormat, format.Integer(input.CompletedRuleCount),
                        format.Integer(input.RuleCount)),
                    string.Empty, LiveOpsHubStrings.ExportGateFreshnessRunningMeta, ExportGateRowAction.None, string.Empty, null);
            }
            if (!hasReport)
            {
                return new ExportGateRow(ExportGateRowKind.CheckFreshness, ExportGateRowState.NotMeasured, LiveOpsHubStrings.ExportGateFreshnessNeverChecked,
                    string.Empty, LiveOpsHubStrings.ExportGateFreshnessRunningMeta, ExportGateRowAction.StartCheck, LiveOpsHubStrings.ExportGateStartCheckAction, null);
            }

            DateTime checkedAtUtc = input.CheckedAtUtc ?? input.LastReport.CheckedAtUtc;
            if (isStale)
            {
                string checkedText = ClockWithSeconds(checkedAtUtc);
                string text;
                if (input.PassedMilestoneUtc.HasValue)
                {
                    text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateFreshnessStaleMilestoneFormat,
                        format.ShortDateTimeUtc(input.PassedMilestoneUtc.Value), checkedText);
                }
                else if (input.DraftChangedAtUtc.HasValue)
                {
                    text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateFreshnessStaleChangedFormat,
                        ClockWithSeconds(input.DraftChangedAtUtc.Value), checkedText);
                }
                else
                {
                    text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateFreshnessStaleFormat, checkedText);
                }
                return new ExportGateRow(ExportGateRowKind.CheckFreshness, ExportGateRowState.NotMeasured, text,
                    LiveOpsHubStrings.ExportGateFreshnessStaleNarrow, LiveOpsHubStrings.ExportGateFreshnessRunningMeta, ExportGateRowAction.StartCheck,
                    LiveOpsHubStrings.ExportGateStartCheckAction, null);
            }

            return new ExportGateRow(ExportGateRowKind.CheckFreshness, ExportGateRowState.Ok,
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateFreshnessOkFormat, ClockWithSeconds(checkedAtUtc)),
                string.Empty, string.Empty, ExportGateRowAction.None, string.Empty, null);
        }

        private static ExportGateRow RemoteRow(ExportGateInput input, PublishedCalendarStamp stamp, bool justMarked, LiveOpsHubFormat format)
        {
            if (!input.RemotePasted)
            {
                // (e) vừa ghi dấu: câu nhắc đối chiếu thay cho "chưa dán" — người dùng vừa nói đã dán, hub chỉ chưa kiểm lại.
                string text = justMarked ? LiveOpsHubStrings.ExportGateRemoteNotVerifiedAfterMark : LiveOpsHubStrings.ExportGateRemoteNotPasted;
                string narrow = justMarked ? LiveOpsHubStrings.ExportGateRemoteNotVerifiedAfterMark : LiveOpsHubStrings.ExportGateRemoteNotPastedNarrow;
                return new ExportGateRow(ExportGateRowKind.RemoteSnapshot, ExportGateRowState.NotMeasured, text, narrow, string.Empty,
                    ExportGateRowAction.PasteRunningJson, LiveOpsHubStrings.ExportGateRemotePasteAction, null);
            }
            if (stamp == null)
            {
                return new ExportGateRow(ExportGateRowKind.RemoteSnapshot, ExportGateRowState.Warning, LiveOpsHubStrings.ExportGateRemoteWithoutStamp,
                    string.Empty, string.Empty, ExportGateRowAction.PasteRunningJson, LiveOpsHubStrings.ExportGateRemotePasteAction, null);
            }

            string stampText = StampTimeText(stamp, format);
            string verifiedSuffix = input.RemoteVerifiedUtc.HasValue
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateRemoteVerifiedSuffixFormat, Clock(input.RemoteVerifiedUtc.Value))
                : string.Empty;
            if (input.RemoteMatchesStamp)
            {
                string matches = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateRemoteMatchesFormat, stampText);
                return new ExportGateRow(ExportGateRowKind.RemoteSnapshot, ExportGateRowState.Ok, matches + verifiedSuffix, matches, string.Empty,
                    ExportGateRowAction.None, string.Empty, null);
            }
            string differs = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateRemoteDiffersFormat, stampText);
            return new ExportGateRow(ExportGateRowKind.RemoteSnapshot, ExportGateRowState.Warning, differs + verifiedSuffix, differs, string.Empty,
                ExportGateRowAction.CompareWithRemote, LiveOpsHubStrings.ExportGateRemoteCompareAction,
                LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Export).WithCompareSource(LiveOpsHubCompareSource.Remote));
        }

        private static ExportGateRow ReviewedRow(bool hasStamp, LiveEventCalendarDiffResult diff, int reviewedCount, int requiredCount, int unreviewedCount,
            LiveOpsHubFormat format)
        {
            if (!hasStamp)
            {
                // (g) lần đăng đầu: không có bản so thì không có thay đổi "bắt buộc" — dòng ẩn, không đếm vào dạng gọn.
                return new ExportGateRow(ExportGateRowKind.RequiredChangesReviewed, ExportGateRowState.Hidden, string.Empty, string.Empty, string.Empty,
                    ExportGateRowAction.None, string.Empty, null);
            }
            if (diff == null)
            {
                return new ExportGateRow(ExportGateRowKind.RequiredChangesReviewed, ExportGateRowState.NotMeasured, LiveOpsHubStrings.ExportGateReviewedDiffMissing,
                    string.Empty, string.Empty, ExportGateRowAction.None, string.Empty, null);
            }
            if (requiredCount == 0)
            {
                return new ExportGateRow(ExportGateRowKind.RequiredChangesReviewed, ExportGateRowState.Ok, LiveOpsHubStrings.ExportGateReviewedNothingRequired,
                    string.Empty, string.Empty, ExportGateRowAction.None, string.Empty, null);
            }
            if (unreviewedCount == 0)
            {
                return new ExportGateRow(ExportGateRowKind.RequiredChangesReviewed, ExportGateRowState.Ok,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateReviewedOkFormat, format.Integer(reviewedCount), format.Integer(requiredCount)),
                    string.Empty, string.Empty, ExportGateRowAction.ShowPublishedDiff, string.Empty, null);
            }

            string narrow = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateReviewedBlockedFormat, format.Integer(unreviewedCount));
            string items = UnreviewedItemsText(diff, reviewedCount);
            string text = items.Length > 0
                ? narrow + string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateReviewedItemsSuffixFormat, items)
                : narrow;
            return new ExportGateRow(ExportGateRowKind.RequiredChangesReviewed, ExportGateRowState.Blocked, text, narrow, string.Empty,
                ExportGateRowAction.ShowPublishedDiff, string.Empty, null);
        }

        // ------------------------------------------------------------------------------------------------------- nút + lý do

        private static string BlockedReason(ReadBackStatus readBackStatus, bool hasReport, bool isStale, bool isRunning, int droppedCount,
            int unreviewedCount, bool diffMissing, LiveOpsHubFormat format)
        {
            var parts = new List<string>();
            if (readBackStatus == ReadBackStatus.Failed) parts.Add(LiveOpsHubStrings.ExportGateReasonReadBackFailed);
            else if (readBackStatus == ReadBackStatus.Mismatch) parts.Add(LiveOpsHubStrings.ExportGateReasonReadBackMismatch);

            if (isRunning) parts.Add(LiveOpsHubStrings.ExportGateReasonRunning);
            else if (!hasReport) parts.Add(LiveOpsHubStrings.ExportGateReasonNeverChecked);
            else if (isStale) parts.Add(LiveOpsHubStrings.ExportGateReasonStale);
            else if (droppedCount > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateReasonDroppedFormat, format.Integer(droppedCount)));
            }

            if (diffMissing) parts.Add(LiveOpsHubStrings.ExportGateReasonDiffMissing);
            else if (unreviewedCount > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateReasonUnreviewedFormat, format.Integer(unreviewedCount)));
            }
            return LiveOpsHubStrings.ExportGateReasonBlockedPrefix + string.Join(LiveOpsHubStrings.ExportGateReasonPartSeparator, parts);
        }

        private static string CopyBlockedTooltip(ExportGateRow[] rows, ReadBackStatus readBackStatus, bool hasReport, bool isRunning, int droppedCount,
            int unreviewedCount, bool diffMissing, string unreviewedItems, LiveOpsHubFormat format)
        {
            var parts = new List<string>();
            if (readBackStatus == ReadBackStatus.Failed) parts.Add(LiveOpsHubStrings.ExportGateCopyTooltipReadBackFailed);
            else if (readBackStatus == ReadBackStatus.Mismatch) parts.Add(LiveOpsHubStrings.ExportGateCopyTooltipReadBackMismatch);

            if (isRunning) parts.Add(LiveOpsHubStrings.ExportGateCopyTooltipRunning);
            else if (!hasReport) parts.Add(LiveOpsHubStrings.ExportGateCopyTooltipNeverChecked);
            else if (rows[(int)ExportGateRowKind.CheckFreshness].State != ExportGateRowState.Ok) parts.Add(LiveOpsHubStrings.ExportGateCopyTooltipStale);
            else if (droppedCount > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateCopyTooltipDroppedFormat, format.Integer(droppedCount)));
            }

            if (diffMissing) parts.Add(LiveOpsHubStrings.ExportGateCopyTooltipDiffMissing);
            else if (unreviewedCount > 0)
            {
                string unreviewed = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateCopyTooltipUnreviewedFormat, format.Integer(unreviewedCount));
                if (unreviewedItems.Length > 0)
                {
                    unreviewed += string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateReviewedItemsSuffixFormat, unreviewedItems);
                }
                parts.Add(unreviewed);
            }
            if (parts.Count == 0) return string.Empty;

            // "Copy JSON: còn 2 đợt game sẽ bỏ, và 1 thay đổi …" — phần cuối nối bằng ", và " như câu thiết kế.
            string sentence = parts[0];
            for (int index = 1; index < parts.Count; index++)
            {
                string separator = index == parts.Count - 1 ? LiveOpsHubStrings.ExportGateCopyTooltipLastSeparator : LiveOpsHubStrings.ExportGateListSeparator;
                sentence += separator + parts[index];
            }
            return LiveOpsHubStrings.ExportGateCopyTooltipBlockedPrefix + sentence;
        }

        private static string MarkTooltip(ExportGateInput input, bool exportedCurrent, bool noChanges, bool diffMissing, bool readBackConfirmed,
            string shortSha)
        {
            // Không có gì để ghi là lý do gốc: copy thêm lần nữa cũng không mở nút, nên nói điều đó trước chuyện chưa copy.
            if (noChanges) return LiveOpsHubStrings.ExportGateReasonNoChanges;
            if (!exportedCurrent)
            {
                if (input.LastExportedSha256Hex.Length == 0) return LiveOpsHubStrings.ExportGateMarkTooltipNotExported;
                string previousShortSha = input.LastExportedSha256Hex.Length > shortSha.Length
                    ? input.LastExportedSha256Hex.Substring(0, shortSha.Length)
                    : input.LastExportedSha256Hex;
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateMarkTooltipDraftChangedFormat, previousShortSha);
            }
            if (!readBackConfirmed) return LiveOpsHubStrings.ExportGateMarkTooltipReadBackUnconfirmed;
            if (diffMissing) return LiveOpsHubStrings.ExportGateReviewedDiffMissing;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateMarkTooltipReadyFormat, shortSha);
        }

        // ----------------------------------------------------------------------------------------------------- dãy mục (V-6)

        /// <summary>Một phần tử của dãy so dòng 2: đúng bốn trường V-6 (id, loại, giữ, lý do bỏ chính).</summary>
        private sealed class EntrySignature
        {
            public EntrySignature(string eventId, string eventType, bool isKept, LiveEventCalendarDropReason dropReason)
            {
                EventId = eventId ?? string.Empty;
                EventType = eventType ?? string.Empty;
                IsKept = isKept;
                DropReason = dropReason;
            }

            public string EventId { get; }
            public string EventType { get; }
            public bool IsKept { get; }
            public LiveEventCalendarDropReason DropReason { get; }

            public bool SameAs(EntrySignature other)
            {
                return string.Equals(EventId, other.EventId, StringComparison.Ordinal)
                    && string.Equals(EventType, other.EventType, StringComparison.Ordinal)
                    && IsKept == other.IsKept
                    && DropReason == other.DropReason;
            }
        }

        /// <summary>
        /// Dãy mục game PHẢI đọc được từ JSON này, suy từ nháp đã biên dịch theo thứ tự xuất. Định dạng 1 không ghi luật lặp nên
        /// parser chỉ thấy đợt cố định; đợt bị luật lặp che trong nháp lại được giữ — bỏ che là bước cuối của bộ biên dịch, sau
        /// trùng id và chồng giờ, nên mọi lý do khác của đợt không đổi (dựng lại từ nháp là chính xác, không phải ước lượng).
        /// </summary>
        private static List<EntrySignature> ExpectedEntries(LiveEventCalendarCompilation draftCompilation, LiveEventCalendarJsonFormat jsonFormat)
        {
            var signatures = new List<EntrySignature>(draftCompilation.EntryCount);
            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = draftCompilation.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome entry = entries[index];
                if (jsonFormat == LiveEventCalendarJsonFormat.Version1)
                {
                    if (entry.Kind == LiveEventCalendarEntryKind.RecurringRule) continue;
                    if (entry.DropReason == LiveEventCalendarDropReason.ShadowedByRecurring)
                    {
                        signatures.Add(new EntrySignature(entry.EventId, entry.EventType, true, LiveEventCalendarDropReason.None));
                        continue;
                    }
                }
                signatures.Add(new EntrySignature(entry.EventId, entry.EventType, entry.IsKept, entry.DropReason));
            }
            return signatures;
        }

        private static List<EntrySignature> SignaturesOf(LiveEventCalendarCompilation compilation)
        {
            var signatures = new List<EntrySignature>();
            if (compilation == null) return signatures;
            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = compilation.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome entry = entries[index];
                signatures.Add(new EntrySignature(entry.EventId, entry.EventType, entry.IsKept, entry.DropReason));
            }
            return signatures;
        }

        private static bool SameSequence(List<EntrySignature> expectedEntries, List<EntrySignature> readBackEntries)
        {
            if (expectedEntries.Count != readBackEntries.Count) return false;
            for (int index = 0; index < expectedEntries.Count; index++)
            {
                if (!expectedEntries[index].SameAs(readBackEntries[index])) return false;
            }
            return true;
        }

        private static int KeptCountOf(List<EntrySignature> entries)
        {
            int kept = 0;
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].IsKept) kept++;
            }
            return kept;
        }

        private static string DescribeEntry(EntrySignature entry)
        {
            if (entry == null) return LiveOpsHubStrings.ExportGateEntryAbsent;
            string name = entry.EventId.Length == 0
                ? entry.EventType
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateEntryNameFormat, entry.EventId, entry.EventType);
            return entry.IsKept
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateEntryKeptFormat, name)
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateEntryDroppedFormat, name, DropReasonLabel(entry.DropReason));
        }

        /// <summary>
        /// "1 không đọc được · 1 bị bỏ vì chồng giờ" — nhóm theo thứ tự lý do của bộ biên dịch; giờ bắt đầu và giờ kết thúc hỏng
        /// chung một nhóm vì với người đọc cả hai đều là "không đọc được".
        /// </summary>
        private static string DropReasonSummary(List<EntrySignature> entries, LiveOpsHubFormat format)
        {
            var countsByGroup = new int[DropReasonGroupCount];
            for (int index = 0; index < entries.Count; index++)
            {
                if (!entries[index].IsKept) countsByGroup[DropReasonGroupOf(entries[index].DropReason)]++;
            }

            var parts = new List<string>();
            for (int group = 0; group < countsByGroup.Length; group++)
            {
                if (countsByGroup[group] == 0) continue;
                parts.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateReasonCountFormat, format.Integer(countsByGroup[group]),
                    DropReasonGroupLabel(group)));
            }
            return string.Join(LiveOpsHubStrings.ExportGateReasonPartSeparator, parts);
        }

        private const int DropReasonGroupCount = 8;

        private static int DropReasonGroupOf(LiveEventCalendarDropReason reason)
        {
            switch (reason)
            {
                case LiveEventCalendarDropReason.UnreadableStartUtc:
                case LiveEventCalendarDropReason.UnreadableEndUtc:
                    return 0;
                case LiveEventCalendarDropReason.InvalidIdentifier: return 1;
                case LiveEventCalendarDropReason.EndNotAfterStart: return 2;
                case LiveEventCalendarDropReason.DuplicateEventId: return 3;
                case LiveEventCalendarDropReason.OverlapsSameType: return 4;
                case LiveEventCalendarDropReason.ShadowedByRecurring: return 5;
                case LiveEventCalendarDropReason.InvalidRecurringRule: return 6;
                default: return 7;
            }
        }

        private static string DropReasonGroupLabel(int group)
        {
            switch (group)
            {
                case 0: return LiveOpsHubStrings.ExportGateDropReasonUnreadable;
                case 1: return LiveOpsHubStrings.ExportGateDropReasonInvalidIdentifier;
                case 2: return LiveOpsHubStrings.ExportGateDropReasonEndNotAfterStart;
                case 3: return LiveOpsHubStrings.ExportGateDropReasonDuplicateEventId;
                case 4: return LiveOpsHubStrings.ExportGateDropReasonOverlapsSameType;
                case 5: return LiveOpsHubStrings.ExportGateDropReasonShadowedByRecurring;
                case 6: return LiveOpsHubStrings.ExportGateDropReasonInvalidRecurringRule;
                default: return LiveOpsHubStrings.ExportGateDropReasonDuplicateRecurringType;
            }
        }

        private static string DropReasonLabel(LiveEventCalendarDropReason reason)
        {
            return DropReasonGroupLabel(DropReasonGroupOf(reason));
        }

        // --------------------------------------------------------------------------------------------------------- chữ phụ

        /// <summary>
        /// "weekly-pass tiền tố" cho các thay đổi bắt buộc chưa xem. Đầu vào chỉ có SỐ đã xem (V-9), không biết mục nào đã tick,
        /// nên chỉ liệt kê khi chưa tick mục nào — khi đó danh sách bắt buộc đúng bằng danh sách chưa xem; liệt kê lúc đã tick
        /// một phần có thể nêu nhầm mục đã xem.
        /// </summary>
        private static string UnreviewedItemsText(LiveEventCalendarDiffResult diff, int reviewedCount)
        {
            if (diff == null || reviewedCount > 0) return string.Empty;
            var items = new List<string>();
            IReadOnlyList<LiveEventCalendarChange> changes = diff.Changes;
            for (int index = 0; index < changes.Count; index++)
            {
                LiveEventCalendarChange change = changes[index];
                if (!change.IsReviewRequired) continue;
                string fieldLabel = change.Kind == LiveEventCalendarChangeKind.Changed && change.Fields.Count > 0
                    ? FieldLabel(change.Fields[0].FieldName)
                    : string.Empty;
                items.Add(fieldLabel.Length > 0
                    ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateItemFieldFormat, change.ItemId, fieldLabel)
                    : change.ItemId);
            }
            return string.Join(LiveOpsHubStrings.ExportGateListSeparator, items);
        }

        private static string FieldLabel(string fieldName)
        {
            switch (fieldName)
            {
                case FieldNameIdPrefix: return LiveOpsHubStrings.ExportGateFieldIdPrefix;
                case FieldNameAnchor: return LiveOpsHubStrings.ExportGateFieldAnchor;
                case FieldNamePeriod: return LiveOpsHubStrings.ExportGateFieldPeriod;
                case FieldNameActiveDuration: return LiveOpsHubStrings.ExportGateFieldActiveDuration;
                case FieldNameStart: return LiveOpsHubStrings.ExportGateFieldStart;
                case FieldNameEnd: return LiveOpsHubStrings.ExportGateFieldEnd;
                case FieldNameType: return LiveOpsHubStrings.ExportGateFieldType;
                default: return fieldName ?? string.Empty;
            }
        }

        /// <summary>"11/9 16:20"; chuỗi giờ của dấu hỏng thì in nguyên văn — dấu vẫn là bản so, không được biến mất khỏi câu.</summary>
        private static string StampTimeText(PublishedCalendarStamp stamp, LiveOpsHubFormat format)
        {
            return stamp.TryGetPublishedUtc(out DateTime publishedUtc) ? format.ShortDateTime(publishedUtc) : stamp.PublishedUtcText;
        }

        private static string ClockWithSeconds(DateTime utc)
        {
            return utc.ToString(ClockWithSecondsPattern, CultureInfo.InvariantCulture);
        }

        private static string Clock(DateTime utc)
        {
            return utc.ToString(ClockPattern, CultureInfo.InvariantCulture);
        }
    }
}
