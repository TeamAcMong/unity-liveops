using System;
using System.Collections.Generic;
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
    /// Nhóm Logic vì mọi khẳng định chỉ đọc cây element vừa dựng — không cần layout, không cần cửa sổ. Unity không có đường
    /// công khai nào bấm hộ một nút chưa gắn vào cửa sổ, mà mở cửa sổ hub thật thì đụng đúng chỗ nhớ asset của người đang chạy
    /// test; nên nút được kiểm hai lớp: <c>AssertButtonRuns</c> đọc delegate đang treo trên nút để chứng minh nó NỐI với
    /// <c>RequestOpenInHub</c> (đổi nút sang <c>new Button()</c> là đỏ), rồi gọi thẳng hàm đó để kiểm nó mở đúng asset.
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
            Label editHint = root.Q<Label>(LiveEventCalendarAssetInspector.EditHintElementName);
            Assert.AreEqual(LiveOpsHubStrings.InspectorEditHint, editHint.text,
                "inspector nói rõ nó chỉ tóm tắt, sửa ở hub");
            Assert.AreEqual(1, root.styleSheets.count,
                "inspector tự nạp stylesheet của chính nó — panel cửa sổ Inspector không thấy sheet nào của hub");
            Assert.IsTrue(editHint.ClassListContains(LiveOpsHubClassNames.InspectorLine),
                "mọi dòng chữ mang class xuống dòng: câu dài nhất của màn này hơn 100 ký tự, mà white-space khởi đầu là nowrap");
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
                Button openInHubButton = root.Q<Button>(LiveEventCalendarAssetInspector.OpenInHubButtonElementName);
                Assert.IsNotNull(openInHubButton, "luôn có nút sang hub");
                AssertButtonRuns(openInHubButton, _inspector,
                    nameof(LiveEventCalendarAssetInspector.RequestOpenInHub));
                ((LiveEventCalendarAssetInspector)_inspector).RequestOpenInHub();
            }

            Assert.AreEqual(1, openCount, "bấm một lần mở một lần");
            Assert.AreSame(asset, openedAsset, "hub nhận đúng asset đang chọn, không phải asset đang nhớ của người dùng");
            Assert.AreEqual(LiveEventCalendarAssetInspector.DefaultOpenInHub, FieldOpenInHub(),
                "ra khỏi scope thì trả lại đường thật");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Hub sửa / đăng / Hoàn tác trong khi asset vẫn đang chọn ở Project view: inspector phải dựng lại, không thì mọi con số
        /// đứng yên ở lúc bấm chọn và cảnh báo cũ còn doạ sau khi đã sửa xong (mục 7.0).
        /// </summary>
        [Test]
        public void AssetChangedWhileShown_RebuildsEveryNumberAndWarning()
        {
            MethodInfo trackSerializedObjectValue = typeof(UnityEditor.UIElements.BindingExtensions).GetMethod(
                nameof(UnityEditor.UIElements.BindingExtensions.TrackSerializedObjectValue),
                BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(VisualElement), typeof(UnityEditor.SerializedObject), typeof(Action<UnityEditor.SerializedObject>) },
                null);
            Assert.AreEqual(trackSerializedObjectValue, LiveEventCalendarAssetInspector.DefaultTrackSerializedObject.Method,
                "đường nghe thật phải là TrackSerializedObjectValue của Unity — có ở cả 2022.3 lẫn 6000.6");
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateMemoryAsset(LiveEventCalendarDocument.Empty);
            VisualElement trackedElement = null;
            UnityEditor.SerializedObject trackedSerializedObject = null;
            Action<UnityEditor.SerializedObject> rebuild = null;
            VisualElement root;

            using (LiveEventCalendarAssetInspector.OverrideTrackSerializedObject((element, serialized, callback) =>
            {
                trackedElement = element;
                trackedSerializedObject = serialized;
                rebuild = callback;
            }))
            {
                root = BuildInspector(asset);
            }

            Assert.AreSame(root, trackedElement, "nghe trên chính cây vừa dựng");
            Assert.IsNotNull(trackedSerializedObject);
            Assert.AreSame(asset, trackedSerializedObject.targetObject, "nghe đúng asset đang hiện");
            Assert.IsNotNull(rebuild, "inspector phải đăng ký một hàm dựng lại, không chỉ dựng một lần rồi đứng yên");
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorSummaryFormat, "0", "0", "0"),
                root.Q<Label>(LiveEventCalendarAssetInspector.SummaryElementName).text);
            AssertNoWarnings(root);

            AddEventTypeEntryWithEmptyId(asset);
            rebuild(trackedSerializedObject);

            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorSummaryFormat, "1", "0", "0"),
                root.Q<Label>(LiveEventCalendarAssetInspector.SummaryElementName).text,
                "số của tóm tắt theo asset lúc này, không phải lúc bấm chọn");
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InspectorBrokenEventTypesFormat, "1"),
                root.Q<HelpBox>(LiveEventCalendarAssetInspector.BrokenEventTypesHelpElementName).text,
                "cảnh báo asset hỏng hiện ra ngay khi asset hỏng đi");
            Button openInHubButton = root.Q<Button>(LiveEventCalendarAssetInspector.OpenInHubButtonElementName);
            Assert.IsNotNull(openInHubButton, "dựng lại vẫn còn đường sang hub");
            AssertButtonRuns(openInHubButton, _inspector, nameof(LiveEventCalendarAssetInspector.RequestOpenInHub));
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Khẳng định nút CHẠY đúng hàm của inspector khi bấm — không chỉ "nút có mặt". Vì sao bằng reflection:
        /// <c>Clickable.SimulateSingleClick</c> là internal của Unity và <c>SendEvent</c> cần một panel thật, nên với nút chưa
        /// gắn vào cửa sổ, đọc delegate đang treo trên nút là đường duy nhất thấy được nó nối vào đâu.
        /// </summary>
        private static void AssertButtonRuns(Button button, object expectedTarget, string expectedMethodName)
        {
            MethodInfo expectedMethod = expectedTarget.GetType().GetMethod(expectedMethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(expectedMethod, expectedMethodName + ": không có hàm này trên " + expectedTarget.GetType().Name);
            List<Delegate> handlers = new List<Delegate>();
            CollectDelegateFields(button, handlers);
            CollectDelegateFields(button.clickable, handlers);

            for (int index = 0; index < handlers.Count; index++)
            {
                if (!ReferenceEquals(handlers[index].Target, expectedTarget)) continue;
                if (handlers[index].Method != expectedMethod) continue;
                return;
            }

            Assert.Fail("nút không nối với " + expectedMethodName + " — bấm vào sẽ không làm gì (" + handlers.Count
                + " hành động đang treo)");
        }

        private static void CollectDelegateFields(object owner, List<Delegate> handlers)
        {
            if (owner == null) return;
            for (Type type = owner.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly);
                for (int index = 0; index < fields.Length; index++)
                {
                    if (!typeof(Delegate).IsAssignableFrom(fields[index].FieldType)) continue;
                    Delegate handler = fields[index].GetValue(owner) as Delegate;
                    if (handler == null) continue;
                    handlers.AddRange(handler.GetInvocationList());
                }
            }
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
