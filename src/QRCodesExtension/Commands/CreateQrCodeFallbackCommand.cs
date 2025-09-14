// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;
using CodeCreatorPage = JPSoftworks.QrCodesExtension.Pages.CodeCreatorPage;

namespace JPSoftworks.QrCodesExtension.Commands;

internal sealed class CreateQrCodeFallbackCommand : FallbackCommandItem
{
    private readonly CodeCreatorPage _creatorPage;
    private readonly InstantQrCodeCommand _ic;

    public CreateQrCodeFallbackCommand(SettingsManager settingsManager) : base(new NoOpCommand(), "Create QR code")
    {
        this._ic = new InstantQrCodeCommand(string.Empty, null, settingsManager);
        this._creatorPage = new CodeCreatorPage();
        this.Command = this._creatorPage;
        this.Icon = Icons.NewQrCode;
        this.Title = "Create QR code";
        this.Subtitle = "Create a new QR code from input text";
        this.MoreCommands =
        [
            new CommandContextItem(this._ic)
        ];
    }

    public override void UpdateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
        {
            this.Title = string.Empty;
            this._creatorPage.Name = string.Empty;
        }
        else
        {
            this.Title = $"Create QR code for \"{query}\"";
            this._ic.Input = query.Trim();
            this._creatorPage.Name = "Create QR code";
            this._creatorPage.SetInput(query.Trim());
        }
    }
}