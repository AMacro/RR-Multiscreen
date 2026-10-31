// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Modified in 2026 for the Railroader 0.6 port; see NOTICE.

using System;
using HarmonyLib;
using Multiscreen.Util;
using UI.Windows;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Multiscreen.Patches.Misc;

// ALT+click a window to move it to the other display
[HarmonyPatch(typeof(Window))]
public static class WindowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Window.OnPointerDown))]
    private static void OnPointerDown(Window __instance, PointerEventData eventData)
    {
        if (AltHeld())
            __instance.ToggleDisplay();
    }

    private static bool AltHeld()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed))
            return true;

        try
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }
        catch (InvalidOperationException)
        {
            return false; // legacy input disabled
        }
    }
}
