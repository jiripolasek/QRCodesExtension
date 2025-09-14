// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Text.Json.Serialization;

namespace JPSoftworks.QrCodesExtension.Services;

public class QrCodeType
{
    public QrCodeType()
    {
        this.Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        this.CreatedAt = DateTime.UtcNow;
    }

    public QrCodeType(string displayName, QrCodeCategory category = QrCodeCategory.Unknown) : this()
    {
        this.DisplayName = displayName;
        this.Category = category;
    }

    // New overload allowing explicit type identifier
    public QrCodeType(string displayName, string typeId, QrCodeCategory category = QrCodeCategory.Unknown) : this()
    {
        this.DisplayName = displayName;
        this.TypeId = typeId;
        this.Category = category;
    }

    /// <summary>
    ///     Human-readable name of the QR code type (e.g., "Wi-Fi", "Email", "Phone number")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    ///     Stable machine-friendly identifier for the specific QR code subtype (e.g., "wifi", "email", "phone", "url", "vcard").
    ///     This is more granular than <see cref="Category"/> and can be used for icon mapping, analytics, etc.
    ///     Keep this value lowercase and without spaces for consistency.
    /// </summary>
    public string TypeId { get; set; } = string.Empty;

    /// <summary>
    ///     Categorizes the QR code into broad functional groups
    /// </summary>
    public QrCodeCategory Category { get; set; } = QrCodeCategory.Unknown;

    /// <summary>
    ///     Raw QR code data that was parsed
    /// </summary>
    public string? RawData { get; set; }

    /// <summary>
    ///     Structured metadata extracted from the QR code
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; }

    /// <summary>
    ///     When this QR code was parsed
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     Indicates if the QR code was successfully parsed and contains valid data
    /// </summary>
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(this.DisplayName) && this.DisplayName != "Invalid" &&
                           this.Metadata.Count > 0;

    /// <summary>
    ///     Indicates if this QR code contains actionable data (can be acted upon)
    /// </summary>
    [JsonIgnore]
    public bool IsActionable =>
        this.Category switch
        {
            QrCodeCategory.Communication => this.HasMetadata("Phone", "Email", "Number"),
            QrCodeCategory.Network => this.HasMetadata("Url", "SSID"),
            QrCodeCategory.Contact => this.HasMetadata("FullName", "Name", "N"),
            QrCodeCategory.Location => this.HasMetadata("Latitude", "Longitude"),
            _ => false
        };

    /// <summary>
    ///     Gets a user-friendly description of the QR code content
    /// </summary>
    [JsonIgnore]
    public string Description => this.GenerateDescription() ?? "";

    /// <summary>
    ///     Gets the primary value from the metadata (the most important piece of information)
    /// </summary>
    [JsonIgnore]
    public string? PrimaryValue => this.GetPrimaryValue();

    /// <summary>
    ///     Checks if any of the specified metadata keys exist
    /// </summary>
    public bool HasMetadata(params string[] keys)
    {
        return keys.Any(key => this.Metadata.ContainsKey(key) && !string.IsNullOrWhiteSpace(this.Metadata[key]));
    }

    /// <summary>
    ///     Gets metadata value safely (returns null if key doesn't exist)
    /// </summary>
    public string? GetMetadata(string key)
    {
        return this.Metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    ///     Sets metadata value and returns this instance for chaining
    /// </summary>
    public QrCodeType SetMetadata(string key, string value)
    {
        this.Metadata[key] = value;
        return this;
    }

    /// <summary>
    ///     Removes metadata key and returns this instance for chaining
    /// </summary>
    public QrCodeType RemoveMetadata(string key)
    {
        this.Metadata.Remove(key);
        return this;
    }

    /// <summary>
    ///     Creates a copy of this QrCodeType
    /// </summary>
    public QrCodeType Clone()
    {
        return new QrCodeType(this.DisplayName, this.TypeId, this.Category)
        {
            RawData = this.RawData,
            Metadata = new Dictionary<string, string>(this.Metadata, StringComparer.OrdinalIgnoreCase),
            CreatedAt = this.CreatedAt
        };
    }

    private string? GenerateDescription()
    {
        return this.Category switch
        {
            QrCodeCategory.Communication when this.HasMetadata("Phone") =>
                $"{this.GetMetadata("Phone")}",
            QrCodeCategory.Communication when this.HasMetadata("Email") =>
                $"{this.GetMetadata("Email")}",
            QrCodeCategory.Communication when this.HasMetadata("Number") =>
                $"{this.GetMetadata("Number")}",
            QrCodeCategory.Network when this.HasMetadata("Url") =>
                $"{this.GetMetadata("Host") ?? this.GetMetadata("Url")}",
            QrCodeCategory.Network when this.HasMetadata("SSID") =>
                $"SSID '{this.GetMetadata("SSID")}'",
            QrCodeCategory.Contact when this.HasMetadata("Full Name") =>
                $"{this.GetMetadata("Full Name")}",
            QrCodeCategory.Contact when this.HasMetadata("Name") =>
                $"{this.GetMetadata("Name")}",
            QrCodeCategory.Location when this.HasMetadata("Latitude", "Longitude") =>
                $"{this.GetMetadata("Latitude")}, {this.GetMetadata("Longitude")}",
            QrCodeCategory.Text when this.HasMetadata("Text") => this.GetMetadata("Text")?.Length > 50
                ? this.GetMetadata("Text")?[..47] + "..."
                : this.GetMetadata("Text"),
            _ => this.DisplayName
        };
    }

    private string? GetPrimaryValue()
    {
        return this.Category switch
        {
            QrCodeCategory.Communication => this.GetMetadata("Phone") ??
                                            this.GetMetadata("Email") ?? this.GetMetadata("Number"),
            QrCodeCategory.Network => this.GetMetadata("Url") ?? this.GetMetadata("SSID"),
            QrCodeCategory.Contact => this.GetMetadata("FullName") ?? this.GetMetadata("Name"),
            QrCodeCategory.Location => this.HasMetadata("Latitude", "Longitude")
                ? $"{this.GetMetadata("Latitude")},{this.GetMetadata("Longitude")}"
                : null,
            QrCodeCategory.Text => this.GetMetadata("Text"),
            _ => this.Metadata.FirstOrDefault().Value
        };
    }

    public override string ToString()
    {
        return $"{this.DisplayName}: {this.Description}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not QrCodeType other)
        {
            return false;
        }

        return this.DisplayName == other.DisplayName
               && this.TypeId == other.TypeId
               && this.Category == other.Category
               && this.RawData == other.RawData;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.DisplayName, this.TypeId, this.Category, this.RawData);
    }
}