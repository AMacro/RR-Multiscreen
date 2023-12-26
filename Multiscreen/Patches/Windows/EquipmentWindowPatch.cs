using HarmonyLib;
using Multiscreen.Utils;
using UI.Equipment;

namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(EquipmentWindow))]

public static class EquipmentWindowPatch
{
    public static EquipmentWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EquipmentWindow), nameof(EquipmentWindow.Awake))]
    private static bool Awake(EquipmentWindow __instance)
    {
        _instance = __instance;
        Multiscreen.Log($"EquipmentWindow.Awake() {__instance.name}");
        //__instance.SetDisplay(true);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EquipmentWindow), nameof(EquipmentWindow.Shared), MethodType.Getter)]
    private static bool Instance_Get(EquipmentWindow __instance, ref EquipmentWindow __result)
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