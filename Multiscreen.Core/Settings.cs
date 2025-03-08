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

    public int Version = 2;

    //V1.x Settings
    public bool FocusManager = true;

    //V2.x Settings 
    public List<DisplaySettings> Displays = [];
    public bool SaveWindowsOnExit = true;
    public bool RestoreWindowsOnLoad = true;
    public List<WindowSettings> Windows;

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
    private string bgColour = "#000000";

    public string Name = "None";
    public string DeviceId = "";
    public DisplayMode Mode = DisplayMode.Disabled;
    public float Scale = 1f;
    public bool AllowWindows = true;

    public bool SolidBg = false;
    public string BgColour
    {
        get
        {
            return bgColour.StartsWith("#") ? bgColour : "#" + bgColour;
        }
        set
        {
            bgColour = value?.TrimStart('#') ?? "000000";
        }
    }


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

    public override string ToString()
    {
        return $"{{Name: {Name} DeviceId: {DeviceId} Mode: {Mode} Scale: {Scale} SolidBg: {SolidBg} BgColour: {BgColour} AllowWindows: {AllowWindows}}}";
    }
}

public class WindowSettings
{
    public string WindowName;
    public string DeviceId;
    public Vector3 WindowPos;
    public Vector3 WindowSize;
    public bool Show;
}