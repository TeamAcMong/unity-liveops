using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phiên lịch sống của một cửa sổ hub (4.3): asset đang mở, tài liệu nháp + bản biên dịch theo thứ tự xuất, bản chụp đã lưu,
    /// Undo, lưu, file đổi ngoài, kiểm lịch, bản so và JSON đang chạy đã dán. MỌI sửa đi qua <see cref="Apply"/> hoặc bộ kéo — một
    /// chỗ duy nhất gọi <c>Undo.RecordObject</c> → <c>ApplyDocument</c> → <c>SetDirty</c> (R-6: thiếu SetDirty thì ⌘S không ghi, tab không
    /// có *). Màn chỉ đọc phiên; phiên không biết màn nào. Chữ ký đóng băng khi cổng W3 xanh (PD-35).
    /// </summary>
    internal sealed class LiveOpsHubCalendarSession : IDisposable
    {
        internal const string SavedSnapshotStoreName = "SavedSnapshot";
        internal const string DraftSnapshotStoreName = "DraftSnapshot";
        internal const string SavedFileHashStoreName = "SavedFileHash";
        internal const string CheckRunningStoreName = "CheckRunning";

        private readonly ILiveOpsClock _clock;
        private readonly LiveEventCalendarValidator _validator;
        private readonly LiveOpsHubAssetLocator _locator;
        private readonly LiveOpsHubSectionBus _bus;

        private LiveOpsHubSessionStore _store = new LiveOpsHubSessionStore(string.Empty);
        private LiveEventCalendarAsset _asset;
        private string _assetPath = string.Empty;
        private string _assetGuid = string.Empty;
        private LiveEventCalendarDocument _document = LiveEventCalendarDocument.Empty;
        private LiveEventCalendarCompilation _compilation = LiveEventCalendarCompiler.CompileInExportOrder(LiveEventCalendarDocument.Empty);
        private LiveEventCalendarDocument _savedDocument = LiveEventCalendarDocument.Empty;
        private string _savedFileHash = string.Empty;
        private int _documentRevision;
        private int _stateVersion;
        private int _calendarAssetCount;

        private int _unsavedRevision = -1;
        private LiveEventCalendarDiffResult _unsavedDiff;
        private bool _hasUnsavedChanges;

        private int _continuousGroup = LiveOpsHubEditOutcome.NoUndoGroup;
        private string _continuousUndoName = string.Empty;
        private LiveEventCalendarDocument _continuousStartDocument;
        private LiveOpsHubCheckState.StaleSnapshot _continuousStaleSnapshot;
        private DateTime? _continuousStartChangedUtc;
        private bool _continuousHasUpdate;

        private bool _isSaving;
        private bool _isUndoSubscribed;
        private bool _isPumpRegistered;
        private bool _isDisposed;

        private int _exportGateStateVersion = -1;
        private LiveOpsHubFormat _exportGateFormat;
        private ILiveOpsHubJsonReadBack _exportGateReadBack;
        private ExportGateState _exportGate;
        private string _readBackSha = string.Empty;
        private ILiveOpsHubJsonReadBack _readBackSource;
        private LiveEventCalendarParseResult _readBackResult;
        private LiveEventCalendarDocumentParseResult _readBackDocumentResult;

        /// <param name="locator">null = không tìm/nhớ asset (test, chụp ảnh, dữ liệu mẫu).</param>
        /// <param name="bus">null = không có cửa sổ nghe (outcome "asset đã bị xoá" chỉ còn ở trạng thái phiên).</param>
        internal LiveOpsHubCalendarSession(ILiveOpsClock clock, LiveEventCalendarValidator validator, ILiveOpsHubPublisherIdentity publisherIdentity,
            LiveOpsHubAssetLocator locator, LiveOpsHubSectionBus bus)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            PublisherIdentity = publisherIdentity ?? throw new ArgumentNullException(nameof(publisherIdentity));
            _locator = locator;
            _bus = bus;
            Check = new LiveOpsHubCheckState(validator);
            Check.Changed += OnCheckChanged;
            Remote = new LiveOpsHubRemoteSnapshot(_store);
            Remote.Changed += OnRemoteChanged;
            Publish = new LiveOpsHubPublishState(this);
        }

        /// <summary>Sửa, Undo/Redo, tải lại, đổi asset, lưu — mọi view vẽ lại trong một frame.</summary>
        public event Action DocumentChanged;

        public event Action CheckChanged;

        /// <summary>File asset đổi ngoài trong lúc còn thay đổi chưa lưu — <see cref="DiskConflict"/> vừa có.</summary>
        public event Action DiskChangeDetected;

        /// <summary>null = chưa có asset.</summary>
        public LiveEventCalendarAsset Asset => _asset;

        /// <summary>"" khi chưa có asset hoặc asset chỉ nằm trong bộ nhớ.</summary>
        public string AssetPath => _assetPath;

        /// <summary>Số <see cref="LiveEventCalendarAsset"/> trong project lúc nạp — HelpBox "Có 2 LiveEventCalendarAsset" (0 khi không tìm).</summary>
        public int CalendarAssetCount => _calendarAssetCount;

        public LiveEventCalendarDocument Document => _document;

        /// <summary>(V-6) Luôn <see cref="LiveEventCalendarCompiler.CompileInExportOrder"/> — cùng tập mục mà game giữ khi đọc JSON xuất.</summary>
        public LiveEventCalendarCompilation Compilation => _compilation;

        /// <summary>Lần tài liệu đổi gần nhất (sửa, Undo, tải lại); null khi chưa đổi từ lúc nạp.</summary>
        public DateTime? LastChangedUtc { get; private set; }

        /// <summary>
        /// Asset khác bản chụp lúc nạp/lưu gần nhất (PD-22). Rộng hơn <see cref="UnsavedDiff"/> một chút: dấu đã đăng, cảnh báo đã bỏ qua,
        /// key remote và thứ tự mục cũng là thay đổi của file dù diff hậu quả không liệt kê.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get
            {
                EnsureUnsaved();
                return _hasUnsavedChanges;
            }
        }

        /// <summary>"chưa lưu" (PD-22): bản chụp đã lưu → nháp, so theo EntryKey.</summary>
        public LiveEventCalendarDiffResult UnsavedDiff
        {
            get
            {
                EnsureUnsaved();
                return _unsavedDiff;
            }
        }

        public LiveOpsHubCheckState Check { get; }
        public LiveOpsHubPublishState Publish { get; }
        public LiveOpsHubRemoteSnapshot Remote { get; }

        /// <summary>null khi không có xung đột.</summary>
        public LiveOpsHubDiskConflict DiskConflict { get; private set; }

        internal ILiveOpsClock Clock => _clock;
        internal ILiveOpsHubPublisherIdentity PublisherIdentity { get; }
        internal LiveEventCalendarValidator Validator => _validator;
        internal LiveOpsHubSessionStore Store => _store;
        internal string AssetGuid => _assetGuid;
        internal LiveEventCalendarDocument SavedDocument => _savedDocument;

        /// <summary>Tăng mỗi lần tài liệu đổi — cache của trạng thái đăng/xuất khoá theo số này.</summary>
        internal int DocumentRevision => _documentRevision;

        /// <summary>Tăng mỗi lần tài liệu, kiểm, bản dán hay trạng thái đăng đổi — cache cổng xuất khoá theo số này.</summary>
        internal int StateVersion => _stateVersion;

        internal bool IsContinuousEditOpen => _continuousGroup != LiveOpsHubEditOutcome.NoUndoGroup;

        /// <summary>Tên file asset ("Main.asset"); "" khi chưa có.</summary>
        internal string AssetFileName => _asset == null ? string.Empty : (_assetPath.Length > 0 ? Path.GetFileName(_assetPath) : _asset.name);

        // ============================================================================================================ asset

        /// <summary>Nạp asset theo thứ tự: asset truyền tường minh (builder) → GUID đã nhớ của project → asset duy nhất/đầu tiên trong project.</summary>
        internal void Initialize(LiveEventCalendarAsset explicitAsset, bool useExplicitAsset, bool autoCheckOnOpen)
        {
            LiveEventCalendarAsset asset = useExplicitAsset ? explicitAsset : (_locator != null ? _locator.FindRemembered() : null);
            _calendarAssetCount = _locator != null ? _locator.CountAssets() : 0;
            Load(asset);
            if (autoCheckOnOpen && _asset != null && Check.LastReport == null && !Check.IsRunning) StartCheck();
        }

        /// <summary>Tạo asset rỗng tại đường dẫn (trong Assets/), nhớ GUID, mở nó; một Undo group "Tạo lịch LiveOps" (file tạo ra không nằm trong Undo).</summary>
        public bool TryCreateAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            string normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) || !normalized.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) return false;
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(normalized) != null) return false;
            if (!EnsureFolder(Path.GetDirectoryName(normalized).Replace('\\', '/'))) return false;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(LiveOpsHubStrings.ServicesUndoCreateCalendar);
            LiveEventCalendarAsset asset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            asset.ApplyDocument(LiveEventCalendarDocument.Empty);
            AssetDatabase.CreateAsset(asset, normalized);
            AssetDatabase.SaveAssetIfDirty(asset);
            if (!AssetDatabase.Contains(asset)) return false;
            if (_locator != null)
            {
                _locator.Remember(asset);
                _calendarAssetCount = _locator.CountAssets();
            }
            Load(asset);
            return true;
        }

        public bool TrySelectAsset(LiveEventCalendarAsset asset)
        {
            if (asset == null) return false;
            if (ReferenceEquals(asset, _asset)) return true;
            if (_locator != null)
            {
                _locator.Remember(asset);
                _calendarAssetCount = _locator.CountAssets();
            }
            Load(asset);
            return true;
        }

        // ============================================================================================================ sửa

        /// <summary>
        /// Một lệnh sửa = một Undo group mang tên <paramref name="undoName"/> (= câu toast): RecordObject → ApplyDocument → SetDirty →
        /// cập nhật nháp + biên dịch → kiểm thành cũ → <see cref="DocumentChanged"/>.
        /// </summary>
        public LiveOpsHubEditOutcome Apply(LiveEventCalendarEdit edit, string undoName)
        {
            if (edit == null) throw new ArgumentNullException(nameof(edit));
            if (_asset == null) return LiveOpsHubEditOutcome.Failure(LiveOpsHubStrings.ServicesEditFailedNoAsset);
            if (IsContinuousEditOpen) return LiveOpsHubEditOutcome.Failure(LiveOpsHubStrings.ServicesEditFailedContinuousEditOpen);
            if (!LiveEventCalendarEdits.TryApply(_document, edit, out LiveEventCalendarDocument result))
            {
                return LiveOpsHubEditOutcome.Failure(LiveOpsHubStrings.ServicesEditFailedNotApplicable);
            }

            string name = undoName ?? string.Empty;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(name);
            WriteToAsset(result, name);
            Undo.CollapseUndoOperations(group);
            AcceptAssetDocument(true);
            return LiveOpsHubEditOutcome.Success(group, name);
        }

        /// <summary>Bắt đầu kéo: mở một Undo group; mọi bước kéo sau đó gộp vào group này.</summary>
        public int BeginContinuousEdit(string undoName)
        {
            if (_asset == null) return LiveOpsHubEditOutcome.NoUndoGroup;
            if (IsContinuousEditOpen) CancelContinuousEdit(_continuousGroup);
            Undo.IncrementCurrentGroup();
            _continuousGroup = Undo.GetCurrentGroup();
            _continuousUndoName = undoName ?? string.Empty;
            Undo.SetCurrentGroupName(_continuousUndoName);
            _continuousStartDocument = _document;
            _continuousStaleSnapshot = Check.CaptureStale();
            _continuousStartChangedUtc = LastChangedUtc;
            _continuousHasUpdate = false;
            return _continuousGroup;
        }

        /// <summary>
        /// Một bước kéo. Lệnh sửa áp lên tài liệu LÚC BẮT ĐẦU kéo (không cộng dồn qua các bước) — mỗi bước nói "mục này giờ ở đâu".
        /// (SP-9) RecordObject TRƯỚC mỗi ApplyDocument ở MỌI frame: RevertAllDownToGroup chỉ khôi phục property đổi trước lần flush đầu
        /// sau RecordObject, nên ghi một lần đầu kéo thì Esc không trả được field đổi ở frame sau.
        /// </summary>
        public void UpdateContinuousEdit(LiveEventCalendarEdit edit)
        {
            if (edit == null) throw new ArgumentNullException(nameof(edit));
            if (!IsContinuousEditOpen || _asset == null) return;
            if (!LiveEventCalendarEdits.TryApply(_continuousStartDocument, edit, out LiveEventCalendarDocument result)) return;
            WriteToAsset(result, _continuousUndoName);
            _continuousHasUpdate = true;
            AcceptAssetDocument(true);
        }

        /// <summary>Thả chuột: gộp mọi bước thành một bước Undo. Không có bước nào thì không để lại gì.</summary>
        public LiveOpsHubEditOutcome CommitContinuousEdit(int group)
        {
            if (!IsContinuousEditOpen || group != _continuousGroup)
            {
                return LiveOpsHubEditOutcome.Failure(LiveOpsHubStrings.ServicesEditFailedContinuousGroupMismatch);
            }
            string name = _continuousUndoName;
            bool hasUpdate = _continuousHasUpdate;
            if (!hasUpdate)
            {
                CancelContinuousEdit(group);
                return LiveOpsHubEditOutcome.Failure(LiveOpsHubStrings.ServicesEditFailedNoChange);
            }
            Undo.CollapseUndoOperations(group);
            CloseContinuousEdit();
            return LiveOpsHubEditOutcome.Success(group, name);
        }

        /// <summary>Esc: trả asset về lúc bắt đầu kéo bằng RevertAllDownToGroup — không để lại bước Undo nào, không redo (SP-9).</summary>
        public void CancelContinuousEdit(int group)
        {
            if (!IsContinuousEditOpen || group != _continuousGroup) return;
            LiveOpsHubCheckState.StaleSnapshot staleSnapshot = _continuousStaleSnapshot;
            DateTime? startChangedUtc = _continuousStartChangedUtc;
            bool hadUpdate = _continuousHasUpdate;
            CloseContinuousEdit();
            Undo.RevertAllDownToGroup(group);
            if (_asset == null || !hadUpdate) return;
            AcceptAssetDocument(false);
            LastChangedUtc = startChangedUtc;
            Check.RestoreStale(staleSnapshot);
        }

        // ============================================================================================================ lưu / đĩa

        /// <summary>SaveAssetIfDirty + chụp lại bản đã lưu + hash file (phân biệt "mình vừa lưu" với "người khác đổi"). true khi file đã ghi.</summary>
        public bool Save()
        {
            if (_asset == null || _assetPath.Length == 0) return false;
            if (IsContinuousEditOpen) CommitContinuousEdit(_continuousGroup);
            _isSaving = true;
            try
            {
                // Tài liệu nháp đã ghi vào asset ở mỗi Apply; SetDirty lại để chắc asset được ghi dù cờ bẩn bị ai đó xoá.
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssetIfDirty(_asset);
            }
            finally
            {
                _isSaving = false;
            }
            if (EditorUtility.IsDirty(_asset)) return false;

            _savedFileHash = ComputeFileHash(_assetPath);
            _savedDocument = _document;
            DiskConflict = null;
            PersistSnapshots();
            MarkUnsavedDirty();
            RaiseDocumentChanged();
            return true;
        }

        /// <summary>"Tải lại": đọc file trên đĩa thành nháp; thay đổi chưa lưu bị bỏ (màn hỏi xác nhận trước).</summary>
        public void ReloadFromDisk()
        {
            if (_asset == null) return;
            if (IsContinuousEditOpen) CancelContinuousEdit(_continuousGroup);
            LiveEventCalendarDocument disk = ReadDiskDocument() ?? _asset.ToDocument();
            _asset.ApplyDocument(disk);
            EditorUtility.ClearDirty(_asset);
            _savedFileHash = _assetPath.Length > 0 ? ComputeFileHash(_assetPath) : string.Empty;
            _savedDocument = _asset.ToDocument();
            DiskConflict = null;
            AcceptAssetDocument(true);
            PersistSnapshots();
        }

        /// <summary>"Giữ bản trong Editor": ghi lại bản chụp nháp vào asset (một Undo group) — asset bẩn, tab có *, người dùng tự lưu đè.</summary>
        public void KeepEditorVersion()
        {
            if (_asset == null) return;
            LiveEventCalendarDocument draft = DiskConflict != null ? DiskConflict.EditorDocument : _document;
            if (DiskConflict != null) _savedDocument = DiskConflict.DiskDocument;
            DiskConflict = null;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(LiveOpsHubStrings.ServicesUndoKeepEditorVersion);
            WriteToAsset(draft, LiveOpsHubStrings.ServicesUndoKeepEditorVersion);
            Undo.CollapseUndoOperations(group);
            AcceptAssetDocument(true);
        }

        /// <summary>Bỏ thay đổi chưa lưu (cửa sổ đóng → "Không lưu"): asset về bản chụp lúc lưu, không còn bẩn.</summary>
        internal void DiscardChanges()
        {
            if (_asset == null || !HasUnsavedChanges) return;
            if (IsContinuousEditOpen) CancelContinuousEdit(_continuousGroup);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(LiveOpsHubStrings.ServicesUndoDiscardUnsaved);
            WriteToAsset(_savedDocument, LiveOpsHubStrings.ServicesUndoDiscardUnsaved);
            Undo.CollapseUndoOperations(group);
            EditorUtility.ClearDirty(_asset);
            AcceptAssetDocument(true);
        }

        // ============================================================================================================ kiểm

        /// <summary>Kiểm lại toàn bộ: chạy một luật mỗi nhịp <c>EditorApplication.update</c> (không đóng băng cửa sổ).</summary>
        public void StartCheck()
        {
            if (_asset == null) return;
            Check.Begin(BuildCheckContext(_clock.UtcNow));
            if (!Check.IsRunning) return;
            _store.SetInt(CheckRunningStoreName, 1);
            if (!_isPumpRegistered)
            {
                EditorApplication.update += AdvanceCheck;
                _isPumpRegistered = true;
            }
        }

        /// <summary>Kiểm nhanh một làn trên bản xem trước (kéo, thêm đợt) — không đổi kết quả Kiểm lịch của phiên.</summary>
        public LiveEventCalendarCheckReport CheckLane(string eventType, LiveEventCalendarDocument preview)
        {
            LiveEventCalendarDocument document = preview ?? _document;
            LiveEventCalendarCheckContext context = BuildCheckContextFor(document, null, _clock.UtcNow);
            return _validator.CheckLane(context, eventType ?? string.Empty);
        }

        /// <summary>Một nhịp của lần kiểm đang chạy — <c>EditorApplication.update</c> gọi; test gọi thẳng để đếm đúng một luật mỗi nhịp.</summary>
        internal void AdvanceCheck()
        {
            if (Check.IsRunning) Check.Step();
            if (!Check.IsRunning) StopPump();
        }

        /// <summary>Chạy xong lần kiểm ngay (kịch bản chụp, test): bắt đầu nếu chưa chạy.</summary>
        internal void RunCheckToCompletion()
        {
            if (_asset == null) return;
            if (!Check.IsRunning) Check.Begin(BuildCheckContext(_clock.UtcNow));
            Check.RunToCompletion();
            StopPump();
        }

        /// <summary>
        /// Ngữ cảnh kiểm của nháp: bản so = dấu đang chọn (luật 9/10), bản remote đã dán (luật 8/12) và (CC-VALB-1) tài liệu của dấu MỚI
        /// NHẤT cho luật 12 — độc lập với bản so đang chọn, vì remote phải khớp lần đăng cuối chứ không phải dấu cũ đang được so.
        /// </summary>
        internal LiveEventCalendarCheckContext BuildCheckContext(DateTime nowUtc)
        {
            return BuildCheckContextFor(_document, _compilation, nowUtc);
        }

        /// <summary>Nhịp 1 giây của cửa sổ: kết quả kiểm thành cũ khi thời gian vượt mốc (PD-23).</summary>
        internal void Tick()
        {
            Check.Reevaluate(_clock.UtcNow);
        }

        /// <summary>
        /// Cổng Xuất JSON của nháp (V-9) — cache theo <see cref="StateVersion"/>: health màn Xuất gọi mỗi nhịp mà không đọc lại JSON bằng
        /// parser mỗi giây. Parser đọc lại qua <paramref name="readBack"/> (V-16) để kịch bản (h)/lệch dựng được bằng seam.
        /// </summary>
        internal ExportGateState EvaluateExportGate(ILiveOpsHubJsonReadBack readBack, LiveOpsHubFormat format)
        {
            if (readBack == null) throw new ArgumentNullException(nameof(readBack));
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (_exportGate != null && _exportGateStateVersion == _stateVersion && ReferenceEquals(_exportGateFormat, format)
                && ReferenceEquals(_exportGateReadBack, readBack))
            {
                return _exportGate;
            }
            _exportGate = ExportGateModel.Evaluate(BuildExportGateInput(readBack, format));
            _exportGateStateVersion = _stateVersion;
            _exportGateFormat = format;
            _exportGateReadBack = readBack;
            return _exportGate;
        }

        /// <summary>Đầu vào cổng Xuất dựng từ phiên — màn Xuất (G-EXPORT) dùng cùng hàm để card và health không bao giờ lệch nhau.</summary>
        internal ExportGateInput BuildExportGateInput(ILiveOpsHubJsonReadBack readBack, LiveOpsHubFormat format)
        {
            LiveEventCalendarJsonText json = Publish.CurrentJson;
            EnsureReadBack(readBack, json);
            LiveEventCalendarDiffResult publishedDiff = Publish.PublishedDiff;
            int reviewedRequired = 0;
            foreach (LiveEventCalendarChange change in publishedDiff.Changes)
            {
                if (change.IsReviewRequired && Publish.IsReviewed(change)) reviewedRequired++;
            }
            PublishedCalendarStamp latestStamp = _document.LatestStamp;
            bool remoteMatches = Remote.MatchesStampExactly(latestStamp) || IsRemoteDriftPassedInFreshReport();
            return new ExportGateInput(json, _compilation, _readBackResult, _readBackDocumentResult.IsReadable, _readBackDocumentResult.ReadErrorText, format)
                .WithCheck(Check.LastReport, Check.IsStale, Check.IsRunning, Check.CompletedRuleCount, Check.RuleCount, Check.CheckedAtUtc)
                .WithStaleCause(Check.CalendarEditedUtc, Check.PassedMilestoneUtc)
                .WithPublished(Publish.ActiveStamp, publishedDiff, reviewedRequired)
                .WithLastExportedSha256Hex(Publish.LastExportedSha256Hex)
                .WithRemote(Remote.HasSnapshot, remoteMatches, Remote.VerifiedUtc);
        }

        // ============================================================================================================ vòng đời

        /// <summary>Đóng cửa sổ / domain reload (R-25): huỷ kéo dở, dừng nhịp kiểm (cờ "đang kiểm" giữ lại để lần dựng sau báo bị cắt ngang).</summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            if (IsContinuousEditOpen) CancelContinuousEdit(_continuousGroup);
            PersistSnapshots();
            if (_isPumpRegistered)
            {
                EditorApplication.update -= AdvanceCheck;
                _isPumpRegistered = false;
            }
            Check.Cancel();
            UnsubscribeAssetEvents();
            _isDisposed = true;
        }

        /// <summary>Postprocessor (và test) báo đường dẫn đổi trên đĩa.</summary>
        internal void HandleAssetsChanged(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (_asset == null || _assetPath.Length == 0) return;

            int movedIndex = IndexOf(movedFromAssetPaths, _assetPath);
            if (movedIndex >= 0 && movedAssets != null && movedIndex < movedAssets.Length)
            {
                _assetPath = movedAssets[movedIndex];
                RaiseDocumentChanged();
                return;
            }

            if (IndexOf(deletedAssets, _assetPath) >= 0)
            {
                string fileName = Path.GetFileName(_assetPath);
                Load(null);
                _bus?.ShowOutcome(LiveOpsOutcomeRecord.Blocked(
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesAssetDeletedHeadlineFormat, fileName),
                    LiveOpsHubStrings.ServicesAssetDeletedDetail, _clock.UtcNow));
                return;
            }

            if (IndexOf(importedAssets, _assetPath) < 0) return;
            string fileHash = ComputeFileHash(_assetPath);
            if (_isSaving)
            {
                // SaveAssetIfDirty báo postprocessor ĐỒNG BỘ trong lần lưu của chính hub (SP-8a): hash lúc này là hash của lần tự lưu.
                _savedFileHash = fileHash;
                return;
            }
            if (fileHash.Length > 0 && string.Equals(fileHash, _savedFileHash, StringComparison.Ordinal)) return;

            LiveEventCalendarDocument disk = ReadDiskDocument() ?? _asset.ToDocument();
            LiveEventCalendarDocument draft = _document;
            bool hadUnsaved = HasUnsavedChanges;
            _savedFileHash = fileHash;
            if (!hadUnsaved || DocumentsEqual(disk, draft))
            {
                // Không có gì để mất: nhận bản đĩa (Unity đã nạp lại instance; ghi lại cho chắc khi bản Unity nào đó giữ instance cũ).
                if (!DocumentsEqual(_asset.ToDocument(), disk))
                {
                    _asset.ApplyDocument(disk);
                    EditorUtility.ClearDirty(_asset);
                }
                _savedDocument = _asset.ToDocument();
                DiskConflict = null;
                AcceptAssetDocument(true);
                PersistSnapshots();
                return;
            }

            // Còn nháp chưa lưu: Unity có thể đã đè instance bằng bản đĩa [API §12.7] — giữ nháp làm tài liệu của phiên, đưa quyết định
            // cho người dùng (Tải lại · Xem khác biệt · Giữ bản trong Editor). Không tự ghi gì vào asset.
            DiskConflict = new LiveOpsHubDiskConflict(_clock.UtcNow, disk, draft, _savedDocument);
            _stateVersion++;
            PersistSnapshots();
            DiskChangeDetected?.Invoke();
        }

        // ============================================================================================================ nội bộ

        private void Load(LiveEventCalendarAsset asset)
        {
            if (IsContinuousEditOpen) CancelContinuousEdit(_continuousGroup);
            if (_asset != null) PersistSnapshots();
            StopPump();
            UnsubscribeAssetEvents();
            Check.Cancel();
            Check.Reset();
            DiskConflict = null;
            LastChangedUtc = null;

            _asset = asset;
            _assetPath = asset != null ? AssetDatabase.GetAssetPath(asset) ?? string.Empty : string.Empty;
            _assetGuid = _assetPath.Length > 0 ? AssetDatabase.AssetPathToGUID(_assetPath) ?? string.Empty : string.Empty;
            _store = new LiveOpsHubSessionStore(_assetGuid);
            Remote.Rebind(_store);

            if (asset == null)
            {
                _document = LiveEventCalendarDocument.Empty;
                _savedDocument = LiveEventCalendarDocument.Empty;
                _savedFileHash = string.Empty;
            }
            else
            {
                _document = asset.ToDocument();
                // Sau domain reload asset trong bộ nhớ còn nháp bẩn: bản chụp đã lưu trong SessionState là thứ duy nhất biết "chưa lưu" từ đâu.
                string savedText = _store.GetString(SavedSnapshotStoreName, string.Empty);
                _savedDocument = savedText.Length > 0 && LiveOpsHubDocumentSnapshot.TryRead(savedText, out LiveEventCalendarDocument saved) ? saved : _document;
                _savedFileHash = _store.GetString(SavedFileHashStoreName, string.Empty);
                if (_savedFileHash.Length == 0 && _assetPath.Length > 0) _savedFileHash = ComputeFileHash(_assetPath);
                SubscribeAssetEvents();
                if (_store.GetInt(CheckRunningStoreName, 0) == 1)
                {
                    _store.Erase(CheckRunningStoreName);
                    Check.MarkInterruptedByReload();
                }
            }
            _compilation = LiveEventCalendarCompiler.CompileInExportOrder(_document);
            _documentRevision++;
            Publish.Rebind();
            MarkUnsavedDirty();
            RaiseDocumentChanged();
        }

        private void WriteToAsset(LiveEventCalendarDocument document, string undoName)
        {
            Undo.RecordObject(_asset, undoName);
            _asset.ApplyDocument(document);
            EditorUtility.SetDirty(_asset);
        }

        /// <summary>Asset là sự thật: đọc lại tài liệu từ asset, biên dịch lại, (tuỳ chọn) đánh dấu kiểm cũ, báo mọi view.</summary>
        private void AcceptAssetDocument(bool markEdited)
        {
            _document = _asset != null ? _asset.ToDocument() : LiveEventCalendarDocument.Empty;
            _compilation = LiveEventCalendarCompiler.CompileInExportOrder(_document);
            _documentRevision++;
            if (markEdited)
            {
                DateTime now = _clock.UtcNow;
                LastChangedUtc = now;
                Check.MarkCalendarEdited(now);
            }
            MarkUnsavedDirty();
            RaiseDocumentChanged();
        }

        private LiveEventCalendarCheckContext BuildCheckContextFor(LiveEventCalendarDocument document, LiveEventCalendarCompilation compilation, DateTime nowUtc)
        {
            LiveEventCalendarCheckContextBuilder builder = new LiveEventCalendarCheckContextBuilder(document, nowUtc)
                .WithPublishedBaseline(Publish.ActiveBaseline)
                .WithRemoteSnapshot(Remote.Document, Remote.Sha256Hex)
                .WithLatestStampBaseline(Publish.LatestStampBaseline);
            if (compilation != null) builder.WithCompilation(compilation);
            return builder.Build();
        }

        private bool IsRemoteDriftPassedInFreshReport()
        {
            LiveEventCalendarCheckReport report = Check.LastReport;
            if (report == null || Check.IsStale || !Remote.HasSnapshot) return false;
            foreach (LiveEventCalendarRuleResult result in report.RuleResults)
            {
                if (string.Equals(result.RuleId, LiveEventCalendarRuleIds.RemoteSnapshotDrift, StringComparison.Ordinal))
                {
                    return result.Outcome == LiveEventCalendarRuleOutcome.Passed;
                }
            }
            return false;
        }

        private void EnsureReadBack(ILiveOpsHubJsonReadBack readBack, LiveEventCalendarJsonText json)
        {
            if (_readBackResult != null && ReferenceEquals(_readBackSource, readBack) && string.Equals(_readBackSha, json.Sha256Hex, StringComparison.Ordinal)) return;
            _readBackDocumentResult = readBack.ReadBackDocument(json.Text);
            _readBackResult = readBack.ReadBack(json.Text);
            _readBackSource = readBack;
            _readBackSha = json.Sha256Hex;
        }

        private void EnsureUnsaved()
        {
            if (_unsavedRevision == _documentRevision && _unsavedDiff != null) return;
            _unsavedDiff = LiveEventCalendarDiff.CompareByEntryKey(_savedDocument, _document, _clock.UtcNow);
            _hasUnsavedChanges = _asset != null && (!_unsavedDiff.IsEmpty || !DocumentsEqual(_savedDocument, _document));
            _unsavedRevision = _documentRevision;
        }

        private void MarkUnsavedDirty()
        {
            _unsavedRevision = -1;
            _stateVersion++;
        }

        internal void NotifyPublishStateChanged()
        {
            _stateVersion++;
        }

        private void RaiseDocumentChanged()
        {
            DocumentChanged?.Invoke();
        }

        private void OnCheckChanged()
        {
            _stateVersion++;
            CheckChanged?.Invoke();
        }

        private void OnRemoteChanged()
        {
            _stateVersion++;
            // Bản remote là đầu vào của luật 8 và 12: kết quả kiểm trước khi dán không nói gì về bản vừa dán.
            Check.MarkCalendarEdited(_clock.UtcNow);
            CheckChanged?.Invoke();
        }

        private void OnUndoRedoPerformed()
        {
            if (_asset == null || IsContinuousEditOpen) return;
            LiveEventCalendarDocument restored = _asset.ToDocument();
            if (DocumentsEqual(restored, _document)) return;
            AcceptAssetDocument(true);
        }

        private void OnAssetsChanged(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            HandleAssetsChanged(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        private void SubscribeAssetEvents()
        {
            if (_isUndoSubscribed) return;
            // Chỉ nghe khi có asset: registry dựng không cửa sổ (test hợp đồng màn) tạo phiên không asset và không bao giờ Dispose —
            // đăng ký sự kiện tĩnh ở đó sẽ giữ phiên sống mãi.
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            LiveOpsCalendarAssetPostprocessor.AssetsChanged += OnAssetsChanged;
            _isUndoSubscribed = true;
        }

        private void UnsubscribeAssetEvents()
        {
            if (!_isUndoSubscribed) return;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            LiveOpsCalendarAssetPostprocessor.AssetsChanged -= OnAssetsChanged;
            _isUndoSubscribed = false;
        }

        private void StopPump()
        {
            if (_isPumpRegistered)
            {
                EditorApplication.update -= AdvanceCheck;
                _isPumpRegistered = false;
            }
            if (!Check.IsRunning) _store.Erase(CheckRunningStoreName);
        }

        private void CloseContinuousEdit()
        {
            _continuousGroup = LiveOpsHubEditOutcome.NoUndoGroup;
            _continuousUndoName = string.Empty;
            _continuousStartDocument = null;
            _continuousStaleSnapshot = null;
            _continuousStartChangedUtc = null;
            _continuousHasUpdate = false;
        }

        private void PersistSnapshots()
        {
            if (_asset == null || _assetGuid.Length == 0) return;
            _store.SetString(SavedSnapshotStoreName, LiveOpsHubDocumentSnapshot.Write(_savedDocument));
            _store.SetString(DraftSnapshotStoreName, LiveOpsHubDocumentSnapshot.Write(DiskConflict != null ? DiskConflict.EditorDocument : _document));
            _store.SetString(SavedFileHashStoreName, _savedFileHash);
        }

        private LiveEventCalendarDocument ReadDiskDocument()
        {
            if (_assetPath.Length == 0) return null;
            UnityEngine.Object[] loaded = InternalEditorUtility.LoadSerializedFileAndForget(_assetPath);
            if (loaded == null) return null;
            LiveEventCalendarDocument document = null;
            foreach (UnityEngine.Object candidate in loaded)
            {
                if (document == null && candidate is LiveEventCalendarAsset calendar) document = calendar.ToDocument();
                if (candidate != null) UnityEngine.Object.DestroyImmediate(candidate);
            }
            return document;
        }

        private static string ComputeFileHash(string assetPath)
        {
            string physicalPath = FileUtil.GetPhysicalPath(assetPath);
            if (string.IsNullOrEmpty(physicalPath) || !File.Exists(physicalPath)) return string.Empty;
            return LiveEventCalendarSha256.ComputeHex(File.ReadAllBytes(physicalPath));
        }

        private static bool EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return false;
            if (AssetDatabase.IsValidFolder(folder)) return true;
            string parent = Path.GetDirectoryName(folder);
            if (string.IsNullOrEmpty(parent)) return false;
            parent = parent.Replace('\\', '/');
            if (!EnsureFolder(parent)) return false;
            return AssetDatabase.CreateFolder(parent, Path.GetFileName(folder)).Length > 0;
        }

        private static int IndexOf(string[] paths, string path)
        {
            if (paths == null) return -1;
            for (int index = 0; index < paths.Length; index++)
            {
                if (string.Equals(paths[index], path, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        /// <summary>So từng field asset lưu (thứ tự danh sách giữ nguyên nghĩa: đổi thứ tự là thay đổi của file).</summary>
        internal static bool DocumentsEqual(LiveEventCalendarDocument left, LiveEventCalendarDocument right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null) return false;
            if (!string.Equals(left.RemoteConfigKey, right.RemoteConfigKey, StringComparison.Ordinal)) return false;
            if (left.EventTypes.Count != right.EventTypes.Count || left.RecurringRules.Count != right.RecurringRules.Count
                || left.FixedEvents.Count != right.FixedEvents.Count || left.PublishedStamps.Count != right.PublishedStamps.Count
                || left.IgnoredWarnings.Count != right.IgnoredWarnings.Count)
            {
                return false;
            }
            for (int index = 0; index < left.EventTypes.Count; index++)
            {
                LiveEventTypeDefinition a = left.EventTypes[index];
                LiveEventTypeDefinition b = right.EventTypes[index];
                if (!string.Equals(a.TypeId, b.TypeId, StringComparison.Ordinal) || !string.Equals(a.DisplayName, b.DisplayName, StringComparison.Ordinal)
                    || a.ColorSlot != b.ColorSlot || a.RequiresJoin != b.RequiresJoin
                    || !string.Equals(a.DefaultConfigKey, b.DefaultConfigKey, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            for (int index = 0; index < left.RecurringRules.Count; index++)
            {
                RecurringLiveEventRule a = left.RecurringRules[index];
                RecurringLiveEventRule b = right.RecurringRules[index];
                if (!string.Equals(a.CanonicalText, b.CanonicalText, StringComparison.Ordinal) || !string.Equals(a.IdPrefix, b.IdPrefix, StringComparison.Ordinal)) return false;
            }
            for (int index = 0; index < left.FixedEvents.Count; index++)
            {
                FixedLiveEventEntry a = left.FixedEvents[index];
                FixedLiveEventEntry b = right.FixedEvents[index];
                if (!string.Equals(a.EntryKey, b.EntryKey, StringComparison.Ordinal) || !string.Equals(a.CanonicalText, b.CanonicalText, StringComparison.Ordinal)) return false;
            }
            for (int index = 0; index < left.PublishedStamps.Count; index++)
            {
                PublishedCalendarStamp a = left.PublishedStamps[index];
                PublishedCalendarStamp b = right.PublishedStamps[index];
                if (!string.Equals(a.PublishedUtcText, b.PublishedUtcText, StringComparison.Ordinal) || !string.Equals(a.Publisher, b.Publisher, StringComparison.Ordinal)
                    || !string.Equals(a.Sha256Hex, b.Sha256Hex, StringComparison.Ordinal) || a.ByteCount != b.ByteCount || a.FormatVersion != b.FormatVersion
                    || !string.Equals(a.Note, b.Note, StringComparison.Ordinal) || !string.Equals(a.SnapshotJson, b.SnapshotJson, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            for (int index = 0; index < left.IgnoredWarnings.Count; index++)
            {
                IgnoredCalendarWarning a = left.IgnoredWarnings[index];
                IgnoredCalendarWarning b = right.IgnoredWarnings[index];
                if (!string.Equals(a.RuleId, b.RuleId, StringComparison.Ordinal) || !string.Equals(a.TargetId, b.TargetId, StringComparison.Ordinal)
                    || !string.Equals(a.RangeStartUtcText, b.RangeStartUtcText, StringComparison.Ordinal)
                    || !string.Equals(a.RangeEndUtcText, b.RangeEndUtcText, StringComparison.Ordinal)
                    || !string.Equals(a.Note, b.Note, StringComparison.Ordinal) || !string.Equals(a.ExpiresUtcText, b.ExpiresUtcText, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Kho SessionState theo GUID asset (8.10: <c>DreamTech.LiveOps.Hub.&lt;Tên&gt;.&lt;guid&gt;</c>) — sống qua domain reload, mất khi đóng
    /// Unity, không làm asset bẩn. Asset không có GUID (chỉ trong bộ nhớ: dữ liệu mẫu, test) giữ trong từ điển riêng của kho: hai
    /// asset bộ nhớ không được dùng chung khoá "" và làm bẩn trạng thái của nhau.
    /// </summary>
    internal sealed class LiveOpsHubSessionStore
    {
        internal const string KeyPrefix = "DreamTech.LiveOps.Hub.";

        private readonly Dictionary<string, string> _memory = new Dictionary<string, string>(StringComparer.Ordinal);

        internal LiveOpsHubSessionStore(string assetGuid)
        {
            AssetGuid = assetGuid ?? string.Empty;
        }

        public string AssetGuid { get; }

        public bool IsPersistent => AssetGuid.Length > 0;

        public static string KeyOf(string name, string assetGuid)
        {
            return KeyPrefix + name + "." + assetGuid;
        }

        public string GetString(string name, string defaultValue)
        {
            if (!IsPersistent) return _memory.TryGetValue(name, out string value) ? value : defaultValue;
            return SessionState.GetString(KeyOf(name, AssetGuid), defaultValue);
        }

        public void SetString(string name, string value)
        {
            if (!IsPersistent)
            {
                _memory[name] = value ?? string.Empty;
                return;
            }
            SessionState.SetString(KeyOf(name, AssetGuid), value ?? string.Empty);
        }

        public int GetInt(string name, int defaultValue)
        {
            string text = GetString(name, string.Empty);
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : defaultValue;
        }

        public void SetInt(string name, int value)
        {
            SetString(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public void Erase(string name)
        {
            if (!IsPersistent)
            {
                _memory.Remove(name);
                return;
            }
            SessionState.EraseString(KeyOf(name, AssetGuid));
        }
    }

    /// <summary>
    /// Tài liệu ↔ chuỗi để giữ bản chụp qua domain reload, bằng CHÍNH định dạng field của asset (JsonUtility trên một asset tạm):
    /// không có bộ mã hoá thứ hai phải giữ khớp với YAML của người dùng.
    /// </summary>
    internal static class LiveOpsHubDocumentSnapshot
    {
        public static string Write(LiveEventCalendarDocument document)
        {
            LiveEventCalendarAsset scratch = CreateScratch();
            try
            {
                scratch.ApplyDocument(document ?? LiveEventCalendarDocument.Empty);
                return JsonUtility.ToJson(scratch);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(scratch);
            }
        }

        public static bool TryRead(string text, out LiveEventCalendarDocument document)
        {
            document = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            LiveEventCalendarAsset scratch = CreateScratch();
            try
            {
                JsonUtility.FromJsonOverwrite(text, scratch);
                document = scratch.ToDocument();
                return true;
            }
            catch (ArgumentException)
            {
                // Chuỗi hỏng (bản package khác ghi) → coi như không có bản chụp; phiên lấy tài liệu hiện tại làm mốc.
                return false;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(scratch);
            }
        }

        private static LiveEventCalendarAsset CreateScratch()
        {
            LiveEventCalendarAsset scratch = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            scratch.hideFlags = HideFlags.HideAndDontSave;
            return scratch;
        }
    }
}
