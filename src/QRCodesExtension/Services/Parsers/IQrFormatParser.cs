// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

public interface IQrFormatParser
{
    /// <summary>
    ///     Attempts to parse the input string. Returns null if the implementation does not handle the format.
    /// </summary>
    QrCodeType? Parse(string input);
}