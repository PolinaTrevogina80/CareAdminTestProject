using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;

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
        { "Date and Time of Notification (Relative)", (async () => await SelectDateTimeInPickerAsync("Date and Time of Notification", data.RelativeNotified.Date, data.RelativeNotified.Time, 0), true) },
        
        // MD Notification
        { "MD Notified (MD)", (async () => await GetFieldByLabel("MD Notified").FillAsync(data.MDNotified.MDName), true) },
        { "Who Notified MD", (async () => await GetFieldByLabel("Who Notified MD").FillAsync(data.MDNotified.WhoNotified), true) },
        { "Date and Time of Notification (MD)", (async () => await SelectDateTimeInPickerAsync("Date and Time of Notification", data.MDNotified.Date, data.MDNotified.Time, 1), true) },

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
        await GetFieldByLabel("Name of Relative Notified").FillAsync(rel.Name);
        await GetFieldByLabel("Relationship").FillAsync(rel.Relationship);
        await GetFieldByLabel("Who Notified").First.FillAsync(rel.WhoNotified);

        // Предполагаю, что "notificationDateRelative" — ID или Label для пикера
        await SelectDateTimeInPickerAsync("Date and Time of Notification", rel.Date, rel.Time, 0);
    }

    private async Task FillMDNotificationAsync(MDNotification md)
    {
        await GetFieldByLabel("MD Notified").FillAsync(md.MDName);
        await GetFieldByLabel("Who Notified MD").FillAsync(md.WhoNotified);

        await SelectDateTimeInPickerAsync("Date and Time of Notification", md.Date, md.Time, 1);
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
    public async Task SelectDateTimeInPickerAsync(string labelText, DateTime date, TimeOnly time, int index = 0)
    {
        // 1. Открываем пикер
        Log.Debug($"Open the calendar {labelText} and try to select a date");

        var fieldContainer = Page.Locator("cad-label-value-field").Filter(new() { HasText = labelText }).Nth(index);
        await fieldContainer.Locator("button.k-input-button").ClickAsync();

        // 2. Ждем, пока календарь станет видимым
        var calendar = Page.Locator("kendo-calendar, .k-calendar-container");
        await calendar.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // 3. Кликаем дату
        // Используем GetByRole("gridcell"), так как ячейки календаря — это именно ячейки таблицы
        var dateCell = calendar.GetByRole(AriaRole.Gridcell, new() { Name = date.Day.ToString(), Exact = true });

        // Если ячеек несколько (например, 31 число видно из другого месяца), берем ту, что в текущем месяце
        // Обычно у активного месяца нет класса .k-other-month
        await dateCell.Filter(new() { HasNot = Page.Locator(".k-other-month") }).First.ClickAsync();

        Log.Debug($"Date is clicked: {date.Day}");

        // 3. Переключаемся на Time (Kendo иногда требует явного клика по вкладке)
        var timeTab = Page.Locator("kendo-datetimepicker .k-button").GetByText("Time");
        if (await timeTab.IsVisibleAsync())
        {
            await timeTab.ClickAsync();
        }

        // 4. Подготовка данных (Kendo TimeList требует формат "01", "02"...)
        var englishCulture = System.Globalization.CultureInfo.InvariantCulture;
        string hourWithZero = time.ToString("hh", englishCulture); // "08"
        string hourSimple = time.Hour > 12 ? (time.Hour - 12).ToString() : time.Hour.ToString(); // "8"
        if (hourSimple == "0") hourSimple = "12"; // Обработка полночи/полдня
        string minute = time.ToString("mm", englishCulture); // "46"
        string tt = time.ToString("tt", englishCulture);     // ГАРАНТИРОВАНО "AM" или "PM"

        // Определяем индекс часа в колонке: 
        // Если AM — берем первое вхождение "05", если PM — второе.
        int hourIndex = (tt == "AM") ? 0 : 1;

        // 1. Находим именно тот контейнер, который открылся СЕЙЧАС (он последний в DOM)
        var activePopup = Page.Locator("kendo-popup, .k-animation-container").Last;
        // Ждем, пока он станет видимым, чтобы не кликать в пустоту
        await activePopup.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var hourColumn = activePopup.Locator(".k-time-list").Nth(0);
        var minuteColumn = activePopup.Locator(".k-time-list").Nth(1);
        var ttColumn = activePopup.Locator(".k-time-list").Nth(2);

        // ВЫБОР ЧАСА (с учетом AM/PM)
        var pattern = $"^({hourWithZero}|{hourSimple})$";

        var hourItem = hourColumn.Locator(".k-item").Filter(new()
        {
            HasTextRegex = new System.Text.RegularExpressions.Regex(pattern)
        }).Nth(hourIndex);

        await hourItem.ScrollIntoViewIfNeededAsync();
        await hourItem.ClickAsync();

        await hourItem.ScrollIntoViewIfNeededAsync();
        await hourItem.ClickAsync();

        // ВЫБОР МИНУТ
        var minuteItem = minuteColumn.Locator(".k-item").GetByText(minute, new() { Exact = true }).First;
        await minuteItem.ScrollIntoViewIfNeededAsync();
        await minuteItem.ClickAsync();


        // ВЫБОР AM/PM (в третьей колонке)
        var ttItem = ttColumn.Locator(".k-item").GetByText(tt, new() { Exact = true }).First;
        await ttItem.ScrollIntoViewIfNeededAsync();
        await ttItem.ClickAsync();

        // 5. Подтверждение (кнопка Set)
        await activePopup.Locator("button.k-time-accept, .k-datetime-footer button:has-text('Set')").ClickAsync();

        // Перед кликом по новому пикеру
        await Page.Locator(".k-animation-container").WaitForAsync(new() { State = WaitForSelectorState.Hidden });

    }

    public async Task ClearAndSaveDiagnosesAsync()
    {
        var field = GetFieldByLabel("All Diagnoses");
        AllDiagnises = await field.InputValueAsync(); // Сохраняем в поле класса
        await field.ClearAsync();
        Log.Debug("All Diagnoses cleared");
    }
}
