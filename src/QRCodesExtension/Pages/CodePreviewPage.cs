// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension.Pages;

internal sealed partial class CodePreviewPage : ContentPage
{
    private readonly string _previewPath;

    public CodePreviewPage(string previewPath)
    {
        this.Icon = Icons.QrCode;
        this.Title = "QR code preview";
        this.Name = "Preview";
        this._previewPath = previewPath;
    }

    public override IContent[] GetContent() => [new CodePreviewFormContent(new Uri(this._previewPath))];
}