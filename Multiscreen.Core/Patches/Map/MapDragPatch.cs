using HarmonyLib;
using UI.Map;
using UnityEngine;

namespace Multiscreen.Patches.Map;

[HarmonyPatch(typeof(MapDrag))]

public static class MapDrag_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapDrag), nameof(MapDrag.NormalizedMousePosition))]
    private static bool NormalizedMousePosition(MapDrag __instance, ref Vector2 __result)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(__instance._rectTransform, Display.RelativeMouseAt(Input.mousePosition), null, out Vector2 vector);
        Rect rect = __instance._rectTransform.rect;

        __result = new Vector2((vector.x - rect.x) / rect.width, (vector.y - rect.y) / rect.height);

        return false;
    }
}