using HarmonyLib;
using Multiscreen.Util;
using Multiscreen.Utils;
using UI.Map;
using UnityEngine;

namespace Multiscreen.Patches.Windows;

[HarmonyPatch(typeof(MapWindow))]
public static class MapWindowPatch
{
    public static MapWindow _instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapWindow), nameof(MapWindow.Start))]
    private static bool Start(MapWindow __instance)
    {
        _instance = __instance;
        Multiscreen.Log($"MapWindow.Start() {__instance.name}");
        __instance.SetDisplay(true);

        //Display.displays[0].SetRenderingResolution(Display.displays[0].systemWidth - 400, Display.displays[0].systemHeight - 400);
        //WinNativeUtil.SetWindowLong(Multiscreen.hwnd, WinNativeUtil.GWL_STYLE, WinNativeUtil.WS_BORDER | WinNativeUtil.WS_CLIPSIBLINGS | WinNativeUtil.WS_CAPTION);
        //WinNativeUtil.SetWindowPos(Multiscreen.hwnd, 0, 200, 200,Display.displays[0].renderingWidth, Display.displays[0].renderingHeight,WinNativeUtil.SWP_SHOWWINDOW);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapWindow), nameof(MapWindow.instance), MethodType.Getter)]
    private static bool Instance_Get(MapWindow __instance, ref MapWindow __result)
    {
        __result = _instance;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapWindow), nameof(MapWindow.OnZoom))]
    private static void OnZoom(MapWindow __instance, float delta, Vector2 viewportNormalizedPoint)
    {
        Multiscreen.Log($"MapWindow.OnZoom({delta}, {viewportNormalizedPoint})");

    }
    
}