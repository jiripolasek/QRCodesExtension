// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension;

public static class Program
{
    [MTAThread]
    public static async Task Main(string[] args)
    {
        await ExtensionHostRunner.RunAsync(
            args,
            new ExtensionHostRunnerParameters
            {
                PublisherMoniker = "JPSoftworks",
                ProductMoniker = "QRCodesExtension",
                IsDebug = false, // default is false
                EnableEfficiencyMode = true, // default is true
                ExtensionFactories =
                [
                    new DelegateExtensionFactory(manualResetEvent => new QrCodesExtension(manualResetEvent))
                ]
            });
    }
}