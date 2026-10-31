using System.Collections.Generic;
using HarmonyLib;
using Helpers;
using Multiscreen.Util;
using UI.Builder;
using UI.PreferencesWindow;
using UnityEngine;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Misc;

// Adds "UI Scale (Second Display)" and "Solid Background" under the game's UI Scale on the Graphics tab
// (both the in-game Preferences window and the main menu Preferences use this builder).
[HarmonyPatch(typeof(PreferencesBuilder))]
public static class SettingsBuilderPatch
{
    // Offered in the background colour dropdown; the saved colour is added if it isn't one of these.
    internal static readonly string[] Palette =
    {
        "#000000", "#1A1A1A", "#333333", "#4D4D4D", "#2B3A4A", "#1F3B2C", "#3B2A1F", "#B4A790", "#FFFFFF"
    };

    [HarmonyPostfix]
    [HarmonyPatch("BuildTabGraphics")]
    private static void BuildTabGraphics(UIPanelBuilder builder, IConfigurableElement ____uiScaleRectTransform)
    {
        if (!Multiscreen.IsActive || Multiscreen.background == null)
            return;

        IConfigurableElement slider = builder.AddField("UI Scale (Second Display)", builder.AddSlider(
            () => Multiscreen.settings.secondDisplayScale,
            () => $"{Mathf.Round(Multiscreen.settings.secondDisplayScale * 100f)}%",
            f =>
            {
                Multiscreen.settings.secondDisplayScale = f;
                WindowUtils.UpdateScale(f);
            },
            0.2f, 2f, false,
            _ => Multiscreen.settings.Save()));
        slider.RectTransform.name = "Multiscreen UI Scale";

        IConfigurableElement bgSet = builder.AddField("Solid Background", builder.HStack(row =>
        {
            row.AddToggle(() => Multiscreen.settings.solidBG, isOn =>
            {
                Multiscreen.settings.solidBG = isOn;
                Multiscreen.background.enabled = isOn;
                Multiscreen.settings.Save();
            });

            row.Spacer(2f);

            List<string> colours = PaletteWith(Multiscreen.settings.bgColour, out int selected);
            row.AddColorDropdown(colours, selected, index =>
            {
                if (index < 0 || index >= colours.Count || !Multiscreen.TryParseColour(colours[index], out Color colour))
                    return;
                Multiscreen.settings.bgColour = colours[index].TrimStart('#');
                Multiscreen.background.color = colour;
                Multiscreen.settings.Save();
            }).Width(60f);
        }));

        // Sit right under the game's own UI Scale row when we can find it
        RectTransform uiScaleRow = ____uiScaleRectTransform?.RectTransform;
        if (uiScaleRow != null && uiScaleRow.parent == slider.RectTransform.parent)
        {
            slider.RectTransform.SetSiblingIndex(uiScaleRow.GetSiblingIndex() + 1);
            bgSet.RectTransform.SetSiblingIndex(slider.RectTransform.GetSiblingIndex() + 1);
        }
        else
        {
            Logger.LogDebug("BuildTabGraphics(): UI Scale row not found, our rows stay at the end");
        }
    }

    internal static List<string> PaletteWith(string current, out int selected)
    {
        List<string> colours = new List<string>(Palette);
        string hex = Multiscreen.TryParseColour(current, out Color c) ? ColorHelper.HexFromColor(c) : "#000000";
        selected = colours.FindIndex(x => string.Equals(x, hex, System.StringComparison.OrdinalIgnoreCase));
        if (selected < 0)
        {
            colours.Insert(0, hex);
            selected = 0;
        }
        return colours;
    }
}
