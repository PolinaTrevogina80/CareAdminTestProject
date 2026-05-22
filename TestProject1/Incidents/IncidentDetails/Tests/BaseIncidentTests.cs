using CareAdminTestProject.Incidents.IncidentDetails.Steps;
using Microsoft.Playwright;
using CareAdminTestProject.Common;
using static IncidentDataFactory;
using Log = CareAdminTestProject.Common.TestLog;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    /// <summary>
    /// Serves as the functional base suite for incident automation tests.
    /// Orchestrates standard multi-tab setups, shared pre-requisites, and test execution workflows.
    /// </summary>
    [TestFixture]
    public class BaseIncidentTests : BaseTest
    {
        /// <summary> The main high-level BDD step layer manager scoped to the current test context execution loop. </summary>
        public IncidentDetailsSteps steps;

        /// <summary> The complex reference test data payload record model used to populate forms during execution. </summary>
        public IncidentTestData data;

        /// <summary> Biographical and room placement location constraints captured for the target resident profile. </summary>
        public IncidentCreatePage.ResidentInfo resident;

        private static int _globalResidentCounter = 0;

        /// <summary>
        /// Executes foundational test pre-requisites before each automated test case runs, 
        /// enforcing target facility selections, opening new blank forms, and caching model references.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SetUp]
        public async Task Setup()
        {
            Log.Debug($"Executing Setup lifecycle sequence: switching to Carillon facility area...");

            await EnsureFacilitySelected("Carillon");

            // Instantiating the step runner, mapping it to the thread-isolated Page instance
            steps = new IncidentDetailsSteps(Page);
            await steps.NavigateToTrackerViaMenu();
            await steps.OpenNewIncidentAsync();

            // ДИНАМИЧЕСКИЙ РАСЧЕТ ИНДЕКСА РЕЗИДЕНТА ДЛЯ ПОТОКА:
            // Берем хэш-код текущего потока (он уникален для каждого параллельного воркера)
            int threadId = Environment.CurrentManagedThreadId;

            // Оператор % гарантирует, что индекс ВСЕГДА будет в диапазоне от 0 до 4, независимо от ID потока
            int currentTestRunIndex = System.Threading.Interlocked.Increment(ref _globalResidentCounter);

            // Используем оператор %, чтобы если тестов станет больше 45, индексы плавно пошли по второму кругу
            // Начинаем с индекса 1 (пропуская 0, если первый элемент в дропдауне — это какой-то пустой плейсхолдер)
            int residentIndex = (currentTestRunIndex % 45) + 1;

            Log.Debug($"[PARALLEL ENGINE] Test '{TestContext.CurrentContext.Test.Name}' triggered. " +
                         $"Global Run Number: {currentTestRunIndex}. Selecting unique Resident Index: {residentIndex}");
            // =========================================================================


            Log.Debug($"[THREAD PARALLEL] Thread ID {threadId} evaluates to Resident Index: {residentIndex}");

            resident = await steps.SelectResidentAsync(residentIndex);
            data = IncidentDataFactory.CreateDefaultFall(resident);
        }

        /// <summary>
        /// Navigates to the incident main dashboard and dynamically handles a Kendo DropdownTree overlay element to switch the active facility selection.
        /// </summary>
        /// <param name="facilityName">The exact exact text label string of the destination facility node.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SelectFacilityAsync(string facilityName)
        {
            await Page.GotoAsync("/accident-incident/dashboards/incident-main");
            var dropdown = Page.Locator("kendo-dropdowntree.k-dropdowntree").First;

            // Utilizing JavaScript evaluation execution injection to guarantee stable clicks on overlay triggers
            await dropdown.EvaluateAsync("el => el.click()");

            var option = Page.Locator(".k-popup .k-treeview-leaf, .k-animation-container .k-treeview-leaf")
                             .GetByText(facilityName, new() { Exact = true });

            await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await option.ClickAsync();
            Log.Information($"Switching to facility context '{facilityName}' completed successfully.");

            // Synchronize execution bounds until target text modifications update completely within the selector template
            await Expect(Page.Locator(".k-input-value-text").First).ToContainTextAsync(facilityName);
        }



        /// <summary>
        /// Navigates to the incident dashboard layout page and uses reliable asynchronous JavaScript evaluation 
        /// to expand the custom Kendo DropdownTree overlay element before selecting a specific target facility node.
        /// </summary>
        /// <param name="targetName">The exact exact text label string of the target facility selection choice.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SelectInTreeAsync(string targetName)
        {
            await Page.GotoAsync("/accident-incident/dashboards/incident-main");

            var dropdown = Page.Locator("kendo-dropdowntree.k-dropdowntree").First;
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Attached });

            // Open the selection item list via JavaScript (your most stable interaction strategy)
            await dropdown.EvaluateAsync("el => el.click()");

            // Resolve the target list option row within the expanded dropdown window layout layers
            var option = Page.Locator(".k-popup .k-treeview-leaf, .k-animation-container .k-treeview-leaf")
                             .GetByText(targetName, new() { Exact = true });

            await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await option.ClickAsync();


            // Verify that the inner text displayed inside the selector element changes to match target values
            await Expect(Page.Locator(".k-input-value-text").First)
                .ToContainTextAsync(targetName, new() { Timeout = 10000 });

            Log.Information($"{targetName} selected");
        }

        /// <summary>
        /// Reads current interface configurations and enforces a switch selection 
        /// over to the "Cassena Care" group context if it is not already designated as active.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureCassenaCareSelected()
        {
            const string target = "Cassena Care";
            if (await GetCurrentSelectionAsync() != target)
            {
                // NOTE: Review debug log
                Log.Debug($"[SETUP] Toggling selection properties toward parent group context: {target}");
                await SelectInTreeAsync(target);
            }
        }

        /// <summary>
        /// Optimizes setup pipelines by assessing current active select text content 
        /// and skipping rendering updates if the target facility choice is verified as already chosen.
        /// </summary>
        /// <param name="facilityName">The exact user-facing string description name of the target facility node.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureFacilitySelected(string facilityName)
        {
            await Page.GotoAsync("/accident-incident/dashboards/incident-main");
            var currentText = await Page.Locator(".k-input-value-text").First.TextContentAsync();

            if (currentText?.Trim() != facilityName)
            {
                // NOTE: Review debug log
                Log.Debug($"Current facility context is evaluated as - Cassena Care");
                // NOTE: Review debug log
                Log.Debug($"[SETUP] Actively switching institutional facility work scope to '{facilityName}'");
                await SelectInTreeAsync(facilityName);
                await GetCurrentSelectionAsync();

            }
        }


        /// <summary>
        /// Extracts and tracks the active text content currently populated inside the primary Kendo selector field box.
        /// </summary>
        /// <returns>A trimmed string mapping the exact selected label value choice name, or empty if properties evaluate as null.</returns>
        public async Task<string> GetCurrentSelectionAsync()
        {
            // await Page.GotoAsync("/accident-incident/dashboards/incident-main");
            var text = await Page.Locator(".k-input-value-text").First.TextContentAsync();
            // NOTE: Review debug log
            Log.Debug($"Active work scope selection context reads as: {text}");

            return text?.Trim() ?? string.Empty;
        }
    }
}