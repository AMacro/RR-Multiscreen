using Game;
using HarmonyLib;
using Multiscreen.Utils;
using System;
using System.Drawing;
using TMPro;
using UI.Builder;
using UI.CarInspector;
using UI.PreferencesWindow;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.InputSystem.Layouts.InputControlLayout;

namespace Multiscreen.Patches.Misc;

[HarmonyPatch(typeof(SettingsBuilder))]
public static class SettingsBuilderPatch
{
    /*
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SettingsBuilder), nameof(SettingsBuilder.BuildTabs))]
    private static void BuildTabs(UITabbedPanelBuilder builder)
    {

        UITabbedPanelBuilder mstab = builder.AddTab("Multiscreen", "Multiscreen", new Action<UIPanelBuilder>(BuildTabMultiScreen));
        Multiscreen.Log($"Render Width: {mstab._tabView.GetComponentInChildren<TMP_Text>().renderedWidth} Flex Width: {mstab._tabView.GetComponentInChildren<TMP_Text>().flexibleWidth} Pref Width: {mstab._tabView.GetComponentInChildren<TMP_Text>().preferredWidth}");

        Vector2 size = PreferencesWindow.Instance._window.GetContentSize();
        size.x += 100;
        PreferencesWindow.Instance._window.SetContentSize(size);

        Multiscreen.Log($"Render Width: {mstab._tabView.GetComponentInChildren<TMP_Text>().renderedWidth} Flex Width: {mstab._tabView.GetComponentInChildren<TMP_Text>().flexibleWidth} Pref Width: {mstab._tabView.GetComponentInChildren<TMP_Text>().preferredWidth}");
    }*/
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SettingsBuilder), nameof(SettingsBuilder.BuildTabGraphics))]
    private static void BuildTabGraphics(UIPanelBuilder builder)
    {
        //Canvas - Modals/UI.PreferencesWindow.PreferencesWindow/Content/Tab View(Clone)/Content Holder/Content/Field Row(Clone)
        //Canvas/Navigation Controller/Settings Menu(Clone)/Content/Tab View(Clone)/Content Holder/Content
        //Canvas/Navigation Controller/Settings Menu(Clone)/Content/Tab View(Clone)/Content Holder/Content/Field

        //find vanilla UI slider
        TMP_Text[] children;
        children = builder._container.transform.GetComponentsInChildren<TMP_Text>();

        Transform sibling = null;

        Multiscreen.Log($"Child count: {children.Length}");
        for (int i = 0;  i < children.Length; i++)
        {
            //Multiscreen.Log($"Child name: {children[i].text}");
            if (children[i].text == "UI Scale")
            {
                sibling = children[i].transform.parent;
                //Multiscreen.Log($"Sibling: {sibling.name} Index: {sibling.GetSiblingIndex()}" );
            }
        }
        
        //add our custom slider
        IConfigurableElement slider = builder.AddField("UI Scale (Second Display)", builder.AddSlider(() => Multiscreen.Settings.secondDisplayScale, () => string.Format("{0}%", Mathf.Round(Multiscreen.Settings.secondDisplayScale * 100f)),
                   delegate (float f)
                   {
                       Multiscreen.Settings.secondDisplayScale = f;
                       WindowUtils.UpdateScale();
                   },
                   0.2f, 2f, false));
        slider.RectTransform.name = "Multiscreen UI Scale";

        //position beneath vanilla slider
        if (sibling != null)
        {
            Multiscreen.Log($"slider rect: {slider.RectTransform.name} Index: {slider.RectTransform.GetSiblingIndex()}");
            slider.RectTransform.SetSiblingIndex(sibling.GetSiblingIndex()+1);
        }
        

        //expand window if using preferences in game
        if (PreferencesWindow.Instance != null)
        {

            Vector2 size = PreferencesWindow.Instance._window.GetContentSize();
            Multiscreen.Log($"Window Size: {size} Slider Size: {slider.RectTransform.transform.GetComponent<RectTransform>().rect.height}");
            size.y += slider.RectTransform.GetComponent<LayoutElement>().minHeight;// slider.RectTransform.rect.height;
  
            PreferencesWindow.Instance._window.SetContentSize(size);
            Multiscreen.Log($"Window Size: {size} Slider Size: {slider.RectTransform.parent}");
        }
    }

    /*
    private static void BuildTabMultiScreen(UIPanelBuilder builder)
    {
        builder.AddSection("UI");

        
        /*SettingsBuilder._uiScaleRectTransform = *//*builder.AddField("UI Scale",
            builder.AddSlider(() => Multiscreen.Settings.secondDisplayScale, () => string.Format("{0}%", Mathf.Round(Multiscreen.Settings.secondDisplayScale * 100f)),
            delegate (float f)
                {
                    Multiscreen.Settings.secondDisplayScale = f;
                    WindowUtils.UpdateScale();
                },
            0.2f, 2f, false));


        builder.AddExpandingVerticalSpacer();

    }
    */

}
