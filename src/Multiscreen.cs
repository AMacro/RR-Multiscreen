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
    // Owner id for Harmony; kept distinct from any other Multiscreen build so unpatching only removes ours.
    public const string HarmonyId = "multiscreen.port06";

    public static Settings settings;
    public static int gameDisplay = 0;
    public static int secondDisplay = 1;
    public static int targetDisplay = 1;
    public static RawImage background;
    public static bool focusManager;

    // Set when Display.Activate turned off a fullscreen the player had at that moment; consumed once.
    internal static bool fullscreenRestorePending;

    public const string UNDOCK = "Canvas - Undock";
    public const string DISPLAY_FOCUS_MANAGER = "DisplayFocusManager";

    // The second display's canvas; windows parented here render on targetDisplay.
    public static Canvas undockCanvas;

    // Unity displays can't be deactivated, so activation (and the canvas it creates) happens once per process,
    // even when the game unloads and reloads the mod.
    private static bool _activated;
    private static Harmony _harmony;

    // Applied one class at a time: a target missing in a future game build disables only that feature
    // (logged once). The Transform enumerator pair is not in this list; it is applied as one unit and
    // self-tested in ApplyUndockCore, and undocking stays off unless it passes.
    private static readonly Type[] PatchClasses =
    {
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

        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);
        LogDisplays(displays);

        if (Display.displays.Length <= 1)
        {
            Logger.LogInfo("Multiscreen: fewer than 2 displays detected, nothing to do");
            return;
        }

        if (!_activated)
        {
            ChooseDisplays();
            // Set first: if activation throws part way, a reload must not try to activate again
            _activated = true;
            Activate(displays);
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

        _harmony = new Harmony(HarmonyId);
        WindowUtils.UndockAvailable = ApplyUndockCore();
        ApplyPatches();

        if (settings.focusManager)
            EnableDisplayFocusManager();
    }

    internal static void Unload()
    {
        WindowUtils.ReleaseUndocked();
        WindowUtils.UndockAvailable = false;
        EnableDisplayFocusManager(false);
        _harmony?.UnpatchAll(HarmonyId);
        _harmony = null;
    }

    // The two Transform.Enumerator patches only work together (MoveNext alone makes every GetWindow<T> throw
    // out of bounds; neither leaves undocked windows unfindable), so they go on as one unit and are proven
    // through a real foreach before anything is allowed onto the second display.
    private static bool ApplyUndockCore()
    {
        if (!TransformEnumeratorPatch.TryApply(_harmony, out string why))
        {
            Logger.LogWarning($"Multiscreen: undocking disabled, the window lookup patch could not be applied: {why}");
            return false;
        }

        if (!TransformEnumeratorPatch.SelfTest(undockCanvas.transform, out why))
        {
            TransformEnumeratorPatch.Remove(_harmony);
            Logger.LogWarning($"Multiscreen: undocking disabled, the window lookup self-test failed: {why}");
            return false;
        }

        Logger.LogInfo("Multiscreen: window lookup patch applied and self-test passed");
        return true;
    }

    private static void ApplyPatches()
    {
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
        Logger.LogInfo($"Multiscreen: {applied} of {PatchClasses.Length} feature patches applied, undocking {(WindowUtils.UndockAvailable ? "on" : "off")}");
    }

    private static void LogDisplays(List<DisplayInfo> layout)
    {
        Logger.LogInfo($"Multiscreen: {Display.displays.Length} Unity display(s), {layout.Count} in the layout");
        for (int i = 0; i < Display.displays.Length; i++)
            Logger.LogInfo($"Multiscreen: Unity display {i}: {Display.displays[i].systemWidth}x{Display.displays[i].systemHeight}");
        for (int i = 0; i < layout.Count; i++)
        {
            DisplayInfo d = layout[i];
            Logger.LogInfo($"Multiscreen: layout {i} \"{d.name}\": {d.width}x{d.height} @ {d.refreshRate} Hz, work area {d.workArea}");
        }
    }

    // The game and the second screen must be different monitors, and Unity display 0 always holds the
    // game window, so the second screen is 1 or higher.
    public static bool IsValidPair(int game, int second, int count) =>
        game >= 0 && game < count && second >= 1 && second < count && second != game;

    public static int FirstSecondFor(int game, int count)
    {
        for (int i = 1; i < count; i++)
            if (i != game)
                return i;
        return -1;
    }

    private static void ChooseDisplays()
    {
        int count = Display.displays.Length;
        int game = settings.gameDisplay;
        int second = settings.secondDisplay;

        // Out of range first, then the pair as a whole
        if (game < 0 || game >= count)
            game = 0;
        if (second < 1 || second >= count)
            second = -1;

        if (!IsValidPair(game, second, count))
        {
            second = FirstSecondFor(game, count);
            if (second < 0)
            {
                game = 0;
                second = 1;
            }
        }

        if (game != settings.gameDisplay || second != settings.secondDisplay)
            Logger.LogInfo($"Multiscreen: display settings game {settings.gameDisplay} / second {settings.secondDisplay} adjusted to game {game} / second {second}");

        gameDisplay = game;
        secondDisplay = second;
        targetDisplay = second;

        Logger.LogInfo($"Multiscreen: game display {gameDisplay}, second display {secondDisplay}");
    }

    private static void Activate(List<DisplayInfo> displayInfo)
    {
        Display target = Display.displays[targetDisplay];
        int width2 = target.systemWidth;
        int height2 = target.systemHeight;

        Logger.LogDebug($"Display {targetDisplay}: {width2}x{height2}, full screen now: {Screen.fullScreen}");

        bool canMove = gameDisplay < displayInfo.Count;
        if (canMove)
            Screen.MoveMainWindowTo(displayInfo[gameDisplay], Vector2Int.zero);

        Logger.LogInfo($"Multiscreen: activating display {targetDisplay}...");

        // Display.Activate drops Screen.fullScreen (Unity UUM-59482). Remember whether the player was in
        // fullscreen right now; MenuManagerPatch puts it back once if activation took it away.
        bool wasFullScreen = Screen.fullScreen;
        Screen.fullScreen = true;
        target.Activate();
        target.SetRenderingResolution(width2, height2);
        target.SetParams(width2, height2, 0, 0);
        fullscreenRestorePending = wasFullScreen;

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
