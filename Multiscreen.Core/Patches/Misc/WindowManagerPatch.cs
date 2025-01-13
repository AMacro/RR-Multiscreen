using HarmonyLib;
using UnityEngine;
using UI.Common;
using Logger = Multiscreen.Util.Logger;
using Multiscreen.Util;
using System.Linq;

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

        // if we're not on the second display then hand back to the game's code
        if (mousePosition.z != Multiscreen.targetDisplay)
            return true;

        var undockParent = GameObject.Find(Multiscreen.UNDOCK);
        if (undockParent == null)
            return true;

        // Get all visible windows sorted by Z-order (top to bottom)
        var windows = undockParent.GetComponentsInChildren<Window>()
            .Where(w => w != null && w.IsShown)
            .OrderByDescending(w => w.transform.GetSiblingIndex());

        foreach (var window in windows)
        {
            var rectTransform = window.RectTransform;
            var point = rectTransform.InverseTransformPoint(mousePosition);

            if (rectTransform.rect.Contains(point))
            {
                Logger.LogVerbose($"Hit Test({mousePosition}) - FOUND {window?.name}");
                __result = window;
                return false;
            }
        }

        return true;
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(WindowManager), nameof(WindowManager.CloseAllWindows))]
    private static void CloseAllWindows(WindowManager __instance)
    {
        Logger.LogDebug($"Closing All Windows");
        GameObject undockParent = GameObject.Find(Multiscreen.UNDOCK);

        if (undockParent == null)
            return;

        //close all undocked windows
        for (int i = 0; i < undockParent.transform.childCount; i++)
        {
            Window window = undockParent.transform.GetChild(i).GetComponent<Window>();
            if (window != null && window.IsShown)
            {
                window.SetDisplay(false);
                //window.CloseWindow();                
            }
        }

        Logger.LogDebug($"End Closing All Windows");
    }
}