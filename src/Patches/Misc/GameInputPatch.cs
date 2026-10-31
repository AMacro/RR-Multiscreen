// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Modified in 2026 for the Railroader 0.6 port; see NOTICE.

using HarmonyLib;
using Multiscreen.Util;
using UI;
using UI.Windows;
using UnityEngine;

namespace Multiscreen.Patches.Misc;

// Multi-display aware version of GameInput.IsMouseOverGameWindow(window). In 0.6.2 it is static and reads
// Mouse.current.position, which only knows the main window.
[HarmonyPatch(typeof(GameInput))]
public static class GameInputPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameInput.IsMouseOverGameWindow))]
    private static bool IsMouseOverGameWindow(ref bool __result, Window window)
    {
        if (!Multiscreen.IsActive || WindowManager.Shared == null)
            return true;

        Vector3 mouse = MouseUtil.RelativeMouse();
        int displayIndex = (int)mouse.z;

        // Per-display bounds; a portrait second display is taller than the main one (issue #8)
        float width = MouseUtil.DisplayWidth(displayIndex);
        float height = MouseUtil.DisplayHeight(displayIndex);
        if (mouse.x < 0f || mouse.y < 0f || mouse.x > width || mouse.y > height)
        {
            __result = false;
            return false;
        }

        if (UIStateArbiter.PhotoModeActive)
        {
            __result = window == null;
            return false;
        }

        // window == null asks "is the mouse over the 3D world?"; the second display has no world under it
        if (window == null && displayIndex == Multiscreen.targetDisplay)
        {
            __result = false;
            return false;
        }

        __result = WindowManager.Shared.HitTest(mouse) == window;
        return false;
    }
}
