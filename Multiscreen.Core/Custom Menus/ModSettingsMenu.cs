using Analytics;
using Helpers;
using TMPro;
using UI;
using UI.Builder;
using UI.CompanyWindow;
using UI.Menu;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

using Multiscreen.Patches.Menus;
using Multiscreen.Util;
using static Multiscreen.Util.DisplayUtils;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.CustomMenu;

public class ModSettingsMenu : MonoBehaviour
{
    const int SCREEN_LAYOUT_WIDTH = 460;
    const int SCREEN_LAYOUT_HEIGHT = 200;
    const int SCREEN_LAYOUT_PADDING = 10;
    private static readonly string[] DISPLAY_MODES = { "Disabled", "Main Game", "Extra Windows", "Full Screen Map", "CTC" };

    private GameObject contentPanel;
    private UIBuilderAssets assets;
    private static readonly UIState<string> SelectedTab = new("disp");

    private readonly List<DisplayInfo> displays = [];
    private readonly List<int> displayValues = [];

    private readonly List<string> displayModes = [];
    private readonly List<int> displayModeValues = [];

    private readonly Dictionary<int, DisplaySettings> pendingSettings = [];
    private HashSet<int> originalActiveDisplays;
    private int selectedDisplay = 0;

    private int originalMainDisplay = 0;

    private bool originalFocusManager;
    private bool newFocusManager;



    protected void Awake()
    {
        // Get the PreferencesMenu component
        var preferencesMenu = this.GetComponent<PreferencesMenu>();

        // Grab assets and remove Preferences Menu
        assets = this.transform.GetComponent<PreferencesMenu>().panelAssets;
        contentPanel = GameObject.Find("Preferences Menu(Clone)/Content");

        // Clean up UI
        DestroyImmediate(GameObject.Find("Preferences Menu(Clone)/Content/Tab View(Clone)"));
        TextMeshProUGUI title = GetComponentInChildren<TextMeshProUGUI>();
        title.text = "Multiscreen";

        //Destroy(this.GetComponent<PreferencesMenu>());

        // Get all displays connected to the system
        Screen.GetDisplayLayout(displays);
        displayValues.AddRange(displays.Select((DisplayInfo r, int i) => i));

        // Initialise display modes list
        displayModes.AddRange(DISPLAY_MODES);
        displayModeValues.AddRange(displayModes.Select((String r, int i) => i));

        //Get current settings
        LoadSettings();
    }

    protected void Start()
    {
        BuildPanelContent();
    }

