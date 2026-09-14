using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Cấp hộp xác nhận (8.6): cấp 1 = một hộp có nút an toàn focus sẵn; cấp 2 = phải gõ đúng id đang chạy.</summary>
    internal enum LiveOpsConfirmLevel
    {
        Level1 = 1,
        TypeToConfirm = 2,
    }

    /// <summary>Kết quả hộp xác nhận. Enter/Esc/đóng cửa sổ đều là <see cref="Safe"/> — chỉ bấm nút phá huỷ mới ra Destructive.</summary>
    internal enum LiveOpsConfirmResult
    {
        Safe = 0,
        Destructive = 1,
    }

    /// <summary>
    /// Dữ liệu một hộp xác nhận (8.6) — tách khỏi cửa sổ modal để section dựng câu, test đọc lại câu qua
    /// <c>ScriptedLiveOpsHubConfirmationPresenter</c>, và <c>LiveOpsConfirmContent</c> (G-FEEDBACK) vẽ được mà không gọi
    /// <c>ShowModalUtility</c>. Dựng bằng <see cref="Builder"/>; <see cref="Builder.Build"/> ném với request thiếu phần
    /// bắt buộc vì đó là lỗi của code dựng hộp, không phải dữ liệu người dùng.
    /// </summary>
    internal sealed class LiveOpsConfirmRequest
    {
        private LiveOpsConfirmRequest(Builder builder, string keyHint)
        {
            Level = builder.Level;
            Title = builder.Title;
            Body = builder.Body;
            BodyStyle = builder.BodyStyle;
            KeyHint = keyHint;
            DestructiveLabel = builder.DestructiveLabel;
            SafeLabel = builder.SafeLabel;
            TypeToConfirmText = builder.TypeToConfirmText;
            BodyTooltip = builder.BodyTooltip;
        }

        public LiveOpsConfirmLevel Level { get; }

        /// <summary>Động từ + đối tượng ("Xoá đợt weekly-pass-35?"), không "Bạn có chắc?".</summary>
        public string Title { get; }

        public string Body { get; }

        /// <summary><see cref="HelpBoxMessageType.None"/> (thân chữ thường) hoặc Warning (HelpBox ở cấp 2).</summary>
        public HelpBoxMessageType BodyStyle { get; }

        /// <summary>"Enter / Esc: Giữ lại" — mặc định suy từ cấp và nhãn nút an toàn khi builder không đặt.</summary>
        public string KeyHint { get; }

        public string DestructiveLabel { get; }
        public string SafeLabel { get; }

        /// <summary>Chuỗi phải gõ đúng (Ordinal) để mở nút phá huỷ ở cấp 2; "" ở cấp 1.</summary>
        public string TypeToConfirmText { get; }

        public string BodyTooltip { get; }

        internal sealed class Builder
        {
            internal LiveOpsConfirmLevel Level { get; private set; } = LiveOpsConfirmLevel.Level1;
            internal string Title { get; private set; } = string.Empty;
            internal string Body { get; private set; } = string.Empty;
            internal HelpBoxMessageType BodyStyle { get; private set; } = HelpBoxMessageType.None;
            internal string KeyHint { get; private set; } = string.Empty;
            internal string DestructiveLabel { get; private set; } = string.Empty;
            internal string SafeLabel { get; private set; } = LiveOpsHubStrings.KitConfirmKeepLabel;
            internal string TypeToConfirmText { get; private set; } = string.Empty;
            internal string BodyTooltip { get; private set; } = string.Empty;

            public Builder WithLevel(LiveOpsConfirmLevel level)
            {
                Level = level;
                return this;
            }

            public Builder WithTitle(string title)
            {
                Title = title ?? string.Empty;
                return this;
            }

            public Builder WithBody(string body)
            {
                Body = body ?? string.Empty;
                return this;
            }

            public Builder WithHelpBoxWarning()
            {
                BodyStyle = HelpBoxMessageType.Warning;
                return this;
            }

            public Builder WithKeyHint(string keyHint)
            {
                KeyHint = keyHint ?? string.Empty;
                return this;
            }

            public Builder WithButtons(string destructiveLabel, string safeLabel)
            {
                DestructiveLabel = destructiveLabel ?? string.Empty;
                SafeLabel = safeLabel ?? string.Empty;
                return this;
            }

            /// <summary>Chuyển sang cấp 2 và bật HelpBox cảnh báo — cấp 2 luôn đụng đợt đang chạy nên thân luôn là cảnh báo (8.6).</summary>
            public Builder WithTypeToConfirm(string typeToConfirmText)
            {
                Level = LiveOpsConfirmLevel.TypeToConfirm;
                TypeToConfirmText = typeToConfirmText ?? string.Empty;
                BodyStyle = HelpBoxMessageType.Warning;
                return this;
            }

            public Builder WithBodyTooltip(string bodyTooltip)
            {
                BodyTooltip = bodyTooltip ?? string.Empty;
                return this;
            }

            public LiveOpsConfirmRequest Build()
            {
                if (Title.Length == 0) throw new ArgumentException(LiveOpsHubStrings.KitErrorConfirmTitleEmpty);
                if (DestructiveLabel.Length == 0 || SafeLabel.Length == 0)
                {
                    throw new ArgumentException(LiveOpsHubStrings.KitErrorConfirmButtonLabelsEmpty);
                }
                if (Level == LiveOpsConfirmLevel.TypeToConfirm && TypeToConfirmText.Length == 0)
                {
                    throw new ArgumentException(LiveOpsHubStrings.KitErrorConfirmTypeToConfirmTextEmpty);
                }
                // Cấp 1 không vẽ ô gõ: chuỗi cần gõ mà bị bỏ qua im lặng thì hộp nhẹ hơn ý người viết — báo sớm.
                if (Level == LiveOpsConfirmLevel.Level1 && TypeToConfirmText.Length > 0)
                {
                    throw new ArgumentException(LiveOpsHubStrings.KitErrorConfirmLevel1WithTypeText);
                }
                string keyHint = KeyHint.Length > 0 ? KeyHint : DefaultKeyHint(Level, SafeLabel);
                return new LiveOpsConfirmRequest(this, keyHint);
            }

            private static string DefaultKeyHint(LiveOpsConfirmLevel level, string safeLabel)
            {
                // Cấp 2: Enter trong ô gõ không chạy nút nào, nên gợi ý nói rõ "Enter không đổi gì" thay vì hứa Enter = an toàn.
                string format = level == LiveOpsConfirmLevel.TypeToConfirm
                    ? LiveOpsHubStrings.KitConfirmTypeToConfirmKeyHintFormat
                    : LiveOpsHubStrings.KitConfirmLevel1KeyHintFormat;
                return string.Format(CultureInfo.InvariantCulture, format, safeLabel);
            }
        }
    }
}
