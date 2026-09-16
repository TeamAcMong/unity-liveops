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
- **Trước khi phát hành:** chạy ma trận ở `CLAUDE.md` gốc repo (compile-check hai bản, lint, EditMode Logic + UI và PlayMode
  trên Unity 6, EditMode trên project 2022.3 trỏ `file:` vào package, chụp ảnh hub hai skin × hai bản). Pass hết mới bump
  version. Test UI của hub (`LiveOpsHub.UI`) chạy không `-nographics`.
- **Phát hành:** bump `package.json` → cập nhật `CHANGELOG.md` của package + gốc repo → commit + push `main` →
  `./deploy.sh --semver X.Y.Z`. Tag là bất biến: không xoá, không dùng lại số.

## Kiến trúc và bất biến

Phụ thuộc một chiều: `DreamTech.LiveOps` (core) ← `DreamTech.LiveOps.Unity` ← `DreamTech.LiveOps.Editor` (LiveOps Hub, chỉ
Editor) và ← demo. Core là `noEngineReferences` và **không tham chiếu gì** — không `using UnityEngine`, không UniTask, không
Newtonsoft, không SDK nào. Package chỉ phụ thuộc module có sẵn của Unity (`imgui`, `jsonserialize`, `unitywebrequest`,
`uielements`). Package không biết game nào hay kit nào: không tham chiếu asmdef của game, không `Resources.Load` đường dẫn
của game, không nhắc tên hệ thống của game trong code.

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
bỏ mục và ghi lý do. `LiveEventCalendarAsset` cũng vậy: YAML sửa tay hoặc xung đột merge không được làm `Build()` của game ném —
field null coi như rỗng, mục sai quy tắc bị bỏ, chuỗi giờ hỏng giữ nguyên văn để bộ biên dịch báo.

**Lịch, JSON và bộ kiểm — bất biến:**
- **Asset chỉ là nơi lưu.** Mọi phán "mục này có hợp lệ không" nằm ở bộ biên dịch trong core, để "game đọc asset", "game đọc
  JSON đã xuất" và "hub kiểm lịch" không bao giờ tự đoán luật riêng.
- **Thứ tự xuất là một hàm core** (`LiveEventCalendarExportOrder`): khử trùng id của runtime phụ thuộc thứ tự nhận, nên hub và
  game phải nhận cùng thứ tự. JSON đọc vào thì biên dịch đúng thứ tự nhận, không sắp lại.
- **Bộ ghi JSON tất định đến từng byte** (UTF-8 không BOM, LF, không LF cuối, thụt 2, thứ tự field cố định,
  `InvariantCulture`): dấu "đã đăng" là SHA-256 của đúng chuỗi đó. Giờ đọc được thì chuẩn hoá, **không bao giờ cắt** phần lẻ
  giây; giờ không đọc được ghi nguyên văn.
- **Đường chạy của game không gọi SHA-256** (chỉ hub gọi) — `code-lint.py` chặn `Runtime/` gọi `LiveEventCalendarSha256`.
- **`Parse(string)` và mọi property 0.1.0 giữ nguyên chữ ký và nguyên chuỗi `Problems`** (golden test khoá). Tính năng mới đi
  bằng thành viên mới: `ParseDocument`, `ParseOrDefault`, `CombinedCalendar`, `FormatVersion`.
- **Id của 12 luật (`LiveEventCalendarRuleIds`) là dữ liệu của người dùng** — ghi chú "Bỏ qua" lưu theo id trong asset. Đổi
  chuỗi id sau khi phát hành = mọi cảnh báo đã bỏ qua mất hiệu lực. Id chỉ viết ở đúng file đó, và có anchor trong README.

**LiveOps Hub (`Editor/`, namespace `DreamTech.LiveOps.Editor`):**
- **Không bao giờ viết `Editor` trần** trong namespace này — trùng tên namespace. Kiểu của Unity viết đủ `UnityEditor.Editor`,
  `UnityEditor.PopupWindow` (`PopupWindow` trần còn mơ hồ với `UnityEngine.UIElements`, CS0104 ở cả hai bản).
- **Hub đọc lịch bằng đúng đường game đọc:** JSON xuất ra được đọc lại bằng parser của `DreamTech.LiveOps.Unity`; section
  không gọi thẳng `JsonLiveEventCalendarParser.Parse` mà đi qua port đọc lại của services.
- **Mọi tác dụng phụ của Editor đi qua port** (clipboard, hộp chọn file, hộp xác nhận modal, danh tính người đăng, đồng hồ,
  múi giờ máy, trạng thái biên dịch) với adapter `Editor…` / `InMemory…` / `Manual…` / `Scripted…`; test không bao giờ gọi
  `ShowModalUtility`, `SaveFilePanel`, `DisplayDialog`.
