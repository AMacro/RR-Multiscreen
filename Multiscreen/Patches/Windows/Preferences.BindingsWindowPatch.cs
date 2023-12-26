using HarmonyLib;
using Multiscreen.Utils;
using UI.PreferencesWindow;


namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(BindingsWindow))]

public static class BindingsWindowPatch
{
    public static BindingsWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BindingsWindow), nameof(BindingsWindow.Awake))]
    private static bool Awake(BindingsWindow __instance)
    {
        _instance = __instance;
        Multiscreen.Log($"PreferencesWindow.Awake() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BindingsWindow), nameof(BindingsWindow.Instance), MethodType.Getter)]
    private static bool Instance_Get(BindingsWindow __instance, ref BindingsWindow __result)
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