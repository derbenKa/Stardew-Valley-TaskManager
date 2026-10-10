using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace TaskManager.UI
{
    /// <summary>Lists every item still missing from the Community Center bundles.</summary>
    internal class BundlePage : Page
    {
        private const int ItemRowHeight = 68;

        /// <summary>Area order as shown in the game's bundle menu.</summary>
        private static readonly string[] AreaOrder = { "Crafts Room", "Pantry", "Fish Tank", "Boiler Room", "Bulletin Board", "Vault", "Abandoned Joja Mart" };

        private readonly ModEntry mod;
        private readonly List<Row> rows = new();
        private readonly List<(Rectangle Area, string Key)> areaHeaders = new();
        private readonly List<(Rectangle Cell, BundleItem Item)> itemCells = new();
        private int firstRow;

        private enum RowKind { Message, Area, Bundle, Items }

        private class Row
        {
            public RowKind Kind;
            public string Text = "";
            public string Info = "";
            public string AreaKey = "";
            public bool Collapsed;
            public bool Done;
            public List<BundleItem> Items = new();
            public int Height;
        }

        private class BundleItem
        {
            public Item? Item;
            public string Name = "";
            public int Count;
            public int Quality;
            public bool IsGold;
            public string Bundle = "";

            /// <summary>Text for a to-do created from this item.</summary>
            public string TodoText
            {
                get
                {
                    string name = !IsGold && Quality > 0 ? $"{Name} ({I18n.Get("quality." + Quality)})" : Name;
                    if (!IsGold && Count > 1)
                        name += $" x{Count}";
                    return I18n.Get("cc.todoText", new { item = name, bundle = Bundle });
                }
            }
        }

        private class BundleInfo
        {
            public string Area = "";
            public string Name = "";
            public int Required;
            public int Donated;
            public List<BundleItem> Missing = new();
            public bool Complete => Donated >= Required;
        }

        public BundlePage(ModEntry mod)
        {
            this.mod = mod;
        }

        private int Columns => Bounds.Width >= 1000 ? 3 : 2;

        public override void SetBounds(Rectangle bounds)
        {
            bool widthChanged = bounds.Width != Bounds.Width;
            base.SetBounds(bounds);
            if (widthChanged && rows.Count > 0)
                Build();
        }

        public override void OnActivate() => Build();

        /*********
        ** Data
        *********/
        private void Build()
        {
            rows.Clear();
            areaHeaders.Clear();
            firstRow = 0;

            if (Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
            {
                AddMessage(I18n.Get("cc.joja"), false);
                return;
            }

            List<BundleInfo> bundles = ReadBundles();
            if (bundles.Count == 0)
            {
                AddMessage(I18n.Get("cc.noData"), false);
                return;
            }

            int done = bundles.Count(p => p.Complete);
            if (done == bundles.Count || Game1.MasterPlayer.hasCompletedCommunityCenter())
                AddMessage(I18n.Get("cc.complete"), true);
            AddMessage(I18n.Get("cc.summary", new { done, total = bundles.Count }), false);

            var areas = bundles
                .GroupBy(p => p.Area)
                .OrderBy(g => Array.IndexOf(AreaOrder, g.Key) is int i && i >= 0 ? i : int.MaxValue);

            foreach (var area in areas)
            {
                var list = area.ToList();
                int areaDone = list.Count(p => p.Complete);
                bool areaComplete = areaDone == list.Count;
                bool collapsed = mod.Data.CollapsedAreas.Contains(area.Key);
                rows.Add(new Row
                {
                    Kind = RowKind.Area,
                    Text = AreaName(area.Key),
                    Info = areaComplete ? I18n.Get("cc.areaDone") : $"{areaDone}/{list.Count}",
                    AreaKey = area.Key,
                    Collapsed = collapsed,
                    Done = areaComplete,
                    Height = 60
                });
                if (collapsed)
                    continue;

                foreach (BundleInfo bundle in list.Where(p => !p.Complete))
                {
                    int needed = bundle.Required - bundle.Donated;
                    string info = bundle.Missing.Count > needed
                        ? I18n.Get("cc.bundleChoose", new { count = needed, options = bundle.Missing.Count })
                        : I18n.Get("cc.bundleNeed", new { count = needed });
                    rows.Add(new Row { Kind = RowKind.Bundle, Text = bundle.Name, Info = info, Height = 44 });

                    for (int i = 0; i < bundle.Missing.Count; i += Columns)
                        rows.Add(new Row { Kind = RowKind.Items, Items = bundle.Missing.Skip(i).Take(Columns).ToList(), Height = ItemRowHeight });
                }

                if (mod.Config.ShowCompletedBundles)
                {
                    foreach (BundleInfo bundle in list.Where(p => p.Complete))
                        rows.Add(new Row { Kind = RowKind.Bundle, Text = bundle.Name, Info = I18n.Get("cc.bundleDone"), Done = true, Height = 40 });
                }
            }
        }

        private void AddMessage(string text, bool highlight)
        {
            string wrapped = Game1.parseText(text, Game1.smallFont, Bounds.Width - 32);
            int height = (int)Game1.smallFont.MeasureString(wrapped).Y + 16;
            rows.Add(new Row { Kind = RowKind.Message, Text = wrapped, Done = highlight, Height = height });
        }

        private static List<BundleInfo> ReadBundles()
        {
            var result = new List<BundleInfo>();
            var world = Game1.netWorldState.Value;
            Dictionary<string, string>? data = world?.BundleData;
            if (data == null)
                return result;

            foreach (var (key, value) in data)
            {
                string[] keyParts = key.Split('/');
                if (keyParts.Length < 2 || !int.TryParse(keyParts[1], out int index))
                    continue;

                string[] fields = value.Split('/');
                string[] tokens = fields.Length > 2 ? fields[2].Split(' ', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();
                int itemCount = tokens.Length / 3;
                if (itemCount == 0)
                    continue;

                int required = fields.Length > 4 && int.TryParse(fields[4], out int r) && r > 0 ? Math.Min(r, itemCount) : itemCount;
                bool[] donated = world!.Bundles.ContainsKey(index) ? world.Bundles[index] : Array.Empty<bool>();

                var bundle = new BundleInfo
                {
                    Area = keyParts[0],
                    Name = fields.Length > 6 && !string.IsNullOrWhiteSpace(fields[6]) ? fields[6] : fields[0],
                    Required = required
                };

                for (int i = 0; i < itemCount; i++)
                {
                    if (i < donated.Length && donated[i])
                    {
                        bundle.Donated++;
                        continue;
                    }
                    int.TryParse(tokens[i * 3 + 1], out int count);
                    int.TryParse(tokens[i * 3 + 2], out int quality);
                    BundleItem item = CreateItem(tokens[i * 3], count, quality);
                    item.Bundle = bundle.Name;
                    bundle.Missing.Add(item);
                }
                result.Add(bundle);
            }
            return result;
        }

        private static BundleItem CreateItem(string id, int count, int quality)
        {
            if (id == "-1")
                return new BundleItem { IsGold = true, Count = count, Name = I18n.Get("cc.gold", new { amount = count.ToString("N0") }) };

            Item? item = ItemRegistry.Create(id, 1, quality, allowNull: true);
            return new BundleItem
            {
                Item = item,
                Name = item?.DisplayName ?? ItemRegistry.GetDataOrErrorItem(id).DisplayName,
                Count = count,
                Quality = quality
            };
        }

        private static string AreaName(string area)
        {
            string key = "area." + area.Replace(" ", "");
            string name = I18n.Get(key);
            return name.Contains(key) ? area : name;
        }

        /*********
        ** Input
        *********/
        private int MaxFirstRow()
        {
            int height = 0;
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                height += rows[i].Height;
                if (height > Bounds.Height)
                    return i + 1;
            }
            return 0;
        }

        public override void LeftClick(int x, int y)
        {
            foreach (var (area, key) in areaHeaders)
            {
                if (!area.Contains(x, y))
                    continue;

                if (!mod.Data.CollapsedAreas.Remove(key))
                    mod.Data.CollapsedAreas.Add(key);
                mod.MarkDirty();
                Game1.playSound("shwip");

                // rebuild but keep the clicked header where it was
                int scroll = firstRow;
                Build();
                firstRow = Math.Clamp(scroll, 0, MaxFirstRow());
                return;
            }

            foreach (var (cell, item) in itemCells)
            {
                if (!cell.Contains(x, y))
                    continue;

                string text = item.TodoText;
                if (HasOpenTodo(text))
                {
                    Game1.playSound("cancel");
                    return;
                }
                var todo = new TodoItem { Text = text };
                if (item.Item != null)
                {
                    todo.ItemId = item.Item.QualifiedItemId;
                    todo.Quantity = Math.Max(1, item.Count);
                    todo.Quality = item.Quality;
                }
                mod.Data.Todos.Add(todo);
                mod.MarkDirty();
                Game1.playSound("coin");
                return;
            }
        }

        private bool HasOpenTodo(string text) => mod.Data.Todos.Any(t => !t.Done && t.Text == text);

        public override void Scroll(int direction, int mouseX, int mouseY)
        {
            firstRow = Math.Clamp(firstRow + (direction > 0 ? -2 : 2), 0, MaxFirstRow());
        }

        /*********
        ** Drawing
        *********/
        public override void Draw(SpriteBatch b)
        {
            HoverText = null;
            areaHeaders.Clear();
            itemCells.Clear();
            SpriteFont font = Game1.smallFont;
            int y = Bounds.Y;
            int visibleRows = 0;

            for (int i = firstRow; i < rows.Count; i++)
            {
                Row row = rows[i];
                if (y + row.Height > Bounds.Bottom)
                    break;
                visibleRows++;
                var rect = new Rectangle(Bounds.X, y, Bounds.Width - 20, row.Height);

                switch (row.Kind)
                {
                    case RowKind.Message:
                        b.DrawString(font, row.Text, new Vector2(rect.X + 8, rect.Y + 8), row.Done ? Color.DarkGreen : Game1.textColor);
                        break;

                    case RowKind.Area:
                    {
                        var bar = new Rectangle(rect.X, rect.Y + 8, rect.Width, rect.Height - 12);
                        areaHeaders.Add((bar, row.AreaKey));
                        float alpha = UIHelper.IsHovered(bar) ? 0.65f : 0.45f;
                        b.Draw(Game1.staminaRect, bar, (row.Done ? Color.LightGreen : Color.SandyBrown) * alpha);

                        Rectangle arrowSource = row.Collapsed ? UIHelper.ArrowRightSource : UIHelper.ArrowDownSource;
                        var arrow = new Rectangle(bar.X + 10, bar.Center.Y - arrowSource.Height, arrowSource.Width * 2, arrowSource.Height * 2);
                        b.Draw(Game1.mouseCursors, arrow, arrowSource, Color.White);

                        Vector2 size = Game1.dialogueFont.MeasureString(row.Text) * 0.7f;
                        b.DrawString(Game1.dialogueFont, row.Text, new Vector2(arrow.Right + 12, bar.Center.Y - size.Y / 2), Game1.textColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 1f);
                        if (UIHelper.IsHovered(bar))
                            HoverText = I18n.Get(row.Collapsed ? "cc.expand" : "cc.collapse");
                        Vector2 infoSize = font.MeasureString(row.Info);
                        b.DrawString(font, row.Info, new Vector2(bar.Right - infoSize.X - 12, bar.Center.Y - infoSize.Y / 2), row.Done ? Color.DarkGreen : Game1.textColor);
                        break;
                    }

                    case RowKind.Bundle:
                    {
                        Color color = row.Done ? Game1.textColor * 0.45f : Game1.textColor;
                        Vector2 size = font.MeasureString(row.Text);
                        var pos = new Vector2(rect.X + 16, rect.Y + (rect.Height - size.Y) / 2 + 2);
                        Utility.drawTextWithShadow(b, row.Text, font, pos, color, 1f, -1f, -1, -1, row.Done ? 0f : 0.5f);
                        b.DrawString(font, row.Info, new Vector2(pos.X + size.X + 20, pos.Y), row.Done ? Color.DarkGreen * 0.7f : Game1.textColor * 0.65f);
                        break;
                    }

                    case RowKind.Items:
                        DrawItems(b, rect, row.Items);
                        break;
                }
                y += row.Height;
            }

            UIHelper.DrawScrollBar(b, new Rectangle(Bounds.Right - 8, Bounds.Y, 6, Bounds.Height), firstRow, visibleRows, rows.Count);
        }

        private void DrawItems(SpriteBatch b, Rectangle rect, List<BundleItem> items)
        {
            SpriteFont font = Game1.smallFont;
            int cellWidth = (rect.Width - 32) / Columns;

            for (int i = 0; i < items.Count; i++)
            {
                BundleItem entry = items[i];
                var cell = new Rectangle(rect.X + 32 + i * cellWidth, rect.Y + 2, cellWidth - 8, rect.Height - 4);
                bool hovered = UIHelper.IsHovered(cell);
                bool inTodo = HasOpenTodo(entry.TodoText);
                itemCells.Add((cell, entry));
                if (hovered)
                    b.Draw(Game1.staminaRect, cell, Color.Wheat * 0.35f);

                if (entry.IsGold)
                    b.Draw(Game1.mouseCursors, new Rectangle(cell.X + 14, cell.Y + 12, 36, 40), UIHelper.CoinSource, Color.White);
                else
                    entry.Item?.drawInMenu(b, new Vector2(cell.X, cell.Y), 0.75f);

                string label = entry.IsGold || entry.Count <= 1 ? entry.Name : $"{entry.Name} x{entry.Count}";
                string shown = UIHelper.Truncate(font, label, cell.Width - 76);
                Vector2 size = font.MeasureString(shown);
                b.DrawString(font, shown, new Vector2(cell.X + 68, cell.Center.Y - size.Y / 2 + 2), Game1.textColor);

                // mark items that already have an open to-do
                if (inTodo)
                    b.Draw(Game1.mouseCursors, new Rectangle(cell.X + 40, cell.Y + 2, 20, 20), UIHelper.CheckboxChecked, Color.White);

                if (hovered)
                {
                    string tooltip = label;
                    if (!entry.IsGold && entry.Quality > 0)
                        tooltip += "\n" + I18n.Get("cc.minQuality", new { quality = I18n.Get("quality." + entry.Quality) });
                    tooltip += "\n\n" + I18n.Get(inTodo ? "cc.alreadyTodo" : "cc.addTodo");
                    HoverText = tooltip;
                }
            }
        }
    }
}
