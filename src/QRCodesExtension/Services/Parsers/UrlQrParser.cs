// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

public class UrlQrParser : IQrFormatParser
{
    public QrCodeType? Parse(string input)
    {
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Url"] = uri.ToString() };

        if (!string.IsNullOrEmpty(uri.Host))
        {
            metadata["Host"] = uri.Host;
        }

        if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
        {
            metadata["Path"] = uri.AbsolutePath;
        }

        return new QrCodeType("Web address", QrCodeTypeIds.Url, QrCodeCategory.Network) { Metadata = metadata };
    }
}