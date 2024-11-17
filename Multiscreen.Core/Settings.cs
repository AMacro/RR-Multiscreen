using System;
using UnityEngine;
using UnityModManagerNet;
using Multiscreen.Util;
using static UnityModManagerNet.UnityModManager;

namespace Multiscreen;

[Serializable]
[DrawFields(DrawFieldMask.OnlyDrawAttr)]
public class Settings : UnityModManager.ModSettings, IDrawable
{
    public static Action<Settings> OnSettingsUpdated;

    //[Header("Second Display")]
    //[Draw("Second Display", Tooltip = "This is the Unity Display number: -1 not set, must be >= 1")]
    public int gameDisplay = -1;
    public int secondDisplay = -1;
    public float secondDisplayScale = 1f;
    public bool solidBG = false;
    public string bgColour = "000000";
    public bool focusManager = true;

    [Space(10)]
    [Draw("Logging Level", Tooltip = "Amount of debug logging to capture."/*, VisibleOn = "ShowAdvancedSettings|true"*/)]
    public LogLevel DebugLogging = LogLevel.Info;
    /*[Draw("Enable Log File", Tooltip = "Whether to create a separate file for logs. This is useful for debugging, but should otherwise be kept off.", VisibleOn = "ShowAdvancedSettings|true")]
    public bool EnableLogFile;*/
   

    public void Draw(UnityModManager.ModEntry modEntry)
    {
        Settings self = this;
        UnityModManager.UI.DrawFields(ref self, modEntry, DrawFieldMask.OnlyDrawAttr, OnChange);
        /*if (ShowAdvancedSettings && GUILayout.Button("Enable Developer Commands"))
            Console.RegisterDevCommands();*/
        
    }

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        if(gameDisplay == secondDisplay)
        {
            secondDisplay = -1;
        }

        secondDisplayScale = Mathf.Clamp(secondDisplayScale,0.2f,2f);

        /*if (!UnloadWatcher.isQuitting)
            OnSettingsUpdated?.Invoke(this);*/
        Save(this, modEntry);
    }

    public void OnChange()
    {
        // yup
    }

}
