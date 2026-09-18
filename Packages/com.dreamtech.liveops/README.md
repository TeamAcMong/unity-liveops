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

> **Yêu cầu:** Unity 2022.3+. Không cần package ngoài — chỉ dùng module có sẵn của Unity (IMGUI, JSONSerialize,
> UIElements, UnityWebRequest).

> **0.2.0 chưa có tag.** README này tả cả phần đang nằm ở mục `[Unreleased]` của [`CHANGELOG.md`](CHANGELOG.md):
> asset lịch `LiveEventCalendarAsset`, JSON định dạng 2 và cửa sổ **LiveOps Hub**. Git URL ở trên trỏ tag `0.1.0`, nên
> bản cài theo tag đó **chưa có** những phần này.

### Phiên bản đã kiểm

| Unity | Đã chạy gì |
|---|---|
| **6000.6.0f1** | EditMode + PlayMode trên dev project; test giao diện của hub chạy **không** `-nographics`; chụp ảnh cửa sổ hai skin |
| **2022.3.62f2** | EditMode trên project tạm trỏ `file:` vào package; chụp ảnh cửa sổ hai skin |
| 6000.0 – 6000.5 | **chưa kiểm** — nhánh `#if UNITY_2023_2_OR_NEWER` / `UNITY_6000_0_OR_NEWER` mới chỉ chạy trên 6000.6 |

Số test của từng bản phát hành ghi ở [`CHANGELOG.md`](CHANGELOG.md).

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
                LiveEventCalendarAsset (lịch lưu trong project), JsonLiveEventCalendarParser (+ ParseOrDefault),
                HttpDateHeaderServerTimeSource, PlayerPrefsLiveOpsTextStore,
                LiveOpsUnityRunner (nhịp tim + vòng đời app), LiveOpsDebugPanel (bảng thử IMGUI)
Editor          DreamTech.LiveOps.Editor  CHỈ Editor — LiveOps Hub: cửa sổ designer lên lịch event (mục 5)
Tests/Editor    test EditMode của core, của lớp Unity và của hub
```

Không có UI sản phẩm: màn event, icon Home, popup kết quả là của game (đọc `GetStatus` / `GetPendingResults`). Khi chưa có UI,
dùng `LiveOpsDebugPanel`. LiveOps Hub là công cụ của designer trong Editor — assembly `DreamTech.LiveOps.Editor` chỉ Editor nên
không có gì của hub đi vào build của game.

## 2. Lắp vào game

### Bước 1 — thêm package

Thêm git URL ở trên. Không cần thêm gì khác.

### Bước 2 — tạo asset lịch

`Assets → Create → DreamTech → LiveOps → Lịch LiveOps` sinh một `LiveEventCalendarAsset`. Đây là **một** file
ScriptableObject review được bằng git, giữ: định nghĩa loại event, luật lặp, đợt cố định, lịch sử đăng và cảnh báo đã bỏ
qua. Game dùng nó làm **lịch mặc định khi remote config trống** và để đăng ký loại event; designer sửa nó bằng
**LiveOps Hub** (mục 5).

> `publishedStamps` / `ignoredWarnings` đi theo build và `ToDocument()` có nạp chúng vào tài liệu, nhưng **không đường
> chạy nào của game dùng tới**: chỉ hub đọc để so bản đã đăng và để nhớ cảnh báo đã bỏ qua. Kích thước theo số lần đăng
> vì mỗi dấu giữ nguyên chuỗi JSON của bản đó — xoá bớt dấu cũ trong hub nếu asset phình. Đổi lại chỉ có **một** asset
> duy nhất để review bằng git.

Asset nằm trong build như mọi ScriptableObject được tham chiếu: giữ tham chiếu trong scene hoặc trong config của game,
package không `Resources.Load` đường dẫn cố định.

### Bước 3 — viết glue của game (nằm ở phía game, không nằm trong package)

| Port | Game cài bằng gì | Ví dụ |
|---|---|---|
| `ILiveOpsTextStore` | save system của game | 3 hàm đọc/ghi/xoá chuỗi theo khoá; chưa có thì dùng `PlayerPrefsLiveOpsTextStore` |
| `ILiveOpsRewardGranter` | kho đồ của game | đổi `item.ItemId` ("coin", "booster.magnet") sang loại vật phẩm thật; trả false nếu kho chưa sẵn sàng |
| `ILiveEventEligibility` | điều kiện vào đợt | `new DelegateLiveEventEligibility((instance, now) => level >= 11)` |
| `ILiveEventCompletionRule` | quà cuối đợt | đọc `record.Points`, `record.ClaimedKeys` → trả gói quà |
| `IServerTimeSource` | nguồn giờ | `HttpDateHeaderServerTimeSource("<URL server/CDN của game>")` |

### Bước 4 — composition root

```csharp
var store = new PlayerPrefsLiveOpsTextStore();                                   // ← hoặc save system của game
var serverClock = new SyncedLiveOpsClock(new HttpDateHeaderServerTimeSource(cdnUrl), store);
var clock = new OffsetLiveOpsClock(serverClock);                                 // tua giờ bằng cheat

