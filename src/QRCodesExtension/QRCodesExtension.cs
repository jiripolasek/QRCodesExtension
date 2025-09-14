// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions;

namespace JPSoftworks.QrCodesExtension;

[Guid("e46f1678-9c7b-4be6-b304-a3bb059af2c3")]
public sealed partial class QrCodesExtension(ManualResetEvent extensionDisposedEvent) : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent = extensionDisposedEvent;

    private readonly QrCodesExtensionCommandsProvider _provider = new();

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => this._provider,
            _ => null
        };
    }

    public void Dispose() => this._extensionDisposedEvent.Set();
}