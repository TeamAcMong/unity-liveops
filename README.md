# DreamTech LiveOps

> Nền live-ops cho game mobile: event theo mùa, pass, đua, săn kho báu... chạy đúng giờ trên mọi máy, không mất quà, không phát
> trùng, không bị vặn giờ máy. Core thuần C# không phụ thuộc engine hay thư viện ngoài; nơi lưu, kho đồ, điều kiện tham gia và
> luật quà đều cắm qua port — package không biết game nào đang dùng nó.

## Cài đặt

**Package Manager → `+` → Add package from git URL:**

```
https://github.com/TeamAcMong/unity-liveops.git#0.1.0
```

Hoặc trong `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dreamtech.liveops": "https://github.com/TeamAcMong/unity-liveops.git#0.1.0"
  }
}
```

> **Yêu cầu:** Unity 2022.3+. Không cần package ngoài — chỉ dùng module có sẵn của Unity (IMGUI, JSONSerialize, UnityWebRequest).

### Phiên bản đã kiểm

| Unity | EditMode | PlayMode (dev project) |
|---|---|---|
| 6000.6.0f1 | 47/47 | 5/5 |
| 2022.3.62f2 | 47/47 | — |

## 1. Có gì trong package

```
Runtime/Core    DreamTech.LiveOps        C# thuần (noEngineReferences), không tham chiếu gì
  Domain/       LiveEventInstance, LiveEventPhase, LiveEventRecord, LiveOpsRewardBundle/Item
  Clock/        ILiveOpsClock, SyncedLiveOpsClock, SystemLiveOpsClock, OffsetLiveOpsClock, ManualLiveOpsClock,
                IServerTimeSource (Delegate, Fallback), IElapsedTimeSource (Stopwatch)
  Calendar/     ILiveEventCalendar, RecurringLiveEventCalendar, FixedLiveEventCalendar, CompositeLiveEventCalendar
  Application/  LiveOpsSystem, LiveOpsSystemBuilder, LiveOpsSystemRegistry, LiveOpsSettings, LiveEventTypeRules,
                port của host (store, granter, eligibility, completion rule), kết quả trả về
  Adapters/     InMemory store, Deferred/Delegate granter, Always/Delegate eligibility, No/Delegate completion rule
Runtime/Unity   DreamTech.LiveOps.Unity  cần UnityEngine
                HttpDateHeaderServerTimeSource, JsonLiveEventCalendarParser, PlayerPrefsLiveOpsTextStore,
                LiveOpsUnityRunner (nhịp tim + vòng đời app), LiveOpsDebugPanel (bảng thử IMGUI)
Tests/Editor    test EditMode của core và của lớp Unity
```

Không có UI sản phẩm: màn event, icon Home, popup kết quả là của game (đọc `GetStatus` / `GetPendingResults`). Khi chưa có UI,
dùng `LiveOpsDebugPanel`.

## 2. Lắp vào game

### Bước 1 — thêm package

Thêm git URL ở trên. Không cần thêm gì khác.

### Bước 2 — viết glue của game (nằm ở phía game, không nằm trong package)

| Port | Game cài bằng gì | Ví dụ |
|---|---|---|
| `ILiveOpsTextStore` | save system của game | 3 hàm đọc/ghi/xoá chuỗi theo khoá; chưa có thì dùng `PlayerPrefsLiveOpsTextStore` |
| `ILiveOpsRewardGranter` | kho đồ của game | đổi `item.ItemId` ("coin", "booster.magnet") sang loại vật phẩm thật; trả false nếu kho chưa sẵn sàng |
| `ILiveEventEligibility` | điều kiện vào đợt | `new DelegateLiveEventEligibility((instance, now) => level >= 11)` |
| `ILiveEventCompletionRule` | quà cuối đợt | đọc `record.Points`, `record.ClaimedKeys` → trả gói quà |
| `IServerTimeSource` | nguồn giờ | `HttpDateHeaderServerTimeSource("<URL server/CDN của game>")` |

### Bước 3 — composition root

