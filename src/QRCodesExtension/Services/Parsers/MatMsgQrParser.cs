// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

public class MatMsgQrParser : IQrFormatParser
{
    public QrCodeType? Parse(string input)
    {
        if (!input.StartsWith("MATMSG:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var body = input[7..];
        var fields = body.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var field in fields)
        {
            if (field.StartsWith("TO:", StringComparison.OrdinalIgnoreCase))
            {
                metadata["To"] = field[3..];
            }
            else if (field.StartsWith("SUB:", StringComparison.OrdinalIgnoreCase))
            {
                metadata["Subject"] = field[4..];
            }
            else if (field.StartsWith("BODY:", StringComparison.OrdinalIgnoreCase))
            {
                metadata["Body"] = field[5..];
            }
        }

        return new QrCodeType("Email", QrCodeTypeIds.MatMsg, QrCodeCategory.Communication) { Metadata = metadata };
    }
}