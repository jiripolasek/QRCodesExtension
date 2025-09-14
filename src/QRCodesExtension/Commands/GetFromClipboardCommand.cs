// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.CommandPalette.Extensions.Toolkit.Logging;
using JPSoftworks.QrCodesExtension.Helpers;
using JPSoftworks.QrCodesExtension.Pages;
using JPSoftworks.QrCodesExtension.Resources;
using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ClipboardHelper = JPSoftworks.QrCodesExtension.Helpers.ClipboardHelper;
using CodesIndexPage = JPSoftworks.QrCodesExtension.Pages.CodesIndexPage;

namespace JPSoftworks.QrCodesExtension.Commands;

internal class GetFromClipboardCommand(QrCodeManager qrCodeManager, CodesIndexPage codesIndexPage) : InvokableCommand
{
    public override ICommandResult Invoke()
    {
        try
        {
            var bitmap = ClipboardHelper.GetBitmap();
            if (bitmap != null)
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
                    codesIndexPage.SearchText = string.Empty;
                    return CommandResult.ShowToast(new ToastArgs
                    {
                        Message = Strings.Commands_GetFromBitmapQrCode_Success, Result = CommandResult.KeepOpen()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }

        return CommandResult.ShowToast(new ToastArgs
        {
            Message = Strings.Commands_GetFromClipboardQrCode_NotFound, Result = CommandResult.KeepOpen()
        });
    }
}