// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension.Helpers;

internal partial class NumberSetting : Setting<string>
{
    public string Placeholder { get; set; } = string.Empty;

    public int Minimum { get; set; }

    public int Maximum { get; set; }

    public int DefaultValue { get; set; }

    private NumberSetting()
        : base()
    {
        this.Value = string.Empty;
    }

    public NumberSetting(string key, string defaultValue)
        : base(key, defaultValue)
    {
    }

    public NumberSetting(string key, string label, string description, string defaultValue)
        : base(key, label, description, defaultValue)
    {
    }

    public override Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "type", "Input.Number" },
            { "title", this.Label },
            { "id", this.Key },
            { "label", this.Description },
            { "value", int.TryParse(this.Value, out var n) ? n : this.DefaultValue },
            { "min", this.Minimum},
            { "max", this.Maximum },
            { "isRequired", this.IsRequired },
            { "errorMessage", this.ErrorMessage },
            { "placeholder", this.Placeholder },
        };
    }

    public override void Update(JsonObject payload)
    {
        // If the key doesn't exist in the payload, don't do anything
        if (payload[this.Key] is not null)
        {
            this.Value = payload[this.Key]?.GetValue<string>();
        }
    }

    public override string ToState() => $"\"{this.Key}\": {JsonSerializer.Serialize(this.Value)}";
}