using HarmonyLib;
using Multiscreen.Util;
using UI.Map;
using UnityEngine;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(MapWindow))]
public static class MapWindowPatch
{
    public static MapWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapWindow), nameof(MapWindow.Start))]
    private static bool Start(MapWindow __instance)
    {
        _instance = __instance;
        Logger.LogTrace($"MapWindow.Start() {__instance.name}");
        __instance.SetDisplay(true);

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapWindow), nameof(MapWindow.instance), MethodType.Getter)]
    private static bool Instance_Get(MapWindow __instance, ref MapWindow __result)
    {
        __result = _instance;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapWindow), nameof(MapWindow.OnZoom))]
    private static void OnZoom(MapWindow __instance, float delta, Vector2 viewportNormalizedPoint)
    {
        Logger.LogVerbose($"MapWindow.OnZoom({delta}, {viewportNormalizedPoint})");

    }
    
}