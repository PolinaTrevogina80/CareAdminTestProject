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

    public async Task<ResidentInfo> SelectResidentAsyncByInd(int index)
    {
        // 1. Открываем список
        var residentField = _page.GetByLabel("Resident");
        await residentField.ClickAsync();

        // 2. Ждем появления контейнера
        Log.Debug("Находим дропдаун");

        var overlay = _page.Locator(".mat-mdc-select-trigger");
        await overlay.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });

        // 3. Находим нужную опцию
        Log.Debug("Дропдаун нашли, ищем резидента по индексу");
        Log.Debug("Ищем резидента по индексу через Role");

        var targetOption = _page.GetByRole(AriaRole.Option).Nth(index);
        await targetOption.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });


        // 4. ИЗВЛЕКАЕМ ТЕКСТ (сохраняем твои Room и Bed)
        var fullText = await targetOption.InnerTextAsync();
        var parts = fullText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim()).ToArray();

        // 5. КЛИКАЕМ по опции
        // 5. ФОКУСИРУЕМСЯ И НАЖИМАЕМ ENTER (имитация реального выбора пользователя)
        // Альтернативный вариант (JS-клик, который точно дернет Angular-биндеры)
        await targetOption.DispatchEventAsync("click");
        Log.Debug("Резидента выбрали через DispatchEvent");
        //Log.Debug("Резидента нашли и кликнули");

        // --- ДОБАВЛЯЕМ "ДОЖИМ", ЧТОБЫ ФОРМА ПРОСНУЛАСЬ ---
        await Task.Delay(500); // Даем время на срабатывание клика

        // Если список не закрылся сам, кликаем по полю еще раз и жмем Enter

        if (await targetOption.IsVisibleAsync())
        {
            Log.Debug("Дропдаун сам не закрылся, пробуем закрыть по ентеру");

            await residentField.ClickAsync(new() { Force = true });
            await _page.Keyboard.PressAsync("Enter");
        }

        // 6. ЖДЕМ ЗАКРЫТИЯ ПАНЕЛИ (используем класс выпадающего списка, а не кнопки)
        var dropdownPanel = _page.Locator(".mat-mdc-select-panel");
        await dropdownPanel.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });
        Log.Debug("Дропдаун закрылся");

        // 7. ОЖИДАНИЕ ОБНОВЛЕНИЯ ФОРМЫ
        // Так как вкладки появились, лучше привязаться к появлению контейнера вкладок или формы
        Log.Debug("Ждем активации вкладок формы...");
        await _page.Locator(".mat-mdc-tab-body-wrapper, [role='tabpanel']").First.WaitForAsync(new() { Timeout = 15000 });

        // 8. ВОЗВРАЩАЕМ ОБЪЕКТ (твоя логика сохранена)
        return new ResidentInfo(
            Name: parts.ElementAtOrDefault(0) ?? "",
            Room: parts.ElementAtOrDefault(1) ?? "",
            Bed: parts.ElementAtOrDefault(2) ?? ""
        );
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