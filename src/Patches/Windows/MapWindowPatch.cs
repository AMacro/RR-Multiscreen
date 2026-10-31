using HarmonyLib;
using Multiscreen.Util;
using UI.Map;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Windows;

// The map opens on the second display
[HarmonyPatch(typeof(MapWindow))]
public static class MapWindowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    private static void Start(MapWindow __instance)
    {
        Logger.LogTrace($"MapWindow.Start() {__instance.name}");
        __instance.SetDisplay(true);
    }
}
