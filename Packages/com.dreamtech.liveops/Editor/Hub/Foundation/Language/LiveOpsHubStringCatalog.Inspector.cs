namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Inspector — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.Inspector.cs</c>, không đổi một
    /// ký tự), bản tiếng Anh dịch ngay tại đây. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để chỗ giữ chỗ <c>{0}</c>/<c>{1}</c>
    /// soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.Inspector.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterInspector(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.InspectorSummaryFormat),
                vietnamese: "{0} loại · {1} luật lặp · {2} đợt cố định",
                english: "{0} event types · {1} recurring rules · {2} fixed events");
            table.Add(nameof(LiveOpsHubStrings.InspectorRemoteConfigKeyFormat),
                vietnamese: "Khoá remote config: {0}",
                english: "Remote config key: {0}");
            table.Add(nameof(LiveOpsHubStrings.InspectorIgnoredWarningsFormat),
                vietnamese: "{0} cảnh báo Kiểm lịch đã bỏ qua",
                english: "{0} calendar-check warnings ignored");

            table.Add(nameof(LiveOpsHubStrings.InspectorPublishedFormat),
                vietnamese: "Đã đăng {0} lần · lần cuối {1}",
                english: "Published {0} times · last one {1}");
            table.Add(nameof(LiveOpsHubStrings.InspectorPublishedNone),
                vietnamese: "Chưa đăng lần nào",
                english: "Never published");

            table.Add(nameof(LiveOpsHubStrings.InspectorNewerSchemaFormat),
                vietnamese: "Asset lịch này được lưu bởi một bản package mới hơn (schema {0}, bản đang cài hiểu {1}). Hub vẫn đọc được các field quen, nhưng field mà chỉ bản mới biết sẽ mất khi hub lưu đè.",
                english: "This calendar asset was saved by a newer package version (schema {0}; the installed version understands {1}). The hub still reads the fields it knows, but any field only the newer version knows is lost when the hub saves over it.");

            table.Add(nameof(LiveOpsHubStrings.InspectorBrokenEventTypesFormat),
                vietnamese: "{0} loại event trong asset không dùng được (id rỗng, sai ký tự hoặc trùng id) — game bỏ chúng, kèm mọi đợt thuộc chúng.",
                english: "{0} event types in this asset cannot be used (empty id, invalid characters, or a duplicate id) — the game drops them, and every event that belongs to them.");
            table.Add(nameof(LiveOpsHubStrings.InspectorDroppedEntriesFormat),
                vietnamese: "{0} mục của lịch bị bỏ khi game đọc asset này. Mở hub rồi chạy Kiểm lịch để xem từng mục và cách sửa.",
                english: "{0} calendar entries are dropped when the game reads this asset. Open the hub and run the calendar check to see each one and how to fix it.");

            table.Add(nameof(LiveOpsHubStrings.InspectorManyStampsFormat),
                vietnamese: "Lịch sử đăng có {0} bản chụp, nhiều hơn mốc {1}. Mỗi bản khoảng 2 KB nên asset phình dần — gỡ bớt bản cũ ở màn Xuất JSON của hub; hub không tự xoá bản nào.",
                english: "The publish history holds {0} snapshots, more than the {1} mark. Each one is about 2 KB, so the asset keeps growing — remove old ones on the hub's Export JSON screen; the hub never deletes any by itself.");

            table.Add(nameof(LiveOpsHubStrings.InspectorOpenInHubButton),
                vietnamese: "Mở trong LiveOps Hub",
                english: "Open in LiveOps Hub");
            table.Add(nameof(LiveOpsHubStrings.InspectorOpenInHubTooltip),
                vietnamese: "Mở hub với chính asset lịch này",
                english: "Open the hub on this calendar asset");

            table.Add(nameof(LiveOpsHubStrings.InspectorEditHint),
                vietnamese: "Inspector này chỉ tóm tắt. Sửa lịch trong hub để mỗi thao tác có Hoàn tác có tên và được kiểm trước khi xuất.",
                english: "This inspector only summarises. Edit the calendar in the hub so every action gets a named Undo and is checked before export.");

            table.Add(nameof(LiveOpsHubStrings.InspectorErrorAssetMissing),
                vietnamese: "Inspector lịch cần một LiveEventCalendarAsset.",
                english: "The calendar inspector needs a LiveEventCalendarAsset.");
        }
    }
}
