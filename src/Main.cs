// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Added in 2026 for the Railroader 0.6 port; see NOTICE.

using System;
using Multiscreen.Util;
using Railroader.Modding;

namespace Multiscreen;

// Railroader 0.6 entry point. The game constructs this and calls OnModLoad at boot, before the main menu
// scene loads, and may unload and reload it when a save with a different mod set is loaded.
public sealed class MultiscreenMod : IModEntryPoint, IWorldLifecycle
{
    public void OnModLoad(IModContext context)
    {
        Logger.Sink = context.Log;
        try
        {
            Multiscreen.Load(context.ModDirectory);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Multiscreen failed to load and is inactive: {ex}");
            Multiscreen.Unload();
        }
    }

    public void OnModUnload()
    {
        try { Multiscreen.Unload(); }
        catch (Exception ex) { Logger.LogWarning($"Multiscreen unload: {ex.Message}"); }
    }

    public void OnWorldLoad()
    {
    }

    // The game UI scene is already gone by now (it unloads before world-unload); this only sweeps up.
    public void OnWorldUnload()
    {
        try { WindowUtils.ReleaseUndocked(); }
        catch (Exception ex) { Logger.LogWarning($"Multiscreen world unload: {ex.Message}"); }
    }
}
