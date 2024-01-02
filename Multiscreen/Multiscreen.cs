using System;
using System.IO;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine.UI;
using UnityEngine;
using UnityModManagerNet;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Xml;
using Analytics;
using Helpers;
using TMPro;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections;
using Multiscreen.Util;



namespace Multiscreen;

public static class Multiscreen
{

    public static IntPtr hwnd;
    public static UnityModManager.ModEntry ModEntry;
    public static Settings Settings;
    public static int gameDisplay = 0;
    public static int secondDisplay = 1;
    public static int targetDisplay = 0;

    private const string LOG_FILE = "multiscreen.log";
    private const string STARTING_POINT = "[Assembly-CSharp.dll]Game.State.StateManager.Awake:Before";

    [UsedImplicitly]
    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        Settings = Settings.Load<Settings>(modEntry);
        ModEntry.OnGUI = Settings.Draw;
        ModEntry.OnSaveGUI = Settings.Save;
        ModEntry.OnLateUpdate += Multiscreen.LateUpdate;

        Harmony harmony = null;

        try
        {
            File.Delete(LOG_FILE);

            //Make sure we have the right restart point
            Log($"Checking Starting Point...");
            if (!CheckStartPoint())
            {
                ShowRestart();
                return false;
            }

            //Apply patches
            Log("Patching...");
            harmony = new Harmony(ModEntry.Info.Id);
            harmony.PatchAll();
            Log("Patched");


            //Write screen data to log
            Multiscreen.Log($"Display Count: {Display.displays.Length}");

            List<DisplayInfo> displays = new List<DisplayInfo>();
            Screen.GetDisplayLayout(displays);

            int i = 0;
            foreach (DisplayInfo displayInfo in displays)
            {
                Multiscreen.Log($"Display {i} ({displayInfo.name}): {displayInfo.width}x{displayInfo.height} {displayInfo.refreshRate} Hz");

                i++;
            }

            if (Display.displays.Length <= 1)
            {
                Log("Less than 2 displays detected, nothing to do...");
                return true;
            }

            //Validate screen selection settings
            gameDisplay = Settings.gameDisplay;
            secondDisplay = Settings.secondDisplay;

            LogDebug(() => $"\r\n\tGame Display: {gameDisplay}\r\n\tSecond Display: {secondDisplay}");

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

            LogDebug(() => $"\r\n\tGame Display: {gameDisplay}\r\n\tSecond Display: {secondDisplay}\r\n\tTarget Display: {targetDisplay}");

            int mainDisp = displays.FindIndex(s => s.Equals(Screen.mainWindowDisplayInfo));
            LogDebug(() => $"Main Display: {mainDisp}");

            for( i=0; i<Display.displays.Length; i++)
            {
                LogDebug(() => $"Display {i} Active: {Display.displays[i].active}");

            }
            /*
            LogDebug(() => $"Platform: {Application.platform}");
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                if(secondDisplay == 0)
                {
                    LogDebug(() => $"Platform: {Application.platform}");
                    //need to determine which screen to place against
                    GameObject go = new("DispSwapTest");
                    go.AddComponent<DisplaySwapTest>();
                    go.SetActive(true);
                }
                else
                {
                    Activate();
                }
            }
            else
            {
                
                if (secondDisplay == 0)
                {
                    LogWarning($"Only Windows supports config: Second Display = Display 0");
                    LogWarning($"If you are not using Windows please log a bug report on Nexus Mods: https://www.nexusmods.com/railroader/mods/6");
                    LogWarning($"Defaulting to: Game Display = Display 0 and Second Display = Display 1");

                    gameDisplay = 0;
                    secondDisplay = 1;
                    Activate();
                }
            }
            
            */
            Activate();
        }
        catch (Exception ex)
        {
            LogException("Failed to load:", ex);
            harmony?.UnpatchAll();
            return false;
        }

