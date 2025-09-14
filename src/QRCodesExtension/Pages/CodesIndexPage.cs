// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Drawing;
using JPSoftworks.CommandPalette.Extensions.Toolkit.Logging;
using JPSoftworks.QrCodesExtension.Commands;
using JPSoftworks.QrCodesExtension.Helpers;
using JPSoftworks.QrCodesExtension.Resources;
using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ClipboardHelper = JPSoftworks.QrCodesExtension.Helpers.ClipboardHelper;

namespace JPSoftworks.QrCodesExtension.Pages;

internal sealed partial class CodesIndexPage : DynamicListPage, IDisposable
{
    private readonly QrCodeMetadataParser _metadataParser;
    private readonly SettingsManager _settingsManager;
    private readonly ClipboardMonitor _clipboardMonitor;
    private readonly CodeCreatorPage _codeCreatorPage;
    private readonly ListItem _createFromScreenListItem;

    private readonly ClipboardPreviewListItem _clipboardPreviewItem;
    private readonly ListItem _instantCodeListItem;
    private readonly ListItem _newCodeListItem;
    private readonly ListItem _scanBitmapInClipboardListItem;
    private readonly Lock _sync = new();
    private List<CodeListItem> _codes = [];
    private bool _disposed;
    private bool _loaded;
    private bool _loading;
    private List<IListItem> _results = [];

    public CodesIndexPage(QrCodeMetadataParser metadataParser, SettingsManager settingsManager)
    {
        ArgumentNullException.ThrowIfNull(metadataParser);
        ArgumentNullException.ThrowIfNull(settingsManager);

        this._metadataParser = metadataParser;
        this._settingsManager = settingsManager;

        this._settingsManager.Settings.SettingsChanged += this.Settings_SettingsChanged;

        this.Icon = Icons.QrCode;
        this.Id = "com.jpsoftworks.cmdpal.qrcodes.index";
        this.Title = Strings.CodesIndexPage_Title;
        this.Name = Strings.CodesIndexPage_Name;
        this.PlaceholderText = Strings.CodesIndexPage_Placeholder;
        this.ShowDetails = true;

        this._clipboardMonitor = new ClipboardMonitor();
        this._clipboardMonitor.ClipboardChanged += this.ClipboardMonitorOnClipboardChanged;
        this._clipboardMonitor.StartMonitoring();

        this._codeCreatorPage = new CodeCreatorPage();

        this._newCodeListItem = new ListItem(this._codeCreatorPage)
        {
            Subtitle = Strings.CodesIndexPage_NewCode_Subtitle_Default
        };

        this._clipboardPreviewItem = new ClipboardPreviewListItem(this, settingsManager)
        {
            Title = Strings.CodesIndexPage_ClipboardPreview_Title,
            Subtitle = Strings.CodesIndexPage_ClipboardPreview_Subtitle,
        };

        this._instantCodeListItem = new ListItem
        {
            Title = Strings.CodesIndexPage_Instant_Title,
            Subtitle = Strings.CodesIndexPage_Instant_Subtitle_Default,
            Icon = Icons.QrBolt
        };

        this._createFromScreenListItem = new ListItem(new GetFromScreenCommand(QrCodeManager.Instance, this))
        {
            Title = Strings.CodesIndexPage_FromScreen_Title,
            Subtitle = Strings.CodesIndexPage_FromScreen_Subtitle,
            Icon = Icons.QrScanDesktop
        };

        this._scanBitmapInClipboardListItem = new ListItem(new GetFromClipboardCommand(QrCodeManager.Instance, this))
        {
            Title = Strings.CodesIndexPage_FromClipboardImage_Title,
            Subtitle = Strings.CodesIndexPage_FromClipboardImage_Subtitle,
            Icon = Icons.QrScanClipboard
        };


        QrCodeManager.Instance.QrCodeAdded += this.OnQrCodeAdded;
        QrCodeManager.Instance.QrCodeDeleted += this.OnQrCodeDeleted;

        this.Refresh();
    }

    private void Settings_SettingsChanged(object sender, Settings args)
    {
        this.Refresh();

        lock (this._sync)
        {
            foreach (var code in this._codes)
            {
                code.ShowIcons = this._settingsManager.ShowIcons;
            }
        }
    }

    public override string SearchText
    {
        get => base.SearchText;
        set
        {
            var oldSearch = base.SearchText;
            this.SetSearchNoUpdate(value);
            this.OnPropertyChanged(nameof(this.SearchText));
            this.UpdateSearchText(oldSearch, value);
        }
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        QrCodeManager.Instance.QrCodeAdded -= this.OnQrCodeAdded;
        QrCodeManager.Instance.QrCodeDeleted -= this.OnQrCodeDeleted;

        this._clipboardMonitor.ClipboardChanged -= this.ClipboardMonitorOnClipboardChanged;
        this._clipboardMonitor.StopMonitoring();
        this._clipboardMonitor.Dispose();

        this._disposed = true;
    }

