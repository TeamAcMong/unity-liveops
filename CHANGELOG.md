# Changelog

Mọi thay đổi đáng kể của package ghi ở đây. Định dạng theo [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
phiên bản theo [Semantic Versioning](https://semver.org/).

## [Unreleased]

**LiveOps Hub** — lịch event sống trong project, có cửa sổ cho designer, có bộ kiểm trước khi đăng. Đường chạy của game
không đổi: `Parse`, `LiveOpsSystem` và mọi port của 0.1.0 giữ nguyên chữ ký và nguyên chuỗi `Problems`.

### Added

**Lịch trong project**
- `LiveEventCalendarAsset` (ScriptableObject, schema 1): định nghĩa loại, luật lặp, đợt cố định, lịch sử đăng, cảnh báo
  đã bỏ qua — một file review được bằng git. `ToDocument` / `ApplyDocument` / `Compile` / `ToParseResult`; asset sửa tay
  hỏng không làm `Build()` của game ném.
- `LiveOpsSystemBuilder.WithEventTypesFrom(asset, completionRuleForType, eligibilityForType)`: đăng ký mọi loại khai
  trong asset (`requiresJoin` → `ExplicitJoin`, còn lại `JoinOnFirstProgress`).
- `JsonLiveEventCalendarParser.ParseOrDefault(json, asset[, policy])`: remote trống → lịch trong asset; remote hỏng →
  theo `LiveEventCalendarRemoteFailurePolicy` (`KeepRemoteResult` mặc định = hành vi 0.1.0, hoặc `UseDefaultCalendar`).

**JSON định dạng 2**
- Gốc `version` + `recurring` + `events`; luật lặp đổi được bằng remote config. Parser đọc cả định dạng 1 và 2, và đọc
  được phần quen của định dạng mới hơn kèm một `Problem`. Luật lặp hỏng bị bỏ riêng luật đó, `events` vẫn đọc.
- `LiveEventCalendarParseResult` thêm `CombinedCalendar` (`Composite(luật lặp…, đợt cố định)`), `FormatVersion`,
  `EntryOutcomes`, `CameFromDefaultCalendar`; `Calendar` giữ nghĩa 0.1.0 (chỉ đợt cố định).
- Bộ ghi JSON tất định đến từng byte (UTF-8 không BOM, LF, thụt 2, thứ tự field cố định) + SHA-256 của đúng chuỗi đó,
  để dấu "đã đăng" so được. Thứ tự xuất là một hàm core dùng chung cho cả hub lẫn game.

**Kiểm lịch (core)**
- 12 luật (`LiveEventCalendarRuleIds`): `utc-time-format`, `end-before-start`, `invalid-identifier`,
  `duplicate-event-id`, `overlap-same-type`, `recurring-rule-invalid`, `shadowed-by-recurring`, `unknown-event-type`,
  `running-event-id-changed`, `config-key-missing`, `long-gap-between-events`, `remote-snapshot-drift`; mỗi luật một
  test. Phát hiện mang hậu quả với người chơi (Bỏ / Mất tiến độ / Nên xem) và đề xuất sửa.
- Diff hai tài liệu lịch phân loại theo hậu quả; game tự thêm luật riêng được qua `ILiveEventCalendarRule`.

**LiveOps Hub (`DreamTech.LiveOps.Editor`, chỉ Editor)**
- Cửa sổ `Tools → DreamTech → LiveOps → LiveOps Hub` (và nút "Mở trong LiveOps Hub" ở inspector asset) với 6 màn:
  Tổng quan, Loại event, Lịch, Luật lặp, Kiểm lịch, Xuất JSON. Hai skin sáng/tối, palette ⌘K, status bar, toast + Undo.
- **Lịch:** timeline kéo–thả **bắt lưới** theo menu "Bắt lưới" (Tự động / 15 phút / 1 giờ / 1 ngày / Tắt), tag đọc
  nhanh ngay trên thanh khi kéo ("sẽ bị bỏ", chồng bao nhiêu), dòng gợi ý nói cử chỉ nhấp đúp khi con trỏ đứng ở chỗ
  trống của một làn; minimap, hover card ghim được, menu chuột phải, ⌘+kéo tạo đợt, đưa làn lên/xuống, pane "Danh sách",
  pane "So với" (bản đã đăng hoặc bản trên đĩa), inspector thành drawer khi cửa sổ hẹp.
- **Chọn nhiều đợt trên timeline:** ⌘-click cộng dồn, Shift-click chọn cả dải, kéo khung chọn trên nền; inspector đổi sang
  bản tóm tắt ("2 đợt · lava-quest") và các việc làm được cho cả tập chọn. Giữ Shift **sau khi** đã bắt đầu kéo thì mọi đợt
  phía sau trên cùng làn đi theo, readout nói kéo theo mấy đợt.
- **Làn thu gọn:** menu header làn "Thu gọn / Mở làn" hạ làn xuống 22 px để lịch dài vẫn nhìn được cả bức.
- **Luật lặp:** neo / chu kỳ / thời gian chạy / tiền tố id, preset, và foldout "JSON của luật này" sửa thẳng bằng JSON
  với lỗi có dòng và cột.
- **Kiểm lịch:** sửa an toàn, sửa hàng loạt (một bước Undo), card xem trước, bỏ qua kèm ghi chú và phạm vi, hẹn xem lại
  có tag "đã tới hẹn"; nút áp đề xuất in động từ theo cách sửa.
- **Xuất JSON:** chọn định dạng 1 hay 2, cổng xuất đếm điều kiện chặn, Copy, lưu file `liveops_calendar-<sha6>.json`, đánh dấu đã
  đăng. Hub **không** gửi gì lên Firebase.
- **Dán / Nhập JSON đang chạy:** dán bản đang chạy để so (`remote-snapshot-drift` hết "chưa kiểm"), thay nháp bằng bản
  dán, hoặc nhập thành asset mới khi project chưa có asset nào.
- **Khung:** rail thu còn 36 px kèm menu tầng ở cửa sổ hẹp, băng "asset đã đổi trên đĩa" (tự so hash, không tự đè),
  "Đổi key remote…", "Hiện dữ liệu mẫu" không ghi đĩa. Lý do một nút/ô/tab bị khoá luôn in **thành chữ**, không chỉ
  tooltip. Menu ⋮ có "Hiện hướng dẫn phím tắt": popover liệt kê mọi phím của hub đọc thẳng từ Shortcut Manager (không chép
  tay, nên nhãn phím đúng theo máy và theo nền tảng) kèm nút mở cửa sổ Shortcuts.
- Hai bộ chữ đầy đủ **Tiếng Việt + English**, mặc định English; mọi chuỗi đọc qua catalog theo khoá.

### Changed
- `package.json` thêm module `com.unity.modules.uielements` (hub dùng UI Toolkit).
- `README.md`: thêm asset lịch, định dạng 2, `ParseOrDefault`, `CombinedCalendar`, `WithEventTypesFrom`, cách mở hub,
  bảng 12 luật có anchor `#<rule-id>`, thứ tự nâng cấp "game 0.2.0 trước, JSON định dạng 2 sau".
- `Documentation/DESIGN_NOTES.md`: thêm 3.10–3.13 (vì sao của asset, định dạng 2, cách xử remote hỏng/về muộn, hub) và
  đổi §5 thành lộ trình D6.

### Notes
- **Đã kiểm: Unity 2022.3.62f2 và Unity 6000.6.0f1** — EditMode ở cả hai bản (số test khớp nhau), PlayMode trên dev
  project Unity 6, và ảnh cửa sổ hub chụp hai skin × hai bản. **6000.0 – 6000.5: chưa kiểm** — nhánh
  `#if UNITY_2023_2_OR_NEWER` / `UNITY_6000_0_OR_NEWER` mới chỉ chạy trên 6000.6.
- Test giao diện của hub (category `LiveOpsHub.UI`) phải chạy **không** `-nographics`; 2022.3 với `-nographics` cho
  layout NaN. Category `LiveOpsHub.Logic` và test core/Unity chạy được `-nographics`.
- Số test và bộ ảnh của bản phát hành được chốt ở mục kiểm chứng khi bump version.
- Đường chạy của game hiện không gọi SHA-256 (chỉ hub gọi). **Chưa dựng build IL2CPP để kiểm** — kết luận
  "strip không ảnh hưởng game" chờ bản demo IL2CPP của R-28.

### Chưa có ở bản này
- **Màn P2 của hub** (Trực tiếp, Dữ liệu người chơi) và **màn P3** (Mô phỏng): chưa đăng ký, rail và ⌘7–9 **ẩn** chứ
  không hiện dạng khoá.
- **Mốc thưởng / battle pass** là module 0.3.0; **backend** (định danh người chơi, cloud save, điểm
  server-authoritative) là 0.4.0.
- **Không có API đổi lịch giữa phiên.** Remote config về muộn thì chờ có hạn giờ rồi mới `Build()` — README mục 4.4.
- Chưa chặn triệt để việc vặn giờ tới lúc offline để vào đợt sau sớm (cần giờ server khi vào đợt).

## [0.1.0] - 2026-09-13

Bản đầu. Nền live-ops dùng chung cho mọi game, viết mới và không phụ thuộc kit nội bộ hay SDK nào: đồng hồ server chống vặn giờ
máy, lịch event, vòng đời từng đợt, điểm, quà, kết quả chờ UI.

### Added
- Core thuần C# `DreamTech.LiveOps` (`noEngineReferences`, không tham chiếu assembly nào): `LiveEventInstance`, `LiveEventPhase`,
  `LiveEventRecord`, `LiveOpsRewardBundle` / `LiveOpsRewardItem`.
- Đồng hồ: `ILiveOpsClock` (có `IsTrusted`), `SyncedLiveOpsClock` (giờ server + bộ đếm đơn điệu; mốc tin được chặn vặn giờ lùi và
  lưu qua các lần mở app; giờ chưa tin được không nâng mốc; bỏ phản hồi đồng bộ bắt đầu trước khi app vào nền),
  `SystemLiveOpsClock`, `OffsetLiveOpsClock` (cheat tua giờ), `ManualLiveOpsClock`. Nguồn giờ `IServerTimeSource` với
  `FallbackServerTimeSource`, `DelegateServerTimeSource`; `IElapsedTimeSource` với `StopwatchElapsedTimeSource`.
- Lịch: `ILiveEventCalendar`, `RecurringLiveEventCalendar` (id đợt tất định từ mốc UTC), `FixedLiveEventCalendar` (bỏ mục rỗng,
  trùng id, chồng giờ cùng loại và ghi lý do), `CompositeLiveEventCalendar` (lịch trước được ưu tiên, kết quả nhất quán theo mọi
  khung giờ hỏi).
- `LiveOpsSystem` + `LiveOpsSystemBuilder` + `LiveOpsSystemRegistry`: trạng thái từng loại event (đợt đang chạy hoặc sắp tới), tham
  gia tự động khi có điểm hoặc bấm tham gia, điều kiện vào đợt, cộng điểm chống trùng theo grant id, quà giữa đợt theo khoá,
  khép đợt đúng một lần (kể cả đợt hết giờ lúc app tắt), quà vào hàng chờ trước khi phát, kết quả chờ UI báo, dọn bản ghi quá hạn
  giữ (tính từ lúc khép) nhưng nhớ id để không vào lại, lịch thắng khi đợt chưa khép, lỗi của luật / granter báo qua event thay vì
  làm hỏng Refresh.
- Port của host: `ILiveOpsTextStore`, `ILiveOpsRewardGranter`, `ILiveEventEligibility`, `ILiveEventCompletionRule`. Adapter
  `InMemoryLiveOpsTextStore`, `DeferredLiveOpsRewardGranter`, `DelegateLiveOpsRewardGranter`, `AlwaysLiveEventEligibility`,
  `DelegateLiveEventEligibility`, `NoLiveEventCompletionReward`, `DelegateLiveEventCompletionRule`. Tham số chung `LiveOpsSettings`.
- `DreamTech.LiveOps.Unity`: `HttpDateHeaderServerTimeSource` (header `Date` + `Age` + nửa thời gian khứ hồi, không cần dịch vụ
  thời gian bên thứ ba), `JsonLiveEventCalendarParser` (lịch từ remote config, không ném exception với dữ liệu hỏng),
  `PlayerPrefsLiveOpsTextStore`, `LiveOpsUnityRunner` (Refresh định kỳ theo giờ thật, vào nền → Invalidate, quay lại → đồng bộ lại,
  thử lại khi chưa có mạng), `LiveOpsDebugPanel` (bảng thử IMGUI dùng được cả trong game thật).
- 47 test EditMode (lịch, đồng hồ, hệ thống, adapter Unity).

### Notes
- Đã kiểm: Unity 6000.6.0f1 — 47/47 EditMode + 5/5 PlayMode (demo trong dev project); Unity 2022.3.62f2 — 47/47 EditMode (project tạm
  trỏ `file:` vào package).
- Chưa có backend (định danh người chơi, cloud save, điểm server-authoritative) và chưa có module event có mốc — xem backlog trong
  `Documentation/DESIGN_NOTES.md`.
