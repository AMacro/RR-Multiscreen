using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Multiscreen.Patches.Misc;

[HarmonyPatch(typeof(MultipleDisplayUtilities))]
public class MultipleDisplayUtilitiesPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MultipleDisplayUtilities), nameof(MultipleDisplayUtilities.RelativeMouseAtScaled))]
    private static bool RelativeMouseAtScaled(ref Vector3 __result, Vector2 position, ref int displayIndex)
    {
        var result = Display.RelativeMouseAt(position);

        if (result.z == 0)
        return true;

        __result = result;
        return false;

    }
}
