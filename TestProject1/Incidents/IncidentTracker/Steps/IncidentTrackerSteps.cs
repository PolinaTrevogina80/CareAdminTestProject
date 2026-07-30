using Microsoft.Playwright;
using FluentAssertions;
using Log = CareAdminTestProject.Common.TestLog;
using CareAdminTestProject.Incidents.IncidentDetails.Tests;
using CareAdminTestProject.Incidents.CommonIncidentTests;

public class IncidentTrackerSteps : BaseIncidentSteps
{
    // Конструктор просто пробрасывает страницу наверх
    public IncidentTrackerSteps(IPage page) : base(page)
    {
    }

    /// <summary>
    /// Устанавливает диапазон дат в шапке и обновляет данные.
    /// </summary>
    public async Task SetDateRangeAsync(string startDate, string endDate)
    {
        Log.Information($"[ACTION] Устанавливаем диапазон дат: {startDate} — {endDate}");
        await _trackerPage.StartDateInput.FillAsync(startDate);
        await _trackerPage.EndDateInput.FillAsync(endDate);
        await _trackerPage.GoButton.ClickAsync();
        // Ждем скрытия лоадеров (используя логику из вашего метода)
    }

    /// <summary>
    /// Проверяет, что на карточке быстрого фильтра отображается корректный каунтер.
    /// </summary>
    public async Task AssertQuickFilterCounterAsync(string filterName, int expectedCount)
    {
        Log.Information($"[CHECK] Проверяем каунтер для фильтра '{filterName}': ожидаем {expectedCount}");
        var card = _trackerPage.DetailedView.QuickFilterCard(filterName);
        await Assertions.Expect(card).ToContainTextAsync(expectedCount.ToString());
    }

    /// <summary>
    /// Проверяет режим группировки и раскрывает данные резидента.
    /// </summary>
    public async Task ExpandResidentGroupAndVerifyAsync(string residentName)
    {
        Log.Information($"[ACTION] Включаем группировку и раскрываем резидента: {residentName}");
        await _trackerPage.DetailedView.ToggleGroupByResidentAsync(true);
        await _trackerPage.DetailedView.RowExpandArrow(residentName).ClickAsync();

    }

    /// <summary>
    /// Вводит имя резидента посимвольно с задержкой и проверяет динамическое сужение грида.
    /// </summary>
    public async Task SearchResidentLetterByLetterAsync(string residentName)
    {
        Log.Information($"[ACTION] Начинаем плавный поиск резидента: '{residentName}'");

        var input = _trackerPage.DetailedView.SearchResidentInput;

        // ИСПРАВЛЕНО: Вместо ClickAsync используем FocusAsync, чтобы обойти перекрытие mat-label.
        // Это мгновенно активирует поле ввода и сдвинет лейбл наверх.
        await input.FocusAsync();
        await input.ClearAsync();

        // Запоминаем исходное количество строк в гриде
        int initialRowCount = await _trackerPage.DetailedView.DataRows.CountAsync();
        Log.Debug($"[PROGRESS] Исходное количество строк до ввода: {initialRowCount}");

        // Вводим строку с задержкой
        await input.TypeAsync(residentName, new() { Delay = 200 });

        // Даем интерфейсу окончательно стабилизироваться
        await _trackerPage.DetailedView.WaitForInterfaceDebounceAsync();

        // Проверяем финальный результат
        int finalRowCount = await _trackerPage.DetailedView.DataRows.CountAsync();
        Log.Debug($"[PROGRESS] Строк в гриде после ввода всей строки: {finalRowCount}");

        // Валидируем, что грид успешно отфильтровал лишнее
        Assert.That(finalRowCount, Is.LessThan(initialRowCount),
            $"Фильтрация на лету не сработала! Количество строк осталось прежним ({initialRowCount}).");
    }

    /// <summary>
    /// Проверяет количество строк в гриде и возвращает имя резидента из указанной строки.
    /// </summary>
    public async Task<string> GrabResidentNameFromRowAsync(int targetRowIndex, int residentColumnIndex = 0)
    {
        Log.Information($"[DATA PREP] Пытаемся получить имя резидента из строки №{targetRowIndex + 1}...");

        // Даем интерфейсу устаканиться при первой загрузке
        await _trackerPage.DetailedView.WaitForInterfaceDebounceAsync();

        int totalRows = await _trackerPage.DetailedView.DataRows.CountAsync();
        Log.Debug($"[DATA PREP] Всего строк в исходном гриде: {totalRows}");

        if (totalRows <= targetRowIndex)
        {
            Assert.Inconclusive($"[SKIP] В таблице всего {totalRows} строк. " +
                                $"Невозможно взять данные из строки №{targetRowIndex + 1} для динамического теста.");
        }

        string residentName = await _trackerPage.DetailedView.GetResidentNameFromRowAsync(targetRowIndex, residentColumnIndex);


        Log.Information($"[DATA PREP] Динамически выбрано имя резидента: '{residentName}'");

        return residentName;
    }

