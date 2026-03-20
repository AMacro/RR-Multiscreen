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

        //Use the correct display dimensions based on which display the mouse is on
        int displayIndex = (int)vector.z;
        float displayWidth;
        float displayHeight;

        if (displayIndex > 0 && displayIndex < Display.displays.Length)
        {
            displayWidth = Display.displays[displayIndex].renderingWidth;
            displayHeight = Display.displays[displayIndex].renderingHeight;
        }
        else
        {
            displayWidth = Screen.width;
            displayHeight = Screen.height;
        }

        bool bounds = 0f <= vector.x && 0f <= vector.y && displayWidth >= vector.x && displayHeight >= vector.y;
        Window hit = WindowManager.Shared.HitTest(vector);

        //Multiscreen.Log($"IsMouseOverGameWindow({window?.name}): __result: {__result}\r\n\tBounds:{bounds}\r\n\thit: {hit?.name} :: {hit == window}");

        __result = bounds && hit == window;

        return false;
    }

}