```csharp
var store = new PlayerPrefsLiveOpsTextStore();                                   // ← hoặc save system của game
var serverClock = new SyncedLiveOpsClock(new HttpDateHeaderServerTimeSource(cdnUrl), store);
var clock = new OffsetLiveOpsClock(serverClock);                                 // tua giờ bằng cheat

LiveEventCalendarParseResult special = JsonLiveEventCalendarParser.Parse(remoteConfigJson);
var calendar = new CompositeLiveEventCalendar(
    new RecurringLiveEventCalendar("weekly-pass", mondayUtc, TimeSpan.FromDays(7), TimeSpan.FromDays(7)),
    special.Calendar);

LiveOpsSystem liveOps = new LiveOpsSystemBuilder("main")
    .WithClock(clock)
    .WithCalendar(calendar)
    .WithTextStore(store)
    .WithRewardGranter(new DelegateLiveOpsRewardGranter(GrantToInventory))
    .WithSettings(new LiveOpsSettings(finalizeRequiresTrustedClock: true))
    .WithEventType("weekly-pass", passCompletionRule)
    .WithEventType("sky-race", raceCompletionRule, levelEligibility, LiveEventJoinPolicy.ExplicitJoin)
    .Build();
LiveOpsSystemRegistry.Register(liveOps);
LiveOpsUnityRunner.Create(liveOps, serverClock);                                 // Refresh định kỳ + vòng đời app
```

### Bước 4 — game gọi

| Việc | Gọi gì |
|---|---|
| Icon Home, đồng hồ đếm ngược | `GetStatus(type)` — đợt đang chạy hay sắp tới, còn bao lâu, đã vào chưa, điểm |
| Nút "Tham gia" | `TryJoin(type)` (loại `ExplicitJoin`) |
| Thắng level / nhặt vật phẩm | `AddPoints(type, points, grantId)` — ghi ngay, không chờ mạng; `grantId` chống cộng trùng |
| Nhận quà mốc giữa đợt | module kiểm đủ điều kiện → `ClaimReward(eventId, claimKey, bundle)` |
| Mở Home | `GetPendingResults()` → popup kết quả → `AcknowledgeResult(eventId)` |
| Kho đồ vừa sẵn sàng | `GrantPendingRewards()` |
| Lưu trạng thái riêng của module | `TrySetCustomState(eventId, text)` → đọc lại ở `record.CustomState` |
| UI cần biết thay đổi | `StateChanged`, `EventFinalized` |

## 3. Đồng hồ và chống vặn giờ máy

`SyncedLiveOpsClock` lấy giờ server **một lần** rồi cộng thời gian trôi đơn điệu (`Stopwatch`), không đọc giờ máy nữa.

| Trạng thái | `UtcNow` | `IsTrusted` |
|---|---|---|
| Chưa đồng bộ được (mất mạng lúc mở app) | giờ máy + độ lệch đo ở lần đồng bộ trước, **không sớm hơn mốc tin được** | false |
| Đã đồng bộ | giờ server + thời gian trôi | true |
| App vừa vào nền (`Invalidate`) | như "chưa đồng bộ"; mốc tin được được chốt và lưu | false |

- **Vặn giờ lùi để chơi lại đợt cũ:** chặn. Mốc tin được lưu qua các lần mở app; đợt đã khép hoặc đã dọn không vào lại được.
- **Vặn giờ tới lúc offline:** giờ ước lượng đi theo, nhưng không nâng mốc — có mạng là về đúng giờ server. Nếu không muốn đợt
  bị khép sớm trong lúc đó, bật `LiveOpsSettings.finalizeRequiresTrustedClock` (đổi lại người chơi offline phải chờ có mạng mới
  thấy kết quả).
- **Vì sao phải `Invalidate` khi app vào nền:** bộ đếm đơn điệu của iOS/Android không chạy lúc máy ngủ. `LiveOpsUnityRunner` làm
  sẵn: vào nền → `Invalidate`, quay lại → đồng bộ lại; chưa có mạng thì thử lại mỗi `syncRetryIntervalSeconds`.
- **Chọn URL:** `HttpDateHeaderServerTimeSource` đọc header `Date` (cộng `Age` của CDN và nửa thời gian khứ hồi) nên trỏ vào bất
  kỳ server nào của game — host catalog Addressables, API riêng. Không cần dịch vụ thời gian bên thứ ba. Nhiều nguồn thì bọc
  `FallbackServerTimeSource`.
