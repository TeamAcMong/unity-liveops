using System;
using System.Globalization;
using DreamTech.LiveOps.Unity;
using UnityEditor;
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
    /// (<see cref="LiveEventCalendarAsset.ToDocument"/> và <see cref="LiveEventCalendarAsset.Compile"/>), nên inspector không
    /// bao giờ nói khác Kiểm lịch. Đây cũng là lý do inspector không cho sửa field: mọi thao tác sửa lịch phải đi qua phiên của
    /// hub để có Undo có tên và có kiểm trước khi xuất.
    /// </para>
    /// <para>
    /// Số đếm hiện ra lấy từ <see cref="UnityEditor.Editor.serializedObject"/> — tức ĐÚNG những gì nằm trong file YAML, kể cả
    /// mục hỏng — chứ không lấy từ tài liệu đã lọc; khoảng chênh giữa hai bên chính là câu cảnh báo asset hỏng.
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

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            root.name = RootElementName;

            LiveEventCalendarAsset asset = target as LiveEventCalendarAsset;
            if (asset == null)
            {
                // Không ném: inspector hỏng không được làm cửa sổ Inspector của người dùng trắng cả trang.
                root.Add(new HelpBox(LiveOpsHubStrings.InspectorErrorAssetMissing, HelpBoxMessageType.Error));
                return root;
            }

            LiveEventCalendarDocument document = asset.ToDocument();
            int eventTypeCount = ArraySize(EventTypesFieldName);
            int recurringRuleCount = ArraySize(RecurringRulesFieldName);
            int fixedEventCount = ArraySize(FixedEventsFieldName);
            int publishedStampCount = ArraySize(PublishedStampsFieldName);
            int ignoredWarningCount = ArraySize(IgnoredWarningsFieldName);

            AddNewerSchemaWarning(root, asset);
            AddBrokenEventTypesWarning(root, eventTypeCount, document.EventTypes.Count);
            AddDroppedEntriesWarning(root, asset);

            root.Add(Line(SummaryElementName, string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorSummaryFormat,
                Count(eventTypeCount), Count(recurringRuleCount), Count(fixedEventCount))));
            root.Add(Line(RemoteConfigKeyElementName, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.InspectorRemoteConfigKeyFormat, document.RemoteConfigKey)));
            root.Add(Line(PublishedElementName, PublishedText(document, publishedStampCount)));
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
            return root;
        }

        /// <summary>
        /// Test thay đường mở hub để không dựng cửa sổ thật (batchmode <c>-nographics</c> không có cửa sổ, và một cửa sổ hub
        /// mở ra sẽ mang theo cả phiên + asset đang nhớ của người chạy). <c>Dispose</c> trả lại đường thật.
        /// </summary>
        internal static IDisposable OverrideOpenInHub(Func<LiveEventCalendarAsset, LiveOpsHubWindow> openInHub)
        {
            if (openInHub == null) throw new ArgumentNullException(nameof(openInHub));
            OpenInHubScope scope = new OpenInHubScope(_openInHub);
            _openInHub = openInHub;
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

        private void AddNewerSchemaWarning(VisualElement root, LiveEventCalendarAsset asset)
        {
            if (asset.SchemaVersion <= LiveEventCalendarAsset.CurrentSchemaVersion) return;
            root.Add(Help(NewerSchemaHelpElementName, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.InspectorNewerSchemaFormat, Count(asset.SchemaVersion),
                Count(LiveEventCalendarAsset.CurrentSchemaVersion))));
        }

        private void AddBrokenEventTypesWarning(VisualElement root, int eventTypeCount, int usableEventTypeCount)
        {
            int brokenEventTypeCount = eventTypeCount - usableEventTypeCount;
            if (brokenEventTypeCount <= 0) return;
            root.Add(Help(BrokenEventTypesHelpElementName, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.InspectorBrokenEventTypesFormat, Count(brokenEventTypeCount))));
        }

        private void AddDroppedEntriesWarning(VisualElement root, LiveEventCalendarAsset asset)
        {
            // Cùng bộ biên dịch ở THỨ TỰ XUẤT mà Kiểm lịch và game dùng (V-6), nên con số này không bao giờ lệch với hub.
            int droppedCount = asset.Compile().DroppedCount;
            if (droppedCount <= 0) return;
            root.Add(Help(DroppedEntriesHelpElementName, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.InspectorDroppedEntriesFormat, Count(droppedCount))));
        }

        private void AddManyStampsHint(VisualElement root, int publishedStampCount)
        {
            if (publishedStampCount <= ManyStampsThreshold) return;
            root.Add(Help(ManyStampsHelpElementName, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.InspectorManyStampsFormat, Count(publishedStampCount), Count(ManyStampsThreshold)),
                HelpBoxMessageType.Info));
        }

        private static string PublishedText(LiveEventCalendarDocument document, int publishedStampCount)
        {
            if (publishedStampCount <= 0 || document.PublishedStamps.Count == 0) return LiveOpsHubStrings.InspectorPublishedNone;
            // Giờ đăng in NGUYÊN VĂN như trong file (PD-2): dấu do lần xuất trước ghi, inspector không parse lại nên chuỗi
            // hỏng vẫn hiện ra đúng cái người dùng phải đi sửa.
            PublishedCalendarStamp latest = document.PublishedStamps[document.PublishedStamps.Count - 1];
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

        private static Label Line(string elementName, string text)
        {
            Label line = new Label(text);
            line.name = elementName;
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

        /// <summary>Trả lại đường mở hub trước đó — lồng nhau được, như <c>LiveOpsHubLanguage.Override</c>.</summary>
        private sealed class OpenInHubScope : IDisposable
        {
            private readonly Func<LiveEventCalendarAsset, LiveOpsHubWindow> _previousOpenInHub;
            private bool _isDisposed;

            internal OpenInHubScope(Func<LiveEventCalendarAsset, LiveOpsHubWindow> previousOpenInHub)
            {
                _previousOpenInHub = previousOpenInHub;
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                _openInHub = _previousOpenInHub;
            }
        }
    }
}
