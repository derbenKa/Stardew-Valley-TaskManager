using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace TaskManager.UI
{
    internal static class UIHelper
    {
        private static readonly Rectangle BoxSource = new(384, 396, 15, 15);
        private static readonly Rectangle ButtonSource = new(432, 439, 9, 9);
        private static readonly Rectangle CloseSource = new(337, 494, 12, 12);
        public static readonly Rectangle CheckboxUnchecked = new(227, 425, 9, 9);
        public static readonly Rectangle CheckboxChecked = new(236, 425, 9, 9);
        public static readonly Rectangle CoinSource = new(193, 373, 9, 10);
        private static readonly Rectangle StarSource = new(346, 400, 8, 8);
        public static readonly Rectangle ArrowRightSource = new(365, 495, 12, 11);
        public static readonly Rectangle ArrowDownSource = new(421, 472, 11, 12);

        public static bool IsHovered(Rectangle r) => r.Contains(Game1.getMouseX(), Game1.getMouseY());

        public static void DrawBox(SpriteBatch b, Rectangle r)
        {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, BoxSource, r.X, r.Y, r.Width, r.Height, Color.White, 4f, false);
        }

        public static void DrawButton(SpriteBatch b, Rectangle r, string text)
        {
            Color tint = IsHovered(r) ? Color.Wheat : Color.White;
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, ButtonSource, r.X, r.Y, r.Width, r.Height, tint, 4f, false);
            Vector2 size = Game1.smallFont.MeasureString(text);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2(r.X + (r.Width - size.X) / 2, r.Y + (r.Height - size.Y) / 2 + 2), Game1.textColor);
        }

        public static void DrawDeleteIcon(SpriteBatch b, Rectangle r, bool highlighted)
        {
            Color tint = highlighted ? Color.Red : IsHovered(r) ? Color.White : Color.White * 0.6f;
            b.Draw(Game1.mouseCursors, r, CloseSource, tint);
        }

        public static void DrawPinIcon(SpriteBatch b, Rectangle r, bool pinned)
        {
            Color tint = pinned ? Color.White : IsHovered(r) ? Color.White * 0.8f : Color.Black * 0.35f;
            b.Draw(Game1.mouseCursors, r, StarSource, tint);
        }

        public static void DrawScrollBar(SpriteBatch b, Rectangle track, int first, int visible, int total)
        {
            if (total <= visible)
                return;
            b.Draw(Game1.staminaRect, track, Color.Black * 0.15f);
            int height = Math.Max(24, track.Height * visible / total);
            int y = track.Y + (track.Height - height) * first / Math.Max(1, total - visible);
            b.Draw(Game1.staminaRect, new Rectangle(track.X, y, track.Width, height), Color.SaddleBrown * 0.8f);
        }

        /// <summary>Shorten text with "..." so it fits into the given width.</summary>
        public static string Truncate(SpriteFont font, string text, float maxWidth)
        {
            if (font.MeasureString(text).X <= maxWidth)
                return text;
            const string ellipsis = "...";
            int len = text.Length;
            while (len > 0 && font.MeasureString(text.Substring(0, len) + ellipsis).X > maxWidth)
                len--;
            return text.Substring(0, len).TrimEnd() + ellipsis;
        }
    }
}
