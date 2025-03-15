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
using Logger = Multiscreen.Util.Logger;
using Multiscreen.Patches.Windows;
using static Multiscreen.DisplaySettings;
using MonoMod.Utils;

namespace Multiscreen.CustomMenu;

public class ModSettingsMenu : MonoBehaviour
{
    #region Constants
    const int SCREEN_LAYOUT_WIDTH = 460;
    const int SCREEN_LAYOUT_HEIGHT = 200;
    const int SCREEN_LAYOUT_PADDING = 10;
    #endregion

    #region Fields

    // Caches
    private readonly List<string> windowPositions = [];
    private readonly List<int> windowPositionValues = [];

    private readonly List<string> displayModes = [];
    private readonly List<int> displayModeValues = [];

    private readonly List<DisplayInfo> displays = [];
    private readonly List<int> displayValues = [];

    private readonly List<string> windowNames = [];

    private readonly Dictionary<int, DisplaySettings> pendingDisplaySettings = [];
    private readonly List<WindowSettings> pendingWindowSettings = [];

    // UI
    private GameObject contentPanel;
    private UIBuilderAssets assets;
    private UIPanelBuilder buttonsBuilder;
    private static readonly UIState<string> SelectedTab = new("disp");

    private HashSet<int> originalActiveDisplays;
    private int selectedDisplay = 0;

    private int originalMainDisplay = 0;

    private bool originalFocusManager;
    private bool newFocusManager;
    #endregion

    #region Initialization

    protected void Awake()
    {
        InitializeUIComponents();
        LoadDisplayInformation();
    }

    protected void Start()
    {
        BuildPanelContent();
    }

    protected void OnEnable()
    {
        LoadSettings();
    }

    private void InitializeUIComponents()
    {
        // Grab assets and remove Preferences Menu
        assets = this.transform.GetComponent<PreferencesMenu>().panelAssets;
        contentPanel = GameObject.Find("Preferences Menu(Clone)/Content");

        // Clean up UI
        DestroyImmediate(GameObject.Find("Preferences Menu(Clone)/Content/Tab View(Clone)"));
        TextMeshProUGUI title = GetComponentInChildren<TextMeshProUGUI>();
        title.text = "Multiscreen";

        // Build static list caches
        var names = Enum.GetNames(typeof(DisplaySettings.DisplayModes))
                        .Select(name => name.SplitCamelCase())
                        .ToList();
        displayModes.AddRange(names);
        displayModeValues.AddRange(displayModes.Select((String r, int i) => i));


        names = Enum.GetNames(typeof(WindowSettings.Positions))
                        .Select(name => name.SplitCamelCase())
                        .ToList();

        windowPositions.AddRange(names);
        windowPositionValues.AddRange(windowPositions.Select((String r, int i) => i));


        names = GenericWindowInstanceHelper.GetWindowTypesToPatch()
                        .Select(t => t.Name.Split('.').Last().Replace("Window", "").Replace("Panel", "").SplitCamelCase())
                        .OrderBy(t => t)
                        .ToList();

        windowNames.AddRange(names);

        Logger.LogDebug($"Window names: {string.Join(", ", windowNames)}");

    }

    private void LoadDisplayInformation()
    {
        // Get all displays connected to the system
        Screen.GetDisplayLayout(displays);
        displayValues.AddRange(displays.Select((DisplayInfo r, int i) => i));
    }
    #endregion

    #region UI Building
    public void BuildPanelContent()
    {
        if (contentPanel == null)
            return;

        UIPanel.Create(contentPanel.transform.GetComponent<RectTransform>(), assets, delegate (UIPanelBuilder builder)
        {
            Logger.LogTrace("PanelCreate");

            builder.AddTabbedPanels(SelectedTab, BuildTabs);
            BuildButtonSection(builder);
        });
    }

    private void BuildTabs(UITabbedPanelBuilder builder)
    {
        builder.AddTab("Display", "disp", BuildDisplayPanel);
        builder.AddTab("Windows", "win", BuildWindowsPanel);
    }

