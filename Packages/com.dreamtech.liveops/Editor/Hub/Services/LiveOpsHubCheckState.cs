using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Vì sao kết quả Kiểm lịch không còn là bằng chứng (PD-23). Số giá trị là hợp đồng (SessionState, đóng băng W3): chỉ thêm vào cuối.
    /// </summary>
    internal enum LiveOpsHubCheckStaleReason
    {
        /// <summary>Kết quả mới: kiểm sau lần đổi lịch cuối và chưa qua mốc nào.</summary>
        None = 0,

        /// <summary>Chưa có lần kiểm nào trong phiên — không phải "cũ" (không có con số cũ để giữ), mà là chưa đo.</summary>
        NeverChecked = 1,

        CalendarEdited = 2,

        /// <summary>Thời gian vượt qua mốc bắt đầu/kết thúc của một đợt hoặc lần lặp — luật 9 và 11 phụ thuộc "bây giờ".</summary>
        MilestonePassed = 3,

        /// <summary>Unity nạp lại script khi lần kiểm đang chạy (R-25): kết quả dở dang đã mất.</summary>
        InterruptedByReload = 4,
    }

    /// <summary>
    /// Trạng thái Kiểm lịch của một phiên: lần chạy đang dở (một luật mỗi nhịp, "Đang kiểm 7/12"), báo cáo gần nhất và vì sao nó cũ.
    /// Thuần (không đụng Editor) để test Logic dựng mọi trạng thái cũ bằng đồng hồ tay; phiên nối nhịp update và SessionState.
    /// </summary>
    internal sealed class LiveOpsHubCheckState
    {
        /// <summary>Trần số đợt cố định quét mốc mỗi lần <see cref="Reevaluate"/> (gọi mỗi giây) — lịch sửa tay khổng lồ không làm giật cửa sổ.</summary>
        internal const int MaximumMilestoneScanCount = 4096;

        private readonly LiveEventCalendarValidator _validator;
        private readonly int _validatorRuleCount;
        private LiveEventCalendarCheckRun _run;
        private LiveEventCalendarCompilation _runCompilation;
        private DateTime? _editedDuringRunUtc;
        private LiveEventCalendarCompilation _checkedCompilation;

        internal LiveOpsHubCheckState(LiveEventCalendarValidator validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _validatorRuleCount = validator.Rules.Count;
            StaleReason = LiveOpsHubCheckStaleReason.NeverChecked;
        }

        /// <summary>Bắt đầu, từng luật xong, xong, thành cũ, huỷ — phiên chuyển thành <c>CheckChanged</c>.</summary>
        internal event Action Changed;

        /// <summary>null = chưa kiểm lần nào (hoặc lần kiểm duy nhất bị cắt ngang).</summary>
        public LiveEventCalendarCheckReport LastReport { get; private set; }

        public DateTime? CheckedAtUtc => LastReport != null ? LastReport.CheckedAtUtc : (DateTime?)null;

        public bool IsStale => StaleReason == LiveOpsHubCheckStaleReason.CalendarEdited
                               || StaleReason == LiveOpsHubCheckStaleReason.MilestonePassed
                               || StaleReason == LiveOpsHubCheckStaleReason.InterruptedByReload;

        public LiveOpsHubCheckStaleReason StaleReason { get; private set; }

        /// <summary>Mốc bắt đầu/kết thúc sớm nhất trong <c>(CheckedAtUtc, now]</c> — "Đã qua mốc 14/9 00:00 UTC sau lần kiểm"; null khi không cũ vì thời gian.</summary>
        public DateTime? PassedMilestoneUtc { get; private set; }

        /// <summary>Lần đổi lịch đầu tiên sau lần kiểm — "Lịch đã đổi lúc 08:46:50 UTC, sau lần kiểm 08:46:30"; null khi không cũ vì sửa.</summary>
        internal DateTime? CalendarEditedUtc { get; private set; }

        public bool IsRunning => _run != null;

        public int CompletedRuleCount => _run != null ? _run.CompletedRuleCount : (LastReport != null ? LastReport.RuleResults.Count : 0);

        public int RuleCount => _run != null ? _run.RuleCount : (LastReport != null ? LastReport.RuleResults.Count : _validatorRuleCount);

        /// <summary>Id luật sắp chạy ở nhịp kế; "" khi không chạy.</summary>
        internal string CurrentRuleId => _run != null ? _run.CurrentRuleId : string.Empty;

        /// <summary>
        /// Gọi mỗi giây từ nhịp health: kết quả mới mà thời gian đã vượt một mốc của lịch ĐÃ KIỂM thì thành cũ. Quét trên bản biên dịch
        /// lúc kiểm (không phải nháp hiện tại) vì câu hỏi là "kết quả kia còn đúng không", và nháp đổi thì đã cũ vì lý do khác.
        /// </summary>
        public void Reevaluate(DateTime nowUtc)
        {
            if (LastReport == null || StaleReason != LiveOpsHubCheckStaleReason.None || _checkedCompilation == null) return;
            DateTime? milestone = FindEarliestMilestone(_checkedCompilation, LastReport.CheckedAtUtc, nowUtc);
            if (!milestone.HasValue) return;
            StaleReason = LiveOpsHubCheckStaleReason.MilestonePassed;
            PassedMilestoneUtc = milestone;
            Changed?.Invoke();
        }

        /// <summary>Bắt đầu một lần kiểm mới trên ngữ cảnh đã dựng; lần đang chạy (nếu có) bị bỏ — kết quả của nó đã thuộc về nháp cũ.</summary>
        internal void Begin(LiveEventCalendarCheckContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _run = _validator.BeginCheck(context);
            _runCompilation = context.Compilation;
            _editedDuringRunUtc = null;
            if (_run.IsComplete) Complete();
            else Changed?.Invoke();
        }

        /// <summary>Chạy đúng một luật. Trả true khi lần kiểm vừa xong ở lần gọi này.</summary>
        internal bool Step()
        {
            if (_run == null) return false;
            _run.Step();
            if (_run.IsComplete)
            {
                Complete();
                return true;
            }
            Changed?.Invoke();
            return false;
        }

        /// <summary>Chạy hết các luật còn lại trong một lần gọi (kịch bản chụp, test, F5 khi cửa sổ không hiện).</summary>
        internal void RunToCompletion()
        {
            while (_run != null) Step();
        }

        /// <summary>Huỷ lần đang chạy (đóng cửa sổ, domain reload): báo cáo trước đó (nếu có) giữ nguyên với lý do cũ của nó.</summary>
        internal void Cancel()
        {
            if (_run == null) return;
            _run = null;
            _runCompilation = null;
            _editedDuringRunUtc = null;
            Changed?.Invoke();
        }

        /// <summary>Lịch vừa đổi (sửa, Undo/Redo, tải lại, đổi bản so): kết quả hiện có thành cũ; lần đang chạy xong sẽ ra kết quả cũ ngay.</summary>
        internal void MarkCalendarEdited(DateTime changedUtc)
        {
            bool changed = false;
            if (_run != null && !_editedDuringRunUtc.HasValue)
            {
                _editedDuringRunUtc = changedUtc;
                changed = true;
            }
            if (LastReport != null && StaleReason != LiveOpsHubCheckStaleReason.CalendarEdited)
            {
                StaleReason = LiveOpsHubCheckStaleReason.CalendarEdited;
                CalendarEditedUtc = changedUtc;
                PassedMilestoneUtc = null;
                changed = true;
            }
            if (changed) Changed?.Invoke();
        }

        /// <summary>Đổi asset: kết quả kiểm thuộc về lịch cũ, không mang sang lịch mới.</summary>
        internal void Reset()
        {
            _run = null;
            _runCompilation = null;
            _editedDuringRunUtc = null;
            _checkedCompilation = null;
            LastReport = null;
            StaleReason = LiveOpsHubCheckStaleReason.NeverChecked;
            PassedMilestoneUtc = null;
            CalendarEditedUtc = null;
            Changed?.Invoke();
        }

        /// <summary>Phiên dựng lại sau domain reload thấy cờ "đang kiểm" còn trong SessionState (R-25): không còn lần chạy, kết quả dở đã mất.</summary>
        internal void MarkInterruptedByReload()
        {
            _run = null;
            _runCompilation = null;
            _editedDuringRunUtc = null;
            StaleReason = LiveOpsHubCheckStaleReason.InterruptedByReload;
            PassedMilestoneUtc = null;
            Changed?.Invoke();
        }

        /// <summary>Chụp lý do cũ trước khi kéo — Esc huỷ kéo trả lịch về y nguyên nên kết quả kiểm cũng phải về y nguyên.</summary>
        internal StaleSnapshot CaptureStale()
        {
            return new StaleSnapshot(StaleReason, PassedMilestoneUtc, CalendarEditedUtc, _editedDuringRunUtc);
        }

        internal void RestoreStale(StaleSnapshot snapshot)
        {
            if (snapshot == null) return;
            bool changed = StaleReason != snapshot.Reason || CalendarEditedUtc != snapshot.CalendarEditedUtc;
            StaleReason = snapshot.Reason;
            PassedMilestoneUtc = snapshot.PassedMilestoneUtc;
            CalendarEditedUtc = snapshot.CalendarEditedUtc;
            if (_run != null) _editedDuringRunUtc = snapshot.EditedDuringRunUtc;
            if (changed) Changed?.Invoke();
        }

        /// <summary>
        /// Mốc bắt đầu/kết thúc sớm nhất nằm trong <c>(afterUtc, untilUtc]</c> của các đợt cố định được giữ và của lần lặp của mọi luật hợp
        /// lệ; null khi không có. Lần lặp chỉ cần xét cửa sổ một chu kỳ sau <paramref name="afterUtc"/>: mốc kế tiếp của một luật luôn là
        /// kết thúc của lần đang mở hoặc bắt đầu của lần sau, không bao giờ xa hơn một chu kỳ.
        /// </summary>
        internal static DateTime? FindEarliestMilestone(LiveEventCalendarCompilation compilation, DateTime afterUtc, DateTime untilUtc)
        {
            if (compilation == null || untilUtc <= afterUtc) return null;
            DateTime? earliest = null;

            IReadOnlyList<LiveEventInstance> fixedInstances = compilation.FixedCalendar.Instances;
            int scanCount = Math.Min(fixedInstances.Count, MaximumMilestoneScanCount);
            for (int index = 0; index < scanCount; index++)
            {
                LiveEventInstance instance = fixedInstances[index];
                earliest = EarlierMilestone(earliest, instance.StartUtc, afterUtc, untilUtc);
                earliest = EarlierMilestone(earliest, instance.EndUtc, afterUtc, untilUtc);
            }

            IReadOnlyList<RecurringLiveEventCalendar> recurringCalendars = compilation.RecurringCalendars;
            for (int calendarIndex = 0; calendarIndex < recurringCalendars.Count; calendarIndex++)
            {
                RecurringLiveEventCalendar recurring = recurringCalendars[calendarIndex];
                DateTime windowStart = SafeAdd(afterUtc, -recurring.ActiveDuration);
                DateTime windowEnd = SafeAdd(SafeAdd(afterUtc, recurring.Period), TimeSpan.FromTicks(1));
                IReadOnlyList<LiveEventInstance> occurrences = recurring.GetInstances(recurring.EventType, windowStart, windowEnd);
                for (int index = 0; index < occurrences.Count; index++)
                {
                    earliest = EarlierMilestone(earliest, occurrences[index].StartUtc, afterUtc, untilUtc);
                    earliest = EarlierMilestone(earliest, occurrences[index].EndUtc, afterUtc, untilUtc);
                }
            }
            return earliest;
        }

        private void Complete()
        {
            LastReport = _run.Report;
            _checkedCompilation = _runCompilation;
            if (_editedDuringRunUtc.HasValue)
            {
                // Lịch đổi giữa lúc kiểm: báo cáo mô tả bản trước khi đổi — giữ con số nhưng nói ngay là cũ.
                StaleReason = LiveOpsHubCheckStaleReason.CalendarEdited;
                CalendarEditedUtc = _editedDuringRunUtc;
            }
            else
            {
                StaleReason = LiveOpsHubCheckStaleReason.None;
                CalendarEditedUtc = null;
            }
            PassedMilestoneUtc = null;
            _run = null;
            _runCompilation = null;
            _editedDuringRunUtc = null;
            Changed?.Invoke();
        }

        private static DateTime? EarlierMilestone(DateTime? current, DateTime candidate, DateTime afterUtc, DateTime untilUtc)
        {
            if (candidate <= afterUtc || candidate > untilUtc) return current;
            return !current.HasValue || candidate < current.Value ? candidate : current;
        }

        private static DateTime SafeAdd(DateTime value, TimeSpan offset)
        {
            long ticks = value.Ticks + offset.Ticks;
            if (ticks < DateTime.MinValue.Ticks) return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            if (ticks > DateTime.MaxValue.Ticks || (offset.Ticks > 0 && ticks < value.Ticks)) return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        /// <summary>Bản chụp lý do cũ (xem <see cref="CaptureStale"/>).</summary>
        internal sealed class StaleSnapshot
        {
            internal StaleSnapshot(LiveOpsHubCheckStaleReason reason, DateTime? passedMilestoneUtc, DateTime? calendarEditedUtc, DateTime? editedDuringRunUtc)
            {
                Reason = reason;
                PassedMilestoneUtc = passedMilestoneUtc;
                CalendarEditedUtc = calendarEditedUtc;
                EditedDuringRunUtc = editedDuringRunUtc;
            }

            public LiveOpsHubCheckStaleReason Reason { get; }
            public DateTime? PassedMilestoneUtc { get; }
            public DateTime? CalendarEditedUtc { get; }
            public DateTime? EditedDuringRunUtc { get; }
        }
    }

    /// <summary>Đích health của một màn đọc phát hiện (6.4). Tổng quan và Xuất JSON không nằm ở đây: Tổng quan luôn Ok, Xuất theo cổng.</summary>
    internal enum LiveOpsHubHealthTarget
    {
        Calendar = 0,
        RecurringRules = 1,
        EventTypes = 2,
        Validation = 3,
    }

    /// <summary>
    /// MỘT hàm từ phát hiện tới health cho mọi màn (6.4): Bị bỏ → Blocked; Mất tiến độ, Nên xem → Warning; luật NotMeasured/Failed →
    /// NotMeasured (Q-12: "chưa kiểm không có nghĩa là ổn"). Mỗi health mang <see cref="LiveOpsHubFindingCounts"/> đếm đúng đích
    /// (V-21 CC-SHELL-5 (b)) để rail cộng số, và tooltip dựng từ <see cref="LiveOpsFindingText.ShortLabel"/> + <see cref="LiveOpsFindingText.PlainText"/>
    /// (V-8, CC-FT-1) — không màn nào tự viết câu cho phát hiện. Phát hiện về JSON đang chạy đã dán (V-17) làm Loại event Blocked nhưng
    /// không vào số đếm của Kiểm lịch.
    /// </summary>
    internal static class LiveOpsHubFindingRouting
    {
        // Luật có thể nói về đích (cột "Đích" bảng 6.1) — dùng để đếm "luật chưa kiểm" đúng màn, vì kết quả luật không mang đích.
        private static readonly HashSet<string> CalendarRuleIds = new HashSet<string>(StringComparer.Ordinal)
        {
            LiveEventCalendarRuleIds.UtcTimeFormat, LiveEventCalendarRuleIds.EndBeforeStart, LiveEventCalendarRuleIds.InvalidIdentifier,
            LiveEventCalendarRuleIds.DuplicateEventId, LiveEventCalendarRuleIds.OverlapSameType, LiveEventCalendarRuleIds.ShadowedByRecurring,
            LiveEventCalendarRuleIds.UnknownEventType, LiveEventCalendarRuleIds.RunningEventIdChanged, LiveEventCalendarRuleIds.ConfigKeyMissing,
        };

        private static readonly HashSet<string> RecurringRuleIds = new HashSet<string>(StringComparer.Ordinal)
        {
            LiveEventCalendarRuleIds.UtcTimeFormat, LiveEventCalendarRuleIds.RecurringRuleInvalid, LiveEventCalendarRuleIds.RunningEventIdChanged,
            LiveEventCalendarRuleIds.ConfigKeyMissing,
        };

        private static readonly HashSet<string> EventTypeRuleIds = new HashSet<string>(StringComparer.Ordinal)
        {
            LiveEventCalendarRuleIds.UnknownEventType,
        };

        public static HealthState StateOf(LiveEventCalendarConsequence consequence)
        {
            switch (consequence)
            {
                case LiveEventCalendarConsequence.Dropped: return HealthState.Blocked;
                case LiveEventCalendarConsequence.ProgressLost:
                case LiveEventCalendarConsequence.ShouldReview: return HealthState.Warning;
                default: return HealthState.Ok;
            }
        }

        /// <summary>Luật không đo được (NotMeasured, hoặc ném → Failed) góp NotMeasured; các kết quả khác không tự quyết health.</summary>
        public static bool IsNotMeasured(LiveEventCalendarRuleOutcome outcome)
        {
            return outcome == LiveEventCalendarRuleOutcome.NotMeasured || outcome == LiveEventCalendarRuleOutcome.Failed;
        }

        /// <summary>Phát hiện này có thuộc đích không.</summary>
        public static bool BelongsTo(LiveEventCalendarFinding finding, LiveOpsHubHealthTarget target)
        {
            if (finding == null) return false;
            bool isUnknownType = string.Equals(finding.RuleId, LiveEventCalendarRuleIds.UnknownEventType, StringComparison.Ordinal);
            switch (target)
            {
                case LiveOpsHubHealthTarget.Calendar:
                    return !finding.IsAboutRemoteSnapshot && (finding.TargetKind == LiveEventCalendarTargetKind.FixedEvent || isUnknownType);
                case LiveOpsHubHealthTarget.RecurringRules:
                    return !finding.IsAboutRemoteSnapshot && finding.TargetKind == LiveEventCalendarTargetKind.RecurringRule;
                case LiveOpsHubHealthTarget.EventTypes:
                    return isUnknownType;
                default:
                    return !finding.IsAboutRemoteSnapshot;
            }
        }

        /// <summary>Số đếm theo đích: phát hiện chưa bỏ qua theo hậu quả + luật chưa kiểm được có thể nói về đích.</summary>
        public static LiveOpsHubFindingCounts CountsFor(LiveEventCalendarCheckReport report, LiveOpsHubHealthTarget target)
        {
            if (report == null) return default(LiveOpsHubFindingCounts);
            int dropped = 0;
            int progressLost = 0;
            int shouldReview = 0;
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (finding.IsIgnored || !BelongsTo(finding, target)) continue;
                switch (finding.Consequence)
                {
                    case LiveEventCalendarConsequence.Dropped: dropped++; break;
                    case LiveEventCalendarConsequence.ProgressLost: progressLost++; break;
                    case LiveEventCalendarConsequence.ShouldReview: shouldReview++; break;
                }
            }
            int notMeasured = 0;
            foreach (LiveEventCalendarRuleResult result in report.RuleResults)
            {
                if (IsNotMeasured(result.Outcome) && RuleConcerns(result.RuleId, target)) notMeasured++;
            }
            return new LiveOpsHubFindingCounts(dropped, progressLost, shouldReview, notMeasured);
        }

        /// <summary>
        /// Health màn theo đích. <paramref name="document"/> = nháp hiện tại (chỉ màn Loại event dùng, cho Warning "trùng màu" tính tại
        /// chỗ — không phải luật); <paramref name="format"/> để nêu giờ mốc trong tooltip kết quả cũ.
        /// </summary>
        public static SectionHealth HealthFor(LiveOpsHubHealthTarget target, bool hasAsset, LiveOpsHubCheckState check,
            LiveEventCalendarDocument document, LiveOpsHubFormat format)
        {
            if (check == null) throw new ArgumentNullException(nameof(check));
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (!hasAsset) return SectionHealth.NotMeasured(LiveOpsHubStrings.ServicesHealthNoAsset);

            if (target == LiveOpsHubHealthTarget.Validation)
            {
                if (check.IsRunning)
                {
                    return SectionHealth.NotMeasured(LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ServicesHealthRunningFormat),
                        check.CompletedRuleCount, check.RuleCount));
                }
                if (check.LastReport == null)
                {
                    return SectionHealth.NotMeasured(check.StaleReason == LiveOpsHubCheckStaleReason.InterruptedByReload
                        ? LiveOpsHubStrings.ServicesHealthInterruptedByReload
                        : LiveOpsHubStrings.ServicesHealthNeverChecked);
                }
            }

            SectionHealth fromReport = check.LastReport != null ? FromReport(check.LastReport, target) : SectionHealth.Ok();
            if (target == LiveOpsHubHealthTarget.EventTypes && fromReport.State != HealthState.Blocked)
            {
                SectionHealth collision = ColorCollisionHealth(document);
                if (collision.State == HealthState.Warning && fromReport.State != HealthState.Warning) fromReport = collision.WithCounts(fromReport.Counts);
            }
            if (check.LastReport == null || !check.IsStale) return fromReport;

            // Kết quả cũ: Kiểm lịch thành NotMeasured giữ badge + số cũ (AsStale, PD-23); Lịch/Luật lặp/Loại event vẫn tô theo lần kiểm
            // cũ nhưng tooltip nói rõ là cũ (6.4) — không màn nào im lặng coi con số cũ là đúng.
            string prefix = StalePrefix(check, format);
            string reason = prefix + (fromReport.Reason.Length > 0 ? fromReport.Reason : LiveOpsHubStrings.ServicesHealthStaleNoDetail);
            SectionHealth prefixed = WithReason(fromReport, reason);
            return target == LiveOpsHubHealthTarget.Validation ? prefixed.AsStale(check.LastReport.CheckedAtUtc) : prefixed;
        }

        /// <summary>Health màn Xuất JSON = health của cổng (V-9): Blocked khi chặn Copy, NotMeasured khi kiểm cũ/chưa kiểm, Ok khi sẵn sàng.</summary>
        public static SectionHealth ExportHealth(bool hasAsset, ExportGateState gate)
        {
            if (!hasAsset) return SectionHealth.NotMeasured(LiveOpsHubStrings.ServicesHealthNoAsset);
            if (gate == null) throw new ArgumentNullException(nameof(gate));
            return gate.Health;
        }

        /// <summary>Tổng quan không tự kết luận — mọi dấu đã có ở màn gốc, tự tô sẽ đếm đôi.</summary>
        public static SectionHealth OverviewHealth()
        {
            return SectionHealth.Ok();
        }

        /// <summary>Health theo id màn từ services — một lối cho màn W4 (<c>GetHealth</c> chỉ đọc phiên, không biên dịch hay kiểm).</summary>
        public static SectionHealth ForSection(string sectionId, LiveOpsHubServices services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            LiveOpsHubCalendarSession session = services.Session;
            bool hasAsset = session.Asset != null;
            switch (sectionId)
            {
                case LiveOpsHubSections.Ids.Overview:
                    return OverviewHealth();
                case LiveOpsHubSections.Ids.EventTypes:
                    return HealthFor(LiveOpsHubHealthTarget.EventTypes, hasAsset, session.Check, session.Document, services.Format);
                case LiveOpsHubSections.Ids.Calendar:
                    return HealthFor(LiveOpsHubHealthTarget.Calendar, hasAsset, session.Check, session.Document, services.Format);
                case LiveOpsHubSections.Ids.RecurringRules:
                    return HealthFor(LiveOpsHubHealthTarget.RecurringRules, hasAsset, session.Check, session.Document, services.Format);
                case LiveOpsHubSections.Ids.Validation:
                    return HealthFor(LiveOpsHubHealthTarget.Validation, hasAsset, session.Check, session.Document, services.Format);
                case LiveOpsHubSections.Ids.Export:
                    return hasAsset
                        ? ExportHealth(true, session.EvaluateExportGate(services.JsonReadBack, services.Format))
                        : ExportHealth(false, null);
                default:
                    throw new ArgumentOutOfRangeException(nameof(sectionId), sectionId, null);
            }
        }

        private static SectionHealth FromReport(LiveEventCalendarCheckReport report, LiveOpsHubHealthTarget target)
        {
            LiveOpsHubFindingCounts counts = CountsFor(report, target);
            List<LiveEventCalendarFinding> dropped = new List<LiveEventCalendarFinding>();
            List<LiveEventCalendarFinding> progressLost = new List<LiveEventCalendarFinding>();
            List<LiveEventCalendarFinding> shouldReview = new List<LiveEventCalendarFinding>();
            int remoteFindingCount = 0;
            bool hasUnknownType = false;
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (finding.IsIgnored) continue;
                if (target == LiveOpsHubHealthTarget.Validation && finding.IsAboutRemoteSnapshot) remoteFindingCount++;
                if (!BelongsTo(finding, target)) continue;
                if (target == LiveOpsHubHealthTarget.EventTypes) hasUnknownType = true;
                switch (finding.Consequence)
                {
                    case LiveEventCalendarConsequence.Dropped: dropped.Add(finding); break;
                    case LiveEventCalendarConsequence.ProgressLost: progressLost.Add(finding); break;
                    case LiveEventCalendarConsequence.ShouldReview: shouldReview.Add(finding); break;
                }
            }

            StringBuilder reason = new StringBuilder();
            AppendGroup(reason, nameof(LiveOpsHubStrings.ServicesHealthDroppedGroupFormat), dropped);
            AppendGroup(reason, nameof(LiveOpsHubStrings.ServicesHealthProgressLostGroupFormat), progressLost);
            AppendGroup(reason, nameof(LiveOpsHubStrings.ServicesHealthShouldReviewGroupFormat), shouldReview);
            if (counts.NotMeasured > 0) AppendPart(reason, LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ServicesHealthNotMeasuredGroupFormat), counts.NotMeasured));
            if (remoteFindingCount > 0) AppendPart(reason, LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ServicesHealthRemoteFindingsFormat), remoteFindingCount));

            // (V-17) loại chưa khai báo — kể cả loại chỉ có trong JSON đang chạy — chặn màn Loại event (tầng CẤU HÌNH không phải cổng).
            if (counts.Dropped > 0 || hasUnknownType)
            {
                string badge = counts.Dropped > 0 ? CountText(LiveOpsHubStrings.ShellRailDroppedCountFormat, counts.Dropped) : FirstWarningBadge(counts);
                return SectionHealth.Blocked(badge, reason.ToString()).WithCounts(counts);
            }
            if (counts.ProgressLost > 0 || counts.ShouldReview > 0)
            {
                return SectionHealth.Warning(FirstWarningBadge(counts), reason.ToString()).WithCounts(counts);
            }
            if (counts.NotMeasured > 0) return SectionHealth.NotMeasured(reason.ToString()).WithCounts(counts);
            return SectionHealth.Ok();
        }

        private static SectionHealth ColorCollisionHealth(LiveEventCalendarDocument document)
        {
            if (document == null) return SectionHealth.Ok();
            IReadOnlyList<LiveEventTypeDefinition> types = document.EventTypes;
            for (int index = 0; index < types.Count; index++)
            {
                IReadOnlyList<LiveEventTypeDefinition> sharing = LiveEventTypeColorSlots.TypesSharingSlot(types[index], types);
                if (sharing.Count == 0) continue;
                StringBuilder names = new StringBuilder(types[index].TypeId);
                foreach (LiveEventTypeDefinition other in sharing)
                {
                    if (string.Equals(other.TypeId, types[index].TypeId, StringComparison.Ordinal)) continue;
                    names.Append(LiveOpsHubStrings.ServicesHealthItemSeparator).Append(other.TypeId);
                }
                return SectionHealth.Warning(LiveOpsHubStrings.ServicesHealthColorCollisionBadge, string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ServicesHealthColorCollisionFormat, names, types[index].ColorSlot));
            }
            return SectionHealth.Ok();
        }

        private static string StalePrefix(LiveOpsHubCheckState check, LiveOpsHubFormat format)
        {
            string checkedAt = check.LastReport.CheckedAtUtc.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            if (check.StaleReason == LiveOpsHubCheckStaleReason.MilestonePassed && check.PassedMilestoneUtc.HasValue)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesHealthStaleMilestonePrefixFormat, checkedAt,
                    format.ShortDateTime(check.PassedMilestoneUtc.Value));
            }
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesHealthStaleEditedPrefixFormat, checkedAt);
        }

        private static SectionHealth WithReason(SectionHealth health, string reason)
        {
            switch (health.State)
            {
                case HealthState.Blocked: return SectionHealth.Blocked(health.Badge, reason).WithCounts(health.Counts);
                case HealthState.Warning: return SectionHealth.Warning(health.Badge, reason).WithCounts(health.Counts);
                case HealthState.NotMeasured: return SectionHealth.NotMeasured(reason).WithCounts(health.Counts);
                // Ok không mang lý do: bản cũ của Ok dựng NotMeasured có lý do ở Kiểm lịch (AsStale); màn khác giữ Ok yên lặng.
                default: return health;
            }
        }

        private static bool RuleConcerns(string ruleId, LiveOpsHubHealthTarget target)
        {
            switch (target)
            {
                case LiveOpsHubHealthTarget.Calendar: return CalendarRuleIds.Contains(ruleId ?? string.Empty);
                case LiveOpsHubHealthTarget.RecurringRules: return RecurringRuleIds.Contains(ruleId ?? string.Empty);
                case LiveOpsHubHealthTarget.EventTypes: return EventTypeRuleIds.Contains(ruleId ?? string.Empty);
                default: return true;
            }
        }

        /// <param name="groupKey">
        /// KHOÁ chữ (không phải câu đã tra): nhóm có số đếm nên câu tiếng Anh mang dấu số ít/số nhiều, mà dấu chỉ chọn được
        /// vế khi biết đối số — <c>LiveOpsHubStringCatalog.Format</c> làm việc đó (Q-W5-2).
        /// </param>
        private static void AppendGroup(StringBuilder reason, string groupKey, List<LiveEventCalendarFinding> findings)
        {
            if (findings.Count == 0) return;
            StringBuilder items = new StringBuilder();
            foreach (LiveEventCalendarFinding finding in findings)
            {
                if (items.Length > 0) items.Append(LiveOpsHubStrings.ServicesHealthItemSeparator);
                items.Append(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesHealthItemFormat,
                    LiveOpsFindingText.PlainText(finding.TargetId), LiveOpsFindingText.PlainText(LiveOpsFindingText.ShortLabel(finding))));
            }
            AppendPart(reason, LiveOpsHubStringCatalog.Format(groupKey, findings.Count, items));
        }

        private static void AppendPart(StringBuilder reason, string part)
        {
            if (reason.Length > 0) reason.Append(LiveOpsHubStrings.ServicesHealthGroupSeparator);
            reason.Append(part);
        }

        private static string FirstWarningBadge(LiveOpsHubFindingCounts counts)
        {
            if (counts.ProgressLost > 0) return CountText(LiveOpsHubStrings.ShellRailProgressLostCountFormat, counts.ProgressLost);
            if (counts.ShouldReview > 0) return CountText(LiveOpsHubStrings.ShellRailShouldReviewCountFormat, counts.ShouldReview);
            return string.Empty;
        }

        private static string CountText(string format, int count)
        {
            return string.Format(CultureInfo.InvariantCulture, format, count);
        }
    }
}
