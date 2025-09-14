// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Drawing;
using JPSoftworks.QrCodesExtension.Resources;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ClipboardHelper = JPSoftworks.QrCodesExtension.Helpers.ClipboardHelper;

namespace JPSoftworks.QrCodesExtension.Commands;

internal sealed partial class CopyBitmapToClipboardCommand : InvokableCommand
{
    private readonly string _filePath;

    public CopyBitmapToClipboardCommand(string filePath)
    {
        this._filePath = filePath;
        this.Icon = Icons.CopyBitmap;
        this.Name = Strings.Commands_CopyBitmapToClipboard_Name;
    }

    public override ICommandResult Invoke()
    {
        if (File.Exists(this._filePath))
        {
            using var bimap = new Bitmap(this._filePath);
            ClipboardHelper.SetBitmap(bimap);
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = Strings.Commands_CopyBitmapToClipboard_Success, Result = CommandResult.Dismiss()
            });
        }

        return CommandResult.ShowToast(new ToastArgs
        {
            Message = Strings.Commands_CopyBitmapToClipboard_Failure, Result = CommandResult.Dismiss()
        });
    }
}