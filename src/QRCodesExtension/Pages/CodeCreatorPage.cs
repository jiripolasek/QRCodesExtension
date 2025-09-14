// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using JPSoftworks.QrCodesExtension.Resources; // added for localization

namespace JPSoftworks.QrCodesExtension.Pages;

internal sealed partial class CodeCreatorPage : ContentPage
{
    private readonly CodeCreatorFormContent _codeCreatorForm;

    private IContent _current;

    public CodeCreatorPage()
    {
        this._codeCreatorForm = new CodeCreatorFormContent(this);
        this._current = this._codeCreatorForm;

        this.Icon = Icons.NewQrCode;
        this.Title = Strings.CodeCreatorPage_Title;
        this.Name = Strings.CodeCreatorPage_Name;
    }

    public void SetInput(string value)
    {
        this._codeCreatorForm.SetInput(value);
    }

    public override IContent[] GetContent()
    {
        return [this._current];
    }

    public void MoveHome()
    {
        this._current = this._codeCreatorForm;
        this.RaiseItemsChanged();
    }
}