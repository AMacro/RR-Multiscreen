using HarmonyLib;
using Multiscreen.Utils;
using UI.Guide;


namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(GuideWindow))]

public static class GuideWindowPatch
{
    public static GuideWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuideWindow), nameof(GuideWindow.Awake))]
    private static bool Awake(GuideWindow __instance)
    {
        _instance = __instance;
        Multiscreen.Log($"GuideWindow.Awake() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuideWindow), nameof(GuideWindow.Instance), MethodType.Getter)]
    private static bool Instance_Get(GuideWindow __instance, ref GuideWindow __result)
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