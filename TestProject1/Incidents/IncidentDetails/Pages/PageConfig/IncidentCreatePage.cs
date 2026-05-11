using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;
using System.IO;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.BaseIncidentTabs;
using static System.Net.Mime.MediaTypeNames;

public class IncidentCreatePage : BaseIncidentTabs
{
    private readonly IPage _page;
    // Ссылка на вкладку как часть страницы
    public record ResidentInfo(string Name, string Room, string Bed);

    public GeneralTab General { get; }
    public DetailsTab Details{ get; }
    public StateTab State { get; }
    public MedicationTab Medication { get; }
    public RNSupervisorTab RNSupervisor { get; }
    public SummaryTab Summary { get; }
    public AttachmentsTab Attachments { get; }


    public IncidentCreatePage(IPage page) : base(page)
    {
        _page = page;
        // Инициализируем вкладки, передавая ей ту же страницу браузера
        General = new GeneralTab(page);
        Details = new DetailsTab(page);
        State = new StateTab(page);
        Medication = new MedicationTab(page);
        RNSupervisor = new RNSupervisorTab(page);
        Summary = new SummaryTab(page);
        Attachments = new AttachmentsTab(page);
    }

    public async Task SelectResidentAsyncByName(string residentName)
    {
        // Кликаем по полю выбора резидента (например, mat-select или input)
        await _page.GetByLabel("Resident").ClickAsync();

        // Выбираем первого резидента из списка
        await _page.Locator(".cdk-overlay-container")
                   .GetByText(residentName, new() { Exact = true })
                   .ClickAsync();
    }

    public async Task<ResidentInfo> SelectResidentAsyncByInd(int index, int maxRetries = 3)
    {
        var residentField = _page.Locator("mat-select, .mat-mdc-select").First;
        var dropdownPanel = _page.Locator(".mat-mdc-select-panel, .mat-select-panel").First;

        // Ждем, пока прекратятся фоновые сетевые запросы Angular, чтобы UI не тормозил
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            Log.Debug($"[Попытка {attempt}/{maxRetries}] Открываем селект резидентов...");

            try
            {
                await residentField.ScrollIntoViewIfNeededAsync();
                await residentField.FocusAsync();
                await Task.Delay(300); // Даем фокусу зафиксироваться
                await _page.Keyboard.PressAsync("Space");

                // Увеличиваем таймаут ожидания панели до 8 секунд
                await dropdownPanel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
                await Task.Delay(500);

                var targetOption = dropdownPanel.GetByRole(AriaRole.Option).Nth(index);
                await targetOption.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

                var fullText = await targetOption.InnerTextAsync();
                var parts = fullText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                var expectedName = parts.FirstOrDefault() ?? "";

                Log.Debug($"Выбираем резидента '{expectedName}' стрелками...");

                for (int i = 0; i < index; i++)
                {
                    await _page.Keyboard.PressAsync("ArrowDown");
                    await Task.Delay(150);
                }

                await _page.Keyboard.PressAsync("Enter");
                await dropdownPanel.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });

                await _page.Keyboard.PressAsync("Tab");

                Log.Debug("Проверяем появление текстовых блоков MRN и Gender на странице...");

                // Ищем текст MRN и Gender глобально по всей странице, так как они находятся выше тега <form>
                await Assertions.Expect(_page.Locator("body")).ToContainTextAsync("MRN", new() { Timeout = 10000 });
                await Assertions.Expect(_page.Locator("body")).ToContainTextAsync("Gender", new() { Timeout = 3000 });
                await Assertions.Expect(_page.Locator("body")).ToContainTextAsync(expectedName, new() { Timeout = 3000 });

                Log.Debug("Форма успешно подгрузила данные резидента!");

                return new ResidentInfo(
                    Name: expectedName,
                    Room: parts.ElementAtOrDefault(1) ?? "",
                    Bed: parts.ElementAtOrDefault(2) ?? ""
                );
            }
            catch (Exception ex)
            {
                Log.Warning($"Попытка {attempt} не удалась. Ошибка: {ex.Message}");

                await _page.Keyboard.PressAsync("Escape");
                await Task.Delay(2000);
            }
        }

        throw new Exception($"Не удалось подтвердить выбор резидента и загрузку его метаданных за {maxRetries} попыток.");
    }


    public async Task ClickTabAsync(string tabName, LocatorClickOptions options = null)
    {
        // Находим вкладку (tab) по её текстовому названию и кликаем
        // Метод автоматически дождется появления элемента и его готовности к клику
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle); // Ждем затишья сетевой активности
        await _page.GetByRole(AriaRole.Tab, new() { Name = tabName, Exact = false }).ClickAsync();
    }

    public async Task ClickCreateIncident()
    {
        // 1. Нажимаем на кнопку "Create"
        // Используем GetByRole, так как это наиболее стабильный способ для Material Design кнопок
        await _page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();

        // 2. Ждем, пока сеть "успокоится" (отсутствие активных запросов в течение 500 мс)
        // Это полезно, если после нажатия идет сохранение данных
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForTimeoutAsync(1500);

        Log.Information($"Текущий URL после создания: {_page.Url}");

    }

    public async Task ClickSaveIncident()
    {
        // 1. Нажимаем на кнопку "Save"
        // Используем GetByRole, так как это наиболее стабильный способ для Material Design кнопок
        var saveButton = _page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true })
                         .Filter(new() { Visible = true });
        // Проверяем, что кнопка есть в DOM и видна, прежде чем что-то с ней делать
        await saveButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await _page.Keyboard.PressAsync("Tab"); // Уходим из последнего поля
        await saveButton.FocusAsync(); // Фокусируемся на кнопке
        await saveButton.ClickAsync();

        // 2. Ждем, пока сеть "успокоится" (отсутствие активных запросов в течение 500 мс)
        // Это полезно, если после нажатия идет сохранение данных
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForTimeoutAsync(1500);

        Log.Information($"Текущий URL после сохранения: {_page.Url}");
    }
}