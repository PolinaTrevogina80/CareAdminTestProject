using CareAdminTestProject.Common;
using Microsoft.Playwright;
using Log = CareAdminTestProject.Common.TestLog;


namespace CareAdminTestProject.Incidents.CommonIncidentTests
{
    public class BaseIncidentHubTests : BaseTest
    {
        [SetUp]
        public async Task BaseHubSetup()
        {
            Log.Debug($"Executing BaseHubSetup lifecycle sequence: switching to Carillon facility area...");

            // Гарантируем, что мы в нужном учреждении на уровне всего модуля инцидентов
            await EnsureFacilitySelected("Carillon");
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
            var text = await Page.Locator(".k-input-value-text").First.TextContentAsync();
            // NOTE: Review debug log
            Log.Debug($"Active work scope selection context reads as: {text}");

            return text?.Trim() ?? string.Empty;
        }

    }
}
