using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Log = CareAdminTestProject.Common.TestLog;

/// <summary>
/// Represents the Summary tab within the incident reporting form.
/// Manages the validation mapping and data entry for conclusions, care plan updates, and required agency reporting.
/// </summary>
public class SummaryTab : BaseIncidentTabs
{
    /// <summary>
    /// Represents comprehensive incident summary details and legal/medical conclusions.
    /// </summary>
    public record IncidentSummaryInfo(
        bool CarePlanUpdated,
        bool SetAsReportable,
        bool MajorInjury,
        bool SendToLegal,
        string Summary,
        string Plan,
        string Conclusion, // Avoidable, Unavoidable, Undetermined
        bool EvidenceOfAbuse, // No = false, Yes = true
        string EvidenceReason,
        bool ReportedToAgency,
        IReadOnlyList<string> PossibleContributingFactor,
        string DirectorSignature // Name for the signature field
    );

    /// <summary>
    /// Constructs a map of fields to their respective summary data entry actions and validation statuses.
    /// </summary>
    /// <param name="data">The incident summary dataset used to populate fields dynamically.</param>
    /// <returns>A dictionary mapping field names to an execution action and a required flag.</returns>
    public Dictionary<string, (Func<Task> Action, bool IsRequired)> GetRequiredFieldsMap(IncidentSummaryInfo data)
    {
        var map = new Dictionary<string, (Func<Task> Action, bool IsRequired)>
    {
        // 1. Checkboxes at the top (Optional fields, IsRequired = false)
        { "Care Plan Updated", (() => SetCheckboxAsync("Care Plan Updated", true), false) },
        { "Set as reportable", (() => SetCheckboxAsync("Set as reportable", true), false) },
        { "Major Injury", (() => SetCheckboxAsync("Major Injury", true), false) },
        { "Send to Legal", (() => SetCheckboxAsync("Send to Legal", true), false) },

        // 2. Rich Text Editors (Required fields, IsRequired = true)
        { "Summary", (() => FillRichTextFieldAsync("summary", data.Summary), true) },
        { "Plan", (() => FillRichTextFieldAsync("summaryPlan", data.Plan), true) },

        // 3. Conclusion Radio Buttons (Required field)
        { "Conclusion Reached", (() =>
            SelectQuestionRadioAsync("Based upon the collection and review of all attached information, the following conclusion has been reached:", data.Conclusion),
            true)
        },

        // 4. Evidence of abuse section (Required field, handles both No/Yes logic and conditional sub-fields)
        { "Evidence", (() => FillEvidenceSectionAsync(true, "Evidence Reason"), true) },

        // 5. This will be reported to the DOH, OHMS... (Required radio field from the second screenshot)
        { "This will be reported to the DOH, OHMS, or other agency to intervene?", (() =>
            SelectSummaryRadioOptionAsync("This will be reported to the DOH, OHMS, or other agency to intervene?", "Yes"),
            true)
        },

        // 6. Possible Contributing Factor (Required Kendo multi-select field)
        { "Possible Contributing Factor", (() => SelectContributingFactorAsync(data.PossibleContributingFactor), true)
        }
    };

        return map;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryTab"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public SummaryTab(IPage page) : base(page) { }

    /// <summary>
    /// Populates the complete Summary form fields with data provided.
    /// </summary>
    /// <param name="info">The model holding all structural incident summary data parameters.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FillSummaryInfoAsync(IncidentSummaryInfo info)
    {
        // 1. Checkboxes at the top
        await SetCheckboxAsync("Care Plan Updated", info.CarePlanUpdated);
        await SetCheckboxAsync("Set as reportable", info.SetAsReportable);
        await SetCheckboxAsync("Major Injury", info.MajorInjury);
        await SetCheckboxAsync("Send to Legal", info.SendToLegal);

        // 2. Rich Text Editors
        await FillRichTextFieldAsync("summary", info.Summary);
        await FillRichTextFieldAsync("summaryPlan", info.Plan);

        // 3. Conclusion Radio Buttons
        await SelectQuestionRadioAsync("Based upon the collection and review of all attached information, the following conclusion has been reached:", info.Conclusion);

        // 4. Evidence of abuse (No/Yes Radio Buttons)
        await FillEvidenceSectionAsync(info.EvidenceOfAbuse, info.EvidenceReason);

        // 5. Reason field
        //await GetFieldByLabel("Evidence Reason").FillAsync(info.EvidenceReason);

        // 3. This will be reported to the DOH, OHMS... (Radio)
        await SelectSummaryRadioOptionAsync("This will be reported to the DOH, OHMS, or other agency to intervene?",
            info.ReportedToAgency ? "Yes" : "No");

        // 4. Possible Contributing Factor (Dropdown/Select)
        // Assuming GetFieldByLabel in your base class can handle Select components
        await SelectContributingFactorAsync(info.PossibleContributingFactor);

        // 5. Director of Nursing or Designee (Signature/Text field)
        //await GetFieldByLabel("Director of Nursing or Designee").FillAsync(info.DirectorSignature);
    }

    /// <summary>
    /// Auxiliary method for handling checkboxes by validating state before performing click actions.
    /// </summary>
    /// <param name="label">The exact inner text identifier of the checkbox container.</param>
    /// <param name="state">The target selection status boolean.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetCheckboxAsync(string label, bool state)
    {
        var fieldContainer = Page.Locator("cad-label-value-field")
            .Filter(new() { HasText = label });

        var checkbox = fieldContainer.Locator("mat-checkbox");

        // Verify the current state via attributes or classes 
        // to prevent unnecessary extra clicks if the state is already correct
        var isChecked = await checkbox.Locator("input").IsCheckedAsync();

        if (isChecked != state)
        {
            await checkbox.ClickAsync();
        }
    }

    /// <summary>
    /// Handles the evidence of abuse question toggles and branches dynamically into dropdown lists or rich text inputs.
    /// </summary>
    /// <param name="choice">The evaluation selection flag indicating if evidence exists.</param>
    /// <param name="reasonOrText">The explicit text reasoning statement or dropdown target description.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FillEvidenceSectionAsync(Boolean choice, string reasonOrText)
    {
        // 1. Select the radio button (using the logic from the previous step)
        await SelectSummaryRadioOptionAsync("There is probable evidence of abuse, neglect or mistreatment:\r\n",
            choice ? "Yes" : " No ");

        if (choice == false)
        {
            // 2. If 'No' is selected — interact with the dropdown list (mat-select)
            // Look for the select element next to the text "Choose reason"
            var select = Page.Locator("mat-select").Filter(new() { HasText = "Choose reason" });
            await select.ClickAsync();

            // Click the option in the dropdown list (they usually render in a separate overlay container at the end of the DOM)
            await Page.Locator("mat-option").GetByText(reasonOrText, new() { Exact = true }).ClickAsync();
        }
        else if (choice == true)
        {
            // 3. If 'Yes' is selected — fill out the iFrame via your rich text method
            // Field name for Evidence Reason (verify in code, likely 'evidenceReason')
            await FillRichTextFieldAsync("evidenceReason", reasonOrText);
        }
    }


    /// <summary>
    /// Locates a question group container by its header section label text and selects a specific target radio choice option value.
    /// </summary>
    /// <param name="sectionLabel">The exact or trimmed descriptive question header text string.</param>
    /// <param name="optionValue">The target radio label option value to click (e.g., "Yes" or "No").</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SelectSummaryRadioOptionAsync(string sectionLabel, string optionValue)
    {
        // 1. Locate the question container based on the header text. 
        // In the screenshot, this is a div containing class="question-field" or simply the text.
        var questionContainer = Page.Locator("div, mat-radio-group")
            .Filter(new() { HasText = sectionLabel.Trim() })
            .Last; // Retrieve the last one if nested layout wrappers exist

        // 2. Search for the radio button inside the container by its name/label text ("No" or "Yes")
        // GetByRole resolves the native input inside mat-radio-button, and Playwright handles clicking the appropriate target area.
        await questionContainer.GetByRole(AriaRole.Radio, new() { Name = optionValue.Trim() })
            .ClickAsync();
        await Task.Delay(300);
    }

    /// <summary>
    /// Opens the Kendo MultiSelect dropdown for contributing factors and activates all provided factor choice selections.
    /// </summary>
    /// <param name="factors">The read-only collection list of targeted contributing factors to toggle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown if an expected contributing factor text string option cannot be resolved inside the Kendo list popup container.</exception>
    public async Task SelectContributingFactorAsync(IReadOnlyList<string> factors)
    {
        if (factors == null || !factors.Any()) return;

        // 1. Click the MultiSelect element container itself.
        // In the screenshot, it is visible that the header "Possible Contributing Factor" stands apart.
        // Resolve the specific interactive field element situated directly AFTER this descriptive text node block.
        var multiSelect = Page.Locator("label:has-text('Possible Contributing Factor') + * kendo-multiselect, kendo-multiselect").First;
        await multiSelect.ClickAsync();

        // 2. Explicitly wait for the list popup element overlay container (kendo-popup) to render
        var popup = Page.Locator("kendo-popup").First;
        await popup.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        foreach (var factor in factors)
        {
            // Use GetByText with loose matching rules, as newline layouts or spacing structures 
            // can surround the target string text inside the DOM.
            var option = popup.Locator("li.k-list-item")
                .GetByText(factor.Trim(), new() { Exact = false });

            if (await option.CountAsync() > 0)
            {
                await option.First.ClickAsync();
                // Introduce a brief execution pause allowing Kendo code components to process the active state adjustment before processing consecutive iterations
                await Page.WaitForTimeoutAsync(200);
            }
            else
            {
                throw new Exception($"Option '{factor}' not found in the list.");
            }
        }

        // 3. Dismiss the active list view overlay layer
        await Page.Keyboard.PressAsync("Escape");
    }

    /// <summary>
    /// Simulates the user sign-off workflow sequence by opening the modal workspace and committing the legal sign signature confirmation button action.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SignAndConfirmIncident()
    {
        var signButton = Page.GetByRole(AriaRole.Button, new() { Name = "Sign Here" });

        await signButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await signButton.ClickAsync();

        var confirmButton = Page.GetByRole(AriaRole.Button, new() { Name = "Confirm", Exact = true })
                                .Filter(new() { Visible = true });

        await confirmButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await confirmButton.ClickAsync();
        await Assertions.Expect(confirmButton).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Удаляет цифровую подпись на вкладке, нажимая на крестик.
    /// </summary>
    public async Task RemoveSignatureAsync()
    {
        Log.Information("[ACTION] Удаляем цифровую подпись с вкладки...");

        // Нацеливаемся строго на компонент подписи внутри панели таба Summary
        var summarySignature = Page.GetByRole(AriaRole.Tabpanel, new() { Name = "Summary" })
                                   .Locator("cad-incident-sign");

        // Скроллим и кликаем по крестику
        await summarySignature.ScrollIntoViewIfNeededAsync();
        await summarySignature.Locator("mat-icon.remove-sign").ClickAsync();

        Log.Information("[ACTION] Подтверждаем удаление...");
        // Ждем и кликаем подтверждение
        await Page.Locator("button:has-text('OK')").ClickAsync();

        Log.Information("[CHECK] Проверяем, что подпись удалена и кнопка 'Sign Here' вернулась...");

        // Проверяем, что кнопка "Sign Here" снова отображается и доступна для клика
        var signHereButton = summarySignature.GetByRole(AriaRole.Button, new() { Name = "Sign Here" });
        await Assertions.Expect(signHereButton).ToBeVisibleAsync(new() { Timeout = 5000 });

    }

    /// <summary>
    /// Evaluates if the signature image container has rendered correctly and contains a populated, operational media source URL.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown if the target image locator successfully registers as visible but lacks a valid source reference attribute.</exception>
    public async Task VerifySignatureImageVisible()
    {
        // 1. Resolve the organizational layout selector context enclosing the signature graphic resource (using the class name from screenshot: signature-box)
        var signatureImage = Page.Locator(".signature-box img");

        // 2. Validate that the target graphic asset not only resides in the DOM tree, but registers as active and visible to the running browser environment view
        await Assertions.Expect(signatureImage).ToBeVisibleAsync(new() { Timeout = 10000 });

        // 3. Supplementary verification checkpoint: examine the layout properties to confirm that a non-empty source attribute link exists
        var src = await signatureImage.GetAttributeAsync("src");
        if (string.IsNullOrEmpty(src))
        {
            throw new Exception("The signature must be present, but the image source URL reference (src) attribute is empty.");
        }

        Console.WriteLine("The signature has been successfully captured and displays properly as an image asset.");
    }
    /// <summary>
    /// Extracts the incident GUID from the current active URL, triggers a file download via the confirmation popup, 
    /// saves the file to a temporary location, and attaches it directly to the NUnit test context result repository.
    /// </summary>
    /// <returns>The absolute local system string file path pointing to the fully downloaded attachment report asset.</returns>
    public async Task<string> DownloadSummaryReportAsync()
    {
        // 1. Extract GUID from the current active address URL string
        // The regular expression searches for the standard GUID structure format (8-4-4-4-12 characters syntax validation)
        var match = Regex.Match(Page.Url, @"([a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12})");
        string guid = match.Success ? match.Value : "unknown-id";

        string attachmentName = $"{guid}_Summary";

        // 2. Initialize the dynamic asynchronous download watcher cycle handler
        var downloadTask = Page.WaitForDownloadAsync(new() { Timeout = 300000 }); // Wait threshold allocated up to 180 seconds

        // 3. Trigger the confirmation event handler via the "Yes" action button control inside the overlay dialog layout
        var yesButton = Page.GetByRole(AriaRole.Button, new() { Name = "Yes", Exact = true });
        await yesButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await yesButton.ClickAsync();

        // 4. Block thread asynchronously until the target streaming file download event terminates successfully
        var download = await downloadTask;

        // 5. Construct local physical file destination indicators (Temp directory path + GUID identity layout string + layout file extensions resolved by the browser layer)
        var extension = Path.GetExtension(download.SuggestedFilename);
        var tempPath = Path.Combine(Path.GetTempPath(), $"{attachmentName}{extension}");

        // 6. Persist buffer stream arrays to local disks and serialize reference metadata directly into NUnit test result context attachments
        await download.SaveAsAsync(tempPath);
        TestContext.AddTestAttachment(tempPath, attachmentName);

        Log.Information($"File is downloaded and attached to test: {tempPath}");
        return tempPath;
    }

    /// <summary>
    /// Reads the runtime check selection status of a standard mat-checkbox UI component resolved by its field label context text.
    /// </summary>
    /// <param name="label">The exact string label description residing within the field container elements.</param>
    /// <returns>True if the matching checkbox input indicator properties register as checked; otherwise, false.</returns>
    public async Task<bool> IsCheckboxCheckedAsync(string label)
    {
        var container = Page.Locator("cad-label-value-field").Filter(new() { HasText = label });
        return await container.Locator("mat-checkbox input").IsCheckedAsync();
    }

    /// <summary>
    /// Reads the runtime disabled status of a standard mat-checkbox UI component resolved by its field label context text.
    /// </summary>
    /// <param name="label">The exact string label description residing within the field container elements.</param>
    /// <returns>True if the matching checkbox input properties register as disabled; otherwise, false.</returns>
    public async Task<bool> IsCheckboxDisabledAsync(string label)
    {
        var container = Page.Locator("cad-label-value-field").Filter(new() { HasText = label });

        // Проверяем нативное свойство disabled у input или атрибут disabled на mat-checkbox
        bool isInputDisabled = await container.Locator("mat-checkbox input").IsDisabledAsync();
        bool isMatCheckboxDisabled = await container.Locator("mat-checkbox").GetAttributeAsync("disabled") != null;

        return isInputDisabled || isMatCheckboxDisabled;
    }

    /// <summary>
    /// Evaluates custom multi-tier layout fallbacks to extract the plaintext data value residing inside a designated Kendo Rich Text UI editor field.
    /// </summary>
    /// <param name="fieldName">The target identifier or technical html element name attribute string configured for the rich text element container.</param>
    /// <returns>A trimmed plaintext string extraction containing the user input values compiled out of paragraph trees or embedded frames.</returns>
    public async Task<string> GetRichTextValueAsync(string fieldName)
    {
        // 1. Resolve and isolate the root kendo-editor node component using targeted identifier property matches
        var kendoEditor = Page.Locator($"kendo-editor[name='{fieldName}'], [id='{fieldName}']").First;
        await kendoEditor.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        // Allocate a brief synchronization timeout margin allowing Angular UI code frameworks to initialize internal workspace nodes fully
        await Page.WaitForTimeoutAsync(300);

        // SYSTEM APPROACH 1: Attempt text parsing workflows utilizing inner iframe DOM structures (applicable if specific Kendo setups construct dynamic nested frame document wrappers)
        var iframe = kendoEditor.Locator("iframe");
        if (await iframe.CountAsync() > 0)
        {
            try
            {
                var frameBody = kendoEditor.FrameLocator("iframe").Locator("body");
                string iframeText = await frameBody.InnerTextAsync();
                if (!string.IsNullOrEmpty(iframeText.Trim())) return iframeText.Trim();
            }
            catch { /* Gracefully catch active frame processing faults and proceed directly to alternative recovery methods */ }
        }

        // SYSTEM APPROACH 2: Attempt validation extraction directly out of custom ProseMirror structures or elements having active contenteditable status values configured
        var editableDiv = kendoEditor.Locator(".k-editor-content [contenteditable='true'], .k-editor-content, .k-content");
        if (await editableDiv.CountAsync() > 0)
        {
            string divText = await editableDiv.First.EvaluateAsync<string>("el => el.textContent || el.innerText") ?? "";
            if (!string.IsNullOrEmpty(divText.Trim())) return divText.Trim();
        }

        // SYSTEM APPROACH 3: Hardened absolute root container inner-text fallback validation recovery sequence
        // If underlying Kendo rendering pipelines split text values across separate child paragraph layouts,
        // triggering InnerTextAsync directly at the root kendo-editor layer aggregates all internal lines.
        // Technical control panel item keywords (B, I, U actions) are stripped out during post-processing.
        string fullEditorText = await kendoEditor.InnerTextAsync();

        // Scrub technical layout keywords and operational toolbox labels commonly populated inside Kendo toolbar headers
        string cleanText = fullEditorText
            .Replace("Format", "")
            .Replace("Font", "")
            .Replace("Summary", "")
            .Replace("Plan", "")
            .Trim();

        return cleanText;
    }

    /// <summary>
    /// Версифицирует, что текстовое поле или Rich Text редактор полностью очищены от контента.
    /// </summary>
    /// <param name="fieldIdentifier">Идентификатор поля (например, "evidenceReason").</param>
    /// <returns>Текущий инстанс страницы для Fluent-цепочек.</returns>
    public async Task<SummaryTab> VerifyRichTextFieldIsEmptyAsync(string fieldIdentifier)
    {
        Log.Information($"[ASSERT] Проверяем, что поле Rich Text '{fieldIdentifier}' полностью пустое...");

        string actualValue = await GetRichTextValueAsync(fieldIdentifier);

        // Очищаем от HTML-тегов, которые Angular Quill/Kendo может оставлять при пустом поле (например, <p><br></p>)
        string cleanText = System.Text.RegularExpressions.Regex.Replace(actualValue, "<.*?>", "").Trim();

        // ПРАВКА: Если Kendo UI при очистке подставил дефолтное техническое слово-заполнитель
        if (cleanText.Equals("Paragraph", StringComparison.OrdinalIgnoreCase))
        {
            cleanText = string.Empty;
        }

        Assert.That(cleanText, Is.Empty, $"Ожидалось, что поле '{fieldIdentifier}' будет пустым, но оно содержит текст: '{actualValue}'");

        return this;
    }

    /// <summary>
    /// Resolves the plaintext string label value associated with the currently enabled choice inside a designated radio option group component.
    /// </summary>
    /// <param name="questionLabel">The descriptive label question text string matching target application blocks.</param>
    /// <returns>A trimmed plaintext descriptive status title representing the selection value choice.</returns>
    public async Task<string> GetSelectedRadioTextAsync(string questionLabel)
    {
        // Universal mapping framework: reconcile human-readable question title text inputs with explicit DOM name attribute variables
        string groupName = "incidentSummaryConclusion"; // Define default target name for the summary conclusion question block

        if (questionLabel.Contains("probable evidence of abuse"))
        {
            groupName = "hasEvidenceOfAbuse"; // Target name structure matching the abuse assessment block questions
        }
        else if (questionLabel.Contains("reported to the DOH"))
        {
            groupName = "reportedToAgency"; // Target name structure matching the agency intervention disclosure block questions
        }

        // 1. Isolate the target radio selection group locator container using target name attribute conditions
        var radioGroup = Page.Locator($"mat-radio-group[name='{groupName}'], mat-radio-group")
            .Filter(new() { Has = Page.Locator("mat-radio-button, mat-mdc-radio-button") });

        // If technical group names cannot be resolved, invoke fallback discovery steps querying structural text blocks inside target layout wrappers
        if (await radioGroup.CountAsync() == 0 || await radioGroup.CountAsync() > 1)
        {
            var questionField = Page.Locator("div.question-field").Filter(new() { HasText = questionLabel });
            radioGroup = questionField.Locator("mat-radio-group");
        }

        // 2. FIX: Resolve the active choice button component by querying active Angular MDC framework check status style definitions
        var checkedRadioButton = radioGroup.Locator("mat-radio-button.mat-mdc-radio-checked, mat-radio-button.mat-radio-checked, mat-radio-button[aria-checked='true']").First;

        // Ensure layout rendering cycles complete before processing metrics
        await checkedRadioButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        // 3. Extract and return the target inner textual status title (e.g., "Unavoidable")
        string selectedText = await checkedRadioButton.InnerTextAsync();

        return selectedText.Trim();
    }

    /// <summary>
    /// Reads and extracts the collection array of active selected chips text currently displayed inside a Kendo or Angular MultiSelect input interface wrapper.
    /// </summary>
    /// <returns>A read-only string list collection containing the active chosen factor criteria titles.</returns>
    public async Task<IReadOnlyList<string>> GetSelectedContributingFactorsAsync()
    {
        // Under standard execution layouts, Kendo MultiSelect components map chosen items using custom chip or tag element blocks
        var chips = Page.Locator("kendo-multiselect .k-chip-label, mat-chip-list mat-chip");
        return await chips.AllInnerTextsAsync();
    }
    /// <summary>
    /// Verifies that all checkboxes, rich text fields, radio options, and multi-select choices inside the Summary tab match the expected incident summary records.
    /// </summary>
    /// <param name="expected">The structural dataset containing the expected summary configuration values for validation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task VerifyDataFieldsAsync(IncidentSummaryInfo expected)
    {
        // 1. Checkboxes at the top
        Assert.That(await IsCheckboxCheckedAsync("Care Plan Updated"), Is.EqualTo(expected.CarePlanUpdated));
        Assert.That(await IsCheckboxCheckedAsync("Set as reportable"), Is.EqualTo(expected.SetAsReportable));
        Assert.That(await IsCheckboxCheckedAsync("Major Injury"), Is.EqualTo(expected.MajorInjury));
        Assert.That(await IsCheckboxCheckedAsync("Send to Legal"), Is.EqualTo(expected.SendToLegal));

        // 2. Rich Text Editors
        if (!string.IsNullOrEmpty(expected.Summary))
        {
            var actualSummary = await GetRichTextValueAsync("summary");
            Assert.That(actualSummary, Does.Contain(expected.Summary));
        }

        if (!string.IsNullOrEmpty(expected.Plan))
        {
            var actualPlan = await GetRichTextValueAsync("summaryPlan");
            Assert.That(actualPlan, Does.Contain(expected.Plan));
        }

        // 3. Conclusion Radio Buttons
        if (!string.IsNullOrEmpty(expected.Conclusion))
        {
            var actualConclusion = await GetSelectedRadioTextAsync("Based upon the collection and review of all attached information, the following conclusion has been reached:");
            Assert.That(actualConclusion, Does.Contain(expected.Conclusion));
        }

        // 4. Evidence of abuse (Radio Buttons + Reason)
        // 3. Verify the state of the Evidence of abuse radio button
        string expectedAbuseRadio = expected.EvidenceOfAbuse ? "Yes" : "No";
        var actualAbuseRadio = await GetSelectedRadioTextAsync("There is probable evidence of abuse, neglect or mistreatment:");
        Assert.That(actualAbuseRadio, Does.Contain(expectedAbuseRadio), "The state of the 'Evidence of abuse' radio button does not match.");

        // 4. Verify the Evidence Reason rich text editor via the SAME universal method
        if (!string.IsNullOrEmpty(expected.EvidenceReason))
        {
            // NOTE: Review debug log
            Log.Debug("[SUMMARY_TAB] Verifying Evidence Reason via universal method...");

            // Pass the name "evidenceReason", the method will handle the extraction logic and await the text
            var actualReasonRichText = await GetRichTextValueAsync("evidenceReason");

            // Strip whitespaces and carriage returns for resilient string comparison evaluation
            string cleanActual = actualReasonRichText.Replace("\r", "").Replace("\n", "").Trim();
            string cleanExpected = expected.EvidenceReason.Replace("\r", "").Replace("\n", "").Trim();

            Assert.That(cleanActual, Does.Contain(cleanExpected.Substring(0, 30)),
                "The text in the Evidence Reason field does not match the expected draft data.");
        }

        // 5. This will be reported to the DOH... (Radio)
        string expectedReportedRadio = expected.ReportedToAgency ? "Yes" : "No";
        var actualReportedRadio = await GetSelectedRadioTextAsync("This will be reported to the DOH, OHMS, or other agency to intervene?");
        Assert.That(actualReportedRadio, Does.Contain(expectedReportedRadio));

        // 6. Possible Contributing Factor (MultiSelect)
        if (expected.PossibleContributingFactor != null && expected.PossibleContributingFactor.Count > 0)
        {
            var actualFactors = await GetSelectedContributingFactorsAsync();
            foreach (var expectedFactor in expected.PossibleContributingFactor)
            {
                Assert.That(actualFactors, Contains.Item(expectedFactor),
                    $"The factor '{expectedFactor}' was not found among the active selected UI chip items.");
            }
        }
    }
}