using HarmonyLib;
using Multiscreen.Utils;
using UI.CompanyWindow;


namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(CompanyWindow))]

public static class CompanyWindowPatch
{
    public static CompanyWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CompanyWindow), nameof(CompanyWindow.Awake))]
    private static bool Awake(CompanyWindow __instance)
    {
        _instance = __instance;
        Multiscreen.Log($"CompanyWindow.Awake() {__instance.name}");
        __instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CompanyWindow), nameof(CompanyWindow.Shared), MethodType.Getter)]
    private static bool Instance_Get(CompanyWindow __instance, ref CompanyWindow __result)
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