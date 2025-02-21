using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;

namespace Multiscreen.Util;

public static class DisplayUtils
{
    #region Native functions
    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
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

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    #endregion

    public class MonitorInfo
    {
        public RECT Bounds { get; set; }
        public bool IsPrimary { get; set; }
        public string Name { get; set; }
    }

    private const string UNDOCK = "Canvas - Undock #";
    private const string MODALS = "Canvas - Modals";
    private const string DISPLAY_FOCUS_MANAGER = "DisplayFocusManager";

    private static readonly List<ActiveDisplayInfo> ActiveDisplays = [];

    public static bool FocusManagerActive { get; private set; }
    public static int DisplayCount => ActiveDisplays.Count;
    public static bool Initialised { get; private set; }

    public class ActiveDisplayInfo
    {
        //public int LogicalIndex { get; set; }  // Current system display number
        public GameObject DockContainer { get; set; }
        public RawImage Background { get; set; }
        public DisplaySettings Settings { get; set; }
    }

    private class CoroutineRunner : MonoBehaviour { }

    public static void InitialiseDisplays(Settings settings)
    {
        int mainDisplayIndex = 0;

        //default case if no settings/bad settings
        var defaultDisplaySettings = new DisplaySettings
        {
            mode = DisplayMode.Main,
            scale = 1f
        };

        // No settings - register main game on current display
        if (settings.displays == null || settings.displays.Length == 0)
        {
            Logger.LogInfo("Initialising displays, no settings found");
            settings.displays = [defaultDisplaySettings];
            InitialiseMain(defaultDisplaySettings);
            return;
        }

        // Find DisplayMode.Main in settings
        var mainDisplaySettings = settings.displays.FirstOrDefault(d => d.mode == DisplayMode.Main);
        if (mainDisplaySettings == null)
        {
            //todo: handle this better - which screen are we on? overwrite just this entry?
            Logger.LogInfo("Initialising displays, no main display found, clearing settings");
            settings.displays = [defaultDisplaySettings];
            InitialiseMain(defaultDisplaySettings);
            return;
        };

        //find the index of the main display in the settings, this will be container 0
        mainDisplayIndex = Array.IndexOf(settings.displays, mainDisplaySettings);
        InitialiseMain(mainDisplaySettings, mainDisplayIndex);

        // Activate additional displays from settings
        foreach (var displaySetting in settings.displays.Where(d =>
            d.mode != DisplayMode.Disabled &&
            d.mode != DisplayMode.Main))
        {
            var logicDisplay = Array.IndexOf(settings.displays, displaySetting);
            ActivateDisplay(logicDisplay, displaySetting);
        }

        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);

        DrawDisplayLayout();
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

            Screen.MoveMainWindowTo(displays[logicDisplay], new Vector2Int(0, 0));
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

        var systemDisplay = Display.displays[displayIndex];

        // Create display info
        var newDisplay = new ActiveDisplayInfo
        {
            //LogicalIndex = displayIndex,
            //Resolution = new Vector2Int(systemDisplay.systemWidth, systemDisplay.systemHeight),
            //IsActive = true
            Settings = settings,
        };


        Logger.LogDebug($"Display {displayIndex}: {systemDisplay.systemWidth}x{systemDisplay.systemHeight}");

        Screen.fullScreen = true;
        systemDisplay.Activate();
        systemDisplay.SetRenderingResolution(systemDisplay.systemWidth, systemDisplay.systemHeight);
        systemDisplay.SetParams(systemDisplay.systemWidth, systemDisplay.systemHeight, 0, 0);

        //Create a new camera for the display
        var camera = CreateDisplayCamera(displayIndex);

        // Create and setup canvas
        var canvas = CreateDisplayCanvas(displayIndex, camera, settings);
        newDisplay.DockContainer = canvas;

        var bg = SetupBackground(canvas, settings);
        newDisplay.Background = bg;

        // Store in actived display
        ActiveDisplays.Add(newDisplay);

        Logger.LogInfo($"Display {displayIndex} Activated");
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

    private static GameObject CreateDisplayCanvas(int displayIndex, Camera camera, DisplaySettings settings)
    {
        var canvasGO = new GameObject(UNDOCK + displayIndex);
        canvasGO.layer = 5; // GUI layer

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1;
        canvas.worldCamera = camera;
        canvas.targetDisplay = displayIndex;

        //SetupBackground(background, settings); //moved to ActivateDisplay(), unless we need to do this before adding scalers

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.SetActive(true);

        return canvas.gameObject;
    }

    private static RawImage SetupBackground(GameObject canvas, DisplaySettings settings)
    {
        var background = canvas.AddComponent<RawImage>();

        background.enabled = settings.solidBG;

        if (ColorUtility.TryParseHtmlString(settings.bgColour, out Color newCol))
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

    public static DisplaySettings GetDisplaySettings(int displayIndex)
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

        if (displayInfo.DockContainer == null)
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

        //for the main display, we need to use the containers lossy scale
        //todo: see if we can harmonise this in the future
        if (displayIndex == 0)
        {
            Logger.Log($"GetDisplaySettings({displayIndex}) Attempt settings for main");
            ActiveDisplays[displayIndex].Settings.scale = ActiveDisplays[displayIndex].DockContainer.transform.lossyScale.x;
        }
        Logger.Log($"GetDisplaySettings({displayIndex}) return");
        return ActiveDisplays[displayIndex].Settings;
    }

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
        Material blitMaterial = new Material(Shader.Find("Unlit/Texture"));
        blitMaterial.color = Color.white;

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
        Texture2D finalTex = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture.active = dest;
        finalTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        finalTex.Apply();

        File.WriteAllBytes(Path.Combine(Multiscreen.ModEntry.Path, "DisplayLayout.png"), finalTex.EncodeToPNG());

        // Cleanup
        RenderTexture.ReleaseTemporary(source);
        RenderTexture.ReleaseTemporary(dest);
    }

    public static List<MonitorInfo> GetMonitorLayout()
    {
        var displays = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFOEX mi = new MONITORINFOEX();
                mi.Size = Marshal.SizeOf(typeof(MONITORINFOEX));

                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    var monitorInfo = new MonitorInfo
                    {
                        Bounds = mi.Monitor,
                        IsPrimary = mi.Flags == 1,
                        Name = mi.DeviceName
                    };
                    displays.Add(monitorInfo);
                }
                return true;
            }, IntPtr.Zero);

        return displays;
    }

    public static List<MonitorInfo> GetNormalisedMonitorLayout(List<MonitorInfo> monitors)
    {
        List <MonitorInfo> result = [];

        // Find the leftmost and topmost coordinates
        int minX = monitors.Min(m => m.Bounds.Left);
        int minY = monitors.Min(m => m.Bounds.Top);

        // Shift all monitors so the leftmost and topmost points are at (0,0)
        foreach (var monitor in monitors)
        {
            var normalizedMonitor = new MonitorInfo
            {
                Name = monitor.Name,
                IsPrimary = monitor.IsPrimary,
                Bounds = new RECT
                {
                    Left = monitor.Bounds.Left - minX,
                    Right = monitor.Bounds.Right - minX,
                    Top = monitor.Bounds.Top - minY,
                    Bottom = monitor.Bounds.Bottom - minY
                }
            };

            result.Add(normalizedMonitor);
        }

        return result;
    }
}
