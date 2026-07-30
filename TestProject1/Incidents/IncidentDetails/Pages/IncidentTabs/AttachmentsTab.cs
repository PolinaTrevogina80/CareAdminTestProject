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
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

        var globalLoader = Page.Locator("mat-progress-spinner, .ngx-spinner-overlay, .loader").First;
        try
        {
            // Даём приложению до 10 секунд на то, чтобы обработать файл и скрыть спиннер
            await globalLoader.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 10000 });
            Log.Debug("Global loading spinner is now hidden.");
        }
        catch (TimeoutException)
        {
            Log.Warning("Loading spinner did not disappear within 10s, attempting to proceed anyway.");
        }

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
            var matSelect = dialog.Locator("mat-select").First;
            await matSelect.ScrollIntoViewIfNeededAsync();
            await matSelect.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            // Пытаемся открыть дропдаун (максимум 3 попытки)
            bool isExpanded = false;
            int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Log.Debug($"[Attempt {attempt}/{maxAttempts}] Opening dropdown...");

                if (attempt == 1)
                {
                    // Первая попытка — стандартный клик
                    await matSelect.ClickAsync();
                }
                else
                {
                    // Если клик не помог, во 2-й и 3-й раз жмем Space
                    Log.Debug("Dropdown didn't open via click. Retrying with Space key...");
                    await matSelect.FocusAsync();
                    await matSelect.PressAsync("Space");
                }

                // Даем Angular буквально 200-300мс на рендеринг оверлея перед проверкой атрибута
                await Page.WaitForTimeoutAsync(300);

                // Проверяем, открылся ли список на самом деле
                string? ariaExpanded = await matSelect.GetAttributeAsync("aria-expanded");
                if (ariaExpanded == "true")
                {
                    Log.Debug("Dropdown successfully expanded!");
                    isExpanded = true;
                    break;
                }
            }

            if (!isExpanded)
            {
                throw new Exception("Failed to open mat-select dropdown after maximum retry attempts.");
            }

            // 4. Target list option selection action
            Log.Debug("Verifying if option overlay wrapper expanded...");
            var overlay = Page.Locator(".cdk-overlay-container");
            var option = overlay.Locator("mat-option").GetByText(currentCategory, new() { Exact = false });

            // Уменьшаем таймаут ожидания самой опции, так как оверлей мы уже гарантированно дождались сверху
            await option.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
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
    /// Triggers a file download operation for a specific category row and returns the Playwright download descriptor object.
    /// </summary>
    /// <param name="category">The category name used to isolate the target table row.</param>
    public async Task<IDownload> InitiateAttachmentDownloadAsync(string category)
    {
        // Заменяем пробелы для маски, как в вашем методе верификации отображения
        string formattedCategory = category.Replace(" ", "_").Replace("–", "-");
        string searchMask = formattedCategory.Split('_')[0];

        // Изолируем строку по маске
        var targetRow = Page.Locator("tbody tr").Filter(new() { HasText = searchMask }).First;

        // Запускаем ожидание скачивания ПЕРЕД кликом
        var downloadButton = targetRow.Locator("mat-icon[data-mat-icon-name='download-icon'], mat-icon:has-text('download')");

        // 2. ИСПРАВЛЕНО: Правильный вызов перехвата скачивания в Playwright C#
        // Метод запускает действие (клик) и одновременно ждет триггера загрузки от браузера
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await downloadButton.ClickAsync();
        });

        // 3. Возвращаем перехваченный объект загрузки в слой шагов
        return download;
    }


    /// <summary>
    /// Verifies that a uploaded attachment file is correctly generated and displayed within the grid data table by formatting search masks based on resident MRN and target category values.
    /// </summary>
    /// <param name="category">The specific document classification category string to evaluate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task VerifyAttachmentIsDisplayedAsync(string category)
    {
        // 1. Получаем MRN текущей формы
        string mrn = await GetResidentMrnAsync();
        Log.Debug($"Extracted Resident MRN code: {mrn}");

        // Проверяем, передан ли постфикс (например, "CNA Statement_A")
        string baseCategory = category;
        string postfix = "";

        if (category.Contains("_") && category.Length > 2)
        {
            var parts = category.Split('_');
            baseCategory = parts[0]; // "CNA Statement"
            postfix = parts[1];      // "A"
        }

        // 2. Форматируем базовую категорию для маски (заменяем пробелы и тире)
        string formattedCategory = baseCategory.Replace(" ", "_").Replace("–", "-");

        // Вытаскиваем первое слово категории, как у вас и было (например, "Accident" или "CNA")
        string firstWord = formattedCategory.Split('_')[0];
        string searchMask = $"{mrn}_{firstWord}";

        Log.Debug($"Searching table rows for file matching base mask criteria: '{searchMask}'");

        // 3. Умное ожидание исчезновения Kendo-спиннеров вместо хардкод-паузы 1.5с
        var noRecordsMessage = Page.GetByText("No records available");
        if (await noRecordsMessage.IsVisibleAsync())
        {
            Log.Debug("[POM] Таблица в исходном пустом состоянии. Ожидаем появления данных от бэкенда...");
            // Ждем, пока заглушка пустоты скроется, уступая место строкам (таймаут 10 секунд)
            await Assertions.Expect(noRecordsMessage).ToBeHiddenAsync(new() { Timeout = 10000 });
        }

        // Теперь, когда данные пошли, проверяем и ждем стандартный Kendo-спиннер (если он появился)
        var tableSpinner = Page.Locator(".k-loading-overlay, .k-i-loading, mat-progress-bar, .spinner").First;
        if (await tableSpinner.IsVisibleAsync())
        {
            await Assertions.Expect(tableSpinner).ToBeHiddenAsync(new() { Timeout = 30000 });
            Log.Debug("[POM] Спиннер загрузки данных успешно скрылся.");
        }

        // Дополнительная микро-пауза, чтобы Angular завершил привязку данных к строкам
        await Page.WaitForTimeoutAsync(500);

        // 4. Изолируем строки таблицы, подходящие под базовую маску (MRN + Первое слово категории)
        var matchingRows = Page.Locator("tbody tr").Filter(new() { HasText = searchMask });

        // Ждем, чтобы количество строк, подходящих под маску, стало больше 0.
        // Playwright будет опрашивать DOM в течение 10 секунд, пока бэкенд не вставит новую строку.
        try
        {
            await Assertions.Expect(matchingRows).Not.ToHaveCountAsync(0, new() { Timeout = 30000 });
        }
        catch (Exception)
        {
            Assert.Fail($"Ни одной строки с маской '{searchMask}' не появилось в таблице за 30 секунд!");
        }

        int count = await matchingRows.CountAsync();

        // 5. Если передан постфикс, ищем строку, которая содержит и маску, и букву постфикса на конце перед .pdf
        if (!string.IsNullOrEmpty(postfix))
        {
            bool postfixFound = false;
            for (int i = 0; i < count; i++)
            {
                string rowText = await matchingRows.Nth(i).InnerTextAsync();
                // Проверяем, что строка заканчивается на _A.pdf, _B.pdf и т.д.
                if (rowText.Contains($"_{postfix}.pdf") || rowText.Contains($"_{postfix}_"))
                {
                    await Assertions.Expect(matchingRows.Nth(i)).ToBeVisibleAsync(new() { Timeout = 5000 });
                    Log.Information($"Строка для категории '{baseCategory}' с постфиксом '{postfix}' успешно найдена и валидирована.");
                    postfixFound = true;
                    break;
                }
            }
            Assert.That(postfixFound, Is.True, $"Строка с базовой маской '{searchMask}' найдена, но у нее отсутствует постфикс '_{postfix}'!");
        }
        else
        {
            // Если постфикса нет — работаем по вашей стандартной логике (проверяем первую попавшуюся строку)
            var targetRow = matchingRows.First;
            await Assertions.Expect(targetRow).ToBeVisibleAsync(new() { Timeout = 10000 });
            Log.Information($"Файл для категории '{category}' (search mask: '{searchMask}') успешно отображается в таблице.");
        }
    }

    /// <summary>
    /// Extracts the numeric counter value from the "Attachments (X)" tab title.
    /// </summary>
    public async Task<int> GetTabCounterValueAsync()
    {
        // Нацеливаемся строго на span Kendo-вкладки с классом k-link, содержащий слово Attachments
        var tabElement = Page.Locator("span.k-link")
            .Filter(new() { HasText = "Attachments" })
            .First;

        // Ждем видимости элемента на экране перед чтением текста
        await tabElement.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        string tabText = await tabElement.InnerTextAsync();

        Log.Debug($"[POM] Извлечен текст Kendo-вкладки для парсинга счетчика: '{tabText}'");

        // Вытаскиваем число из скобок (0)
        var match = System.Text.RegularExpressions.Regex.Match(tabText, @"\((\d+)\)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    /// <summary>
    /// Counts the actual number of visible rows inside the attachments data table.
    /// </summary>
    public async Task<int> GetVisibleAttachmentsCountAsync()
    {
        // Если на экране видна заглушка пустого состояния, строк гарантированно 0
        var noRecordsMessage = Page.GetByText("No records available");
        if (await noRecordsMessage.IsVisibleAsync())
        {
            return 0;
        }

        var rows = Page.Locator("tbody tr");
        return await rows.CountAsync();
    }

    /// <summary>
    /// Finds an attachment row by category, clicks the delete icon, and confirms the action in the Angular Material dialog.
    /// </summary>
    // Внутри класса AttachmentsTab

    /// <summary>
    /// Finds an attachment row by category, clicks the delete icon, and confirms the action in the Angular Material dialog.
    /// </summary>
    public async Task DeleteAttachmentByCategoryAsync(string category)
    {
        // 1. Получаем MRN текущей формы
        string mrn = await GetResidentMrnAsync();

        // 2. ИСПРАВЛЕНО: Вместо одного слова "Accident" ищем комбинацию MRN и очищенного имени категории.
        // Это гарантирует уникальность, даже если в других строках тоже есть слово Accident.
        string formattedCategory = category.Replace(" ", "_").Replace("–", "-");

        // Берем первые 2 сегмента категории для поиска, чтобы избежать проблем с обрезкой длинных строк в UI
        string categoryPart = formattedCategory.Split('_')[0];
        string strictSearchMask = $"{mrn}_{categoryPart}";

        // Нацеливаем локатор на строку, содержащую именно эту уникальную маску
        var targetRow = Page.Locator("tbody tr").Filter(new() { HasText = strictSearchMask }).First;

        // 3. Находим кнопку "корзины" внутри этой строки и кликаем
        var deleteButton = targetRow.Locator("mat-icon:has-text('delete'), button.trash-btn, [data-mat-icon-name='delete-icon']");
        await deleteButton.ClickAsync();
        Log.Debug($"Нажата кнопка удаления для категории {category}");

        // 4. Ждем появления модального окна подтверждения
        var confirmDialog = Page.Locator("mat-dialog-container, .confirm-dialog, cad-confirmation-dialog").First;
        await confirmDialog.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        // 5. Кликаем кнопку подтверждения
        var confirmButton = confirmDialog.GetByRole(AriaRole.Button, new() { Name = "Delete" })
            .Or(confirmDialog.GetByRole(AriaRole.Button, new() { Name = "OK" }));

        await confirmButton.ClickAsync();
        Log.Information($"Удаление файла категории '{category}' подтверждено в попапе.");

        // 6. Ждем, пока КОНКРЕТНО ЭТА строка исчезнет из таблицы
        await Assertions.Expect(targetRow).ToBeHiddenAsync(new() { Timeout = 5000 });
    }
    public async Task<String> GetResidentMrnAsync()
    {

        var mrnElement = Page.Locator("div").Filter(new() { HasText = "MRN" }).Locator("xpath=..").Locator("span, div").Nth(1);
        string mrn = await Page.GetByText("MRN").Locator("..").InnerTextAsync();
        Log.Debug($"Extracted Resident MRN code: {mrn}");

        return Regex.Match(mrn, @"\d+").Value;
    }

    /// <summary>
    /// Changes the category dropdown value inside a specific row of the attachments table.
    /// </summary>
    public async Task ChangeAttachmentCategoryInRowAsync(string currentCategory, string newCategory)
    {
        string mrn = await GetResidentMrnAsync();
        string formattedCategory = currentCategory.Replace(" ", "_").Replace("–", "-");
        string strictSearchMask = $"{mrn}_{formattedCategory.Split('_')[0]}";

        // Находим дропдаун (mat-select или kendo-dropdownlist) внутри этой конкретной строки
        // 1. Находим и изолируем нужную строку (это у вас уже работает)
        var targetRow = Page.Locator("tbody tr").Filter(new() { HasText = strictSearchMask }).First;
        await Assertions.Expect(targetRow).ToBeVisibleAsync();

        // 2. Находим дропдаун Kendo внутри этой строки и кликаем по нему
        var rowDropdown = targetRow.Locator("kendo-dropdownlist, .k-dropdownlist, mat-select").First;
        await rowDropdown.ScrollIntoViewIfNeededAsync();
        await rowDropdown.ClickAsync();
        Log.Debug("[POM] Кликнули по инлайн-дропдауну категории в строке таблицы.");

        // 3. Точный локатор для всплывающего меню Kendo UI (.k-list-item)
        // Метод GetByText найдет элемент по точному текстовому совпадению
        var option = Page.Locator(".k-animation-container .k-list-item, .k-popup .k-list-item, mat-option")
            .GetByText(newCategory, new() { Exact = true })
            .First;

        // Ждем видимости опции и кликаем
        await option.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await option.ClickAsync();

        var kendoPopup = Page.Locator(".k-animation-container, .k-popup").First;
        if (await kendoPopup.IsVisibleAsync())
        {
            Log.Debug("[POM] Ожидаем закрытия всплывающего меню Kendo...");
            await Assertions.Expect(kendoPopup).ToBeHiddenAsync(new() { Timeout = 3000 });
        }

        await Page.WaitForTimeoutAsync(500);

        Log.Information($"[POM] Категория в строке успешно изменена на '{newCategory}'.");
    }
    /// <summary>
    /// Extracts the currently selected text from the category dropdown inside a specific table row.
    /// </summary>
    public async Task<string> GetSelectedCategoryFromRowAsync(string initialCategory)
    {
        // 1. Динамически получаем MRN прямо здесь, внутри POM
        string mrn = await GetResidentMrnAsync();

        // 2. Форматируем имя старой категории, по которому мы будем искать строку файла
        string formattedCategory = initialCategory.Replace(" ", "_").Replace("–", "-");
        string strictSearchMask = $"{mrn}_{formattedCategory.Split('_')[0]}";

        // 3. Находим нужную строку таблицы
        var targetRow = Page.Locator("tbody tr").Filter(new() { HasText = strictSearchMask }).First;
        await Assertions.Expect(targetRow).ToBeVisibleAsync(new() { Timeout = 5000 });

        // 4. Извлекаем текст из дропдауна Kendo UI (.k-input-inner или .k-input-text)
        var dropdownValueElement = targetRow.Locator("kendo-dropdownlist .k-input-inner, kendo-dropdownlist .k-input-text").First;
        await dropdownValueElement.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        string selectedText = await dropdownValueElement.InnerTextAsync();
        Log.Debug($"[POM] Извлечено выбранное значение дропдауна для строки '{strictSearchMask}': '{selectedText.Trim()}'");

        return selectedText.Trim();
    }

}