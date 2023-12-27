using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UI.Menu;
using Multiscreen.Custom_Window;
namespace Multiscreen.Patches.Menus;
/*
[HarmonyPatch(typeof(MainMenu))]
public static class MainMenu_Patch
{
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Awake))]
    private static bool Awake(MainMenu __instance)
    {
        

        }
        return true;

    }
}

/*
[HarmonyPatch(typeof(MenuManager))]
public static class MenuManager_Patch
{
    public static CreditsMenu mss;
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.MakeMainMenu))]
    private static void MakeMainMenu(MenuManager __instance)
    {
        mss = UnityEngine.Object.Instantiate<CreditsMenu>(new CreditsMenu());
    }
    
    }
*/