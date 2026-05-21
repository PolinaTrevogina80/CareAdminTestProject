using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Log = CareAdminTestProject.Common.TestLog;

/// <summary>
/// Represents the State tab within the incident reporting form.
/// Provides data records and mappings to interact with a patient's status configurations.
/// </summary>
public class StateTab : BaseIncidentTabs
{
    /// <summary>
    /// Represents full patient status information during the incident assessment.
    /// </summary>
    public record IncidentStateInfo(
        CommunicationStatus Communication,
        string AmbulatoryStatus, // Independent, Ambulatory, Non Ambulatory
        string NeedsAssistanceOf,
        bool UsesLift,
        bool NeedsSupervision,
        bool Restrained,
        string TypeOfRestraint,
        bool SideRail,
        bool NotInvolved,
        BowelBladderStatus BowelBladder,
        AlarmsStatus Alarms,
        string OtherAlarmDetails,
        AssistiveDevices Devices
    );

    /// <summary>
    /// Represents the communication and cognitive status of the patient.
    /// </summary>
    public record CommunicationStatus(
        bool Oriented, bool Person, bool Time, bool Place,
        bool Alert, bool Confused, bool Forgetful, bool Uncooperative,
        bool NonCompliant, bool Agitated, bool NonVerbal, bool BlindDeaf, bool LanguageBarrier
    );

    /// <summary>
    /// Represents the bowel and bladder continental metrics of the patient.
    /// </summary>
    public record BowelBladderStatus(
        bool Foley, bool Colostomy, bool Continent, bool Incontinent, bool Bowel, bool Bladder
    );

    /// <summary>
    /// Represents safety alarms structural setup configured for the patient.
    /// </summary>
    public record AlarmsStatus(
        bool NoAlarmOrder, bool BedAlarm, bool ChairAlarm, bool PinAlarm, bool OtherType
    );

    /// <summary>
    /// Represents technical assistive physical devices utilized by the patient.
    /// </summary>
    public record AssistiveDevices(
        string Wheelchair, string WalkerCrutch, string Cane, string HearingAid, string Glasses
    );

