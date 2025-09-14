// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Services.Parsers;

namespace JPSoftworks.QrCodesExtension.Services;

public class QrCodeMetadataParser
{
    private readonly List<IQrFormatParser> _parsers =
    [
        new WifiQrParser(),
        new SmsQrParser(),
        new TelQrParser(),
        new MailtoQrParser(),
        new GeoQrParser(),
        new VCardQrParser(),
        new MeCardQrParser(),
        new MatMsgQrParser(),
        new UrlQrParser(),
        new TextQrParser()
    ];

    public QrCodeType Parse(string input)
    {
        // Iterate through parsers and return the first successful parse result
        foreach (var parser in this._parsers)
        {
            try
            {
                var result = parser.Parse(input);
                if (result is not null)
                {
                    result.RawData = input;
                    // Attempt simple category inference if category is Unknown
                    if (result.Category == QrCodeCategory.Unknown)
                    {
                        result.Category = InferCategory(result);
                    }

                    return result;
                }
            }
            catch
            {
                // Ignore individual parser errors and continue
            }
        }

        // Should not reach here because TextQrParser always returns something, but fallback just in case
        return new QrCodeType("Invalid") { RawData = input };
    }

    private static QrCodeCategory InferCategory(QrCodeType type)
    {
        if (type.HasMetadata("Phone", "Email", "Number"))
        {
            return QrCodeCategory.Communication;
        }

        if (type.HasMetadata("Url", "SSID"))
        {
            return QrCodeCategory.Network;
        }

        if (type.HasMetadata("FullName", "Name", "FamilyName", "GivenName"))
        {
            return QrCodeCategory.Contact;
        }

        if (type.HasMetadata("Latitude", "Longitude"))
        {
            return QrCodeCategory.Location;
        }

        if (type.HasMetadata("Text"))
        {
            return QrCodeCategory.Text;
        }

        return QrCodeCategory.Unknown;
    }
}