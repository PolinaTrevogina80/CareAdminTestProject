using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Log = CareAdminTestProject.Common.TestLog;

/// <summary>
/// Represents the Attachments tab within the incident reporting form.
/// Provides methods to handle file uploading, dialog control management, and operational category assignments.
/// <para><b>--- METHOD DIRECTORY & QUICK LINKS ---</b></para>
/// <list type="bullet">
///   <item> <description> Context Pre-Configuration Setup Hook: <see cref="AttachmentsTab"/> </description> </item>
///   <item> <description> Test Setup Lifecycle Initializer: <see cref="BaseSetup"/> </description> </item>
///   <item> <description> Inline Session Expiration Interrogator: <see cref="RefreshTokenIfNeeded"/> </description> </item>
///   <item> <description> Context Tear-Down Capture Automation: <see cref="TearDown"/> </description> </item>
/// </list>
/// </summary>
public class AttachmentsTab : BaseIncidentTabs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttachmentsTab"/> class.
    /// </summary>
    /// <para><b>--- METHOD DIRECTORY & QUICK LINKS ---</b></para>
    /// <list type="bullet">
    ///   <item> <description> Permitted File Classifications: <see cref="AttachmentCategories"/> </description> </item>
    ///   <item> <description> Document Upload Stream Handler: <see cref="UploadAttachmentAsync(string)"/> </description> </item>
    ///   <item> <description> Single Category Processing Broadcast: <see cref="AssignCategoriesToAllPagesAsync(string, string?)"/> </description> </item>
    ///   <item> <description> Multi-Category Sequenced Mapping Step: <see cref="AssignCategoriesToAllPagesAsync(IReadOnlyList{string}, string?)"/> </description> </item>
    ///   <item> <description> Post-Upload Display Verification Node: <see cref="VerifyAttachmentIsDisplayedAsync(string)"/> </description> </item>
    /// </list>
    /// <param name="page">The Playwright page instance.</param>
    public AttachmentsTab(IPage page) : base(page) { }

    /// <summary>
    /// Holds the static list of authorized document type attachment classification categories.
    /// </summary>
    public static readonly List<string> AttachmentCategories = new()
    {
        "Accident Report",
        "Charge Nurse – Accident Post Investigation",
        "Licensed Nurse - Occurrence Investigative Form",
        "CNA - Occurrence Investigative Form",
        "CNA Statement",
        "Employee Statement",
        "Resident Statement",
        "Witness Statement",
        "RN Supervisor - Occurrence Investigative Form",
        "Hourly/Half Hourly Rounding Sheet",
        "Shift Staffing Sheet from Smartlinx",
        "Other",
        "Summary"
    };

    /// <summary>
    /// Uploads a local document file using the file path string captured during earlier execution reporting workflows.
    /// </summary>
    /// <param name="filePath">The absolute system path to the target file (e.g., tempPath from the previous step).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the provided target path cannot be resolved as an existing file on disk.</exception>
    public async Task UploadAttachmentAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        string fileName = Path.GetFileName(filePath);

        var addButton = Page.Locator("button").Filter(new()
        {
            Has = Page.Locator("mat-icon[data-mat-icon-name='add-icon'], mat-icon:has-text('add')")
        });
        await addButton.ClickAsync();
        Log.Debug("Attach button is clicked");

        // Search for an exact text match to avoid layout collision ambiguities with subheaders
        var popupHeader = Page.GetByText("Upload a file", new() { Exact = true });
        await popupHeader.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

        // 1. Await dynamic overlay dialog generation and assign the local file to the input element target
        var fileInput = Page.Locator("cad-incident-add-attachment-dialog input[type='file']");
        await fileInput.SetInputFilesAsync(filePath);

        // 2. Synchronize execution thread until the uploaded target file name renders inside the active queue grid list (.files-list inside your DOM)
        fileName = Path.GetFileName(filePath);
        var fileItem = Page.Locator(".files-list").GetByText(fileName);
        await fileItem.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // 3. VERIFICATION CHECKPOINT: Await explicit element visibility of the specific filename string inside the attachment wizard container
        var uploadedFile = Page.Locator("cad-incident-add-attachment-dialog").GetByText(fileName);

        // Wait until the text node representing the active uploaded file switches to a visible state
        await uploadedFile.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        Log.Information("Attachment file selected");


        // 4. Click the "Next" step progression button
        var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
        await nextButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await nextButton.ClickAsync(new() { Timeout = 180000 });
    }

    /// <summary>
    /// Overload helper method configured to broadcast and apply a single target category type universally across every file page container line.
    /// </summary>
    /// <param name="categoryName">The explicit target category descriptor label value to apply.</param>
    /// <param name="notes">Optional supplementary string text notes to attach to document pages.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AssignCategoriesToAllPagesAsync(string categoryName, string? notes = null)
    {
        // NOTE: Review debug log
        Log.Debug($"Method invoked to apply a single category classification '{categoryName}' across all document pages.");

        // Wrap the single string parameter constraint value inside an isolated single-element collection list container
        IReadOnlyList<string> categoryNames = new List<string> { categoryName };

        // Delegate execution workflow forward to the primary collection processor method overload structure
        await AssignCategoriesToAllPagesAsync(categoryNames, notes);
    }

    /// <summary>
    /// Sequentially steps through document pages inside the dynamic assignment modal overlay dialog 
    /// and maps specific index or single configuration categories to individual pages.
    /// </summary>
    /// <param name="categoryNames">The collection array of target classification categories to apply sequentially.</param>
    /// <param name="notes">The operational string note value to inject if the fallback category choice evaluates to 'Other'.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AssignCategoriesToAllPagesAsync(IReadOnlyList<string> categoryNames, string? notes = null)
    {
        // NOTE: Review debug log
        Log.Debug("Starting the process of page-by-page category list assignment.");

        // 1. Locate the dialog overlay container wrapper
        var dialog = Page.Locator(".incident-assign-pdf-to-category").First;
        await dialog.WaitForAsync();
        // NOTE: Review debug log
        Log.Debug("Assign Pages popup found and displayed.");

        // 2. Query target pagination text node metrics to identify total document pages inside the PDF layout stream
        var paginationElement = dialog.Locator(".pagination-wrapper, .page-configuration, .pagination").Filter(new() { Has = Page.Locator("mat-icon") }).First;
        var paginationText = await paginationElement.InnerTextAsync();
        // NOTE: Review debug log
        Log.Debug($"Pagination text extracted: '{paginationText}'");

        var match = System.Text.RegularExpressions.Regex.Match(paginationText, @"of\s+(\d+)");
        int totalPages = match.Success ? int.Parse(match.Groups[1].Value) : 1;
        // NOTE: Review debug log
        Log.Debug($"Determined total number of pages: {totalPages}");

        // Ensure the provided configuration parameter list covers or safely cycles structural document page iterations
        int iterationsCount = totalPages;

        for (int i = 1; i <= iterationsCount; i++)
        {
            // NOTE: Review debug log
            Log.Debug($"--- Processing page {i} of {totalPages} ---");

            // Extract string item by indexing, falling back onto duplicating the final list choice item across trailing sections
            string currentCategory = (i - 1 < categoryNames.Count)
                ? categoryNames[i - 1]
                : categoryNames[categoryNames.Count - 1];

            // 3. Search and trigger click events at the material select field container level
            var dropdown = dialog.Locator(".mat-mdc-select-trigger").First;
            await dropdown.ScrollIntoViewIfNeededAsync();
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            //// NOTE: Review debug log
            //Log.Debug("Waiting 1.5 seconds to guarantee stable binding of Angular Event Listeners...");
            //await Page.WaitForTimeoutAsync(1500);

            // NOTE: Review debug log
            Log.Debug("Focusing on select field and clicking...");
            await dropdown.FocusAsync();
            await dropdown.ClickAsync();

            // 4. Target list option selection action
            // Implement a fallback button key trigger in case standard element clicking is ignored — dispatch Space key to trigger overlay expansions
            // NOTE: Review debug log
            Log.Debug("Verifying if option overlay wrapper expanded, if not — dispatching Space key sequence...");
            var overlay = Page.Locator(".cdk-overlay-container");
            var option = overlay.Locator("mat-option").GetByText(currentCategory, new() { Exact = false });

            await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await option.ClickAsync();

            // NOTE: Review debug log
            Log.Debug($"Category '{currentCategory}' successfully assigned for page {i}");

            // 5. Context branch evaluation if category status properties match the 'Other' selector keyword
            if (currentCategory.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                // NOTE: Review debug log
                Log.Debug("Category 'Other' selected, populating notes field...");
                var notesField = dialog.Locator("input[name='notes'], input[formcontrolname='notes']").First;
                string notesToFill = string.IsNullOrEmpty(notes) ? "Auto-generated test notes" : notes;
                await notesField.FillAsync(notesToFill);
                // NOTE: Review debug log
                Log.Debug($"Notes field filled with text: '{notesToFill}'");
            }

            // 6. Pagination layout stepping advancement forward logic
            if (i < totalPages)
            {
                // NOTE: Review debug log
                Log.Debug("Clicking the 'right' navigation arrow control to move onto the next document page frame layout.");

                // Locate the div container with class pagination-button encapsulating an internal chevron_right icon node
                var nextButton = dialog.Locator("div.pagination-button")
                    .Filter(new() { HasText = "keyboard_arrow_right" })
                    .First;

                await nextButton.ScrollIntoViewIfNeededAsync();
                await nextButton.ClickAsync();

                // Synchronize loop iteration logic states until the active pagination text updates to target indicators (e.g., matching i + 1 value layouts)
                var expectedPageText = $"{i + 1} of {totalPages}";
                await Assertions.Expect(paginationElement).ToContainTextAsync(expectedPageText);
                // NOTE: Review debug log
                Log.Debug($"Successfully transitioned to page {i + 1}.");
            }
        }

        // 7. Workflow termination process and commit action persistence
        var assignButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Assign Pages" });
        // NOTE: Review debug log
        Log.Debug("Evaluating usability of final 'Assign Pages' layout buttons...");

        await assignButton.ClickAsync();
        // NOTE: Review debug log
        Log.Debug("'Assign Pages' action button clicked.");

        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        // NOTE: Review debug log
        Log.Debug("Popup layer dismissed, page mapping operation array completed successfully.");
    }
    /// <summary>
    /// Verifies that a uploaded attachment file is correctly generated and displayed within the grid data table by formatting search masks based on resident MRN and target category values.
    /// </summary>
    /// <param name="category">The specific document classification category string to evaluate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task VerifyAttachmentIsDisplayedAsync(string category)
    {
        // 1. Extract the resident's MRN directly from the form header elements
        var mrnElement = Page.Locator("div").Filter(new() { HasText = "MRN" }).Locator("xpath=..").Locator("span, div").Nth(1);
        string mrnText = await Page.GetByText("MRN").Locator("..").InnerTextAsync();
        var mrn = System.Text.RegularExpressions.Regex.Match(mrnText, @"\d+").Value;

        // NOTE: Review debug log
        Log.Debug($"Extracted Resident MRN code: {mrn}");

        // 2. Format the target category label text for identification routing checks (scrubbing spaces and dashes)
        string formattedCategory = category.Replace(" ", "_").Replace("–", "-");

        // Extract only the base initial substring segment (e.g., "124216_Accident" or "124216_Charge"), 
        // because long string parameters can be truncated by the user interface layout controls
        string searchMask = $"{mrn}_{formattedCategory.Split('_')[0]}";

        // NOTE: Review debug log
        Log.Debug($"Searching table rows for file matching mask criteria: '{searchMask}'");

        // 3. Wait until the user interface completely updates after closing the overlay dialog window component layers. 
        // Allocate a 1.5-second pause for the Angular framework to redraw grid arrays since file composition operations aggregate asynchronously on back-end systems
        await Page.WaitForTimeoutAsync(1500);

        // 4. Isolate the explicit grid data table row structure containing the generated mask criteria string
        var targetRow = Page.Locator("tbody tr").Filter(new() { HasText = searchMask }).First;

        // 5. Assert visibility status on the isolated table row container using a high-threshold timeout margin (10 seconds to accommodate complex PDF generation processing)
        await Assertions.Expect(targetRow).ToBeVisibleAsync(new() { Timeout = 10000 });

        Log.Information($"The file for category classification '{category}' (search mask: '{searchMask}') is successfully visible within the data table view grid.");
    }
}