// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.CommandPalette.Extensions.Toolkit.Logging;
using JPSoftworks.QrCodesExtension.Helpers;
using JPSoftworks.QrCodesExtension.Pages;
using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using CodesIndexPage = JPSoftworks.QrCodesExtension.Pages.CodesIndexPage;

namespace JPSoftworks.QrCodesExtension.Commands;

internal class GetFromScreenCommand(QrCodeManager qrCodeManager, CodesIndexPage codesIndexPage) : InvokableCommand
{
    public override ICommandResult Invoke()
    {
        try
        {
            var scanner = new MultiScreenQrCodeScanner();
            var qr = scanner.ScanAllScreens();
            if (qr?.Text != null)
            {
                string? qrSnippetFile = null;
                if (qr.QrCodeBitmap != null)
                {
                    qrSnippetFile = DecoderHelper.SaveToTemp(qr.QrCodeBitmap);
                }

                qrCodeManager.Add(qr.Text, QrErrorCorrection.Low, 20, qrSnippetFile);
                codesIndexPage.SearchText = string.Empty;
                return CommandResult.ShowToast(new ToastArgs
                {
                    Message = "QR code detected and added.", Result = CommandResult.KeepOpen()
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }


        return CommandResult.ShowToast(new ToastArgs
        {
            Message = "QR code not found", Result = CommandResult.KeepOpen()
        });
    }
}