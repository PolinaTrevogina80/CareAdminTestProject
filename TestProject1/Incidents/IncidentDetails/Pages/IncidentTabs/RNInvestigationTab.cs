using Microsoft.Playwright;
using Serilog;
using System.Globalization;
using System.Text.RegularExpressions;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab.RNSupervisorTabInfo;
using static DetailsTab;
using static Microsoft.Playwright.Assertions;

namespace CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;

public class RNSupervisorTab : BaseIncidentTabs
{
    private Boolean ToDoScreenshots = false;
    public RNSupervisorTab(IPage page) : base(page) { }

    public record RNSupervisorTabInfo(
        IReadOnlyList<string> Locations, // Список строк для выбора в мультиселекте
        LastSeenInfo LastSeen,
        DescribeExactlyInfo DescribeExactly,
    // Steps (4-28)
    IReadOnlyList<QuestionWithDetails> Questions
    )
    {
        public record LastSeenInfo(TimeOnly Time, string Details);
        public record DescribeExactlyInfo(string Details);
        public record QuestionWithDetails(bool Answer, string Comments = "");
    }

    public async Task FillQuestionsAsync(RNSupervisorTabInfo info, Func<int, Task>? onStepFilled = null)
    {
        await SelectLocationsAsync(info.Locations);


        // Шаг 1: Время последнего визита
        await FillFormStepAsync(1, async () =>
        {
            // Используем "answerTime" (имя из вашего примера)
            await SelectTimeInPickerAsync("answerTime", info.LastSeen.Time);

            // Находим textarea на текущей странице
            var detailsArea = Page.GetByPlaceholder("Enter details");
            await detailsArea.FillAsync(info.LastSeen.Details);
            if (ToDoScreenshots)
            {
                await Page.MakeScreenshotAsync("RN_Step1_Filled");
            }
        });

        // Шаг 2: Детальное описание
        await FillFormStepAsync(2, async () =>
        {
            // Заполняем детали для второго вопроса
            var detailsArea = Page.GetByPlaceholder("Enter details");
            await detailsArea.FillAsync(info.DescribeExactly.Details);
            if (ToDoScreenshots)
            {
                await Page.MakeScreenshotAsync("RN_Step2_Filled");
            }
        });

        for (int i = 0; i < info.Questions.Count; i++)
        {
            var currentQuestion = info.Questions[i];
            int stepNumber = i + 3; // Начинаем с 4-го шага

            await FillFormStepAsync(stepNumber, async () =>
            {
                var buttonName = currentQuestion.Answer ? "YES" : "NO";

                // 1. Ищем mat-button-toggle, который содержит нужный текст
                // Это более надежно для Angular Material, чем GetByRole
                var toggleButton = Page.Locator("mat-button-toggle")
                    .Filter(new() { HasTextRegex = new Regex($"^{buttonName}$", RegexOptions.IgnoreCase) });

                // 2. Ждем кликабельности и нажимаем
                await toggleButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                await toggleButton.ClickAsync();

                // 3. Если есть текст комментария — заполняем
                if (!string.IsNullOrEmpty(currentQuestion.Comments))
                {
                    var detailsArea = Page.GetByPlaceholder("Enter comments");
                    await detailsArea.FillAsync(currentQuestion.Comments);
                }

                if (ToDoScreenshots)
                {
                    await Page.MakeScreenshotAsync($"RN_Step_{stepNumber}_Filled");
                }
            });
        }

    }

    /// <summary>
    /// Обертка для обработки шага: ждет номер страницы, выполняет действия и жмет "Далее"
    /// </summary>
    private async Task FillFormStepAsync(int stepNumber, Func<Task> fillAction)
    {
        var pagination = Page.Locator("div.pagination").Last;

        // Ждем, когда страница станет именно той, которую мы заполняем
        await Expect(pagination).ToContainTextAsync($"{stepNumber} of 28", new() { Timeout = 10000 });

        // Выполняем логику заполнения полей
        await fillAction();

        // Переходим к следующему шагу
        await GoToNextStepAsync();

        // ФИКС: Ждем исчезновения текста ТОЛЬКО если это НЕ последняя страница
        if (stepNumber < 28)
        {
            await Expect(pagination).Not.ToContainTextAsync($"{stepNumber} of 28", new() { Timeout = 3000 });
        }
    }

    public async Task SelectLocationsAsync(IReadOnlyList<string> locations)
    {
        if (locations == null || !locations.Any()) return;

        // 1. Находим и кликаем на кнопку "+" внутри блока Area(s) of Accident
        // Ищем контейнер секции, а затем в нем кнопку
        var addButton = Page.Locator(".locations")
            .Locator("button, mat-icon")
            .Filter(new() { HasText = "add" })
            .First;

        await addButton.ClickAsync();

        // 2. Ждем появления контейнера со списком (обычно это mat-option или mat-list-item)
        // В Angular Material выпадающие списки рендерятся в отдельном контейнере в корне body
        var optionsContainer = Page.Locator(".cdk-overlay-container");
        await optionsContainer.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        foreach (var location in locations)
        {
            // Ищем элемент списка, который содержит точный текст (с учетом возможных пробелов)
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

        // 3. Закрываем список (нажимаем Escape или кликаем в пустое место), 
        // если он не закрылся автоматически после выбора
        await Page.Keyboard.PressAsync("Escape");
    }

    public async Task GoToNextStepAsync()
    {
        var nextButton = Page.Locator(".pagination-button:has(mat-icon:text('keyboard_arrow_right'))").First;
        await nextButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await nextButton.ClickAsync();

        // Завершение анимации перехода слайда в Kendo/Angular
        // Без этого клики могут регистрироваться браузером дважды или проглатываться
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle); // Ждем, если идут микро-запросы
        await Task.Delay(250); // Даем четверть секунды на завершение CSS-транзишна слайда
    }

