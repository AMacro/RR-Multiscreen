using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Multiscreen.Util;

public static class DisplayUtils
{
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

    private class CoroutineRunner : MonoBehaviour {}

    public static void InitialiseDisplays(Settings settings)
    {
        int mainDisplayIndex = 0;

        //default case if no settings/bad settings
        var defaultDisplaySettings = new DisplaySettings
            {
                mode = DisplayMode.Main,
                scale = 1f
            };

        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);

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
        yield return new WaitUntil(()=> modalContainer = GameObject.Find(MODALS));

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
}
