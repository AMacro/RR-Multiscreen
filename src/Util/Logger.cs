// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Modified in 2026 for the Railroader 0.6 port; see NOTICE.

using System;
using System.Collections.Generic;
using Railroader.Modding;

namespace Multiscreen.Util;

public enum LogLevel
{
    Info = 0,
    Debug = 1,
    Trace = 2,
    Verbose = 3
}

// Writes through the game's mod logger (Player.log), filtered by the LogLevel in settings.json.
// Debug, Trace and Verbose go out at Info so they show whatever the game's own log level is.
public static class Logger
{
    internal static IModLogger Sink;
    private static readonly HashSet<string> Once = new HashSet<string>();

    private static LogLevel Level => Multiscreen.settings?.DebugLogging ?? LogLevel.Info;

    public static void Log(object msg) => Sink?.Info($"{msg}");

    public static void LogInfo(object msg) => Sink?.Info($"{msg}");

    public static void LogWarning(object msg) => Sink?.Warning($"{msg}");

    // A warning that prints once per game session, however often it is hit.
    public static void LogWarningOnce(string key, object msg)
    {
        if (Once.Add(key))
            Sink?.Warning($"{msg}");
    }

    public static void LogDebug(object msg)
    {
        if (Level >= LogLevel.Debug)
            Sink?.Info($"[Debug] {msg}");
    }

    public static void LogDebug(Func<object> resolver)
    {
        if (Level >= LogLevel.Debug)
            Sink?.Info($"[Debug] {resolver?.Invoke()}");
    }

    public static void LogTrace(object msg)
    {
        if (Level >= LogLevel.Trace)
            Sink?.Info($"[Trace] {msg}");
    }

    public static void LogVerbose(object msg)
    {
        if (Level >= LogLevel.Verbose)
            Sink?.Info($"[Verbose] {msg}");
    }

    public static void LogVerbose(Func<object> resolver)
    {
        if (Level >= LogLevel.Verbose)
            Sink?.Info($"[Verbose] {resolver?.Invoke()}");
    }
}
