using HarmonyLib;
using Multiscreen.Util;
using UI.CarEditor;


namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(CarEditorWindow))]

public static class CarEditorWindowPatch
{
    public static CarEditorWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CarEditorWindow), nameof(CarEditorWindow.Awake))]
    private static bool Awake(CarEditorWindow __instance)
    {
        _instance = __instance;
        Logger.LogTrace($"CarEditorWindow.Awake() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CarEditorWindow), nameof(CarEditorWindow.Instance), MethodType.Getter)]
    private static bool Instance_Get(CarEditorWindow __instance, ref CarEditorWindow __result)
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