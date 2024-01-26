using HarmonyLib;
using Multiscreen.Util;
using UnityEngine;
using UI;
using UI.Common;

using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Multiscreen.Patches.Misc;

[HarmonyPatch(typeof(Window))]
public class WindowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Window), nameof(Window.OnPointerDown))]
    private static void OnPointerDown(Window __instance, PointerEventData eventData)
    {
        //force Input to be refreshed
        Keyboard.current.altKey.ReadValue();

        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            __instance.ToggleDisplay();
        }

    }

}
