using Microsoft.Playwright;
using CareAdminTestProject.Common;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab.RNSupervisorTabInfo;
using static Microsoft.Playwright.Assertions;
using Log = CareAdminTestProject.Common.TestLog;


namespace CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;

/// <summary>
/// Represents the RN/Supervisor questionnaire tab within the incident reporting form.
/// Manages the step-by-step injection, evaluation, and verification of responses.
/// </summary>
public class RNSupervisorTab : BaseIncidentTabs
{
    private Boolean ToDoScreenshots = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="RNSupervisorTab"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public RNSupervisorTab(IPage page) : base(page) { }

    /// <summary>
    /// Encapsulates all data records and response information for the RN/Supervisor questionnaire form.
    /// </summary>
    public record RNSupervisorTabInfo(
        IReadOnlyList<string> Locations, // List of strings for multiselect choices
        LastSeenInfo LastSeen,
        DescribeExactlyInfo DescribeExactly,
        // Steps (4-28)
        IReadOnlyList<QuestionWithDetails> Questions
    )
    {
        /// <summary>
        /// Stores detailed info about when the patient was last seen.
        /// </summary>
        public record LastSeenInfo(TimeOnly Time, string Details);

        /// <summary>
        /// Stores meticulous textual descriptive details about the scene.
        /// </summary>
        public record DescribeExactlyInfo(string Details);

        /// <summary>
        /// Holds boolean responses along with optional corresponding comments for specific validation points.
        /// </summary>
        public record QuestionWithDetails(bool Answer, string Comments = "");
    }

    /// <summary>
    /// Sequentially steps through the RN/Supervisor wizard form and executes input actions for each logical step.
    /// </summary>
    /// <param name="info">The complete questionnaire reference dataset holding expected values.</param>
    /// <param name="onStepFilled">An optional callback delegate triggered after completing an isolated workflow step.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FillQuestionsAsync(RNSupervisorTabInfo info, Func<int, Task>? onStepFilled = null)
    {
        await SelectLocationsAsync(info.Locations);


        // Step 1: Time of the last visit
        await FillFormStepAsync(1, async () =>
        {
            // Use "answerTime" (the name matching your framework reference)
            await SelectTimeInPickerAsync("answerTime", info.LastSeen.Time);

            // Locate the details textarea on the current active view wrapper
            var detailsArea = Page.GetByPlaceholder("Enter details");
            await detailsArea.FillAsync(info.LastSeen.Details);
            if (ToDoScreenshots)
            {
                // NOTE: Review debug screenshot sequence
                await Page.MakeScreenshotAsync("RN_Step1_Filled");
            }
        });

        // Step 2: Exact meticulous description
        await FillFormStepAsync(2, async () =>
        {
            // Populate details context block for the second question
            var detailsArea = Page.GetByPlaceholder("Enter details");
            await detailsArea.FillAsync(info.DescribeExactly.Details);
            if (ToDoScreenshots)
            {
                // NOTE: Review debug screenshot sequence
                await Page.MakeScreenshotAsync("RN_Step2_Filled");
            }
        });

        for (int i = 0; i < info.Questions.Count; i++)
        {
            var currentQuestion = info.Questions[i];
            int stepNumber = i + 3; // Offset index to start precisely from the 4th logical form step

            await FillFormStepAsync(stepNumber, async () =>
            {
                var buttonName = currentQuestion.Answer ? "YES" : "NO";

                // 1. Locate the mat-button-toggle component matching target text
                // This approach is more resilient for Angular Material structures than default GetByRole methods
                var toggleButton = Page.Locator("mat-button-toggle")
                    .Filter(new() { HasTextRegex = new Regex($"^{buttonName}$", RegexOptions.IgnoreCase) });

                // 2. Wait until the target element handles click actions, then execute click
                await toggleButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                await toggleButton.ClickAsync();

                // 3. Inject comments if additional string context properties are populated
                if (!string.IsNullOrEmpty(currentQuestion.Comments))
                {
                    var detailsArea = Page.GetByPlaceholder("Enter comments");
                    await detailsArea.FillAsync(currentQuestion.Comments);
                }

                if (ToDoScreenshots)
                {
                    // NOTE: Review debug screenshot sequence
                    await Page.MakeScreenshotAsync($"RN_Step_{stepNumber}_Filled");
                }
            });
        }

    }

