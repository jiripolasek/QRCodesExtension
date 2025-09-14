// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Drawing;
using ZXing;

namespace JPSoftworks.QrCodesExtension.Services;

public class QrCodeScannerResult
{
    public string? Text { get; set; }
    public Bitmap? QrCodeBitmap { get; set; }
    public Rectangle BoundingBox { get; set; }
    public ResultPoint[]? ResultPoints { get; set; }
    public int ScreenIndex { get; set; }
    public Rectangle ScreenBounds { get; set; }
}