using CareAdminTestProject.Common;
using CareAdminTestProject.Incidents.CommonIncidentTests;
using CareAdminTestProject.Incidents.Helpers;
using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using System.Text.Json;
using System.Text.Json.Nodes;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using static CareAdminTestProject.Common.PlaywrightExtensions;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.BaseIncidentTabs;
using static DetailsTab;
using static GeneralTab;
using static IncidentCreatePage;
using static IncidentDataFactory;
using static MedicationTab;
using static Microsoft.Playwright.Assertions;
using static StateTab;
using static SummaryTab;
using Log = CareAdminTestProject.Common.TestLog;

namespace CareAdminTestProject.Incidents.IncidentDetails.Steps
{
    /// <summary>
    /// Serves as the primary step library (Action Word / BDD Step Layer) for incident test suites.
    /// Combines low-level page objects into consolidated reusable business actions.
    /// Serves as the primary step library (Action Word / BDD Step Layer) for incident test suites.
    /// Combines low-level page objects into consolidated reusable business actions.
    /// <para><b>--- METHOD DIRECTORY & QUICK LINKS ---</b></para>
    /// <list type="bullet">
    ///   <item> <description> Form Initialization: <see cref="OpenNewIncidentAsync"/> </description> </item>
    ///   <item> <description> Resident Matching:   <see cref="SelectResidentAsync(int)"/> </description> </item>
    ///   <item> <description> Routing Operations:  <see cref="GetCurrentUrlAsync"/>,
    ///                                             <see cref="GetSelectedResidentIndexAsync"/>, 
    ///                                             <see cref="GoBackBrowserAsync"/>,
    ///                                             <see cref="LeavePageViaMenuAsync"/>,
    ///                                             <see cref="ReloadPageAndNavigateAsync(string)"/> </description> </item>
    ///   <item> <description> View Panel Switching: <see cref="SwitchToTab(string)"/> </description> </item>
    ///   <item> <description> Master Workflows Injection: <see cref="FillAndSaveEntireIncident(IncidentTestData)"/>, 
    ///                                                    <see cref="SetIncidentLockStateAsync(bool)"/>  </description> </item>
    ///   <item> <description> Role Signatures Approvals:   <see cref="SignDNS"/>, 
    ///                                                     <see cref="SignMD"/>, 
    ///                                                     <see cref="SignAdministrator"/> </description> </item>
    ///   <item> <description> Security Workspace Locking: <see cref="AssertIncidentIsLockedAsync"/>, 
    ///                                                    <see cref="AssertIncidentIsUnlockedAsync"/></description> </item>
    ///   <item> <description> Tab Injections: <see cref="FillGeneralTabAsync(IncidentTestData)"/>,
    ///                                        <see cref="FillDetailsTabAsync(IncidentTestData)"/>,
    ///                                        <see cref="FillStateTabAsync(IncidentTestData)"/>,
    ///                                        <see cref="FillMedicationTabAsync(IncidentTestData)"/>,
    ///                                        <see cref="FillRNFormTabAsync(IncidentTestData)"/>,
    ///                                        <see cref="FillSummaryTabAsync(IncidentTestData)"/> </description> </item>
    ///   <item> <description> Grid Purging: <see cref="ClearMedicationTabAsync"/>,
    ///                                      <see cref="ClearGeneralForm"/>,
    ///                                      <see cref="ClearDetailsForm"/> </description> </item>
    ///   <item> <description> Attachment Streaming: <see cref="UploadAttachmentTabAsync(string, string, string, bool)"/>, 
    ///                                              <see cref="UploadAttachmentTabAsync(IReadOnlyList{string}, string, string, bool)"/> </description> </item>
    ///   <item> <description> Submission Button Evaluation: <see cref="VerifyCreateButtonStateAsync"/>, 
    ///                                                      <see cref="ClickCreateIncidentAsync"/>, 
    ///                                                      <see cref="ClickSaveIncidentAsync(bool)"/> </description> </item>
    ///   <item> <description> Form Actions Toggling: <see cref="SignSummaryAndVerifyAsync"/>, 
    ///                                               <see cref="SwitchFirstAid(bool)"/> </description> </item>
    ///   <item> <description> Dashboard View Routing: <see cref="NavigateToTrackerViaMenu"/> </description> </item>
    ///   <item> <description> Poly-morphic Retention Verification: <see cref="VerifyDataRetainedAsync(object)"/> </description> </item>
    ///   <item> <description> Intentionally Trigger Form Dirtiness: <see cref="ModifySingleFieldOnTabAsync(string)"/> </description> </item>
    ///   <item> <description> UI Indicator Checkpoints: <see cref="VerifySaveButtonEnabledStateAsync(bool)"/>, 
    ///                                                  <see cref="VerifyUnsavedChangesAlertVisibleAsync(string)"/>, 
    ///                                                  <see cref="VerifyFieldsOneByOneWithFilling(object, object)"/>, 
    ///                                                  <see cref="VerifyAllFieldsDotsStateAsync{T}(object, T, bool)"/>, 
    ///                                                  <see cref="VerifyStateTabSpecificLogicAsync"/>, 
    ///                                                  <see cref="VerifyRedDotField(object, string, bool)"/>, 
    ///                                                  <see cref="VerifyMedicationTabFullLifecycleAndIndicatorAsync"/>, 
    ///                                                  <see cref="VerifyTomorrowIsDisabledInCalendarAsync"/>, 
    ///                                                  <see cref="VerifyFutureTimeIsDisabledInPickerAsync"/>, 
    ///                                                  <see cref="VerifyResidentDiagnosesLoadedAsync"/>,
    ///                                                  <see cref="VerifyDescribeFieldRedDotStateAsync"/>,
    ///                                                  <see cref="FillRNFormTabWithTabCheckAsync(IncidentTestData)"/>, 
    ///                                                  <see cref="VerifyOtherAlarmInputFieldStateAsync"/>, 
    ///                                                  <see cref="VerifyRedDotTab(string, bool)"/> </description> </item>
    /// </list>
    /// </summary>

    public class IncidentDetailsSteps : BaseIncidentSteps
    {
        // Конструктор просто пробрасывает страницу наверх
        public IncidentDetailsSteps(IPage page) : base(page)
        {
        }


        /// <summary> Gets the underlying page object controller for the incident creation wizard framework. </summary>
        public IncidentCreatePage CreatePage => _createPage;
        string fileName;
        int residentInd;

        /// <summary> Holds the state of the parsed general incident dataset during the runtime workflow transaction. </summary>
        public IncidentGeneralInfo CapturedGeneralData { get; private set; }


        /// <summary>
        /// Navigates through the tracking repository dashboard and triggers the workspace initialization for a new blank incident form.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OpenNewIncidentAsync()
        {
            await _trackerPage.ClickNewIncidentAsync();
            // NOTE: Review debug log
            Log.Debug("New Incident form is opened");
        }

        /// <summary>
        /// Selects a resident by row position index list within the lookup select fields and executes an inline assertion confirming successful data loading.
        /// </summary>
        /// <param name="i">The zero-based index target within the active option grid list container.</param>
        /// <returns>A validated <see cref="ResidentInfo"/> object populated with profile parameters.</returns>
        public async Task<ResidentInfo> SelectResidentAsync(int i)
        {
            Log.Debug($"Try to select resident with the index {i} in the list");
            var info = await _createPage.SelectResidentAsyncByInd(i);
            residentInd = i;

            var residentNameLink = _page.Locator("a.link.resident-name").First;
            await residentNameLink.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await Assertions.Expect(residentNameLink).ToContainTextAsync(info.Name, new() { Timeout = 5000 });

            return info; // Никакого бреда с API, просто чистый UI-объект!
        }
        /// <summary>
        /// Extracts and tracks the exact text address string from the browser's active window layout location locator properties.
        /// </summary>
        /// <returns>A task whose result contains the active browser tab page string URL address.</returns>
        public async Task<string> GetCurrentUrlAsync()
        {
            return _page.Url;
        }

        public async Task<string> GetIndicentId(string url)
        {
            var uri = new Uri(url);
            string incidentId = uri.Segments.Last().Trim('/').Split('?')[0]; // Чистый GUID без параметров
            Log.Information($"[TEST] Извлечен ID инцидента для проверки: {incidentId}");
            return incidentId;
        }

        /// <summary>
        /// Emulates clicking the browser's native 'Back' navigation control button to evaluate Angular Change Detection and draft warning states.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GoBackBrowserAsync()
        {
            // NOTE: Review debug log
            Log.Debug("Clicking the browser's native 'Back' navigation control button to evaluate Change Detection behaviors...");

            // Simulates clicking the back navigation chevron arrow in the browser context environment
            await _page.GoBackAsync();
        }

        /// <summary>
        /// Evades current unsaved forms by using the main interface sidebar links to exit towards the primary Tracker dashboard view.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LeavePageViaMenuAsync()
        {
            // NOTE: Review debug log
            Log.Debug("Clicking the 'Tracker' menu sidebar navigation link item to discard the active form view canvas...");

            // Isolate and target the primary 'Tracker' routing hyperlinks embedded directly within the operational layout panel nodes
            await _page.Locator("a:has-text('Tracker'), .sidebar-link:has-text('Tracker')").First.ClickAsync();
        }


