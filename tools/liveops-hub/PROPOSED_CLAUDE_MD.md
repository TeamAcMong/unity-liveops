# Đề xuất sửa CLAUDE.md (gốc repo + package) cho LiveOps Hub

Trạng thái: **ĐÃ ÁP** — G-DOCS (đợt W5) đã ghi tám khối dưới đây vào `CLAUDE.md` gốc và
`Packages/com.dreamtech.liveops/CLAUDE.md` (Q-10 = user cho sửa). File này giữ lại làm bản gốc của đề xuất và làm chỗ ghi
những chỗ bản áp **lệch** so với đề xuất W0 — xem mục "Lệch khi áp" ở cuối. Mỗi khối ghi rõ: file đích, vị trí, hành động
(thay / thêm).

Vì sao cần sửa: CLAUDE.md hiện chỉ biết package 0.1.0 (core + Unity, không Editor). Hub P1 thêm assembly Editor, test UI cần
GPU, công cụ compile/lint/slot/chụp ảnh; không ghi vào CLAUDE.md thì phiên sau lặp lại lỗi mà công cụ đã chặn (chạy test UI
với `-nographics`, gọi Unity trực tiếp làm quá tải máy, viết `Editor` trần trong namespace `DreamTech.LiveOps.Editor`...).

---

## Khối 1 — `CLAUDE.md` gốc · bảng "Bố cục" · THÊM hai dòng

```markdown
| `Packages/com.dreamtech.liveops/Editor/` | LiveOps Hub (assembly `DreamTech.LiveOps.Editor`, chỉ Editor) | ✅ |
| `tools/liveops-hub/` | Công cụ dev: compile-check, lint, slot Unity, test, chụp ảnh — xem `tools/liveops-hub/README.md` | ❌ |
```

## Khối 2 — `CLAUDE.md` gốc · mục "Luật riêng của repo" · THÊM cuối mục

```markdown
- **Không gọi Unity trực tiếp.** Mọi lượt Unity batchmode đi qua `tools/liveops-hub/unity-slot.sh` (hoặc `run-editmode.sh`,
  `run-playmode.sh`, `capture.sh`): tối đa 2 Unity đồng thời trên máy, mọi lệnh có hạn giờ, tự chờ khi project đang được
  Unity khác mở. Không dùng MCP Unity cho repo này.
- **Compile trước khi mở Unity:** `tools/liveops-hub/compile-check.sh` (Roslyn C# 9 với DLL của 2022.3 VÀ 6000.6; runtime
  compile không có `UNITY_EDITOR`; 6000.6 coi CS0618/CS0619 là lỗi). Gói chỉ đụng `Runtime/` dùng `--runtime-only`.
- **Lint ở mỗi thay đổi, không dồn cuối:** `code-lint.py` (biết nhánh `#if`), `uss-lint.py`, `check-class-names.py`,
  `check-meta.sh`. Ngoại lệ có chủ đích ghi ở `tools/liveops-hub/lint-allow.tsv` kèm lý do.
- **`.meta` chỉ do Unity 6000.6 sinh** (import batchmode trên đúng worktree đang sửa); lượt chạy 2022.3 không được làm đổi
  `.meta` nào — `check-meta.sh` bắt dòng `M`, gặp thì `git checkout -- '<file>.meta'` và ghi lại.
- **Test UI của hub cần GPU:** category `LiveOpsHub.UI` chạy **không** `-nographics` (2022.3 với `-nographics` cho layout NaN,
  không vẽ, không nhận phím); category `LiveOpsHub.Logic` và test core/Unity chạy được `-nographics`.
- **Giao diện hub nghiệm thu bằng ảnh cửa sổ thật:** `capture.sh` (hai skin × hai bản Unity) + `measure-capture.py` (đo trên
  PNG gốc) + `contact-sheet.py`. Không kết luận lệch/đạt bằng mắt trên ảnh thu nhỏ.
```

## Khối 3 — `CLAUDE.md` gốc · mục "Ma trận test trước khi phát hành" · THAY toàn bộ khối lệnh

```bash
T=tools/liveops-hub
$T/compile-check.sh                                          # bốn phần × hai bản DLL
python3 $T/code-lint.py && python3 $T/uss-lint.py && python3 $T/check-class-names.py --strict
$T/check-interim.sh --none                                   # không còn nhánh tạm INTERIM

# Unity 6 — dev project
$T/unity-slot.sh --timeout 1800 -- <Unity 6000.6> -batchmode -nographics -projectPath . \
  -executeMethod DreamTech.LiveOps.Demo.EditorTools.LiveOpsDemoSceneBuilder.BuildFromCommandLine -quit
