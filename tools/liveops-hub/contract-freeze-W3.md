# Hợp đồng đóng băng ở cổng W3 (PD-35)

Sinh bằng reflection từ test `CalendarSessionTests.ContractFreeze_WritesSignatures` (G-SESSION) — không sửa tay. Gói W4/W5 cần đổi chữ ký nào dưới đây thì ghi `tools/liveops-hub/contract-changes.md` và dừng (10.1).

## IHubHost

```csharp
IReadOnlyList<IHubSection> Sections { get; }
LiveOpsHubServices Services { get; }
String GetSectionViewState(String sectionId)
void Navigate(String sectionId)
void SetSectionViewState(String sectionId, String viewStateJson)
```

## LiveOpsHubServices

```csharp
internal ILiveOpsHubActions Actions { get; }
internal ILiveOpsHubConfirmationPresenter Confirmation { get; }
internal ILiveOpsHubJsonReadBack JsonReadBack { get; }
internal ILiveOpsHubLayoutLoader LayoutLoader { get; }
internal LiveOpsHubCalendarSession Session { get; }
internal LiveOpsHubFormat Format { get; }
internal LiveOpsHubSectionBus Bus { get; }
internal LiveOpsHubServices(ILiveOpsClock clock, ILiveOpsHubClipboard clipboard, ILiveOpsHubFileDialog fileDialog, ILiveOpsHubPublisherIdentity publisherIdentity, ILiveOpsHubTimeZone timeZone, ILiveOpsHubCompilationState compilationState, LiveEventCalendarValidator validator, ILiveOpsHubConfirmationPresenter confirmation, ILiveOpsHubActions actions, ILiveOpsHubJsonReadBack jsonReadBack, ILiveOpsHubLayoutLoader layoutLoader, LiveOpsHubSectionBus bus, LiveOpsHubCalendarSession session, LiveOpsHubFormat format)
public ILiveOpsClock Clock { get; }
public ILiveOpsHubClipboard Clipboard { get; }
public ILiveOpsHubCompilationState CompilationState { get; }
public ILiveOpsHubFileDialog FileDialog { get; }
public ILiveOpsHubPublisherIdentity PublisherIdentity { get; }
public ILiveOpsHubTimeZone TimeZone { get; }
public LiveEventCalendarValidator Validator { get; }
```

## LiveOpsHubCalendarSession

```csharp
internal Boolean IsContinuousEditOpen { get; }
internal ExportGateInput BuildExportGateInput(ILiveOpsHubJsonReadBack readBack, LiveOpsHubFormat format)
internal ExportGateState EvaluateExportGate(ILiveOpsHubJsonReadBack readBack, LiveOpsHubFormat format)
internal ILiveOpsClock Clock { get; }
internal ILiveOpsHubPublisherIdentity PublisherIdentity { get; }
internal Int32 DocumentRevision { get; }
internal Int32 StateVersion { get; }
internal LiveEventCalendarCheckContext BuildCheckContext(DateTime nowUtc)
internal LiveEventCalendarDocument SavedDocument { get; }
internal LiveEventCalendarValidator Validator { get; }
internal LiveOpsHubCalendarSession(ILiveOpsClock clock, LiveEventCalendarValidator validator, ILiveOpsHubPublisherIdentity publisherIdentity, LiveOpsHubAssetLocator locator, LiveOpsHubSectionBus bus)
internal LiveOpsHubSessionStore Store { get; }
internal String AssetFileName { get; }
internal String AssetGuid { get; }
internal static Boolean DocumentsEqual(LiveEventCalendarDocument left, LiveEventCalendarDocument right)
internal void AdvanceCheck()
internal void DiscardChanges()
internal void HandleAssetsChanged(String[] importedAssets, String[] deletedAssets, String[] movedAssets, String[] movedFromAssetPaths)
internal void Initialize(LiveEventCalendarAsset explicitAsset, Boolean useExplicitAsset, Boolean autoCheckOnOpen)
internal void NotifyPublishStateChanged()
internal void RunCheckToCompletion()
internal void Tick()
public Boolean HasUnsavedChanges { get; }
public Boolean Save()
public Boolean TryCreateAsset(String assetPath)
public Boolean TrySelectAsset(LiveEventCalendarAsset asset)
public DateTime? LastChangedUtc { get; }
public Int32 BeginContinuousEdit(String undoName)
public Int32 CalendarAssetCount { get; }
public LiveEventCalendarAsset Asset { get; }
public LiveEventCalendarCheckReport CheckLane(String eventType, LiveEventCalendarDocument preview)
public LiveEventCalendarCompilation Compilation { get; }
public LiveEventCalendarDiffResult UnsavedDiff { get; }
public LiveEventCalendarDocument Document { get; }
public LiveOpsHubCheckState Check { get; }
public LiveOpsHubDiskConflict DiskConflict { get; }
public LiveOpsHubEditOutcome Apply(LiveEventCalendarEdit edit, String undoName)
public LiveOpsHubEditOutcome CommitContinuousEdit(Int32 group)
public LiveOpsHubPublishState Publish { get; }
public LiveOpsHubRemoteSnapshot Remote { get; }
public String AssetPath { get; }
public event Action CheckChanged
public event Action DiskChangeDetected
public event Action DocumentChanged
public void CancelContinuousEdit(Int32 group)
public void Dispose()
public void KeepEditorVersion()
public void ReloadFromDisk()
public void StartCheck()
public void UpdateContinuousEdit(LiveEventCalendarEdit edit)
```

