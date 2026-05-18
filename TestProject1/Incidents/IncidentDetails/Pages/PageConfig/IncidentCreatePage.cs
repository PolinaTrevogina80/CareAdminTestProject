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


    private readonly string _createButtonSelector = "button#create-incident"; // Ваш селектор


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
        // Комбинированный локатор: ищет контейнер и в пустом, и в заполненном состоянии
        var lookupContainer = _page.Locator("cad-lookup-select, rnt-resident-lookup-select").First;

        // Находим строго активный mat-select внутри текущего контейнера
        var selectContainer = lookupContainer.Locator("mat-select:not(.mat-select-disabled)").First;
        var arrowTrigger = selectContainer.Locator(".mat-mdc-select-arrow-wrapper, .mat-select-arrow-wrapper");
        var dropdownPanel = _page.Locator(".cdk-overlay-container");

        // Ждем, пока селектор физически появится в DOM и станет видимым
        await selectContainer.ScrollIntoViewIfNeededAsync();
        await selectContainer.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            Log.Debug($"[Попытка {attempt}/{maxRetries}] Открываем селект резидентов...");

            try
            {
                var isExpanded = await selectContainer.GetAttributeAsync("aria-expanded");
                if (isExpanded != "true")
                {
                    await arrowTrigger.ClickAsync(new() { Force = true });
                }

                await dropdownPanel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                var options = dropdownPanel.Locator("mat-option, mat-mdc-option");
                await options.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

                var targetOption = options.Nth(index);
                var fullText = await targetOption.InnerTextAsync();
                var parts = fullText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                var expectedName = parts.FirstOrDefault() ?? "";

                Log.Debug($"Кликаем по резиденту '{expectedName}'...");
                await targetOption.ClickAsync(new() { Force = true });

                // Ждем закрытия шторки дропдауна
                await dropdownPanel.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });

                // ЖЕСТКИЙ ТАЙМАУТ: Даем Angular время полностью перестроить DOM и подгрузить карточку
                Log.Debug("Ожидаем завершения анимации и рендеринга Angular...");
                await Task.Delay(2000);

                Log.Debug("Ожидаем появление карточки резидента с MRN и Gender...");

                // Ищем элементы на странице независимо, без привязки к старому lookupContainer
                var mrnField = _page.Locator("cad-label-value-field[label='MRN']").First;
                var genderField = _page.Locator("cad-label-value-field[label='Gender']").First;

                // Проверяем видимость новых элементов формы инцидента
                await Assertions.Expect(mrnField).ToBeVisibleAsync(new() { Timeout = 10000 });
                await Assertions.Expect(genderField).ToBeVisibleAsync(new() { Timeout = 3000 });

                return new ResidentInfo(
                    Name: expectedName,
                    Room: parts.ElementAtOrDefault(1) ?? "",
                    Bed: parts.ElementAtOrDefault(2) ?? ""
                );
            }
            catch (Exception ex)
            {
                Log.Warning($"Попытка {attempt} не удалась. Ошибка: {ex.Message}");

                if (await dropdownPanel.IsVisibleAsync())
                {
                    await _page.Keyboard.PressAsync("Escape");
                    await dropdownPanel.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 3000 });
                }

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

    public async Task<ILocator> GetCreateIncidentButtonLocator()
    {
        return _page.GetByRole(AriaRole.Button, new() { Name = "Create" });
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