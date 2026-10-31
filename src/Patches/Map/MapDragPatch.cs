// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Modified in 2026 for the Railroader 0.6 port; see NOTICE.

using HarmonyLib;
using Multiscreen.Util;
using UI.Map;
using UnityEngine;

namespace Multiscreen.Patches.Map;

// Map zoom and teleport use the mouse position inside the map; make it relative to the map's own display.
[HarmonyPatch(typeof(MapDrag))]
public static class MapDragPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("NormalizedMousePosition")]
    private static bool NormalizedMousePosition(RectTransform ____rectTransform, ref Vector2 __result)
    {
        if (!Multiscreen.IsActive || ____rectTransform == null)
            return true;

        Vector3 mouse = MouseUtil.RelativeMouse();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(____rectTransform, mouse, null, out Vector2 local);
        Rect rect = ____rectTransform.rect;

        __result = new Vector2((local.x - rect.x) / rect.width, (local.y - rect.y) / rect.height);
        return false;
    }
}
