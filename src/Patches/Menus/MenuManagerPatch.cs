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

// Display.Activate clears Screen.fullScreen; once the main menu is up, restore what the player had.
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
        yield return new WaitForSecondsRealtime(2f);
        if (Multiscreen.userPrefFullScr && !Screen.fullScreen)
            Screen.fullScreen = true;
    }
}