    public async Task<IReadOnlyList<string>> GetSelectedLocationsAsync()
    {
        // 1. Находим контейнер со всеми выбранными элементами
        var chipsContainer = Page.Locator("div.cad-chips-wrapper, div.selected-items").First;
        await chipsContainer.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        // 2. Небольшая задержка для завершения рендеринга Angular
        await Page.WaitForTimeoutAsync(200);

        // 3. Берем именно span с текстом внутри каждого выбранного элемента
        var locationSpans = chipsContainer.Locator("div.selected-item span");

        var cleanLocations = new List<string>();
        int count = await locationSpans.CountAsync();

        for (int i = 0; i < count; i++)
        {
            // Извлекаем чистый текст напрямую из тега span
            string rawText = await locationSpans.Nth(i).EvaluateAsync<string>("el => el.textContent") ?? "";

            string cleanText = rawText.Trim();

            if (!string.IsNullOrEmpty(cleanText))
            {
                cleanLocations.Add(cleanText);
            }
        }

        return cleanLocations;
    }

    // Проверка того, какая кнопка-тоггл (YES/NO) сейчас активна
    public async Task<string> GetSelectedToggleValueAsync()
    {
        // Находим mat-button-toggle, у которого присутствует класс активности (например, mat-button-toggle-checked)
        var checkedToggle = Page.Locator("mat-button-toggle.mat-button-toggle-checked, mat-button-toggle[checked='true']");
        if (await checkedToggle.CountAsync() > 0)
        {
            return await checkedToggle.InnerTextAsync();
        }
        return string.Empty;
    }


    public async Task VerifyDataFieldsAsync(RNSupervisorTabInfo expected)
    {

        Log.Debug("[RN_SUPERVISOR_TAB] Запуск пошаговой верификации формы...");

        // 0. Проверяем выбранные локации на стартовом экране
        if (expected.Locations != null && expected.Locations.Any())
        {
            var actualLocations = await GetSelectedLocationsAsync();

            foreach (var expectedLocation in expected.Locations)
            {
                // Проверяем, что ХОТЯ БЫ ОДИН элемент на UI частично содержит имя нашей локации
                bool isFound = actualLocations.Any(act => act.Contains(expectedLocation.Trim(), StringComparison.OrdinalIgnoreCase));

                Assert.That(isFound, Is.True,
                    $"Локация '{expectedLocation}' не найдена среди выбранных на UI. Доступно на UI: {string.Join(", ", actualLocations)}");
            }
        }

        // Шаг 1: Время последнего визита
        await VerifyFormStepAsync(1, async () =>
        {
            // Проверяем заполненное время через InputValueAsync
            var actualTime = await Page.Locator("kendo-timepicker[name='answerTime'] input:visible").InputValueAsync();
            string expectedTimeStr = expected.LastSeen.Time.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(actualTime, Is.EqualTo(expectedTimeStr), "Шаг 1: Время последнего визита не совпадает.");

            // Проверяем детальное описание
            var actualDetails = await Page.GetByPlaceholder("Enter details").InputValueAsync();
            Assert.That(actualDetails, Is.EqualTo(expected.LastSeen.Details), "Шаг 1: Детали не совпадают.");
        });

        // Шаг 2: Детальное описание происшествия
        await VerifyFormStepAsync(2, async () =>
        {
            var actualDetails = await Page.GetByPlaceholder("Enter details").InputValueAsync();
            Assert.That(actualDetails, Is.EqualTo(expected.DescribeExactly.Details), "Шаг 2: Детальное описание не совпадает.");
        });

        // Шаги 3-28: Циклический перебор динамических вопросов (YES/NO + Комментарии)
        for (int i = 0; i < expected.Questions.Count; i++)
        {
            var currentQuestion = expected.Questions[i];
            int stepNumber = i + 3; // По логике заполнения, вопросы начинаются со страницы 3

            await VerifyFormStepAsync(stepNumber, async () =>
            {
                // 1. Проверяем выбранное состояние тоггла (YES или NO)
                string expectedButtonName = currentQuestion.Answer ? "YES" : "NO";
                string actualButtonName = await GetSelectedToggleValueAsync();

                Assert.That(actualButtonName.ToUpper(), Is.EqualTo(expectedButtonName),
                    $"Шаг {stepNumber}: Неверный выбор переключателя.");

                // 2. Если в черновике был сохранен комментарий, сверяем его текстовое поле
                if (!string.IsNullOrEmpty(currentQuestion.Comments))
                {
                    var actualComment = await Page.GetByPlaceholder("Enter comments").InputValueAsync();
                    Assert.That(actualComment, Is.EqualTo(currentQuestion.Comments),
                        $"Шаг {stepNumber}: Комментарий не совпадает.");
                }
            });
        }

        Log.Debug("[RN_SUPERVISOR_TAB] Пошаговая верификация визарда успешно завершена.");
    }

    /// <summary>
    /// Обертка для обработки шага верификации: контролирует пагинацию, выполняет проверки на текущем слайде и жмет "Далее"
    /// </summary>
    private async Task VerifyFormStepAsync(int stepNumber, Func<Task> verifyAction)
    {
        var pagination = Page.Locator("div.pagination").Last;

        // Гарантируем, что визард успел переключиться на нужную страницу (например, "3 of 28")
        await Expect(pagination).ToContainTextAsync($"{stepNumber} of 28", new() { Timeout = 10000 });

        // Выполняем ассерты для текущей страницы
        await verifyAction();

        // Переходим к следующему шагу визарда
        await GoToNextStepAsync();
    }


}
