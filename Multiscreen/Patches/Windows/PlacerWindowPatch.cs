using HarmonyLib;
using Multiscreen.Util;
using UI.Placer;

namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(PlacerWindow))]
public static class PlacerWindowPatch
{
    public static PlacerWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlacerWindow), nameof(PlacerWindow.Start))]
    private static bool Start(PlacerWindow __instance)
    {
        _instance = __instance;
        Logger.LogTrace($"PlacerWindow.Start() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlacerWindow), nameof(PlacerWindow.instance), MethodType.Getter)]
    private static bool Instance_Get(PlacerWindow __instance, ref PlacerWindow __result)
    {
        __result = _instance;
        return false;
    }
}