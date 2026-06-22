using Microsoft.Playwright;
using Log = CareAdminTestProject.Common.TestLog;

/// <summary>
/// Represents the Incident Tracker dashboard page.
/// Serves as the primary entry point to manage repository lists, view grids, and initialize new form creation workflows.
/// </summary>
public class IncidentTrackerPage
{
    private readonly IPage _page; // Declare the core browser automation page field string reference

    /// <summary>
    /// Initializes a new instance of the <see cref="IncidentTrackerPage"/> class.
    /// </summary>
    /// <param name="page">The active thread-isolated Playwright page context instance forwarded from the test suite layers.</param>
    public IncidentTrackerPage(IPage page) // Received directly out of test runtime contexts
    {
        _page = page;
    }

    /// <summary>
    /// Locates the primary workflow creation action button on the dashboard repository layout and dispatches a click trigger event.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClickNewIncidentAsync()
    {
        Log.Debug("[NAVIGATION] Checking for active loading overlays before clicking 'New Incident'...");

        // 1. Расширяем селектор реальным лоадером ".loader-wrapper", который виден в логах Playwright
        // Иконка ромбика на скриншоте — это как раз элемент внутри этого контейнера
        var loader = _page.Locator(".loader-wrapper, kendo-loading, .k-loading-overlay, .mat-mdc-progress-bar").First;

        // Ждем пару мгновений (200-300мс), чтобы Angular успел вставить лоадер в DOM после перехода на страницу
        await Task.Delay(300);

        // Если лоадер появился на экране, железно ждем его гарантированного скрытия
        if (await loader.IsVisibleAsync())
        {
            Log.Debug("[NAVIGATION] Active spinner detected. Waiting for it to disappear...");
            try
            {
                // Ждем перехода элемента в состояние Hidden (исчезновение из DOM или invisible)
                await loader.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 30000 });
                Log.Debug("[NAVIGATION] Loading spinner state is cleared.");
            }
            catch (TimeoutException)
            {
                Log.Warning("[NAVIGATION] Spinner did not disappear within 30s, proceeding to action anyway.");
            }
        }
        else
        {
            Log.Debug("[NAVIGATION] No active loading spinner detected on page entry.");
        }

        // Дополнительная микро-пауза, чтобы анимация скрытия слоя полностью завершилась в браузере
        await Task.Delay(200);

        // 2. Теперь находим кнопку и кликаем по ней
        var newIncidentButton = _page.GetByRole(AriaRole.Button, new() { Name = "New Incident" });

        await newIncidentButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

        Log.Debug("[NAVIGATION] Attempting to click 'New Incident' button...");
        await newIncidentButton.ClickAsync();

        Log.Debug("[NAVIGATION] 'New Incident' button clicked successfully.");
    }
}
