using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using Log = CareAdminTestProject.Common.TestLog;


namespace CareAdminTestProject.Incidents.CommonIncidentTests
{
    public class BaseIncidentSteps
    {

        public readonly IPage _page;
        public readonly IncidentCreatePage _createPage;
        public readonly IncidentTrackerPage _trackerPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentDetailsSteps"/> class.
        /// </summary>
        /// <param name="page">The isolated Playwright page context instance assigned to the running thread.</param>
        protected BaseIncidentSteps(IPage page)
        {
            _page = page;

            // Железно инициализируем страницы при создании любого класса шагов
            _createPage = new IncidentCreatePage(page);
            _trackerPage = new IncidentTrackerPage(page);
        }

        /// <summary>
        /// Manages the full dashboard navigation workflow loop by expanding targeted sidebar panels and tracking the resulting browser URL transformation changes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task NavigateToTrackerViaMenu()
        {
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Log.Debug($"[NAVIGATION] Attempt {attempt} of {maxRetries}: Checking current page state...");

                    var newIncidentBtn = _page.GetByRole(AriaRole.Button, new() { Name = "New Incident" });

                    // Fast track: if we are already on the tracker page and the main action button is visible, skip navigation
                    if (_page.Url.Contains("/tracker", StringComparison.OrdinalIgnoreCase) && await newIncidentBtn.IsVisibleAsync())
                    {
                        Log.Information("[NAVIGATION] Already on the Tracker page with active UI. Skipping menu interaction.");
                        return;
                    }

                    Log.Debug("[NAVIGATION] Opening Tracker via menu...");

                    var parentMenu = _page.Locator("li").Filter(new() { HasText = "Accident/Incident" });
                    var trackerLink = parentMenu.Locator("a").Filter(new() { HasText = "Tracker" });

                    if (!await trackerLink.IsVisibleAsync())
                    {
                        Log.Debug("[NAVIGATION] Sidebar panel is collapsed. Triggering menu expansion...");
                        var menuTrigger = parentMenu.Locator(".k-icon, .arrow-icon, span, a")
                                                    .GetByText("Accident/Incident", new() { Exact = false })
                                                    .First;

                        // Force click if the menu is covered by a fading overlay or transition
                        await menuTrigger.ClickAsync(new() { Force = true });
                        await trackerLink.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                    }

                    Log.Debug("[NAVIGATION] Clicking on the Tracker link...");
                    await trackerLink.ClickAsync();

                    Log.Debug("[NAVIGATION] Waiting for 'New Incident' button to ensure page is loaded...");
                    // Reduced initial timeout per attempt to fail fast and retry if UI is frozen
                    await newIncidentBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

                    var trackerSpinner = _page.Locator(".loading-overlay, .spinner, kendo-textbox-loading-icon, [class*='loading']").First;

                    if (await trackerSpinner.IsVisibleAsync())
                    {
                        Log.Debug("[NAVIGATION] Tracker page loading spinner detected. Waiting for data grid to stabilize...");
                        await trackerSpinner.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 20000 });
                    }

                    await Task.Delay(500);
                    Log.Information($"[NAVIGATION SUCCESS] Navigated to Tracker menu successfully on attempt {attempt}.");
                    return; // Success! Exit the method.
                }
                catch (Exception ex)
                {
                    Log.Warning($"[NAVIGATION FAILED] Attempt {attempt} failed. Current URL: {_page.Url}. Error: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        Log.Error($"[NAVIGATION CRITICAL] Failed to navigate to Tracker after {maxRetries} attempts.");
                        throw;
                    }

                    // Refreshing the page before the next attempt can clear broken UI/Kendo states
                    Log.Debug("[NAVIGATION RETRY] Refreshing page state before next navigation attempt...");
                    await _page.ReloadAsync(new() { WaitUntil = WaitUntilState.Commit });
                    await Task.Delay(1500);
                }
            }
        }


    }
}