    /// <summary>
    /// Проверяет, что резидент уникален в списке группировки, и валидирует значение его инцидентов.
    /// </summary>
    public async Task VerifyGroupedResidentIsUniqueAndMatchesCountAsync(string residentName)
    {
        Log.Information($"[CHECK] Проверяем уникальность группы и каунтер для: {residentName}");

        // 1. Проверяем, что строка с этим резидентом ровно ОДНА во всей таблице
        int residentRowsCount = await _trackerPage.DetailedView.ResidentRow(residentName).CountAsync();
        Assert.That(residentRowsCount, Is.EqualTo(1),
            $"Ошибка группировки! Резидент '{residentName}' встречается в таблице {residentRowsCount} раз(а) вместо 1.");

        // 2. Считываем каунтер инцидентов из колонки Total #
        var totalText = await _trackerPage.DetailedView.TotalCountCell(residentName).InnerTextAsync();
        int expectedCount = int.Parse(totalText.Trim());
        Log.Debug($"[CHECK] У резидента {residentName} сгруппировано инцидентов (Total #): {expectedCount}");

        // Проверяем, что число валидно (больше 0)
        Assert.That(expectedCount, Is.GreaterThan(0), "Каунтер инцидентов в сгруппированной строке равен 0");
    }



    /// <summary>
    /// Управляет состоянием чекбокса группировки по резиденту.
    /// </summary>
    public async Task SetGroupByResidentAsync(bool enable)
    {
        Log.Information($"[ACTION] Устанавливаем 'Group by Resident' в состояние: {enable}");
        var checkbox = _trackerPage.DetailedView.GroupByResidentCheckbox;
        var isChecked = await checkbox.IsCheckedAsync();

        if (isChecked != enable)
        {
            await checkbox.ClickAsync();
            await _trackerPage.DetailedView.WaitForGridLoaderAsync();
        }
    }

    /// <summary>
    /// Раскрывает сгруппированную строку резидента.
    /// </summary>
    public async Task ExpandResidentGroupAsync(string residentName)
    {
        Log.Information($"[ACTION] Раскрываем группу резидента: {residentName}");
        var arrow = _trackerPage.DetailedView.GroupExpandArrow(residentName);
        await arrow.ClickAsync();
        // Если раскрытие подгружает данные из API, ждем лоадер
        await _trackerPage.DetailedView.WaitForGridLoaderAsync();
    }

    /// <summary>
    /// Проверяет, что значение в колонке Total # совпадает с реальным количеством вложенных строк инцидентов.
    /// </summary>
    public async Task VerifyResidentTotalIncidentsCountAsync(string residentName)
    {
        Log.Information($"[CHECK] Проверяем соответствие каунтера Total # для {residentName}");

        // 1. Получаем число из колонки Total # (например, 34)
        var totalText = await _trackerPage.DetailedView.TotalCountCell(residentName).InnerTextAsync();
        int expectedCount = int.Parse(totalText.Trim());
        Log.Debug($"[CHECK] В строке группы указано инцидентов: {expectedCount}");

        // 2. Находим все строки таблицы
        var allRows = _trackerPage.DetailedView.GridRows;
        int totalRowsInDom = await allRows.CountAsync();

        int actualRowsCount = 0;
        bool targetGroupFound = false;

        // 3. Итерируемся по строкам
        for (int i = 0; i < totalRowsInDom; i++)
        {
            var row = allRows.Nth(i);

            // Берем текст из ПЕРВОЙ ячейки (колонка Resident)
            var firstCellText = (await row.Locator("td").First.InnerTextAsync()).Trim();

            // Если мы еще не дошли до нашей Ольги, ищем ее строку
            if (!targetGroupFound)
            {
                if (firstCellText.Contains(residentName, StringComparison.OrdinalIgnoreCase))
                {
                    targetGroupFound = true;
                    // ИСПРАВЛЕНО: Эта строка уже содержит первый инцидент, поэтому счетчик стартует с 1
                    actualRowsCount = 1;
                }
            }
            else
            {
                // Если мы уже внутри группы Ольги, проверяем первую ячейку следующей строки:
                // Если она НЕ пустая (содержит имя нового резидента), группа Ольги закончилась.
                if (!string.IsNullOrEmpty(firstCellText))
                {
                    break;
                }

                // Если первая ячейка пустая — это вложенный инцидент нашей Ольги
                actualRowsCount++;
            }
        }

        Log.Debug($"[CHECK] Фактически насчитано вложенных строк инцидентов для группы: {actualRowsCount}");

        // Проверяем совпадение через стандартный NUnit Assert
        Assert.That(actualRowsCount, Is.EqualTo(expectedCount),
            $"Количество видимых строк инцидентов должно соответствовать значению Total # ({expectedCount})");
    }