        return true;
    }



    private static void LateUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
    {
        if(ModEntry.NewestVersion != null && ModEntry.NewestVersion.ToString() != "")
        {
            Multiscreen.Log($"Multiscreen Latest Version: {ModEntry.NewestVersion}");

            ModEntry.OnLateUpdate -= Multiscreen.LateUpdate;

            if (ModEntry.NewestVersion > ModEntry.Version)
            {
                ShowUpdate();
            }
            
        }
        
    }

    private static bool CheckStartPoint()
    {
        string config = Path.GetFullPath(Application.dataPath+ "/Managed/UnityModManager/Config.xml");
        if (File.Exists(config))
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(config);

            XmlNode StartingPoint = doc.SelectSingleNode(@"/Config/StartingPoint");

            Log($"Starting Point: {StartingPoint.InnerText}");
            if(StartingPoint.InnerText != STARTING_POINT)
            {
                Log($"Starting Point requires update");
                StartingPoint.InnerText = STARTING_POINT;
                doc.Save(config);
                return false;
            }
            
        }
        Log($"Starting Point: Pass");

        return true;
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
        //rt.sizeDelta = new Vector2(rt.rect.width, rt.rect.height);


        UnityEngine.Object.DestroyImmediate(GameObject.Find("Canvas/EA(Clone)/EA Panel/Label Regular"));
        //UnityEngine.Object.DestroyImmediate(GameObject.Find("Canvas/EA/EA Panel/Buttons/Opt In"));
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
        int width2, height2,x2 = 0,y2 = 0;

        width = Display.displays[gameDisplay].renderingWidth;
        height = Display.displays[gameDisplay].renderingHeight;

            
        width2 = Display.displays[targetDisplay].systemWidth;
        height2 = Display.displays[targetDisplay].systemHeight;

        Multiscreen.Log($"Display 0: {width}x{height} Full Screen: {Screen.fullScreen}");
        Multiscreen.Log($"Display {targetDisplay}: {width2}x{height2}");

        List<DisplayInfo> displayInfo = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displayInfo);

        Screen.MoveMainWindowTo(displayInfo[gameDisplay],new Vector2Int(0,0));
            

        Multiscreen.Log($"Display {targetDisplay} Activating...");

        //Display.displays[1].Activate(width2, height2, 60);
        //Screen.fullScreen = false;
        Display.displays[targetDisplay].Activate();
        Display.displays[targetDisplay].SetRenderingResolution(width2, height2);
        Display.displays[targetDisplay].SetParams(width2, height2, x2, y2);


        //Display.displays[0].SetRenderingResolution(width, height);

        //Screen.fullScreen = false;

        GameObject myGO;
        GameObject myCamGO;
        Camera myCamera;
        Canvas myCanvas;

        //Create a new camera for the display
        myCamGO = new GameObject("myCam");
        myCamera = myCamGO.AddComponent<Camera>();
        myCamera.targetDisplay = targetDisplay;

        myCamGO.SetActive(true);

        // Canvas
        myGO = new GameObject("Canvas - Undock");
        myGO.layer = 5; //GUI layer

        myCanvas = myGO.AddComponent<Canvas>();

        myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        myCanvas.sortingOrder = 1;
        myCanvas.worldCamera = myCamera;
        myCanvas.targetDisplay = targetDisplay;

        myGO.AddComponent<CanvasScaler>();
        myGO.AddComponent<GraphicRaycaster>();

        myGO.SetActive(true);

        Multiscreen.Log($"Display {targetDisplay} Activated");
           
    }

    #region Logging

    public static void LogDebug(Func<object> resolver)
    {
        if (!Settings.DebugLogging)
            return;
        WriteLog($"[Debug] {resolver.Invoke()}");
    }

    public static void Log(object msg)
    {
        WriteLog($"[Info] {msg}");
    }

    public static void LogWarning(object msg)
    {
        WriteLog($"[Warning] {msg}");
    }

    public static void LogError(object msg)
    {
        WriteLog($"[Error] {msg}");
    }

    public static void LogException(object msg, Exception e)
    {
        ModEntry.Logger.LogException($"{msg}", e);
    }

    private static void WriteLog(string msg)
    {
        string str = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        ModEntry.Logger.Log(str);
    }
    #endregion
}
