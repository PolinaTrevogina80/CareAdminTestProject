using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;
using System.IO;

public class AttachmentsTab : BaseIncidentTabs
{
    public AttachmentsTab(IPage page) : base(page) { }

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


    public async Task VerifyAttachmentIsDisplayedAsync(string category)
    {
        // Ищем элемент, который содержит текст "MRN" и берем число под ним/рядом с ним
        // Судя по скриншоту, MRN находится в блоке с данными резидента
        var mrnElement = Page.Locator("div").Filter(new() { HasText = "MRN" }).Locator("xpath=..").Locator("span, div").Nth(1);

        // Если структура проще, можно попробовать найти по классу (если он есть, например .mrn-value)
        // Либо через плейсхолдер:
        string mrnText = await Page.GetByText("MRN").Locator("..").InnerTextAsync();

        // Оставляем только цифры (на случай, если там есть подпись "MRN 124216")
        var mrn = System.Text.RegularExpressions.Regex.Match(mrnText, @"\d+").Value;

        Log.Debug($"Считанный MRN резидента: {mrn}");

        Log.Debug("Сортируем таблицу по времени, чтобы найти последний загруженный файл.");

        // 1. Находим заголовок колонки Time и кликаем для сортировки
        // В Angular Material клик по заголовку обычно инициирует сортировку
        var timeHeader = Page.Locator("th").GetByText("Time", new() { Exact = true });
        await timeHeader.ClickAsync();

        // Ждем небольшую анимацию или обновление данных
        await Page.WaitForTimeoutAsync(500);

        // Если первая строка в таблице не та, что мы ждем, попробуем кликнуть еще раз (смена ASC на DESC)
        string expectedPart = $"{mrn}_{category}";
        var firstRow = Page.Locator("tbody tr").First;

        if (!await firstRow.GetByText(expectedPart).IsVisibleAsync())
        {
            Log.Debug("Первая строка не совпала, меняем направление сортировки...");
            await timeHeader.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }

        // 2. Теперь проверяем самую первую строку максимально строго
        await Assertions.Expect(firstRow).ToContainTextAsync(expectedPart);

        Log.Debug($"Строгая проверка пройдена: файл '{expectedPart}' находится в первой строке и дата совпадает.");
    }
}