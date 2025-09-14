// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Drawing;
using System.Runtime.InteropServices;

namespace JPSoftworks.QrCodesExtension.Helpers;

internal static partial class NativeScreenCapture
{
    private const string User32 = "user32.dll";
    private const string Gdi32 = "gdi32.dll";

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const uint SRCCOPY = 0x00CC0020;

    [LibraryImport(User32)]
    private static partial IntPtr GetDesktopWindow();

    [LibraryImport(User32)]
    private static partial IntPtr GetWindowDC(IntPtr hWnd);

    [LibraryImport(User32)]
    private static partial int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [LibraryImport(Gdi32)]
    private static partial IntPtr CreateCompatibleDC(IntPtr hdc);

    [LibraryImport(Gdi32)]
    private static partial IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [LibraryImport(Gdi32)]
    private static partial IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [LibraryImport(Gdi32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BitBlt(
        IntPtr hdcDest,
        int nXDest,
        int nYDest,
        int nWidth,
        int nHeight,
        IntPtr hdcSrc,
        int nXSrc,
        int nYSrc,
        uint dwRop);

    [LibraryImport(Gdi32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteDC(IntPtr hdc);

    [LibraryImport(Gdi32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(IntPtr hObject);

    [LibraryImport(User32)]
    private static partial int GetSystemMetrics(int nIndex);

    [LibraryImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumProc lpfnEnum,
        IntPtr dwData);

    [LibraryImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    public static List<ScreenInfo> GetAllScreens()
    {
        var screens = new List<ScreenInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            (hMonitor, hdcMonitor, ref lprcMonitor, dwData) =>
            {
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };

                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    screens.Add(new ScreenInfo
                    {
                        Bounds = new Rectangle(
                            mi.rcMonitor.Left,
                            mi.rcMonitor.Top,
                            mi.rcMonitor.Width,
                            mi.rcMonitor.Height),
                        IsPrimary = (mi.dwFlags & 1) == 1, // MONITORINFOF_PRIMARY = 1
                        Handle = hMonitor
                    });
                }

                return true;
            }, IntPtr.Zero);

        return screens;
    }

    public static Rectangle GetVirtualScreenBounds()
    {
        return new Rectangle(
            GetSystemMetrics(SM_XVIRTUALSCREEN),
            GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN),
            GetSystemMetrics(SM_CYVIRTUALSCREEN)
        );
    }

    public static Bitmap CaptureAllScreens()
    {
        var virtualScreen = GetVirtualScreenBounds();
        return CaptureRegion(virtualScreen);
    }

    public static Bitmap CapturePrimaryScreen()
    {
        var screens = GetAllScreens();
        var primaryScreen = screens.FirstOrDefault(s => s.IsPrimary) ?? screens.First();
        return CaptureRegion(primaryScreen.Bounds);
    }

    public static Bitmap CaptureScreen(int screenIndex)
    {
        var screens = GetAllScreens();
        if (screenIndex < 0 || screenIndex >= screens.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(screenIndex));
        }

        return CaptureRegion(screens[screenIndex].Bounds);
    }

    public static Bitmap CaptureRegion(Rectangle region)
    {
        IntPtr hDesk = IntPtr.Zero;
        IntPtr hSrce = IntPtr.Zero;
        IntPtr hDest = IntPtr.Zero;
        IntPtr hBmp = IntPtr.Zero;
        IntPtr hOldBmp = IntPtr.Zero;

        try
        {
            hDesk = GetDesktopWindow();
            hSrce = GetWindowDC(hDesk);
            hDest = CreateCompatibleDC(hSrce);
            hBmp = CreateCompatibleBitmap(hSrce, region.Width, region.Height);
            hOldBmp = SelectObject(hDest, hBmp);

            BitBlt(hDest, 0, 0, region.Width, region.Height,
                hSrce, region.X, region.Y, SRCCOPY);

            var bitmap = Image.FromHbitmap(hBmp);

            return bitmap;
        }
        finally
        {
            if (hOldBmp != IntPtr.Zero)
            {
                SelectObject(hDest, hOldBmp);
            }

            if (hBmp != IntPtr.Zero)
            {
                DeleteObject(hBmp);
            }

            if (hDest != IntPtr.Zero)
            {
                DeleteDC(hDest);
            }

            if (hSrce != IntPtr.Zero)
            {
                ReleaseDC(hDesk, hSrce);
            }
        }
    }

    public static List<Bitmap> CaptureEachScreen()
    {
        var screens = GetAllScreens();
        return screens.Select(screen => CaptureRegion(screen.Bounds)).ToList();
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate bool MonitorEnumProc(
        IntPtr hMonitor,
        IntPtr hdcMonitor,
        ref RECT lprcMonitor,
        IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width => this.Right - this.Left;
        public readonly int Height => this.Bottom - this.Top;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public class ScreenInfo
    {
        public Rectangle Bounds { get; set; }
        public bool IsPrimary { get; set; }
        public IntPtr Handle { get; set; }
    }
}