# Design notes — DreamTech LiveOps

Vì sao package được thiết kế như vậy. Cách dùng ở `README.md`, luật cho người sửa code ở `CLAUDE.md`.

## 1. Yêu cầu gốc

Người dùng (2026-09-13) review MLGameKitV2 — kit nội bộ đang chép vào từng game — để xem đã có hệ thống chuẩn cho meta theo mùa
(vừa local vừa có backend) như các game puzzle lớn chưa. Kết luận: có nhiều tính năng meta đóng gói sẵn nhưng không có tầng
live-ops chung và không có backend. Người dùng chốt làm thành **core module package** theo khuôn các package đang dùng
(`com.game.addressables`, `com.dreamtech.uicore`, `com.dreamtech.leaderboard`), nguyên văn:

> "làm như không có MLGamekit vậy đừng để bị coupling và dependency nha phải làm thật chuẩn như package vào"

Và luật chung đã có từ trước cho mọi hệ thống mới: module hoá, decoupling, lắp ráp như Lego — mang sang game khác được, gỡ ra
không phải mổ code.

## 2. Hệ thống cũ làm gì, lỗi ở đâu (bối cảnh, không phải phụ thuộc)

Package không tham chiếu kit. Phần này chỉ ghi lại để hiểu các quyết định ở mục 3.

| Hiện tượng trong kit | Hệ quả |
|---|---|
| Mùa của battle pass neo vào lúc người chơi **thấy tính năng lần đầu**, dài N phút | Mỗi người một mùa; không làm được event đồng loạt hay cạnh tranh |
| Giờ server lấy một lần lúc boot (fire-and-forget, timeout 60s), lỗi thì **âm thầm** dùng giờ máy, không đồng bộ lại khi app quay lại | Không biết lúc nào giờ đáng tin |
| Nhiều module (quà hằng ngày, gói bán theo ngày, giới hạn số lần mỗi ngày của trigger) gọi thẳng `DateTime.Now` | Vặn giờ máy là ăn gian |
| Reset save của mùa chạy ở **phiên sau** khi đã tự nhận quà; màn tổng kết cuối mùa giữ trong RAM chờ Home | Luồng "hết mùa lúc app tắt" dễ mất màn kết quả |
| Mỗi module tự viết timer, reset, save, auto-collect (600–1100 dòng mỗi module), chép sang từng game rồi sửa lệch nhau | Vá một lỗi không sang game khác |
| Quà phát thẳng vào kho đồ, không hàng chờ | Kho chưa sẵn sàng hoặc app tắt giữa chừng là mất |

## 3. Quyết định và lý do

### 3.1 Dữ liệu khoá theo id từng đợt, giờ tuyệt đối đến từ lịch

Mỗi đợt là `LiveEventInstance(id, type, startUtc, endUtc)`. Lịch lặp sinh id `tiền tố + số thứ tự` từ một mốc UTC, nên mọi máy,
mọi lần cài lại đều ra cùng đợt. Tiến độ, quà, kết quả của người chơi nằm trong bản ghi khoá theo id đó.

Hệ quả mong muốn: event đồng loạt được; hai đợt không bao giờ dùng chung dữ liệu; "đợt mới" không cần bước reset save — nó là
một bản ghi mới. Chặn bằng `Recurring_IdsAreDeterministic_NegativeBeforeAnchor`, `NextOccurrence_StartsWithFreshRecord_OldOneFinalizedFirst`.

### 3.2 Bản ghi giữ bản sao khung giờ; lịch thắng khi đợt chưa khép

Lịch đến từ remote config nên có thể đổi bất cứ lúc nào. Hai tình huống thật:
- Designer kéo dài event đang chạy → người chơi phải được chơi tiếp. Refresh tìm lại đợt theo id trong khung
  `[start đã lưu, max(end đã lưu, bây giờ)]` và cập nhật. (`CalendarExtendsRunningEvent_RecordFollowsCalendar`)