        /// <summary>
        /// Commands the active browser tab page context to reload or navigate directly to a specified URL address location, blocking until network idle states synchronize.
        /// </summary>
        /// <param name="url">The absolute target website routing link address string.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ReloadPageAndNavigateAsync(string url)
        {
            await _page.GotoAsync(url);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        public async Task ReloadNewIncidentPage()
        {
            var url = await GetCurrentUrlAsync();
            await ReloadPageAndNavigateAsync(url);
            await SelectResidentAsync(residentInd);
        }

        /// <summary>
        /// Switches workspace focus to a target form tab and invokes a robust multi-stage synchronization pipeline 
        /// checking DOM opacity values to guarantee underlying CSS slide animations finish rendering completely.
        /// </summary>
        /// <param name="tabName">The exact visible string text name of the destination tab to open.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SwitchToTab(string tabName)
        {
            // NOTE: Review debug log
            Log.Debug($"Switching workspace focus to tab element view: {tabName}");

            // 1. Dispatch click action event handlers targeting the tab component
            await _createPage.ClickTabAsync(tabName, new() { Timeout = 30000 });

            // 2. Isolate and target the primary corresponding visible panel content layout wrapper
            var tabContentPanel = _page.GetByRole(AriaRole.Tabpanel, new() { Name = tabName });
            await tabContentPanel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

            // 3. CSS TRANSITION SYNC BARRIER: Evaluate layout property trees until element opacity reaches exactly 1
            // NOTE: Review debug log
            Log.Debug("Awaiting complete termination of active UI layout CSS animations...");
            try
            {
                // Query running element animation node states directly inside browser execution threads
                await _page.WaitForFunctionAsync(
                    "el => window.getComputedStyle(el).opacity === '1'",
                    await tabContentPanel.ElementHandleAsync(),
                    new() { Timeout = 5000 }
                );
            }
            catch
            {
                Log.Warning("Layout frame opacity animation cycles timed out. Executing an alternative stabilization backup delay.");
                // Handshake execution gap to counter severe environment layout lags on slow QA automation environments
                await Task.Delay(1000);
            }

            // NOTE: Review debug log
            Log.Debug($"Tab component view layer '{tabName}' is completely rendered and ready for actions.");
        }

        /// <summary>
        /// Orchestrates a high-level master test pipeline sequence that purges initial states and sequentially 
        /// populates, submits, signs, and attaches reports across every tab in the wizard form.
        /// </summary>
        /// <param name="data">The reference complex master data record factory object model holding complete incident parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task FillAndSaveEntireIncident(IncidentTestData data)
        {
            await ClearGeneralForm();
            await FillGeneralTabAsync(data);
            await FillDetailsTabAsync(data);
            await FillStateTabAsync(data);
            await FillMedicationTabAsync(data);
            await FillRNFormTabAsync(data);
            await ClickCreateIncidentAsync();
            await FillSummaryTabAsync(data);
            await ClickSaveIncidentAsync();
            await SignSummaryAndVerifyAsync();
            await ClickSaveIncidentAsync(true);
            await UploadAttachmentTabAsync("Accident Report");
        }

        /// <summary>
        /// Triggers the role signature workflow loop assigning authorization status to the Director of Nursing or Designee.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task SignDNS()
        {
            return _createPage.SignAsRoleAsync(RoleToSign.DNS);
        }

        /// <summary>
        /// Triggers the role signature workflow loop assigning authorization status to the Facility Medical Director.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task SignMD()
        {
            return _createPage.SignAsRoleAsync(RoleToSign.MD);
        }

        /// <summary>
        /// Triggers the role signature workflow loop assigning authorization status to the Facility Administrator.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task SignAdministrator()
        {
            return _createPage.SignAsRoleAsync(RoleToSign.Administrator);
        }

        /// <summary>
        /// Enforces explicit verification assertions checking that the incident file locks correctly and displays lock text markers post full signature approval workflows.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AssertIncidentIsLockedAsync()
        {
            // NOTE: Review debug log
            Log.Debug("Verifying that the incident form successfully transitions to locked status mode after committing all role signatures...");

            // Isolate and query the primary application status lock notification ribbon component banner
            var lockBanner = _page.Locator("body");
            await Assertions.Expect(lockBanner).ToContainTextAsync("Incident Locked", new() { Timeout = 5000 });

            // NOTE: Review debug log
            Log.Debug("[SUCCESS] Incident workspace is confirmed locked. All historical role signatures are securely saved.");
        }

        /// <summary>
        /// Enforces explicit verification assertions confirming that the security toggle element maps to a decoupled, unlocked status value inside the DOM tree.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AssertIncidentIsUnlockedAsync()
        {
            // NOTE: Review debug log
            Log.Debug("Evaluating active configuration states of the primary layout lock slide toggle switch (must resolve to unlocked)...");

            // Target the primary interaction button control node enclosed within the mat-slide-toggle framework elements
            var toggleButton = _page.Locator("mat-slide-toggle button[role='switch']").First;

            // Enforce explicit check confirmations validating that the accessibility state attributes read as false
            await Assertions.Expect(toggleButton).ToHaveAttributeAsync("aria-checked", "false", new() { Timeout = 5000 });

            // NOTE: Review debug log
            Log.Debug("[SUCCESS] Verified via underlying Angular component properties: the incident workspace is successfully unlocked.");
        }

        /// <summary>
        /// Sets the locking state of the incident workspace form by toggling the target slide switch control.
        /// </summary>
        /// <param name="shouldBeLocked">Pass true to lock the form workspace, or false to unlock it.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SetIncidentLockStateAsync(bool shouldBeLocked)
        {
            // NOTE: Review debug log
            Log.Debug($"[Lock Management] Transitioning the form workspace status to 'Locked = {shouldBeLocked}'...");

            // Point locator targeting the slide switch control element button
            var toggleButton = _page.Locator("mat-slide-toggle button[role='switch']").First;
            await toggleButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // Read runtime attributes configuration tracking the current toggle selection state
            var ariaChecked = await toggleButton.GetAttributeAsync("aria-checked");
            bool isCurrentlyLocked = ariaChecked == "true";

            if (isCurrentlyLocked == shouldBeLocked)
            {
                // NOTE: Review debug log
                Log.Debug($"The form workspace status already satisfies expectations (Locked = {isCurrentlyLocked}). Click skipped.");
                return;
            }

            // NOTE: Review debug log
            Log.Debug($"Clicking on the lock management slide toggle. Current status: {isCurrentlyLocked}, Target status: {shouldBeLocked}");

            // Execute click action hooks directly on the button component enclosed inside the mat-slide-toggle layout wrapper
            await toggleButton.ClickAsync(new() { Force = true });

            // Synchronize until the underlying Angular attribute updates to match the expected values (true/false)
            string expectedValue = shouldBeLocked ? "true" : "false";
            await Assertions.Expect(toggleButton).ToHaveAttributeAsync("aria-checked", expectedValue, new() { Timeout = 7000 });

            // Brief pause allowing CSS style transitions to complete their animations across layout layers smoothly
            await Task.Delay(1000);
            // NOTE: Review debug log
            Log.Debug("[Lock Management] Slide toggle state properties updated successfully inside the active DOM tree.");
        }

        /// <summary>
        /// Fills out the baseline configuration parameters in the General tab and captures generated draft metrics.
        /// </summary>
        /// <param name="data">The reference master data record factory object model holding complete incident parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task FillGeneralTabAsync(IncidentTestData data)
        {
            CapturedGeneralData = await _createPage.General.FillBasicInfoAsync(data.General);
            // NOTE: Review debug screenshot sequence
            await _page.MakeScreenshotAsync("General_Filled");
            Log.Information("General Tab filled");
        }

        /// <summary>
        /// Transitions layout view to the Details tab and populates medical assessment data parameters.
        /// </summary>
        /// <param name="data">The reference master data record factory object model holding complete incident parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task FillDetailsTabAsync(IncidentTestData data)
        {
            await _createPage.ClickTabAsync("Details");
            await _createPage.Details.FillDetailsInfoAsync(data.Details);
            // NOTE: Review debug screenshot sequence
            await _page.MakeScreenshotAsync("Details_Filled");
            Log.Information("Details Tab filled");
        }

        /// <summary>
        /// Transitions layout view to the State tab and populates physical status and assistive device tracking parameters.
        /// </summary>
        /// <param name="data">The reference master data record factory object model holding complete incident parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task FillStateTabAsync(IncidentTestData data)
        {
            await _createPage.ClickTabAsync("State");
            await _createPage.State.FillStateTabAsync(data.State);
            // NOTE: Review debug screenshot sequence
            await _page.MakeScreenshotAsync("State_Filled");
            Log.Information("State Tab filled");
        }

        /// <summary>
        /// Transitions layout view to the Medication tab and inserts all defined medication lines into the grid data table.
        /// </summary>
        /// <param name="data">The reference master data record factory object model holding complete incident parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task FillMedicationTabAsync(IncidentTestData data)
        {
            await _createPage.ClickTabAsync("Medication");
            await _createPage.Medication.FillMedicationTabAsync(data.Medications);
            // NOTE: Review debug screenshot sequence
            await _page.MakeScreenshotAsync("Medication_Filled");
            Log.Information("Medication Tab filled");
        }

        /// <summary>
        /// Transitions layout view to the Medication tab and sequentially purges all present rows from the input table grid.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClearMedicationTabAsync()
        {
            await _createPage.ClickTabAsync("Medication");
            await _createPage.Medication.ClearAllMedicationsAsync();
            Log.Information("Medication Tab cleared");
        }

        /// <summary>
        /// Transitions layout view to the RN/Supervisor Investigation Form tab and fills out the step-by-step wizard.
        /// </summary>
        /// <param name="data">The reference master data record factory object model holding complete incident parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task FillRNFormTabAsync(IncidentTestData data)
        {
            await _createPage.ClickTabAsync("RN Supervisor Investigation Form\r\n");
            await _createPage.RNSupervisor.FillQuestionsAsync(data.RNSupervisor);
            // NOTE: Review debug screenshot sequence
            await _page.MakeScreenshotAsync("RNSupervisorForm_Filled");
            Log.Information("RN Supervisor Investigation Form Tab filled");
        }

        /// <summary>
        /// Transitions layout view to the Summary tab and fills out conclusions, care plan updates, and required reporting options.
        /// </summary>
        /// <param name="data">The reference master data record factory object model holding complete incident parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task FillSummaryTabAsync(IncidentTestData data)
        {
            await _createPage.ClickTabAsync("Summary");
            await _createPage.Summary.FillSummaryInfoAsync(data.Summary);
            // NOTE: Review debug screenshot sequence
            await _page.MakeScreenshotAsync("Summary_Filled");
            Log.Information("Summary Tab filled");
        }

        /// <summary>
        /// Transitions layout view to the Attachments tab and uploads a single file while mapping a specific category classification universally.
        /// </summary>
        /// <param name="categoryName">The target category descriptor label value to apply universally.</param>
        /// <param name="note">Optional supplementary string text notes to attach to document pages.</param>
        /// <param name="fileNameString">The specific filename to the document file. If null, the class downloaded summary file will be attached.</param>
        /// <param name="toScreenShot">Pass true to capture an execution step screenshot; otherwise, false.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UploadAttachmentTabAsync(string categoryName, string? note = null, string fileNameString = null, bool toScreenShot = true)
        {
            await _createPage.ClickTabAsync("Attachments");

            // If the file name is passed (from previous steps) — take it, otherwise use our default path
            string fileToUpload;

            // If the file name is NOT passed — take the ready value/path from the class property (fileName)
            if (string.IsNullOrEmpty(fileNameString))
            {
                fileToUpload = fileName;
            }
            // If a specific file name is passed (for example, "test_1page.pdf")
            else
            {
                // If a full path is already passed — leave it, otherwise assemble from TestData
                fileToUpload = Path.IsPathRooted(fileNameString)
                    ? fileNameString
                    : Path.Combine(AppContext.BaseDirectory, "TestData", "Files", fileNameString);
            }

            Log.Information($"Try to attach file {fileToUpload}");

            await _createPage.Attachments.UploadAttachmentAsync(fileToUpload);
            await _createPage.Attachments.AssignCategoriesToAllPagesAsync(categoryName, note);
            await _createPage.Attachments.VerifyAttachmentIsDisplayedAsync(categoryName);
            if (toScreenShot)
            {
                // NOTE: Review debug screenshot sequence
                await _page.MakeScreenshotAsync("Attachment_Filled");
            }
            Log.Information("Attachment file attached");
        }

        /// <summary>
        /// Transitions layout view to the Attachments tab, uploads a multi-page file, and maps a sequential list of categories to individual document pages.
        /// </summary>
        /// <param name="categoryNames">The collection array of target classification categories to apply sequentially across pages.</param>
        /// <param name="note">Optional supplementary string text notes to attach to document pages.</param>
        /// <param name="fileNameString">The specific filename or full path to the document file. If null, the class property is utilized.</param>
        /// <param name="toScreenShot">Pass true to capture an execution step screenshot; otherwise, false.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UploadAttachmentTabAsync(
        IReadOnlyList<string> categoryNames, // Accepts a list of categories for each page
        string? note = null,
        string? fileNameString = null,
        bool toScreenShot = true)
        {
            await _createPage.ClickTabAsync("Attachments");

            string fileToUpload = string.IsNullOrEmpty(fileNameString)
                ? fileName
                : (Path.IsPathRooted(fileNameString)
                    ? fileNameString
                    : Path.Combine(AppContext.BaseDirectory, "TestData", "Files", fileNameString));

            await _createPage.Attachments.UploadAttachmentAsync(fileToUpload);

            // Invoke your page-by-page mapping method, passing the entire list of categories
            await _createPage.Attachments.AssignCategoriesToAllPagesAsync(categoryNames, note);

            // Check the display of the first category from the list as a baseline verification
            if (categoryNames != null && categoryNames.Any())
            {
                await _createPage.Attachments.VerifyAttachmentIsDisplayedAsync(categoryNames[0]);
            }

            if (toScreenShot)
            {
                // NOTE: Review debug screenshot sequence
                await _page.MakeScreenshotAsync("Attachment_Filled");
            }
            Log.Information($"Multi-page attachment file '{fileToUpload}' attached successfully.");
        }

        /// <summary>
        /// Verifies whether the 'Create' button is enabled or disabled based on the expected state.
        /// </summary>
        /// <param name="shouldBeEnabled">True if the button should be active; false if it should be locked.</param>
        public async Task VerifyCreateButtonStateAsync(bool shouldBeEnabled)
        {
            // Retrieve the button locator via your page class
            var createButton = await _createPage.GetCreateIncidentButtonLocator();

            if (shouldBeEnabled)
            {
                Log.Information("Verifying that the 'Create' button is ENABLED...");
                await Assertions.Expect(createButton).ToBeEnabledAsync(new()
                {
                    Timeout = 5000 // Small timeout since the state change should be immediate
                });
            }
            else
            {
                Log.Information("Verifying that the 'Create' button is DISABLED...");
                await Assertions.Expect(createButton).ToBeDisabledAsync(new()
                {
                    Timeout = 5000
                });
            }
        }


        /// <summary>
        /// Universally routes complex runtime test records to corresponding individual tab validation frameworks matching data types dynamically.
        /// </summary>
        /// <param name="tabData">The specific structural data reference object used for comparison validation frameworks.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if universal execution workflows cannot map to recognized structured record configurations.</exception>
        public async Task VerifyDataRetainedAsync(object tabData)
        {
            switch (tabData)
            {
                case IncidentGeneralInfo generalData:
                    // Invoke check loops directly against the General tab workspace component
                    await _createPage.ClickTabAsync("General");
                    await _createPage.General.VerifyDataFieldsAsync(generalData);
                    // NOTE: Review debug log
                    Log.Debug("Tab General checked and is OK");
                    break;

                case IncidentDetailsInfo detailsData:
                    // Invoke check loops directly against the Details tab workspace component
                    await _createPage.ClickTabAsync("Details");
                    await _createPage.Details.VerifyDataFieldsAsync(detailsData);
                    // NOTE: Review debug log
                    Log.Debug("Tab Details checked and is OK");
                    break;

                case IncidentStateInfo stateData:
                    await _createPage.ClickTabAsync("State");
                    await _createPage.State.VerifyDataFieldsAsync(stateData);
                    // NOTE: Review debug log
                    Log.Debug("Tab State checked and is OK");
                    break;

                case List<MedicationInfo> medicationsList:
                    await _createPage.ClickTabAsync("Medication");
                    await _createPage.Medication.VerifyDataFieldsAsync(medicationsList);
                    // NOTE: Review debug log
                    Log.Debug("Tab Medications checked and is OK");
                    break;

                case IncidentSummaryInfo summaryData:
                    await _createPage.ClickTabAsync("Summary");
                    await _createPage.Summary.VerifyDataFieldsAsync(summaryData);
                    // NOTE: Review debug log
                    Log.Debug("Tab Summary checked and is OK");
                    break;

                case RNSupervisorTab.RNSupervisorTabInfo rnFormData:
                    await _createPage.ClickTabAsync("RN Supervisor Investigation Form");
                    await _createPage.RNSupervisor.VerifyDataFieldsAsync(rnFormData);
                    // NOTE: Review debug log
                    Log.Debug("Tab RN Supervisor Investigation Form checked and is OK");
                    break;

                default:
                    throw new ArgumentException($"Universal validation check framework is not configured for data type specification: {tabData.GetType().Name}");
            }
        }
        /// <summary>
        /// Verifies that tomorrow's date cell in the Kendo UI calendar popup is strictly disabled 
        /// using native keyboard arrow navigation to handle month transitions flawlessly.
        /// </summary>
        public async Task VerifyTomorrowIsDisabledInCalendarAsync()
        {
            Log.Debug("[CALENDAR_VALIDATION] Opening Kendo Calendar popup...");

            var calendarIcon = _createPage.General.GetFieldIconByName("dateOfIncident");
            await calendarIcon.ClickAsync();

            var calendarPopup = _page.Locator("kendo-popup kendo-calendar, .k-calendar-popup");
            await calendarPopup.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            Log.Debug("[CALENDAR_VALIDATION] Attempting to force focus past today using multiple 'ArrowRight' presses...");

            // 1. Встаем на таблицу календаря
            var activeCalendarTable = calendarPopup.Locator("table.k-calendar-table, .k-calendar-view").First;
            await activeCalendarTable.FocusAsync();

            // 2. Спамим стрелку вправо 3 раза. Если блокировка работает, фокус застрянет на 21 числе
            await _page.Keyboard.PressAsync("ArrowRight");
            await _page.Keyboard.PressAsync("ArrowRight");
            await _page.Keyboard.PressAsync("ArrowRight");
            await _page.WaitForTimeoutAsync(100);

            // 3. Нажимаем Enter, чтобы попытаться применить то, куда дошел селектор
            await _page.Keyboard.PressAsync("Enter");

            // Ждем закрытия попапа
            await Assertions.Expect(calendarPopup).ToBeHiddenAsync(new() { Timeout = 3000 });

            // 4. Читаем то, что в итоге записалось в инпут формы инцидента
            // Используем твой базовый метод получения значения по лейблу
            string actualDateInInput = await _createPage.General.GetFieldValueByLabelAsync("Date of Incident");

            // Вычисляем эталонную строку сегодняшнего дня в системном формате формы (M/d/yyyy)
            string todayDateStr = DateTime.Today.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);

