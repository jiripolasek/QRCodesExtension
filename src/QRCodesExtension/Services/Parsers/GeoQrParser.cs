// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

public class GeoQrParser : IQrFormatParser
{
    public QrCodeType? Parse(string input)
    {
        if (!input.StartsWith("geo:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var geo = input[4..];
        var parts = geo.Split('?', 2);
        var coords = parts[0].Split(',', 2);
        if (coords.Length == 2)
        {
            metadata["Latitude"] = coords[0];
            metadata["Longitude"] = coords[1];
        }

        if (parts.Length == 2)
        {
            metadata["Query"] = parts[1];
        }

        return new QrCodeType("Geolocation", QrCodeTypeIds.Geo, QrCodeCategory.Location) { Metadata = metadata };
    }
}