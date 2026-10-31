using HarmonyLib;
using Multiscreen.Util;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Menus;

// Quitting to the main menu: dock every window back first, so the game saves main-display positions
// and nothing is left on the second display's canvas when the game UI unloads.
[HarmonyPatch(typeof(PauseMenu))]
public static class PauseMenuPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("_Quit")]
    private static void Quit()
    {
        Logger.LogDebug("PauseMenu._Quit(): docking windows");
        WindowUtils.ReleaseUndocked();
    }
}
