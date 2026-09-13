# Changelog

Mọi thay đổi đáng kể của package ghi ở đây. Định dạng theo [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
phiên bản theo [Semantic Versioning](https://semver.org/).

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
