
# Railroader Multiscreen Mod

By **Macka** ([AMacro](https://github.com/AMacro/RR-Multiscreen)). Version 2.0 is a port to the Railroader 0.6 mod loader (game 0.6.2 and later); see [NOTICE](NOTICE). Versions 1.x, for the pre-0.6 game with Unity Mod Manager, remain on the original repository.

This mod expands Railroader from single to dual screen, allowing you to move game windows (map, company, station, car inspector, etc.) off the main game screen to a second screen. 

**Core Features**
* Move game windows between screens by ALT+Clicking the title bar of the window
* Scale the secondary display UI separately from the main game UI
* For setups with more than 2 monitors: select which screen is the main game screen and which screen is the secondary display screen


## Installation
Railroader 0.6 loads mods itself; Unity Mod Manager is not used (and should be removed from the game folder if it is still there from an older version).

1. In Railroader, open Preferences and turn on **Allow Unsafe DLL Mods** (Multiscreen is a DLL mod), then restart the game.
2. Download the latest Multiscreen release and unzip it into the game's `Mods` folder, so you have `Railroader/Mods/Multiscreen/` containing `Info.json`, `Multiscreen.dll` and `0Harmony.dll`.
3. Ensure your second screen is connected and working in extended mode (on Windows: Windows Key + P > Extend).
4. Start Railroader and enable Multiscreen in the main menu's Mods screen. It is an optional mod: it only changes your own screens, and other players in a multiplayer session don't need it.

If it is working, the second screen shows a sky background (or your solid colour) as soon as the game starts, and the map and company windows open there. Multiscreen logs to the game's `Player.log` (lines from the `macka/multiscreen` mod).

### Settings file
Settings are kept in `settings.json` in the mod's folder, written when you change them in game. You can also edit it with the game closed, for example to set `"DebugLogging": "Debug"` (Info, Debug, Trace or Verbose) when reporting a problem.

### Linux (Proton)
Multiscreen 1.x was used on Linux through Proton with a second monitor: the second screen is its own window, which Wine places on that monitor. Version 2.0 on game 0.6 (Unity 6) has not been confirmed there yet; reports with your Proton version and renderer (Direct3D 12 by default, or `-force-d3d11`) are welcome.

## Usage

### Swapping Game Windows Between Screens
By default, the map and company windows will open on the second screen. To move any window to the second screen or from the second screen to the main screen, hold ALT and click the title bar of the window.


### Lost Windows
If you try to drag a window from one screen to the other it will get "lost". This can also happen if your screens do not have the same resolution and scaling.

To bring the window back, hide it then show it again.
e.g. if you lose the map window press 'M' to hide the map and then press 'M' a second time to show the map again.


### UI Scaling
You can set the UI scaling on the secondary display separately from the main screen. You will find it in the game preferences on the 'Graphics' tab, under the game's own UI Scale, together with the Solid Background option.


### Choosing a Different Monitor for your Game and Second Screen
If your system has 3 or more monitors connected, you may find the default second screen is on the wrong monitor. You can choose a different monitor for the second screen.

1. Start Railroader.
2. Click the 'Multiscreen' button on the Main Menu.
3. Select your Game Display and Secondary Displays from the dropdowns.

Note: changing your secondary display requires a game restart (sorry, this is a limitation of UnityEngine and I can't do anything about it).

Note: Display 0 can only be used for the main game display and can not be used by the secondary display (sorry, this is a limitation of UnityEngine and I can't do anything about it). A manual work around on Windows is to change your Primary Monitor in the Windows Display settings.

4. Click Apply.

5. If the Secondary Display setting has changed, you will receive a warning to restart the game. Clicking 'Quit' only closes the game, you need to manually reopen Railroader; 'Later' keeps playing and the change applies on the next start.




## Building From Source

### Requirements
- The .NET SDK (6 or later; `dotnet build` works on Windows and Linux), or Visual Studio 2022
- [Railroader](https://store.steampowered.com/app/1683150/) 0.6.2 or later installed on the system

### Setup
1. Clone/download this repository to your computer
2. Create a copy of 'Directory.Build.targets.example' and name it 'Directory.Build.targets' (it is git-ignored)
3. Set ``InstallDir`` to your Railroader install folder (the one containing `Railroader_Data`)
4. Optional, Windows only: set ``Cert-Thumb`` and ``SignToolPath`` to code-sign Release builds; leave ``Cert-Thumb`` empty to skip signing

### Building
``dotnet build Multiscreen.csproj -c Release`` (or open Multiscreen.sln). The output is `bin/Release/Multiscreen.dll` and `bin/Release/0Harmony.dll`; a release is those two files plus `Info.json` in a `Multiscreen` folder.

### How it works on 0.6
Railroader 0.6 keeps every game window as a child of its `WindowManager` and finds windows by walking those children. Multiscreen moves a window to a canvas on the second display, so it also teaches those walks (`WindowManager.GetWindow<T>`, `GetWindowByIdentifier`, `EnumerateWindows`, `HitTest`) about the windows it holds. Every Harmony patch is applied on its own: if a future game version changes a target, that one feature is skipped with a warning in `Player.log` and the rest keep working.
