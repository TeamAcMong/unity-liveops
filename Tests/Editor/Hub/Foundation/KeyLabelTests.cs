using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class KeyLabelTests
    {
        [Test]
        public void UnknownId_ReturnsEmpty()
        {
            // GetShortcutBinding ném ArgumentException với id không tồn tại ([API §6.2]) — helper phải nuốt và trả "".
            Assert.AreEqual(string.Empty, LiveOpsHubKeyLabels.For("LiveOps Hub/Does Not Exist"));
            Assert.AreEqual(string.Empty, LiveOpsHubKeyLabels.WithParentheses("LiveOps Hub/Does Not Exist"),
                "không có phím thì không in cặp ngoặc rỗng '()' trong câu toast");
            Assert.AreEqual(string.Empty, LiveOpsHubKeyLabels.For(null));
        }

        [Test]
        public void UndoRedo_Readable()
        {
            string undo = LiveOpsHubKeyLabels.Undo;
            string redo = LiveOpsHubKeyLabels.Redo;

            StringAssert.Contains("Z", undo, "Hoàn tác mặc định gán phím Z (+ Cmd/Ctrl) ở hai bản Unity");
            StringAssert.Contains("Z", redo);
            Assert.AreNotEqual(undo, redo, "Làm lại có thêm Shift — trùng nhãn với Hoàn tác là đọc sai binding");
            Assert.AreEqual("(" + undo + ")", LiveOpsHubKeyLabels.WithParentheses("Main Menu/Edit/Undo"));
        }

        [Test]
        public void FrameSelectedWithoutBinding_ReturnsEmptyNotSymbol()
        {
            // Id có trong ShortcutManager nhưng không gán phím trên macOS ([API §6.2], S-8): không được in "⌘" cụt.
            string frameSelected = LiveOpsHubKeyLabels.For("Main Menu/Edit/Frame Selected");
            string trimmed = frameSelected.Trim();
            Assert.IsFalse(trimmed == "⌘" || trimmed == "Cmd" || trimmed == "Ctrl+",
                "nhãn chỉ có ký hiệu modifier mà không có phím là nhãn sai: '" + frameSelected + "'");
            if (trimmed.Length > 0)
            {
                // Máy có gán phím cho Frame Selected (Windows gán F) thì nhãn phải có ký tự phím thật.
                Assert.IsTrue(char.IsLetterOrDigit(trimmed[trimmed.Length - 1]), "nhãn phải kết thúc bằng phím: '" + frameSelected + "'");
            }
        }

        [Test]
        public void ReplaceMissingGlyphs_KeepsLabelWhenFontHasGlyphs()
        {
            // Inter-Regular có đủ ⌘ ⇧ ⌥ ⌫ ở hai bản ([API §12.3]) → nhãn giữ nguyên ký hiệu; font thiếu thì đổi sang chữ.
            string label = "⇧⌘Z";
            string replaced = LiveOpsHubKeyLabels.ReplaceMissingGlyphs(label);
            if (LiveOpsHubKeyLabels.EditorFontHas("⇧⌘"))
            {
                Assert.AreEqual(label, replaced);
            }
            else
            {
                StringAssert.Contains("Cmd", replaced);
                Assert.IsTrue(replaced.EndsWith("Z", System.StringComparison.Ordinal), replaced);
            }
        }
    }
}
