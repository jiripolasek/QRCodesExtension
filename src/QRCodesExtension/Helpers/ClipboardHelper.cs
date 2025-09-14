// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace JPSoftworks.QrCodesExtension.Helpers;

internal static partial class ClipboardHelper
{
    private const uint GHND = GMEM_MOVEABLE | GMEM_ZEROINIT;

    // Added: BITMAPINFOHEADER struct & BI_RGB constant
    private const int BI_RGB = 0;
    private static readonly bool? _clipboardSupported = true;

    // Used if an external clipboard is not available, e.g. if xclip is missing.
    // This is useful for testing in CI as well.
    private static string? _internalClipboard;
    private static Bitmap? _internalBitmapClipboard; // Added

    public static string GetText()
    {
        if (_clipboardSupported == false)
        {
            return _internalClipboard ?? string.Empty;
        }

        var tool = string.Empty;
        var args = string.Empty;
        var clipboardText = string.Empty;

        ExecuteOnStaThread(() => GetTextImpl(out clipboardText));
        return clipboardText;
    }

    public static void SetText(string text)
    {
        if (_clipboardSupported == false)
        {
            _internalClipboard = text;
            return;
        }

        var tool = string.Empty;
        var args = string.Empty;
        ExecuteOnStaThread(() => SetClipboardData(Tuple.Create(text, CF_UNICODETEXT)));
    }

    // Added: Set bitmap content to clipboard
    public static void SetBitmap(Bitmap bitmap)
    {
        if (_clipboardSupported == false)
        {
            _internalBitmapClipboard?.Dispose();
            _internalBitmapClipboard = (Bitmap)bitmap.Clone();
            return;
        }

        ExecuteOnStaThread(() => SetClipboardBitmap(bitmap));
    }

    // Added: Get bitmap content from clipboard (null if unavailable)
    public static Bitmap? GetBitmap()
    {
        if (_clipboardSupported == false)
        {
            return _internalBitmapClipboard is null ? null : (Bitmap)_internalBitmapClipboard.Clone();
        }

        Bitmap? bmp = null;
        ExecuteOnStaThread(() => GetBitmapImpl(out bmp));
        return bmp;
    }

    public static void SetRtf(string plainText, string rtfText)
    {
        if (s_CF_RTF == 0)
        {
            s_CF_RTF = RegisterClipboardFormat("Rich Text Format");
        }

        ExecuteOnStaThread(() => SetClipboardData(
            Tuple.Create(plainText, CF_UNICODETEXT),
            Tuple.Create(rtfText, s_CF_RTF)));
    }

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GlobalAlloc(uint flags, UIntPtr dwBytes);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GlobalFree(IntPtr hMem);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GlobalLock(IntPtr hMem);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(IntPtr hMem);

    [LibraryImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
    private static partial void CopyMemory(IntPtr dest, IntPtr src, uint count);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsClipboardFormatAvailable(uint format);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(IntPtr hWndNewOwner);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetClipboardData(uint format);

    [LibraryImport("user32.dll")]
    private static partial IntPtr SetClipboardData(uint format, IntPtr data);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint RegisterClipboardFormat(string lpszFormat);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(IntPtr hObject); // Added for bitmap cleanup on failure

    private static bool GetTextImpl(out string text)
    {
        try
        {
            if (IsClipboardFormatAvailable(CF_UNICODETEXT))
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    var data = GetClipboardData(CF_UNICODETEXT);
                    if (data != IntPtr.Zero)
                    {
                        data = GlobalLock(data);
                        text = Marshal.PtrToStringUni(data) ?? string.Empty;
                        GlobalUnlock(data);
                        return true;
                    }
                }
            }
            else if (IsClipboardFormatAvailable(CF_TEXT))
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    var data = GetClipboardData(CF_TEXT);
                    if (data != IntPtr.Zero)
                    {
                        data = GlobalLock(data);
                        text = Marshal.PtrToStringAnsi(data) ?? string.Empty;
                        GlobalUnlock(data);
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Ignore exceptions
        }
        finally
        {
            CloseClipboard();
        }

        text = string.Empty;
        return false;
    }

