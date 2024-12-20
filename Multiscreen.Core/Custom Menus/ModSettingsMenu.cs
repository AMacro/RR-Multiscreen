﻿using UI.Builder;
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

namespace Multiscreen.CustomMenu;

public class ModSettingsMenu : MonoBehaviour
{
    GameObject contentPanel;
    UIBuilderAssets assets;

    int newGameDisplay;
    int oldGameDisplay;
    int newSecondDisplay;
    int oldSecondDisplay;
    bool oldFocusManager;
    bool oldSolidBg;
    Color oldColour;
    float oldScale;
    float newScale;

    // Token: 0x06000AA5 RID: 2725 RVA: 0x00054B67 File Offset: 0x00052D67
    private void Awake()
    {
        TextMeshProUGUI title = GetComponentInChildren<TextMeshProUGUI>();
        title.text = "Multiscreen";
  
        GameObject.DestroyImmediate(GameObject.Find("Preferences Menu(Clone)/Content/Tab View(Clone)"));

        contentPanel = GameObject.Find("Preferences Menu(Clone)/Content");

        assets = this.transform.GetComponent<PreferencesMenu>().panelAssets;

        oldGameDisplay = newGameDisplay = Multiscreen.gameDisplay;
        oldSecondDisplay = newSecondDisplay = Multiscreen.secondDisplay;

        oldFocusManager = Multiscreen.focusManager;
        oldSolidBg = Multiscreen.background.enabled;
        oldColour = Multiscreen.background.color;
        oldScale = Multiscreen.settings.secondDisplayScale;
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

                UIPanel.Create(contentPanel.transform.GetComponent<RectTransform>(), assets, delegate (UIPanelBuilder builder)
                {
                    builder.AddSection("Game Display");

                    List<DisplayInfo> displays = new();
                    Screen.GetDisplayLayout(displays);

                    List<int> values = displays.Select((DisplayInfo r, int i) => i).ToList();

                    builder.AddField("Display", builder.AddDropdownIntPicker(values, oldGameDisplay, (int i) => (i >= 0) ? $"Display: {i} ({displays[i].name})" : $"00", canWrite: true, delegate (int i)
                    {
                        Logger.LogDebug($"Selected Display: {i}");
                        if(i == oldSecondDisplay)
                        {
                            newSecondDisplay = oldGameDisplay;
                        }
                        newGameDisplay = i;

                    }));

                    /*
                    builder.AddField("Full Screen", builder.AddToggle(() => Screen.fullScreen, delegate (bool en)
                    {
                        Multiscreen.Log($"Game Display Full Screen: {en}");
                    })).Disable(true);
                    */

                    builder.AddSection("Secondary Display");

                    builder.AddField("Display", builder.AddDropdownIntPicker(values, oldSecondDisplay, (int i) => (i >= 0) ? $"Display: {i} ({displays[i].name})" : $"00", canWrite: true, delegate (int i)
                    {
                        Logger.LogDebug($"Secondary Display: {i}");
                        if (i == 0)
                        {
                            oldSecondDisplay = -1;
                            return;
                        }
                            
                        if (i == oldGameDisplay)
                        {
                            newGameDisplay = oldSecondDisplay;
                        }
                        newSecondDisplay = i;
                        //Screen.MoveMainWindowTo(displays[i], new Vector2Int(0, 0));
                    }));
                    builder.AddLabel("Note: Display 0 can not be used for the Secondary Display due to a Unity Engine limitation.\r\nA work-around for this on Windows is to change your Primary display in the Windows Display settings.");

                    builder.Spacer().Height(20f);

                    builder.AddField("UI Scale", builder.AddSlider(() => newScale, () => string.Format("{0}%", Mathf.Round(newScale * 100f)),delegate (float f)
                    {
                        newScale = f;
                        WindowUtils.UpdateScale(newScale);
                    },
                    0.2f, 2f, false));

                    
                    builder.AddField("Solid Background",builder.HStack(delegate (UIPanelBuilder builder)
                    {
                        builder.AddToggle(() => Multiscreen.background.enabled, isOn =>
                        {
                            Multiscreen.background.enabled = isOn;
                            builder.Rebuild();
                        });

                        builder.Spacer(2f);

                        builder.AddColorDropdown(ColorHelper.HexFromColor(Multiscreen.background.color), colour => 
                        {
                            UnityEngine.Color temp = Multiscreen.background.color;
                            UnityEngine.Color newCol;
                            if (ColorUtility.TryParseHtmlString(colour, out newCol))
                            {
                                Multiscreen.background.color = newCol;
                            }
                            else
                            {
                                Multiscreen.background.color = temp;
                            }
                            //Logger.LogInfo(colour);
                        }
                        ).Width(60f);
                    })
                    );


                    builder.AddField("Focus Management", builder.AddToggle(() => Multiscreen.focusManager, delegate (bool en)
                    {
                        Multiscreen.EnableDisplayFocusManager(en);
                    }));

                    builder.AddLabel("Focus management ensures each display is focused when the mouse is over it.");

                    builder.AddExpandingVerticalSpacer();

                });

                builder.Spacer(16f);
                builder.HStack(delegate (UIPanelBuilder builder)
                {
                    builder.AddButton("Back", delegate
                    {
                        Multiscreen.EnableDisplayFocusManager(oldFocusManager);

                        Multiscreen.background.enabled = oldSolidBg;
                        Multiscreen.background.color = oldColour;

                        Multiscreen.settings.secondDisplayScale = oldScale;
                        WindowUtils.UpdateScale(oldScale);

                        MenuManagerPatch._MMinstance.navigationController.Pop();

                    });

                    builder.Spacer().FlexibleWidth(1f);

                    builder.AddButton("Apply", delegate
                    {

                        Multiscreen.settings.focusManager = Multiscreen.focusManager;

                        Multiscreen.settings.solidBG = Multiscreen.background.enabled;
                        Multiscreen.settings.bgColour = ColorHelper.HexFromColor(Multiscreen.background.color);

                        Multiscreen.settings.secondDisplayScale = newScale;

                        List<DisplayInfo> displays = new();
                        Screen.GetDisplayLayout(displays);

                        if (Multiscreen.gameDisplay != newGameDisplay)
                        {
                            Multiscreen.gameDisplay = newGameDisplay;
                            Multiscreen.settings.gameDisplay = newGameDisplay;

                            Screen.MoveMainWindowTo(displays[Multiscreen.gameDisplay], new Vector2Int(0, 0));
                        }

                        if (Multiscreen.secondDisplay != newSecondDisplay)
                        {
                            Multiscreen.secondDisplay = newSecondDisplay;
                            Multiscreen.settings.secondDisplay = newSecondDisplay;

                            /*
                            if (Application.platform == RuntimePlatform.WindowsPlayer)
                            {
                                Screen.MoveMainWindowTo(displays[Multiscreen.secondDisplay], new Vector2Int(0, 0));
                                
                                WinNativeUtil.RECT r = new();
                                WinNativeUtil.GetWindowRect(Process.GetCurrentProcess().MainWindowHandle,ref r);

                                Multiscreen.Log($"Rect: {r.left}, {r.right}, {r.top}, {r.bottom}");
                                Screen.MoveMainWindowTo(displays[Multiscreen.gameDisplay], new Vector2Int(0, 0));
                            }
                            else
                            {
                                ShowRestart();
                            }*/

                            ShowRestart();
                        }

                        MenuManagerPatch._MMinstance.navigationController.Pop();

                    });
                });
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