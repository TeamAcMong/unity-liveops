using System;
using System.IO;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// File asset đổi trên đĩa trong lúc hub đang mở (R-5, 4.3): không còn gì để mất thì nhận bản đĩa im lặng; còn nháp chưa lưu thì
    /// dựng băng xung đột và KHÔNG tự ghi gì. (SP-8a) Phân biệt "mình vừa lưu" với "người khác đổi" bằng hash file, không bằng cờ thời
    /// gian: <c>SaveAssetIfDirty</c> có bắn postprocessor đồng bộ ngay trong lần lưu của chính hub.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class DiskChangeTests
    {
        private const string AssetFileName = "SessionDisk.asset";
        private const string DiskEndUtc = "2026-09-21T00:00:00Z";
        private const string DraftEndUtc = "2026-09-22T00:00:00Z";

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        [Test]
        public void Session_DiskChangeWithoutUnsaved_Reloads()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = CalendarSessionTests.OpenSession(asset);
            int diskChangeCount = 0;
            session.DiskChangeDetected += () => diskChangeCount++;
            Assert.IsFalse(session.HasUnsavedChanges, "mở asset vừa lưu thì chưa có gì chưa lưu");

            WriteDiskDocument(session.AssetPath, WithLavaQuestEnd(LiveOpsDesignSample.Document, DiskEndUtc));
            session.HandleAssetsChanged(new[] { session.AssetPath }, null, null, null);

            Assert.IsNull(session.DiskConflict, "không có nháp để mất thì không hỏi gì");
            Assert.AreEqual(0, diskChangeCount, "nhận bản đĩa im lặng — không dựng băng xung đột");
            Assert.AreEqual(DiskEndUtc, CalendarSessionTests.FindLavaQuestMid(session.Document).EndUtcText, "nháp phải là bản trên đĩa");
            Assert.IsFalse(session.HasUnsavedChanges, "vừa nhận bản đĩa xong thì không còn thay đổi chưa lưu");
        }

        [Test]
        public void Session_DiskChangeWithUnsaved_Conflict_KeepEditorVersionRestoresDraft()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = CalendarSessionTests.OpenSession(asset);
            int diskChangeCount = 0;
            session.DiskChangeDetected += () => diskChangeCount++;
            session.Apply(CalendarSessionTests.MoveLavaQuestEnd(session.Document, DraftEndUtc), "Dời kết thúc lava-quest");
            Assert.IsTrue(session.HasUnsavedChanges);

            WriteDiskDocument(session.AssetPath, WithLavaQuestEnd(LiveOpsDesignSample.Document, DiskEndUtc));
            session.HandleAssetsChanged(new[] { session.AssetPath }, null, null, null);

            Assert.AreEqual(1, diskChangeCount, "còn nháp chưa lưu thì phải báo đúng một lần để cửa sổ hiện băng");
            Assert.IsNotNull(session.DiskConflict, "phải có băng xung đột — quyết định là của người dùng");
            Assert.AreEqual(DraftEndUtc, CalendarSessionTests.FindLavaQuestMid(session.Document).EndUtcText, "nháp giữ nguyên, không bị bản đĩa đè");
            Assert.AreEqual(DiskEndUtc, CalendarSessionTests.FindLavaQuestMid(session.DiskConflict.DiskDocument).EndUtcText);
            Assert.AreEqual(DraftEndUtc, CalendarSessionTests.FindLavaQuestMid(session.DiskConflict.EditorDocument).EndUtcText);
            Assert.IsFalse(session.DiskConflict.DiskVersusEditor.IsEmpty, "băng phải nêu được khác biệt đĩa ↔ Editor");

            session.KeepEditorVersion();

            Assert.IsNull(session.DiskConflict, "chọn xong thì băng biến mất");
            Assert.AreEqual(DraftEndUtc, CalendarSessionTests.FindLavaQuestMid(session.Document).EndUtcText, "Giữ bản trong Editor = nháp thắng");
            Assert.AreEqual(DraftEndUtc, CalendarSessionTests.FindLavaQuestMid(session.Asset.ToDocument()).EndUtcText, "nháp phải được ghi lại vào asset");
            Assert.IsTrue(session.HasUnsavedChanges, "asset còn bẩn so với bản trên đĩa — tab vẫn có *, người dùng tự lưu đè");
        }

        [Test]
        public void Session_OwnSave_NotReportedAsDiskChange()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = CalendarSessionTests.OpenSession(asset);
            int diskChangeCount = 0;
            session.DiskChangeDetected += () => diskChangeCount++;
            session.Apply(CalendarSessionTests.MoveLavaQuestEnd(session.Document, DraftEndUtc), "Dời kết thúc lava-quest");

            Assert.IsTrue(session.Save(), "lưu được asset có file");

            // (SP-8a) SaveAssetIfDirty bắn postprocessor ĐỒNG BỘ ngay trong Save; lần bắn lại dưới đây mô phỏng import muộn của Unity.
            session.HandleAssetsChanged(new[] { session.AssetPath }, null, null, null);

            Assert.AreEqual(0, diskChangeCount, "lần lưu của chính hub không bao giờ là 'người khác đổi file'");
            Assert.IsNull(session.DiskConflict);
            Assert.IsFalse(session.HasUnsavedChanges, "lưu xong thì hết * trên tab");
        }

        [Test]
        public void Session_DiskChangeEqualToDraft_NoConflict()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = CalendarSessionTests.OpenSession(asset);
            int diskChangeCount = 0;
            session.DiskChangeDetected += () => diskChangeCount++;
            session.Apply(CalendarSessionTests.MoveLavaQuestEnd(session.Document, DraftEndUtc), "Dời kết thúc lava-quest");

            // Người khác lưu ĐÚNG bản mà mình đang có trong nháp (ví dụ git pull về chính commit mình vừa đẩy từ máy khác).
            WriteDiskDocument(session.AssetPath, WithLavaQuestEnd(LiveOpsDesignSample.Document, DraftEndUtc));
            session.HandleAssetsChanged(new[] { session.AssetPath }, null, null, null);

            Assert.AreEqual(0, diskChangeCount, "cùng nội dung thì không có gì để mất — không hỏi");
            Assert.IsNull(session.DiskConflict);
            Assert.AreEqual(DraftEndUtc, CalendarSessionTests.FindLavaQuestMid(session.Document).EndUtcText);
            Assert.IsFalse(session.HasUnsavedChanges, "đĩa đã có đúng bản này rồi nên không còn gì chưa lưu");
        }

        [Test]
        public void Session_AssetMovedOnDisk_TracksNewPath()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = CalendarSessionTests.OpenSession(asset);
            string oldPath = session.AssetPath;
            string newPath = LiveOpsHubTestServices.TestFolder + "/SessionDiskRenamed.asset";

            // Đường thật: postprocessor bắn, phiên tự tìm đường dẫn của mình trong mảng movedFrom.
            string error = AssetDatabase.MoveAsset(oldPath, newPath);

            Assert.IsEmpty(error, "đổi tên asset phải thành công");
            Assert.AreEqual(newPath, session.AssetPath, "phiên bám theo file, không bám theo đường dẫn cũ");
            Assert.AreSame(asset, session.Asset, "đổi tên không phải đổi asset");
            Assert.IsNull(session.DiskConflict, "đổi tên không phải đổi nội dung");
        }

        // ------------------------------------------------------------------------------------------------------------ hỗ trợ

        /// <summary>Ghi thẳng file serialize ở đường dẫn asset — mô phỏng git pull / sửa tay: instance đang nạp trong Editor KHÔNG đổi theo.</summary>
        private static void WriteDiskDocument(string assetPath, LiveEventCalendarDocument document)
        {
            string physicalPath = FileUtil.GetPhysicalPath(assetPath);
            Assert.IsTrue(File.Exists(physicalPath), "asset của test phải có file thật để ghi đè");
            LiveEventCalendarAsset writer = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            writer.hideFlags = HideFlags.DontSave;
            writer.ApplyDocument(document);
            try
            {
                InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { writer }, physicalPath, true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(writer);
            }
        }

        private static LiveEventCalendarDocument WithLavaQuestEnd(LiveEventCalendarDocument document, string endUtc)
        {
            LiveEventCalendarEdit edit = CalendarSessionTests.MoveLavaQuestEnd(document, endUtc);
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, edit, out LiveEventCalendarDocument result), "lệnh dời kết thúc phải áp được");
            return result;
        }
    }
}
