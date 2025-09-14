// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using MixERP.Net.VCards;

namespace JPSoftworks.QrCodesExtension.Services.Parsers;

internal sealed class VCardQrParser : IQrFormatParser
{
    private const string VCardPrefix = "BEGIN:VCARD";
    
    public QrCodeType? Parse(string input)
    {
        if (!input.StartsWith(VCardPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var cards = TryDeserializeWithLibrary(input);
        if (cards == null || !cards.Any())
        {
            return FallbackManualParse(input);
        }

        var metadata = cards.First() is { } firstCard
            ? ExtractMetadata(firstCard)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return new QrCodeType("Contact (vCard)", QrCodeCategory.Contact)
        {
            Metadata = metadata,
            RawData = input
        };
    }

    private static IEnumerable<VCard>? TryDeserializeWithLibrary(string input)
    {
        try
        {
            var normalized = input.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
            return Deserializer.GetVCards(normalized);
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, string> ExtractMetadata(VCard card)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        TryAddFormattedName(card, metadata);
        TryAddNameComponents(card, metadata);
        TryAddEmails(card, metadata);
        TryAddPhones(card, metadata);
        TryAddOrganization(card, metadata);
        TryAddSimpleList(card, metadata, "Titles", "Title");
        TryAddSimpleList(card, metadata, "Urls", "URL");
        TryAddSimpleList(card, metadata, "Notes", "Note");
        TryAddAggregatedList(card, metadata, "Categories", "Categories");
        TryAddAddress(card, metadata);

        return metadata;
    }

    private static void TryAddFormattedName(VCard card, Dictionary<string, string> metadata)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(card.FormattedName))
            {
                metadata["Full Name"] = card.FormattedName;
            }
        }
        catch { }
    }

    private static void TryAddNameComponents(VCard card, Dictionary<string, string> metadata)
    {
        try
        {
            AddIfNotEmpty("Last Name", card.LastName);
            AddIfNotEmpty("First Name", card.FirstName);
            AddIfNotEmpty("Middle Name", card.MiddleName);
            AddIfNotEmpty("Prefix", card.Prefix);
            AddIfNotEmpty("Suffix", card.Suffix);

            void AddIfNotEmpty(string key, string? value)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        metadata[key] = value!;
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private static void TryAddEmails(VCard card, Dictionary<string, string> metadata)
    {
        try
        {
            if (card.Emails is not IEnumerable<object> emails)
            {
                return;
            }

            int index = 0;
            foreach (var email in emails)
            {
                try
                {
                    var emailAddress = email.GetType()
                        .GetProperty("EmailAddress")
                        ?.GetValue(email) as string;

                    if (!string.IsNullOrWhiteSpace(emailAddress))
                    {
                        metadata[index == 0 ? "Email" : $"Email {index + 1}"] = emailAddress;
                        index++;
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private static void TryAddPhones(VCard card, Dictionary<string, string> metadata)
    {
        try
        {
            if (card.Telephones is not IEnumerable<object> phones)
            {
                return;
            }

            int index = 0;
            foreach (var phone in phones)
            {
                try
                {
                    var number = phone.GetType()
                        .GetProperty("Number")
                        ?.GetValue(phone) as string;

                    if (!string.IsNullOrWhiteSpace(number))
                    {
                        metadata[index == 0 ? "Phone" : $"Phone {index + 1}"] = number;
                        index++;
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private static void TryAddOrganization(VCard card, Dictionary<string, string> metadata)
    {
        try
        {
            if (card.Organization == null)
            {
                return;
            }

            var units = card.Organization.GetType()
                .GetProperty("Units")
                ?.GetValue(card.Organization) as IEnumerable<string>;

            if (units == null)
            {
                return;
            }

            var orgString = string.Join(" ", units.Where(u => !string.IsNullOrWhiteSpace(u)));
            if (!string.IsNullOrWhiteSpace(orgString))
            {
                metadata["Organization"] = orgString;
            }
        }
        catch { }
    }

    private static void TryAddSimpleList(VCard card, Dictionary<string, string> metadata, 
        string propertyName, string displayLabel)
    {
        try
        {
            var list = GetPropertyValue(card, propertyName) as IEnumerable<object>;
            if (list == null)
            {
                return;
            }

            int index = 0;
            foreach (var item in list)
            {
                var value = item as string ?? item?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    metadata[index == 0 ? displayLabel : $"{displayLabel} {index + 1}"] = value;
                    index++;
                }
            }
        }
        catch { }
    }

    private static void TryAddAggregatedList(VCard card, Dictionary<string, string> metadata, 
        string propertyName, string displayLabel)
    {
        try
        {
            var list = GetPropertyValue(card, propertyName) as IEnumerable<object>;
            if (list == null)
            {
                return;
            }

            var values = list
                .Select(o => o as string ?? o?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Cast<string>();

            if (values.Any())
            {
                metadata[displayLabel] = string.Join(", ", values);
            }
        }
        catch { }
    }

    private static void TryAddAddress(VCard card, Dictionary<string, string> metadata)
    {
        try
        {
            if (card.DeliveryAddress == null)
            {
                return;
            }

            var addressParts = new[] { "Street", "Locality", "Region", "PostalCode", "Country" }
                .Select(propName => card.DeliveryAddress.GetType()
                    .GetProperty(propName)
                    ?.GetValue(card.DeliveryAddress) as string)
                .Where(val => !string.IsNullOrWhiteSpace(val));

            var formatted = string.Join(", ", addressParts);
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                metadata["Address"] = formatted;
            }
        }
        catch { }
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        try
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }

    private static QrCodeType FallbackManualParse(string input)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = input.Split(["\r\n", "\n"], StringSplitOptions.None);

        foreach (var line in lines)
        {
            if (line.StartsWith("FN:", StringComparison.OrdinalIgnoreCase))
            {
                metadata["Full Name"] = line[3..];
            }
            else if (line.StartsWith("N:", StringComparison.OrdinalIgnoreCase))
            {
                ParseNameLine(line, metadata);
            }
            else if (line.StartsWith("TEL:", StringComparison.OrdinalIgnoreCase))
            {
                metadata["Phone"] = line[4..];
            }
            else if (line.StartsWith("EMAIL:", StringComparison.OrdinalIgnoreCase))
            {
                metadata["Email"] = line[6..];
            }
        }

        return new QrCodeType("vCard", QrCodeCategory.Contact)
        {
            Metadata = metadata,
            RawData = input
        };
    }

    private static void ParseNameLine(string line, Dictionary<string, string> metadata)
    {
        var nameParts = line[2..].Split(';');
        var (family, given, additional, prefixes, suffixes) = (
            GetPart(0), GetPart(1), GetPart(2), GetPart(3), GetPart(4)
        );

        AddIfNotEmpty("Last Name", family);
        AddIfNotEmpty("First Name", given);
        AddIfNotEmpty("Middle Name", additional);
        AddIfNotEmpty("Prefix", prefixes);
        AddIfNotEmpty("Suffix", suffixes);

        var displayName = BuildDisplayName(prefixes, given, additional, family, suffixes);
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            metadata["Name"] = displayName;
            metadata.TryAdd("Full Name", displayName);
        }

        string GetPart(int index) => 
            index < nameParts.Length ? nameParts[index] : string.Empty;

        void AddIfNotEmpty(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                metadata[key] = value;
            }
        }
    }

    private static string BuildDisplayName(params string[] parts)
    {
        return string.Join(" ", parts
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Replace(",", " ")));
    }
}