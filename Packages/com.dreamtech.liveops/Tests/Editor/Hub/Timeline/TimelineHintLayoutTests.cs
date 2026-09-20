using System;
using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// (W9-29 + W9-25 chỗ #2 và #7) Ma trận BỐ CỤC của dòng gợi ý đáy trục và của nhãn tầng tháng trên thước — dựng đúng
    /// những trạng thái DỮ LIỆU XẤU NHẤT mà ma trận cổng W9 chưa bao giờ dựng tới, nên ba lỗi ảnh của đợt trước lọt lưới.
    /// <para>
    /// Vì sao phải mở cửa sổ hub THẬT chứ không dùng <see cref="TimelineTestPanel"/> cho phần gợi ý: trong panel thử, trục
    /// chiếm trọn bề rộng cửa sổ (700px ⇒ trục 700px), còn trong hub thật trục còn 384px ở cùng cỡ (rail + đệm màn). Câu
    /// gợi ý "đợt bị bỏ" cần 402px (vi) / 502px (en) — nó vừa ở trục 700 và KHÔNG vừa ở trục 384. Đo trong panel là đo một
    /// cửa sổ không có thật và không lỗi nào hiện ra.
    /// </para>
    /// <para>
    /// Bốn biến thể dữ liệu: NGHỈ (không chọn gì), đợt BỊ BỎ (câu dài nhất — cộng thêm vế phát hiện rồi mới tới ⌘⌫),
    /// đợt cố định sắp diễn ra (câu có ⌘D), và ID DÀI NHẤT (54 ký tự — id đẩy phần mô tả ra khỏi hàng nếu nó không co).
    /// Hai biến thể sau cùng chính là hai chỗ mà bản trước mất cụm phím.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class TimelineHintLayoutTests
    {
        /// <summary>Id đợt dài nhất của mẫu xấu nhất: 54 ký tự, dài hơn mọi id thật trong <see cref="LiveOpsDesignSample"/>.</summary>
        private const string LongEventId = "treasure-hunt-autumn-festival-2026-week-38-bonus-round";

        private const string LongEntryKey = "entry-" + LongEventId;

        /// <summary>Sai số một pixel của Yoga khi so hình học đã qua layout; phép đo chữ lệch nhiều hơn nên dùng riêng.</summary>
        private const float LayoutTolerance = 1f;

        /// <summary>
        /// Sai số RIÊNG cho nhánh đo chữ: nhãn đo (Label song sinh) và nhãn thật đi qua cùng bộ chữ nhưng khác lượt làm tròn
        /// theo pixelsPerPoint, nên 3px là nhiễu của phép đo chứ không phải chữ người dùng đọc thiếu.
        /// </summary>
        private const float TextMeasureTolerance = 3f;

        /// <summary>
        /// Trần chiều cao của dòng gợi ý: bốn hàng chữ 10px. Dòng gợi ý được phép CAO THÊM để giữ cụm phím (W9-29), nhưng
        /// không được phép nuốt trục — vượt trần này nghĩa là đã chữa lỗi cắt chữ bằng cách lấy hết chỗ của làn.
        /// </summary>
        private const float MaximumHintHeight = 56f;

        /// <summary>
        /// Bảy cỡ của ma trận kiểm bố cục (giống <c>UxHubWindowFixture.AllLayoutSizes</c>, chép lại để test này không phụ
        /// thuộc thứ tự mảng của cổng — cỡ ở đây là dữ liệu của gói, không phải hợp đồng chỉ mục).
        /// </summary>
        private static readonly UxWindowSize[] SizeMatrix =
        {
            new UxWindowSize(700, 560),
            new UxWindowSize(820, 560),
            new UxWindowSize(950, 700),
            new UxWindowSize(1024, 700),
            new UxWindowSize(1280, 760),
            new UxWindowSize(1440, 900),
            new UxWindowSize(1920, 1040),
        };

        private static readonly LiveOpsHubLanguageId[] Languages = { LiveOpsHubLanguageId.Vietnamese, LiveOpsHubLanguageId.English };

        private UxHubWindowFixture _fixture;
        private TimelineTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _fixture?.Dispose();
            _fixture = null;
            _panel?.Dispose();
            _panel = null;
        }

        // ================================================================================================ W9-29 dòng gợi ý

        /// <summary>
        /// (W9-29) Dòng gợi ý phải đọc được TRỌN VẸN ở cả bảy cỡ, cả hai ngôn ngữ, ở cả bốn biến thể dữ liệu — kể cả biến
        /// thể DÀI NHẤT. Trước gói này, câu "đợt bị bỏ" bị ellipsis cắt từ cuối ở bốn cỡ, và thứ đứng cuối câu đúng là cụm
        /// phím ⌘⌫ / ⌘D — thứ mà dòng gợi ý là chỗ DUY NHẤT trong hub nói ra.
        /// </summary>
        [UnityTest]
        public IEnumerator HintLine_ReadsWhole_AtEverySize_EveryWorstCaseVariant_BothLanguages()
        {
            foreach (LiveOpsHubLanguageId language in Languages)
            {
                foreach (UxWindowSize size in SizeMatrix)
                {
                    yield return AssertHintVariant(language, size, HintVariant.Idle);
                    yield return AssertHintVariant(language, size, HintVariant.Dropped);
                    yield return AssertHintVariant(language, size, HintVariant.FixedUpcoming);
                    yield return AssertHintVariant(language, size, HintVariant.LongId);
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (W9-29) Cụm phím là vế CUỐI câu, nên nó là thứ đầu tiên mất khi câu bị cắt: khẳng định riêng rằng nhãn phím thật
        /// (đọc từ profile phím của Editor) vẫn nằm trong chữ đang hiển thị ở cỡ hẹp nhất. Không có binding thì không có vế
        /// nào để kiểm — <see cref="LiveOpsHubKeyLabels"/> trả "" và câu không in "⌘" cụt.
        /// </summary>
        [UnityTest]
        public IEnumerator HintLine_KeepsShortcutCluster_AtNarrowestSize_BothLanguages()
        {
            foreach (LiveOpsHubLanguageId language in Languages)
            {
                string duplicateKey = LiveOpsHubKeyLabels.For("Main Menu/Edit/Duplicate");
                string deleteKey = LiveOpsHubKeyLabels.For("Main Menu/Edit/Delete");

                yield return OpenHub(language, SizeMatrix[0], null);
                yield return SelectBar(LiveOpsDesignSample.HuntEarlyEntryKey);
                LiveOpsTimelineHintLine hint = HintLineOf(_fixture);
                if (duplicateKey.Length > 0)
                {
                    StringAssert.Contains(duplicateKey + " " + LiveOpsHubStrings.TimelineHintDuplicate, hint.Description.text,
                        language + ": câu gợi ý của đợt cố định phải còn cụm phím nhân bản ở cỡ hẹp nhất");
                }
                CloseHub();

                yield return OpenHub(language, SizeMatrix[0], null);
                yield return SelectBar(LiveOpsDesignSample.HuntBonusEntryKey);
                hint = HintLineOf(_fixture);
                if (deleteKey.Length > 0)
                {
                    StringAssert.Contains(deleteKey + " " + LiveOpsHubStrings.TimelineHintDelete, hint.Description.text,
                        language + ": câu gợi ý của đợt bị bỏ phải còn cụm phím xoá ở cỡ hẹp nhất");
                }
                CloseHub();
            }
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ W9-25 chỗ #7 nhãn tháng

        /// <summary>
        /// (W9-25 chỗ #7) Nhãn tầng tháng ("THÁNG 11 2026") ôm khít ô của nó — ô tự co theo chữ nên chỉ hụt 1px là cắt, và
        /// một mốc tháng bị cắt đọc ra "THÁNG 3 20", một mốc KHÔNG có thật. Quét bảy cỡ × hai ngôn ngữ ở mức thu nhỏ nhất
        /// với khoảng đang xem bắc qua ranh giới tháng 11 — đúng khoảng mà nhãn dài nhất của bản tiếng Việt xuất hiện.
        /// </summary>
        [UnityTest]
        public IEnumerator RulerMonthLabels_FitTheirBox_AtEverySize_BothLanguages()
        {
            DateTime rangeStartUtc = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
            foreach (LiveOpsHubLanguageId language in Languages)
            {
                using (LiveOpsHubLanguage.Override(language))
                {
                    foreach (UxWindowSize size in SizeMatrix)
                    {
                        yield return OpenPanel(size, LiveOpsTimelineZoom.Month, rangeStartUtc);
                        IReadOnlyList<Label> months = _panel.Harness.Element.Ruler.VisibleMonthLabels;
                        Assert.Greater(months.Count, 0, language + " " + size + ": tiền đề thước có nhãn tầng tháng");
                        bool sawMonthMark = false;
                        for (int index = 0; index < months.Count; index++)
                        {
                            yield return AssertLabelReadsWhole(months[index], _panel.Harness.Element,
                                language + " " + size + " nhãn thước");
                            if (months[index].ClassListContains(LiveOpsHubClassNames.TimelineRulerLabelEmphasized)) sawMonthMark = true;
                        }
                        Assert.IsTrue(sawMonthMark,
                            language + " " + size + ": tiền đề khoảng đang xem phải bắc qua một mốc THÁNG (nhãn nhấn mạnh)");
                        ClosePanel();
                    }
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        // ================================================================================================ phụ trợ

        private enum HintVariant
        {
            Idle,
            Dropped,
            FixedUpcoming,
            LongId,
        }

        private IEnumerator AssertHintVariant(LiveOpsHubLanguageId language, UxWindowSize size, HintVariant variant)
        {
            string context = language + " " + size + " " + variant;
            yield return OpenHub(language, size, variant == HintVariant.LongId ? LongIdServices() : null);
            switch (variant)
            {
                case HintVariant.Idle:
                    break;
                case HintVariant.Dropped:
                    yield return SelectBar(LiveOpsDesignSample.HuntBonusEntryKey);
                    break;
                case HintVariant.FixedUpcoming:
                    yield return SelectBar(LiveOpsDesignSample.HuntEarlyEntryKey);
                    break;
                default:
                    yield return SelectBar(LongEntryKey);
                    break;
            }

            LiveOpsTimelineElement timeline = TimelineOf(_fixture);
            LiveOpsTimelineHintLine hint = timeline.HintLine;
            Assert.Greater(hint.Description.text.Length, 0, context + ": dòng gợi ý phải có câu mô tả");

            yield return AssertLabelReadsWhole(hint.Description, timeline, context + " mô tả");
            if (hint.Subject.text.Length > 0) yield return AssertLabelReadsWhole(hint.Subject, timeline, context + " chủ ngữ");

            Assert.LessOrEqual(hint.layout.height, MaximumHintHeight,
                context + ": dòng gợi ý cao " + hint.layout.height + "px — được phép gập để giữ cụm phím nhưng không được nuốt trục");
            Assert.GreaterOrEqual(timeline.Body.layout.height,
                LiveOpsTimelineGeometry.LanePaddingTop + LiveOpsTimelineGeometry.RowPitch - LayoutTolerance,
                context + ": dòng gợi ý gập xuống không được lấy mất chỗ của làn đầu tiên");
            CloseHub();
        }

        /// <summary>
        /// Nhãn đọc được TRỌN VẸN: (1) mẩu chữ liền KHÔNG ngắt được (từ dài nhất, gạch nối tính là chỗ ngắt) phải vừa bề
        /// rộng ô — nếu không thì dù có gập vẫn tràn ngang; (2) chữ đã gập phải vừa CHIỀU CAO ô. Hai vế cùng bắt buộc: vế
        /// một mình bắt được "chữ xuống dòng nhưng ô không cao thêm", vế hai mình bắt được "ô đủ cao nhưng một từ dài hơn ô".
        /// </summary>
        private IEnumerator AssertLabelReadsWhole(Label label, VisualElement host, string context)
        {
            Rect content = label.contentRect;
            Assert.Greater(content.width, 0f, context + ": nhãn phải có bề rộng thật (test này phải chạy KHÔNG -nographics)");

            Label runProbe = CreateProbe(host, label, LongestUnbreakableRun(label.text), float.NaN);
            Label wrapProbe = CreateProbe(host, label, label.text, content.width);
            yield return UxEventSender.WaitUntil(() => runProbe.layout.width > 0f && wrapProbe.layout.height > 0f,
                context + ": nhãn đo chưa có layout");
            float runWidth = runProbe.layout.width;
            float wrappedHeight = wrapProbe.layout.height;
            runProbe.RemoveFromHierarchy();
            wrapProbe.RemoveFromHierarchy();

            Assert.LessOrEqual(runWidth, content.width + TextMeasureTolerance,
                context + ": mẩu chữ liền dài nhất của \"" + label.text + "\" cần " + runWidth + "px mà ô chỉ rộng "
                + content.width + "px — gập cũng không cứu được, chữ sẽ tràn ngang rồi bị cắt");
            Assert.LessOrEqual(wrappedHeight, content.height + TextMeasureTolerance,
                context + ": chữ \"" + label.text + "\" gập lại cần " + wrappedHeight + "px chiều cao mà ô chỉ cao "
                + content.height + "px — phần cuối chữ bị cắt mất");
        }

        /// <summary>
        /// Mẩu chữ liền KHÔNG ngắt được dài nhất. Chỗ ngắt = khoảng trắng và NGAY SAU dấu gạch nối (UI Toolkit ngắt được ở
        /// gạch nối, nên "treasure-hunt-…" gập thật chứ không tràn). Trả "" khi chuỗi rỗng.
        /// </summary>
        internal static string LongestUnbreakableRun(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string longest = string.Empty;
            int runStart = 0;
            for (int index = 0; index < text.Length; index++)
            {
                char current = text[index];
                bool breakAfter = current == '-';
                bool breakBefore = char.IsWhiteSpace(current);
                if (!breakAfter && !breakBefore) continue;
                int runEnd = breakBefore ? index : index + 1;
                if (runEnd - runStart > longest.Length) longest = text.Substring(runStart, runEnd - runStart);
                runStart = breakBefore ? index + 1 : index + 1;
            }
            if (text.Length - runStart > longest.Length) longest = text.Substring(runStart);
            return longest;
        }

        /// <summary>
        /// Label song sinh: cùng class nên cùng bộ chữ và cùng cỡ, đặt tuyệt đối ngoài luồng layout. <paramref name="width"/>
        /// NaN = đo bề rộng tự nhiên một hàng; có số = ép đúng bề rộng ô rồi đọc chiều cao đã gập.
        /// </summary>
        private static Label CreateProbe(VisualElement host, Label source, string text, float width)
        {
            Label probe = new Label(text) { pickingMode = PickingMode.Ignore };
            foreach (string className in source.GetClasses()) probe.AddToClassList(className);
            probe.style.position = Position.Absolute;
            probe.style.left = 0f;
            probe.style.top = 0f;
            // Nhãn nguồn có thể là absolute căng theo cha (nhãn thước dùng top:0/bottom:0): không gỡ hai mép kia thì nhãn
            // đo cao bằng cả khung và phép đo vô nghĩa.
            probe.style.right = StyleKeyword.Auto;
            probe.style.bottom = StyleKeyword.Auto;
            probe.style.minWidth = 0f;
            probe.style.maxWidth = StyleKeyword.None;
            probe.style.maxHeight = StyleKeyword.None;
            probe.style.height = StyleKeyword.Auto;
            if (float.IsNaN(width))
            {
                probe.style.whiteSpace = WhiteSpace.NoWrap;
            }
            else
            {
                probe.style.whiteSpace = WhiteSpace.Normal;
                probe.style.width = width;
            }
            host.Add(probe);
            return probe;
        }

        private static LiveOpsTimelineElement TimelineOf(UxHubWindowFixture fixture)
        {
            LiveOpsTimelineElement timeline = fixture.Root.Q<LiveOpsTimelineElement>();
            Assert.IsNotNull(timeline, "màn Lịch phải có control timeline");
            return timeline;
        }

        private static LiveOpsTimelineHintLine HintLineOf(UxHubWindowFixture fixture)
        {
            return TimelineOf(fixture).HintLine;
        }

        private IEnumerator OpenHub(LiveOpsHubLanguageId language, UxWindowSize size, LiveOpsHubServices services)
        {
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, size, language, services);
            yield return _fixture.WaitForLayout();
        }

        private void CloseHub()
        {
            _fixture?.Dispose();
            _fixture = null;
        }

        private IEnumerator SelectBar(string entryKey)
        {
            LiveOpsTimelineBar bar = _fixture.BarOf(entryKey);
            Assert.IsNotNull(bar, "tiền đề: trục phải vẽ thanh '" + entryKey + "'");
            yield return UxEventSender.Click(_fixture.Window, bar);
            yield return UxEventSender.Settle(UxEventSender.SettleFrames * 2, UxEventSender.SettleMilliseconds * 2);
        }

        /// <summary>Mẫu thiết kế + MỘT đợt id dài nhất — không sửa <see cref="LiveOpsDesignSample"/> (JSON đã đăng ghim theo mẫu cũ).</summary>
        private static LiveOpsHubServices LongIdServices()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(document.RemoteConfigKey);
            for (int index = 0; index < document.EventTypes.Count; index++) builder.WithEventType(document.EventTypes[index]);
            for (int index = 0; index < document.RecurringRules.Count; index++) builder.WithRecurringRule(document.RecurringRules[index]);
            for (int index = 0; index < document.FixedEvents.Count; index++) builder.WithFixedEvent(document.FixedEvents[index]);
            builder.WithFixedEvent(new FixedLiveEventEntry(LongEntryKey, LongEventId, "star-tournament",
                "2026-09-15T00:00:00Z", "2026-09-19T00:00:00Z", "star_tournament_v1"));
            for (int index = 0; index < document.PublishedStamps.Count; index++) builder.WithPublishedStamp(document.PublishedStamps[index]);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(new ManualLiveOpsClock(LiveOpsDesignSample.NowUtc, true))
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(builder.Build())));
            services.Session.RunCheckToCompletion();
            return services;
        }

        private IEnumerator OpenPanel(UxWindowSize size, LiveOpsTimelineZoom zoom, DateTime rangeStartUtc)
        {
            _panel = TimelineTestPanel.Open(size.Width, size.Height);
            VisualElement root = _panel.CreateRoot(false);
            TimelineHarness harness = TimelineHarness.Create(root, TimelineViewInputs.Checked(LiveOpsDesignSample.Document));
            harness.Start(rangeStartUtc, zoom);
            _panel.Harness = harness;
            yield return harness.WaitReady();
        }

        private void ClosePanel()
        {
            _panel?.Dispose();
            _panel = null;
        }
    }
}
