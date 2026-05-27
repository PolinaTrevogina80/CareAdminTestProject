using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;
using Log = CareAdminTestProject.Common.TestLog;


/// <summary>
/// Represents the Medication tab within the incident reporting form.
/// Provides methods to populate, clear, and verify data grid records for medications.
/// </summary>
public class MedicationTab : BaseIncidentTabs
{
    /// <summary>
    /// Represents the structural data model for a single medication entry row.
    /// </summary>
    public record MedicationInfo(
        string Name,
        string Dosage,
        string Frequency,
        string TimeReceived
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="MedicationTab"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public MedicationTab(IPage page) : base(page) { }

    /// <summary>
    /// Fills the medication data grid table sequentially by adding rows for each medication provided.
    /// </summary>
    /// <param name="medications">The list of medication datasets to populate inside the form.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FillMedicationTabAsync(List<MedicationInfo> medications)
    {
        // NOTE: Review debug log
        Log.Debug("[MEDICATION_TAB] Running medication injection into the data table...");

        for (int i = 0; i < medications.Count; i++)
        {
            // 1. Click the action button to insert a new dynamic row
            await GetButtonByText("Add Medication").ClickAsync();

            var medication = medications[i];

            // 2. Resolve the i-th data line container (skipping the header template)
            var row = Page.Locator(".medication-row.ng-star-inserted").Nth(i);

            // 3. Fill out target inputs inside this specific container using column indexes
            await row.Locator("input").Nth(0).FillAsync(medication.Name);
            await row.Locator("input").Nth(1).FillAsync(medication.Dosage);
            await row.Locator("input").Nth(2).FillAsync(medication.Frequency);
            await row.Locator("input").Nth(3).FillAsync(medication.TimeReceived);

            // NOTE: Review debug log
            Log.Debug($"[MEDICATION_TAB] Row added #{i + 1}");
        }
    }

    /// <summary>
    /// Sequentially purges all present medication records from the grid using line deletion actions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClearAllMedicationsAsync()
    {
        // NOTE: Review debug log
        Log.Debug("[MEDICATION_TAB] Launching removal of all medications from the table...");

        // Locator target pointing to the first actionable delete button within active rows
        var firstDeleteButton = Page.Locator(".medication-row.ng-star-inserted").First.GetByRole(AriaRole.Button, new() { Name = "Delete" });

        int deletedCount = 0;

        // Execute sequential loop as long as an active row deletion trigger remains visible
        while (await firstDeleteButton.IsVisibleAsync())
        {
            // NOTE: Review debug log
            Log.Debug($"[MEDICATION_TAB] Deleting row #{deletedCount + 1}");

            // Trigger click action on the top-most line deletion anchor
            await firstDeleteButton.ClickAsync();
            deletedCount++;

            // Brief pause allowing Angular rendering cycle to clear element from DOM and reset indicators
            await Page.WaitForTimeoutAsync(150);
        }

        // NOTE: Review debug log
        Log.Debug($"[MEDICATION_TAB] Clean up complete. Total rows deleted: {deletedCount}");
    }

    /// <summary>
    /// Generates a designated number of empty medication input fields in the table grid.
    /// </summary>
    /// <param name="count">The requested number of empty lines to spawn.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddEmptyMedicationRowsAsync(int count)
    {
        // NOTE: Review debug log
        Log.Debug($"[MEDICATION_TAB] Appending {count} empty medication rows...");

        for (int i = 0; i < count; i++)
        {
            await GetButtonByText("Add Medication").ClickAsync();

            // Establish visible state expectation barrier to prevent multi-click racing anomalies
            await Page.Locator(".medication-row.ng-star-inserted").Nth(i).WaitForAsync(new() { State = WaitForSelectorState.Visible });
        }

        // NOTE: Review debug log
        Log.Debug("[MEDICATION_TAB] Empty rows successfully generated.");
    }

    /// <summary>
    /// Clicks the Delete button for a specific row index.
    /// </summary>
    public async Task DeleteMedicationRowAsync(int rowIndex)
    {
        // Находим кнопку Delete конкретной строки
        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).Nth(rowIndex).ClickAsync();
    }

    /// <summary>
    /// Fills specific fields in a medication row by its dynamic index without adding a new row.
    /// Useful for atomized cell-filling validation tests.
    /// </summary>
    public async Task FillMedicationRowAsync(int rowIndex, string medName = "", string dosage = "", string frequency = "", string time = "")
    {
        // Находим контейнер строки по её индексу
        var row = Page.Locator(".medication-row.ng-star-inserted").Nth(rowIndex);

        // Дожидаемся появления строки в DOM-дереве перед вводом данных
        await row.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // Заполняем только те поля, значения для которых были переданы в метод
        if (medName != null) await row.Locator("input").Nth(0).FillAsync(medName);
        if (dosage != null) await row.Locator("input").Nth(1).FillAsync(dosage);
        if (frequency != null) await row.Locator("input").Nth(2).FillAsync(frequency);
        if (time != null) await row.Locator("input").Nth(3).FillAsync(time);

        // Имитируем выход из поля (blur), чтобы Angular зафиксировал изменения для валидации
        await row.Locator("input").Nth(0).PressAsync("Tab");
    }

