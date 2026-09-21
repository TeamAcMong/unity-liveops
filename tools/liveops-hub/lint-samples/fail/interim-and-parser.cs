// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Sections/Export/ExportSample.cs
// Mẫu vi phạm: INTERIM trỏ gói không có, hằng Interim thiếu comment, section gọi thẳng parser của game.
// lint-expect: interim-owner
// lint-expect: interim-constant
// lint-expect: hub-parser-direct
using DreamTech.LiveOps.Unity;

namespace DreamTech.LiveOps.Editor
{
    internal sealed class ExportSample
    {
        // INTERIM(G-NOT-A-PACKAGE): gói không tồn tại
        private bool _placeholder;


        private const string InterimExportReason = "not built";

        public int Count(string json)
        {
            return JsonLiveEventCalendarParser.Parse(json).Problems.Count;
        }
    }
}
