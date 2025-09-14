// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.QrCodesExtension.Resources;
using JPSoftworks.QrCodesExtension.Services;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.QrCodesExtension.Pages;

internal sealed partial class CodePreviewFormContent : FormContent
{
    public CodePreviewFormContent(Uri imageUri)
    {
        this.TemplateJson = $$"""
                              {
                                  "type": "AdaptiveCard",
                                  "$schema": "https://adaptivecards.io/schemas/adaptive-card.json",
                                  "version": "1.5",
                                  "body": [
                                      {
                                          "type": "Image",
                                          "url": "{{imageUri}}"
                                      }
                                  ]
                              }
                              """;
    }
}

internal sealed partial class WorkingFormContent : FormContent
{
    public WorkingFormContent(CodeCreatorPage codeCreatorPage, QrCode qrCodeData)
    {
        this.TemplateJson = $$"""
                            {
                                "type": "AdaptiveCard",
                                "$schema": "https://adaptivecards.io/schemas/adaptive-card.json",
                                "version": "1.5",
                                "body": [
                                    {
                                        "type": "ProgressRing",
                                        "label": "{{Strings.WorkingForm_Label}}",
                                        "horizontalAlignment": "Center"
                                    },
                                    {
                                        "type": "TextBlock",
                                        "text": "{{Strings.WorkingForm_Text}}",
                                        "wrap": true,
                                        "style": "heading",
                                        "weight": "Lighter",
                                        "color": "Accent",
                                        "horizontalAlignment": "Center"
                                    }
                                ]
                            }
                            """;

        _ = Task.Run(() =>
        {
            QrCodeManager.Instance.Add(qrCodeData);
            codeCreatorPage.MoveHome();
        });
    }
}