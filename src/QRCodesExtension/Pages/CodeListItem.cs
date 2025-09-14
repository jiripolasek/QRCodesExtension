// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.CommandPalette.Extensions.Toolkit.Logging;
using JPSoftworks.QrCodesExtension.Commands;
using JPSoftworks.QrCodesExtension.Resources;
using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension.Pages;

internal sealed partial class CodeListItem : ListItem
{
    private static readonly string[]? NewLinesSeparators = ["\r\n", "\r", "\n"];

    private readonly QrCodeMetadataParser _qrCodeMetadataParser;

    // Previously readonly constructor argument; now mutable via property.
    private bool _showIcons;

    // Cache both icon variants so we can switch dynamically when ShowIcons changes.
    private IIconInfo? _qrIcon;
    private IIconInfo? _qrThumbnail;

    public CodeListItem(QrCode qr, QrCodeMetadataParser qrCodeMetadataParser, bool showIcons)
    {
        this._qrCodeMetadataParser = qrCodeMetadataParser;
        this._showIcons = showIcons;
        ArgumentNullException.ThrowIfNull(qr);

        this.Data = qr;
        this.Title = qr.Value;
        _ = Task.Run(() => this.StartCreate());
    }

    public Guid Id => this.Data.Id;

    public DateTime CreatedUtc => this.Data.CreatedUtc;

    public QrCode Data { get; }

    /// <summary>
    /// Whether to show semantic category icons instead of thumbnails. Changing this updates the displayed icon.
    /// </summary>
    public bool ShowIcons
    {
        get => this._showIcons;
        set
        {
            if (this._showIcons != value)
            {
                this._showIcons = value;
                this.UpdateIcon();
            }
        }
    }

    private void UpdateIcon()
    {
        // If generation not finished yet, there is nothing better than current Icon to show.
        // After StartCreate sets _qrIcon / _qrThumbnail this will pick the proper one.
        if (this._qrIcon is null && this._qrThumbnail is null)
        {
            return;
        }

        // If thumbnails are hidden show semantic icon; otherwise prefer thumbnail (falling back to icon if not available)
        this.Icon = this._showIcons ? this._qrIcon ?? this._qrThumbnail : this._qrThumbnail ?? this._qrIcon;
    }

    private async Task StartCreate()
    {
        try
        {
            this.Icon = new IconInfo("\uF16A");
            this.Subtitle = Strings.CodeListItem_Generating;

            var metadata = this._qrCodeMetadataParser.Parse(this.Data.Value);

            IIconInfo? qrThumbnail = null;
            var filePath = QrCodeManager.Instance.GetPngPath(this.Data.Id);
            if (File.Exists(filePath))
            {
                try
                {
                    qrThumbnail = IconInfo.FromStream(await ThumbnailHelper.GetImageThumbnailAsync(filePath));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }

            var qrIcon = GetIcon(metadata);
            qrThumbnail ??= qrIcon;

            // Cache for dynamic switching later.
            this._qrIcon = qrIcon;
            this._qrThumbnail = qrThumbnail;

            this.Title = LimitToTwoLines(metadata.PrimaryValue ?? this.Data.Value);
            this.Subtitle = $"{metadata.DisplayName}";

            this.UpdateIcon();

            if (!string.IsNullOrWhiteSpace(metadata.Description))
            {
                this.Subtitle += $" • {metadata.Description}";
            }

            List<IDetailsElement> detailElements =
            [
                new DetailsElement
                {
                    Key = Strings.CodeListItem_Metadata_Type,
                    Data = new DetailsTags { Tags = [new Tag(metadata.DisplayName)] }
                },
                ..metadata.Metadata.Select(t => BuildMetadataRow(t)),
                new DetailsElement
                {
                    Key = Strings.CodeListItem_Metadata_CreatedAt,
                    Data = new DetailsLink { Text = this.Data.CreatedUtc.ToLocalTime().ToString("g") }
                },
            ];

            if (!this.Data.IsExternal)
            {
                detailElements.Add(
                    new DetailsElement
                    {
                        Key = Strings.CodeListItem_Metadata_ModuleSize,
                        Data = new DetailsLink { Text = this.Data.ModuleSize.ToString() }
                    });
                detailElements.Add(
                    new DetailsElement
                    {
                        Key = Strings.CodeListItem_Metadata_ErrorCorrection,
                        Data = new DetailsLink { Text = this.Data.ErrorCorrection.ToString() }
                    });
            }

            this.Details = new Details
            {
                HeroImage = this._qrThumbnail,
                Body = this.Data.Value,
                Metadata = [.. detailElements]
            };

            this.Command = new CopyBitmapToClipboardCommand(filePath) { Name = Strings.CodeListItem_Command_CopyBitmap };
            this.MoreCommands =
            [
                new CommandContextItem(new CopyTextCommand(this.Data.Value) { Name = Strings.CodeListItem_Command_CopyValue, Icon = Icons.Copy }),
                new CommandContextItem(new PreviewQrCodeCommand(this.Data.Id))
                {
                    Title = Strings.CodeListItem_Command_Preview, Icon = Icons.QrPreview
                },
                new CommandContextItem(new DeleteQrCodeCommand(this.Data.Id))
                {
                    Title = Strings.CodeListItem_Command_Delete, Icon = Icons.Delete, IsCritical = true, RequestedShortcut = KeyChords.Delete
                }
            ];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            this.Subtitle = string.Format(Strings.CodeListItem_Error_Generating_Format, ex.Message);
            this.Icon = new IconInfo("\uF16A");
        }
    }

    private static DetailsElement BuildMetadataRow(KeyValuePair<string, string> t)
    {
        // TODO: make metadata itself more structured to avoid this heuristic
        Uri? uri = null;
        if (Uri.TryCreate(t.Value, UriKind.Absolute, out var u)
           && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeMailto))
        {
            uri = u;
        }

        return new DetailsElement { Key = t.Key, Data = new DetailsLink { Text = t.Value, Link = uri } };
    }

    private static IIconInfo GetIcon(QrCodeType metadata) =>
        metadata.TypeId switch
        {
            QrCodeTypeIds.Url => Icons.QrCodeCategories.Link,
            QrCodeTypeIds.Wifi => Icons.QrCodeCategories.Wifi,
            QrCodeTypeIds.VCard or QrCodeTypeIds.MeCard => Icons.QrCodeCategories.Contact,
            QrCodeTypeIds.Geo => Icons.QrCodeCategories.Location,
            QrCodeTypeIds.MatMsg or QrCodeTypeIds.Email => Icons.QrCodeCategories.Email,
            QrCodeTypeIds.Sms => Icons.QrCodeCategories.Sms,
            QrCodeTypeIds.Phone => Icons.QrCodeCategories.Phone,
            _ => metadata.Category switch
            {
                QrCodeCategory.Communication => Icons.QrCodeCategories.Communication,
                QrCodeCategory.Network => Icons.QrCodeCategories.Network,
                QrCodeCategory.Contact => Icons.QrCodeCategories.Contact,
                QrCodeCategory.Location => Icons.QrCodeCategories.Location,
                _ => Icons.QrCodeCategories.Generic
            }
        };

    private static string LimitToTwoLines(string clipboardText)
    {
        var lines = clipboardText.Split(NewLinesSeparators, StringSplitOptions.None);
        return lines.Length <= 2 ? clipboardText : lines[0] + Environment.NewLine + lines[1] + "...";
    }
}