    /// <summary>
    /// Wrapper for step processing: waits for the step/page number, performs the actions, and clicks "Next".
    /// </summary>
    /// <param name="stepNumber">The target step sequential position number out of the total 28 steps.</param>
    /// <param name="fillAction">The delegate wrapper housing input form workflow steps.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task FillFormStepAsync(int stepNumber, Func<Task> fillAction)
    {
        var pagination = Page.Locator("div.pagination").Last;

        // Wait until the pagination counter matches the precise page index currently being evaluated
        await Expect(pagination).ToContainTextAsync($"{stepNumber} of 28", new() { Timeout = 10000 });

        // Execute structural internal field injection logic
        await fillAction();

        // Advance layout pointer forward to the next step
        await GoToNextStepAsync();

        // FIX: Only await text disappearance if this evaluation is NOT the final 28th step index
        if (stepNumber < 28)
        {
            await Expect(pagination).Not.ToContainTextAsync($"{stepNumber} of 28", new() { Timeout = 3000 });
        }
    }

    /// <summary>
    /// Selects multiple accident location checkboxes or items out of an overlay list container options.
    /// </summary>
    /// <param name="locations">The collection list of descriptive accident areas to enable.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SelectLocationsAsync(IReadOnlyList<string> locations)
    {
        if (locations == null || !locations.Any()) return;

        // 1. Locate and click on the "+" trigger button inside the Area(s) of Accident interface container
        // Isolate the parent section wrapper, then resolve its core internal trigger element
        var addButton = Page.Locator(".locations")
            .Locator("button, mat-icon")
            .Filter(new() { HasText = "add" })
            .First;

        await addButton.ClickAsync();

        // 2. Synchronize layout expectation until the pop-up modal overlay containing options finishes rendering
        // Under Angular Material, selection dropdown lists populate inside a separate body root overlay element wrapper
        var optionsContainer = Page.Locator(".cdk-overlay-container");
        await optionsContainer.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        foreach (var location in locations)
        {
            // Resolve selection rows whose internal label text models exactly match target values (handling spaces)
            var option = optionsContainer.Locator("mat-list-item, mat-option, .mat-mdc-list-item")
                .Filter(new()
                {
                    HasTextRegex = new Regex($"^\\s*{location}\\s*$", RegexOptions.IgnoreCase)
                });

            if (await option.CountAsync() > 0)
            {
                await option.First.ClickAsync();
            }
        }

        // 3. Dismiss the active overlay picker template (triggering an Escape fallback action) 
        // in case the modal layer interface container failed to close automatically post selection sequence
        await Page.Keyboard.PressAsync("Escape");
    }

