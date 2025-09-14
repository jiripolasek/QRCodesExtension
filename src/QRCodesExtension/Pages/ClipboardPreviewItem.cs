// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Commands;
using JPSoftworks.QrCodesExtension.Helpers;
using JPSoftworks.QrCodesExtension.Resources;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension.Pages;

internal sealed partial class ClipboardPreviewListItem : ListItem
{
    private static readonly string[]? NewLinesSeparators = ["\r\n", "\r", "\n"];
    private readonly InstantQrCodeCommand _command;

    public ClipboardPreviewListItem(CodesIndexPage indexPage, SettingsManager settingsManager)
    {
        this.Command = this._command = new InstantQrCodeCommand("", indexPage, settingsManager);
        this.Subtitle = Strings.CodesIndexPage_ClipboardPreview_Subtitle;
        this.Icon = Icons.QrClipboard;
        this.Update(string.Empty);
    }

    internal void Update(string clipboardText)
    {
        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            this.Title = string.Empty;
            this.Subtitle = Strings.CodesIndexPage_ClipboardPreview_Empty;
            this.Details = null;
        }
        else
        {
            this.Title = Strings.CodesIndexPage_ClipboardPreview_Title;
            this.Subtitle = LimitToTwoLines(clipboardText);
            this.Details = new Details
            {
                Title = Strings.CodesIndexPage_ClipboardPreview_DetailsTitle,
                Body = MarkdownHelpers.WrapInCodeBlock(clipboardText)
            };
        }

        this._command.Input = clipboardText;
    }

    private static string LimitToTwoLines(string clipboardText)
    {
        var lines = clipboardText.Split(NewLinesSeparators, StringSplitOptions.None);
        return lines.Length <= 2 ? clipboardText : lines[0] + Environment.NewLine + lines[1];
    }
}