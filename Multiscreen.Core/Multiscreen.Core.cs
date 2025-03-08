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
using Multiscreen.Util;
using UnityEngine.SceneManagement;

namespace Multiscreen;

public static class Multiscreen
{
    public static UnityModManager.ModEntry ModEntry;
    public static Settings settings;
    public static bool userPrefFullScr;

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
            Logger.Log($"Game Version: {Application.version}");
            //File.Delete(LOG_FILE);

            DisplayUtils.LogSystemDisplayConfiguration();
            DisplayUtils.LogUnityDisplayInfo();

            //get user preference
            userPrefFullScr = Screen.fullScreen;
            Logger.Log($"User Preference 'Full screen': {userPrefFullScr}");

            SceneManager.sceneLoaded += (Scene s, LoadSceneMode mode) =>
            {
                Logger.Log($"Scene Loaded! {s.name}");
                Screen.fullScreen = (s.name == "MainMenu" && userPrefFullScr);
            };

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

            if (settings.focusManager)
                DisplayUtils.EnableDisplayFocusManager();
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
        button.onClick.AddListener(delegate
        {
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
        button.onClick.AddListener(delegate
        {
            Application.Quit();
        });

        earlyAccessSplash.Show();
    }
}
