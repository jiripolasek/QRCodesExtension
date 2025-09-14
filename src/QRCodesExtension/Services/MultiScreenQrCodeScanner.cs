// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Drawing;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using NativeScreenCapture = JPSoftworks.QrCodesExtension.Helpers.NativeScreenCapture;

namespace JPSoftworks.QrCodesExtension.Services;

public class MultiScreenQrCodeScanner : IQrCodeScanner
{
    private const float QuietAndFinderFactor = 0.20f;
    private readonly BarcodeReader _reader;

    public MultiScreenQrCodeScanner()
    {
        this._reader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };
    }

    public QrCodeScannerResult? ScanAllScreens()
    {
        using var screenshot = NativeScreenCapture.CaptureAllScreens();
        var virtualBounds = NativeScreenCapture.GetVirtualScreenBounds();

        var result = this.ScanFromBitmap(screenshot);
        if (result != null)
        {
            result.ScreenBounds = virtualBounds;
            result.ScreenIndex = -1; // Indicates all screens
        }

        return result;
    }

    public List<QrCodeScannerResult> ScanAllScreensSeparately()
    {
        var results = new List<QrCodeScannerResult>();
        var screens = NativeScreenCapture.GetAllScreens();

        for (int i = 0; i < screens.Count; i++)
        {
            var screen = screens[i];
            using var screenshot = NativeScreenCapture.CaptureScreen(i);

            var screenResults = this.ScanMultipleFromBitmap(screenshot);
            foreach (var result in screenResults)
            {
                result.ScreenIndex = i;
                result.ScreenBounds = screen.Bounds;
                // Adjust bounding box to absolute coordinates
                result.BoundingBox = new Rectangle(
                    result.BoundingBox.X + screen.Bounds.X,
                    result.BoundingBox.Y + screen.Bounds.Y,
                    result.BoundingBox.Width,
                    result.BoundingBox.Height
                );
                results.Add(result);
            }
        }

        return results;
    }

    public QrCodeScannerResult? ScanPrimaryScreen()
    {
        using var screenshot = NativeScreenCapture.CapturePrimaryScreen();
        var screens = NativeScreenCapture.GetAllScreens();
        var primaryScreen = screens.FirstOrDefault(s => s.IsPrimary) ?? screens.First();

        var result = this.ScanFromBitmap(screenshot);
        if (result != null)
        {
            result.ScreenBounds = primaryScreen.Bounds;
            result.ScreenIndex = screens.IndexOf(primaryScreen);
        }

        return result;
    }

    public QrCodeScannerResult? ScanFromBitmap(Bitmap bitmap)
    {
        var result = this._reader.Decode(bitmap);

        if (result == null)
        {
            return null;
        }

        var qrResult = new QrCodeScannerResult { Text = result.Text, ResultPoints = result.ResultPoints };

        if (result.ResultPoints is { Length: > 0 })
        {
            var boundingBox = this.CalculateQrBoundingBox(result.ResultPoints, bitmap.Size);
            qrResult.BoundingBox = boundingBox;
            qrResult.QrCodeBitmap = this.CropQRCode(bitmap, boundingBox, 32);
        }

        return qrResult;
    }

    public List<QrCodeScannerResult> ScanMultipleFromBitmap(Bitmap bitmap)
    {
        var reader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };

        var results = reader.DecodeMultiple(bitmap);

        if (results == null)
        {
            return [];
        }

        return results.Select(r =>
        {
            var calculateQrBoundingBox = this.CalculateQrBoundingBox(r.ResultPoints, bitmap.Size);

            return new QrCodeScannerResult
            {
                Text = r.Text,
                ResultPoints = r.ResultPoints,
                BoundingBox = calculateQrBoundingBox,
                QrCodeBitmap = this.CropQRCode(bitmap, calculateQrBoundingBox, 32)
            };
        }).ToList();
    }

    private Bitmap CropQRCode(Bitmap source, Rectangle boundingBox, int padding = 0)
    {
        var cropRect = new Rectangle(
            Math.Max(0, boundingBox.X - padding),
            Math.Max(0, boundingBox.Y - padding),
            Math.Min(source.Width - Math.Max(0, boundingBox.X - padding),
                boundingBox.Width + (padding * 2)),
            Math.Min(source.Height - Math.Max(0, boundingBox.Y - padding),
                boundingBox.Height + (padding * 2))
        );

        var croppedBitmap = new Bitmap(cropRect.Width, cropRect.Height);

        using var graphics = Graphics.FromImage(croppedBitmap);
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, cropRect.Width, cropRect.Height),
            cropRect,
            GraphicsUnit.Pixel
        );

        return croppedBitmap;
    }

    private Rectangle CalculateQrBoundingBox(ResultPoint[]? points, Size imageSize)
    {
        if (points == null || points.Length < 3)
        {
            return Rectangle.Empty;
        }

        // Pick three finder centers that form the largest triangle (robust if alignment point is present).
        var (a, b, c) = PickBestFinderTriplet(points);

        // Order A,B,C so that: A=top-left, B=top-right, C=bottom-left (ZXing-style).
        OrderBestPatterns(ref a, ref b, ref c);

        // Vectors along the QR edges (from top-left to right/bottom).
        var ab = new PointF(b.X - a.X, b.Y - a.Y);
        var ac = new PointF(c.X - a.X, c.Y - a.Y);

        float lenAb = (float)Math.Sqrt(ab.X * ab.X + ab.Y * ab.Y);
        float lenAc = (float)Math.Sqrt(ac.X * ac.X + ac.Y * ac.Y);
        if (lenAb < 1 || lenAc < 1)
        {
            return Rectangle.Empty;
        }

        // Unit vectors along AB and AC
        var uAb = new PointF(ab.X / lenAb, ab.Y / lenAb);
        var uAc = new PointF(ac.X / lenAc, ac.Y / lenAc);

        // Grow outward from the finder centers to cover finder size + quiet zone.
        float growAb = lenAb * QuietAndFinderFactor;
        float growAc = lenAc * QuietAndFinderFactor;

        // Compute the fourth corner (parallelogram) and then expand all four corners.
        var ptD = new PointF(b.X + (c.X - a.X), b.Y + (c.Y - a.Y)); // bottom-right before growth

        // Expanded corners (push out along both axes of the QR)
        var exA = new PointF(a.X - uAb.X * growAb - uAc.X * growAc, a.Y - uAb.Y * growAb - uAc.Y * growAc);
        var exB = new PointF(b.X + uAb.X * growAb - uAc.X * growAc, b.Y + uAb.Y * growAb - uAc.Y * growAc);
        var exC = new PointF(c.X - uAb.X * growAb + uAc.X * growAc, c.Y - uAb.Y * growAb + uAc.Y * growAc);
        var exD = new PointF(ptD.X + uAb.X * growAb + uAc.X * growAc, ptD.Y + uAb.Y * growAb + uAc.Y * growAc);

        // Axis-aligned rectangle covering the expanded oriented square
        float minX = MathF.Min(MathF.Min(exA.X, exB.X), MathF.Min(exC.X, exD.X));
        float minY = MathF.Min(MathF.Min(exA.Y, exB.Y), MathF.Min(exC.Y, exD.Y));
        float maxX = MathF.Max(MathF.Max(exA.X, exB.X), MathF.Max(exC.X, exD.X));
        float maxY = MathF.Max(MathF.Max(exA.Y, exB.Y), MathF.Max(exC.Y, exD.Y));

        // Convert to int rectangle and clamp to bitmap
        int x = (int)Math.Floor(minX);
        int y = (int)Math.Floor(minY);
        int w = (int)Math.Ceiling(maxX - minX);
        int h = (int)Math.Ceiling(maxY - minY);

        var rect = new Rectangle(x, y, w, h);
        return ClampRectangle(rect, imageSize);
    }

    private static (ResultPoint a, ResultPoint b, ResultPoint c) PickBestFinderTriplet(ResultPoint[] points)
    {
        // If exactly 3, done.
        if (points.Length == 3)
        {
            return (points[0], points[1], points[2]);
        }

        // Otherwise choose the 3 points forming the largest perimeter (or area).
        float bestScore = -1f;
        (ResultPoint, ResultPoint, ResultPoint) best = (points[0], points[1], points[2]);
        for (int i = 0; i < points.Length - 2; i++)
        {
            for (int j = i + 1; j < points.Length - 1; j++)
            {
                for (int k = j + 1; k < points.Length; k++)
                {
                    float p = Dist(points[i], points[j]) + Dist(points[j], points[k]) + Dist(points[k], points[i]);
                    if (p > bestScore)
                    {
                        bestScore = p;
                        best = (points[i], points[j], points[k]);
                    }
                }
            }
        }

        return best;

        static float Dist(ResultPoint r1, ResultPoint r2)
        {
            float dx = r1.X - r2.X;
            float dy = r1.Y - r2.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }

    // Port of ZXing.ResultPointHelper.OrderBestPatterns logic (simplified):
    // Ensures A=top-left, B=top-right, C=bottom-left
    private static void OrderBestPatterns(ref ResultPoint a, ref ResultPoint b, ref ResultPoint c)
    {
        // Identify the two points with the largest distance; those are B and C (base), A is the remaining.
        float dAb = SquaredDist(a, b);
        float dBc = SquaredDist(b, c);
        float dAc = SquaredDist(a, c);

        ResultPoint topLeft, topRight, bottomLeft;

        ResultPoint p0, p1, p2;
        if (dBc >= dAb && dBc >= dAc)
        {
            p0 = a;
            p1 = b;
            p2 = c; // A is leftover
        }
        else if (dAc >= dAb && dAc >= dBc)
        {
            p0 = b;
            p1 = a;
            p2 = c; // B is leftover
        }
        else
        {
            p0 = c;
            p1 = a;
            p2 = b; // C is leftover
        }

        // p1 and p2 are the base endpoints, p0 is the "leftover" (intended top-left).
        topLeft = p0;
        // Determine orientation via cross product to assign topRight/bottomLeft.
        var cross = Cross(p1, topLeft, p2);
        if (cross < 0)
        {
            topRight = p1;
            bottomLeft = p2;
        }
        else
        {
            topRight = p2;
            bottomLeft = p1;
        }

        a = topLeft;
        b = topRight;
        c = bottomLeft;
        return;

        static float SquaredDist(ResultPoint r1, ResultPoint r2)
        {
            float dx = r1.X - r2.X;
            float dy = r1.Y - r2.Y;
            return dx * dx + dy * dy;
        }

        static float Cross(ResultPoint a, ResultPoint b, ResultPoint c)
        {
            return (c.X - b.X) * (a.Y - b.Y) - (c.Y - b.Y) * (a.X - b.X);
        }
    }

    private static Rectangle ClampRectangle(Rectangle r, Size size)
    {
        int x = Math.Max(0, Math.Min(r.X, size.Width));
        int y = Math.Max(0, Math.Min(r.Y, size.Height));
        int w = Math.Max(0, Math.Min(r.Width, size.Width - x));
        int h = Math.Max(0, Math.Min(r.Height, size.Height - y));
        return new Rectangle(x, y, w, h);
    }
}