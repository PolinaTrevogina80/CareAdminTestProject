using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;
using System.IO;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.BaseIncidentTabs;
using static System.Net.Mime.MediaTypeNames;
using Log = CareAdminTestProject.Common.TestLog;


/// <summary>
/// Represents the primary Incident Creation page.
/// Manages initial incident workspace activation, multi-tab routing, and sophisticated resilient resident selection.
/// </summary>
public class IncidentCreatePage : BaseIncidentTabs
{
    private readonly IPage _page;

    /// <summary>
    /// Encapsulates biographical and location metadata captured for a chosen resident.
    /// </summary>
    public record ResidentInfo(string Name, string Room, string Bed);

    /// <summary> Gets the operational controller instance for the General tab form fields. </summary>
    public GeneralTab General { get; }

    /// <summary> Gets the operational controller instance for the Details tab form fields. </summary>
    public DetailsTab Details { get; }

    /// <summary> Gets the operational controller instance for the State tab form fields. </summary>
    public StateTab State { get; }

    /// <summary> Gets the operational controller instance for the Medication tab form fields. </summary>
    public MedicationTab Medication { get; }

    /// <summary> Gets the operational controller instance for the RN/Supervisor questionnaire tab. </summary>
    public RNSupervisorTab RNSupervisor { get; }

    /// <summary> Gets the operational controller instance for the Summary tab conclusions and approvals. </summary>
    public SummaryTab Summary { get; }

    /// <summary> Gets the operational controller instance for the Attachments tab file streaming layout. </summary>
    public AttachmentsTab Attachments { get; }

    private readonly string _createButtonSelector = "button#create-incident"; // Your target selector framework identifier

    /// <summary>
    /// Initializes a new instance of the <see cref="IncidentCreatePage"/> class and compositionally spawns internal tab controllers.
    /// </summary>
    /// <param name="page">The current runtime Playwright page context instance.</param>
    public IncidentCreatePage(IPage page) : base(page)
    {
        _page = page;
        // Initialize individual form tabs, forwarding the identical shared browser automation page context reference
        General = new GeneralTab(page);
        Details = new DetailsTab(page);
        State = new StateTab(page);
        Medication = new MedicationTab(page);
        RNSupervisor = new RNSupervisorTab(page);
        Summary = new SummaryTab(page);
        Attachments = new AttachmentsTab(page);
    }

    /// <summary>
    /// Performs direct target clicking to choose a matching resident profile out of overlay list grids using an exact string name lookup criteria.
    /// </summary>
    /// <param name="residentName">The complete literal string name of the target resident profile.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SelectResidentAsyncByName(string residentName)
    {
        // Click the root selection area wrapper control assigned to the Resident label context
        await _page.GetByLabel("Resident").ClickAsync();

        // Isolate the matching record card option directly inside the newly spawned viewport dropdown container layer
        await _page.Locator(".cdk-overlay-container")
                   .GetByText(residentName, new() { Exact = true })
                   .ClickAsync();
    }

    /// <summary>
    /// Executes a highly robust, multi-retry synchronization pipeline to choose a resident profile by index, 
    /// mitigating UI timing races by tracking explicit card transformations and post-rendering Angular hooks.
    /// </summary>
    /// <param name="index">The zero-based position identifier within the open option list layer.</param>
    /// <param name="maxRetries">The threshold tracking allowable workflow reset cycles before throwing severe faults.</param>
    /// <returns>A validated <see cref="ResidentInfo"/> instance containing structural location and name criteria strings.</returns>
    /// <exception cref="Exception">Thrown if layout changes or card fields fail to populate within visibility bounds after max execution retries terminate.</exception>
    public async Task<ResidentInfo> SelectResidentAsyncByInd(int index, int maxRetries = 3)
    {
        Log.Debug("[RESIDENT_DIAG] Starting SelectResidentAsyncByInd process...");

        var lookupContainer = _page.Locator("cad-lookup-select, rnt-resident-lookup-select").First;
        var selectContainer = lookupContainer.Locator("mat-select:not(.mat-select-disabled)").First;
        var dropdownPanel = _page.Locator(".mat-mdc-select-panel, .mat-select-panel, div[role='listbox']").First;

        await selectContainer.ScrollIntoViewIfNeededAsync();
        await selectContainer.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            Log.Debug($"\n--- [Attempt {attempt}/{maxRetries}] STARTING DROP-DOWN INTERACTION ---");

            try
            {
                var isExpanded = await selectContainer.GetAttributeAsync("aria-expanded");
                if (isExpanded != "true")
                {
                    await selectContainer.ClickAsync();
                    await Task.Delay(300); // Короткая пауза, чтобы Angular успел развернуть DOM оверлея
                }

                await dropdownPanel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

                var options = dropdownPanel.Locator("mat-option, mat-mdc-option");
                await options.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

                var targetOption = options.Nth(index);

                // 1. Вместо FocusAsync (который переносит фокус ввода) делаем скролл к опции, чтобы она отрендерилась виртуальным скроллом Angular
                Log.Debug($"[RESIDENT_DIAG] Scrolling to target option at index {index}...");
                await targetOption.ScrollIntoViewIfNeededAsync();
                await Task.Delay(200); // Даем Angular Material отрисовать элемент после скролла

                var fullText = await targetOption.InnerTextAsync();
                var parts = fullText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                var expectedName = parts.FirstOrDefault() ?? "";

                Log.Debug($"[RESIDENT_DIAG] Resolved expectedName: '{expectedName}'. Hovering over the element...");

                // 2. Наводим мышку на элемент (имитируем реальный жест пользователя)
                await targetOption.HoverAsync();
                await Task.Delay(200);

                Log.Debug("[RESIDENT_DIAG] Executing targetOption.ClickAsync() with human-like delay...");

                // 3. ЖЕСТКИЙ ФИКС: Кликаем с искусственной задержкой зажатия кнопки мыши (Delay = 150ms).
                // Это не дает клику пролететь за 0мс и заставляет Angular гарантированно затриггерить selectionChange!
                await targetOption.ClickAsync(new() { Delay = 150 });

                Log.Debug("[RESIDENT_DIAG] Click sent. Waiting for dropdownPanel to transition to Hidden state...");
                await dropdownPanel.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });

