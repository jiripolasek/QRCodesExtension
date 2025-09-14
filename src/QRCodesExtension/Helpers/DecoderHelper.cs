// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Drawing;
using System.Drawing.Imaging;

namespace JPSoftworks.QrCodesExtension.Helpers;

internal static class DecoderHelper
{
    public static string? SaveToTemp(Bitmap src)
    {
        var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        src.Save(file, ImageFormat.Png);
        return file;
    }
}