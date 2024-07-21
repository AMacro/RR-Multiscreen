using HarmonyLib;
using Multiscreen.Util;
using UnityEngine;
using UnityEngine.UI;
using UI.Common;

using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UI.Builder;

using Logger = Multiscreen.Util.Logger;
using System.Collections.Generic;
using UI;
using System.Runtime.CompilerServices;
using TMPro;

namespace Multiscreen.Patches.Misc;

[HarmonyPatch(typeof(Window))]
public class WindowPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Window), nameof(Window.OnPointerDown))]
    private static void OnPointerDown(Window __instance, PointerEventData eventData)
    {
        //force Input to be refreshed
        Keyboard.current.altKey.ReadValue();

        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            //__instance.ToggleDisplay();

            int display = __instance.GetDisplayForWindow();
            TMP_Dropdown? dropdown = __instance.transform.Find("Chrome/Title Bar/Screen Selector")?.gameObject.GetComponentInChildren<TMP_Dropdown>();

            display++;
            if(display >1) { display=0; }

            if (dropdown != null)
            {
                Logger.LogDebug("Found TMP_Dropdown");
                dropdown.value = display;
                dropdown.RefreshShownValue();
            }
            else
            {
                Logger.LogDebug("Didn't find TMP_Dropdown");
            }
        }

    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Window), nameof(Window.Start))]
    private static void Start(Window __instance) 
    {
        //find the title bar
        GameObject tb = __instance.transform.Find("Chrome/Title Bar").gameObject;

        if (tb != null)
        {
            //create a container for our dropdown
            GameObject goSelector = new GameObject("Screen Selector"/* typeof(RectTransform)*/);
            RectTransform rtSelector = goSelector.AddComponent<RectTransform>();
            goSelector.transform.SetParent(tb.transform, false);

            //place and size our dropdown
            rtSelector.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, -2, 20);
            rtSelector.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 10, 40);

            //Create a UI panel and build the dropdown inside it
            UIPanel.Create(rtSelector, GameObject.FindObjectOfType<ProgrammaticWindowCreator>().builderAssets, delegate (UIPanelBuilder builder) {

                Logger.LogDebug("Getting current display for selector...");
                int currentDisp = __instance.GetDisplayForWindow();
                Logger.LogDebug($"Display found: {currentDisp}");
                RectTransform dropdown = null;
                dropdown = builder.AddDropdown(new List<string>() { "0", "1"}, currentDisp, delegate (int index)
                {
                    Logger.LogInfo($"Window {__instance.name} selected index: {index}");

                    if (index == 0)
                    {
                        __instance.SetDisplay(false);
                    }
                    else
                    {
                        __instance.SetDisplay(true);
                    }

                    Window win = __instance.GetComponentInChildren<Window>();
                    win.ClampToParentBounds();
                });

                //Disable the background image, looks nicer on the title bar
                dropdown.Find("Background Image").gameObject.SetActive(false);
                //dropdown.Find("Arrow").gameObject.SetActive(false);

            });
        }
        else
        {
            Logger.LogVerbose($"Unable to Locate TB {tb?.name}");
        }
    }

}
