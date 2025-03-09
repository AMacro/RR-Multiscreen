using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;
using Game;
using System.Text;
using static Multiscreen.DisplaySettings;

namespace Multiscreen.Util;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public class MonitorInfo
{
    public string Name { get; set; }            // This is the device name (e.g. \\.\DISPLAY1)
    public string FriendlyName { get; set; }    // User-friendly display name
    public string DeviceID { get; set; }        // Hardware identifier from EnumDisplayDevices
    public ulong Handle { get; set; }          // Handle to the display device (common across Windows and Unity)
    public bool IsPrimary { get; set; }         // Is system primary monitor
    public RECT Bounds { get; set; }            // Bounds of the display in screen coordinates, can be negative when arranged left or above the system primary monitor
    public RECT NormalisedBounds { get; set; }  // Bounds of the display in screen coordinates, normalised so all values are positive
}

public class ActiveDisplayInfo
{
    //public int LogicalIndex { get; set; }  // Current system display number
    public GameObject DockContainer { get; set; }
    public RawImage Background { get; set; }
    public DisplaySettings Settings { get; set; }
}

public static class DisplayUtils
{
    #region Native functions
    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [Flags]
    private enum DisplayDeviceStateFlags
    {
        NotAttached = 0x0,
        AttachedToDesktop = 0x1,
        PrimaryDevice = 0x4
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int Size;
        public RECT Monitor;
        public RECT WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)]
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        [MarshalAs(UnmanagedType.U4)]
        public DisplayDeviceStateFlags StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
    #endregion

    private const string UNDOCK = "Canvas - Undock #";
    private const string MODALS = "Canvas - Modals";
    private const string DISPLAY_FOCUS_MANAGER = "DisplayFocusManager";

    private static readonly List<ActiveDisplayInfo> ActiveDisplays = [];
    private static readonly List<MonitorInfo> cachedMonitorInfo = [];
    private static readonly Dictionary<int, MonitorInfo> unityToWindowsMonitorMap = [];

    public static bool FocusManagerActive { get; private set; }
    public static int DisplayCount => ActiveDisplays.Count;
    public static bool Initialised { get; private set; }


    private class CoroutineRunner : MonoBehaviour { }

    public static void InitialiseDisplays(Settings settings)
    {
        // Register for display updates
        Display.onDisplaysUpdated += OnDisplaysUpdated;

        // Build caches and mapping
        OnDisplaysUpdated();

        // Get Unity's display layout
        List<DisplayInfo> displays = [];
        Screen.GetDisplayLayout(displays);

        // Get System display layout
        var monitors = GetMonitorLayout();

        // No settings - register main game on current display
        settings.Displays ??= [];

        // Find the current display and monitor
        int currentDisplayIndex = displays.FindIndex(d => d.Equals(Screen.mainWindowDisplayInfo));
        var currentMonitor = GetWindowsMonitorForUnityDisplay(currentDisplayIndex);
        Logger.LogInfo($"Current Unity display: {currentDisplayIndex}, Monitor: {currentMonitor?.FriendlyName ?? "Unknown"}");

        // Find all settings for main display that are currently connected
        var connectedMainDisplaySettings = settings.Displays
            .Where(d => d.Mode == DisplayModes.Main)
            .Where(d => cachedMonitorInfo.Any(m => string.Equals(d.DeviceId, m.DeviceID, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        DisplaySettings mainDisplaySettings;

        if (connectedMainDisplaySettings.Count == 0)
        {
            // No settings for main display match a currently connected monitor
            // Create new display settings for the current display, continue with main display initialisation
            mainDisplaySettings = new DisplaySettings
            {
                Name = displays[currentDisplayIndex].name,
                DeviceId = currentMonitor.DeviceID,
                Mode = DisplayModes.Main,
                Scale = Preferences.GraphicsCanvasScale,
                AllowWindows = true
            };

            settings.Displays.Add(mainDisplaySettings);

        }
        else if (connectedMainDisplaySettings.Count == 1)
        {
            // Exactly one main display setting
            mainDisplaySettings = connectedMainDisplaySettings.First();

            // Check if current display matches the main display in settings
            bool isCurrentDisplayTheMainOne = string.Equals(
                currentMonitor?.DeviceID,
                mainDisplaySettings.DeviceId,
                StringComparison.OrdinalIgnoreCase);

            if (!isCurrentDisplayTheMainOne)
            {
                // Current display is different from main in settings - move and restart
                Logger.LogInfo($"Current display doesn't match main display in settings. Moving to {mainDisplaySettings.Name}");
                MoveAndRestartApplication(mainDisplaySettings, displays);
                return;
            }

            // 2aii. Current display is already the main one - proceed
            Logger.LogInfo($"Current display matches main display in settings: {mainDisplaySettings.Name}");
        }
        else 
        {
            // 2b. Multiple main display settings - check if current is one of them
            var settingForCurrentDisplay = connectedMainDisplaySettings.FirstOrDefault(
                d => string.Equals(d.DeviceId, currentMonitor?.DeviceID, StringComparison.OrdinalIgnoreCase));

            if (settingForCurrentDisplay != null)
            {
                // Current display is one of the main displays in settings - use it
                mainDisplaySettings = settingForCurrentDisplay;
                Logger.LogInfo($"Multiple main displays in settings. Current display is one of them - using it: {mainDisplaySettings.Name}");

                // Set other main displays to Secondary
                foreach (var otherMain in connectedMainDisplaySettings.Where(d => d != mainDisplaySettings))
                {
                    otherMain.Mode = DisplayModes.ExtraWindows;
                    Logger.LogInfo($"Changed '{otherMain.Name}' from Main to Secondary mode");
                }
            }
            else
            {
                // Current display is not in the main displays - move to first one
                mainDisplaySettings = connectedMainDisplaySettings[0];
                Logger.LogInfo($"Current display is not among main displays in settings. Moving to: {mainDisplaySettings.Name}");
                MoveAndRestartApplication(mainDisplaySettings, displays);
                return; // Early return since we're restarting
            }
        }

        //// Set other connected main displays to Secondary mode
        //foreach (var otherMain in connectedMainDisplaySettings.Where(d => d != mainDisplaySettings))
        //{
        //    otherMain.Mode = DisplayModes.Secondary;
        //    Logger.LogInfo($"Changed '{otherMain.Name}' from Main to Secondary mode");
        //}

        InitialiseMain(mainDisplaySettings, currentDisplayIndex);

        // Find all settings for other displays that are currently connected
        var connectedOtherDisplaySettings = 
            settings.Displays
                .Where(d => d.Mode != DisplayModes.Disabled && d.Mode != DisplayModes.Main)
                .Where(d => cachedMonitorInfo.Any(m => string.Equals(d.DeviceId, m.DeviceID, StringComparison.OrdinalIgnoreCase)))
                .ToList();

        // Activate additional displays from settings
        foreach (var displaySetting in connectedOtherDisplaySettings)
        {
            var logicDisplay = settings.Displays.IndexOf(displaySetting);

            // display 0 is reserved and must be either the main display or not used
            if (logicDisplay > 0)
                ActivateDisplay(logicDisplay, displaySetting);
            else
                Logger.LogInfo($"Secondary screen nominated on primary display, skipping!");
        }

        //DrawDisplayLayout();
    }

    private static void InitialiseMain(DisplaySettings settings, int logicDisplay = 0)
    {

        var dispInfo = new ActiveDisplayInfo
        {
            Settings = settings,
        };

        ActiveDisplays.Add(dispInfo);

        Logger.LogDebug($"Main Display: {logicDisplay}");

        if (logicDisplay != 0)
        {
            List<DisplayInfo> displays = [];
            Screen.GetDisplayLayout(displays);

            //Screen.MoveMainWindowTo(displays[logicDisplay], new Vector2Int(0, 0));
        }

        var coroutineObj = new GameObject("WaitforModals");
        GameObject.DontDestroyOnLoad(coroutineObj);
        coroutineObj.SetActive(true);
        coroutineObj.AddComponent<CoroutineRunner>().StartCoroutine(WaitForModals(dispInfo, coroutineObj));
    }

    private static IEnumerator WaitForModals(ActiveDisplayInfo info, GameObject coroutineObj)
    {
        GameObject modalContainer = null;

        // Find MODALS container
        yield return new WaitUntil(() => modalContainer = GameObject.Find(MODALS));

        Logger.LogInfo("WaitForModals() Found Modals container");

        info.DockContainer = modalContainer;

        GameObject.Destroy(coroutineObj);

        Initialised = true;
        WindowUtils.ProcessQueuedWindows();
    }

    public static void ActivateDisplay(int displayIndex, DisplaySettings settings)
    {
        Logger.LogInfo($"Display {displayIndex} Activating...");

        // Create active display info
        var newDisplay = new ActiveDisplayInfo
        {
            Settings = settings,
        };

        var targetDisplay = Display.displays[displayIndex];
        Logger.LogDebug($"Display {displayIndex}: {targetDisplay.systemWidth}x{targetDisplay.systemHeight}");

        Screen.fullScreen = true;
        targetDisplay.Activate();
        targetDisplay.SetRenderingResolution(targetDisplay.systemWidth, targetDisplay.systemHeight);
        targetDisplay.SetParams(targetDisplay.systemWidth, targetDisplay.systemHeight, 0, 0);

        //Create a new camera for the display
        var camera = CreateDisplayCamera(displayIndex);

        // Create and setup canvas
        var canvas = CreateDisplayCanvas(displayIndex, camera);
        newDisplay.DockContainer = canvas;

        var bg = SetupBackground(canvas, settings);
        newDisplay.Background = bg;

        // Store in active display
        ActiveDisplays.Add(newDisplay);

        Logger.LogInfo($"Display {displayIndex} Activated");
    }

    public static void MoveAndRestartApplication(DisplaySettings targetSettings, List<DisplayInfo> displays)
    {
        // Find the Unity display that matches the target device ID
        int targetDisplayIndex = -1;
        for (int i = 0; i < displays.Count; i++)
        {
            var monitor = GetWindowsMonitorForUnityDisplay(i);
            if (monitor != null && string.Equals(monitor.DeviceID, targetSettings.DeviceId, StringComparison.OrdinalIgnoreCase))
            {
                targetDisplayIndex = i;
                break;
            }
        }

        if (targetDisplayIndex >= 0)
        {
            // Create a coroutine handler object that survives scene changes
            var coroutineObj = new GameObject("DisplayMoveHandler");
            GameObject.DontDestroyOnLoad(coroutineObj);
            var runner = coroutineObj.AddComponent<CoroutineRunner>();

            // Start the move coroutine
            runner.StartCoroutine(MoveDisplayAndRestart(displays[targetDisplayIndex], coroutineObj));
        }
        else
        {
            Logger.Log($"Warning: Could not find Unity display for target display {targetSettings.Name}");
        }
    }

    private static IEnumerator MoveDisplayAndRestart(DisplayInfo targetDisplay, GameObject coroutineObj)
    {
        Logger.LogInfo($"Moving game window to display: {targetDisplay.name} ({targetDisplay.width}x{targetDisplay.height})");

        //Switch to windowed mode (can't move windows in fullscreen)
        FullScreenMode originalMode = Screen.fullScreenMode;

        Logger.LogInfo("Switching to windowed mode");
        Screen.fullScreenMode = FullScreenMode.Windowed;

        // Wait 1 frame to ensure mode change is applied
        yield return null;

        // Change resolution to match the target display
        Logger.LogInfo($"Setting resolution to {targetDisplay.width}x{targetDisplay.height}");
        Screen.SetResolution(targetDisplay.width, targetDisplay.height, false);

        // Wait 1 frame to ensure mode change is applied
        yield return null;

        // Move the window
        Logger.LogInfo("Moving window");
        AsyncOperation moveOperation = Screen.MoveMainWindowTo(in targetDisplay, new Vector2Int(0, 0));

        // Wait for the move to complete
        while (!moveOperation.isDone)
        {
            yield return null;
        }

        Logger.LogInfo($"Returning to fullscreen mode: {originalMode}");
        Screen.fullScreenMode = originalMode;

        // Wait 1 frame to ensure mode change is applied
        yield return null;

        Logger.LogInfo("Display move complete, preparing to restart");
        yield return new WaitForSeconds(1.0f);

        GameObject.Destroy(coroutineObj);

        RestartApplication();
    }

    public static void RestartApplication()
    {
        Logger.LogInfo("Requesting restart via Steam");

        // Launch the game via Steam protocol URI
        string steamProtocolUri = "steam://rungameid/1683150";

        System.Diagnostics.Process.Start(steamProtocolUri);
        Logger.LogInfo("Exiting application for restart");
        Application.Quit();
    }

    private static Camera CreateDisplayCamera(int displayIndex)
    {
        var cameraGO = new GameObject($"Display{displayIndex}Camera");
        var camera = cameraGO.AddComponent<Camera>();
        camera.targetDisplay = displayIndex;
        camera.cullingMask = 0; // Fix for trees rendering
        cameraGO.SetActive(true);
        return camera;
    }

    private static GameObject CreateDisplayCanvas(int displayIndex, Camera camera)
    {
        var canvasGO = new GameObject(UNDOCK + displayIndex)
        {
            layer = 5 // GUI layer
        };

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1;
        canvas.worldCamera = camera;
        canvas.targetDisplay = displayIndex;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.SetActive(true);

        return canvas.gameObject;
    }

    private static RawImage SetupBackground(GameObject canvas, DisplaySettings settings)
    {
        var background = canvas.AddComponent<RawImage>();

        background.enabled = settings.SolidBg;

        if (ColorUtility.TryParseHtmlString(settings.BgColour, out Color newCol))
            background.color = newCol;
        else
            background.color = Color.black;

        return background;
    }

    public static void EnableDisplayFocusManager(bool enable = true)
    {
        GameObject displayManager = GameObject.Find(DISPLAY_FOCUS_MANAGER);

        if (enable == (displayManager != null))
            return;

        if (enable)
        {
            Logger.LogInfo("Enabling Display Focus Manager");
            //Add our DisplayFocusManager to try to keep both windows active
            displayManager = new GameObject(DISPLAY_FOCUS_MANAGER);
            displayManager.AddComponent<DisplayFocusManager>();
            GameObject.DontDestroyOnLoad(displayManager);
            displayManager.SetActive(true);
        }
        else
        {
            Logger.LogInfo("Removing Display Focus Manager");
            GameObject.Destroy(displayManager);
        }

        FocusManagerActive = enable;
    }

    public static GameObject GetDisplayContainerFromIndex(int displayIndex)
    {
        //todo: update to access list of active displays
        if (displayIndex < 0 || displayIndex > ActiveDisplays.Count - 1)
            return null;

        return ActiveDisplays[displayIndex].DockContainer;
    }

    public static ActiveDisplayInfo GetDisplayInfoFromIndex(int displayIndex)
    {
        if (displayIndex < 0 || displayIndex > ActiveDisplays.Count - 1)
            return null;

        return ActiveDisplays[displayIndex];
    }

    public static int GetDisplayIndexForContainer(GameObject container)
    {
        // Check active displays first for exact match
        for (int i = 0; i < ActiveDisplays.Count; i++)
        {
            if (ActiveDisplays[i].DockContainer == container)
                return i;
        }

        // Fallback to name-based lookup
        string containerName = container.name;

        if (containerName == MODALS)
            return 0;

        if (containerName.StartsWith(UNDOCK) &&
            int.TryParse(containerName.Substring(UNDOCK.Length), out int displayIndex))
            return displayIndex;

        // Default to main display if no match found
        return 0;
    }

    public static DisplaySettings GetDisplaySettings(int displayIndex, bool forSettings = false)
    {
        Logger.Log($"GetDisplaySettings({displayIndex}) {ActiveDisplays?.Count}");

        if (displayIndex < 0 || displayIndex >= ActiveDisplays.Count)
            return null;

        var displayInfo = ActiveDisplays[displayIndex];
        Logger.Log($"GetDisplaySettings({displayIndex}) got displayInfo");
        // Ensure display info exists
        if (displayInfo == null)
        {
            Logger.Log($"Display info missing for index {displayIndex}");
            return null;
        }

        if (displayInfo.DockContainer == null && !forSettings)
        {
            Logger.Log($"Display container missing for index {displayIndex}");
            return null;
        }

        if (displayInfo.Settings == null)
        {
            Logger.Log($"Display Settings missing for index {displayIndex}");
            return null;
        }

        Logger.Log($"GetDisplaySettings({displayIndex}) passed checks");

        //for the main display, we need to use the game's setting
        //todo: see if we can harmonise this in the future
        if (displayIndex == 0)
        {
            Logger.Log($"GetDisplaySettings({displayIndex}) Attempt settings for main");
            ActiveDisplays[displayIndex].Settings.Scale = Preferences.GraphicsCanvasScale;
        }

        Logger.Log($"GetDisplaySettings({displayIndex}) return");
        return ActiveDisplays[displayIndex].Settings;
    }

    /// <summary>
    /// Gets Windows monitor information for a Unity display index
    /// </summary>
    /// <param name="unityDisplayIndex">The Unity display index to look up</param>
    /// <returns>Corresponding Windows monitor info, or null if not found</returns>
    public static MonitorInfo GetWindowsMonitorForUnityDisplay(int unityDisplayIndex)
    {
        // Make sure mapping exists
        if (unityToWindowsMonitorMap.Count == 0)
            MapUnityDisplaysToWindowsDisplays();

        if (unityToWindowsMonitorMap.TryGetValue(unityDisplayIndex, out MonitorInfo monitor))
        {
            return monitor;
        }

        return null;
    }

#if DEBUG
    public static void DrawDisplayLayout()
    {
        // Ensure monitors are enumerated
        var monitors = GetMonitorLayout();

        // Validate we have monitors to draw
        if (monitors == null || monitors.Count == 0)
        {
            Logger.LogInfo("No monitors detected to draw layout");
            return;
        }

        int width = 1920;
        int height = 1080;

        // Calculate monitor bounds
        int minX = monitors.Min(m => m.Bounds.Left);
        int minY = monitors.Min(m => m.Bounds.Top);
        int maxX = monitors.Max(m => m.Bounds.Right);
        int maxY = monitors.Max(m => m.Bounds.Bottom);

        Logger.LogInfo($"Monitor bounds: ({minX},{minY}) to ({maxX},{maxY})");

        // Calculate spacing for both directions
        float spacingX = (maxX - minX) * 0.02f;
        float spacingY = (maxY - minY) * 0.02f;

        // Count unique vertical and horizontal positions
        int uniqueVerticalPositions = monitors.Select(m => m.Bounds.Top).Distinct().Count();
        int uniqueHorizontalPositions = monitors.Select(m => m.Bounds.Left).Distinct().Count();

        // Calculate total dimensions including spacing
        float totalWidth = maxX - minX + (spacingX * (uniqueHorizontalPositions - 1));
        float totalHeight = maxY - minY + (spacingY * (uniqueVerticalPositions - 1));

        // Calculate scale to fit everything with padding
        float scaleX = width * 0.8f / totalWidth;
        float scaleY = height * 0.8f / totalHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        Logger.LogInfo($"Scale factor: {scale}");

        // Calculate centering offsets
        float scaledTotalWidth = totalWidth * scale;
        float scaledTotalHeight = totalHeight * scale;
        float centerOffsetX = (width - scaledTotalWidth) * 0.5f;
        float centerOffsetY = (height - scaledTotalHeight) * 0.5f;

        // Create rendering resources
        RenderTexture source = RenderTexture.GetTemporary(width, height, 24);
        RenderTexture dest = RenderTexture.GetTemporary(width, height, 24);
        Material blitMaterial = new(Shader.Find("Unlit/Texture"))
        {
            color = Color.white
        };

        // Setup rendering
        Graphics.SetRenderTarget(dest);
        GL.Clear(true, true, Color.black);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, width, height, 0);

        // Draw each monitor
        foreach (var monitor in monitors)
        {
            // Calculate spacing offsets in both directions
            int monitorsToLeft = monitors.Count(m => m.Bounds.Left < monitor.Bounds.Left);
            int monitorsAbove = monitors.Count(m => m.Bounds.Top < monitor.Bounds.Top);

            float x = (monitor.Bounds.Left - minX) * scale + (monitorsToLeft * spacingX * scale) + centerOffsetX;
            float y = (monitor.Bounds.Top - minY) * scale + (monitorsAbove * spacingY * scale) + centerOffsetY;
            float w = (monitor.Bounds.Right - monitor.Bounds.Left) * scale;
            float h = (monitor.Bounds.Bottom - monitor.Bounds.Top) * scale;

            Logger.LogInfo($"Drawing monitor at ({x},{y}) size: {w}x{h}");
            Graphics.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture, blitMaterial);
        }

        GL.PopMatrix();

        // Save result
        Texture2D finalTex = new(width, height, TextureFormat.RGB24, false);
        RenderTexture.active = dest;
        finalTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        finalTex.Apply();

        File.WriteAllBytes(Path.Combine(Multiscreen.ModEntry.Path, "DisplayLayout.png"), finalTex.EncodeToPNG());

        // Cleanup
        RenderTexture.ReleaseTemporary(source);
        RenderTexture.ReleaseTemporary(dest);
    }
#endif

    private static void OnDisplaysUpdated()
    {
        Logger.LogInfo("DisplaysUpdated");
        GetMonitorLayout(true);
        MapUnityDisplaysToWindowsDisplays();
    }

    /// <summary>
    /// Retrieves information about all active monitors in the system.
    /// </summary>
    /// <remarks>
    /// This method enumerates all monitors currently attached to the desktop and collects
    /// information about each one, including position, size, device identifiers, and
    /// friendly names. 
    /// 
    /// For each monitor, it attempts to get the physical monitor device ID, which can be
    /// used to uniquely identify monitors across system restarts and display configuration
    /// changes. If a proper device ID cannot be obtained, a fallback ID is generated based
    /// on monitor properties.
    /// </remarks>
    /// <returns>
    /// A list of <see cref="MonitorInfo"/> objects
    /// </returns>
    public static List<MonitorInfo> GetMonitorLayout(bool refreshCache = false)
    {
        if (refreshCache)
            cachedMonitorInfo.Clear();

        if (cachedMonitorInfo.Count > 0)
            return cachedMonitorInfo;

        // Pre-cache all display adapters (essentially, gfx cards)
        var adapterCache = GetDisplayDevices();
        // Pre-cache all display monitors (this may be an issue with casting to tablets)
        var displayMonitors = GetDisplayMonitors();

        foreach (var monitor in displayMonitors) 
        {
            // Look up the adapter in our cache
            if (adapterCache.TryGetValue(monitor.Name, out DISPLAY_DEVICE displayAdapter))
            {
                // We found the adapter, now let's get the monitor attached to it
                if (GetMonitorDeviceInfo(displayAdapter.DeviceName, out DISPLAY_DEVICE monitorDevice))
                {
                    monitor.DeviceID = monitorDevice.DeviceID;
                    monitor.FriendlyName = monitorDevice.DeviceString;

                    Logger.LogDebug($"Monitor: {monitor.Name}, Monitor Device ID: {monitorDevice.DeviceID}, Monitor Name: {monitorDevice.DeviceString}, Monitor Handle: {monitor.Handle}");
                }
                else
                {
                    //No monitor info returned
                    monitor.DeviceID = CreateFallbackDeviceID(monitor);
                    monitor.FriendlyName = $"Unknown Monitor ({monitor.Name})";

                    Logger.Log($"Warning: No monitor info available for monitor {monitor.Name}, using fallback ID: {monitor.DeviceID}");
                }
            }
            else
            {
                // We found a monitor but no matching adapter
                monitor.DeviceID = CreateFallbackDeviceID(monitor);

                Logger.Log($"Warning: No adapter found for monitor {monitor.Name}, using fallback ID: {monitor.DeviceID}");
            }
        }

        NormaliseLayout(displayMonitors);

        cachedMonitorInfo.AddRange(displayMonitors);

        return cachedMonitorInfo;
    }

    public static void LogSystemDisplayConfiguration()
    {
        StringBuilder sb = new();
        sb.AppendLine("\r\n===== SYSTEM DISPLAY CONFIGURATION =====");

        // 1. Log all adapters first using the existing cache method
        sb.AppendLine("\r\n--- Graphics Adapters ---");
        var adapters = GetDisplayDevices();

        if (adapters.Count == 0)
        {
            sb.AppendLine("No display adapters found");
        }
        else
        {
            int adapterIndex = 0;
            foreach (var adapterEntry in adapters)
            {
                var adapter = adapterEntry.Value;
                sb.AppendLine($"{(adapterIndex > 0 ? "\r\n" : "")}Adapter {adapterIndex}: {adapter.DeviceName}");
                sb.AppendLine($"\tDescription: {adapter.DeviceString}");
                sb.AppendLine($"\tDeviceID: {adapter.DeviceID}");
                sb.AppendLine($"\tState: {adapter.StateFlags}");

                // For each adapter, log its monitors
                int monitorCount = 0;
                DISPLAY_DEVICE monitor = new() { cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };

                for (uint monitorIndex = 0; EnumDisplayDevices(adapter.DeviceName, monitorIndex, ref monitor, 0); monitorIndex++)
                {
                    monitorCount++;
                    sb.AppendLine($"\tMonitor {monitorIndex}: {monitor.DeviceString}");
                    sb.AppendLine($"\t\tDeviceID: {monitor.DeviceID}");
                    // Reset for next monitor
                    monitor = new() { cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };
                }

                if (monitorCount == 0)
                {
                    sb.AppendLine("\tNo monitors found for this adapter");
                }

                adapterIndex++;
            }
        }

        // 2. Log the actual monitor layout (reuses your GetMonitorLayout)
        sb.AppendLine("\r\n--- Active Monitor Layout ---");
        var monitors = GetMonitorLayout();

        if (monitors.Count == 0)
        {
            sb.AppendLine("No active monitors found");
        }
        else
        {
            for (int i = 0; i < monitors.Count; i++)
            {
                var monitor = monitors[i];
                int width = monitor.Bounds.Right - monitor.Bounds.Left;
                int height = monitor.Bounds.Bottom - monitor.Bounds.Top;

                sb.AppendLine($"{(i > 0 ? "\r\n" : "")}Monitor: {monitor.Name}");
                sb.AppendLine($"\tDevice: {monitor.FriendlyName}");
                sb.AppendLine($"\tDeviceID: {monitor.DeviceID}");
                sb.AppendLine($"\tHandle: {monitor.Handle}");
                sb.AppendLine($"\tPosition: ({monitor.Bounds.Left}, {monitor.Bounds.Top})");
                sb.AppendLine($"\tResolution: {width} x {height}");
                sb.AppendLine($"\tPrimary: {(monitor.IsPrimary ? "Yes" : "No")}");
            }
        }

        sb.AppendLine("========================================");
        Logger.Log(sb.ToString());
    }

    public static void LogUnityDisplayInfo()
    {
        List<DisplayInfo> displays = [];
        Screen.GetDisplayLayout(displays);

        StringBuilder sb = new();
        sb.AppendLine("\r\n===== UNITY DISPLAY INFORMATION =====");

        if (displays.Count == 0)
        {
            sb.AppendLine("No displays found!");
        }
        else
        {
            for (int i = 0; i < displays.Count; i++)
            {
                var display = displays[i];

                sb.AppendLine($"\r\nDisplay {i}: {display.name}");
                sb.AppendLine($"\tHandle: {display.handle}");
                sb.AppendLine($"\tResolution: {display.width} x {display.height}");
                sb.AppendLine($"\tRefresh Rate: {display.refreshRate.value} Hz"); // Convert from thousandths to Hz
                sb.AppendLine($"\tWork Area:");
                sb.AppendLine($"\t\tPosition: ({display.workArea.x}, {display.workArea.y})");
                sb.AppendLine($"\t\tSize: {display.workArea.width} x {display.workArea.height}");

                // Additional information from Unity's Screen class
                if (i < Display.displays.Length)
                {
                    var unityDisplay = Display.displays[i];
                    sb.AppendLine($"\tSystem Resolution: {unityDisplay.systemWidth} x {unityDisplay.systemHeight}");
                    sb.AppendLine($"\tRenderingWidth/Height: {unityDisplay.renderingWidth} x {unityDisplay.renderingHeight}");
                    sb.AppendLine($"\tActive: {unityDisplay.active}");
                }

                // Display index information
                if (i == 0)
                {
                    sb.AppendLine($"\tMain Display: Yes");
                }
                else
                {
                    sb.AppendLine($"\tMain Display: No");
                }
            }
        }

        sb.AppendLine("\r\n--- Additional Screen Information ---");
        sb.AppendLine($"Current Resolution: {Screen.currentResolution.width} x {Screen.currentResolution.height} @ {Screen.currentResolution.refreshRate} Hz");
        sb.AppendLine($"Full Screen: {Screen.fullScreen}");
        sb.AppendLine($"Full Screen Mode: {Screen.fullScreenMode}");
        sb.AppendLine($"System Resolution: {Screen.width} x {Screen.height}");

        sb.AppendLine("========================================");

        Logger.Log(sb.ToString());
    }

    /// <summary>
    /// Maps Unity display indices to Windows monitor information using handles
    /// </summary>
    private static void MapUnityDisplaysToWindowsDisplays()
    {
        unityToWindowsMonitorMap.Clear();

        var windowsMonitors = GetMonitorLayout().ToArray();

        // Get Unity displays
        List<DisplayInfo> unityDisplays = [];
        Screen.GetDisplayLayout(unityDisplays);

        Logger.LogDebug($"Mapping {unityDisplays.Count} Unity displays to {windowsMonitors.Count()} Windows monitors");

        for (int unityIndex = 0; unityIndex < unityDisplays.Count; unityIndex++)
        {
            var unityDisplay = unityDisplays[unityIndex];

            int monitorIndex = 0;
            bool found = false;
            while (monitorIndex < windowsMonitors.Length)
            {
                if (unityDisplay.handle == windowsMonitors[monitorIndex].Handle)
                {
                    found = true;
                    break;
                }
                monitorIndex++;
            }

            if (found)
            {
                var monitor = windowsMonitors[monitorIndex];
                unityToWindowsMonitorMap[unityIndex] = monitor;

                Logger.LogDebug($"Mapped Unity display {unityIndex} ({unityDisplay.name}) to Windows monitor: {monitor.Name}, {monitor.FriendlyName}");
            }
            else
            {
                Logger.Log($"Warning: Could not find Windows monitor with handle {unityDisplay.handle} for Unity display {unityDisplay.name} - {unityDisplay.width}x{unityDisplay.height} {unityIndex}");
            }
        }
    }

    private static string CreateFallbackDeviceID(MonitorInfo monitor)
    {
        int width = monitor.Bounds.Right - monitor.Bounds.Left;
        int height = monitor.Bounds.Bottom - monitor.Bounds.Top;
        return $"FALLBACK_{monitor.Name}_{width}x{height}_{monitor.IsPrimary}";
    }

    // Calculate nomalised monitor bounds
    private static void NormaliseLayout(List<MonitorInfo> monitors)
    {
        // Find the leftmost and topmost coordinates
        int minX = monitors.Min(m => m.Bounds.Left);
        int minY = monitors.Min(m => m.Bounds.Top);

        // Shift all monitors so the leftmost and topmost points are at (0,0)
        foreach (var monitor in monitors)
        {
            monitor.NormalisedBounds = new RECT
            {
                Left = monitor.Bounds.Left - minX,
                Right = monitor.Bounds.Right - minX,
                Top = monitor.Bounds.Top - minY,
                Bottom = monitor.Bounds.Bottom - minY
            };
        }
    }

    // Enumerate all system display devices (graphics cards)
    private static Dictionary<string, DISPLAY_DEVICE> GetDisplayDevices()
    {
        var adapterCache = new Dictionary<string, DISPLAY_DEVICE>(StringComparer.OrdinalIgnoreCase);

        DISPLAY_DEVICE adapter = new() { cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };

        for (uint i = 0; EnumDisplayDevices(null, i, ref adapter, 0); i++)
        {
            // Store in cache using DeviceName as key
            adapterCache[adapter.DeviceName] = adapter;

            // Reset for next adapter
            adapter = new () { cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };
        }

        return adapterCache;
    }

    // Enumerate all system display monitors
    private static List<MonitorInfo> GetDisplayMonitors()
    {
        List<MonitorInfo> displayCache = [];

        // Enumerate all monitors
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            // delegate called for each monitor
            (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFOEX mi = new() { Size = Marshal.SizeOf(typeof(MONITORINFOEX)) };

                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    var monitorInfo = new MonitorInfo
                    {
                        Name = mi.DeviceName,
                        Handle = (ulong)hMonitor,
                        IsPrimary = mi.Flags == 1,
                        Bounds = mi.Monitor,
                    };

                    displayCache.Add(monitorInfo);
                }

                return true;
            }, IntPtr.Zero);

        return displayCache;
    }


    //Gets monitor specific information
    private static bool GetMonitorDeviceInfo(string adapterName, out DISPLAY_DEVICE monitorDevice)
    {
        monitorDevice = new DISPLAY_DEVICE();
        monitorDevice.cb = Marshal.SizeOf(monitorDevice);

        // Get the monitor attached to this adapter (iDevNum = 0 for the first/primary monitor)
        if (EnumDisplayDevices(adapterName, 0, ref monitorDevice, 0))
        {
            // We got a monitor!
            return true;
        }

        return false;
    }
}
