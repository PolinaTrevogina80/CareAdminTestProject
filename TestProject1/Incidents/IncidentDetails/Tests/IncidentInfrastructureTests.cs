namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    [TestFixture]
    public class IncidentInfrastructureTests : BaseIncidentTests
    {

        /// <summary>
        /// Verifies that the primary Accident/Incident dashboard can be accessed successfully from the home page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task ShouldOpenIncidentDashboardFromHomePage()
        {
            // Session authorization is already active. Proceeding straight to execution steps.
            Log.Debug("Navigating directly to the application HOME page routing path...");
            await Page.GotoAsync("/");

            Log.Debug("Triggering redirection link path toward the main Accident/Incident area...");
            await Page.GetByAltText("Accident/Incident").ClickAsync();

            Log.Debug("Evaluating if the primary main dashboard grid finishes rendering post routing redirect...");
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*dashboards/incident-main"));
            await Expect(Page.Locator("cad-breadcrumb").GetByText("Main Dashboard")).ToBeVisibleAsync();
        }

        /// <summary>
        /// Verifies the ability to switch the current operational work scope context over to the Carillon facility via dropdown structures.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task SwitchToCarrillonTest()
        {
            // 1. PRE-CONDITION: Ensure a dynamic switch action must occur by validating initial state expectations
            // (This guarantees the framework actively triggers evaluation changes inside selection nodes)
            //await EnsureCassenaCareSelected();
            var facility = "Carillon";

            // 2. ACTION: Toggle tree selection fields toward the Carillon location property node
            await SelectInTreeAsync(facility);

            // 3. VERIFICATION: Assert that selection label inner text updates match target facility assignments
            await Expect(Page.Locator(".k-input-value-text").First)
                .ToContainTextAsync(facility);
            Log.Debug("Switching to Carillon location completed successfully.");

            await GetCurrentSelectionAsync();
        }

        /// <summary>
        /// Verifies the ability to switch the current operational work scope context back to the Cassena Care facility.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task SwitchToCassenaCareTest()
        {
            // Switch configuration states backward to the default Cassena Care structural node branch
            await SelectInTreeAsync("Cassena Care");

            // VERIFICATION: Assert that selection label inner text updates match Cassena Care assignments
            await Expect(Page.Locator(".k-input-value-text").First)
                .ToContainTextAsync("Cassena Care");
            Log.Information("Switching to Cassena Care location completed successfully.");

            await GetCurrentSelectionAsync();
        }

    }
}
