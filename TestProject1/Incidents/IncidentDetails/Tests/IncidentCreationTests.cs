using CareAdminTestProject.Incidents.IncidentDetails.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab;
using static GeneralTab;
using static IncidentCreatePage;
using static System.Net.Mime.MediaTypeNames;

[TestFixture]
public class IncidentTests : BaseIncidentTests
{

    [Test]
    public async Task CreateIncident_StartWithResidentSelection()
    {
        await steps.FillGeneralTabAsync(data);
        await steps.ClickCreateIncidentAsync();

        await steps.FillDetailsTabAsync(data);
        await steps.FillStateTabAsync(data);
        await steps.FillMedicationTabAsync(data);
        await steps.FillRNFormTabAsync(data);
        await steps.FillSummaryTabAsync(data);

        // 6. Сохранение и подписание
        await steps.ClickSaveIncidentAsync();
        await steps.SignSummaryAndVerifyAsync();
        await steps.ClickSaveIncidentAsync(true);

        // 7. добавление аттачей
        await steps.UploadAttachmentTabAsync("Other", "This is a test note");
        //await steps.SaveIncidentAsync();

    }
    [Test]
    public async Task CreateIncident_WithOnlyRequiredFields_ShouldPass()
    {
        // Вырезаем все лишнее из General, оставляя только поля со звездочкой
        var minimalGeneral = data.General.GetOnlyRequiredFields();
        var minimalData = data with { General = minimalGeneral };

        await steps.FillGeneralTabAsync(minimalData);
        await steps.ClickCreateIncidentAsync();

    }

    // 2. Негативный тест: Поочередно очищаем каждое обязательное поле
    [TestCase("Room")]
    [TestCase("Bed")]
    [TestCase("Date")]
    [TestCase("Time")]
    [TestCase("Location")]
    [TestCase("Type")]
    public async Task CreateIncident_MissingRequiredField_ShouldShowValidationError(string missingField)
    {
        //Очищаем форму
        await steps.ClearGeneralForm();

        // 1. Берем чистый обязательный набор данных
        var baseRequiredGeneral = data.General.GetOnlyRequiredFields();

        // 2. Модифицируем конкретное поле под пустое значение
        var invalidGeneral = missingField switch
        {
            "Room" => baseRequiredGeneral with { room = null },
            "Bed" => baseRequiredGeneral with { bed = null },
            "Date" => baseRequiredGeneral with { date = null },     // Не заполнит дату
            "Time" => baseRequiredGeneral with { time = null },     // Не заполнит время
            "Location" => baseRequiredGeneral with { location = null },
            "Type" => baseRequiredGeneral with { type = null },
            _ => throw new ArgumentException($"Unknown field: {missingField}")
        };

        // 3. Собираем IncidentTestData с инвалидной вкладкой General
        var invalidData = data with { General = invalidGeneral };

        // 4. Заполняем форму некорректными данными
        await steps.FillGeneralTabAsync(invalidData);

        // 5. Проверяем, что кнопка Создать заблокирована (Вариант А)
        await steps.VerifyCreateButtonIsDisabledAsync();

        // Либо Вариант Б:
        // await steps.ClickCreateIncidentAsync(shouldBeEnabled: false);
    }


    [Test]
    public async Task CreateIncident_RequieredFields_ShouldRetainDataAfterReload()
    {
        // 1. Берем только обязательные поля и собираем структуру данных
        var requiredGeneral = data.General.GetOnlyRequiredFields();
        var partialData = data with { General = requiredGeneral };

        // 2. Заполняем форму на вкладке General
        await steps.FillGeneralTabAsync(partialData);

        // 3. Сохраняем
        await steps.ClickCreateIncidentAsync();
        await Task.Delay(1000);

        // 4. Получаем URL сохраненного инцидента
        string draftUrl = await steps.GetCurrentUrlAsync();

        // 5. Перезагружаем страницу по сохраненному URL
        await steps.ReloadPageAndNavigateAsync(draftUrl);

        // 6. Проверяем, что все данные успешно восстановились из черновика
        await steps.VerifyDataRetainedAsync(requiredGeneral);
    }

    [Test]
    public async Task CreateIncident_FullGeneral_ShouldRetainDataAfterReload()
    {
        // 2. Заполняем форму на вкладке General
        await steps.FillGeneralTabAsync(data);

        // 3. Сохраняем
        await steps.ClickCreateIncidentAsync();
        await Task.Delay(1000);

        // 4. Получаем URL сохраненного инцидента
        string draftUrl = await steps.GetCurrentUrlAsync();

        // 5. Перезагружаем страницу по сохраненному URL
        await steps.ReloadPageAndNavigateAsync(draftUrl);

        // 6. Проверяем, что все данные успешно восстановились из черновика
        await steps.VerifyDataRetainedAsync(data.General);
    }

