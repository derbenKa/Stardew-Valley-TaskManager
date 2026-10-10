using StardewModdingAPI;

namespace TaskManager.Integrations
{
    /// <summary>Registers the config options with Generic Mod Config Menu, if it's installed.</summary>
    internal static class GenericModConfigMenu
    {
        public static void Register(ModEntry mod)
        {
            var api = mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api == null)
                return;

            IManifest manifest = mod.ModManifest;
            api.Register(
                manifest,
                reset: () => mod.Config = new ModConfig(),
                save: () => mod.Helper.WriteConfig(mod.Config)
            );

            api.AddSectionTitle(manifest, () => I18n.Get("config.general"));
            api.AddKeybindList(manifest, () => mod.Config.ToggleKey, value => mod.Config.ToggleKey = value,
                () => I18n.Get("config.toggleKey"), () => I18n.Get("config.toggleKey.tooltip"));

            api.AddSectionTitle(manifest, () => I18n.Get("config.pinned"));
            api.AddBoolOption(manifest, () => mod.Config.ShowPinnedTodos, value => mod.Config.ShowPinnedTodos = value,
                () => I18n.Get("config.showPinned"), () => I18n.Get("config.showPinned.tooltip"));
            api.AddNumberOption(manifest, () => mod.Config.PinnedScale, value => mod.Config.PinnedScale = value,
                () => I18n.Get("config.pinnedScale"), () => I18n.Get("config.pinnedScale.tooltip"),
                min: 0.5f, max: 2f, interval: 0.05f, formatValue: value => $"{value * 100:0}%");
            api.AddNumberOption(manifest, () => mod.Config.PinnedWidth, value => mod.Config.PinnedWidth = value,
                () => I18n.Get("config.pinnedWidth"), () => I18n.Get("config.pinnedWidth.tooltip"),
                min: 200, max: 800, interval: 10);
            api.AddNumberOption(manifest, () => mod.Config.PinnedX, value => mod.Config.PinnedX = value,
                () => I18n.Get("config.pinnedX"), null, min: 0, max: 3000, interval: 4);
            api.AddNumberOption(manifest, () => mod.Config.PinnedY, value => mod.Config.PinnedY = value,
                () => I18n.Get("config.pinnedY"), null, min: 0, max: 2000, interval: 4);

            api.AddSectionTitle(manifest, () => I18n.Get("config.bundles"));
            api.AddBoolOption(manifest, () => mod.Config.ShowCompletedBundles, value => mod.Config.ShowCompletedBundles = value,
                () => I18n.Get("config.showCompleted"), () => I18n.Get("config.showCompleted.tooltip"));
        }
    }
}
