using HarmonyLib;
using Multiscreen.Util;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen.Patches.Menus;

[HarmonyPatch(typeof(PauseMenu))]
public class PauseMenuPatch

{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu._Quit))]
    private static void _Quit(PauseMenu __instance)
    {
        Logger.LogDebug($"_Quit()");

        WindowUtils.ReturnAllWindowsToMain();

        Logger.LogDebug($"_Quit() All windows closed");
    }

}
