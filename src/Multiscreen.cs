// Multiscreen by Macka (https://github.com/AMacro/RR-Multiscreen), Apache-2.0.
// Modified in 2026 for the Railroader 0.6 port; see NOTICE.

using System;
using System.Collections.Generic;
using HarmonyLib;
using Multiscreen.Patches.Map;
using Multiscreen.Patches.Menus;
using Multiscreen.Patches.Misc;
using Multiscreen.Patches.Windows;
using Multiscreen.Util;
using UnityEngine;
using UnityEngine.UI;
using Logger = Multiscreen.Util.Logger;

namespace Multiscreen;

public static class Multiscreen
{
    public const string HarmonyId = "macka.multiscreen";

    public static Settings settings;
    public static int gameDisplay = 0;
    public static int secondDisplay = 1;
    public static int targetDisplay = 0;
    public static RawImage background;
    public static bool focusManager;

    public static bool userPrefFullScr;

    public const string UNDOCK = "Canvas - Undock";
    public const string DISPLAY_FOCUS_MANAGER = "DisplayFocusManager";

    // The second display's canvas; windows parented here render on targetDisplay.
    public static Canvas undockCanvas;

    // Unity displays can't be deactivated, so activation (and the canvas it creates) happens once per process,
    // even when the game unloads and reloads the mod.
    private static bool _activated;
    private static Harmony _harmony;

    // Every patch class, applied one at a time so a target missing in a future game build only disables
    // that one feature (logged once) instead of the whole mod.
    private static readonly Type[] PatchClasses =
    {
        typeof(TransformEnumeratorPatch.MoveNext),
        typeof(TransformEnumeratorPatch.Current),
        typeof(WindowManagerPatch),
        typeof(WindowManagerEnumeratePatch),
        typeof(WindowPatch),
        typeof(GameInputPatch),
        typeof(MultipleDisplayUtilitiesPatch),
        typeof(MapDragPatch),
        typeof(MapWindowPatch),
        typeof(CompanyWindowPatch),
        typeof(SettingsBuilderPatch),
        typeof(MainMenuPatch),
        typeof(MenuManagerPatch),
        typeof(PauseMenuPatch),
    };

    public static bool IsActive => undockCanvas != null;

    internal static void Load(string modDirectory)
    {
        settings = Settings.Load(modDirectory);

        Logger.LogInfo($"Multiscreen: game {Application.version}, Unity {Application.unityVersion}, {SystemInfo.graphicsDeviceType}");

        if (!_activated)
            userPrefFullScr = Screen.fullScreen;
        Logger.LogDebug($"User preference 'Full screen': {userPrefFullScr}");

        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);
        Logger.LogInfo($"Multiscreen: {Display.displays.Length} Unity display(s), {displays.Count} in the layout");
        for (int i = 0; i < displays.Count; i++)
            Logger.LogDebug($"Display {i} ({displays[i].name}): {displays[i].width}x{displays[i].height} @ {displays[i].refreshRate} Hz");

        if (Display.displays.Length <= 1)
        {
            Logger.LogInfo("Multiscreen: fewer than 2 displays detected, nothing to do");
            return;
        }

        if (!_activated)
        {
            ChooseDisplays();
            Activate(displays);
            _activated = true;
        }
        else
        {
            // Keep the activation-time displays: the canvas can't move until a restart, even if the
            // settings now name another monitor (changed, then "Later" on the restart prompt).
            Logger.LogInfo($"Multiscreen: display {targetDisplay} already active (mod reloaded)");
        }

        if (undockCanvas == null)
        {
            Logger.LogWarning("Multiscreen: the second display canvas is missing; staying inactive");
            return;
        }

        ApplyPatches();

