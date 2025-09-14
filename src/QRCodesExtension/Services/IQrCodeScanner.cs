// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Drawing;

namespace JPSoftworks.QrCodesExtension.Services;

public interface IQrCodeScanner
{
    QrCodeScannerResult? ScanAllScreens();

    List<QrCodeScannerResult> ScanAllScreensSeparately();

    QrCodeScannerResult? ScanPrimaryScreen();

    QrCodeScannerResult? ScanFromBitmap(Bitmap bitmap);

    List<QrCodeScannerResult> ScanMultipleFromBitmap(Bitmap bitmap);
}