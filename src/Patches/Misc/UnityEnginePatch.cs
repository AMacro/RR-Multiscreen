using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Multiscreen.Patches.Misc;

// uGUI's GraphicRaycaster asks MultipleDisplayUtilities which display a pointer is on. Use the engine's own
// multi-display lookup when it reports a secondary display, so clicks reach windows on the second screen.
// The class is internal to UnityEngine.UI; in Unity 6 the signature is RelativeMouseAtScaled(Vector2, int).
[HarmonyPatch]
public static class MultipleDisplayUtilitiesPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(AccessTools.TypeByName("UnityEngine.UI.MultipleDisplayUtilities"), "RelativeMouseAtScaled",
                           new[] { typeof(Vector2), typeof(int) });

    private static bool Prefix(ref Vector3 __result, Vector2 position)
    {
        Vector3 result = Display.RelativeMouseAt(position);

        if (result.z <= 0f)
            return true;

        __result = result;
        return false;
    }
}
