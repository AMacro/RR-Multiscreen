using HarmonyLib;
using Multiscreen.Util;
using System;
using UI.Windows;
using UnityEngine;
using Logger = Multiscreen.Util.Logger;

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
            while (undockParent.transform.childCount > 0)
            {
                Window window = undockParent.transform.GetChild(0).GetComponent<Window>();
                if (window != null)
                {
                    window.SetDisplay(false);
                }
                else
                {
                    GameObject.Destroy(undockParent.transform.GetChild(0).gameObject);
                }
                Logger.LogDebug($"_Quit() Child windows: {undockParent.transform.childCount}, i: {i}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"_Quit() Error closing windows!:\r\n{ex.Message}");
        }

        if (undockParent.transform.childCount > 0)
            Logger.Log($"_Quit() All windows closed, but {undockParent.transform.childCount} remaining objects");

        Logger.LogDebug($"_Quit() All windows closed");
    }

}