    /// <summary>
    /// Builds a technical action map for validating state requirements, checkbox triggers, and cleanups.
    /// </summary>
    /// <returns>A dictionary containing target fields, execution mappings, reset handlers, and validation flags.</returns>
    public Dictionary<string, (Func<Task> Action, Func<Task> Reset, bool IsRequired)> GetStateRequiredFieldsMap()
    {
        return new Dictionary<string, (Func<Task> Action, Func<Task> Reset, bool IsRequired)>
        {
            // --- Communication/Mood Status (Checkboxes) ---
            { "Oriented", (
            () => SetCheckboxAsync("Oriented",true),
            () => SetCheckboxAsync("Oriented",false),
            false)
            },
            { "Person", (
            () => SetCheckboxAsync("Person",true),
            () => SetCheckboxAsync("Person",false),
            false)
            },
            { "Time", (
            () => SetCheckboxAsync("Time",true),
            () => SetCheckboxAsync("Time",false),
            false)
            },
            { "Place", (
            () => SetCheckboxAsync("Place",true),
            () => SetCheckboxAsync("Place",false),
            false)
            },
            { "Alert", (
            () => SetCheckboxAsync("Alert",true),
            () => SetCheckboxAsync("Alert",false),
            false)
            },
            { "Confused", (
            () => SetCheckboxAsync("Confused",true),
            () => SetCheckboxAsync("Confused",false),
            false)
            },
            { "Forgetful", (
            () => SetCheckboxAsync("Forgetful",true),
            () => SetCheckboxAsync("Forgetful",false),
            false)
            },
            { "Uncooperative", (
            () => SetCheckboxAsync("Uncooperative",true),
            () => SetCheckboxAsync("Uncooperative",false),
            false)
            },
            { "NonCompliant", (
            () => SetCheckboxAsync("Non-compliant with plan of care",true),
            () => SetCheckboxAsync("Non-compliant with plan of care",false),
            false)
            },
            { "Agitated", (
            () => SetCheckboxAsync("Agitated",true),
            () => SetCheckboxAsync("Agitated",false),
            false)
            },
            { "NonVerbal", (
            () => SetCheckboxAsync("Non-verbal",true),
            () => SetCheckboxAsync("Non-verbal",false),
            false)
            },
            { "BlindDeaf", (
            () => SetCheckboxAsync("Blind/Deaf",true),
            () => SetCheckboxAsync("Blind/Deaf",false),
            false)
            },
            { "LanguageBarrier", (
            () => SetCheckboxAsync("Language barrier",true),
            () => SetCheckboxAsync("Language barrier",false),
            false)
            },

            //// --- Ambulatory Transfer Status (Radio Buttons) ---
            //{ "AmbulatoryTransferStatus", (
            //async () => await SelectRadioOptionAsync("Ambulatory Transfer Status", "Independent"),
            //async () => await ClearRadioOptionAsync("Ambulatory Transfer Status"),
            //false)
            //},

            // --- Bowel and Bladder Status (Checkboxes) ---
            { "Foley", (
            () => SetCheckboxAsync("Foley",true),
            () => SetCheckboxAsync("Foley",false),
            false)
            },
            { "ColostomyIleostomy", (
            () => SetCheckboxAsync("Colostomy/Ileostomy",true),
            () => SetCheckboxAsync("Colostomy/Ileostomy",false),
            false)
            },
            { "Continent", (
            () => SetCheckboxAsync("Continent",true),
            () => SetCheckboxAsync("Continent",false),
            false)
            },
            { "Incontinent", (
            () => SetCheckboxAsync("Incontinent",true),
            () => SetCheckboxAsync("Incontinent",false),
            false)
            },
            { "Bowel", (
            () => SetCheckboxAsync("Bowel",true),
            () => SetCheckboxAsync("Bowel",false),
            false)
            },
            { "Bladder", (
            () => SetCheckboxAsync("Bladder",true),
            () => SetCheckboxAsync("Bladder",false),
            false)
            },


            // --- Alarms (Checkboxes) ---
            { "Bed Alarm", (
            () => SetCheckboxAsync("Bed Alarm",true),
            () => SetCheckboxAsync("Bed Alarm",false),
            false)
            },

            // --- Assistive Device (Checkbox + Radio) ---
            { "Wheelchair", (
                () => SetCheckboxAsync("Wheelchair",true),
                () => SetCheckboxAsync("Wheelchair",false),
            false)
            },
            { "WalkerCrutch", (
                () => SetCheckboxAsync("Walker/Crutch", true),
                () => SetCheckboxAsync("Walker/Crutch", false),
                false)
            },
            { "Cane", (
                () => SetCheckboxAsync("Cane", true),
                () => SetCheckboxAsync("Cane", false),
                false)
            },
            { "HearingAid", (
                () => SetCheckboxAsync("Hearing Aid", true),
                () => SetCheckboxAsync("Hearing Aid", false),
                false)
            },
            { "Glasses", (
                () => SetCheckboxAsync("Glasses", true),
                () => SetCheckboxAsync("Glasses", false),
                false)
            }
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateTab"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public StateTab(IPage page) : base(page) { }

    /// <summary>
    /// Checks whether the general state completeness indicator is visible on the page.
    /// </summary>
    /// <returns>True if the completeness indicator is visible; otherwise, false.</returns>
    public async Task<bool> IsGeneralStatePointVisibleAsync()
    {
        return await GeneralPointLocator.IsVisibleAsync();
    }

    /// <summary>
    /// Fills the entire State form tab using the provided state configuration information.
    /// </summary>
    /// <param name="info">The dataset containing comprehensive information about the patient's physical and mental state.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FillStateTabAsync(IncidentStateInfo info)
    {
        await Task.Delay(500);

        // Communication/Mood Status
        await SetCheckboxAsync("Oriented", info.Communication.Oriented);
        await SetCheckboxAsync("Person", info.Communication.Person);
        await SetCheckboxAsync("Time", info.Communication.Time);
        await SetCheckboxAsync("Place", info.Communication.Place);
        await SetCheckboxAsync("Alert", info.Communication.Alert);
        await SetCheckboxAsync("Confused", info.Communication.Confused);
        await SetCheckboxAsync("Forgetful", info.Communication.Forgetful);
        await SetCheckboxAsync("Uncooperative", info.Communication.Uncooperative);
        await SetCheckboxAsync("Non-compliant with plan of care", info.Communication.NonCompliant);
        await SetCheckboxAsync("Agitated", info.Communication.Agitated);
        await SetCheckboxAsync("Non-verbal", info.Communication.NonVerbal);
        await SetCheckboxAsync("Blind/Deaf", info.Communication.BlindDeaf);
        await SetCheckboxAsync("Language barrier", info.Communication.LanguageBarrier);

        // Ambulatory Transfer Status
        await SelectRadioOptionAsync("Ambulatory Transfer Status", info.AmbulatoryStatus);
        await GetFieldByLabel("Needs assistance of").FillAsync(info.NeedsAssistanceOf);
        await SetCheckboxAsync("Uses a lift", info.UsesLift);
        await SetCheckboxAsync("Needs supervision", info.NeedsSupervision);
        await SetCheckboxAsync("Restrained", info.Restrained);
        await GetFieldByLabel("Type of restraint:").FillAsync(info.TypeOfRestraint);
        await SetCheckboxAsync("Side rail", info.SideRail);
        await SetCheckboxAsync("Not involved", info.NotInvolved);

        // Bowel and Bladder Status
        await SetCheckboxAsync("Foley", info.BowelBladder.Foley);
        await SetCheckboxAsync("Colostomy/Ileostomy", info.BowelBladder.Colostomy);
        await SetCheckboxAsync("Continent", info.BowelBladder.Continent);
        await SetCheckboxAsync("Incontinent", info.BowelBladder.Incontinent);
        await SetCheckboxAsync("Bowel", info.BowelBladder.Bowel);
        await SetCheckboxAsync("Bladder", info.BowelBladder.Bladder);

        // Alarms
        await SetCheckboxAsync("Patient does NOT have Alarm Order", info.Alarms.NoAlarmOrder);
        await SetCheckboxAsync("Bed Alarm", info.Alarms.BedAlarm);
        await SetCheckboxAsync("Chair Alarm", info.Alarms.ChairAlarm);
        await SetCheckboxAsync("Pin Alarm", info.Alarms.PinAlarm);
        await SetCheckboxAsync("Other Type of Alarm", info.Alarms.OtherType);
        if (info.Alarms.OtherType)
            await Page.Locator("textarea").Last.FillAsync(info.OtherAlarmDetails); // Field without an explicit label underneath 'Other'

        // Assistive Device (Used / Not Used)
        await SetAssistiveDeviceAsync("Wheelchair", info.Devices.Wheelchair);
        await SetAssistiveDeviceAsync("Walker/Crutch", info.Devices.WalkerCrutch);
        await SetAssistiveDeviceAsync("Cane", info.Devices.Cane);
        await SetAssistiveDeviceAsync("Hearing Aid", info.Devices.HearingAid);
        await SetAssistiveDeviceAsync("Glasses", info.Devices.Glasses);
    }

    /// <summary>
    /// Gets the locator pointing to the visible completeness indicator.
    /// </summary>
    public ILocator GeneralPointLocator => Page.Locator(".completeness-message .completeness-indicator:visible");


    /// <summary>
    /// Toggles a specific checkbox element to the desired state if it differs from the current state.
    /// </summary>
    /// <param name="label">The exact inner text of the checkbox element to look for.</param>
    /// <param name="isChecked">The targeted boolean checkbox status.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SetCheckboxAsync(string label, bool isChecked)
    {
        // Search for the container using an exact match of the text inside the block
        var checkboxFieldContainer = Page.Locator(".checkbox-field:visible")
            .Filter(new() { Has = Page.Locator("span").GetByText(label, new() { Exact = true }) });

        // Navigate inside this container to the native input element to check its current status
        var nativeInput = checkboxFieldContainer.Locator("input[type='checkbox']");

        // Navigate to the mat-checkbox component to perform the click
        var matCheckbox = checkboxFieldContainer.Locator("mat-checkbox");

        // Read the current state of the native input element
        bool currentState = await nativeInput.IsCheckedAsync();

        if (currentState != isChecked)
        {
            // Click the mat-checkbox inside the identified container
            await matCheckbox.ClickAsync();
        }
    }


    /// <summary>
    /// Resolves an interactive input element by looking up its associated exact span label text.
    /// </summary>
    /// <param name="labelText">The unique label identifier string text.</param>
    /// <returns>An ILocator pointing to input, textarea, or mat-select fields.</returns>
    protected new ILocator GetFieldByLabel(string labelText)
    {
        // Here we look for the .input-field container,
        // which contains a span with the class .input-field__label and the matching text
        return Page.Locator("div.input-field")
                    .Filter(new() { Has = Page.Locator("span.input-field__label").GetByText(labelText, new() { Exact = true }) })
                    .Locator("input, textarea, mat-select");
    }

    /// <summary>
    /// Enables an assistive device checkbox and selects its activation option ("Used" or "Not Used").
    /// </summary>
    /// <param name="deviceLabel">The descriptive label identifying the targeted device type.</param>
    /// <param name="status">The operational selection value string ("Used" or "Not Used").</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SetAssistiveDeviceAsync(string deviceLabel, string status) // status: "Used" or "Not Used"
    {
        if (string.IsNullOrEmpty(status)) return;
        await SetCheckboxAsync(deviceLabel, true);


        // 1. Locate the specific row (container) where the device text resides (for example, "Wheelchair")
        var row = Page.Locator("div.checkbox-field")
            .Filter(new() { Has = Page.Locator("span.checkbox-field__label").GetByText(deviceLabel, new() { Exact = true }) });

        // 2. Search for the radio button inside this row whose text strictly matches the target status
        var radioButton = row.Locator("mat-radio-button")
            .GetByText(status, new() { Exact = true });

        // Click the radio button
        await radioButton.ClickAsync();
    }

    /// <summary>
    /// Evaluates if an assistive device checkbox is enabled and its associated radio selection matches the expected configuration.
    /// </summary>
    /// <param name="deviceLabel">The targeted device field descriptive label.</param>
    /// <param name="expectedStatus">The expected selection value configuration ("Used" or "Not Used").</param>
    /// <returns>True if the matching option configuration is verified as selected; otherwise, false.</returns>
    public async Task<bool> IsAssistiveDeviceSetAsync(string deviceLabel, string expectedStatus)
    {
        // If the status is not provided or empty in data, the device is not verified
        if (string.IsNullOrEmpty(expectedStatus))
        {
            return true;
        }

        // 1. Locate the entire row container for the device (for example, "Wheelchair")
        var deviceRowContainer = Page.Locator("div.checkbox-field")
            .Filter(new() { HasTextRegex = new Regex($"{deviceLabel}", RegexOptions.IgnoreCase) })
            .First;

        await deviceRowContainer.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        // 2. Verify that the main checkbox is checked (since they are all ticked)
        var checkboxInput = deviceRowContainer.Locator("mat-checkbox input");
        bool isCheckboxChecked = await checkboxInput.IsCheckedAsync();
        Assert.That(isCheckboxChecked, Is.True, $"The checkbox for the device '{deviceLabel}' must be selected.");

        // 3. Locate the radio button whose text strictly matches the expected status ("Used" or "Not Used")
        var radioButton = deviceRowContainer.Locator("mat-radio-button, mat-mdc-radio-button")
            .Filter(new() { HasTextRegex = new Regex($"{expectedStatus.Trim()}", RegexOptions.IgnoreCase) })
            .First;

        // 4. Read its activity attributes
        string classAttribute = await radioButton.GetAttributeAsync("class") ?? "";
        string ariaChecked = await radioButton.GetAttributeAsync("aria-checked") ?? "false";

        bool isRadioSelected = classAttribute.Contains("mat-mdc-radio-checked")
                              || classAttribute.Contains("mat-radio-checked")
                              || ariaChecked.Equals("true", StringComparison.OrdinalIgnoreCase);

        return isRadioSelected;
    }

    /// <summary>
    /// Verifies that all status checkboxes, radio options, and input values in the State tab match the expected incident state records.
    /// </summary>
    /// <param name="expected">The structural dataset containing the expected status configuration values for validation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task VerifyDataFieldsAsync(IncidentStateInfo expected)
    {

        await Task.Delay(500); // UI synchronization, same as in the filling method

        // 1. Communication/Mood Status (Checkboxes)
        Assert.That(await IsCheckboxCheckedAsync("Oriented"), Is.EqualTo(expected.Communication.Oriented));
        Assert.That(await IsCheckboxCheckedAsync("Person"), Is.EqualTo(expected.Communication.Person));
        Assert.That(await IsCheckboxCheckedAsync("Time"), Is.EqualTo(expected.Communication.Time));
        Assert.That(await IsCheckboxCheckedAsync("Place"), Is.EqualTo(expected.Communication.Place));
        Assert.That(await IsCheckboxCheckedAsync("Alert"), Is.EqualTo(expected.Communication.Alert));
        Assert.That(await IsCheckboxCheckedAsync("Confused"), Is.EqualTo(expected.Communication.Confused));
        Assert.That(await IsCheckboxCheckedAsync("Forgetful"), Is.EqualTo(expected.Communication.Forgetful));
        Assert.That(await IsCheckboxCheckedAsync("Uncooperative"), Is.EqualTo(expected.Communication.Uncooperative));
        Assert.That(await IsCheckboxCheckedAsync("Non-compliant with plan of care"), Is.EqualTo(expected.Communication.NonCompliant));
        Assert.That(await IsCheckboxCheckedAsync("Agitated"), Is.EqualTo(expected.Communication.Agitated));
        Assert.That(await IsCheckboxCheckedAsync("Non-verbal"), Is.EqualTo(expected.Communication.NonVerbal));
        Assert.That(await IsCheckboxCheckedAsync("Blind/Deaf"), Is.EqualTo(expected.Communication.BlindDeaf));
        Assert.That(await IsCheckboxCheckedAsync("Language barrier"), Is.EqualTo(expected.Communication.LanguageBarrier));

        // 2. Ambulatory Transfer Status
        if (!string.IsNullOrEmpty(expected.AmbulatoryStatus))
        {
            // Group name is passed as in the code, or kept as old (the method will filter by text)
            Assert.That(await IsRadioOptionSelectedAsync("ambulatoryStatus", expected.AmbulatoryStatus), Is.True,
                $"Radio option '{expected.AmbulatoryStatus}' must be selected.");
        }

        if (!string.IsNullOrEmpty(expected.NeedsAssistanceOf))
        {
            var actualAssistance = await GetFieldByLabel("Needs assistance of").InputValueAsync();
            Assert.That(actualAssistance, Is.EqualTo(expected.NeedsAssistanceOf));
        }

        Assert.That(await IsCheckboxCheckedAsync("Uses a lift"), Is.EqualTo(expected.UsesLift));
        Assert.That(await IsCheckboxCheckedAsync("Needs supervision"), Is.EqualTo(expected.NeedsSupervision));
        Assert.That(await IsCheckboxCheckedAsync("Restrained"), Is.EqualTo(expected.Restrained));

        if (!string.IsNullOrEmpty(expected.TypeOfRestraint))
        {
            var actualRestraintType = await GetFieldByLabel("Type of restraint:").InputValueAsync();
            Assert.That(actualRestraintType, Is.EqualTo(expected.TypeOfRestraint));
        }

        Assert.That(await IsCheckboxCheckedAsync("Side rail"), Is.EqualTo(expected.SideRail));
        Assert.That(await IsCheckboxCheckedAsync("Not involved"), Is.EqualTo(expected.NotInvolved));

        // 3. Bowel and Bladder Status
        Assert.That(await IsCheckboxCheckedAsync("Foley"), Is.EqualTo(expected.BowelBladder.Foley));
        Assert.That(await IsCheckboxCheckedAsync("Colostomy/Ileostomy"), Is.EqualTo(expected.BowelBladder.Colostomy));
        Assert.That(await IsCheckboxCheckedAsync("Continent"), Is.EqualTo(expected.BowelBladder.Continent));
        Assert.That(await IsCheckboxCheckedAsync("Incontinent"), Is.EqualTo(expected.BowelBladder.Incontinent));
        Assert.That(await IsCheckboxCheckedAsync("Bowel"), Is.EqualTo(expected.BowelBladder.Bowel));
        Assert.That(await IsCheckboxCheckedAsync("Bladder"), Is.EqualTo(expected.BowelBladder.Bladder));

        // 4. Alarms
        Assert.That(await IsCheckboxCheckedAsync("Patient does NOT have Alarm Order"), Is.EqualTo(expected.Alarms.NoAlarmOrder));
        Assert.That(await IsCheckboxCheckedAsync("Bed Alarm"), Is.EqualTo(expected.Alarms.BedAlarm));
        Assert.That(await IsCheckboxCheckedAsync("Chair Alarm"), Is.EqualTo(expected.Alarms.ChairAlarm));
        Assert.That(await IsCheckboxCheckedAsync("Pin Alarm"), Is.EqualTo(expected.Alarms.PinAlarm));
        Assert.That(await IsCheckboxCheckedAsync("Other Type of Alarm"), Is.EqualTo(expected.Alarms.OtherType));

        if (expected.Alarms.OtherType && !string.IsNullOrEmpty(expected.OtherAlarmDetails))
        {
            var actualAlarmDetails = await Page.Locator("textarea").Last.InputValueAsync();
            Assert.That(actualAlarmDetails, Is.EqualTo(expected.OtherAlarmDetails));
        }

        // 5. Assistive Device 
        Assert.That(await IsAssistiveDeviceSetAsync("Wheelchair", expected.Devices.Wheelchair), Is.True, "Wheelchair status mismatch");
        Assert.That(await IsAssistiveDeviceSetAsync("Walker/Crutch", expected.Devices.WalkerCrutch), Is.True, "Walker/Crutch status mismatch");
        Assert.That(await IsAssistiveDeviceSetAsync("Cane", expected.Devices.Cane), Is.True, "Cane status mismatch");
        Assert.That(await IsAssistiveDeviceSetAsync("Hearing Aid", expected.Devices.HearingAid), Is.True, "Hearing Aid status mismatch");
        Assert.That(await IsAssistiveDeviceSetAsync("Glasses", expected.Devices.Glasses), Is.True, "Glasses status mismatch");


    }

}