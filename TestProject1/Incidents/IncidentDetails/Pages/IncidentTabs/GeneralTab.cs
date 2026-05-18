using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Microsoft.Testing.Platform.Extensions.Messages;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;
using System.Globalization;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class GeneralTab : BaseIncidentTabs
{
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

            // Новые поля для хранения текстовых значений из UI
            string LoadedSupervisorName = "",
            string LoadedChargeNurseName = "",
            string LoadedCnaName = ""
        )
    {
    public IncidentGeneralInfo GetOnlyRequiredFields()
        {
            return this with
            {
                activity = "",       // Нет звездочки
                summary = "",        // Нет звездочки
                supervisor = 0,      // Нет звездочки
                chargeNurse = 0,     // Нет звездочки
                cna = 0              // Нет звездочки
            };
    
    }
};

    public record InjuryInfo(
        string injury, 
        string site,
        string length,
        string width,
        string depth
    );

    public GeneralTab(IPage page) : base(page) { }

    public Dictionary<string, (Func<Task> Action, bool IsRequired)> GetRequiredFieldsMap(IncidentGeneralInfo data)
    {
        // 1. Статичные поля
        var map = new Dictionary<string, (Func<Task> Action, bool IsRequired)>
    {
        { "Room", (() => GetFieldByLabel("Room").FillAsync(data.room), true) },
        { "Bed", (() => GetFieldByLabel("Bed").FillAsync(data.bed), true) },
        
        // Выбираем "Сегодня" только если дата передана
        { "Date of Incident", (async () => { if (data.date.HasValue) await SelectTodayAsync("dateOfIncident"); }, true) },
        
        // Извлекаем чистое значение через .Value, предварительно проверив на null
        { "Time of Incident", (async () => { if (data.time.HasValue) await SelectTimeInPickerAsync("timeOfIncident", data.time.Value); }, true) },
        
        // Извлекаем числовые значения через .Value, если они переданы
        { "Supervisor", (async () => { if (data.supervisor.HasValue) await SelectDropdownOptionAsync("Supervisor", data.supervisor.Value); }, true) },
        { "Charge nurse", (async () => { if (data.chargeNurse.HasValue) await SelectDropdownOptionAsync("Charge nurse", data.chargeNurse.Value); }, true) },
        { "CNA", (async () => { if (data.cna.HasValue) await SelectDropdownOptionAsync("CNA", data.cna.Value); }, true) },

        { "Location of Incident", (() => SelectDropdownOptionAsync("Location of incident", data.location), true) },
        { "Type of Incident", (() => SelectDropdownOptionAsync("Type of incident", data.type), true) },
        { "Activity Prior", (() => SelectDropdownOptionAsync("Activity Prior", data.activity), false) },
        { "SBARSummary", (() => GetFieldByLabel("SBARSummary").FillAsync(data.summary), true) }
    };

        // 2. Добавление динамического списка травм с защитой от null-ссылки
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


    public async Task<IncidentGeneralInfo> FillBasicInfoAsync(IncidentGeneralInfo generalInfo)
    {
        // поля для хранения текстовых значений из UI
        string supervisorName = "";
        string chargeNurseName = "";
        string cnaName = "";

        // Заполняем текстовые поля, только если они не пустые
        if (!string.IsNullOrEmpty(generalInfo.room))
            await GetFieldByLabel("Room").FillAsync(generalInfo.room);

        if (!string.IsNullOrEmpty(generalInfo.bed))
            await GetFieldByLabel("Bed").FillAsync(generalInfo.bed);

        // Заполняем дропдауны, только если передано значение
        if (!string.IsNullOrEmpty(generalInfo.unit))
            await SelectDropdownOptionAsync("Unit", generalInfo.unit);

        if (!string.IsNullOrEmpty(generalInfo.location))
            await SelectDropdownOptionAsync("Location of incident", generalInfo.location);

        if (!string.IsNullOrEmpty(generalInfo.type))
            await SelectDropdownOptionAsync("Type of incident", generalInfo.type);

        if (!string.IsNullOrEmpty(generalInfo.activity))
            await SelectDropdownOptionAsync("Activity Prior", generalInfo.activity);

        if (!string.IsNullOrEmpty(generalInfo.summary))
            await GetFieldByLabel("SBARSummary").FillAsync(generalInfo.summary);

        // Обработка Даты: выбираем "Сегодня" только если date не null
        if (generalInfo.date.HasValue)
        {
            await SelectTodayAsync("dateOfIncident");
        }

        // Обработка Времени: выбираем время только если time не null
        if (generalInfo.time.HasValue && generalInfo.date.HasValue)
        {
            await SelectTimeInPickerAsync("timeOfIncident", generalInfo.time.Value);
        }

        // Числовые дропдауны заполняем, только если они указаны
        if (generalInfo.supervisor.HasValue)
        {
            await SelectDropdownOptionAsync("Supervisor", generalInfo.supervisor.Value);
            // Считываем текст, который фактически появился в поле после выбора
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

        // Заполнение травм
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


    public async Task AddInjuryAsync(int i, InjuryInfo injury)
    {
        // Если кнопка "Add Injury" общая, кликаем её просто так. 
        // Но судя по циклу в FillBasicInfoAsync, вы уже кликаете её там.
        // Если метод вызывается ВНУТРИ цикла, повторный клик здесь может быть лишним.

        await SelectDropdownOptionAsync("Injuries Sustained", injury.injury, i);
        await SelectDropdownOptionAsync("Site of Injury", injury.site, i);

        // Для обычных полей ввода (Fill) оставляем как было:
        await GetFieldByLabel("Length (Centimeters)").Nth(i).FillAsync(injury.length);
        await GetFieldByLabel("Width (Centimeters)").Nth(i).FillAsync(injury.width);
        await GetFieldByLabel("Depth (Centimeters)").Nth(i).FillAsync(injury.depth);
    }



    public async Task SelectTodayAsync(string nameAttribute)
    {

        await ClickControlIcon(nameAttribute);

        // Упрощаем локатор до самого базового, который виден в коде на скриншоте
        // Инициализируем локатор БЕЗ .Last, чтобы не упасть раньше времени
        var popupSearch = Page.Locator("kendo-popup");

        try
        {
            // Ждем появления хотя бы одного попапа в DOM
            await popupSearch.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // Теперь берем последний активный
            var popup = popupSearch.Last;
            // 3. Поиск кнопки "Today"
            // В Kendo UI кнопка может быть как button, так и span внутри нее. Используем текст.
            var todayBtn = popup.GetByRole(AriaRole.Button).Filter(new() { HasText = "Today" });

            if (await todayBtn.CountAsync() > 0)
            {
                await todayBtn.First.ClickAsync(new() { Force = true });
            }
            else
            {
                // Резервный вариант, если это не Button по роли
                await popup.Locator(".k-calendar-nav-today, .k-nav-today").ClickAsync(new() { Force = true });
            }

            // 4. Ждем закрытия
            await popup.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            Log.Debug($"Date 'Today' is selected for the field {nameAttribute}");
        }
        catch (Exception ex)
        {
            Log.Error($"Error while operating with the calendar  {ex.Message}");
            // Делаем скриншот именно в момент ошибки внутри метода
            await Page.ScreenshotAsync(new() { Path = $"popup_error_{nameAttribute}.png" });
            throw;
        }
    }

    public async Task SelectDateInCalendarAsync(string label, DateTime date)
    {
        // 1. Открываем календарь
        await GetFieldIcon(label).ClickAsync();

        // 2. Переходим в режим выбора года/месяца (клик по заголовку, например "FEB 2026")
        var periodButton = Page.Locator(".mat-calendar-period-button");
        await periodButton.ClickAsync();

        // 3. Выбираем год
        await Page.Locator(".mat-calendar-body-cell")
                   .GetByText(date.Year.ToString(), new() { Exact = true })
                   .ClickAsync();

        // 4. Выбираем месяц (сокращенное название, например "MAY" или "МАЙ")
        // Формат зависит от локализации вашего приложения
        var monthName = date.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture).ToUpper();
        await Page.Locator(".mat-calendar-body-cell")
                   .GetByText(monthName, new() { Exact = true })
                   .ClickAsync();

        // 5. Выбираем день
        await Page.Locator(".mat-calendar-body-cell")
                   .GetByText(date.Day.ToString(), new() { Exact = true })
                   .ClickAsync();
    }

    public async Task ClearPreFilledFieldsAsync()
    {
        await GetFieldByLabel("Room").ClearAsync();
        await GetFieldByLabel("Bed").ClearAsync();
        Log.Debug("Pre-filled fields are cleared for falidation");
    }

    public async Task VerifyDataFieldsAsync(IncidentGeneralInfo expected)
    {
        // Проверяем Room, только если он был заполнен в объекте данных
        if (!string.IsNullOrEmpty(expected.room))
        {
            var actualRoom = await GetFieldByLabel("Room").InputValueAsync();
            Assert.That(actualRoom, Is.EqualTo(expected.room));
        }

        if (!string.IsNullOrEmpty(expected.bed))
        {
            var actualBed = await GetFieldByLabel("Bed").InputValueAsync();
            Assert.That(actualBed, Is.EqualTo(expected.bed));
        }

        if (!string.IsNullOrEmpty(expected.location))
        {
            // InnerTextAsync или InputValueAsync в зависимости от типа дропдауна
            var actualLocation = await GetFieldByLabel("Location of incident").InnerTextAsync();
            Assert.That(actualLocation, Does.Contain(expected.location));
        }

        if (!string.IsNullOrEmpty(expected.unit))
        {
            // InnerTextAsync или InputValueAsync в зависимости от типа дропдауна
            var actualUnit = await GetFieldByLabel("Unit").InnerTextAsync();
            Assert.That(actualUnit, Does.Contain(expected.unit));
        }

        if (expected.date.HasValue)
        {
            var actualDate = await GetFieldByLabel("Date of Incident").InputValueAsync();

            // Добавляем CultureInfo.InvariantCulture, чтобы разделителем всегда был слэш '/'
            string expectedDateStr = expected.date.Value.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);

            Assert.That(actualDate, Is.EqualTo(expectedDateStr));
        }

        if (expected.time.HasValue)
        {
            // ИСПОЛЬЗУЕМ СТРОКУ С UI: "Time of Incident" вместо "timeOfIncident"
            var actualTime = await GetFieldByLabel("Time of Incident").InputValueAsync();

            // На скриншоте время отображается в формате "11:12 AM". 
            // Приводим TimeOnly к строке соответствующего формата для сверки:
            string expectedTimeStr = expected.time.Value.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);

            Assert.That(actualTime, Is.EqualTo(expectedTimeStr));
        }

        if (!string.IsNullOrEmpty(expected.summary))
        {
            var actualSummary = await GetFieldByLabel("SBARSummary").InputValueAsync();
            Assert.That(actualSummary, Is.EqualTo(expected.summary));
        }

        if (!string.IsNullOrEmpty(expected.LoadedSupervisorName))
        {
            var actualSupervisor = await GetFieldByLabel("Supervisor").InnerTextAsync();
            Assert.That(actualSupervisor, Is.EqualTo(expected.LoadedSupervisorName),
                "Supervisor draft text mismatch!");
        }

        // Проверка Charge Nurse
        if (!string.IsNullOrEmpty(expected.LoadedChargeNurseName))
        {
            var actualChargeNurse = await GetFieldByLabel("Charge nurse").InnerTextAsync();
            Assert.That(actualChargeNurse, Is.EqualTo(expected.LoadedChargeNurseName),
                "Charge nurse draft text mismatch!");
        }

        // Проверка CNA
        if (!string.IsNullOrEmpty(expected.LoadedCnaName))
        {
            var actualCna = await GetFieldByLabel("CNA").InnerTextAsync();
            Assert.That(actualCna, Is.EqualTo(expected.LoadedCnaName),
                "CNA draft text mismatch!");
        }
    }

}