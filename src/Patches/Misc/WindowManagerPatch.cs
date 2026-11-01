// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Modified in 2026 for the Railroader 0.6 port; see NOTICE.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Multiscreen.Util;
using UI.Windows;
using UnityEngine;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Misc;

[HarmonyPatch(typeof(WindowManager))]
public static class WindowManagerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("Awake")]
    private static void Awake(WindowManager __instance)
    {
        if (WindowManager.Shared == __instance)
            WindowUtils.ManagerTransform = __instance.transform;
        Logger.LogTrace($"WindowManager.Awake(): {__instance.transform.name}");
    }

    // Every way out of a game (pause-menu quit, disconnect, loading another save, editor) calls this first,
    // while the manager is still alive: dock the undocked windows so they die with the game UI scene like
    // vanilla windows do. The pause-menu prefix and OnDestroy below stay as fallbacks.
    [HarmonyPrefix]
    [HarmonyPatch(nameof(WindowManager.DestroyAllCustomWindows))]
    private static void DestroyAllCustomWindows(WindowManager __instance)
    {
        if (WindowManager.Shared != __instance)
            return;

        Logger.LogDebug("WindowManager.DestroyAllCustomWindows(): docking undocked windows");
        WindowUtils.ReleaseUndocked();
    }

    // The game UI scene is unloading: our undocked windows belong to this manager, so they go with it.
    [HarmonyPrefix]
    [HarmonyPatch("OnDestroy")]
    private static void OnDestroy(WindowManager __instance)
    {
        if (WindowManager.Shared != __instance)
            return;

        Logger.LogDebug("WindowManager.OnDestroy(): releasing undocked windows");
        WindowUtils.ReleaseUndocked(managerDying: true);
        WindowUtils.ManagerTransform = null;
    }

    // Vanilla tests the main display's windows against a main-display mouse position. GameInputPatch passes a
    // display-relative position whose z is the display index; test only the windows on that display.
    [HarmonyPrefix]
    [HarmonyPatch(nameof(WindowManager.HitTest))]
    private static bool HitTest(WindowManager __instance, ref Window __result, Vector3 mousePosition)
    {
        Transform undock = WindowUtils.UndockRoot;
        if (undock == null)
            return true;

        Transform root = (int)mousePosition.z == Multiscreen.targetDisplay ? undock : __instance.transform;
        Vector3 point2D = new Vector3(mousePosition.x, mousePosition.y, 0f);

        // Last sibling is drawn on top, so walk from the end
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Window window = root.GetChild(i).GetComponent<Window>();
            if (window == null || !window.IsShown)
                continue;

            RectTransform rectTransform = window.GetComponent<RectTransform>();
            if (rectTransform.rect.Contains(rectTransform.InverseTransformPoint(point2D)))
            {
                Logger.LogVerbose($"HitTest({mousePosition}) found {window.name}");
                __result = window;
                return false;
            }
        }

        __result = null;
        return false;
    }
}

// EnumerateWindows walks the manager's children with GetChild, so add the undocked windows after them.
// It backs CloseAllWindows (on enable) and the topmost-window lookup that Escape uses to close windows.
[HarmonyPatch]
public static class WindowManagerEnumeratePatch
{
    private static MethodBase TargetMethod() => AccessTools.Method(typeof(WindowManager), "EnumerateWindows");

    private static bool Prefix(WindowManager __instance, ref IEnumerable<Window> __result)
    {
        Transform undock = WindowUtils.UndockRoot;
        if (undock == null || undock.childCount == 0)
            return true;

        __result = Combined(__instance.transform, undock);
        return false;
    }

    private static IEnumerable<Window> Combined(Transform manager, Transform undock)
    {
        foreach (Transform root in new[] { manager, undock })
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Window window = root.GetChild(i).GetComponent<Window>();
                if (window != null)
                    yield return window;
            }
        }
    }
}

// WindowManager.GetWindow<T>() (behind every window's static Shared/Instance getter, including the generic
// BuilderWindowBase<T>.Shared) and GetWindowByIdentifier find windows with `foreach (Transform t in transform)`
// over the manager's direct children, and GetWindow<T> throws when it finds nothing. GetWindow<T> is generic,
// and on Mono reference-type instantiations share one compiled body, so a Harmony patch on it can't know T.
//
// Instead this extends the foreach itself: Transform's enumerator (always called through IEnumerator, so it is
// never inlined) yields the undocked windows after the manager's real children. Every other transform in the
// game takes the vanilla path after one reference comparison.
//
// MoveNext and Current only work as a pair, so they are applied together (TryApply), removed together, and
// proven by SelfTest through a real foreach before undocking is switched on.
public static class TransformEnumeratorPatch
{
    private static MethodInfo _moveNext;
    private static MethodInfo _current;

