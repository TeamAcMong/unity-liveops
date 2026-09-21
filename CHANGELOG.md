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
- `JsonLiveEventCalendarParser.ParseOrDefault(json, asset[, policy])`: remote trống → lịch trong asset; remote **không
  dùng được** → theo `LiveEventCalendarRemoteFailurePolicy`. "Không dùng được" gồm hai ca: `JsonUtility` không đọc nổi,
  và JSON đúng cú pháp nhưng **không có mảng lịch nào** (`{}`, gõ sai tên mảng) — hai ca cùng hậu quả "không đợt nào
  chạy" nên xử như nhau, mỗi ca một câu `Problem` riêng. Mặc định `UseDefaultCalendar` (lịch trong asset + `Problem`);
  chọn tay `KeepRemoteResult` để được đúng cách của `Parse` (lịch rỗng + `Problem`). `"events": []` — mảng có mặt mà
  rỗng — là lịch rỗng **có chủ ý**, luôn dùng kết quả remote.

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
- **`LiveOpsIdentifierLimits` (core): giới hạn độ dài của dữ liệu lịch, dạng hằng có tài liệu.** Trước bản này lõi chỉ
  kiểm KÝ TỰ của định danh (`ValidateIdentifier`: không rỗng, không `#`, không xuống dòng) mà không có giới hạn ĐỘ DÀI
  nào, nên "id dài tới đâu thì hub vẫn vẽ được" là câu không ai trả lời được bằng số. Nay: `MaxIdentifierLength = 64`
  (id đợt, id loại, khoá mục, config key, khoá nhận quà, `systemId`), `MaxRecurringIdPrefixLength = 44` (suy ra:
  64 − 20, chừa chỗ cho số thứ tự lần lặp), `MaxOccurrenceIndexLength = 20`, `MaxDisplayNameLength = 64`,
  `MaxNoteLength = 160`, `MaxAssetNameLength = 64`, `MaxGrantIdLength = 143`. Xem README mục 4.5 cho cách suy ra từng
  con số. **Không có hành vi nào đổi:** vượt giới hạn vẫn không ném, không mục nào bị bỏ, 12 luật Kiểm lịch giữ nguyên —
  đây là hợp đồng soạn thảo và là mức mà bộ dữ liệu "xấu nhất" của cổng bố cục dựng theo.

