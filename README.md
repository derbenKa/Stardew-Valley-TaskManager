# TaskManager (Stardew Valley Mod)

A SMAPI mod that adds an in-game notebook with notes, to-do lists and a Community Center checklist.
Press **F2** to open it.

## Features

- **Notes** – as many notes as you like, multi-line with word wrap. The list is on the left, the editor on the right.
  Delete a note with the red X (click twice to confirm).
- **To-do** – type a task at the top and press Enter or click "Add". Click the checkbox to complete a task
  (this also unpins it); "Remove completed" clears finished tasks.
  - **Pin** a task with the star to show it on screen while playing (top left). You can check it off right there.
  - Optionally **choose an item and amount**: the task then shows the item icon and your progress, counted from
    your inventory, e.g. "Wood 23/50".
- **Shared** – a to-do list shared by all players in multiplayer (everyone needs the mod). The host stores it in
  the save file and changes are synced to everyone instantly. Pinning is per player.
- **Community Center** – every item still missing, grouped by room and bundle, with icon, amount and minimum
  quality (tooltip). Click a room to collapse or expand it. Click an item to add it as a to-do.
  Reads the actual save data, so remixed bundles work too.

The mod is translated into English and German.

## Controls

- `F2` opens/closes the menu, `Esc` closes it (while typing, it leaves the text field first)
- Mouse wheel scrolls lists and the editor
- In text fields: arrow keys, Home/End, Delete, Backspace

Data is stored per save file in `Mods/TaskManager/data/<save name>.json`.

## Configuration

`Mods/TaskManager/config.json` (created on first launch):

```json
{
  "ToggleKey": "F2",
  "ShowCompletedBundles": true,
  "ShowPinnedTodos": true,
  "PinnedX": 16,
  "PinnedY": 16,
  "PinnedWidth": 340,
  "PinnedScale": 1.0
}
```

| Option | Description |
| --- | --- |
| `ToggleKey` | Key that opens and closes the menu. |
| `ShowCompletedBundles` | List completed bundles (greyed out) on the Community Center page. |
| `ShowPinnedTodos` | Show pinned to-dos on screen while playing. |
| `PinnedX`, `PinnedY` | Position of the pinned to-do box in pixels. |
| `PinnedWidth` | Width of the pinned to-do box at 100% size. |
| `PinnedScale` | Size of the pinned to-do box (1.0 = 100%). |

With [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) installed, all options can
also be changed in-game (gear icon on the title screen, or "Mod Options" in the game menu).

## Requirements

- [SMAPI](https://smapi.io/) 4.0 or later
- Stardew Valley 1.6
- Optional: [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098)

## Building

Requires the [.NET 6 SDK or later](https://dotnet.microsoft.com/download).

```bash
dotnet build -c Release
```

The build automatically copies the mod into the game's `Mods` folder
(via [Pathoschild.Stardew.ModBuildConfig](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig)).
