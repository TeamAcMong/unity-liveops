using System;
using System.Globalization;
using System.Reflection;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Inspector gọn của asset lịch (10.3 G-INSPECTOR): dựng được, tóm tắt đúng, cảnh báo khi asset hỏng, và nút đưa sang hub
    /// bằng ĐÚNG asset đang chọn.
    /// <para>
    /// Nhóm Logic vì mọi khẳng định chỉ đọc cây element vừa dựng — không cần layout, không cần cửa sổ. Nút được thử bằng
    /// cách gọi thẳng <c>RequestOpenInHub</c>, đúng hàm mà nút dựng với: Unity không có đường công khai nào bấm hộ một nút
    /// chưa gắn vào cửa sổ, mà mở cửa sổ hub thật thì đụng đúng chỗ nhớ asset của người đang chạy test.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class LiveEventCalendarAssetInspectorTests
    {
        private UnityEditor.Editor _inspector;

        [TearDown]
        public void TearDown()
        {
            if (_inspector != null) UnityEngine.Object.DestroyImmediate(_inspector);
            _inspector = null;
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void CreateInspectorGUI_NoThrow()
        {
            VisualElement root = null;

            Assert.DoesNotThrow(() => root = BuildInspector(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)),
                "asset mẫu thiết kế có sẵn mục hỏng — inspector vẫn phải dựng được");

            Assert.IsNotNull(root);
            Assert.AreEqual(LiveEventCalendarAssetInspector.RootElementName, root.name);
            Button openInHubButton = root.Q<Button>(LiveEventCalendarAssetInspector.OpenInHubButtonElementName);
            Assert.IsNotNull(openInHubButton, "luôn có đường sang hub, kể cả khi asset hỏng");
            Assert.AreEqual(LiveOpsHubStrings.InspectorOpenInHubButton, openInHubButton.text);
            Assert.AreEqual(LiveOpsHubStrings.InspectorEditHint,
                root.Q<Label>(LiveEventCalendarAssetInspector.EditHintElementName).text,
                "inspector nói rõ nó chỉ tóm tắt, sửa ở hub");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void EmptyAsset_SummarisesCountsAndNeverPublished()
        {
            VisualElement root = BuildInspector(LiveOpsHubTestServices.CreateMemoryAsset(LiveEventCalendarDocument.Empty));

            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorSummaryFormat, "0", "0", "0"),
                root.Q<Label>(LiveEventCalendarAssetInspector.SummaryElementName).text);
            Assert.AreEqual(LiveOpsHubStrings.InspectorPublishedNone,
                root.Q<Label>(LiveEventCalendarAssetInspector.PublishedElementName).text);
            Assert.IsNull(root.Q(LiveEventCalendarAssetInspector.IgnoredWarningsElementName),
                "không có cảnh báo bỏ qua thì không in dòng rỗng");
            AssertNoWarnings(root);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DesignSampleAsset_SummaryAndRemoteKeyFollowTheAsset()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;

            VisualElement root = BuildInspector(LiveOpsHubTestServices.CreateMemoryAsset(document));

            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorSummaryFormat,
                    document.EventTypes.Count.ToString(CultureInfo.InvariantCulture),
                    document.RecurringRules.Count.ToString(CultureInfo.InvariantCulture),
                    document.FixedEvents.Count.ToString(CultureInfo.InvariantCulture)),
                root.Q<Label>(LiveEventCalendarAssetInspector.SummaryElementName).text);
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorRemoteConfigKeyFormat,
                    document.RemoteConfigKey),
                root.Q<Label>(LiveEventCalendarAssetInspector.RemoteConfigKeyElementName).text);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NewerSchema_ShowsHelpBox()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateMemoryAsset(LiveEventCalendarDocument.Empty);
            Assert.IsNull(BuildInspector(asset).Q(LiveEventCalendarAssetInspector.NewerSchemaHelpElementName),
                "asset đúng schema của bản này không được doạ người dùng");
            UnityEngine.Object.DestroyImmediate(_inspector);
            _inspector = null;
            int newerSchemaVersion = LiveEventCalendarAsset.CurrentSchemaVersion + 1;
            SetSchemaVersion(asset, newerSchemaVersion);

            VisualElement root = BuildInspector(asset);

            HelpBox help = root.Q<HelpBox>(LiveEventCalendarAssetInspector.NewerSchemaHelpElementName);
            Assert.IsNotNull(help, "schema lớn hơn bản code biết phải báo TRƯỚC khi hub lưu đè (mục 4.1)");
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorNewerSchemaFormat,
                    newerSchemaVersion.ToString(CultureInfo.InvariantCulture),
                    LiveEventCalendarAsset.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture)),
                help.text);
            Assert.AreEqual(HelpBoxMessageType.Warning, help.messageType);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ManyStamps_Over50_ShowsHint()
        {
            LiveEventCalendarAsset atThreshold = LiveOpsHubTestServices.CreateMemoryAsset(
                DocumentWithStamps(LiveEventCalendarAssetInspector.ManyStampsThreshold));
            Assert.IsNull(BuildInspector(atThreshold).Q(LiveEventCalendarAssetInspector.ManyStampsHelpElementName),
                "đúng mốc thì chưa gợi ý — chỉ khi vượt mốc");
            UnityEngine.Object.DestroyImmediate(_inspector);
            _inspector = null;
            int stampCount = LiveEventCalendarAssetInspector.ManyStampsThreshold + 1;

            VisualElement root = BuildInspector(LiveOpsHubTestServices.CreateMemoryAsset(DocumentWithStamps(stampCount)));

            HelpBox hint = root.Q<HelpBox>(LiveEventCalendarAssetInspector.ManyStampsHelpElementName);
            Assert.IsNotNull(hint, "hơn 50 bản chụp thì gợi ý gỡ bớt (mục 4.1)");
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorManyStampsFormat,
                    stampCount.ToString(CultureInfo.InvariantCulture),
                    LiveEventCalendarAssetInspector.ManyStampsThreshold.ToString(CultureInfo.InvariantCulture)),
                hint.text);
            Assert.AreEqual(HelpBoxMessageType.Info, hint.messageType, "gợi ý dọn dẹp không phải lỗi");
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorPublishedFormat,
                    stampCount.ToString(CultureInfo.InvariantCulture), StampTimeText(stampCount - 1)),
                root.Q<Label>(LiveEventCalendarAssetInspector.PublishedElementName).text,
                "dòng lịch sử nêu bản chụp CUỐI danh sách");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BrokenAsset_ShowsBrokenTypesAndDroppedEntries()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateMemoryAsset(DocumentWithUnreadableEndTime());
            AddEventTypeEntryWithEmptyId(asset);

            VisualElement root = BuildInspector(asset);

            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorBrokenEventTypesFormat, "1"),
                root.Q<HelpBox>(LiveEventCalendarAssetInspector.BrokenEventTypesHelpElementName).text,
                "loại có id rỗng nằm trong file nhưng không vào được tài liệu — chênh lệch đó là câu cảnh báo");
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorDroppedEntriesFormat, "1"),
                root.Q<HelpBox>(LiveEventCalendarAssetInspector.DroppedEntriesHelpElementName).text,
                "số mục bị bỏ lấy từ chính bộ biên dịch mà game dùng");
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorSummaryFormat, "2", "0", "1"),
                root.Q<Label>(LiveEventCalendarAssetInspector.SummaryElementName).text,
                "tóm tắt đếm cả loại hỏng vì đó là thứ có thật trong file");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OpenInHub_CallsOpenWithAsset()
        {
            MethodInfo openWithAsset = typeof(LiveOpsHubWindow).GetMethod(nameof(LiveOpsHubWindow.OpenWithAsset),
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(LiveEventCalendarAsset) }, null);
            Assert.AreEqual(openWithAsset, LiveEventCalendarAssetInspector.DefaultOpenInHub.Method,
                "đường mặc định của nút phải là chính LiveOpsHubWindow.OpenWithAsset, không phải một bản mở cửa sổ chép tay");
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document);
            LiveEventCalendarAsset openedAsset = null;
            int openCount = 0;
            VisualElement root = BuildInspector(asset);

            using (LiveEventCalendarAssetInspector.OverrideOpenInHub(opened =>
            {
                openedAsset = opened;
                openCount++;
                return null;
            }))
            {
                Assert.IsNotNull(root.Q<Button>(LiveEventCalendarAssetInspector.OpenInHubButtonElementName),
                    "nút phải có mặt — nó dựng với chính RequestOpenInHub làm hành động");
                ((LiveEventCalendarAssetInspector)_inspector).RequestOpenInHub();
            }

            Assert.AreEqual(1, openCount, "bấm một lần mở một lần");
            Assert.AreSame(asset, openedAsset, "hub nhận đúng asset đang chọn, không phải asset đang nhớ của người dùng");
            Assert.AreEqual(LiveEventCalendarAssetInspector.DefaultOpenInHub, FieldOpenInHub(),
                "ra khỏi scope thì trả lại đường thật");
            LogAssert.NoUnexpectedReceived();
        }

        private VisualElement BuildInspector(LiveEventCalendarAsset asset)
        {
            _inspector = UnityEditor.Editor.CreateEditor(asset, typeof(LiveEventCalendarAssetInspector));
            Assert.IsInstanceOf<LiveEventCalendarAssetInspector>(_inspector, "Unity phải dựng đúng inspector của package");
            return _inspector.CreateInspectorGUI();
        }

        private static void AssertNoWarnings(VisualElement root)
        {
            Assert.IsNull(root.Q(LiveEventCalendarAssetInspector.NewerSchemaHelpElementName));
            Assert.IsNull(root.Q(LiveEventCalendarAssetInspector.BrokenEventTypesHelpElementName));
            Assert.IsNull(root.Q(LiveEventCalendarAssetInspector.DroppedEntriesHelpElementName));
            Assert.IsNull(root.Q(LiveEventCalendarAssetInspector.ManyStampsHelpElementName));
        }

        private static void SetSchemaVersion(LiveEventCalendarAsset asset, int schemaVersion)
        {
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(asset);
            serialized.FindProperty("schemaVersion").intValue = schemaVersion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Loại có id rỗng chỉ vào được asset bằng đường sửa tay YAML / xung đột merge — dựng lại đúng đường đó.</summary>
        private static void AddEventTypeEntryWithEmptyId(LiveEventCalendarAsset asset)
        {
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(asset);
            UnityEditor.SerializedProperty eventTypes = serialized.FindProperty("eventTypes");
            eventTypes.InsertArrayElementAtIndex(eventTypes.arraySize);
            UnityEditor.SerializedProperty entry = eventTypes.GetArrayElementAtIndex(eventTypes.arraySize - 1);
            entry.FindPropertyRelative("typeId").stringValue = string.Empty;
            entry.FindPropertyRelative("displayName").stringValue = string.Empty;
            entry.FindPropertyRelative("defaultConfigKey").stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LiveEventCalendarDocument DocumentWithStamps(int stampCount)
        {
            LiveEventCalendarDocumentBuilder builder = new LiveEventCalendarDocumentBuilder();
            for (int index = 0; index < stampCount; index++)
            {
                builder.WithPublishedStamp(new PublishedCalendarStamp(StampTimeText(index), LiveOpsDesignSample.PublishedPublisher,
                    LiveOpsDesignSample.PublishedSha256Hex, LiveOpsDesignSample.PublishedByteCount, 2, string.Empty, "{}"));
            }
            return builder.Build();
        }

        private static string StampTimeText(int index)
        {
            return new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(index)
                .ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        }

        /// <summary>Một đợt có giờ kết thúc không đọc được (PD-2: chuỗi giữ nguyên) — bộ biên dịch bỏ đúng một mục.</summary>
        private static LiveEventCalendarDocument DocumentWithUnreadableEndTime()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("lava-quest", "Lava Quest", 0, false, string.Empty))
                .WithFixedEvent(new FixedLiveEventEntry("entry-broken", "lava-quest-2026-10", "lava-quest",
                    "2026-10-01T00:00:00Z", "2026-10-3", string.Empty))
                .Build();
        }

        private static Delegate FieldOpenInHub()
        {
            FieldInfo field = typeof(LiveEventCalendarAssetInspector).GetField("_openInHub",
                BindingFlags.NonPublic | BindingFlags.Static);
            return (Delegate)field.GetValue(null);
        }
    }
}
