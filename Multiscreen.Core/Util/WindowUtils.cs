using System.Collections.Generic;
using TMPro;
using UI.Builder;
using UI;
using UI.Common;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;

namespace Multiscreen.Util;

public static class WindowUtils
{
    // Window to Display Map
    private static readonly Dictionary<Window, int> WindowDisplayMap = [];
    private static readonly Dictionary<Window, TMP_Dropdown> WindowSelectorMap = [];

    // Queue of windows waiting for initialisation
    private static readonly Queue<(Window window, int display)> pendingWindowMoves = [];

    public static void SetDisplay(this Component targetWindow, int displayIndex)
    {
        if (targetWindow == null || targetWindow.gameObject == null)
            return;

        var window = targetWindow.GetComponent<Window>();
        if (window == null)
        {
            Logger.LogInfo($"SetDisplay: {targetWindow?.name} does not contain a Window component");
            return;
        }

        if (!DisplayUtils.Initialised)
        {
            Logger.LogDebug($"SetDisplay: Displays not initialized yet, queueing {window?.name} for display {displayIndex}");
            pendingWindowMoves.Enqueue((window, displayIndex));
            return;
        }

        // Update window tracking
        WindowDisplayMap[window] = displayIndex;

        Logger.LogDebug($"SetDisplay({targetWindow?.name}, {displayIndex})\r\n\tCurrent Transform: {targetWindow?.transform?.name}\r\n\tCurrent Transform Parent: {targetWindow?.transform?.parent?.name}");

        // Get display container from DisplayUtils
        var displayContainer = DisplayUtils.GetDisplayContainerFromIndex(displayIndex);
        if (displayContainer == null)
        {
            Logger.LogInfo($"SetDisplay({targetWindow?.name}, {displayIndex}) Unable to find display container");
            return;
        }

        // Get display settings for scaling
        var displaySettings = DisplayUtils.GetDisplaySettings(displayIndex);
        var scale = displaySettings.Scale;

        //update scaling for new display
        targetWindow.transform.SetLossyScale(scale);

        Logger.LogDebug($"SetDisplay({targetWindow?.name}, {displayIndex}) New parent: {displayContainer.name}");
        targetWindow.transform.SetParent(displayContainer.transform);

        // Update dropdown if it exists
        if (WindowSelectorMap.TryGetValue(window, out var dropdown))
        {
            dropdown.value = displayIndex;
            dropdown.RefreshShownValue();
        }
    }

    public static void UpdateScale(int display, float scale)
    {
        if (display == 0)
            return;

        CleanupDestroyedWindows();

        foreach (var windowEntry in WindowDisplayMap.Where(w => w.Value == display))
        {
            var window = windowEntry.Key;
            window?.transform.SetLossyScale(scale);
        }
    }

    public static void SetLossyScale(this Transform targetTransform, float lossyScale)
    {
        targetTransform.localScale = new Vector3(targetTransform.localScale.x * (lossyScale / targetTransform.lossyScale.x),
                                                 targetTransform.localScale.y * (lossyScale / targetTransform.lossyScale.y),
                                                 targetTransform.localScale.z * (lossyScale / targetTransform.lossyScale.z));
    }

    public static int GetDisplayForWindow(this Window window)
    {
        if (window == null)
            return 0;

        //check fastpath first
        if (WindowDisplayMap.TryGetValue(window, out int display))
            return display;

        // Get parent container
        var container = window?.transform?.parent?.gameObject;
        display = DisplayUtils.GetDisplayIndexForContainer(container);

        return display;
    }

    public static void SetupWindow(this Window window)
    {
        TMP_Dropdown selector;

        if (window == null)
            return;

        //capture this window
        if (!WindowDisplayMap.TryGetValue(window, out _))
        {
            WindowDisplayMap[window] = window.GetDisplayForWindow();
        }

        //add a titlebar selector
        if (!WindowSelectorMap.TryGetValue(window, out _))
        {
            selector = CreateDropdown(window);
            Logger.LogDebug($"SetupWindow({window?.name}) selector is null {selector == null}");
            if (selector != null)
                WindowSelectorMap[window] = selector;
            else
                Logger.LogInfo($"Failed to create dropdown for window ({window?.name})");
        }
    }