    // Added: Bitmap retrieval implementation
    private static bool GetBitmapImpl(out Bitmap? bitmap)
    {
        bitmap = null;
        try
        {
            if (IsClipboardFormatAvailable(CF_BITMAP) && OpenClipboard(IntPtr.Zero))
            {
                var hBitmap = GetClipboardData(CF_BITMAP);
                if (hBitmap != IntPtr.Zero)
                {
                    // Image.FromHbitmap duplicates the bitmap so we do not own the original handle
                    bitmap = Image.FromHbitmap(hBitmap);
                    return true;
                }
            }
        }
        catch
        {
            // Ignore
        }
        finally
        {
            CloseClipboard();
        }

        return false;
    }

    private static bool SetClipboardData(params Tuple<string, uint>[] data)
    {
        try
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return false;
            }

            EmptyClipboard();

            foreach (var d in data)
            {
                if (!SetSingleClipboardData(d.Item1, d.Item2))
                {
                    return false;
                }
            }
        }
        finally
        {
            CloseClipboard();
        }

        return true;
    }

    private static bool SetSingleClipboardData(string text, uint format)
    {
        var hGlobal = IntPtr.Zero;
        var data = IntPtr.Zero;

        try
        {
            uint bytes;
            if (format == s_CF_RTF || format == CF_TEXT)
            {
                bytes = (uint)(text.Length + 1);
                data = Marshal.StringToHGlobalAnsi(text);
            }
            else if (format == CF_UNICODETEXT)
            {
                bytes = (uint)((text.Length + 1) * 2);
                data = Marshal.StringToHGlobalUni(text);
            }
            else
            {
                // Not yet supported format.
                return false;
            }

            if (data == IntPtr.Zero)
            {
                return false;
            }

            hGlobal = GlobalAlloc(GHND, bytes);
            if (hGlobal == IntPtr.Zero)
            {
                return false;
            }

            var dataCopy = GlobalLock(hGlobal);
            if (dataCopy == IntPtr.Zero)
            {
                return false;
            }

            CopyMemory(dataCopy, data, bytes);
            GlobalUnlock(hGlobal);

            if (SetClipboardData(format, hGlobal) != IntPtr.Zero)
            {
                // The clipboard owns this memory now, so don't free it.
                hGlobal = IntPtr.Zero;
            }
        }
        catch
        {
            // Ignore failures
        }
        finally
        {
            if (data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(data);
            }

            if (hGlobal != IntPtr.Zero)
            {
                GlobalFree(hGlobal);
            }
        }

        return true;
    }

    // Added: Bitmap setter implementation (now also sets CF_DIB)
    private static bool SetClipboardBitmap(Bitmap bitmap)
    {
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr hGlobalDib = IntPtr.Zero;
        try
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return false;
            }

            EmptyClipboard();

            // 1. Create HBITMAP (CF_BITMAP)
            hBitmap = bitmap.GetHbitmap();
            if (hBitmap == IntPtr.Zero)
            {
                return false;
            }

            // 2. Create DIB (CF_DIB) from bitmap bits
            if (!TryCreateDibHandle(bitmap, out hGlobalDib))
            {
                // Still attempt to set CF_BITMAP even if CF_DIB failed
                hGlobalDib = IntPtr.Zero;
            }

            var anySuccess = false;

            if (SetClipboardData(CF_BITMAP, hBitmap) != IntPtr.Zero)
            {
                // Ownership transferred
                hBitmap = IntPtr.Zero;
                anySuccess = true;
            }

            if (hGlobalDib != IntPtr.Zero)
            {
                if (SetClipboardData(CF_DIB, hGlobalDib) != IntPtr.Zero)
                {
                    // Ownership transferred
                    hGlobalDib = IntPtr.Zero;
                    anySuccess = true;
                }
            }

            return anySuccess;
        }
        catch
        {
            return false;
        }
        finally
        {
            CloseClipboard();
            if (hBitmap != IntPtr.Zero)
            {
                DeleteObject(hBitmap);
            }

            if (hGlobalDib != IntPtr.Zero)
            {
                GlobalFree(hGlobalDib);
            }
        }
    }

    // Added: Create a DIB (CF_DIB) HGLOBAL from a Bitmap (32bpp ARGB -> BI_RGB top-down)
    private static bool TryCreateDibHandle(Bitmap bitmap, out IntPtr hGlobal)
    {
        hGlobal = IntPtr.Zero;
        Bitmap? temp = null;
        try
        {
            // Ensure 32bpp format for predictable stride
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                temp = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(temp);
                g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
            }

            var src = temp ?? bitmap;

            var rect = new Rectangle(0, 0, src.Width, src.Height);
            var bd = src.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var width = src.Width;
                var height = src.Height;
                var stride = bd.Stride; // Should be width * 4 aligned
                var imageSize = stride * height;

                // BITMAPINFOHEADER (40 bytes)
                var headerSize = Marshal.SizeOf<BITMAPINFOHEADER>();
                var totalSize = headerSize + imageSize;

                hGlobal = GlobalAlloc(GHND, (UIntPtr)totalSize);
                if (hGlobal == IntPtr.Zero)
                {
                    return false;
                }

                var ptr = GlobalLock(hGlobal);
                if (ptr == IntPtr.Zero)
                {
                    GlobalFree(hGlobal);
                    hGlobal = IntPtr.Zero;
                    return false;
                }

                try
                {
                    // Fill header (top-down DIB -> negative height)
                    var bih = new BITMAPINFOHEADER
                    {
                        biSize = headerSize,
                        biWidth = width,
                        biHeight = -height, // top-down so no vertical flip needed
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = BI_RGB,
                        biSizeImage = imageSize,
                        biXPelsPerMeter = 3780, // ~96 DPI
                        biYPelsPerMeter = 3780,
                        biClrUsed = 0,
                        biClrImportant = 0
                    };

                    Marshal.StructureToPtr(bih, ptr, false);

                    // Copy pixels
                    var destBits = ptr + headerSize;
                    // Copy raw pixel data
                    var bytes = new byte[imageSize];
                    Marshal.Copy(bd.Scan0, bytes, 0, imageSize);
                    Marshal.Copy(bytes, 0, destBits, imageSize);
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }
            }
            finally
            {
                src.UnlockBits(bd);
            }

            return true;
        }
        catch
        {
            if (hGlobal != IntPtr.Zero)
            {
                GlobalFree(hGlobal);
                hGlobal = IntPtr.Zero;
            }

            return false;
        }
        finally
        {
            temp?.Dispose();
        }
    }

    private static void ExecuteOnStaThread(Func<bool> action)
    {
        const int RetryCount = 5;
        var tries = 0;

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            while (tries++ < RetryCount && !action())
            {
                // wait until RetryCount or action
            }

            return;
        }

        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                while (tries++ < RetryCount && !action())
                {
                    // wait until RetryCount or action
                }
            }
            catch (Exception e)
            {
                exception = e;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw exception;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

#pragma warning disable SA1310 // Field names should not contain underscore
    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint GMEM_ZEROINIT = 0x0040;
#pragma warning restore SA1310 // Field names should not contain underscore

#pragma warning disable SA1310 // Field names should not contain underscore
    private const uint CF_TEXT = 1;
    private const uint CF_BITMAP = 2; // Added
    private const uint CF_DIB = 8; // Added DIB for broader compatibility
    private const uint CF_UNICODETEXT = 13;

#pragma warning disable SA1308 // Variable names should not be prefixed
    private static uint s_CF_RTF;
#pragma warning restore SA1308 // Variable names should not be prefixed
#pragma warning restore SA1310 // Field names should not contain underscore
}