    public void BuildPanelContent()
    {
        
        if (contentPanel != null)
        {
            UIPanel.Create(contentPanel.transform.GetComponent<RectTransform>(), assets, delegate (UIPanelBuilder builder)
            {
                Logger.LogTrace("PanelCreate");

                builder.AddTabbedPanels(SelectedTab, BuildTabs);

                builder.AddField("Focus Management", builder.AddToggle(() => DisplayUtils.FocusManagerActive, delegate (bool en)
                {
                    newFocusManager = en;
                    DisplayUtils.EnableDisplayFocusManager(en);
                }));

                builder.AddLabel("Focus management ensures each display is focused when the mouse is over it.");

                builder.Spacer(4f);
                builder.HStack(delegate (UIPanelBuilder builder)
                {
                    builder.AddButton("Back", delegate
                    { 
                        DisplayUtils.EnableDisplayFocusManager(originalFocusManager);

                        // Previews
                        int currentGameDisplay = displays.FindIndex(d => d.Equals(Screen.mainWindowDisplayInfo));

                        for (int i = 0; i < displays.Count; i++)
                        {
                            var originalSettings = DisplayUtils.GetDisplaySettings(i, true);
                            if (originalSettings == null)
                                continue;

                            var dispInfo = DisplayUtils.GetDisplayInfoFromIndex(i);
                            if (dispInfo == null)
                                continue;

                            if (dispInfo.Background != null)
                            {
                                dispInfo.Background.enabled = originalSettings.SolidBg;

                                if (ColorUtility.TryParseHtmlString(originalSettings.BgColour, out Color col))
                                    dispInfo.Background.color = col;
                                else
                                    Logger.LogInfo($"Failed to parse colour {originalSettings.BgColour}");
                            }

                            if (dispInfo.DockContainer != null)
                                WindowUtils.UpdateScale(i, originalSettings.Scale);
                        }

                        MenuManagerPatch._MMinstance.navigationController.Pop();

                    });

                    builder.Spacer().FlexibleWidth(1f);

                    builder.AddButton("Apply", delegate
                    {
                        if (originalFocusManager != newFocusManager)
                            Multiscreen.settings.focusManager = DisplayUtils.FocusManagerActive;

                        // Save new settings
                        Multiscreen.settings.displays = pendingSettings.Values.ToList();
                        Multiscreen.settings.Save(Multiscreen.ModEntry);

                        // Check if main display changed
                        var mainDisplayEntry = pendingSettings.First(x => x.Value.Mode == DisplayMode.Main);
                        int newMainDisplay = mainDisplayEntry.Key;
                        DisplaySettings newMainDisplaySettings = mainDisplayEntry.Value;

                        var newActiveDisplayIndices = pendingSettings.Where(x => x.Value.Mode != DisplayMode.Disabled).Select(x => x.Key);
                        var newActiveDisplays = pendingSettings.Where(x => x.Value.Mode != DisplayMode.Disabled);

                        // Check if any previously active displays are being disabled
                        bool requiresRestart = originalActiveDisplays.Any(d => !newActiveDisplayIndices.Contains(d));// || newActiveDisplays.Any(d => !originalActiveDisplays.Contains(d));

                        // Check if main display changed
                        if (newMainDisplay != originalMainDisplay)
                        {
                            DisplayUtils.MoveAndRestartApplication(newMainDisplaySettings, displays);
                        }
   
                        if (requiresRestart)
                        {
                            DisplayUtils.RestartApplication();
                            return;
                        }

                        // Activate any new displays
                        foreach (var entry in newActiveDisplays)
                        {
                            int displayIndex = entry.Key;
                            var settings = entry.Value;

                            // Skip main display or disabled displays
                            if (displayIndex == 0 || settings.Mode == DisplayMode.Disabled)
                                continue;

                            // Is this a new active display?
                            bool isNewDisplay = !originalActiveDisplays.Contains(displayIndex);

                            if (isNewDisplay)
                            {
                                Logger.LogInfo($"Activating new display {displayIndex} with mode {settings.Mode}");
                                DisplayUtils.ActivateDisplay(displayIndex, settings);
                            }
                            else
                            {
                                // Update existing display settings
                                var displayInfo = DisplayUtils.GetDisplayInfoFromIndex(displayIndex);
                                if (displayInfo != null)
                                {
                                    // Update background settings if available
                                    if (displayInfo.Background != null)
                                    {
                                        displayInfo.Background.enabled = settings.SolidBg;

                                        if (!string.IsNullOrEmpty(settings.BgColour) &&
                                            ColorUtility.TryParseHtmlString(settings.BgColour, out Color col))
                                        {
                                            displayInfo.Background.color = col;
                                        }
                                    }

                                    // Update scale if container exists
                                    if (displayInfo.DockContainer != null)
                                    {
                                        WindowUtils.UpdateScale(displayIndex, settings.Scale);
                                    }
                                }
                            }
                        }

                        MenuManagerPatch._MMinstance.navigationController.Pop();
                    });
                });
            });
        }
    }

    private void BuildTabs(UITabbedPanelBuilder builder)
    {
        builder.AddTab("Display", "disp", BuildDisplayPanel);
        builder.AddTab("Windows", "win", BuildWindowsPanel);
    }

