using CareAdminTestProject.Incidents.IncidentDetails.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using TestProject1.Common;
using static IncidentDataFactory;
using static System.Net.Mime.MediaTypeNames;

[TestFixture]
public class BaseIncidentTests : BaseTest
{

    public IncidentDetailsSteps steps;
    public IncidentTestData data;
    public IncidentCreatePage.ResidentInfo resident;

    [SetUp]
    public async Task Setup()
    {
        Log.LogDebug($"Make Setup, switch to Carrilon");

        await EnsureFacilitySelected("Carillon");

        steps = new IncidentDetailsSteps(Page);
        await steps.NavigateToTrackerViaMenu();
        await steps.OpenNewIncidentAsync();
        resident = await steps.SelectResidentAsync(1);
        data = IncidentDataFactory.CreateDefaultFall(resident);
    }

    [Test]
    public async Task ShouldOpenIncidentDashboardFromHomePage()
    {
        // Вы уже авторизованы! Переходим сразу к делу
        Log.LogDebug("Open HOME page");
        await Page.GotoAsync("/");

        Log.LogDebug("Open A/I Page");

        await Page.GetByAltText("Accident/Incident").ClickAsync();

        Log.LogDebug("Check Main Dashboard is opened");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*dashboards/incident-main"));
        await Expect(Page.Locator("cad-breadcrumb").GetByText("Main Dashboard")).ToBeVisibleAsync();
    }

    [Test]
    public async Task SwitchToCarrillonTest()
    {
        // 1. ПРЕДУСЛОВИЕ: Убеждаемся, что изначально выбрано Cassena Care
        // (Это гарантирует, что тесту точно ПРИДЕТСЯ переключать значение)
        //await EnsureCassenaCareSelected();
        var facility = "Carillon";

        // 2. ДЕЙСТВИЕ: Переключаем на Carillon
        await SelectInTreeAsync(facility);

        // 3. ПРОВЕРКА: Значение действительно изменилось
        await Expect(Page.Locator(".k-input-value-text").First)
            .ToContainTextAsync(facility);
        Log.LogDebug("Switching to Carillon successful");

        await GetCurrentSelectionAsync();
    }

    [Test]
    public async Task SwitchToCassenaCareTest()
    {

        // Переключаем обратно на Cassena Care
        await SelectInTreeAsync("Cassena Care");

        // ПРОВЕРКА: Значение изменилось на Cassena Care
        await Expect(Page.Locator(".k-input-value-text").First)
            .ToContainTextAsync("Cassena Care");
        Log.LogInformation("Switching to Cassena Care successful");

        await GetCurrentSelectionAsync();
    }

    public async Task SelectFacilityAsync(string facilityName)
    {
        await Page.GotoAsync("/accident-incident/dashboards/incident-main");
        var dropdown = Page.Locator("kendo-dropdowntree.k-dropdowntree").First;

        // Используем ваш проверенный способ с Evaluate
        await dropdown.EvaluateAsync("el => el.click()");

        var option = Page.Locator(".k-popup .k-treeview-leaf, .k-animation-container .k-treeview-leaf")
                         .GetByText(facilityName, new() { Exact = true });

        await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await option.ClickAsync();
        Log.LogInformation($"Switching to {facilityName} successful");

        // Ждем обновления текста в селекторе
        await Expect(Page.Locator(".k-input-value-text").First).ToContainTextAsync(facilityName);
    }

    public async Task SelectInTreeAsync(string targetName)
    {
        await Page.GotoAsync("/accident-incident/dashboards/incident-main");

        var dropdown = Page.Locator("kendo-dropdowntree.k-dropdowntree").First;
        await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Attached });

        // Открываем список через JS (самый стабильный ваш метод)
        await dropdown.EvaluateAsync("el => el.click()");

        // Ищем пункт в выпадающем окне
        var option = Page.Locator(".k-popup .k-treeview-leaf, .k-animation-container .k-treeview-leaf")
                         .GetByText(targetName, new() { Exact = true });

        await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await option.ClickAsync();


        // Проверяем, что текст в селекторе изменился
        await Expect(Page.Locator(".k-input-value-text").First)
            .ToContainTextAsync(targetName, new() { Timeout = 10000 });

        Log.LogInformation($"{targetName} selected");
    }

    public async Task EnsureCassenaCareSelected()
    {
        const string target = "Cassena Care";
        if (await GetCurrentSelectionAsync() != target)
        {
            Log.LogDebug($"[SETUP] Переключаем на группу: {target}");
            await SelectInTreeAsync(target);
        }
    }

    public async Task EnsureFacilitySelected(string facilityName)
    {
        await Page.GotoAsync("/accident-incident/dashboards/incident-main");
        var currentText = await Page.Locator(".k-input-value-text").First.TextContentAsync();

        if (currentText?.Trim() != facilityName)
        {
            Log.LogDebug($"Контекст - Кассена Кэр");
            Log.LogDebug($"[SETUP] Переключаем учреждение на {facilityName}");
            await SelectInTreeAsync(facilityName);
            await GetCurrentSelectionAsync();

        }
    }


    private async Task<string> GetCurrentSelectionAsync()
    {
       // await Page.GotoAsync("/accident-incident/dashboards/incident-main");
        var text = await Page.Locator(".k-input-value-text").First.TextContentAsync();
        Log.LogDebug($"Сейчас контекст: {text}");

        return text?.Trim() ?? string.Empty;
    }
}

