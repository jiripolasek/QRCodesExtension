// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Pages;
using JPSoftworks.QrCodesExtension.Resources;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension.Helpers;

internal sealed partial class SettingsManager : JsonSettingsManager
{
    private static string Namespaced(string propertyName) => $"jpsoftworks.qrcodes.{propertyName}";
    
    private readonly ChoiceSetSetting _instantQrCodeEcc = new(
        Namespaced(nameof(InstantQrCodeEcc)),
        Strings.Settings_InstantQrCode_Ecc_Label,
        Strings.Settings_InstantQrCode_Ecc_Description,
        [
            new ChoiceSetSetting.Choice(Strings.Settings_InstantQrCode_Ecc_Choice_Default, QrErrorCorrection.Medium.ToString("G")),
            new ChoiceSetSetting.Choice(Strings.CodeCreator_ErrorCorrection_Choice_Low, QrErrorCorrection.Low.ToString("G")),
            new ChoiceSetSetting.Choice(Strings.CodeCreator_ErrorCorrection_Choice_Medium, QrErrorCorrection.Medium.ToString("G")),
            new ChoiceSetSetting.Choice(Strings.CodeCreator_ErrorCorrection_Choice_Quartile, QrErrorCorrection.Quartile.ToString("G")),
            new ChoiceSetSetting.Choice(Strings.CodeCreator_ErrorCorrection_Choice_High, QrErrorCorrection.High.ToString("G")),
        ]
    )
    { IsRequired = true };

    public QrErrorCorrection InstantQrCodeEcc => Enum.TryParse<QrErrorCorrection>(this._instantQrCodeEcc.Value, out var v) ? v : QrErrorCorrection.Medium;

    private readonly NumberSetting _elementSize = new(
        Namespaced(nameof(ElementSize)),
        Strings.Settings_InstantQrCode_ElementSize_Label,
        Strings.Settings_InstantQrCode_ElementSize_Description,
        "20")
    { Minimum = 2, Maximum = 64, DefaultValue = 20, IsRequired = true };

    public int ElementSize => int.TryParse(this._elementSize.Value, out var n) ? Math.Clamp(n, 2, 128) : 20;

    private readonly ToggleSetting _showIcons = new(
        Namespaced(nameof(ShowIcons)),
        Strings.Settings_ShowIcons_Label,
        Strings.Settings_ShowIcons_Description,
        false);

    public bool ShowIcons => this._showIcons.Value;

    public SettingsManager()
    {
        this.Settings.Add(this._instantQrCodeEcc);
        this.Settings.Add(this._elementSize);
        this.Settings.Add(this._showIcons);

        this.FilePath = BuildPath("settings.json");
        this.LoadSettings();

        this.Settings.SettingsChanged += this.OnSettingsOnSettingsChanged;
    }

    private void OnSettingsOnSettingsChanged(object o, Settings settings)
    {
        this.SaveSettings();
    }

    private static string BuildPath(string filename)
    {
        var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
        Directory.CreateDirectory(directory);

        // now, the state is just next to the exe
        return Path.Combine(directory, filename);
    }
}