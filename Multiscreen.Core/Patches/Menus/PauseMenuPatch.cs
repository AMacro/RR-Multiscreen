using HarmonyLib;
using Multiscreen.Util;
using Logger = Multiscreen.Util.Logger;
using UI.CarCustomizeWindow;
using UI.CarEditor;
using UI.CarInspector;
using UI.Common;
using UI.CompanyWindow;
using UI.Equipment;
using UI.Guide;
using UI.Map;
using UI.Placer;
using UI.PreferencesWindow;
using UI.StationWindow;
using UI.SwitchList;
using UI.Tutorial;
using UnityEngine;
using System;

namespace Multiscreen.Patches.Menus;

[HarmonyPatch(typeof(PauseMenu))]
public class PauseMenuPatch

{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu._Quit))]
    private static void _Quit(PauseMenu __instance)
    {
        Logger.LogDebug($"_Quit()");
        GameObject undockParent = GameObject.Find(Multiscreen.UNDOCK);

        if (undockParent == null)
            return;

        //close all undocked windows
        Logger.LogDebug($"_Quit() Child windows: {undockParent.transform.childCount}");
        int i = 0;
        try
        {
            while (undockParent.transform.childCount > 0 && i < undockParent.transform.childCount)
            {
                Window window = undockParent.transform.GetChild(i).GetComponent<Window>();
                if (window != null && window.IsShown)
                {
                    window.SetDisplay(false);
                    i = 0;
                }
                else
                {
                    i++;
                }
                Logger.LogDebug($"_Quit() Child windows: {undockParent.transform.childCount}, i: {i}");
            }

        }
        catch (Exception ex)
        {
            Logger.LogInfo($"_Quit() Error closing windows!:\r\n{ex.Message}");
        }

        if(undockParent.transform.childCount > 0)
            Logger.Log($"_Quit() All windows closed, but {undockParent.transform.childCount} remaining objects");

        Logger.LogDebug($"_Quit() All windows closed");

        //Logger.LogTrace("PauseMenu._Quit()");
        //CarCustomizeWindow.Instance?.SetDisplay(false);
        //CarEditorWindow.Instance?.SetDisplay(false);
        //CarInspector._instance?.SetDisplay(false);
        //Logger.LogDebug("PauseMenu._Quit() Car Inspector Window");
        //CompanyWindow.Shared?.SetDisplay(false);
        //EquipmentWindow.Shared?.SetDisplay(false);
        //GuideWindow.Instance?.SetDisplay(false);
        //Logger.LogDebug("PauseMenu._Quit() Guide Window");
        //MapWindow.instance.SetDisplay(false);
        //PlacerWindow.instance?.SetDisplay(false);
        //PreferencesWindow.Instance?.SetDisplay(false);
        //BindingsWindow.Instance?.SetDisplay(false);
        //StationWindow.Shared?.SetDisplay(false);
        //Logger.LogDebug("PauseMenu._Quit() Station Window");
        //SwitchListPanel.Shared?.SetDisplay(false);
        //Logger.LogDebug("PauseMenu._Quit() Switch List Window");
        //TutorialWindow.Shared?.SetDisplay(false);
        //UI.Console.Console._instance?.SetDisplay(false);
        //Logger.LogTrace("End PauseMenu._Quit()");
    }

}
