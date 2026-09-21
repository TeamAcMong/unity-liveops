using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Ba biến thể của hộp Đánh dấu đã đăng [SD2 §3.11] — quyết định bố cục, không chỉ đổi chữ.</summary>
    internal enum MarkPublishedVariant
    {
        /// <summary>Trái: sha đã copy khớp nháp, chỉ còn tick + ghi chú.</summary>
        Ready = 0,

        /// <summary>Phải: nháp đã đổi sau lần copy — phần xác nhận KHOÁ tới khi copy bản mới.</summary>
        DraftChanged = 1,

        /// <summary>Chưa copy hay lưu lần nào: nút ở section header lẽ ra đã khoá, nhưng hộp vẫn phải nói được lý do.</summary>
        NeverExported = 2,
    }

    /// <summary>
    /// Dữ liệu hộp Đánh dấu đã đăng cần để viết câu — bất biến, dựng ở section rồi truyền vào
    /// <see cref="MarkPublishedModel.Evaluate(MarkPublishedInput)"/>. Không giữ tham chiếu tới phiên: hộp là cửa sổ riêng,
    /// sống qua một vòng modal, và test dựng đủ trạng thái mà không cần asset.
    /// </summary>
    internal sealed class MarkPublishedInput
    {
        public MarkPublishedInput(string currentSha256Hex, string lastExportedSha256Hex, DateTime? lastExportedUtc, DateTime nowUtc,
            int byteCount, string assetFileName, string publisher, string publisherSource, LiveOpsHubFormat format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format), LiveOpsHubStrings.ExportGateErrorFormatMissing);
            CurrentSha256Hex = currentSha256Hex ?? string.Empty;
            LastExportedSha256Hex = lastExportedSha256Hex ?? string.Empty;
            LastExportedUtc = lastExportedUtc;
            NowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            ByteCount = byteCount;
            AssetFileName = assetFileName ?? string.Empty;
            Publisher = publisher ?? string.Empty;
            PublisherSource = publisherSource ?? string.Empty;
            Format = format;
            ConfirmTicked = false;
            Note = string.Empty;
            CopiedInsideDialog = false;
        }

        public string CurrentSha256Hex { get; }
        public string LastExportedSha256Hex { get; }
        public DateTime? LastExportedUtc { get; }
        public DateTime NowUtc { get; }
        public int ByteCount { get; }
        public string AssetFileName { get; }
        public string Publisher { get; }
        public string PublisherSource { get; }
        public LiveOpsHubFormat Format { get; }

        public bool ConfirmTicked { get; private set; }
        public string Note { get; private set; }

        /// <summary>Người dùng vừa bấm "Copy JSON &lt;sha mới&gt;" NGAY TRONG hộp — dòng sha đổi sang lời nhắc dán lại.</summary>
        public bool CopiedInsideDialog { get; private set; }

        public MarkPublishedInput WithUserInput(bool confirmTicked, string note)
        {
            MarkPublishedInput copy = Clone();
            copy.ConfirmTicked = confirmTicked;
            copy.Note = note ?? string.Empty;
            return copy;
        }

        public MarkPublishedInput WithCopiedInsideDialog(string copiedSha256Hex, DateTime copiedUtc)
        {
            MarkPublishedInput copy = new MarkPublishedInput(CurrentSha256Hex, copiedSha256Hex, copiedUtc, NowUtc, ByteCount, AssetFileName,
                Publisher, PublisherSource, Format);
            copy.ConfirmTicked = ConfirmTicked;
            copy.Note = Note;
            copy.CopiedInsideDialog = true;
            return copy;
        }

        private MarkPublishedInput Clone()
        {
            MarkPublishedInput copy = new MarkPublishedInput(CurrentSha256Hex, LastExportedSha256Hex, LastExportedUtc, NowUtc, ByteCount,
                AssetFileName, Publisher, PublisherSource, Format);
            copy.CopiedInsideDialog = CopiedInsideDialog;
            return copy;
        }
    }

    /// <summary>Chữ và trạng thái bật/tắt của hộp Đánh dấu đã đăng — view chỉ gán, không tự quyết gì.</summary>
    internal sealed class MarkPublishedState
    {
        internal MarkPublishedState(MarkPublishedVariant variant, bool canMark, bool confirmSectionEnabled, string missingText,
            string headingText, string bodyText, string shaText, HealthState shaState, string copyButtonText)
        {
            Variant = variant;
            CanMark = canMark;
            ConfirmSectionEnabled = confirmSectionEnabled;
            MissingText = missingText ?? string.Empty;
            HeadingText = headingText ?? string.Empty;
            BodyText = bodyText ?? string.Empty;
            ShaText = shaText ?? string.Empty;
            ShaState = shaState;
            CopyButtonText = copyButtonText ?? string.Empty;
        }

        public MarkPublishedVariant Variant { get; }

        /// <summary>Nút "Ghi dấu đã đăng" bật: sha khớp VÀ đã tick VÀ ghi chú không rỗng [SD2 §3.11].</summary>
        public bool CanMark { get; }

        /// <summary>Toggle + ô ghi chú mở khoá; biến thể nháp đã đổi khoá cả hai cho tới khi copy bản mới.</summary>
        public bool ConfirmSectionEnabled { get; }

        /// <summary>"Còn thiếu: xác nhận đã dán, ghi chú" — in THÀNH CHỮ cạnh nút, tooltip chỉ phụ (SPIKE-B SP-3).</summary>
        public string MissingText { get; }

        public string HeadingText { get; }
        public string BodyText { get; }
        public string ShaText { get; }
        public HealthState ShaState { get; }

        /// <summary>"Copy JSON 91aa02" của biến thể nháp đã đổi; "" ở hai biến thể kia.</summary>
        public string CopyButtonText { get; }

        public bool HasCopyButton => CopyButtonText.Length > 0;
    }

    /// <summary>
    /// Điều kiện bật nút "Ghi dấu đã đăng" và câu chữ ba biến thể của hộp [SD2 §3.11]. Thuần: không đụng phiên, không đọc
    /// đồng hồ máy — nhờ vậy ba biến thể kiểm được bằng test Logic, không cần mở cửa sổ.
    /// <para>
    /// Vì sao có model riêng thay vì để hộp tự xét: cùng một điều kiện "đã copy đúng bản này" cũng quyết định nút
    /// "Đánh dấu đã đăng…" ở section header (qua <see cref="ExportGateState.MarkPublished"/>). Hai nơi đọc một luật thì luật
    /// phải nằm ngoài cả hai.
    /// </para>
    /// </summary>
    internal static class MarkPublishedModel
    {
        /// <summary>Đủ điều kiện ghi dấu chưa — chữ ký gọn cho test và cho nơi chỉ cần bật/tắt.</summary>
        public static bool CanMark(string lastExportedSha256Hex, string currentSha256Hex, bool confirmTicked, string note)
        {
            return ShaMatches(lastExportedSha256Hex, currentSha256Hex) && confirmTicked && !string.IsNullOrEmpty(TrimNote(note));
        }

        /// <summary>Phần còn thiếu, nối bằng ", " theo đúng thứ tự đọc của hộp; "" khi không thiếu gì.</summary>
        public static string MissingText(string lastExportedSha256Hex, string currentSha256Hex, bool confirmTicked, string note)
        {
            List<string> missing = new List<string>();
            if (!ShaMatches(lastExportedSha256Hex, currentSha256Hex)) missing.Add(LiveOpsHubStrings.ExportMarkMissingCopy);
            if (!confirmTicked) missing.Add(LiveOpsHubStrings.ExportMarkMissingConfirm);
            if (string.IsNullOrEmpty(TrimNote(note))) missing.Add(LiveOpsHubStrings.ExportMarkMissingNote);
            if (missing.Count == 0) return string.Empty;
            return LiveOpsHubStrings.ExportMarkMissingPrefix + string.Join(LiveOpsHubStrings.ExportMarkMissingSeparator, missing.ToArray());
        }

        /// <summary>Biến thể theo sha: khớp → trái, lệch → phải, chưa copy lần nào → nói thẳng là chưa copy.</summary>
        public static MarkPublishedVariant VariantOf(string lastExportedSha256Hex, string currentSha256Hex)
        {
            if (string.IsNullOrEmpty(lastExportedSha256Hex)) return MarkPublishedVariant.NeverExported;
            return ShaMatches(lastExportedSha256Hex, currentSha256Hex) ? MarkPublishedVariant.Ready : MarkPublishedVariant.DraftChanged;
        }

        /// <summary>Trạng thái đầy đủ (chữ + bật/tắt) của hộp.</summary>
        public static MarkPublishedState Evaluate(MarkPublishedInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input), LiveOpsHubStrings.ExportGateErrorInputMissing);

            MarkPublishedVariant variant = VariantOf(input.LastExportedSha256Hex, input.CurrentSha256Hex);
            bool shaMatches = variant == MarkPublishedVariant.Ready;
            bool canMark = CanMark(input.LastExportedSha256Hex, input.CurrentSha256Hex, input.ConfirmTicked, input.Note);
            string missing = MissingText(input.LastExportedSha256Hex, input.CurrentSha256Hex, input.ConfirmTicked, input.Note);
            string currentShort = ShortSha(input.CurrentSha256Hex);
            string exportedShort = ShortSha(input.LastExportedSha256Hex);

            string heading = shaMatches || variant == MarkPublishedVariant.NeverExported
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportMarkHeadingReadyFormat, currentShort)
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportMarkHeadingDraftChangedFormat,
                    ClockText(input.LastExportedUtc, input.Format));

            string body = variant == MarkPublishedVariant.DraftChanged
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportMarkBodyDraftChangedFormat, currentShort, exportedShort)
                // Câu ExportMarkBodyFormat ĐÃ mang chữ " UTC" và " byte"; dùng ShortDateTimeUtc/Bytes ở đây là in hai lần
                // ("09:04 UTC UTC", "1.612 byte byte") — nơi gọi chỉ đưa SỐ, đơn vị thuộc về câu.
                : LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ExportMarkBodyFormat), input.AssetFileName,
                    input.Format.ShortDateTime(input.NowUtc), input.Publisher, input.PublisherSource, currentShort,
                    input.Format.Integer(input.ByteCount));

            string shaText;
            HealthState shaState;
            string copyButton = string.Empty;
            if (variant == MarkPublishedVariant.NeverExported)
            {
                shaText = LiveOpsHubStrings.ExportGateMarkTooltipNotExported;
                shaState = HealthState.Blocked;
            }
            else if (!shaMatches)
            {
                shaText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportMarkShaBlockedFormat, exportedShort, currentShort);
                shaState = HealthState.Blocked;
                copyButton = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportMarkCopyNewButtonFormat, currentShort);
            }
            else if (input.CopiedInsideDialog)
            {
                // Vừa copy trong hộp: người dùng CHƯA dán bản mới lên Firebase, nên câu phải là lời nhắc dán lại rồi mới tick.
                shaText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportMarkShaCopiedFormat, currentShort,
                    ClockText(input.LastExportedUtc, input.Format));
                shaState = HealthState.Ok;
            }
            else
            {
                shaText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportMarkShaOkFormat, currentShort,
                    ClockText(input.LastExportedUtc, input.Format));
                shaState = HealthState.Ok;
            }

            // Phần xác nhận khoá đúng ở biến thể "nháp đã đổi": tick trước khi copy bản mới là khai sai thứ đã dán lên Firebase.
            bool confirmEnabled = variant != MarkPublishedVariant.DraftChanged;
            return new MarkPublishedState(variant, canMark, confirmEnabled, missing, heading, body, shaText, shaState, copyButton);
        }

        internal static bool ShaMatches(string lastExportedSha256Hex, string currentSha256Hex)
        {
            if (string.IsNullOrEmpty(lastExportedSha256Hex) || string.IsNullOrEmpty(currentSha256Hex)) return false;
            return string.Equals(lastExportedSha256Hex, currentSha256Hex, StringComparison.Ordinal);
        }

        internal static string ShortSha(string sha256Hex)
        {
            if (string.IsNullOrEmpty(sha256Hex)) return string.Empty;
            return sha256Hex.Length >= ShortShaLength ? sha256Hex.Substring(0, ShortShaLength) : sha256Hex;
        }

        private static string TrimNote(string note)
        {
            return note == null ? string.Empty : note.Trim();
        }

        /// <summary>"09:02" giờ UTC — câu của hộp tự mang chữ UTC, nên ở đây chỉ là giờ trần, không đổi sang giờ máy.</summary>
        private static string ClockText(DateTime? utc, LiveOpsHubFormat format)
        {
            // format vẫn nằm trong chữ ký: nơi gọi luôn có sẵn, và đổi cách in giờ sau này không phải sửa mọi chỗ gọi.
            if (format == null || !utc.HasValue) return string.Empty;
            return utc.Value.ToString(ClockPattern, CultureInfo.InvariantCulture);
        }

        private const string ClockPattern = "HH:mm";

        /// <summary>Sáu ký tự hex đầu, đúng như <c>LiveEventCalendarJsonText.ShortSha</c> và <c>PublishedCalendarStamp.ShortSha</c>.</summary>
        private const int ShortShaLength = 6;
    }
}