$T/run-editmode.sh --unity 6000 --category all              # gồm LiveOpsHub.UI → không -nographics
$T/run-playmode.sh

# Unity 2022.3 — project tạm (manifest: file: package + test-framework 1.1.33 + imgui/jsonserialize/unitywebrequest/uielements)
$T/make-temp-project-2022.sh --bootstrap
$T/run-editmode.sh --unity 2022 --category all
$T/check-meta.sh                                             # lượt 2022.3 không làm đổi .meta

# Giao diện hub — hai skin × hai bản
$T/capture.sh --unity all --scenarios registered --label release
python3 $T/contact-sheet.py ~/.cache/unity-liveops/captures/release
```

Kèm câu dưới khối lệnh: "Đảm bảo đúng hai bản đã chạy: **2022.3.62f2** và **6000.6.0f1**. Nhánh `#if UNITY_2023_2_OR_NEWER`
/ `UNITY_6000_0_OR_NEWER` chỉ được kiểm trên 6000.6 — 6000.0–6000.5 ghi là chưa kiểm (PD-34)."

## Khối 4 — `CLAUDE.md` gốc · mục "Luật riêng của repo", dòng "Commit" · THAY

```markdown
- **Commit:** conventional commits `type(scope): mô tả` tiếng Việt (scope: `liveops`, `demo`, `docs`, `release`, `tools`).
```

---

## Khối 5 — `Packages/com.dreamtech.liveops/CLAUDE.md` · mục "Kiến trúc và bất biến" · THAY đoạn đầu

```markdown
Phụ thuộc một chiều: `DreamTech.LiveOps` (core) ← `DreamTech.LiveOps.Unity` ← `DreamTech.LiveOps.Editor` (LiveOps Hub, chỉ
Editor) và ← demo. Core là `noEngineReferences` và **không tham chiếu gì** — không `using UnityEngine`, không UniTask, không
Newtonsoft, không SDK nào. Package chỉ phụ thuộc module có sẵn của Unity (`imgui`, `jsonserialize`, `unitywebrequest`,
`uielements`). Package không biết game nào hay kit nào: không tham chiếu asmdef của game, không `Resources.Load` đường dẫn
của game, không nhắc tên hệ thống của game trong code.
```

## Khối 6 — `Packages/com.dreamtech.liveops/CLAUDE.md` · THÊM mục mới sau "Unity layer"

```markdown
**LiveOps Hub (`Editor/`, namespace `DreamTech.LiveOps.Editor`):**
- **Không bao giờ viết `Editor` trần** trong namespace này — trùng tên namespace. Kiểu của Unity viết đủ `UnityEditor.Editor`,
  `UnityEditor.PopupWindow` (`PopupWindow` trần còn mơ hồ với `UnityEngine.UIElements`, CS0104 ở cả hai bản).
- **Hub đọc lịch bằng đúng đường game đọc:** JSON xuất ra được đọc lại bằng parser của `DreamTech.LiveOps.Unity`; section
  không gọi thẳng `JsonLiveEventCalendarParser.Parse` mà đi qua port đọc lại của services.
- **Mọi tác dụng phụ của Editor đi qua port** (clipboard, hộp chọn file, hộp xác nhận modal, danh tính người đăng, đồng hồ,
  múi giờ máy, trạng thái biên dịch) với adapter `Editor…` / `InMemory…` / `Manual…` / `Scripted…`; test không bao giờ gọi
  `ShowModalUtility`, `SaveFilePanel`, `DisplayDialog`.
- **Mức xác nhận chỉ có một nguồn:** `LiveOpsConfirmationPolicy`; view không tự quyết khi nào hỏi.
- **Section nói chuyện với cửa sổ qua bus/điều hướng** (`LiveOpsHubSectionBus`, `LiveOpsHubNavigation`), không giữ tham chiếu
  cửa sổ; mỗi màn có **model thuần** test được không cần GPU.
- **Style inline chỉ ở 10 chỗ** của thiết kế (hình học suy từ dữ liệu, class skin/breakpoint, lớp nổi, rich text…); mỗi dòng
  mang `// style-inline-allowed: <số chỗ>`. Màu, viền, font, padding, trạng thái luôn bằng class qua `EnableInClassList`;
  mọi class là hằng trong `LiveOpsHubClassNames*.cs`; mọi màu đi qua token trong `liveops-hub-theme.uss`.
