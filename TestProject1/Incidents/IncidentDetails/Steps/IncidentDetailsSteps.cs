using CareAdminTestProject.Common;
using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;
using System.Buffers.Text;
using static CareAdminTestProject.Common.BaseTest;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.BaseIncidentTabs;
using static DetailsTab;
using static GeneralTab;
using static IncidentCreatePage;
using static IncidentDataFactory;
using static MedicationTab;
using static StateTab;
using static SummaryTab;
using static CareAdminTestProject.Common.PlaywrightExtensions;
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
    ///   <item> <description> UI Indicator Checkpoints: <see cref="VerifyUnsavedChangesAlertVisibleAsync(string)"/>, 
    ///                                                  <see cref="VerifyFieldsOneByOneWithFilling(object, object)"/>, 
    ///                                                  <see cref="VerifyAllFieldsDotsStateAsync{T}(object, T, bool)"/>, 
    ///                                                  <see cref="VerifyStateTabSpecificLogicAsync"/>, 
    ///                                                  <see cref="VerifyRedDotField(object, string, bool)"/>, 
    ///                                                  <see cref="VerifyMedicationTabFullLifecycleAndIndicatorAsync"/>, 
    ///                                                  <see cref="VerifyTomorrowIsDisabledInCalendarAsync"/>, 
    ///                                                  <see cref="VerifyFutureTimeIsDisabledInPickerAsync"/>, 
    ///                                                  <see cref="FillRNFormTabWithTabCheckAsync(IncidentTestData)"/>, 
    ///                                                  <see cref="VerifyRedDotTab(string, bool)"/> </description> </item>
    /// </list>
    /// </summary>

    public class IncidentDetailsSteps
    {
        private readonly IPage _page;
        private readonly IncidentCreatePage _createPage;
        private readonly IncidentTrackerPage _trackerPage;

        /// <summary> Gets the underlying page object controller for the incident creation wizard framework. </summary>
        public IncidentCreatePage CreatePage => _createPage;
        string fileName;

        /// <summary> Holds the state of the parsed general incident dataset during the runtime workflow transaction. </summary>
        public IncidentGeneralInfo CapturedGeneralData { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentDetailsSteps"/> class.
        /// </summary>
        /// <param name="page">The isolated Playwright page context instance assigned to the running thread.</param>
        public IncidentDetailsSteps(IPage page)
        {
            _page = page;
            _createPage = new IncidentCreatePage(page);
            _trackerPage = new IncidentTrackerPage(page);
        }

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
            await ClickCreateIncidentAsync();
            await FillDetailsTabAsync(data);
            await FillStateTabAsync(data);
            await FillMedicationTabAsync(data);
            await FillRNFormTabAsync(data);
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
            await _page.WaitForURLAsync(guidRegex, new() { Timeout = 30000 });


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
                await Task.Delay(300);

                await VerifyRedDotField(tabComponent, field.Key, false);
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
        public async Task ClearDetailsForm()
        {
            // NOTE: Review debug log
            Log.Debug("Try to clear All Diagnoses");
            await _createPage.Details.ClearAndSaveDiagnosesAsync();
            // NOTE: Review debug screenshot sequence
            await _page.MakeScreenshotAsync("AllDiagnoses_cleared");
        }

        /// <summary>
        /// Dispatches a quick command toggle targeting the First Aid Administration toggle switch component.
        /// </summary>
        /// <param name="answer">Pass true to toggle the switch status to enabled; otherwise, false.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SwitchFirstAid(bool answer)
        {
            await _createPage.Details.SelectFirstAdmitedAsync(answer, "");
        }

        /// <summary>
        /// Captures the 'employees' API response and verifies that UI staff dropdowns 
        /// contain the correct number of records according to employment status (active vs terminated).
        /// </summary>
        public async Task VerifyStaffDropdownsCountWithApiAsync()
        {

            var jsonString = await _page.ApiPostRequest("employees");

            Assert.That(jsonString, Does.Not.Contain("Network Error"), $"Full JSON output was:\n{jsonString}");



            // Парсим корневой JSON
            using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            List<System.Text.Json.JsonElement> recordsArray;

            // Если корень — это массив (как говорит ошибка)
            if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                recordsArray = root.EnumerateArray().ToList();
            }
            // На всякий случай оставляем проверку свойства records, если на разных стендах структура отличается
            else if (root.TryGetProperty("records", out var recordsProp) && recordsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                recordsArray = recordsProp.EnumerateArray().ToList();
            }
            else
            {
                Assert.Fail("API Response Error: Root JSON element is neither an Array nor an Object with 'records' property.");
                return;
            }

            // Вычисляем эталонные значения на основе твоей бизнес-гипотезы
            int totalEmployeesInApi = recordsArray.Count;

            // Считаем активных сотрудников (termDate == null)
            int activeEmployeesInApi = recordsArray.Count(e =>
                e.TryGetProperty("termDate", out var termProp) && termProp.ValueKind == System.Text.Json.JsonValueKind.Null);

            Log.Information($"[STAFF_VALIDATION] API Data Stats -> Total: {totalEmployeesInApi}, Active (termDate == null): {activeEmployeesInApi}");

            // Считаем реальное количество элементов на UI для каждого дропдауна
            int uiSupervisorCount = await GetMaterialDropdownOptionsCountAsync("Supervisor");
            int uiChargeNurseCount = await GetMaterialDropdownOptionsCountAsync("Charge nurse");
            int uiCnaCount = await GetMaterialDropdownOptionsCountAsync("CNA");

            Log.Information($"[STAFF_VALIDATION] UI Data Stats -> Supervisor: {uiSupervisorCount}, Charge Nurse: {uiChargeNurseCount}, CNA: {uiCnaCount}");

            // Финальные ассерты согласно твоей гипотезе
            Assert.Multiple(() =>
            {
                // Гипотеза 1: В Supervisor только активные (termDate == null)
                Assert.That(uiSupervisorCount, Is.EqualTo(activeEmployeesInApi),
                    $"Staff Mismatch: 'Supervisor' dropdown should display ONLY active employees ({activeEmployeesInApi}), but displays {uiSupervisorCount}.");

                // Гипотеза 2: В Charge Nurse и CNA доступны все сотрудники (включая уволенных)
                Assert.That(uiChargeNurseCount, Is.EqualTo(totalEmployeesInApi),
                    $"Staff Mismatch: 'Charge Nurse' dropdown should display ALL employees ({totalEmployeesInApi}), but displays {uiChargeNurseCount}.");

                Assert.That(uiCnaCount, Is.EqualTo(totalEmployeesInApi),
                    $"Staff Mismatch: 'CNA' dropdown should display ALL employees ({totalEmployeesInApi}), but displays {uiCnaCount}.");
            });

            Log.Information("[STAFF_VALIDATION] Hypothesis successfully confirmed! Employee counts match API filters.");
        }

        /// <summary>
        /// Helper method to open a Kendo dropdown by its label, count its items, and close it safely.
        /// </summary>
        private async Task<int> GetMaterialDropdownOptionsCountAsync(string dropdownLabel)
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
    }
}

