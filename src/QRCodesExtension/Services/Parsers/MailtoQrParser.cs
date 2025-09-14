// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

public class MailtoQrParser : IQrFormatParser
{
    public QrCodeType? Parse(string input)
    {
        if (!input.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Email"] = input[7..] };

        return new QrCodeType("Email", QrCodeTypeIds.Email, QrCodeCategory.Communication) { Metadata = metadata };
    }
}