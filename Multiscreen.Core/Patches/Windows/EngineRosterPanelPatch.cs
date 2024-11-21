using HarmonyLib;
using Multiscreen.Util;
using UI.EngineRoster;


namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(EngineRosterPanel))]

public static class EngineRosterPanelPatch
{
    public static EngineRosterPanel _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EngineRosterPanel), nameof(EngineRosterPanel.Awake))]
    private static bool Awake(EngineRosterPanel __instance)
    {
        _instance = __instance;
        Logger.LogTrace($"EngineRosterPanel.Awake() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EngineRosterPanel), nameof(EngineRosterPanel.Shared), MethodType.Getter)]
    private static bool Instance_Get(EngineRosterPanel __instance, ref EngineRosterPanel __result)
    {
        __result = _instance;
        return false;
    }

    /*
     * Not implemented in company window
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EngineRosterPanel), nameof(EngineRosterPanel.OnClick))]
    private static bool OnClick(EngineRosterPanel __instance)
    {
        Multiscreen.Log($"MapWindow.OnClick() Alt: {GameInput.IsAltDown}");

        if (!GameInput.IsAltDown)
            return true;



        return true;

    }
    */
}