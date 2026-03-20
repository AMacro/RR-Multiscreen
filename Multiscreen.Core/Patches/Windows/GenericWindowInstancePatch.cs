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
        var types = GenericWindowInstanceHelper.GetWindowTypesToPatch();
        var methods = types.SelectMany(t => GenericWindowInstanceHelper.GetMethodsToPatch(t)).ToList();

        Logger.LogDebug(()=>
        {
            StringBuilder sb = new();

            sb.AppendLine($"GenericWindowInstancePatch Found {methods.Count} methods to patch");

            foreach (var method in methods)
                sb.AppendLine($"Will patch: {method.DeclaringType.Name}.{method.Name}");

            return sb.ToString();
        });

        return methods;
    }

    public static bool Prefix(MethodBase __originalMethod, ref object __result)
    {

        var windowType = __originalMethod.DeclaringType;
        Logger.LogVerbose($"Custom Instance/Shared getter called {windowType}");

        GameObject undockParent = GameObject.Find(Multiscreen.UNDOCK);

        if (undockParent != null)
        {
            foreach (Transform child in undockParent.transform)
            {
                var component = child.GetComponent(windowType);
                if (component != null)
                {
                    Logger.LogVerbose($"Custom Instance/Shared getter called {windowType} Found!");
                    __result = component;
                    return false;
                }
            }
        }

        Logger.LogVerbose($"Custom Instance/Shared getter called {windowType} Not found, passing to default!");

        return true;
    }
}

public static class GenericWindowInstanceHelper
{

    static readonly string[] MANUAL_TYPES = { "EngineRosterPanel", "PlacerWindow" };

    public static IEnumerable<Type> GetWindowTypesToPatch()
    {

        //Manual additions due to inconsistency in game code
        //Using PatchUtils.GetAllTypesSafe() instead of raw GetTypes() to handle
        //ReflectionTypeLoadException after Unity engine updates
        var manualTypes = PatchUtils.GetAllTypesSafe()
                    .Where(t => MANUAL_TYPES.Contains(t.Name));

        var windowTypes = PatchUtils.ScanForInterface("IProgrammaticWindow")
            .Union(PatchUtils.ScanForInterface("IBuilderWindow"))
            .Union(PatchUtils.ScanForRequireComponent(typeof(Window)))
            .Union(manualTypes)
            .Distinct();

        return windowTypes.Where(HasPropertyToPatch);
    }

    private static bool HasPropertyToPatch(Type type)
    {
        try
        {
            var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            var hasShared = properties.Any(p => p.Name.Equals("shared", StringComparison.OrdinalIgnoreCase));
            var hasInstance = properties.Any(p => p.Name.Equals("instance", StringComparison.OrdinalIgnoreCase));

            Logger.LogDebug($"HasPropertyToPatch({type.Name}) {hasShared || hasInstance}");
            return hasShared || hasInstance;
        }
        catch (Exception ex)
        {
            Logger.LogDebug($"Error checking properties on type {type.Name}: {ex.Message}");
            return false;
        }
    }

    public static IEnumerable<MethodBase> GetMethodsToPatch(Type type)
    {
        var methods = new List<MethodBase>();

        try
        {
            var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            var sharedProp = properties.FirstOrDefault(p => p.Name.Equals("shared", StringComparison.OrdinalIgnoreCase));
            if (sharedProp?.GetGetMethod(true) != null)  // true to get non-public accessor
                methods.Add(sharedProp.GetGetMethod(true));

            var instanceProp = properties.FirstOrDefault(p => p.Name.Equals("instance", StringComparison.OrdinalIgnoreCase));
            if (instanceProp?.GetGetMethod(true) != null)  // true to get non-public accessor
                methods.Add(instanceProp.GetGetMethod(true));
        }
        catch (Exception ex)
        {
            Logger.LogDebug($"Error getting methods to patch on type {type.Name}: {ex.Message}");
        }

        return methods; 
    }
}
