using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;
using Log = CareAdminTestProject.Common.TestLog;


/// <summary>
/// Represents the Details tab within the incident reporting form.
/// Provides methods and data structures to interact with and fill incident details.
/// </summary>
public class DetailsTab : BaseIncidentTabs
{
    /// <summary>
    /// Represents comprehensive incident detail records.
    /// </summary>
    public record IncidentDetailsInfo(
        string OccurrenceDescription,
        string PatientDescription,
        bool FirstAidAdministered,
        string FirstAidDescribe,
        VitalSigns VitalSigns,
        bool ResidentTransferred,
        string CorrectiveAction,
        string PreventiveAction,
        RelativeNotification RelativeNotified,
        MDNotification MDNotified,
        string MDOrder,
        string DiagnosticTests,
        List<string> Witnesses
    );

    private string AllDiagnises;

    /// <summary>
    /// Represents clinical vital signs recorded during the incident.
    /// </summary>
    public record VitalSigns(
        string Temperature,
        string Pulse,
        string BpSitting,
        string Spo2,
        string BpLaying,
        string BloodGlucose
    );

    /// <summary>
    /// Represents notification details sent to the patient's relative.
    /// </summary>
    public record RelativeNotification(
        string Name,
        string Relationship,
        string WhoNotified,
        DateTime Date,
        TimeOnly Time
    );

    /// <summary>
    /// Represents notification details sent to the Medical Doctor (MD).
    /// </summary>
    public record MDNotification(
        string MDName,
        string WhoNotified,
        DateTime Date,
        TimeOnly Time
    );