// Remote config trống HOẶC hỏng → lịch trong asset; đọc được → lịch remote. Một nhánh cho cả hai nguồn.
LiveEventCalendarParseResult calendarResult =
    JsonLiveEventCalendarParser.ParseOrDefault(remoteConfigJson, mainCalendarAsset);

LiveOpsSystem liveOps = new LiveOpsSystemBuilder("main")
    .WithClock(clock)
    .WithCalendar(calendarResult.CombinedCalendar)                               // KHÔNG dùng .Calendar: nó chỉ có đợt cố định
    .WithTextStore(store)
    .WithRewardGranter(new DelegateLiveOpsRewardGranter(GrantToInventory))
    .WithSettings(new LiveOpsSettings(finalizeRequiresTrustedClock: true))
    .WithEventTypesFrom(mainCalendarAsset, CreateCompletionRuleFor, CreateEligibilityFor)
    .Build();
LiveOpsSystemRegistry.Register(liveOps);
LiveOpsUnityRunner.Create(liveOps, serverClock);                                 // Refresh định kỳ + vòng đời app

foreach (string problem in calendarResult.Problems) Debug.LogWarning(problem);   // mục lịch bị bỏ và vì sao
```

- `WithEventTypesFrom(asset, …)` đăng ký **mọi** loại khai trong asset: `requiresJoin` → `ExplicitJoin`, còn lại
  `JoinOnFirstProgress`. Luật quà và điều kiện vẫn là code của game — hai hàm nhận `typeId` và trả `null` khi muốn dùng
  mặc định của builder. Vẫn đăng ký tay bằng `WithEventType(...)` được; trùng id thì builder ném như trước.
- `calendarResult.CombinedCalendar` = `Composite(luật lặp…, đợt cố định)`. `calendarResult.Calendar` giữ nghĩa 0.1.0
  (chỉ đợt cố định) nên **đừng** truyền nó vào `WithCalendar` ở 0.2.0 — luật lặp sẽ biến mất.

### Bước 5 — game gọi

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
| `LiveEventCalendarAsset` (bước 2) | nguồn chính của designer; làm lịch mặc định khi remote config trống | đợt cố định: do designer đặt · luật lặp: `tiền tố + số thứ tự` |
| `RecurringLiveEventCalendar(type, anchorUtc, period, activeDuration)` | pass tuần, đua mỗi ngày — không cần mạng | `tiền tố + số thứ tự` (âm nếu trước mốc) |
| `FixedLiveEventCalendar(instances)` / `JsonLiveEventCalendarParser.Parse(json)` | event đặc biệt do designer lên lịch trên remote config | do designer đặt |
| `CompositeLiveEventCalendar(a, b, ...)` | ghép các lịch; lịch đứng trước được ưu tiên khi chồng giờ | giữ nguyên |

### 4.1 Định dạng JSON

Giá trị của một key remote config. **Định dạng 2** (0.2.0) thêm `version` và `recurring`; **định dạng 1** (0.1.0) chỉ có
`events` — parser 0.2.0 đọc được cả hai.

```json
{
  "version": 2,
  "recurring": [
    { "type": "weekly-pass", "anchorUtc": "2026-09-07T00:00:00Z", "idPrefix": "weekly-pass-",
      "periodHours": 168, "activeHours": 168, "configKey": "pass_v3" }
  ],
  "events": [
    { "id": "lava-quest-2026-09", "type": "lava-quest",
      "startUtc": "2026-09-14T00:00:00Z", "endUtc": "2026-09-17T00:00:00Z", "configKey": "lava_quest_v2" }
  ]
}
```

- Giờ theo ISO 8601; không ghi múi giờ = UTC. `configKey` tuỳ chọn — game dùng để tra cấu hình riêng của đợt.
- **Id là khoá dữ liệu người chơi.** Đổi id của một đợt đang chạy = người chơi mất tiến độ đợt đó. Id không chứa `#` hay xuống dòng.
- `idPrefix` để trống = mặc định **`<loại>-`** (loại `weekly-pass` → `weekly-pass-`). Bộ ghi của hub luôn xuất tiền tố
  hiệu lực ra JSON, để diff và sha không phụ thuộc mặc định của runtime.
