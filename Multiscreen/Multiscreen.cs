using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine.UI;
using UnityEngine;
using UnityModManagerNet;
using System.Collections.Generic;
using Analytics;
using Helpers;
using TMPro;
using Logger = Multiscreen.Util.Logger;


namespace Multiscreen;

public static class Multiscreen
{
    public static UnityModManager.ModEntry ModEntry;
    public static Settings settings;
    public static int gameDisplay = 0;
    public static int secondDisplay = 1;
    public static int targetDisplay = 0;
    public static RawImage background;

    //private const string LOG_FILE = "multiscreen.log";

    public const string UNDOCK = "Canvas - Undock";
    public const string MODALS = "Canvas - Modals";

    [UsedImplicitly]
    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        settings = Settings.Load<Settings>(modEntry);
        ModEntry.OnGUI = settings.Draw;
        ModEntry.OnSaveGUI = settings.Save;
        ModEntry.OnLateUpdate += Multiscreen.LateUpdate;

        Harmony harmony = null;

        try
        {
            //File.Delete(LOG_FILE);

            //Apply patches
            Logger.LogInfo("Patching...");
            harmony = new Harmony(ModEntry.Info.Id);
            harmony.PatchAll();
            Logger.LogInfo("Patched");

            //Write screen data to log
            Logger.LogInfo($"Display Count: {Display.displays.Length}");

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
                Logger.LogInfo("Less than 2 displays detected, nothing to do...");
                return true;
            }

            //Validate screen selection settings
            gameDisplay = settings.gameDisplay;
            secondDisplay = settings.secondDisplay;

            Logger.LogDebug(() => $"\r\n\tGame Display: {gameDisplay}\r\n\tSecond Display: {secondDisplay}");

            if(gameDisplay == secondDisplay)
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

            for( i=0; i<Display.displays.Length; i++)
            {
                Logger.LogDebug($"Display {i} Active: {Display.displays[i].active}");

            }
           
            Activate();
        }
        catch (Exception ex)
        {
            Logger.LogInfo($"Failed to load: {ex.Message}\r\n{ex.StackTrace}");
            harmony?.UnpatchAll();
            return false;
        }

        return true;
    }



    private static void LateUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
    {
        if(ModEntry.NewestVersion != null && ModEntry.NewestVersion.ToString() != "")
        {
            Logger.LogInfo($"Multiscreen Latest Version: {ModEntry.NewestVersion}");

            ModEntry.OnLateUpdate -= Multiscreen.LateUpdate;

            if (ModEntry.NewestVersion > ModEntry.Version)
            {
                ShowUpdate();
            }
            
        }
        
    }
    private static void ShowUpdate()
    {
        EarlyAccessSplash earlyAccessSplash = UnityEngine.Object.FindObjectOfType<EarlyAccessSplash>();

        if (earlyAccessSplash == null)
            return;

        earlyAccessSplash = UnityEngine.Object.Instantiate<EarlyAccessSplash>(earlyAccessSplash, earlyAccessSplash.transform.parent);

        TextMeshProUGUI text = GameObject.Find("Canvas/EA(Clone)/EA Panel/Scroll View/Viewport/Text").GetComponentInChildren<TextMeshProUGUI>();
        text.text = $"\r\n<style=h3>Multiscreen Update</style>\r\n\r\nA new version of Multiscreen Mod is available.\r\n\r\nCurrent version: {ModEntry.Version}\r\nNew version: {ModEntry.NewestVersion}\r\n\r\nRun Unity Mod Manager Installer to apply the update.";

        RectTransform rt = GameObject.Find("Canvas/EA(Clone)/EA Panel").transform.GetComponent<RectTransform>();


        UnityEngine.Object.DestroyImmediate(GameObject.Find("Canvas/EA(Clone)/EA Panel/Label Regular"));
        UnityEngine.Object.DestroyImmediate(GameObject.Find("Canvas/EA(Clone)/EA Panel/Buttons/Opt Out"));

        UnityEngine.UI.Button button = GameObject.Find("Canvas/EA(Clone)/EA Panel/Buttons/Opt In").GetComponentInChildren<UnityEngine.UI.Button>();
        button.TMPText().text = "OK";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate {
            earlyAccessSplash.Dismiss();
            UnityEngine.Object.Destroy(earlyAccessSplash);
        });

        earlyAccessSplash.Show();
    }
    private static void ShowRestart()
    {
        EarlyAccessSplash earlyAccessSplash = UnityEngine.Object.FindObjectOfType<EarlyAccessSplash>();

        if (earlyAccessSplash == null)
            return;

        earlyAccessSplash = UnityEngine.Object.Instantiate<EarlyAccessSplash>(earlyAccessSplash, earlyAccessSplash.transform.parent);

        TextMeshProUGUI text = GameObject.Find("Canvas/EA(Clone)/EA Panel/Scroll View/Viewport/Text").GetComponentInChildren<TextMeshProUGUI>();
        text.text = "\r\n<style=h3>Restart Required!</style>\r\n\r\nAn update has been made to Unity Mod Manager settings.\r\n\r\nPlease restart Railroader.";

        RectTransform rt = GameObject.Find("Canvas/EA(Clone)/EA Panel").transform.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.rect.width, rt.rect.height / 2);


        UnityEngine.Object.DestroyImmediate(GameObject.Find("Canvas/EA(Clone)/EA Panel/Label Regular"));
        //UnityEngine.Object.DestroyImmediate(GameObject.Find("Canvas/EA/EA Panel/Buttons/Opt In"));
        UnityEngine.Object.DestroyImmediate(GameObject.Find("Canvas/EA(Clone)/EA Panel/Buttons/Opt Out"));

        UnityEngine.UI.Button button = GameObject.Find("Canvas/EA(Clone)/EA Panel/Buttons/Opt In").GetComponentInChildren<UnityEngine.UI.Button>();
        button.TMPText().text = "Quit";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate {
            Application.Quit();
        });

        earlyAccessSplash.Show();
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

        Screen.MoveMainWindowTo(displayInfo[gameDisplay],new Vector2Int(0,0));


        Logger.LogInfo($"Display {targetDisplay} Activating...");

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
        }else
        {
            Multiscreen.settings.bgColour = "000000";
            background.color = UnityEngine.Color.black;
        }

        myGO.AddComponent<CanvasScaler>();
        myGO.AddComponent<GraphicRaycaster>();

        myGO.SetActive(true);

        Logger.LogInfo($"Display {targetDisplay} Activated");
           
    }
}
