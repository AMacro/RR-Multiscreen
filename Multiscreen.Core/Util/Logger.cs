using System;

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
    public static void Log(object msg)
    {
        Multiscreen.ModContext.Log.Info($"{msg}");
    }

    public static void LogDebug(object msg)
    {
        if (Multiscreen.settings.DebugLogging >= LogLevel.Debug)
            Multiscreen.ModContext.Log.Debug($"{msg}");
    }

    public static void LogDebug(Func<object> resolver)
    {
        if (!(Multiscreen.settings.DebugLogging >= LogLevel.Debug))
            return;

        Multiscreen.ModContext.Log.Debug($"{resolver?.Invoke()}");
    }

    public static void LogTrace(object msg)
    {
        if (Multiscreen.settings.DebugLogging >= LogLevel.Trace)
            LogDebug($"[Trace] {msg}");
    }

    public static void LogVerbose(object msg)
    {
        if (Multiscreen.settings.DebugLogging >= LogLevel.Verbose)
            LogDebug($"[Verbose] {msg}");
    }
}

