using System;
using UnityEngine;
using UnityModManagerNet;
using Multiscreen.Util;
using static UnityModManagerNet.UnityModManager;
using Logger = Multiscreen.Util.Logger;
using System.Collections.Generic;

namespace Multiscreen;

[Serializable]
[DrawFields(DrawFieldMask.OnlyDrawAttr)]
public class Settings : UnityModManager.ModSettings, IDrawable
{
    public static Action<Settings> OnSettingsUpdated;

    //[Header("Second Display")]
    //[Draw("Second Display", Tooltip = "This is the Unity Display number: -1 not set, must be >= 1")]

    public int version = 2;

    //V1.x Settings
    //public int gameDisplay = -1;
    //public int secondDisplay = -1;
    //public float secondDisplayScale = 1f;
    //public bool solidBG = false;
    //public string bgColour = "000000";
    public bool focusManager = true;

    //V2.x Settings 
    public List<DisplaySettings> displays = [];
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
        //if(gameDisplay == secondDisplay)
        //{
        //    secondDisplay = -1;
        //}

        //secondDisplayScale = Mathf.Clamp(secondDisplayScale,0.2f,2f);

        //Todo: add validation code for displays

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
    public string Name = "None";
    public string DeviceId = "";
    public DisplayMode Mode = DisplayMode.Disabled;
    public float Scale = 1f;
    public bool SolidBg = false;
    public string BgColour = "#000000";
    public bool AllowWindows = true;

    public DisplaySettings Clone()
    {
        return new DisplaySettings
        {
            Name = this.Name,
            DeviceId = this.DeviceId,
            Mode = this.Mode,
            Scale = this.Scale,
            SolidBg = this.SolidBg,
            BgColour = this.BgColour,
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