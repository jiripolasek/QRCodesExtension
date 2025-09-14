// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Pages;

internal record QrCode(
    Guid Id,
    string Value,
    QrErrorCorrection ErrorCorrection,
    int ModuleSize,
    DateTime CreatedUtc,
    bool IsExternal);