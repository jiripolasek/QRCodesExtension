// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Text.RegularExpressions;

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

public class WifiQrParser : IQrFormatParser
{
    public QrCodeType? Parse(string input)
    {
        if (!input.StartsWith("WIFI:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var match = Regex.Matches(input, @"([A-Z]):([^;]*)", RegexOptions.IgnoreCase);
        foreach (Match m in match)
        {
            switch (m.Groups[1].Value.ToUpperInvariant())
            {
                case "T": metadata["AuthType"] = m.Groups[2].Value; break;
                case "S": metadata["SSID"] = m.Groups[2].Value; break;
                case "P": metadata["Password"] = m.Groups[2].Value; break;
            }
        }

        return new QrCodeType("Wi-Fi", QrCodeTypeIds.Wifi, QrCodeCategory.Network) { Metadata = metadata };
    }
}