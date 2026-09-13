# DreamTech LiveOps — hướng dẫn cho Claude Code

Nền live-ops lắp ráp kiểu Lego: đồng hồ server, lịch event, vòng đời từng đợt, điểm, quà, kết quả chờ UI. Mã nguồn sống ở repo
**TeamAcMong/unity-liveops**: nhánh `main` là dev project Unity 6, package nằm ở `Packages/com.dreamtech.liveops`, game cài bằng
git URL có tag.

- Cách dùng, hợp đồng host, port, định dạng lịch JSON: `README.md`.
- Vì sao có các luật dưới đây, lỗi của hệ thống cũ và cách sửa, kết quả kiểm chứng: `Documentation/DESIGN_NOTES.md`.
- Phát hành: `DEPLOY_UPM_SUBTREE.md` + `deploy.sh` ở gốc repo.

## Phát triển và phát hành

- **Sửa trong dev repo, không sửa trong game.** Game cài bằng git URL thì package chỉ đọc. Muốn thử ngay trong game thì tạm
  trỏ manifest của game sang `file:<đường dẫn repo>/Packages/com.dreamtech.liveops`, xong trả lại git URL trước khi commit.
- **Hỗ trợ Unity 2022.3 → Unity 6.** Dev project chạy Unity 6 nên dễ lỡ dùng API mới: không dùng API chỉ có từ 2023.1+
  (`FindAnyObjectByType`, `Awaitable`...) trong `Runtime/` hay `Tests/` của package. C# tối đa 9 (không `record`, `init`,
  file-scoped namespace). Code demo trong `Assets/Demo` chỉ chạy trên dev project nên được dùng API Unity 6.
- **Trước khi phát hành:** chạy EditMode + PlayMode trên dev project (Unity 6) VÀ EditMode trên một project Unity 2022.3 trỏ
  `file:` vào package. Pass cả hai mới bump version.
- **Phát hành:** bump `package.json` → cập nhật `CHANGELOG.md` của package + gốc repo → commit + push `main` →
  `./deploy.sh --semver X.Y.Z`. Tag là bất biến: không xoá, không dùng lại số.

## Kiến trúc và bất biến

Phụ thuộc một chiều: `DreamTech.LiveOps` (core) ← `DreamTech.LiveOps.Unity` ← demo. Core là `noEngineReferences` và **không
tham chiếu gì** — không `using UnityEngine`, không UniTask, không Newtonsoft, không SDK nào. Package không biết game nào hay kit
nào: không tham chiếu asmdef của game, không `Resources.Load` đường dẫn của game, không nhắc tên hệ thống của game trong code.

**Port (chỉ thêm, không đổi chữ ký nếu không bump major):** `ILiveOpsClock`, `IServerTimeSource`, `IElapsedTimeSource`,
`ILiveEventCalendar`, `ILiveOpsTextStore`, `ILiveOpsRewardGranter`, `ILiveEventEligibility`, `ILiveEventCompletionRule`.
Enum trạng thái (`LiveEventPhase`, `LiveEventJoinStatus`, `LiveEventProgressStatus`, `LiveOpsClaimStatus`) chỉ thêm giá trị vào
cuối, không đổi số.

**Bất biến của `LiveOpsSystem` — mỗi cái có test chặn, đừng nới:**
- Dữ liệu người chơi khoá theo **id từng đợt**, không theo loại event hay theo "lúc người chơi thấy lần đầu". Đợt đã khép hoặc
  đã dọn không bao giờ vào lại, kể cả khi giờ bị vặn lùi (`RetiredEventIds`).
- Bản ghi giữ bản sao khung giờ. **Lịch thắng khi đợt chưa khép** (remote config dời giờ thì bản ghi theo); lịch xoá đợt thì bản
  ghi vẫn khép theo giờ đã lưu và vẫn phát quà.
- Luật khép đợt chạy **đúng một lần** mỗi đợt, gọi TRƯỚC khi đổi trạng thái (luật ném lỗi thì bản ghi còn nguyên, lần sau thử lại).
- Quà được ghi vào hàng chờ (và lưu) **trước** khi đưa cho granter. Grant id ổn định: `liveops.complete#<eventId>`,
  `liveops.claim#<eventId>#<claimKey>` — đổi định dạng này là game đang chống trùng theo id sẽ phát trùng.
- Lỗi trong code của game (luật, granter) **không làm hỏng Refresh**: báo qua `CompletionRuleFailed` / `RewardGrantFailed`.
- Phát quà chờ **trước** khi bắn `EventFinalized`, để UI nghe sự kiện thì ví đã có quà.

**Bất biến của `SyncedLiveOpsClock`:**
- Giờ chưa tin được **không bao giờ nâng mốc** (`TrustedHighWaterUtc`); chỉ giờ server và giờ tin được lúc `Invalidate` nâng mốc.
- Lần đồng bộ bắt đầu trước `Invalidate` bị bỏ khi phản hồi về (bộ đếm đơn điệu đứng yên lúc máy ngủ).
- Đồng hồ không tự đọc giờ máy khi đang tin được.

**Unity layer:** `UnityWebRequest` chỉ đụng trên main thread — huỷ từ thread khác phải Post lên context của Unity (xem
`HttpDateHeaderServerTimeSource.SendAsync`). Parser lịch và nguồn giờ không ném exception vì dữ liệu xấu từ xa: dữ liệu hỏng thì
bỏ mục và ghi lý do.

## Quy ước code

- Theo khuôn của `com.dreamtech.leaderboard`: một namespace cho mỗi assembly, `sealed` mặc định, builder `With...` cho composition
  root, adapter `Delegate...` / `InMemory...` / `Manual...` cho test và lắp nhanh, debug panel IMGUI thay cho UI sản phẩm.
- Không viết tắt tên biến/hàm. Comment và thông điệp lỗi bằng tiếng Việt, giải thích VÌ SAO.
- Số cấu hình là hằng có tên hoặc tham số của `LiveOpsSettings` — không số trần trong logic.
- Thêm hành vi = thêm test EditMode trước; hành vi cần vòng đời Unity (runner, panel) thì thêm test PlayMode trong `Assets/Demo/Tests`.

## Khi đổi thiết kế

Cập nhật `README.md` (cách dùng), `Documentation/DESIGN_NOTES.md` (vì sao) và `CHANGELOG.md` (cả bản ở gốc repo) trong cùng commit.
