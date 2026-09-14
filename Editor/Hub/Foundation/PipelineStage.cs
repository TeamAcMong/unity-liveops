using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Tầng của rail theo đường đi của lịch: cấu hình → lên lịch → kiểm → xuất → chạy. Số giá trị cố định vì section
    /// khai tầng bằng enum này và rail xếp theo <see cref="PipelineStages.All"/>. <see cref="Run"/> để dành cho P2/P3 —
    /// P1 không có section nào ở tầng này nên rail không vẽ nó (PD-1).
    /// </summary>
    internal enum PipelineStage
    {
        Configure = 0,
        Schedule = 1,
        Check = 2,
        Export = 3,
        Run = 4,
    }

    /// <summary>Thông tin tĩnh của từng tầng — một chỗ duy nhất để rail, palette và Tổng quan không tự suy caption/icon.</summary>
    internal static class PipelineStages
    {
        private static readonly PipelineStage[] Ordered =
        {
            PipelineStage.Configure, PipelineStage.Schedule, PipelineStage.Check, PipelineStage.Export, PipelineStage.Run,
        };

        /// <summary>Thứ tự rail. Mảng bọc chỉ đọc để nơi gọi không sửa được thứ tự dùng chung.</summary>
        public static IReadOnlyList<PipelineStage> All { get; } = Array.AsReadOnly(Ordered);

        /// <summary>
        /// Tầng cổng: dừng ở đây thì đường nối phía sau "chết" trên rail. Cấu hình và Lên lịch là nơi làm việc, không phải
        /// cổng — lỗi ở đó chỉ hiện thành phát hiện của Kiểm.
        /// </summary>
        public static bool IsGate(PipelineStage stage)
        {
            return stage == PipelineStage.Check || stage == PipelineStage.Export || stage == PipelineStage.Run;
        }

        /// <summary>Caption HOA viết sẵn: USS không có text-transform ([FD §2.9]).</summary>
        public static string CaptionOf(PipelineStage stage)
        {
            switch (stage)
            {
                case PipelineStage.Configure: return LiveOpsHubStrings.StageCaptionConfigure;
                case PipelineStage.Schedule: return LiveOpsHubStrings.StageCaptionSchedule;
                case PipelineStage.Check: return LiveOpsHubStrings.StageCaptionCheck;
                case PipelineStage.Export: return LiveOpsHubStrings.StageCaptionExport;
                case PipelineStage.Run: return LiveOpsHubStrings.StageCaptionRun;
                default: throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
        }

        /// <summary>Tên truyền cho <see cref="LiveOpsHubIcons.Get"/> (không có tiền tố d_ — helper tự chọn theo skin).</summary>
        public static string IconNameOf(PipelineStage stage)
        {
            switch (stage)
            {
                case PipelineStage.Configure: return "Settings";
                case PipelineStage.Schedule: return LiveOpsHubPaths.CalendarIconName;
                case PipelineStage.Check: return "Valid";
                case PipelineStage.Export: return "SaveAs";
                case PipelineStage.Run: return "PlayButton";
                default: throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
        }
    }
}
