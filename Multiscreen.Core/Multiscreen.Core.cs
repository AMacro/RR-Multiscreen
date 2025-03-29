using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityModManagerNet;
using System.Collections.Generic;
using Logger = Multiscreen.Util.Logger;
using Multiscreen.Util;
using UnityEngine.SceneManagement;
using UI.Common;

namespace Multiscreen;

public static class Multiscreen
{
    public static UnityModManager.ModEntry ModEntry;
    public static Settings settings;
    public static bool userPrefFullScr;
    public static CoroutineRunner CoroutineRunner;

    [UsedImplicitly]
    internal static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        settings = Settings.Load<Settings>(modEntry);
        ModEntry.OnGUI = settings.Draw;
        ModEntry.OnSaveGUI = settings.Save;
        ModEntry.OnLateUpdate += Multiscreen.LateUpdate;

        Harmony harmony = null;

        try
        {
            Logger.Log($"Game Version: {Application.version}");

            DisplayUtils.LogSystemDisplayConfiguration();
            DisplayUtils.LogUnityDisplayInfo();

            var coroutineObj = new GameObject("Multiscreen CoroutineRunner");
            GameObject.DontDestroyOnLoad(coroutineObj);
            coroutineObj.SetActive(true);
            CoroutineRunner = coroutineObj.AddComponent<CoroutineRunner>();

            //get user preference
            userPrefFullScr = Screen.fullScreen;
            Logger.Log($"User Preference 'Full screen': {userPrefFullScr}");

            SceneManager.sceneLoaded += SceneLoaded;

            //Apply patches
            Logger.LogInfo("Patching...");
            harmony = new Harmony(ModEntry.Info.Id);
            harmony.PatchAll();
            Logger.LogInfo("Patched");

            LogDisplayInfo();

            if (Display.displays.Length <= 1)
            {
                Logger.LogInfo("Less than 2 displays detected, nothing to do...");
                return true;
            }


            DisplayUtils.InitialiseDisplays(settings);

            if (settings.FocusManager)
                DisplayUtils.EnableDisplayFocusManager();

            string message = "";
            bool showMessage = false;

            if(settings.LastRun == -1)
            {
                message = "Multiscreen mod has loaded successfully!\r\n\r\nPlease click the 'Multiscreen Mod' button on the main menu to configure your screens.";
                showMessage = true;
            }

            if (showMessage)
                ModalAlertController.PresentOkay("Multiscreen", message, null);

            if (settings.LastRun != settings.Version)
                settings.LastRun = settings.Version;

        }
        catch (Exception ex)
        {
            Logger.LogInfo($"Failed to load: {ex.Message}\r\n{ex.StackTrace}");
            harmony?.UnpatchAll();
            return false;
        }

        return true;
    }

    private static void LogDisplayInfo()
    {
        Logger.LogInfo($"Display Count: {Display.displays.Length}");

        List<DisplayInfo> displays = [];
        Screen.GetDisplayLayout(displays);

        for (int i = 0; i < displays.Count; i++)
        {
            var display = displays[i];
            Logger.LogDebug($"Display {i} ({display.name}): {display.width}x{display.height} @ {display.refreshRate} Hz");
        }
    }

    private static void LateUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
    {
        if (ModEntry.NewestVersion != null && ModEntry.NewestVersion.ToString() != "")
        {
            Logger.LogInfo($"Multiscreen Latest Version: {ModEntry.NewestVersion}");

            ModEntry.OnLateUpdate -= Multiscreen.LateUpdate;

            if (ModEntry.NewestVersion > ModEntry.Version)
            {
                ModalAlertController.PresentOkay("Multiscreen", "A new version of Multiscreen Mod is available.\r\n\r\n" +
                    $"Current version: { ModEntry.Version}\r\n" +
                    $"New version: { ModEntry.NewestVersion}\r\n\r\n" +
                    $"Run Unity Mod Manager Installer to apply the update.", null);
            }

        }
    }

    private static void SceneLoaded(Scene s, LoadSceneMode mode)
    {
        Logger.Log($"Scene Loaded! {s.name}");
        Screen.fullScreen = (s.name == "MainMenu" && userPrefFullScr);
    }
}

public class CoroutineRunner : MonoBehaviour { }