// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Modified in 2026 for the Railroader 0.6 port; see NOTICE.

using UI.Windows;
using UnityEngine;

namespace Multiscreen.Util
{
    // Moves game windows between the game's window manager (main display) and the undock canvas (second display).
    //
    // Railroader 0.6 keeps every window as a direct child of WindowManager and finds them by walking those
    // children (WindowManager.GetWindow<T>, GetWindowByIdentifier, EnumerateWindows). A window parented to the
    // undock canvas is therefore invisible to the game unless the patches in TransformEnumeratorPatch and
    // WindowManagerEnumeratePatch put it back into those walks.
    public static class WindowUtils
    {
        // Cached by WindowManagerPatch (Awake/OnDestroy) so the hot enumerator patch only compares references.
        internal static Transform ManagerTransform;

        // False until the window lookup patch is applied and its startup self-test passes. While false nothing
        // moves to the second display (no auto-undock, no ALT+click), so no window can become unfindable.
        internal static bool UndockAvailable;

        public static Transform UndockRoot => Multiscreen.undockCanvas != null ? Multiscreen.undockCanvas.transform : null;

        public static Transform DockRoot
        {
            get
            {
                WindowManager manager = WindowManager.Shared;
                return manager != null ? manager.transform : null;
            }
        }

        public static bool IsUndocked(this Component targetWindow)
        {
            Transform undock = UndockRoot;
            return targetWindow != null && undock != null && targetWindow.transform.parent == undock;
        }

        public static void SetDisplay(this Component targetWindow, bool secondary)
        {
            if (targetWindow == null)
                return;

            if (secondary && !UndockAvailable)
            {
                Logger.LogWarningOnce("undock-off", "Multiscreen: undocking is disabled on this game version (see the warning at startup); windows stay on the main display");
                return;
            }

            Transform newParent = secondary ? UndockRoot : DockRoot;

            Logger.LogVerbose(() =>
            {
                Transform parent = targetWindow.transform.parent;
                return $"SetDisplay({targetWindow.name}, {secondary}) current parent: {(parent != null ? parent.name : "none")}, new parent: {(newParent != null ? newParent.name : "none")}";
            });

            if (newParent == null || targetWindow.transform.parent == newParent)
                return;

            if (secondary)
            {
                // The manager may predate this mod load (the game reloaded the mod mid-session)
                if (ManagerTransform == null)
                    ManagerTransform = DockRoot;

                float s = Multiscreen.settings.secondDisplayScale;
                targetWindow.transform.SetLossyScale(new Vector3(s, s, s));
            }
            else
            {
                targetWindow.transform.SetLossyScale(newParent.lossyScale);
            }

            // worldPositionStays: overlay canvases are in pixels, so the window keeps its spot and scale
            targetWindow.transform.SetParent(newParent, true);
            Logger.LogDebug($"SetDisplay({targetWindow.name}, {secondary}) moved to {newParent.name}");
        }

        public static void ToggleDisplay(this Component targetWindow)
        {
            if (targetWindow == null || targetWindow.transform.parent == null)
                return;

            Transform parent = targetWindow.transform.parent;
            Logger.LogDebug($"ToggleDisplay({targetWindow.name}) current parent: \"{parent.name}\"");

            if (parent == UndockRoot)
                targetWindow.SetDisplay(false);
            else if (parent == DockRoot)
                targetWindow.SetDisplay(true);
            else
                return;

            // ShowWindow on a shown window only clamps it to its new parent's bounds and orders it front
            Window win = targetWindow.GetComponent<Window>();
            if (win != null && win.IsShown)
                win.ShowWindow();
        }

        public static void UpdateScale(float scale)
        {
            Transform undock = UndockRoot;
            if (undock == null)
                return;

            for (int i = 0; i < undock.childCount; i++)
            {
                Transform child = undock.GetChild(i);
                if (child.GetComponent<Window>() != null)
                    child.SetLossyScale(new Vector3(scale, scale, scale));
            }
        }

        // Puts every undocked window back under the window manager, or destroys it when the manager is gone
        // (the game UI scene unloaded and the window would otherwise outlive it on our persistent canvas).
        public static void ReleaseUndocked(bool managerDying = false)
        {
            Transform undock = UndockRoot;
            if (undock == null || undock.childCount == 0)
                return;

            Transform dock = managerDying ? null : DockRoot;
            Logger.LogDebug($"ReleaseUndocked: {undock.childCount} window(s), {(dock != null ? "docking" : "destroying")}");

            for (int i = undock.childCount - 1; i >= 0; i--)
            {
                Transform child = undock.GetChild(i);
                if (dock != null)
                    child.SetDisplay(false);
                else
                    Object.Destroy(child.gameObject);
            }

            // Destroy is deferred to the end of the frame; detach now so nothing walks a dying window
            if (dock == null)
                undock.DetachChildren();
        }

        public static void SetLossyScale(this Transform targetTransform, Vector3 lossyScale)
        {
            Vector3 current = targetTransform.lossyScale;
            if (Mathf.Approximately(current.x, 0f) || Mathf.Approximately(current.y, 0f) || Mathf.Approximately(current.z, 0f))
                return;

            Vector3 local = targetTransform.localScale;
            targetTransform.localScale = new Vector3(local.x * (lossyScale.x / current.x),
                                                     local.y * (lossyScale.y / current.y),
                                                     local.z * (lossyScale.z / current.z));
        }
    }

    // The mouse position relative to the display it is on: (x, y, display index).
    public static class MouseUtil
    {
        // Display.RelativeMouseAt is a native call (a cursor query that may round-trip to the window system
        // under Wine) and uGUI asks it once per raycaster per pointer update; answer repeats from a one-entry
        // cache keyed on the frame and the position.
        private static int _cachedFrame = -1;
        private static Vector2 _cachedPosition;
        private static Vector3 _cachedResult;

        public static Vector3 RelativeMouseAt(Vector2 position)
        {
            int frame = Time.frameCount;
            if (frame == _cachedFrame && position == _cachedPosition)
                return _cachedResult;

            _cachedResult = Display.RelativeMouseAt(position);
            _cachedPosition = position;
            _cachedFrame = frame;
            return _cachedResult;
        }

        public static Vector3 RelativeMouse()
        {
            Vector3 raw = Input.mousePosition;
            Vector3 rel = RelativeMouseAt(raw);

            // RelativeMouseAt returns zero where multi-display isn't supported; fall back to the raw position
            if (rel == Vector3.zero)
                return new Vector3(raw.x, raw.y, 0f);

            if (rel.z < 0f)
                rel.z = 0f;

            return rel;
        }

        public static float DisplayWidth(int displayIndex) =>
            displayIndex > 0 && displayIndex < Display.displays.Length ? Display.displays[displayIndex].renderingWidth : Screen.width;

        public static float DisplayHeight(int displayIndex) =>
            displayIndex > 0 && displayIndex < Display.displays.Length ? Display.displays[displayIndex].renderingHeight : Screen.height;
    }
}
