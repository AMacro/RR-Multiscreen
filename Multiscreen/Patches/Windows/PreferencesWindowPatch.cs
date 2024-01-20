using HarmonyLib;
using Multiscreen.Utils;
using UI.PreferencesWindow;


namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(PreferencesWindow))]

public static class PreferencesWindowPatch
{
    public static PreferencesWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PreferencesWindow), nameof(PreferencesWindow.Awake))]
    private static bool Awake(PreferencesWindow __instance)
    {
        _instance = __instance;
        Multiscreen.Log($"PreferencesWindow.Awake() {__instance.name}");
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PreferencesWindow), nameof(PreferencesWindow.Instance), MethodType.Getter)]
    private static bool Instance_Get(PreferencesWindow __instance, ref PreferencesWindow __result)
    {
        __result = _instance;
        return false;
    }

}