    #region Display Panel
    private void BuildDisplayPanel(UIPanelBuilder builder)
    {
        // Build physical monitor layout
        var monitors = DisplayUtils.GetMonitorLayout();
        BuildScreenLayout(builder, monitors);

        // Get or create settings for current display
        EnsureDisplaySettingsExist();
        var currentSettings = pendingDisplaySettings[selectedDisplay];
        var dispInfo = DisplayUtils.GetDisplayInfoFromIndex(selectedDisplay);

        // Build display selection dropdown
        BuildDisplaySelector(builder);

        // Build mode selector
        BuildModeSelector(builder, currentSettings);

        // Build UI scale slider
        BuildScaleSlider(builder, currentSettings, dispInfo);

        // Build background settings
        BuildBackgroundSettings(builder, currentSettings, dispInfo);

        // Build window allowed toggle
        BuildWindowsAllowedToggle(builder, currentSettings);

        builder.AddExpandingVerticalSpacer();
        BuildFocusManagementSection(builder);
    }

    #region Screen Layout Graphic
    private void BuildScreenLayout(UIPanelBuilder builder, List<MonitorInfo> monitors)
    {
        // Create a container for the layout
        var container = builder.CreateRectView("ScreenLayout", SCREEN_LAYOUT_WIDTH, SCREEN_LAYOUT_HEIGHT);
        SetupScreenLayoutContainer(container);

        var buttonContainer = CreateButtonContainer(container);
        RenderMonitorButtons(buttonContainer, monitors, builder);
    }

    private void SetupScreenLayoutContainer(RectTransform container)
    {
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
    }

    private RectTransform CreateButtonContainer(RectTransform container)
    {
        var buttonContainer = new GameObject("ButtonContainer").AddComponent<RectTransform>();
        buttonContainer.SetParent(container, false);
        buttonContainer.anchorMin = Vector2.zero;
        buttonContainer.anchorMax = Vector2.one;
        buttonContainer.offsetMin = new Vector2(SCREEN_LAYOUT_PADDING, SCREEN_LAYOUT_PADDING);
        buttonContainer.offsetMax = new Vector2(-SCREEN_LAYOUT_PADDING, -SCREEN_LAYOUT_PADDING);
        return buttonContainer;
    }

    private void RenderMonitorButtons(RectTransform buttonContainer, List<MonitorInfo> monitors, UIPanelBuilder builder)
    {
        // Calculate layout parameters
        int maxX = monitors.Max(m => m.NormalisedBounds.Right);
        int maxY = monitors.Max(m => m.NormalisedBounds.Bottom);

        float usableWidth = SCREEN_LAYOUT_WIDTH - (2 * SCREEN_LAYOUT_PADDING);
        float usableHeight = SCREEN_LAYOUT_HEIGHT - (2 * SCREEN_LAYOUT_PADDING);

        // Calculate the scaling factor
        float scaleX = (usableWidth) / maxX;
        float scaleY = (usableHeight) / maxY;
        float scale = Mathf.Min(scaleX, scaleY);

        // Create a button for each monitor
        foreach (var monitor in monitors)
        {
            CreateMonitorButton(buttonContainer, monitor, monitors, scale, builder);
        }
    }

    private void CreateMonitorButton(RectTransform buttonContainer, MonitorInfo monitor, List<MonitorInfo> monitors, float scale, UIPanelBuilder builder)
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

