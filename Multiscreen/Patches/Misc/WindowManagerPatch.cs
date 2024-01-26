using HarmonyLib;
using UnityEngine;
using UI.Common;
using Multiscreen.Util;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Misc;

[HarmonyPatch(typeof(WindowManager))]
public static class WindowManager_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WindowManager), nameof(WindowManager.Awake))]
    private static void Awake(WindowManager __instance)
    {
        Logger.LogTrace($"WindowManager.Awake(): {__instance?.transform?.name}");
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WindowManager), nameof(WindowManager.HitTest))]
    private static bool HitTest(WindowManager __instance, ref Window __result, Vector3 mousePosition)
    {
        Logger.LogVerbose($"Hit Test({mousePosition})");

        GameObject undockParent = GameObject.Find(Multiscreen.UNDOCK);

        if (undockParent == null)
            return true;

        for (int i = 0; i < undockParent.transform.childCount; i++)
        {
            Window window = undockParent.transform.GetChild(i).GetComponent<Window>();
            if (window != null && window.IsShown)
            {
                RectTransform component = window.GetComponent<RectTransform>();
                Vector3 point = component.InverseTransformPoint(mousePosition);
                if (component.rect.Contains(point) && mousePosition.z == Multiscreen.targetDisplay) //confirm mouse is on the same display as the canvas
                {
                    Logger.LogVerbose ($"Hit Test({mousePosition}) - FOUND {window?.name}");
                    __result = window;
                    return false;
                }
            }
        }

        return true;
    }

    /*
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WindowManager), nameof(WindowManager.CloseAllWindows))]
    private static void CloseAllWindows(WindowManager __instance)
    {
        Multiscreen.Log($"Closing Windows");
        GameObject undockParent = GameObject.Find(Multiscreen.UNDOCK);

        if (undockParent == null)
            return;

        //close all undocked windows
        for (int i = 0; i < undockParent.transform.childCount; i++)
        {
            Window window = undockParent.transform.GetChild(i).GetComponent<Window>();
            if (window != null && window.IsShown)
            {
                window.CloseWindow();                
            }
        }

        Multiscreen.Log($"End Closing Windows");
    }
    */
}




