using HarmonyLib;
using Multiscreen.Util;
using UI.CarCustomizeWindow;
using UI.CarEditor;
using UI.CarInspector;
using UI.CompanyWindow;
using UI.Equipment;
using UI.Guide;
using UI.Map;
using UI.Placer;
using UI.PreferencesWindow;
using UI.StationWindow;
using UI.SwitchList;
using UI.Tutorial;

namespace Multiscreen.Patches.Menus;

[HarmonyPatch(typeof(PauseMenu))]
public class PauseMenuPatch

{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu._Quit))]
    private static void _Quit(PauseMenu __instance)
    {
        
        Logger.LogTrace("PauseMenu._Quit()");
        CarCustomizeWindow.Instance?.SetDisplay(false);
        CarEditorWindow.Instance?.SetDisplay(false);
        CarInspector._instance?.SetDisplay(false);
        Logger.LogDebug("PauseMenu._Quit() Car Inspector Window");
        CompanyWindow.Shared?.SetDisplay(false);
        EquipmentWindow.Shared?.SetDisplay(false);
        GuideWindow.Instance?.SetDisplay(false);
        Logger.LogDebug("PauseMenu._Quit() Guide Window");
        MapWindow.instance.SetDisplay(false);
        PlacerWindow.instance?.SetDisplay(false);
        PreferencesWindow.Instance?.SetDisplay(false);
        BindingsWindow.Instance?.SetDisplay(false);
        StationWindow.Shared?.SetDisplay(false);
        Logger.LogDebug("PauseMenu._Quit() Station Window");
        SwitchListPanel.Shared?.SetDisplay(false);
        Logger.LogDebug("PauseMenu._Quit() Switch List Window");
        TutorialWindow.Shared?.SetDisplay(false);
        UI.Console.Console._instance?.SetDisplay(false);
        Logger.LogTrace("End PauseMenu._Quit()");
    }

}
