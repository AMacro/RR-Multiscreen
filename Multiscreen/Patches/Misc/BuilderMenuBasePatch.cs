using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Builder;
using UI.Menu;

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
            Multiscreen.Log($"BuilderMenuBase OnEnable {__instance.name}");
            Multiscreen.Log($"BuilderMenuBase Panel Assets {__instance.panelAssets != null} {__instance.panelAssets?.button?.name}");
            builderAssets = __instance.panelAssets;
        }
    }
}
