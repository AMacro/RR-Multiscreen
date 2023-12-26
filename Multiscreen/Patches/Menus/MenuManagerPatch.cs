using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UI.Menu;
using Multiscreen.Custom_Window;
namespace Multiscreen.Patches.Menus;

[HarmonyPatch(typeof(MainMenu))]
public static class MainMenu_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Awake))]
    private static bool Awake(MainMenu __instance)
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