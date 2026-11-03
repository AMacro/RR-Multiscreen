using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using UI.Common;
using UnityEngine.Networking;
using UnityEngine;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen;

internal class UpdateCheck : MonoBehaviour
{
    protected IEnumerator Start()
    {
        Logger.Log("Checking for update...");
        var path = Path.Combine(Multiscreen.ModContext.ModDirectory, "info.json");
        string jsonString = File.ReadAllText(path);
        var data = JObject.Parse(jsonString);

        string versionStr = (string)data.GetValue("version", StringComparison.OrdinalIgnoreCase);
        if (versionStr == null || !Version.TryParse(versionStr, out Version version))
        {
            Logger.Log($"Info.json does not contain a valid version: {versionStr}");
            yield break;
        }

        string uriStr = (string)data.GetValue("repository", StringComparison.OrdinalIgnoreCase);
        if (uriStr == null || !Uri.TryCreate(uriStr, UriKind.Absolute, out Uri uri))
        {
            Logger.Log($"Info.json does not contain a valid repository URL: {uriStr}");
            yield break;
        }
        using UnityWebRequest webRequest = UnityWebRequest.Get(uri);
        webRequest.downloadHandler = new DownloadHandlerBuffer();

        string homeString = (string)data.GetValue("HomePage", StringComparison.OrdinalIgnoreCase);
        Uri homeUri = null;
        if (homeString != null && Uri.TryCreate(homeString, UriKind.Absolute, out homeUri))
        {
            if (homeUri.Scheme != Uri.UriSchemeHttp && homeUri.Scheme != Uri.UriSchemeHttps)
            {
                Logger.Log($"Info.json does not contain a valid HomePage URL: {homeString}");
                homeUri = null;
            }
        }

        Logger.Log("Sending web request...");
        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Logger.Log($"Failed to check for update: {webRequest.error}");
            yield break;
        }

        var responseText = webRequest.downloadHandler.text;
        Logger.Log($"Successfully checked for update:\r\n{responseText}");
        data = JObject.Parse(responseText);

        if (data.GetValue("releases", StringComparison.OrdinalIgnoreCase) is not JArray releases || releases.Count == 0)
        {
            Logger.Log("Repository does not contain any releases.");
            yield break;
        }

        Version maxReleaseVer = null;
        foreach (var release in releases)
        {
            var id = (string)((JObject)release).GetValue("id", StringComparison.OrdinalIgnoreCase);
            if (id == null || !id.Equals("multiscreen", StringComparison.OrdinalIgnoreCase))
                continue;

            string releaseVersionStr = (string)((JObject)release).GetValue("version", StringComparison.OrdinalIgnoreCase);
            if (releaseVersionStr == null || !Version.TryParse(releaseVersionStr, out Version releaseVersion))
                continue;

            if (maxReleaseVer == null || releaseVersion > maxReleaseVer)
                maxReleaseVer = releaseVersion;
        }

        Logger.Log($"Multiscreen Current Version: {version}, Latest Version: {maxReleaseVer}");

        if (maxReleaseVer > version)
            ShowUpdate(version, maxReleaseVer, homeUri);

        Destroy(this.gameObject);
    }

    private static void ShowUpdate(Version current, Version latest, Uri targetUrl = null)
    {
        List<(int, string)> options = new(2);

        if (targetUrl != null)
            options.Add((0, "Yes"));

        options.Add((1, "No"));

        ModalAlertController.Present
        (
            "Multiscreen Update",
            $"A new version of Multiscreen Mod is available.\r\n\r\n" +
            $"Current version: {current.ToString()}\r\n" +
            $"New version: {latest.ToString()}\r\n\r\n" +
            "Open the mod page to download the update?",
            options,
            delegate (int buttonIndex)
            {
                if (buttonIndex == 0)
                {
                    Application.OpenURL(targetUrl.ToString());
                }
            }
        );
    }
}
