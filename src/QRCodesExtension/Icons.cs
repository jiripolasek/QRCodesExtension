// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension;

internal static class Icons
{
    internal static IconInfo AppLogo { get; } = IconHelpers.FromRelativePath("Assets\\Logo.svg");

    internal static IconInfo Copy { get; } = new("\uE8C8"); // Copy icon

    internal static IconInfo QrCode { get; } = IconHelpers.FromRelativePath("Assets\\Icons\\qr.png");

    internal static IconInfo NewQrCode { get; } = IconHelpers.FromRelativePath("Assets\\Icons\\qr_add.png");

    internal static IconInfo QrClipboard { get; } = IconHelpers.FromRelativePath("Assets\\Icons\\qr_clipboard.png");

    internal static IconInfo QrBolt { get; } = IconHelpers.FromRelativePath("Assets\\Icons\\qr_bolt.png");

    internal static IconInfo Delete { get; } = new("\uE74D"); // Delete icon

    internal static IconInfo QrPreview { get; } = new("\uF78B"); // Magnifying glass

    internal static IconInfo Export { get; } = new("\uE74E"); // Export icon

    internal static IconInfo CopyBitmap { get; } = new("\uE8B9"); // Pictures

    internal static IconInfo QrScanClipboard { get; } = IconHelpers.FromRelativePath("Assets\\Icons\\qr_scan_clipboard.png");

    internal static IconInfo QrScanDesktop { get; } = IconHelpers.FromRelativePath("Assets\\Icons\\qr_scan_desktop.png");

    internal static IconInfo QrCodeFluentIcon { get; } = new("\uED14"); // QR code icon

    internal static class QrCodeCategories
    {
        internal static IconInfo Generic { get; } = new("\uED14"); // QR code icon

        internal static IconInfo Communication { get; } = new("\uE717"); // Contact icon
        internal static IconInfo Network { get; } = new("\uE774"); // Globe icon
        internal static IconInfo Contact { get; } = new("\uE77B"); // Contact icon
        internal static IconInfo Location { get; } = new("\uE707"); // Location icon
        internal static IconInfo Text { get; } = new("\uE8D2"); // Text icon

        internal static IconInfo Phone { get; } = new("\uE717"); // Phone
        internal static IconInfo Sms { get; } = new("\uE8BD"); // Message
        internal static IconInfo Email { get; } = new("\uE715"); // Email
        internal static IconInfo Wifi { get; } = new("\uE701"); // Wifi
        internal static IconInfo Url { get; } = new("\uE774"); // Globe

        internal static IconInfo Link { get; } = new("\uE71B"); // Link
    }
}