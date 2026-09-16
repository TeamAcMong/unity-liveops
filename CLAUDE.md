# unity-liveops — hướng dẫn cho Claude Code

Repo này là **dev project Unity 6** của một UPM package (`com.dreamtech.liveops`), không phải game. Thứ được phát hành là thư
mục `Packages/com.dreamtech.liveops/`; game cài bằng `https://github.com/TeamAcMong/unity-liveops.git#<tag>`.

**Luật của package (kiến trúc, bất biến, quy ước code) nằm ở
[`Packages/com.dreamtech.liveops/CLAUDE.md`](Packages/com.dreamtech.liveops/CLAUDE.md) — đọc file đó trước khi sửa bất cứ gì
trong package.**

## Bố cục

| Đường dẫn | Là gì | Đi theo package? |
|---|---|---|
| `Packages/com.dreamtech.liveops/` | Package | ✅ |
| `Assets/Demo/` | Scene demo, composition root mẫu, test PlayMode | ❌ |
| `deploy.sh`, `DEPLOY_UPM_SUBTREE.md` | Phát hành bằng subtree split + tag | ❌ |
| `Packages/com.dreamtech.liveops/Editor/` | LiveOps Hub (assembly `DreamTech.LiveOps.Editor`, chỉ Editor) | ✅ |
| `tools/liveops-hub/` | Công cụ dev: compile-check, lint, slot Unity, test, chụp ảnh — xem `tools/liveops-hub/README.md` | ❌ |
| `CHANGELOG.md` (gốc) | Bản sao CHANGELOG của package — sửa cả hai cùng lúc | ❌ |

## Luật riêng của repo

- **Unity:** dev project mở bằng 6000.6.0f1. Package phải chạy từ **Unity 2022.3**: code trong package không dùng API chỉ có từ
  2023.1+, C# tối đa 9. Code trong `Assets/Demo` chỉ chạy ở dev project nên được dùng API Unity 6 (vd `FindAnyObjectByType`).
- **Mọi file mới trong package phải có `.meta` được commit.** Package cài bằng git là chỉ đọc; thiếu `.meta` thì Unity của game
  bỏ qua file và GUID đổi giữa các máy. Mở Unity (hoặc chạy batchmode) một lần trước khi commit file mới.
- **Đừng mở dev project bằng Unity 6 và project 2022.3 trỏ `file:` vào package CÙNG LÚC** — cả hai cùng sinh `.meta` cho file mới
  sẽ ra hai GUID khác nhau. Chạy lần lượt.
- **Scene demo là sản phẩm sinh ra.** Sửa bố cục trong `Assets/Demo/Editor/LiveOpsDemoSceneBuilder.cs` rồi dựng lại, không sửa tay
  `LiveOpsDemo.unity`.
- **Phát hành:** chỉ sau khi ma trận test ở dưới xanh. Tag là bất biến.
- **Commit:** conventional commits `type(scope): mô tả` tiếng Việt (scope: `liveops`, `demo`, `docs`, `release`, `tools`).
- **Không gọi Unity trực tiếp.** Mọi lượt Unity batchmode đi qua `tools/liveops-hub/unity-slot.sh` (hoặc `run-editmode.sh`,
  `run-playmode.sh`, `capture.sh`): tối đa 2 Unity đồng thời trên máy, mọi lệnh có hạn giờ, tự chờ khi project đang được
  Unity khác mở. Không dùng MCP Unity cho repo này.
- **Compile trước khi mở Unity:** `tools/liveops-hub/compile-check.sh` (Roslyn C# 9 với DLL của 2022.3 VÀ 6000.6; runtime
  compile không có `UNITY_EDITOR`; 6000.6 coi CS0618/CS0619 là lỗi). Gói chỉ đụng `Runtime/` dùng `--runtime-only`.
- **Lint ở mỗi thay đổi, không dồn cuối:** `code-lint.py` (biết nhánh `#if`), `uss-lint.py`, `check-class-names.py`,
  `check-meta.sh`. Ngoại lệ có chủ đích ghi ở `tools/liveops-hub/lint-allow.tsv` kèm lý do.
- **`.meta` chỉ do Unity 6000.6 sinh** (import batchmode trên đúng worktree đang sửa); lượt chạy 2022.3 không được làm đổi
  `.meta` nào — `check-meta.sh` bắt dòng `M`, gặp thì `git checkout -- '<file>.meta'` và ghi lại. Ngoại lệ đã biết:
  lượt 2022.3 bổ sung khối `MonoImporter` mặc định vào `.cs.meta` dạng ngắn — commit bản dạng dài đó.
- **Test UI của hub cần GPU:** category `LiveOpsHub.UI` chạy **không** `-nographics` (2022.3 với `-nographics` cho layout NaN,
  không vẽ, không nhận phím); category `LiveOpsHub.Logic` và test core/Unity chạy được `-nographics`.
- **Giao diện hub nghiệm thu bằng ảnh cửa sổ thật:** `capture.sh` (hai skin × hai bản Unity) + `measure-capture.py` (đo trên
  PNG gốc) + `contact-sheet.py`. Không kết luận lệch/đạt bằng mắt trên ảnh thu nhỏ.
- **Chữ ký đóng băng của hub:** `tools/liveops-hub/contract-freeze-W3.md` (sinh bằng test, không sửa tay). Cần đổi một
  thành viên trong đó thì DỪNG và ghi phiếu `contract-changes-<gói>.md`, đừng lặng lẽ sinh lại file.

## Ma trận test trước khi phát hành

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

Đảm bảo đúng hai bản đã chạy: **2022.3.62f2** và **6000.6.0f1**. Nhánh `#if UNITY_2023_2_OR_NEWER` /
`UNITY_6000_0_OR_NEWER` chỉ được kiểm trên 6000.6 — 6000.0–6000.5 ghi là chưa kiểm, ở README và CHANGELOG.
