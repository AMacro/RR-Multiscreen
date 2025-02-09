using System;
using UnityModManagerNet;
using JetBrains.Annotations;
using UnityEngine;
using System.Reflection;
using System.IO;


namespace Multiscreen;

public static class MultiscreenLoader
{
    public static UnityModManager.ModEntry ModEntry;
    const string CORE_NAME = "Multiscreen.Core.";


    [UsedImplicitly]
    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;

        string coreVer = CORE_NAME;

        WriteLog($"Game Version: {Application.version}");
        switch (Application.version.Substring(0, 6))
        {
            case "2024.6":
                coreVer += "Beta";
                break;

            default:
                coreVer += "Main";
                break;
        }

        WriteLog($"Selected Core Version: {coreVer}");

        string coreDll = coreVer + ".dll";

        string coreAssemblyPath = Path.Combine(modEntry.Path, coreDll);

        if (!File.Exists(coreAssemblyPath))
        {
            WriteLog($"Failed to find core assembly at {coreAssemblyPath}");
            return false;
        }

        try
        {
            Assembly coreAssembly = Assembly.LoadFrom(coreAssemblyPath);

            Type modType = coreAssembly.GetType("Multiscreen.Multiscreen");
            MethodInfo loadMethod = modType.GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static);

            if (loadMethod == null)
            {
                WriteLog("Failed to find the Load method in the core assembly.");
                return false;
            }

            bool result = (bool)loadMethod.Invoke(null, new object[] { modEntry });

            return result;

        }
        catch (Exception ex)
        {
            //handle and log
            WriteLog($"Failed to load core assembly: {ex.Message}\r\n{ex.StackTrace}");
            return false;
        }
    }

    private static void WriteLog(string msg)
    {
        string str = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        ModEntry.Logger.Log(str);
    }
}