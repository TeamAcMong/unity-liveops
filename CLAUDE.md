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
- **Commit:** conventional commits `type(scope): mô tả` (scope: `liveops`, `demo`, `docs`, `release`).

## Ma trận test trước khi phát hành

```bash
# Unity 6 — dev project
Unity -batchmode -nographics -projectPath . -executeMethod DreamTech.LiveOps.Demo.EditorTools.LiveOpsDemoSceneBuilder.BuildFromCommandLine
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml
Unity -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml

# Unity 2022.3 — project tạm có manifest:
#   "com.dreamtech.liveops": "file:<repo>/Packages/com.dreamtech.liveops",
#   "com.unity.test-framework": "1.1.33",
#   "com.unity.modules.imgui" / "jsonserialize" / "unitywebrequest": "1.0.0",
#   "testables": ["com.dreamtech.liveops"]
Unity-2022.3 -batchmode -nographics -projectPath <project tạm> -runTests -testPlatform EditMode -testResults editmode-2022.xml
```
