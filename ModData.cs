using System;
using System.Collections.Generic;

namespace TaskManager
{
    /// <summary>Everything stored per save file.</summary>
    public class ModData
    {
        public List<Note> Notes { get; set; } = new();
        public List<TodoItem> Todos { get; set; } = new();

        /// <summary>Community Center areas collapsed on the bundle page.</summary>
        public List<string> CollapsedAreas { get; set; } = new();

        /// <summary>IDs of shared to-dos this player pinned to the screen.</summary>
        public List<string> PinnedShared { get; set; } = new();
    }

    public class Note
    {
        public string Text { get; set; } = "";
        public string Created { get; set; } = "";
    }

    public class TodoItem
    {
        /// <summary>Unique ID, used to sync shared to-dos.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Text { get; set; } = "";
        public bool Done { get; set; }

        /// <summary>Whether the task is shown in the on-screen overlay.</summary>
        public bool Pinned { get; set; }

        /// <summary>Qualified item ID if the task is about collecting an item, else null.</summary>
        public string? ItemId { get; set; }

        /// <summary>How many of the item are needed.</summary>
        public int Quantity { get; set; }

        /// <summary>Minimum item quality (0 normal, 1 silver, 2 gold, 4 iridium).</summary>
        public int Quality { get; set; }

        /// <summary>Name of the player who added a shared task.</summary>
        public string? AddedBy { get; set; }
    }
}
