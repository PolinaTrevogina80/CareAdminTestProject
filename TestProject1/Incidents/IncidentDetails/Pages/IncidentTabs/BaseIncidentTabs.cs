using Microsoft.Playwright;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Internal.Execution;
using Serilog;
using System.Globalization;
using TestProject1.Common;
using static System.Net.Mime.MediaTypeNames;

namespace CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs
{
    // Сделаем класс abstract, так как сам по себе он не является страницей
    public abstract class BaseIncidentTabs
    {
        protected readonly IPage Page;

        // Конструктор, который принимает страницу из теста или родительского PageObject
        protected BaseIncidentTabs(IPage page)
        {
            Page = page;
        }

        protected ILocator GetFieldByLabel(string labelText)
        {
            return Page.Locator("cad-label-value-field")
                        .Filter(new() { HasText = labelText })
                        .Locator("input, textarea, mat-select");
        }

        protected ILocator GetButtonByText(string buttonText)
        {
            // Ищет именно кнопку с указанным текстом (без учета регистра)
            return Page.GetByRole(AriaRole.Button, new() { Name = buttonText });
        }

        [Obsolete]
        protected async Task FillRichTextFieldAsync(string fieldName, string text)
        {
            // 1. Находим сам кастомный элемент kendo-editor. 
            // На скриншоте видно атрибут name="summary" или name="summaryPlan"
            var editor = Page.Locator($"kendo-editor[name='{fieldName}']");

            // 2. Инициализируем FrameLocator. 
            // Важно: FrameLocator сам по себе ленивый, он начнет поиск только при обращении к внутренним элементам.
            var frame = editor.FrameLocator("iframe");

            // 3. Находим body внутри фрейма. 
            // В Kendo Editor именно body имеет атрибут contenteditable="true"
            var editableArea = frame.Locator("body");

            // 4. Ожидаем, что фрейм не просто прикреплен, а готов к работе.
            // Для iframe лучше использовать State.Visible или дождаться конкретного атрибута.
            await editableArea.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

            // 5. Фокусируемся на поле. ClickAsync часто надежнее, чем просто Focus.
            await editableArea.ClickAsync();

            // 6. Очистка и ввод. 
            // Вместо Keyboard.Type (который имитирует нажатия на уровне всей страницы), 
            // лучше использовать FillAsync для самого элемента, если Kendo это позволяет, 
            // НО для RichText Keyboard действительно надежнее.

            // Очистка через горячие клавиши (как у вас)
            //await _page.Keyboard.DownAsync("Control");
            //await _page.Keyboard.PressAsync("a");
            //await _page.Keyboard.UpAsync("Control");
            //await _page.Keyboard.PressAsync("Backspace");

            // Ввод текста
            await editableArea.PressSequentiallyAsync(text, new() { Delay = 50 });

            Log.Debug($"Field '{fieldName}' filled.");
        }

        protected ILocator GetFieldIcon(string labelText)
        {
            // Находим контейнер по лейблу, затем ищем кнопку открытия календаря Kendo
            return Page.Locator("kendo-formfield")
                        .Filter(new() { HasText = labelText })
                        .Locator("button.k-input-button");
        }

        protected ILocator GetFieldIconByName(string nameAttribute)
        {
            // Ищем kendo-datepicker с нужным именем и внутри него кнопку календаря
            return Page.Locator($"kendo-datepicker[name='{nameAttribute}'] button.k-input-button");
        }

        protected async Task SelectMatOptionByLabel(string labelText, int index)
        {
            var field = GetFieldByLabel(labelText);
            await field.ClickAsync();

            // Ждем появления контейнера с опциями
            var options = Page.Locator("mat-option, .k-item");
            await options.First.WaitForAsync();

            // Кликаем по индексу
            await options.Nth(index).ClickAsync();
        }

        protected async Task ClickControlIcon(string nameAttribute)
        {
            // 1. Кликаем по иконке
            var calendarIcon = GetFieldIconByName(nameAttribute);

            // Скроллим к иконке перед кликом на всякий случай
            await calendarIcon.ScrollIntoViewIfNeededAsync();
            await calendarIcon.ClickAsync();

            // Оставляем задержку на анимацию открытия
            await Task.Delay(1000);

            // 2. Ждем появления попапа
        }

        public async Task SelectDropdownOptionAsync(string labelText, string optionText, int indexInList = 0)
        {
            // Находим i-й дропдаун с таким лейблом
            var dropdown = GetFieldByLabel(labelText).Nth(indexInList);

            await dropdown.ClickAsync();

            // Оверлей с опциями обычно один на всю страницу в конкретный момент времени
            var option = Page.Locator("mat-option", new() { HasText = optionText }).First;

            await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await option.ClickAsync();
        }

