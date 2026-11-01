// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Modified in 2026 for the Railroader 0.6 port; see NOTICE.

using System.Collections;
using HarmonyLib;
using Multiscreen.CustomMenu;
using TMPro;
using UI.Common;
using UI.Menu;
using UnityEngine;
using UnityEngine.UI;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Menus;

// A "Multiscreen" button on the main menu, just below Preferences
[HarmonyPatch(typeof(MainMenu))]
public static class MainMenuPatch
{
    public const string ButtonTitle = "Multiscreen";

    [HarmonyPostfix]
    [HarmonyPatch("Awake")]
    private static void Awake(MainMenu __instance)
    {
        Logger.LogVerbose("MainMenu.Awake()");

        MainMenu menu = __instance;
        menu.AddButton(ButtonTitle, () => Open(menu));

        int insertIndex = -1;
        Button ours = null;
        foreach (Button button in menu.GetComponentsInChildren<Button>(true))
        {
            string text = button.GetComponentInChildren<TMP_Text>()?.text;
            if (text == "Preferences")
                insertIndex = button.transform.GetSiblingIndex() + 1;
            else if (text == ButtonTitle)
                ours = button;
        }

        if (ours != null && insertIndex >= 0)
            ours.transform.SetSiblingIndex(insertIndex);
    }

    private static void Open(MainMenu menu)
    {
        NavigationController navigation = ((INavigationView)menu).NavigationController();
        if (navigation == null)
        {
            Logger.LogWarningOnce("menu-nav", "Multiscreen: main menu navigation not found; can't open the settings menu");
            return;
        }

        navigation.Push(BuilderMenuBase<MultiscreenMenu>.CreatePanel());
    }
}

// Display.Activate clears Screen.fullScreen. Once the main menu is up, put fullscreen back, but only once per
// game session and only if the player was in fullscreen when the display was activated; later visits to the
// main menu leave the player's own choice alone.
[HarmonyPatch(typeof(MenuManager))]
public static class MenuManagerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    private static void Start(MenuManager __instance)
    {
        Logger.LogTrace("MenuManager.Start()");
        __instance.StartCoroutine(RestoreFullScreen());
    }

    private static IEnumerator RestoreFullScreen()
    {
        if (!Multiscreen.fullscreenRestorePending)
            yield break;

        yield return new WaitForSecondsRealtime(2f);
        if (!Multiscreen.fullscreenRestorePending)
            yield break;

        Multiscreen.fullscreenRestorePending = false;
        if (!Screen.fullScreen)
        {
            Logger.LogInfo("Multiscreen: restoring fullscreen after display activation");
            Screen.fullScreen = true;
        }
    }
}
