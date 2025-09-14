// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Helpers;
using JPSoftworks.QrCodesExtension.Pages;
using JPSoftworks.QrCodesExtension.Resources;
using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using CodesIndexPage = JPSoftworks.QrCodesExtension.Pages.CodesIndexPage;

namespace JPSoftworks.QrCodesExtension.Commands;

internal class InstantQrCodeCommand : InvokableCommand
{
    private readonly CodesIndexPage? _codesIndexPage;
    private readonly SettingsManager _settingsManager;

    public InstantQrCodeCommand(string text, CodesIndexPage? codesIndexPage, SettingsManager settingsManager)
    {
        ArgumentNullException.ThrowIfNull(text);

        this._codesIndexPage = codesIndexPage;
        this._settingsManager = settingsManager;
        this.Name = Strings.Commands_InstantQrCode_Name;
        this.Icon = Icons.QrBolt;
        this.Input = text;
    }

    public string Input { get; set; }

    public override ICommandResult Invoke()
    {
        try
        {
            var qrCode = new QrCode(
                Guid.NewGuid(), 
                this.Input, 
                this._settingsManager.InstantQrCodeEcc,
                this._settingsManager.ElementSize,
                DateTime.UtcNow,
                false);

            QrCodeManager.Instance.Add(qrCode);

            this._codesIndexPage?.ClearSearch();

            return CommandResult.ShowToast(new ToastArgs
            {
                Message = Strings.Commands_InstantQrCode_Success, Result = CommandResult.KeepOpen()
            });
        }
        catch (Exception ex)
        {
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = string.Format(Strings.Commands_InstantQrCode_Failure_Format, ex.Message), Result = CommandResult.KeepOpen()
            });
        }
    }
}