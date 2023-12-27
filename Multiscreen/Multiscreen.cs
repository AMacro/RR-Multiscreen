using System;
using System.IO;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine.UI;
using UnityEngine;
using UnityModManagerNet;

namespace Multiscreen;

public static class Multiscreen
{
    public static UnityModManager.ModEntry ModEntry;
    public static Settings Settings;

    private const string LOG_FILE = "multiscreen.log";

    [UsedImplicitly]
    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        Settings = Settings.Load<Settings>(modEntry);
        ModEntry.OnGUI = Settings.Draw;
        ModEntry.OnSaveGUI = Settings.Save;

        Harmony harmony = null;

        try
        {
            File.Delete(LOG_FILE);

            Log("Patching...");
            harmony = new Harmony(ModEntry.Info.Id);
            harmony.PatchAll();

            Log("Patched");
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

    private static void Activate()
    {
        Multiscreen.Log("MainMenu.Awake()");
        Multiscreen.Log($"Display Count: {Display.displays.Length}");
        Multiscreen.Log($"Screen (Display 0): {Screen.width}x{Screen.height}");

        if (Display.displays.Length > 1 && !Display.displays[1].active)
        {
            int width2, height2;
            width2 = Display.displays[1].systemWidth;
            height2 = Display.displays[1].systemHeight;

            Multiscreen.Log($"Display 1: {width2}x{height2}");

            Display.displays[1].SetParams(width2, height2, 0, 0);

            Multiscreen.Log("Display 1 Activating...");

            Display.displays[1].Activate(width2, height2, 60);

            Screen.fullScreen = true;

            GameObject myGO;
            GameObject myCamGO;
            Camera myCamera;
            Canvas myCanvas;

            //Create a new camera for the display
            myCamGO = new GameObject("myCam");
            myCamera = myCamGO.AddComponent<Camera>();
            myCamera.targetDisplay = 1;

            myCamGO.SetActive(true);

            // Canvas
            myGO = new GameObject("Canvas - Undock");
            myGO.layer = 5; //GUI layer

            myCanvas = myGO.AddComponent<Canvas>();

            myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            myCanvas.sortingOrder = 1;
            myCanvas.worldCamera = myCamera;
            myCanvas.targetDisplay = 1;

            myGO.AddComponent<CanvasScaler>();
            myGO.AddComponent<GraphicRaycaster>();

            myGO.SetActive(true);

            Multiscreen.Log("Display 1 Activated");
        }
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