        UIPanel.Create(rectTransform, assets, layoutBuilder =>
        {
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
    #endregion

    private void BuildDisplaySelector(UIPanelBuilder builder)
    {
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
    }

    private void BuildModeSelector(UIPanelBuilder builder, DisplaySettings currentSettings)
    {
        // Get available display modes for the current display
        List<int> availableModes = GetAvailableModesForDisplay(selectedDisplay);
        TMP_Dropdown dropdown;

        var dropdownRect =
            builder.AddDropdownIntPicker(
                displayModeValues,
                (int)currentSettings.Mode,
                (int i) => (i >= 0) ? displayModes[i] : displayModes[0],
                canWrite: true,
                delegate (int i)
                {
                    bool allowed = availableModes.Contains(i);
                    Logger.LogDebug($"Selected Mode: {i} \'{displayModes[i]}\', mode allowed: {allowed}");

                    // Only allow selecting available modes
                    if (allowed)
                    {
                        UpdateDisplayMode(selectedDisplay, (DisplayModes)i);
                        builder.Rebuild();
                        buttonsBuilder.Rebuild();
                    }
                }
            );

        builder.AddField("Display Mode", dropdownRect);

        dropdown = dropdownRect.GetComponent<TMP_Dropdown>();


        if (dropdown != null)
        {
            // Apply visual indication for disabled options
            for (int i = 0; i < dropdown.options.Count; i++)
            {
                // If this mode isn't available for this display, mark it as disabled
                if (!availableModes.Contains(i))
                {
                    var option = dropdown.options[i];
                    option.text = $"<color=#888888>{option.text}</color>";
                    dropdown.options[i] = option;
                }
            }

            // Force refresh the dropdown
            dropdown.RefreshShownValue();

            // Add a custom handler to prevent selecting disabled options
            dropdown.onValueChanged.AddListener(value =>
            {
                if (!availableModes.Contains(value))
                {
                    // Reset to current value if user tries to select a disabled option
                    dropdown.value = (int)currentSettings.Mode;
                    dropdown.RefreshShownValue();
                }
            });
        }
    }

    private void BuildScaleSlider(UIPanelBuilder builder, DisplaySettings currentSettings, ActiveDisplayInfo dispInfo)
    {
        builder.AddField("UI Scale",
            builder.AddSlider(() => currentSettings.Scale,
                () => string.Format("{0}%", Mathf.Round(currentSettings.Scale * 100f)),
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
    }

    private void BuildBackgroundSettings(UIPanelBuilder builder, DisplaySettings currentSettings, ActiveDisplayInfo dispInfo)
    {
        builder.AddExpandingVerticalSpacer();

        builder.AddField("Solid Background",
            builder.HStack(delegate (UIPanelBuilder builder)
            {
                // This setting can only be changed when the display is secondary
                var enabled = currentSettings.Mode == DisplayModes.ExtraWindows;

                var toggle = builder.AddToggle(
                    () => dispInfo?.Background != null ? dispInfo.Background.enabled : currentSettings.SolidBg,
                    isOn =>
                    {
                        currentSettings.SolidBg = isOn;
                        var background = DisplayUtils.GetDisplayInfoFromIndex(selectedDisplay)?.Background;
                        if (background != null)
                            background.enabled = isOn;
                    },
                    enabled
                    );


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
    }

    private void BuildWindowsAllowedToggle(UIPanelBuilder builder, DisplaySettings currentSettings)
    {
        builder.AddField("Allow Windows",
            builder.AddToggle(
                () => currentSettings.AllowWindows,
                isOn => currentSettings.AllowWindows = isOn
            )
        );
    }

    private void BuildFocusManagementSection(UIPanelBuilder builder)
    {
        builder.AddField("Focus Management", builder.AddToggle(() => DisplayUtils.FocusManagerActive, delegate (bool en)
        {
            newFocusManager = en;
            DisplayUtils.EnableDisplayFocusManager(en);
        }));

        builder.AddLabel("Focus management ensures each display is focused when the mouse is over it.");
        builder.Spacer(4f);
    }
    #endregion

    #region Windows Panel
    private void BuildWindowsPanel(UIPanelBuilder builder)
    {
        builder.VScrollView((builder) =>
        {
            foreach (var windowName in windowNames)
            {
                var winSettings = GetWindowSettings(windowName);

                builder.AddSection(windowName);

                // Get display options
                var allowedDisplays = GetDisplaysAllowingWindows();

                // Get current display index for this window
                int currentDisplayIndex = GetDisplayIndexForDeviceId(winSettings.DeviceId);

                var dropdownRect = builder.AddDropdownIntPicker
                    (
                        displayValues,
                        currentDisplayIndex,
                        (int i) =>
                        {
                            pendingDisplaySettings.TryGetValue(i, out var settings);

                            string mode;
                            if (!settings.AllowWindows)
                                mode = "No Windows";
                            else
                                mode = settings.Mode.ToString().SplitCamelCase();

                            return $"Display: {i} ({displays[i].name}) ({mode})";
                        },
                        true,
                        (int i) =>
                        {
                            // Only allow selecting if display isn't disabled
                            pendingDisplaySettings.TryGetValue(i, out var displaySetting);
                            bool isDisabled = displaySetting?.Mode == DisplayModes.Disabled && i != 0;

                            if (!isDisabled)
                            {
                                UpdateWindowDisplay(windowName, i);
                            }
                        }
                    );

                builder.AddField("Default Display", dropdownRect);
                var dropdown = dropdownRect.GetComponent<TMP_Dropdown>();

                if (dropdown != null)
                {
                    // Apply visual indication for disabled options
                    for (int i = 0; i < dropdown.options.Count; i++)
                    {
                        // If this mode isn't available for this display, mark it as disabled
                        if (!allowedDisplays.Contains(i))
                        {
                            var option = dropdown.options[i];
                            option.text = $"<color=#888888>{option.text}</color>";
                            dropdown.options[i] = option;
                        }
                    }

                    // Force refresh the dropdown
                    dropdown.RefreshShownValue();

                    // Add a custom handler to prevent selecting disabled options
                    dropdown.onValueChanged.AddListener(value =>
                    {
                        if (!allowedDisplays.Contains(value))
                        {
                            // Reset to current value if user tries to select a disabled option
                            dropdown.value = currentDisplayIndex;
                            dropdown.RefreshShownValue();
                        }
                    });
                }

                builder.AddField(
                    "Position Mode",
                    builder.AddDropdownIntPicker
                    (
                            windowPositionValues,
                            (int)winSettings.PositionMode,
                            (int i) => (i >= 0) ? windowPositions[i] : windowPositions[0],
                            true,
                            (int i) =>
                            {
                                UpdateWindowPositionMode(windowName, (WindowSettings.Positions)i);
                            }
                    )
                );

                if (winSettings.PositionMode == WindowSettings.Positions.Custom)
                {
                    builder.AddField
                    (
                        "Custom Position",
                        builder.HStack(delegate (UIPanelBuilder builder)
                        {
                            builder.AddInputFieldValidated
                            (
                                winSettings.Position.x.ToString(),
                                (xString) =>
                                {
                                },
                                "[0-9]?",
                                null,
                                4
                            );

                            builder.AddInputFieldValidated
                            (
                                winSettings.Position.y.ToString(),
                                (xString) =>
                                {
                                },
                                "[0-9]?",
                                null,
                                4
                            );
                        })
                    );
                }
            }
            builder.AddExpandingVerticalSpacer();
        },
        new RectOffset(0, 4, 0, 0));

        builder.Spacer().Height(4f);
    }
    #endregion

    private void BuildButtonSection(UIPanelBuilder builder)
    {
        builder.HStack(delegate (UIPanelBuilder builder)
        {
            buttonsBuilder = builder;

            builder.AddButton("Back", delegate
            {
                RestoreOriginalSettings();
                MenuManagerPatch._MMinstance.navigationController.Pop();
            });

            builder.Spacer().FlexibleWidth(1f);

            bool settingsValid = ValidateSettings(out string errorMessage);

            Logger.Log($"Test Validation: {settingsValid}, {errorMessage}");

            if (!settingsValid)
            {
                builder.AddLabel($"<alpha=#66><i>{errorMessage}</i>");

                builder.Spacer().FlexibleWidth(1f);
            }

            builder.AddButton("Apply", delegate
            {
                SaveSettings();
                HandleDisplayChanges();
                MenuManagerPatch._MMinstance.navigationController.Pop();
            }).Disable(!settingsValid);
        });
    }

    #endregion

    #region Helper Methods

    private void LoadSettings()
    {
        pendingDisplaySettings.Clear();

        // Get current display index
        int currentGameDisplay = displays.FindIndex(d => d.Equals(Screen.mainWindowDisplayInfo));

        for (int i = 0; i < displays.Count; i++)
        {
            var settings = DisplayUtils.GetDisplaySettings(i, true) ??
                new DisplaySettings
                {
                    Mode = DisplayModes.Disabled
                };

            pendingDisplaySettings[i] = settings.Clone();
        }

        originalActiveDisplays = new(pendingDisplaySettings.Where(x => x.Value.Mode != DisplayModes.Disabled).Select(x => x.Key));
        originalMainDisplay = pendingDisplaySettings.First(x => x.Value.Mode == DisplayModes.Main).Key;
        originalFocusManager = DisplayUtils.FocusManagerActive;
        newFocusManager = originalFocusManager;

        //get current window settings
        pendingWindowSettings.Clear();
        pendingWindowSettings.AddRange(Multiscreen.settings?.Windows);
    }

    private void UpdateDisplayMode(int displayIndex, DisplayModes newMode)
    {
        // Ensure device information is populated for the display
        EnsureDeviceInfoForDisplay(displayIndex);

        if (newMode == DisplayModes.Main)
        {
            // Find and update previous main display
            var previousMain = pendingDisplaySettings.FirstOrDefault(x => x.Value.Mode == DisplayModes.Main);
            if (previousMain.Value != null)
            {
                previousMain.Value.Mode = DisplayModes.Disabled;
            }
        }

        pendingDisplaySettings[displayIndex].Mode = newMode;
    }

    private void EnsureDisplaySettingsExist()
    {
        if (!pendingDisplaySettings.TryGetValue(selectedDisplay, out DisplaySettings _))
        {
            var displayInfo = displays[selectedDisplay];
            var monitor = DisplayUtils.GetWindowsMonitorForUnityDisplay(selectedDisplay);

            var newSettings = new DisplaySettings();

            if (monitor == null)
            {
                Logger.LogInfo($"Warning: Did not find Windows display for Unity display index {selectedDisplay}");
                return;
            }

            newSettings.Name = displayInfo.name;
            newSettings.DeviceId = monitor.DeviceID;
            pendingDisplaySettings[selectedDisplay] = newSettings;
        }
    }

    private void EnsureDeviceInfoForDisplay(int displayIndex)
    {
        // If device info is missing, populate it
        if (string.IsNullOrEmpty(pendingDisplaySettings[displayIndex].Name) ||
            string.IsNullOrEmpty(pendingDisplaySettings[displayIndex].DeviceId))
        {
            var displayInfo = displays[displayIndex];
            var monitor = DisplayUtils.GetWindowsMonitorForUnityDisplay(displayIndex);

            if (monitor != null)
            {
                pendingDisplaySettings[displayIndex].Name = displayInfo.name;
                pendingDisplaySettings[displayIndex].DeviceId = monitor.DeviceID;
                Logger.LogDebug($"Populated device info for display {displayIndex}: {displayInfo.name}, {monitor.DeviceID}");
            }
        }
    }

    private void RestoreOriginalSettings()
    {
        DisplayUtils.EnableDisplayFocusManager(originalFocusManager);

        // Restore preview changes
        for (int i = 0; i < displays.Count; i++)
        {
            var originalSettings = DisplayUtils.GetDisplaySettings(i, true);
            // Skip if no settings found
            if (originalSettings == null)
                continue;

            var dispInfo = DisplayUtils.GetDisplayInfoFromIndex(i);
            // Skip if display info not found
            if (dispInfo == null)
                continue;

            // Restore background settings
            if (dispInfo.Background != null)
            {
                dispInfo.Background.enabled = originalSettings.SolidBg;

                if (ColorUtility.TryParseHtmlString(originalSettings.BgColour, out Color col))
                    dispInfo.Background.color = col;
            }

            // Restore scale
            if (dispInfo.DockContainer != null)
                WindowUtils.UpdateScale(i, originalSettings.Scale);
        }
    }

    private void SaveSettings()
    {
        // Save focus manager setting
        if (originalFocusManager != newFocusManager)
            Multiscreen.settings.FocusManager = DisplayUtils.FocusManagerActive;

        // Save displays settings
        Multiscreen.settings.Displays = pendingDisplaySettings.Values.ToList();

        // Update window settings for any display changes
        UpdateWindowSettingsForDisplayChanges();

        // Save settings
        Multiscreen.settings.Save(Multiscreen.ModEntry);
    }

    private void HandleDisplayChanges()
    {
        // Get new settings
        var mainDisplayEntry = pendingDisplaySettings.First(x => x.Value.Mode == DisplayModes.Main);
        int newMainDisplay = mainDisplayEntry.Key;
        DisplaySettings newMainDisplaySettings = mainDisplayEntry.Value;

        var newActiveDisplayIndices = pendingDisplaySettings
            .Where(x => x.Value.Mode != DisplayModes.Disabled)
            .Select(x => x.Key)
            .ToHashSet();

        // Check if any previously active displays are being disabled
        bool requiresRestart = originalActiveDisplays.Any(d => !newActiveDisplayIndices.Contains(d));

        // Check if main display changed
        if (newMainDisplay != originalMainDisplay)
        {
            DisplayUtils.MoveAndRestartApplication(newMainDisplaySettings, displays);
            return;
        }

        if (requiresRestart)
        {
            DisplayUtils.RestartApplication();
            return;
        }

        // Process display changes that don't require restart
        ApplyDisplayChanges();
    }

    private void ApplyDisplayChanges()
    {
        // Activate any new displays or update existing ones
        foreach (var entry in pendingDisplaySettings.Where(x => x.Value.Mode != DisplayModes.Disabled))
        {
            int displayIndex = entry.Key;
            var settings = entry.Value;

            // Skip main display
            if (displayIndex == 0)
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
                    // Update background settings
                    if (displayInfo.Background != null)
                    {
                        displayInfo.Background.enabled = settings.SolidBg;

                        if (ColorUtility.TryParseHtmlString(settings.BgColour, out Color col))
                            displayInfo.Background.color = col;
                    }

                    // Update scale
                    if (displayInfo.DockContainer != null)
                    {
                        WindowUtils.UpdateScale(displayIndex, settings.Scale);
                    }
                }
            }
        }
    }

    private List<int> GetAvailableModesForDisplay(int displayIndex)
    {
        List<int> availableModes = [];

        // Display 0 can only be Disabled or Main
        if (displayIndex == 0)
        {
            availableModes.Add((int)DisplayModes.Disabled);
            availableModes.Add((int)DisplayModes.Main);
        }
        // Display 1+ can be any mode
        else
        {
            // All modes are available for secondary displays
            availableModes.AddRange(displayModeValues);
        }

        return availableModes;
    }

    private bool ValidateSettings(out string errorMessage)
    {
        errorMessage = string.Empty;

        // Count displays set to Main mode
        int mainDisplayCount = pendingDisplaySettings.Count(x => x.Value.Mode == DisplayModes.Main);

        if (mainDisplayCount == 0)
        {
            errorMessage = "No 'Main Game' display set";
            return false;
        }
        else if (mainDisplayCount > 1)
        {
            errorMessage = "Only one display can be 'Main Game'";
            return false;
        }

        return true;
    }

    private WindowSettings GetWindowSettings(string windowName)
    {
        WindowSettings result = null;

        if (Multiscreen.settings != null && Multiscreen.settings.Windows != null)
        {
            result = Multiscreen.settings.Windows.FirstOrDefault(x => x.WindowName == windowName);
        }

        return result ?? new WindowSettings
        {
            WindowName = windowName,
        };
    }

    private List<int> GetDisplaysAllowingWindows()
    {
        List<int> result = [];

        // Add all other displays
        for (int i = 0; i < displays.Count; i++)
        {
            pendingDisplaySettings.TryGetValue(i, out var settings);

            if (settings.Mode != DisplayModes.Disabled && settings.AllowWindows)
                result.Add(i);
        }

        return result;
    }


    private void UpdateWindowPositionMode(string windowName, WindowSettings.Positions newMode)
    {
        // Ensure Windows collection exists
        Multiscreen.settings.Windows ??= [];

        // Find existing or create new
        var window = Multiscreen.settings.Windows.FirstOrDefault(w => w.WindowName == windowName);
        if (window == null)
        {
            window = new WindowSettings { WindowName = windowName };
            Multiscreen.settings.Windows.Add(window);
        }

        window.PositionMode = newMode;
        Logger.LogDebug($"Updated window {windowName} position mode to {newMode}");
    }

    private void UpdateWindowDisplay(string windowName, int displayIndex)
    {
        // Ensure Windows collection exists
        if (Multiscreen.settings.Windows == null)
            Multiscreen.settings.Windows = new List<WindowSettings>();

        // Find existing or create new
        var window = Multiscreen.settings.Windows.FirstOrDefault(w => w.WindowName == windowName);
        if (window == null)
        {
            window = new WindowSettings { WindowName = windowName };
            Multiscreen.settings.Windows.Add(window);
        }

        // Set device ID from the display
        window.DeviceId = GetDeviceIdForDisplay(displayIndex);
        Logger.LogDebug($"Updated window {windowName} display to {displayIndex} (DeviceId: {window.DeviceId})");
    }

    private void UpdateWindowSettingsForDisplayChanges()
    {
        if (Multiscreen.settings.Windows == null)
            return;

        // Get all active display IDs
        HashSet<string> activeDeviceIds = new();
        foreach (var entry in pendingDisplaySettings)
        {
            if (entry.Value.Mode != DisplayModes.Disabled && !string.IsNullOrEmpty(entry.Value.DeviceId))
                activeDeviceIds.Add(entry.Value.DeviceId);
        }

        // Update window settings
        foreach (var window in Multiscreen.settings.Windows)
        {
            // If window is set to a display that's no longer active, reset to main display
            if (!string.IsNullOrEmpty(window.DeviceId) && !activeDeviceIds.Contains(window.DeviceId))
            {
                window.DeviceId = GetDeviceIdForDisplay(0); // Reset to main display
                Logger.LogInfo($"Moved window {window.WindowName} to main display as previous display is no longer active");
            }
        }
    }

    private int GetDisplayIndexForDeviceId(string deviceId)
    {
        // Empty DeviceId means main display (display 0)
        if (string.IsNullOrEmpty(deviceId))
            return 0;

        // Find matching display in pending settings
        for (int i = 0; i < displays.Count; i++)
        {
            if (pendingDisplaySettings.TryGetValue(i, out var settings) &&
                settings.DeviceId == deviceId)
            {
                return i;
            }
        }

        // If not found, default to main display
        Logger.LogDebug($"DeviceId {deviceId} not found in current displays, defaulting to main display");
        return 0;
    }

    private string GetDeviceIdForDisplay(int displayIndex)
    {
        // Special case: main display is represented by empty string
        if (displayIndex == 0)
            return "";

        // Get device ID for the specified display
        if (pendingDisplaySettings.TryGetValue(displayIndex, out var settings) &&
            !string.IsNullOrEmpty(settings.DeviceId))
        {
            return settings.DeviceId;
        }

        // Should never happen if display settings are properly initialized
        Logger.Log($"Warning: No DeviceId found for display {displayIndex}");
        return "";
    }
    #endregion
}