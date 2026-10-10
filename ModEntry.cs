using System.IO;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using TaskManager.UI;

namespace TaskManager
{
    public class ModEntry : Mod
    {
        internal ModConfig Config = null!;
        internal ModData Data = new();
        internal int LastTab;
        internal LocalTodoList LocalTodos = null!;
        internal SharedTodoList SharedTodos = null!;

        private PinnedOverlay overlay = null!;
        private string? dataPath;
        private bool dirty;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            I18n.Init(helper.Translation);
            LocalTodos = new LocalTodoList(this);
            SharedTodos = new SharedTodoList(this);
            overlay = new PinnedOverlay(this);

            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Display.RenderedHud += (_, e) => overlay.Draw(e.SpriteBatch);
            helper.Events.GameLoop.GameLaunched += (_, _) => Integrations.GenericModConfigMenu.Register(this);
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += (_, _) => SaveData();
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        internal void MarkDirty() => dirty = true;

        internal void SaveData()
        {
            if (dataPath == null)
                return;
            Helper.Data.WriteJsonFile(dataPath, Data);
            dirty = false;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            string key = Constants.SaveFolderName ?? $"{Game1.player.Name}_{Game1.uniqueIDForThisGame}";
            foreach (char c in Path.GetInvalidFileNameChars())
                key = key.Replace(c, '_');

            dataPath = Path.Combine("data", key + ".json");
            Data = Helper.Data.ReadJsonFile<ModData>(dataPath) ?? new ModData();
            Data.Notes ??= new();
            Data.Todos ??= new();
            Data.CollapsedAreas ??= new();
            Data.PinnedShared ??= new();
            dirty = false;
            SharedTodos.OnSaveLoaded();
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            SaveData();
            dataPath = null;
            Data = new ModData();
            SharedTodos.Reset();
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            // never leave one of our text fields capturing the keyboard after the menu is gone
            if (Game1.activeClickableMenu is not NotesMenu && Game1.keyboardDispatcher.Subscriber is TextArea)
                Game1.keyboardDispatcher.Subscriber = null;

            if (dirty && e.IsMultipleOf(60))
                SaveData();
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.MouseLeft && Context.IsPlayerFree && overlay.Click(e.Cursor.GetScaledScreenPixels()))
                Helper.Input.Suppress(e.Button);
        }

        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.ToggleKey.JustPressed())
                return;

            if (Game1.activeClickableMenu is NotesMenu menu)
            {
                if (!menu.IsTyping)
                    menu.exitThisMenu();
            }
            else if (Game1.activeClickableMenu == null && Context.IsPlayerFree)
            {
                Helper.Input.SuppressActiveKeybinds(Config.ToggleKey);
                Game1.activeClickableMenu = new NotesMenu(this, LastTab);
            }
        }
    }
}
