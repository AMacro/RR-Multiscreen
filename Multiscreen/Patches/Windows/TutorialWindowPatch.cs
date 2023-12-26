using HarmonyLib;
using Multiscreen.Utils;
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
        _instance = __instance;
        Multiscreen.Log($"TutorialWindow.Awake() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TutorialWindow), nameof(TutorialWindow.Shared), MethodType.Getter)]
    private static bool Instance_Get(TutorialWindow __instance, ref TutorialWindow __result)
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