    private static TMP_Dropdown CreateDropdown(Window window)
    {
        if (window == null)
            return null;

        //no point if there's only one display
        if (DisplayUtils.DisplayCount < 2)
            return null;

        //find the title bar
        GameObject tb = window?.transform?.Find("Chrome/Title Bar")?.gameObject;
        if (tb == null)
            return null;

        //create a container for our dropdown
        GameObject goSelector = new("Screen Selector");
        RectTransform rtSelector = goSelector.AddComponent<RectTransform>();
        goSelector.transform.SetParent(tb.transform, false);

        //position dropdown
        rtSelector.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, -2, 20);
        rtSelector.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 10, 40);

        //Create a UI panel and build the dropdown inside it
        UIPanel.Create(rtSelector, GameObject.FindObjectOfType<ProgrammaticWindowCreator>().builderAssets, delegate (UIPanelBuilder builder)
        {
            Logger.LogDebug($"Getting current display for window \'{window.name}\' selector...");
            int currentDisp = window.GetDisplayForWindow();
            Logger.LogDebug($"Display found: {currentDisp}");
            RectTransform dropdown = null;

            // Get display names/numbers from active displays
            List<string> displayOptions = [];
            for (int i = 0; i < DisplayUtils.DisplayCount; i++)
                displayOptions.Add(i.ToString());

            dropdown = builder.AddDropdown(displayOptions, currentDisp, delegate (int index)
            {
                Logger.LogInfo($"Window {window.name} selected index: {index}");
                window.SetDisplay(index);
                window.ClampToParentBounds();
            });

            //Disable the background image, looks nicer on the title bar
            dropdown.Find("Background Image").gameObject.SetActive(false);
            //dropdown.Find("Arrow").gameObject.SetActive(false);

        });

        return tb.GetComponentInChildren<TMP_Dropdown>();
    }

    public static void ReturnAllWindowsToMain()
    {
        Logger.LogDebug("Moving all windows to main display");

        var windowsToMove = GetAllWindows().ToList();  // Create a copy of the keys


        // Use our existing window tracking
        foreach (var window in windowsToMove)
        {
            try
            {
                window?.SetDisplay(0);
            }
            catch (Exception ex)
            {
                Logger.LogInfo($"ReturnAllWindowsToMain() Error closing windows!:\r\n{ex.Message}");
            }
        }
    }

    public static IEnumerable<(Window window, int display)> GetWindowsOnDisplay(int displayIndex)
    {
        CleanupDestroyedWindows();

        return WindowDisplayMap
            .Where(kvp => kvp.Value == displayIndex)
            .Select(kvp => (kvp.Key, kvp.Value));
    }

    public static IEnumerable<Window> GetAllWindows()
    {
        CleanupDestroyedWindows();

        return WindowDisplayMap.Keys;
    }

    public static void CleanupDestroyedWindows()
    {
        // Clean up destroyed windows first
        var keysToRemove = WindowDisplayMap.Keys
            .Where(window => window == null || window.gameObject == null)
            .ToList();

        foreach (var key in keysToRemove)
        {
            WindowDisplayMap.Remove(key);
            if (WindowSelectorMap.ContainsKey(key))
                WindowSelectorMap.Remove(key);
        }
    }

    public static void ProcessQueuedWindows()
    {
        Logger.LogInfo($"Processing {pendingWindowMoves.Count} queued windows");
        while (pendingWindowMoves.Count > 0)
        {
            var (window, display) = pendingWindowMoves.Dequeue();
            window?.SetDisplay(display);
        }
    }

    /// <summary>
    /// Gets the settings for a window
    /// </summary>
    /// <param name="windowType">The type name of the window to retrieve the settings for</param>
    /// <param name="windowSettings">The settings defined for the window</param>
    /// <returns>True if the settings were found</returns>
    public static bool GetWindowSettings(string windowType, out WindowSettings windowSettings)
    {
        windowSettings = Multiscreen.settings.Windows.FirstOrDefault(w => w.WindowName == windowType);
         
        if (windowSettings == null)
            Logger.LogDebug($"GetStartupPosition() No settings found for {windowType}");

        return windowSettings != null;
    }

    public static void ApplyStartupPosition(Window window, string windowType)
    {
        Multiscreen.CoroutineRunner.StartCoroutine(ApplyStartupPositionCoroutine(window, windowType));
    }
    
    private static IEnumerator ApplyStartupPositionCoroutine(Window window, string windowType)
    {
        var name = windowType.Split('.').Last().Replace("Window", "").Replace("Panel", "").SplitCamelCase();

        // Get user defined settings for the window
        if (!GetWindowSettings(name, out var windowSettings))
        {
            Logger.Log($"Settings not found for {windowType}");
            yield break;
        }

        Logger.LogDebug($"{windowType}.Postfix() Waiting for load...");
        yield return new WaitUntil(
            () => 
            {
                return DisplayUtils.Initialised
                    && window != null
                    && window?.gameObject != null
                    && window?.RectTransform != null
                    && window?.contentRectTransform != null;
            }
        );

        Logger.LogDebug($"{windowType}.Postfix() Loaded");

        if (!DisplayUtils.Initialised || window == null || window?.RectTransform == null || window?.gameObject == null) 
        {
            Logger.LogDebug($"{windowType}.Postfix()  Waiting for load: DisplayUtils.Initialised: {DisplayUtils.Initialised}, window: {window != null}, RectTransform: {window?.RectTransform != null}, gameObject: {window?.gameObject != null}");
            yield break;
        }

        Logger.LogDebug($"{windowType}.Postfix() Settings: DeviceId: {windowSettings.DeviceId}, Position: {windowSettings.Position}, Size: {windowSettings.Size}, Position Mode: {windowSettings.PositionMode}, Size Mode: {windowSettings.SizeMode}");

        // Check if display nominated is active and return index
        if (!DisplayUtils.GetActiveDisplayFromDeviceId(windowSettings.DeviceId, out int displayIndex) || displayIndex < 0)
            displayIndex = 0;


        // Get scaling factor and screen size
        var scale = DisplayUtils.GetDisplaySettings(displayIndex).Scale;
        var screenSize = DisplayUtils.GetWindowsMonitorForUnityDisplay(displayIndex).Bounds;

        Logger.LogDebug($"{windowType}.Postfix() Index: {displayIndex}, scale: {scale}, screenSize: {screenSize.Width}x{screenSize.Height}");

        // Move window to correct display
        if (displayIndex > 0)
            window.SetDisplay(displayIndex);

        Logger.LogDebug($"{windowType}.Postfix() IsResizable: {window._resizable}");

        // Handle sizing
        if (window._resizable && windowSettings.SizeMode != WindowSettings.Sizing.Default)
        {
            var windowRectTransform = window.RectTransform; //overall window
            var contentRectTransform = window.contentRectTransform; //content only
            Vector2 currentContentSize = contentRectTransform.rect.size - windowRectTransform.rect.size;

            var minSize = window.resizer.minSize;
            var maxSize = window.resizer.maxSize;

            Vector2Int newContentSize = new Vector2Int
            (
                (int)Mathf.Clamp(windowSettings.Size.x, minSize.x, maxSize.x),
                (int)Mathf.Clamp(windowSettings.Size.y, minSize.y, maxSize.y)
            );

            Vector2 newWindowSize = newContentSize - currentContentSize;

            Logger.LogDebug($"{windowType}.Postfix() currentContentSize: {currentContentSize}, minSize:{minSize}, maxSize: {maxSize}, newContentSize: {newContentSize}, curentWindowSize: {windowRectTransform.rect.size}, newWindowSize: {newWindowSize}");

            newWindowSize.x = Mathf.Min(newWindowSize.x, (float)screenSize.Width / scale);
            newWindowSize.y = Mathf.Min(newWindowSize.y, (float)screenSize.Height / scale);

            Logger.LogDebug($"{windowType}.Postfix() scaledWindowSize: {newWindowSize}");

            windowRectTransform.sizeDelta = newWindowSize;
        }

        //todo: handle different position modes

        //calculate scaled position
        var scaledPosition = new Vector2(
            windowSettings.Position.x * (float)screenSize.Width * scale,
            windowSettings.Position.y * (float)screenSize.Height * scale
            );

        window.SetPositionRestoring(scaledPosition);
    }
}
