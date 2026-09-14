# tools/liveops-hub — công cụ dev của LiveOps Hub P1

Công cụ của **dev repo**, không đi theo package (`Packages/com.dreamtech.liveops` không tham chiếu gì ở đây, nằm ngoài
`Assets`/`Packages` nên không có `.meta`). Mọi script nhận worktree bất kỳ: truyền `--repository <worktree>` (hoặc biến
`REPOSITORY`), không thì lấy `git rev-parse --show-toplevel` của thư mục hiện tại **nếu nó có package**; không có thì script
thoát 2 — **không** rơi về repo chứa script (gọi script G-TOOLS bằng đường dẫn tuyệt đối từ cwd khác từng kiểm nhầm worktree
G-TOOLS và báo xanh giả). Mọi script in `repository=<đường dẫn>` ra stderr: đọc dòng đó để chắc đang kiểm đúng worktree.
`code-lint.py` / `uss-lint.py` nhận file thì tính repo theo **chính từng file** (thư mục tổ tiên có package); file ngoài mọi
repo, hoặc khác `--repository`, là lỗi `file-outside-repository`.

Kế hoạch gốc: `IMPLEMENTATION_PLAN.md` mục 9.4–9.6 (kiểm chứng), 10.1 (luật chạy), 10.4 (quyền ghi), 12 (INTERIM).

## Luật bắt buộc

- **Không gọi `Unity` trực tiếp.** Mọi lượt Unity (import, test, probe, chụp ảnh, dựng demo) đi qua `unity-slot.sh`
  (hoặc `run-editmode.sh` / `run-playmode.sh` / `capture.sh` / `make-temp-project-2022.sh --bootstrap`, vốn gọi slot).
  Trần mặc định **2 Unity batch đồng thời** trên máy (PD-26); mọi lệnh có hạn giờ (import 1800 s, test 1500 s, chụp 900 s).
- **Một project không bao giờ mở bởi hai Unity cùng lúc** — slot tự chờ khi thấy project đang mở (kể cả Editor GUI).
- **Chỉ batchmode.** Không MCP Unity. Test UI và chụp ảnh chạy **không** `-nographics`; test Logic chạy `-nographics`.
- **Trước khi mở Unity kiểm đĩa:** các script dừng (thoát 3) khi `/System/Volumes/Data` còn dưới 5 GB.
- **`.meta` do Unity 6000.6 sinh trên worktree của gói**; lượt 2022.3 không được làm đổi `.meta` nào (`check-meta.sh`).

## Script

