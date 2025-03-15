using HarmonyLib;
using Multiscreen.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Logger = Multiscreen.Util.Logger;
using System.Text;

namespace Multiscreen.Patches.Windows;


[HarmonyPatch]
public static class GenericWindowStartPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        try
        {
            Logger.LogInfo("Beginning TargetMethods() in GenericWindowStartPatch");

            var types = PatchUtils.GetWindowTypes();
            if (types == null)
            {
                Logger.LogInfo("GetWindowTypes() returned null");
                return [];
            }

            Logger.LogInfo($"Found {types.Count()} window types to patch");

            var methods = new List<MethodBase>();
            foreach (var type in types)
            {
                try
                {
                    var methodForType = GenericWindowStartHelper.GetMethodToPatch(type);
                    if (methodForType != null)
                        methods.Add(methodForType);
                }
                catch (Exception ex)
                {
                    Logger.LogInfo($"Error getting methods for type {type.Name}: {ex.Message}");
                }
            }

            Logger.LogInfo($"Found {methods.Count} methods to patch");
            Logger.LogDebug(() =>
            {
                StringBuilder sb = new();

                sb.AppendLine($"GenericWindowStartPatch Found {methods?.Count} methods to patch");

                foreach (var method in methods)
                    sb.AppendLine($"Will patch: {method?.DeclaringType?.Name}.{method?.Name}");

                return sb.ToString();
            });

            return methods;
        }
        catch (Exception ex)
        {
            Logger.LogInfo($"Critical error in TargetMethods: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }


    public static void Postfix(MethodBase __originalMethod)
    {
        var windowType = __originalMethod?.DeclaringType;
        Logger.LogDebug($"GenericWindowStartPatch.Postfix() {__originalMethod?.Name} called {windowType}");

    }
}

public static class GenericWindowStartHelper
{

    public static MethodBase GetMethodToPatch(Type type)
    {

        var allMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        Logger.LogDebug(() =>
        {
            StringBuilder builder = new();
            builder.AppendLine($"GenericWindowStartHelper Methods for type {type.Name}:");
            foreach (var method in allMethods)
                builder.AppendLine($"\t{method.Name}");

            return builder.ToString();
        });

        var method = allMethods.FirstOrDefault(p => p.Name.Equals("start", StringComparison.OrdinalIgnoreCase)) ??
                     allMethods.FirstOrDefault(p => p.Name.Equals("awake", StringComparison.OrdinalIgnoreCase));

        return method;
    }
}   