    private void OnQrCodeAdded(object? sender, QrCodeEventArgs e)
    {
        _ = Task.Run(() =>
        {
            try
            {
                lock (this._sync)
                {
                    if (this._loaded)
                    {
                        var existing = this._codes.FirstOrDefault(t => t.Id == e.QrCode.Id);
                        if (existing != null)
                        {
                            this._codes.Remove(existing);
                        }

                        this._codes.Insert(0, new CodeListItem(e.QrCode, this._metadataParser, this._settingsManager.ShowIcons));
                    }
                }

                this.Refresh();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        });
    }

    private void OnQrCodeDeleted(object? sender, QrCodeEventArgs e)
    {
        _ = Task.Run(() =>
        {
            try
            {
                lock (this._sync)
                {
                    if (this._loaded)
                    {
                        var existing = this._codes.FirstOrDefault(t => t.Id == e.QrCode.Id);
                        if (existing != null)
                        {
                            this._codes.Remove(existing);
                        }
                    }
                }

                this.Refresh();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        });
    }

    private void ClipboardMonitorOnClipboardChanged(object? sender, EventArgs e)
    {
        this.Refresh();
    }

    public override IListItem[] GetItems()
    {
        return [.. this._results];
    }

    private async Task BackgroundLoadAsync()
    {
        this.IsLoading = true;

        List<QrCode>? existing = null;
        try
        {
            existing = (await QrCodeManager.Instance.GetAllAsync()).OrderByDescending(c => c.CreatedUtc).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }

        if (existing is not null)
        {
            lock (this._sync)
            {
                if (!this._loaded)
                {
                    this._codes = [.. existing.Select(qr => new CodeListItem(qr, this._metadataParser, this._settingsManager.ShowIcons))];
                    this._loaded = true;
                }
            }

            this.IsLoading = false;
            this.Refresh();
        }
        else
        {
            lock (this._sync)
            {
                this._loading = false;
            }
        }
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        this.Refresh();
    }

    private void Refresh()
    {
        if (!this._loaded && !this._loading)
        {
            lock (this._sync)
            {
                if (!this._loaded && !this._loading)
                {
                    this._loading = true;
                    _ = Task.Run(this.BackgroundLoadAsync);
                }
            }
        }

        if (!this._loaded)
        {
            return;
        }

        var searchText = this.SearchText.Trim() ?? string.Empty;
        var results = new List<IListItem>();

        string textToTransform;
        string? clipboardText;
        Bitmap? clipboardBitmap;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            clipboardText = ClipboardHelper.GetText().Trim();
            textToTransform = clipboardText;
            clipboardBitmap = ClipboardHelper.GetBitmap();
        }
        else
        {
            clipboardText = null;
            clipboardBitmap = null;
            textToTransform = searchText;
        }

        if (!string.IsNullOrWhiteSpace(clipboardText))
        {
            this._clipboardPreviewItem.Update(clipboardText);
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            lock (this._sync)
            {
                this._codeCreatorPage.SetInput(string.Empty);
                this._newCodeListItem.Subtitle = Strings.CodesIndexPage_NewCode_Subtitle_NoInput;
                results.Add(this._newCodeListItem);

                results.Add(this._createFromScreenListItem);

                if (!string.IsNullOrWhiteSpace(clipboardText))
                {
                    results.Add(this._clipboardPreviewItem);
                }

                if (clipboardBitmap is { Size: { Width: > 21, Height: > 21 } })
                {
                    // Only show if the image is large enough to contain a QR code
                    results.Add(this._scanBitmapInClipboardListItem);
                }

                results.AddRange(this._loaded
                    ? this._codes.OrderByDescending(c => c.CreatedUtc).OfType<IListItem>().ToList()
                    : []);
            }
        }
        else
        {
            lock (this._sync)
            {
                if (this._loaded)
                {
                    this._codeCreatorPage.SetInput(textToTransform);
                    this._newCodeListItem.Subtitle = string.Format(Strings.CodesIndexPage_NewCode_Subtitle_WithInput, textToTransform);
                    results.Add(this._newCodeListItem);

                    if (clipboardBitmap != null)
                    {
                        results.Add(this._scanBitmapInClipboardListItem);
                    }

                    this._instantCodeListItem.Subtitle = string.Format(Strings.CodesIndexPage_Instant_Subtitle_WithInput, textToTransform);
                    this._instantCodeListItem.Command = new InstantQrCodeCommand(textToTransform, this, _settingsManager);
                    results.Add(this._instantCodeListItem);

                    results.AddRange(this._loaded
                        ? this._codes
                            .Where(t => t.Data.Value is not null &&
                                        t.Data.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(c => c.CreatedUtc).OfType<IListItem>().ToList()
                        : []);
                }
            }
        }

        this._results = results;
        this.RaiseItemsChanged();
    }

    public void ClearSearch()
    {
        if (this.SearchText != string.Empty)
        {
            this.SearchText = string.Empty;
            this.Refresh();
        }
    }
}