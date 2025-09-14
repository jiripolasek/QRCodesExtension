// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

public class TextQrParser : IQrFormatParser
{
    public QrCodeType? Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new QrCodeType { DisplayName = "Invalid", Metadata = new Dictionary<string, string>() };
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Text"] = input.Trim() };
        return new QrCodeType("Text", QrCodeCategory.Text) { Metadata = metadata };
    }
}