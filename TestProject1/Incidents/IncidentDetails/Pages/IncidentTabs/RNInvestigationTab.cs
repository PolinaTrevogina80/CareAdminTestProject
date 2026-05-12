using Microsoft.Playwright;
using System.Globalization;
using System.Text.RegularExpressions;
using static Microsoft.Playwright.Assertions;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab.RNSupervisorTabInfo;

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
        // Ждем индикатор страницы (например, "1 of 28")
        var pagination = Page.Locator("div.pagination").Last;

        // Ждем, когда в этом блоке появится нужный текст (например, "1 of")
        await Expect(pagination).ToContainTextAsync($"{stepNumber} of 28", new() { Timeout = 10000 });

        // Выполняем логику заполнения полей
        await fillAction();

        // Переходим к следующему шагу
        await GoToNextStepAsync();
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

        // Ждем завершения анимации перехода слайда в Kendo/Angular
    }

}
