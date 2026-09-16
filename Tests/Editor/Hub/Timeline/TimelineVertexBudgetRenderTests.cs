using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// R-10 đo bằng cách VẼ THẬT, không chỉ ước lượng: ở 2022.3 một <c>VisualElement</c> vượt 65.535 vertex thì mất hình và Unity log
    /// <c>Error</c> — test runner biến log đó thành fail, nên fixture này là nơi duy nhất bắt được "làn trống ở zoom xa". Ở 6000.6 không
    /// lỗi ở mọi cỡ, vì vậy **bắt buộc xanh trên 2022.3** (nghiệm thu 10.3).
    ///
    /// Lịch thử: dữ liệu mẫu + một loại cố định chạy 20 giờ MỖI NGÀY suốt một năm (365 thanh — vượt ngân sách một làn) cộng các luật lặp
    /// sẵn có, quét qua cả năm ở ba preset zoom và ở zoom liên tục nhỏ nhất (0,25 px/giờ). Model phải gom dải cho tới khi mỗi làn ≤
    /// <see cref="LiveOpsTimelineVertexBudget.LaneSafetyBudget"/>; view phải vẽ đúng chừng đó. Vẽ ép mỗi bước bằng
    /// <c>RepaintImmediately</c> — không ép thì panel gộp khung và test xanh giả trong batchmode.
    ///
    /// Element dựng trần từ <see cref="LiveOpsDesignSample"/> (W3-DEPS: không phiên, không <c>OpenWithServices</c>).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class TimelineVertexBudgetRenderTests
    {
        /// <summary>Quét cả năm theo bước 37 ngày: đủ để mọi khung rơi vào cả vùng dày (đợt hằng ngày) lẫn vùng thưa, mà không thành 365 lượt vẽ.</summary>
        private const int SweepStepDays = 37;

        private static readonly LiveOpsTimelineZoom[] Presets =
        {
            LiveOpsTimelineZoom.Day, LiveOpsTimelineZoom.ThreeWeeks, LiveOpsTimelineZoom.Month,
        };

        private TimelineTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        [UnityTest]
        public IEnumerator VertexBudget_OneYearAllTypes_EveryZoomAndMinScale_NoError()
        {
            LiveEventCalendarDocument document = TimelineViewInputs.WithDailyFixedYear(LiveOpsDesignSample.Document);
            _panel = TimelineTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            TimelineHarness harness = TimelineHarness.Create(root, TimelineViewInputs.Unchecked(document));
            _panel.Harness = harness;
            harness.Start(TimelineViewInputs.DailyYearStartUtc, LiveOpsTimelineZoom.ThreeWeeks);
            yield return harness.WaitReady();

            var failures = new List<string>();
            int drawnFrames = 0;

            foreach (LiveOpsTimelineZoom zoom in Presets)
            {
                for (int dayOffset = 0; dayOffset < 365; dayOffset += SweepStepDays)
                {
                    DateTime rangeStartUtc = TimelineViewInputs.DailyYearStartUtc.AddDays(dayOffset);
                    harness.Element.SetRange(rangeStartUtc, zoom);
                    yield return null;
                    _panel.RepaintImmediately();
                    CheckDrawnBudget(harness.Element, Describe(zoom.ToString(), rangeStartUtc), failures);
                    drawnFrames++;
                }
            }

            // Zoom liên tục nhỏ nhất (0,25 px/giờ): cả năm gần như nằm trong một khung — trường hợp làm mất hình ở 2022.3 nếu không gom dải.
            harness.Element.SetContinuousScale(LiveOpsTimelineGeometry.MinimumPixelsPerHour, TimelineViewInputs.DailyYearStartUtc);
            yield return null;
            _panel.RepaintImmediately();
            Assert.AreEqual(LiveOpsTimelineGeometry.MinimumPixelsPerHour, harness.Element.PixelsPerHour, 0.0001,
                "tiền đề: phải kẹp đúng 0,25 px/giờ để đo trường hợp xấu nhất");
            CheckDrawnBudget(harness.Element, Describe("0,25 px/giờ", TimelineViewInputs.DailyYearStartUtc), failures);
            drawnFrames++;

            // Và ở đầu kia của thang (48 px/giờ) để chắc rằng gom dải không phải là cái duy nhất giữ ngân sách.
            harness.Element.SetContinuousScale(LiveOpsTimelineGeometry.MaximumPixelsPerHour, LiveOpsDesignSample.NowUtc);
            yield return null;
            _panel.RepaintImmediately();
            CheckDrawnBudget(harness.Element, Describe("48 px/giờ", LiveOpsDesignSample.NowUtc), failures);
            drawnFrames++;

            Assert.IsEmpty(failures, "Làn/minimap vẽ quá ngân sách (2022.3 sẽ mất hình + log Error):\n" + string.Join("\n", failures));
            Assert.GreaterOrEqual(drawnFrames, 32, "phải quét đủ khung qua cả năm ở ba preset cộng hai đầu thang zoom");
            // Log Error của UIElements (nếu có) đã làm fail test từ trước; dòng này chặn cả Warning lạ sinh ra khi vẽ.
            LogAssert.NoUnexpectedReceived();
        }

        private static void CheckDrawnBudget(LiveOpsTimelineElement element, string context, List<string> failures)
        {
            Assert.IsNotNull(element.Model, context + ": presenter thử chưa dựng lại model sau khi đổi khoảng");
            for (int index = 0; index < element.LaneCount; index++)
            {
                LiveOpsTimelineLane lane = element.LaneAt(index);
                string laneName = lane.Model == null ? "(không có model)" : LaneNameOf(lane.Model.TypeId);
                if (lane.EstimatedVertexCount > LiveOpsTimelineVertexBudget.LaneSafetyBudget)
                {
                    failures.Add(context + " · làn " + laneName + ": " + lane.EstimatedVertexCount + " > ngân sách an toàn " +
                        LiveOpsTimelineVertexBudget.LaneSafetyBudget);
                }
                if (lane.EstimatedVertexCount > LiveOpsTimelineVertexBudget.MaximumVerticesPerElement)
                {
                    failures.Add(context + " · làn " + laneName + ": " + lane.EstimatedVertexCount + " > trần cứng của 2022.3 " +
                        LiveOpsTimelineVertexBudget.MaximumVerticesPerElement);
                }
            }
            if (element.Minimap.EstimatedVertexCount > LiveOpsTimelineVertexBudget.MaximumVerticesPerElement)
            {
                failures.Add(context + " · minimap: " + element.Minimap.EstimatedVertexCount + " > trần cứng của 2022.3");
            }
        }

        private static string LaneNameOf(string typeId)
        {
            return typeId.Length == 0 ? LiveOpsHubStrings.TimelineUntypedLaneName : typeId;
        }

        private static string Describe(string scale, DateTime rangeStartUtc)
        {
            return scale + " từ " + rangeStartUtc.ToString("d/M/yyyy", CultureInfo.InvariantCulture);
        }
    }
}
