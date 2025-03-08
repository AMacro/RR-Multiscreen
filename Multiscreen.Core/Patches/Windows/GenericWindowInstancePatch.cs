using HarmonyLib;
using Multiscreen.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UI.Common;
using Logger = Multiscreen.Util.Logger;
using System.Text;

namespace Multiscreen.Patches.Windows;


[HarmonyPatch]
public static class GenericWindowInstancePatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        try
        {
            Logger.LogInfo("Beginning TargetMethods() in GenericWindowInstancePatch");

            var types = GenericWindowInstanceHelper.GetWindowTypesToPatch();
            if (types == null)
            {
                Logger.LogInfo("GetWindowTypesToPatch() returned null");
                return [];
            }

            Logger.LogInfo($"Found {types.Count()} window types to patch");

            var methods = new List<MethodBase>();
            foreach (var type in types)
            {
                try
                {
                    var methodsForType = GenericWindowInstanceHelper.GetMethodsToPatch(type);
                    if (methodsForType != null)
                        methods.AddRange(methodsForType);
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

                sb.AppendLine($"GenericWindowInstancePatch Found {methods?.Count} methods to patch");

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


    public static bool Prefix(MethodBase __originalMethod, ref object __result)
    {
        var windowType = __originalMethod.DeclaringType;
        Logger.LogVerbose($"Custom Instance/Shared getter called {windowType}");

        
        foreach (var window in WindowUtils.GetAllWindows())
        {
            var component = window.GetComponent(windowType);
            if (component != null)
            {
                Logger.LogVerbose($"Custom Instance/Shared getter called {windowType} Found!");
                __result = component;
                return false;
            }
        }

        Logger.LogVerbose($"Custom Instance/Shared getter called {windowType} Not found, passing to default!");

        return true;
    }
}

public static class GenericWindowInstanceHelper
{

    static readonly string[] MANUAL_TYPES = ["EngineRosterPanel", "PlacerWindow"];

    public static IEnumerable<Type> GetWindowTypesToPatch()
    {

        //Manual additions due to inconsistency in game code
        var manualTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(t => t.GetTypes())
                    .Where(t => MANUAL_TYPES.Contains(t.Name));

        var windowTypes = PatchUtils.ScanForInterface("IProgrammaticWindow")
            .Union(PatchUtils.ScanForInterface("IBuilderWindow"))
            .Union(PatchUtils.ScanForRequireComponent(typeof(Window)))
            .Union(manualTypes)
            .Distinct();

        //if(manualTypes != null) 
        //{
        //    Logger.LogDebug(() =>
        //    {
        //        StringBuilder sb = new();

        //        sb.AppendLine($"GetWindowTypesToPatch Found {manualTypes.Count()} methods to patch");

        //        foreach (var method in manualTypes)
        //            sb.AppendLine($"GetWindowTypesToPatch: {method.Name}.{method.Name}");

        //        return sb.ToString();
        //    });
        //    windowTypes = windowTypes.Union(manualTypes).Distinct();
        //}

        return windowTypes.Where(HasPropertyToPatch);
    }

    private static bool HasPropertyToPatch(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        var hasShared = properties.Any(p => p.Name.Equals("shared", StringComparison.OrdinalIgnoreCase));
        var hasInstance = properties.Any(p => p.Name.Equals("instance", StringComparison.OrdinalIgnoreCase));

        Logger.LogDebug($"HasPropertyToPatch({type.Name}) {hasShared || hasInstance}");
        return hasShared || hasInstance;
    }

    public static IEnumerable<MethodBase> GetMethodsToPatch(Type type)
    {
        var methods = new List<MethodBase>();

        var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        var sharedProp = properties.FirstOrDefault(p => p.Name.Equals("shared", StringComparison.OrdinalIgnoreCase));
        if (sharedProp?.GetGetMethod(true) != null)  // true to get non-public accessor
            methods.Add(sharedProp.GetGetMethod(true));

        var instanceProp = properties.FirstOrDefault(p => p.Name.Equals("instance", StringComparison.OrdinalIgnoreCase));
        if (instanceProp?.GetGetMethod(true) != null)  // true to get non-public accessor
            methods.Add(instanceProp.GetGetMethod(true));

        return methods;
    }
}   