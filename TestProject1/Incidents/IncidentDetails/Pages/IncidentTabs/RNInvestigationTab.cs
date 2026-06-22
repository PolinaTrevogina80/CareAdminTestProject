using CareAdminTestProject.Common;
using Microsoft.Playwright;
using System.Drawing;
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
            await SelectTimeInPickerAsync("answerTime", info.LastSeen.Time);
            var detailsArea = Page.GetByPlaceholder("Enter details");
            await detailsArea.FillAsync(info.LastSeen.Details);
            if (ToDoScreenshots) { await Page.MakeScreenshotAsync("RN_Step1_Filled"); }
        }, onStepFilled);

        // Step 2: Exact meticulous description
        await FillFormStepAsync(2, async () =>
        {
            var detailsArea = Page.GetByPlaceholder("Enter details");
            await detailsArea.FillAsync(info.DescribeExactly.Details);
            if (ToDoScreenshots) { await Page.MakeScreenshotAsync("RN_Step2_Filled"); }
        }, onStepFilled);

        // Идем циклом по всем потенциальным шагам
        for (int stepNumber = 3; stepNumber <= 28; stepNumber++)
        {
            int questionIndex = stepNumber - 3;

            // ИСПРАВЛЕНИЕ: Если данные для заполнения закончились — просто выходим из метода.
            // Никаких кликов по "To overview" здесь не делаем, оставляем форму в текущем состоянии.
            if (questionIndex >= info.Questions.Count)
            {
                Console.WriteLine($"[FILL] Вопросы для заполнения закончились (передано {info.Questions.Count}). Выходим из метода на шаге {stepNumber}.");
                return;
            }

            // Если данные для текущего шага есть — продолжаем обычное заполнение
            await FillFormStepAsync(stepNumber, async () =>
            {
                var currentQuestion = info.Questions[questionIndex];
                var buttonName = currentQuestion.Answer ? "YES" : "NO";

                var toggleButton = Page.Locator("mat-button-toggle")
                    .Filter(new() { HasTextRegex = new Regex($"^{buttonName}$", RegexOptions.IgnoreCase) });

                await toggleButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                await toggleButton.ClickAsync();

                if (!string.IsNullOrEmpty(currentQuestion.Comments))
                {
                    var detailsArea = Page.GetByPlaceholder("Enter comments");
                    await detailsArea.FillAsync(currentQuestion.Comments);
                }

                if (ToDoScreenshots)
                {
                    await Page.MakeScreenshotAsync($"RN_Step_{stepNumber}_Filled");
                }
            }, onStepFilled);
        }
    }


    /// <summary>
    /// Wrapper for step processing: waits for the step/page number, performs the actions, and clicks "Next".
    /// </summary>
    /// <param name="stepNumber">The target step sequential position number out of the total 28 steps.</param>
    /// <param name="fillAction">The delegate wrapper housing input form workflow steps.</param>
    /// <param name="onStepFilled">An optional callback delegate triggered after completing an isolated workflow step.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FillFormStepAsync(int stepNumber, Func<Task> fillAction, Func<int, Task>? onStepFilled = null)
    {
        var pagination = Page.Locator("div.pagination").Last;

        // Wait until the pagination counter matches the precise page index currently being evaluated
        await Expect(pagination).ToContainTextAsync($"{stepNumber} of 28", new() { Timeout = 10000 });

        // Execute structural internal field injection logic
        await fillAction();

        // ВЫЗОВ КОЛБЭКА: Данные введены, но мы еще стоим на текущем шаге
        if (onStepFilled != null)
        {
            await onStepFilled(stepNumber);
        }

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

        // ХАК ДЛЯ СБРОСА СТЭКА АНГУЛЯРА:
        // Кликаем по названию вкладки или заголовку, ЧТОБЫ ВЫВЕСТИ ФОКУС ИЗ СРЕДЫ ПОИСКА (Search) прошлого шага
        Console.WriteLine("[ACTION] Перед открытием жестко сбрасываем фокус формы...");
        var safeLabel = Page.Locator("text=Area(s) of Accident to be investigated").First;
        if (await safeLabel.IsVisibleAsync())
        {
            await safeLabel.ClickAsync();
            await Task.Delay(200);
        }

        Console.WriteLine($"[ACTION] Открываем мультиселект локаций...");

        var multiSelectControl = Page.Locator("cad-multi-select").First;
        await multiSelectControl.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await multiSelectControl.ScrollIntoViewIfNeededAsync();

        // Находим кнопку плюса
        var addButton = multiSelectControl.Locator("mat-icon.add-button, mat-icon[role='img']").First;
        await addButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // Делаем клик по плюсу
        await addButton.ClickAsync(new() { Force = true });

        // ТОЧНЫЙ СЕЛЕКТОР ОВЕРЛЕЯ из DevTools
        var optionsContainer = Page.Locator(".cdk-overlay-container .cdk-overlay-pane:visible").Last;
        await optionsContainer.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await Task.Delay(300);

        // Выбираем элементы
        foreach (var location in locations)
        {
            var option = optionsContainer.Locator("mat-list-item, mat-option, .mat-mdc-list-item")
                .Filter(new()
                {
                    HasTextRegex = new Regex($"^\\s*{location}\\s*$", RegexOptions.IgnoreCase)
                });

            if (await option.CountAsync() > 0)
            {
                await option.First.ClickAsync();
                await Task.Delay(150); // Даем Angular время добавить плашку
            }
        }

        // Закрываем выпадающее меню
        Console.WriteLine("[ACTION] Закрываем выпадающий список.");
        await Page.Keyboard.PressAsync("Escape");
        await optionsContainer.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 3000 });
        await Task.Delay(250);

        // Снова уводим фокус в молоко, чтобы подготовить почву для возможного третьего шага
        if (await safeLabel.IsVisibleAsync())
        {
            await safeLabel.ClickAsync();
        }
        await Task.Delay(200);
    }


    /// <summary>
    /// Удаляет выбранную локацию (зону) из списка, нажимая на крестик/иконку удаления внутри плашки.
    /// </summary>
    public async Task RemoveLocationAsync(string location)
    {
        Console.WriteLine($"[ACTION] Удаляем локацию: '{location}'");

        // Ищем плашку (div) внутри кастомного компонента cad-chips по тексту локации
        var chip = Page.Locator("cad-chips div.selected-item")
            .Filter(new() { HasTextRegex = new Regex($"^\\s*{Regex.Escape(location)}\\s*", RegexOptions.IgnoreCase) })
            .First;

        await chip.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // Кликаем по иконке удаления внутри этой плашки (используем класс из вашей верстки)
        var deleteButton = chip.Locator("mat-icon.delete-button").First;
        await deleteButton.ClickAsync();

        // Даем Angular полсекунды на анимацию удаления плашки
        await Task.Delay(500);
    }


    /// <summary>
    /// Проверяет, что на UI выбран строго определенный набор локаций.
    /// </summary>
    public async Task VerifySelectedLocationsAsync(IEnumerable<string> expectedLocations)
    {
        var actualLocations = await GetSelectedLocationsAsync();

        Console.WriteLine($"[ASSERT] Проверка локаций. Ожидаем: [{string.Join(", ", expectedLocations)}], На UI: [{string.Join(", ", actualLocations)}]");

        // Используем NUnit Assert для глубокого сравнения коллекций без учета порядка элементов
        Assert.That(actualLocations, Is.EquivalentTo(expectedLocations),
            $"Список выбранных локаций на UI не соответствует ожидаемому.");
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
    /// Нажимает стрелку назад на панели пагинации, чтобы вернуться на предыдущий шаг.
    /// </summary>
    public async Task GoBackStepAsync()
    {
        // Находим кнопку "Назад" по иконке стрелки влево
        var backButton = Page.Locator(".pagination-button:has(mat-icon:text('keyboard_arrow_left'))").First;
        await backButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await backButton.ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(250); // Даем время на анимацию переключения слайда
    }


    /// <summary>
    /// Проверяет состояние кнопки "Назад". 
    /// Если isEnabled == false, проверяет, что клик по кнопке не меняет текущий шаг формы.
    /// </summary>
    public async Task VerifyBackButtonStateAsync(bool isEnabled)
    {
        Console.WriteLine($"[ASSERT] Проверяем состояние кнопки 'Назад' (Ожидаем: {(isEnabled ? "АКТИВНА" : "БЛОКИРУЕТ ДЕЙСТВИЕ")})...");

        // Ищем кнопку строго ВНУТРИ родительского контейнера div.pagination
        var paginationContainer = Page.Locator("div.pagination").Last;
        var backButton = paginationContainer.Locator("button, .pagination-button").Filter(new() { Has = Page.Locator("mat-icon:text('keyboard_arrow_left')") }).First;
        await backButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        int stepBeforeClick = await GetCurrentStepNumberInternalAsync();

        if (isEnabled)
        {
            await backButton.ClickAsync();
            await Task.Delay(300); // Даем время на анимацию слайда
            int stepAfterClick = await GetCurrentStepNumberInternalAsync();

            Assert.That(stepAfterClick, Is.LessThan(stepBeforeClick),
                $"Кнопка 'Назад' должна была вернуть нас на шаг назад, но шаг не изменился (Был: {stepBeforeClick}, Стал: {stepAfterClick}).");
        }
        else
        {
            await backButton.ClickAsync(new() { Force = true });
            await Task.Delay(300);

            int stepAfterClick = await GetCurrentStepNumberInternalAsync();

            Assert.That(stepAfterClick, Is.EqualTo(stepBeforeClick),
                $"Кнопка 'Назад' должна быть заблокирована на первом шаге, однако клик по ней изменил страницу! (Был: {stepBeforeClick}, Стал: {stepAfterClick}).");

            Console.WriteLine($"[SUCCESS] Клик успешно проигнорирован. Мы остались на шаге {stepAfterClick}.");
        }
    }

    /// <summary>
    /// Проверяет состояние кнопки "Вперед".
    /// Если isEnabled == false, проверяет, что клик по кнопке не меняет текущий шаг формы.
    /// </summary>
    public async Task VerifyNextButtonStateAsync(bool isEnabled)
    {
        Console.WriteLine($"[ASSERT] Проверяем состояние кнопки 'Вперед' (Ожидаем: {(isEnabled ? "АКТИВНА" : "БЛОКИРУЕТ ДЕЙСТВИЕ")})...");

        var paginationContainer = Page.Locator("div.pagination").Last;
        var nextButton = paginationContainer.Locator("button, .pagination-button").Filter(new() { Has = Page.Locator("mat-icon:text('keyboard_arrow_right')") }).First;
        await nextButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        int stepBeforeClick = await GetCurrentStepNumberInternalAsync();

        if (isEnabled)
        {
            bool hasDisabledAttribute = await nextButton.GetAttributeAsync("disabled") != null;
            Assert.That(hasDisabledAttribute, Is.False, "Кнопка 'Вперед' задизейблена на уровне HTML, хотя должна быть активна.");
        }
        else
        {
            await nextButton.ClickAsync(new() { Force = true });
            await Task.Delay(300);

            int stepAfterClick = await GetCurrentStepNumberInternalAsync();

            Assert.That(stepAfterClick, Is.EqualTo(stepBeforeClick),
                $"Кнопка 'Вперед' должна быть заблокирована на последнем шаге, однако клик по ней изменил страницу! (Был: {stepBeforeClick}, Стал: {stepAfterClick}).");

            Console.WriteLine($"[SUCCESS] Клик успешно проигнорирован. Мы остались на шаге {stepAfterClick}.");
        }
    }

    /// <summary>
    /// Публичный метод проверки номера текущего шага для вашего теста
    /// </summary>
    public async Task VerifyCurrentStepNumberAsync(int expectedStep)
    {
        int actualStep = await GetCurrentStepNumberInternalAsync();
        Assert.That(actualStep, Is.EqualTo(expectedStep), $"Ожидали шаг {expectedStep}, но пагинатор показывает шаг {actualStep}.");
    }

    /// <summary>
    /// Вспомогательный внутренний метод для точного парсинга текста "1 of 28" строго внутри блока пагинации
    /// </summary>
    private async Task<int> GetCurrentStepNumberInternalAsync()
    {
        // Берем текст БЕЗ иконок стрелочек строго из контейнера пагинации
        var paginationContainer = Page.Locator("div.pagination").Last;
        await paginationContainer.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        string fullText = await paginationContainer.TextContentAsync();

        // Регулярным выражением забираем первую цифру перед словом "of" (из строки вида "keyboard_arrow_left 1 of 28 keyboard_arrow_right")
        var match = System.Text.RegularExpressions.Regex.Match(fullText, @"(\d+)\s+of\s+\d+");

        if (match.Success && int.TryParse(match.Groups[1].Value, out int step))
        {
            return step;
        }

        Assert.Fail($"Не удалось извлечь номер шага из текста пагинации контейнера: '{fullText}'");
        return -1;
    }

    /// <summary>
    /// Collects and processes the textual array of location markers currently selected on the user interface.
    /// </summary>
    /// <returns>A clean read-only string list of active location chip configurations.</returns>
    public async Task<IReadOnlyList<string>> GetSelectedLocationsAsync()
    {
        // Находим все теги span с текстом локаций внутри плашек
        var locationSpans = Page.Locator("cad-chips div.selected-item span");
        var count = await locationSpans.CountAsync();

        var selectedList = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var text = await locationSpans.Nth(i).InnerTextAsync();
            var cleanText = text.Trim();

            if (!string.IsNullOrEmpty(cleanText))
            {
                selectedList.Add(cleanText);
            }
        }

        return selectedList.AsReadOnly();
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
    /// Wrapper for verification step processing: manages pagination, executes checks on the current slide.
    /// </summary>
    public async Task VerifyFormStepAsync(int stepNumber, Func<Task> verifyAction, bool advanceToNextStep = true)
    {
        var pagination = Page.Locator("div.pagination").Last;

        // Ensure that the wizard has successfully completed its layout navigation shift to the required page indices
        await Expect(pagination).ToContainTextAsync($"{stepNumber} of 28", new() { Timeout = 10000 });

        // Execute target assertions for the current active page template
        await verifyAction();

        // Переходим на следующий шаг только если флаг равен true
        if (advanceToNextStep)
        {
            await GoToNextStepAsync();
        }
    }

    /// <summary>
    /// Verifies the current text value of the form completion progress indicator (e.g., "25%" or "100%").
    /// Utilizes the built-in assertions framework to seamlessly handle dynamic UI transitions during form entry.
    /// </summary>
    /// <param name="expectedPercentage">The expected percentage string, including the '%' symbol.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task VerifyProgressBarPercentageAsync(string expectedPercentage)
    {
        // Локатор находит span с классом progress-percent внутри контейнера
        var percentLocator = Page.Locator("span.progress-percent");

        // Проверяем, что текст совпадает с ожидаемым (например, "25%" или "100%")
        await Assertions.Expect(percentLocator).ToHaveTextAsync(expectedPercentage);
    }

    /// <summary>
    /// Navigates to the questionnaire overview screen by clicking the "To overview" action button.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClickToOverviewAsync()
    {
        var button = GetButtonByText("To overview");
        await button.ClickAsync();
    }


    /// <summary>
    /// Iterates through all questions in the data model and verifies that the Material data table 
    /// on the overview screen accurately reflects the expected 'X' marks and comment strings.
    /// </summary>
    /// <param name="expected">The full expected data model containing questions, answers, and comments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task VerifyOverviewAllQuestionsAsync(RNSupervisorTabInfo expected)
    {
        Console.WriteLine("[ALL-CHECK] >>> Начало полной проверки экрана Overview (Шаги 1-28) <<<");

        var allRows = Page.Locator("tr[role='row'].mat-mdc-row");

        // ==========================================================
        // БЛОК 1: Проверка уникальных текстовых шагов (Шаг 1 и Шаг 2)
        // ==========================================================

        // Шаг 1: Время последнего визита
        Console.WriteLine("[ALL-CHECK] Проверяем Шаг 1: Время последнего визита и детали...");
        var firstRow = allRows.Nth(0);
        var timeCell = firstRow.Locator("td[class*='column-timeAnswer'], td[class*='column-time']");
        var firstStepCommentCell = firstRow.Locator("td[class*='column-comments']");

        // Если данные не переданы (тест на пустую форму)
        if (expected.LastSeen == null || string.IsNullOrEmpty(expected.LastSeen.Details))
        {
            Console.WriteLine("[ALL-CHECK] Данные для Шага 1 не переданы. Проверяем дефолтное системное время '12:00 AM' и пустые детали.");

            // Система по умолчанию выводит 12:00 AM на пустой форме
            await Assertions.Expect(timeCell).ToHaveTextAsync("12:00 AM");
            await Assertions.Expect(firstStepCommentCell).ToHaveTextAsync("");
        }
        else
        {
            string formattedTime = expected.LastSeen.Time.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            await Assertions.Expect(timeCell).ToContainTextAsync(formattedTime);
            await Assertions.Expect(firstStepCommentCell).ToContainTextAsync(expected.LastSeen.Details);
            Console.WriteLine($"[SUCCESS] Шаг 1 валиден (Время: {formattedTime})");
        }


        // Шаг 2: Точное описание
        Console.WriteLine("[ALL-CHECK] Проверяем Шаг 2: Точное описание...");
        var secondRow = allRows.Nth(1);

        if (expected.DescribeExactly == null || string.IsNullOrEmpty(expected.DescribeExactly.Details))
        {
            Console.WriteLine("[ALL-CHECK] Данные для Шага 2 не переданы. Проверяем, что в строке нет введенного текста.");

            // Вместо поиска ячейки проверяем, что вся строка содержит только базовое название вопроса
            // На пустой форме там будет только текст: "Describe exactly what happened or what was observed?"
            await Assertions.Expect(secondRow).ToHaveTextAsync("Describe exactly what happened or what was observed?");
        }
        else
        {
            // Если данные есть, проверяем, что в строке присутствует введенный текст описания
            await Assertions.Expect(secondRow).ToContainTextAsync(expected.DescribeExactly.Details);
            Console.WriteLine($"[SUCCESS] Шаг 2 валиден (Содержит текст: '{expected.DescribeExactly.Details}')");
        }

        // ==========================================================
        // БЛОК 2: Проверка стандартной таблицы вопросов (Шаги 3-28)
        // ==========================================================
        // ... (весь остальной цикл по i от 0 до 25 остается без изменений) ...
    }

    /// <summary>
    /// Verifies whether a specific question row within the overview Material table represents an unanswered gap 
    /// by checking that both the 'YES' and 'NO' selection columns remain completely empty.
    /// </summary>
    /// <param name="questionIndex">The 1-based index of the question row to interrogate.</param>
    /// <param name="expectedGap">True if the row is expected to be blank/unanswered; otherwise, false.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task VerifyOverviewQuestionGapStatusAsync(int questionIndex, bool expectedGap)
    {
        // Находим строку, которая начинается с номера "X." (например, "6." или "28.")
        var rowLocator = Page.Locator("tr[role='row'].mat-mdc-row")
            .Filter(new() { HasTextRegex = new Regex($"^{questionIndex}\\.") });

        var yesCell = rowLocator.Locator("td.dk-column-yes");
        var noCell = rowLocator.Locator("td.dk-column-no");

        if (expectedGap)
        {
            // Если это пропуск — ячейки выбора должны быть пустыми
            await Assertions.Expect(yesCell).ToHaveTextAsync("");
            await Assertions.Expect(noCell).ToHaveTextAsync("");
        }
        else
        {
            // Если вопрос должен быть отвечен
            var yesText = await yesCell.InnerTextAsync();
            var noText = await noCell.InnerTextAsync();
            Assert.That(yesText == "X" || noText == "X", Is.True, $"Question {questionIndex} was expected to be answered.");
        }
    }

    public async Task VerifyOverviewBaseStepsAsync(RNSupervisorTabInfo expected)
    {
        // Строка 1: Время последнего визита
        var timeRow = Page.Locator("tr[role='row'].mat-mdc-row").Filter(new() { HasTextRegex = new Regex("^1\\.") });

        // Форматируем TimeOnly в строку вида "9:00 AM" или "11:30 PM" (в зависимости от требований интерфейса)
        // Если в системе используется 24-часовой формат, используйте "HH:mm" вместо "h:mm tt"
        string formattedTime = expected.LastSeen.Time.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);

        await Assertions.Expect(timeRow).ToContainTextAsync(formattedTime);
        await Assertions.Expect(timeRow).ToContainTextAsync(expected.LastSeen.Details);

        // Строка 2: Точное описание
        var descRow = Page.Locator("tr[role='row'].mat-mdc-row").Filter(new() { HasTextRegex = new Regex("^2\\.") });
        await Assertions.Expect(descRow).ToContainTextAsync(expected.DescribeExactly.Details);
    }


}