        // Метод для выбора по индексу (например, первый элемент)
        public async Task SelectDropdownOptionAsync(string labelText, int index)
        {
            var dropdown = GetFieldByLabel(labelText).First;
            await dropdown.ClickAsync();

            // Ищем опцию только внутри активного выпадающего списка
            var option = Page.Locator(".cdk-overlay-container mat-option:visible").Nth(index - 1);

            await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await option.ClickAsync();

            // Ждем, пока список закроется, чтобы не мешать следующим шагам
            await Page.Locator(".cdk-overlay-container").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        public async Task SelectTimeInPickerAsync(string nameAttribute, TimeOnly time)
        {
            var pickerContainer = Page.Locator($"kendo-timepicker[name='{nameAttribute}']");
            await pickerContainer.Locator("button.k-input-button").ClickAsync();

            var popup = Page.Locator("kendo-popup:visible, .k-animation-container:visible").First;
            await popup.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            await Task.Delay(200);

            // Подготавливаем оба варианта для часов: "1" и "01"
            var hourSingle = time.ToString("%h", CultureInfo.InvariantCulture);
            var hourDouble = time.ToString("hh", CultureInfo.InvariantCulture);

            var minute = time.ToString("mm", CultureInfo.InvariantCulture);
            var amPm = time.ToString("tt", CultureInfo.InvariantCulture).ToUpper();

            // Передаем массив или регулярку в метод выбора часов
            await SelectKendoColumnValue(popup, 0, hourSingle, hourDouble);
            await SelectKendoColumnValue(popup, 1, minute);
            await SelectKendoColumnValue(popup, 2, amPm);

            await popup.GetByRole(AriaRole.Button, new() { Name = "Set" }).DispatchEventAsync("click");
            await popup.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        private async Task SelectKendoColumnValue(ILocator popup, int columnIndex, params string[] values)
        {
            var column = popup.Locator(".k-time-list").Nth(columnIndex);

            // Создаем паттерн для поиска точного совпадения любого из значений
            var pattern = $"^({string.Join("|", values)})$";

            // В C# Playwright GetByText принимает Regex напрямую
            var item = column.Locator(".k-item")
                .GetByText(new Regex(pattern))
                .First;

            await item.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await item.ScrollIntoViewIfNeededAsync();
            await item.DispatchEventAsync("click");

            Log.Debug($"[SUCCESS] Значение из списка [{string.Join(", ", values)}] успешно выбрано");

            await Task.Delay(100);
        }



        public async Task SelectRadioOptionAsync(string sectionLabel, string optionValue)
        {
            if (string.IsNullOrEmpty(optionValue)) return;

            // 1. Находим заголовок панели
            var header = Page.Locator(".state-panel__header")
                .GetByText(sectionLabel, new() { Exact = true });

            // 2. Находим саму панель (родитель), чтобы внутри неё искать контент
            var panel = Page.Locator(".state-panel")
                .Filter(new() { Has = Page.Locator(".state-panel__header").GetByText(sectionLabel, new() { Exact = true }) });

            // 3. Внутри этой панели ищем радиокнопку по тексту её label
            var radioButton = panel.Locator("mat-radio-button")
                .GetByText(optionValue, new());
            Log.Debug($"RadioButton {sectionLabel} is set as {optionValue}");
            // Кликаем именно по элементу mat-radio-button
            await radioButton.ClickAsync();
        }

        // Сброс радиокнопок в конкретной секции по заголовку



        public async Task SelectQuestionRadioAsync(string questionText, string optionValue)
        {
            if (string.IsNullOrEmpty(optionValue)) return;

            // 1. Ищем контейнер вопроса, который содержит нужный текст
            // На скриншоте это div с классом question-field
            var questionContainer = Page.Locator(".question-field")
                .Filter(new() { HasText = questionText });

            // 2. Внутри этого контейнера ищем радиокнопку по тексту (например, "Unavoidable")
            // Используем .Locator("mat-radio-button"), так как GetByText на mat-radio-button 
            // отлично находит внутренний label
            var radioButton = questionContainer.Locator("mat-radio-button")
                .GetByText(optionValue, new() { Exact = true });

            // 3. Скроллим к элементу (на всякий случай) и кликаем
            await radioButton.ScrollIntoViewIfNeededAsync();
            await radioButton.ClickAsync();

            Log.Debug($"Radio '{optionValue}' selected for question: {questionText.Substring(0, 20)}...");
        }

        public ILocator GetRedDotLocator(string fieldName)
        {
            int requestedIndex = 0;
            string realLabel = fieldName;

            // 1. Определяем желаемый индекс
            if (fieldName.Contains("(Relative)"))
            {
                requestedIndex = 0;
                realLabel = fieldName.Replace(" (Relative)", "");
            }
            else if (fieldName.Contains("(MD)"))
            {
                requestedIndex = 1;
                realLabel = fieldName.Replace(" (MD)", "");
            }

            var escapedLabel = Regex.Escape(realLabel).Replace("'", ".*");
            var pattern = new Regex(escapedLabel);

            // 2. Сначала находим все подходящие контейнеры
            var allFields = Page.Locator("cad-label-value-field")
                                .Filter(new() { HasTextRegex = pattern });

            // 3. Возвращаем локатор для конкретной точки
            // Используем .First если просим 0, и .Last если просим 1, но элементов мало
            // Это самый безопасный способ для "плавающего" количества элементов
            return requestedIndex == 0
                ? allFields.First.Locator("span.completeness-indicator")
                : allFields.CountAsync().Result > 1
                    ? allFields.Nth(1).Locator("span.completeness-indicator")
                    : allFields.First.Locator("span.completeness-indicator");
        }

        // Теперь старый метод можно сократить, чтобы не дублировать код
        public async Task<bool> IsFieldMarkedRequiredAsync(string fieldName)
        {
            return await GetRedDotLocator(fieldName).IsVisibleAsync();
        }

        // Проверка точки на самом названии вкладки в таб-баре (Kendo TabStrip)
        public async Task<bool> IsTabMarkedIncompleteAsync(string tabName)
        {

            // В Kendo табы — это элементы li с ролью tab
            return await Page.Locator("li[role='tab']")
                .Filter(new() { HasText = tabName })
                .Locator("span.completeness-indicator")
                .IsVisibleAsync();
        }
    }
}