    /// <summary>
    /// Clears text content from all input fields of a specific medication row by its index.
    /// </summary>
    public async Task ClearMedicationRowAsync(int rowIndex)
    {
        var row = Page.Locator(".medication-row.ng-star-inserted").Nth(rowIndex);

        // Очищаем ячейки последовательно
        await row.Locator("input").Nth(0).ClearAsync();
        await row.Locator("input").Nth(1).ClearAsync();
        await row.Locator("input").Nth(2).ClearAsync();
        await row.Locator("input").Nth(3).ClearAsync();

        // Триггерим событие валидации
        await row.Locator("input").Nth(0).PressAsync("Tab");
    }

    /// <summary>
    /// Verifies the total number of medication rows currently present on the UI.
    /// </summary>
    public async Task VerifyMedicationRowsCountAsync(int expectedCount)
    {
        var rows = Page.Locator(".medication-row.ng-star-inserted");
        await Assertions.Expect(rows).ToHaveCountAsync(expectedCount);
    }

    /// <summary>
    /// Validates the text content of the primary Medication Name field inside a designated row.
    /// </summary>
    public async Task VerifyMedicationRowDataAsync(int rowIndex, string expectedMedName)
    {
        var medNameInput = Page.Locator(".medication-row.ng-star-inserted").Nth(rowIndex).Locator("input").Nth(0);
        await Assertions.Expect(medNameInput).ToHaveValueAsync(expectedMedName);
    }

    /// <summary>
    /// Performs sorting-agnostic validation of current grid inputs against an expected list of medication configurations.
    /// </summary>
    /// <param name="expected">The reference dataset list holding expected configuration structures.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task VerifyDataFieldsAsync(List<MedicationInfo> expected)
    {
        // NOTE: Review debug log
        Log.Debug("[MEDICATION_TAB] Initiating flexible medication grid validation...");

        // 1. Assert expectations on an empty grid configuration context
        if (expected == null || expected.Count == 0)
        {
            var rowsCount = await Page.Locator(".medication-row.ng-star-inserted").CountAsync();
            Assert.That(rowsCount, Is.EqualTo(0), "Expected the medication grid table to be empty.");
            return;
        }

        // 2. Synchronize until structural elements finish rendering to UI
        var actualRows = Page.Locator(".medication-row.ng-star-inserted");
        await actualRows.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        int actualCount = await actualRows.CountAsync();
        Assert.That(actualCount, Is.EqualTo(expected.Count), "The total row count within the UI medication table does not match expectations.");

        // 3. Extract all data field metrics directly from interface indicators
        var uiMedications = new List<MedicationInfo>();

        for (int i = 0; i < actualCount; i++)
        {
            var row = actualRows.Nth(i);

            var actualName = await row.Locator("input").Nth(0).InputValueAsync();
            var actualDosage = await row.Locator("input").Nth(1).InputValueAsync();
            var actualFrequency = await row.Locator("input").Nth(2).InputValueAsync();
            var actualTimeReceived = await row.Locator("input").Nth(3).InputValueAsync();

            uiMedications.Add(new MedicationInfo(
                actualName.Trim(),
                actualDosage.Trim(),
                actualFrequency.Trim(),
                actualTimeReceived.Trim()
            ));
        }

        // 4. Evaluate fields against references by arranging elements ordered by Name metric
        var sortedExpected = expected.OrderBy(m => m.Name).ToList();
        var sortedUi = uiMedications.OrderBy(m => m.Name).ToList();

        for (int i = 0; i < sortedExpected.Count; i++)
        {
            Assert.That(sortedUi[i].Name, Is.EqualTo(sortedExpected[i].Name), $"Validation fault at row #{i + 1} post-sorting: Name property mismatch.");
            Assert.That(sortedUi[i].Dosage, Is.EqualTo(sortedExpected[i].Dosage), $"Validation fault at row #{i + 1} post-sorting: Dosage property mismatch.");
            Assert.That(sortedUi[i].Frequency, Is.EqualTo(sortedExpected[i].Frequency), $"Validation fault at row #{i + 1} post-sorting: Frequency property mismatch.");
            Assert.That(sortedUi[i].TimeReceived, Is.EqualTo(sortedExpected[i].TimeReceived), $"Validation fault at row #{i + 1} post-sorting: TimeReceived property mismatch.");
        }

        // NOTE: Review debug log
        Log.Debug("[MEDICATION_TAB] All medication lines resolved and verified successfully, regardless of row sequence.");
    }
}