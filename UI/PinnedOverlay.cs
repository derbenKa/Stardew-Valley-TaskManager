using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace TaskManager.UI
{
    /// <summary>Shows pinned to-dos on screen while playing; their checkboxes can be clicked.</summary>
    internal class PinnedOverlay
    {
        private const int Padding = 16;
        private const int CheckboxSize = 28;

        private readonly ModEntry mod;
        private readonly List<(Rectangle Checkbox, TodoItem Todo, ITodoList List)> hitAreas = new();

        public PinnedOverlay(ModEntry mod)
        {
            this.mod = mod;
        }

        public void Draw(SpriteBatch b)
        {
            hitAreas.Clear();
            if (!mod.Config.ShowPinnedTodos || Game1.activeClickableMenu != null)
                return;

            var pinned = new[] { (ITodoList)mod.LocalTodos, mod.SharedTodos }
                .SelectMany(list => list.Items.Where(t => t.Pinned && !t.Done).Select(t => (Todo: t, List: list)))
                .ToList();
            if (pinned.Count == 0)
                return;

            SpriteFont font = Game1.smallFont;
            float scale = MathHelper.Clamp(mod.Config.PinnedScale, 0.25f, 3f);
            int padding = (int)(Padding * scale);
            int checkboxSize = (int)(CheckboxSize * scale);
            int gap = (int)(8 * scale);
            int x = mod.Config.PinnedX;
            int y = mod.Config.PinnedY;
            int width = (int)(mod.Config.PinnedWidth * scale);

            // text is wrapped at normal size, then drawn scaled
            float textWidth = (width - padding * 2 - checkboxSize - 12 * scale) / scale;
            int iconSize = (int)(32 * scale);
            float iconSpace = iconSize + 8 * scale;

            string title = I18n.Get("hud.title");
            var lines = pinned.Select(p => (p.Todo, p.List, Text: Game1.parseText(LabelOf(p.Todo), font, (int)(p.Todo.ItemId != null ? textWidth - iconSpace / scale : textWidth)))).ToList();
            int titleHeight = (int)(font.MeasureString(title).Y * scale);
            int height = padding * 2 + titleHeight + gap + lines.Sum(l => Math.Max((int)(font.MeasureString(l.Text).Y * scale), l.Todo.ItemId != null ? iconSize : 0) + gap);

            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), x, y, width, height, Color.White, Math.Max(2f, 4f * scale), false);
            Utility.drawTextWithShadow(b, title, font, new Vector2(x + padding, y + padding), Game1.textColor, scale);

            int cy = y + padding + titleHeight + gap;
            foreach (var (todo, list, text) in lines)
            {
                int lineHeight = (int)(font.MeasureString(text).Y * scale);
                if (todo.ItemId != null)
                    lineHeight = Math.Max(lineHeight, iconSize);
                var checkbox = new Rectangle(x + padding, cy + (int)(2 * scale), checkboxSize, checkboxSize);
                b.Draw(Game1.mouseCursors, checkbox, todo.Done ? UIHelper.CheckboxChecked : UIHelper.CheckboxUnchecked, Color.White);
                float textX = checkbox.Right + 12 * scale;
                if (todo.ItemId != null)
                {
                    // drawInMenu draws centered in a 64px box, so offset it to fill an iconSize box at textX
                    Item? icon = ItemCatalog.GetIcon(todo.ItemId, todo.Quality);
                    icon?.drawInMenu(b, new Vector2(textX + iconSize / 2f - 32, cy + iconSize / 2f - 32), iconSize / 64f);
                    textX += iconSpace;
                }
                b.DrawString(font, text, new Vector2(textX, cy), todo.Done ? Game1.textColor * 0.45f : Game1.textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);

                hitAreas.Add((checkbox, todo, list));
                cy += lineHeight + gap;
            }
        }

        private static string LabelOf(TodoItem todo)
        {
            if (todo.ItemId == null)
                return todo.Text;
            int have = ItemCatalog.CountInInventory(todo.ItemId, todo.Quality);
            return $"{todo.Text} ({Math.Min(have, todo.Quantity)}/{todo.Quantity})";
        }

        /// <summary>Toggle a pinned to-do if the click hits one. Returns whether the click was handled.</summary>
        public bool Click(Vector2 position)
        {
            foreach (var (area, todo, list) in hitAreas)
            {
                if (area.Contains((int)position.X, (int)position.Y))
                {
                    list.SetDone(todo, !todo.Done);
                    Game1.playSound("drumkit6");
                    return true;
                }
            }
            return false;
        }
    }
}