- Designer gỡ event → dừng cộng điểm ngay, nhưng người đã chơi vẫn được khép và nhận quà theo giờ đã lưu.
  (`CalendarDropsJoinedEvent_StopsProgress_StillFinalizesAtStoredEnd`)

### 3.3 Khép đợt: luật chạy một lần, quà vào hàng chờ trước khi phát

`Refresh` khép mọi bản ghi đã hết giờ — kể cả hết giờ lúc app tắt 20 ngày. Luật khép đợt được gọi TRƯỚC khi đổi trạng thái: luật
ném lỗi thì bản ghi còn nguyên, lần sau thử lại, đợt khác vẫn khép bình thường. Quà được lưu vào hàng chờ trước khi đưa cho granter;
granter trả false hoặc ném lỗi thì quà nằm chờ qua các lần mở app. Grant id ổn định để game chống trùng nếu kho đồ hỗ trợ.

"Tự nhận quà chưa nhận" của battle pass không phải cơ chế riêng — luật khép đợt đọc `ClaimedKeys`. (`CompletionRule_SeesClaimedKeys_AutoCollectsOnlyUnclaimed`)

### 3.4 Kết quả chờ nằm trong save, không nằm trong RAM

Đợt đã khép mà UI chưa báo (`IsPendingResult`) được lưu cùng bản ghi. Màn Home hỏi `GetPendingResults()` bất cứ lúc nào nó hiện,
không cần bắt đúng thời điểm sự kiện. (`EventEnd_FinalizesOnce_PaysCompletionReward_ResultWaitsForAcknowledge`)

### 3.5 Đồng hồ: một nguồn, có trạng thái tin được rõ ràng

- Neo giờ server + `Stopwatch`, không đọc giờ máy khi đang tin được.
- `IsTrusted` là một phần của port, để chính sách (vd `finalizeRequiresTrustedClock`) quyết định — không giấu việc đang dùng giờ ước lượng.
- **Mốc tin được** chặn vặn giờ lùi qua các lần mở app; **chỉ giờ tin được mới nâng mốc**, nếu không người vặn giờ tới lúc offline
  sẽ làm đồng hồ kẹt ở tương lai sau khi có mạng. (`UntrustedForwardTime_NeverRaisesHighWater`)
- `Invalidate` khi vào nền vì bộ đếm đơn điệu không chạy lúc máy ngủ; lần đồng bộ đang bay lúc đó bị bỏ vì nó neo vào bộ đếm cũ.
  (`GoingToBackgroundDuringSync_DiscardsStaleResponse`)
- Id đợt đã dọn vẫn được nhớ (`RetiredEventIds`), nên dọn bản ghi cũ không mở lại lỗ vặn giờ lùi.
  (`RewoundClock_CannotReplayFinishedEvent_EvenAfterRecordRetired`)

### 3.6 Nguồn giờ = header `Date` của server của game

Kit cũ gọi các API thời gian công cộng miễn phí theo IP. Header `Date` có trên mọi phản hồi HTTP nên game trỏ vào server/CDN của
chính mình, không phụ thuộc dịch vụ bên thứ ba. Độ chính xác ±1 giây là đủ cho event tính theo giờ/ngày. Phản hồi lấy từ cache CDN
mang `Date` của lúc tạo bản gốc nên phải cộng `Age`.

### 3.7 `finalizeRequiresTrustedClock` mặc định false

Bật mặc định thì game dùng `SystemLiveOpsClock` (không bao giờ tin được) sẽ không bao giờ khép đợt — lỗi âm thầm. Mặc định an toàn
cho mọi đồng hồ; game dùng `SyncedLiveOpsClock` bật lên, đổi lại người chơi offline phải chờ có mạng mới thấy kết quả.

### 3.8 Không phụ thuộc gì

