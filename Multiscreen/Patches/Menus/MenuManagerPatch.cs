using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;
using UI.Menu;
using TMPro;
using Multiscreen.CustomMenu;
using Multiscreen.Util;
using Logger = Multiscreen.Util.Logger;
using System.Collections;

namespace Multiscreen.Patches.Menus;

[HarmonyPatch(typeof(MainMenu))]
public static class MenuManagerPatch
{
    public static MainMenu _instance;
    public static MenuManager _MMinstance;
    public static PreferencesMenu ModMenu;
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Awake))]
    private static void Awake(MainMenu __instance)
    {
        Logger.LogVerbose("MainMenu.Start()");

        _instance = __instance;

        _instance.AddButton("Multiscreen Mod",onClick);

        Button[] buttons = _instance.GetComponentsInChildren<Button>();

        int insertindex = 0;
        Button modSettings = null;
        foreach (Button button in buttons)
        {
            if(button.GetComponentInChildren<TMP_Text>()?.text == "Preferences" )
                insertindex= button.transform.GetSiblingIndex() + 1;

            if (button.GetComponentInChildren<TMP_Text>()?.text == "Multiscreen Mod")
                modSettings = button;
        }

        if (modSettings != null)
        {
            modSettings.transform.SetSiblingIndex(insertindex);
        }
  

        //Multiscreen.Log($"My Index: {modSettings.transform.GetSiblingIndex()}");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
    private static void Start(MenuManager __instance)
    {
        Logger.LogTrace("MenuManager.Start()");

        _MMinstance = __instance;

        __instance.StartCoroutine(WaitForLoad());

        /*if ( Multiscreen.userPrefFullScr)
            Screen.fullScreen = true;*/
    }

    private static IEnumerator WaitForLoad()
    {
        yield return new WaitForSecondsRealtime(2f);
        if(Multiscreen.userPrefFullScr)
            Screen.fullScreen = true;

    }

    private static void onClick()
    {
        if (_MMinstance != null)
        {
            ModMenu = UnityEngine.Object.Instantiate<PreferencesMenu>(_MMinstance.preferencesMenu);
            ModMenu.transform.gameObject.AddComponent<ModSettingsMenu>();
            _MMinstance.navigationController.Push(ModMenu);
           
        }
    }
}

