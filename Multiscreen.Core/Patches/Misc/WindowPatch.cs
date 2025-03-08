using HarmonyLib;
using Multiscreen.Util;
using UnityEngine;
using UI.Common;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Misc;

[HarmonyPatch(typeof(Window))]
public class WindowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Window), nameof(Window.OnPointerDown))]
    private static void OnPointerDown(Window __instance)
    {
        //force Input to be refreshed
        Keyboard.current.altKey.ReadValue();

        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {

            int currentDisplay = __instance.GetDisplayForWindow();
            int nextDisplay = (currentDisplay + 1) % DisplayUtils.DisplayCount;

            __instance.SetDisplay(nextDisplay);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Window), nameof(Window.Start))]
    private static void Start(Window __instance) 
    {
        __instance.SetupWindow();
    }

}
