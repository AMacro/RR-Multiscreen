using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Multiscreen.Util;

public static class PatchUtils
{
    public static Type[] ScanForInterface(string interfaceName)
    {
        var interfaceType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == interfaceName);

        if (interfaceType != null)
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
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
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(type =>
                type.GetCustomAttributes<RequireComponent>()
                    .Any(attr =>
                        (attr.m_Type0 != null && attr.m_Type0 == component) ||
                        (attr.m_Type1 != null && attr.m_Type1 == component) ||
                        (attr.m_Type2 != null && attr.m_Type2 == component)
                    ))
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
