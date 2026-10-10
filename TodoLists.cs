using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace TaskManager
{
    /// <summary>A to-do list the UI can show and edit.</summary>
    internal interface ITodoList
    {
        IReadOnlyList<TodoItem> Items { get; }

        /// <summary>Extra info shown below the list, if any.</summary>
        string? StatusText { get; }

        void Add(TodoItem item);
        void SetDone(TodoItem item, bool done);
        void SetPinned(TodoItem item, bool pinned);
        void Remove(TodoItem item);
        int ClearDone();
    }

    /// <summary>The player's own to-do list, saved in the mod's data folder.</summary>
    internal class LocalTodoList : ITodoList
    {
        private readonly ModEntry mod;

        public LocalTodoList(ModEntry mod)
        {
            this.mod = mod;
        }

        public IReadOnlyList<TodoItem> Items => mod.Data.Todos;

        public string? StatusText => null;

        public void Add(TodoItem item)
        {
            mod.Data.Todos.Add(item);
            mod.MarkDirty();
        }

        public void SetDone(TodoItem item, bool done)
        {
            item.Done = done;
            if (done)
                item.Pinned = false;
            mod.MarkDirty();
        }

        public void SetPinned(TodoItem item, bool pinned)
        {
            item.Pinned = pinned;
            mod.MarkDirty();
        }

        public void Remove(TodoItem item)
        {
            mod.Data.Todos.Remove(item);
            mod.MarkDirty();
        }

        public int ClearDone()
        {
            int removed = mod.Data.Todos.RemoveAll(t => t.Done);
            if (removed > 0)
                mod.MarkDirty();
            return removed;
        }
    }

    /// <summary>
    /// A to-do list shared by all players. The host owns the list and stores it in the save file;
    /// other players send their changes to the host, which applies them and broadcasts the new list.
    /// </summary>
    internal class SharedTodoList : ITodoList
    {
        private const string SaveKey = "shared-todos";
        private const string MessageRequest = "SharedTodos.Request";
        private const string MessageSync = "SharedTodos.Sync";
        private const string MessageChange = "SharedTodos.Change";

        private readonly ModEntry mod;
        private List<TodoItem> items = new();
        private bool received;

        /// <summary>A single change to the shared list.</summary>
        private class Change
        {
            public string Type { get; set; } = "";
            public string? Id { get; set; }
            public bool Done { get; set; }
            public TodoItem? Item { get; set; }
        }

        public SharedTodoList(ModEntry mod)
        {
            this.mod = mod;
            mod.Helper.Events.Multiplayer.ModMessageReceived += OnMessageReceived;
        }

        public IReadOnlyList<TodoItem> Items => items;

        public string? StatusText => received ? I18n.Get("shared.info") : I18n.Get("shared.waiting");

        public void OnSaveLoaded()
        {
            items = new();
            received = false;
            if (Context.IsMainPlayer)
            {
                items = mod.Helper.Data.ReadSaveData<List<TodoItem>>(SaveKey) ?? new();
                received = true;
                ApplyPins();
            }
            else
                Send(MessageRequest, true, toHost: true);
        }

        public void Reset()
        {
            items = new();
            received = false;
        }

        public void Add(TodoItem item)
        {
            item.AddedBy = Game1.player.Name;
            item.Pinned = false;
            Apply(new Change { Type = "add", Item = item });
        }

        public void SetDone(TodoItem item, bool done) => Apply(new Change { Type = "done", Id = item.Id, Done = done });

        public void Remove(TodoItem item) => Apply(new Change { Type = "remove", Id = item.Id });

        public int ClearDone()
        {
            int count = items.Count(t => t.Done);
            if (count > 0)
                Apply(new Change { Type = "clear" });
            return count;
        }

        /// <summary>Pins are per player, so they're stored locally rather than synced.</summary>
        public void SetPinned(TodoItem item, bool pinned)
        {
            item.Pinned = pinned;
            mod.Data.PinnedShared.Remove(item.Id);
            if (pinned)
                mod.Data.PinnedShared.Add(item.Id);
            mod.MarkDirty();
        }

        private void Apply(Change change)
        {
            // apply right away so the UI responds instantly; the host's sync overwrites it afterwards
            ApplyLocally(change);
            if (Context.IsMainPlayer)
                Publish();
            else
                Send(MessageChange, change, toHost: true);
        }

        private void ApplyLocally(Change change)
        {
            switch (change.Type)
            {
                case "add":
                    if (change.Item != null && items.All(t => t.Id != change.Item.Id))
                        items.Add(change.Item);
                    break;

                case "done":
                    TodoItem? item = items.FirstOrDefault(t => t.Id == change.Id);
                    if (item != null)
                        item.Done = change.Done;
                    break;

                case "remove":
                    items.RemoveAll(t => t.Id == change.Id);
                    break;

                case "clear":
                    items.RemoveAll(t => t.Done);
                    break;
            }
            ApplyPins();
        }

        /// <summary>Host only: save the list and send it to everyone.</summary>
        private void Publish()
        {
            mod.Helper.Data.WriteSaveData(SaveKey, items);
            Send(MessageSync, items);
        }

        private void ApplyPins()
        {
            // finished tasks are unpinned, same as in the personal list
            mod.Data.PinnedShared.RemoveAll(id => items.Any(t => t.Id == id && t.Done));
            foreach (TodoItem item in items)
                item.Pinned = mod.Data.PinnedShared.Contains(item.Id);
        }

        private void Send<T>(string type, T message, bool toHost = false)
        {
            long[]? players = toHost ? new[] { Game1.MasterPlayer.UniqueMultiplayerID } : null;
            mod.Helper.Multiplayer.SendMessage(message, type, new[] { mod.ModManifest.UniqueID }, players);
        }

        private void OnMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != mod.ModManifest.UniqueID || !Context.IsWorldReady)
                return;

            switch (e.Type)
            {
                case MessageRequest when Context.IsMainPlayer:
                    mod.Helper.Multiplayer.SendMessage(items, MessageSync, new[] { mod.ModManifest.UniqueID }, new[] { e.FromPlayerID });
                    break;

                case MessageChange when Context.IsMainPlayer:
                    ApplyLocally(e.ReadAs<Change>());
                    Publish();
                    break;

                case MessageSync when !Context.IsMainPlayer:
                    items = e.ReadAs<List<TodoItem>>() ?? new();
                    received = true;
                    ApplyPins();
                    break;
            }
        }
    }
}