- **Mặc định của `ParseOrDefault` khi remote không dùng được (Q-9) — KHÔNG phải đổi hành vi so với 0.1.0.** Bản 0.1.0
  không có `ParseOrDefault` (chỉ có `Parse`, và `Parse` giữ nguyên chữ ký lẫn từng chuỗi `Problems`), nên game bump
  `0.1.0` → `0.2.0` không bị đổi gì. Dòng này là để đối chiếu với **các bản dựng trước của nhánh 0.2.0**: hằng
  `JsonLiveEventCalendarParser.DefaultRemoteFailurePolicy` đổi từ `KeepRemoteResult` sang `UseDefaultCalendar`, và
  điều kiện "không dùng được" mở rộng từ "`JsonUtility` ném" sang "`JsonUtility` ném **hoặc** JSON không có mảng lịch
  nào". Lý do: một bản remote hỏng — kể cả `{}` — không được làm cả game không đợt nào chạy. Vẫn luôn để lại `Problem`
  ("JSON remote hỏng, dùng lịch mặc định trong asset: …" hoặc "JSON remote không có mảng lịch nào, dùng lịch mặc định
  trong asset.") — không nuốt lỗi. Muốn đúng cách của `Parse`: overload ba tham số với
  `LiveEventCalendarRemoteFailurePolicy.KeepRemoteResult`. Ba trường hợp còn lại **không đổi**: JSON trống vẫn dùng
  asset (không thêm `Problem`), `"events": []` vẫn là lịch rỗng có chủ ý của remote, JSON có mảng lịch mà vài mục hỏng
  vẫn luôn dùng kết quả remote (không bao giờ trộn hai nguồn).
- `package.json` thêm module `com.unity.modules.uielements` (hub dùng UI Toolkit).
- `README.md`: thêm asset lịch, định dạng 2, `ParseOrDefault` + bảng hai chính sách và hai ca "không dùng được" (mục 4.4),
  `CombinedCalendar`, `WithEventTypesFrom`, cách mở hub, bảng 12 luật có anchor `#<rule-id>`, thứ tự nâng cấp
  "game 0.2.0 trước, JSON định dạng 2 sau".
- `Documentation/DESIGN_NOTES.md`: thêm 3.10–3.13 (vì sao của asset, định dạng 2, cách xử remote hỏng/về muộn, hub) và
  đổi §5 thành lộ trình D6.

### Notes
- **Luật rút gọn có điều kiện nhận LOẠI Ô THỨ BA (J2-03, USER chốt 21/9/2026): nhãn thanh đợt trên trục.** Bề rộng một
  thanh bằng khoảng thời gian của đợt nhân tỉ lệ zoom, nên không có bề rộng cửa sổ nào làm id vừa nhãn. Chỗ này trước
  đây được tha bằng LỜI KHAI trong `UxLayoutAllowList`; nay nó chuyển sang `UxTruncationExemption`, nơi mỗi lượt kiểm
  ĐO lại hai điều kiện — (a) thanh đang mang tooltip nói đúng id của chính nó, (b) dòng id trên tiêu đề inspector giữ
  chuỗi đầy đủ và đang hiện. Gỡ tooltip của thanh là miễn trừ biến mất và phát hiện đỏ lại ngay lượt sau.
  **Phạm vi khai rõ, không rộng hơn mục cũ:** mục này chỉ tha loại `textCut`. Khoảng dư mỏng (`textTight`, luật W9-25)
  của nhãn thanh vẫn phải đi qua điều kiện (a) như mọi chỗ khác, nên 4 chỗ `textTight` thật của hai màn Lịch **vẫn đỏ và
  vẫn nằm trong nợ W11-01/W11-06** — không bị nuốt mất. Điều kiện (a) cũng đo được trên ĐÚNG hình dạng mà sản phẩm dùng:
  `LiveOpsTimelineGeometry.ShortenedIdentifier` ưu tiên giữ HẬU TỐ ("…mega-final-round-2"), và phép so riêng cho hình
  dạng ấy đòi tooltip có một TỪ kết thúc đúng bằng mẩu còn lại — một tooltip nói về đợt KHÁC là đỏ.
- **Đã kiểm: Unity 2022.3.62f2 và Unity 6000.6.0f1** — EditMode ở cả hai bản (số test khớp nhau), PlayMode trên dev
  project Unity 6, và ảnh cửa sổ hub chụp hai skin × hai bản. **6000.0 – 6000.5: chưa kiểm** — nhánh
  `#if UNITY_2023_2_OR_NEWER` / `UNITY_6000_0_OR_NEWER` mới chỉ chạy trên 6000.6.
- Test giao diện của hub (category `LiveOpsHub.UI`) phải chạy **không** `-nographics`; 2022.3 với `-nographics` cho
  layout NaN. Category `LiveOpsHub.Logic` và test core/Unity chạy được `-nographics`.
- Số test và bộ ảnh của bản phát hành được chốt ở mục kiểm chứng khi bump version.
- Đường chạy của game hiện không gọi SHA-256 (chỉ hub gọi). **Chưa dựng build IL2CPP để kiểm** — kết luận
  "strip không ảnh hưởng game" chờ bản demo IL2CPP của R-28.

### Chưa có ở bản này
- **Nợ bố cục trên dữ liệu dài — 12 màn, 739 chỗ người dùng không dùng được (đo 21/9/2026, Unity 6000.6.0f1).** USER chốt
  ngày 21/9/2026: sửa lỗi mắt thấy rồi phát hành, phần bố cục này để đợt W11. Không ngưỡng nào bị nới, không câu assert
  nào đổi, và danh sách tha SUÔNG còn ngắn đi một dòng — 12 ca vẫn là 12 ca, chỉ chuyển sang trạng thái HOÃN CÓ TÊN trong
  `UxLayoutDeferralList`, gỡ một dòng là ca chạy lại đầy đủ ngay lượt sau.

  | Mã | Màn | Chỗ | Loại lỗi | Cỡ còn hoãn |
  |---|---|---|---|---|
  | W11-01 | `calendar-worst-data` | 178 | chữ bị cắt 98 · con tràn cha 70 · anh em chồng nhau 8 · chữ chật 2 | cả 7 |
  | W11-02 | `event-types-worst-data` | 157 | con tràn cha 79 · chữ bị cắt 78 | cả 7 |
  | W11-03 | `recurring-worst-data` | 145 | chữ bị cắt 120 · con tràn cha 25 | cả 7 |
  | W11-04 | `overview-worst-data` | 98 | chữ bị cắt 56 · con tràn cha 42 | cả 7 |
  | W11-05 | `export-worst-data` | 56 | chữ bị cắt 56 | cả 7 |
  | W11-06 | `calendar-multi-selection-worst-data` | 44 | chữ bị cắt 28 · con tràn cha 14 · chữ chật 2 | cả 7 |
  | W11-07 | `calendar-medium-drawer-worst-data` | 20 | chữ bị cắt 10 · con tràn cha 8 · anh em chồng nhau 2 | cả 7 |
  | W11-08 | `add-event-popover-worst-data` | 16 | chữ bị cắt 4 · con tràn cha 4 · anh em chồng nhau 4 · cuộn ngang 4 | cả 7 |
  | W11-09 | `calendar-inspector-fielderror` | 16 | chữ bị cắt 8 · con tràn cha 8 | 700x560 · 820x560 |
  | W11-10 | `calendar-empty` | 5 | chữ bị cắt 5 | 1024x700 |
  | W11-11 | `export-no-baseline` | 3 | chữ bị cắt 3 (chỉ bản tiếng Anh) | 1280x760 · 1440x900 · 1920x1040 |
  | W11-12 | `overview-empty` | 1 | chữ bị cắt 1 | 700x560 |

  Bốn mục **W11-09 · W11-10 · W11-11 · W11-12 KHÔNG phải nợ "dữ liệu dài"**: chúng đứng trên dữ liệu đẹp hoặc dữ liệu
  RỖNG, nên số của chúng không nhúc nhích khi bộ dữ liệu xấu nhất được dựng lại theo giới hạn. Bốn mục ấy còn **hoãn
  THEO CỠ chứ không hoãn cả màn** (cột cuối của bảng): chúng chỉ đỏ ở 2/7, 1/7, 3/7 và 1/7 cỡ, nên 46 cặp cỡ × ngôn ngữ
  đang SẠCH của bốn màn ấy vẫn được kiểm đầy đủ mỗi lượt. Ba trong bốn còn là **MỘT nguyên nhân** mỗi mục, đã truy được
  tới dòng: nhãn `UTC` của ô ngày giờ tràn hàng 7px (W11-09) · câu "not published" cần 120px mà ô có 115px (W11-11) ·
  phụ đề `13/9 08:47 → 20/9 08:47 UTC` cần 142px mà ô có 111px (W11-12). Cả ba sửa RẺ, nhưng chạm file sản phẩm ngoài
  phạm vi gói G-W11-LIMITS nên đợt W11 nhận trước tiên.

  Tám mục "dữ liệu dài" đổi số sau khi siết fixture: tổng **719 → 739**, do `event-types` +9, `export` +6, `recurring`
  +5. Màn Lịch và màn chọn nhiều đợt **KHÔNG đổi** (178 và 44, đúng bằng nền `G-W10-GATE`). Tăng là ĐÚNG hướng — bản
  trước đo một ngày tồi tệ NHẸ hơn dữ liệu hợp lệ thật.

  **Con số của tám mục hoãn CẢ màn là ẢNH CHỤP ngày 21/9/2026, không được đo lại mỗi lượt.** Mục hoãn cả màn dừng test
  bằng `Assert.Ignore` TRƯỚC khi quét, nên `FindingCount` chỉ còn câu "phải lớn hơn 0" gác. Bảng trên ra từ một lượt đo
  cố ý gỡ hết danh sách hoãn (bằng chứng: `plan/w11/measure-uxgate-6000.xml`). Bốn mục hoãn theo cỡ thì phần cỡ không
  hoãn vẫn tự gác mỗi lượt.
- **W11-13 — nhiều màn của hub bỏ trống phần lớn cửa sổ ở cỡ rộng (J2-05).** Các màn xếp nội dung trong một cột bề rộng
  cố định và không giãn theo chiều cao, nên cửa sổ càng rộng thì phần trống càng lớn. Đo bằng mắt trên ảnh bằng chứng:
  màn **Kiểm lịch** và màn **Xuất JSON** bỏ trống gần nửa BỀ NGANG ở 1440x900; màn **Loại event** chỉ dùng khoảng 1/4
  CHIỀU CAO ở 1280x760 — tức ngay ở cỡ THIẾT KẾ, không phải chỉ ở cỡ rộng bất thường (ảnh
  `w11-event-types-worst-data-1280x760-dark.png`). Hoãn sang đợt W11 vì sửa là đổi lưới của các màn ấy (cho cột giãn,
  chia hai cột ở bậc rộng, cho bảng ăn hết chiều cao), không phải một bản vá rẻ. **Cổng bố cục hiện KHÔNG bắt được lỗi
  này**: máy chạy cổng kẹp 1440x900 còn 1440x881, và luật `notStretched` chỉ hỏi "có giãn không", không hỏi "giãn tới
  đâu" — nên đây là nợ nhìn bằng MẮT, không có con số máy đo.
- **W11-14 — `MaxAssetNameLength` chưa được nối vào ma trận kiểm bố cục.** Sáu trong bảy giới hạn đã được
  `LiveOpsIdentifierLimitsTests` đo: bốn bằng chuỗi fixture dài ĐÚNG mức mà ma trận 12 màn vẽ ra (định danh, tiền tố
  luật lặp, tên hiển thị, ghi chú) và hai bằng phép suy (bề rộng chữ của `long.MinValue`, phép ghép grant id). Giới hạn
  thứ bảy (tên asset lịch) mới có hằng
  `LiveOpsWorstCaseSample.LongAssetName` + ca đo độ dài; chưa màn nào đứng trên một tên asset 64 ký tự, nên chip tên
  lịch của KHUNG chưa được nghiệm thu ở mức ấy. Lý do hoãn: tên asset do `LiveOpsHubTestServices.CreateMemoryAsset` đặt
  — file dùng chung cho MỌI màn của ma trận, nên đổi nó là đổi số phát hiện của cả những màn đang xanh và nằm ngoài 12
  phiếu USER đã duyệt, tức tự mở phiếu thứ 13.
- **Chưa có luật kiểm cho giới hạn độ dài.** `LiveOpsIdentifierLimits` là hằng có tài liệu; màn Kiểm lịch vẫn đúng 12
  luật và không cảnh báo khi một id vượt 64 ký tự.
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
