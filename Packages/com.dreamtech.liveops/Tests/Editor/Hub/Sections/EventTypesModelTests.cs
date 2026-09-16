using System;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Model thuần của màn Loại event (7.2): thứ tự làn (V-12), câu trùng màu kèm ô trống gợi ý mà KHÔNG tự ghi (PD Q1),
    /// câu ghi đè theo băm, khoá id loại, nhãn "Xoá loại (còn n đợt)" khớp bảng 7.0, hàng loại chưa khai báo từ JSON đang chạy
    /// (V-17) và tiền tố "Kiểm lịch · " của card tham chiếu (V-22 CC-FT-2 (a)).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class EventTypesModelTests
    {
        private const string StarTournament = "star-tournament";
        private const string TreasureHunt = "treasure-hunt";
        private const string LavaQuest = "lava-quest";
        private const string SkyRace = "sky-race";
        private const string WeeklyPass = "weekly-pass";
        private const string RemoteOnlyType = "lucky-spin";

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
        public void LaneOrder_RowsFollowDocumentOrder()
        {
            EventTypesModel model = EventTypesModel.Build(LiveOpsDesignSample.Document, null, null);

            string[] expected = { LavaQuest, SkyRace, StarTournament, TreasureHunt, WeeklyPass };
            Assert.AreEqual(expected.Length, model.Rows.Count, "chỉ có năm loại mẫu, không hàng chưa khai báo nào");
            for (int index = 0; index < expected.Length; index++)
            {
                Assert.AreEqual(expected[index], model.Rows[index].TypeId,
                    "(V-12) bảng theo THỨ TỰ LÀN của tài liệu khi chưa bật sort cột");
                Assert.AreEqual(index, model.Rows[index].LaneIndex);
            }
        }

        [Test]
        public void ColorCollision_WarningListsFreeSlots_NoAutoWrite()
        {
            LiveEventCalendarDocument document = WithStarTournamentSlot(7);
            EventTypesModel model = EventTypesModel.Build(document, null, null);

            string collision = model.ColorCollisionText(StarTournament);
            StringAssert.Contains(EventTypesModel.SlotNumberText(7), collision, "câu phải nêu ô đang trùng");
            StringAssert.Contains(TreasureHunt, collision, "câu phải nêu loại đang giữ ô đó");
            StringAssert.Contains("2, 3, 5", collision, "câu phải nêu ô trống để bấm chọn [SD1 §2.1]");

            IReadOnlyList<int> suggestions = model.SuggestedFreeSlots(StarTournament);
            CollectionAssert.AreEqual(new[] { 2, 3, 5 }, suggestions, "chỉ gợi ý, đúng ba ô đầu");

            // Không tự ghi (PD Q1): tài liệu sau khi dựng model vẫn giữ nguyên ô 7 cho cả hai loại.
            Assert.AreEqual(7, document.EventTypes[2].ColorSlot, "model chỉ đọc — gợi ý ô trống không được ghi vào nháp");
            Assert.AreEqual(7, document.EventTypes[3].ColorSlot);
            Assert.AreEqual(HealthState.Warning, model.Health.State, "trùng màu chưa xử lý là Warning");
        }

        [Test]
        public void ColorCollision_Resolved_NoWarningButQuietOverrideLine()
        {
            EventTypesModel model = EventTypesModel.Build(LiveOpsDesignSample.Document, null, null);

            Assert.AreEqual(string.Empty, model.ColorCollisionText(StarTournament), "ô 6 không còn trùng ai nên không cảnh báo");
            string overrideText = model.HashOverrideText(StarTournament);
            StringAssert.Contains(TreasureHunt, overrideText);
            StringAssert.Contains(EventTypesModel.SlotNumberText(7), overrideText, "ô theo băm");
            StringAssert.Contains(EventTypesModel.SlotNamedText(6), overrideText, "ô đang ghi đè, kèm tên màu");
            Assert.IsTrue(model.CanResetColorToHash(StarTournament), "đang khác ô theo băm nên có đường về");
            Assert.AreEqual(7, model.HashedSlotOf(StarTournament));
            Assert.AreEqual(HealthState.Ok, model.Health.State);
        }

        [Test]
        public void HashOverride_SilentWhenHashedSlotIsFree()
        {
            // lava-quest băm ra ô 0 và đang ở ô 0; dời sang ô 2 (không loại nào giữ) thì không có gì để giải thích.
            LiveEventCalendarDocument document = WithEventTypeSlot(LiveOpsDesignSample.Document, LavaQuest, 2);
            EventTypesModel model = EventTypesModel.Build(document, null, null);

            Assert.AreEqual(string.Empty, model.HashOverrideText(LavaQuest), "không trùng ai thì câu 'theo băm trùng X' là câu sai");
            Assert.IsTrue(model.CanResetColorToHash(LavaQuest), "vẫn có nút về màu theo băm");
        }

        [Test]
        public void DeleteTypeWithEvents_DisabledWithReason()
        {
            EventTypesModel model = EventTypesModel.Build(LiveOpsDesignSample.Document, null, null);

            Assert.IsFalse(model.CanDeleteType(LavaQuest, out string menuLabel, out string reason),
                "lava-quest còn 3 đợt nên không cho xoá (bảng 7.0 ô 'Xoá loại')");
            StringAssert.Contains("3", menuLabel, "nhãn menu phải nêu số đợt còn lại");
            Assert.IsNotEmpty(reason, "disabled luôn kèm lý do");

            Assert.IsFalse(model.CanEditTypeId(LavaQuest, out string lockReason), "loại đã có đợt thì id khoá");
            Assert.IsNotEmpty(lockReason);
        }

        [Test]
        public void DeleteUnusedType_Allowed()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("empty-mode", "Chế độ rỗng", 3, false, string.Empty))
                .Build();
            EventTypesModel model = EventTypesModel.Build(document, null, null);

            Assert.IsTrue(model.CanDeleteType("empty-mode", out string menuLabel, out string reason));
            Assert.AreEqual(LiveOpsHubStrings.EventTypesDeleteMenuItem, menuLabel);
            Assert.AreEqual(string.Empty, reason);
            Assert.IsTrue(model.CanEditTypeId("empty-mode", out _), "chưa có đợt nào thì id sửa được");
        }

        [Test]
        public void CanDeleteType_MatchesConfirmationPolicy()
        {
            AssertDeleteAgreesWithPolicy(LiveOpsDesignSample.Document);
        }

        /// <summary>
        /// Tài liệu dán vào có thể có NHIỀU luật cùng loại (core giữ luật đứng trước). Hỏi <c>TryGetRecurringRule</c> chỉ ra
        /// một luật, nên con số trong "Xoá loại (còn n đợt)" và trong lý do khoá ô id đếm thiếu so với policy — mà con số đó là
        /// chữ người dùng đọc. So cả BOOLEAN lẫn CON SỐ.
        /// </summary>
        [Test]
        public void DuplicateRecurringRules_UsageCountCountsEveryRule()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(SkyRace, "Đua trên mây", 2, false, string.Empty))
                .WithRecurringRule(new RecurringLiveEventRule(SkyRace, "2026-01-05T00:00:00Z", "sky-race-", 24, 20, string.Empty))
                .WithRecurringRule(new RecurringLiveEventRule(SkyRace, "2026-02-05T00:00:00Z", "sky-race-b-", 48, 20, string.Empty))
                .Build();
            EventTypesModel model = EventTypesModel.Build(document, null, null);
            EventTypeRow row = FindRow(model, SkyRace);

            Assert.IsNotNull(row);
            Assert.AreEqual(2, row.RecurringRuleCount, "hai luật cùng loại trong tài liệu");
            Assert.AreEqual(2, row.UsageCount, "không đợt cố định nào — hai luật là hai mục dùng loại");
            Assert.IsTrue(row.HasRecurringRule, "cột Nguồn/ô Đợt vẫn chỉ cần CÓ hay KHÔNG");

            Assert.IsFalse(model.CanDeleteType(SkyRace, out string menuLabel, out string reason));
            StringAssert.Contains("2", menuLabel, "nhãn menu nêu đúng số mục còn dùng loại");
            StringAssert.Contains("2", reason);
            Assert.IsFalse(model.CanEditTypeId(SkyRace, out string lockReason));
            StringAssert.Contains("2", lockReason, "lý do khoá ô id đếm cùng cách");

            AssertDeleteAgreesWithPolicy(document);
        }

        [Test]
        public void UnknownTypeFromRemote_RowIsUndeclaredWithRemoteSentence()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            LiveOpsHubCalendarSession session = services.Session;
            session.Remote.Set(RemoteJsonWithLuckySpin(), LiveOpsDesignSample.NowUtc);
            session.RunCheckToCompletion();

            EventTypesModel model = EventTypesModel.Build(session.Document, session.Check.LastReport, session.Remote);
            EventTypeRow row = FindRow(model, RemoteOnlyType);

            Assert.IsNotNull(row, "(V-17) loại chỉ có trong JSON đang chạy vẫn phải thành một hàng của bảng");
            Assert.IsFalse(row.IsDeclared, "hàng chưa khai báo — view mờ 0,7 và swatch viền rỗng theo cờ này");
            Assert.IsTrue(row.IsFromRemoteSnapshotOnly);
            Assert.AreEqual(-1, row.ColorSlot, "chưa khai báo thì hub chưa biết màu nào");
            Assert.AreEqual(LiveOpsHubStrings.EventTypesUndeclaredName, row.DisplayName);
            Assert.AreEqual(2, row.UsageCount, "hai đợt lucky-spin trong bản dán");

            string sentence = model.UndeclaredText(RemoteOnlyType);
            StringAssert.Contains(RemoteOnlyType, sentence);
            StringAssert.Contains("2", sentence);
            Assert.AreEqual(HealthState.Blocked, model.Health.State, "(V-17) loại chưa khai báo chặn màn Loại event");
            Assert.AreEqual(RemoteOnlyType, model.FirstUndeclaredTypeId);
        }

        [Test]
        public void ReferenceHeadline_AddsCalendarCheckPrefix()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            LiveOpsHubCalendarSession session = services.Session;
            session.Remote.Set(RemoteJsonWithLuckySpin(), LiveOpsDesignSample.NowUtc);
            session.RunCheckToCompletion();

            EventTypesModel model = EventTypesModel.Build(session.Document, session.Check.LastReport, session.Remote);
            LiveEventCalendarFinding finding = model.ReferenceFinding(RemoteOnlyType);
            Assert.IsNotNull(finding, "báo cáo phải có phát hiện luật 8 cho loại này, nếu không test vô nghĩa");

            string source = LiveOpsFindingText.Headline(finding, services.Format);
            string headline = model.ReferenceHeadline(RemoteOnlyType, services.Format);
            Assert.AreEqual(LiveOpsHubStrings.EventTypesReferencePrefix + source, headline,
                "(V-22 CC-FT-2 (a)) tiền tố do màn Loại event ghép, nguồn câu không mang tiền tố");
            StringAssert.DoesNotContain(LiveOpsHubStrings.EventTypesReferencePrefix, source,
                "câu gốc của LiveOpsFindingText phải sạch tiền tố — Kiểm lịch dùng lại chính nó");
            StringAssert.Contains(RemoteOnlyType, model.ReferenceRuleIdLine(RemoteOnlyType));
        }

        [Test]
        public void Health_StateMatchesFindingRouting()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            LiveOpsHubCalendarSession session = services.Session;
            AssertHealthAgrees(session, services, "mẫu thiết kế đã kiểm");

            session.Remote.Set(RemoteJsonWithLuckySpin(), LiveOpsDesignSample.NowUtc);
            session.RunCheckToCompletion();
            AssertHealthAgrees(session, services, "có loại chưa khai báo trong bản dán");

            LiveOpsHubServices collisionServices = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(WithStarTournamentSlot(7))));
            collisionServices.Session.RunCheckToCompletion();
            AssertHealthAgrees(collisionServices.Session, collisionServices, "hai loại cùng ô màu");
        }

        [Test]
        public void Health_NeverChecked_IsOkLikeRail()
        {
            EventTypesModel model = EventTypesModel.Build(WithStarTournamentSlot(7), null, null);
            Assert.AreEqual(HealthState.Warning, model.Health.State,
                "trùng màu tính tại chỗ nên thấy được ngay cả khi chưa kiểm — không phụ thuộc báo cáo");

            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            EventTypesModel neverChecked = EventTypesModel.Build(services.Session.Document, null, null);
            Assert.AreEqual(HealthState.Ok, neverChecked.Health.State, "chưa kiểm + không trùng màu = Ok, đúng như rail");
        }

        [Test]
        public void Footer_CountsDeclaredTypesOnly()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            LiveOpsHubCalendarSession session = services.Session;
            session.Remote.Set(RemoteJsonWithLuckySpin(), LiveOpsDesignSample.NowUtc);
            session.RunCheckToCompletion();

            EventTypesModel model = EventTypesModel.Build(session.Document, session.Check.LastReport, session.Remote);
            Assert.AreEqual(6, model.Rows.Count, "năm loại đã khai + một hàng chưa khai báo");
            Assert.AreEqual("5", model.FooterText.Substring(0, 1),
                "footer đếm loại ĐÃ KHAI — hàng chưa khai báo không phải một loại");
        }

        [Test]
        public void UsageText_NamesNextFixedEvent()
        {
            EventTypesModel model = EventTypesModel.Build(LiveOpsDesignSample.Document, null, null);
            LiveOpsHubFormat format = new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset);

            string usage = model.UsageText(StarTournament, LiveOpsDesignSample.NowUtc, format);
            StringAssert.Contains(format.ShortDateTimeUtc(new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc)), usage,
                "card 'Dùng ở đâu' nêu đợt tới của loại");
            Assert.IsTrue(model.TryFindNextFixedEvent(StarTournament, LiveOpsDesignSample.NowUtc, out FixedLiveEventEntry entry, out _));
            Assert.AreEqual("star-tournament-2026-10", entry.EventId, "'Xem trên lịch' căn khung tới đúng đợt này");

            string ruleUsage = model.UsageText(SkyRace, LiveOpsDesignSample.NowUtc, format);
            StringAssert.Contains(LiveOpsHubStrings.EventTypesUsageRecurringRule, ruleUsage, "loại sinh từ luật nói là luật");
        }

        [Test]
        public void RowText_ReadsEntryAndSourceFromDefinition()
        {
            EventTypesModel model = EventTypesModel.Build(LiveOpsDesignSample.Document, null, null);

            EventTypeRow hunt = FindRow(model, TreasureHunt);
            Assert.AreEqual(LiveOpsHubStrings.EventTypesEntryRequiresJoin, hunt.EntryText);
            Assert.AreEqual(LiveOpsHubStrings.EventTypesSourceFixed, hunt.SourceText);
            Assert.AreEqual("2", hunt.EventCountText);
            Assert.IsFalse(hunt.IsEventCountRuleText);

            EventTypeRow skyRace = FindRow(model, SkyRace);
            Assert.AreEqual(LiveOpsHubStrings.EventTypesEntrySelfJoin, skyRace.EntryText);
            Assert.AreEqual(LiveOpsHubStrings.EventTypesSourceRecurring, skyRace.SourceText);
            Assert.AreEqual(LiveOpsHubStrings.EventTypesEventCountRule, skyRace.EventCountText);
            Assert.IsTrue(skyRace.IsEventCountRuleText, "view hạ sáng ô 'luật' theo cờ, không so chuỗi");
        }

        private static void AssertHealthAgrees(LiveOpsHubCalendarSession session, LiveOpsHubServices services, string context)
        {
            EventTypesModel model = EventTypesModel.Build(session.Document, session.Check.LastReport, session.Remote);
            SectionHealth routed = LiveOpsHubFindingRouting.HealthFor(LiveOpsHubHealthTarget.EventTypes, true, session.Check,
                session.Document, services.Format);
            Assert.AreEqual(routed.State, model.Health.State,
                context + ": model của màn và nguồn health chính thức của rail không được nói hai chuyện khác nhau");
        }

        /// <summary>Nhãn menu "Xoá loại" và bảng 7.0 phải nói cùng một chuyện — cả cho phép/không lẫn CON SỐ nêu trong nhãn.</summary>
        private static void AssertDeleteAgreesWithPolicy(LiveEventCalendarDocument document)
        {
            EventTypesModel model = EventTypesModel.Build(document, null, null);
            for (int index = 0; index < document.EventTypes.Count; index++)
            {
                string typeId = document.EventTypes[index].TypeId;
                LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.RemoveEventType, document, null,
                    null, LiveOpsDesignSample.NowUtc, typeId);
                bool canDelete = model.CanDeleteType(typeId, out string menuLabel, out _);
                Assert.AreEqual(decision.Requirement == LiveOpsConfirmRequirement.None, canDelete,
                    typeId + ": nhãn menu và bảng 7.0 phải nói cùng một chuyện");
                if (canDelete) continue;
                StringAssert.Contains(decision.InUseCount.ToString(CultureInfo.InvariantCulture), menuLabel,
                    typeId + ": số trong nhãn menu phải đúng bằng số policy đếm");
            }
        }

        /// <summary>
        /// (V-8) Câu dòng dưới bảng của loại lạ trong bản dán trùng TỪNG CHỮ với câu hậu quả luật 8 ở vùng Findings. Hai khoá
        /// tồn tại vì màn phải nói được câu này trước lần kiểm đầu tiên (chưa có finding nào) — test này chặn hai bên trôi lệch.
        /// </summary>
        [Test]
        public void UnknownTypeInRemote_TextMatchesFindingText()
        {
            IReadOnlyList<LiveOpsHubLanguageId> languages = LiveOpsHubLanguage.Available;
            for (int index = 0; index < languages.Count; index++)
            {
                LiveOpsHubLanguageId language = languages[index];
                Assert.IsTrue(LiveOpsHubStringCatalog.TryGetExact(language,
                    nameof(LiveOpsHubStrings.EventTypesUnknownTypeInRemoteFormat), out string eventTypesText), language.ToString());
                Assert.IsTrue(LiveOpsHubStringCatalog.TryGetExact(language,
                    nameof(LiveOpsHubStrings.FindingUnknownTypeRemoteConsequenceFormat), out string findingText), language.ToString());
                Assert.AreEqual(findingText, eventTypesText,
                    language + ": một câu người dùng đọc thì một chữ — sửa vùng Findings phải sửa vùng EventTypes cùng nhịp");
            }
        }

        private static EventTypeRow FindRow(EventTypesModel model, string typeId)
        {
            IReadOnlyList<EventTypeRow> rows = model.Rows;
            for (int index = 0; index < rows.Count; index++)
            {
                if (string.Equals(rows[index].TypeId, typeId, StringComparison.Ordinal)) return rows[index];
            }
            return null;
        }

        private static LiveEventCalendarDocument WithStarTournamentSlot(int colorSlot)
        {
            return WithEventTypeSlot(LiveOpsDesignSample.Document, StarTournament, colorSlot);
        }

        private static LiveEventCalendarDocument WithEventTypeSlot(LiveEventCalendarDocument document, string typeId, int colorSlot)
        {
            Assert.IsTrue(document.TryGetEventType(typeId, out LiveEventTypeDefinition type), typeId);
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new SetEventTypeEdit(type.WithColorSlot(colorSlot)),
                out LiveEventCalendarDocument result));
            return result;
        }

        /// <summary>Bản dán có hai đợt loại <c>lucky-spin</c> mà nháp không khai (Hình 10b).</summary>
        private static string RemoteJsonWithLuckySpin()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.DocumentWithoutPublishedStamp())
                .WithFixedEvent(new FixedLiveEventEntry("entry-lucky-1", "lucky-spin-0919", RemoteOnlyType,
                    "2026-09-19T00:00:00Z", "2026-09-20T00:00:00Z", "lucky_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lucky-2", "lucky-spin-0926", RemoteOnlyType,
                    "2026-09-26T00:00:00Z", "2026-09-27T00:00:00Z", "lucky_v1"))
                .Build();
            return LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).Text;
        }
    }
}
