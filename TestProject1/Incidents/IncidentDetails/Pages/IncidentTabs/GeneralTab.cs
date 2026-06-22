using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using static System.Net.Mime.MediaTypeNames;
using Log = CareAdminTestProject.Common.TestLog;

/// <summary>
/// Represents the "General" information tab page object within the incident creation or editing workflow.
/// Encapsulates all UI interaction patterns, form filling operations, and field validation data maps.
/// <para><b>--- METHOD DIRECTORY & QUICK LINKS ---</b></para>
/// <list type="bullet">
/// <item><description> Wipes non-essential fields for indicator checks: <see cref="IncidentGeneralInfo.GetOnlyRequiredFields()"/> </description></item>
/// <item><description> Constructs a form boundary interactive map grid: <see cref="GetRequiredFieldsMap(IncidentGeneralInfo)"/></description></item>
/// <item><description> Executes sequential UI insertions to fill basic form data: <see cref="FillBasicInfoAsync(IncidentGeneralInfo)"/></description></item>
/// <item><description> Populates sequential dynamic injury records by row index: <see cref="AddInjuryAsync(int, InjuryInfo)"/></description></item>
///     <item> <description> Control Icon Scroller & Action Tracker: <see cref="ClickControlIcon(string)"/> </description> </item>
/// <item><description> Locates and selects the current date in Kendo UI calendars: <see cref="SelectTodayAsync(string)"/></description></item>
/// <item><description> Traverses Angular Material date pickers to select specific dates: <see cref="SelectDateInCalendarAsync(string, DateTime)"/></description></item>
/// <item><description> Flushes room and bed control inputs for form validation: <see cref="ClearPreFilledFieldsAsync()"/></description></item>
/// <item><description> Runs UI assertions to match physical values against source data: <see cref="VerifyDataFieldsAsync(IncidentGeneralInfo)"/></description></item>
/// </list>
/// </summary>
public class GeneralTab : BaseIncidentTabs
{
    /// <summary>
    /// Models the comprehensive dataset structure representing all configurable fields on the General incident tab.
    /// </summary>
    /// <param name="room">The target resident's room identifier slot.</param>
    /// <param name="bed">The target resident's bed assignment slot.</param>
    /// <param name="date">The calendar timestamp tracking when the incident occurred.</param>
    /// <param name="time">The daily time tracker logging the moment of the event.</param>
    /// <param name="unit">The corporate facility or medical unit context.</param>
    /// <param name="location">The concrete physical environment dropdown value where the incident took place.</param>
    /// <param name="type">The primary categorization type classification of the incident.</param>
    /// <param name="activity">The resident behavior tracking field immediately preceding the event.</param>
    /// <param name="summary">The descriptive text matching SBAR summary protocols.</param>
    /// <param name="supervisor">The entity option position indexing the targeted Supervisor node.</param>
    /// <param name="chargeNurse">The entity option position indexing the targeted Charge Nurse node.</param>
    /// <param name="cna">The entity option position indexing the targeted CNA node.</param>
    /// <param name="injury">The collection array parsing dynamic specific structural injury metrics.</param>
    /// <param name="LoadedSupervisorName">The contextual runtime string extracted straight from the UI element post-selection.</param>
    /// <param name="LoadedChargeNurseName">The contextual runtime string extracted straight from the UI element post-selection.</param>
    /// <param name="LoadedCnaName">The contextual runtime string extracted straight from the UI element post-selection.</param>
    public record IncidentGeneralInfo(
            string? room,
            string? bed,
            DateTime? date,
            TimeOnly? time,
            string? unit,
            string? location,
            string? type,
            string? activity,
            string? summary,
            int? supervisor,
            int? chargeNurse,
            int? cna,
            List<InjuryInfo> injury,

            // Temporary fields using for tests
            string LoadedSupervisorName = "",
            string LoadedChargeNurseName = "",
            string LoadedCnaName = ""
        )
    {
        /// <summary>
        /// Generates a modified shallow copy of the current entity record with all optional fields explicitly wiped.
        /// Used mainly during mandatory requirement indicators checks.
        /// </summary>
        /// <returns>A modified record state where non-essential data elements are reduced to default empty bounds.</returns>
        public IncidentGeneralInfo GetOnlyRequiredFields()
        {
            return this with
            {
                activity = "",       // Нет звездочки
                summary = "",        // Нет звездочки
                supervisor = null,      // Нет звездочки
                chargeNurse = null,     // Нет звездочки
                cna = null              // Нет звездочки
            };

        }
    };

