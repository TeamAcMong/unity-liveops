using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DreamTech.LiveOps.Tests;
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

        /// <summary>Hover card [SD1 §3.8].</summary>
        private const float CalendarHoverCardWidth = 260f;

        /// <summary>Hình 13: cửa sổ 820×560 nằm dưới <c>NarrowBelowWidth</c> = 900 nên rail phải là 36px (I-8 của G-SHELLPOLISH).</summary>
        private const int CalendarNarrowWidth = 820;
        private const int CalendarNarrowHeight = 560;

        /// <summary>Rail hẹp [SD1 §3.9] — I-8 của G-SHELLPOLISH; ảnh h13 là chỗ duy nhất của W5 chụp nó ở 820px.</summary>
        private const float CalendarNarrowRailWidth = 36f;

        /// <summary>Hình 11: kéo mép cuối lava-quest-2026-09b từ 19/9 sang 20/9 lúc 08:46:50 — đúng tình huống của hình.</summary>
        private const string CalendarCompareMovedEndUtcText = "2026-09-20T00:00:00Z";

        private const string CalendarCompareDiskAssetFileName = "CaptureCalendarCompareDisk.asset";
        private const string CalendarCompareDiskMarkerComment = "# sửa tay ngoài Unity";
        private const string MetaExtension = ".meta";

        static partial void RegisterCalendarDepth(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H11CalendarComparePane, CalendarWidth,
                    CalendarHeight, OpenCalendarComparePane)
                .WithExpectedFrames(CalendarComparePaneFrame()));

            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame02, () => OpenCalendarFrame(
                LiveOpsDesignSample.Document, PoseCalendarFrameHover)));

            // Khung đo của Hình 13 là RAIL 36px, không phải drawer: drawer ở `--medium` có viền trái 1px và nằm sát mép phải
            // cửa sổ, nên measure-capture.py dò cạnh ra 282px trong khi worldBound đúng 280.0 — giới hạn của công cụ, không
            // phải lệch layout. Bề rộng drawer khoá bằng CalendarDepthTests thay vì bằng ảnh.
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H13CalendarNarrowDrawer, CalendarNarrowWidth,
                    CalendarNarrowHeight, OpenCalendarNarrowDrawer)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.ShellElementNames.Rail,
                    CalendarNarrowRailWidth, 0f)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28fCalendarCompareDisk, CalendarWidth,
                    CalendarHeight, OpenCalendarCompareDisk)
                .WithExpectedFrames(CalendarComparePaneFrame()));
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
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.RunCheckToCompletion();
            MoveLavaQuestEnd(services);
            return OpenCalendarWithComparePane(services, LiveOpsDesignSample.LavaQuestMidEntryKey, CalendarWidth, CalendarHeight);
        }

        /// <summary>
        /// (V-13) Hình 28 khung 4 → "Xem khác biệt": cùng pane nhưng nguồn so là bản trên ĐĨA. Xung đột dựng bằng một asset thật
        /// rồi sửa file ngoài Unity — nguồn Disk chỉ tồn tại khi phiên thật sự có <c>DiskConflict</c>, nặn tay sẽ chụp một pane
        /// mà người dùng không bao giờ thấy.
        /// </summary>
        private static EditorWindow OpenCalendarCompareDisk()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateAssetFile(CalendarCompareDiskAssetFileName,
                        LiveOpsDesignSample.Document)));
            services.Session.RunCheckToCompletion();
            MoveLavaQuestEnd(services);

            string assetPath = services.Session.AssetPath;
            File.AppendAllText(Path.GetFullPath(assetPath), Environment.NewLine + CalendarCompareDiskMarkerComment + Environment.NewLine);
            services.Session.HandleAssetsChanged(new[] { assetPath }, null, null, null);
            services.Session.Publish.SelectCompareSource(LiveOpsHubCompareSource.Disk);
            EraseCaptureAssetFiles(assetPath);

            return OpenCalendarWithComparePane(services, LiveOpsDesignSample.LavaQuestMidEntryKey, CalendarWidth, CalendarHeight);
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

        private static EditorWindow OpenCalendarWithComparePane(LiveOpsHubServices services, string selectedBarKey, int width,
            int height)
        {
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            LiveOpsHubWindow window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
            window.position = new Rect(0f, 0f, width, height);
            window.HubRoot?.AddToClassList(LiveOpsHubClassNames.NoMotion);
            CalendarSection calendar = CalendarOf(sections);
            calendar.Presenter.SetSelectedBarKey(selectedBarKey);
            // Bật qua chính nút toolbar: ảnh phải cho thấy nút ở trạng thái BẬT, không chỉ pane đang mở.
            if (calendar.Toolbar != null) calendar.Toolbar.CompareToggle.value = true;
            return window;
        }

        /// <summary>
        /// Xoá file asset tạm NGAY sau khi xung đột đã dựng xong: lượt chụp không có TearDown, nên một asset để lại sẽ nằm
        /// trong <c>git status</c> của worktree và làm cổng quyền ghi đỏ. Xoá bằng <see cref="File"/> chứ không bằng
        /// <c>AssetDatabase.DeleteAsset</c>: DeleteAsset HUỶ luôn instance mà phiên đang giữ, và màn Lịch sẽ vẽ trạng thái
        /// "chưa có asset" thay vì pane So với. Xung đột đã cầm sẵn cả ba tài liệu nên không ai đọc lại file nữa.
        /// </summary>
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

        private static void MoveLavaQuestEnd(LiveOpsHubServices services)
        {
            if (!services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                    out FixedLiveEventEntry entry)) return;
            services.Session.Apply(new ReplaceFixedEventEdit(entry.WithTimes(entry.StartUtcText, CalendarCompareMovedEndUtcText)),
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarMoveEndToastFormat, entry.EventId,
                    services.Format.ShortDateTime(new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc)),
                    services.Format.ShortDateTime(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc))));
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
            VisualElement card = CalendarHoverCardContent.Build(bar.Model, FirstFindingFor(services, bar.Model.BarKey), services,
                services.Session.Document.LatestStamp);
            VisualElement host = new VisualElement();
            host.AddToClassList(LiveOpsHubClassNames.HoverCard);
            host.AddToClassList(LiveOpsHubClassNames.HoverCardVisible);
            host.style.position = Position.Absolute;
            host.style.width = CalendarHoverCardWidth;
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