    private static bool IsManager(Transform outer, out Transform undock)
    {
        undock = null;
        Transform manager = WindowUtils.ManagerTransform;
        if ((object)manager == null || (object)outer != (object)manager)
            return false;

        undock = WindowUtils.UndockRoot;
        return (object)undock != null;
    }

    private static bool MoveNextPrefix(Transform ___outer, ref int ___currentIndex, ref bool __result)
    {
        if (!IsManager(___outer, out Transform undock))
            return true;

        int extra = undock.childCount;
        if (extra == 0)
            return true;

        ___currentIndex++;
        __result = ___currentIndex < ___outer.childCount + extra;
        return false;
    }

    private static bool CurrentPrefix(Transform ___outer, int ___currentIndex, ref object __result)
    {
        if (!IsManager(___outer, out Transform undock))
            return true;

        int own = ___outer.childCount;
        if (___currentIndex < own)
            return true;

        int index = ___currentIndex - own;
        if (index >= undock.childCount)
            return true;

        __result = undock.GetChild(index);
        return false;
    }

    // Both prefixes or neither
    internal static bool TryApply(Harmony harmony, out string why)
    {
        why = null;
        System.Type enumerator = AccessTools.Inner(typeof(Transform), "Enumerator");
        _moveNext = enumerator != null ? AccessTools.Method(enumerator, nameof(IEnumerator.MoveNext)) : null;
        _current = enumerator != null ? AccessTools.PropertyGetter(enumerator, nameof(IEnumerator.Current)) : null;
        if (_moveNext == null || _current == null)
        {
            why = "UnityEngine.Transform.Enumerator MoveNext/Current not found";
            return false;
        }

        try
        {
            harmony.Patch(_moveNext, prefix: new HarmonyMethod(AccessTools.Method(typeof(TransformEnumeratorPatch), nameof(MoveNextPrefix))));
            harmony.Patch(_current, prefix: new HarmonyMethod(AccessTools.Method(typeof(TransformEnumeratorPatch), nameof(CurrentPrefix))));
            return true;
        }
        catch (System.Exception ex)
        {
            why = (ex.InnerException ?? ex).Message;
            Remove(harmony);
            return false;
        }
    }

    internal static void Remove(Harmony harmony)
    {
        foreach (MethodInfo method in new[] { _moveNext, _current })
        {
            if (method == null)
                continue;
            try { harmony.Unpatch(method, HarmonyPatchType.Prefix, harmony.Id); }
            catch (System.Exception ex) { Logger.LogDebug($"Unpatch {method.Name}: {ex.Message}"); }
        }
    }

    // Stands in a temporary "manager" with one child and one temporary child on the undock canvas, then walks
    // both through real foreach loops: the manager walk must yield its own child first and the undock child
    // last, and an unrelated transform's walk must be exactly vanilla.
    internal static bool SelfTest(Transform undock, out string why)
    {
        why = null;
        Transform savedManager = WindowUtils.ManagerTransform;
        GameObject manager = null, other = null, probe = null;
        try
        {
            manager = new GameObject("Multiscreen.SelfTest.Manager");
            GameObject own = new GameObject("own");
            own.transform.SetParent(manager.transform, false);

            other = new GameObject("Multiscreen.SelfTest.Other");
            GameObject otherChild = new GameObject("child");
            otherChild.transform.SetParent(other.transform, false);

            probe = new GameObject("Multiscreen.SelfTest.Probe");
            probe.transform.SetParent(undock, false);

            WindowUtils.ManagerTransform = manager.transform;

            List<Transform> seen = new List<Transform>();
            foreach (Transform t in manager.transform)
                seen.Add(t);

            int expected = 1 + undock.childCount;
            if (seen.Count != expected || seen[0] != own.transform || seen[seen.Count - 1] != probe.transform)
            {
                why = $"manager walk yielded {seen.Count} item(s), expected {expected} ending with the undocked probe";
                return false;
            }

            List<Transform> vanilla = new List<Transform>();
            foreach (Transform t in other.transform)
                vanilla.Add(t);

            if (vanilla.Count != 1 || vanilla[0] != otherChild.transform)
            {
                why = $"an unrelated transform's walk yielded {vanilla.Count} item(s), expected exactly its 1 child";
                return false;
            }

            return true;
        }
        catch (System.Exception ex)
        {
            why = ex.Message;
            return false;
        }
        finally
        {
            WindowUtils.ManagerTransform = savedManager;
            if (probe != null) Object.DestroyImmediate(probe);
            if (manager != null) Object.DestroyImmediate(manager);
            if (other != null) Object.DestroyImmediate(other);
        }
    }
}
