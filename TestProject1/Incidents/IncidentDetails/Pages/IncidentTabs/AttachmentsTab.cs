using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;

public class AttachmentsTab : BaseIncidentTabs
{
    public AttachmentsTab(IPage page) : base(page) { }

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

    // Локатор кнопки "+" (Add Attachment)
    private ILocator AddButton => Page.Locator("button.mdc-icon-button:has(mat-icon[data-mat-icon-name='add-icon'])");

    /// <summary>
    /// Загружает файл, используя путь к сохраненному ранее отчету
    /// </summary>
    /// <param name="filePath">Полный путь к файлу (tempPath из прошлого шага)</param>
    public async Task UploadAttachmentAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Файл не найден: {filePath}");

        string fileName = Path.GetFileName(filePath);

        var addButton = Page.Locator("button").Filter(new()
        {
            Has = Page.Locator("mat-icon[data-mat-icon-name='add-icon'], mat-icon:has-text('add')")
        });
        await addButton.ClickAsync();

        // Ищем строгое совпадение, чтобы не путать с подзаголовком
        var popupHeader = Page.GetByText("Upload a file", new() { Exact = true });
        await popupHeader.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

        // 1. Ожидаем открытие диалога и выбираем файл
        var fileInput = Page.Locator("cad-incident-add-attachment-dialog input[type='file']");
        await fileInput.SetInputFilesAsync(filePath);

        // 3. Ждем, когда файл отобразится в списке (селектор .files-list из вашего DOM)
        fileName = Path.GetFileName(filePath);
        var fileItem = Page.Locator(".files-list").GetByText(fileName);
        await fileItem.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // 2. ПРОВЕРКА: Ждем, когда имя файла появится в списке загруженных
        var uploadedFile = Page.Locator("cad-incident-add-attachment-dialog").GetByText(fileName);

        // Ждем, пока текст файла станет видимым
        await uploadedFile.WaitForAsync(new() { State = WaitForSelectorState.Visible });


