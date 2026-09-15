using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// JSON viewer của Xuất JSON ([FD §2.6], [SD2 §3.6], kế hoạch 7.6): ListView dòng 16px — số dòng 32px, lề ký hiệu 14px (icon lỗi /
    /// cảnh báo 12px, <c>+</c> <c>~</c> <c>–</c>), code mono có màu cú pháp, chú thích cuối dòng; dòng lỗi/cảnh báo tint 0,16; dải tổng
    /// quan 10px bên phải; tab "Đã định dạng | Một dòng" (PD-12).
    ///
    /// Màu cú pháp là thẻ rich text <c>&lt;color=#…&gt;</c>, màu đọc từ token khai lại trên chính <c>.liveops-hub-json-view</c> — rich
    /// text không đọc được <c>var()</c> và custom property không kế thừa từ root ([API §12.2]). MỌI giá trị thô bọc
    /// <c>&lt;noparse&gt;</c> trước khi ghép thẻ: JSON dán vào có thể chứa <c>&lt;b&gt;</c> hay <c>&lt;color&gt;</c>, và thẻ bị hiểu thì
    /// dòng hiện sai chữ so với byte sẽ đăng — người đọc duyệt nhầm nội dung (test <c>JsonView_NoParse_ShowsTagsLiterally</c>).
    ///
    /// Control không dựng câu chữ: chú thích nhận câu đã dựng sẵn qua <see cref="SetAnnotations"/> (V-21 D-5; presenter lấy từ
    /// <c>LiveOpsFindingText</c>), lề +/~/– nhận qua <see cref="SetLineChanges"/> (presenter lấy từ diff) — không phụ thuộc
    /// <c>LiveEventCalendarFinding</c> hay G-FINDINGTEXT.
    /// </summary>
    public sealed partial class LiveOpsJsonView : VisualElement
    {
        internal static readonly CustomStyleProperty<Color> KeyColorProperty = new CustomStyleProperty<Color>("--liveops-hub-json-view-key");
        internal static readonly CustomStyleProperty<Color> StringColorProperty = new CustomStyleProperty<Color>("--liveops-hub-json-view-string");
        internal static readonly CustomStyleProperty<Color> NumberColorProperty = new CustomStyleProperty<Color>("--liveops-hub-json-view-number");
        internal static readonly CustomStyleProperty<Color> LiteralColorProperty = new CustomStyleProperty<Color>("--liveops-hub-json-view-literal");

        internal const float LineHeight = 16f;
        internal const int FormattedTabIndex = 0;
        internal const int SingleLineTabIndex = 1;
        private const int GutterIconSize = 12;
        private const string ErrorIconName = "console.erroricon.sml";
        private const string WarningIconName = "console.warnicon.sml";
        private const string NoParseOpen = "<noparse>";
        private const string NoParseClose = "</noparse>";
        // "</noparse>" nằm TRONG giá trị thô sẽ đóng khối noparse sớm. Tách nó thành hai khối: "</" rồi "noparse>" — mỗi khối không
        // chứa chuỗi đóng hoàn chỉnh nên hiện nguyên văn.
        private const string EscapedNoParseClose = "</" + NoParseClose + NoParseOpen + "noparse>";
        private const string AnnotationSeparator = " · ";
        private const double PercentScale = 100d;

        private readonly List<int> _lineIndices = new List<int>();
        private readonly List<VisualElement> _overviewMarkers = new List<VisualElement>();
        private readonly Dictionary<int, LineDecoration> _decorations = new Dictionary<int, LineDecoration>();
        private IReadOnlyList<LiveOpsJsonLineAnnotation> _annotations = Array.Empty<LiveOpsJsonLineAnnotation>();
        private IReadOnlyList<LiveOpsJsonLineChange> _changes = Array.Empty<LiveOpsJsonLineChange>();
        private string[] _formattedLines = Array.Empty<string>();
        private string[] _singleLine = Array.Empty<string>();
        private string[] _richLines = Array.Empty<string>();
        private JsonSyntaxColors _colors;
        private bool _isSingleLine;
        private ScrollView _scrollView;

        public LiveOpsJsonView()
        {
            AddToClassList(LiveOpsHubClassNames.JsonView);

            ModeTabs = new LiveOpsTabStrip { Choices = LiveOpsHubStrings.JsonViewFormattedTab + LiveOpsTabStrip.ChoiceSeparator + LiveOpsHubStrings.JsonViewSingleLineTab };
            ModeTabs.AddToClassList(LiveOpsHubClassNames.JsonViewTabs);
            ModeTabs.SelectedIndexChanged += index => IsSingleLine = index == SingleLineTabIndex;
            Add(ModeTabs);

            VisualElement body = new VisualElement();
            body.AddToClassList(LiveOpsHubClassNames.JsonViewBody);
            Add(body);

            LineList = new ListView(_lineIndices, LineHeight, MakeLine, BindLine)
            {
                selectionType = SelectionType.None,
                horizontalScrollingEnabled = false,
            };
            LineList.AddToClassList(LiveOpsHubClassNames.JsonViewList);
            body.Add(LineList);

            Overview = new VisualElement();
            Overview.AddToClassList(LiveOpsHubClassNames.JsonOverview);
            body.Add(Overview);
            OverviewViewport = new VisualElement { pickingMode = PickingMode.Ignore };
            OverviewViewport.AddToClassList(LiveOpsHubClassNames.JsonOverviewViewport);
            Overview.Add(OverviewViewport);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            LineList.RegisterCallback<GeometryChangedEvent>(geometryEvent => UpdateViewport());
        }

        /// <summary>Số dòng đang hiện (cách xem hiện tại).</summary>
        internal int LineCount => _lineIndices.Count;

        /// <summary>true = tab "Một dòng": một dòng không lề, không chú thích, không dải tổng quan (số dòng chỉ có nghĩa ở bản định dạng).</summary>
        public bool IsSingleLine
        {
            get => _isSingleLine;
            set
            {
                if (_isSingleLine == value) return;
                _isSingleLine = value;
                ModeTabs.SetSelectedIndexWithoutNotify(value ? SingleLineTabIndex : FormattedTabIndex);
                RebuildLines();
            }
        }

        internal LiveOpsTabStrip ModeTabs { get; }
        internal ListView LineList { get; }
        internal VisualElement Overview { get; }
        internal VisualElement OverviewViewport { get; }
        internal IReadOnlyList<VisualElement> OverviewMarkers => _overviewMarkers;

        /// <summary>
        /// Nội dung hai cách xem: bản định dạng (nhiều dòng, LF hoặc CRLF) và bản một dòng. Thay nội dung giữ nguyên chú thích và lề
        /// đã đặt — người gọi đặt lại khi số dòng đổi nghĩa.
        /// </summary>
        public void SetText(string formattedText, string singleLineText)
        {
            _formattedLines = SplitLines(formattedText);
            _singleLine = new[] { StripLineEnd(singleLineText ?? string.Empty) };
            RebuildLines();
        }

        /// <summary>
        /// Thay toàn bộ chú thích cuối dòng; rỗng = không chú thích. Nhiều chú thích cùng dòng: mức nặng nhất quyết định icon/tint,
        /// câu nối bằng " · ". internal vì <see cref="HealthState"/> internal (CS0051).
        /// </summary>
        internal void SetAnnotations(IReadOnlyList<LiveOpsJsonLineAnnotation> annotations)
        {
            _annotations = annotations ?? Array.Empty<LiveOpsJsonLineAnnotation>();
            RebuildDecorations();
        }

        /// <summary>Thay toàn bộ dấu lề +/~/– (diff với bản so); rỗng = không dấu.</summary>
        internal void SetLineChanges(IReadOnlyList<LiveOpsJsonLineChange> changes)
        {
            _changes = changes ?? Array.Empty<LiveOpsJsonLineChange>();
            RebuildDecorations();
        }

        /// <summary>Cuộn tới dòng (1-based) — bấm vạch dải tổng quan, "Tới dòng" của phát hiện.</summary>
        public void ScrollToLine(int lineNumber)
        {
            if (lineNumber < 1 || lineNumber > _lineIndices.Count) return;
            LineList.ScrollToItem(lineNumber - 1);
        }

        /// <summary>Chuỗi rich text của dòng (1-based) đang hiện — test đọc để kiểm noparse và màu.</summary>
        internal string RichTextOfLine(int lineNumber)
        {
            return _richLines[lineNumber - 1];
        }

        /// <summary>Màu cú pháp đã đọc từ token; <see cref="JsonSyntaxColors.IsResolved"/> false khi stylesheet controls chưa nạp.</summary>
        internal JsonSyntaxColors Colors => _colors;

        /// <summary>
        /// Một dòng JSON → rich text: dấu câu và khoảng trắng giữ nguyên; key, chuỗi, số, true/false/null bọc màu; mọi giá trị thô bọc
        /// noparse. Không phải parser: dòng hỏng (JSON dán lỗi) vẫn hiện đủ chữ, chỉ mất màu ở chỗ không nhận ra.
        /// </summary>
        internal static string BuildRichLine(string rawLine, JsonSyntaxColors colors)
        {
            string line = rawLine ?? string.Empty;
            StringBuilder builder = new StringBuilder(line.Length * 2 + 32);
            int position = 0;
            while (position < line.Length)
            {
                char character = line[position];
                if (character == ' ' || character == '\t' || IsPunctuation(character))
                {
                    // Khoảng trắng và {}[],: không tạo được thẻ — ghi thẳng cho chuỗi ngắn.
                    builder.Append(character);
                    position++;
                    continue;
                }
                if (character == '"')
                {
                    int end = FindStringEnd(line, position);
                    string token = line.Substring(position, end - position);
                    bool isKey = NextNonSpaceIs(line, end, ':');
                    AppendColored(builder, token, isKey ? colors.KeyHex : colors.StringHex);
                    position = end;
                    continue;
                }
                if (character == '-' || char.IsDigit(character))
                {
                    int end = position + 1;
                    while (end < line.Length && IsNumberCharacter(line[end])) end++;
                    AppendColored(builder, line.Substring(position, end - position), colors.NumberHex);
                    position = end;
                    continue;
                }
                if (char.IsLetter(character))
                {
                    int end = position + 1;
                    while (end < line.Length && char.IsLetter(line[end])) end++;
                    string word = line.Substring(position, end - position);
                    bool isLiteral = string.Equals(word, "true", StringComparison.Ordinal) || string.Equals(word, "false", StringComparison.Ordinal) ||
                                     string.Equals(word, "null", StringComparison.Ordinal);
                    AppendColored(builder, word, isLiteral ? colors.LiteralHex : null);
                    position = end;
                    continue;
                }
                int otherEnd = position + 1;
                while (otherEnd < line.Length && !IsTokenStart(line[otherEnd])) otherEnd++;
                AppendColored(builder, line.Substring(position, otherEnd - position), null);
                position = otherEnd;
            }
            return builder.ToString();
        }

        /// <summary>Bọc chuỗi thô trong noparse, kể cả khi chuỗi chứa sẵn "&lt;/noparse&gt;".</summary>
        internal static string ProtectRawText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            StringBuilder builder = new StringBuilder(raw.Length + NoParseOpen.Length + NoParseClose.Length);
            builder.Append(NoParseOpen);
            int start = 0;
            while (true)
            {
                int found = raw.IndexOf(NoParseClose, start, StringComparison.OrdinalIgnoreCase);
                if (found < 0) break;
                builder.Append(raw, start, found - start);
                builder.Append(EscapedNoParseClose);
                start = found + NoParseClose.Length;
            }
            builder.Append(raw, start, raw.Length - start);
            builder.Append(NoParseClose);
            return builder.ToString();
        }

        private static void AppendColored(StringBuilder builder, string raw, string colorHex)
        {
            if (string.IsNullOrEmpty(colorHex))
            {
                builder.Append(ProtectRawText(raw));
                return;
            }
            builder.Append("<color=#").Append(colorHex).Append('>').Append(ProtectRawText(raw)).Append("</color>");
        }

        private static int FindStringEnd(string line, int openingQuote)
        {
            int position = openingQuote + 1;
            while (position < line.Length)
            {
                char character = line[position];
                if (character == '\\')
                {
                    position += 2;
                    continue;
                }
                position++;
                if (character == '"') return position;
            }
            return line.Length;
        }

        private static bool NextNonSpaceIs(string line, int start, char expected)
        {
            for (int position = start; position < line.Length; position++)
            {
                if (line[position] == ' ' || line[position] == '\t') continue;
                return line[position] == expected;
            }
            return false;
        }

        private static bool IsPunctuation(char character)
        {
            return character == '{' || character == '}' || character == '[' || character == ']' || character == ',' || character == ':';
        }

        private static bool IsNumberCharacter(char character)
        {
            return char.IsDigit(character) || character == '.' || character == 'e' || character == 'E' || character == '+' || character == '-';
        }

        private static bool IsTokenStart(char character)
        {
            return character == ' ' || character == '\t' || character == '"' || IsPunctuation(character) || character == '-' || char.IsLetterOrDigit(character);
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
            string[] lines = text.Split('\n');
            for (int index = 0; index < lines.Length; index++) lines[index] = StripLineEnd(lines[index]);
            return lines;
        }

        private static string StripLineEnd(string line)
        {
            return line.Length > 0 && line[line.Length - 1] == '\r' ? line.Substring(0, line.Length - 1) : line;
        }

        private string[] VisibleLines => _isSingleLine ? _singleLine : _formattedLines;

        private void RebuildLines()
        {
            string[] lines = VisibleLines;
            _lineIndices.Clear();
            for (int index = 0; index < lines.Length; index++) _lineIndices.Add(index);
            RebuildRichLines();
            LineList.horizontalScrollingEnabled = _isSingleLine;
            RebuildDecorations();
        }

        private void RebuildRichLines()
        {
            string[] lines = VisibleLines;
            _richLines = new string[lines.Length];
            for (int index = 0; index < lines.Length; index++) _richLines[index] = BuildRichLine(lines[index], _colors);
        }

        private void RebuildDecorations()
        {
            _decorations.Clear();
            if (!_isSingleLine)
            {
                foreach (LiveOpsJsonLineAnnotation annotation in _annotations)
                {
                    if (annotation.LineNumber < 1 || annotation.LineNumber > _lineIndices.Count) continue;
                    LineDecoration decoration = DecorationOf(annotation.LineNumber);
                    decoration.AddAnnotation(annotation.Severity, annotation.Text);
                }
                foreach (LiveOpsJsonLineChange change in _changes)
                {
                    if (change.LineNumber < 1 || change.LineNumber > _lineIndices.Count || change.Kind == LiveOpsJsonLineChangeKind.None) continue;
                    DecorationOf(change.LineNumber).Change = change.Kind;
                }
            }
            RebuildOverviewMarkers();
            LineList.RefreshItems();
            UpdateViewport();
        }

        private LineDecoration DecorationOf(int lineNumber)
        {
            if (!_decorations.TryGetValue(lineNumber, out LineDecoration decoration))
            {
                decoration = new LineDecoration();
                _decorations[lineNumber] = decoration;
            }
            return decoration;
        }

        private VisualElement MakeLine()
        {
            return new JsonLineRow();
        }

        private void BindLine(VisualElement element, int index)
        {
            JsonLineRow row = (JsonLineRow)element;
            int lineNumber = index + 1;
            row.Number.text = _isSingleLine ? string.Empty : lineNumber.ToString(CultureInfo.InvariantCulture);
            row.Code.text = index < _richLines.Length ? _richLines[index] : string.Empty;

            _decorations.TryGetValue(lineNumber, out LineDecoration decoration);
            HealthState? severity = decoration?.Severity;
            row.EnableInClassList(LiveOpsHubClassNames.JsonLineBlocked, severity == HealthState.Blocked);
            row.EnableInClassList(LiveOpsHubClassNames.JsonLineWarning, severity == HealthState.Warning);

            // Lề: ✕/! thắng +/~/– (dòng lỗi nằm trong khối thêm mới vẫn mang icon lỗi).
            string iconName = severity == HealthState.Blocked ? ErrorIconName : severity == HealthState.Warning ? WarningIconName : null;
            row.GutterIcon.image = iconName == null ? null : LiveOpsHubIcons.Get(iconName);
            row.GutterIcon.EnableInClassList(LiveOpsHubClassNames.JsonPartHidden, iconName == null);
            string mark = iconName == null && decoration != null ? GutterMarkOf(decoration.Change) : string.Empty;
            row.GutterMark.text = mark;
            row.GutterMark.EnableInClassList(LiveOpsHubClassNames.JsonPartHidden, string.IsNullOrEmpty(mark));

            string annotationText = decoration?.Text ?? string.Empty;
            row.Annotation.text = annotationText;
            row.Annotation.EnableInClassList(LiveOpsHubClassNames.JsonPartHidden, annotationText.Length == 0);
            if (annotationText.Length > 0)
            {
                LiveOpsHubStyle.SetStateText(row.Annotation, severity == HealthState.Blocked || severity == HealthState.Warning ? severity.Value : HealthState.NotMeasured);
            }
        }

        private static string GutterMarkOf(LiveOpsJsonLineChangeKind kind)
        {
            switch (kind)
            {
                case LiveOpsJsonLineChangeKind.Added: return LiveOpsHubStrings.JsonGutterAdded;
                case LiveOpsJsonLineChangeKind.Modified: return LiveOpsHubStrings.JsonGutterModified;
                case LiveOpsJsonLineChangeKind.Removed: return LiveOpsHubStrings.JsonGutterRemoved;
                default: return string.Empty;
            }
        }

        private void RebuildOverviewMarkers()
        {
            foreach (VisualElement marker in _overviewMarkers) marker.RemoveFromHierarchy();
            _overviewMarkers.Clear();
            int lineCount = _lineIndices.Count;
            Overview.EnableInClassList(LiveOpsHubClassNames.JsonPartHidden, _isSingleLine);
            if (_isSingleLine || lineCount == 0) return;

            List<int> lineNumbers = new List<int>(_decorations.Keys);
            lineNumbers.Sort();
            LiveOpsJsonLineChangeKind previousChange = LiveOpsJsonLineChangeKind.None;
            int previousLine = 0;
            foreach (int lineNumber in lineNumbers)
            {
                LineDecoration decoration = _decorations[lineNumber];
                if (decoration.Severity == HealthState.Blocked)
                {
                    AddMarker(lineNumber, lineCount, LiveOpsHubClassNames.JsonOverviewMarkerDropped, LiveOpsHubStrings.JsonOverviewDroppedTooltipFormat);
                }
                else if (decoration.Severity == HealthState.Warning)
                {
                    AddMarker(lineNumber, lineCount, LiveOpsHubClassNames.JsonOverviewMarkerWarning, LiveOpsHubStrings.JsonOverviewWarningTooltipFormat);
                }

                LiveOpsJsonLineChangeKind change = decoration.Change;
                // Một khối + liên tiếp chỉ vẽ một vạch ở dòng đầu ([SD2 §3.6]) — 20 dòng của mục thêm mới không thành vệt đặc.
                bool continuesAddedRun = change == LiveOpsJsonLineChangeKind.Added && previousChange == LiveOpsJsonLineChangeKind.Added && previousLine == lineNumber - 1;
                if (change != LiveOpsJsonLineChangeKind.None && !continuesAddedRun)
                {
                    AddMarker(lineNumber, lineCount, LiveOpsHubClassNames.JsonOverviewMarkerChange, LiveOpsHubStrings.JsonOverviewChangeTooltipFormat);
                }
                previousChange = change;
                previousLine = lineNumber;
            }
            OverviewViewport.BringToFront();
        }

        private void AddMarker(int lineNumber, int lineCount, string kindClass, string tooltipFormat)
        {
            VisualElement marker = new VisualElement
            {
                tooltip = string.Format(CultureInfo.InvariantCulture, tooltipFormat, lineNumber),
                userData = lineNumber,
            };
            marker.AddToClassList(LiveOpsHubClassNames.JsonOverviewMarker);
            marker.AddToClassList(kindClass);
            float topPercent = (float)((lineNumber - 1) * PercentScale / lineCount);
            marker.style.top = new Length(topPercent, LengthUnit.Percent); // style-inline-allowed: 5
            marker.RegisterCallback<PointerDownEvent>(pointerEvent => ScrollToLine((int)marker.userData));
            Overview.Add(marker);
            _overviewMarkers.Add(marker);
        }

        private void UpdateViewport()
        {
            if (_scrollView == null)
            {
                _scrollView = LineList.Q<ScrollView>();
                if (_scrollView != null) _scrollView.verticalScroller.valueChanged += scrollValue => UpdateViewport();
            }
            float totalHeight = _lineIndices.Count * LineHeight;
            if (_scrollView == null || totalHeight <= 0f || float.IsNaN(_scrollView.contentViewport.layout.height))
            {
                OverviewViewport.EnableInClassList(LiveOpsHubClassNames.JsonPartHidden, true);
                return;
            }
            float viewportHeight = _scrollView.contentViewport.layout.height;
            float topPercent = Mathf.Clamp(_scrollView.scrollOffset.y / totalHeight, 0f, 1f) * (float)PercentScale;
            float heightPercent = Mathf.Clamp(viewportHeight / totalHeight, 0f, 1f) * (float)PercentScale;
            OverviewViewport.EnableInClassList(LiveOpsHubClassNames.JsonPartHidden, heightPercent >= (float)PercentScale);
            OverviewViewport.style.top = new Length(topPercent, LengthUnit.Percent); // style-inline-allowed: 5
            OverviewViewport.style.height = new Length(heightPercent, LengthUnit.Percent); // style-inline-allowed: 5
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent resolvedEvent)
        {
            JsonSyntaxColors resolved = JsonSyntaxColors.Read(customStyle);
            if (resolved.Equals(_colors)) return;
            _colors = resolved;
            RebuildRichLines();
            LineList.RefreshItems();
        }

        /// <summary>Hex màu cú pháp (RRGGBBAA) cho thẻ rich text; null = không tô (stylesheet chưa nạp → vẫn đủ chữ).</summary>
        internal readonly struct JsonSyntaxColors : IEquatable<JsonSyntaxColors>
        {
            private JsonSyntaxColors(string keyHex, string stringHex, string numberHex, string literalHex)
            {
                KeyHex = keyHex;
                StringHex = stringHex;
                NumberHex = numberHex;
                LiteralHex = literalHex;
            }

            public string KeyHex { get; }
            public string StringHex { get; }
            public string NumberHex { get; }
            public string LiteralHex { get; }
            public bool IsResolved => KeyHex != null;

            public static JsonSyntaxColors FromColors(Color key, Color stringColor, Color number, Color literal)
            {
                return new JsonSyntaxColors(ColorUtility.ToHtmlStringRGBA(key), ColorUtility.ToHtmlStringRGBA(stringColor),
                    ColorUtility.ToHtmlStringRGBA(number), ColorUtility.ToHtmlStringRGBA(literal));
            }

            public static JsonSyntaxColors Read(ICustomStyle style)
            {
                if (!style.TryGetValue(KeyColorProperty, out Color key) || !style.TryGetValue(StringColorProperty, out Color stringColor) ||
                    !style.TryGetValue(NumberColorProperty, out Color number) || !style.TryGetValue(LiteralColorProperty, out Color literal))
                {
                    return default;
                }
                return FromColors(key, stringColor, number, literal);
            }

            public bool Equals(JsonSyntaxColors other)
            {
                return string.Equals(KeyHex, other.KeyHex, StringComparison.Ordinal) && string.Equals(StringHex, other.StringHex, StringComparison.Ordinal) &&
                       string.Equals(NumberHex, other.NumberHex, StringComparison.Ordinal) && string.Equals(LiteralHex, other.LiteralHex, StringComparison.Ordinal);
            }

            public override bool Equals(object other)
            {
                return other is JsonSyntaxColors colors && Equals(colors);
            }

            public override int GetHashCode()
            {
                return KeyHex == null ? 0 : StringComparer.Ordinal.GetHashCode(KeyHex);
            }
        }

        private sealed class LineDecoration
        {
            private readonly StringBuilder _text = new StringBuilder();

            public HealthState? Severity { get; private set; }
            public LiveOpsJsonLineChangeKind Change { get; set; }
            public string Text => _text.ToString();

            public void AddAnnotation(HealthState severity, string text)
            {
                if (!Severity.HasValue || SeverityRank(severity) > SeverityRank(Severity.Value)) Severity = severity;
                if (string.IsNullOrEmpty(text)) return;
                if (_text.Length > 0) _text.Append(AnnotationSeparator);
                _text.Append(text);
            }

            private static int SeverityRank(HealthState state)
            {
                switch (state)
                {
                    case HealthState.Blocked: return 3;
                    case HealthState.Warning: return 2;
                    case HealthState.NotMeasured: return 1;
                    default: return 0;
                }
            }
        }

        /// <summary>Một dòng của ListView: số dòng · lề (icon hoặc dấu) · code · chú thích. Tạo trong makeItem (FD §2.14 mục 8).</summary>
        private sealed class JsonLineRow : VisualElement
        {
            public JsonLineRow()
            {
                AddToClassList(LiveOpsHubClassNames.JsonLine);
                Number = new Label { pickingMode = PickingMode.Ignore };
                Number.AddToClassList(LiveOpsHubClassNames.JsonLineNumber);
                Add(Number);

                VisualElement gutter = new VisualElement { pickingMode = PickingMode.Ignore };
                gutter.AddToClassList(LiveOpsHubClassNames.JsonGutter);
                GutterIcon = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                GutterIcon.AddToClassList(LiveOpsHubClassNames.Icon);
                GutterIcon.AddToClassList(LiveOpsHubIcons.SizeClassOf(GutterIconSize));
                GutterIcon.AddToClassList(LiveOpsHubClassNames.JsonGutterIcon);
                gutter.Add(GutterIcon);
                GutterMark = new Label { pickingMode = PickingMode.Ignore };
                GutterMark.AddToClassList(LiveOpsHubClassNames.JsonGutterMark);
                gutter.Add(GutterMark);
                Add(gutter);

                Code = new Label { enableRichText = true };
                Code.AddToClassList(LiveOpsHubClassNames.JsonCode);
                Code.AddToClassList(LiveOpsHubClassNames.Mono);
                Add(Code);

                Annotation = new Label();
                Annotation.AddToClassList(LiveOpsHubClassNames.JsonAnnotation);
                Add(Annotation);
            }

            public Label Number { get; }
            public Image GutterIcon { get; }
            public Label GutterMark { get; }
            public Label Code { get; }
            public Label Annotation { get; }
        }
    }

    /// <summary>Loại dấu lề của dòng JSON so với bản so.</summary>
    internal enum LiveOpsJsonLineChangeKind
    {
        None = 0,
        /// <summary>Dòng thuộc mục thêm mới (<c>+</c>).</summary>
        Added = 1,
        /// <summary>Field đổi giá trị so với bản so (<c>~</c>).</summary>
        Modified = 2,
        /// <summary>Dòng có ở bản so mà nháp đã bỏ (<c>–</c>).</summary>
        Removed = 3,
    }

    /// <summary>Dấu lề của một dòng (1-based) — presenter G-EXPORT dựng từ diff + <c>json.Lines</c>.</summary>
    internal readonly struct LiveOpsJsonLineChange
    {
        public LiveOpsJsonLineChange(int lineNumber, LiveOpsJsonLineChangeKind kind)
        {
            if (lineNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNumber),
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.JsonAnnotationLineOutOfRangeFormat, lineNumber));
            }
            LineNumber = lineNumber;
            Kind = kind;
        }

        public int LineNumber { get; }
        public LiveOpsJsonLineChangeKind Kind { get; }
    }

    /// <summary>Chú thích cuối một dòng JSON (V-21 D-5): câu DỰNG SẴN — control không nhận <c>LiveEventCalendarFinding</c>.</summary>
    internal readonly struct LiveOpsJsonLineAnnotation
    {
        public LiveOpsJsonLineAnnotation(int lineNumber, HealthState severity, string text)
        {
            if (lineNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNumber),
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.JsonAnnotationLineOutOfRangeFormat, lineNumber));
            }
            LineNumber = lineNumber;
            Severity = severity;
            Text = text ?? string.Empty;
        }

        /// <summary>1-based, như <c>LiveEventCalendarJsonLine.Number</c>.</summary>
        public int LineNumber { get; }

        /// <summary>Blocked = icon lỗi + vạch bị bỏ; Warning = icon cảnh báo; Ok = chú thích trung tính.</summary>
        public HealthState Severity { get; }

        /// <summary>Câu dựng sẵn (presenter G-EXPORT lấy từ <c>LiveOpsFindingText</c>).</summary>
        public string Text { get; }
    }
}