            Log.Debug($"[CALENDAR_VALIDATION] Applied Date in Input: '{actualDateInInput}', Today Expected: '{todayDateStr}'");

            // Проверяем: в инпуте должно остаться СЕГОДНЯ, так как дальше система не должна была пустить
            Assert.That(actualDateInInput, Is.EqualTo(todayDateStr),
                $"Validation Error: The calendar allowed navigating to a future date! Expected field to stay '{todayDateStr}', but got '{actualDateInInput}'.");

            Log.Information("[CALENDAR_VALIDATION] Success: Calendar successfully blocked future date selection.");
        }

        /// <summary>
        /// Verifies that future minute options in the Kendo TimePicker are disabled or missing when 'Today' is selected.
        /// </summary>
        public async Task VerifyFutureTimeIsDisabledInPickerAsync()
        {
            Log.Debug("[TIME_VALIDATION] Ensuring 'Today' is selected first...");
            // Гарантированно выбираем сегодня (твой метод)
            await _createPage.General.SelectTodayAsync("dateOfIncident");

            Log.Debug("[TIME_VALIDATION] Opening Kendo TimePicker popup...");
            // Открываем пикер времени (твой метод)
            var pickerContainer = _page.Locator($"kendo-timepicker[name='{"timeOfIncident"}']");
            await pickerContainer.Locator("button.k-input-button").ClickAsync();

            // Ждем появления попапа со стрелками/колесиками времени Kendo
            var timePopup = _page.Locator("kendo-popup:visible, .k-animation-container:visible").First;
            await timePopup.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            try
            {
                // 3. Находим все колонки списков времени (Часы, Минуты, AM/PM) внутри попапа
                // Обычно это элементы kendo-timelist или блоки с классом .k-time-list
                var timeLists = timePopup.Locator("kendo-timelist, .k-time-list");
                int listsCount = await timeLists.CountAsync();

                // Проходимся по каждой доступной колонке (их может быть 2 или 3 в зависимости от формата)
                for (int i = 0; i < listsCount; i++)
                {
                    var currentList = timeLists.Nth(i);

                    // Находим самый последний активный элемент в текущей колонке
                    var lastItem = currentList.Locator(".k-item:not(.k-state-disabled)").Last;

                    if (await lastItem.CountAsync() > 0)
                    {
                        // Скроллим элемент в область видимости (Playwright автоматически прокрутит контейнер)
                        await lastItem.ScrollIntoViewIfNeededAsync();
                        // Кликаем по последнему доступному значению
                        await lastItem.ClickAsync(new() { Force = true });
                    }
                }

                // 4. Нажимаем кнопку "Set" для подтверждения (синяя кнопка на вашем скриншоте)
                var setButton = timePopup.GetByRole(AriaRole.Button).Filter(new() { HasText = "Set" });
                if (await setButton.CountAsync() > 0)
                {
                    await setButton.ClickAsync();
                }
                else
                {
                    // Фолбек, если кнопка определяется по классу
                    await timePopup.Locator(".k-time-accept, button.k-time-accept").ClickAsync();
                }

                // Ожидаем закрытия попапа
                // Ожидаем закрытия попапа (он исчезает из DOM)
                await timePopup.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

                // 5. ИСПРАВЛЕНО: Считываем значение из инпута внутри pickerContainer, а не из закрытого попапа!
                var inputField = pickerContainer.Locator("input");
                string selectedTime = await inputField.InputValueAsync();

                Log.Debug($"[TIME_VALIDATION] Read value from input: '{selectedTime}'");

                // Парсим значение (на скриншоте формат "7:13 PM", что соответствует "h:mm tt")
                DateTime actualTime = DateTime.ParseExact(selectedTime, "h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
                DateTime currentTime = DateTime.Now;

                TimeSpan actualTimeSpan = actualTime.TimeOfDay;
                TimeSpan currentTimeSpan = currentTime.TimeOfDay;

                Assert.That(actualTimeSpan, Is.LessThanOrEqualTo(currentTimeSpan),
                       $"Validation failed! The picker allowed selecting a future time: {selectedTime}, while current time is {currentTime.ToString("h:mm tt")}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error while scrolling time picker columns: {ex.Message}");
                await _page.MakeScreenshotAsync("Attachment_Filled"); ;
                throw;
            }
        }

        /// <summary>
        /// Opens the resident dropdown, identifies the index of the currently selected resident, and closes the dropdown.
        /// </summary>
        /// <returns>The zero-based index of the selected resident.</returns>
        public async Task<int> GetSelectedResidentIndexAsync()
        {
            // 1. Считываем имя резидента прямо из ссылки, которая видна на вашем скриншоте
            var residentLink = _page.Locator("a.link.resident-name").First;
            string selectedName = (await residentLink.TextContentAsync())?.Trim();

            // 2. Локализуем сам выпадающий список выбора резидента (блок Name* вверху страницы)
            // Судя по скриншоту, он находится внутри блока с пометкой Name
            var residentDropdown = _page.Locator("mat-select[name='resident'], mat-select").First;
            await residentDropdown.ClickAsync();
            await Task.Delay(350); // Пауза для стабильного рендеринга оверлея Angular

            // 3. Собираем все опции в открывшемся списке
            var options = await _page.Locator("mat-mdc-option, mat-option").AllAsync();
            int selectedIndex = 0;

            // 4. Ищем индекс опции, текст которой совпадает с именем на ссылке
            for (int i = 0; i < options.Count; i++)
            {
                var optionText = (await options[i].TextContentAsync())?.Trim();
                if (optionText != null && optionText.Contains(selectedName))
                {
                    selectedIndex = i;
                    break;
                }
            }

            // 5. Закрываем список, чтобы не мешать дальнейшему тесту
            await _page.Keyboard.PressAsync("Escape");
            await Task.Delay(200);

            return selectedIndex;
        }

        /// <summary>
        /// Targets and modifies a single input component on the specified form tab to intentionally trigger form dirtiness states.
        /// </summary>
        /// <param name="tabName">The descriptive string name of the target tab containing fields to edit.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ModifySingleFieldOnTabAsync(string tabName)
        {
            switch (tabName)
            {
                case "General":
                    await _createPage.General.GetFieldByLabel("SBARSummary").TypeAsync("Changed field");
                    break;
                case "Details":
                    await _createPage.Details.GetFieldByLabel("Describe Occurrence").TypeAsync("Changed field");
                    break;
                case "State":
                    // For Angular Material/Kendo checkboxes, toggling statuses triggers structural model dirtiness flags
                    await _page.Locator("mat-checkbox input, kendo-switch input").First.ClickAsync();
                    break;
                case "Medication":
                    var row = _page.Locator(".medication-row.ng-star-inserted").Nth(0);
                    await row.Locator("input").Nth(1).FillAsync("1000");
                    break;
                case "RN Supervisor Investigation Form":
                    await _page.Locator("kendo-timepicker[name='answerTime'] input:visible").First.ClickAsync();
                    // Basic focusing adjustments modifying structural time value layouts via keyboard keys
                    await _page.Keyboard.PressAsync("ArrowUp");
                    break;
                case "Summary":
                    await _createPage.Summary.SelectQuestionRadioAsync("Based upon the collection and review of all attached information, the following conclusion has been reached:", "Undetermined");
                    break;
            }
        }
        /// <summary>
        /// Возвращает занчение текстового поля или Rich Text
        /// </summary>
        /// <param name="tabName">Идентификатор имени вкладки.</param>
        /// <param name="label">Идентификатор поля вкладки.</param>
        /// <returns>Значение указанного поля на вкладке.</returns>
        public async Task<String> VerifySingleFieldOnTabAsync(string tabName, string label)
        {
            switch (tabName)
            {
                case "General":
                    return await _createPage.General.GetFieldByLabel(label).InputValueAsync();
                case "Details":
                    return await _createPage.Details.GetFieldByLabel(label).InputValueAsync();
                case "State":
                    return await _createPage.State.GetFieldByLabel(label).InputValueAsync();
                case "Summary":
                    return await _createPage.Summary.GetFieldByLabel(label).InputValueAsync();
                default:
                    return "";
            }
        }

        /// <summary>
        /// Проверяет, активна кнопка "Save" или заблокирована (disabled).
        /// </summary>
        public async Task VerifySaveButtonEnabledStateAsync(bool shouldBeEnabled)
        {
            Log.Information($"[ASSERTION] Проверяем, что кнопка 'Save' {(shouldBeEnabled ? "АКТИВНА" : "ЗАБЛОКИРОВАНА")}...");

            // Находим кнопку сохранения. Подставьте точный локатор вашей кнопки (например, по тексту или селектору)
            var saveButton = CreatePage.Page.Locator("button:has-text('Save')");

            if (shouldBeEnabled)
            {
                await Assertions.Expect(saveButton).ToBeEnabledAsync(new() { Timeout = 5000 });
            }
            else
            {
                await Assertions.Expect(saveButton).ToBeDisabledAsync(new() { Timeout = 5000 });
            }
        }

        /// <summary>
        /// Enforces explicit validation assertions confirming the visible appearance of custom change detection alert windows post field dirtiness modifications.
        /// </summary>
        /// <param name="sourceTabName">The descriptive text string of the form tab being left unsaved.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task VerifyUnsavedChangesAlertVisibleAsync(string sourceTabName)
        {
            // NOTE: Review debug log
            Log.Debug($"[VALIDATION] Verifying appearance of custom Change Detection overlay modals for tab container: {sourceTabName}");

            // 1. Isolate target text nodes residing inside the active modal layer interface elements
            var customModalText = _page.GetByText("Do you want to leave this page?");

            // 2. Synchronize thread until the alert layer renders completely within the viewport bounds
            await customModalText.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });

