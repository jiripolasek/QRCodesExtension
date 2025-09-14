// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Pages;
using QRCoder;

namespace JPSoftworks.QrCodesExtension.Services;

internal class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GenerateQrCode(QrCode qrCode)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCode.Value, (QRCodeGenerator.ECCLevel)qrCode.ErrorCorrection);
        using var png = new PngByteQRCode(qrCodeData);
        return png.GetGraphic(qrCode.ModuleSize);
    }
}