using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Khớp của palette ⌘K ([FD §3.8]) + hành vi view palette không cần panel: bỏ dấu tiếng Việt, xếp hạng theo trường, câu gõ rỗng giữ thứ tự
    /// rail, rich text bọc noparse; mở chọn hàng đầu, mũi tên + Enter mở mục và đóng, ⌘K lần nữa đóng + mở Unity Search, không khớp nêu câu gõ.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class PaletteMatcherTests
    {
        private static LiveOpsPaletteMatcher.Entry Section(string title, string stage, string subtitle, string sectionId, SectionHealth health,
            string reason = "")
        {
            return new LiveOpsPaletteMatcher.Entry(title, stage, subtitle, string.Empty, sectionId, health, reason);
        }

        private static LiveOpsPaletteMatcher.Entry Rule(string ruleId)
        {
            return new LiveOpsPaletteMatcher.Entry(LiveOpsHubStrings.ShellValidationTitle, LiveOpsHubStrings.StageCaptionCheck, string.Empty, ruleId,
                LiveOpsHubSections.Ids.Validation, SectionHealth.Ok(), string.Empty);
        }

        /// <summary>Sáu màn P1 theo thứ tự rail (dữ liệu mẫu Hình 6) + hai id luật.</summary>
        private static List<LiveOpsPaletteMatcher.Entry> DesignEntries()
        {
            return new List<LiveOpsPaletteMatcher.Entry>
            {
                Section("Tổng quan", "CẤU HÌNH", "Lịch đang mở, việc cần làm", LiveOpsHubSections.Ids.Overview, SectionHealth.Ok()),
                Section("Loại event", "CẤU HÌNH", "Loại event game biết đọc", LiveOpsHubSections.Ids.EventTypes, SectionHealth.Ok()),
                Section("Lịch", "LÊN LỊCH", "Đợt cố định và lần lặp trên trục thời gian", LiveOpsHubSections.Ids.Calendar,
                    SectionHealth.Blocked("2 bị bỏ", "2 đợt sẽ bị game bỏ khi đọc lịch")),
                Section("Luật lặp", "LÊN LỊCH", "Luật sinh đợt theo chu kỳ", LiveOpsHubSections.Ids.RecurringRules,
                    SectionHealth.Warning("1 mất tiến độ", "weekly-pass đổi tiền tố khi weekly-pass-35 đang chạy")),
                Section("Kiểm lịch", "KIỂM", "Game sẽ làm gì với lịch", LiveOpsHubSections.Ids.Validation,
                    SectionHealth.Blocked("2 bị bỏ", "2 đợt sẽ bị game bỏ khi đọc lịch")),
                Section("Xuất JSON", "XUẤT", "JSON cho remote config", LiveOpsHubSections.Ids.Export, SectionHealth.Blocked("chặn", "Còn 2 đợt bị bỏ")),
                Rule("overlap-same-type"),
                Rule("duplicate-event-id"),
            };
        }

        [Test]
        public void PaletteMatcher_FoldsVietnameseDiacritics()
        {
            Assert.AreEqual("kiem lich", LiveOpsPaletteMatcher.Fold("Kiểm lịch"));
            Assert.AreEqual("duong di cua lich", LiveOpsPaletteMatcher.Fold("Đường đi của lịch"), "đ/Đ không tách dấu được bằng FormD — phải thay tay");
            Assert.AreEqual("xuat json", LiveOpsPaletteMatcher.Fold("XUẤT JSON"));
            // Chuỗi đã tách dấu sẵn (FormD, vd dán từ macOS) cho cùng kết quả.
            Assert.AreEqual("kiem", LiveOpsPaletteMatcher.Fold("Kiểm".Normalize(System.Text.NormalizationForm.FormD)));

            IReadOnlyList<LiveOpsPaletteMatcher.Match> kiem = LiveOpsPaletteMatcher.Find(DesignEntries(), "kiem");
            Assert.GreaterOrEqual(kiem.Count, 1);
            Assert.AreEqual(LiveOpsHubSections.Ids.Validation, kiem[0].Entry.SectionId, "gõ không dấu \"kiem\" tới Kiểm lịch");
            Assert.IsFalse(kiem[0].Entry.IsRule, "màn đứng trước các id luật cùng tiêu đề (thứ tự rail)");
            Assert.AreEqual("<b><noparse>Kiểm</noparse></b><noparse> lịch</noparse>", kiem[0].RichTitle, "in đậm đúng chữ có dấu đã khớp");

            List<LiveOpsPaletteMatcher.Entry> withD = new List<LiveOpsPaletteMatcher.Entry>
            {
                Section("Đánh dấu đã đăng", "XUẤT", string.Empty, LiveOpsHubSections.Ids.Export, SectionHealth.Ok()),
            };
            IReadOnlyList<LiveOpsPaletteMatcher.Match> byD = LiveOpsPaletteMatcher.Find(withD, "d");
            Assert.AreEqual(1, byD.Count, "\"d\" khớp \"Đ\"");
            Assert.IsTrue(byD[0].RichTitle.StartsWith("<b><noparse>Đ</noparse></b>", StringComparison.Ordinal), "bắt đầu bằng chuỗi mong đợi");
            Assert.AreEqual(1, LiveOpsPaletteMatcher.Find(withD, "DANH DAU").Count, "hoa/thường và khoảng trắng không quan trọng");
            Assert.AreEqual(1, LiveOpsPaletteMatcher.Find(withD, "đánh").Count, "gõ có dấu vẫn khớp");
        }

        [Test]
        public void PaletteMatcher_RanksTitleOverStageOverSubtitleOverRuleId()
        {
            // Mỗi mục chỉ khớp "abc" ở đúng một trường; đưa vào theo thứ tự ngược hạng để thứ tự ra không phải do thứ tự vào.
            List<LiveOpsPaletteMatcher.Entry> entries = new List<LiveOpsPaletteMatcher.Entry>
            {
                new LiveOpsPaletteMatcher.Entry("Mục bốn", "TẦNG", "không", "rule-abc", "four", SectionHealth.Ok(), string.Empty),
                new LiveOpsPaletteMatcher.Entry("Mục ba", "TẦNG", "có abc ở subtitle", string.Empty, "three", SectionHealth.Ok(), string.Empty),
                new LiveOpsPaletteMatcher.Entry("Mục hai", "ABC", "không", string.Empty, "two", SectionHealth.Ok(), string.Empty),
                new LiveOpsPaletteMatcher.Entry("a-rải-rác-b-rải-rác-c", "TẦNG", "không", string.Empty, "one", SectionHealth.Ok(), string.Empty),
            };
            IReadOnlyList<LiveOpsPaletteMatcher.Match> matches = LiveOpsPaletteMatcher.Find(entries, "abc");
            CollectionAssert.AreEqual(new[] { "one", "two", "three", "four" }, IdsOf(matches),
                "tiêu đề (kể cả khớp rải rác) > tầng > subtitle > id luật");
            Assert.AreEqual("<noparse>Mục bốn</noparse>", matches[3].RichTitle, "khớp ngoài tiêu đề thì tiêu đề không in đậm");

            // Cùng trường: khớp liền ở đầu từ đứng trước khớp rải rác, dù mục rải rác đứng trước trong rail.
            List<LiveOpsPaletteMatcher.Entry> gapped = new List<LiveOpsPaletteMatcher.Entry>
            {
                Section("Kho lịch về mẫu", "XUẤT", string.Empty, "gapped", SectionHealth.Ok()),
                Section("Kiểm lịch", "KIỂM", string.Empty, "word-start", SectionHealth.Ok()),
            };
            CollectionAssert.AreEqual(new[] { "word-start", "gapped" }, IdsOf(LiveOpsPaletteMatcher.Find(gapped, "kiem")),
                "\"kiem\" liền ở đầu từ lên trước \"k…i…e…m\" rải rác");
        }

        [Test]
        public void PaletteMatcher_EmptyQueryListsRailOrder()
        {
            List<LiveOpsPaletteMatcher.Entry> entries = DesignEntries();
            IReadOnlyList<LiveOpsPaletteMatcher.Match> all = LiveOpsPaletteMatcher.Find(entries, string.Empty);
            Assert.AreEqual(entries.Count, all.Count, "câu gõ rỗng liệt kê mọi mục");
            for (int index = 0; index < entries.Count; index++) Assert.AreSame(entries[index], all[index].Entry, "giữ thứ tự rail ở hàng " + index);
            Assert.AreEqual(entries.Count, LiveOpsPaletteMatcher.Find(entries, "   ").Count, "chỉ khoảng trắng = rỗng");
            Assert.AreEqual(0, LiveOpsPaletteMatcher.Find(entries, "xyz").Count, "không khớp thì rỗng — không đề xuất lệnh thay thế");
        }

        [Test]
        public void PaletteMatcher_RichTextEscapesRawText()
        {
            Assert.AreEqual("<noparse><color=red>x</color></noparse>", LiveOpsPaletteMatcher.Escape("<color=red>x</color>"));
            // Chữ thô chứa đúng thẻ đóng noparse không được thoát khỏi khối noparse.
            string escaped = LiveOpsPaletteMatcher.Escape("a</noparse><b>b");
            Assert.AreEqual("<noparse>a</</noparse><noparse>noparse><b>b</noparse>", escaped);
            Assert.AreEqual(string.Empty, LiveOpsPaletteMatcher.Escape(string.Empty));

            List<LiveOpsPaletteMatcher.Entry> entries = new List<LiveOpsPaletteMatcher.Entry>
            {
                Section("<b>Lịch</b>", "LÊN LỊCH", string.Empty, "raw", SectionHealth.Warning("<i>1</i>", "lý do <u>thô</u>"), "lý do <u>thô</u>"),
            };
            IReadOnlyList<LiveOpsPaletteMatcher.Match> matches = LiveOpsPaletteMatcher.Find(entries, "lich");
            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual("<noparse><b></noparse><b><noparse>Lịch</noparse></b><noparse></b></noparse>", matches[0].RichTitle,
                "chỉ thẻ <b> của palette là thẻ thật; thẻ trong tiêu đề thành chữ");
            string row = LiveOpsHubPalette.RowText(matches[0]);
            StringAssert.Contains("<noparse> · <i>1</i></noparse>", row, "badge thô bọc noparse");
            StringAssert.Contains("<noparse> — lý do <u>thô</u></noparse>", row, "lý do thô bọc noparse");
        }

        [Test]
        public void Palette_OpenArrowsEnter_OpensSelectedAndCloses()
        {
            VisualElement root = new VisualElement();
            List<LiveOpsPaletteMatcher.Entry> entries = DesignEntries();
            LiveOpsPaletteMatcher.Entry opened = null;
            int searchCount = 0;
            LiveOpsHubPalette palette = new LiveOpsHubPalette(root, () => entries, entry => opened = entry, () => searchCount++);

            Assert.IsFalse(palette.IsOpen);
            Assert.IsTrue(palette.Panel.ClassListContains(LiveOpsHubClassNames.PaletteHidden), "đóng thì panel không chiếm layout");
            palette.Open();
            Assert.IsTrue(palette.IsOpen);
            Assert.IsFalse(palette.Scrim.ClassListContains(LiveOpsHubClassNames.ScrimHidden), "scrim phủ cả cửa sổ khi mở");
            Assert.AreSame(root, palette.Scrim.parent);
            Assert.AreEqual(entries.Count, palette.Rows.Count, "mở trống liệt kê mọi mục theo rail");
            Assert.AreEqual(0, palette.SelectedIndex, "chọn sẵn hàng đầu");
            Assert.IsTrue(palette.Rows[0].ClassListContains(LiveOpsHubClassNames.PaletteRowSelected));
            Assert.AreEqual(palette.Panel, root[root.childCount - 1], "panel nằm trên cùng (BringToFront)");

            palette.HandleKey(KeyCode.UpArrow);
            Assert.AreEqual(0, palette.SelectedIndex, "không vượt đầu danh sách");
            palette.HandleKey(KeyCode.DownArrow);
            palette.HandleKey(KeyCode.DownArrow);
            Assert.AreEqual(2, palette.SelectedIndex);
            Assert.IsFalse(palette.Rows[0].ClassListContains(LiveOpsHubClassNames.PaletteRowSelected));
            palette.HandleKey(KeyCode.Return);
            Assert.AreSame(entries[2], opened, "Enter mở mục đang chọn");
            Assert.IsFalse(palette.IsOpen, "mở mục xong palette đóng");

            palette.Open();
            palette.ApplyQuery("overlap");
            Assert.AreEqual(1, palette.Matches.Count);
            palette.HandleKey(KeyCode.KeypadEnter);
            Assert.AreEqual("overlap-same-type", opened.RuleId, "gõ id luật dẫn tới Kiểm lịch đã lọc luật — không phải lệnh");

            palette.Open();
            palette.HandleKey(KeyCode.Escape);
            Assert.IsFalse(palette.IsOpen, "Esc đóng");
            Assert.AreEqual(0, searchCount);
        }

        [Test]
        public void Palette_ToggleWhileOpen_ClosesAndOpensUnitySearch_NoMatchNamesQuery()
        {
            VisualElement root = new VisualElement();
            int searchCount = 0;
            LiveOpsHubPalette palette = new LiveOpsHubPalette(root, DesignEntries, entry => { }, () => searchCount++);

            palette.ToggleOrSearch();
            Assert.IsTrue(palette.IsOpen, "⌘K khi đóng thì mở");
            Assert.AreEqual(0, searchCount);

            palette.ApplyQuery("xyz");
            Assert.AreEqual(0, palette.Rows.Count);
            Assert.AreEqual("Không có màn nào khớp \"xyz\"", palette.EmptyLabel.text);
            Assert.IsFalse(palette.EmptyLabel.ClassListContains(LiveOpsHubClassNames.PaletteEmptyHidden));
            Assert.IsFalse(palette.EmptyLabel.enableRichText, "câu gõ thô không được thành thẻ rich text");
            palette.HandleKey(KeyCode.Return);
            Assert.IsTrue(palette.IsOpen, "không có hàng thì Enter không làm gì");

            palette.ToggleOrSearch();
            Assert.IsFalse(palette.IsOpen, "⌘K lần nữa đóng palette");
            Assert.AreEqual(1, searchCount, "và mở Unity Search (⌘K vốn là phím của Unity Search)");
        }

        [Test]
        public void Palette_RowText_NotOkShowsBadgeAndReason_RuleShowsId()
        {
            LiveOpsPaletteMatcher.Entry live = Section("Trực tiếp", "CHẠY", string.Empty, "live", SectionHealth.NotMeasured("Không ở Play Mode"),
                "Không ở Play Mode");
            string liveRow = LiveOpsHubPalette.RowText(LiveOpsPaletteMatcher.Find(new[] { live }, string.Empty)[0]);
            Assert.AreEqual("<noparse>Trực tiếp</noparse><noparse> · chưa kiểm</noparse><noparse> — Không ở Play Mode</noparse>", liveRow,
                "hàng không Ok in badge và lý do sau tên — palette không có tooltip");

            string ruleRow = LiveOpsHubPalette.RowText(LiveOpsPaletteMatcher.Find(new[] { Rule("overlap-same-type") }, string.Empty)[0]);
            Assert.AreEqual("<noparse>Kiểm lịch</noparse><noparse> / overlap-same-type</noparse>", ruleRow);

            string okRow = LiveOpsHubPalette.RowText(LiveOpsPaletteMatcher.Find(DesignEntries(), string.Empty)[0]);
            Assert.AreEqual("<noparse>Tổng quan</noparse>", okRow, "hàng Ok không badge");
        }

        private static List<string> IdsOf(IReadOnlyList<LiveOpsPaletteMatcher.Match> matches)
        {
            List<string> ids = new List<string>();
            foreach (LiveOpsPaletteMatcher.Match match in matches) ids.Add(match.Entry.SectionId);
            return ids;
        }
    }
}
