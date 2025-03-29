using System;
using UnityEngine;
using UnityModManagerNet;
using Multiscreen.Util;
using static UnityModManagerNet.UnityModManager;
using Logger = Multiscreen.Util.Logger;
using System.Collections.Generic;
using UI.Common;

namespace Multiscreen;

[Serializable]
[DrawFields(DrawFieldMask.OnlyDrawAttr)]
public class Settings : UnityModManager.ModSettings, IDrawable
{
    public static Action<Settings> OnSettingsUpdated;

    public int Version = 2;
    public int LastRun = -1;
    public bool UpdatePrompted = false;

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

}


public interface ICloneable<T>
{
    T Clone();
}
public class DisplaySettings : ICloneable<DisplaySettings>
{
    public enum DisplayModes
    {
        Disabled,
        Main,
        ExtraWindows,
        Map,
        CTC
    }

    private string bgColour = "#000000";

    public string Name = "None";
    public string DeviceId = "";
    public DisplayModes Mode = DisplayModes.Disabled;
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

public class WindowSettings : ICloneable<WindowSettings>
{
    public enum Positions
    {
        Default,
        RestoreLast,
        Custom,
        LowerLeft,
        LowerRight,
        UpperLeft,
        UpperRight,
        Center,
        CenterRight
    }
    public enum Sizing
    {
        Default,
        RestoreLast,
        Custom
    }

    public string WindowName;
    public string DeviceId;
    public Positions PositionMode = Positions.RestoreLast;
    public Sizing SizeMode = Sizing.RestoreLast;
    public Vector2 Position;
    public Vector2Int Size;
    public bool Shown;

    public WindowSettings Clone()
    {
        return new WindowSettings
        {
            WindowName = this.WindowName,
            DeviceId = this.DeviceId,
            PositionMode = this.PositionMode,
            Position = this.Position,
            Size = this.Size,
            Shown = this.Shown,
        };
    }
}