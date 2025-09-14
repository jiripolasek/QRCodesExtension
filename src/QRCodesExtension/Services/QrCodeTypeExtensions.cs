// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services;

public static class QrCodeTypeExtensions
{
    /// <summary>
    ///     Filters QR codes by category
    /// </summary>
    public static IEnumerable<QrCodeType> OfCategory(this IEnumerable<QrCodeType> qrCodes, QrCodeCategory category)
    {
        return qrCodes.Where(qr => qr.Category == category);
    }

    /// <summary>
    ///     Gets only actionable QR codes
    /// </summary>
    public static IEnumerable<QrCodeType> Actionable(this IEnumerable<QrCodeType> qrCodes)
    {
        return qrCodes.Where(qr => qr.IsActionable);
    }

    /// <summary>
    ///     Gets only valid QR codes
    /// </summary>
    public static IEnumerable<QrCodeType> Valid(this IEnumerable<QrCodeType> qrCodes)
    {
        return qrCodes.Where(qr => qr.IsValid);
    }
}