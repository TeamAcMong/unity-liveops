using System;
using System.Collections.Generic;
using System.IO;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng CalendarDepth (G-CALENDAR-DEPTH, W5) của ma trận 9.5:
    /// <list type="bullet">
    /// <item><c>h11-calendar-compare-pane</c>: pane "So với đã đăng" thay chỗ inspector ngay sau khi kéo mép cuối [SD1 §3.4].</item>
    /// <item><c>h12-frame-02</c>: khung 2 của Hình 12 — thanh <c>--hover</c> có tay nắm và hover card 260px.</item>
    /// <item><c>h13-calendar-narrow-drawer</c>: cửa sổ 820×560 — rail 36px, inspector thành drawer [SD1 §3.9].</item>
    /// <item><c>h28f-calendar-compare-disk</c> (V-13): cùng pane nhưng nguồn so là BẢN TRÊN ĐĨA.</item>
    /// </list>
    /// Năm menu chuột phải KHÔNG có ảnh: menu gốc của Unity không chụp được (S-24) — nội dung khoá bằng
    /// <see cref="CalendarContextMenusTests"/>.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>Bề rộng pane So với [SD1 §3.4] — đúng 280 như inspector, vì hai pane thay chỗ nhau.</summary>
        private const float CalendarComparePaneWidth = 280f;

        /// <summary>Hình 13: cửa sổ 820×560 nằm dưới <c>NarrowBelowWidth</c> = 900 nên rail phải là 36px (I-8 của G-SHELLPOLISH).</summary>
        private const int CalendarNarrowWidth = 820;
        private const int CalendarNarrowHeight = 560;

        /// <summary>Rail hẹp [SD1 §3.9] — I-8 của G-SHELLPOLISH; ảnh h13 là chỗ duy nhất của W5 chụp nó ở 820px.</summary>
        private const float CalendarNarrowRailWidth = 36f;

        /// <summary>
        /// Hình 11: kéo mép cuối lava-quest-2026-09b từ 19/9 sang 20/9. Lịch mẫu ĐÃ khép đợt này ở 20/9, nên nháp trước khi kéo
        /// phải được đặt về 19/9 — không thì "lệnh sửa" ghi lại đúng giá trị cũ và cả ảnh lẫn trạng thái phiên không đổi gì.
        /// </summary>
        private const string CalendarCompareBeforeEndUtcText = "2026-09-19T00:00:00Z";
        private const string CalendarCompareMovedEndUtcText = "2026-09-20T00:00:00Z";

        /// <summary>Bản trên ĐĨA của h28f: "người khác" đã kéo đợt đó tới 21/9 bằng git pull, khác cả nháp lẫn bản đã đăng.</summary>
        private const string CalendarCompareDiskEndUtcText = "2026-09-21T00:00:00Z";

        private const string CalendarCompareDiskAssetFileName = "CaptureCalendarCompareDisk.asset";
        private const string MetaExtension = ".meta";

        static partial void RegisterCalendarDepth(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H11CalendarComparePane, CalendarWidth,
                    CalendarHeight, OpenCalendarComparePane)
                .WithExpectedFrames(CalendarComparePaneFrame()));

            // Khung 2 khai THÊM một khung đo: hover card 260px. Không có dòng này thì "MEASURE OK" chỉ nói canvas 803×420 và làn
            // 635 — đúng cả khi thẻ vẽ không có sheet của màn Lịch và tự co theo chữ (PD-36).
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H12Frame02, CalendarWidth, CalendarHeight,
                    () => OpenCalendarFrame(LiveOpsDesignSample.Document, PoseCalendarFrameHover),
                    window => window.rootVisualElement.Q(CalendarFrameCanvasElementName))
                .WithMinimumSettleFrames(CalendarFrameSettleFrames)
                .WithExpectedFrames(
                    new LiveOpsHubCaptureExpectedFrame(CalendarFrameCanvasElementName, CalendarFrameCanvasWidth, CalendarFrameCanvasHeight),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.TimelineLane, CalendarFrameLaneWidth, 0f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.CalendarDepthElementNames.HoverCard,
                        CalendarHoverCardContent.CardWidth, 0f)));

            // Khung đo của Hình 13 là RAIL 36px, không phải drawer: drawer ở `--medium` có viền trái 1px và nằm sát mép phải
            // cửa sổ, nên measure-capture.py dò cạnh ra 282px trong khi worldBound đúng 280.0 — giới hạn của công cụ, không
            // phải lệch layout. Bề rộng drawer khoá bằng CalendarDepthTests thay vì bằng ảnh.
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H13CalendarNarrowDrawer, CalendarNarrowWidth,
                    CalendarNarrowHeight, OpenCalendarNarrowDrawer)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.ShellElementNames.Rail,
                    CalendarNarrowRailWidth, 0f)));

            // Dọn SAU khi chụp: xung đột đĩa sống bằng chính file asset. Xoá file lúc dựng trạng thái thì trong lúc lượt chụp chờ
            // layout, Asset Pipeline gọi lại postprocessor với đường dẫn không còn file, phiên đọc "đĩa == nháp" và tự GỠ xung đột
            // — pane lặng lẽ rơi về nguồn Published và ảnh h28f trùng h11.
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28fCalendarCompareDisk, CalendarWidth,
                    CalendarHeight, OpenCalendarCompareDisk)
                .WithExpectedFrames(CalendarComparePaneFrame())
                .WithCleanup(EraseCaptureAssetFiles));
        }

        /// <summary>
        /// Ảnh đo CHÍNH thứ nó sinh ra (PD-36): pane So với rộng đúng 280px. Không khai khung này thì "MEASURE OK" chỉ chứng minh
        /// cái vỏ cửa sổ, còn pane có co giãn theo nội dung hay không thì không ai biết.
        /// </summary>
        private static LiveOpsHubCaptureExpectedFrame CalendarComparePaneFrame()
        {
            return new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.CalendarDepthElementNames.ComparePane,
                CalendarComparePaneWidth, 0f);
        }

        /// <summary>
        /// Hình 11: nháp đã kéo mép cuối 09b sang 20/9, pane So với MỞ. Sửa qua chính phiên (không nặn tay tài liệu) để dải chip
        /// nháp, status bar và badge rail trong ảnh là hệ quả thật của thao tác đó.
        /// </summary>
        private static EditorWindow OpenCalendarComparePane()
        {
            LiveOpsHubServices services = CalendarCompareServices(
                LiveOpsHubTestServices.CreateMemoryAsset(CalendarCompareBeforeDragDocument()));
            LiveOpsHubWindow window = OpenCalendarWindow(services, out CalendarSection calendar);
            // Kéo TRƯỚC khi bật pane: chip nháp, toast, bước Undo và badge của Hình 11 đều là hệ quả của cú kéo, nên cú kéo phải
            // chạy trên cửa sổ đã dựng — làm trước khi mở cửa sổ thì không có toast nào để chụp.
            DragLavaQuestEnd(calendar);
            OpenComparePane(calendar, LiveOpsDesignSample.LavaQuestMidEntryKey);
            RequireUnsavedDraft(services);
            return window;
        }

        /// <summary>
        /// (V-13) Hình 28 khung 4 → "Xem khác biệt": cùng pane nhưng nguồn so là bản trên ĐĨA. Xung đột dựng bằng một asset thật
        /// rồi sửa file ngoài Unity — nguồn Disk chỉ tồn tại khi phiên thật sự có <c>DiskConflict</c>, nặn tay sẽ chụp một pane
        /// mà người dùng không bao giờ thấy.
        /// </summary>
        private static EditorWindow OpenCalendarCompareDisk()
        {
            LiveOpsHubServices services = CalendarCompareServices(LiveOpsHubTestServices.CreateAssetFile(
                CalendarCompareDiskAssetFileName, CalendarCompareBeforeDragDocument()));
            LiveOpsHubWindow window = OpenCalendarWindow(services, out CalendarSection calendar);
            DragLavaQuestEnd(calendar);
            RequireUnsavedDraft(services);

            // Xung đột đĩa chỉ sinh ra khi CÒN nháp chưa lưu VÀ bản trên đĩa khác nháp: sửa thẳng file bằng một giờ thứ ba (21/9)
            // là mô phỏng đúng "git pull kéo về bản của người khác", và giữ YAML hợp lệ để phiên đọc lại được thành tài liệu.
            string assetPath = services.Session.AssetPath;
            EditAssetFileOnDisk(assetPath);
            services.Session.HandleAssetsChanged(new[] { assetPath }, null, null, null);
            services.Session.Publish.SelectCompareSource(LiveOpsHubCompareSource.Disk);

            OpenComparePane(calendar, LiveOpsDesignSample.LavaQuestMidEntryKey);
            RequireCompareSource(services, LiveOpsHubCompareSource.Disk);
            RequireComparePaneSaysDisk(calendar);
            return window;
        }

        /// <summary>Lịch mẫu với lava-quest-2026-09b khép ở 19/9 — trạng thái NGAY TRƯỚC cú kéo của Hình 11.</summary>
        private static LiveEventCalendarDocument CalendarCompareBeforeDragDocument()
        {
            return TimelineViewInputs.WithFixedTimes(LiveOpsDesignSample.Document, LiveOpsDesignSample.LavaQuestMidEntryKey,
                LiveOpsDesignSample.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                    out FixedLiveEventEntry entry) ? entry.StartUtcText : string.Empty, CalendarCompareBeforeEndUtcText);
        }

        private static LiveOpsHubServices CalendarCompareServices(LiveEventCalendarAsset asset)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock()).WithCalendarAsset(asset));
            services.Session.RunCheckToCompletion();
            return services;
        }

        private static LiveOpsHubWindow OpenCalendarWindow(LiveOpsHubServices services, out CalendarSection calendar)
        {
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            LiveOpsHubWindow window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
            window.position = new Rect(0f, 0f, CalendarWidth, CalendarHeight);
            window.HubRoot?.AddToClassList(LiveOpsHubClassNames.NoMotion);
            calendar = CalendarOf(sections);
            return window;
        }

        /// <summary>
        /// Cú kéo mép cuối của Hình 11 đi đúng đường người dùng: hai ý định Preview → Commit qua presenter của màn. Presenter mở
        /// một Undo group, đặt tên bước, phát toast qua bus của cửa sổ và để lại nháp chưa lưu — bốn thứ mà ảnh phải cho thấy.
        /// </summary>
        private static void DragLavaQuestEnd(CalendarSection calendar)
        {
            if (!calendar.Services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                    out FixedLiveEventEntry entry) || !entry.TryGetStartUtc(out DateTime startUtc))
            {
                throw new InvalidOperationException("lịch mẫu thiếu đợt " + LiveOpsDesignSample.LavaQuestMidEntryKey);
            }
            DateTime movedEndUtc = LiveEventUtcText.TryParse(CalendarCompareMovedEndUtcText, out DateTime parsedEndUtc)
                ? parsedEndUtc
                : startUtc;
            calendar.Presenter.SetSelectedBarKey(LiveOpsDesignSample.LavaQuestMidEntryKey);
            calendar.Presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, startUtc, movedEndUtc,
                LiveOpsTimelineGesturePhase.Preview));
            calendar.Presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, startUtc, movedEndUtc,
                LiveOpsTimelineGesturePhase.Commit));
        }

        private static void OpenComparePane(CalendarSection calendar, string selectedBarKey)
        {
            calendar.Presenter.SetSelectedBarKey(selectedBarKey);
            if (calendar.Toolbar == null) throw new InvalidOperationException("view màn Lịch chưa dựng — chưa có nút So với để bật");
            // Bật qua chính nút toolbar: ảnh phải cho thấy nút ở trạng thái BẬT, không chỉ pane đang mở.
            calendar.Toolbar.CompareToggle.value = true;
            if (!calendar.IsComparePaneOpen) throw new InvalidOperationException("bật nút So với mà pane không mở");
        }

        /// <summary>
        /// Sửa file asset NGOÀI Unity: đổi giờ kết thúc của lava-quest-2026-09b trong YAML. Ném khi không tìm thấy chuỗi giờ —
        /// một lần sửa trượt sẽ cho "bản đĩa" bằng đúng nháp, và cả h28f rơi êm về nguồn Published mà không ai biết.
        /// </summary>
        private static void EditAssetFileOnDisk(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            string text = File.ReadAllText(fullPath);
            if (text.IndexOf(CalendarCompareBeforeEndUtcText, StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("file asset không chứa giờ " + CalendarCompareBeforeEndUtcText
                    + " — không dựng được xung đột đĩa");
            }
            File.WriteAllText(fullPath, text.Replace(CalendarCompareBeforeEndUtcText, CalendarCompareDiskEndUtcText));
        }

        /// <summary>
        /// Hình 11 đòi chip nháp dạng (a) ("Chưa lưu · ⌘S"), thứ chỉ có khi phiên THẬT SỰ còn thay đổi chưa lưu. Ném thay vì chụp
        /// im lặng: một cú kéo không đổi gì cho ra đúng bức ảnh của bản chưa sửa, và khung đo 280px của pane không bắt được.
        /// </summary>
        private static void RequireUnsavedDraft(LiveOpsHubServices services)
        {
            if (services.Session.HasUnsavedChanges) return;
            throw new InvalidOperationException("kéo xong mà phiên không còn thay đổi chưa lưu — chip nháp sẽ sai dạng");
        }

        /// <summary>(V-13) Nguồn bản so phải là nguồn kịch bản chọn; rơi về Published thì ảnh h28f trùng h11 và vô nghĩa.</summary>
        private static void RequireCompareSource(LiveOpsHubServices services, LiveOpsHubCompareSource expected)
        {
            LiveOpsHubCompareSource actual = services.Session.Publish.ActiveCompareSource;
            if (actual == expected) return;
            throw new InvalidOperationException("nguồn bản so là " + actual + " chứ không phải " + expected);
        }

        /// <summary>
        /// Chốt cuối của V-13: pane ĐÃ VẼ phải nói đúng nguồn. Trạng thái phiên đúng mà pane vẽ bằng câu của nguồn Published là
        /// đúng thứ đã làm ảnh h28f của lượt trước trùng từng pixel với h11 mà không công cụ nào bắt được.
        /// </summary>
        private static void RequireComparePaneSaysDisk(CalendarSection calendar)
        {
            string title = calendar.ComparePane == null ? string.Empty : calendar.ComparePane.Title.text;
            string note = calendar.ComparePane == null ? string.Empty : calendar.ComparePane.Note.text;
            if (!string.Equals(note, LiveOpsHubStrings.CalendarDepthCompareDiskNote, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("pane So với vẽ chú thích của nguồn Published: " + note);
            }
            if (title.IndexOf(LiveOpsHubStrings.CalendarDepthCompareDiskTitleFormat.Substring(0, 12), StringComparison.Ordinal) != 0)
            {
                throw new InvalidOperationException("tiêu đề pane không phải của nguồn Disk: " + title);
            }
        }

        /// <summary>Hình 13: cửa sổ 820×560, đợt hunt-0916-bonus đang chọn nên inspector mở thành drawer phủ bên phải.</summary>
        private static EditorWindow OpenCalendarNarrowDrawer()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.RunCheckToCompletion();
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            LiveOpsHubWindow window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
            window.position = new Rect(0f, 0f, CalendarNarrowWidth, CalendarNarrowHeight);
            window.HubRoot?.AddToClassList(LiveOpsHubClassNames.NoMotion);
            CalendarOf(sections).Presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            return window;
        }

        /// <summary>
        /// Việc dọn của kịch bản h28f — chạy ở <c>WithCleanup</c>, tức SAU khi ảnh đã chụp và cửa sổ đã đóng: một asset để lại sẽ
        /// nằm trong <c>git status</c> của worktree và làm cổng quyền ghi đỏ. Xoá bằng <see cref="File"/> chứ không bằng
        /// <c>AssetDatabase.DeleteAsset</c>: DeleteAsset HUỶ luôn instance mà phiên đang giữ.
        /// </summary>
        private static void EraseCaptureAssetFiles()
        {
            EraseCaptureAssetFiles(LiveOpsHubTestServices.TestFolder + "/" + CalendarCompareDiskAssetFileName);
        }

        private static void EraseCaptureAssetFiles(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
            if (File.Exists(fullPath + MetaExtension)) File.Delete(fullPath + MetaExtension);
            string directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;
            if (Directory.GetFileSystemEntries(directory).Length > 0) return;
            Directory.Delete(directory);
            if (File.Exists(directory + MetaExtension)) File.Delete(directory + MetaExtension);
        }

        private static CalendarSection CalendarOf(IReadOnlyList<IHubSection> sections)
        {
            for (int index = 0; index < sections.Count; index++)
            {
                CalendarSection calendar = sections[index] as CalendarSection;
                if (calendar != null) return calendar;
            }
            throw new InvalidOperationException("registry của hub không có màn Lịch");
        }

        /// <summary>
        /// Hình 12 khung 2: hunt-0916-bonus ở trạng thái <c>--hover</c> (tay nắm 2×12, tag "bị bỏ" ẩn) kèm hover card 260px dựng
        /// từ CHÍNH <see cref="CalendarHoverCardContent"/>. Bắt chuột thật không chạy trong lượt chụp batchmode, nên tư thế đặt
        /// bằng API của element — cùng cách mọi khung Hình 12 khác đã dùng.
        /// </summary>
        private static void PoseCalendarFrameHover(LiveOpsTimelineElement timeline)
        {
            if (!ApplyCalendarFrameRange(timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc,
                CalendarFrameHuntLaneTypeId)) return;
            LiveOpsTimelineLane lane = timeline.FindLaneElement(CalendarFrameHuntLaneTypeId);
            LiveOpsTimelineBar bar = timeline.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            if (lane == null || bar == null) return;
            bar.SetInteractionState(true, false, false);
            lane.SetDroppedTagHidden(bar.Model.BarKey, true);
            AttachCalendarFrameHoverCard(timeline, bar);
        }

        /// <summary>
        /// Thẻ vẽ ngay trong khung ảnh, neo dưới thanh 4px như <c>LiveOpsHoverCardHost</c> đặt nó — host thật cần 500ms chuột
        /// đứng yên, thứ không có trong batchmode.
        /// </summary>
        private static void AttachCalendarFrameHoverCard(LiveOpsTimelineElement timeline, LiveOpsTimelineBar bar)
        {
            VisualElement canvas = timeline.parent;
            if (canvas == null) return;
            VisualElement existing = canvas.Q(LiveOpsHubPaths.CalendarDepthElementNames.HoverCard);
            existing?.RemoveFromHierarchy();

            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            // Khung 12 dựng trên canvas trần (không phải CalendarSection), nên sheet của màn Lịch chưa có ở đây. Mọi luật hình
            // dạng của thẻ (260px, hàng 17px, cột key 58px) nằm trong sheet đó — thiếu nó thì thẻ tự co theo chữ và ảnh khung 2
            // không so được với [SD1 §3.8]. Nạp sheet THẬT thay vì đặt bề rộng bằng tay: bề rộng phải do USS quyết.
            StyleSheet calendarSheet = services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.CalendarSectionUss);
            if (calendarSheet != null && !canvas.styleSheets.Contains(calendarSheet)) canvas.styleSheets.Add(calendarSheet);

            VisualElement card = CalendarHoverCardContent.Build(bar.Model, FirstFindingFor(services, bar.Model.BarKey), services,
                services.Session.Document.LatestStamp);
            VisualElement host = new VisualElement();
            host.AddToClassList(LiveOpsHubClassNames.HoverCard);
            host.AddToClassList(LiveOpsHubClassNames.HoverCardVisible);
            host.style.position = Position.Absolute;
            host.Add(card);
            canvas.Add(host);

            Rect barBound = bar.worldBound;
            Rect canvasBound = canvas.worldBound;
            if (float.IsNaN(barBound.x) || float.IsNaN(canvasBound.x)) return;
            host.style.left = barBound.x - canvasBound.x;
            host.style.top = barBound.yMax - canvasBound.y + LiveOpsHoverCardHost.CardOffset;
        }

        private static LiveEventCalendarFinding FirstFindingFor(LiveOpsHubServices services, string entryKey)
        {
            services.Session.RunCheckToCompletion();
            IReadOnlyList<LiveEventCalendarFinding> findings = CalendarHoverCardContent.FindingsInOrder(services.Session);
            for (int index = 0; index < findings.Count; index++)
            {
                if (string.Equals(findings[index].TargetEntryKey, entryKey, StringComparison.Ordinal)) return findings[index];
            }
            return null;
        }
    }
}
