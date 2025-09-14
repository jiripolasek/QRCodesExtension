// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.QrCodesExtension.Services;

public enum QrCodeCategory
{
    Unknown,
    Communication, // Phone, SMS, Email
    Network, // Wi-Fi, URL
    Contact, // vCard, MeCard
    Location, // Geo coordinates
    Text // Plain text
}