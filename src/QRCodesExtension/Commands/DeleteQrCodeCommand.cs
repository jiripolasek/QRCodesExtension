// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension.Commands;

public class DeleteQrCodeCommand : InvokableCommand
{
    private readonly Guid _qrCodeId;

    public DeleteQrCodeCommand(Guid qrCodeId)
    {
        ArgumentNullException.ThrowIfNull(qrCodeId);

        this._qrCodeId = qrCodeId;
        this.Name = "Delete QR code";
        this.Icon = Icons.Delete;
    }

    public override ICommandResult Invoke()
    {
        QrCodeManager.Instance.Delete(this._qrCodeId);
        return CommandResult.ShowToast(new ToastArgs
        {
            Message = "QR code deleted", Result = CommandResult.KeepOpen()
        });
    }
}