- **Core `noEngineReferences`, không UniTask:** UPM không tự kéo git dependency khai trong `package.json` của package, nên package
  phụ thuộc UniTask bắt game tự thêm. `Task` của BCL đủ cho một lần gọi mạng. Core không đụng Unity nên test chạy nhanh và dùng lại
  được ở server .NET nếu sau này có backend C#.
- **Không JSON lib trong core:** trạng thái mã hoá bằng bản ghi `khoá=giá trị` nhiều dòng có `format` (đọc được bằng mắt trong
  PlayerPrefs, đổi định dạng về sau được). Lịch JSON đọc bằng `JsonUtility` ở lớp Unity.
- **Không UI sản phẩm:** chỗ đặt UI và art là của game. Package chỉ có debug panel IMGUI.
- **Không phụ thuộc `com.dreamtech.leaderboard`:** League có port đồng hồ/lịch mùa riêng. Nối bằng adapter phía game để gỡ package
  này không làm vỡ package kia.

### 3.9 Lỗi của code game đi qua event, không đi qua exception của Refresh

`Refresh` chạy mỗi vài giây từ runner và từ bên trong `AddPoints` / `TryJoin` / `ClaimReward`. Nếu luật của một loại event ném lỗi
làm hỏng Refresh, mọi thao tác của mọi loại event khác cũng hỏng theo. Nên lỗi được bắt theo từng bản ghi và báo qua
`CompletionRuleFailed` / `RewardGrantFailed`; runner ghi log.

### 3.10 Lịch sống trong project, không chỉ trên remote config (0.2.0, quyết định D1)

0.1.0 chỉ có hai nguồn lịch: code (`RecurringLiveEventCalendar`) và chuỗi JSON từ remote config. Cả hai đều **không
review được bằng git** và không ai thấy lịch sắp đăng trông ra sao trước khi dán lên console.

0.2.0 thêm `LiveEventCalendarAsset` — MỘT ScriptableObject giữ định nghĩa loại, luật lặp, đợt cố định, lịch sử đăng và
cảnh báo đã bỏ qua. Hệ quả chọn có chủ đích:

- **Một asset, không nhiều file.** `publishedStamps` / `ignoredWarnings` đi theo build; `ToDocument()` nạp cả hai vào
  tài liệu nhưng không đường chạy nào của game dùng tới (chỉ hub đọc). Mỗi dấu đã đăng giữ nguyên chuỗi JSON của bản đó
  nên kích thước lớn theo số lần đăng, không phải hằng số.
  Đổi lại toàn bộ lịch là một file để review, một lần merge, một lịch sử git.
- **Asset chỉ là nơi lưu.** Mọi phán "mục này có hợp lệ không" nằm ở bộ biên dịch trong core, nên "game đọc asset",
  "game đọc JSON đã xuất" và "hub kiểm lịch" không bao giờ tự đoán luật riêng. YAML sửa tay hỏng hay xung đột merge
  không được làm `Build()` của game ném: field null coi như rỗng, mục sai quy tắc bị bỏ.
- **Giờ lưu nguyên văn chuỗi ISO** (PD-2), chu kỳ/thời gian chạy lưu số nguyên giờ. Một đợt gõ nhầm `"2026-10-3"` phải
  còn nguyên trong asset, trong JSON và trong danh sách lỗi — chuẩn hoá sớm là giấu mất lỗi của designer. Cũng tránh
  `DateTime` `Kind = Local` lọt vào dữ liệu.
- **Asset là lịch mặc định khi remote trống hoặc không dùng được** (`ParseOrDefault`), nên game mới cài chưa kịp fetch,
  hoặc gặp một bản remote hỏng, vẫn có event. Chi tiết hai ca "không dùng được" ở §3.12.

### 3.11 JSON định dạng 2 và bộ ghi viết tay (D2, PD-6)

