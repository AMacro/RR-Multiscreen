
using HarmonyLib;
using Multiscreen.Util;
using TMPro;
using UI.Builder;
using UI.PreferencesWindow;
using UnityEngine;
using UnityEngine.UI;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Misc;

[HarmonyPatch(typeof(PreferencesBuilder))]
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
    [HarmonyPatch(typeof(PreferencesBuilder), nameof(PreferencesBuilder.BuildTabGraphics))]
    private static void BuildTabGraphics(UIPanelBuilder builder)
    {
        //Canvas - Modals/UI.PreferencesWindow.PreferencesWindow/Content/Tab View(Clone)/Content Holder/Content/Field Row(Clone)
        //Canvas/Navigation Controller/Settings Menu(Clone)/Content/Tab View(Clone)/Content Holder/Content
        //Canvas/Navigation Controller/Settings Menu(Clone)/Content/Tab View(Clone)/Content Holder/Content/Field

        //find vanilla UI slider
        TMP_Text[] children;
        children = builder._container.transform.GetComponentsInChildren<TMP_Text>();

        Transform sibling = null;

        Logger.LogVerbose($"Child count: {children.Length}");
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
        IConfigurableElement slider = builder.AddField("UI Scale (Second Display)", builder.AddSlider(() => Multiscreen.settings.secondDisplayScale, () => string.Format("{0}%", Mathf.Round(Multiscreen.settings.secondDisplayScale * 100f)),
                   delegate (float f)
                   {
                       Multiscreen.settings.secondDisplayScale = f;
                       WindowUtils.UpdateScale(f);
                   },
                   0.2f, 2f, false));
        slider.RectTransform.name = "Multiscreen UI Scale";

        //Add our secondary display BG settings
        IConfigurableElement bgSet = builder.AddField("Solid Background", builder.HStack(delegate (UIPanelBuilder builder)
        {
            builder.AddToggle(() => Multiscreen.settings.solidBG, isOn =>
            {
                Multiscreen.settings.solidBG = isOn;
                Multiscreen.background.enabled = isOn;
                builder.Rebuild();
            });

            builder.Spacer(2f);
            Logger.LogDebug($"BuildTabGraphics() Colour");
            builder.AddColorDropdown(Multiscreen.settings.bgColour, colour =>
            {
                UnityEngine.Color temp = Multiscreen.background.color;
                UnityEngine.Color newCol;
                if (ColorUtility.TryParseHtmlString(colour, out newCol))
                {
                    Multiscreen.settings.bgColour = colour;
                    Multiscreen.background.color = newCol;
                }
                else
                {
                    Multiscreen.background.color = temp;
                }
                Logger.LogDebug(colour);
            }
            ).Width(60f);
        }));

        Logger.LogDebug($"BuildTabGraphics() Positioning");
        //position beneath vanilla slider
        if (sibling != null)
        {
            Logger.LogDebug($"slider rect: {slider.RectTransform.name} Index: {slider.RectTransform.GetSiblingIndex()}");
            slider.RectTransform.SetSiblingIndex(sibling.GetSiblingIndex()+1);
            bgSet.RectTransform.SetSiblingIndex(slider.RectTransform.GetSiblingIndex() + 1);
        }

        Logger.LogDebug($"BuildTabGraphics() Preferences");
        //expand window if using preferences in game
        if (PreferencesWindow.Instance != null)
        {
            Logger.LogDebug($"BuildTabGraphics() Preferences !null");
            Vector2 size = PreferencesWindow.Instance._window.GetContentSize();
            Logger.LogDebug($"Window Size: {size} Slider Size: {slider.RectTransform.transform.GetComponent<RectTransform>().rect.height}");
            size.y += slider.RectTransform.GetComponent<LayoutElement>().minHeight;// slider.RectTransform.rect.height;
  
            PreferencesWindow.Instance._window.SetContentSize(size);
            Logger.LogDebug($"Window Size: {size} Slider Size: {slider.RectTransform.parent}");
        }
        else
        {
            Logger.LogDebug($"BuildTabGraphics() Preferences is null");
        }
    }

    /*
    private static void BuildTabMultiScreen(UIPanelBuilder builder)
    {
        builder.AddSection("UI");

        
        /*SettingsBuilder._uiScaleRectTransform = *//*builder.AddField("UI Scale",
            builder.AddSlider(() => Multiscreen.settings.secondDisplayScale, () => string.Format("{0}%", Mathf.Round(Multiscreen.settings.secondDisplayScale * 100f)),
            delegate (float f)
                {
                    Multiscreen.settings.secondDisplayScale = f;
                    WindowUtils.UpdateScale();
                },
            0.2f, 2f, false));


        builder.AddExpandingVerticalSpacer();

    }
    */

}
