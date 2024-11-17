using HarmonyLib;
using Multiscreen.Util;
using UI.Tutorial;

namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(TutorialWindow))]
public static class TutorialWindowPatch
{
    public static TutorialWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TutorialWindow), nameof(TutorialWindow.Awake))]
    private static bool Awake(TutorialWindow __instance)
    {
        Logger.LogTrace($"TutorialWindow.Awake() {__instance.name}"); 

        _instance = __instance;
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPatch(typeof(TutorialWindow), nameof(TutorialWindow.Shared), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool Instance_Get(TutorialWindow __instance, ref TutorialWindow __result)
    {
        Logger.LogDebug($"TutorialWindow.Instance_Get()");
        __result = _instance;
        return false;
    }

}