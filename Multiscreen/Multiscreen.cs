using System;
using UnityModManagerNet;
using JetBrains.Annotations;
using UnityEngine;
using System.Reflection;
using System.IO;


namespace Multiscreen;

public static class MultiscreenLoader
{
    public static UnityModManager.ModEntry ModEntry;
    const string CORE_NAME = "Multiscreen.Core.";

    public const string UNDOCK = "Canvas - Undock #";
    public const string MODALS = "Canvas - Modals";

    [UsedImplicitly]
    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        settings = Settings.Load<Settings>(modEntry);
        settings.UpgradeSettings();
        
        ModEntry.OnGUI = settings.Draw;
        ModEntry.OnSaveGUI = settings.Save;
        ModEntry.OnLateUpdate += Multiscreen.LateUpdate;

        string coreVer = CORE_NAME;

        WriteLog($"Game Version: {Application.version}");
        switch (Application.version.Substring(0,6))
        {
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
            case "2024.6":
                coreVer += "Beta";
                break;

            default:
                coreVer += "Main";
                break;
        }

        WriteLog($"Selected Core Version: {coreVer}");

        string coreDll = coreVer + ".dll";

        string coreAssemblyPath = Path.Combine(modEntry.Path, coreDll);

        if (!File.Exists(coreAssemblyPath))
        {
            WriteLog($"Failed to find core assembly at {coreAssemblyPath}");
            return false;
        }

        try
        {
            Assembly coreAssembly = Assembly.LoadFrom(coreAssemblyPath);

            Type modType = coreAssembly.GetType("Multiscreen.Multiscreen");  
            MethodInfo loadMethod = modType.GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static);

            if (loadMethod == null)
            {
                WriteLog("Failed to find the Load method in the core assembly.");
                return false;
            }

            bool result = (bool)loadMethod.Invoke(null, new object[] { modEntry });

            return result;

        } catch (Exception ex) {
            //handle and log
            WriteLog($"Failed to load core assembly: {ex.Message}\r\n{ex.StackTrace}");
            return false;
        }
    }

    private static void WriteLog(string msg)
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
        myGO = new GameObject(UNDOCK + targetDisplay);
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
           
        string str = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        ModEntry.Logger.Log(str);
    }
}
