using HarmonyLib;
using Multiscreen.Util;
using Logger = Multiscreen.Util.Logger;
using UI.Windows;
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
    }

}
