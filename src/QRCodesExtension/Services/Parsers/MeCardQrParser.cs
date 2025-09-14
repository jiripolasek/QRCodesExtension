// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

public class MeCardQrParser : IQrFormatParser
{
    public QrCodeType? Parse(string input)
    {
        if (!input.StartsWith("MECARD:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var body = input[7..];
        var fields = body.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var field in fields)
        {
            var kv = field.Split(':', 2);
            if (kv.Length == 2)
            {
                metadata[kv[0]] = kv[1];
            }
        }

        return new QrCodeType("Contact (MeCard)", QrCodeTypeIds.MeCard, QrCodeCategory.Contact) { Metadata = metadata };
    }
}