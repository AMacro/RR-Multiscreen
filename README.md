
# Railroader Multiscreen Mod

This mod expands Railroader from single to dual screen, allowing you to move game windows (map, company, station, car inspector, etc.) off the main game screen to a second screen. 

**Core Features**
* Move game windows between screens by ALT+Clicking the title bar of the window
* Scale the secondary display UI separately from the main game UI
* For setups with more than 2 monitors: select which screen is the main game screen and which screen is the secondary display screen


## Installation
This mod requires Unity Mod Manager, if you are familiar with UMM you can download the [latest Multiscreen release](https://www.nexusmods.com/site/mods/21), otherwise see detailed instructions below.

1. Download the latest version of 'Multiscreen': https://www.nexusmods.com/railroader/mods/6

1. Download the latest version of Unity Mod Manager: https://www.nexusmods.com/site/mods/21 and unzip to a folder (e.g. your desktop)

1. Run 'UnityModManager.exe'

1. Select 'Railroader' from the dropdown list and click 'Install' (use the doorstop method)

1. Click on the 'Mods' tab

1. Either drag the 'Multiscreen' zip file (downloaded in step 1) into the UMM Installer window OR click 'Install Mod' and select the 'Multiscreen' zip file

1. Ensure your second screen is connected and working in Extend mode (Windows Key + P > Extend if it is in Duplicate mode).

1. Start Railroader.

The Unity Mod Manager window will appear, check the mod status:

- Green 'Active' status: you should be good to go, your second screen should have been detected and setup automatically and the second screen will show a blue sky and brown horizon background, if not, restart the game (this should only be necessary the first time you use the mod).

- Grey 'Inactive' status: you need to activate the mod - press the 'On/Off' square button next to the status light, then restart the game.

- Red 'Need restart' status: try restarting the game, if problems persist please post on the forum with your log file.

9. Start the game as normal (new game/load a saved game/join or host a multiplayer server).

10. Bonus step: once the Multiscreen mod is working, you can disable the UMM popup window in the UMM settings.

### Installing Updates
When starting Railroader you will be prompted if a new version of Multiscreen has been released.

1. Close Railroader if it is running
2. Open the Unity Mod Manager installer (run 'UnityModManager.exe') and go to the 'Mods' tab
3. Right click 'Multiscreen' and select update
Alternatively, the latest zip file can be downloaded from the Nexus Mods page and updated the same way as the original install.


## Usage

### Swapping Game Windows Between Screens
By default, the map and company windows will open on the second screen. To move any window to the second screen or from the second screen to the main screen, hold ALT and click the title bar of the window.


### Lost Windows
If you try to drag a window from one screen to the other it will get "lost". This can also happen if your screens do not have the same resolution and scaling.

To bring the window back, hide it then show it again.
e.g. if you lose the map window press 'M' to hide the map and then press 'M' a second time to show the map again.


### UI Scaling
As of version 1.2.0 you can now set the UI scaling on the secondary display. You will find the option in the game preferences on the 'Graphics' tab
Spoiler:  Show


### Choosing a Different Monitor for your Game and Second Screen
If your system has 3 or more monitors connected, you may find the default second screen is on the wrong monitor. As of version 1.0.2, you can choose a different monitor for the second screen.

1. Start Railroader.
2. Click the 'Multiscreen Mod' button on the Main Menu.
3. Select your Game Display and Secondary Displays from the dropdowns.

Note: changing your secondary display requires a game restart (sorry, this is a limitation of UnityEngine and I can't do anything about it).

Note: Display 0 can only be used for the main game display and can not be used by the secondary display (sorry, this is a limitation of UnityEngine and I can't do anything about it). A manual work around on Windows is to change your Primary Monitor in the Windows Display settings.

4. Click Apply.

5. If the Secondary Display setting has changed, you will receive a warning to restart the game. Clicking 'Quit' only closes the game, you need to manually reopen Railroader.




## Building From Source
This section assumes you have a basic knowledge of Microsoft Visual Studio and GitHub.

### Requirements
- [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/) (not Visual Studio Code)
- [Railroader](https://store.steampowered.com/app/1683150/) is installed on the system

### Setup
1. Clone/download this repository to your computer
2. Create a copy of 'Directory.Build.targets.example' and name it 'Directory.Build.targets'
3. Open 'Directory.Build.targets'
4. Update the ``InstallDir`` parameter to match your Railroader install path, default is:  
  ``<InstallDir>C:\Program Files (x86)\Steam\steamapps\common\Railroader</InstallDir>``

5. Update the ``Cert-Thum`` parameter[^1]
- If you do not have a code signing certificate/hardware key this should be left blank i.e.  
  ``<Cert-Thumb></Cert-Thumb>``
- If you do have a code signing certifcate/hardware key, enter certificate's thumbprint e.g.  
  ``<Cert-Thumb>2ce2b8a98a59ffd407ada2e94f233bf24a0e68b9</Cert-Thumb>``
6. Update the ``SignToolPath`` parameter
- If you do not have a code signing certificate/hardware key leave as-is.
- If you do have a code signing certifcate/hardware key, update as required.
7. Save and close 'Directory.Build.targets'

### Building
1. Open Multiscreen solution
2. Build your prefered target (Debug/Release)

Note: If the build config is set to 'Release' a zip archive containing the output dll and json files will be created in the solution's 'Release' folder
  
[^1]: ``Cert-Thum`` is used when building in 'Release' mode and, if specified, will run SignTool to codesign the compiled assembly