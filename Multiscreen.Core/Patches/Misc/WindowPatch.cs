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
        __instance.SetupWindow();
    }

}