## LiveOpsHubCheckState

```csharp
internal Boolean Step()
internal DateTime? CalendarEditedUtc { get; }
internal LiveOpsHubCheckState(LiveEventCalendarValidator validator)
internal StaleSnapshot CaptureStale()
internal String CurrentRuleId { get; }
internal event Action Changed
internal static DateTime? FindEarliestMilestone(LiveEventCalendarCompilation compilation, DateTime afterUtc, DateTime untilUtc)
internal void Begin(LiveEventCalendarCheckContext context)
internal void Cancel()
internal void MarkCalendarEdited(DateTime changedUtc)
internal void MarkInterruptedByReload()
internal void Reset()
internal void RestoreStale(StaleSnapshot snapshot)
internal void RunToCompletion()
public Boolean IsRunning { get; }
public Boolean IsStale { get; }
public DateTime? CheckedAtUtc { get; }
public DateTime? PassedMilestoneUtc { get; }
public Int32 CompletedRuleCount { get; }
public Int32 RuleCount { get; }
public LiveEventCalendarCheckReport LastReport { get; }
public LiveOpsHubCheckStaleReason StaleReason { get; }
public void Reevaluate(DateTime nowUtc)
```

## LiveOpsHubPublishState

```csharp
internal Boolean LastExportedViaFile { get; }
internal LiveEventCalendarDocument LatestStampBaseline { get; }
internal LiveOpsHubPublishState(LiveOpsHubCalendarSession session)
internal void Rebind()
internal void SelectActiveStamp(PublishedCalendarStamp stamp)
public Boolean IsReviewed(LiveEventCalendarChange change)
public DateTime? LastExportedUtc { get; }
public LiveEventCalendarDiffResult CompareDiff { get; }
public LiveEventCalendarDiffResult PublishedDiff { get; }
public LiveEventCalendarDocument ActiveBaseline { get; }
public LiveEventCalendarDocument CompareDocument { get; }
public LiveEventCalendarJsonFormat SelectedFormat { get; set; }
public LiveEventCalendarJsonText CurrentJson { get; }
public LiveOpsHubCompareSource ActiveCompareSource { get; }
public LiveOpsHubEditOutcome MarkPublished(String note)
public LiveOpsHubEditOutcome RemoveLatestStamp()
public PublishedCalendarStamp ActiveStamp { get; }
public String LastExportedSha256Hex { get; }
public void RecordExport(LiveEventCalendarJsonText json, Boolean viaFile)
public void SelectCompareSource(LiveOpsHubCompareSource source)
public void SetReviewed(LiveEventCalendarChange change, Boolean reviewed)
```
