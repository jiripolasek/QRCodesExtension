// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using Windows.System;
using Microsoft.CommandPalette.Extensions;

namespace JPSoftworks.QrCodesExtension;

internal static class KeyChords
{
    internal static KeyChord Delete =>
        new(VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, (int)VirtualKey.Delete, 0);

    internal static KeyChord Export =>
        new(VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, (int)VirtualKey.E, 0);
}