using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace TaskManager.UI
{
    /// <summary>A checklist with an input field on top; tasks can optionally track an item amount.</summary>
    internal class TodoPage : Page
    {
        private const int RowHeight = 56;
        private const int ControlHeight = 56;
        private const int Gap = 12;
        private const int PickerCellHeight = 64;
        private const int PickerColumns = 3;

        private readonly ITodoList list;
        private readonly TextArea input;
        private readonly TextArea quantity;
        private readonly TextArea search;
        private Rectangle addButton;
        private Rectangle itemButton;
        private Rectangle itemClear;
        private Rectangle quantityLabel;
        private Rectangle listRect;
        private Rectangle clearButton;
        private int listScroll;

        // selected item for the next task
        private string? selectedItem;

        // item picker
        private bool pickerOpen;
        private Rectangle pickerRect;
        private Rectangle pickerClose;
        private Rectangle resultsRect;
        private List<(string Id, string Name)> results = new();
        private int pickerScroll;

        private IReadOnlyList<TodoItem> Todos => list.Items;
        private int VisibleRows => Math.Max(1, (listRect.Height - 16) / RowHeight);
        private int VisiblePickerRows => Math.Max(1, resultsRect.Height / PickerCellHeight);
        private int PickerRowCount => (results.Count + PickerColumns - 1) / PickerColumns;

        public TodoPage(ITodoList list)
        {
            this.list = list;
            input = new TextArea
            {
                MaxLength = 200,
                Placeholder = I18n.Get("todo.placeholder"),
                OnSubmit = Add
            };
            quantity = new TextArea
            {
                MaxLength = 5,
                Placeholder = "1",
                Filter = char.IsDigit,
                OnSubmit = Add
            };
            search = new TextArea
            {
                MaxLength = 40,
                Placeholder = I18n.Get("todo.searchPlaceholder"),
                OnChanged = text =>
                {
                    results = ItemCatalog.Search(text);
                    pickerScroll = 0;
                }
            };
        }

        public override bool IsTyping => input.Selected || quantity.Selected || search.Selected;

        public override void StopTyping()
        {
            input.Selected = false;
            quantity.Selected = false;
            search.Selected = false;
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            pickerOpen = false;
        }

        public override void SetBounds(Rectangle bounds)
        {
            base.SetBounds(bounds);
            int row2 = bounds.Y + ControlHeight + Gap;

            input.SetBounds(new Rectangle(bounds.X, bounds.Y, bounds.Width - 236, ControlHeight));
            addButton = new Rectangle(bounds.Right - 220, bounds.Y, 220, ControlHeight);

            itemButton = new Rectangle(bounds.X, row2, 460, ControlHeight);
            itemClear = new Rectangle(itemButton.Right - 46, itemButton.Y + 10, 36, 36);
            quantityLabel = new Rectangle(itemButton.Right + 16, row2, 110, ControlHeight);
            quantity.SetBounds(new Rectangle(quantityLabel.Right, row2, 140, ControlHeight));

            int listTop = row2 + ControlHeight + Gap;
            listRect = new Rectangle(bounds.X, listTop, bounds.Width, bounds.Bottom - ControlHeight - Gap - listTop);
            clearButton = new Rectangle(bounds.Right - 340, bounds.Bottom - ControlHeight, 340, ControlHeight);

            pickerRect = new Rectangle(bounds.X, listTop, bounds.Width, bounds.Bottom - listTop);
            search.SetBounds(new Rectangle(pickerRect.X + 16, pickerRect.Y + 16, pickerRect.Width - 88, ControlHeight));
            pickerClose = new Rectangle(pickerRect.Right - 60, pickerRect.Y + 26, 36, 36);
            resultsRect = new Rectangle(pickerRect.X + 16, pickerRect.Y + 16 + ControlHeight + Gap, pickerRect.Width - 32, pickerRect.Height - 32 - ControlHeight - Gap);
        }

        /*********
        ** Actions
        *********/
        private void Add()
        {
            string text = input.Text.Trim();
            if (text.Length == 0 && selectedItem == null)
                return;

            var todo = new TodoItem { Text = text };
            if (selectedItem != null)
            {
                todo.ItemId = selectedItem;
                todo.Quantity = int.TryParse(quantity.Text, out int amount) && amount > 0 ? amount : 1;
                if (text.Length == 0)
                    todo.Text = ItemCatalog.GetName(selectedItem);
            }

            list.Add(todo);
            input.Text = "";
            quantity.Text = "";
            selectedItem = null;
            StopTyping();
            listScroll = Math.Max(0, Todos.Count - VisibleRows);
            Game1.playSound("smallSelect");
        }

        private void OpenPicker()
        {
            pickerOpen = true;
            StopTyping();
            search.Text = "";
            results.Clear();
            pickerScroll = 0;
            search.Selected = true;
            Game1.playSound("bigSelect");
        }

        private void ClosePicker()
        {
            pickerOpen = false;
            search.Selected = false;
        }

        /*********
        ** Input
        *********/
        public override void LeftClick(int x, int y)
        {
            if (pickerOpen)
            {
                PickerClick(x, y);
                return;
            }

            foreach (TextArea field in new[] { input, quantity })
            {
                if (field.Bounds.Contains(x, y) && (field != quantity || selectedItem != null))
                {
                    StopTyping();
                    field.Selected = true;
                    field.Click(x, y);
                    return;
                }
            }
            StopTyping();

            if (addButton.Contains(x, y))
            {
                Add();
                return;
            }

            if (selectedItem != null && itemClear.Contains(x, y))
            {
                selectedItem = null;
                quantity.Text = "";
                Game1.playSound("trashcan");
                return;
            }

            if (itemButton.Contains(x, y))
            {
                OpenPicker();
                return;
            }

            if (clearButton.Contains(x, y))
            {
                if (list.ClearDone() > 0)
                {
                    listScroll = Math.Clamp(listScroll, 0, Math.Max(0, Todos.Count - VisibleRows));
                    Game1.playSound("trashcan");
                }
                return;
            }

            if (listRect.Contains(x, y))
            {
                int index = (y - listRect.Y - 8) / RowHeight + listScroll;
                if (index < 0 || index >= Todos.Count || index >= listScroll + VisibleRows)
                    return;

                Rectangle row = RowRect(index);
                if (PinRect(row).Contains(x, y))
                {
                    list.SetPinned(Todos[index], !Todos[index].Pinned);
                    Game1.playSound(Todos[index].Pinned ? "coin" : "smallSelect");
                }
                else if (DeleteRect(row).Contains(x, y))
                {
                    list.Remove(Todos[index]);
                    listScroll = Math.Clamp(listScroll, 0, Math.Max(0, Todos.Count - VisibleRows));
                    Game1.playSound("trashcan");
                }
                else if (CheckboxRect(row).Contains(x, y))
                {
                    list.SetDone(Todos[index], !Todos[index].Done);
                    Game1.playSound("drumkit6");
                }
            }
        }

        private void PickerClick(int x, int y)
        {
            if (search.Bounds.Contains(x, y))
            {
                search.Selected = true;
                search.Click(x, y);
                return;
            }

            if (pickerClose.Contains(x, y) || !pickerRect.Contains(x, y))
            {
                ClosePicker();
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                if (PickerCell(i) is Rectangle cell && cell.Contains(x, y))
                {
                    selectedItem = results[i].Id;
                    ClosePicker();
                    quantity.Selected = true;
                    quantity.CaretToEnd();
                    Game1.playSound("coin");
                    return;
                }
            }
        }

        public override void Scroll(int direction, int mouseX, int mouseY)
        {
            int delta = direction > 0 ? -1 : 1;
            if (pickerOpen)
                pickerScroll = Math.Clamp(pickerScroll + delta, 0, Math.Max(0, PickerRowCount - VisiblePickerRows));
            else
                listScroll = Math.Clamp(listScroll + delta, 0, Math.Max(0, Todos.Count - VisibleRows));
        }

        /*********
        ** Layout helpers
        *********/
        private Rectangle RowRect(int index) =>
            new(listRect.X + 8, listRect.Y + 8 + (index - listScroll) * RowHeight, listRect.Width - 16, RowHeight - 4);

        private static Rectangle CheckboxRect(Rectangle row) => new(row.X + 8, row.Y + (row.Height - 36) / 2, 36, 36);

        private static Rectangle DeleteRect(Rectangle row) => new(row.Right - 44, row.Y + (row.Height - 36) / 2, 36, 36);

        private static Rectangle PinRect(Rectangle row) => new(row.Right - 92, row.Y + (row.Height - 32) / 2, 32, 32);

        /// <summary>The cell for a picker result, or null if it's scrolled out of view.</summary>
        private Rectangle? PickerCell(int index)
        {
            int row = index / PickerColumns - pickerScroll;
            if (row < 0 || row >= VisiblePickerRows)
                return null;
            int cellWidth = resultsRect.Width / PickerColumns;
            return new Rectangle(resultsRect.X + index % PickerColumns * cellWidth, resultsRect.Y + row * PickerCellHeight, cellWidth - 8, PickerCellHeight - 4);
        }

        /// <summary>Draw an item icon scaled to 32x32 at the given position.</summary>
        private static void DrawSmallIcon(SpriteBatch b, Item? item, int x, int y)
        {
            item?.drawInMenu(b, new Vector2(x - 16, y - 16), 0.5f);
        }

        /*********
        ** Drawing
        *********/
        public override void Draw(SpriteBatch b)
        {
            HoverText = null;
            SpriteFont font = Game1.smallFont;

            input.Draw(b);
            UIHelper.DrawButton(b, addButton, I18n.Get("todo.add"));
            DrawItemControls(b, font);

            if (pickerOpen)
            {
                DrawPicker(b, font);
                return;
            }

            UIHelper.DrawBox(b, listRect);
            if (Todos.Count == 0)
            {
                string hint = I18n.Get("todo.empty");
                Vector2 size = font.MeasureString(hint);
                b.DrawString(font, hint, new Vector2(listRect.Center.X - size.X / 2, listRect.Center.Y - size.Y / 2), Game1.textColor * 0.6f);
            }

            for (int i = listScroll; i < Todos.Count && i < listScroll + VisibleRows; i++)
                DrawRow(b, font, Todos[i], RowRect(i));
            UIHelper.DrawScrollBar(b, new Rectangle(listRect.Right - 12, listRect.Y + 12, 6, listRect.Height - 24), listScroll, VisibleRows, Todos.Count);

            int done = Todos.Count(t => t.Done);
            string summary = I18n.Get("todo.summary", new { open = Todos.Count - done, done });
            if (list.StatusText != null)
                summary += "\n" + list.StatusText;
            Vector2 summarySize = font.MeasureString(summary);
            b.DrawString(font, summary, new Vector2(Bounds.X + 8, clearButton.Center.Y - summarySize.Y / 2), Game1.textColor);
            UIHelper.DrawButton(b, clearButton, I18n.Get("todo.clearDone"));
        }

        private void DrawItemControls(SpriteBatch b, SpriteFont font)
        {
            UIHelper.DrawButton(b, itemButton, "");
            if (selectedItem != null)
            {
                DrawSmallIcon(b, ItemCatalog.GetIcon(selectedItem), itemButton.X + 16, itemButton.Y + 12);
                string name = UIHelper.Truncate(font, ItemCatalog.GetName(selectedItem), itemButton.Width - 120);
                b.DrawString(font, name, new Vector2(itemButton.X + 60, itemButton.Center.Y - font.MeasureString(name).Y / 2 + 2), Game1.textColor);
                UIHelper.DrawDeleteIcon(b, itemClear, false);
                if (UIHelper.IsHovered(itemClear))
                    HoverText = I18n.Get("todo.removeItem");
            }
            else
            {
                string label = I18n.Get("todo.chooseItem");
                b.DrawString(font, label, new Vector2(itemButton.X + 20, itemButton.Center.Y - font.MeasureString(label).Y / 2 + 2), Game1.textColor * 0.7f);
            }

            bool enabled = selectedItem != null;
            string amountLabel = I18n.Get("todo.amount");
            Vector2 labelSize = font.MeasureString(amountLabel);
            b.DrawString(font, amountLabel, new Vector2(quantityLabel.Right - labelSize.X - 10, quantityLabel.Center.Y - labelSize.Y / 2 + 2), Game1.textColor * (enabled ? 1f : 0.4f));
            if (enabled)
                quantity.Draw(b);
            else
                UIHelper.DrawBox(b, quantity.Bounds);
        }

        private void DrawRow(SpriteBatch b, SpriteFont font, TodoItem todo, Rectangle row)
        {
            bool hovered = UIHelper.IsHovered(row);
            if (hovered)
                b.Draw(Game1.staminaRect, row, Color.Wheat * 0.3f);

            b.Draw(Game1.mouseCursors, CheckboxRect(row), todo.Done ? UIHelper.CheckboxChecked : UIHelper.CheckboxUnchecked, Color.White);

            int textX = row.X + 60;
            float textRight = row.Right - 104;

            // item icon and progress
            string? progress = null;
            Color progressColor = Game1.textColor;
            if (todo.ItemId != null)
            {
                DrawSmallIcon(b, ItemCatalog.GetIcon(todo.ItemId, todo.Quality), textX, row.Y + (row.Height - 32) / 2);
                textX += 40;

                int have = ItemCatalog.CountInInventory(todo.ItemId, todo.Quality);
                progress = $"{Math.Min(have, todo.Quantity)}/{todo.Quantity}";
                progressColor = have >= todo.Quantity ? Color.DarkGreen : Game1.textColor * 0.7f;
                textRight -= font.MeasureString(progress).X + 16;
            }

            string text = UIHelper.Truncate(font, todo.Text, textRight - textX);
            Vector2 size = font.MeasureString(text);
            var pos = new Vector2(textX, row.Y + (row.Height - size.Y) / 2 + 2);
            Color color = todo.Done ? Game1.textColor * 0.45f : Game1.textColor;
            b.DrawString(font, text, pos, color);
            if (todo.Done)
                b.Draw(Game1.staminaRect, new Rectangle((int)pos.X, (int)(pos.Y + size.Y / 2) - 1, (int)size.X, 2), color);

            Rectangle pin = PinRect(row);
            if (progress != null)
            {
                Vector2 progressSize = font.MeasureString(progress);
                b.DrawString(font, progress, new Vector2(pin.X - progressSize.X - 16, pos.Y), todo.Done ? color : progressColor);
            }

            if (todo.Pinned || hovered)
                UIHelper.DrawPinIcon(b, pin, todo.Pinned);

            if (hovered)
            {
                Rectangle del = DeleteRect(row);
                UIHelper.DrawDeleteIcon(b, del, false);
                if (UIHelper.IsHovered(del))
                    HoverText = I18n.Get("todo.delete");
                else if (UIHelper.IsHovered(pin))
                    HoverText = I18n.Get(todo.Pinned ? "todo.unpin" : "todo.pin");
                else
                {
                    var parts = new List<string>();
                    if (todo.ItemId != null || text != todo.Text)
                        parts.Add(Game1.parseText(todo.Text, font, 600));
                    if (todo.ItemId != null)
                        parts.Add(I18n.Get("todo.inventoryHint"));
                    if (todo.AddedBy != null)
                        parts.Add(I18n.Get("shared.addedBy", new { name = todo.AddedBy }));
                    if (parts.Count > 0)
                        HoverText = string.Join("\n", parts);
                }
            }
        }

        private void DrawPicker(SpriteBatch b, SpriteFont font)
        {
            UIHelper.DrawBox(b, pickerRect);
            search.Draw(b);
            UIHelper.DrawDeleteIcon(b, pickerClose, false);

            if (results.Count == 0)
            {
                string hint = I18n.Get(search.Text.Trim().Length == 0 ? "todo.searchHint" : "todo.noResults");
                Vector2 size = font.MeasureString(hint);
                b.DrawString(font, hint, new Vector2(resultsRect.Center.X - size.X / 2, resultsRect.Y + 40), Game1.textColor * 0.6f);
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                if (PickerCell(i) is not Rectangle cell)
                    continue;
                if (UIHelper.IsHovered(cell))
                    b.Draw(Game1.staminaRect, cell, Color.Wheat * 0.4f);

                ItemCatalog.GetIcon(results[i].Id)?.drawInMenu(b, new Vector2(cell.X, cell.Y), 0.75f);
                string name = UIHelper.Truncate(font, results[i].Name, cell.Width - 76);
                b.DrawString(font, name, new Vector2(cell.X + 68, cell.Center.Y - font.MeasureString(name).Y / 2 + 2), Game1.textColor);
                if (name != results[i].Name && UIHelper.IsHovered(cell))
                    HoverText = results[i].Name;
            }
            UIHelper.DrawScrollBar(b, new Rectangle(resultsRect.Right - 6, resultsRect.Y, 6, resultsRect.Height), pickerScroll, VisiblePickerRows, PickerRowCount);
        }
    }
}