| Script | Việc | Ghi chú |
|---|---|---|
| `compile-check.sh [--runtime-only] [--parts …] [--unity 2022\|6000\|all]` | Roslyn C# 9 bốn phần: core (chỉ netstandard, `-warnaserror+`) · runtime (không `UNITY_EDITOR`, không DLL Editor) · editor · test; DLL của từng bản; 6000.6 bật `-warnaserror+:CS0618,CS0619` | cần DLL test cache: `make-temp-project-2022.sh --bootstrap` (2022.3) và Library đã import (6000.6). In `COMPILE CHECK OK` |
| `unity-slot.sh [--timeout S] [--label …] -- <lệnh>` | khoá `~/.cache/unity-liveops/slots/slot-{1..N}` + `projects/<băm project>` (một lệnh mỗi `-projectPath`, giữ suốt lệnh) bằng `mkdir`; khoá ghi pid wrapper **và** pid lệnh con (sống khi một trong hai sống — wrapper bị SIGKILL không làm mất khoá); dọn mồ côi bằng dấu `reaping` + `mv` nguyên tử; INT/TERM dừng lệnh con trước khi trả khoá; bọc `perl alarm` | `--status`, `--clean`, `--self-test`; `LIVEOPS_UNITY_SLOTS` 1–3; hết hạn giờ thoát 142 |
| `make-worktree.sh <gói>` | `git worktree add ../unity-liveops-wt/<gói> -b wt/<gói> feature/liveops-hub-p1` + `cp -c -R Library` | `--remove <gói>` từ chối khi còn thay đổi/commit chưa merge (trừ `--force`) |
| `make-temp-project-2022.sh [--bootstrap]` | project 2022.3 tạm `~/.cache/unity-liveops/temp-2022/<gói>` trỏ `file:` vào package của worktree | `--bootstrap` import một lần + chép `nunit.framework.dll`, `UnityEngine/UnityEditor.TestRunner.dll` vào cache |
| `run-editmode.sh --unity 6000\|2022 [--category Logic\|UI\|all] [--filter regex]` | chạy test qua slot, đọc XML, in `N passed / M failed / K skipped` | thoát ≠ 0 khi fail, thiếu XML, total = 0 (trừ `--allow-empty`), hoặc log có `error CS`. `--category-mode fixture` là dự phòng SP-15 |
| `run-playmode.sh` | PlayMode demo trên 6000.6 | dùng chung phần đọc XML của `run-editmode.sh` |
| `capture.sh --unity … --scenarios id,…\|registered --label …` | chụp `LiveOpsHubCaptureCommand.CaptureFromCommandLine` × skin × bản, rồi `measure-capture.py`; mỗi skin một lượt Unity, EditorPrefs `UserSkin` đặt trước lượt dưới khoá `slots/skin-preference/` và trả lại sau lượt/trong trap (SP-4: không đổi skin giữa phiên) | `--dry-run` in lệnh; ra `~/.cache/unity-liveops/captures/<nhãn>/<bản>/` |
| `measure-capture.py <thư mục>` | đo PNG gốc: khung (rail 196, header 26, section header 36, status 20, nội dung 1084) bằng dò cạnh ≥ 2 đường mẫu, ΔE màu điểm neo, tương phản WCAG (ngưỡng thiết kế − 0,3) | Python 3 thư viện chuẩn; `--self-test` |
| `contact-sheet.py <thư mục nhãn>` | `contact-sheet.html`: lưới 2022.3 \| 6000.6 × dark \| light, hình thiết kế + gói + đợt theo ma trận 9.5, bảng đạt/lệch, link ảnh gốc | không kết luận bằng ảnh thu nhỏ |
| `code-lint.py <file…>` | lint luật package, biết nhánh `#if` | `--self-test` chạy `lint-samples/`; `--report-only` cho file 0.1.0 cũ ở cổng đợt; ngoại lệ ở `lint-allow.tsv` |
| `uss-lint.py <file…>` | chặn `gap`, `box-shadow`, `:last-child`, `:nth-child`, `@media`, `!important`, `*`, `text-transform`, `var(--unity-icons-`, `-unity-font:`, `z-index`, grid, màu trần ngoài theme | `--self-test` |
| `check-class-names.py [--strict]` | class `liveops-hub-*` trong USS/UXML/C# = hằng `LiveOpsHubClassNames*.cs`; không trùng giá trị; class riêng màn chỉ ở file vùng của màn | `--strict` từ cổng W5 |
| `check-meta.sh [<worktree>]` | mọi file/thư mục trong package và `Assets/Demo` có `.meta`, không mồ côi, không trùng GUID, không `.meta` bị sửa | in `META CHECK OK` |
| `check-ownership.py <gói>` | file gói đã chạm (so merge-base với `feature/liveops-hub-p1`, gồm chưa commit/chưa track) ⊆ `ownership.tsv`; thư mục mới đúng chủ 10.4 | `--wave-deps`: không gói nào phụ thuộc gói cùng đợt |
| `check-interim.sh [--for <gói> \| --none]` | liệt kê `INTERIM(<gói>)` + định danh `Interim…`; `--for` đỏ khi gói gỡ còn comment của mình, còn file/định danh mà sổ mục 12 (trong script) giao gói xoá — ở bất kỳ file nào, còn file ownership.tsv ghi "<gói> xoá" (cả `.meta`), hoặc còn `Interim*` trong file gói được ghi; `--none` cho G-ACCEPT | mục 12 đổi thì sửa `INTERIM_REGISTER` trong script |

## Cổng gói (gói tự chạy trên worktree của mình, mục 10.1)

```bash
T=/Users/datho/unity-liveops-wt/G-TOOLS/tools/liveops-hub   # sau cổng W0: tools/liveops-hub của chính worktree
W=/Users/datho/unity-liveops-wt/<gói>
cd "$W"
python3 $T/check-ownership.py <gói>
python3 $T/code-lint.py <file .cs/.uxml của gói>
python3 $T/uss-lint.py <file .uss của gói>
python3 $T/check-class-names.py                  # từ W1
$T/check-interim.sh --for <gói>                   # gói gỡ INTERIM
$T/compile-check.sh [--runtime-only]
mkdir -p "$HOME/.cache/unity-liveops/logs"
$T/unity-slot.sh --timeout 1800 --label "import <gói>" -- \
  /Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath "$W" -quit -logFile "$HOME/.cache/unity-liveops/logs/<gói>-import.log"
git add <file + .meta của gói> && git commit …
$T/run-editmode.sh --unity 6000 --category Logic --filter "<regex của gói>"
$T/run-editmode.sh --unity 6000 --category UI --filter "<regex của gói>"          # gói UI
$T/make-temp-project-2022.sh --bootstrap                                          # lần đầu của gói
$T/run-editmode.sh --unity 2022 --category all --filter "<regex của gói>"
$T/check-meta.sh                                                                  # sau lượt 2022.3
$T/capture.sh --unity 6000 --scenarios "<id của gói>" --label "<gói>"            # gói UI từ W2
```

