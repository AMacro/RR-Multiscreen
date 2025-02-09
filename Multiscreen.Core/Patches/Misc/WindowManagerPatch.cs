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

        // Get display index from mouse position Z coordinate
        int displayIndex = (int)mousePosition.z;

        // Let game handle main display hit testing
        if (displayIndex == 0)
            return true;

        // Get display container
        var displayContainer = DisplayUtils.GetDisplayContainerFromIndex(displayIndex);
        if (displayContainer == null)
            return true;

        // Get all visible windows for the container sorted by Z-order (top to bottom)
        var windows = WindowUtils.GetWindowsOnDisplay(displayIndex)
            .Select(x => x.window)
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

        WindowUtils.ReturnAllWindowsToMain();

        Logger.LogDebug($"End Closing All Windows");
    }
}