    /// <summary>
    /// Stores precise positional tracking parameters detailing properties for a distinct dynamic grid injury node.
    /// </summary>
    /// <param name="injury">The dynamic textual definition mapping the damage event category.</param>
    /// <param name="site">The anatomical designation locating the area impacted.</param>
    /// <param name="length">The physical length dimension property parameter.</param>
    /// <param name="width">The physical width dimension property parameter.</param>
    /// <param name="depth">The physical depth dimension property parameter.</param>
    public record InjuryInfo(
        string injury,
        string? site,
        string? length,
        string? width,
        string? depth
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralTab"/> page object entity.
    /// </summary>
    /// <param name="page">The current operating Playwright automation platform <see cref="IPage"/> abstraction instance context.</param>
    public GeneralTab(IPage page) : base(page) { }

    /// <summary>
    /// Constructs an execution action dictionary matrix linking form properties to their physical interactive input delegates.
    /// Crucial for completeness looping routines testing distinct required interface boundaries.
    /// </summary>
    /// <param name="data">The reference data state payload container powering execution parameter parameters.</param>
    /// <returns>A mapped dictionary grid capturing descriptive field strings, execution Task instructions, and mandatory configuration flags.</returns>
    public Dictionary<string, (Func<Task> Action, bool IsRequired)> GetRequiredFieldsMap(IncidentGeneralInfo data)
    {
        // Static fields
        var map = new Dictionary<string, (Func<Task> Action, bool IsRequired)>
    {
        { "Room", (() => GetFieldByLabel("Room").FillAsync(data.room), true) },
        { "Bed", (() => GetFieldByLabel("Bed").FillAsync(data.bed), true) },
        
        // Selecting "today" if only date is selected
        { "Date of Incident", (async () => { if (data.date.HasValue) await SelectTodayAsync("dateOfIncident"); }, true) },
        
        // Extracting pure .Value, checking by null before
        { "Time of Incident", (async () => { if (data.time.HasValue) await SelectTimeInPickerAsync("timeOfIncident", data.time.Value);}, true) },
        
        // Extracting numeric values via .Value, if they were transfered
        { "Supervisor", (async () => { if (data.supervisor.HasValue) await SelectDropdownOptionAsync("Supervisor", data.supervisor.Value); }, true) },
        { "Charge nurse", (async () => { if (data.chargeNurse.HasValue) await SelectDropdownOptionAsync("Charge nurse", data.chargeNurse.Value); }, true) },
        { "CNA", (async () => { if (data.cna.HasValue) await SelectDropdownOptionAsync("CNA", data.cna.Value); }, true) },

        { "Location of Incident", (() => SelectDropdownOptionAsync("Location of incident", data.location), true) },
        { "Type of Incident", (() => SelectDropdownOptionAsync("Type of incident", data.type), true) },
        { "Activity Prior", (() => SelectDropdownOptionAsync("Activity Prior", data.activity), false) },
        { "SBARSummary", (() => GetFieldByLabel("SBARSummary").FillAsync(data.summary), true) }
    };

        // Adding dinamicaly Injury list
        if (data.injury != null)
        {
            for (int i = 0; i < data.injury.Count; i++)
            {
                var index = i;
                var injuryItem = data.injury[i];

                string injuryKey = i == 0 ? "Injuries Sustained" : $"Injuries Sustained {i + 1}";

                map.Add(injuryKey, (async () =>
                {
                    if (index > 0) await GetButtonByText("Add Injury").ClickAsync();
                    await AddInjuryAsync(index, injuryItem);
                }, true));
            }
        }

        return map;
    }


    /// <summary>
    /// Executes sequential UI text insertions and component operations to comprehensively fill basic form attributes.
    /// Captures actual rendering names inside structural layout zones for downstream logging evaluation steps.
    /// </summary>
    /// <param name="generalInfo">The instruction sequence blueprint detailing target state field entries.</param>
    /// <returns>An adjusted record snapshot holding contextual post-selection personnel values obtained in real time.</returns>
    public async Task<IncidentGeneralInfo> FillBasicInfoAsync(IncidentGeneralInfo generalInfo)
    {
        // Fields for saving text values form UI
        string supervisorName = "";
        string chargeNurseName = "";
        string cnaName = "";

        // Fill in text fields if only they are not empty
        if (!string.IsNullOrEmpty(generalInfo.room))
            await GetFieldByLabel("Room").FillAsync(generalInfo.room);

        if (!string.IsNullOrEmpty(generalInfo.bed))
            await GetFieldByLabel("Bed").FillAsync(generalInfo.bed);

        // Fill in dropdwns if only they are not empty
        if (!string.IsNullOrEmpty(generalInfo.unit))
            await SelectDropdownOptionAsync("Unit", generalInfo.unit);

        if (!string.IsNullOrEmpty(generalInfo.location))
            await SelectDropdownOptionAsync("Location of Incident", generalInfo.location);

        if (!string.IsNullOrEmpty(generalInfo.type))
            await SelectDropdownOptionAsync("Type of Incident", generalInfo.type);

        if (!string.IsNullOrEmpty(generalInfo.activity))
            await SelectDropdownOptionAsync("Activity Prior", generalInfo.activity);

        if (!string.IsNullOrEmpty(generalInfo.summary))
            await GetFieldByLabel("SBARSummary").FillAsync(generalInfo.summary);

        // Date proccessing: choose "Today" if only date is not null
        if (generalInfo.date.HasValue)
        {
            await SelectTodayAsync("dateOfIncident");
        }

        // Time processing: choose time if only it is not null
        if (generalInfo.time.HasValue && generalInfo.date.HasValue)
        {
            await SelectTimeInPickerAsync("timeOfIncident", generalInfo.time.Value);
        }

        // Fill in staff dropdowns if only they are not empty
        if (generalInfo.supervisor.HasValue)
        {
            await SelectDropdownOptionAsync("Supervisor", generalInfo.supervisor.Value);
            supervisorName = await GetFieldByLabel("Supervisor").InnerTextAsync();
        }

        if (generalInfo.chargeNurse.HasValue)
        {
            await SelectDropdownOptionAsync("Charge nurse", generalInfo.chargeNurse.Value);
            chargeNurseName = await GetFieldByLabel("Charge nurse").InnerTextAsync();
        }

        if (generalInfo.cna.HasValue)
        {
            await SelectDropdownOptionAsync("CNA", generalInfo.cna.Value);
            cnaName = await GetFieldByLabel("CNA").InnerTextAsync();
        }

        // Filling in Injuries section
        if (generalInfo.injury != null && generalInfo.injury.Count > 0)
        {
            for (int i = 0; i < generalInfo.injury.Count; i++)
            {
                await AddInjuryAsync(i, generalInfo.injury[i]);

                if (i < generalInfo.injury.Count - 1)
                {
                    await GetButtonByText("Add Injury").ClickAsync();
                }
            }
        }

        return generalInfo with
        {
            LoadedSupervisorName = supervisorName,
            LoadedChargeNurseName = chargeNurseName,
            LoadedCnaName = cnaName
        };
    }


    /// <summary>
    /// Adds information about a specific injury based on its index.
    /// </summary>
    /// <param name="i">The zero-based index of the injury fields row.</param>
    /// <param name="injury">The injury details containing type, site, and dimensions.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task AddInjuryAsync(int i, InjuryInfo injury)
    {
        // 1. Обязательный дропдаун (всегда заполняем)
        if (!string.IsNullOrEmpty(injury.injury))
        {
            await SelectDropdownOptionAsync("Injuries Sustained", injury.injury, i);
        }

        // 2. Необязательный дропдаун — заполняем ТОЛЬКО если передано значение
        if (!string.IsNullOrEmpty(injury.site))
        {
            await SelectDropdownOptionAsync("Site of Injury", injury.site, i);
        }

        // 3. Инпуты размеров — заполняем ТОЛЬКО если они не null
        if (!string.IsNullOrEmpty(injury.length))
        {
            await GetFieldByLabel("Length (Centimeters)").Nth(i).FillAsync(injury.length);
        }

        if (!string.IsNullOrEmpty(injury.width))
        {
            await GetFieldByLabel("Width (Centimeters)").Nth(i).FillAsync(injury.width);
        }

        if (!string.IsNullOrEmpty(injury.depth))
        {
            await GetFieldByLabel("Depth (Centimeters)").Nth(i).FillAsync(injury.depth);
        }
    }

    /// <summary>
    /// Focuses and clicks on a date picker control icon identified by its technical html name attribute.
    /// </summary>
    /// <param name="nameAttribute">The targeted identifier or technical html element name attribute string.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClickControlIcon(string nameAttribute)
    {
        // 1. Click the calendar action button icon element
        var calendarIcon = GetFieldIconByName(nameAttribute);

        // Force alignment scrolling towards target indicators prior to handling click events
        await calendarIcon.ScrollIntoViewIfNeededAsync();
        await calendarIcon.ClickAsync();

        // Maintain synchronization delay ensuring overlay animations expand fully before execution steps proceed
        await Task.Delay(1000);

        // 2. Wait for the popup container layer to reveal
    }

    /// <summary>
    /// Selects the current date ("Today") from a Kendo UI calendar popup.
    /// </summary>
    /// <param name="nameAttribute">The name attribute or identifier of the date control.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SelectTodayAsync(string nameAttribute)
    {

        await ClickControlIcon(nameAttribute);

        // Simplify the locator to the most basic one visible in the screenshot code
        // Initialize the locator WITHOUT .Last to avoid premature failures
        var popupSearch = Page.Locator("kendo-popup");

        try
        {
            // Wait for at least one popup to appear in the DOM
            await popupSearch.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // Now take the last active one
            var popup = popupSearch.Last;
            // 3. Search for the "Today" button
            // In Kendo UI, the button can be either a button element or a span inside it. We use text.
            var todayBtn = popup.GetByRole(AriaRole.Button).Filter(new() { HasText = "Today" });

            //if (await todayBtn.CountAsync() > 0)
            //{
            //    await todayBtn.First.ClickAsync(new() { Force = true });
            //}
            //else
            //{
            //    // Fallback option if it is not a Button by role
            //    await popup.Locator(".k-calendar-nav-today, .k-nav-today").ClickAsync(new() { Force = true });
            //}

            // Перехватываем и ждем сетевой ответ от бэкенда при клике
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                if (await todayBtn.CountAsync() > 0)
                {
                    await todayBtn.First.ClickAsync(new() { Force = true });
                }
                else
                {
                    await popup.Locator(".k-calendar-nav-today, .k-nav-today").ClickAsync(new() { Force = true });
                }
            }, response => response.Url.Contains("responsible-employees") && response.Status == 200);


            // 4. Wait for closure
            await popup.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 1000 });
            // NOTE: Review debug log
            Log.Debug($"Date 'Today' is selected for the field {nameAttribute}");
        }
        catch (Exception ex)
        {
            Log.Error($"Error while operating with the calendar  {ex.Message}");
            // Take a screenshot precisely at the moment of error inside the method
            await Page.ScreenshotAsync(new() { Path = $"popup_error_{nameAttribute}.png" });
            throw;
        }
    }

    /// <summary>
    /// Selects a specific date in an Angular Material calendar picker. Not quiet ready yet.
    /// </summary>
    /// <param name="label">The label text of the date field.</param>
    /// <param name="date">The target date time value to select.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SelectDateInCalendarAsync(string label, DateTime date)
    {
        // Open the calendar
        await GetFieldIcon(label).ClickAsync();

        // 2. Switch to year/month selection mode (click the header, for example "FEB 2026")
        var periodButton = Page.Locator(".mat-calendar-period-button");
        await periodButton.ClickAsync();

        // 3. Select the year
        await Page.Locator(".mat-calendar-body-cell")
                   .GetByText(date.Year.ToString(), new() { Exact = true })
                   .ClickAsync();

        // 4. Select the month (abbreviated name, for example "MAY")
        // The format depends on your application's localization
        var monthName = date.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture).ToUpper();
        await Page.Locator(".mat-calendar-body-cell")
                   .GetByText(monthName, new() { Exact = true })
                   .ClickAsync();

        // 5. Select the day
        await Page.Locator(".mat-calendar-body-cell")
                   .GetByText(date.Day.ToString(), new() { Exact = true })
                   .ClickAsync();
    }

    /// <summary>
    /// Clears pre-filled "Room" and "Bed" fields for validation purposes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ClearPreFilledFieldsAsync()
    {
        await GetFieldByLabel("Room").ClearAsync();
        await GetFieldByLabel("Bed").ClearAsync();
        // NOTE: Review debug log
        Log.Debug("Pre-filled fields are cleared for falidation");
    }

    /// <summary>
    /// Verifies that the actual values in the form fields match the expected incident data.
    /// </summary>
    /// <param name="expected">The data object containing expected values for verification.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task VerifyDataFieldsAsync(IncidentGeneralInfo expected)
    {
        // Check fields only if it was filled in the data object
        if (!string.IsNullOrEmpty(expected.room))
        {
            var actualRoom = await GetFieldByLabel("Room").InputValueAsync();
            Assert.That(actualRoom, Is.EqualTo(expected.room));
        }

        if (!string.IsNullOrEmpty(expected.bed))
        {
            // Стандартный кейс: если в тест-данных передана конкретная кровать, сверяем с ней
            var actualBed = await GetFieldByLabel("Bed").InputValueAsync();
            Assert.That(actualBed, Is.EqualTo(expected.bed));
        }
        else
        {
            // Кейс автозаполнения: если в expected кровать пустая, проверяем, что фронтенд 
            // САМ заполнил это поле на форме хоть каким-то значением (оно не осталось пустым)
            var actualBed = await GetFieldByLabel("Bed").InputValueAsync();
            Assert.That(actualBed, Is.Not.Null.And.Not.Empty,
                "Validation Error: The 'Bed' field on the form is empty, but it should be auto-populated from the resident profile!");
        }

        if (!string.IsNullOrEmpty(expected.location))
        {
            // InnerTextAsync or InputValueAsync depending on the dropdown type
            var actualLocation = await GetFieldByLabel("Location of incident").InnerTextAsync();
            Assert.That(actualLocation, Does.Contain(expected.location));
        }

        if (!string.IsNullOrEmpty(expected.unit))
        {
            // InnerTextAsync or InputValueAsync depending on the dropdown type
            var actualUnit = await GetFieldByLabel("Unit").InnerTextAsync();
            Assert.That(actualUnit, Does.Contain(expected.unit));
        }

        if (expected.date.HasValue)
        {
            var actualDate = await GetFieldByLabel("Date of Incident").InputValueAsync();

            // Add CultureInfo.InvariantCulture so that the separator is always a slash '/'
            string expectedDateStr = expected.date.Value.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);

            Assert.That(actualDate, Is.EqualTo(expectedDateStr));
        }

        if (expected.time.HasValue)
        {
            var actualTime = await GetFieldByLabel("Time of Incident").InputValueAsync();

            // Convert TimeOnly to a string of the corresponding format for verification:
            string expectedTimeStr = expected.time.Value.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);

            Assert.That(actualTime, Is.EqualTo(expectedTimeStr));
        }

        if (!string.IsNullOrEmpty(expected.summary))
        {
            var actualSummary = await GetFieldByLabel("SBARSummary").InputValueAsync();
            Assert.That(actualSummary, Is.EqualTo(expected.summary));
        }

        // Charge Supervisor verification
        if (!string.IsNullOrEmpty(expected.LoadedSupervisorName))
        {
            var actualSupervisor = await GetFieldByLabel("Supervisor").InnerTextAsync();
            Assert.That(actualSupervisor, Is.EqualTo(expected.LoadedSupervisorName),
                "Supervisor draft text mismatch!");
        }

        // Charge Nurse verification
        if (!string.IsNullOrEmpty(expected.LoadedChargeNurseName))
        {
            var actualChargeNurse = await GetFieldByLabel("Charge nurse").InnerTextAsync();
            Assert.That(actualChargeNurse, Is.EqualTo(expected.LoadedChargeNurseName),
                "Charge nurse draft text mismatch!");
        }

        // CNA verification
        if (!string.IsNullOrEmpty(expected.LoadedCnaName))
        {
            var actualCna = await GetFieldByLabel("CNA").InnerTextAsync();
            Assert.That(actualCna, Is.EqualTo(expected.LoadedCnaName),
                "CNA draft text mismatch!");
        }
    }

}