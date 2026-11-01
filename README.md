# Archipelago.Buggos

A [BepInEx](https://github.com/BepInEx/BepInEx) (IL2CPP) mod that lets **Buggos** be played as an [Archipelago](https://archipelago.gg) client. Archipelago is a multiworld randomizer framework: your Buggos progress sends items to other players' games, and their progress sends items to yours. For more information, visit the website or join the Discord.


**This is the game-side mod.** The Archipelago world that generates your randomizer is in [Archipelago.Buggos-World](https://github.com/lkk965516-sys/Archipelago.Buggos-World). You need both.

## Gameplay

**Checks** are sent when you:
- beat a mission for the first time
- collect evolution points

There are **161 locations** in total.

**Items** in the pool are:
- every evolution tree talent
- your starting unit
- filler stat increases
- traps

### Traps

| Trap | Effect |
| --- | --- |
| Damage Zero | Your units deal no damage for a while (default 30 s) |
| Leave Mission | Kicks you out of the current mission |
| Lock Swarmer | Temporarily locks the Swarmer (default 120 s) |

Trap durations can be changed in the [config file](#configuration). The item names shown by the server (for example "Set Damage to Zero for 30s") are fixed text and don't change with the config.

## Achieving Victory

The goal is to **beat every mission in the game**. Clearing the final area triggers the victory cutscene and completes your goal.

## What you need

- A copy of **Buggos** (Steam, PC)
- **BepInEx 6 (IL2CPP, 64-bit)**. Get the **Unity IL2CPP x64** build from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases).
- The latest **Archipelago.Buggos** zip from this repository's [Releases](https://github.com/lkk965516-sys/Archipelago.Buggos/releases) page
- The [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) launcher (0.6.8 or newer), plus the `buggos.apworld` and YAML template from the [world's releases](https://github.com/lkk965516-sys/Archipelago.Buggos-World/releases), to generate a game. You can also have a friend host it for you.

You don't need to install any extra libraries. The release already includes the Archipelago client library.

## Installing the mod

1. **Install BepInEx.** Extract the BepInEx zip into your Buggos game folder, so that `BepInEx/` and `winhttp.dll` sit next to `Buggos.exe`.
   (To find the folder in Steam: right-click Buggos → Manage → Browse local files.)
2. **Run the game once, then close it.** BepInEx uses this first launch to set itself up. This can take a minute or two, so wait for the game window to appear.
3. **Install the mod.** Open the `Buggos` folder inside the release zip and copy its contents into your Buggos game folder. Allow Windows to merge the existing `BepInEx` folders if prompted. You should end up with:
   ```
   Buggos/
   ├─ Buggos.exe
   └─ BepInEx/
      ├─ config/
      │  └─ HarmonyModPlugin.cfg
      └─ plugins/
         └─ ArchipelagoBuggos/
            ├─ ArchipelagoBuggos.dll
            ├─ Archipelago.MultiClient.Net.dll
            └─ Newtonsoft.Json.dll
   ```
4. **Launch the game.**

If you're updating from an earlier version and already have a `HarmonyModPlugin.cfg`, keep it to preserve your settings.

## Setting up your game

1. Install the Buggos `.apworld` into your Archipelago launcher (see the [world README](https://github.com/lkk965516-sys/Archipelago.Buggos-World#installation)).
2. Make your YAML and put it in your Archipelago `Players` folder.
3. Generate the game and host it, or give the YAML to whoever is hosting.

## Connecting

**In game:** at the main menu, press **F8** to open the connection window. Enter the server (`host:port`), your slot name and the password if there is one, and connect. Then choose **New Game** or **Continue**. Press F8 again to hide the window.

**Config file:** you can also set your details in `BepInEx/config/HarmonyModPlugin.cfg`:

```ini
[Archipelago]
Server = archipelago.gg:38281
Slotname = YourSlotName
Password =
```

To connect automatically a few seconds after launch, set `ConnectOnStart = true` in the `[Connection]` section.

## Save files

The mod keeps **one game save per slot** by swapping save files when you connect, and makes a backup first. This is controlled by the `[Saves]` section of the config.

Your received items and sent checks are stored per slot and seed in `BepInEx/BuggosAPWorld/`, so you can quit and resume without losing items or re-sending checks.

## Building from source

Building needs the game's interop assemblies (from `BepInEx/interop`, created after the first launch with BepInEx), so you need a copy of Buggos with BepInEx installed.

1. Set the `BUGGOS_GAME_DIR` environment variable to your Buggos folder.
2. Build `Source/HarmonyModPlugin.csproj` (.NET 6). The build copies the plugin into `BepInEx/plugins` automatically.

Requires the [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net) package (restored automatically).

## Credits

Iggy and the Archipelago community. The connection handling was adapted from an existing Archipelago mod for a different game.
