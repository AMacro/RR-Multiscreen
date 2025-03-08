using System.Collections.Generic;
using TMPro;
using UI.Builder;
using UI;
using UI.Common;
using UnityEngine;
using System;
using System.Linq;

namespace Multiscreen.Util;

public static class WindowUtils
{
    private static readonly Dictionary<Window, int> WindowDisplayMap = [];
    private static readonly Dictionary<Window, TMP_Dropdown> WindowSelectorMap = [];
    private static readonly Queue<(Window window, int display)> pendingWindows = [];

    public static void SetDisplay(this Component targetWindow, int displayIndex)
    {
        if (targetWindow == null)
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
            pendingWindows.Enqueue((window, displayIndex));
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

        //check fastpath first
        if (WindowDisplayMap.TryGetValue(window, out int display))
            return display;

        // Get parent container
        var container = window.transform.parent.gameObject;
        display = DisplayUtils.GetDisplayIndexForContainer(container);

        return display;
    }

    public static void SetupWindow(this Window window)
    {
        TMP_Dropdown selector;

        //capture this window
        if (!WindowDisplayMap.TryGetValue(window, out _))
        {
            WindowDisplayMap[window] = window.GetDisplayForWindow();
        }

        //add a titlebar selector
        if (!WindowSelectorMap.TryGetValue(window, out _))
        {
            selector = CreateDropdown(window);
            Logger.LogDebug($"SetupWindow({window.name}) selector is null {selector == null}");
            if (selector != null)
                WindowSelectorMap[window] = selector;
            else
                Logger.LogInfo($"Failed to create dropdown for window ({window.name})");
        }
    }

    private static TMP_Dropdown CreateDropdown(Window window)
    {
        //no point if there's only one display
        if (DisplayUtils.DisplayCount < 2)
            return null;

        //find the title bar
        GameObject tb = window.transform.Find("Chrome/Title Bar").gameObject;
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

        var windowsToMove = WindowDisplayMap.Keys.ToList();

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
        return WindowDisplayMap
            .Where(kvp => kvp.Value == displayIndex)
            .Select(kvp => (kvp.Key, kvp.Value));
    }

    public static IEnumerable<Window> GetAllWindows()
    {
        return WindowDisplayMap.Keys;
    }

    public static void ProcessQueuedWindows()
    {
        Logger.LogInfo($"Processing {pendingWindows.Count} queued windows");
        while (pendingWindows.Count > 0)
        {
            var (window, display) = pendingWindows.Dequeue();
            window.SetDisplay(display);
        }
    }
}
