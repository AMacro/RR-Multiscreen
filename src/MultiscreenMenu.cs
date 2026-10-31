// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Modified in 2026 for the Railroader 0.6 port; see NOTICE.

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Multiscreen.Patches.Misc;
using Multiscreen.Util;
using UI.Builder;
using UI.Common;
using UI.CompanyWindow;
using UI.Menu;
using UnityEngine;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.CustomMenu;

// Main menu > Multiscreen: pick the game and second displays, the second display's UI scale and background,
// and focus management. Back reverts what was previewed; Apply keeps and saves it.
public class MultiscreenMenu : BuilderMenuBase<MultiscreenMenu>
{
    private int newGameDisplay;
    private int newSecondDisplay;
    private bool oldFocusManager;
    private bool oldSolidBg;
    private Color oldColour;
    private float oldScale;
    private float newScale;
    private bool captured;
    private bool rebuildPending;

    protected override Vector2 PreferredSize => new Vector2(560f, 640f);

    protected override string PanelTitle => "Multiscreen";

    protected override string BackButtonTitle => "Back";

    protected override string ForwardButtonTitle => "Apply";

    protected override void Awake()
    {
        base.Awake();
        Capture();
    }

    private void Capture()
    {
        if (captured)
            return;
        captured = true;

        newGameDisplay = Multiscreen.gameDisplay;
        newSecondDisplay = Multiscreen.secondDisplay;
        oldFocusManager = Multiscreen.focusManager;
        oldSolidBg = Multiscreen.background != null && Multiscreen.background.enabled;
        oldColour = Multiscreen.background != null ? Multiscreen.background.color : Color.black;
        oldScale = Multiscreen.settings.secondDisplayScale;
        newScale = oldScale;
    }

    protected override void BuildPanelContent(UIPanelBuilder builder)
    {
        Capture();

        if (!Multiscreen.IsActive)
        {
            builder.AddLabel("Multiscreen found only one display, so it is not active.\n\nConnect a second monitor in extended mode and restart Railroader.");
            builder.AddExpandingVerticalSpacer();
            return;
        }

        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);
        List<int> values = displays.Select((_, i) => i).ToList();
        string Name(int i) => i >= 0 && i < displays.Count ? $"Display {i} ({displays[i].name})" : "Not set";

        builder.AddSection("Game Display");
        builder.AddField("Display", builder.AddDropdownIntPicker(values, newGameDisplay, Name, canWrite: true, i =>
        {
            Logger.LogDebug($"Selected game display: {i}");
            if (i == newSecondDisplay)
                newSecondDisplay = newGameDisplay;
            newGameDisplay = i;
            rebuildPending = true;
        }));

        builder.AddSection("Second Display");
        builder.AddField("Display", builder.AddDropdownIntPicker(values, newSecondDisplay, Name, canWrite: true, i =>
        {
            Logger.LogDebug($"Selected second display: {i}");
            if (i != 0)
            {
                if (i == newGameDisplay)
                    newGameDisplay = newSecondDisplay;
                newSecondDisplay = i;
            }
            rebuildPending = true;
        }));
        builder.AddLabel("Display 0 can't be the second display (a Unity engine limitation). On Windows, making another monitor your primary display in the system display settings works around it.");

        builder.Spacer().Height(20f);

        builder.AddField("UI Scale", builder.AddSlider(() => newScale, () => $"{Mathf.Round(newScale * 100f)}%", f =>
        {
            newScale = f;
            WindowUtils.UpdateScale(newScale);
        }, 0.2f, 2f, false));

        builder.AddField("Solid Background", builder.HStack(row =>
        {
            row.AddToggle(() => Multiscreen.background.enabled, isOn => Multiscreen.background.enabled = isOn);

            row.Spacer(2f);

            List<string> colours = SettingsBuilderPatch.PaletteWith(ColorHelper.HexFromColor(Multiscreen.background.color), out int selected);
            row.AddColorDropdown(colours, selected, index =>
            {
                if (index >= 0 && index < colours.Count && Multiscreen.TryParseColour(colours[index], out Color colour))
                    Multiscreen.background.color = colour;
            }).Width(60f);
        }));

        builder.AddField("Focus Management", builder.AddToggle(() => Multiscreen.focusManager, en => Multiscreen.EnableDisplayFocusManager(en)));
        builder.AddLabel("Focus management gives each display focus when the mouse moves onto it.");

        builder.AddExpandingVerticalSpacer();
    }

    // Rebuild a frame after a dropdown pick, not inside the dropdown's own callback
    private void Update()
    {
        if (!rebuildPending)
            return;
        rebuildPending = false;
        RebuildPanel();
    }

    // Back: undo the live previews
    protected override void PerformBackAction()
    {
        if (Multiscreen.IsActive)
        {
            Multiscreen.EnableDisplayFocusManager(oldFocusManager);
            Multiscreen.background.enabled = oldSolidBg;
            Multiscreen.background.color = oldColour;
            Multiscreen.settings.secondDisplayScale = oldScale;
            WindowUtils.UpdateScale(oldScale);
        }

        base.PerformBackAction();
    }

    protected override void PerformForwardAction()
    {
        if (!Multiscreen.IsActive)
        {
            base.PerformBackAction();
            return;
        }

        Settings settings = Multiscreen.settings;
        settings.focusManager = Multiscreen.focusManager;
        settings.solidBG = Multiscreen.background.enabled;
        settings.bgColour = ColorHelper.HexFromColor(Multiscreen.background.color).TrimStart('#');
        settings.secondDisplayScale = newScale;

        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);

        if (Multiscreen.gameDisplay != newGameDisplay)
        {
            Multiscreen.gameDisplay = newGameDisplay;
            settings.gameDisplay = newGameDisplay;
            if (newGameDisplay >= 0 && newGameDisplay < displays.Count)
                Screen.MoveMainWindowTo(displays[newGameDisplay], Vector2Int.zero);
        }

        bool restart = false;
        if (Multiscreen.secondDisplay != newSecondDisplay)
        {
            Multiscreen.secondDisplay = newSecondDisplay;
            settings.secondDisplay = newSecondDisplay;
            restart = true;
        }

        settings.Save();

        // Capture the applied values so a later Back from this same panel reverts to them
        captured = false;
        Capture();

        base.PerformBackAction();

        if (restart)
            ShowRestart();
    }

    private static void ShowRestart()
    {
        ModalAlertController.Present("Restart Required",
            "Moving the second display to a different monitor needs a game restart (a Unity engine limitation).",
            new List<(bool, string)> { (true, "Quit"), (false, "Later") },
            quit =>
            {
                if (quit)
                    Application.Quit();
            });
    }
}