        // 3. Нажимаем Next
        var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
        await nextButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await nextButton.ClickAsync(new() { Timeout = 180000 });
    }

    public async Task AssignCategoriesToAllPagesAsync(string categoryName, string? notes = null)
    {
        Log.Debug($"Вызван метод для применения одной категории '{categoryName}' ко всем страницам.");

        // Быстро определяем общее число страниц, чтобы построить корректный список
        var dialog = Page.Locator("mat-dialog-container, cad-incident-assign-pdf-to-category").First;
        await dialog.WaitForAsync();

        var paginationElement = dialog.Locator(".pagination-wrapper, .page-configuration, .pagination").Filter(new() { Has = Page.Locator("mat-icon") }).First;
        var paginationText = await paginationElement.InnerTextAsync();
        var match = System.Text.RegularExpressions.Regex.Match(paginationText, @"of\s+(\d+)");
        int totalPages = match.Success ? int.Parse(match.Groups[1].Value) : 1;

        // Генерируем список, где имя категории дублируется для каждой страницы document
        IReadOnlyList<string> categoryNames = Enumerable.Repeat(categoryName, totalPages).ToList();

        // Вызываем единый обработчик
        await AssignCategoriesInternalAsync(categoryNames, notes);
    }

    public async Task AssignCategoriesToAllPagesAsync(IReadOnlyList<string> categoryNames, string? notes = null)
    {
        Log.Debug($"Вызван метод для полистного распределения списка из {categoryNames.Count} категорий.");

        // Передаем список напрямую в единый обработчик
        await AssignCategoriesInternalAsync(categoryNames, notes);
    }

    private async Task AssignCategoriesInternalAsync(IReadOnlyList<string> categoryNames, string? notes = null)
    {
        // 1. Находим контейнер попапа
        var dialog = Page.Locator("mat-dialog-container, cad-incident-assign-pdf-to-category").First;
        await dialog.WaitForAsync();
        Log.Debug("Попап Assign Pages найден и отображен.");

        // 2. Ищем текст пагинации для определения количества страниц
        var paginationElement = dialog.Locator(".pagination-wrapper, .page-configuration, .pagination")
            .Filter(new() { Has = Page.Locator("mat-icon") }).First;
        var paginationText = await paginationElement.InnerTextAsync();
        Log.Debug($"Текст пагинации извлечен: '{paginationText}'");

        var match = System.Text.RegularExpressions.Regex.Match(paginationText, @"of\s+(\d+)");
        int totalPages = match.Success ? int.Parse(match.Groups[1].Value) : 1;
        Log.Debug($"Определено общее количество страниц: {totalPages}");

        // Защита от выхода за границы переданной коллекции
        int iterationsCount = Math.Min(totalPages, categoryNames.Count);

        for (int i = 1; i <= iterationsCount; i++)
        {
            Log.Debug($"--- Обработка страницы {i} из {totalPages} ---");

            // Очищаем строку от случайных пробелов по краям (решает проблему с Exact = true)
            string currentCategory = categoryNames[i - 1]?.Trim() ?? string.Empty;

            // 3. Поиск и клик по селекту (БЕЗ Force: true для стабильности Angular)
            var dropdown = dialog.Locator("mat-select, cad-lookup-select").First;
            await dropdown.ScrollIntoViewIfNeededAsync();
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            Log.Debug($"Кликаем по выпадающему списку для выбора '{currentCategory}'...");
            await dropdown.ClickAsync();

            // 4. Выбор опции внутри оверлея
            var overlay = Page.Locator(".cdk-overlay-container");
            await Assertions.Expect(overlay).ToBeVisibleAsync();

            // Ищем опцию. Если Exact = true продолжит падать, можно заменить на Exact = false
            var option = overlay.Locator("mat-option").GetByText(currentCategory, new() { Exact = true });
            await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await option.ClickAsync();

            Log.Debug($"Категория '{currentCategory}' успешно выбрана для страницы {i}");

            // Ждем, пока оверлей полностью закроется, чтобы не перегружать DOM Angular
            await Assertions.Expect(overlay.Locator("mat-option")).ToHaveCountAsync(0);

            // 5. Обработка категории Other для текущей страницы
            if (currentCategory.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                Log.Debug("Выбрана категория 'Other', заполняем Notes...");
                var notesField = dialog.Locator("input[name='notes'], input[formcontrolname='notes']").First;
                string notesToFill = string.IsNullOrEmpty(notes) ? "Auto-generated test notes" : notes;
                await notesField.FillAsync(notesToFill);
                Log.Debug($"Поле Notes заполнено текстом: '{notesToFill}'");
            }

            // 6. Переход к следующей странице (Рабочий локатор из div.pagination-button)
            if (i < totalPages)
            {
                Log.Debug("Нажимаем стрелку 'вправо' для перехода к следующей странице.");

                var nextButton = dialog.Locator("div.pagination-button")
                    .Filter(new() { HasText = "keyboard_arrow_right" })
                    .First;

                await nextButton.ScrollIntoViewIfNeededAsync();
                await nextButton.ClickAsync();

                // Жесткое ожидание обновления UI
                var expectedPageText = $"{i + 1} of {totalPages}";
                await Assertions.Expect(paginationElement).ToContainTextAsync(expectedPageText);
                Log.Debug($"Успешно перешли на страницу {i + 1}.");
            }
        }

        // 7. Завершение и сохранение
        var assignButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Assign Pages" });
        Log.Debug("Проверяем доступность финальной кнопки 'Assign Pages'...");

        await assignButton.ClickAsync();
        Log.Debug("Кнопка 'Assign Pages' нажата.");

        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        Log.Debug("Попап закрыт, процесс успешно завершен.");
    }

    public async Task VerifyAttachmentIsDisplayedAsync(string category)
    {
        // 1. Получаем MRN резидента из шапки формы
        var mrnElement = Page.Locator("div").Filter(new() { HasText = "MRN" }).Locator("xpath=..").Locator("span, div").Nth(1);
        string mrnText = await Page.GetByText("MRN").Locator("..").InnerTextAsync();
        var mrn = System.Text.RegularExpressions.Regex.Match(mrnText, @"\d+").Value;

        Log.Debug($"Считанный MRN резидента: {mrn}");

        // 2. Форматируем имя категории для поиска (убираем пробелы и спецсимволы)
        string formattedCategory = category.Replace(" ", "_").Replace("–", "-");

        // Берем только базовую часть: "124216_Accident" или "124216_Charge", 
        // так как длинные строки могут обрезаться интерфейсом (как видно по "Accident ..." на скриншоте)
        string searchMask = $"{mrn}_{formattedCategory.Split('_')[0]}";

        Log.Debug($"Ищем файл по маске: '{searchMask}'");

        // 3. Ждем, пока UI полностью обновится после закрытия попапа. 
        // Даем Angular 1.5 секунды на перерисовку грида, так как файлы склеиваются на бэкенде
        await Page.WaitForTimeoutAsync(1500);

        // 4. Локализируем строку таблицы, которая содержит нашу маску
        var targetRow = Page.Locator("tbody tr").Filter(new() { HasText = searchMask }).First;

        // 5. Проверяем видимость строки с увеличенным таймаутом (10 секунд на случай долгой склейки PDF)
        await Assertions.Expect(targetRow).ToBeVisibleAsync(new() { Timeout = 10000 });

        Log.Information($"Файл для категории '{category}' (маска: '{searchMask}') успешно отображается в таблице.");
    }
}