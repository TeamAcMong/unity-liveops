using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phần vùng EventTypes của <see cref="LiveOpsHubPaths"/> (V-5, G-EVENTTYPES): tên element viết tay trong
    /// <c>EventTypesSection.uxml</c>. Đường dẫn UXML/USS của màn đã có sẵn ở file gốc (G-SKELETON) nên file này chỉ khai tên
    /// element. Cả chín tên đều tồn tại ở MỌI trạng thái của màn (kể cả không asset) — probe CLI và
    /// <c>HubWindowTests.EverySection_RequiredElementsPresent</c> duyệt hai ngữ cảnh nên phần nào chưa dùng thì ẩn chứ không bị gỡ.
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        internal static class EventTypesElementNames
        {
            internal const string Body = "event-types-body";
            internal const string Empty = "event-types-empty";
            internal const string Content = "event-types-content";
            internal const string TableHost = "event-types-table-host";
            internal const string Table = "event-types-table";
            internal const string Footer = "event-types-footer";
            internal const string UnknownBar = "event-types-unknown-bar";
            internal const string ReferenceCard = "event-types-reference-card";
            internal const string Inspector = "event-types-inspector";
        }

        /// <summary>Hợp đồng <see cref="IHubSection.RequiredElementNames"/> của màn Loại event.</summary>
        internal static readonly IReadOnlyList<string> RequiredEventTypesElementNames = Array.AsReadOnly(new[]
        {
            EventTypesElementNames.Body, EventTypesElementNames.Empty, EventTypesElementNames.Content,
            EventTypesElementNames.TableHost, EventTypesElementNames.Table, EventTypesElementNames.Footer,
            EventTypesElementNames.UnknownBar, EventTypesElementNames.ReferenceCard, EventTypesElementNames.Inspector,
        });
    }
}
