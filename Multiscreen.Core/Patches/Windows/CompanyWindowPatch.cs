using HarmonyLib;
using Multiscreen.Util;
using UI.CompanyWindow;


namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(CompanyWindow))]

public static class CompanyWindowPatch
{

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CompanyWindow), nameof(CompanyWindow.Awake))]
    private static bool Awake(CompanyWindow __instance)
    {
        Logger.LogTrace($"CompanyWindow.Awake() {__instance.name}");
        __instance.SetDisplay(true);
        return true;
    }
}