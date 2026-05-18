using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;
using static GeneralTab;

public class DetailsTab : BaseIncidentTabs
{
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


    public record VitalSigns(
        string Temperature,
        string Pulse,
        string BpSitting,
        string Spo2,
        string BpLaying,
        string BloodGlucose
    );

    public record RelativeNotification(
        string Name,
        string Relationship,
        string WhoNotified,
        DateTime Date,
        TimeOnly Time
    );

    public record MDNotification(
        string MDName,
        string WhoNotified,
        DateTime Date,
        TimeOnly Time
    );

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
            // Это поле без точки, просто переключаем его в "Yes", чтобы появилось следующее поле
            await SelectFirstAdmitedAsync(true, "");
        }, false) },

        { "Describe (MD)", (async () => {
            // 1. Убеждаемся, что свитч включен (на случай, если мы проверяем поля вразнобой)
            await SelectFirstAdmitedAsync(true, "");
    
            // 2. Заполняем само поле описания
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
            // Передаем контекст строки relativeLineContext последним аргументом
            await SelectDateTimeInPickerAsync("Date and Time of Notification", data.RelativeNotified.Date, data.RelativeNotified.Time, relativeLineContext), true)
        },        
        // MD Notification
        { "MD Notified (MD)", (async () => await GetFieldByLabel("MD Notified").FillAsync(data.MDNotified.MDName), true) },
        { "Who Notified MD", (async () => await GetFieldByLabel("Who Notified MD").FillAsync(data.MDNotified.WhoNotified), true) },
        { "Date and Time of Notification (MD)", (async () => 
            // Передаем контекст строки mdLineContext последним аргументом
            await SelectDateTimeInPickerAsync("Date and Time of Notification", data.MDNotified.Date, data.MDNotified.Time, mdLineContext), true)
        },
        // Final Fields
        { "MD Order (If applicable)", (async () => await GetFieldByLabel("MD Order (If applicable)").FillAsync(data.MDOrder), true) },
        { "Diagnostic Tests Ordered", (async () => await GetFieldByLabel("Diagnostic Tests Ordered").FillAsync(data.DiagnosticTests), true) }
    };

        return map;
    }

    public DetailsTab(IPage page) : base(page) { }

    public async Task FillDetailsInfoAsync(IncidentDetailsInfo details)
    {
        // Описания (Textareas)
        await GetFieldByLabel("Describe Occurrence").FillAsync(details.OccurrenceDescription);
        await GetFieldByLabel("Patient’s Description of Occurrence").FillAsync(details.PatientDescription);
        if (!string.IsNullOrEmpty(AllDiagnises))
        {
            await GetFieldByLabel("All Diagnoses").FillAsync(AllDiagnises);
            Log.Debug("All Diagnoses field restored from saved variable.");
        }
        
        // Vital Signs
        await FillVitalSignsAsync(details.VitalSigns);

        // Frsrt Aid
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

    private async Task FillVitalSignsAsync(VitalSigns vitals)
    {
        await GetFieldByLabel("Temperature").FillAsync(vitals.Temperature);
        await GetFieldByLabel("Pulse").FillAsync(vitals.Pulse);
        await GetFieldByLabel("Blood Pressure Sitting").FillAsync(vitals.BpSitting);
        await GetFieldByLabel("SPO2").FillAsync(vitals.Spo2);
        await GetFieldByLabel("Blood Pressure Laying").FillAsync(vitals.BpLaying);
        await GetFieldByLabel("Blood Glucose").FillAsync(vitals.BloodGlucose);
    }

    private async Task FillRelativeNotificationAsync(RelativeNotification rel)
    {
        var relativeLineContext = Page.Locator("div.panel-line, div.col-wrap")
            .Filter(new() { HasText = "Relationship" });

        await relativeLineContext.Locator(GetFieldByLabel("Name of Relative Notified")).FillAsync(rel.Name);
        await relativeLineContext.Locator(GetFieldByLabel("Relationship")).FillAsync(rel.Relationship);
        await relativeLineContext.Locator(GetFieldByLabel("Who Notified")).FillAsync(rel.WhoNotified);

        // Передаем контекст строки вместо индекса 0
        await SelectDateTimeInPickerAsync("Date and Time of Notification", rel.Date, rel.Time, relativeLineContext);
    }

    private async Task FillMDNotificationAsync(MDNotification md)
    {
        var mdLineContext = Page.Locator("div.panel-line, div.col-wrap")
            .Filter(new() { HasText = "Who Notified MD" });

        await mdLineContext.Locator(GetFieldByLabel("MD Notified")).FillAsync(md.MDName);
        await mdLineContext.Locator(GetFieldByLabel("Who Notified MD")).FillAsync(md.WhoNotified);

        // Передаем контекст строки вместо индекса 1
        await SelectDateTimeInPickerAsync("Date and Time of Notification", md.Date, md.Time, mdLineContext);
    }

    public async Task SelectFirstAdmitedAsync(Boolean firstAidAdministered, string describe)
    {
        // First Aid (Radio + Describe)
        // Ищем контейнер, в котором есть текст заголовка, и внутри него находим свитч

        Log.Debug($"Looking for the field First Aid Administered, and try to switch it");

        await SelectRadioOptionAsync("First Aid Administered:", firstAidAdministered ? "Yes" : "No");

        // Если нужно "Yes" (true), а сейчас "false" — кликаем
        if (firstAidAdministered == true)
        {
            Log.Debug($"Enter Description");

            var describeField = Page.Locator("textarea[name='firstAidDesc']");

            await describeField.WaitForAsync();
            await describeField.FillAsync(describe);
        }

    }
    protected new async Task SelectRadioOptionAsync(string labelText, string option)
    {
        Log.Debug($"Looking for radiobutton: '{labelText}' with the value: '{option}'");

        // 1. Поиск контейнера (используем более гибкий селектор div.horizontal-field)
        var fieldContainer = Page.Locator("div.horizontal-field")
            .Filter(new() { HasText = labelText });

        // Проверка существования контейнера
        var containerCount = await fieldContainer.CountAsync();
        if (containerCount == 0)
        {
            // Выведем список всех найденных полей для отладки
            var allLabels = await Page.Locator("div.horizontal-field span.label-text").AllInnerTextsAsync();
            Log.Debug($"Available lables on the page: {string.Join(", ", allLabels)}");
            throw new Exception($"Field container '{labelText}' not found.");
        }

        // 2. Поиск kendo-switch внутри контейнера
        var kendoSwitch = fieldContainer.Locator("kendo-switch");
        await kendoSwitch.ScrollIntoViewIfNeededAsync();

        if (!await kendoSwitch.IsVisibleAsync())
        {
            Log.Debug($"[ERROR] Element 'kendo-switch' was found in DOM, but is not visible for the field '{labelText}'.");
        }

        // 3. Чтение текущего состояния
        var ariaChecked = await kendoSwitch.GetAttributeAsync("aria-checked");
        bool isCurrentlyChecked = ariaChecked?.ToLower() == "true";
        bool shouldBeChecked = option.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase);

        // 4. Клик, если состояние нужно изменить
        if (isCurrentlyChecked != shouldBeChecked)
        {
            await kendoSwitch.ClickAsync();

            // Проверка: изменилось ли состояние после клика?
            var newState = await kendoSwitch.GetAttributeAsync("aria-checked");
        }

        // Небольшая пауза для Angular/Kendo анимаций и рендеринга зависимых полей
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    public async Task SelectDateTimeInPickerAsync(string labelText, DateTime date, TimeOnly time, ILocator rowContext)
    {
        // 1. Открываем пикер строго внутри нашей изолированной строки panel-line
        Log.Debug($"Open the calendar {labelText} and try to select a date");
        var fieldContainer = rowContext.Locator("cad-label-value-field").Filter(new() { HasText = labelText });
        await fieldContainer.Locator("button.k-input-button").ClickAsync();

        // 2. Ждем, пока календарь станет видимым
        var calendar = Page.Locator("kendo-calendar, .k-calendar-container");
        await calendar.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // =========================================================================
        // ИСПРАВЛЕНИЕ: КЛИКАЕМ ПО КНОПКЕ "TODAY" ДЛЯ ГАРАНТИРОВАННОГО ВЫБОРА ДАТЫ
        // =========================================================================
        Log.Debug("Кликаем по кнопке Today в Kendo календаре...");

        // Находим кнопку Today внутри открытого календаря
        var todayButton = calendar.Locator("button.k-calendar-title + button, .k-nav-today, button:has-text('Today')").First;

        await todayButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await todayButton.ClickAsync();

        Log.Debug("Кнопка Today успешна нажата. Текущая дата выбрана.");
        // =========================================================================

        // 3. Переключаемся на Time (Ваш рабочий код)
        var timeTab = Page.Locator("kendo-datetimepicker .k-button").GetByText("Time");
        if (await timeTab.IsVisibleAsync())
        {
            await timeTab.ClickAsync();
        }

        // 4. Подготовка данных времени
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

        // 5. Подтверждение (кнопка Set)
        await activePopup.Locator("button.k-time-accept, .k-datetime-footer button:has-text('Set')").ClickAsync();

        // Стабильный барьер закрытия поп-апа
        await Page.Locator(".k-animation-container").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await Page.Keyboard.PressAsync("Escape");

        var kendoPopups = Page.Locator("kendo-popup, kendo-calendar, .k-calendar-container");
        if (await kendoPopups.CountAsync() > 0)
        {
            await kendoPopups.First.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 3000 });
        }
        await Page.WaitForTimeoutAsync(250); // Пауза для фиксации состояния формы Angular
    }
    public async Task ClearAndSaveDiagnosesAsync()
    {
        var field = GetFieldByLabel("All Diagnoses");
        AllDiagnises = await field.InputValueAsync(); // Сохраняем в поле класса
        await field.ClearAsync();
        Log.Debug("All Diagnoses cleared");
    }

    public async Task<bool> IsRadioOptionSelectedAsync(string groupName, string optionValue)
    {
        // Ищет радио-кнопку по тексту группы и значению
        return await Page.Locator($"//div[contains(., '{groupName}')]//input[@type='radio' and @value='{optionValue}']").IsCheckedAsync();
    }

    public async Task<bool> IsSwitchOnAsync(string labelText)
    {
        // Находим контейнер поля по тексту лейбла ("First Aid Administered:")
        var container = Page.Locator("div.horizontal-field").Filter(new() { HasText = labelText });

        // Внутри контейнера ищем сам kendo-switch
        var kendoSwitch = container.Locator("kendo-switch");

        // Считываем значение атрибута aria-checked ("true" или "false")
        string ariaChecked = await kendoSwitch.GetAttributeAsync("aria-checked") ?? "false";

        return ariaChecked.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> GetInputValueByLabelAsync(string label)
    {
        return await GetFieldByLabel(label).InputValueAsync();
    }

    public async Task VerifyDataFieldsAsync(IncidentDetailsInfo expected)
    {
        // 1. Описания (Textareas)
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
            "Состояние тогла 'First Aid Administered:' не совпадает.");

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
            "Состояние тогла 'Was Resident Transferred to the Hospital?' не совпадает.");


        // 5. Actions
        if (!string.IsNullOrEmpty(expected.CorrectiveAction))
            Assert.That(await GetInputValueByLabelAsync("Corrective Action (Immediate Intervention)"), Is.EqualTo(expected.CorrectiveAction));

        if (!string.IsNullOrEmpty(expected.PreventiveAction))
            Assert.That(await GetInputValueByLabelAsync("Preventive Action (Long Term Plan)"), Is.EqualTo(expected.PreventiveAction));

        // ==========================================
        // 6. Секция: Relative Notification (Изоляция через Relationship)
        // ==========================================
        var relativeLineContext = Page.Locator("div.panel-line, div.col-wrap")
            .Filter(new() { HasText = "Relationship" });

        if (!string.IsNullOrEmpty(expected.RelativeNotified.Name))
        {
            // Ищем поле внутри изолированной строки по его тексту лейбла
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
            // Теперь мы гарантированно берем Who Notified из блока родственников, игнорируя блок врача
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
        // 7. Секция: MD Notification (Изоляция через Who Notified MD)
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