Định dạng 1 chỉ có `events`, nên đổi neo/chu kỳ/tiền tố của một luật lặp phải phát hành lại game. Định dạng 2 thêm
`version` + `recurring`.

- **Bộ ghi viết tay bằng `StringBuilder`, không `JsonUtility`.** `JsonUtility` không bỏ được field, không cố định
  được định dạng số và ngày. JSON của hub phải **tất định đến từng byte** giữa hai bản Unity, vì dấu "đã đăng" là
  SHA-256 của đúng chuỗi đó — một khoảng trắng lệch là "khác bản đã đăng" giả.
- **Thứ tự xuất là một hàm core** (PD-39): `FixedLiveEventCalendar` khử trùng id *theo thứ tự nhận*, nên nếu hub kiểm
  theo thứ tự asset còn game đọc JSON đã sắp thì hai mục trùng id sẽ giữ khác nhau ở hai phía. Một hàm dùng chung là
  cách duy nhất chắc chắn.
- **`recurring` giữ thứ tự trong asset, không theo thứ tự làn trên màn** (PD-41). Đưa một làn lên/xuống chỉ là cách
  xem; nếu nó đổi byte thì sha đổi trong khi diff rỗng, và hub sẽ nói "khác bản đã đăng" mà không chỉ ra được gì.
- **Tương thích ngược có giá.** Game cài 0.1.0 đọc JSON định dạng 2 vẫn đọc `events` nhưng bỏ `recurring` **im lặng**.
  Không chữa được ở phía package (bản cũ đã ra ngoài), nên chữa bằng quy trình: README mục 4.2 ghi thứ tự nâng cấp
  game trước, và hub cảnh báo ngay tại màn Xuất JSON.
- **Khoá remote config là dữ liệu, không phải hằng** (PD-8): field `remoteConfigKey` trong asset. Đổi quyết định
  "dùng key nào" không phải sửa code package.

### 3.12 Khi remote config không dùng được (Q-9, Q-16)

Hai tình huống khác nhau, hai cách xử khác nhau — cả hai đều **không** đoán thay game:

- **JSON có chữ nhưng không dùng được.** `LiveEventCalendarRemoteFailurePolicy` là enum chứ không phải `bool`, để
  người đọc code thấy ngay hệ quả và để thêm cách thứ ba sau này mà không đổi chữ ký. Mặc định là `UseDefaultCalendar`
  (user chốt Q-9): "một bản remote hỏng làm cả game không đợt nào chạy" là hỏng nặng hơn "lịch trong build có thể cũ
  hơn bản đã đăng" — và cái sau còn tự lành ở lần đăng tiếp theo. Bản hỏng vẫn để lại `Problem` nên dev không mất dấu;
  game nào cần đúng cách của `Parse` thì gọi overload ba tham số với `KeepRemoteResult`.
  Đây là **mặc định của một API mới, không phải đổi hành vi của 0.1.0**: bản 0.1.0 chưa có `ParseOrDefault`, nên game
  bump `0.1.0` → `0.2.0` không đổi gì; chỉ các bản dựng trước của nhánh `0.2.0` mới thấy khác.
- **"Không dùng được" phải gồm cả JSON đúng cú pháp mà thiếu mảng lịch** (`{}`, gõ sai tên mảng). Nếu chỉ bắt ca
  `JsonUtility` ném thì lời hứa trên thủng đúng ở ca dễ gặp nhất — một key remote config bị đặt sai giá trị vẫn làm cả
  game trắng lịch. Ranh giới là **mảng có mặt hay không**: `"events": []` là designer cố ý gỡ hết đợt, luôn dùng kết quả
  remote, kẻo không còn cách nào tắt sạch event từ xa.
- **JSON đọc được nhưng có mục hỏng** thì LUÔN dùng kết quả remote, không bao giờ trộn với asset. Trộn hai nguồn sinh
  ra một lịch **không ai từng đăng** — không ai kiểm được nó.
