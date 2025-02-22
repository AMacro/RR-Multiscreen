using UI.Builder;
using UI.Menu;
using UnityEngine;
using System.Linq;
using Multiscreen.Patches.Menus;
using TMPro;
using System.Collections.Generic;
using UI.CompanyWindow;
using Analytics;
using Helpers;
using Multiscreen.Util;
using Logger = Multiscreen.Util.Logger;
using System;
using UnityEngine.UI;
using static Multiscreen.Util.DisplayUtils;
using System.Threading;

namespace Multiscreen.CustomMenu;

public class ModSettingsMenu : MonoBehaviour
{
    const int SCREEN_LAYOUT_WIDTH = 460;
    const int SCREEN_LAYOUT_HEIGHT = 200;
    const int SCREEN_LAYOUT_PADDING = 10;

    public GameObject contentPanel;
    public UIBuilderAssets assets;
    public static readonly UIState<string> SelectedTab = new("disp");
    List<DisplayInfo> displays = new();
    List<int> values = new();

    private int selectedDisplay = 0;

    int newGameDisplay;
    int oldGameDisplay;
    int newSecondDisplay;
    int oldSecondDisplay;
    bool oldFocusManager;
    bool oldSolidBg;
    Color oldColour;
    float oldScale;
    float newScale;

    public List<DisplaySettings> newDisplaySettings = [];
    public List<DisplaySettings> oldDisplaySettings = [];
    public List<string> DisplayModes = ["Disabled", "Main Game", "Secondary (Game Background)","Secondary (Solid Background)", "Full Screen Map", "CTC"];


    private void Awake()
    {
        TextMeshProUGUI title = GetComponentInChildren<TextMeshProUGUI>();
        title.text = "Multiscreen";
  
        GameObject.DestroyImmediate(GameObject.Find("Preferences Menu(Clone)/Content/Tab View(Clone)"));

        contentPanel = GameObject.Find("Preferences Menu(Clone)/Content");

        assets = this.transform.GetComponent<PreferencesMenu>().panelAssets;

        oldGameDisplay = newGameDisplay = Multiscreen.gameDisplay;
        oldSecondDisplay = newSecondDisplay = Multiscreen.secondDisplay;

        //get all displays connected to the system
        Screen.GetDisplayLayout(displays);
        values = displays.Select((DisplayInfo r, int i) => i).ToList();
           
        int mainDisp = displays.FindIndex(s => s.Equals(Screen.mainWindowDisplayInfo));

        Logger.LogInfo("Pre-loop");

        //preload the old display settings
        /*DisplaySettings currDisp = new DisplaySettings();
        oldDisplaySettings.Add(currDisp);
        currDisp = new DisplaySettings();
        oldDisplaySettings.Add(currDisp);
        currDisp = new DisplaySettings();
        oldDisplaySettings.Add(currDisp);*/
        
        /*
        for (int i = 1; i < values.Count; i++)
        {
            DisplaySettings currDisp = new DisplaySettings();
            Logger.LogInfo("Pre-if");
            if (i == mainDisp)
            {
                currDisp.mode = DisplayMode.Main;
                currDisp.AllowWindows = true;
            }
            Logger.LogInfo("Post-if");
            currDisp.name = displays[i].name;
            currDisp.nativeWidth = displays[i].width;
            currDisp.nativeHeight = displays[i].height;

            Logger.LogInfo("Pre-append");
            oldDisplaySettings.Add(currDisp);
        }

        Logger.LogInfo("Pre-copy");
        newDisplaySettings = oldDisplaySettings.Select(a => a.Clone()).ToList();
        Logger.LogInfo("Post-copy");
        Logger.LogInfo($"Old: {oldDisplaySettings.First().scale}, New: {newDisplaySettings.First().scale}");
        oldDisplaySettings.First().scale = 5.0f;
        Logger.LogInfo($"Old: {oldDisplaySettings.First().scale}, New: {newDisplaySettings.First().scale}");
       */

        oldFocusManager = DisplayUtils.FocusManagerActive;
        //oldSolidBg = Multiscreen.background.enabled;
        //oldColour = Multiscreen.background.color;
        //oldScale = Multiscreen.settings.secondDisplayScale;
        newScale = oldScale;
    } 

    private void Start()
    {
        BuildPanelContent();
    }

