// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Text.Json.Serialization;
using JPSoftworks.QrCodesExtension.Pages;

namespace JPSoftworks.QrCodesExtension.Services;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(QrCode))]
[JsonSerializable(typeof(QrCodeStore))]
[JsonSerializable(typeof(List<QrCode>))]
internal sealed partial class QrCodeJsonContext : JsonSerializerContext;