using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace TaskManager.UI
{
    /// <summary>A list of notes on the left and an editor for the selected note on the right.</summary>
    internal class NotesPage : Page
    {
        private const int ListWidth = 320;
        private const int RowHeight = 72;
        private const int ButtonHeight = 56;
        private const int HeaderHeight = 40;

        private readonly ModEntry mod;
        private readonly TextArea editor;
        private Rectangle newButton;
        private Rectangle listRect;
        private int selected = -1;
        private int listScroll;
        private int pendingDelete = -1;

        private List<Note> Notes => mod.Data.Notes;
        private Note? Current => selected >= 0 && selected < Notes.Count ? Notes[selected] : null;
        private int VisibleRows => Math.Max(1, (listRect.Height - 16) / RowHeight);

        public NotesPage(ModEntry mod)
        {
            this.mod = mod;
            editor = new TextArea
            {
                Multiline = true,
                Placeholder = I18n.Get("notes.placeholder"),
                OnChanged = text =>
                {
                    if (Current != null)
                    {
                        Current.Text = text;
                        mod.MarkDirty();
                    }
                }
            };
        }

        public override bool IsTyping => editor.Selected;

        public override void StopTyping() => editor.Selected = false;

        public override void SetBounds(Rectangle bounds)
        {
            base.SetBounds(bounds);
            newButton = new Rectangle(bounds.X, bounds.Y, ListWidth, ButtonHeight);
            listRect = new Rectangle(bounds.X, bounds.Y + ButtonHeight + 12, ListWidth, bounds.Height - ButtonHeight - 12);
            int editorX = bounds.X + ListWidth + 16;
            editor.SetBounds(new Rectangle(editorX, bounds.Y + HeaderHeight, bounds.Right - editorX, bounds.Height - HeaderHeight));
        }

        public override void OnActivate()
        {
            if (Current == null && Notes.Count > 0)
                Select(0);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            pendingDelete = -1;
        }

        private void Select(int index)
        {
            selected = index;
            editor.Text = Notes[index].Text;
            editor.CaretToEnd();
        }

        private void NewNote()
        {
            Notes.Insert(0, new Note { Created = SDate.Now().ToLocaleString() });
            listScroll = 0;
            Select(0);
            editor.Selected = true;
            mod.MarkDirty();
            Game1.playSound("smallSelect");
        }

        private void Delete(int index)
        {
            Notes.RemoveAt(index);
            pendingDelete = -1;
            editor.Selected = false;

            if (Notes.Count == 0)
            {
                selected = -1;
                editor.Text = "";
            }
            else
            {
                int next = index < selected ? selected - 1 : selected;
                Select(Math.Clamp(next, 0, Notes.Count - 1));
            }
            listScroll = Math.Clamp(listScroll, 0, Math.Max(0, Notes.Count - VisibleRows));
            mod.MarkDirty();
            Game1.playSound("trashcan");
        }

        private Rectangle RowRect(int index) =>
            new(listRect.X + 8, listRect.Y + 8 + (index - listScroll) * RowHeight, listRect.Width - 16, RowHeight - 4);

        private static Rectangle DeleteRect(Rectangle row) => new(row.Right - 40, row.Y + (row.Height - 36) / 2, 36, 36);

        private static string TitleOf(Note note)
        {
            foreach (string line in note.Text.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    return line.Trim();
            }
            return I18n.Get("notes.untitled");
        }

        public override void LeftClick(int x, int y)
        {
            if (newButton.Contains(x, y))
            {
                pendingDelete = -1;
                NewNote();
                return;
            }

            if (listRect.Contains(x, y))
            {
                int index = (y - listRect.Y - 8) / RowHeight + listScroll;
                if (index < 0 || index >= Notes.Count || index >= listScroll + VisibleRows)
                    return;

                if (DeleteRect(RowRect(index)).Contains(x, y))
                {
                    if (pendingDelete == index)
                        Delete(index);
                    else
                        pendingDelete = index;
                    return;
                }

                pendingDelete = -1;
                editor.Selected = false;
                if (index != selected)
                {
                    Select(index);
                    Game1.playSound("smallSelect");
                }
                return;
            }

            pendingDelete = -1;
            if (Current != null && editor.Bounds.Contains(x, y))
            {
                editor.Selected = true;
                editor.Click(x, y);
            }
            else
                editor.Selected = false;
        }

        public override void Scroll(int direction, int mouseX, int mouseY)
        {
            if (listRect.Contains(mouseX, mouseY))
                listScroll = Math.Clamp(listScroll + (direction > 0 ? -1 : 1), 0, Math.Max(0, Notes.Count - VisibleRows));
            else
                editor.Scroll(direction);
        }

        public override void Draw(SpriteBatch b)
        {
            HoverText = null;
            SpriteFont font = Game1.smallFont;

            // note list
            UIHelper.DrawButton(b, newButton, I18n.Get("notes.new"));
            UIHelper.DrawBox(b, listRect);
            for (int i = listScroll; i < Notes.Count && i < listScroll + VisibleRows; i++)
            {
                Rectangle row = RowRect(i);
                bool hovered = UIHelper.IsHovered(row);
                if (i == selected)
                    b.Draw(Game1.staminaRect, row, Color.Wheat * 0.7f);
                else if (hovered)
                    b.Draw(Game1.staminaRect, row, Color.Wheat * 0.3f);

                string title = UIHelper.Truncate(font, TitleOf(Notes[i]), row.Width - 60);
                b.DrawString(font, title, new Vector2(row.X + 10, row.Y + 8), Game1.textColor);
                b.DrawString(font, Notes[i].Created, new Vector2(row.X + 10, row.Y + 40), Game1.textColor * 0.6f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 1f);

                if (hovered || i == selected || i == pendingDelete)
                {
                    Rectangle del = DeleteRect(row);
                    UIHelper.DrawDeleteIcon(b, del, i == pendingDelete);
                    if (UIHelper.IsHovered(del))
                        HoverText = I18n.Get(i == pendingDelete ? "notes.confirmDelete" : "notes.delete");
                }
            }
            UIHelper.DrawScrollBar(b, new Rectangle(listRect.Right - 12, listRect.Y + 12, 6, listRect.Height - 24), listScroll, VisibleRows, Notes.Count);

            // editor
            if (Current != null)
            {
                string header = I18n.Get("notes.created", new { date = Current.Created });
                b.DrawString(font, header, new Vector2(editor.Bounds.X + 8, Bounds.Y + 4), Game1.textColor * 0.7f);
                editor.Draw(b);
            }
            else
            {
                UIHelper.DrawBox(b, editor.Bounds);
                string hint = Game1.parseText(I18n.Get("notes.none"), font, editor.Bounds.Width - 64);
                Vector2 size = font.MeasureString(hint);
                b.DrawString(font, hint, new Vector2(editor.Bounds.Center.X - size.X / 2, editor.Bounds.Center.Y - size.Y / 2), Game1.textColor * 0.6f);
            }
        }
    }
}
