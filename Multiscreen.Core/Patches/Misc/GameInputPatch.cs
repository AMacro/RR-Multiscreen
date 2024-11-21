using HarmonyLib;
using UnityEngine;
using UI;
using UI.Common;



namespace Multiscreen.Patches.Misc;

[HarmonyPatch]
public class GameInputPatch
{

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.IsMouseOverGameWindow))]
    private static bool IsMouseOverGameWindow(GameInput __instance, ref bool __result, Window window)
    {
        //Multidisplay aware mousePosition
        Vector3 vector = Display.RelativeMouseAt(Input.mousePosition);

        bool bounds = 0f <= vector.x && 0f <= vector.y && Screen.width >= vector.x && Screen.height >= vector.y;
        Window hit = WindowManager.Shared.HitTest(vector);

        //Multiscreen.Log($"IsMouseOverGameWindow({window?.name}): __result: {__result}\r\n\tBounds:{bounds}\r\n\thit: {hit?.name} :: {hit == window}");

        __result = bounds && hit == window;

        return false;
    }

}
