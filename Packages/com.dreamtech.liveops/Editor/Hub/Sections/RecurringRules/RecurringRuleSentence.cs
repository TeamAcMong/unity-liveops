using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Câu đọc được của luật ([SD1 §4.1]): "Mỗi [7 ngày], neo từ [thứ Hai 5/1/2026 00:00 UTC], mỗi đợt chạy [7 ngày],
    /// id = [pass-] + số thứ tự." Bốn giá trị là token bấm được — bấm token thì focus đúng field, nên người đọc câu trước rồi
    /// mới đi vào ô cần sửa, thay vì dò nhãn.
    /// <para>
    /// Token của field đang focus mang class <c>--highlighted</c>; token đang gây hậu quả "người chơi mất tiến độ" mang
    /// <c>--warning</c>. Hai class tách nhau vì một token có thể vừa focus vừa warning.
    /// </para>
    /// </summary>
    internal sealed class RecurringRuleSentence : VisualElement
    {
        private readonly List<Button> _tokenButtons = new List<Button>();
        private readonly List<string> _tokenFields = new List<string>();

        internal RecurringRuleSentence()
        {
            AddToClassList(LiveOpsHubClassNames.RuleSentence);
        }

        /// <summary>Người dùng bấm một token — tham số là tên field (<see cref="RecurringRuleFields"/>).</summary>
        public event Action<string> TokenClicked;

        public int TokenCount => _tokenButtons.Count;

        public void SetTokens(IReadOnlyList<RecurringSentenceToken> tokens)
        {
            Clear();
            _tokenButtons.Clear();
            _tokenFields.Clear();
            if (tokens == null) return;

            // (UX-21) Câu wrap theo CỤM: mỗi "chữ nối + token" là một khối không tách. Thả thẳng chữ và token làm con của
            // câu thì wrap bẻ ngay giữa ", neo từ" và ngày tháng — người đọc mất đúng mối nối đang đọc dở.
            VisualElement group = NewGroup();
            for (int index = 0; index < tokens.Count; index++)
            {
                RecurringSentenceToken token = tokens[index];
                if (!token.IsToken)
                {
                    // Chữ nối mở một cụm MỚI: cụm cũ đã đủ (chữ nối cũ + token của nó), chỗ ngắt dòng hợp lý là ở đây.
                    if (group.childCount > 0) group = NewGroup();
                    Label text = new Label(token.Text);
                    // (UX-32) Label của UI Toolkit có padding mặc định: "7 ngày" + ", neo từ" đọc thành "7 ngày , neo từ".
                    text.AddToClassList(LiveOpsHubClassNames.RecurringSentenceText);
                    group.Add(text);
                    continue;
                }
                string fieldName = token.FieldName;
                Button button = new Button(() => RaiseTokenClicked(fieldName)) { name = TokenElementName(fieldName), text = token.Text };
                button.AddToClassList(LiveOpsHubClassNames.RuleToken);
                button.EnableInClassList(LiveOpsHubClassNames.Mono, token.IsMono);
                button.EnableInClassList(LiveOpsHubClassNames.RuleTokenWarning, token.State == RecurringTokenState.Warning);
                button.EnableInClassList(LiveOpsHubClassNames.RuleTokenHighlighted, token.State == RecurringTokenState.Highlighted);
                // (J2-06) Nhìn tới MẢNH KẾ: nó bắt đầu bằng dấu câu thì padding phải của chip đẩy dấu ấy rời khỏi chữ, phải kéo lại.
                // Quyết định ở ĐÂY chứ không ở USS vì USS không đọc được ký tự đầu của phần tử đứng sau.
                button.EnableInClassList(LiveOpsHubClassNames.RuleTokenFollowedByPunctuation,
                    StartsWithPunctuation(index + 1 < tokens.Count ? tokens[index + 1] : null));
                group.Add(button);
                _tokenButtons.Add(button);
                _tokenFields.Add(fieldName);
            }
        }

        /// <summary>Viền token của field đang focus; "" gỡ hết viền.</summary>
        public void SetFocusedField(string fieldName)
        {
            for (int index = 0; index < _tokenButtons.Count; index++)
            {
                _tokenButtons[index].EnableInClassList(LiveOpsHubClassNames.RuleTokenHighlighted,
                    fieldName != null && string.Equals(_tokenFields[index], fieldName, StringComparison.Ordinal));
            }
        }

        /// <summary>
        /// (J2-06) Mảnh đứng sau chip có mở đầu bằng dấu câu không — tức dấu ấy thuộc về chữ TRONG chip và phải dính vào nó.
        /// Mảnh mở đầu bằng khoảng trắng (" + số thứ tự.") thì khoảng trắng ấy là khoảng cách THẬT của câu, không được kéo lại.
        /// </summary>
        private static bool StartsWithPunctuation(RecurringSentenceToken next)
        {
            if (next == null || next.IsToken) return false;
            string text = next.Text;
            if (string.IsNullOrEmpty(text)) return false;
            return IsSentencePunctuation(text[0]);
        }

        /// <summary>Dấu câu có thể mở đầu một mảnh nối của câu luật ở cả hai ngôn ngữ — xem <see cref="StartsWithPunctuation"/>.</summary>
        private static bool IsSentencePunctuation(char character)
        {
            for (int index = 0; index < SentencePunctuation.Length; index++)
            {
                if (SentencePunctuation[index] == character) return true;
            }
            return false;
        }

        private static readonly char[] SentencePunctuation = { ',', '.', ';', ':', ')', ']', '!', '?' };

        internal static string TokenElementName(string fieldName)
        {
            return TokenElementPrefix + fieldName;
        }

        internal const string TokenElementPrefix = "recurring-token-";

        /// <summary>Một cụm mới của câu, đã treo vào câu — cụm rỗng ở cuối không hại gì, nó cao 0 và không có chữ.</summary>
        private VisualElement NewGroup()
        {
            VisualElement group = new VisualElement();
            group.AddToClassList(LiveOpsHubClassNames.RecurringSentenceGroup);
            Add(group);
            return group;
        }

        private void RaiseTokenClicked(string fieldName)
        {
            if (TokenClicked != null) TokenClicked(fieldName);
        }
    }
}
