using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace TaskManager.UI
{
    /// <summary>A text field with word wrapping, caret movement and optional multi-line input.</summary>
    internal class TextArea : IKeyboardSubscriber
    {
        private const int Padding = 16;

        private readonly SpriteFont font = Game1.smallFont;
        private readonly List<(int Start, int Length)> lines = new();
        private string text = "";
        private int caret;
        private int scrollLine;
        private bool selected;
        private bool layoutDirty = true;

        public Rectangle Bounds { get; private set; }
        public bool Multiline { get; init; }
        public int MaxLength { get; init; } = 10000;
        public string Placeholder { get; set; } = "";
        public Action? OnSubmit { get; set; }
        public Action<string>? OnChanged { get; set; }

        /// <summary>Optional check which characters may be typed.</summary>
        public Func<char, bool>? Filter { get; init; }

        public string Text
        {
            get => text;
            set
            {
                text = value ?? "";
                caret = Math.Clamp(caret, 0, text.Length);
                scrollLine = 0;
                layoutDirty = true;
            }
        }

        public bool Selected
        {
            get => selected;
            set
            {
                if (selected == value)
                    return;
                selected = value;
                if (value)
                    Game1.keyboardDispatcher.Subscriber = this;
                else if (Game1.keyboardDispatcher.Subscriber == this)
                    Game1.keyboardDispatcher.Subscriber = null;
            }
        }

        private int LineHeight => font.LineSpacing;
        private int VisibleLines => Math.Max(1, (Bounds.Height - Padding * 2) / LineHeight);
        private int TopY => Multiline ? Bounds.Y + Padding : Bounds.Y + (Bounds.Height - LineHeight) / 2;

        public void SetBounds(Rectangle bounds)
        {
            if (bounds.Width != Bounds.Width)
                layoutDirty = true;
            Bounds = bounds;
        }

        public void CaretToEnd()
        {
            caret = text.Length;
            EnsureCaretVisible();
        }

        /*********
        ** Keyboard input
        *********/
        public void RecieveTextInput(char inputChar) => Insert(inputChar.ToString());

        public void RecieveTextInput(string input) => Insert(input);

        public void RecieveCommandInput(char command)
        {
            switch (command)
            {
                case '\b':
                    if (caret > 0)
                    {
                        text = text.Remove(caret - 1, 1);
                        caret--;
                        Changed();
                    }
                    break;

                case '\r':
                case '\n':
                    if (Multiline)
                        Insert("\n");
                    else
                        OnSubmit?.Invoke();
                    break;
            }
        }

        public void RecieveSpecialInput(Keys key)
        {
            EnsureLayout();
            int line = CaretLine();
            switch (key)
            {
                case Keys.Left:
                    if (caret > 0)
                        caret--;
                    break;
                case Keys.Right:
                    if (caret < text.Length)
                        caret++;
                    break;
                case Keys.Up:
                    MoveVertical(line, -1);
                    break;
                case Keys.Down:
                    MoveVertical(line, 1);
                    break;
                case Keys.Home:
                    caret = lines[line].Start;
                    break;
                case Keys.End:
                    caret = lines[line].Start + lines[line].Length;
                    if (IsWrapped(line) && caret > lines[line].Start)
                        caret--;
                    break;
                case Keys.Delete:
                    if (caret < text.Length)
                    {
                        text = text.Remove(caret, 1);
                        Changed();
                    }
                    break;
                default:
                    return;
            }
            EnsureCaretVisible();
        }

        private void Insert(string input)
        {
            if (string.IsNullOrEmpty(input))
                return;

            input = input.Replace("\r\n", "\n").Replace('\r', '\n');
            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (c == '\n')
                    sb.Append(Multiline ? '\n' : ' ');
                else if (c == '\t')
                    sb.Append(' ');
                else if (!char.IsControl(c) && font.Characters.Contains(c) && (Filter == null || Filter(c)))
                    sb.Append(c);
            }

            int room = MaxLength - text.Length;
            if (room <= 0 || sb.Length == 0)
                return;
            string insert = sb.Length > room ? sb.ToString(0, room) : sb.ToString();

            text = text.Insert(caret, insert);
            caret += insert.Length;
            Changed();
        }

        private void Changed()
        {
            layoutDirty = true;
            EnsureCaretVisible();
            OnChanged?.Invoke(text);
        }

        /*********
        ** Mouse input
        *********/
        public void Click(int x, int y)
        {
            EnsureLayout();
            int line = Math.Clamp(scrollLine + (y - TopY) / LineHeight, 0, lines.Count - 1);
            caret = ColumnAt(line, x - Bounds.X - Padding);
        }

        public void Scroll(int direction)
        {
            EnsureLayout();
            scrollLine = Math.Clamp(scrollLine + (direction > 0 ? -3 : 3), 0, Math.Max(0, lines.Count - VisibleLines));
        }

        /*********
        ** Layout
        *********/
        private float Measure(int start, int length) => length <= 0 ? 0 : font.MeasureString(text.Substring(start, length)).X;

        private void EnsureLayout()
        {
            if (!layoutDirty)
                return;
            layoutDirty = false;
            lines.Clear();

            float maxWidth = Math.Max(20, Bounds.Width - Padding * 2 - 12);
            int lineStart = 0;
            int lastSpace = -1;
            int i = 0;
            while (true)
            {
                if (i >= text.Length)
                {
                    lines.Add((lineStart, text.Length - lineStart));
                    break;
                }

                char c = text[i];
                if (c == '\n')
                {
                    lines.Add((lineStart, i - lineStart));
                    lineStart = i + 1;
                    lastSpace = -1;
                    i++;
                    continue;
                }
                if (c == ' ')
                    lastSpace = i;

                if (i > lineStart && Measure(lineStart, i - lineStart + 1) > maxWidth)
                {
                    if (lastSpace >= lineStart && lastSpace < i)
                    {
                        // break after the last space
                        lines.Add((lineStart, lastSpace - lineStart + 1));
                        lineStart = lastSpace + 1;
                    }
                    else if (c == ' ')
                    {
                        lines.Add((lineStart, i - lineStart + 1));
                        lineStart = i + 1;
                        i++;
                    }
                    else
                    {
                        // a single word longer than the line
                        lines.Add((lineStart, i - lineStart));
                        lineStart = i;
                    }
                    lastSpace = -1;
                    continue;
                }
                i++;
            }
        }

        /// <summary>Whether the line continues on the next line without a line break.</summary>
        private bool IsWrapped(int line) => line + 1 < lines.Count && lines[line + 1].Start == lines[line].Start + lines[line].Length;

        private int CaretLine()
        {
            EnsureLayout();
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Start <= caret)
                    return i;
            }
            return 0;
        }

        private int ColumnAt(int line, float x)
        {
            var (start, length) = lines[line];
            int maxCol = IsWrapped(line) && length > 0 ? length - 1 : length;
            int best = start;
            float bestDistance = float.MaxValue;
            for (int k = 0; k <= maxCol; k++)
            {
                float distance = Math.Abs(Measure(start, k) - x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = start + k;
                }
            }
            return best;
        }

        private void MoveVertical(int line, int delta)
        {
            int target = line + delta;
            if (target < 0)
                caret = 0;
            else if (target >= lines.Count)
                caret = text.Length;
            else
                caret = ColumnAt(target, Measure(lines[line].Start, caret - lines[line].Start));
        }

        private void EnsureCaretVisible()
        {
            EnsureLayout();
            int line = CaretLine();
            if (line < scrollLine)
                scrollLine = line;
            if (line >= scrollLine + VisibleLines)
                scrollLine = line - VisibleLines + 1;
            scrollLine = Math.Clamp(scrollLine, 0, Math.Max(0, lines.Count - VisibleLines));
        }

        /*********
        ** Drawing
        *********/
        public void Draw(SpriteBatch b)
        {
            UIHelper.DrawBox(b, Bounds);
            EnsureLayout();

            int x = Bounds.X + Padding;
            int y = TopY;

            if (text.Length == 0 && !selected)
            {
                b.DrawString(font, Placeholder, new Vector2(x, y), Game1.textColor * 0.45f);
                return;
            }

            for (int k = 0; k < VisibleLines && scrollLine + k < lines.Count; k++)
            {
                var (start, length) = lines[scrollLine + k];
                if (length > 0)
                    b.DrawString(font, text.Substring(start, length), new Vector2(x, y + k * LineHeight), Game1.textColor);
            }

            if (selected && Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000 < 550)
            {
                int line = CaretLine();
                if (line >= scrollLine && line < scrollLine + VisibleLines)
                {
                    int caretX = x + (int)Measure(lines[line].Start, caret - lines[line].Start);
                    b.Draw(Game1.staminaRect, new Rectangle(caretX, y + (line - scrollLine) * LineHeight, 2, LineHeight), Game1.textColor);
                }
            }

            if (Multiline)
                UIHelper.DrawScrollBar(b, new Rectangle(Bounds.Right - 16, Bounds.Y + 16, 6, Bounds.Height - 32), scrollLine, VisibleLines, lines.Count);
        }
    }
}
