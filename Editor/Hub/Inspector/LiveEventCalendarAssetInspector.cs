using System;
using System.Globalization;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Inspector gọn của <see cref="LiveEventCalendarAsset"/> (mục 1.1, 10.3): ba việc và chỉ ba việc —
    /// <list type="number">
    /// <item>đưa người dùng sang hub bằng đúng asset đang chọn ("Mở trong LiveOps Hub");</item>
    /// <item>tóm tắt asset đủ để review trong Project mà không phải mở hub;</item>
    /// <item>cảnh báo khi asset hỏng: schema do bản package mới hơn lưu (mục 4.1), loại event không dùng được, mục bị bộ biên
    /// dịch bỏ, lịch sử đăng dài quá mốc.</item>
    /// </list>
    /// <para>
    /// KHÔNG dựng lại logic phiên: inspector không có <c>LiveOpsHubCalendarSession</c>, không Undo, không sửa, không chạy Kiểm
    /// lịch. Mọi câu chỉ NÊU SỐ rồi chỉ sang hub — số lấy từ chính hai đường mà game dùng
    /// (<see cref="LiveEventCalendarAsset.ToDocument"/> và <see cref="LiveEventCalendarCompiler.CompileInExportOrder"/>): cùng bộ
    /// biên dịch mà game dùng, đo TRÊN FILE — còn Kiểm lịch của hub đo trên tài liệu nháp đang sửa, nên hai bên trùng nhau khi
    /// nháp sạch và lệch nhau đúng phần chưa lưu. Đây cũng là lý do inspector không cho sửa field: mọi thao tác sửa lịch phải đi
    /// qua phiên của hub để có Undo có tên và có kiểm trước khi xuất.
    /// </para>
    /// <para>
    /// Số đếm hiện ra lấy từ <see cref="UnityEditor.Editor.serializedObject"/> — tức ĐÚNG những gì nằm trong file YAML, kể cả
    /// mục hỏng — chứ không lấy từ tài liệu đã lọc; khoảng chênh giữa hai bên chính là câu cảnh báo asset hỏng. Ngoại lệ duy
    /// nhất là dòng lịch sử đăng: nó đếm theo tài liệu, vì cùng một câu còn nêu dấu CUỐI lấy từ tài liệu — đếm một nơi rồi lấy
    /// mục ở nơi khác thì câu tự mâu thuẫn khi YAML có mục null.
    /// </para>
    /// </summary>
    [CustomEditor(typeof(LiveEventCalendarAsset))]
    internal sealed class LiveEventCalendarAssetInspector : UnityEditor.Editor
    {
        /// <summary>Quá mốc này thì gợi ý gỡ bớt bản chụp (mục 4.1) — gợi ý, KHÔNG tự xoá.</summary>
        internal const int ManyStampsThreshold = 50;

        // Tên field YAML của asset (mục 4.1) — là định dạng file của người dùng nên không bao giờ đổi tên.
        private const string EventTypesFieldName = "eventTypes";
        private const string RecurringRulesFieldName = "recurringRules";
        private const string FixedEventsFieldName = "fixedEvents";
        private const string PublishedStampsFieldName = "publishedStamps";
        private const string IgnoredWarningsFieldName = "ignoredWarnings";

        internal const string RootElementName = "calendar-asset-inspector";
        internal const string NewerSchemaHelpElementName = "calendar-asset-newer-schema";
        internal const string BrokenEventTypesHelpElementName = "calendar-asset-broken-event-types";
        internal const string DroppedEntriesHelpElementName = "calendar-asset-dropped-entries";
        internal const string SummaryElementName = "calendar-asset-summary";
        internal const string RemoteConfigKeyElementName = "calendar-asset-remote-config-key";
        internal const string PublishedElementName = "calendar-asset-published";
        internal const string IgnoredWarningsElementName = "calendar-asset-ignored-warnings";
        internal const string ManyStampsHelpElementName = "calendar-asset-many-stamps";
        internal const string OpenInHubButtonElementName = "calendar-asset-open-in-hub";
        internal const string EditHintElementName = "calendar-asset-edit-hint";

        /// <summary>
        /// Đường THẬT từ inspector sang hub. Khai thành field để test khẳng định được đúng hàm này (không phải một bản sao
        /// chép tay) là hàm nút gọi — <c>OpenWithAsset</c> là chỗ duy nhất biết cách đổi asset của cửa sổ đang mở.
        /// </summary>
        internal static readonly Func<LiveEventCalendarAsset, LiveOpsHubWindow> DefaultOpenInHub = LiveOpsHubWindow.OpenWithAsset;

        private static Func<LiveEventCalendarAsset, LiveOpsHubWindow> _openInHub = DefaultOpenInHub;

        /// <summary>
        /// Đường THẬT nghe asset đổi. Khai thành field vì hai lẽ: test khẳng định được inspector CÓ đăng ký nghe (chứ không chỉ
        /// dựng một lần rồi đứng yên), và <c>TrackSerializedObjectValue</c> chỉ chạy thật khi cây element đã gắn vào panel —
        /// nhóm Logic không có panel nên phải thay bằng một đường giả để gọi lại được hàm dựng.
        /// <c>UnityEditor.UIElements.BindingExtensions.TrackSerializedObjectValue</c> có ở cả 2022.3 lẫn 6000.6 với đúng chữ ký
        /// này ([API §11]) nên không cần nhánh <c>#if</c>.
        /// </summary>
        internal static readonly Action<VisualElement, SerializedObject, Action<SerializedObject>> DefaultTrackSerializedObject =
            BindingExtensions.TrackSerializedObjectValue;

        private static Action<VisualElement, SerializedObject, Action<SerializedObject>> _trackSerializedObject =
            DefaultTrackSerializedObject;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            root.name = RootElementName;
            // Panel của cửa sổ Inspector không thấy stylesheet nào của hub, nên inspector tự nạp sheet nhỏ của chính nó.
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(LiveOpsHubPaths.CalendarAssetInspectorUss);
            if (styleSheet != null) root.styleSheets.Add(styleSheet);
            RebuildContent(root);

            // Asset vẫn đang chọn ở Project view trong khi hub sửa / đăng / Hoàn tác trên chính nó: không nghe thì mọi con số
            // đứng yên ở lúc bấm chọn, và HelpBox "N mục bị bỏ" còn doạ sau khi người dùng đã sửa xong trong hub (và ngược lại).
            _trackSerializedObject(root, serializedObject, changedSerializedObject => RebuildContent(root));
            return root;
        }

        /// <summary>
        /// Dựng lại TOÀN BỘ nội dung inspector từ trạng thái asset lúc này. Dựng lại cả cây (thay vì vá từng dòng) vì các
        /// HelpBox cảnh báo có/không tuỳ dữ liệu — vá tay sẽ đẻ ra đủ tổ hợp thêm/bớt, còn cây này chỉ vài element.
        /// </summary>
        internal void RebuildContent(VisualElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            root.Clear();

            LiveEventCalendarAsset asset = target as LiveEventCalendarAsset;
            if (asset == null)
            {
                // Không ném: inspector hỏng không được làm cửa sổ Inspector của người dùng trắng cả trang.
                root.Add(new HelpBox(LiveOpsHubStrings.InspectorErrorAssetMissing, HelpBoxMessageType.Error));
                return;
            }

            // Hub ghi đè file trong khi asset đang chọn: không Update thì SerializedObject còn giữ bản đọc lúc dựng.
            serializedObject.UpdateIfRequiredOrScript();

            LiveEventCalendarDocument document = asset.ToDocument();
            int eventTypeCount = ArraySize(EventTypesFieldName);
            int recurringRuleCount = ArraySize(RecurringRulesFieldName);
            int fixedEventCount = ArraySize(FixedEventsFieldName);
            int publishedStampCount = ArraySize(PublishedStampsFieldName);
            int ignoredWarningCount = ArraySize(IgnoredWarningsFieldName);

            AddNewerSchemaWarning(root, asset);
            AddBrokenEventTypesWarning(root, eventTypeCount, document.EventTypes.Count);
            AddDroppedEntriesWarning(root, document);

            root.Add(Line(SummaryElementName, string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorSummaryFormat,
                Count(eventTypeCount), Count(recurringRuleCount), Count(fixedEventCount))));
            root.Add(Line(RemoteConfigKeyElementName, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.InspectorRemoteConfigKeyFormat, document.RemoteConfigKey)));
            root.Add(Line(PublishedElementName, PublishedText(document)));
            if (ignoredWarningCount > 0)
            {
                root.Add(Line(IgnoredWarningsElementName, string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.InspectorIgnoredWarningsFormat, Count(ignoredWarningCount))));
            }

            AddManyStampsHint(root, publishedStampCount);

            Button openInHubButton = new Button(RequestOpenInHub);
            openInHubButton.name = OpenInHubButtonElementName;
            openInHubButton.text = LiveOpsHubStrings.InspectorOpenInHubButton;
            openInHubButton.tooltip = LiveOpsHubStrings.InspectorOpenInHubTooltip;
            root.Add(openInHubButton);

            root.Add(Line(EditHintElementName, LiveOpsHubStrings.InspectorEditHint));
        }

        /// <summary>
        /// Test thay đường mở hub để không dựng cửa sổ thật (batchmode <c>-nographics</c> không có cửa sổ, và một cửa sổ hub
        /// mở ra sẽ mang theo cả phiên + asset đang nhớ của người chạy). <c>Dispose</c> trả lại đường thật.
        /// </summary>
        internal static IDisposable OverrideOpenInHub(Func<LiveEventCalendarAsset, LiveOpsHubWindow> openInHub)
        {
            if (openInHub == null) throw new ArgumentNullException(nameof(openInHub));
            OverrideScope<Func<LiveEventCalendarAsset, LiveOpsHubWindow>> scope =
                new OverrideScope<Func<LiveEventCalendarAsset, LiveOpsHubWindow>>(_openInHub, previous => _openInHub = previous);
            _openInHub = openInHub;
            return scope;
        }

        /// <summary>
        /// Test thay đường nghe asset đổi: <c>TrackSerializedObjectValue</c> thật chỉ gọi lại khi cây element đã gắn vào panel
        /// của một cửa sổ, mà nhóm Logic cố ý không dựng cửa sổ. Đường giả giữ lại hàm dựng lại để test gọi đúng cái mà
        /// inspector đã đăng ký. <c>Dispose</c> trả lại đường thật.
        /// </summary>
        internal static IDisposable OverrideTrackSerializedObject(
            Action<VisualElement, SerializedObject, Action<SerializedObject>> trackSerializedObject)
        {
            if (trackSerializedObject == null) throw new ArgumentNullException(nameof(trackSerializedObject));
            OverrideScope<Action<VisualElement, SerializedObject, Action<SerializedObject>>> scope =
                new OverrideScope<Action<VisualElement, SerializedObject, Action<SerializedObject>>>(_trackSerializedObject,
                    previous => _trackSerializedObject = previous);
            _trackSerializedObject = trackSerializedObject;
            return scope;
        }

        /// <summary>
        /// Hành động của nút "Mở trong LiveOps Hub" — là một hàm CÓ TÊN (không lambda) để test gọi đúng hàm mà nút gọi:
        /// <c>Clickable.SimulateSingleClick</c> là internal của Unity và <c>SendEvent</c> cần một panel thật, nên không có
        /// đường công khai nào bấm hộ một nút chưa gắn vào cửa sổ.
        /// </summary>
        internal void RequestOpenInHub()
        {
            LiveEventCalendarAsset asset = target as LiveEventCalendarAsset;
            if (asset == null) return;
            _openInHub(asset);
        }

        private static void AddNewerSchemaWarning(VisualElement root, LiveEventCalendarAsset asset)
        {
            if (asset.SchemaVersion <= LiveEventCalendarAsset.CurrentSchemaVersion) return;
            root.Add(Help(NewerSchemaHelpElementName, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.InspectorNewerSchemaFormat, SchemaVersionText(asset.SchemaVersion),
                SchemaVersionText(LiveEventCalendarAsset.CurrentSchemaVersion))));
        }

        private static void AddBrokenEventTypesWarning(VisualElement root, int eventTypeCount, int usableEventTypeCount)
        {
            int brokenEventTypeCount = eventTypeCount - usableEventTypeCount;
            if (brokenEventTypeCount <= 0) return;
            root.Add(Help(BrokenEventTypesHelpElementName, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.InspectorBrokenEventTypesFormat, Count(brokenEventTypeCount))));
        }

        private static void AddDroppedEntriesWarning(VisualElement root, LiveEventCalendarDocument document)
        {
            // Cùng bộ biên dịch ở THỨ TỰ XUẤT mà Kiểm lịch và game dùng (V-6) — gọi thẳng compiler trên tài liệu đã dựng ở trên
            // thay vì asset.Compile(), vì Compile() dựng lại tài liệu lần nữa (mỗi lần bấm vào asset là hai lượt ToDocument).
            int droppedCount = LiveEventCalendarCompiler.CompileInExportOrder(document).DroppedCount;
            if (droppedCount <= 0) return;
            root.Add(Help(DroppedEntriesHelpElementName, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.InspectorDroppedEntriesFormat, Count(droppedCount))));
        }

        private static void AddManyStampsHint(VisualElement root, int publishedStampCount)
        {
            if (publishedStampCount <= ManyStampsThreshold) return;
            root.Add(Help(ManyStampsHelpElementName, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.InspectorManyStampsFormat, Count(publishedStampCount), Count(ManyStampsThreshold)),
                HelpBoxMessageType.Info));
        }

        private static string PublishedText(LiveEventCalendarDocument document)
        {
            // Đếm theo ĐÚNG danh sách lấy "lần cuối" ra: mục null trong YAML (sửa tay / xung đột merge) không vào được tài liệu,
            // nên đếm dòng YAML rồi lấy dấu cuối của tài liệu là trộn hai nguồn — câu in ra sẽ tự mâu thuẫn.
            int publishedStampCount = document.PublishedStamps.Count;
            if (publishedStampCount <= 0) return LiveOpsHubStrings.InspectorPublishedNone;
            // Giờ đăng in NGUYÊN VĂN như trong file (PD-2): dấu do lần xuất trước ghi, inspector không parse lại nên chuỗi
            // hỏng vẫn hiện ra đúng cái người dùng phải đi sửa.
            PublishedCalendarStamp latest = document.PublishedStamps[publishedStampCount - 1];
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorPublishedFormat,
                Count(publishedStampCount), latest.PublishedUtcText);
        }

        private int ArraySize(string fieldName)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            return property == null ? 0 : property.arraySize;
        }

        /// <summary>Số đếm đi qua đúng bộ định dạng của hub để inspector và hub không viết số theo hai kiểu.</summary>
        private static string Count(int value)
        {
            return CountFormat.Integer(value);
        }

        /// <summary>
        /// Số hiệu schema KHÔNG đi qua <see cref="Count"/>: đó là bộ ngăn hàng nghìn của hub ("1.601") nên schema 1000 sẽ in ra
        /// "1.000" — số phiên bản không bao giờ được ngăn hàng nghìn.
        /// </summary>
        private static string SchemaVersionText(int schemaVersion)
        {
            return schemaVersion.ToString(CultureInfo.InvariantCulture);
        }

        private static Label Line(string elementName, string text)
        {
            Label line = new Label(text);
            line.name = elementName;
            // Xuống dòng bằng class (LiveEventCalendarAssetInspector.uss): giá trị khởi đầu của white-space là nowrap nên câu
            // dài bị cắt cụt trong cột Inspector hẹp, mà [FD §2.14] không cho C# gán style chữ inline.
            line.AddToClassList(LiveOpsHubClassNames.InspectorLine);
            return line;
        }

        private static HelpBox Help(string elementName, string text)
        {
            return Help(elementName, text, HelpBoxMessageType.Warning);
        }

        private static HelpBox Help(string elementName, string text, HelpBoxMessageType messageType)
        {
            HelpBox help = new HelpBox(text, messageType);
            help.name = elementName;
            return help;
        }

        // Inspector không hiện giờ theo máy nên lệch múi giờ không dùng tới — chỉ mượn cách viết số của hub ("1.601").
        private static readonly LiveOpsHubFormat CountFormat = new LiveOpsHubFormat(TimeSpan.Zero);

        /// <summary>Trả lại đường trước đó — lồng nhau được, như <c>LiveOpsHubLanguage.Override</c>.</summary>
        private sealed class OverrideScope<TOverride> : IDisposable
        {
            private readonly TOverride _previousOverride;
            private readonly Action<TOverride> _restore;
            private bool _isDisposed;

            internal OverrideScope(TOverride previousOverride, Action<TOverride> restore)
            {
                _previousOverride = previousOverride;
                _restore = restore;
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                _restore(_previousOverride);
            }
        }
    }
}
