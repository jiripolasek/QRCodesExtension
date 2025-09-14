// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

public class SmsQrParser : IQrFormatParser
{
    public QrCodeType? Parse(string input)
    {
        if (!input.StartsWith("SMSTO:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var body = input[6..];
        var parts = body.Split(':', 2);
        if (parts.Length > 0)
        {
            metadata["Number"] = parts[0];
        }

        if (parts.Length > 1)
        {
            metadata["Message"] = parts[1];
        }

        return new QrCodeType("SMS", QrCodeTypeIds.Sms, QrCodeCategory.Communication) { Metadata = metadata };
    }
}