- Game offline hoặc prototype: `SystemLiveOpsClock` (giờ máy, không bao giờ tin được — đừng bật `finalizeRequiresTrustedClock`).

## 4. Lịch event

| Lịch | Dùng khi | Id đợt |
|---|---|---|
| `RecurringLiveEventCalendar(type, anchorUtc, period, activeDuration)` | pass tuần, đua mỗi ngày — không cần mạng | `tiền tố + số thứ tự` (âm nếu trước mốc) |
| `FixedLiveEventCalendar(instances)` / `JsonLiveEventCalendarParser.Parse(json)` | event đặc biệt do designer lên lịch trên remote config | do designer đặt |
| `CompositeLiveEventCalendar(a, b, ...)` | ghép các lịch; lịch đứng trước được ưu tiên khi chồng giờ | giữ nguyên |

Định dạng JSON (thường là giá trị của một key remote config):

```json
{ "events": [
  { "id": "lava-quest-2026-09", "type": "lava-quest",
    "startUtc": "2026-09-14T00:00:00Z", "endUtc": "2026-09-17T00:00:00Z", "configKey": "lava_quest_v2" }
] }
```

- Giờ theo ISO 8601; không ghi múi giờ = UTC. `configKey` tuỳ chọn — game dùng để tra cấu hình riêng của đợt.
- **Id là khoá dữ liệu người chơi.** Đổi id của một đợt đang chạy = người chơi mất tiến độ đợt đó. Id không chứa `#` hay xuống dòng.
- Hai đợt cùng loại không được chồng giờ. Mục hỏng (thiếu id, sai định dạng giờ, kết thúc trước khi bắt đầu, trùng id, chồng giờ)
  bị bỏ và ghi lý do vào `Problems` — không ném exception. Hiện `Problems` trong debug panel qua `CalendarProblems`.
- **Dời giờ một đợt đang chạy:** giữ nguyên id, đổi `endUtc` — bản ghi của người chơi theo lịch cho tới lúc khép.
- **Gỡ một đợt đang chạy:** người chơi dừng cộng điểm ngay; bản ghi vẫn khép theo giờ đã lưu và quà cuối đợt vẫn phát.

## 5. Vòng đời một đợt với một người chơi

```
chưa vào ──AddPoints (JoinOnFirstProgress) / TryJoin (ExplicitJoin)──▶ đã vào: cộng điểm, ClaimReward, CustomState
                                                                          │ hết giờ (Refresh — runner tự gọi)
                                                                          ▼
                                    khép: luật khép đợt chạy đúng 1 lần → quà vào hàng chờ → granter → EventFinalized
                                                                          │ UI báo kết quả (AcknowledgeResult)
                                                                          ▼
                                    quá recordRetention kể từ lúc khép: dọn bản ghi, vẫn nhớ id (không vào lại được)
```

- Đợt hết giờ lúc app đang tắt được khép ở lần Refresh đầu tiên sau khi mở app.
- Quà giữa đợt (`ClaimReward`) được chốt ngay cả khi kho đồ chưa nhận (`Deferred`); gọi lại hoặc `GrantPendingRewards` sẽ phát
  đúng gói đã chốt, không phát trùng.
- "Tự nhận quà chưa nhận" cuối đợt (kiểu battle pass) nằm trong luật khép đợt: đọc `record.ClaimedKeys` để biết mốc nào chưa nhận.
- Loại event đã bị gỡ khỏi game mà máy còn bản ghi: đợt vẫn khép (không quà) và không treo trong danh sách kết quả chờ.

## 6. Port và adapter có sẵn

