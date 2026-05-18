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
        public enum RoleToSign
        {
            DNS, // Director of Nursing or Designee
            MD,  // Medical Director
            Administrator
        }

        // Конструктор, который принимает страницу из теста или родительского PageObject
        public BaseIncidentTabs(IPage page)
        {
            Page = page;
        }


        public ILocator GetFieldByLabel(string labelText)
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
        public async Task FillRichTextFieldAsync(string fieldName, string text)
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

            // Ввод текста
            await editableArea.PressSequentiallyAsync(text, new() { Delay = 50 });

            Log.Debug($"Field '{fieldName}' filled.");
        }
        public async Task<string> GetFieldValueByLabelAsync(string label)
        {
            // Считывает значение из обычного текстового инпута (Room, Bed, SBARSummary)
            return await GetFieldByLabel(label).InputValueAsync();
        }

        public async Task<string> GetDropdownValueAsync(string label)
        {
            // Считывает выбранный текст или value из дропдауна Angular (Unit, Location, Type)
            // Метод зависит от структуры вашего селекта, например:
            return await Page.Locator($"[data-label='{label}'] .selected-value").InnerTextAsync();
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

        public async Task SignAsRoleAsync(RoleToSign role)
        {
            // Маппинг enum на реальный текст, который отображается в блоке подписи на форме
            string roleText = role switch
            {
                RoleToSign.DNS => "Director of Nursing",
                RoleToSign.MD => "Medical Director",
                RoleToSign.Administrator => "Administrator",
                _ => throw new ArgumentOutOfRangeException(nameof(role), $"Неизвестная роль: {role}")
            };

            Log.Debug($"[Подпись] Начинаем процесс для роли: {roleText}...");

            // Находим конкретный блок cad-incident-sign по тексту должности
            var signatureContainer = Page.Locator("cad-incident-sign")
                .Filter(new() { HasText = roleText })
                .First;

            var signButton = signatureContainer.Locator("button:has-text('Sign Here')");

            // Скроллим и кликаем на "Sign Here"
            await signButton.ScrollIntoViewIfNeededAsync();
            await signButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await signButton.ClickAsync();
            Log.Debug($"Кнопка 'Sign Here' для {roleText} успешно нажата.");

            Log.Debug("Ожидаем появление модального окна Confirm Signature...");
            var confirmDialog = Page.Locator("cad-incident-confirm-sign-dialog");
            await confirmDialog.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // Находим и нажимаем кнопку Confirm внутри модального окна
            var confirmButton = confirmDialog.Locator("button:has-text('Confirm')");
            await confirmButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
            await confirmButton.ClickAsync();
            Log.Debug($"Кнопка 'Confirm' для {roleText} успешно нажата.");

            // Ждем, пока модальное окно полностью исчезнет
            await confirmDialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
            Log.Debug($"Процесс подписи для {roleText} успешно завершен.");
        }

        public async Task VerifySignatureImageVisible()
        {
            // 1. Находим контейнер подписи (по классу со скриншота: signature-box)
            var signatureImage = Page.Locator(".signature-box img");

            // 2. Проверяем, что картинка не просто есть в DOM, но и видна пользователю
            await Assertions.Expect(signatureImage).ToBeVisibleAsync(new() { Timeout = 10000 });

            // 3. Дополнительная проверка: убедимся, что у картинки есть src (она загрузилась)
            var src = await signatureImage.GetAttributeAsync("src");
            if (string.IsNullOrEmpty(src))
            {
                throw new Exception("Подпись должна быть, но ссылка на изображение (src) пуста.");
            }

            Console.WriteLine("Подпись успешно сохранена и отображается как изображение.");
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

        public async Task<ILocator> GetRedDotLocatorAsync(string fieldName)
        {
            // 1. Проверяем точное совпадение для Summary или Plan
            bool isSummaryField = fieldName.Equals("Summary", StringComparison.OrdinalIgnoreCase) ||
                                  fieldName.Equals("Enter summary", StringComparison.OrdinalIgnoreCase);

            bool isPlanField = fieldName.Equals("Plan", StringComparison.OrdinalIgnoreCase) ||
                               fieldName.Equals("Enter plan", StringComparison.OrdinalIgnoreCase);

            if (isSummaryField || isPlanField)
            {
                Log.Debug($"[RedDot Diagnostic] Зашли в ветку RichText для поля: '{fieldName}'");

                var container = Page.Locator("cad-incident-edit-summary");
                var allWrappers = container.Locator("div.editor-wrapper");
                int wrappersCount = await allWrappers.CountAsync();
                Log.Debug($"[RedDot Diagnostic] Всего найдено 'div.editor-wrapper': {wrappersCount}");

                // Выводим реальный текст всех найденных оберток
                for (int i = 0; i < wrappersCount; i++)
                {
                    var currentText = await allWrappers.Nth(i).InnerTextAsync();
                    Log.Debug($"[RedDot Diagnostic] Контейнер #{i} InnerText: '{currentText?.Replace("\n", " ")}'");
                }

                string searchKeyword = isSummaryField ? "Summary" : "Plan";

                // Используем гибкое регулярное выражение для поиска ключевого слова в тексте контейнера
                var richTextPattern = new Regex($@"\b{searchKeyword}\b", RegexOptions.IgnoreCase);
                var targetWrapper = allWrappers.Filter(new() { HasTextRegex = richTextPattern });

                int matchedCount = await targetWrapper.CountAsync();
                Log.Debug($"[RedDot Diagnostic] После фильтрации по regex '{searchKeyword}' осталось матчей: {matchedCount}");

                var indicator = targetWrapper.First.Locator("span.completeness-indicator");

                bool exists = await indicator.CountAsync() > 0;
                bool isVisible = exists && await indicator.IsVisibleAsync();
                Log.Debug($"[RedDot Diagnostic] Итог для '{fieldName}': присутствует в DOM = {exists}, виден = {isVisible}");

                return indicator;
            }

            // 2. Специальная обработка для полей-вопросов и сложных секций (Conclusion, Evidence)
            if (fieldName.Equals("Conclusion Reached", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Equals("Evidence", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Equals("Evidence Reason", StringComparison.OrdinalIgnoreCase) ||
                fieldName.StartsWith("This will be reported", StringComparison.OrdinalIgnoreCase)) // Перехватываем длинный вопрос DOH
            {
                // Если это любой из вопросов с радиокнопками (Conclusion, Evidence или DOH)
                if (!fieldName.Equals("Evidence Reason", StringComparison.OrdinalIgnoreCase))
                {
                    string keyword;

                    if (fieldName.Equals("Conclusion Reached", StringComparison.OrdinalIgnoreCase))
                        keyword = "conclusion";
                    else if (fieldName.Equals("Evidence", StringComparison.OrdinalIgnoreCase))
                        keyword = "evidence of abuse";
                    else
                        keyword = "reported to the DOH"; // Ключевое слово для поиска блока DOH/OHMS

                    var questionPattern = new Regex(keyword, RegexOptions.IgnoreCase);

                    return Page.Locator("cad-incident-edit-summary")
                               .Locator("div.question-field")
                               .Filter(new() { HasTextRegex = questionPattern })
                               .Locator("span.completeness-indicator");
                }

                // Если это внутренний RichText редактор для Evidence Reason ("Explain reasoning...")
                if (fieldName.Equals("Evidence Reason", StringComparison.OrdinalIgnoreCase))
                {
                    var reasonPattern = new Regex("reasoning", RegexOptions.IgnoreCase);

                    return Page.Locator("cad-incident-edit-summary")
                               .Locator("div.editor-wrapper")
                               .Filter(new() { HasTextRegex = reasonPattern })
                               .Locator("span.completeness-indicator");
                }
            }


            // 3. СТАНДАРТНАЯ ЛОГИКА ДЛЯ ВСЕХ ОСТАЛЬНЫХ ПОЛЕЙ (БЕЗ ИЗМЕНЕНИЙ)
            int requestedIndex = 0;
            string realLabel = fieldName;

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
            var patternStandard = new Regex(escapedLabel);

            var allFields = Page.Locator("cad-label-value-field")
                                .Filter(new() { HasTextRegex = patternStandard });

            int count = await allFields.CountAsync();

            return requestedIndex == 0
                ? allFields.First.Locator("span.completeness-indicator")
                : count > 1
                    ? allFields.Nth(1).Locator("span.completeness-indicator")
                    : allFields.First.Locator("span.completeness-indicator");
        }

        public async Task<bool> IsFieldMarkedRequiredAsync(string fieldName)
        {
            var locator = await GetRedDotLocatorAsync(fieldName);
            return await locator.IsVisibleAsync();
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

        public async Task<bool> IsCheckboxCheckedAsync(string label)
        {
            // 1. Ищем контейнер поля, который содержит спан с точным текстом лейбла
            var checkboxFieldContainer = Page.Locator("div.checkbox-field")
                .Filter(new() { HasTextRegex = new Regex($"^{label}$", RegexOptions.IgnoreCase) })
                .First;

            // 2. Если вдруг на других вкладках структура отличается, делаем запасной фолбэк
            if (await checkboxFieldContainer.CountAsync() == 0)
            {
                checkboxFieldContainer = Page.Locator("mat-checkbox, cad-label-value-field")
                    .Filter(new() { HasText = label })
                    .First;
            }

            // 3. Находим mat-checkbox и скрытый input строго внутри этого контейнера
            var checkboxInput = checkboxFieldContainer.Locator("mat-checkbox input");

            return await checkboxInput.IsCheckedAsync();
        }

        // Метод возвращает true/false для кастомных тоглов/чекбоксов Assistive Device
        public async Task<bool> IsAssistiveDeviceSetAsync(string deviceLabel, string expectedStatus)
        {
            // Если в данных статус не передан или пустой, значит девайс не проверяем
            if (string.IsNullOrEmpty(expectedStatus))
            {
                return true;
            }

            // 1. Находим контейнер всей строки девайса (например, "Wheelchair")
            var deviceRowContainer = Page.Locator("div.checkbox-field")
                .Filter(new() { HasTextRegex = new Regex($"{deviceLabel}", RegexOptions.IgnoreCase) })
                .First;

            await deviceRowContainer.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // 2. Проверяем, что главный чекбокс взведен (так как они чекаются все)
            var checkboxInput = deviceRowContainer.Locator("mat-checkbox input");
            bool isCheckboxChecked = await checkboxInput.IsCheckedAsync();
            Assert.That(isCheckboxChecked, Is.True, $"Чекбокс для девайса '{deviceLabel}' должен быть выбран.");

            // 3. Находим радиобаттон, текст которого строго совпадает с ожидаемым статусом ("Used" или "Not Used")
            var radioButton = deviceRowContainer.Locator("mat-radio-button, mat-mdc-radio-button")
                .Filter(new() { HasTextRegex = new Regex($"{expectedStatus.Trim()}", RegexOptions.IgnoreCase) })
                .First;

            // 4. Считываем атрибуты его активности
            string classAttribute = await radioButton.GetAttributeAsync("class") ?? "";
            string ariaChecked = await radioButton.GetAttributeAsync("aria-checked") ?? "false";

            bool isRadioSelected = classAttribute.Contains("mat-mdc-radio-checked")
                                  || classAttribute.Contains("mat-radio-checked")
                                  || ariaChecked.Equals("true", StringComparison.OrdinalIgnoreCase);

            return isRadioSelected;
        }
        // Метод проверяет, выбрана ли конкретная радио-кнопка
        public async Task<bool> IsRadioOptionSelectedAsync(string groupName, string optionValue)
        {
            // 1. Находим группу mat-radio-group
            var radioGroup = Page.Locator("mat-radio-group[name='ambulatoryStatus'], mat-radio-group");

            if (await radioGroup.CountAsync() > 1)
            {
                radioGroup = radioGroup.Filter(new() { HasTextRegex = new Regex("Ambulatory", RegexOptions.IgnoreCase) });
            }

            string cleanOptionValue = optionValue.Trim();

            var radioButton = radioGroup.Locator("mat-radio-button, mat-mdc-radio-button")
                .Filter(new() { HasTextRegex = new Regex(cleanOptionValue, RegexOptions.IgnoreCase) })
                .First;

            // Ждем появления элемента на экране
            await radioButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // 3. Считываем атрибуты состояния активности
            string classAttribute = await radioButton.GetAttributeAsync("class") ?? "";
            string ariaChecked = await radioButton.GetAttributeAsync("aria-checked") ?? "false";

            return classAttribute.Contains("mat-mdc-radio-checked")
                   || classAttribute.Contains("mat-radio-checked")
                   || ariaChecked.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}