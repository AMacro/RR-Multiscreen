
using HarmonyLib;
using Multiscreen.Util;
using UI.CarInspector;

namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(CarInspector))]

public static class CarInspectorPatch
{
    public static CarInspector _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CarInspector), nameof(CarInspector.Awake))]
    private static bool Awake(CarInspector __instance)
    {
        _instance = __instance;
        Logger.LogTrace($"CarInspector.Awake() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }
}