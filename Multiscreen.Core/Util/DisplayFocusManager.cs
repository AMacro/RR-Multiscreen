using System;
using System.Collections.Generic;
using System.Text;

namespace Multiscreen.Util;

using System.Runtime.InteropServices;
using UnityEngine;
public class DisplayFocusManager : MonoBehaviour
{
    private int lastScreen;
    private Dictionary<int, IntPtr> displayToWindow = new Dictionary<int, IntPtr>();

    #region Windows API

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessId();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    // GetWindowRect() structure
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    // GetWindow() constants
    private const uint GW_HWNDNEXT = 3;
    private const uint GW_HWNDPREV = 2;

    //GetWindowText() constants
    private const int MAX_WINDOW_TITLE_LENGTH = 256;
    #endregion

    private void Start()
    {
        Logger.LogDebug($"DisplayFocusManager.Start() runInBackground: {Application.runInBackground}");
        Application.runInBackground = true;

        Vector3 mousePos = Display.RelativeMouseAt(Input.mousePosition);
        lastScreen = mousePos.z >= 0 ? (int)mousePos.z : 0;


        Logger.LogDebug($"DisplayFocusManager.Start()  Enumerating Windows...");

        var windows = FindCurrentProcessWindows();
        foreach (var window in windows)
        {
            Logger.LogDebug($"Window hWnd: {window.Item1}, Title: \"{window.Item2}\"");

            if (window.Item2 == Application.productName || window.Item2 == "Railroader")
                displayToWindow[0] = window.Item1;
            else if (window.Item2 == "Unity Secondary Display")
                displayToWindow[1] = window.Item1;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Logger.LogDebug($"DisplayFocusManager.OnApplicationFocus({hasFocus})");
    }

    private void Update()
    {
        Vector3 mousePos = Display.RelativeMouseAt(Input.mousePosition);
        int currentScreen = mousePos.z >= 0 ? (int)mousePos.z : 0;

        if (currentScreen != lastScreen)
        {
            Logger.LogDebug($"DisplayFocusManager.Update() Mouse Moved last: {lastScreen}, new: {currentScreen}, applicationIsFocused: {Application.isFocused}");

            lastScreen = currentScreen;

            if (Application.isFocused)
            {
                GetFocus();
            }
        }
    }

    private List<Tuple<IntPtr, string>> FindCurrentProcessWindows()
    {
        List<Tuple<IntPtr, string>> processWindows = new List<Tuple<IntPtr, string>>();
        uint currentPID = GetCurrentProcessId();

        IntPtr shellWindow = GetActiveWindow();
        IntPtr windowHandle = shellWindow;

        while (windowHandle != IntPtr.Zero)
        {
            uint windowPID;
            GetWindowThreadProcessId(windowHandle, out windowPID);

            if (windowPID == currentPID)
            {
                StringBuilder title = new StringBuilder(MAX_WINDOW_TITLE_LENGTH);
                GetWindowText(windowHandle, title, MAX_WINDOW_TITLE_LENGTH);

                Tuple<IntPtr, string> newWin = new (windowHandle, title.ToString());

                processWindows.Add(newWin);
            }

            windowHandle = GetWindow(windowHandle, GW_HWNDPREV);
        }

        return processWindows;
    }

    private void GetFocus()
    {
        if (displayToWindow.TryGetValue(lastScreen, out IntPtr windowHandle))
        {
            //Make sure if the user has brought another window on top of this display, we don't steal focus

            if (IsWindowTopMostOnDisplay(windowHandle))
            {
                Logger.LogDebug($"GetFocus() Setting focus to window handle: {windowHandle} for display {lastScreen}");
                SetForegroundWindow(windowHandle);
            }else
            {
                Logger.LogDebug($"GetFocus() Skipping focus");
            }
        }
    }

    private bool IsWindowTopMostOnDisplay(IntPtr windowHandle)
    {
        // Get the monitor for our window
        IntPtr monitor = MonitorFromWindow(windowHandle, MONITOR_DEFAULTTONEAREST);
        Logger.LogDebug($"IsWindowTopMostOnDisplay() MonitorFromWindow({windowHandle}): {monitor}, for display {lastScreen}");

        // Get our window's rectangle
        RECT windowRect;
        GetWindowRect(windowHandle, out windowRect);
        Logger.LogDebug($"IsWindowTopMostOnDisplay() Target window rect: {windowHandle}: {{({windowRect.Left},{windowRect.Top}),({windowRect.Right},{windowRect.Bottom})}}");

        // Start from the foreground window and walk down the Z-order
        IntPtr currentWindow = GetForegroundWindow();
        
        Logger.LogDebug(()=> 
            {
                StringBuilder fgTitle = new StringBuilder(MAX_WINDOW_TITLE_LENGTH);
                GetWindowText(currentWindow, fgTitle, MAX_WINDOW_TITLE_LENGTH);

                return $"IsWindowTopMostOnDisplay() Starting at ForegroundWindow: {currentWindow}, Title: \"{fgTitle}\"";
        
            });

        while (currentWindow != IntPtr.Zero)
        {

            Logger.LogDebug(() =>
                {
                    StringBuilder title = new StringBuilder(MAX_WINDOW_TITLE_LENGTH);
                    GetWindowText(currentWindow, title, MAX_WINDOW_TITLE_LENGTH);

                    return $"IsWindowTopMostOnDisplay() Checking window: {currentWindow}, Title: \"{title}\"";

                });

            if (MonitorFromWindow(currentWindow, MONITOR_DEFAULTTONEAREST) == monitor)
            {

                if (currentWindow == windowHandle)
                {
                    Logger.LogDebug($"IsWindowTopMostOnDisplay() Found our window - nothing above it");
                    return true;
                }

                RECT currentRect;
                GetWindowRect(currentWindow, out currentRect);
                Logger.LogDebug($"IsWindowTopMostOnDisplay() On same monitor - Window: {currentWindow}, Rect: {{({currentRect.Left},{currentRect.Top}),({currentRect.Right},{currentRect.Bottom})}}");

                if (RectsOverlap(windowRect, currentRect))
                {
                    Logger.LogDebug($"IsWindowTopMostOnDisplay() Found overlapping window above target");
                    return false;
                }
            }

            currentWindow = GetWindow(currentWindow, GW_HWNDNEXT);
        }

        Logger.LogDebug($"IsWindowTopMostOnDisplay() Reached end of window list");
        return true;
    }

    private bool RectsOverlap(RECT r1, RECT r2)
    {
        return !(r2.Left > r1.Right ||
                 r2.Right < r1.Left ||
                 r2.Top > r1.Bottom ||
                 r2.Bottom < r1.Top);
    }
}