- **Mức xác nhận chỉ có một nguồn:** `LiveOpsConfirmationPolicy`; view không tự quyết khi nào hỏi.
- **Lý do một nút / ô / tab bị khoá luôn in thành CHỮ** cạnh nó; tooltip chỉ là phụ và không test nào assert tooltip.
- **Section nói chuyện với cửa sổ qua bus/điều hướng** (`LiveOpsHubSectionBus`, `LiveOpsHubNavigation`), không giữ tham chiếu
  cửa sổ; mỗi màn có **model thuần** test được không cần GPU.
- **Style inline chỉ ở 10 chỗ** của thiết kế (hình học suy từ dữ liệu, class skin/breakpoint, lớp nổi, rich text…); mỗi dòng
  mang `// style-inline-allowed: <số chỗ>`. Màu, viền, font, padding, trạng thái luôn bằng class qua `EnableInClassList`;
  mọi class là hằng trong `LiveOpsHubClassNames*.cs`; mọi màu đi qua token trong `liveops-hub-theme.uss`.
- **Chữ UI đi qua catalog hai ngôn ngữ.** Khoá và điểm gọi là property của `LiveOpsHubStrings.<Vùng>.cs` (partial chia vùng);
  câu thật nằm ở `Editor/Hub/Foundation/Language/LiveOpsHubStringCatalog.<Vùng>.cs` với **cả Tiếng Việt và English**, mặc
  định **English**. Không chuỗi hiển thị nào viết thẳng trong code hay UXML (lint chặn).
- **Custom control đăng ký hai đường:** `#if UNITY_2023_2_OR_NEWER` `[UxmlElement] partial` + `[UxmlAttribute]`, `#else`
  `UxmlFactory` + `UxmlTraits` — mỗi nhánh lỗi compile ở bản kia. Tương tự `sortingMode` (6000+) / `sortingEnabled`,
  `focusController.IgnoreEvent` (2023.2+) / `PreventDefault`.
- **API cấm (đã chứng minh hỏng ở một bản):** `GetInstanceID`, `EditorUtility.IsDirty(int)`, `AssetDatabase.ScheduleRefresh`,
  `TabView`, `ToggleButtonGroup`, `textEdition.placeholder`, icon `winbtn_win_close`; hub không dùng instance id dạng `int`.
- **Nhánh tạm có chủ đích** mang `// INTERIM(<gói gỡ>): <mô tả>` và hằng tên `Interim…`; không nhánh tạm nào tới bản phát
  hành (`tools/liveops-hub/check-interim.sh --none`).

## Quy ước code

- Theo khuôn của `com.dreamtech.leaderboard`: một namespace cho mỗi assembly, `sealed` mặc định, builder `With...` cho composition
  root, adapter `Delegate...` / `InMemory...` / `Manual...` cho test và lắp nhanh, debug panel IMGUI thay cho UI sản phẩm.
- Không viết tắt tên biến/hàm. Comment và thông điệp lỗi bằng tiếng Việt, giải thích VÌ SAO.
- Số cấu hình là hằng có tên hoặc tham số của `LiveOpsSettings` — không số trần trong logic.
- Thêm hành vi = thêm test EditMode trước; hành vi cần vòng đời Unity (runner, panel) thì thêm test PlayMode trong `Assets/Demo/Tests`.
- Không LINQ (`using System.Linq`) trong package; `var` chỉ khi vế phải là `new` (kiểu hiện ra ngay); không field `m_X`;
  so chuỗi luôn có `StringComparison`; đọc/ghi số-ngày luôn `CultureInfo.InvariantCulture`. `tools/liveops-hub/code-lint.py`
  kiểm các luật này cùng danh sách tên viết tắt cấm (`evt`, `ctx`, `idx`, `e`, `x`…).

## Khi đổi thiết kế

Cập nhật `README.md` (cách dùng), `Documentation/DESIGN_NOTES.md` (vì sao) và `CHANGELOG.md` (cả bản ở gốc repo) trong cùng commit.

`README.md` không phải văn xuôi tự do: `ReadmeSnippetCompileTests` chép nguyên văn từng khối ```` ```csharp ```` của nó vào
assembly test và **chạy** — sửa đoạn code trong README mà không sửa test là test đỏ, và ngược lại. Test cũng đòi mọi id trong
`LiveEventCalendarRuleIds.All` có anchor trong README, và hai bản `CHANGELOG.md` giống hệt nhau.