        if (settings.focusManager)
            EnableDisplayFocusManager();
    }

    internal static void Unload()
    {
        WindowUtils.ReleaseUndocked();
        EnableDisplayFocusManager(false);
        _harmony?.UnpatchAll(HarmonyId);
        _harmony = null;
    }

    private static void ApplyPatches()
    {
        _harmony = new Harmony(HarmonyId);
        int applied = 0;
        foreach (Type patch in PatchClasses)
        {
            try
            {
                _harmony.CreateClassProcessor(patch).Patch();
                applied++;
            }
            catch (Exception ex)
            {
                Exception inner = ex.InnerException ?? ex;
                Logger.LogWarningOnce("patch:" + patch.FullName, $"Multiscreen: skipping {patch.Name}, its target is missing or changed on this game version: {inner.Message}");
            }
        }
        Logger.LogInfo($"Multiscreen: {applied} of {PatchClasses.Length} patches applied");
    }

    private static void ChooseDisplays()
    {
        gameDisplay = settings.gameDisplay;
        secondDisplay = settings.secondDisplay;

        if (gameDisplay == secondDisplay)
        {
            gameDisplay = 0;
            secondDisplay = 1;
        }

        if (gameDisplay < 0 || gameDisplay >= Display.displays.Length)
            gameDisplay = 0;

        if (secondDisplay < 0 || secondDisplay >= Display.displays.Length)
            secondDisplay = 1;

        // To use display 0 for the second screen we target display 1, then move the main window to display 0
        targetDisplay = secondDisplay == 0 ? 1 : secondDisplay;

        Logger.LogDebug($"Game display: {gameDisplay}, second display: {secondDisplay}, target display: {targetDisplay}");
    }

    private static void Activate(List<DisplayInfo> displayInfo)
    {
        Display target = Display.displays[targetDisplay];
        int width2 = target.systemWidth;
        int height2 = target.systemHeight;

        Logger.LogDebug($"Display 0: {Display.displays[gameDisplay].renderingWidth}x{Display.displays[gameDisplay].renderingHeight} Full screen: {Screen.fullScreen}");
        Logger.LogDebug($"Display {targetDisplay}: {width2}x{height2}");

        bool canMove = gameDisplay < displayInfo.Count;
        if (canMove)
            Screen.MoveMainWindowTo(displayInfo[gameDisplay], Vector2Int.zero);

        Logger.LogInfo($"Multiscreen: activating display {targetDisplay}...");

        // Display.Activate drops Screen.fullScreen (Unity UUM-59482); MenuManagerPatch restores the user's choice.
        Screen.fullScreen = true;
        target.Activate();
        target.SetRenderingResolution(width2, height2);
        target.SetParams(width2, height2, 0, 0);

        // Activating a display can push the main window back to the first monitor on 3+ monitor setups
        // (Unity UUM-140744); put it back where it was asked to be.
        if (canMove && gameDisplay != 0)
            Screen.MoveMainWindowTo(displayInfo[gameDisplay], Vector2Int.zero);

        // A camera for the display; it renders nothing but lets the overlay canvas present there
        GameObject camGO = new GameObject("SecondDisplayCam");
        Camera camera = camGO.AddComponent<Camera>();
        camera.targetDisplay = targetDisplay;
        camera.cullingMask = 0; // fix for trees rendering at the top of the screen
        UnityEngine.Object.DontDestroyOnLoad(camGO);

        GameObject canvasGO = new GameObject(UNDOCK);
        canvasGO.layer = 5; // UI layer
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1;
        canvas.worldCamera = camera;
        canvas.targetDisplay = targetDisplay;

        background = canvasGO.AddComponent<RawImage>();
        background.enabled = settings.solidBG;
        if (TryParseColour(settings.bgColour, out Color colour))
        {
            background.color = colour;
        }
        else
        {
            settings.bgColour = "000000";
            background.color = Color.black;
        }

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        UnityEngine.Object.DontDestroyOnLoad(canvasGO);
        canvasGO.SetActive(true);

        undockCanvas = canvas;
        Logger.LogInfo($"Multiscreen: display {targetDisplay} activated");
    }

    // Held by reference and removed with DestroyImmediate: the game unloads and reloads mods in one frame,
    // and a deferred Destroy would still be found (and kept dying) by the reload.
    private static GameObject _focusManagerGO;

    public static void EnableDisplayFocusManager(bool enable = true)
    {
        bool present = _focusManagerGO != null;
        if (enable == present)
        {
            focusManager = enable;
            return;
        }

        if (enable)
        {
            Logger.LogInfo("Multiscreen: enabling display focus manager");
            _focusManagerGO = new GameObject(DISPLAY_FOCUS_MANAGER);
            _focusManagerGO.AddComponent<DisplayFocusManager>();
            UnityEngine.Object.DontDestroyOnLoad(_focusManagerGO);
            _focusManagerGO.SetActive(true);
        }
        else
        {
            Logger.LogInfo("Multiscreen: removing display focus manager");
            UnityEngine.Object.DestroyImmediate(_focusManagerGO);
            _focusManagerGO = null;
        }

        focusManager = enable;
    }

    public static bool TryParseColour(string hex, out Color colour)
    {
        return Helpers.ColorHelper.TryColorFromHex(hex, out colour);
    }
}
