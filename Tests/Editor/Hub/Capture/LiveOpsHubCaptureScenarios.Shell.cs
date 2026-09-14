using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Shell (G-SHELL, W2 → G-HOSTUI W4): Hình 2 (4 HealthState + họ giai đoạn), khung với màn giữ chỗ, Hình 28
    /// khung 1–3. Mọi kịch bản mở cửa sổ hub thật bằng <c>OpenForTest</c>; kịch bản thiếu UXML dùng loader giả đường dẫn, không đổi
    /// file trên đĩa (V-16).
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        internal const string StateMarksGalleryElementName = "capture-state-marks";
        private const int StandardWidth = 1280;
        private const int StandardHeight = 760;
        private const int StateMarksWidth = 640;
        private const int StateMarksHeight = 300;

        static partial void RegisterShell(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H02StateMarks, StandardWidth, StandardHeight, OpenStateMarksGallery,
                window => window.rootVisualElement.Q(StateMarksGalleryElementName)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HsShellSkeleton, StandardWidth, StandardHeight,
                () => LiveOpsHubWindow.OpenForTest(LiveOpsHubSections.Create(), new ManualLiveOpsHubCompilationState(false), null, LiveOpsHubSections.Ids.Overview)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28aFailureSection, StandardWidth, StandardHeight, OpenFailureSection));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28bMissingUxml, StandardWidth, StandardHeight,
                () => LiveOpsHubWindow.OpenForTest(LiveOpsHubSections.Create(), new ManualLiveOpsHubCompilationState(false),
                    new MissingPathsLiveOpsHubLayoutLoader(new[] { LiveOpsHubPaths.ShellUxml }), LiveOpsHubSections.Ids.Overview)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28cCompiling, StandardWidth, StandardHeight,
                () => LiveOpsHubWindow.OpenForTest(LiveOpsHubSections.Create(), new ManualLiveOpsHubCompilationState(true), null, LiveOpsHubSections.Ids.Calendar)));
        }

        /// <summary>
        /// Hình 28 khung 1 ([FD §4.1]): Tổng quan · Lịch (active, ném khi dựng) · Luật lặp Warning · Kiểm lịch Blocked. Exception ném thật
        /// từ màn giả để thân card là stack thật, không phải chữ dán.
        /// </summary>
        private static EditorWindow OpenFailureSection()
        {
            List<FakeHubSection> sections = FakeHubSection.CreateRegistryShaped();
            sections[2].CreateViewException = CreateThrownException();
            sections[3].Health = SectionHealth.Warning("1 mất tiến độ", "weekly-pass đổi tiền tố khi weekly-pass-35 đang chạy");
            sections[4].Health = SectionHealth.Blocked("2 bị bỏ", "2 đợt sẽ bị game bỏ khi đọc lịch");
            return LiveOpsHubWindow.OpenForTest(FakeHubSection.AsSections(sections), new ManualLiveOpsHubCompilationState(false), null,
                LiveOpsHubSections.Ids.Calendar);
        }

        private static Exception CreateThrownException()
        {
            try
            {
                object lane = null;
                return new NullReferenceException(lane.ToString());
            }
            catch (NullReferenceException exception)
            {
                return exception;
            }
        }

        /// <summary>
        /// Hình 2 ([FD §2.4]): bảng 640 px — cột HealthState 84 · Dấu 36 · Hàng tầng 168 · Dòng section 136 · Việc cần làm 200, hàng
        /// 36 px; dưới là hàng giai đoạn đợt. Dựng bằng đúng class của rail/hàng phát hiện trong cửa sổ hub (có token hai skin), ảnh cắt
        /// theo bảng. Kích thước hình học là style inline của code chụp — không phải UI sản phẩm.
        /// </summary>
        private static EditorWindow OpenStateMarksGallery()
        {
            FakeHubSection gallery = new FakeHubSection("state-marks", LiveOpsHubStrings.ShellWindowTitle, "Hình 2", PipelineStage.Configure)
            {
                ViewFactory = BuildStateMarksGallery,
                RequiredElementNames = new[] { StateMarksGalleryElementName },
            };
            return LiveOpsHubWindow.OpenForTest(new List<IHubSection> { gallery }, new ManualLiveOpsHubCompilationState(false), null, gallery.Id);
        }

        private static VisualElement BuildStateMarksGallery()
        {
            VisualElement table = new VisualElement { name = StateMarksGalleryElementName };
            table.style.width = StateMarksWidth;
            table.style.height = StateMarksHeight;
            table.style.flexShrink = 0;
            table.style.paddingLeft = 8;
            table.style.paddingRight = 8;
            table.style.paddingTop = 6;

            VisualElement headerRow = CreateTableRow(20f, false);
            string[] headers = { "HealthState", "Dấu", "Hàng tầng trên rail", "Dòng section", "Việc cần làm" };
            for (int column = 0; column < headers.Length; column++)
            {
                Label caption = new Label(headers[column]);
                caption.AddToClassList(LiveOpsHubClassNames.Caption);
                headerRow.Add(CreateCell(column, caption));
            }
            table.Add(headerRow);

            table.Add(CreateStateRow(HealthState.Blocked, "Blocked", LiveOpsHubStrings.StageCaptionCheck, "2 bị bỏ", LiveOpsHubStrings.ShellValidationTitle,
                "2 đợt sẽ bị game bỏ"));
            table.Add(CreateStateRow(HealthState.Warning, "Warning", LiveOpsHubStrings.StageCaptionSchedule, "1 mất tiến độ", LiveOpsHubStrings.ShellRecurringRulesTitle,
                "weekly-pass đổi tiền tố"));
            table.Add(CreateStateRow(HealthState.NotMeasured, "NotMeasured", LiveOpsHubStrings.StageCaptionRun, LiveOpsHubStrings.ShellRailNotMeasuredBadge, "Trực tiếp",
                "Không ở Play Mode"));
            table.Add(CreateStateRow(HealthState.Ok, "Ok", LiveOpsHubStrings.StageCaptionConfigure, string.Empty, LiveOpsHubStrings.ShellOverviewTitle, string.Empty));

            VisualElement phaseRow = CreateTableRow(36f, true);
            phaseRow.Add(CreatePhaseTag(LiveEventPhase.Active, false, "Đang chạy"));
            phaseRow.Add(CreatePhaseTag(LiveEventPhase.Upcoming, false, "Sắp tới"));
            phaseRow.Add(CreatePhaseTag(LiveEventPhase.Ended, false, "Đã khép"));
            phaseRow.Add(CreatePhaseTag(LiveEventPhase.Ended, true, "Chờ hiện kết quả"));
            table.Add(phaseRow);
            return table;
        }

        private static readonly float[] ColumnWidths = { 84f, 36f, 168f, 136f, 200f };

        private static VisualElement CreateStateRow(HealthState state, string stateName, string stageCaption, string badgeText, string sectionTitle, string needsAction)
        {
            VisualElement row = CreateTableRow(36f, true);

            Label name = new Label(stateName);
            LiveOpsHubStyle.SetStateText(name, state);
            row.Add(CreateCell(0, name));

            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.SetHealth(state);
            row.Add(CreateCell(1, mark));

            VisualElement stageRow = new VisualElement();
            stageRow.AddToClassList(LiveOpsHubClassNames.RailStageRow);
            stageRow.style.flexGrow = 1;
            VisualElement gutter = new VisualElement();
            gutter.AddToClassList(LiveOpsHubClassNames.Gutter);
            LiveOpsStateMark stageMark = new LiveOpsStateMark();
            stageMark.SetHealth(state);
            gutter.Add(stageMark);
            stageRow.Add(gutter);
            Label caption = new Label(stageCaption);
            caption.AddToClassList(LiveOpsHubClassNames.RailStageLabel);
            stageRow.Add(caption);
            if (!string.IsNullOrEmpty(badgeText))
            {
                Label badge = new Label(badgeText);
                badge.AddToClassList(LiveOpsHubClassNames.RailBadge);
                LiveOpsHubStyle.SetStateText(badge, state);
                stageRow.Add(badge);
            }
            row.Add(CreateCell(2, stageRow));

            VisualElement sectionRow = new VisualElement();
            sectionRow.AddToClassList(LiveOpsHubClassNames.RailRow);
            sectionRow.style.flexGrow = 1;
            Label sectionLabel = new Label(sectionTitle);
            sectionLabel.AddToClassList(LiveOpsHubClassNames.RailRowLabel);
            sectionLabel.style.marginLeft = 0;
            sectionRow.Add(sectionLabel);
            LiveOpsStateMark sectionMark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            sectionMark.SetHealth(state);
            sectionMark.AddToClassList(LiveOpsHubClassNames.RailRowMark);
            sectionMark.EnableInClassList(LiveOpsHubClassNames.RailRowMarkHidden, state == HealthState.Ok);
            sectionRow.Add(sectionMark);
            row.Add(CreateCell(3, sectionRow));

            VisualElement findingRow = new VisualElement();
            findingRow.AddToClassList(LiveOpsHubClassNames.FindingRow);
            findingRow.style.flexGrow = 1;
            if (state != HealthState.Ok)
            {
                LiveOpsHubStyle.SetSeverityStripe(findingRow, state);
                LiveOpsStateMark findingMark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
                findingMark.SetHealth(state);
                findingRow.Add(findingMark);
                Label finding = new Label(needsAction);
                finding.style.marginLeft = 6;
                findingRow.Add(finding);
            }
            else
            {
                Label none = new Label("không có hàng");
                none.AddToClassList(LiveOpsHubClassNames.TextQuiet);
                findingRow.Add(none);
            }
            row.Add(CreateCell(4, findingRow));
            return row;
        }

        private static VisualElement CreatePhaseTag(LiveEventPhase phase, bool isPendingResult, string text)
        {
            VisualElement tag = new VisualElement();
            tag.style.flexDirection = FlexDirection.Row;
            tag.style.alignItems = Align.Center;
            tag.style.marginRight = 16;
            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.SetPhase(phase, isPendingResult);
            tag.Add(mark);
            Label label = new Label(text);
            label.style.marginLeft = 6;
            tag.Add(label);
            return tag;
        }

        private static VisualElement CreateTableRow(float height, bool hasTopBorder)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = height;
            row.style.flexShrink = 0;
            if (hasTopBorder)
            {
                row.style.borderTopWidth = 1;
                row.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);
            }
            return row;
        }

        private static VisualElement CreateCell(int column, VisualElement content)
        {
            VisualElement cell = new VisualElement();
            cell.style.width = ColumnWidths[column];
            cell.style.flexShrink = 0;
            cell.style.flexDirection = FlexDirection.Row;
            cell.style.alignItems = Align.Center;
            cell.Add(content);
            return cell;
        }
    }
}
