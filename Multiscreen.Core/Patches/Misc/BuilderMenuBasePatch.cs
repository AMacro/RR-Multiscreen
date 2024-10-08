using HarmonyLib;
using UI.Builder;
using UI.Menu;
using Multiscreen.Util;

namespace Multiscreen.Patches.Misc
{
    [HarmonyPatch]
    public static class BuilderMenuBasePatch
    {
        public static UIBuilderAssets builderAssets;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuilderMenuBase), nameof(BuilderMenuBase.OnEnable))]
        public static void OnEnable(BuilderMenuBase __instance)
        {
            Logger.LogTrace($"BuilderMenuBase OnEnable {__instance.name}");
            Logger.LogDebug($"BuilderMenuBase Panel Assets {__instance.panelAssets != null} {__instance.panelAssets?.button?.name}");
            builderAssets = __instance.panelAssets;
        }
    }
}
