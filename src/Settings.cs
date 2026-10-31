// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Modified in 2026 for the Railroader 0.6 port; see NOTICE.

using System;
using System.IO;
using Multiscreen.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen;

// Persisted as settings.json in the mod's folder (the 0.6 loader has no settings store of its own).
[Serializable]
public class Settings
{
    public const string FileName = "settings.json";

    // Unity display indices: -1 means "not chosen yet" (game on 0, second screen on 1).
    public int gameDisplay = -1;
    public int secondDisplay = -1;
    public float secondDisplayScale = 1f;
    public bool solidBG = false;
    public string bgColour = "000000";
    public bool focusManager = true;

    [JsonConverter(typeof(StringEnumConverter))]
    public LogLevel DebugLogging = LogLevel.Info;

    [JsonIgnore]
    private string _path;

    public static Settings Load(string modDirectory)
    {
        string path = Path.Combine(modDirectory ?? ".", FileName);
        Settings settings = null;
        try
        {
            if (File.Exists(path))
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Multiscreen: could not read {FileName}, using defaults: {ex.Message}");
        }

        settings ??= new Settings();
        settings._path = path;
        settings.Sanitise();
        return settings;
    }

    public void Save()
    {
        Sanitise();
        if (string.IsNullOrEmpty(_path))
            return;

        try
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch (Exception ex)
        {
            Logger.LogWarningOnce("settings-save", $"Multiscreen: could not save {FileName}: {ex.Message}");
        }
    }

    private void Sanitise()
    {
        if (gameDisplay == secondDisplay)
            secondDisplay = -1;

        secondDisplayScale = Mathf.Clamp(secondDisplayScale, 0.2f, 2f);

        if (string.IsNullOrEmpty(bgColour))
            bgColour = "000000";
    }
}
