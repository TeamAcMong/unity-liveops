using System;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// <see cref="LiveOpsHubUndoTracker"/> trên stack Undo THẬT của Editor (Undo chạy được trong batchmode — [API §2] RUN): một lần kéo gộp
    /// thành một bước, group tự tăng không có bản ghi không làm mất "còn trên đỉnh", thao tác khác có bản ghi thì mất, sự kiện Undo/Redo
    /// mang đúng tên, số group và chiều.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class UndoTrackerTests
    {
        private LiveOpsHubUndoTracker _tracker;
        private UndoTestTarget _target;

        [SetUp]
        public void SetUp()
        {
            _tracker = new LiveOpsHubUndoTracker();
            _target = new UndoTestTarget();
        }

        [TearDown]
        public void TearDown()
        {
            _tracker.Dispose();
            _target.Dispose();
        }

        [Test]
        public void UndoTracker_DragCollapsesToOneStep()
        {
            _target.SetKey("before-drag", "Đặt giá trị đầu");
            const string dragName = "Đã dời kết thúc lava-quest-2026-09b 19/9 → 20/9 00:00 UTC";
            int group = _tracker.BeginGroup(dragName);
            // Khung kéo đầu ghi ngay trong group vừa mở (bước gộp mang tên group có bản ghi đầu tiên); các khung sau Unity có thể mở
            // group mới (sự kiện chuột) và bản ghi mang tên riêng của khung.
            _target.SetKey("drag-frame-1", dragName);
            for (int frame = 2; frame <= 4; frame++)
            {
                Undo.IncrementCurrentGroup();
                _target.SetKey("drag-frame-" + frame, "khung kéo " + frame);
            }
            Assert.IsFalse(_tracker.IsGroupOnTop(group), "trước khi gộp, các khung sau là bản ghi mới hơn group của toast");
            _tracker.CollapseGroup(group);

            Assert.IsTrue(_tracker.IsGroupOnTop(group), "sau khi gộp, cả lần kéo là thao tác mới nhất của toast");
            _tracker.PerformUndo();
            Assert.AreEqual("before-drag", _target.Key, "một lần Undo gỡ hết mọi khung của lần kéo");
            Assert.AreEqual(dragName, _tracker.LastUndoRedoGroupName, "bước Undo mang đúng câu toast");
            Assert.IsFalse(_tracker.LastUndoRedoWasRedo);
            Assert.IsTrue(_tracker.IsLastUndoRedoOf(group), "sự kiện Undo mang đúng số group của toast");
            Assert.IsTrue(_tracker.IsGroupOnTop(group), "sau Undo (Unity tự tăng số group) chưa có bản ghi mới → Làm lại vẫn đúng bước");

            _tracker.PerformRedo();
            Assert.AreEqual("drag-frame-4", _target.Key, "một lần Redo trả lại cả lần kéo");
            Assert.IsTrue(_tracker.LastUndoRedoWasRedo);
        }

        [Test]
        public void UndoTracker_BeginGroup_NamesGroupAndLastAction()
        {
            int group = _tracker.BeginGroup("Đã xoá hunt-0916-bonus");
            Assert.AreEqual(Undo.GetCurrentGroup(), group);
            Assert.AreEqual("Đã xoá hunt-0916-bonus", Undo.GetCurrentGroupName(), "tên group = câu toast (Edit → Undo History nói cùng câu)");
            Assert.AreEqual("Đã xoá hunt-0916-bonus", _tracker.LastActionText);
            Assert.AreEqual(group, _tracker.LastActionGroup);
            Assert.Throws<ArgumentException>(() => _tracker.BeginGroup(string.Empty));
        }

        [Test]
        public void UndoTracker_AutoIncrementWithoutRecord_StaysOnTop_RecordAfter_IsNotOnTop()
        {
            int group = _tracker.BeginGroup("Đã áp mẫu cho weekly-pass");
            _target.SetKey("preset", "Đã áp mẫu cho weekly-pass");
            Assert.IsTrue(_tracker.IsGroupOnTop(group));

            // Mouse down lên chính nút Hoàn tác: Unity tăng số group mà không ghi gì (GetCurrentGroupName vẫn trả tên cũ — không dùng được).
            Undo.IncrementCurrentGroup();
            Undo.IncrementCurrentGroup();
            Assert.IsTrue(_tracker.IsGroupOnTop(group), "group tự tăng không có bản ghi không phải thao tác khác");

            _target.SetKey("other", "Sửa ở Inspector");
            Assert.IsFalse(_tracker.IsGroupOnTop(group), "một thao tác có ghi sau đó làm Hoàn tác của toast mất hiệu lực");
            Assert.IsFalse(_tracker.IsGroupOnTop(LiveOpsToastModel.NoUndoGroup));
        }

        [Test]
        public void UndoTracker_Dispose_StopsUndoRedoEvent()
        {
            int calls = 0;
            _tracker.UndoRedoPerformed += () => calls++;
            _tracker.BeginGroup("Đã đổi màu loại");
            _target.SetKey("color", "Đã đổi màu loại");
            _tracker.PerformUndo();
            Assert.AreEqual(1, calls);

            _tracker.Dispose();
            Undo.PerformRedo();
            Assert.AreEqual(1, calls, "field delegate đã gỡ — tracker cũ không nghe sau Dispose (không rò qua domain reload)");
        }
    }

    /// <summary>
    /// Đích Undo cho test Feedback: một <see cref="LiveEventCalendarAsset"/> trong bộ nhớ, sửa field <c>remoteConfigKey</c> đúng đường
    /// hub dùng (<c>Undo.RecordObject</c> → sửa → <c>FlushUndoRecordObjects</c>), dọn stack Undo của nó khi xong để test sau không gỡ nhầm.
    /// </summary>
    internal sealed class UndoTestTarget : IDisposable
    {
        private const string KeyPropertyName = "remoteConfigKey";

        private LiveEventCalendarAsset _asset;

        public UndoTestTarget()
        {
            _asset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
        }

        public string Key => new SerializedObject(_asset).FindProperty(KeyPropertyName).stringValue;

        public void SetKey(string value, string undoName)
        {
            Undo.RecordObject(_asset, undoName);
            SerializedObject serialized = new SerializedObject(_asset);
            serialized.FindProperty(KeyPropertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Undo.FlushUndoRecordObjects();
        }

        public void Dispose()
        {
            if (_asset == null) return;
            Undo.ClearUndo(_asset);
            UnityEngine.Object.DestroyImmediate(_asset);
            _asset = null;
        }
    }
}
