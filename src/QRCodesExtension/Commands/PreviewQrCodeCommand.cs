// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Resources;
using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension.Commands;

internal sealed partial class PreviewQrCodeCommand : InvokableCommand
{
    private readonly Guid _qrCodeId;

    public PreviewQrCodeCommand(Guid qrCodeId)
    {
        ArgumentNullException.ThrowIfNull(qrCodeId);
        this._qrCodeId = qrCodeId;
        this.Name = Strings.Commands_PreviewQrCode_Name;
        this.Icon = Icons.QrPreview;
    }

    public override ICommandResult Invoke()
    {
        var image = QrCodeManager.Instance.GetPngPath(this._qrCodeId);
        if (File.Exists(image))
        {
            ShellHelpers.OpenInShell(image);
            return CommandResult.Dismiss();
        }

        return CommandResult.ShowToast(new ToastArgs
        {
            Message = Strings.Commands_PreviewQrCode_NotFound, Result = CommandResult.KeepOpen()
        });
    }
}