    private void Update()
    {
        if(oldGameDisplay != newGameDisplay || oldSecondDisplay != newSecondDisplay)
        {
            Logger.LogDebug($"Update: {oldGameDisplay} {newGameDisplay} - {oldSecondDisplay} {newSecondDisplay}");

            oldGameDisplay = newGameDisplay;
            oldSecondDisplay = newSecondDisplay;

            Logger.LogDebug($"Post Update: {oldGameDisplay} {newGameDisplay} - {oldSecondDisplay} {newSecondDisplay}");

            GameObject.DestroyImmediate(GameObject.Find("Settings Menu(Clone)/Content/Tab View(Clone)"));
            BuildPanelContent();
        }
    }

    public void BuildPanelContent()
    {
        
        if (contentPanel != null)
        {
            UIPanel.Create(contentPanel.transform.GetComponent<RectTransform>(), assets, delegate (UIPanelBuilder builder)
            {
                Logger.LogTrace("PanelCreate");

                builder.AddTabbedPanels(SelectedTab, BuildTabs);

                builder.AddField("UI Scale", builder.AddSlider(() => newScale, () => string.Format("{0}%", Mathf.Round(newScale * 100f)),delegate (float f)
                {
                    newScale = f;
                    WindowUtils.UpdateScale(1, newScale);
                },
                0.2f, 2f, false));

                    
                //builder.AddField("Solid Background",builder.HStack(delegate (UIPanelBuilder builder)
                //{
                //    builder.AddToggle(() => Multiscreen.background.enabled, isOn =>
                //    {
                //        Multiscreen.background.enabled = isOn;
                //        builder.Rebuild();
                //    });

                //    builder.AddColorDropdown(ColorHelper.HexFromColor(Multiscreen.background.color), colour => 
                //    {
                //        UnityEngine.Color temp = Multiscreen.background.color;
                //        UnityEngine.Color newCol;
                //        if (ColorUtility.TryParseHtmlString(colour, out newCol))
                //        {
                //            Multiscreen.background.color = newCol;
                //        }
                //        else
                //        {
                //            Multiscreen.background.color = temp;
                //        }
                //        //Logger.LogInfo(colour);
                //    }
                //    ).Width(60f);
                //})
                //);


                builder.AddField("Focus Management", builder.AddToggle(() => DisplayUtils.FocusManagerActive, delegate (bool en)
                {
                    DisplayUtils.EnableDisplayFocusManager(en);
                }));

                builder.AddLabel("Focus management ensures each display is focused when the mouse is over it.");


                builder.Spacer(16f);
                builder.HStack(delegate (UIPanelBuilder builder)
                {
                    builder.AddButton("Back", delegate
                    {
                        //DisplayUtils.EnableDisplayFocusManager(oldFocusManager);

                        //Multiscreen.background.enabled = oldSolidBg;
                        //Multiscreen.background.color = oldColour;

                        //Multiscreen.settings.secondDisplayScale = oldScale;
                        //WindowUtils.UpdateScale(oldScale);

                        MenuManagerPatch._MMinstance.navigationController.Pop();

                    });

                    builder.Spacer().FlexibleWidth(1f);

                    builder.AddButton("Apply", delegate
                    {

                        Multiscreen.settings.focusManager = DisplayUtils.FocusManagerActive;

                        //Multiscreen.settings.solidBG = Multiscreen.background.enabled;
                        //Multiscreen.settings.bgColour = ColorHelper.HexFromColor(Multiscreen.background.color);

                        //Multiscreen.settings.secondDisplayScale = newScale;

                        //List<DisplayInfo> displays = new();
                        //Screen.GetDisplayLayout(displays);

                        //if (Multiscreen.gameDisplay != newGameDisplay)
                        //{
                        //    Multiscreen.gameDisplay = newGameDisplay;
                        //    Multiscreen.settings.gameDisplay = newGameDisplay;

                        //    Screen.MoveMainWindowTo(displays[Multiscreen.gameDisplay], new Vector2Int(0, 0));
                        //}

                        //if (Multiscreen.secondDisplay != newSecondDisplay)
                        //{
                        //    Multiscreen.secondDisplay = newSecondDisplay;
                        //    Multiscreen.settings.secondDisplay = newSecondDisplay;

                        //    ShowRestart();
                        //}

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
        var monitors = DisplayUtils.GetNormalisedMonitorLayout(DisplayUtils.GetMonitorLayout());
        BuildScreenLayout(builder, monitors);

        //// Add display settings below the layout panel
        //builder.AddField("Display Mode", builder.AddDropdownIntPicker(
        //    DisplayModeValues,
        //    selectedDisplay,
        //    (int i) => (i >= 0) ? DisplayModes[i].ToString() : DisplayModes[0],
        //    canWrite: true,
        //    delegate (int i)
        //    {
        //        Logger.LogDebug($"Selected Mode: {i}");
        //    }));

        builder.AddField("Display", builder.AddDropdownIntPicker(values, selectedDisplay, (int i) => (i >= 0) ? $"Display: {i} ({displays[i].name})" : $"00", canWrite: true, delegate (int i)
        {
            Logger.LogDebug($"Selected Display: {i}");

            //todo load display settings

        }));

        //List<int> DisplayModeValues = new();
        //DisplayModeValues = DisplayModes.Select((String r, int i) => i).ToList();

        //builder.AddField("Display Mode", builder.AddDropdownIntPicker(DisplayModeValues, 0, (int i) => (i >= 0) ? DisplayModes[i].ToString() : DisplayModes[0], canWrite: true, delegate (int i)
        //{
        //    Logger.LogDebug($"Selected Display: {i}");

        //    //todo load display settings

        //}));

        //builder.AddField("UI Scale", builder.AddSlider(() => Multiscreen.settings.secondDisplayScale, () => string.Format("{0}%", Mathf.Round(Multiscreen.settings.secondDisplayScale * 100f)), delegate (float f)
        //{
        //    Multiscreen.settings.secondDisplayScale = f;
        //    WindowUtils.UpdateScale(newScale);
        //},
        //0.2f, 2f, false));


        //builder.AddField("Solid Background", builder.HStack(delegate (UIPanelBuilder builder)
        //{
        //    builder.AddToggle(() => Multiscreen.settings.solidBG, isOn =>
        //    {
        //        Multiscreen.settings.solidBG = isOn;
        //        Multiscreen.background.enabled = isOn;
        //        builder.Rebuild();
        //    });

        //    builder.Spacer(2f);

        //    builder.AddColorDropdown(Multiscreen.settings.bgColour, colour =>
        //    {
        //        UnityEngine.Color temp = Multiscreen.background.color;
        //        UnityEngine.Color newCol;
        //        if (ColorUtility.TryParseHtmlString(colour, out newCol))
        //        {
        //            Multiscreen.settings.bgColour = colour;
        //            Multiscreen.background.color = newCol;
        //        }
        //        else
        //        {
        //            Multiscreen.background.color = temp;
        //        }
        //        //Logger.LogInfo(colour);
        //    }
        //    ).Width(60f);
        //})
        //);

        //builder.AddField("Allow Windows", builder.AddToggle(() => Multiscreen.settings.solidBG, isOn =>
        //{
        //    Multiscreen.settings.solidBG = isOn;
        //    Multiscreen.background.enabled = isOn;
        //    builder.Rebuild();
        //}));


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
        int maxX = monitors.Max(m => m.Bounds.Right);
        int maxY = monitors.Max(m => m.Bounds.Bottom);

        float usableWidth = SCREEN_LAYOUT_WIDTH - (2 * SCREEN_LAYOUT_PADDING);
        float usableHeight = SCREEN_LAYOUT_HEIGHT - (2 * SCREEN_LAYOUT_PADDING);

        // Calculate the scaling factor for the screen buttons
        float scaleX = (usableWidth) / maxX;
        float scaleY = (usableHeight) / maxY;
        float scale = Mathf.Min(scaleX, scaleY);

        foreach (var monitor in monitors)
        {
            float w = (monitor.Bounds.Right - monitor.Bounds.Left) * scale;
            float h = (monitor.Bounds.Bottom - monitor.Bounds.Top) * scale;
            float x = monitor.Bounds.Left * scale;
            float y = monitor.Bounds.Top * scale;

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
        text.text = "\r\n<style=h3>Restart Required!</style>\r\n\r\nMoving the secondary display to a different monitor requires the game to be restarted (this is a UnityEngine limitation).\r\n\r\nPlease restart Railroader.";

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
}


/*
 
<style=h3>Welcome, Railroader!</style>

There's a lot of work to do and railroad fun to be had, both for you, as well as for us, the developers of Railroader! In your travels on the rails, remember that Railroader is in <i>Early Access</i> and is a <i>work in progress</i>. 

Many of the towns you pass through are not yet populated by buildings, and models may not be complete. We will be working to fill in the picture during Early Access, as well as listening to your feedback on gameplay to determine how best to shape Company mode and other mechanics. We sincerely hope you enjoy your time in the game, even if you have to use your imagination from time to time.

Before you continue, it will help us build a better Railroader if we can collect anonymous usage data. Your name, railroad name, and other personalized information are not collected. If you change your mind you can always opt in or out in  Settings.

Whatever your choice, thank you for playing <style=serif><b>RAILROADER</b></style> Early Access!
 * */