// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Text.Json;
using JPSoftworks.CommandPalette.Extensions.Toolkit.Logging;
using JPSoftworks.QrCodesExtension.Pages;

namespace JPSoftworks.QrCodesExtension.Services;

internal sealed class QrCodeManager : IDisposable
{
    private static readonly Lazy<QrCodeManager> _instance = new(() => new QrCodeManager(new QrCodeGenerator()));

    private readonly QrCodeGenerator _codeGenerator;
    private readonly List<QrCode> _codes = [];
    private readonly SemaphoreSlim _eventMutex = new(1, 1); // Lock for event invocations
    private readonly string _metadataFile;
    private readonly SemaphoreSlim _mutex = new(1, 1); // Async-compatible lock
    private readonly string _rootFolder;

    private QrCodeManager(QrCodeGenerator codeGenerator)
    {
        ArgumentNullException.ThrowIfNull(codeGenerator);

        this._codeGenerator = codeGenerator;
        this._rootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QRCodesExtension");
        Directory.CreateDirectory(this._rootFolder);
        this._metadataFile = Path.Combine(this._rootFolder, "qrcodes.json");
        this.Load();
    }

    public static QrCodeManager Instance => _instance.Value;

    public void Dispose()
    {
        this._mutex?.Dispose();
        this._eventMutex?.Dispose();
    }

    // Events for notifying about changes
    public event EventHandler<QrCodeEventArgs>? QrCodeAdded;
    public event EventHandler<QrCodeEventArgs>? QrCodeDeleted;

    public async Task<IReadOnlyList<QrCode>> GetAllAsync(CancellationToken ct = default)
    {
        await this._mutex.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return this._codes
                .OrderByDescending(c => c.CreatedUtc)
                .ToList(); // snapshot
        }
        finally
        {
            this._mutex.Release();
        }
    }

    public async Task<QrCode> AddAsync(
        string value,
        QrErrorCorrection errorCorrection,
        int moduleSize,
        string? existingImage = null,
        CancellationToken ct = default)
    {
        var code = new QrCode(Guid.NewGuid(), value, errorCorrection, moduleSize, DateTime.UtcNow,
            existingImage is not null);

        if (existingImage is null)
        {
            byte[]? bytes = null;
            try
            {
                bytes = await Task.Run(() => this._codeGenerator.GenerateQrCode(code), ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            if (bytes is not null)
            {
                try
                {
                    await File.WriteAllBytesAsync(this.GetPngPath(code.Id), bytes, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }
        else
        {
            try
            {
                var destFile = this.GetPngPath(code.Id);
                File.Copy(existingImage, destFile, true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        await this._mutex.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            this._codes.Add(code);
        }
        finally
        {
            this._mutex.Release();
        }

        await this.SaveAsync(ct).ConfigureAwait(false);

        // Raise the event after successful add
        this.RaiseEventSafely(() => this.QrCodeAdded?.Invoke(this, new QrCodeEventArgs(code)));

        return code;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        QrCode? deletedCode = null;
        var removed = false;
        await this._mutex.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var idx = this._codes.FindIndex(c => c.Id == id);
            if (idx >= 0)
            {
                deletedCode = this._codes[idx];
                this._codes.RemoveAt(idx);
                removed = true;
            }
        }
        finally
        {
            this._mutex.Release();
        }

        if (!removed)
        {
            return removed;
        }

        try
        {
            var file = this.GetPngPath(id);
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }

        await this.SaveAsync(ct).ConfigureAwait(false);

        // Raise the event after successful delete
        if (deletedCode != null)
        {
            this.RaiseEventSafely(() => this.QrCodeDeleted?.Invoke(this, new QrCodeEventArgs(deletedCode)));
        }

        return removed;
    }

    public IReadOnlyList<QrCode> GetAll()
    {
        return this.GetAllAsync().GetAwaiter().GetResult();
    }

    public QrCode Add(string value, QrErrorCorrection errorCorrection, int moduleSize, string? existingImage)
    {
        return this.AddAsync(value, errorCorrection, moduleSize, existingImage).GetAwaiter().GetResult();
    }

    public QrCode Add(QrCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return this.AddAsync(code.Value, code.ErrorCorrection, code.ModuleSize).GetAwaiter().GetResult();
    }

    public bool Delete(Guid id)
    {
        return this.DeleteAsync(id).GetAwaiter().GetResult();
    }

    public string GetPngPath(Guid id)
    {
        return Path.Combine(this._rootFolder, $"{id}.png");
    }

    // Helper method to safely raise events with proper synchronization
    private void RaiseEventSafely(Action eventInvocation)
    {
        _ = Task.Run(async () =>
        {
            await this._eventMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                eventInvocation?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            finally
            {
                this._eventMutex.Release();
            }
        });
    }

    private void Load()
    {
        this.LoadAsync().GetAwaiter().GetResult();
    }

    private async Task LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(this._metadataFile))
        {
            return;
        }

        try
        {
            QrCodeStore? store = null;
            // Attempt new format
            try
            {
                await using var fs = new FileStream(this._metadataFile, FileMode.Open, FileAccess.Read, FileShare.Read,
                    4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                store = await JsonSerializer.DeserializeAsync(fs, QrCodeJsonContext.Default.QrCodeStore, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            if (store is null)
            {
                return;
            }

            await this._mutex.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                this._codes.Clear();
                this._codes.AddRange(store.Codes);
            }
            finally
            {
                this._mutex.Release();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    private void Save()
    {
        this.SaveAsync().GetAwaiter().GetResult();
    }

    private async Task SaveAsync(CancellationToken ct = default)
    {
        List<QrCode> snapshot;
        await this._mutex.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            snapshot = this._codes.ToList();
        }
        finally
        {
            this._mutex.Release();
        }

        var store = new QrCodeStore { Codes = snapshot, Version = 1 };
        var temp = this._metadataFile + ".tmp";
        try
        {
            await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await JsonSerializer.SerializeAsync(fs, store, QrCodeJsonContext.Default.QrCodeStore, ct)
                    .ConfigureAwait(false);
            }

            // Atomic replace
            File.Copy(temp, this._metadataFile, true);
            File.Delete(temp);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }
}