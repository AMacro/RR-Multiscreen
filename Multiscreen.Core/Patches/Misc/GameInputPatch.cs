using HarmonyLib;
using UnityEngine;
using UI;
using UI.Common;
using Logger = Multiscreen.Util.Logger;
using System;


namespace Multiscreen.Patches.Misc;

[HarmonyPatch(typeof(UI.GameInput))]
public class GameInputPatch
{

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.IsMouseOverGameWindow))]
    private static bool IsMouseOverGameWindow(ref bool __result, Window window)
    {
        //Multidisplay aware mousePosition
        Vector3 vector = Display.RelativeMouseAt(Input.mousePosition);

        bool bounds = 0f <= vector.x && 0f <= vector.y && Screen.width >= vector.x && Screen.height >= vector.y;
        Window hit = WindowManager.Shared.HitTest(vector);

        __result = bounds && hit == window;

        return false;
    }

}
