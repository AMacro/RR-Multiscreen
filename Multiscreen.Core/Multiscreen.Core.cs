using HarmonyLib;
using Multiscreen.Util;
using Railroader.Modding;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen;

public sealed class Multiscreen : IModEntryPoint
{
    public static IModContext ModContext;
    public static Settings settings;
    public static int gameDisplay = 0;
    public static int secondDisplay = 1;
    public static int targetDisplay = 0;
    public static RawImage background;
    public static bool focusManager;

    public static bool userPrefFullScr;

    public const string UNDOCK = "Canvas - Undock";
    public const string MODALS = "Canvas - Modals";
    public const string DISPLAY_FOCUS_MANAGER = "DisplayFocusManager";

    public void OnModLoad(IModContext context)
    {
        context.Log.Info($"Loaded from {context.ModDirectory}");

        ModContext = context;

        settings = Settings.Load(ModContext.ModDirectory);

        Harmony harmony = null;

        try
        {
            Logger.Log($"Game Version: {Application.version}");

            //get user preference
            userPrefFullScr = Screen.fullScreen;
            Logger.Log($"User Preference 'Full screen': {userPrefFullScr}");

            //Apply patches
            Logger.Log("Patching...");
            harmony = new Harmony(ModContext.ModIdentifier);
            harmony.PatchAll();
            Logger.Log("Patched");

            //Write screen data to log
            Logger.Log($"Display Count: {Display.displays.Length}");

            List<DisplayInfo> displays = new List<DisplayInfo>();
            Screen.GetDisplayLayout(displays);

            int i = 0;
            foreach (DisplayInfo displayInfo in displays)
            {
                Logger.LogDebug($"Display {i} ({displayInfo.name}): {displayInfo.width}x{displayInfo.height} @ {displayInfo.refreshRate} Hz");

                i++;
            }

            if (Display.displays.Length <= 1)
            {
                Logger.Log("Less than 2 displays detected, nothing to do...");
                return;
            }

            //Validate screen selection settings
            gameDisplay = settings.gameDisplay;
            secondDisplay = settings.secondDisplay;

            Logger.LogDebug($"\r\n\tGame Display: {gameDisplay}\r\n\tSecond Display: {secondDisplay}");

            if (gameDisplay == secondDisplay)
            {
                gameDisplay = 0;
                secondDisplay = 1;
            }

            if (gameDisplay < 0 || gameDisplay >= Display.displays.Length)
            {
                gameDisplay = 0;
            }

            if (secondDisplay < 0 || secondDisplay >= Display.displays.Length)
            {
                secondDisplay = 1;
            }

            //To use Display 0 for the second display we need to target display 1, then move the window to display 0
            targetDisplay = secondDisplay == 0 ? 1 : secondDisplay;

            Logger.LogDebug($"\r\n\tGame Display: {gameDisplay}\r\n\tSecond Display: {secondDisplay}\r\n\tTarget Display: {targetDisplay}");

            int mainDisp = displays.FindIndex(s => s.Equals(Screen.mainWindowDisplayInfo));
            Logger.LogDebug($"Main Display: {mainDisp}");

            for (i = 0; i < Display.displays.Length; i++)
            {
                Logger.LogDebug($"Display {i} Active: {Display.displays[i].active}");

            }

            Activate();

            if (settings.focusManager)
                EnableDisplayFocusManager();

            GameObject updateChecker = new("UpdateChecker");
            GameObject.DontDestroyOnLoad(updateChecker);
            updateChecker.AddComponent<UpdateCheck>();
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to load: {ex.Message}\r\n{ex.StackTrace}");
            harmony?.UnpatchAll();
            return;
        }

        return;
    }

    public void OnModUnload()
    {
    }

    public static void Activate()
    {

        int width, height;
        int width2, height2, x2 = 0, y2 = 0;

        width = Display.displays[gameDisplay].renderingWidth;
        height = Display.displays[gameDisplay].renderingHeight;

        width2 = Display.displays[targetDisplay].systemWidth;
        height2 = Display.displays[targetDisplay].systemHeight;

        Logger.LogDebug($"Display 0: {width}x{height} Full Screen: {Screen.fullScreen}");
        Logger.LogDebug($"Display {targetDisplay}: {width2}x{height2}");

        List<DisplayInfo> displayInfo = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displayInfo);

        Screen.MoveMainWindowTo(displayInfo[gameDisplay], new Vector2Int(0, 0));


        Logger.Log($"Display {targetDisplay} Activating...");

        Screen.fullScreen = true;
        Display.displays[targetDisplay].Activate();
        Display.displays[targetDisplay].SetRenderingResolution(width2, height2);
        Display.displays[targetDisplay].SetParams(width2, height2, x2, y2);

        GameObject myGO;
        GameObject myCamGO;
        Camera myCamera;
        Canvas myCanvas;

        //Create a new camera for the display
        myCamGO = new GameObject("SecondDisplayCam");
        myCamera = myCamGO.AddComponent<Camera>();
        myCamera.targetDisplay = targetDisplay;
        myCamera.cullingMask = 0; //fix for trees rendering at top of screen

        myCamGO.SetActive(true);

        // Canvas
        myGO = new GameObject(UNDOCK);
        myGO.layer = 5; //GUI layer

        myCanvas = myGO.AddComponent<Canvas>();

        myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        myCanvas.sortingOrder = 1;
        myCanvas.worldCamera = myCamera;
        myCanvas.targetDisplay = targetDisplay;

        background = myGO.AddComponent<RawImage>();

        background.enabled = Multiscreen.settings.solidBG;

        UnityEngine.Color newCol;

        if (ColorUtility.TryParseHtmlString(Multiscreen.settings.bgColour, out newCol))
        {
            background.color = newCol;
        }
        else
        {
            Multiscreen.settings.bgColour = "000000";
            background.color = UnityEngine.Color.black;
        }

        myGO.AddComponent<CanvasScaler>();
        myGO.AddComponent<GraphicRaycaster>();

        myGO.SetActive(true);

        Logger.Log($"Display {targetDisplay} Activated");

    }
    public static void EnableDisplayFocusManager(bool enable = true)
    {
        GameObject displayManager = GameObject.Find(DISPLAY_FOCUS_MANAGER);

        if (enable == (displayManager != null))
            return;

        if (enable)
        {
            Logger.Log("Enabling Display Focus Manager");
            //Add our DisplayFocusManager to try to keep both windows active
            displayManager = new GameObject(DISPLAY_FOCUS_MANAGER);
            displayManager.AddComponent<DisplayFocusManager>();
            GameObject.DontDestroyOnLoad(displayManager);
            displayManager.SetActive(true);
        }
        else
        {
            Logger.Log("Removing Display Focus Manager");
            GameObject.Destroy(displayManager);
        }

        focusManager = enable;
    }
}
