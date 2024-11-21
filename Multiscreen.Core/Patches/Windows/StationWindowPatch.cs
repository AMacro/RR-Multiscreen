using HarmonyLib;
using Multiscreen.Util;
using UI.StationWindow;

namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(StationWindow))]

public static class StationWindowPatch
{
    public static StationWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StationWindow), nameof(StationWindow.Awake))]
    private static bool Awake(StationWindow __instance)
    {
        _instance = __instance;
        Logger.LogTrace($"StationWindow.Awake() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StationWindow), nameof(StationWindow.Shared), MethodType.Getter)]
    private static bool Instance_Get(StationWindow __instance, ref StationWindow __result)
    {
        __result = _instance;
        return false;
    }

    /*
     * Not implemented in company window
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CompanyWindow), nameof(CompanyWindow.OnClick))]
    private static bool OnClick(CompanyWindow __instance)
    {
        Multiscreen.Log($"MapWindow.OnClick() Alt: {GameInput.IsAltDown}");

        if (!GameInput.IsAltDown)
            return true;



        return true;

    }
    */
}