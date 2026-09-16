using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Phần chiều sâu của màn Lịch (W5) trên cửa sổ hub THẬT: hai nút toolbar, pane Danh sách, pane "So với đã đăng" (nguồn
    /// Published và Disk — V-13), hover card ghim bằng F8 kèm bộ đếm, nút đề xuất của inspector mở <c>ProposalPopover</c>
    /// (mục 12 I-4), và vế tag kiểm nhanh trong readout (nợ D-3(a)).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class CalendarDepthTests
    {
        private const int MaximumLayoutFrames = 60;
        private const int MaximumLayoutMilliseconds = 5000;

        private LiveOpsHubWindow _window;

        /// <summary>
        /// (V-23) Đóng cửa sổ rồi NHƯỜNG một khung trước khi test kế tiếp chạy: ở 2022.3 batchmode, cửa sổ vừa đóng còn giữ
        /// quyền nhận phím thêm một khung, nên test sau gọi <c>Focus()</c> một lần là rơi vào hư không — đúng cách lượt đầu của
        /// gói này làm <c>CalendarSectionTests.Frame03a_SelectedBarKeepsTimelineFocus</c> đỏ dù nó không đụng gì tới W5.
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
                yield return null;
            }
            LiveOpsHubTestServices.ReleaseAll();
            yield return null;
        }

        // ------------------------------------------------------------------------------------------------ toolbar (mục 12 I-5)

        /// <summary>
        /// Hai nút W5 phải CÓ THẬT trên toolbar [SD1 §3.1]. Ở W4 chúng cố ý không hiện (INTERIM I-5); test này là thứ chứng minh
        /// dấu INTERIM đã được gỡ đúng chỗ, không phải chỉ xoá comment.
        /// </summary>
        [UnityTest]
        public IEnumerator Toolbar_HasListAndCompare()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarToolbar toolbar = Calendar().Toolbar;

            Assert.IsNotNull(toolbar.ListToggle, "toolbar có ToolbarToggle 'Danh sách'");
            Assert.AreEqual(LiveOpsHubStrings.CalendarDepthListToggle, toolbar.ListToggle.text);
            Assert.IsNotNull(toolbar.CompareToggle, "toolbar có ToolbarToggle 'So với đã đăng (n)'");
            StringAssert.Contains(LiveOpsHubStrings.CalendarDepthCompareToggleFormat.Substring(0, 6), toolbar.CompareToggle.text);
            Assert.IsFalse(toolbar.SnapMenu.text.Contains("bản sau"),
                "menu Bắt lưới không còn câu tạm 'bước lưới nối ở bản sau' (nợ D-3(c))");
        }

        /// <summary>SPIKE-B SP-3: chưa đăng lần nào thì nút So với TẮT và lý do in thành CHỮ cạnh nút, không chỉ nằm trong tooltip.</summary>
        [UnityTest]
        public IEnumerator Toolbar_CompareDisabledReasonIsText()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.EmptyAssetScenario);
            CalendarToolbar toolbar = Calendar().Toolbar;

            Assert.IsFalse(toolbar.CompareToggle.enabledSelf, "chưa có dấu đã đăng thì không có gì để so");
            Assert.AreEqual(LiveOpsHubStrings.CalendarDepthCompareUnavailableReason, toolbar.CompareDisabledReason.text);
            Assert.IsFalse(toolbar.CompareDisabledReason.ClassListContains(LiveOpsHubClassNames.CalendarHidden),
                "lý do phải HIỆN — tooltip chỉ là bản phụ (SPIKE-B SP-3)");
        }

        // ------------------------------------------------------------------------------------------------ pane Danh sách

        /// <summary>
        /// Đợt có ngày không đọc được vẫn có một dòng đầy đủ [SD1 §3.12]: nó không lên được trục nhưng vẫn nằm trong asset và vẫn
        /// làm game bỏ đợt — biến mất khỏi màn hình là cách tệ nhất để báo lỗi.
        /// </summary>
        [UnityTest]
        public IEnumerator ListPane_KeepsUnplaceableEventWithRawText()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarSection section = Calendar();

            CalendarListRow row = RowOf(section.ListPane, LiveOpsDesignSample.LavaQuestLateEntryKey);
            Assert.IsNotNull(row, "đợt không đặt được vẫn có dòng trong pane Danh sách");
            Assert.IsTrue(row.IsUnplaceable);
            Assert.AreEqual(LiveOpsHubStrings.CalendarPhaseUnplaceable, row.StateText);
            StringAssert.Contains("2026-10-3", row.EndText, "cột kết thúc in NGUYÊN VĂN chuỗi hỏng để sửa được nó");
        }

        /// <summary>Chip "Không đặt được (n)" ở header làn mở pane Danh sách và chọn đúng dòng [SD1 §3.1].</summary>
        [UnityTest]
        public IEnumerator UnplaceableChip_OpensListPaneAndSelectsRow()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarSection section = Calendar();
            Assert.IsFalse(section.IsListPaneOpen, "mặc định pane Danh sách đóng");

            LiveOpsTimelineLaneHeader header = section.Timeline.FindHeader("lava-quest");
            Assert.IsNotNull(header, "làn lava-quest phải có trên trục");
            using (ClickEvent clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = header.UnplaceableChip;
                header.UnplaceableChip.SendEvent(clickEvent);
            }
            yield return null;

            Assert.IsTrue(section.IsListPaneOpen, "bấm chip mở pane Danh sách");
            Assert.AreEqual(LiveOpsDesignSample.LavaQuestLateEntryKey, section.Presenter.SelectedBarKey,
                "và chọn đúng đợt không đặt được đó");
        }

        // ------------------------------------------------------------------------------------------------ pane So với

        /// <summary>Pane So với THAY CHỖ inspector: hai pane loại trừ nhau, cùng 280px [SD1 §3.4].</summary>
        [UnityTest]
        public IEnumerator ComparePane_ReplacesInspector()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarSection section = Calendar();
            VisualElement inspector = SectionView().Q(LiveOpsHubPaths.CalendarElementNames.Inspector);
            Assert.IsTrue(section.ComparePane.Root.ClassListContains(LiveOpsHubClassNames.CalendarHidden), "mặc định pane So với đóng");

            section.Toolbar.CompareToggle.value = true;
            yield return null;

            Assert.IsTrue(section.IsComparePaneOpen);
            Assert.IsFalse(section.ComparePane.Root.ClassListContains(LiveOpsHubClassNames.CalendarHidden));
            Assert.IsTrue(inspector.ClassListContains(LiveOpsHubClassNames.CalendarHidden),
                "inspector phải nhường chỗ — hai pane cùng một chỗ 280px");
        }

        /// <summary>
        /// (V-13) "Xem khác biệt" của băng "asset đã đổi trên đĩa" điều hướng sang Lịch với <c>CompareSource = Disk</c>: pane mở ở
        /// nguồn Disk, tiêu đề nói bản trên đĩa và note KHÔNG nhắc "Hoàn về bản đã đăng" (mục đó không có ở nguồn này).
        /// </summary>
        [UnityTest]
        public IEnumerator ComparePane_DiskSource_FromNavigation()
        {
            // Nguồn Disk chỉ TỒN TẠI khi phiên thật sự có xung đột đĩa (không thì Publish tự rơi về Published), nên ca này chạy
            // trên asset thật rồi sửa file ngoài Unity — đúng cách băng "asset đã đổi trên đĩa" xuất hiện.
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateAssetFile(DiskConflictAssetFileName,
                        LiveOpsDesignSample.Document)));
            yield return OpenCalendarWith(services);
            CalendarSection section = Calendar();
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry entry));
            services.Session.Apply(new ReplaceFixedEventEdit(entry.WithTimes(entry.StartUtcText, "2026-09-23T12:00:00Z")),
                "Dời kết thúc " + entry.EventId);
            yield return ChangeAssetOnDiskThenNotify(services);

            section.ApplyNavigation(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Calendar)
                .WithCompareSource(LiveOpsHubCompareSource.Disk));
            yield return null;

            Assert.IsTrue(section.IsComparePaneOpen, "tới bằng 'Xem khác biệt' thì pane phải tự mở");
            Assert.AreEqual(LiveOpsHubCompareSource.Disk, services.Session.Publish.ActiveCompareSource);
            Assert.AreEqual(LiveOpsHubStrings.CalendarDepthCompareDiskNote, section.ComparePane.Note.text,
                "nguồn Disk không có mục 'Hoàn về bản đã đăng' nên note cũng phải đổi theo (V-13)");
            StringAssert.StartsWith(LiveOpsHubStrings.CalendarDepthCompareDiskTitleFormat.Substring(0, 12),
                section.ComparePane.Title.text, "tiêu đề nói rõ đang so với bản trên đĩa");
        }

        private IEnumerator ChangeAssetOnDiskThenNotify(LiveOpsHubServices services)
        {
            string assetPath = services.Session.AssetPath;
            Assert.IsNotEmpty(assetPath, "ca này cần asset thật trên đĩa");
            System.IO.File.AppendAllText(System.IO.Path.GetFullPath(assetPath),
                Environment.NewLine + "# sửa tay ngoài Unity" + Environment.NewLine);
            // Cửa sổ ghi một cảnh báo CÓ CHỦ ĐÍCH để Console còn dấu vết lần file đổi ngoài (SP-8b).
            LogAssert.Expect(LogType.Warning, string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ShellDiskConflictLogFormat, services.Session.AssetFileName));
            services.Session.HandleAssetsChanged(new[] { assetPath }, null, null, null);
            yield return null;
        }

        /// <summary>Nút toolbar in ĐÚNG số dòng mà pane vẽ — hai con số đếm bằng hai đường là cách chắc chắn nhất để nút nói dối.</summary>
        [UnityTest]
        public IEnumerator ComparePane_ToggleCountMatchesRows()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarSection section = Calendar();
            section.Toolbar.CompareToggle.value = true;
            yield return null;

            string expected = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarDepthCompareToggleFormat, section.ComparePane.ChangeCount);
            Assert.AreEqual(expected, section.Toolbar.CompareToggle.text);
            Assert.AreEqual(section.ComparePane.ChangeCount, section.ComparePane.Rows.Count);
        }

        // ------------------------------------------------------------------------------------------------ hover card + F8

        /// <summary>
        /// F8 ghim hover card lên phát hiện kế tiếp kèm bộ đếm "1/5" [SD1 §3.8]. Bộ đếm đếm phát hiện của CẢ LỊCH — cùng con số
        /// mà màn Kiểm lịch in, không phải số của một làn.
        /// </summary>
        [UnityTest]
        public IEnumerator HoverCard_F8PinsCardWithCounter()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarSection section = Calendar();
            section.Services.Session.RunCheckToCompletion();
            int findingCount = CalendarHoverCardContent.FindingsInOrder(section.Services.Session).Count;
            Assert.Greater(findingCount, 0, "lịch mẫu phải có phát hiện thì F8 mới có chỗ để tới");

            // F8 đi từ element → presenter → CalendarCommandHandler → màn ghim thẻ; test bơm vào đúng đầu dây đó, không gọi
            // thẳng hàm ghim, để nó chứng minh cả đường dây chứ không chỉ một lời gọi.
            section.Presenter.HandleIntent(new ShowFindingIntent(ShowFindingIntent.Next));
            yield return null;

            Assert.IsTrue(section.HoverCardHost.IsPinned, "F8 GHIM thẻ — rời chuột không tắt");
            Label counter = section.HoverCardHost.Card.Q<Label>(LiveOpsHubPaths.CalendarDepthElementNames.HoverCounter);
            Assert.IsNotNull(counter, "thẻ ghim có bộ đếm");
            Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarDepthHoverCounterFormat, 1, findingCount), counter.text);
            Assert.IsNotNull(section.HoverCardHost.Card.Q<Button>(LiveOpsHubPaths.CalendarDepthElementNames.HoverQuickFix),
                "và nút 'Sửa nhanh…' — thẻ theo chuột thì không có");
        }

        /// <summary>Thẻ theo chuột KHÔNG có nút: thẻ tắt khi rời chuột, nên một cái nút trên đó là cái bẫy.</summary>
        [UnityTest]
        public IEnumerator HoverCard_UnpinnedHasNoQuickFixButton()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarSection section = Calendar();
            LiveOpsTimelineBarModel bar = section.Presenter.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.IsNotNull(bar);

            VisualElement card = CalendarHoverCardContent.Build(bar, null, section.Services, null);

            Assert.IsNull(card.Q<Button>(LiveOpsHubPaths.CalendarDepthElementNames.HoverQuickFix));
            Assert.IsNull(card.Q<Label>(LiveOpsHubPaths.CalendarDepthElementNames.HoverCounter));
        }

        // ------------------------------------------------------------------------------------------------ inspector (mục 12 I-4)

        /// <summary>
        /// (mục 12 I-4) Nút đề xuất trong "Vấn đề (n)" KHÔNG áp gì: nó mở <c>ProposalPopover</c> với lựa chọn vừa bấm chọn sẵn.
        /// Test đi qua chính móc mà section gán, nên nó chứng minh đường dây thật chứ không phải một hàm rỗng.
        /// </summary>
        [UnityTest]
        public IEnumerator InspectorProposal_OpensProposalPopoverPreselected()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarSection section = Calendar();
            section.Services.Session.RunCheckToCompletion();
            section.Presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            yield return null;

            List<int> requested = new List<int>();
            LiveEventCalendarFinding seen = null;
            section.Inspector.ProposalRequested = (finding, repairIndex) =>
            {
                seen = finding;
                requested.Add(repairIndex);
            };
            int before = section.Services.Session.Document.FixedEvents.Count;
            Button repairButton = RepairButtonOf(section);
            Assert.IsNotNull(repairButton, "đợt hunt-0916-bonus có phát hiện kèm cách sửa");

            Click(repairButton);
            yield return null;

            Assert.AreEqual(1, requested.Count, "bấm nút đề xuất mở popover đúng một lần");
            Assert.AreEqual(0, requested[0], "và popover chọn sẵn ĐÚNG lựa chọn vừa bấm");
            Assert.IsNotNull(seen);
            Assert.AreEqual(before, section.Services.Session.Document.FixedEvents.Count,
                "nút đề xuất không áp gì trước khi người dùng bấm Áp trong popover");
        }

        // ------------------------------------------------------------------------------------------------ nợ D-3(a)

        /// <summary>
        /// (nợ D-3(a), Hình 12 khung 5) Câu kiểm nhanh mà presenter tính ra phải TỚI ĐƯỢC NHÃN trên màn: vế thứ ba của readout
        /// hiện câu đó và dấu đi theo kết quả. Trước W5 câu chỉ nằm trong một property mà không chỗ nào vẽ.
        /// </summary>
        [UnityTest]
        public IEnumerator PreviewDrag_QuickCheckTagReachesReadout()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarSection section = Calendar();
            section.Services.Session.RunCheckToCompletion();
            Assert.IsTrue(section.Timeline.ReadoutQuickCheck.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                "chưa kéo thì không có tag nào");

            DateTime clearStartUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
            section.Presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, clearStartUtc,
                clearStartUtc.AddHours(36), LiveOpsTimelineGesturePhase.Preview));
            yield return null;

            Assert.IsFalse(section.Timeline.ReadoutQuickCheck.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                "đang kéo thì vế tag kiểm nhanh HIỆN trên readout");
            Assert.AreEqual(LiveOpsHubStrings.CalendarQuickCheckOkTag, section.Timeline.ReadoutQuickCheckText.text);
            Assert.AreEqual(HealthState.Ok, section.Timeline.ReadoutQuickCheckMark.HealthValue,
                "dấu đi theo kết quả — hết chồng thì Ok");

            DateTime overlappingStartUtc = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);
            section.Presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, overlappingStartUtc,
                overlappingStartUtc.AddHours(36), LiveOpsTimelineGesturePhase.Preview));
            yield return null;
            Assert.AreEqual(HealthState.Blocked, section.Timeline.ReadoutQuickCheckMark.HealthValue,
                "kéo vào chỗ chồng thì dấu đổi sang Blocked, không đứng yên ở Ok");

            section.Presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, overlappingStartUtc,
                overlappingStartUtc.AddHours(36), LiveOpsTimelineGesturePhase.Cancel));
            yield return null;
            Assert.IsTrue(section.Timeline.ReadoutQuickCheck.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                "huỷ cử chỉ thì tag tắt cùng readout");
        }

        // ------------------------------------------------------------------------------------------------ dựng

        /// <summary>
        /// Bấm nút đúng cách người dùng bấm: <c>Button</c> nghe <c>Clickable</c>, không nghe một <c>ClickEvent</c> nặn tay — gửi
        /// ClickEvent thẳng vào nút thì test xanh giả mà nút thật không chạy.
        /// </summary>
        private static void Click(Button button)
        {
            using (NavigationSubmitEvent submitEvent = NavigationSubmitEvent.GetPooled(EventModifiers.None))
            {
                submitEvent.target = button;
                button.SendEvent(submitEvent);
            }
        }

        private static CalendarListRow RowOf(CalendarListPane pane, string entryKey)
        {
            IReadOnlyList<CalendarListRow> rows = pane.Rows;
            for (int index = 0; index < rows.Count; index++)
            {
                if (string.Equals(rows[index].EntryKey, entryKey, StringComparison.Ordinal)) return rows[index];
            }
            return null;
        }

        /// <summary>Nút đề xuất nằm TRONG card phát hiện của foldout "Vấn đề (n)" — nút "Xoá đợt" ở ngoài card, không lẫn được.</summary>
        private Button RepairButtonOf(CalendarSection section)
        {
            VisualElement body = SectionView().Q(LiveOpsHubPaths.CalendarElementNames.InspectorBody);
            Assert.IsNotNull(body);
            List<Button> buttons = new List<Button>();
            body.Query<Button>().ForEach(button =>
            {
                if (button.parent != null && button.parent.ClassListContains(LiveOpsHubClassNames.FindingRow)) buttons.Add(button);
            });
            return buttons.Count == 0 ? null : buttons[0];
        }

        private CalendarSection Calendar()
        {
            foreach (IHubSection section in ((IHubHost)_window).Sections)
            {
                CalendarSection calendar = section as CalendarSection;
                if (calendar != null) return calendar;
            }
            Assert.Fail("cửa sổ hub không có màn Lịch");
            return null;
        }

        private VisualElement SectionView()
        {
            return _window.SectionBody == null || _window.SectionBody.childCount == 0 ? null : _window.SectionBody[0];
        }

        private const string DiskConflictAssetFileName = "CalendarDepthDiskConflict.asset";

        private IEnumerator OpenCalendar(string scenarioId)
        {
            return OpenCalendarWith(LiveOpsHubTestServices.ForScenario(scenarioId));
        }

        private IEnumerator OpenCalendarWith(LiveOpsHubServices services)
        {
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            _window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
            _window.position = new Rect(0, 0, 1280, 760);
            yield return WaitForLayout(_window.rootVisualElement);
        }

        private static IEnumerator WaitForLayout(VisualElement element)
        {
            int frames = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!LiveOpsHubWindowTestScope.HasLayout(element))
            {
                frames++;
                if (frames > MaximumLayoutFrames && stopwatch.ElapsedMilliseconds > MaximumLayoutMilliseconds)
                {
                    Assert.Fail("cửa sổ hub không có layout sau 60 khung và 5 giây — test UI phải chạy không -nographics");
                }
                yield return null;
            }
            yield return null;
            yield return null;
        }
    }
}
