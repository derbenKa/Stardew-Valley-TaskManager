using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace TaskManager.UI
{
    /// <summary>Searchable list of items plus helpers for item-based to-dos.</summary>
    internal static class ItemCatalog
    {
        private static readonly Dictionary<string, Item?> Icons = new();
        private static List<(string Id, string Name)>? all;
        private static string? language;

        /// <summary>All objects and big craftables, one entry per display name.</summary>
        private static List<(string Id, string Name)> All
        {
            get
            {
                string currentLanguage = LocalizedContentManager.CurrentLanguageCode.ToString();
                if (all == null || language != currentLanguage)
                {
                    language = currentLanguage;
                    all = Build();
                }
                return all;
            }
        }

        private static List<(string Id, string Name)> Build()
        {
            var result = new List<(string Id, string Name)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string type in new[] { "(O)", "(BC)" })
            {
                IItemDataDefinition? definition = ItemRegistry.GetTypeDefinition(type);
                if (definition == null)
                    continue;

                foreach (string id in definition.GetAllIds())
                {
                    ParsedItemData? data = ItemRegistry.GetData(type + id);
                    if (data == null || data.IsErrorItem || data.ObjectType == "Litter" || string.IsNullOrWhiteSpace(data.DisplayName))
                        continue;
                    if (seen.Add(data.DisplayName))
                        result.Add((data.QualifiedItemId, data.DisplayName));
                }
            }
            return result;
        }

        public static List<(string Id, string Name)> Search(string query, int limit = 60)
        {
            query = query.Trim();
            if (query.Length == 0)
                return new();

            return All
                .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(p => p.Name)
                .Take(limit)
                .ToList();
        }

        public static string GetName(string id) => ItemRegistry.GetDataOrErrorItem(id).DisplayName;

        /// <summary>A cached item instance for drawing its icon.</summary>
        public static Item? GetIcon(string id, int quality = 0)
        {
            string key = $"{id}|{quality}";
            if (!Icons.TryGetValue(key, out Item? item))
                Icons[key] = item = ItemRegistry.Create(id, 1, quality, allowNull: true);
            return item;
        }

        /// <summary>How many matching items the player is carrying.</summary>
        public static int CountInInventory(string id, int minQuality)
        {
            return Game1.player.Items
                .Where(i => i != null && i.QualifiedItemId == id && i.Quality >= minQuality)
                .Sum(i => i.Stack);
        }
    }
}