- **Remote về muộn (async).** 0.2.0 cố tình *không* thêm API đổi lịch giữa phiên (giữ luật "chỉ thêm API khi có thiết
  kế"): `LiveOpsSystem` chốt lịch lúc `Build()`. Cách dùng là chờ remote có hạn giờ rồi mới `Build()`, hết hạn thì
  dùng asset và bản remote áp ở lần mở app sau. Đoạn code đó nằm trong README mục 4.4 và được **chạy thật** trong
  `ReadmeSnippetCompileTests` — tài liệu không được nói một đằng code một nẻo.

### 3.13 Hub là công cụ Editor, không phải tính năng runtime (D3, D5)

- **`EditorWindow`, không làm UI runtime.** QA trên máy thật vẫn dùng `LiveOpsDebugPanel` có sẵn. Assembly
  `DreamTech.LiveOps.Editor` chỉ Editor nên không có gì của hub đi vào build của game.
- **Hub không tự đăng lên Firebase.** Nó chỉ xuất JSON, Copy, lưu file và ghi dấu "đã đăng". Gắn SDK console vào một
  công cụ Editor là thêm bí mật và quyền vào repo game để đổi lấy một cú bấm.
- **Kiểm lịch so ngữ nghĩa, không so chuỗi** (PD-31): console đổi khoảng trắng, xuống dòng hay thứ tự key mà game vẫn
  đọc ra đúng lịch cũ. Báo lệch giả ở đây làm người dùng mất niềm tin vào chính dấu "khớp bản đã đăng".
- **Id của 12 luật là dữ liệu của người dùng.** Ghi chú "Bỏ qua" lưu theo id trong asset, nên đổi chuỗi id sau khi
  phát hành làm mọi cảnh báo đã bỏ qua mất hiệu lực. Vì thế id chỉ được viết ở đúng một nơi
  (`LiveEventCalendarRuleIds`) và có mục riêng trong README.

### 3.14 Giới hạn độ dài là HẰNG CÓ TÀI LIỆU, không phải cửa ném lỗi (W11)

- **Vì sao phải có một con số.** `ValidateIdentifier` chỉ kiểm ký tự, nên trước W11 không ai trả lời được bằng số câu
  "hub phải vẽ vừa id dài tới đâu". Hệ quả là bộ dữ liệu "xấu nhất" của cổng bố cục dựng bằng chuỗi dài tuỳ hứng: đo ra
  178 chỗ hỏng thì con số ấy nói về dữ liệu người dùng hay nói về trí tưởng tượng của người viết fixture, không phân
  biệt được. Đặt giới hạn biến "xấu nhất" thành một mức ĐO ĐƯỢC.
- **Vì sao KHÔNG ném khi vượt.** Bất biến của package: dữ liệu xấu từ xa không được làm đường chạy của game ném — mục
  hỏng thì bị bỏ và ghi lý do. Thêm một cửa ném ở `ValidateIdentifier` sẽ làm đúng cái nó cấm, và còn là thay đổi phá
  vỡ với game đã đặt id dài hơn 64. Nên giới hạn sống ở `LiveOpsIdentifierLimits` như hợp đồng soạn thảo, và chỗ THẬT
  SỰ đọc nó hôm nay là bộ dữ liệu xấu nhất của cổng bố cục (`LiveOpsWorstCaseSample`, có test đo từng chuỗi).
- **Một hằng chỉ có nghĩa khi có mẩu dữ liệu dựng theo nó.** Sáu trên bảy giới hạn có ca đo mỗi lượt, trong đó bốn được
  ma trận 12 màn vẽ ra bằng chuỗi fixture dài đúng mức. Giới
  hạn thứ bảy — `MaxAssetNameLength` — mới có hằng fixture và ca đo độ dài, chưa màn nào đứng trên một tên asset 64 ký
  tự, và chỗ đặt tên ấy (`LiveOpsHubTestServices`) dùng chung cho mọi màn nên nối vào là đổi số của cả những màn đang
  xanh. Khai thành nợ W11-14 thay vì để hằng nằm im như một con số ai cũng tưởng đã được nghiệm thu.
- **Giới hạn nói về ĐỘ DÀI CHUỖI ĐÃ GIẢI MÃ.** `Main.asset` ghi tên hiển thị dạng escape YAML, nên dòng thô dài hơn hẳn
  chuỗi thật (30 so với 18). Mọi phép suy trong mục này đếm trên chuỗi đã giải mã.
- **Vì sao con số suy ra chứ không chọn tay.** `MaxRecurringIdPrefixLength` = `MaxIdentifierLength` −
  `MaxOccurrenceIndexLength`: id một lần lặp là tiền tố + số thứ tự, nên một tiền tố "trông vẫn ngắn" vẫn sinh ra id
  vượt giới hạn. `MaxOccurrenceIndexLength` = bề rộng chữ của `long.MinValue`, đo được chứ không ước lượng.
  `MaxIdentifierLength` = 64 chốt từ hai phía: hơn hai lần mục dài nhất người thật đã soạn trong repo (29 ký tự), và
  giữ grant id `liveops.claim#…#…` ở 143 ký tự — dưới mốc 255 của các kho khoá-giá trị hay dùng.

## 4. Đã kiểm chứng

Xem bảng kết quả trong `CHANGELOG.md` của phiên bản tương ứng.

## 5. Lộ trình (quyết định D6)

**0.2.0 — LiveOps Hub.** Asset lịch + JSON định dạng 2 + cửa sổ designer. Chia ba đợt, mỗi đợt dùng được ngay:

| Đợt | Nội dung | Trạng thái |
|---|---|---|
| **P1** | Khung hub (token hai skin, rail, ⌘K, status bar, toast, Undo) · Tổng quan · Loại event · Lịch · Luật lặp · Kiểm lịch (12 luật + diff ở core) · Xuất JSON · `LiveEventCalendarAsset` + JSON định dạng 2 | **đang làm** — xem `[Unreleased]` trong `CHANGELOG.md` |
| **P2** | Trực tiếp (hệ thống đang chạy, tua giờ, đồng bộ) · Dữ liệu người chơi (`LiveOpsStateReader`, xuất/nhập file trạng thái) | chưa làm |
| **P3** | Mô phỏng (chạy thử lịch trên đường thời gian, lưu kết quả làm bản so hồi quy) | chưa làm |

Xong P1 khi: designer tạo được lịch 3 tuần, sửa hết lỗi, copy JSON và game đọc ra đúng lịch đó — chạy bằng máy trong
`EndToEndJourneyTests`.

**0.3.0 — module event có mốc.** Lớp nền cho event điểm + mốc + track miễn phí/trả phí (battle pass, collection) trên
`ClaimReward` / luật khép đợt; bảng thưởng theo mốc cấu hình được. Hub 0.2.0 **không** có màn Quà và mốc thưởng — cột
Quà của màn Mô phỏng ghi rõ "chung · mốc thưởng 0.3.0".

**0.4.0 — port backend.** Định danh người chơi, cloud store (`ILiveOpsTextStore` trên cloud save, có giải quyết xung
đột), dịch vụ event phía server (lịch + điểm server-authoritative) kèm bộ test hợp đồng cho mọi bản cài; bản giả lập
offline.

**Chưa xếp phiên bản:**

- Chặn vặn giờ tới lúc offline triệt để (cần giờ server khi vào đợt).
- Lịch đổi được giữa phiên, cho remote config về muộn (Q-16 — 0.2.0 hướng dẫn chờ có hạn giờ thay vì thêm API).
- Local notification "event bắt đầu / sắp hết giờ" qua port (package không phụ thuộc plugin notification).
- Phân nhóm người chơi (A/B, segment) trong lịch JSON.
