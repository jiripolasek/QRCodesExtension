// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services;

/// <summary>
/// Well known QR code type identifiers. These are stable, lowercase, machine-friendly identifiers
/// that can be used for icon mapping, analytics, filtering, etc. Display names may change/localize,
/// these identifiers should not.
/// </summary>
public static class QrCodeTypeIds
{
    public const string Wifi = "wifi";
    public const string Sms = "sms";
    public const string Phone = "phone";
    public const string Email = "email"; // Used for both mailto and MATMSG formats
    public const string Geo = "geo";
    public const string VCard = "vcard";
    public const string MeCard = "mecard";
    public const string MatMsg = "matmsg"; // Alternative email format with subject/body
    public const string Url = "url";
    public const string Text = "text";
    public const string Invalid = "invalid";
}
