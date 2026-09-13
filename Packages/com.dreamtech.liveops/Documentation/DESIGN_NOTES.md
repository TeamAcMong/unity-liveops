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

## 4. Đã kiểm chứng

Xem bảng kết quả trong `CHANGELOG.md` của phiên bản tương ứng.

## 5. Backlog (chưa làm, cần người dùng đồng ý)

- **0.2.0 — module event có mốc:** lớp nền cho event điểm + mốc + track miễn phí/trả phí (battle pass, collection) trên
  `ClaimReward` / luật khép đợt; bảng thưởng theo mốc cấu hình được.
- **0.3.0 — port backend:** định danh người chơi, cloud store (`ILiveOpsTextStore` trên cloud save, có giải quyết xung đột), dịch
  vụ event phía server (lịch + điểm server-authoritative) kèm bộ test hợp đồng cho mọi bản cài; bản giả lập offline.
- Chặn vặn giờ tới lúc offline triệt để (cần giờ server khi vào đợt).
- Local notification "event bắt đầu / sắp hết giờ" qua port (package không phụ thuộc plugin notification).
- Phân nhóm người chơi (A/B, segment) trong lịch JSON.
