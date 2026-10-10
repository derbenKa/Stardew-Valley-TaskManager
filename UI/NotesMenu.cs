using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace TaskManager.UI
{
    /// <summary>The TaskManager menu with tabs for notes, to-dos and Community Center bundles.</summary>
    internal class NotesMenu : IClickableMenu
    {
        private const int TabWidth = 280;
        private const int TabHeight = 80;

        private readonly ModEntry mod;
        private readonly Page[] pages;
        private readonly string[] tabNames;
        private readonly List<Rectangle> tabs = new();
        private int currentTab;

        public bool IsTyping => pages[currentTab].IsTyping;

        public NotesMenu(ModEntry mod, int startTab)
            : base(0, 0, 0, 0, showUpperRightCloseButton: true)
        {
            this.mod = mod;
            tabNames = new[] { I18n.Get("tab.notes"), I18n.Get("tab.todo"), I18n.Get("tab.shared"), I18n.Get("tab.bundles") };
            pages = new Page[] { new NotesPage(mod), new TodoPage(mod.LocalTodos), new TodoPage(mod.SharedTodos), new BundlePage(mod) };
            currentTab = Math.Clamp(startTab, 0, pages.Length - 1);

            Layout();
            pages[currentTab].OnActivate();
            Game1.playSound("bigSelect");
        }

        private void Layout()
        {
            width = Math.Min(1240, Game1.uiViewport.Width - 64);
            height = Math.Min(820, Game1.uiViewport.Height - 160);
            xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
            yPositionOnScreen = (Game1.uiViewport.Height - height) / 2 + 40;
            initializeUpperRightCloseButton();

            tabs.Clear();
            int tabWidth = Math.Min(TabWidth, (width - 64) / pages.Length - 8);
            for (int i = 0; i < pages.Length; i++)
                tabs.Add(new Rectangle(xPositionOnScreen + 32 + i * (tabWidth + 8), yPositionOnScreen - TabHeight + 20, tabWidth, TabHeight));

            var content = new Rectangle(xPositionOnScreen + 44, yPositionOnScreen + 44, width - 88, height - 88);
            foreach (Page page in pages)
                page.SetBounds(content);
        }

        private void SwitchTab(int index)
        {
            if (index == currentTab)
                return;
            pages[currentTab].OnDeactivate();
            currentTab = index;
            mod.LastTab = index;
            pages[currentTab].OnActivate();
            Game1.playSound("smallSelect");
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            Layout();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (upperRightCloseButton?.containsPoint(x, y) == true)
            {
                exitThisMenu();
                return;
            }

            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].Contains(x, y))
                {
                    SwitchTab(i);
                    return;
                }
            }

            pages[currentTab].LeftClick(x, y);
        }

        public override void receiveKeyPress(Keys key)
        {
            if (IsTyping)
            {
                // while typing, keys belong to the text field; Escape only leaves the field
                if (key == Keys.Escape)
                    pages[currentTab].StopTyping();
                return;
            }

            if (key == Keys.Escape || Game1.options.doesInputListContain(Game1.options.menuButton, key))
                exitThisMenu();
        }

        public override void receiveScrollWheelAction(int direction)
        {
            pages[currentTab].Scroll(direction, Game1.getMouseX(), Game1.getMouseY());
        }

        protected override void cleanupBeforeExit()
        {
            pages[currentTab].OnDeactivate();
            mod.SaveData();
            base.cleanupBeforeExit();
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);

            // tabs (drawn first so the main box covers their lower edge)
            for (int i = 0; i < tabs.Count; i++)
            {
                Rectangle tab = tabs[i];
                bool active = i == currentTab;
                if (active)
                    tab.Y -= 8;
                Color tint = active ? Color.White : UIHelper.IsHovered(tab) ? Color.White * 0.9f : Color.LightGray;
                drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), tab.X, tab.Y, tab.Width, tab.Height, tint);

                Vector2 size = Game1.smallFont.MeasureString(tabNames[i]);
                Utility.drawTextWithShadow(b, tabNames[i], Game1.smallFont, new Vector2(tab.Center.X - size.X / 2, tab.Y + 22), Game1.textColor);
            }

            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            Page page = pages[currentTab];
            page.Draw(b);

            base.draw(b);

            if (!string.IsNullOrEmpty(page.HoverText))
                drawHoverText(b, page.HoverText, Game1.smallFont);

            drawMouse(b);
        }
    }
}
