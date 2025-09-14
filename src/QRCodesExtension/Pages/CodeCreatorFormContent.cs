// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using JPSoftworks.QrCodesExtension.Resources;
using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension.Pages;

internal sealed partial class CodeCreatorFormContent : FormContent
{
    private readonly CodeCreatorPage _codeCreatorPage;

    public CodeCreatorFormContent(CodeCreatorPage codeCreatorPage)
    {
        this._codeCreatorPage = codeCreatorPage;

        this.DataJson = """
                        {
                             "input": ""
                        }
                        """;

        this.TemplateJson = $$"""
{
  "type": "AdaptiveCard",
  "$schema": "https://adaptivecards.io/schemas/adaptive-card.json",
  "version": "1.5",
  "body": [
    {
      "type": "Input.Text",
                                        "label": "{{EscapeJson(Strings.CodeCreator_Input_Label)}}",
                                        "placeholder": "{{EscapeJson(Strings.CodeCreator_Input_Placeholder)}}",
      "id": "input",
      "isMultiline": true,
      "isRequired": true,
      "value": "${$root.input}"
    },
    {
      "type": "Input.ChoiceSet",
                                        "label": "{{EscapeJson(Strings.CodeCreator_ErrorCorrection_Label)}}",
      "choices": [
                                            {
                                                "title": "{{EscapeJson(Strings.CodeCreator_ErrorCorrection_Choice_Low)}}",
                                                "value": "Low"
                                            },
                                            {
                                                "title": "{{EscapeJson(Strings.CodeCreator_ErrorCorrection_Choice_Medium)}}",
                                                "value": "Medium"
                                            },
                                            {
                                                "title": "{{EscapeJson(Strings.CodeCreator_ErrorCorrection_Choice_Quartile)}}",
                                                "value": "Quartile"
                                            },
                                            {
                                                "title": "{{EscapeJson(Strings.CodeCreator_ErrorCorrection_Choice_High)}}",
                                                "value": "High"
                                            }
      ],
                                        "placeholder": "{{EscapeJson(Strings.CodeCreator_ErrorCorrection_Placeholder)}}",
      "isRequired": true,
      "id": "ec",
      "value": "Medium"
    },
    {
      "type": "Input.Number",
                                        "label": "{{EscapeJson(Strings.CodeCreator_ModuleSize_Label)}}",
      "min": 2,
      "max": 64,
      "isRequired": true,
      "value": 20,
      "id": "moduleSize"
    }
  ],
  "actions": [
    {
      "type": "Action.Submit",
                                        "title": "{{EscapeJson(Strings.CodeCreator_Create_ActionTitle)}}"
    }
  ]
}
""";
    }

    private static string EscapeJson(string? value) => JsonEncodedText.Encode(value ?? "").ToString();

    public override ICommandResult SubmitForm(string inputs)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(inputs,
            GenericJsonSerializerContext.Default.Dictionary);
        if (dict != null && dict.TryGetValue("input", out var input) && dict.TryGetValue("ec", out var ec) &&
            dict.TryGetValue("moduleSize", out var moduleSize))
        {
            var statusMessage = new StatusMessage
            {
                Message = Strings.CodeCreator_Status_Creating,
                Progress = new ProgressState { IsIndeterminate = true },
                State = MessageState.Info
            };
            ExtensionHost.Host!.ShowStatus(statusMessage, StatusContext.Extension);

            try
            {
                var qrCodeData = new QrCode(
                    Guid.NewGuid(),
                    input.ToString() ?? string.Empty,
                    Enum.TryParse<QrErrorCorrection>(ec.ToString(), out var v) ? v : QrErrorCorrection.Medium,
                    int.Parse(moduleSize.ToString() ?? "20"),
                    DateTime.UtcNow,
                    false);

                ExtensionHost.Host.HideStatus(statusMessage);
                QrCodeManager.Instance.Add(qrCodeData);
                this._codeCreatorPage.MoveHome();
                return CommandResult.GoBack();
            }
            catch (Exception ex)
            {
                ExtensionHost.Host.HideStatus(statusMessage);
                ExtensionHost.Host.ShowStatus(new StatusMessage { Message = ex.ToString(), State = MessageState.Error },
                    StatusContext.Page);
            }
            finally
            {
                ExtensionHost.Host.HideStatus(statusMessage);
            }
        }

        return CommandResult.KeepOpen();
    }

    public void SetInput(string value)
    {
        var dict = new Dictionary<string, object> { ["input"] = value };
        this.DataJson = JsonSerializer.Serialize(dict, GenericJsonSerializerContext.Default.Dictionary);
        this.OnPropertyChanged(nameof(this.DataJson));
    }
}