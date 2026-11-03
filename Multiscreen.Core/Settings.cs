using Multiscreen.Util;
using System.IO;
using System.Xml.Serialization;
using System;
using UnityEngine;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen;

[Serializable]
public class Settings
{
    [XmlIgnore]
    private static string settingsFilePath;
    public int gameDisplay = -1;
    public int secondDisplay = -1;
    public float secondDisplayScale = 1f;
    public bool solidBG = false;
    public string bgColour = "000000";
    public bool focusManager = true;

    public LogLevel DebugLogging = LogLevel.Info;

    public static Settings Load(string modDirectory)
    {
        settingsFilePath = Path.Combine(modDirectory, "Settings.xml");

        if (!File.Exists(settingsFilePath))
        {
            Logger.Log($"Settings file not found at {settingsFilePath}. Creating new settings.");
            return new Settings();
        }

        try
        {
            using (var stream = new FileStream(settingsFilePath, FileMode.OpenOrCreate))
            {
                Logger.Log($"Reading settings file {settingsFilePath}.");
                var serializer = new XmlSerializer(typeof(Settings));
                return (Settings)serializer.Deserialize(stream);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error reading settings file: {ex.Message}. Creating new settings.");
            return new Settings();
        }
    }

    public void Save()
    {
        if (gameDisplay == secondDisplay)
        {
            secondDisplay = -1;
        }

        secondDisplayScale = Mathf.Clamp(secondDisplayScale, 0.2f, 2f);

        XmlSerializer serializer = new XmlSerializer(typeof(Settings));

        using (StreamWriter writer = new StreamWriter(settingsFilePath))
        {
            serializer.Serialize(writer, this);
        }
    }
}
