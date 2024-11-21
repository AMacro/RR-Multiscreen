using System;
using UnityEngine;
using UnityModManagerNet;
using Multiscreen.Util;
using static UnityModManagerNet.UnityModManager;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen;

[Serializable]
[DrawFields(DrawFieldMask.OnlyDrawAttr)]
public class Settings : UnityModManager.ModSettings, IDrawable
{
    public static Action<Settings> OnSettingsUpdated;

    //[Header("Second Display")]
    //[Draw("Second Display", Tooltip = "This is the Unity Display number: -1 not set, must be >= 1")]

    public int version = 0;

    //V1.x Settings
    public int gameDisplay = -1;
    public int secondDisplay = -1;
    public float secondDisplayScale = 1f;
    public bool solidBG = false;
    public string bgColour = "000000";
    public bool focusManager = true;

    //V2.x Settings 
    public DisplaySettings[] displays;
    public bool saveWindowsOnExit = true;
    public bool restoreWindowsOnLoad = true;
    public WindowSettings[] windows;

    [Space(10)]
    [Draw("Logging Level", Tooltip = "Amount of debug logging to capture."/*, VisibleOn = "ShowAdvancedSettings|true"*/)]
    public LogLevel DebugLogging = LogLevel.Info;
 
    public void Draw(UnityModManager.ModEntry modEntry)
    {
        Settings self = this;
        UnityModManager.UI.DrawFields(ref self, modEntry, DrawFieldMask.OnlyDrawAttr, OnChange);       
    }

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        if(gameDisplay == secondDisplay)
        {
            secondDisplay = -1;
        }

        secondDisplayScale = Mathf.Clamp(secondDisplayScale,0.2f,2f);

        Save(this, modEntry);
    }
    public void OnChange()
    {
        // yup
    }

    public void UpgradeSettings()
    {
        //do upgrades
    }


}

public enum DisplayMode
{
    Disabled,
    Main,
    Secondary,
    Solid,
    Map,
    CTC
}

public enum Windows
{
    CarCustomize,
    CarEditor,
    CarInspector,
    CompanyWindow,
    EngineRoster,
    Equipment,

}

public interface ICloneable<T>
{
    T Clone();
}
public class DisplaySettings : ICloneable<DisplaySettings>
{
    public string name = "None";
    public DisplayMode mode = DisplayMode.Disabled;
    public float scale = 1f;
    public int nativeWidth;
    public int nativeHeight;
    public string bgColour = "000000";
    public bool AllowWindows = true;

    public DisplaySettings Clone()
    {
        return new DisplaySettings
        {
            name = this.name,
            mode = this.mode,
            scale = this.scale,
            nativeWidth = this.nativeWidth,
            nativeHeight = this.nativeHeight,
            bgColour = this.bgColour,
            AllowWindows = this.AllowWindows
        };
    }
}

public class WindowSettings
{
    public string windowName;
    public int displayNum;
    public Vector3 windowPos;
    public Vector3 windowSize;
    public bool shown;
    
}