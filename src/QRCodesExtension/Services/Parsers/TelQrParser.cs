// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

public class TelQrParser : IQrFormatParser
{
    public QrCodeType? Parse(string input)
    {
        if (!input.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Phone"] = input[4..] };

        return new QrCodeType("Phone number", QrCodeTypeIds.Phone, QrCodeCategory.Communication) { Metadata = metadata };
    }
}