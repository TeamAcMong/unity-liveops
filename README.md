# DreamTech LiveOps

> Nền live-ops lắp ráp kiểu Lego cho game mobile — event theo mùa, pass, đua, săn kho báu chạy đúng giờ trên mọi máy, không mất
> quà, không phát trùng, không bị vặn giờ máy. Đồng hồ, lịch, nơi lưu, kho đồ, điều kiện tham gia và luật quà đều cắm qua port;
> core thuần C# không phụ thuộc engine, thư viện ngoài hay kit nội bộ nào.

[![Version](https://img.shields.io/badge/version-0.1.0-blue.svg)](https://github.com/TeamAcMong/unity-liveops/tags)
[![Unity](https://img.shields.io/badge/unity-2022.3%20%E2%86%92%206000-black.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## 📦 Cài đặt

**Package Manager → `+` → Add package from git URL:**

```
https://github.com/TeamAcMong/unity-liveops.git#0.1.0
```

Hoặc trong `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dreamtech.liveops": "https://github.com/TeamAcMong/unity-liveops.git#0.1.0"
  }
}
```

> **Yêu cầu:** Unity 2022.3+ (đã kiểm 2022.3.62f2 và 6000.6.0f1). Không cần package ngoài — chỉ module có sẵn của Unity.

## 🚀 Dùng

Toàn bộ hướng dẫn nằm trong package: [`Packages/com.dreamtech.liveops/README.md`](Packages/com.dreamtech.liveops/README.md)
— các port, composition root mẫu, đồng hồ và chống vặn giờ, định dạng lịch JSON, vòng đời một đợt, bẫy đã biết.

Vì sao hệ thống được thiết kế như vậy (lỗi của hệ thống cũ, từng quyết định và test chặn nó, backlog):
[`Documentation/DESIGN_NOTES.md`](Packages/com.dreamtech.liveops/Documentation/DESIGN_NOTES.md).

## 🗂 Repo này có gì

Nhánh `main` là **dev project Unity 6** để phát triển package; người dùng package chỉ tải tag (nội dung package, không kèm project).

```
Packages/com.dreamtech.liveops/   ← package (thứ được phát hành)
  Runtime/Core    DreamTech.LiveOps        C# thuần: đợt event, đồng hồ, lịch, LiveOpsSystem, port, adapter
  Runtime/Unity   DreamTech.LiveOps.Unity  giờ từ header Date, parser lịch JSON, PlayerPrefs, runner, debug panel
  Tests/Editor    47 test EditMode
Assets/Demo/                      ← scene demo + test PlayMode (không đi theo package)
  LiveOpsDemo.unity               bàn thử: một event đua lặp lại + một đợt săn kho báu từ lịch JSON
  Scripts/LiveOpsDemo.cs          composition root mẫu: đồng hồ server bọc đồng hồ tua được, lịch ghép, ví giả lập
  Editor/LiveOpsDemoSceneBuilder  Tools/DreamTech/LiveOps/Demo/Build Demo Scene
  Tests/                          5 test PlayMode chạy scene thật
deploy.sh, DEPLOY_UPM_SUBTREE.md  ← phát hành bằng git subtree split + tag
```

## 🛠 Phát triển

1. Mở repo bằng **Unity 6000.6.0f1** (hoặc Unity 6 mới hơn).
2. Mở `Assets/Demo/LiveOpsDemo.unity` → Play (xem mục dưới).
3. Test:
   ```bash
   Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml
   Unity -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml
   ```
4. Package hỗ trợ từ Unity 2022.3: trước khi phát hành, chạy thêm EditMode trên một project 2022.3 trỏ
   `"com.dreamtech.liveops": "file:<repo>/Packages/com.dreamtech.liveops"` (không mở cùng lúc với dev project).
5. Thử ngay trong game đang dùng package: tạm trỏ manifest của game sang đường dẫn `file:` ở trên, xong trả lại git URL.

Dựng lại scene demo sau khi đổi bố cục:

```bash
Unity -batchmode -nographics -projectPath . -executeMethod DreamTech.LiveOps.Demo.EditorTools.LiveOpsDemoSceneBuilder.BuildFromCommandLine
```

## 🧪 Bàn thử live-ops

`Assets/Demo/LiveOpsDemo.unity` → Play. Bảng vẽ bằng IMGUI (`LiveOpsDebugPanel` của package): mục đích là thử **luật và luồng**,
không phải giao diện.

Hai loại event với luật khác nhau:
- **sky-race** — lịch lặp lại mỗi 24h, chạy 20h; tự vào khi có điểm; hết đợt: đủ 100 điểm được rương vàng, có điểm được rương thường.
- **treasure-hunt** — lịch JSON (như lấy từ remote config, có một mục cố ý hỏng); phải bấm tham gia; cần level ≥ 5; nhận quà mốc
  50 điểm giữa đợt, quên nhận thì cuối đợt tự nhận.

| Nút | Thử điều gì |
|---|---|
| Tua +1h / +1 ngày, → bắt đầu, → hết đợt | Đồng hồ có độ lệch: đếm ngược, khép đợt, đợt kế tiếp |
| Đồng bộ giờ | Đồng bộ lại giờ server (header `Date`); dòng trạng thái cho biết đồng hồ đang tin được hay đang ước lượng |
| Tham gia / +10 / +100 | Tự vào so với bấm vào, điều kiện vào đợt, điểm lưu ngay |
| Nhận quà mốc (săn) | Quà giữa đợt theo khoá: nhận một lần, cuối đợt không phát lại |
| Level người chơi | Đổi giữa 0 và 5 để thấy điều kiện vào đợt |
| Xem xong | Kết quả đợt chờ UI báo — còn đó qua các lần mở app cho tới khi bấm |
| Phát quà chờ / Xoá dữ liệu | Hàng chờ quà; xoá PlayerPrefs của demo và đưa đồng hồ về giờ thật |

Trạng thái lưu bằng PlayerPrefs nên tắt Play rồi Play lại vẫn còn điểm và kết quả chờ. Chỉnh số ở Inspector của GameObject
`LiveOpsDemo`: URL lấy giờ, chu kỳ và thời gian chạy của đua, mục tiêu điểm, level mở khoá.

## 🚢 Phát hành

Xem [`DEPLOY_UPM_SUBTREE.md`](DEPLOY_UPM_SUBTREE.md). Tóm tắt: bump `package.json` → cập nhật hai CHANGELOG → commit + push
`main` → `./deploy.sh --semver X.Y.Z`.

## 📄 License

MIT — xem [LICENSE](LICENSE).
