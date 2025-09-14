// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Pages;

namespace JPSoftworks.QrCodesExtension.Services;

internal class QrCodeEventArgs : EventArgs
{
    public QrCodeEventArgs(QrCode qrCode)
    {
        this.QrCode = qrCode;
    }

    public QrCode QrCode { get; }
}