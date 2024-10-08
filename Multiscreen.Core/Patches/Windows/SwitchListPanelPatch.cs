using HarmonyLib;
using Multiscreen.Util;
using UI.SwitchList;

namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(SwitchListPanel))]

public static class SwitchListPanelPatch
{
    public static SwitchListPanel _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SwitchListPanel), nameof(SwitchListPanel.Start))]
    private static bool Awake(SwitchListPanel __instance)
    {
        _instance = __instance;
        Logger.LogTrace($"SwitchListPanel.Start() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SwitchListPanel), nameof(SwitchListPanel.Shared), MethodType.Getter)]
    private static bool Instance_Get(SwitchListPanel __instance, ref SwitchListPanel __result)
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