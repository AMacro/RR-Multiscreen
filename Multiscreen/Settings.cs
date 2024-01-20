using System;
using Humanizer;
using UnityEngine;
using UnityModManagerNet;
using Multiscreen.Utils;
using static UnityModManagerNet.UnityModManager;
using Model.AI;

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
    //public int resloution = 0;

    //[Draw("scale", Tooltip = "Scale")]

    public float secondDisplayScale = 1f;

    /*
    [Space(10)]
    [Header("Server")]
    [Draw("Default Remote IP", Tooltip = "The default server IP when joining as a client.")]
    public string DefaultRemoteIP = "";
    [Draw("Password", Tooltip = "The password required to join your server. Leave blank for no password.")]
    public string Password = "";
    [Draw("Max Players", Tooltip = "The maximum number of players that can join your server, including yourself.")]
    public int MaxPlayers = 4;
    [Draw("Port", Tooltip = "The port that your server will listen on. You generally don't need to change this.")]
    public int Port = 7777;

    [Space(10)]
    [Header("Preferences")]
    [Draw("Show Name Tags", Tooltip = "Whether to show player names above their heads.")]
    public bool ShowNameTags = true;
    [Draw("Show Ping In Name Tag", Tooltip = "Whether to show player pings above their heads.", VisibleOn = "ShowNameTags|true")]
    public bool ShowPingInNameTags;
    */
    [Space(10)]
    [Draw("Debug Logging", Tooltip = "Whether to log extra information. This is useful for debugging, but should otherwise be kept off."/*, VisibleOn = "ShowAdvancedSettings|true"*/)]
    public bool DebugLogging;
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