    [Test]
    public async Task CreateIncident_FullIncident_ShouldRetainDataAfterReload()
    {
        // 2. Заполняем форму на вкладке General
        await steps.FillGeneralTabAsync(data);
        await steps.FillDetailsTabAsync(data);
        await steps.FillStateTabAsync(data);
        await steps.FillMedicationTabAsync(data);
        await steps.FillRNFormTabAsync(data);
        await steps.FillSummaryTabAsync(data);
        // 3. Сохраняем
        await steps.ClickCreateIncidentAsync();
        await Task.Delay(1000);

        // 4. Получаем URL сохраненного инцидента
        string draftUrl = await steps.GetCurrentUrlAsync();

        // 5. Перезагружаем страницу по сохраненному URL
        await steps.ReloadPageAndNavigateAsync(draftUrl);

        // 6. Проверяем, что все данные успешно восстановились из черновика
        await steps.VerifyDataRetainedAsync(data.General);
        await steps.VerifyDataRetainedAsync(data.Details);
        await steps.VerifyDataRetainedAsync(data.State);
        await steps.VerifyDataRetainedAsync(data.Medications);
        await steps.VerifyDataRetainedAsync(data.RNSupervisor);
        await steps.VerifyDataRetainedAsync(data.Summary);
    }

    [Test]
    public async Task EditIncident_OverwriteWithNewDataSet_ShouldRetainUpdatedData()
    {
        // --- ПОДГОТОВКА ДАННЫХ ---
        // Набор 1 (Берем базовый из фабрики)
        var data1 = IncidentDataFactory.CreateDefaultFall(resident);

        // Набор 2 (Копируем Набор 1, изменяя только те поля, которые хотим перезаписать)
        var data2 = data1 with
        {
            // Перезаписываем данные на вкладке RNSupervisor
            RNSupervisor = data1.RNSupervisor with
            {
                Locations = new[] { "Lobby" }, // Было: "ADL Suite", "Dining Room"
                LastSeen = new RNSupervisorTabInfo.LastSeenInfo(
                    Time: new TimeOnly(14, 30), // Было: 09:00
                    Details: "Resident was walking down the hallway safely."
                )
            },
            // Если нужно, можно точечно поменять что-то еще, например, описание происшествия
            General = data1.General with { summary = "Updated summary after second investigation" }
        };

        // --- ШАГ 1: ЗАПОЛНЕНИЕ НАБОРОМ ДАННЫХ 1 ---
        await steps.FillGeneralTabAsync(data1);
        await steps.FillDetailsTabAsync(data1);
        await steps.FillStateTabAsync(data1);
        await steps.FillMedicationTabAsync(data1);
        await steps.FillRNFormTabAsync(data1);
        await steps.FillSummaryTabAsync(data1);

        // Сохраняем первую итерацию
        await steps.ClickCreateIncidentAsync();
        await Task.Delay(1000);
        string draftUrl = await steps.GetCurrentUrlAsync();

        // --- ШАГ 2: ПЕРЕОТКРЫТИЕ И ПЕРЕЗАПИСЬ НАБОРОМ ДАННЫХ 2 ---
        // Возвращаемся в этот же инцидент по URL
        await steps.ReloadPageAndNavigateAsync(draftUrl);

        // Поверх старых данных вводим новые значения из data2
        // Примечание: ваши методы Fill...TabAsync должны уметь очищать/перезаписывать инпуты
        await steps.FillGeneralTabAsync(data2);
        await steps.FillRNFormTabAsync(data2);

        // Снова сохраняем изменения
        await steps.ClickSaveIncidentAsync();
        await Task.Delay(1000);

        // --- ШАГ 3: ПЕРЕЗАГРУЗКА И ФИНАЛЬНАЯ ПРОВЕРКА (Ожидаем Набор 2) ---
        await steps.ReloadPageAndNavigateAsync(draftUrl);

        // Проверяем измененные вкладки по объекту data2
        await steps.VerifyDataRetainedAsync(data2.General);
        await steps.VerifyDataRetainedAsync(data2.RNSupervisor);

        // Вкладки, которые мы не трогали на Шаге 2, сверяем по data1 (или data2, так как они идентичны)
        await steps.VerifyDataRetainedAsync(data1.Details);
        await steps.VerifyDataRetainedAsync(data1.State);
        await steps.VerifyDataRetainedAsync(data1.Medications);
        await steps.VerifyDataRetainedAsync(data1.Summary);
    }


    // Перечисляем имена вкладок или их порядковые номера (индексы) для проверки
    [TestCase("General")]
    [TestCase("Details")]
    [TestCase("State")]
    [TestCase("Medication")]
    [TestCase("RN Supervisor Investigation Form")]
    [TestCase("Summary")]
    public async Task EveryTab_WhenDataChanged_ShouldTriggerChangeDetectionOnMenuLeave(string tabName)
    {
        // --- ШАГ 1: ЗАПОЛНЕНИЕ НАБОРОМ ДАННЫХ 1 ---
        await steps.FillGeneralTabAsync(data);
        await steps.FillDetailsTabAsync(data);
        await steps.FillStateTabAsync(data);
        await steps.FillMedicationTabAsync(data);
        await steps.FillRNFormTabAsync(data);
        await steps.FillSummaryTabAsync(data);
        await steps.ClickCreateIncidentAsync();
        await Task.Delay(1000);
        string draftUrl = await steps.GetCurrentUrlAsync();
        await steps.ReloadPageAndNavigateAsync(draftUrl);
        await steps.SwitchToTab(tabName);
        await steps.ModifySingleFieldOnTabAsync(tabName);
        await steps.LeavePageViaMenuAsync();
        await steps.VerifyUnsavedChangesAlertVisibleAsync(tabName);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Discard Changes" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await steps.ReloadPageAndNavigateAsync(draftUrl);
    }

}