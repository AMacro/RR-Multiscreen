using Humanizer;
using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Multiscreen.Util;
public static class WinNativeUtil
{
    public const int GWL_STYLE = (-16);

    public const UInt32 WS_POPUP = 0x80000000;
    public const UInt32 WS_CHILD = 0x40000000;
    public const short SWP_NOMOVE = 0X2;
    public const short SWP_NOSIZE = 1;
    public const short SWP_NOZORDER = 0X4;
    public const int SWP_SHOWWINDOW = 0x0040;


    public const UInt32 WS_BORDER = 0x00800000;
    public const UInt32 WS_CAPTION =	0x00C00000;
    public const UInt32 WS_CLIPSIBLINGS =	0x04000000;
    public  const int WS_SYSMENU = 0x00080000;

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, UInt32 dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);


    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

}

public class DisplaySwapTest : MonoBehaviour
{
    private void Start()
    {
        Multiscreen.LogDebug(() => $"DisplaySwapTest Start()");
        StartCoroutine(FindPos());
    }
    public IEnumerator FindPos()
    {
        int i = 0;
        int mainDisp;
        WinNativeUtil.RECT mainDispPos;



        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);


        Multiscreen.LogDebug(() => $"Moving to display 0");
        Screen.MoveMainWindowTo(displays[0], new Vector2Int(0, 0));

        yield return null;

        Multiscreen.LogDebug(() => $"Moved to display 0");


        mainDispPos = new WinNativeUtil.RECT();

        WinNativeUtil.GetWindowRect(Process.GetCurrentProcess().MainWindowHandle, ref mainDispPos);

        Multiscreen.LogDebug(() => $"Display {i} CoOrds: {mainDispPos.left}, {mainDispPos.top}");

        Multiscreen.LogDebug(() => $"Moving to display {Multiscreen.gameDisplay}");
        Screen.MoveMainWindowTo(displays[Multiscreen.gameDisplay], new Vector2Int(0, 0));

        if (Display.displays[1].active == false)
        {
            Multiscreen.LogDebug(() => $"Display 1: NOT ACTIVE");
            Multiscreen.Activate();
        }

        yield return null;

        if (Display.displays[1].active == true)
        {
            Multiscreen.LogDebug(() => $"Preparing to move secondary display");
            IntPtr hWnd = WinNativeUtil.FindWindow("UnityWndClass", "Unity Secondary Display");
            Multiscreen.LogDebug(() => $"Secondary Display hWnd: {hWnd}");

            if (hWnd != IntPtr.Zero)
            {
                Multiscreen.LogDebug(() => $"Moving Secondary Display...");
                WinNativeUtil.MoveWindow(hWnd, mainDispPos.left, mainDispPos.top, mainDispPos.right, mainDispPos.bottom, true);
                Multiscreen.LogDebug(() => $"Moved");
            }
            //WinNativeUtil.SetWindowPos();
        }


    }

}

