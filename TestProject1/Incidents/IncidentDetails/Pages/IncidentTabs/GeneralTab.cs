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
        string room,
        string bed,
        DateTime date,
        TimeOnly time,
        string unit,
        string location,
        string type,
        string activity,
        string summary, 
        List<InjuryInfo> injury
);

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
        { "Room", (() => GetFieldByLabel("Room").FillAsync(data.room),true) },
        { "Bed", (() => GetFieldByLabel("Bed").FillAsync(data.bed), true) },
        { "Date of Incident", (() => SelectTodayAsync("dateOfIncident"), true) },
        { "Time of Incident", (() => SelectTimeInPickerAsync("timeOfIncident", data.time) ,true)},
        { "Supervisor", (() => SelectDropdownOptionAsync("Supervisor", 1), true )},
        { "Charge nurse",( () => SelectDropdownOptionAsync("Charge nurse", 1) , true)},
        { "CNA", (() => SelectDropdownOptionAsync("CNA", 1), true )},
        { "Location of Incident", (() => SelectDropdownOptionAsync("Location of incident", data.location),true) },
        { "Type of Incident",( () => SelectDropdownOptionAsync("Type of incident", data.type),true )},
        { "Activity Prior",( () => SelectDropdownOptionAsync("Activity Prior", data.activity), false) },
        { "SBARSummary",( () => GetFieldByLabel("SBARSummary").FillAsync(data.summary), true )}
    };

        // 2. Ссылаемся на ваш List<InjuryInfo> и добавляем в тот же словарь
        for (int i = 0; i < data.injury.Count; i++)
        {
            var index = i;
            var injuryItem = data.injury[i];

            string injuryKey = i == 0 ? "Injuries Sustained" : $"Injuries Sustained {i + 1}";

            // Добавляем кортеж: (Действие, Обязательность)
            map.Add(injuryKey, (async () =>
            {
                if (index > 0) await GetButtonByText("Add Injury").ClickAsync();
                await AddInjuryAsync(index, injuryItem);
            }, true)); // <--- Вот этот true указывает, что поле обязательное
        }
        return map;
    }


    public async Task FillBasicInfoAsync(IncidentGeneralInfo generalInfo)
    {
        await GetFieldByLabel("Room").FillAsync(generalInfo.room);
        await GetFieldByLabel("Bed").FillAsync(generalInfo.bed);
        await SelectDropdownOptionAsync("Unit", generalInfo.unit);
        await SelectDropdownOptionAsync("Location of incident",generalInfo.location);
        await SelectDropdownOptionAsync("Type of incident",generalInfo.type);
        await SelectDropdownOptionAsync("Activity Prior",generalInfo.activity);
        await GetFieldByLabel("SBARSummary").FillAsync(generalInfo.summary);

        await SelectTodayAsync("dateOfIncident");
        await SelectTimeInPickerAsync("timeOfIncident", generalInfo.time);

        await Page.MakeScreenshotAsync("General_Halfly_Filled"); // <--- Скриншот 2

        await SelectDropdownOptionAsync("Supervisor",1 );
        await SelectDropdownOptionAsync("Charge nurse",1 );
        await SelectDropdownOptionAsync("CNA", 1 );

        
        for (int i = 0; i < generalInfo.injury.Count; i++)
            {
                await AddInjuryAsync(i, generalInfo.injury[i]);

                // Если не последний — жмем добавить
                if (i < generalInfo.injury.Count - 1)
                {
                    await GetButtonByText("Add Injury").ClickAsync();
                }
            }
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

}