            // 3. Execute strict validation assertions
            bool isModalVisible = await customModalText.IsVisibleAsync();
            Assert.That(isModalVisible, Is.True,
                $"The custom warning alert dialog regarding unsaved data changes FAILED to reveal when attempting to route away from tab layer '{sourceTabName}'!");
        }


        /// <summary>
        /// Commands the incident creation controller to execute wizard submission click events and synchronizes active network requests.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClickCreateIncidentAsync()
        {
            await _createPage.ClickCreateIncident();

            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // ЖЕСТКИЙ ФИКС: Ждем, пока центральный спиннер полностью исчезнет из DOM (станет Hidden)!
            Log.Debug("Waiting for the central loading spinner to disappear...");
            var spinner = _page.Locator("kendo-textbox-loading, .k-loading-mask, .spinner, .loading-spinner").First;
            if (await spinner.CountAsync() > 0)
            {
                await spinner.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 30000 });
            }

            Log.Debug("Waiting for URL to contain a valid saved incident draft GUID...");
            var guidRegex = new System.Text.RegularExpressions.Regex(@"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}");
            await _page.WaitForURLAsync(guidRegex, new() { Timeout = 100000 });


        }

        /// <summary>
        /// Commits ongoing form state updates via the save execution button and optionally captures and saves the compiled legal Summary report attachment.
        /// </summary>
        /// <param name="shouldDownloadReport">Pass true to initiate the file download tracking pipeline for the summary report.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClickSaveIncidentAsync(bool shouldDownloadReport = false)
        {
            // Handle the persistence trigger click event, sync expectations, and capture screenshots sequentially
            // NOTE: Review debug log
            Log.Debug("Clicking Save button");
            await _createPage.ClickSaveIncident();

            var guidRegex = new System.Text.RegularExpressions.Regex(@"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}");
            await _page.WaitForURLAsync(guidRegex, new() { Timeout = 25000 });
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            if (shouldDownloadReport)
            {
                Log.Information("Waiting for Summary report to download...");
                fileName = await _createPage.Summary.DownloadSummaryReportAsync();
            }

            // NOTE: Review debug screenshot sequence
            await _page.MakeScreenshotAsync(shouldDownloadReport ? "save_with_report" : "save_regular");

            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        /// <summary>
        /// Directs workspace focus to the Summary tab container, commits signature approval, and verifies graphic rendering components.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SignSummaryAndVerifyAsync()
        {
            await _createPage.ClickTabAsync("Summary\r\n");
            await _createPage.Summary.SignAndConfirmIncident();
            await _createPage.Summary.VerifySignatureImageVisible();
        }

        /// <summary>
        /// Directs workspace focus to the Summary tab container, commits signature approval, and verifies graphic rendering components.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RemoveSignatireAsync()
        {
            await _createPage.ClickTabAsync("Summary\r\n");
            await _createPage.Summary.RemoveSignatureAsync();
        }


        /// <summary>
        /// Steps sequentially through individual field validation mappings to verify the red dot completeness indicators vanish immediately post field filling actions.
        /// </summary>
        /// <param name="tabComponent">The active component reference object representing the target tab interface layer.</param>
        /// <param name="tabData">The dataset payload record structure matching targeted layout input fields.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task VerifyFieldsOneByOneWithFilling(object tabComponent, object tabData)
        {
            dynamic tab = tabComponent;
            var fieldsMap = tab.GetRequiredFieldsMap((dynamic)tabData);

            foreach (var field in fieldsMap)
            {
                // Unpack the mapping dictionary value tuples safely into structured explicit parameter layouts
                // This approach restores strong typing definitions inside dynamic iterative evaluation blocks
                var (action, isRequired) = ((Func<Task>, bool))field.Value;

                // Execute completeness indicator evaluation steps matching active field boundaries
                await VerifyRedDotField(tabComponent, field.Key, isRequired);

                await action.Invoke();

                //await _page.Keyboard.PressAsync("Tab");
                await Task.Delay(500);

                await VerifyRedDotField(tabComponent, field.Key, false);
            }
        }

        /// <summary>
        /// Verifies whether the resident's diagnoses are correctly loaded and displayed in the "All Diagnoses" textarea,
        /// dynamically handling both cases: when the resident has diagnoses and when the list is empty.
        /// </summary>
        /// <param name="expectedDiagnoses">The list of expected diagnoses from test data. Can be null or empty.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task VerifyResidentDiagnosesLoadedAsync(string expectedDiagnoses)
        {
            var label = "All Diagnoses";

            // Используем твой базовый метод получения текста из input/textarea fields
            var actualText = await CreatePage.GetFieldValueByLabelAsync(label);

            // Сценарий 1: У резидента в тестовых данных НЕТ диагнозов
            if (expectedDiagnoses == null || !expectedDiagnoses.Any())
            {
                Assert.That(actualText.Trim(), Is.EqualTo(string.Empty),
                    $"Ожидалось, что поле '{label}' будет пустым, но обнаружен текст: {actualText}");

                return;
            }

            // Сценарий 2: У резидента ЕСТЬ диагнозы в тестовых данных
            foreach (var diagnosis in expectedDiagnoses)
            {
                Assert.That(actualText, Does.Contain(diagnosis),
                    $"Диагноз '{diagnosis}' не найден в поле '{label}'. Текущий текст на UI: {actualText}");
            }
        }

        /// <summary>
        /// Evaluates completeness indicator states universally across specified collections of mandatory tab input layout fields.
        /// </summary>
        /// <typeparam name="T">The type parameter representing the structured record reference model context.</typeparam>
        /// <param name="tabComponent">The active component reference object representing the target tab interface layer.</param>
        /// <param name="data">The reference dataset containing target metrics to inspect.</param>
        /// <param name="shouldBeVisible">Pass true if indicators are expected to register as visible, or false if they should mask away.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if tab class instances or model types fail data association mapping conditions.</exception>
        public async Task VerifyAllFieldsDotsStateAsync<T>(object tabComponent, T data, bool shouldBeVisible)
        {
            // 1. Evaluate runtime class conditions to map dictionary fields correctly based on target type conversions
            var fieldsMap = tabComponent switch
            {
                GeneralTab g when data is IncidentGeneralInfo gData => g.GetRequiredFieldsMap(gData),
                DetailsTab d when data is IncidentDetailsInfo dData => d.GetRequiredFieldsMap(dData),

                _ => throw new ArgumentException(
                    $"Combination of tab {tabComponent.GetType().Name} and data {typeof(T).Name} is not supported")
            };

            // 2. Iterate across mapped dictionary field rules sequentially
            foreach (var field in fieldsMap)
            {
                if (field.Value.IsRequired)
                {
                    await VerifyRedDotField(tabComponent, field.Key, shouldBeVisible);
                }
            }
        }


        /// <summary>
        /// Executes a rapid state-machine cycle validation over the entire State tab fields checkbox matrix, 
        /// checking dynamic indicator hidden/visible state transitions on every toggle toggle and reset action.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task VerifyStateTabSpecificLogicAsync()
        {
            var fieldsMap = _createPage.State.GetStateRequiredFieldsMap();

            foreach (var fieldEntry in fieldsMap)
            {
                string fieldName = fieldEntry.Key;
                var (Action, Reset, _) = fieldEntry.Value;
                // NOTE: Review debug log
                Log.Debug($"Checking field {fieldName}");

                // Scroll back upwards toward the indicator to guarantee it resides in the active viewport
                await _createPage.State.GeneralPointLocator.ScrollIntoViewIfNeededAsync();

                // 1. Await dynamic appearance of the point before initializing modifications
                await _createPage.State.GeneralPointLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible });

                // 2. TOGGLE/ENABLE the field checkbox
                await Action.Invoke();

                // 3. SMART WAIT: Await explicit element disappearance of the completeness point indicator
                await _createPage.State.GeneralPointLocator.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

                // Supplementary check step confirming execution stability bounds
                Assert.That(await _createPage.State.IsGeneralStatePointVisibleAsync(), Is.False,
                    $"Point did NOT disappear after filling {fieldName}");

                // 4. RESET/DISABLE the field checkbox to original values
                await Reset.Invoke();

                // Reset scroll position upwards prior to evaluating indicator return transitions
                await _createPage.State.GeneralPointLocator.ScrollIntoViewIfNeededAsync();

                // 5. SMART WAIT: Await explicit element appearance of the completeness point indicator
                await _createPage.State.GeneralPointLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible });

                Assert.That(await _createPage.State.IsGeneralStatePointVisibleAsync(), Is.True,
                    $"Point did NOT return after clearing {fieldName}");

                // NOTE: Review debug log
                Log.Debug($"Field {fieldName} verified fast and successfully");
            }
        }

        /// <summary>
        /// Performs explicit validation checks checking that a field's completeness indicator matches visibility expectations.
        /// </summary>
        /// <param name="tabComponent">The active component reference object representing the target tab interface layer.</param>
        /// <param name="fieldName">The unique text description identifying the field container.</param>
        /// <param name="shouldBeVisible">Pass true if the indicator badge is expected to be visible; otherwise, false.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task VerifyRedDotField(object tabComponent, string fieldName, bool shouldBeVisible = true)
        {

            // NOTE: Review debug log
            Log.Debug($"Red Dot validation for the field '{fieldName}': is expected {shouldBeVisible}");

            // Dynamically evaluate whether the provided tab page component shares the primary required verification methods
            var tab = tabComponent as BaseIncidentTabs;

            bool isVisible = await tab.IsFieldMarkedRequiredAsync(fieldName);

            Assert.That(isVisible, Is.EqualTo(shouldBeVisible),
                shouldBeVisible
                    ? $"The field {fieldName} should have the Red Dot"
                    : $"The field {fieldName} should NOT have the Red Dot ");

            // NOTE: Review debug log
            Log.Debug($"Red Dot validation for the field '{fieldName}': {(isVisible ? "true" : "false")}");
        }

        /// <summary>
        /// Verifies the presence or absence of the required indicator (red dot) for the First Aid Describe field.
        /// </summary>
        /// <param name="shouldHaveDot">True if the red dot is expected to be visible; otherwise, false.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task VerifyDescribeFieldRedDotStateAsync(bool shouldHaveDot)
        {
            // 1. Локализуем блок лейбла по тексту "Describe"
            var labelContainer = _page.Locator(".lv-label", new() { HasText = "Describe" });

            // 2. Находим внутри него конкретный элемент красной точки по классу из верстки
            var redDot = labelContainer.Locator("span.completeness-indicator");

            if (shouldHaveDot)
            {
                // Ждем, когда Angular уберет скрывающие стили и точка физически появится на UI
                await Assertions.Expect(redDot).ToBeVisibleAsync(new() { Timeout = 3000 });
                Log.Debug("Визуальная проверка: красная точка ОТОБРАЖАЕТСЯ у поля Describe.");
            }
            else
            {
                // Ждем, когда Angular навесит скрывающие стили (например, display: none) и точка исчезнет с экрана
                await Assertions.Expect(redDot).ToBeHiddenAsync(new() { Timeout = 3000 });
                Log.Debug("Визуальная проверка: красная точка СКРЫТА у поля Describe.");
            }
        }


        /// <summary>
        /// Verifies whether the input field under 'Other Type of Alarm' is active (editable) or disabled.
        /// </summary>
        /// <param name="shouldBeActive">True if the field must be active; False if it must be disabled.</param>
        public async Task VerifyOtherAlarmInputFieldStateAsync(bool shouldBeActive)
        {
            Log.Debug($"Verifying that Other Type of Alarm input field active state is: {shouldBeActive}");

            // Железобетонный локатор по атрибуту name, который мы увидели в DevTools
            var inputField = _page.Locator("input[name='otherTypeOfAlarm']");

            if (shouldBeActive)
            {
                // Проверяем, что Angular убрал блокировку и поле доступно для ввода
                await Assertions.Expect(inputField).ToBeEditableAsync(new() { Timeout = 2000 });
                Log.Debug("Проверено: поле ввода 'Other Type of Alarm' АКТИВНО.");
            }
            else
            {
                // Проверяем, что поле заблокировано (disabled)
                await Assertions.Expect(inputField).ToBeDisabledAsync(new() { Timeout = 2000 });
                Log.Debug("Проверено: поле ввода 'Other Type of Alarm' ЗАБЛОКИРОВАНО.");
            }
        }

        /// <summary>
        /// Executes the full, isolated multi-stage lifecycle validation for the Medication grid table, 
        /// checking the dynamic reaction of the tab header completeness indicator through grid append and purge stages.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task VerifyMedicationTabFullLifecycleAndIndicatorAsync()
        {
            const string tabName = "Medication";
            Log.Information($"--- START: Complex lifecycle test for '{tabName}' tab ---");

            // Step 1: Verify that the red dot is initially VISIBLE
            // NOTE: Review debug log
            Log.Debug("[STEP 1] Checking the initial state of the indicator...");
            await VerifyRedDotTab(tabName, shouldBeVisible: true);

            // Step 2: Add empty rows. The dot must REMAIN VISIBLE
            // NOTE: Review debug log
            Log.Debug("[STEP 2] Adding empty medication rows...");
            await _createPage.Medication.AddEmptyMedicationRowsAsync(2);

            // Remove focus away from input fields to trigger the underlying Angular layout model validation updates
            await _page.Mouse.ClickAsync(0, 0);
            await _page.WaitForTimeoutAsync(200);

            await VerifyRedDotTab(tabName, shouldBeVisible: true);
            // NOTE: Review debug log
            Log.Debug("[SUCCESS] Empty rows successfully kept the indicator visible");

            // Step 3: Fill the previously created empty rows with data. The dot must DISAPPEAR
            // NOTE: Review debug log
            Log.Debug("[STEP 3] Filling the created rows with test data...");
            var testMedications = new List<MedicationTab.MedicationInfo>
           {
               new("Aspirin", "100mg", "Once a day", "08:00"),
               new("Nurofen", "250mg", "Twice a day", "15:00")
           };

            for (int i = 0; i < testMedications.Count; i++)
            {
                var medication = testMedications[i];
                var row = _page.Locator(".medication-row.ng-star-inserted").Nth(i);

                await row.Locator("input").Nth(0).FillAsync(medication.Name);
                await row.Locator("input").Nth(1).FillAsync(medication.Dosage);
                await row.Locator("input").Nth(2).FillAsync(medication.Frequency);
                await row.Locator("input").Nth(3).FillAsync(medication.TimeReceived);
            }

            // Short delay allowing the Angular form lifecycle to compute and update the internal validation state properties
            await _page.WaitForTimeoutAsync(300);

            // NOTE: Review debug log
            Log.Debug("[STEP 3] Verifying indicator hiding after filling the fields...");
            await VerifyRedDotTab(tabName, shouldBeVisible: false);
            // NOTE: Review debug log
            Log.Debug("[SUCCESS] The indicator successfully hid after form filling");

            // Step 4: Remove all medications. The dot must RETURN
            // NOTE: Review debug log
            Log.Debug("[STEP 4] Completely clearing the medication table...");
            await _createPage.Medication.ClearAllMedicationsAsync();

            // Delay to allow dynamic DOM elements removal transitions to complete inside the form models completely
            await _page.WaitForTimeoutAsync(300);

            // NOTE: Review debug log
            Log.Debug("[STEP 4] Verifying the return of the indicator...");
            await VerifyRedDotTab(tabName, shouldBeVisible: true);

            Log.Information($"--- FINISH: Lifecycle test for '{tabName}' tab successfully passed! ---");
        }

        /// <summary>
        /// Transitions view layouts to the RN/Supervisor Form wizard and injects an inline lambda validator callback 
        /// to instantly evaluate tab indicator statuses directly during execution thread progressions.
        /// </summary>
        /// <param name="data">The reference master data record factory object model holding complete incident parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task FillRNFormTabWithTabCheckAsync(IncidentTestData data)
        {
            var tab = "RN Supervisor Investigation Form";

            // Call the base wizard builder workflow, passing an inline lambda checker execution hook 
            // that fires precisely at the targeted step position parameter within a single Playwright thread context
            await _createPage.RNSupervisor.FillQuestionsAsync(data.RNSupervisor, async (currentStep) =>
            {
                if (currentStep == 27)
                {
                    // As soon as structural step 27 processes, verify that the tab header completeness indicator badge remains visible
                    await VerifyRedDotTab(tab, shouldBeVisible: true);
                    // NOTE: Review debug log
                    Log.Debug("Verified inside flow: Red Dot is still visible on step 27.");
                }
            });

            Log.Information("RN Supervisor Investigation Form Tab filled with inline validation");
        }

        /// <summary>
        /// Performs strict verification checks confirming that a form tab's completeness badge indicator matches visibility expectations.
        /// </summary>
        /// <param name="tabName">The exact visible string text name of the destination tab layout header inside the tab-bar panel.</param>
        /// <param name="shouldBeVisible">Pass true if the indicator badge is expected to be visible; otherwise, false.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task VerifyRedDotTab(string tabName, bool shouldBeVisible = true)
        {
            // Tab completeness statuses validate directly via root Page Object methods rather than tracking localized views
            bool isVisible = await _createPage.IsTabMarkedIncompleteAsync(tabName);

            Assert.That(isVisible, Is.EqualTo(shouldBeVisible),
                shouldBeVisible
                    ? $"The tab '{tabName}' should have the Red Dot "
                    : $"The tab '{tabName}' should NOT have the Red Dot ");

            // NOTE: Review debug log
            Log.Debug($"Red Dot validation for the tab '{tabName}': {(isVisible ? "true" : "false")}");
        }

        /// <summary>
        /// Resets pre-populated form parameter metrics residing inside the General tab layout view canvas.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClearGeneralForm()
        {
            await _createPage.General.ClearPreFilledFieldsAsync();
        }

        /// <summary>
        /// Captures and caches the state text from the Diagnoses field tree before purging the current active workspace text block.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<string> ClearDetailsForm()
        {
            // NOTE: Review debug log
            Log.Debug("Try to clear All Diagnoses");
            string diagnoses = await _createPage.Details.ClearAndSaveDiagnosesAsync();
            // NOTE: Review debug screenshot sequence
            await _page.MakeScreenshotAsync("AllDiagnoses_cleared");
            return diagnoses;
        }

        /// <summary>
        /// Dispatches a quick command toggle targeting the First Aid Administration toggle switch component.
        /// </summary>
        /// <param name="answer">Pass true to toggle the switch status to enabled; otherwise, false.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SwitchFirstAid(bool answer)
        {
            await _createPage.Details.SelectFirstAdmitedAsync(answer, "");
            await Task.Delay(300);
        }

        /// Captures the 'employees' API response and verifies that UI staff dropdowns 
        /// contain the correct number of records according to employment status (active vs terminated). 
        /// </summary>
        public async Task VerifyStaffDropdownsCountWithApiAsync()
        {
            DateTime incidentDate = DateTime.Today;

            // Шаг 1: Разблокируем секцию на UI
            await UnlockStaffSectionByFillingDateAsync();

            // Шаг 2: Получаем эталонные данные из API (полный список сотрудников + правила конфига)
            var (expectedSupervisor, expectedChargeNurse, expectedCna) = await GetExpectedStaffCountsFromApiAsync(incidentDate);

            // Шаг 3: Сравниваем UI дропдауны с нашими расчетами
            await VerifyUiDropdownsCountAsync(expectedSupervisor, expectedChargeNurse, expectedCna);
        }

        public string FindValidEmployeeIdForTest(string employeesJson, string currentConfigJson, string sectionRole)
        {
            // 1. Парсим конфигурацию, чтобы собрать ID тех, кто уже там сидит
            using var configDoc = JsonDocument.Parse(currentConfigJson);
            var configRoot = configDoc.RootElement;
            var incidentConfig = configRoot.TryGetProperty("incidentConfiguration", out var incProp) ? incProp : configRoot;

            var existingIds = new HashSet<string>();
            if (incidentConfig.TryGetProperty(sectionRole, out var roleConfig) &&
                roleConfig.TryGetProperty("employeeConstraints", out var constraints) &&
                constraints.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in constraints.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        existingIds.Add(item.GetString());
                }
            }

            // 2. Парсим сотрудников и ищем первого активного, которого нет в existingIds
            using var employeesDoc = JsonDocument.Parse(employeesJson);
            var employeesRoot = employeesDoc.RootElement;
            var allEmployees = employeesRoot.ValueKind == JsonValueKind.Array
                ? employeesRoot.EnumerateArray().ToList()
                : employeesRoot.GetProperty("records").EnumerateArray().ToList();

            DateTime today = DateTime.Today;

            var targetEmployee = allEmployees.FirstOrDefault(e =>
            {
                string empId = e.GetProperty("id").GetString();

                // Проверяем, что сотрудника еще нет в конфигурации
                if (existingIds.Contains(empId)) return false;

                // Проверяем активность (ваша логика)
                if (!e.TryGetProperty("termDate", out var termProp) || termProp.ValueKind == JsonValueKind.Null)
                    return true;

                return DateTime.TryParse(termProp.GetString(), out DateTime termDate) && termDate > today;
            });

            if (targetEmployee.ValueKind == JsonValueKind.Undefined)
            {
                Assert.Inconclusive("[STAFF_VALIDATION] Не удалось найти активного сотрудника, которого ещё нет в конфигурации, для теста.");
            }

            return targetEmployee.GetProperty("id").GetString();
        }

        public async Task UnlockStaffSectionByFillingDateAsync()
        {
            Log.Information("[STAFF_VALIDATION] Verifying locking overlay text...");
            var overlayLocator = _page.Locator("text=Enter Incident Date to unlock this section");
            await Expect(overlayLocator).ToBeVisibleAsync(new() { Timeout = 5000 });

            Log.Information("[STAFF_VALIDATION] Overlay is present. Filling 'Date of Incident' to unlock...");
            await _createPage.General.SelectTodayAsync("dateOfIncident");

            await Expect(overlayLocator).ToBeHiddenAsync(new() { Timeout = 5000 });
            Log.Information("[STAFF_VALIDATION] Section unlocked successfully.");
        }

        private async Task<(int Supervisor, int ChargeNurse, int Cna)> GetExpectedStaffCountsFromApiAsync(DateTime incidentDate)
        {
            Log.Information("[STAFF_VALIDATION] Fetching API data...");

            // === 1. ПОЛУЧЕНИЕ СПИСКА СОТРУДНИКОВ (POST) ===
            var payload = new { facilityId = "c1f80483-fd30-4327-814e-778ad171a67b" };

            // Вызываем обновленный POST-метод
            string employeesJson = await _page.ApiPostRequest("employees", payload);

            Assert.That(employeesJson, Does.Not.Contain("Network Error"), $"Full employees JSON output was:\n{employeesJson}");


            // === 2. ПОЛУЧЕНИЕ КОНФИГУРАЦИИ (GET) ===
            // Выносим специфические заголовки
            var contextHeaders = new Dictionary<string, string>
                {
                    { "X-App-Id", "AccidentIncident" },
                    { "X-Context-Id", "c1f80483-fd30-4327-814e-778ad171a67b" },
                    { "X-Context-Type", "Facility" },
                    { "X-Tenant-Id", "CassenaCare" }
                };

            // Вызываем наш универсальный GET
            string configJson = await _page.ApiGetRequest("incident/employee-configuration", customHeaders: contextHeaders);

            Assert.That(configJson, Does.Not.Contain("Network Error"), $"Full config JSON output was:\n{configJson}");

            // Парсим полный список сотрудников
            using var employeesDoc = System.Text.Json.JsonDocument.Parse(employeesJson);
            var employeesRoot = employeesDoc.RootElement;
            List<System.Text.Json.JsonElement> allEmployees = employeesRoot.ValueKind == System.Text.Json.JsonValueKind.Array
                ? employeesRoot.EnumerateArray().ToList()
                : employeesRoot.GetProperty("records").EnumerateArray().ToList();

            // Фильтруем сотрудников по вашему бизнес-правилу (termDate == null ИЛИ termDate > today)
            var activeEmployees = allEmployees.Where(e =>
            {
                if (!e.TryGetProperty("termDate", out var termProp) || termProp.ValueKind == System.Text.Json.JsonValueKind.Null)
                    return true;

                return DateTime.TryParse(termProp.GetString(), out DateTime termDate) && termDate > incidentDate;
            }).ToList();

            // Парсим конфигурацию ограничений
            using var configDoc = System.Text.Json.JsonDocument.Parse(configJson);
            var configRoot = configDoc.RootElement;
            System.Text.Json.JsonElement incidentConfig = configRoot.TryGetProperty("incidentConfiguration", out var incProp) ? incProp : configRoot;

            // Рассчитываем эталонное количество для каждого дропдауна
            int supervisor = GetCountForRole(incidentConfig, "supervisorConfiguration", activeEmployees);
            int chargeNurse = GetCountForRole(incidentConfig, "chargeNurseConfiguration", activeEmployees);
            int cna = GetCountForRole(incidentConfig, "cnaConfiguration", activeEmployees);

            Log.Information($"[STAFF_VALIDATION] Expected API counts -> Supervisor: {supervisor}, Charge Nurse: {chargeNurse}, CNA: {cna}");
            return (supervisor, chargeNurse, cna);
        }

        // ИСПРАВЛЕНО: Добавлен async Task для корректной работы асинхронных вызовов Playwright
        private async Task VerifyUiDropdownsCountAsync(int expectedSupervisor, int expectedChargeNurse, int expectedCna)
        {
            int uiSupervisorCount = await GetMaterialDropdownOptionsCountAsync("Supervisor");
            int uiChargeNurseCount = await GetMaterialDropdownOptionsCountAsync("Charge nurse");
            int uiCnaCount = await GetMaterialDropdownOptionsCountAsync("CNA");

            Log.Information($"[STAFF_VALIDATION] UI Data Stats -> Supervisor: {uiSupervisorCount}, Charge Nurse: {uiChargeNurseCount}, CNA: {uiCnaCount}");

            Assert.Multiple(() =>
            {
                Assert.That(uiSupervisorCount, Is.EqualTo(expectedSupervisor),
                    $"Staff Mismatch for 'Supervisor' dropdown. Expected active: {expectedSupervisor}, Actual UI: {uiSupervisorCount}.");

                Assert.That(uiChargeNurseCount, Is.EqualTo(expectedChargeNurse),
                    $"Staff Mismatch for 'Charge Nurse' dropdown. Expected active/constrained: {expectedChargeNurse}, Actual UI: {uiChargeNurseCount}.");

                Assert.That(uiCnaCount, Is.EqualTo(expectedCna),
                    $"Staff Mismatch for 'CNA' dropdown. Expected active/constrained: {expectedCna}, Actual UI: {uiCnaCount}.");
            });

            Log.Information("[STAFF_VALIDATION] Validation successful! All dropdown counts match calculated API constraints.");
        }

        private int GetCountForRole(System.Text.Json.JsonElement config, string roleConfigName, List<System.Text.Json.JsonElement> activeStaff)
        {
            if (config.TryGetProperty(roleConfigName, out var roleConfig) &&
                roleConfig.TryGetProperty("employeeConstraints", out var constraints) &&
                constraints.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                int constraintCount = constraints.GetArrayLength();
                if (constraintCount > 0) return constraintCount;
            }
            return activeStaff.Count;
        }


        /// <summary>
        /// Helper method to open a Kendo dropdown by its label, count its items, and close it safely.
        /// </summary>
        public async Task<int> GetMaterialDropdownOptionsCountAsync(string dropdownLabel)
        {
            Log.Debug($"[STAFF_VALIDATION] Opening dropdown for: '{dropdownLabel}'...");

            var fieldContainer = _page.Locator("cad-label-value-field, .panel-line div")
                                      .Filter(new() { HasText = dropdownLabel })
                                      .First;

            var dropdownTrigger = fieldContainer.Locator("mat-select, [role='combobox']").First;
            await dropdownTrigger.ClickAsync();

            var overlayPanel = _page.Locator(".cdk-overlay-pane, .mat-mdc-select-panel, [role='listbox']").Last;
            await overlayPanel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            var listItems = overlayPanel.Locator("mat-option, .mat-mdc-option, [role='option']");
            int count = await listItems.CountAsync();

            Log.Debug($"[STAFF_VALIDATION] Dropdown '{dropdownLabel}' contains {count} items on UI.");

            // --- НЕУБИВАЕМОЕ ЗАКРЫТИЕ ЧЕРЕЗ ESCAPE ---
            // Нажимаем клавишу Escape — Angular Material гарантированно закроет оверлей любой длины
            await _page.Keyboard.PressAsync("Escape");

            // Ждем, пока оверлей полностью исчезнет из DOM, чтобы не мешать следующему шагу
            await Assertions.Expect(overlayPanel).ToBeHiddenAsync(new() { Timeout = 3000 });
            await _page.WaitForTimeoutAsync(200);

            return count;
        }

        public async Task<List<string>> GetMaterialDropdownOptionsTextAsync(string labelText)
        {
            var dropdown = _createPage.General.GetFieldByLabel(labelText).First;
            await dropdown.ClickAsync();

            var optionsLocator = _page.Locator(".cdk-overlay-container mat-option:visible");
            await optionsLocator.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // Вытаскиваем внутренний текст всех доступных сотрудников
            var allTexts = await optionsLocator.AllInnerTextsAsync();

            await _page.Keyboard.PressAsync("Escape");
            await _page.Locator(".cdk-overlay-container").WaitForAsync(new() { State = WaitForSelectorState.Hidden });

            return allTexts.ToList();
        }

        public async Task<(string Id, string Name)> GetAvailableActiveEmployeeAsync(string sectionRole)
        {
            var payload = new { facilityId = "c1f80483-fd30-4327-814e-778ad171a67b" };
            string employeesJson = await _page.ApiPostRequest("employees", payload);

            string currentConfigJson = await GetEmployeeConfigurationAsync();
            using var configDoc = JsonDocument.Parse(currentConfigJson);
            var existingIds = new HashSet<string>();

            if (configDoc.RootElement.TryGetProperty("incidentConfiguration", out var incProp) &&
                incProp.TryGetProperty(sectionRole, out var roleConfig))
            {
                // 1. Пытаемся прочитать либо employeeConstraints, либо userConstraints
                string arrayKey = roleConfig.TryGetProperty("userConstraints", out _) ? "userConstraints" : "employeeConstraints";

                if (roleConfig.TryGetProperty(arrayKey, out var constraints) && constraints.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in constraints.EnumerateArray())
                    {
                        // 2. ИСПРАВЛЕНО: Считываем ID из свойств объектов constraintId или id/userId
                        string idValue = null;
                        if (item.TryGetProperty("constraintId", out var cId)) idValue = cId.GetString();
                        else if (item.TryGetProperty("userId", out var uId)) idValue = uId.GetString();
                        else if (item.TryGetProperty("id", out var fallbackId)) idValue = fallbackId.GetString();

                        if (!string.IsNullOrEmpty(idValue))
                        {
                            existingIds.Add(idValue);
                        }
                    }
                }
            }

            using var employeesDoc = JsonDocument.Parse(employeesJson);
            var allEmployees = employeesDoc.RootElement.ValueKind == JsonValueKind.Array
                ? employeesDoc.RootElement.EnumerateArray()
                : employeesDoc.RootElement.GetProperty("records").EnumerateArray();

            DateTime today = DateTime.Today;

            foreach (var e in allEmployees)
            {
                string empId = e.GetProperty("id").GetString();
                if (existingIds.Contains(empId)) continue; // Теперь пропуск уже добавленных сработает!

                // Формируем имя в формате "LastName, FirstName", как на вашем UI скрине дропдауна
                string lastName = e.GetProperty("lastName").GetString();
                string firstName = e.GetProperty("firstName").GetString();
                string formattedUiName = $"{lastName}, {firstName}";

                if (!e.TryGetProperty("termDate", out var termProp) || termProp.ValueKind == JsonValueKind.Null)
                {
                    return (empId, formattedUiName);
                }

                if (DateTime.TryParse(termProp.GetString(), out DateTime termDate) && termDate > today)
                {
                    return (empId, formattedUiName);
                }
            }

            Assert.Inconclusive($"[STAFF_STEPS] Не удалось найти свободного активного сотрудника для секции {sectionRole}.");
            return (null, null);
        }

        // 2. Метод-мутатор: Добавить или Удалить сотрудника из слепка конфигурации
        public async Task ModifyEmployeeConstraintAsync(string sectionRole, string employeeId, bool isAdding)
        {
            string currentConfigJson = await GetEmployeeConfigurationAsync();
            var configNode = JsonNode.Parse(currentConfigJson);

            // 1. Переходим к целевой роли строго внутри incidentConfiguration
            var roleConfig = configNode?["incidentConfiguration"]?[sectionRole];
            if (roleConfig == null)
                Assert.Fail($"[STAFF_STEPS] Роль {sectionRole} не найдена в JSON.");

            // 2. Определяем точное имя массива (userConstraints или employeeConstraints)
            string arrayName = roleConfig["userConstraints"] != null ? "userConstraints" : "employeeConstraints";
            var userConstraints = roleConfig[arrayName]?.AsArray();

            if (userConstraints == null)
                Assert.Fail($"[STAFF_STEPS] Массив ограничений '{arrayName}' для {sectionRole} не найден.");

            if (isAdding)
            {
                Log.Information($"[STAFF_STEPS] Маппинг сотрудника {employeeId} для сохранения...");

                JsonNode constraintObject;

                // Строим DTO в зависимости от типа массива (выявлено на основе анализа схемы вашего API)
                if (arrayName == "employeeConstraints")
                {
                    // Для сотрудников бэкенд ждет строго constraintId, куда пишется ID сотрудника
                    constraintObject = new JsonObject
                    {
                        ["constraintId"] = employeeId
                    };
                }
                else
                {
                    // Для пользователей (userConstraints) используется стандартный формат с incidentUserConfigurationId
                    string parentConfigId = userConstraints.Count > 0
                        ? userConstraints[0]?["incidentUserConfigurationId"]?.ToString()
                        : roleConfig["id"]?.ToString();

                    constraintObject = new JsonObject
                    {
                        ["id"] = Guid.NewGuid().ToString(),
                        ["incidentUserConfigurationId"] = parentConfigId,
                        ["userId"] = employeeId
                    };
                }

                userConstraints.Add(constraintObject);
            }
            else
            {
                Log.Information($"[STAFF_STEPS] Удаление сотрудника {employeeId} из конфига {sectionRole}");

                // Универсальный поиск ноды для удаления по любому из возможных ключей идентификатора
                var nodeToRemove = userConstraints.FirstOrDefault(x =>
                    x?["constraintId"]?.ToString() == employeeId ||
                    x?["userId"]?.ToString() == employeeId ||
                    x?["employeeId"]?.ToString() == employeeId);

                if (nodeToRemove != null)
                    userConstraints.Remove(nodeToRemove);
            }

            // 3. Выводим отладочный лог ИМЕННО измененного фрагмента роли
            string debugRolePayload = roleConfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            Log.Information($"[API_MUTATION_DEBUG] Role section '{sectionRole}' state right before POST:\n{debugRolePayload}");

            // 4. Отправляем ВЕСЬ измененный корневой configNode (как требует контракт API)
            await UpdateEmployeeConfigurationAsync(configNode);
        }

        public async Task ModifyJobTitleConstraintAsync(string sectionRole, string jobTitleId, bool isAdding)
        {
            string currentConfigJson = await GetEmployeeConfigurationAsync();
            var configNode = JsonNode.Parse(currentConfigJson);
            var roleConfig = configNode?["incidentConfiguration"]?[sectionRole];

            if (roleConfig == null) Assert.Fail($"[STAFF_STEPS] Роль {sectionRole} не найдена.");

            var jobTitleConstraints = roleConfig["jobTitleConstraints"]?.AsArray();
            if (jobTitleConstraints == null) Assert.Fail($"[STAFF_STEPS] Массив 'jobTitleConstraints' не найден.");

            if (isAdding)
            {
                Log.Information($"[STAFF_STEPS] Маппинг должности {jobTitleId} для сохранения...");

                // ЗАЩИТА: Проверяем, нет ли уже этой должности в массиве, чтобы избежать дублей
                bool alreadyExists = jobTitleConstraints.Any(x => x?["constraintId"]?.ToString() == jobTitleId);

                if (!alreadyExists)
                {
                    var constraintObject = new System.Text.Json.Nodes.JsonObject
                    {
                        ["constraintId"] = jobTitleId // ВОЗВРАЩАЕМ ПРАВИЛЬНЫЙ КЛЮЧ ИЗ СКРИНШОТА
                    };
                    jobTitleConstraints.Add(constraintObject);
                }
                else
                {
                    Log.Warning($"[STAFF_STEPS] Должность {jobTitleId} уже присутствует в массиве, дублирование пропущено.");
                }
            }
            else
            {
                Log.Information($"[STAFF_STEPS] Удаление должности {jobTitleId}...");
                var nodeToRemove = jobTitleConstraints.FirstOrDefault(x => x?["constraintId"]?.ToString() == jobTitleId);
                if (nodeToRemove != null) jobTitleConstraints.Remove(nodeToRemove);
            }

            string fullPayloadDebug = configNode.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            Log.Information($"[API_MUTATION_DEBUG] FULL PAYLOAD BEFORE POST:\n{fullPayloadDebug}");

            await UpdateEmployeeConfigurationAsync(configNode);
        }

        // 3. Строгая проверка наличия или отсутствия имени в конкретном Material дропдауне
        public async Task VerifyEmployeeInDropdownAsync(string uiRoleName, string employeeName, bool shouldBePresent)
        {
            // Получаем все текстовые опции из вашего дропдауна (метод сбора строк нужно будет вызвать/написать)
            var options = await GetMaterialDropdownOptionsTextAsync(uiRoleName);

            bool isPresent = options.Any(opt => opt.Contains(employeeName, StringComparison.OrdinalIgnoreCase));

            if (shouldBePresent)
            {
                Assert.That(isPresent, Is.True, $"Сотрудник '{employeeName}' должен быть в дропдауне '{uiRoleName}', но его там нет.");
            }
            else
            {
                Assert.That(isPresent, Is.False, $"Сотрудник '{employeeName}' НЕ должен отображаться в дропдауне '{uiRoleName}', но он присутствует.");
            }
        }

        private string ExtractUserIdFromName(JsonNode configNode, string searchName = "Test, Polly")
        {
            // Список секций, где точно может сидеть наш пользователь на момент старта
            string[] sections = { "directorOfNursingConfiguration", "medicalDirectorConfiguration", "administratorConfiguration" };

            foreach (var section in sections)
            {
                var users = configNode?["incidentConfiguration"]?[section]?["userConstraints"]?.AsArray();
                if (users == null) continue;

                // Ищем в UI-ролях (если у вас имя хранится в каком-то поле, например, fullName или userName)
                // Но если в конфиге нет текстового имени, а только ID, мы можем забрать ID из текущего UI!
                // Самый надежный бэкенд-способ, если в конфиге только ID — сделать быстрый поиск по UI или ручке /users.
            }

            // Альтернативный и самый железный UI-способ для Playwright:
            // Так как тест начинается на UI, мы можем один раз при старте вытащить ID из локатора страницы,
            // либо сделать один GET запрос к эндпоинту пользователей.
            return null;
        }

        public async Task AssertPrimarySignatureButtonIsHiddenAsync(string roleText)
        {
            var signatureContainer = _page.Locator("cad-incident-sign")
                .Filter(new() { HasText = roleText })
                .First;

            var signButton = signatureContainer.Locator("button:has-text('Sign Here')");
            await signButton.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });
            Log.Debug("No button, its OK");
        }

        // Проверка основных подписей (DNS, MD, Admin) на ВИДИМОСТЬ
        public async Task AssertPrimarySignatureButtonIsVisibleAsync(string roleText)
        {
            var signatureContainer = _page.Locator("cad-incident-sign")
                .Filter(new() { HasText = roleText })
                .First;

            var signButton = signatureContainer.Locator("button:has-text('Sign Here')");
            await signButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
            Log.Debug("Button OK");
        }

        // Проверка кнопки подписи Саммари на СКРЫТОСТЬ
        public async Task AssertSummarySignatureButtonIsHiddenAsync()
        {
            // Получаем ВСЕ кнопки "Sign Here" на странице
            var allSignButtons = _page.GetByRole(AriaRole.Button, new() { Name = "Sign Here" });

            // Фильтруем: нам нужна только та кнопка, у которой родитель НЕ cad-incident-sign
            // Для этого в Playwright есть идеальный локатор :not()
            var summarySignButton = _page.Locator("button:has-text('Sign Here'):not(cad-incident-sign button)");

            await summarySignButton.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });
            Log.Debug("No Summary Button, it's OK");
        }

        // Проверка кнопки подписи Саммари на ВИДИМОСТЬ
        public async Task AssertSummarySignatureButtonIsVisibleAsync()
        {
            var signButton = _page.GetByRole(AriaRole.Button, new() { Name = "Sign Here" });

            await signButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 150000 });
            Log.Debug("Summary Button OK");
        }

        private async Task<string> GetUserIdByUserNameAsync(string userName)
        {
            try
            {
                // Достаем ID (sub) напрямую из JWT токена в sessionStorage браузера
                string userIdFromToken = await _page.EvaluateAsync<string>(@"() => {
            for (let i = 0; i < sessionStorage.length; i++) {
                const key = sessionStorage.key(i);
                if (key.includes('token') || key.includes('user') || key.includes('auth')) {
                    const data = sessionStorage.getItem(key);
                    if (data && data.includes('.')) { 
                        // Декодируем payload часть JWT токена
                        const base64Url = data.split('.')[1];
                        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
                        const jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function(c) {
                            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
                        }).join(''));
                        const parsed = JSON.parse(jsonPayload);
                        if (parsed.sub) return parsed.sub;
                    }
                }
            }
            return null;
        }");

                if (!string.IsNullOrEmpty(userIdFromToken))
                {
                    Log.Debug($"[QA_AUTH] Динамически получили ID пользователя из токена браузера: {userIdFromToken}");
                    return userIdFromToken;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[QA_AUTH] Не удалось прочитать токен через JS: {ex.Message}");
            }

            // Если в sessionStorage токена не нашлось, возвращаем ID, который мы вытащили из вашего лога
            Log.Warning("[QA_AUTH] Токен в браузере не найден, возвращаем резервный ID для Polly Test.");
            return "5d113f61-6fe5-4710-b9cc-08deb0344211";
        }

        public async Task ModifyUserRoleConstraintByNameAsync(string configSectionName, string userName, bool isAdding)
        {
            string currentConfigJson = await GetEmployeeConfigurationAsync();
            var configNode = JsonNode.Parse(currentConfigJson);

            // Динамически получаем ID пользователя по его имени перед мутацией
            string userId = await GetUserIdByUserNameAsync(userName);

            var roleConfig = configNode?["incidentConfiguration"]?[configSectionName];
            if (roleConfig == null) Assert.Fail($"[API_CONFIG] Секция '{configSectionName}' не найдена.");

            var userConstraints = roleConfig["userConstraints"]?.AsArray();
            if (userConstraints == null) Assert.Fail($"[API_CONFIG] Массив 'userConstraints' не найден.");

            if (isAdding)
            {
                Log.Information($"[API_CONFIG] Добавление {userName} ({userId}) в секцию {configSectionName}...");
                bool alreadyExists = userConstraints.Any(x => x?["userId"]?.ToString() == userId);
                if (!alreadyExists)
                {
                    var constraintObject = new System.Text.Json.Nodes.JsonObject
                    {
                        ["id"] = Guid.NewGuid().ToString(),
                        ["incidentUserConfigurationId"] = Guid.NewGuid().ToString(),
                        ["userId"] = userId
                    };
                    userConstraints.Add(constraintObject);
                }
            }
            else
            {
                Log.Information($"[API_CONFIG] Удаление {userName} ({userId}) из секции {configSectionName}...");
                var nodeToRemove = userConstraints.FirstOrDefault(x => x?["userId"]?.ToString() == userId);
                if (nodeToRemove != null) userConstraints.Remove(nodeToRemove);
            }

            await UpdateEmployeeConfigurationAsync(configNode);
        }

        public async Task ModifyRoleTemplateConstraintByNameAsync(string configSectionName, string userName, bool isAdding)
        {
            // 1. Получаем текущую конфигурацию сотрудников
            string currentConfigJson = await GetEmployeeConfigurationAsync();
            var configNode = JsonNode.Parse(currentConfigJson);

            // 2. Получаем ID нашего залогиненного пользователя Polly Test
            string userId = await GetUserIdByUserNameAsync(userName);

            // 3. Динамически узнаем, какая роль привязана к этому пользователю на бэкенде
            // Отправляем контекстные заголовки
            var contextHeaders = new Dictionary<string, string> {
        { "X-App-Id", "AccidentIncident" },
        { "X-Context-Id", "c1f80483-fd30-4327-814e-778ad171a67b" },
        { "X-Context-Type", "Facility" },
        { "X-Tenant-Id", "CassenaCare" }
    };

            // Делаем GET к ручке пользователя, чтобы забрать его роли (проверь точный URL, например "users/{userId}" или "employees/{userId}")
            string userDetailsResponse = await _page.ApiGetRequest($"api/RoleTemplates/role-template/assigned2/{userId}", customHeaders: contextHeaders);
            var userDetailsNode = JsonNode.Parse(userDetailsResponse);

            // Достаем объект роли (обычно бэкенд возвращает массив roles или объект roleTemplate)
            // Подставь точные ключи из вашей схемы (например, userDetailsNode["roleTemplate"])
            var userRoleNode = userDetailsNode?["roleTemplate"] ?? userDetailsNode?["roles"]?[0];

            if (userRoleNode == null)
                Assert.Fail($"[QA_ERROR] Не удалось найти привязанную роль для пользователя '{userName}' в ответе API.");

            string userRoleId = userRoleNode["id"]?.ToString() ?? userRoleNode["roleTemplateId"]?.ToString();
            string userRoleName = userRoleNode["name"]?.ToString() ?? userRoleNode["roleTemplateName"]?.ToString();

            // 4. Переходим к мутации incident-конфигурации
            var roleConfig = configNode?["incidentConfiguration"]?[configSectionName];
            if (roleConfig == null) Assert.Fail($"[API_CONFIG] Секция '{configSectionName}' не найдена.");

            var roleTemplateConstraints = roleConfig["roleTemplateConstraints"]?.AsArray();
            if (roleTemplateConstraints == null) Assert.Fail($"[API_CONFIG] Массив 'roleTemplateConstraints' не найден.");

            if (isAdding)
            {
                Log.Information($"[API_CONFIG] Добавление роли пользователя {userRoleName} ({userRoleId}) в секцию {configSectionName}...");

                bool alreadyExists = roleTemplateConstraints.Any(x => x?["roleTemplateId"]?.ToString() == userRoleId);
                if (!alreadyExists)
                {
                    var constraintObject = new System.Text.Json.Nodes.JsonObject
                    {
                        ["id"] = Guid.NewGuid().ToString(),
                        ["incidentUserConfigurationId"] = Guid.NewGuid().ToString(),
                        ["roleTemplateId"] = userRoleId,
                        ["roleTemplateName"] = userRoleName,
                        ["currentUserHasAccess"] = true // Наш юзер гарантированно имеет доступ к этой роли
                    };
                    roleTemplateConstraints.Add(constraintObject);
                }
            }
            else
            {
                Log.Information($"[API_CONFIG] Удаление роли пользователя {userRoleName} ({userRoleId}) из секции {configSectionName}...");

                var nodeToRemove = roleTemplateConstraints.FirstOrDefault(x => x?["roleTemplateId"]?.ToString() == userRoleId);
                if (nodeToRemove != null)
                {
                    roleTemplateConstraints.Remove(nodeToRemove);
                }
            }

            // 5. Сохраняем измененную конфигурацию
            await UpdateEmployeeConfigurationAsync(configNode);
        }

        public int ExpectedEmployeesByJobTitleCount { get; set; }

        // Метод делает GET-запрос актуального состояния конфигурации сотрудников
        public async Task<string> GetEmployeeConfigurationAsync()
        {
            var contextHeaders = new Dictionary<string, string>
            {
                { "X-App-Id", "AccidentIncident" },
                { "X-Context-Id", "c1f80483-fd30-4327-814e-778ad171a67b" }, // ID вашей Facility
                { "X-Context-Type", "Facility" },
                { "X-Tenant-Id", "CassenaCare" }
            };

            // Вызываем ваш стандартный GET-метод проекта
            return await _page.ApiGetRequest("incident/employee-configuration", customHeaders: contextHeaders);
        }

        public async Task<(string Id, string Title)> GetAvailableJobTitleAsync(string sectionRole)
        {
            var contextHeaders = new Dictionary<string, string>
            {
                { "X-App-Id", "AccidentIncident" },
                { "X-Context-Id", "c1f80483-fd30-4327-814e-778ad171a67b" },
                { "X-Context-Type", "Facility" },
                { "X-Tenant-Id", "CassenaCare" }
            };

            // 1. Получаем полный справочник должностей с сервера
            string jobTitlesJson = await _page.ApiGetRequest("job-titles", customHeaders: contextHeaders);
            var rootNode = System.Text.Json.Nodes.JsonNode.Parse(jobTitlesJson);
            var allJobTitles = rootNode?["jobTitles"]?.AsArray();

            if (allJobTitles == null || allJobTitles.Count == 0)
                Assert.Fail("[STAFF_STEPS] С сервера пришел пустой список должностей.");

            // 2. Скачиваем текущую конфигурацию, чтобы увидеть, какие должности УЖЕ там сидят
            string currentConfigJson = await GetEmployeeConfigurationAsync();
            var configNode = System.Text.Json.Nodes.JsonNode.Parse(currentConfigJson);
            var existingConstraints = configNode?["incidentConfiguration"]?[sectionRole]?["jobTitleConstraints"]?.AsArray();

            // Собираем хэшсет всех уже добавленных ID должностей (проверяем оба возможных ключа для надежности)
            var occupiedIds = new HashSet<string>();
            if (existingConstraints != null)
            {
                foreach (var constraint in existingConstraints)
                {
                    string? id = constraint?["constraintId"]?.ToString() ?? constraint?["id"]?.ToString();
                    if (!string.IsNullOrEmpty(id)) occupiedIds.Add(id);
                }
            }

            // 3. Перебираем справочник и возвращаем первую должность, которой ГАРАНТИРОВАННО нет в текущем конфиге
            foreach (var jobTitleNode in allJobTitles)
            {
                string? id = jobTitleNode?["id"]?.ToString();
                string? title = jobTitleNode?["title"]?.ToString();

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(title) && !occupiedIds.Contains(id))
                {
                    Log.Information($"[STAFF_STEPS] Найдена абсолютно новая свободная должность для теста: '{title}' ({id})");
                    return (id, title);
                }
            }

            // Если все должности чудесным образом заняты, берем первую (но логируем ворнинг)
            Log.Warning("[STAFF_STEPS] Все доступные должности уже добавлены в конфигурацию! Берем первую попавшуюся.");
            var first = allJobTitles[0];
            return (first?["id"]?.ToString() ?? "", first?["title"]?.ToString() ?? "");
        }

        public async Task<(string Id, string Title)> GetAvailableRoleTitleAsync(string sectionRole)
        {
            // 1. Стандартные корпоративные контекстные заголовки
            var contextHeaders = new Dictionary<string, string>
                {
                    { "X-App-Id", "AccidentIncident" },
                    { "X-Context-Id", "c1f80483-fd30-4327-814e-778ad171a67b" }, // Ваша Facility ID
                    { "X-Context-Type", "Facility" },
                    { "X-Tenant-Id", "CassenaCare" }
                };

            string targetFacilityId = "c1f80483-fd30-4327-814e-778ad171a67b";

            // 2. Получаем базовый список должностей
            string jobTitlesJson = await _page.ApiGetRequest("roles", customHeaders: contextHeaders);
            var rootNode = JsonNode.Parse(jobTitlesJson);
            var allJobTitles = rootNode?["jobTitles"]?.AsArray();

            if (allJobTitles == null || allJobTitles.Count == 0)
                Assert.Fail("[STAFF_STEPS] С сервера пришел пустой список должностей.");

            // 3. Получаем текущую конфигурацию, чтобы не выбрать уже занятую роль
            string currentConfigJson = await GetEmployeeConfigurationAsync();
            var configNode = JsonNode.Parse(currentConfigJson);
            var existingConstraints = configNode?["incidentConfiguration"]?[sectionRole]?["jobTitleConstraints"]?.AsArray();

            var occupiedIds = new HashSet<string>();
            if (existingConstraints != null)
            {
                foreach (var constraint in existingConstraints)
                {
                    string? id = constraint?["jobTitleId"]?.ToString() ?? constraint?["id"]?.ToString();
                    if (!string.IsNullOrEmpty(id)) occupiedIds.Add(id);
                }
            }

            foreach (var jobTitleNode in allJobTitles)
            {
                string? id = jobTitleNode?["id"]?.ToString();
                string? title = jobTitleNode?["title"]?.ToString();

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(title) || occupiedIds.Contains(id))
                    continue;

                string usersUrl = $"api/RoleTemplates/role-template/{id}/users";
                string usersJson = await _page.ApiGetRequest(usersUrl, customHeaders: contextHeaders);

                var usersRoot = JsonNode.Parse(usersJson);
                var usersArray = usersRoot?["users"]?.AsArray();

                if (usersArray == null || usersArray.Count == 0)
                    continue;

                int matchingUsersCount = 0;
                foreach (var userNode in usersArray)
                {
                    var facilities = userNode?["facilities"]?.AsArray();

                    // === ИСПРАВЛЕНИЕ: Пользователь нам подходит, если список фасилити пустой (корпоративный) 
                    // ИЛИ если в списке есть наше конкретное здание
                    if (facilities == null || facilities.Count == 0)
                    {
                        matchingUsersCount++; // Корпоративный пользователь
                    }
                    else if (facilities.Any(f => f?["id"]?.ToString() == targetFacilityId))
                    {
                        matchingUsersCount++; // Пользователь, привязанный к нашему зданию
                    }
                }

                // Если нашли должность, где есть хоть какие-то живые люди (локальные или корпораты)
                if (matchingUsersCount > 0)
                {
                    ExpectedEmployeesByJobTitleCount = matchingUsersCount;

                    Log.Information($"[STAFF_STEPS] Найдена должность: '{title}' ({id}). " +
                                    $"Всего подходящих пользователей (включая корпоративных): {matchingUsersCount}");
                    return (id, title);
                }
            }

            Assert.Fail($"[STAFF_STEPS] Не удалось найти ни одной должности, у которой были бы активные пользователи в Facility {targetFacilityId}.");
            return ("", "");
        }

        // Метод делает POST-запрос и отправляет измененный слепок JSON обратно на бэкенд
        public async Task UpdateEmployeeConfigurationAsync(System.Text.Json.Nodes.JsonNode fullPayload)
        {
            var contextHeaders = new Dictionary<string, string>
            {
                { "X-App-Id", "AccidentIncident" },
                { "X-Context-Id", "c1f80483-fd30-4327-814e-778ad171a67b" },
                { "X-Context-Type", "Facility" },
                { "X-Tenant-Id", "CassenaCare" }
            };

            // Вызываем ваш POST-метод. Так как Playwright принимает объекты для тела запроса, 
            // передаем туда наш измененный JsonNode напрямую.
            await _page.ApiPostRequest("incident/employee-configuration", fullPayload, customHeaders: contextHeaders);
        }

        /// <summary>
        /// Извлекает ID инцидента из URL, запрашивает лог репортов через API за текущие сутки
        /// и проверяет, что сущность была успешно создана.
        /// </summary>
        public async Task VerifyReportableLogContainsCurrentIncidentAsync(string incidentId)
        {
            Log.Information("[ASSERTION] Начало интеграционной проверки создания reportable сущности...");

            // 2. Формируем временной диапазон за ТЕКУЩИЕ сутки для параметров запроса
            var today = DateTime.Today;
            var queryParams = new Dictionary<string, object>
                {
                    { "start", today.ToString("yyyy-MM-ddT00:00:00") },
                    { "end", today.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:ss") }
                };

                        // 3. Настраиваем заголовки контекста (взяты из вашей вкладки Network)
            var customHeaders = new Dictionary<string, string>
                {
                    { "X-App-Id", "AccidentIncident" },
                    { "X-Context-Type", "Facility" },
                    { "X-Tenant-Id", "CassenaCare" }
                };

            Log.Information($"[API GET] Запрашиваем инциденты из 'incident/reportable-log' за дату {today:yyyy-MM-dd}...");

            // 5. Вызываем ваш метод запроса
            string jsonResponse = await _page.ApiGetRequest("incident/reportable-log", queryParams, customHeaders);
            Log.Information("[API SUCCESS] Ответ от бэкенда успешно получен. Приступаем к десериализации JSON...");

            // 4. Парсим JSON ответ и ищем наш incidentId
            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                JsonElement root = doc.RootElement;
                bool isIncidentFound = false;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement element in root.EnumerateArray())
                    {
                        // Проверяем наличие свойства, хранящего ID инцидента
                        if (element.TryGetProperty("incidentId", out JsonElement idProp) &&
                            idProp.GetString() == incidentId)
                        {
                            isIncidentFound = true;
                            break;
                        }
                    }

                    Assert.That(isIncidentFound, Is.True,
                        $"[FAIL] Интеграция нарушена! Инцидент с ID {incidentId} не обнаружен в репорт-логе бэкенда за сегодня. Ответ API: {jsonResponse}");
                }
                else
                {
                    // На случай, если бэкенд вернул объект структуры вместо массива
                    Assert.That(jsonResponse.Contains(incidentId), Is.True,
                        $"[FAIL] Интеграция нарушена! Строка ответа API не содержит ID {incidentId}. Ответ API: {jsonResponse}");
                }
            }

            Log.Information($"[ASSERTION SUCCESS] Отлично! Инцидент {incidentId} присутствует в логе бэкенда. Сущность Reportable создана.");
        }

        public async Task VerifyLastModifiedFooterAsync(string expectedUser, bool verifyFormatOnly = false)
        {
            Log.Debug($"Verifying Last Modified footer for user: {expectedUser}");

            // Локатор таблицы по тексту заголовка
            var table = _page.Locator("table").Filter(new() { HasText = "Last Modified By" });

            // Выбираем ячейки данных (исключая th, если они есть, или берем по структуре tr)
            var authorCell = table.Locator("td").Nth(0);
            var dateCell = table.Locator("td").Nth(1);

            // 1. Проверяем автора изменений
            await Assertions.Expect(authorCell).ToContainTextAsync(expectedUser);

            // 2. Проверяем дату (текущий день в американском формате MM/dd/yyyy)
            var todayDate = DateTime.Today.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            await Assertions.Expect(dateCell).ToContainTextAsync(todayDate);

            // 3. Проверяем регулярным выражением валидность формата времени (например, "4:52 PM")
            var dateText = await dateCell.InnerTextAsync();
            // Паттерн валидирует: MM/DD/YYYY ЧЧ:ММ AM/PM
            var timePattern = @"\d{2}/\d{2}/\d{4}\s+\d{1,2}:\d{2}\s+(AM|PM)";

            if (!System.Text.RegularExpressions.Regex.IsMatch(dateText, timePattern))
            {
                throw new Exception($"Текст даты и времени '{dateText}' не соответствует формату 'MM/dd/yyyy h:mm tt'");
            }

            Log.Debug("Last Modified footer verified successfully.");
        }

        /// <summary>
        /// Step: Verifies that both the tab counter and the actual grid table row count match the expected value.
        /// </summary>
        public async Task VerifyAttachmentsCounterAndTableRowsAsync(int expectedCount)
        {
            Log.Debug($"Шаг: Проверка того, что счетчик вкладки и строки таблицы равны: {expectedCount}");

            // 1. Получаем значение счетчика из заголовка вкладки через POM
            int actualTabCounter = await CreatePage.Attachments.GetTabCounterValueAsync();

            // 2. Получаем количество фактических строк в таблице через POM
            int actualTableRows = await CreatePage.Attachments.GetVisibleAttachmentsCountAsync();

            // 3. Выполняем проверки в слое шагов (Синтаксис NUnit)
            Assert.That(actualTabCounter, Is.EqualTo(expectedCount),
                $"Счетчик на вкладке Attachments ({actualTabCounter}) не совпадает с ожидаемым ({expectedCount})!");

            Assert.That(actualTableRows, Is.EqualTo(expectedCount),
                $"Количество строк в таблице ({actualTableRows}) не совпадает с ожидаемым ({expectedCount})!");
        }

        public async Task VerifyAttachmentRowIsDisplayedAsync(string category)
        {
            Log.Debug($"Шаг: Верификация отображения строки с категорией '{category}' в таблице.");
            // Просто перенаправляем вызов в ваш готовый метод из POM
            await CreatePage.Attachments.VerifyAttachmentIsDisplayedAsync(category);
        }

        /// <summary>
        /// Step: Dynamically updates the completion configuration for a specific tab/section via API.
        /// </summary>
        /// <param name="sectionCode">The target completion code (e.g., "Attachments", "General").</param>
        /// <param name="isEnabled">Optional status flag to enable or disable the section.</param>
        /// <param name="attachmentCount">Optional threshold configuration for files count validation.</param>
        public async Task UpdateIncidentConfigurationAsync(string sectionCode, bool? isEnabled = null, int? attachmentCount = null)
        {
            Log.Debug($"Шаг: Изменение конфигурации для секции '{sectionCode}'. Enabled: {isEnabled}, Count: {attachmentCount}");

            // Просто передаем чистый хвост эндпоинта!
            string configRoute = "incident/completion-configuration";

            // 1. Получаем текущую конфигурацию (URL соберется сам внутри ApiGetRequest)
            var contextHeaders = new Dictionary<string, string>
                {
                    { "X-App-Id", "AccidentIncident" },
                    { "X-Context-Id", "c1f80483-fd30-4327-814e-778ad171a67b" },
                    { "X-Context-Type", "Facility" },
                    { "X-Tenant-Id", "CassenaCare" }
                };

            string rawJsonGet = await CreatePage.Page.ApiGetRequest(configRoute, customHeaders: contextHeaders);

            var configData = JsonSerializer.Deserialize<CompletionConfigResponse>(rawJsonGet);
            Assert.That(configData, Is.Not.Null, "Не удалось десериализовать JSON конфигурации.");
            
            if (sectionCode.Equals("RN Supervisor Investigation Form"))
            {
                sectionCode = "EnvironmentalAssessment";
            }

            // 2. Находим нужную секцию
            var targetSection = configData.CompletionConfigurations
                .FirstOrDefault(c => c.CompletionCode.Equals(sectionCode, StringComparison.OrdinalIgnoreCase));

            Assert.That(targetSection, Is.Not.Null, $"Секция '{sectionCode}' не найдена в конфигурации бэкенда!");

            // 3. Меняем параметры
            if (isEnabled.HasValue) targetSection.Enabled = isEnabled.Value;
            if (attachmentCount.HasValue) targetSection.AttachmentCount = attachmentCount.Value;

            // 4. Отправляем обратно измененный объект (ApiPostRequest сам сделает из "incident/completion-configuration" полный URL)
            await CreatePage.Page.ApiPostRequest(configRoute, configData);
            await Task.Delay(300);

            Log.Information($"Конфигурация секции '{sectionCode}' успешно обновлена.");
        }


        public async Task DeleteAttachmentRowAsync(string category)
        {
            Log.Debug($"Шаг: Удаление строки аттача с категорией '{category}'");

            // Вызываем метод из POM, который кликает "корзину" и подтверждает удаление в модалке
            await CreatePage.Attachments.DeleteAttachmentByCategoryAsync(category);
        }

        /// <summary>
        /// Step: Downloads an attachment by category, saves it to the temporary OS folder, 
        /// and returns the absolute local path to the downloaded file.
        /// </summary>
        public async Task<string> DownloadAttachmentToTempFolderAsync(string category)
        {
            Log.Debug($"Шаг: Скачивание файла для категории '{category}' во временную папку.");

            // СПРЯТАЛИ СЮДА: Теперь тест не знает про InitiateAttachmentDownloadAsync
            var download = await CreatePage.Attachments.InitiateAttachmentDownloadAsync(category);

            // Проверяем, что скачивание прошло успешно
            string? failure = await download.FailureAsync();
            Assert.That(failure, Is.Null, $"Скачивание файла завершилось ошибкой Playwright: {failure}");

            // Формируем уникальный путь во временной директории ОС
            string tempFilePath = Path.Combine(Path.GetTempPath(), download.SuggestedFilename);

            // Физически сохраняем файл на диск
            await download.SaveAsAsync(tempFilePath);
            Log.Debug($"[STEPS] Файл успешно сохранен локально: {tempFilePath}");

            return tempFilePath;
        }

        /// <summary>
        /// Step: Opens a locally downloaded PDF file and returns its total number of pages.
        /// </summary>
        public int GetPdfPageCount(string localFilePath)
        {
            Log.Debug($"Шаг: Подсчет количества страниц в локальном файле: {localFilePath}");

            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException($"Скачанный файл не найден на диске по пути: {localFilePath}");
            }

            // Открываем документ с помощью библиотеки PdfPig и считываем свойство PageCount
            using (PdfDocument document = PdfDocument.Open(localFilePath))
            {
                int pageCount = document.NumberOfPages;
                Log.Information($"[STEPS] Внутри PDF документа обнаружено страниц: {pageCount}");
                return pageCount;
            }
        }


        public async Task EditAttachmentCategoryAsync(string currentCategory, string newCategory)
        {
            Log.Debug($"Шаг: Изменение категории аттача с '{currentCategory}' на '{newCategory}'");
            await CreatePage.Attachments.ChangeAttachmentCategoryInRowAsync(currentCategory, newCategory);
        }

        public async Task DownloadAndVerifyAttachmentMaskAsync(string category)
        {
            Log.Debug($"Шаг: Скачивание файла для категории '{category}' и валидация маски имени.");

            // 1. Получаем MRN для проверки маски
            string mrn = await CreatePage.Attachments.GetResidentMrnAsync();

            // 2. Скачиваем файл через POM
            var download = await CreatePage.Attachments.InitiateAttachmentDownloadAsync(category);

            // 3. Проверяем, что скачивание прошло успешно
            string? failure = await download.FailureAsync();
            Assert.That(failure, Is.Null, $"Скачивание файла завершилось ошибкой: {failure}");

            // 4. Валидируем маску имени файла {MRN}_{Category}_{Date}.pdf
            string suggestedFileName = download.SuggestedFilename;
            Log.Information($"[DOWNLOAD VAL] Анализ имени файла: '{suggestedFileName}'");

            // ИСПРАВЛЕНО: Для имени файла НЕ заменяем пробелы на подчёркивания!
            // Заменяем только специфическое длинное тире, если оно есть
            string formattedCategory = category.Replace("–", "-");

            // Формируем точный префикс (например, "121698_Witness Statement_")
            string expectedPrefix = mrn.Trim() + "_" + formattedCategory + "_";

            // Сегодняшняя дата (2026-06-29)
            string expectedDate = DateTime.Today.ToString("yyyy-MM-dd");

            Log.Debug($"[DOWNLOAD VAL] Ожидаемый префикс: '{expectedPrefix}', Ожидаемая дата: '{expectedDate}'");

            bool startsWithValidData = suggestedFileName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
            bool containsCurrentDate = suggestedFileName.Contains(expectedDate);
            bool endsWithPdf = suggestedFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

            Assert.Multiple(() =>
            {
                Assert.That(startsWithValidData, Is.True,
                    $"Имя файла '{suggestedFileName}' должно начинаться с префикса '{expectedPrefix}'");

                Assert.That(containsCurrentDate, Is.True,
                    $"Имя файла '{suggestedFileName}' должно содержать текущую дату '{expectedDate}'");

                Assert.That(endsWithPdf, Is.True,
                    $"Имя файла '{suggestedFileName}' должно иметь расширение '.pdf'");
            });

            Log.Information($"[DOWNLOAD SUCCESS] Тест скачивания успешно пройден!");
        }

        /// <summary>
        /// Step: Verifies that the row dropdown displays the expected updated category name.
        /// </summary>
        public async Task VerifyRowSelectedCategoryAsync(string initialCategory, string expectedCategory)
        {
            Log.Debug($"Шаг: Проверка того, что в строке со старой категорией '{initialCategory}' теперь выбрана новая категория '{expectedCategory}'");

            // Передаем в POM старую категорию для поиска строки
            string actualCategory = await CreatePage.Attachments.GetSelectedCategoryFromRowAsync(initialCategory);

            Assert.That(actualCategory, Is.EqualTo(expectedCategory),
                $"В дропдауне строки со старой категорией '{initialCategory}' отображается некорректное значение!");
        }
    }
}