                // Даем форме спокойно отправить один XHR-запрос
                Log.Debug("[RESIDENT_DIAG] Waiting 2000ms for heavy Angular form generation pipeline...");
                await Task.Delay(2000);

                Log.Debug("[RESIDENT_DIAG] Locating MRN field via text node...");
                // ИСПРАВЛЕНИЕ ЛОКАТОРА: Ищем поле "MRN" через наш стабильный метод, проверяющий внутренний .lv-label, а не атрибут!
                var mrnField = _page.Locator("cad-label-value-field")
                                    .Filter(new() { Has = _page.Locator(".lv-label").GetByText("MRN", new() { Exact = true }) })
                                    .First;

                // Сначала ждем, чтобы блок хотя бы прикрепился к DOM
                await mrnField.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 15000 });
                // Теперь ждем видимости на экране
                await mrnField.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

                // Проверяем, что бэкенд отдал цифры MRN
                var mrnValueContainer = mrnField.Locator(".lv-value");
                await Assertions.Expect(mrnValueContainer).ToContainTextAsync(new System.Text.RegularExpressions.Regex(@"\d+"), new() { Timeout = 10000 });
                Log.Debug("[RESIDENT_DIAG] MRN successfully validated!");

                return new ResidentInfo(
                    Name: expectedName,
                    Room: parts.ElementAtOrDefault(1) ?? "",
                    Bed: parts.ElementAtOrDefault(2) ?? ""
                );
            }
            catch (Exception ex)
            {
                Log.Error($"[RESIDENT_DIAG] Intercepted crash on attempt {attempt}. Error: {ex.Message}");

                await _page.Keyboard.PressAsync("Escape");

                // ЖЕСТКИЙ ФИКС ДЛЯ РЕТРАЯ: Если упали, даем Angular целых 3 секунды покоя перед следующей попыткой клика!
                Log.Warning($"[RESIDENT_DIAG] Attempt {attempt} failed. Cooling down Angular for 3000ms before retrying...");
                await Task.Delay(3000);
            }
        }

        throw new Exception($"Failed to select resident by index {index} after {maxRetries} attempts.");
    }



    /// <summary>
    /// Coordinates workspace view navigation by shifting active form tabs based on user-facing label names.
    /// </summary>
    /// <param name="tabName">The descriptive text title displaying on the target form tab layout control.</param>
    /// <param name="options">Optional parameter configuration flags to custom-tailor click interactions.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClickTabAsync(string tabName, LocatorClickOptions options = null)
    {
        // Locate the target layout view switcher container (tab) by its name and dispatch a click event handler
        // The framework automatically synchronizes thread states until the element renders and handles clicks smoothly
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle); // Synchronize thread bounds until backend API request queues clear
        await _page.GetByRole(AriaRole.Tab, new() { Name = tabName, Exact = false }).ClickAsync();
    }

    /// <summary>
    /// Commits the initial form input workflow arrays by triggering the base workflow "Create" operation button control.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClickCreateIncident()
    {
        // 1. Dispatch click action event handlers directly onto the "Create" button element
        // Utilizing GetByRole establishes optimal stability when interacting with structural Material Design buttons
        await _page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();

        // 2. Synchronize until active data streams quiet down (verifying zero active requests persist during 500ms intervals)
        // This barrier ensures database synchronization and persistence operations terminate successfully post button submission hooks
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForTimeoutAsync(1500);

        Log.Information($"Current location URL registered post creation sequence: {_page.Url}");
    }

    /// <summary>
    /// Resolves the raw interaction locator targeting the primary form action "Create" button component.
    /// </summary>
    /// <returns>An <see cref="ILocator"/> referencing the target wizard submission element.</returns>
    public async Task<ILocator> GetCreateIncidentButtonLocator()
    {
        return _page.GetByRole(AriaRole.Button, new() { Name = "Create" });
    }

    /// <summary>
    /// Commits ongoing draft updates to form storage data layers by focusing and triggering the operational "Save" button control.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClickSaveIncident()
    {
        // 1. Locate the target application storage persistence execution trigger button ("Save")
        // Utilizing GetByRole establishes optimal stability when interacting with structural Material Design buttons
        var saveButton = _page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true })
                         .Filter(new() { Visible = true });

        // Verify that the button component is fully rendered in the DOM tree and active within current view boundaries prior to dispatching clicks
        await saveButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await _page.Keyboard.PressAsync("Tab"); // Move focus completely away from the final populated field container to force model validation evaluation updates
        await saveButton.FocusAsync(); // Force execution focus directly onto the targeted action button control element layer
        await saveButton.ClickAsync();

        // 2. Synchronize until active data streams quiet down (verifying zero active requests persist during 500ms intervals)
        // This barrier ensures database synchronization and persistence operations terminate successfully post button submission hooks
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForTimeoutAsync(1500);

        Log.Information($"Current location URL registered post persistence saving sequence: {_page.Url}");
    }
}
