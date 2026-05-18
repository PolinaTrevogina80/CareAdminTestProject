using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;
using static DetailsTab;
using static System.Net.WebRequestMethods;

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
        // Ищем контейнер по точному совпадению текста внутри блока с текстом
        var checkboxFieldContainer = Page.Locator(".checkbox-field:visible")
            .Filter(new() { Has = Page.Locator("span").GetByText(label, new() { Exact = true }) });

        // Спускаемся внутрь этого контейнера до нативного инпута для проверки состояния
        var nativeInput = checkboxFieldContainer.Locator("input[type='checkbox']");

        // Спускаемся до компонента mat-checkbox для совершения клика
        var matCheckbox = checkboxFieldContainer.Locator("mat-checkbox");

        // Проверяем текущее состояние нативного инпута
        bool currentState = await nativeInput.IsCheckedAsync();

        if (currentState != isChecked)
        {
            // Кликаем по mat-checkbox внутри найденного контейнера
            await matCheckbox.ClickAsync();
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


    public async Task VerifyDataFieldsAsync(IncidentStateInfo expected)
    {

        await Task.Delay(500); // Синхронизация с UI, как и в методе заполнения

        // 1. Communication/Mood Status (Чекбоксы)
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
            // Имя группы передаем как в коде, либо оставляем старое (метод отфильтрует по тексту)
            Assert.That(await IsRadioOptionSelectedAsync("ambulatoryStatus", expected.AmbulatoryStatus), Is.True,
                $"Радио-опция '{expected.AmbulatoryStatus}' должна быть выбрана.");
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
