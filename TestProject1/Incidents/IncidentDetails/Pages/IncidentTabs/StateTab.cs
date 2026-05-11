using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;

public class StateTab : BaseIncidentTabs
{
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

    public record CommunicationStatus(
        bool Oriented, bool Person, bool Time, bool Place,
        bool Alert, bool Confused, bool Forgetful, bool Uncooperative,
        bool NonCompliant, bool Agitated, bool NonVerbal, bool BlindDeaf, bool LanguageBarrier
    );

    public record BowelBladderStatus(
        bool Foley, bool Colostomy, bool Continent, bool Incontinent, bool Bowel, bool Bladder
    );

    public record AlarmsStatus(
        bool NoAlarmOrder, bool BedAlarm, bool ChairAlarm, bool PinAlarm, bool OtherType
    );

    public record AssistiveDevices(
        string Wheelchair, string WalkerCrutch, string Cane, string HearingAid, string Glasses
    );

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

    public StateTab(IPage page) : base(page) { }

    // Твой текущий метод проверки теперь может использовать этот локатор
    public async Task<bool> IsGeneralStatePointVisibleAsync()
    {
        return await GeneralPointLocator.IsVisibleAsync();
    }

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
            await Page.Locator("textarea").Last.FillAsync(info.OtherAlarmDetails); // Поле без явного лейбла под Other

        // Assistive Device (Used / Not Used)
        await SetAssistiveDeviceAsync("Wheelchair", info.Devices.Wheelchair);
        await SetAssistiveDeviceAsync("Walker/Crutch", info.Devices.WalkerCrutch);
        await SetAssistiveDeviceAsync("Cane", info.Devices.Cane);
        await SetAssistiveDeviceAsync("Hearing Aid", info.Devices.HearingAid);
        await SetAssistiveDeviceAsync("Glasses", info.Devices.Glasses);
    }

    public ILocator GeneralPointLocator => Page.Locator(".completeness-message .completeness-indicator:visible");


    private async Task SetCheckboxAsync(string label, bool isChecked)
    {
        Log.Debug($"Try to set {label} as {isChecked}");

        // 1. Ищем родительский контейнер div, который содержит нужный текст лейбла
        // Модификатор :visible гарантирует работу только на активной вкладке State
        var checkboxFieldContainer = Page.Locator($".checkbox-field:visible:has-text('{label}')");

        // 2. Спускаемся внутрь этого контейнера до нативного инпута для проверки состояния
        var nativeInput = checkboxFieldContainer.Locator("input[type='checkbox']");

        // 3. Спускаемся до компонента mat-checkbox для совершения клика
        var matCheckbox = checkboxFieldContainer.Locator("mat-checkbox");

        // Проверяем текущее состояние нативного инпута
        bool currentState = await nativeInput.IsCheckedAsync();

        if (currentState != isChecked)
        {
            // Кликаем по mat-checkbox внутри найденного контейнера
            await matCheckbox.ClickAsync();
            Log.Debug($"{label} is clicked");
        }
    }


    protected new ILocator GetFieldByLabel(string labelText)
    {
        // Здесь мы ищем контейнер .input-field, 
        // в котором есть span с классом .input-field__label и нужным текстом
        return Page.Locator("div.input-field")
                    .Filter(new() { Has = Page.Locator("span.input-field__label").GetByText(labelText, new() { Exact = true }) })
                    .Locator("input, textarea, mat-select");
    }

    private async Task SetAssistiveDeviceAsync(string deviceLabel, string status) // status: "Used" or "Not Used"
    {
        if (string.IsNullOrEmpty(status)) return;
        await SetCheckboxAsync(deviceLabel, true);


        // 1. Находим конкретную строку (контейнер), где лежит текст девайса (например, "Wheelchair")
        var row = Page.Locator("div.checkbox-field")
            .Filter(new() { Has = Page.Locator("span.checkbox-field__label").GetByText(deviceLabel, new() { Exact = true }) });

        // 2. Внутри этой строки ищем радиокнопку, текст которой строго совпадает со статусом
        var radioButton = row.Locator("mat-radio-button")
            .GetByText(status, new() { Exact = true });

        // Кликаем по радиокнопке
        await radioButton.ClickAsync();
    }

    private async Task ResetAssistiveDeviceAsync(string deviceLabel)
    {
        // Находим ту же строку, что и в SetAssistiveDeviceAsync
        var row = Page.Locator("div.checkbox-field")
            .Filter(new() { Has = Page.Locator("span.checkbox-field__label").GetByText(deviceLabel, new() { Exact = true }) });

        // 1. Сбрасываем радиокнопки через JS
        await ClearRadioInContainerAsync(row);

        // 2. Снимаем основной чекбокс
        await row.Locator("mat-checkbox input").UncheckAsync();
    }


}
