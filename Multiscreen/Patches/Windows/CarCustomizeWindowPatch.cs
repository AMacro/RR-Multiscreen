using HarmonyLib;
using Multiscreen.Utils;
using UI.CarCustomizeWindow;


namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(CarCustomizeWindow))]

public static class CarCustomizeWindowPatch
{
    public static CarCustomizeWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CarCustomizeWindow), nameof(CarCustomizeWindow.Awake))]
    private static bool Awake(CarCustomizeWindow __instance)
    {
        _instance = __instance;
        Multiscreen.Log($"CarCustomizeWindow.Awake() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CarCustomizeWindow), nameof(CarCustomizeWindow.Instance), MethodType.Getter)]
    private static bool Instance_Get(CarCustomizeWindow __instance, ref CarCustomizeWindow __result)
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