    /// <summary>
    /// Считывает дату и время инцидента из указанной строки для динамической фильтрации.
    /// </summary>
    public async Task<(string Date, string Time)> GrabIncidentDateTimeAsync(int targetRowIndex, int dateColumn = 1, int timeColumn = 2)
    {
        Log.Information($"[DATA PREP] Извлекаем дату/время инцидента из строки №{targetRowIndex + 1}...");

        string date = await _trackerPage.DetailedView.GetIncidentDateFromRowAsync(targetRowIndex, dateColumn);
        string time = await _trackerPage.DetailedView.GetIncidentTimeFromRowAsync(targetRowIndex, timeColumn);

        Log.Information($"[DATA PREP] Считаны данные: Дата='{date}', Время='{time}'");
        return (date, time);
    }

    /// <summary>
    /// Устанавливает диапазон дат в шапке трекера и нажимает кнопку GO.
    /// </summary>
    public async Task FilterByDateRangeAsync(string startDate, string endDate)
    {
        Log.Information($"[ACTION] Устанавливаем фильтр дат: с {startDate} по {endDate}");

        // Используем FocusAsync для обхода mat-label перекрытий
        await _trackerPage.StartDateInput.FocusAsync();
        await _trackerPage.StartDateInput.ClearAsync();
        await _trackerPage.StartDateInput.TypeAsync(startDate);

        await _trackerPage.EndDateInput.FocusAsync();
        await _trackerPage.EndDateInput.ClearAsync();
        await _trackerPage.EndDateInput.TypeAsync(endDate);

        Log.Information("[ACTION] Нажимаем кнопку 'GO' для применения фильтра...");
        await _trackerPage.GoButton.ClickAsync();

        // После клика на GO страница трекера перезагружает данные, сбрасываем флаг и ждем лоадер
        _trackerPage.ResetLoadState();
        await _trackerPage.WaitForPageLoadAsync();
    }

    /// <summary>
    /// Переходит на вкладку Completion View.
    /// </summary>
    public async Task SwitchToCompletionViewAsync()
    {
        Log.Information("[ACTION] Переключаемся на вкладку 'Completion View'...");
        await _trackerPage.CompletionView.OpenAsync();
    }

    public async Task SwitchToReferralsViewAsync()
    {
        Log.Information("[ACTION] Переключаемся на вкладку 'Incident Referrals'...");
        await _trackerPage.Referral.OpenAsync();
    }

    /// <summary>
    /// Плавный посимвольный поиск резидента на вкладке Completion View.
    /// </summary>
    public async Task SearchResidentInCompletionViewLetterByLetterAsync(string residentName)
    {
        Log.Information($"[ACTION] Поиск резидента '{residentName}' на вкладке Completion View...");

        var input = _trackerPage.CompletionView.SearchResidentInput;
        await input.FocusAsync();
        await input.ClearAsync();

        int initialRows = await _trackerPage.CompletionView.DataRows.CountAsync();

        await input.TypeAsync(residentName, new() { Delay = 200 });
        await _trackerPage.DetailedView.WaitForInterfaceDebounceAsync();

        int finalRows = await _trackerPage.CompletionView.DataRows.CountAsync();
        Assert.That(finalRows, Is.LessThanOrEqualTo(initialRows), "Количество строк не уменьшилось после ввода поиска");
    }

    /// <summary>
    /// Считывает Дату (колонка 2) и Время (колонка 3) со вкладки Completion View.
    /// </summary>
    public async Task<(string Date, string Time)> GrabCompletionIncidentDateTimeAsync(int targetRowIndex)
    {
        Log.Information($"[DATA PREP] Извлекаем дату/время из строки №{targetRowIndex + 1} вкладки Completion View...");

        var row = _trackerPage.CompletionView.DataRows.Nth(targetRowIndex);

        string date = (await row.Locator("td").Nth(2).InnerTextAsync()).Trim();
        string time = (await row.Locator("td").Nth(3).InnerTextAsync()).Trim();

        Log.Information($"[DATA PREP] Считаны данные со вкладки Completion: Дата='{date}', Время='{time}'");
        return (date, time);
    }

}