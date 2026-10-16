# Archipelago.Buggos

A [BepInEx](https://github.com/BepInEx/BepInEx) (IL2CPP) mod that lets **Buggos** be played as an [Archipelago](https://archipelago.gg) client. Archipelago is a multiworld randomizer framework. For more information, visit the website or join the Discord.

> **Status:** early development. Only the **campaign** is supported. Survival and challenges are not implemented.

## Gameplay

**Checks** are sent when you:
- beat a mission for the first time
- collect evolution points

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

Trap durations can be changed in the config file.

## Achieving Victory

Currently the only goal is to **beat every mission in the game**. Clearing the final area triggers the victory cutscene and completes your goal.

## Installation

1. Install BepInEx 6 (IL2CPP) into your Buggos folder.
2. Download the latest release and place `Archipelago.Buggos.dll` in `BepInEx/plugins/`.
3. Launch the game once so the config file is generated.

<!-- Adjust the steps and file names to match your release. -->

## Example YAML

A YAML for this game looks as follows:

```yaml
name: Player{number}
game: Buggos
Buggos:
  # options go here
```

<!-- Fill in your real options once the apworld is finalised. -->

## How to Play

### What you need
- A copy of **Buggos** (Steam, PC)
- **BepInEx 6 (IL2CPP, 64-bit)**. Get it from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases). Use the **Unity IL2CPP x64** build.
- The latest **Archipelago.Buggos** release zip from this repository's Releases page
- The [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) launcher, to generate a game and host a server (or a friend hosting for you)

You don't need to install any extra libraries or game files. The release zip already includes the Archipelago client library.

### Installing the mod
1. **Install BepInEx.** Extract the BepInEx zip into your Buggos game folder, so that `BepInEx/` and `winhttp.dll` sit next to `Buggos.exe`.
   (To find the folder in Steam: right-click Buggos → Manage → Browse local files.)
2. **Run the game once, then close it.** BepInEx uses this first launch to set itself up and generate its support files. This can take a minute or two, so wait for the game window to appear.
3. **Install the mod.** Extract the release zip into the `BepInEx/plugins/` folder. You should end up with something like:
```
   Buggos/
   ├─ Buggos.exe
   └─ BepInEx/
      └─ plugins/
         └─ Archipelago.Buggos/
            ├─ Archipelago.Buggos.dll
            └─ Archipelago.MultiClient.Net.dll
```
4. **Run the game again.** This creates the mod's config file in `BepInEx/config/`.

### Setting up your game
1. Install the Buggos `.apworld` into your Archipelago launcher.
2. Make your YAML (see [Example YAML](#example-yaml)) and put it in your Archipelago `Players` folder.
3. Generate the game and host it, or give the YAML to whoever is hosting.

### Connecting
1. Close the game and open `BepInEx/config/<plugin GUID>.cfg` in a text editor.
2. Fill in the `[Archipelago]` section:
```ini
   [Archipelago]
   Server = archipelago.gg:38281
   SlotName = YourSlotName
   Password =
```
3. Start the game and

## Connecting to an Archipelago Server

You first need an Archipelago server running. Instructions for hosting one are on [archipelago.gg](https://archipelago.gg).

> **Note:** the in-game connection menu is not finished yet. For now, connect by editing the config file.

Edit `BepInEx/config/<your plugin GUID>.cfg`:

```ini
[Archipelago]
Server = localhost:38281
SlotName = Bug1
Password =
```

Then start the game and choose **New Game** or **Continue**. 

*Planned:* a mod config menu will appear when you press New Game or Continue, with fields for server, slot name and password. Leaving the server blank, or disabling Archipelago, will start the normal game.

### Save files

The mod keeps **one game save per slot** by swapping save files when you connect, and makes a backup first. This is controlled by the `[Saves]` section of the config.

## Known Issues

- Traps are untested.
- Location names are matched against the server on login. If the log reports fewer than 161 resolved locations, set `LocationClearFormat` and `LocationPointFormat` in the config.

## Building

Requires the game's interop assemblies (from `BepInEx/interop`) and the [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net) package.

## Credits

<!-- Your name, the Archipelago community, etc. -->