    private void BuildDisplayPanel(UIPanelBuilder builder)
    {
        // Build our physical layout display
        var monitors = DisplayUtils.GetMonitorLayout();
        BuildScreenLayout(builder, monitors);

        // Grab settings for current display
        if (!pendingSettings.TryGetValue(selectedDisplay, out DisplaySettings currentSettings))
        {
            //no settings exist, get display details
            var displayInfo = displays[selectedDisplay];
            var monitor = DisplayUtils.GetWindowsMonitorForUnityDisplay(selectedDisplay);

            currentSettings = new DisplaySettings();

            if (monitor == null)
            {
                Logger.LogInfo($"Warning: Did not find Windows display for Unity display index {selectedDisplay}");
                builder.AddExpandingVerticalSpacer();
                return;
            }

            currentSettings.Name = displayInfo.name;
            currentSettings.DeviceId = monitor.DeviceID;
            pendingSettings[selectedDisplay] = currentSettings;
        }

        var dispInfo = DisplayUtils.GetDisplayInfoFromIndex(selectedDisplay);

        // Add a picker for displays (works with the physical layout)
        builder.AddField("Display",
            builder.AddDropdownIntPicker(
                displayValues,
                selectedDisplay,
                (int i) => (i >= 0) ? $"Display: {i} ({displays[i].name})" : $"00",
                canWrite: true,
                delegate (int i)
                {
                    Logger.LogDebug($"Selected Display: {i}");

                    selectedDisplay = i;
                    builder.Rebuild();
                }
            )
        );


        builder.AddField("Display Mode",
            builder.AddDropdownIntPicker(
                displayModeValues,
                (int)currentSettings.Mode,
                (int i) => (i >= 0) ? displayModes[i] : displayModes[0],
                canWrite: true,
                delegate (int i)
                {
                    Logger.LogDebug($"Selected Mode: {i}, {displayModes[i]}");

                    UpdateDisplayMode(selectedDisplay, (DisplayMode)i);

                    builder.Rebuild();
                }
            )
        );


        builder.AddField("UI Scale",
            builder.AddSlider(() => currentSettings.Scale,
                () => string.Format("{0}%",
                Mathf.Round(currentSettings.Scale * 100f)),
                delegate (float f)
                {
                    currentSettings.Scale = f;
                    if (dispInfo?.DockContainer != null)
                        WindowUtils.UpdateScale(selectedDisplay, f);
                },
                0.2f,
                2f,
                false)
            );


        builder.AddField("Solid Background",
            builder.HStack(delegate (UIPanelBuilder builder)
            {
                // This setting can only be changed when the display is secondary
                var enabled = currentSettings.Mode == DisplayMode.Secondary;

                builder.Spacer(2f);

                var toggle = builder.AddToggle(
                    () => dispInfo?.Background != null ? dispInfo.Background.enabled : currentSettings.SolidBg,
                    isOn =>
                        {
                            currentSettings.SolidBg = isOn;
                            var background = DisplayUtils.GetDisplayInfoFromIndex(selectedDisplay).Background;

                            if (background != null)
                                background.enabled = isOn;
                        }
                    );

            
                toggle.GetComponent<Toggle>().interactable = enabled;

                builder.Spacer(2f);

                var colourPicker = builder.AddColorDropdown(currentSettings.BgColour, colour =>
                {
                    currentSettings.BgColour = colour;

                    var background = dispInfo?.Background;
                    if (background != null && ColorUtility.TryParseHtmlString(colour, out Color newCol))
                        background.color = newCol;
                }
                ).Width(60f);

                colourPicker.GetComponent<DropdownColorPicker>().interactable = enabled;
            })
        );

        builder.AddField("Allow Windows", 
            builder.AddToggle(
                () => currentSettings.AllowWindows,
                isOn =>
                {
                    currentSettings.AllowWindows = isOn;
                }
             )
        );

        builder.AddExpandingVerticalSpacer();
    }

    private void BuildWindowsPanel(UIPanelBuilder builder)
    {

        builder.AddExpandingVerticalSpacer();
    }