- **Chữ UI chỉ ở `LiveOpsHubStrings.<Vùng>.cs`** (partial chia vùng); UXML không chứa chữ tiếng Việt.
- **Custom control đăng ký hai đường:** `#if UNITY_2023_2_OR_NEWER` `[UxmlElement] partial` + `[UxmlAttribute]`, `#else`
  `UxmlFactory` + `UxmlTraits` — mỗi nhánh lỗi compile ở bản kia. Tương tự `sortingMode` (6000+) / `sortingEnabled`,
  `focusController.IgnoreEvent` (2023.2+) / `PreventDefault`.
- **API cấm (đã chứng minh hỏng ở một bản):** `GetInstanceID`, `EditorUtility.IsDirty(int)`, `AssetDatabase.ScheduleRefresh`,
  `TabView`, `ToggleButtonGroup`, `textEdition.placeholder`, icon `winbtn_win_close`; hub không dùng instance id dạng `int`.
- **Nhánh tạm có chủ đích** mang `// INTERIM(<gói gỡ>): <mô tả>` và hằng tên `Interim…`; không nhánh tạm nào tới bản phát
  hành (`tools/liveops-hub/check-interim.sh --none`).
```

## Khối 7 — `Packages/com.dreamtech.liveops/CLAUDE.md` · mục "Phát triển và phát hành", gạch "Trước khi phát hành" · THAY

```markdown
- **Trước khi phát hành:** chạy ma trận ở `CLAUDE.md` gốc repo (compile-check hai bản, lint, EditMode Logic + UI và PlayMode
  trên Unity 6, EditMode trên project 2022.3 trỏ `file:` vào package, chụp ảnh hub hai skin × hai bản). Pass hết mới bump
  version. Test UI của hub (`LiveOpsHub.UI`) chạy không `-nographics`.
```

## Khối 8 — `Packages/com.dreamtech.liveops/CLAUDE.md` · mục "Quy ước code" · THÊM gạch cuối

```markdown
- Không LINQ (`using System.Linq`) trong package; `var` chỉ khi vế phải là `new` (kiểu hiện ra ngay); không field `m_X`;
  so chuỗi luôn có `StringComparison`; đọc/ghi số-ngày luôn `CultureInfo.InvariantCulture`. `tools/liveops-hub/code-lint.py`
  kiểm các luật này cùng danh sách tên viết tắt cấm (`evt`, `ctx`, `idx`, `e`, `x`…).
```

---

## Lệch khi áp (G-DOCS, đợt W5)

Đề xuất viết ở W0, khi hub mới là kế hoạch. Bản đã ghi vào `CLAUDE.md` giữ nguyên văn tám khối, chỉ thêm những gì W1–W5
làm đổi thật:

| Chỗ | Bản áp có thêm | Vì sao |
|---|---|---|
| Khối 2 (`CLAUDE.md` gốc, luật repo) | ngoại lệ `.cs.meta` dạng ngắn bị lượt 2022.3 bổ sung khối `MonoImporter` — commit bản dạng dài | đo được ở cổng W0 (PD-27); không ghi thì phiên sau `git checkout` nhầm |
| Khối 2 | một gạch mới về chữ ký đóng băng `contract-freeze-W3.md` + phiếu `contract-changes-<gói>.md` | PD-35 chốt sau khi đề xuất này viết xong |
| Khối 3 (ma trận test) | câu PD-34 ghi luôn "ở README và CHANGELOG" | G-DOCS là nơi thực sự ghi hai file đó |
| Khối 6 (luật hub) | mục **"Lịch, JSON và bộ kiểm — bất biến"** đứng trước phần hub | asset lịch, thứ tự xuất, bộ ghi tất định, id 12 luật là bất biến của **package**, không riêng hub — để lẫn trong phần hub thì gói chỉ sửa `Runtime/` sẽ không đọc |
| Khối 6, gạch "Chữ UI" | đổi thành: khoá ở `LiveOpsHubStrings.<Vùng>.cs`, câu thật ở `Language/LiveOpsHubStringCatalog.<Vùng>.cs`, **vi + en, mặc định English** | PD-45 (G-I18N, đợt W3.5) đổi hằng thành property sau khi đề xuất này viết xong |
| Khối 6 | thêm gạch "lý do bị khoá in thành CHỮ, tooltip chỉ phụ" | SPIKE-B SP-3; W5 làm cho cả nút, ô và tab |
| Mục "Khi đổi thiết kế" (`PKG/CLAUDE.md`) | thêm đoạn: README có khối ```` ```csharp ```` bị `ReadmeSnippetCompileTests` chép nguyên văn và chạy | không ghi thì phiên sau sửa README xong không hiểu vì sao test đỏ |

Không khối nào bị bỏ. Không có đổi chữ ký hay đổi hành vi nào kèm theo — G-DOCS chỉ viết tài liệu.