| Port | Adapter có sẵn |
|---|---|
| `ILiveOpsClock` | `SyncedLiveOpsClock`, `SystemLiveOpsClock`, `OffsetLiveOpsClock` (cheat), `ManualLiveOpsClock` (test) |
| `IServerTimeSource` | `HttpDateHeaderServerTimeSource` (Unity), `FallbackServerTimeSource`, `DelegateServerTimeSource` |
| `IElapsedTimeSource` | `StopwatchElapsedTimeSource` |
| `ILiveEventCalendar` | `RecurringLiveEventCalendar`, `FixedLiveEventCalendar`, `CompositeLiveEventCalendar` |
| `ILiveOpsTextStore` | `PlayerPrefsLiveOpsTextStore` (Unity), `InMemoryLiveOpsTextStore` |
| `ILiveOpsRewardGranter` | `DelegateLiveOpsRewardGranter`, `DeferredLiveOpsRewardGranter` (chưa cắm kho đồ — quà nằm chờ) |
| `ILiveEventEligibility` | `AlwaysLiveEventEligibility`, `DelegateLiveEventEligibility` |
| `ILiveEventCompletionRule` | `NoLiveEventCompletionReward`, `DelegateLiveEventCompletionRule` |

`LiveOpsSettings`: `upcomingLookahead` (31 ngày), `recordRetention` (30 ngày), `maximumRememberedGrantIds` (64),
`maximumRetiredEventIds` (256), `finalizeRequiresTrustedClock` (false).

**Bảng thử:** `LiveOpsDebugPanel.Create(liveOps, offsetClock, serverClock)` — đồng hồ, từng loại event, nút tham gia / cộng điểm /
tua tới đầu hoặc cuối đợt / đồng bộ giờ, kết quả chờ, quà chờ, lỗi của lịch. Nút riêng của game gắn qua `DrawExtraControls`.

## 7. Bẫy và giới hạn đã biết

- **Chỉ dùng trên main thread** (cả `LiveOpsSystem`, `SyncedLiveOpsClock`, `HttpDateHeaderServerTimeSource`).
- **Không dùng `LiveOpsUnityRunner` thì phải tự gọi** `Refresh` định kỳ, `Invalidate` khi vào nền và `SyncAsync` khi quay lại. Quên
  `Invalidate` = sau khi máy ngủ, đồng hồ "tin được" chạy chậm hơn giờ thật đúng bằng thời gian ngủ.
- **Đổi `systemId` hoặc `idPrefix` của lịch lặp** = mọi người chơi bắt đầu lại từ đầu.
- **`PlayerPrefsLiveOpsTextStore` gọi `PlayerPrefs.Save()` mỗi lần ghi.** Game cộng điểm dày (mỗi lần nhặt vật phẩm) nên cắm store của
  game có gom ghi.
- **Vặn giờ tới lúc offline có thể vào đợt sau sớm hơn** (chưa chặn ở 0.1.0 — cần backend để chặn triệt để).
- **Chưa có backend:** chưa định danh người chơi, chưa cloud save, chưa bảng xếp hạng người thật. Nối bảng xếp hạng/League của
  `com.dreamtech.leaderboard` bằng adapter phía game — hai package không phụ thuộc nhau.

## 8. Test

```bash
# Dev project Unity 6
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml
Unity -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml
```

| Nhóm | Kiểm gì |
|---|---|
| `LiveEventCalendarTests` | ranh giới giai đoạn, id tất định, lúc nghỉ, lịch cố định bỏ mục trùng/chồng, lịch ghép ưu tiên nhất quán theo mọi khung hỏi |
| `SyncedLiveOpsClockTests` | tin được / chưa tin được, độ lệch, mốc chống vặn lùi qua lần mở app, vặn tới không nâng mốc, đồng bộ chồng nhau, phản hồi cũ sau khi vào nền |
| `LiveOpsSystemTests` | tự vào / bấm vào / điều kiện, grant id, khép đúng một lần, khép khi app tắt, quà chờ qua lần mở app, granter lỗi, quà giữa đợt, auto-collect, đợt kế tiếp, lịch dời/gỡ đợt, vặn giờ lùi sau khi dọn, chờ đồng hồ tin được, luật lỗi, CustomState, loại event bị gỡ, save hỏng, codec |
| `LiveOpsUnityAdapterTests` | tính giờ từ Date + Age + khứ hồi, parser JSON (múi giờ, mục hỏng, JSON hỏng), PlayerPrefs store |
| Demo PlayMode (`Assets/Demo/Tests`) | runner tự khép đợt và phát rương, tiến độ qua lần nạp scene, bấm tham gia + quà mốc một lần, điều kiện level, mục lịch hỏng |
