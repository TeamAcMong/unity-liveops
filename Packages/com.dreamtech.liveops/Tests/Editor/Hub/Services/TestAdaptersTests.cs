using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>Adapter Manual/InMemory/Scripted/Rewriting/MissingPaths + action tạm — hành vi mà test và kịch bản chụp ảnh dựa vào.</summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class TestAdaptersTests
    {
        private const string OneEventWithBrokenEnd =
            "{\"events\":[{\"id\":\"lava-quest-2026-10\",\"type\":\"lava-quest\",\"startUtc\":\"2026-10-01T00:00:00Z\",\"endUtc\":\"2026-10-3\"}]}";

        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void DestroyCreatedObjects()
        {
            for (int index = 0; index < _createdObjects.Count; index++)
            {
                if (_createdObjects[index] != null) UnityEngine.Object.DestroyImmediate(_createdObjects[index]);
            }
            _createdObjects.Clear();
        }

        [Test]
        public void InMemoryClipboard_RoundTrip_NullBecomesEmpty()
        {
            var clipboard = new InMemoryLiveOpsHubClipboard();
            ILiveOpsHubClipboard port = clipboard;

            port.Text = "{\"version\":2}";
            Assert.AreEqual("{\"version\":2}", port.Text);

            port.Text = null;
            Assert.AreEqual(string.Empty, port.Text);
        }

        [Test]
        public void ManualFileDialog_ReturnsPresetPath_RecordsCalls()
        {
            var dialog = new ManualLiveOpsHubFileDialog("/Users/designer/Documents/LiveOps/liveops_calendar-5b0d93.json");
            ILiveOpsHubFileDialog port = dialog;

            string path = port.SaveFile("Lưu JSON lịch", "/Users/designer/Documents/LiveOps", "liveops_calendar-5b0d93", "json");
            port.Reveal(path);

            Assert.AreEqual("/Users/designer/Documents/LiveOps/liveops_calendar-5b0d93.json", path);
            Assert.AreEqual(1, dialog.SaveFileCalls.Count);
            Assert.AreEqual("Lưu JSON lịch", dialog.SaveFileCalls[0].Title);
            Assert.AreEqual("liveops_calendar-5b0d93", dialog.SaveFileCalls[0].DefaultName);
            Assert.AreEqual("json", dialog.SaveFileCalls[0].Extension);
            CollectionAssert.AreEqual(new[] { path }, dialog.RevealedPaths);
        }

        [Test]
        public void ManualFileDialog_DefaultIsCancel()
        {
            Assert.AreEqual(string.Empty, new ManualLiveOpsHubFileDialog().SaveFile("t", "d", "n", "json"));
        }

        [Test]
        public void ScriptedPresenter_ReturnsQueuedInOrder_RecordsRequests()
        {
            var presenter = new ScriptedLiveOpsHubConfirmationPresenter(LiveOpsConfirmResult.Destructive);
            presenter.Enqueue(LiveOpsConfirmResult.Safe);
            LiveOpsConfirmRequest first = BuildRequest("Xoá đợt hunt-0914?");
            LiveOpsConfirmRequest second = BuildRequest("Gỡ dấu đã đăng?");

            Assert.AreEqual(LiveOpsConfirmResult.Destructive, presenter.Confirm(first));
            Assert.AreEqual(LiveOpsConfirmResult.Safe, presenter.Confirm(second));
            Assert.AreEqual(0, presenter.PendingResultCount);
            Assert.AreEqual(2, presenter.Requests.Count);
            Assert.AreSame(first, presenter.Requests[0]);
            Assert.AreSame(second, presenter.Requests[1]);
        }

        [Test]
        public void ScriptedPresenter_EmptyQueue_ReturnsSafe()
        {
            var presenter = new ScriptedLiveOpsHubConfirmationPresenter();
            Assert.AreEqual(LiveOpsConfirmResult.Safe, presenter.Confirm(BuildRequest("Xoá đợt?")));
            Assert.AreEqual(1, presenter.Requests.Count);
        }

        [Test]
        public void ManualIdentityTimeZoneCompilationState_ReturnConfiguredValues()
        {
            ILiveOpsHubPublisherIdentity identity = new ManualLiveOpsHubPublisherIdentity("Minh", "git user.name");
            var timeZone = new ManualLiveOpsHubTimeZone(TimeSpan.FromHours(7));
            var compilationState = new ManualLiveOpsHubCompilationState(true);

            Assert.AreEqual("Minh", identity.PublisherName);
            Assert.AreEqual("git user.name", identity.SourceLabel);
            Assert.AreEqual(TimeSpan.FromHours(7), ((ILiveOpsHubTimeZone)timeZone).DeviceOffsetAt(new DateTime(2026, 9, 13, 8, 47, 0, DateTimeKind.Utc)));
            Assert.IsTrue(((ILiveOpsHubCompilationState)compilationState).IsCompiling);

            compilationState.IsCompiling = false;
            Assert.IsFalse(((ILiveOpsHubCompilationState)compilationState).IsCompiling);
        }

        [Test]
        public void RewritingReadBack_RunsRealParserOnRewrittenText()
        {
            // Không viết lại: parser thật bỏ đợt có giờ kết thúc hỏng.
            LiveEventCalendarParseResult direct = new GameParserLiveOpsHubJsonReadBack().ReadBack(OneEventWithBrokenEnd);
            Assert.AreEqual(0, direct.Calendar.Instances.Count);
            Assert.AreEqual(1, direct.Problems.Count);

            // Viết lại giờ thành đúng trước khi đọc: parser THẬT giữ đợt — seam không giả kết quả.
            var fixing = new RewritingLiveOpsHubJsonReadBack(text => text.Replace("\"2026-10-3\"", "\"2026-10-03T00:00:00Z\""));
            LiveEventCalendarParseResult fixedResult = fixing.ReadBack(OneEventWithBrokenEnd);
            Assert.AreEqual(1, fixedResult.Calendar.Instances.Count);
            Assert.AreEqual("lava-quest-2026-10", fixedResult.Calendar.Instances[0].EventId);
            Assert.IsFalse(fixedResult.HasProblems);
            StringAssert.Contains("2026-10-03T00:00:00Z", fixing.LastRewrittenText);

            // Kịch bản (h): thay cả chuỗi bằng "{" — parser thật báo JSON hỏng, lịch rỗng.
            var breaking = new RewritingLiveOpsHubJsonReadBack(text => "{");
            LiveEventCalendarParseResult brokenResult = breaking.ReadBack(OneEventWithBrokenEnd);
            Assert.AreEqual(0, brokenResult.Calendar.Instances.Count);
            Assert.IsTrue(brokenResult.HasProblems);
            Assert.AreEqual(JsonLiveEventCalendarParser.Parse("{").Problems[0], brokenResult.Problems[0]);
        }

        [Test]
        public void RewritingReadBack_DocumentRunsRealParserOnRewrittenText()
        {
            // Không viết lại: tài liệu giữ nguyên văn giờ hỏng (không biên dịch nên đợt không bị bỏ) — đúng cái JSON viết.
            LiveEventCalendarDocumentParseResult direct = new GameParserLiveOpsHubJsonReadBack().ReadBackDocument(OneEventWithBrokenEnd);
            Assert.IsTrue(direct.IsReadable);
            Assert.AreEqual(1, direct.Document.FixedEvents.Count);
            Assert.AreEqual("2026-10-3", direct.Document.FixedEvents[0].EndUtcText);

            // Viết lại giờ trước khi đọc: parser THẬT đọc chuỗi đã viết lại — seam không giả tài liệu.
            var fixing = new RewritingLiveOpsHubJsonReadBack(text => text.Replace("\"2026-10-3\"", "\"2026-10-03T00:00:00Z\""));
            LiveEventCalendarDocumentParseResult fixedResult = fixing.ReadBackDocument(OneEventWithBrokenEnd);
            Assert.IsTrue(fixedResult.IsReadable);
            Assert.AreEqual(1, fixedResult.Document.FixedEvents.Count);
            Assert.AreEqual("lava-quest-2026-10", fixedResult.Document.FixedEvents[0].EventId);
            Assert.AreEqual("2026-10-03T00:00:00Z", fixedResult.Document.FixedEvents[0].EndUtcText);
            StringAssert.Contains("2026-10-03T00:00:00Z", fixing.LastRewrittenText);

            // Kịch bản (h): thay cả chuỗi bằng JSON sai cú pháp — parser thật báo không đọc được, tài liệu rỗng, cùng lý do như gọi thẳng.
            var breaking = new RewritingLiveOpsHubJsonReadBack(text => "{ not json");
            LiveEventCalendarDocumentParseResult brokenResult = breaking.ReadBackDocument(OneEventWithBrokenEnd);
            LiveEventCalendarDocumentParseResult brokenDirect = JsonLiveEventCalendarParser.ParseDocument("{ not json");
            Assert.IsFalse(brokenResult.IsReadable);
            Assert.AreEqual(0, brokenResult.Document.FixedEvents.Count);
            Assert.AreEqual("{ not json", breaking.LastRewrittenText);
            CollectionAssert.AreEqual(brokenDirect.Problems, brokenResult.Problems);
        }

        [Test]
        public void RewritingReadBack_NullRewrite_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RewritingLiveOpsHubJsonReadBack(null));
        }

        [Test]
        public void MissingPathsLayoutLoader_ReturnsNullOnlyForListedPaths()
        {
            var inner = new FakeLayoutLoader(Track(ScriptableObject.CreateInstance<VisualTreeAsset>()),
                Track(ScriptableObject.CreateInstance<StyleSheet>()));
            var loader = new MissingPathsLiveOpsHubLayoutLoader(new[] { LiveOpsHubPaths.ShellUxml, LiveOpsHubPaths.ThemeUss }, inner);

            Assert.IsNull(loader.LoadVisualTree(LiveOpsHubPaths.ShellUxml));
            Assert.IsNull(loader.LoadStyleSheet(LiveOpsHubPaths.ThemeUss));
            Assert.AreSame(inner.VisualTree, loader.LoadVisualTree(LiveOpsHubPaths.ConfirmWindowUxml));
            Assert.AreSame(inner.StyleSheet, loader.LoadStyleSheet(LiveOpsHubPaths.ShellUss));
            CollectionAssert.AreEqual(new[] { LiveOpsHubPaths.ConfirmWindowUxml, LiveOpsHubPaths.ShellUss }, inner.RequestedPaths,
                "đường dẫn bị giả mất không được chạm tới loader thật");
        }

        [Test]
        public void MissingPathsLayoutLoader_SingleArgumentConstructor_ListedPathReturnsNull()
        {
            // Chỉ kiểm ctor một tham số dựng được và đường dẫn liệt kê trả null. KHÔNG chứng minh loader bên trong là
            // AssetDatabase: ở W1 chưa có UXML/USS nào của hub trên đĩa nên mọi loader đều trả null với đường dẫn chưa liệt kê;
            // đường thật được phủ khi gói có UXML/USS của hub nạp layout qua loader mặc định.
            var loader = new MissingPathsLiveOpsHubLayoutLoader(new[] { LiveOpsHubPaths.ShellUxml });
            Assert.IsNull(loader.LoadVisualTree(LiveOpsHubPaths.ShellUxml));
        }

        // INTERIM(G-PASTE): hai test dưới khoá action tạm (sổ nhánh tạm I-3). G-PASTE xoá InterimUnavailableHubActions thì xoá
        // cùng hai test này và thay bằng Actions_DefaultCanPasteRunningJson_IsTrue ở PasteRunningJsonTests.
        [Test]
        public void Interim_CanPasteRunningJson_False_WithReason()
        {
            ILiveOpsHubActions actions = new InterimUnavailableHubActions();

            Assert.IsFalse(actions.CanPasteRunningJson);
            Assert.AreEqual("Chưa có trong bản dev này", actions.PasteRunningJsonUnavailableReason);
            Assert.DoesNotThrow(() => actions.PasteRunningJson(new Rect(0, 0, 10, 10)));
        }

        [Test]
        public void Interim_CanImportRunningJson_False()
        {
            ILiveOpsHubActions actions = new InterimUnavailableHubActions();

            Assert.IsFalse(actions.CanImportRunningJson);
            Assert.AreEqual("Chưa có trong bản dev này", actions.ImportRunningJsonUnavailableReason);
            Assert.DoesNotThrow(() => actions.ImportRunningJsonIntoNewAsset(new Rect(0, 0, 10, 10)));
        }

        private T Track<T>(T createdObject) where T : UnityEngine.Object
        {
            _createdObjects.Add(createdObject);
            return createdObject;
        }

        private static LiveOpsConfirmRequest BuildRequest(string title)
        {
            return new LiveOpsConfirmRequest.Builder().WithTitle(title).WithButtons("Xoá", "Giữ lại").Build();
        }

        private sealed class FakeLayoutLoader : ILiveOpsHubLayoutLoader
        {
            private readonly List<string> _requestedPaths = new List<string>();

            public FakeLayoutLoader(VisualTreeAsset visualTree, StyleSheet styleSheet)
            {
                VisualTree = visualTree;
                StyleSheet = styleSheet;
            }

            public VisualTreeAsset VisualTree { get; }
            public StyleSheet StyleSheet { get; }
            public IReadOnlyList<string> RequestedPaths => _requestedPaths;

            public VisualTreeAsset LoadVisualTree(string assetPath)
            {
                _requestedPaths.Add(assetPath);
                return VisualTree;
            }

            public StyleSheet LoadStyleSheet(string assetPath)
            {
                _requestedPaths.Add(assetPath);
                return StyleSheet;
            }
        }
    }
}