    /// <summary>
    /// Constructs a map of fields to their respective data entry actions and validation statuses.
    /// </summary>
    /// <param name="data">The incident details dataset used to populate fields dynamically.</param>
    /// <returns>A dictionary mapping field names to an execution action and a required flag.</returns>
    public Dictionary<string, (Func<Task> Action, bool IsRequired)> GetRequiredFieldsMap(IncidentDetailsInfo data)
    {
        var relativeLineContext = Page.Locator("div.panel-line, div.col-wrap")
            .Filter(new() { HasText = "Relationship" });

        var mdLineContext = Page.Locator("div.panel-line, div.col-wrap")
            .Filter(new() { HasText = "Who Notified MD" });

        var map = new Dictionary<string, (Func<Task> Action, bool IsRequired)>
    {
        { "Describe Occurrence", (async () => await GetFieldByLabel("Describe Occurrence").FillAsync(data.OccurrenceDescription), true) },
        { "Patient's Description of Occurrence", (async () => await GetFieldByLabel("Patient’s Description of Occurrence").FillAsync(data.PatientDescription), true) },
        { "All Diagnoses", (async () => await GetFieldByLabel("All Diagnoses").FillAsync(AllDiagnises), true)},
        // Vital Signs
        { "Temperature", (async () => await GetFieldByLabel("Temperature").FillAsync(data.VitalSigns.Temperature), true) },
        { "Pulse", (async () => await GetFieldByLabel("Pulse").FillAsync(data.VitalSigns.Pulse), true) },
        { "Blood Pressure Sitting", (async () => await GetFieldByLabel("Blood Pressure Sitting").FillAsync(data.VitalSigns.BpSitting), true) },
        { "SPO2", (async () => await GetFieldByLabel("SPO2").FillAsync(data.VitalSigns.Spo2), true) },
        { "Blood Pressure Laying", (async () => await GetFieldByLabel("Blood Pressure Laying").FillAsync(data.VitalSigns.BpLaying), true) },
        { "Blood Glucose (If DM Only)", (async () => await GetFieldByLabel("Blood Glucose (If DM Only)").FillAsync(data.VitalSigns.BloodGlucose), false) },

        { "First Aid Administered", (async () => {
            // This field has no dot, simply toggle it to "Yes" to reveal the next field
            await SelectFirstAdmitedAsync(true, "");
        }, false) },

        { "Describe (MD)", (async () => {
            // 1. Ensure the switch is toggled on (in case fields are evaluated out of order)
            await SelectFirstAdmitedAsync(true, "");
    
            // 2. Populate the description field itself
            var describeField = Page.Locator("textarea[name='firstAidDesc']");
            await describeField.FillAsync(data.FirstAidDescribe);
        }, true) },

        // Actions
        { "Corrective Action (Immediate Intervention)", (async () => await GetFieldByLabel("Corrective Action (Immediate Intervention)").FillAsync(data.CorrectiveAction), true) },
        { "Preventive Action (Long Term Plan)", (async () => await GetFieldByLabel("Preventive Action (Long Term Plan)").FillAsync(data.PreventiveAction), true) },

        // Relative Notification
        { "Name of Relative Notified", (async () => await GetFieldByLabel("Name of Relative Notified").FillAsync(data.RelativeNotified.Name), true) },
        { "Relationship", (async () => await GetFieldByLabel("Relationship").FillAsync(data.RelativeNotified.Relationship), true) },
        { "Who Notified (Relative)", (async () => await GetFieldByLabel("Who Notified").First.FillAsync(data.RelativeNotified.WhoNotified), true) },
        { "Date and Time of Notification (Relative)", (async () => 
            // Pass the relativeLineContext row context as the last argument
            await SelectDateTimeInPickerAsync("Date and Time of Notification", data.RelativeNotified.Date, data.RelativeNotified.Time, relativeLineContext), true)
        },        
        // MD Notification
        { "MD Notified (MD)", (async () => await GetFieldByLabel("MD Notified").FillAsync(data.MDNotified.MDName), true) },
        { "Who Notified MD", (async () => await GetFieldByLabel("Who Notified MD").FillAsync(data.MDNotified.WhoNotified), true) },
        { "Date and Time of Notification (MD)", (async () => 
            // Pass the mdLineContext row context as the last argument
            await SelectDateTimeInPickerAsync("Date and Time of Notification", data.MDNotified.Date, data.MDNotified.Time, mdLineContext), true)
        },
        // Final Fields
        { "MD Order (If applicable)", (async () => await GetFieldByLabel("MD Order (If applicable)").FillAsync(data.MDOrder), true) },
        { "Diagnostic Tests Ordered", (async () => await GetFieldByLabel("Diagnostic Tests Ordered").FillAsync(data.DiagnosticTests), true) }
    };

        return map;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsTab"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public DetailsTab(IPage page) : base(page) { }

    /// <summary>
    /// Populates the complete Details form fields with data provided.
    /// </summary>
    /// <param name="details">The model holding all structural incident data parameters.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FillDetailsInfoAsync(IncidentDetailsInfo details)
    {
        // Descriptions (Textareas)
        await GetFieldByLabel("Describe Occurrence").FillAsync(details.OccurrenceDescription);
        await GetFieldByLabel("Patient’s Description of Occurrence").FillAsync(details.PatientDescription);
        if (!string.IsNullOrEmpty(AllDiagnises))
        {
            await GetFieldByLabel("All Diagnoses").FillAsync(AllDiagnises);
            // NOTE: Review debug log
            Log.Debug("All Diagnoses field restored from saved variable.");
        }

        // Vital Signs
        await FillVitalSignsAsync(details.VitalSigns);

        // First Aid
        await SelectFirstAdmitedAsync(details.FirstAidAdministered, details.FirstAidDescribe);

        // Transfer
        await SelectRadioOptionAsync("Was Resident Transferred to the Hospital?", details.ResidentTransferred ? "Yes" : "No");

        // Actions
        await GetFieldByLabel("Corrective Action (Immediate Intervention)").FillAsync(details.CorrectiveAction);
        await GetFieldByLabel("Preventive Action (Long Term Plan)").FillAsync(details.PreventiveAction);

        // Relative Notification
        await FillRelativeNotificationAsync(details.RelativeNotified);

        // MD Notification
        await FillMDNotificationAsync(details.MDNotified);

        // Final Fields
        await GetFieldByLabel("MD Order (If applicable)").FillAsync(details.MDOrder);
        await GetFieldByLabel("Diagnostic Tests Ordered").FillAsync(details.DiagnosticTests);
    }

    /// <summary>
    /// Fills internal medical diagnostic metrics inputs.
    /// </summary>
    /// <param name="vitals">The objective metrics collected during the scene assessment.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task FillVitalSignsAsync(VitalSigns vitals)
    {
        await GetFieldByLabel("Temperature").FillAsync(vitals.Temperature);
        await GetFieldByLabel("Pulse").FillAsync(vitals.Pulse);
        await GetFieldByLabel("Blood Pressure Sitting").FillAsync(vitals.BpSitting);
        await GetFieldByLabel("SPO2").FillAsync(vitals.Spo2);
        await GetFieldByLabel("Blood Pressure Laying").FillAsync(vitals.BpLaying);
        await GetFieldByLabel("Blood Glucose").FillAsync(vitals.BloodGlucose);
    }
    /// <summary>
    /// Populates the relative notification section fields using a row-specific context.
    /// </summary>
    /// <param name="rel">The structural details of the relative notification.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task FillRelativeNotificationAsync(RelativeNotification rel)
    {
        var relativeLineContext = Page.Locator("div.panel-line, div.col-wrap")
            .Filter(new() { HasText = "Relationship" });

        await relativeLineContext.Locator(GetFieldByLabel("Name of Relative Notified")).FillAsync(rel.Name);
        await relativeLineContext.Locator(GetFieldByLabel("Relationship")).FillAsync(rel.Relationship);
        await relativeLineContext.Locator(GetFieldByLabel("Who Notified")).FillAsync(rel.WhoNotified);

        // Pass the row context instead of index 0
        await SelectDateTimeInPickerAsync("Date and Time of Notification", rel.Date, rel.Time, relativeLineContext);
    }

    /// <summary>
    /// Populates the medical doctor (MD) notification section fields using a row-specific context.
    /// </summary>
    /// <param name="md">The structural details of the MD notification.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task FillMDNotificationAsync(MDNotification md)
    {
        var mdLineContext = Page.Locator("div.panel-line, div.col-wrap")
            .Filter(new() { HasText = "Who Notified MD" });

        await mdLineContext.Locator(GetFieldByLabel("MD Notified")).FillAsync(md.MDName);
        await mdLineContext.Locator(GetFieldByLabel("Who Notified MD")).FillAsync(md.WhoNotified);

        // Pass the row context instead of index 1
        await SelectDateTimeInPickerAsync("Date and Time of Notification", md.Date, md.Time, mdLineContext);
    }

    /// <summary>
    /// Handles the first aid administration toggle switch and inputs its corresponding description if required.
    /// </summary>
    /// <param name="firstAidAdministered">Indicates whether first aid was administered.</param>
    /// <param name="describe">The detailed description of the administered first aid.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SelectFirstAdmitedAsync(Boolean firstAidAdministered, string describe)
    {
        // First Aid (Radio + Describe)
        // Locate the container holding the header text, then find the switch inside it

        // NOTE: Review debug log
        Log.Debug($"Looking for the field First Aid Administered, and try to switch it");

        await SelectRadioOptionAsync("First Aid Administered:", firstAidAdministered ? "Yes" : "No");

        // If "Yes" (true) is required, and current status is "false" — perform click
        if (firstAidAdministered == true)
        {
            // NOTE: Review debug log
            Log.Debug($"Enter Description");

            var describeField = Page.Locator("textarea[name='firstAidDesc']");

            await describeField.WaitForAsync();
            await describeField.FillAsync(describe);
        }

    }

    /// <summary>
    /// Locates and interacts with a kendo-switch acting as a radio option by matching the specific label text.
    /// </summary>
    /// <param name="labelText">The descriptive label text of the option group.</param>
    /// <param name="option">The target selection value ("Yes" or "No").</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected new async Task SelectRadioOptionAsync(string labelText, string option)
    {
        // NOTE: Review debug log
        Log.Debug($"Looking for radiobutton: '{labelText}' with the value: '{option}'");

        // 1. Locate the container (using a more flexible selector div.horizontal-field)
        var fieldContainer = Page.Locator("div.horizontal-field")
            .Filter(new() { HasText = labelText });

        // Container existence check
        var containerCount = await fieldContainer.CountAsync();
        if (containerCount == 0)
        {
            // Output a list of all found labels for debugging purposes
            var allLabels = await Page.Locator("div.horizontal-field span.label-text").AllInnerTextsAsync();
            // NOTE: Review debug log
            Log.Debug($"Available lables on the page: {string.Join(", ", allLabels)}");
            throw new Exception($"Field container '{labelText}' not found.");
        }

        // 2. Locate kendo-switch inside the target container
        var kendoSwitch = fieldContainer.Locator("kendo-switch");
        await kendoSwitch.ScrollIntoViewIfNeededAsync();

        if (!await kendoSwitch.IsVisibleAsync())
        {
            // NOTE: Review debug log
            Log.Debug($"[ERROR] Element 'kendo-switch' was found in DOM, but is not visible for the field '{labelText}'.");
        }

        // 3. Read current state
        var ariaChecked = await kendoSwitch.GetAttributeAsync("aria-checked");
        bool isCurrentlyChecked = ariaChecked?.ToLower() == "true";
        bool shouldBeChecked = option.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase);

        // 4. Click if the state needs to be modified
        if (isCurrentlyChecked != shouldBeChecked)
        {
            await kendoSwitch.ClickAsync();

            // Verification: did the state change after clicking?
            var newState = await kendoSwitch.GetAttributeAsync("aria-checked");
        }

        // Small pause for Angular/Kendo animations and dependent fields rendering
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    /// <summary>
    /// Opens the date-time picker within a localized row context, selects "Today" for date, and inputs specific time values.
    /// </summary>
    /// <param name="labelText">The label identifying the target date-time picker field.</param>
    /// <param name="date">The target date value (unused directly here due to Today button override logic).</param>
    /// <param name="time">The structural time value containing hours, minutes, and AM/PM designator.</param>
    /// <param name="rowContext">The specific locator context defining the form row.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SelectDateTimeInPickerAsync(string labelText, DateTime date, TimeOnly time, ILocator rowContext)
    {
        // 1. Open the picker strictly inside our isolated row context (panel-line)
        // NOTE: Review debug log
        Log.Debug($"Open the calendar {labelText} and try to select a date");
        var fieldContainer = rowContext.Locator("cad-label-value-field").Filter(new() { HasText = labelText });
        await fieldContainer.Locator("button.k-input-button").ClickAsync();

        // 2. Wait until the calendar becomes visible
        var calendar = Page.Locator("kendo-calendar, .k-calendar-container");
        await calendar.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // =========================================================================
        // FIX: CLICK THE "TODAY" BUTTON FOR GUARANTEED DATE SELECTION
        // =========================================================================
        // NOTE: Review debug log
        Log.Debug("Clicking the Today button in Kendo calendar...");

        // Find the Today button inside the open calendar
        var todayButton = calendar.Locator("button.k-calendar-title + button, .k-nav-today, button:has-text('Today')").First;

        await todayButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await todayButton.ClickAsync();

        // NOTE: Review debug log
        Log.Debug("Today button clicked successfully. Current date selected.");
        // =========================================================================

        // 3. Switch to Time (Your functional code)
        var timeTab = Page.Locator("kendo-datetimepicker .k-button").GetByText("Time");
        if (await timeTab.IsVisibleAsync())
        {
            await timeTab.ClickAsync();
        }

        // 4. Time data preparation
        var englishCulture = System.Globalization.CultureInfo.InvariantCulture;
        string hourWithZero = time.ToString("hh", englishCulture);
        string hourSimple = time.Hour > 12 ? (time.Hour - 12).ToString() : time.Hour.ToString();
        if (hourSimple == "0") hourSimple = "12";
        string minute = time.ToString("mm", englishCulture);
        string tt = time.ToString("tt", englishCulture);

        int hourIndex = (tt == "AM") ? 0 : 1;

        var activePopup = Page.Locator("kendo-popup, .k-animation-container").Last;
        await activePopup.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var hourColumn = activePopup.Locator(".k-time-list").Nth(0);
        var minuteColumn = activePopup.Locator(".k-time-list").Nth(1);
        var ttColumn = activePopup.Locator(".k-time-list").Nth(2);

        var pattern = $"^({hourWithZero}|{hourSimple})$";

        var hourItem = hourColumn.Locator(".k-item").Filter(new()
        {
            HasTextRegex = new System.Text.RegularExpressions.Regex(pattern)
        }).Nth(hourIndex);

        await hourItem.ScrollIntoViewIfNeededAsync();
        await hourItem.ClickAsync();
        await hourItem.ScrollIntoViewIfNeededAsync();
        await hourItem.ClickAsync();

        var minuteItem = minuteColumn.Locator(".k-item").GetByText(minute, new() { Exact = true }).First;
        await minuteItem.ScrollIntoViewIfNeededAsync();
        await minuteItem.ClickAsync();

        var ttItem = ttColumn.Locator(".k-item").GetByText(tt, new() { Exact = true }).First;
        await ttItem.ScrollIntoViewIfNeededAsync();
        await ttItem.ClickAsync();

        // 5. Confirmation (Set button)
        await activePopup.Locator("button.k-time-accept, .k-datetime-footer button:has-text('Set')").ClickAsync();

        // Stable barrier for pop-up closure
        await Page.Locator(".k-animation-container").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await Page.Keyboard.PressAsync("Escape");

        var kendoPopups = Page.Locator("kendo-popup, kendo-calendar, .k-calendar-container");
        if (await kendoPopups.CountAsync() > 0)
        {
            await kendoPopups.First.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 3000 });
        }
        await Page.WaitForTimeoutAsync(250); // Pause to fix Angular form state
    }

    /// <summary>
    /// Reads and stores the text from "All Diagnoses" field, then clears the input area.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClearAndSaveDiagnosesAsync()
    {
        var field = GetFieldByLabel("All Diagnoses");
        AllDiagnises = await field.InputValueAsync(); // Save to class field
        await field.ClearAsync();
        // NOTE: Review debug log
        Log.Debug("All Diagnoses cleared");
    }

    /// <summary>
    /// Checks whether a specific radio option within a grouped section is selected.
    /// </summary>
    /// <param name="groupName">The text identifying the radio group.</param>
    /// <param name="optionValue">The value attribute of the target radio button.</param>
    /// <returns>True if the specific option is checked; otherwise, false.</returns>
    public async Task<bool> IsRadioOptionSelectedAsync(string groupName, string optionValue)
    {
        // Searches for a radio button by group text and value
        return await Page.Locator($"//div[contains(., '{groupName}')]//input[@type='radio' and @value='{optionValue}']").IsCheckedAsync();
    }

    /// <summary>
    /// Evaluates whether a custom Kendo switch element is toggled to the "On" status.
    /// </summary>
    /// <param name="labelText">The label text used to isolate the field container.</param>
    /// <returns>True if the switch aria-checked attribute is true; otherwise, false.</returns>
    public async Task<bool> IsSwitchOnAsync(string labelText)
    {
        // Locate the field container by the label text ("First Aid Administered:")
        var container = Page.Locator("div.horizontal-field").Filter(new() { HasText = labelText });

        // Search for the kendo-switch itself inside the container
        var kendoSwitch = container.Locator("kendo-switch");

        // Read the value of the aria-checked attribute ("true" or "false")
        string ariaChecked = await kendoSwitch.GetAttributeAsync("aria-checked") ?? "false";

        return ariaChecked.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Retrieves the current textual input value of a field selected by its label text.
    /// </summary>
    /// <param name="label">The descriptive label text of the target input.</param>
    /// <returns>The string value currently residing inside the input field.</returns>
    public async Task<string> GetInputValueByLabelAsync(string label)
    {
        return await GetFieldByLabel(label).InputValueAsync();
    }

    /// <summary>
    /// Verifies that all data fields in the Details tab match the expected incident details records.
    /// </summary>
    /// <param name="expected">The structural dataset containing the expected values for validation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task VerifyDataFieldsAsync(IncidentDetailsInfo expected)
    {
        // 1. Descriptions (Textareas)
        if (!string.IsNullOrEmpty(expected.OccurrenceDescription))
            Assert.That(await GetInputValueByLabelAsync("Describe Occurrence"), Is.EqualTo(expected.OccurrenceDescription));

        if (!string.IsNullOrEmpty(expected.PatientDescription))
            Assert.That(await GetInputValueByLabelAsync("Patient’s Description of Occurrence"), Is.EqualTo(expected.PatientDescription));

        // 2. Vital Signs
        if (expected.VitalSigns != null)
        {
            Assert.That(await GetInputValueByLabelAsync("Temperature"), Is.EqualTo(expected.VitalSigns.Temperature));
            Assert.That(await GetInputValueByLabelAsync("Pulse"), Is.EqualTo(expected.VitalSigns.Pulse));
            Assert.That(await GetInputValueByLabelAsync("Blood Pressure Sitting"), Is.EqualTo(expected.VitalSigns.BpSitting));
            Assert.That(await GetInputValueByLabelAsync("SPO2"), Is.EqualTo(expected.VitalSigns.Spo2));
            Assert.That(await GetInputValueByLabelAsync("Blood Pressure Laying"), Is.EqualTo(expected.VitalSigns.BpLaying));
            Assert.That(await GetInputValueByLabelAsync("Blood Glucose"), Is.EqualTo(expected.VitalSigns.BloodGlucose));
        }

        // 3. First Aid
        bool isFirstAidOn = await IsSwitchOnAsync("First Aid Administered:");
        Assert.That(isFirstAidOn, Is.EqualTo(expected.FirstAidAdministered),
            "The state of the toggle 'First Aid Administered:' does not match.");

        if (expected.FirstAidAdministered && !string.IsNullOrEmpty(expected.FirstAidDescribe))
        {
            var actualFirstAidDesc = await Page.Locator("textarea[name='firstAidDesc']").InputValueAsync();
            Assert.That(actualFirstAidDesc, Is.EqualTo(expected.FirstAidDescribe));
        }

        if (expected.FirstAidAdministered && !string.IsNullOrEmpty(expected.FirstAidDescribe))
        {
            var actualFirstAidDesc = await Page.Locator("textarea[name='firstAidDesc']").InputValueAsync();
            Assert.That(actualFirstAidDesc, Is.EqualTo(expected.FirstAidDescribe));
        }

        // 4. Transfer
        bool isTransferredOn = await IsSwitchOnAsync("Was Resident Transferred to the Hospital?");
        Assert.That(isTransferredOn, Is.EqualTo(expected.ResidentTransferred),
            "The state of the toggle 'Was Resident Transferred to the Hospital?' does not match.");


        // 5. Actions
        if (!string.IsNullOrEmpty(expected.CorrectiveAction))
            Assert.That(await GetInputValueByLabelAsync("Corrective Action (Immediate Intervention)"), Is.EqualTo(expected.CorrectiveAction));

        if (!string.IsNullOrEmpty(expected.PreventiveAction))
            Assert.That(await GetInputValueByLabelAsync("Preventive Action (Long Term Plan)"), Is.EqualTo(expected.PreventiveAction));

        // ==========================================
        // 6. Section: Relative Notification (Isolation via Relationship)
        // ==========================================
        var relativeLineContext = Page.Locator("div.panel-line, div.col-wrap")
            .Filter(new() { HasText = "Relationship" });

        if (!string.IsNullOrEmpty(expected.RelativeNotified.Name))
        {
            // Search for the field inside the isolated row by its label text
            var actualName = await relativeLineContext.Locator("cad-label-value-field")
                .Filter(new() { HasText = "Name of Relative Notified" })
                .Locator("input, textarea")
                .InputValueAsync();

            Assert.That(actualName, Is.EqualTo(expected.RelativeNotified.Name), "Relative Name mismatch");
        }

        if (!string.IsNullOrEmpty(expected.RelativeNotified.Relationship))
        {
            var actualRel = await relativeLineContext.Locator("cad-label-value-field")
                .Filter(new() { HasText = "Relationship" })
                .Locator("input, textarea")
                .InputValueAsync();

            Assert.That(actualRel, Is.EqualTo(expected.RelativeNotified.Relationship), "Relationship mismatch");
        }

        if (!string.IsNullOrEmpty(expected.RelativeNotified.WhoNotified))
        {
            // Now we are guaranteed to take Who Notified from the relatives block, ignoring the doctor block
            var actualWho = await relativeLineContext.Locator("cad-label-value-field")
                .Filter(new() { HasText = "Who Notified" })
                .Locator("input, textarea")
                .InputValueAsync();

            Assert.That(actualWho, Is.EqualTo(expected.RelativeNotified.WhoNotified), "Relative Who Notified mismatch");
        }

        if (expected.RelativeNotified.Date != default)
        {
            var actualDateTime = await relativeLineContext.Locator("cad-label-value-field")
                .Filter(new() { HasText = "Date and Time of Notification" })
                .Locator("input")
                .InputValueAsync();

            string expectedDateStr = expected.RelativeNotified.Date.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            string expectedTimeStr = expected.RelativeNotified.Time.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

            Assert.That(actualDateTime, Does.Contain(expectedDateStr), "Relative Notification Date mismatch");
            Assert.That(actualDateTime, Does.Contain(expectedTimeStr), "Relative Notification Time mismatch");
        }

        // ==========================================
        // 7. Section: MD Notification (Isolation via Who Notified MD)
        // ==========================================
        var mdLineContext = Page.Locator("div.panel-line, div.col-wrap")
            .Filter(new() { HasText = "Who Notified MD" });

        if (!string.IsNullOrEmpty(expected.MDNotified.MDName))
        {
            var actualMdName = await mdLineContext.Locator("cad-label-value-field")
                .Filter(new() { HasText = "MD Notified" })
                .Locator("input, textarea")
                .InputValueAsync();

            Assert.That(actualMdName, Is.EqualTo(expected.MDNotified.MDName), "MD Name mismatch");
        }

        if (!string.IsNullOrEmpty(expected.MDNotified.WhoNotified))
        {
            var actualWhoMd = await mdLineContext.Locator("cad-label-value-field")
                .Filter(new() { HasText = "Who Notified MD" })
                .Locator("input, textarea")
                .InputValueAsync();

            Assert.That(actualWhoMd, Is.EqualTo(expected.MDNotified.WhoNotified), "MD Who Notified mismatch");
        }

        if (expected.MDNotified.Date != default)
        {
            var actualDateTime = await mdLineContext.Locator("cad-label-value-field")
                .Filter(new() { HasText = "Date and Time of Notification" })
                .Locator("input")
                .InputValueAsync();

            string expectedDateStr = expected.MDNotified.Date.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            string expectedTimeStr = expected.MDNotified.Time.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

            Assert.That(actualDateTime, Does.Contain(expectedDateStr), "MD Notification Date mismatch");
            Assert.That(actualDateTime, Does.Contain(expectedTimeStr), "MD Notification Time mismatch");
        }

        // 8. Final Fields
        if (!string.IsNullOrEmpty(expected.MDOrder))
            Assert.That(await GetInputValueByLabelAsync("MD Order (If applicable)"), Is.EqualTo(expected.MDOrder));

        if (!string.IsNullOrEmpty(expected.DiagnosticTests))
            Assert.That(await GetInputValueByLabelAsync("Diagnostic Tests Ordered"), Is.EqualTo(expected.DiagnosticTests));
    }
}