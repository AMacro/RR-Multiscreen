using HarmonyLib;
using Multiscreen.Util;
using UI.CompanyWindow;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Windows;

// The company window opens on the second display. In 0.6 it is created by the window manager at startup
// (CreateWindowWithComponent), and Awake runs inside AddComponent, before its position is restored.
[HarmonyPatch(typeof(CompanyWindow))]
public static class CompanyWindowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Awake")]
    private static void Awake(CompanyWindow __instance)
    {
        Logger.LogTrace($"CompanyWindow.Awake() {__instance.name}");
        __instance.SetDisplay(true);
    }
}