    /// <summary>
    /// Commands the interface pagination indicators to cycle one wizard slide forward.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task GoToNextStepAsync()
    {
        var nextButton = Page.Locator(".pagination-button:has(mat-icon:text('keyboard_arrow_right'))").First;
        await nextButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await nextButton.ClickAsync();

        // Complete the animation layout slide transitions handled by Kendo UI or Angular components
        // Without this brief rest, consecutive automated browser click triggers can duplicate input executions
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle); // Synchronize micro API request hooks if pending
        await Task.Delay(250); // Allocate a quarter-second duration margin for completing underlying CSS slide transition rules
    }

    /// <summary>
    /// Collects and processes the textual array of location markers currently selected on the user interface.
    /// </summary>
    /// <returns>A clean read-only string list of active location chip configurations.</returns>
    public async Task<IReadOnlyList<string>> GetSelectedLocationsAsync()
    {
        // 1. Isolate the base organizational container enclosing every active selected chip component
        var chipsContainer = Page.Locator("div.cad-chips-wrapper, div.selected-items").First;
        await chipsContainer.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        // 2. Introduce a temporary execution pause ensuring Angular layouts complete their local render evaluations
        await Page.WaitForTimeoutAsync(200);

        // 3. Extract the underlying text target directly from the internal span structure of every present chip item
        var locationSpans = chipsContainer.Locator("div.selected-item span");

        var cleanLocations = new List<string>();
        int count = await locationSpans.CountAsync();

        for (int i = 0; i < count; i++)
        {
            // Evaluate raw string node assignments directly out of the span DOM textContent property field
            string rawText = await locationSpans.Nth(i).EvaluateAsync<string>("el => el.textContent") ?? "";

            string cleanText = rawText.Trim();

            if (!string.IsNullOrEmpty(cleanText))
            {
                cleanLocations.Add(cleanText);
            }
        }

        return cleanLocations;
    }

    /// <summary>
    /// Evaluates which toggle button option status (YES or NO) is currently designated as active in the user view.
    /// </summary>
    /// <returns>The descriptive text of the active button element choice string, or empty if none are enabled.</returns>
    public async Task<string> GetSelectedToggleValueAsync()
    {
        // Identify the mat-button-toggle node elements having active visual style definitions applied
        var checkedToggle = Page.Locator("mat-button-toggle.mat-button-toggle-checked, mat-button-toggle[checked='true']");
        if (await checkedToggle.CountAsync() > 0)
        {
            return await checkedToggle.InnerTextAsync();
        }
        return string.Empty;
    }


    /// <summary>
    /// Executes a comprehensive step-by-step verification of all questionnaire wizard data fields.
    /// </summary>
    /// <param name="expected">The structural data object containing the expected responses and configurations for comparison.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task VerifyDataFieldsAsync(RNSupervisorTabInfo expected)
    {
        // NOTE: Review debug log
        Log.Debug("[RN_SUPERVISOR_TAB] Launching step-by-step form verification wizard...");

        // 0. Verify selected locations on the initial screen configuration layout
        if (expected.Locations != null && expected.Locations.Any())
        {
            var actualLocations = await GetSelectedLocationsAsync();

            foreach (var expectedLocation in expected.Locations)
            {
                // Verify that AT LEAST ONE element on the UI partially contains the name of our target location
                bool isFound = actualLocations.Any(act => act.Contains(expectedLocation.Trim(), StringComparison.OrdinalIgnoreCase));

                Assert.That(isFound, Is.True,
                    $"Location '{expectedLocation}' was not found among those selected on the UI. Available on UI: {string.Join(", ", actualLocations)}");
            }
        }

        // Step 1: Time of the last visit validation checkpoint
        await VerifyFormStepAsync(1, async () =>
        {
            // Verify populated time picker string metrics using InputValueAsync
            var actualTime = await Page.Locator("kendo-timepicker[name='answerTime'] input:visible").InputValueAsync();
            string expectedTimeStr = expected.LastSeen.Time.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(actualTime, Is.EqualTo(expectedTimeStr), "Step 1: Time of last visit evaluation mismatch.");

            // Verify explicit details textbox value
            var actualDetails = await Page.GetByPlaceholder("Enter details").InputValueAsync();
            Assert.That(actualDetails, Is.EqualTo(expected.LastSeen.Details), "Step 1: Details description value mismatch.");
        });

        // Step 2: Meticulous incident description validation checkpoint
        await VerifyFormStepAsync(2, async () =>
        {
            var actualDetails = await Page.GetByPlaceholder("Enter details").InputValueAsync();
            Assert.That(actualDetails, Is.EqualTo(expected.DescribeExactly.Details), "Step 2: Detailed description narrative mismatch.");
        });

        // Steps 3-28: Sequential evaluation loop processing dynamic question cards (YES/NO toggles + comments)
        for (int i = 0; i < expected.Questions.Count; i++)
        {
            var currentQuestion = expected.Questions[i];
            int stepNumber = i + 3; // Based on setup logic, dynamic questionnaires initialize starting at wizard page index 3

            await VerifyFormStepAsync(stepNumber, async () =>
            {
                // 1. Verify target toggled selection state (YES or NO matching property flag)
                string expectedButtonName = currentQuestion.Answer ? "YES" : "NO";
                string actualButtonName = await GetSelectedToggleValueAsync();

                Assert.That(actualButtonName.ToUpper(), Is.EqualTo(expectedButtonName),
                    $"Step {stepNumber}: Incorrect toggle button option selection.");

                // 2. Evaluate string comments area if data record parameter constraints are populated
                if (!string.IsNullOrEmpty(currentQuestion.Comments))
                {
                    var actualComment = await Page.GetByPlaceholder("Enter comments").InputValueAsync();
                    Assert.That(actualComment, Is.EqualTo(currentQuestion.Comments),
                        $"Step {stepNumber}: Question comments field string mismatch.");
                }
            });
        }

        // NOTE: Review debug log
        Log.Debug("[RN_SUPERVISOR_TAB] Step-by-step wizard questionnaire validation completed successfully.");
    }

    /// <summary>
    /// Wrapper for verification step processing: manages pagination, executes checks on the current slide, and clicks "Next".
    /// </summary>
    /// <param name="stepNumber">The target page index position that layout indicators must validate against.</param>
    /// <param name="verifyAction">The wrapper delegate housing assertion blocks for the active view frame.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task VerifyFormStepAsync(int stepNumber, Func<Task> verifyAction)
    {
        var pagination = Page.Locator("div.pagination").Last;

        // Ensure that the wizard has successfully completed its layout navigation shift to the required page indices
        await Expect(pagination).ToContainTextAsync($"{stepNumber} of 28", new() { Timeout = 10000 });

        // Execute target assertions for the current active page template
        await verifyAction();

        // Cycle layout view forward to the next wizard slide container
        await GoToNextStepAsync();
    }
}
