// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Pages;

namespace JPSoftworks.QrCodesExtension.Services;

internal interface IQrCodeGenerator
{
    byte[] GenerateQrCode(QrCode qrCode);
}