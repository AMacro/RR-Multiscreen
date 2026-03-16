using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Multiscreen.Util;

public static class PatchUtils
{
    /// <summary>
    /// Safely gets types from an assembly, handling ReflectionTypeLoadException
    /// that occurs when some types reference missing dependencies (common after
    /// Unity engine updates).
    /// </summary>
    public static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            Logger.LogDebug($"ReflectionTypeLoadException in assembly {assembly.FullName}, returning {ex.Types.Count(t => t != null)} of {ex.Types.Length} types");
            return ex.Types.Where(t => t != null);
        }
        catch (Exception ex)
        {
            Logger.LogDebug($"Exception loading types from assembly {assembly.FullName}: {ex.Message}");
            return Enumerable.Empty<Type>();
        }
    }

    /// <summary>
    /// Gets all types from all loaded assemblies, safely handling load failures.
    /// </summary>
    public static IEnumerable<Type> GetAllTypesSafe()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => SafeGetTypes(a));
    }

    public static Type[] ScanForInterface(string interfaceName)
    {
        var interfaceType = GetAllTypesSafe()
            .FirstOrDefault(t => t.Name == interfaceName);

        if (interfaceType != null)
        {
            var types = GetAllTypesSafe()
                .Where(p => !p.IsInterface && interfaceType.IsAssignableFrom(p))
                .ToArray();

            Logger.LogDebug(() =>
            {
                StringBuilder sb = new();
                foreach (var type in types)
                    sb.AppendLine($"Interface {interfaceType.FullName}: class: {type.FullName}");

                return sb.ToString();
            });

            return types;
        }

        Logger.LogInfo($"[Warning] Interface {interfaceName} not found. This may be due to main / beta branch differences");
        return [];
    }

    public static Type[] ScanForRequireComponent(Type component)
    {
        var types = GetAllTypesSafe()
            .Where(type =>
            {
                try
                {
                    return type.GetCustomAttributes<RequireComponent>()
                        .Any(attr =>
                            (attr.m_Type0 != null && attr.m_Type0 == component) ||
                            (attr.m_Type1 != null && attr.m_Type1 == component) ||
                            (attr.m_Type2 != null && attr.m_Type2 == component)
                        );
                }
                catch (Exception ex)
                {
                    Logger.LogDebug($"Error checking RequireComponent on type {type.FullName}: {ex.Message}");
                    return false;
                }
            })
            .ToArray();

        Logger.LogDebug(() =>
        {
            StringBuilder sb = new();
            foreach (var type in types)
                sb.AppendLine($"Class: {type.FullName} requires component {component.Name}");

            return sb.ToString();
        });

        return types;
    }
}
