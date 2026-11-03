using Analytics;
using Helpers;
using Multiscreen.Patches.Menus;
using Multiscreen.Util;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Builder;
using UI.Common;
using UI.CompanyWindow;
using UI.Menu;
using UnityEngine;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.CustomMenu;

public class ModSettingsMenu : BuilderMenuBase<ModSettingsMenu>
{
    public override Vector2 PreferredSize => new Vector2(500f, 650f);
    public override string PanelTitle => "Multiscreen";
    public override string BackButtonTitle => "Back";
    public override string ForwardButtonTitle => "Apply";

    int newGameDisplay;
    int oldGameDisplay;
    int newSecondDisplay;
    int oldSecondDisplay;
    bool oldFocusManager;
    bool oldSolidBg;
    Color oldColour;
    float oldScale;
    float newScale;

    public override void Awake()
    {
        base.Awake();

        oldGameDisplay = newGameDisplay = Multiscreen.gameDisplay;
        oldSecondDisplay = newSecondDisplay = Multiscreen.secondDisplay;

        oldFocusManager = Multiscreen.focusManager;
        oldSolidBg = Multiscreen.background?.enabled ?? false;
        oldColour = Multiscreen.background?.color ?? Color.black;
        oldScale = Multiscreen.settings.secondDisplayScale;
        newScale = oldScale;
    }

    public override void BuildPanelContent(UIPanelBuilder builder)
    {
        builder.AddSection("Game Display");

        List<DisplayInfo> displays = new();
        Screen.GetDisplayLayout(displays);

        List<int> values = displays.Select((DisplayInfo r, int i) => i).ToList();

        builder.AddField("Display", builder.AddDropdownIntPicker(values, oldGameDisplay, (int i) => (i >= 0) ? $"Display: {i} ({displays[i].name})" : $"00", canWrite: true, delegate (int i)
        {
            Logger.LogDebug($"Selected Display: {i}");
            if (i == oldSecondDisplay)
            {
                newSecondDisplay = oldGameDisplay;
            }
            newGameDisplay = i;

        }));

        builder.AddSection("Secondary Display");

        if (displays.Count < 2)
        {
            builder.AddLabel("Only one display detected.");
        }
        else
        {
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
            }));
        }
        builder.AddLabel("Note: Display 0 can not be used for the Secondary Display due to a Unity Engine limitation.\r\nA work-around for this on Windows is to change your Primary display in the Windows Display settings.");

        builder.Spacer().Height(20f);

        builder.AddField("UI Scale", builder.AddSlider(() => newScale, () => string.Format("{0}%", Mathf.Round(newScale * 100f)), delegate (float f)
        {
            newScale = f;
            WindowUtils.UpdateScale(newScale);
        },
        0.2f, 2f, false));

        builder.AddField("Solid Background", builder.HStack(delegate (UIPanelBuilder builder)
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

        builder.Spacer(16f);
        builder.Spacer().FlexibleWidth(1f);
    }

    public override void PerformBackAction()
    {
        Multiscreen.EnableDisplayFocusManager(oldFocusManager);

        Multiscreen.background.enabled = oldSolidBg;
        Multiscreen.background.color = oldColour;

        Multiscreen.settings.secondDisplayScale = oldScale;
        WindowUtils.UpdateScale(oldScale);

        this.NavigationController().Pop();
    }

    public override void PerformForwardAction()
    {
        bool restartRequired = false;

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

            restartRequired = true;
        }

        if (Multiscreen.secondDisplay != newSecondDisplay)
        {
            Multiscreen.secondDisplay = newSecondDisplay;
            Multiscreen.settings.secondDisplay = newSecondDisplay;

            restartRequired = true;
        }

        Multiscreen.settings.Save();

        MenuManagerPatch._MMinstance.navigationController.Pop();

        if (restartRequired)
            ShowRestart();
    }

    private void ShowRestart()
    {
        EarlyAccessSplash earlyAccessSplash = UnityEngine.Object.FindFirstObjectByType<EarlyAccessSplash>();
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
        button.onClick.AddListener(delegate
        {
            Application.Quit();
        });

        earlyAccessSplash.Show();
    }
}