    private void BuildScreenLayout(UIPanelBuilder builder, List<MonitorInfo> monitors)
    {
        // Create a container for the layout
        var container = builder.CreateRectView("ScreenLayout", SCREEN_LAYOUT_WIDTH, SCREEN_LAYOUT_HEIGHT);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;

        var spacer = container.gameObject.AddComponent<LayoutElement>();
        spacer.minWidth = SCREEN_LAYOUT_WIDTH;
        spacer.minHeight = SCREEN_LAYOUT_HEIGHT;

        // Get the background image from the dropdown prefab and copy its properties
        var dropdownBackground = assets.dropdownControl.template.GetComponentInChildren<Image>();
        var bgImage = container.gameObject.AddComponent<Image>();
        bgImage.sprite = dropdownBackground.sprite;
        bgImage.type = dropdownBackground.type;
        bgImage.color = dropdownBackground.color; 

        // Create container for screen buttons
        var buttonContainer = new GameObject("ButtonContainer").AddComponent<RectTransform>();
        buttonContainer.SetParent(container, false);
        buttonContainer.anchorMin = Vector2.zero;
        buttonContainer.anchorMax = Vector2.one;
        buttonContainer.offsetMin = new Vector2(SCREEN_LAYOUT_PADDING, SCREEN_LAYOUT_PADDING);
        buttonContainer.offsetMax = new Vector2(-SCREEN_LAYOUT_PADDING, -SCREEN_LAYOUT_PADDING);

        //max size of layout
        int maxX = monitors.Max(m => m.NormalisedBounds.Right);
        int maxY = monitors.Max(m => m.NormalisedBounds.Bottom);

        float usableWidth = SCREEN_LAYOUT_WIDTH - (2 * SCREEN_LAYOUT_PADDING);
        float usableHeight = SCREEN_LAYOUT_HEIGHT - (2 * SCREEN_LAYOUT_PADDING);

        // Calculate the scaling factor for the screen buttons
        float scaleX = (usableWidth) / maxX;
        float scaleY = (usableHeight) / maxY;
        float scale = Mathf.Min(scaleX, scaleY);

        foreach (var monitor in monitors)
        {
            float w = (monitor.NormalisedBounds.Right - monitor.NormalisedBounds.Left) * scale;
            float h = (monitor.NormalisedBounds.Bottom - monitor.NormalisedBounds.Top) * scale;
            float x = monitor.NormalisedBounds.Left * scale;
            float y = monitor.NormalisedBounds.Top * scale;

            var buttonObj = new GameObject($"Monitor_{monitor.Name}");
            buttonObj.transform.SetParent(buttonContainer, false);

            var rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1f);
            rectTransform.anchorMax = new Vector2(0, 1f);
            rectTransform.pivot = new Vector2(0, 1f);
            rectTransform.sizeDelta = new Vector2(w, h);
            rectTransform.anchoredPosition = new Vector2(x, -y);

            var index = monitors.IndexOf(monitor);
            var name = displays[index].name;

            UIPanel.Create(rectTransform, assets, layoutBuilder => {

                layoutBuilder.AddButtonSelectable(
                    $"Display: {index}\n{name}",
                    index == selectedDisplay,
                    () => 
                    {
                        selectedDisplay = monitors.IndexOf(monitor);
                        builder.Rebuild();
                    }).Width(w).Height(h);
            });
        }
    }

    private void ShowRestart()
    {
        EarlyAccessSplash earlyAccessSplash = UnityEngine.Object.FindObjectOfType<EarlyAccessSplash>();
        earlyAccessSplash = UnityEngine.Object.Instantiate<EarlyAccessSplash>(earlyAccessSplash, earlyAccessSplash.transform.parent);

        TextMeshProUGUI text = GameObject.Find("Canvas/EA(Clone)/EA Panel/Scroll View/Viewport/Text").GetComponentInChildren<TextMeshProUGUI>();
        text.text = "\r\n<style=h3>Restart Required!</style>\r\n\r\nPlease restart Railroader.";

        RectTransform rt = GameObject.Find("Canvas/EA(Clone)/EA Panel").transform.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.rect.width, rt.rect.height / 2);


        UnityEngine.Object.DestroyImmediate(GameObject.Find("Canvas/EA(Clone)/EA Panel/Label Regular"));
        //UnityEngine.Object.DestroyImmediate(GameObject.Find("Canvas/EA/EA Panel/Buttons/Opt In"));
        UnityEngine.Object.DestroyImmediate(GameObject.Find("Canvas/EA(Clone)/EA Panel/Buttons/Opt Out"));

        UnityEngine.UI.Button button = GameObject.Find("Canvas/EA(Clone)/EA Panel/Buttons/Opt In").GetComponentInChildren<UnityEngine.UI.Button>();
        button.TMPText().text = "Quit";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate {
            Application.Quit();
        });

        earlyAccessSplash.Show();
    }

    #region Helper Methods

    private void LoadSettings()
    {
        pendingSettings.Clear();

        // Get current display index
        int currentGameDisplay = displays.FindIndex(d => d.Equals(Screen.mainWindowDisplayInfo));

        for (int i = 0; i < displays.Count; i++)
        {
            var settings = DisplayUtils.GetDisplaySettings(i, true) ??
                new DisplaySettings
                {
                    Mode = DisplayMode.Disabled
                };

            pendingSettings[i] = settings.Clone();
        }

        originalActiveDisplays = new(pendingSettings.Where(x => x.Value.Mode != DisplayMode.Disabled).Select(x => x.Key));

        originalMainDisplay = pendingSettings.First(x => x.Value.Mode == DisplayMode.Main).Key;

        originalFocusManager = DisplayUtils.FocusManagerActive;
    }

    private void UpdateDisplayMode(int displayIndex, DisplayMode newMode)
    {
        if (newMode == DisplayMode.Main)
        {
            // Find and update previous main display
            var previousMain = pendingSettings.FirstOrDefault(x => x.Value.Mode == DisplayMode.Main);
            if (previousMain.Value != null)
            {
                previousMain.Value.Mode = DisplayMode.Secondary;
            }
        }
        pendingSettings[displayIndex].Mode = newMode;
    }
    #endregion
}