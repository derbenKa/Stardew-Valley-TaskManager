using StardewModdingAPI.Utilities;

namespace TaskManager
{
    public class ModConfig
    {
        /// <summary>Key(s) that open and close TaskManager.</summary>
        public KeybindList ToggleKey { get; set; } = KeybindList.Parse("F2");

        /// <summary>Whether completed bundles are listed (greyed out) on the Community Center page.</summary>
        public bool ShowCompletedBundles { get; set; } = true;

        /// <summary>Whether pinned to-dos are shown on screen while playing.</summary>
        public bool ShowPinnedTodos { get; set; } = true;

        /// <summary>Position and width of the pinned to-do overlay, in UI pixels.</summary>
        public int PinnedX { get; set; } = 16;
        public int PinnedY { get; set; } = 16;
        public int PinnedWidth { get; set; } = 340;

        /// <summary>Size of the pinned to-do overlay (1 = normal).</summary>
        public float PinnedScale { get; set; } = 1f;
    }
}
