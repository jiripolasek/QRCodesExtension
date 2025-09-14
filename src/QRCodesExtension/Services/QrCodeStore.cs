// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Pages;

namespace JPSoftworks.QrCodesExtension.Services;

internal sealed class QrCodeStore
{
    public int Version { get; set; } = 1;

    public List<QrCode> Codes { get; set; } = [];
}