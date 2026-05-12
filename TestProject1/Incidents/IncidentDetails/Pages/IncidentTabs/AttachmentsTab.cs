using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;
using System.IO;

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
        Log.Debug("Начинаем процесс присвоения категорий страницам.");

        // 1. Находим контейнер попапа
        var dialog = Page.Locator("mat-dialog-container, cad-incident-assign-pdf-to-category").First;
        await dialog.WaitForAsync();
        Log.Debug("Попап Assign Pages найден и отображен.");

        // 2. Ищем текст пагинации. Нам нужен тот, что сверху между стрелками.
        // Судя по DOM, он обычно лежит в блоке с классом pagination-wrapper или page-configuration
        // Мы можем найти его, ища текст, который содержит "of" и находится рядом со стрелками.
        var paginationElement = dialog.Locator(".pagination-wrapper, .page-configuration, .pagination").Filter(new() { Has = Page.Locator("mat-icon") }).First;
        var paginationText = await paginationElement.InnerTextAsync();
        Log.Debug($"Текст пагинации извлечен: '{paginationText}'");

        // Регулярное выражение теперь ищет число после 'of', игнорируя лишние слова
        var match = System.Text.RegularExpressions.Regex.Match(paginationText, @"of\s+(\d+)");
        int totalPages = match.Success ? int.Parse(match.Groups[1].Value) : 1;
        Log.Debug($"Определено общее количество страниц: {totalPages}");

        for (int i = 1; i <= totalPages; i++)
        {
            Log.Debug($"--- Обработка страницы {i} из {totalPages} ---");

            // 3. Поиск и клик по селекту
            var dropdown = dialog.Locator("mat-select, cad-lookup-select").First;

            // Ждем, чтобы элемент был не просто в DOM, а готов к клику
            await dropdown.ScrollIntoViewIfNeededAsync();
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            Log.Debug("Кликаем по выпадающему списку категорий...");
            // Force: true помогает, если элемент перекрыт прозрачным overlay от предыдущих анимаций
            await dropdown.ClickAsync(new() { Force = true });

            // 4. Выбор опции
            var overlay = Page.Locator(".cdk-overlay-container");
            // Ждем появления контейнера со списком
            await Assertions.Expect(overlay).ToBeVisibleAsync();

            var option = overlay.Locator("mat-option").GetByText(categoryName, new() { Exact = true });
            await option.ClickAsync();

            Log.Debug($"Категория '{categoryName}' успешно выбрана для страницы {i}");

            if (categoryName.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                Log.Debug("Выбрана категория 'Other', заполняем Notes...");
                // Ищем текстовое поле Notes внутри диалога
                var notesField = dialog.Locator("input[name='notes'], input[formcontrolname='notes']").First;

                // Если notes не передан, используем дефолтный текст, так как поле обязательное
                string notesToFill = string.IsNullOrEmpty(notes) ? "Auto-generated test notes" : notes;

                await notesField.FillAsync(notesToFill);
                Log.Debug($"Поле Notes заполнено текстом: '{notesToFill}'");
            }


            // 5. Переход к следующей странице
            if (i < totalPages)
            {
                Log.Debug("Нажимаем стрелку 'вправо' для перехода к следующей странице.");
                var nextButton = dialog.Locator("button").Filter(new() { Has = Page.Locator("mat-icon:has-text('keyboard_arrow_right')") });
                await nextButton.ClickAsync();

                // Ждем обновления контента (номер страницы должен измениться)
                await Page.WaitForTimeoutAsync(700);
                Log.Debug("Ожидание после клика по стрелке завершено.");
            }
        }



        // 6. Завершение
        var assignButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Assign Pages" });
        Log.Debug("Проверяем доступность финальной кнопки 'Assign Pages'...");

        await assignButton.ClickAsync();
        Log.Debug("Кнопка 'Assign Pages' нажата.");

        // Ждем закрытия попапа
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        Log.Debug("Попап закрыт, метод завершен успешно.");
    }

    public async Task AssignCategoriesToAllPagesAsync(IReadOnlyList<string> categoryNames, string? notes = null)
    {
        Log.Debug("Начинаем процесс полистного присвоения списка категорий.");

        // 1. Находим контейнер попапа
        var dialog = Page.Locator("mat-dialog-container, cad-incident-assign-pdf-to-category").First;
        await dialog.WaitForAsync();
        Log.Debug("Попап Assign Pages найден и отображен.");

        // 2. Ищем текст пагинации для определения количества страниц в PDF
        var paginationElement = dialog.Locator(".pagination-wrapper, .page-configuration, .pagination").Filter(new() { Has = Page.Locator("mat-icon") }).First;
        var paginationText = await paginationElement.InnerTextAsync();
        Log.Debug($"Текст пагинации извлечен: '{paginationText}'");

        var match = System.Text.RegularExpressions.Regex.Match(paginationText, @"of\s+(\d+)");
        int totalPages = match.Success ? int.Parse(match.Groups[1].Value) : 1;
        Log.Debug($"Определено общее количество страниц: {totalPages}");

        // На всякий случай проверяем, что переданный список категорий покрывает страницы документа
        int iterationsCount = Math.Min(totalPages, categoryNames.Count);

        for (int i = 1; i <= iterationsCount; i++)
        {
            Log.Debug($"--- Обработка страницы {i} из {totalPages} ---");

            // Ключевое отличие: берем категорию из списка по индексу (i - 1), так как цикл идет с 1
            string currentCategory = categoryNames[i - 1];

            // 3. Поиск и клик по селекту
            var dropdown = dialog.Locator("mat-select, cad-lookup-select").First;
            await dropdown.ScrollIntoViewIfNeededAsync();
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            Log.Debug($"Кликаем по выпадающему списку для выбора '{currentCategory}'...");
            await dropdown.ClickAsync(new() { Force = true });

            // 4. Выбор опции
            var overlay = Page.Locator(".cdk-overlay-container");
            await Assertions.Expect(overlay).ToBeVisibleAsync();

            var option = overlay.Locator("mat-option").GetByText(currentCategory, new() { Exact = true });
            await option.ClickAsync();

            Log.Debug($"Категория '{currentCategory}' успешно выбрана для страницы {i}");

            // 5. Обработка категории Other для текущей страницы
            if (currentCategory.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                Log.Debug("Выбрана категория 'Other', заполняем Notes...");
                var notesField = dialog.Locator("input[name='notes'], input[formcontrolname='notes']").First;
                string notesToFill = string.IsNullOrEmpty(notes) ? "Auto-generated test notes" : notes;
                await notesField.FillAsync(notesToFill);
                Log.Debug($"Поле Notes заполнено текстом: '{notesToFill}'");
            }

            // 6. Переход к следующей странице
            if (i < totalPages)
            {
                Log.Debug("Нажимаем стрелку 'вправо' для перехода к следующей странице.");

                // Ищем div с классом pagination-button, внутри которого есть иконка chevron_right
                var nextButton = dialog.Locator("div.pagination-button")
                    .Filter(new() { HasText = "keyboard_arrow_right" })
                    .First;

                await nextButton.ScrollIntoViewIfNeededAsync();
                await nextButton.ClickAsync();

                // Ждем, пока номер страницы на UI обновится (например, станет i + 1)
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
        Log.Debug("Попап закрыт, метод работы со списком завершен успешно.");
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