## Cổng đợt (một agent, tuần tự)

merge worktree theo thứ tự id → import 6000.6 + `check-meta.sh` → `compile-check.sh` đủ bốn phần → `code-lint.py --report-only`
cho file 0.1.0, `code-lint.py` cho file mới → `run-editmode.sh --unity 6000 --category all` → `run-editmode.sh --unity 2022
--category all` → `check-meta.sh` → probe hai bản → `run-playmode.sh` (từ W2) → `capture.sh --unity all --scenarios registered`
→ `contact-sheet.py` → sinh lại `ownership.tsv` nếu kế hoạch đổi → commit.

## `ownership.tsv`

Bản máy đọc của bảng quyền ghi, sinh từ kế hoạch (mục 2, 10.3, 10.4, V-5); dòng cách nhau bằng TAB:

| Loại | Cột | Nghĩa |
|---|---|---|
| `package` | gói · đợt · cỡ · phụ thuộc | phụ thuộc là gói, `W<n>` (toàn đợt) hoặc `-`; spike SP-n quy về G-SPIKE-A/B |
| `file` | đường dẫn từ gốc repo (glob `*`, `**`) · gói theo thứ tự tạo → sửa · ghi chú | `*` = mọi gói; `G-FIX-W*` = gói sửa của cổng đợt |
| `directory` | thư mục mới · gói chủ · đợt | bảng 10.4; thư mục không có trong bảng thì suy chủ từ file cùng đợt |
| `region` | vùng · họ=gói | tham khảo cho file `LiveOpsHubStrings/ClassNames/Paths.<Vùng>.cs`, `LiveOpsHubCaptureScenarios.<Vùng>.cs` |

## JSON số đo của lệnh chụp (hợp đồng với `LiveOpsHubCaptureCommand`, G-SHELL)

Mỗi kịch bản ghi `<id>-<skin>.png` (đã lật dọc, gốc trên-trái) và `<id>-<skin>.json`:

```json
{
  "scenario": "hs-shell-skeleton", "skin": "dark", "unityVersion": "6000.6.0f1", "pixelsPerPoint": 1,
  "window": { "width": 1280, "height": 760 },
  "elements": [
    { "name": "liveops-hub-rail", "classes": ["liveops-hub-rail"], "worldBound": { "x": 0, "y": 26, "width": 196, "height": 714 } }
  ],
  "expectedFrames": [ { "element": "liveops-hub-rail", "width": 196 } ],
  "colorAnchors": [
    { "name": "rail-blocked-text", "point": { "x": 40, "y": 100 }, "expected": "#FF8080", "maximumDeltaE": 3,
      "background": { "x": 12, "y": 100 }, "minimumContrast": 4.54 }
  ]
}
```

`expectedFrames` tuỳ chọn (không có thì dùng bảng mặc định, khớp theo `name` hoặc `classes`); `window` là kích thước thật
theo `rootVisualElement.layout` (SP-16). Toạ độ theo point; pixel = point × `pixelsPerPoint`.

## Thư mục ngoài git

| Đường dẫn | Nội dung |
|---|---|
| `~/.cache/unity-liveops/slots/` | khoá slot Unity (dùng chung mọi gói + G-SPIKE-A) |
| `~/.cache/unity-liveops/temp-2022/<gói>/` | project 2022.3 tạm — xoá `Library/` và `Temp/` khi gói xong |
| `~/.cache/unity-liveops/test-references/<bản>/` | DLL test cho compile-check (worktree đã có `tools/` giữ thêm bản ở `tools/liveops-hub/.cache/`, bị ignore) |
| `~/.cache/unity-liveops/compile/<tên worktree>/<bản>/` | DLL + file `.rsp`/`.log` của compile-check (ngoài worktree để không bẩn git của gói khác) |
| `~/.cache/unity-liveops/test-results/<gói>/` | XML + log của run-editmode/run-playmode |
| `~/.cache/unity-liveops/captures/<nhãn>/<bản>/` | ảnh chụp + số đo + contact sheet |
| `~/.cache/unity-liveops/review/<đợt>/` | báo cáo cổng user duyệt ảnh |
