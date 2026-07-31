using CareAdminTestProject.Incidents.IncidentTracker.PageConfig.Components;
using Microsoft.Playwright;
using UglyToad.PdfPig.Content;
using Log = CareAdminTestProject.Common.TestLog;

/// <summary>
/// Represents the Incident Tracker dashboard page.
/// Serves as the primary entry point to manage repository lists, view grids, and initialize new form creation workflows.
/// </summary>
public class IncidentTrackerPage
{
    protected readonly IPage _page;


    // Глобальная шапка (видна на всех вкладках)
    // Находим первый инпут внутри контейнера диапазона дат (Start Date)
    public ILocator StartDateInput =>
        _page.Locator("cad-select-date-range .range-wrapper input").First;

    // Находим второй инпут внутри контейнера диапазона дат (End Date)
    public ILocator EndDateInput =>
        _page.Locator("cad-select-date-range .range-wrapper input").Nth(1);

    // Кнопка GO (находим по тексту)
    public ILocator GoButton =>
        _page.GetByRole(AriaRole.Button, new() { Name = "GO", Exact = true });

    // Блок статистики (справа в шапке)
    public ILocator IncidentsCount => _page.Locator("text=/Incidents: \\d+/");
    public ILocator ResidentsCount => _page.Locator("text=/Residents: \\d+/");

    // Вкладки трекера
    public ILocator DetailedViewTabButton => _page.GetByRole(AriaRole.Tab, new() { Name = "Detailed View" });
    public ILocator CompletionViewTabButton => _page.GetByRole(AriaRole.Tab, new() { Name = "Completion View" });
    public ILocator ReferralsTabButton => _page.GetByRole(AriaRole.Tab, new() { Name = "Referrals" });

    // Компонент первой вкладки (Detailed View)
    private bool _isPageLoaded = false; // Флаг, чтобы не ждать лоадер дважды

    private DetailedViewTab? _detailedView;
    private CompletionViewTab? _completionView;
    private ReferralsTab? _referral;

    public DetailedViewTab DetailedView => _detailedView ??= new DetailedViewTab(_page);
    public CompletionViewTab CompletionView => _completionView ??= new CompletionViewTab(_page);
    public ReferralsTab Referral => _referral ??= new ReferralsTab(_page);

    public IncidentTrackerPage(IPage page)
    {
        _page = page;
    }

    /// <summary>
    /// Гарантированно дожидается полной загрузки страницы трекера и исчезновения стартовых лоадеров.
    /// </summary>
    public async Task WaitForPageLoadAsync()
    {
        if (_isPageLoaded) return;

        Log.Debug("[NAVIGATION] Waiting for Incident Tracker page initialization and overlays...");

        var loader = _page.Locator(".loader-wrapper, kendo-loading, .k-loading-overlay, .mat-mdc-progress-bar").First;

        // Ждем инициализации Angular-компонентов в DOM
        await Task.Delay(300);

        if (await loader.IsVisibleAsync())
        {
            Log.Debug("[NAVIGATION] Active spinner detected on page entry. Waiting for it to disappear...");
            try
            {
                await loader.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 30000 });
                Log.Debug("[NAVIGATION] Loading spinner state is cleared.");
            }
            catch (TimeoutException)
            {
                Log.Warning("[NAVIGATION] Spinner did not disappear within 30s, proceeding anyway.");
            }
        }
        else
        {
            Log.Debug("[NAVIGATION] No active loading spinner detected on page entry.");
        }

        await Task.Delay(200);
        _isPageLoaded = true; // Запоминаем, что страница готова
    }

    /// <summary>
    /// Нажимает кнопку создания нового инцидента. Безопасно вызывает ожидание загрузки для старых тестов.
    /// </summary>
    public async Task ClickNewIncidentAsync()
    {
        // 1. Сначала дожидаемся, что тяжелый спиннер загрузки трекера полностью ушел с экрана
        var trackerSpinner = _page.Locator("div.loader-wrapper");
        if (await trackerSpinner.CountAsync() > 0)
        {
            Log.Debug("[NAVIGATION] Waiting for Tracker page loading spinner to disappear...");
            try
            {
                await trackerSpinner.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });
            }
            catch (TimeoutException)
            {
                Log.Warning("[NAVIGATION] Spinner did not hide in 15s, attempting to proceed...");
            }
        }

        // 2. Находим кнопку и ждем, пока она станет полностью видимой в DOM
        var newIncidentButton = _page.GetByRole(AriaRole.Button, new() { Name = "New Incident" });
        await newIncidentButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 100000 });

        // 3. Важнейшая пауза: даем Angular 500 миллисекунд, чтобы привязать обработчик событий клика к кнопке
        await _page.WaitForTimeoutAsync(500);

        Log.Debug("[NAVIGATION] Clicking 'New Incident' button smoothly...");

        // 4. Кликаем ОБЫЧНЫМ кликом (без Force!). 
        // Playwright сам проверит, что кнопка активна, стабильна и не перекрыта оверлеями.
        await newIncidentButton.ClickAsync();

        Log.Debug("[NAVIGATION] 'New Incident' button clicked successfully.");
    }

    /// <summary>
    /// Сбрасывает флаг загрузки страницы (вызывать при переходах на другие разделы или полной перезагрузке)
    /// </summary>
    public void ResetLoadState()
    {
        _isPageLoaded = false;
    }
}
