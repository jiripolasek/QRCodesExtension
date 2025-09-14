// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Helpers;
using JPSoftworks.QrCodesExtension.Pages;
using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension;

internal sealed partial class QrCodesExtensionCommandsProvider : CommandProvider
{
    private readonly SettingsManager _settingsManager = new();
    private readonly QrCodeMetadataParser _qrCodeMetadataParser = new();

    private readonly ICommandItem[] _commands;
    //private readonly IFallbackCommandItem[] _fallbackCommands = [
    //    new CreateQrCodeFallbackCommand()
    //];

    public QrCodesExtensionCommandsProvider()
    {
        this.DisplayName = "QR Codes for Command Palette";
        this.Icon = Icons.AppLogo;
        this.Settings = this._settingsManager.Settings;
        this._commands =
        [
            new CommandItem(new CodesIndexPage(this._qrCodeMetadataParser, this._settingsManager))
            {
                Title = "QR codes", Subtitle = "Generate and manage your QR codes",
                MoreCommands = [
                    new CommandContextItem(this._settingsManager.Settings.SettingsPage)
                    ]
            }
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return this._commands;
    }

    //public override IFallbackCommandItem[] FallbackCommands()
    //{
    //    return _fallbackCommands;
    //}
}