- `periodHours` / `activeHours` tính bằng **giờ**; thời gian chạy phải nằm trong `(0, chu kỳ]`.
- Hai đợt cùng loại không được chồng giờ. Mục hỏng (thiếu id, sai định dạng giờ, kết thúc trước khi bắt đầu, trùng id, chồng giờ)
  bị bỏ và ghi lý do vào `Problems` — không ném exception. Hiện `Problems` trong debug panel qua `CalendarProblems`.
- Luật lặp hỏng cũng bị bỏ riêng luật đó kèm một `Problem`; phần `events` vẫn đọc bình thường.
- **Luật lặp ghép TRƯỚC đợt cố định.** Đợt cố định chồng giờ với một lần lặp cùng loại sẽ bị che — hub báo bằng luật
  [`shadowed-by-recurring`](#shadowed-by-recurring).
- `version` lớn hơn 2 vẫn đọc được `recurring` + `events`, kèm một `Problem` nói dữ liệu mới hơn parser.
- **Dời giờ một đợt đang chạy:** giữ nguyên id, đổi `endUtc` — bản ghi của người chơi theo lịch cho tới lúc khép.
- **Gỡ một đợt đang chạy:** người chơi dừng cộng điểm ngay; bản ghi vẫn khép theo giờ đã lưu và quà cuối đợt vẫn phát.

### 4.2 Thứ tự nâng cấp: game trước, JSON sau

Bản game cài package **0.1.0** đọc JSON định dạng 2 vẫn đọc đúng `events`, nhưng **bỏ `recurring` mà không báo** — luật
lặp biến mất với đúng những người chơi đó. Nên làm theo thứ tự:

1. Phát hành bản game dùng package **0.2.0**.
2. Chờ tới khi tỉ lệ người chơi còn ở bản cũ đủ nhỏ.
3. Rồi mới đăng JSON **định dạng 2** lên remote config.

Chưa tới bước 3 thì xuất **định dạng 1** ở màn Xuất JSON của hub — hub nói ngay "định dạng 1 không có recurring: n luật
lặp sẽ không được xuất".

### 4.3 Key remote config

Key là dữ liệu trong asset (field `remoteConfigKey`, mặc định `liveops_calendar`), đổi được ở màn Tổng quan của hub
("Đổi key remote…"). Package **không** tự đọc remote config: game lấy chuỗi theo key đó bằng SDK của mình rồi đưa vào
`ParseOrDefault`. Đổi key = phải đổi cả hai phía trong cùng một bản game.

> **Một người đăng mỗi lần.** Hub ghi lịch sử đăng vào cuối `publishedStamps` của asset. Hai người đăng song song sẽ
> xung đột git đúng ở chỗ đó; giải xung đột bằng cách **giữ cả hai mục** theo thứ tự giờ, đừng bỏ bên nào.

### 4.4 Remote config hỏng, hoặc về muộn

**JSON có chữ nhưng KHÔNG DÙNG ĐƯỢC** — `ParseOrDefault` xử theo `LiveEventCalendarRemoteFailurePolicy`. "Không dùng
được" gồm **hai** ca, vì hậu quả với người chơi giống hệt nhau (không đợt nào chạy):

1. `JsonUtility` không đọc nổi — sai cú pháp, BOM, gốc không phải object.
2. Đọc được nhưng **không có mảng lịch nào** — `{}`, gõ sai tên mảng (`"evets"`), hoặc một định dạng sau này đổi tên mảng.

| Chính sách | Người chơi thấy gì | Đánh đổi |
|---|---|---|
| `UseDefaultCalendar` (**mặc định của `ParseOrDefault`**) | lịch trong asset + một `Problem` | vẫn có event, đổi lại lịch trong build có thể **cũ hơn** bản đã đăng: đợt đã gỡ mở lại, id đổi thì mất tiến độ |
| `KeepRemoteResult` (giống `Parse`, phải **chọn tay**) | lịch rỗng + một `Problem` | không thấy đợt mới mở; đợt đang chạy vẫn khép theo giờ đã lưu và vẫn phát quà |

Câu `Problem` đứng đầu nói rõ ca nào: `"JSON remote hỏng, dùng lịch mặc định trong asset: <message của JsonUtility>"`
hoặc `"JSON remote không có mảng lịch nào, dùng lịch mặc định trong asset."` — không nuốt lỗi.

> ℹ️ **Đây là mặc định của một API mới, KHÔNG phải đổi hành vi của 0.1.0.** Bản 0.1.0 không có `ParseOrDefault` (chỉ có
> `Parse`, và `Parse` giữ nguyên từng chuỗi `Problems`), nên game bump `0.1.0` → `0.2.0` không bị đổi gì. Chỉ những bản
> dựng **trước** của nhánh `0.2.0` — lúc mặc định còn là `KeepRemoteResult` — mới thấy khác. Muốn đúng cách của `Parse`
> thì gọi overload ba tham số: `ParseOrDefault(json, asset, LiveEventCalendarRemoteFailurePolicy.KeepRemoteResult)`.

JSON **trống** thì luôn dùng lịch trong asset, không phụ thuộc chính sách và **không** thêm `Problem` (remote chưa đăng
không phải lỗi). `"events": []` — mảng **có mặt** mà rỗng — là lịch rỗng **có chủ ý**, luôn là kết quả remote: nếu không
thì không còn cách nào gỡ sạch đợt từ xa. JSON có mảng lịch nhưng vài mục hỏng thì cũng luôn dùng kết quả remote (mục
hỏng bị bỏ + `Problems`) — package không bao giờ trộn hai nguồn, vì trộn sinh ra một lịch không ai từng đăng.

**Remote về muộn (async).** `LiveOpsSystem` chốt lịch lúc `Build()`, và 0.2.0 **chưa có** API đổi lịch giữa phiên. Nên
chờ remote có hạn giờ rồi mới `Build()`; hết hạn thì dùng lịch trong asset và bản remote áp ở **lần mở app sau**:

```csharp
static async Task<LiveEventCalendarParseResult> LoadCalendarAsync(
    Func<Task<string>> fetchRemoteJsonAsync, LiveEventCalendarAsset mainCalendarAsset, TimeSpan waitBudget)
{
    Task<string> fetch = fetchRemoteJsonAsync();
    Task finished = await Task.WhenAny(fetch, Task.Delay(waitBudget));
    bool arrivedInTime = ReferenceEquals(finished, fetch) && fetch.Status == TaskStatus.RanToCompletion;
    string remoteJson = arrivedInTime ? fetch.Result : string.Empty;   // hết hạn / lỗi mạng → "" = dùng lịch trong asset
    return JsonLiveEventCalendarParser.ParseOrDefault(remoteJson, mainCalendarAsset);
}
```

Hạn giờ nên ngắn (vài giây) — người chơi đang chờ ở màn loading. Đoạn này được chạy thật trong
`ReadmeSnippetCompileTests`.

## 5. LiveOps Hub (chỉ Editor)

`Tools → DreamTech → LiveOps → LiveOps Hub`, hoặc nút **"Mở trong LiveOps Hub"** ở inspector của asset lịch. Hub chỉ sửa
asset trong project — **không gửi gì lên Firebase**: đăng vẫn là việc của người dùng trên console, hub chỉ xuất JSON,
Copy, lưu file và ghi dấu "đã đăng".

| Màn | Làm gì |
|---|---|
| Tổng quan | asset đang mở, key remote, sức khoẻ từng màn, việc cần làm, nhập JSON đang chạy thành asset mới |
| Loại event | khai loại, ô màu làn, `requiresJoin`, `configKey` mặc định của loại |
| Lịch | timeline kéo–thả đợt cố định, so với bản đã đăng / bản trên đĩa, dán JSON đang chạy, menu chuột phải, hover card |
| Luật lặp | neo, chu kỳ, thời gian chạy, tiền tố id; foldout "JSON của luật này" sửa thẳng bằng JSON |
| Kiểm lịch | 12 luật ở mục 5.1 — sửa an toàn, sửa hàng loạt, bỏ qua kèm ghi chú, hẹn xem lại |
| Xuất JSON | chọn định dạng 1 hay 2, Copy, Lưu file `liveops_calendar-<sha6>.json`, đánh dấu đã đăng |

Hub và game dùng **cùng một** bộ biên dịch lịch và **cùng một** thứ tự xuất, nên "hub kiểm", "game đọc asset" và "game
đọc JSON đã xuất" luôn giữ đúng cùng tập đợt — kể cả khi có id trùng.

### 5.1 Mười hai luật của màn Kiểm lịch

Id của luật nằm trong UI **và** trong ghi chú "Bỏ qua" lưu ở asset của người dùng: **đừng đổi chuỗi id sau khi phát
hành**, mọi cảnh báo đã bỏ qua sẽ mất hiệu lực. Cột Hậu quả: **Bỏ** = game bỏ mục đó · **Mất tiến độ** = người chơi
đang chơi dở mất tiến độ · **Nên xem** = game vẫn nhận, nhưng đáng nhìn lại.

| # | Id | Hậu quả | Bắn khi |
|---|---|---|---|
| 1 | <a id="utc-time-format"></a>`utc-time-format` | Bỏ | đợt cố định có `startUtc` / `endUtc` không đọc được — đề xuất chuẩn hoá về `yyyy-MM-ddTHH:mm:ssZ` |
| 2 | <a id="end-before-start"></a>`end-before-start` | Bỏ | kết thúc không sau bắt đầu — hai đề xuất: giữ bắt đầu và đặt thời lượng 24 giờ, hoặc đổi chỗ hai giờ |
| 3 | <a id="invalid-identifier"></a>`invalid-identifier` | Bỏ | id hoặc loại rỗng / chứa `#` / có xuống dòng (`#` là dấu ngăn trong grant id). Không sửa tự động — id mới là quyết định của người dùng |
| 4 | <a id="duplicate-event-id"></a>`duplicate-event-id` | Bỏ | id trùng một đợt đứng trước trong thứ tự xuất; game giữ mục đứng trước — đề xuất thêm hậu tố số (`hunt-0914-2`) |
| 5 | <a id="overlap-same-type"></a>`overlap-same-type` | Bỏ | chồng giờ với đợt cùng loại mở sớm hơn — hai đề xuất dời (giữ kết thúc, hoặc giữ thời lượng) |
| 6 | <a id="recurring-rule-invalid"></a>`recurring-rule-invalid` | Bỏ | luật lặp không dựng được (neo hỏng, chu kỳ ≤ 0, thời gian chạy ngoài `(0, chu kỳ]`, tiền tố hoặc loại sai quy tắc), hoặc là luật thứ hai của cùng một loại |
| 7 | <a id="shadowed-by-recurring"></a>`shadowed-by-recurring` | Bỏ | đợt cố định bị một lần lặp cùng loại che (chồng giờ hoặc trùng id) — luật lặp ghép trước. Không sửa tự động: gỡ che là đổi ý đồ lịch |
| 8 | <a id="unknown-event-type"></a>`unknown-event-type` | Bỏ | có đợt hoặc luật thuộc loại mà tài liệu không khai — lịch vẫn giữ mục, nhưng `LiveOpsSystem` không đăng ký loại đó nên game bỏ |
| 9 | <a id="running-event-id-changed"></a>`running-event-id-changed` | Mất tiến độ | đợt **đang chạy** ở bản đã đăng không còn đợt cùng id + loại trong nháp (bị xoá, đổi tiền tố, đổi loại, dời ra ngoài, hoặc khép ngay) |
| 10 | <a id="config-key-missing"></a>`config-key-missing` | Nên xem | mục không tự khai `configKey` (JSON sẽ ghi mặc định của loại) **và** khoá hiệu lực khác bản so — tức khoá tới người chơi sắp đổi |
| 11 | <a id="long-gap-between-events"></a>`long-gap-between-events` | Nên xem | loại **không có** luật lặp mà khoảng trống tới đợt kế cùng loại dài quá ngưỡng — người chơi mất thói quen quay lại |
| 12 | <a id="remote-snapshot-drift"></a>`remote-snapshot-drift` | Nên xem | JSON đang chạy (đã dán vào hub) khác bản đã đăng mới nhất. So **ngữ nghĩa**: console đổi khoảng trắng hay thứ tự key không bị báo lệch |

Game tự thêm luật riêng được (`ILiveEventCalendarRule`); id lạ không nằm trong `LiveEventCalendarRuleIds.All` chỉ mất
thứ tự chạy cố định, không bị từ chối.

## 6. Vòng đời một đợt với một người chơi

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

## 7. Port và adapter có sẵn

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

## 8. Bẫy và giới hạn đã biết

- **Chỉ dùng trên main thread** (cả `LiveOpsSystem`, `SyncedLiveOpsClock`, `HttpDateHeaderServerTimeSource`).
- **Không dùng `LiveOpsUnityRunner` thì phải tự gọi** `Refresh` định kỳ, `Invalidate` khi vào nền và `SyncAsync` khi quay lại. Quên
  `Invalidate` = sau khi máy ngủ, đồng hồ "tin được" chạy chậm hơn giờ thật đúng bằng thời gian ngủ.
- **Đổi `systemId` hoặc `idPrefix` của lịch lặp** = mọi người chơi bắt đầu lại từ đầu.
- **`PlayerPrefsLiveOpsTextStore` gọi `PlayerPrefs.Save()` mỗi lần ghi.** Game cộng điểm dày (mỗi lần nhặt vật phẩm) nên cắm store của
  game có gom ghi.
- **Vặn giờ tới lúc offline có thể vào đợt sau sớm hơn** (chưa chặn — cần backend để chặn triệt để).
- **Lịch chốt lúc `Build()`.** Không có API đổi lịch giữa phiên ở 0.2.0 — remote về muộn thì theo mục 4.4.
- **Game 0.1.0 đọc JSON định dạng 2 bỏ `recurring` mà không báo** — xem thứ tự nâng cấp ở mục 4.2.
- **Đổi id của một luật Kiểm lịch** làm mọi ghi chú "Bỏ qua" đã lưu trong asset mất hiệu lực (mục 5.1).
- **Chưa có backend:** chưa định danh người chơi, chưa cloud save, chưa bảng xếp hạng người thật. Nối bảng xếp hạng/League của
  `com.dreamtech.leaderboard` bằng adapter phía game — hai package không phụ thuộc nhau.

## 9. Test

```bash
# Dev project Unity 6 — dùng công cụ của repo (giữ hạn giờ + tối đa 2 Unity batch một lúc)
tools/liveops-hub/run-editmode.sh --unity 6000 --category all    # gồm test giao diện hub → KHÔNG -nographics
tools/liveops-hub/run-playmode.sh

# Unity 2022.3 — project tạm trỏ file: vào package
tools/liveops-hub/make-temp-project-2022.sh --bootstrap
tools/liveops-hub/run-editmode.sh --unity 2022 --category all
```

Test giao diện của hub (category `LiveOpsHub.UI`) phải chạy **không** `-nographics`: 2022.3 với `-nographics` cho layout
NaN, không vẽ và không nhận phím. Test core, test lớp Unity và category `LiveOpsHub.Logic` chạy được `-nographics`.

| Nhóm | Kiểm gì |
|---|---|
| `LiveEventCalendarTests` | ranh giới giai đoạn, id tất định, lúc nghỉ, lịch cố định bỏ mục trùng/chồng, lịch ghép ưu tiên nhất quán theo mọi khung hỏi |
| `SyncedLiveOpsClockTests` | tin được / chưa tin được, độ lệch, mốc chống vặn lùi qua lần mở app, vặn tới không nâng mốc, đồng bộ chồng nhau, phản hồi cũ sau khi vào nền |
| `LiveOpsSystemTests` | tự vào / bấm vào / điều kiện, grant id, khép đúng một lần, khép khi app tắt, quà chờ qua lần mở app, granter lỗi, quà giữa đợt, auto-collect, đợt kế tiếp, lịch dời/gỡ đợt, vặn giờ lùi sau khi dọn, chờ đồng hồ tin được, luật lỗi, CustomState, loại event bị gỡ, save hỏng, codec |
| `LiveOpsUnityAdapterTests` | tính giờ từ Date + Age + khứ hồi, parser JSON (múi giờ, mục hỏng, JSON hỏng), PlayerPrefs store |
| `JsonLiveEventCalendarParserFormat2Tests` | định dạng 2: luật lặp hỏng bị bỏ riêng, `version` lớn hơn vẫn đọc, thiếu cả hai mảng |
| `LiveEventCalendarAssetTests`, `CalendarRoundTripTests` | asset ↔ tài liệu ↔ JSON khứ hồi; asset sửa tay hỏng không làm `Build()` ném |
| `LiveEventCalendarRemoteFailurePolicyTests` | hai chính sách × hai ca remote không dùng được, và `"events": []` không phải fallback (mục 4.4) |
| `ReadmeSnippetCompileTests` | đoạn code trong README này compile và chạy được, và mọi id luật có mục trong README |
| `Tests/Editor/Hub/**` | LiveOps Hub: khung, 6 màn, 12 luật, xuất JSON, ảnh cửa sổ |
| Demo PlayMode (`Assets/Demo/Tests`) | runner tự khép đợt và phát rương, tiến độ qua lần nạp scene, bấm tham gia + quà mốc một lần, điều kiện level, mục lịch hỏng |
