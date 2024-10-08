using System;
using static UnityModManagerNet.UnityModManager;

namespace Multiscreen.Util;

public enum LogLevel
{
    Info=0,
    Debug=1,
    Trace=2,
    Verbose=3
}

public static class Logger
{

    #region Logging

    /*
    public static void LogDebug(Func<object> resolver)
    {
        if (!Multiscreen.settings.DebugLogging)
            return;
        WriteLog($"[Debug] {resolver.Invoke()}");
    }*/

    public static void Log(object msg, LogLevel level = LogLevel.Info)
    {
        WriteLog($"[Info] {msg}");
    } 
    public static void LogInfo(object msg)
    {
        if (Multiscreen.settings.DebugLogging >= LogLevel.Info)
            WriteLog($"[Info] {msg}");
    }

    public static void LogDebug(object msg)
    {
        if (Multiscreen.settings.DebugLogging >= LogLevel.Debug)
            WriteLog($"[Debug] {msg}");
    }

    public static void LogTrace(object msg)
    {
        if (Multiscreen.settings.DebugLogging >= LogLevel.Trace)
            WriteLog($"[Trace] {msg}");
    }
    public static void LogVerbose(object msg)
    {
        if (Multiscreen.settings.DebugLogging >= LogLevel.Verbose)
            WriteLog($"[Verbose] {msg}");
    }

    private static void WriteLog(string msg)
    {
        string str = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        Multiscreen.ModEntry.Logger.Log(str);
    }
    #endregion

}

