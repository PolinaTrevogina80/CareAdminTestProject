using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;

public class MedicationTab : BaseIncidentTabs
{
    // Описываем структуру данных для одной строки лекарства
    public record MedicationInfo(
        string Name,
        string Dosage,
        string Frequency,
        string TimeReceived
    );

    public MedicationTab(IPage page) : base(page) { }



    public async Task FillMedicationTabAsync(List<MedicationInfo> medications)
    {
        for (int i = 0; i < medications.Count; i++)
        {
            // 1. Нажимаем кнопку добавления новой строки
            await GetButtonByText("Add Medication").ClickAsync();

            var medication = medications[i];

            // 2. Находим i-ю строку данных (пропускаем заголовок)
            // На скриншоте видно класс "medication-row ng-star-inserted" для строк с данными
            var row = Page.Locator(".medication-row.ng-star-inserted").Nth(i);

            // 3. Заполняем поля внутри этой конкретной строки
            // Используем плейсхолдеры или классы колонок, если лейблов нет
            await row.Locator("input").Nth(0).FillAsync(medication.Name);
            await row.Locator("input").Nth(1).FillAsync(medication.Dosage);
            await row.Locator("input").Nth(2).FillAsync(medication.Frequency);
            await row.Locator("input").Nth(3).FillAsync(medication.TimeReceived);

        }
    }
}