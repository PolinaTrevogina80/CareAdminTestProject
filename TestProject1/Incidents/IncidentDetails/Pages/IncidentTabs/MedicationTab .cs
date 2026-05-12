using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;

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
        Log.Debug("[MEDICATION_TAB] Запуск лекарств в таблицу...");

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
            Log.Debug($"[MEDICATION_TAB] строчка добавлена #{i + 1}");


        }
    }

    public async Task ClearAllMedicationsAsync()
    {
        Log.Debug("[MEDICATION_TAB] Запуск удаления всех лекарств из таблицы...");

        // Локатор для первой кнопки Delete в таблице
        var firstDeleteButton = Page.Locator(".medication-row.ng-star-inserted").First.GetByRole(AriaRole.Button, new() { Name = "Delete" });

        // Альтернативный селектор, если GetByRole не сработает (на основе скриншота):
        // var firstDeleteButton = Page.Locator(".medication-row.ng-star-inserted").First.Locator("button:has-text('Delete')");

        int deletedCount = 0;

        // Цикл выполняется, пока на странице видна хотя бы одна кнопка удаления в строке данных
        while (await firstDeleteButton.IsVisibleAsync())
        {
            Log.Debug($"[MEDICATION_TAB] Удаление строки #{deletedCount + 1}");

            // Кликаем по кнопке удаления первой строки
            await firstDeleteButton.ClickAsync();
            deletedCount++;

            // Небольшая задержка, чтобы Angular успел удалить строку из DOM и пересчитать индексы
            await Page.WaitForTimeoutAsync(150);
        }

        Log.Debug($"[MEDICATION_TAB] Удаление завершено. Всего удалено строк: {deletedCount}");
    }


    public async Task AddEmptyMedicationRowsAsync(int count)
    {
        Log.Debug($"[MEDICATION_TAB] Добавление {count} пустых строк лекарств...");

        for (int i = 0; i < count; i++)
        {
            await GetButtonByText("Add Medication").ClickAsync();

            // Ждем анимацию появления новой строки, чтобы клики не слипались
            await Page.Locator(".medication-row.ng-star-inserted").Nth(i).WaitForAsync(new() { State = WaitForSelectorState.Visible });
        }

        Log.Debug("[MEDICATION_TAB] Пустые строки успешно сгенерированы");
    }

}