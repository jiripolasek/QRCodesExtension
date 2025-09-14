// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Drawing;
using JPSoftworks.CommandPalette.Extensions.Toolkit.Logging;
using JPSoftworks.QrCodesExtension.Helpers;
using JPSoftworks.QrCodesExtension.Pages;
using JPSoftworks.QrCodesExtension.Resources;
using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension.Commands;

internal class GetFromBitmapQrCodeCommand(QrCodeManager qrCodeManager, Bitmap bitmap) : InvokableCommand
{
    public override ICommandResult Invoke()
    {
        try
        {
            var scanner = new MultiScreenQrCodeScanner();
            var qr = scanner.ScanFromBitmap(bitmap);
            if (qr?.Text != null)
            {
                string? qrSnippetFile = null;
                if (qr.QrCodeBitmap != null)
                {
                    qrSnippetFile = DecoderHelper.SaveToTemp(qr.QrCodeBitmap);
                }

                qrCodeManager.Add(qr.Text, QrErrorCorrection.Low, 20, qrSnippetFile);

                return CommandResult.ShowToast(new ToastArgs
                {
                    Message = Strings.Commands_GetFromBitmapQrCode_Success, Result = CommandResult.KeepOpen()
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }


        return CommandResult.ShowToast(new ToastArgs
        {
            Message = Strings.Commands_GetFromBitmapQrCode_NotFound, Result = CommandResult.KeepOpen()
        });
    }
}