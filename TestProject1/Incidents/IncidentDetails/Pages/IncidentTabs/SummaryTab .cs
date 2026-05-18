using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;
using static DetailsTab;

public class SummaryTab : BaseIncidentTabs
{
    public record IncidentSummaryInfo(
        bool CarePlanUpdated,
        bool SetAsReportable,
        bool MajorInjury,
        bool SendToLegal,
        string Summary,
        string Plan,
        string Conclusion, // Avoidable, Unavoidable, Undetermined
        bool EvidenceOfAbuse, // No = false, Yes = true
        string EvidenceReason,
        bool ReportedToAgency,
        IReadOnlyList<string> PossibleContributingFactor,
        string DirectorSignature // Имя для поля подписи
    );
    public Dictionary<string, (Func<Task> Action, bool IsRequired)> GetRequiredFieldsMap(IncidentSummaryInfo data)
    {
        var map = new Dictionary<string, (Func<Task> Action, bool IsRequired)>
    {
        // 1. Чекбоксы сверху (Необязательные поля, IsRequired = false)
        { "Care Plan Updated", (() => SetCheckboxAsync("Care Plan Updated", true), false) },
        { "Set as reportable", (() => SetCheckboxAsync("Set as reportable", true), false) },
        { "Major Injury", (() => SetCheckboxAsync("Major Injury", true), false) },
        { "Send to Legal", (() => SetCheckboxAsync("Send to Legal", true), false) },

        // 2. Текстовые редакторы (Rich Text) (Обязательные поля, IsRequired = true)
        { "Summary", (() => FillRichTextFieldAsync("summary", data.Summary), true) },
        { "Plan", (() => FillRichTextFieldAsync("summaryPlan", data.Plan), true) },

        // 3. Радиокнопки заключения (Conclusion) (Обязательное поле)
        { "Conclusion Reached", (() =>
            SelectQuestionRadioAsync("Based upon the collection and review of all attached information, the following conclusion has been reached:", data.Conclusion),
            true)
        },

        // 4. Секция Evidence of abuse (Обязательное поле, обрабатывает сразу логику No/Yes и раскрывающиеся поля)
        { "Evidence", (() => FillEvidenceSectionAsync(true, "Evidence Reason"), true) },

        // 5. This will be reported to the DOH, OHMS... (Обязательное радио-поле со второго скриншота)
        { "This will be reported to the DOH, OHMS, or other agency to intervene?", (() =>
            SelectSummaryRadioOptionAsync("This will be reported to the DOH, OHMS, or other agency to intervene?", "Yes"),
            true)
        },

        // 6. Possible Contributing Factor (Обязательное поле Kendo-мультиселекта)
        { "Possible Contributing Factor", (() => SelectContributingFactorAsync(data.PossibleContributingFactor), true)
        }
    };

        return map;
    }

    public SummaryTab(IPage page) : base(page) { }

    public async Task FillSummaryInfoAsync(IncidentSummaryInfo info)
    {
        // 1. Чекбоксы сверху
        await SetCheckboxAsync("Care Plan Updated", info.CarePlanUpdated);
        await SetCheckboxAsync("Set as reportable", info.SetAsReportable);
        await SetCheckboxAsync("Major Injury", info.MajorInjury);
        await SetCheckboxAsync("Send to Legal", info.SendToLegal);

        // 2. Текстовые редакторы (Rich Text)
        await FillRichTextFieldAsync("summary", info.Summary);
        await FillRichTextFieldAsync("summaryPlan", info.Plan);

        // 3. Радиокнопки заключения (Conclusion)
        await SelectQuestionRadioAsync("Based upon the collection and review of all attached information, the following conclusion has been reached:", info.Conclusion);

        // 4. Evidence of abuse (Радиокнопки No/Yes)
        await FillEvidenceSectionAsync(info.EvidenceOfAbuse, info.EvidenceReason);

        // 5. Поле причины
        //await GetFieldByLabel("Evidence Reason").FillAsync(info.EvidenceReason);

        // 3. This will be reported to the DOH, OHMS... (Radio)
        await SelectSummaryRadioOptionAsync("This will be reported to the DOH, OHMS, or other agency to intervene?",
            info.ReportedToAgency ? "Yes" : "No");

        // 4. Possible Contributing Factor (Dropdown/Select)
        // Предполагаю, что GetFieldByLabel в вашем базовом классе умеет работать с Select
        await SelectContributingFactorAsync(info.PossibleContributingFactor);

        // 5. Director of Nursing or Designee (Signature/Text field)
        //await GetFieldByLabel("Director of Nursing or Designee").FillAsync(info.DirectorSignature);
    }

    // Вспомогательный метод для чекбоксов (если его нет в BaseIncidentTabs)
    private async Task SetCheckboxAsync(string label, bool state)
    {
        var fieldContainer = Page.Locator("cad-label-value-field")
            .Filter(new() { HasText = label });

        var checkbox = fieldContainer.Locator("mat-checkbox");

        // Проверяем текущее состояние через атрибут или класс, 
        // чтобы не кликнуть лишний раз, если состояние уже нужное
        var isChecked = await checkbox.Locator("input").IsCheckedAsync();

        if (isChecked != state)
        {
            await checkbox.ClickAsync();
        }
    }

    public async Task FillEvidenceSectionAsync(Boolean choice, string reasonOrText)
    {
        // 1. Выбираем радиобаттон (используем логику из предыдущего шага)
        await SelectSummaryRadioOptionAsync("There is probable evidence of abuse, neglect or mistreatment:\r\n",
            choice ? "Yes" : " No ");

        if (choice == false)
        {
            // 2. Если выбрали 'No' — работаем с выпадающим списком (mat-select)
            // Ищем селект рядом с текстом "Choose reason"
            var select = Page.Locator("mat-select").Filter(new() { HasText = "Choose reason" });
            await select.ClickAsync();

            // Кликаем по опции в выпадающем списке (они обычно рендерятся в отдельном контейнере в конце DOM)
            await Page.Locator("mat-option").GetByText(reasonOrText, new() { Exact = true }).ClickAsync();
        }
        else if (choice == true)
        {
            // 3. Если выбрали 'Yes' — заполняем iFrame через ваш метод
            // Название поля для Evidence Reason (проверьте в коде, скорее всего 'evidenceReason')
            await FillRichTextFieldAsync("evidenceReason", reasonOrText);
        }
    }

    public async Task SelectSummaryRadioOptionAsync(string sectionLabel, string optionValue)
    {
        // 1. Находим контейнер вопроса по тексту заголовка. 
        // На скриншоте это div, содержащий class="question-field" или просто текст.
        var questionContainer = Page.Locator("div, mat-radio-group")
            .Filter(new() { HasText = sectionLabel.Trim() })
            .Last; // Берем последний, если есть вложенность

        // 2. Внутри контейнера ищем радиокнопку по её названию (тексту "No" или "Yes")
        // GetByRole найдет input внутри mat-radio-button, а Playwright сам кликнет куда нужно.
        await questionContainer.GetByRole(AriaRole.Radio, new() { Name = optionValue.Trim() })
            .ClickAsync();
    }

    public async Task SelectContributingFactorAsync(IReadOnlyList<string> factors)
    {
        if (factors == null || !factors.Any()) return;

        // 1. Кликаем по самому мультиселекту. 
        // На скриншоте видно, что заголовок "Possible Contributing Factor" стоит отдельно.
        // Ищем поле, которое находится ПОСЛЕ этого текста.
        var multiSelect = Page.Locator("label:has-text('Possible Contributing Factor') + * kendo-multiselect, kendo-multiselect").First;
        await multiSelect.ClickAsync();

        // 2. Ждем именно выпадающий список (kendo-popup)
        var popup = Page.Locator("kendo-popup").First;
        await popup.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        foreach (var factor in factors)
        {
            // Используем GetByText с частичным совпадением, так как в DOM 
            // вокруг текста могут быть переносы строк и пробелы.
            var option = popup.Locator("li.k-list-item")
                .GetByText(factor.Trim(), new() { Exact = false });

            if (await option.CountAsync() > 0)
            {
                await option.First.ClickAsync();
                // Небольшая задержка, чтобы Kendo успел обработать выбор перед следующим кликом
                await Page.WaitForTimeoutAsync(200);
            }
            else
            {
                throw new Exception($"Option '{factor}' not found in the list.");
            }
        }

        // 3. Закрываем список
        await Page.Keyboard.PressAsync("Escape");
    }

    public async Task SignAndConfirmIncident()
    {
        // 1. Нажимаем кнопку "Sign Here"
        // Используем GetByRole, так как в DOM виден span с текстом "Sign Here" внутри кнопки
        var signButton = Page.GetByRole(AriaRole.Button, new() { Name = "Sign Here" });
        await signButton.ClickAsync();

        // 2. Ждем появления попапа и кнопки "Confirm"
        // Обычно попапы в Material Design — это модальные окна. 
        // Находим кнопку Confirm, которая стала видимой.
        var confirmButton = Page.GetByRole(AriaRole.Button, new() { Name = "Confirm", Exact = true })
                                .Filter(new() { Visible = true });

        // Ждем, чтобы кнопка была готова к клику (стабильна)
        await confirmButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // 3. Кликаем "Confirm"
        await confirmButton.ClickAsync();

        // 4. Опционально: ждем, пока попап исчезнет
        await Assertions.Expect(confirmButton).ToHaveCountAsync(0);
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

    public async Task<string> DownloadSummaryReportAsync()
    {
        // 1. Извлекаем GUID из текущего URL
        // Регулярное выражение ищет стандартный формат GUID (8-4-4-4-12 символов)
        var match = Regex.Match(Page.Url, @"([a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12})");
        string guid = match.Success ? match.Value : "unknown-id";

        string attachmentName = $"{guid}_Summary";

        // 2. Начинаем ожидание скачивания
        var downloadTask = Page.WaitForDownloadAsync(new() { Timeout = 180000 }); // Ждем 60 секунд

        // 3. Кликаем по кнопке "Yes" в попапе
        var yesButton = Page.GetByRole(AriaRole.Button, new() { Name = "Yes", Exact = true });
        await yesButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await yesButton.ClickAsync();

        // 4. Ждем завершения загрузки
        var download = await downloadTask;

        // 5. Формируем путь (Temp + GUID_Summary + расширение из браузера)
        var extension = Path.GetExtension(download.SuggestedFilename);
        var tempPath = Path.Combine(Path.GetTempPath(), $"{attachmentName}{extension}");

        // 6. Сохраняем и прикрепляем к NUnit
        await download.SaveAsAsync(tempPath);
        TestContext.AddTestAttachment(tempPath, attachmentName);

        Log.Information($"File is downloaded and attached to test: {tempPath}");
        return tempPath;
    }
    // Чтение обычного чекбокса mat-checkbox
    public async Task<bool> IsCheckboxCheckedAsync(string label)
    {
        var container = Page.Locator("cad-label-value-field").Filter(new() { HasText = label });
        return await container.Locator("mat-checkbox input").IsCheckedAsync();
    }

    // Чтение содержимого Rich Text редактора (обычно текст лежит внутри iframe или блока с contenteditable)
    public async Task<string> GetRichTextValueAsync(string fieldName)
    {
        // 1. Находим сам kendo-editor по имени (например, name='summary')
        var kendoEditor = Page.Locator($"kendo-editor[name='{fieldName}'], [id='{fieldName}']").First;
        await kendoEditor.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        // Даем Angular микропаузу для инициализации редактора
        await Page.WaitForTimeoutAsync(300);

        // СИСТЕМНЫЙ СПОСОБ 1: Чтение через iframe (некоторые версии Kendo создают скрытый iframe)
        var iframe = kendoEditor.Locator("iframe");
        if (await iframe.CountAsync() > 0)
        {
            try
            {
                var frameBody = kendoEditor.FrameLocator("iframe").Locator("body");
                string iframeText = await frameBody.InnerTextAsync();
                if (!string.IsNullOrEmpty(iframeText.Trim())) return iframeText.Trim();
            }
            catch { /* Игнорируем ошибку фрейма, идем к следующему способу */ }
        }

        // СИСТЕМНЫЙ СПОСОБ 2: Чтение через ProseMirror/contenteditable (как на прошлом шаге)
        var editableDiv = kendoEditor.Locator(".k-editor-content [contenteditable='true'], .k-editor-content, .k-content");
        if (await editableDiv.CountAsync() > 0)
        {
            string divText = await editableDiv.First.EvaluateAsync<string>("el => el.textContent || el.innerText") ?? "";
            if (!string.IsNullOrEmpty(divText.Trim())) return divText.Trim();
        }

        // СИСТЕМНЫЙ СПОСОБ 3: Железобетонное чтение по всему контейнеру редактора
        // Если Kendo размазал текст по параграфам, InnerTextAsync от корня kendo-editor 
        // соберет все строки, а мы просто отрежем технические кнопки панелей (B, I, U)
        string fullEditorText = await kendoEditor.InnerTextAsync();

        // Отрезаем верхнюю панель инструментов Kendo (она обычно содержит слова Format, Font и т.д.)
        string cleanText = fullEditorText
            .Replace("Format", "")
            .Replace("Font", "")
            .Replace("Summary", "")
            .Replace("Plan", "")
            .Trim();

        return cleanText;
    }

    public async Task<string> GetSelectedRadioTextAsync(string questionLabel)
    {
        // Универсальный маппинг: связываем человеческий заголовок вопроса с техническим name в DOM
        string groupName = "incidentSummaryConclusion"; // По умолчанию для первого вопроса заключения

        if (questionLabel.Contains("probable evidence of abuse"))
        {
            groupName = "hasEvidenceOfAbuse"; // Подставьте реальный name из DOM для второго вопроса, если он отличается
        }
        else if (questionLabel.Contains("reported to the DOH"))
        {
            groupName = "reportedToAgency"; // Подставьте реальный name из DOM для третьего вопроса
        }

        // 1. Ищем группу радиобаттонов по ее точному атрибуту name
        var radioGroup = Page.Locator($"mat-radio-group[name='{groupName}'], mat-radio-group")
            .Filter(new() { Has = Page.Locator("mat-radio-button, mat-mdc-radio-button") });

        // Если по name не нашли, делаем фолбэк-поиск по тексту вопроса в родительском блоке question-field
        if (await radioGroup.CountAsync() == 0 || await radioGroup.CountAsync() > 1)
        {
            var questionField = Page.Locator("div.question-field").Filter(new() { HasText = questionLabel });
            radioGroup = questionField.Locator("mat-radio-group");
        }

        // 2. ИСПРАВЛЕНИЕ: Находим активный радиобаттон по Angular MDC CSS-классу выбранности
        var checkedRadioButton = radioGroup.Locator("mat-radio-button.mat-mdc-radio-checked, mat-radio-button.mat-radio-checked, mat-radio-button[aria-checked='true']").First;

        // Ждем, пока элемент отрендерится
        await checkedRadioButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        // 3. Возвращаем текст активной опции (например, "Unavoidable")
        string selectedText = await checkedRadioButton.InnerTextAsync();

        return selectedText.Trim();
    }

    // Чтение выбранных элементов в мультиселекте Kendo/Angular
    public async Task<IReadOnlyList<string>> GetSelectedContributingFactorsAsync()
    {
        // На скриншотах Kendo MultiSelect обычно рендерит выбранные теги как .k-chip или .mat-chip
        var chips = Page.Locator("kendo-multiselect .k-chip-label, mat-chip-list mat-chip");
        return await chips.AllInnerTextsAsync();
    }
    public async Task VerifyDataFieldsAsync(IncidentSummaryInfo expected)
    {
        // 1. Чекбоксы сверху
        Assert.That(await IsCheckboxCheckedAsync("Care Plan Updated"), Is.EqualTo(expected.CarePlanUpdated));
        Assert.That(await IsCheckboxCheckedAsync("Set as reportable"), Is.EqualTo(expected.SetAsReportable));
        Assert.That(await IsCheckboxCheckedAsync("Major Injury"), Is.EqualTo(expected.MajorInjury));
        Assert.That(await IsCheckboxCheckedAsync("Send to Legal"), Is.EqualTo(expected.SendToLegal));

        // 2. Текстовые редакторы (Rich Text)
        if (!string.IsNullOrEmpty(expected.Summary))
        {
            var actualSummary = await GetRichTextValueAsync("summary");
            Assert.That(actualSummary, Does.Contain(expected.Summary));
        }

        if (!string.IsNullOrEmpty(expected.Plan))
        {
            var actualPlan = await GetRichTextValueAsync("summaryPlan");
            Assert.That(actualPlan, Does.Contain(expected.Plan));
        }

        // 3. Радиокнопки заключения (Conclusion)
        if (!string.IsNullOrEmpty(expected.Conclusion))
        {
            var actualConclusion = await GetSelectedRadioTextAsync("Based upon the collection and review of all attached information, the following conclusion has been reached:");
            Assert.That(actualConclusion, Does.Contain(expected.Conclusion));
        }

        // 4. Evidence of abuse (Радиокнопки + Причина)
        // 3. Проверяем состояние радиокнопки Evidence of abuse
        string expectedAbuseRadio = expected.EvidenceOfAbuse ? "Yes" : "No";
        var actualAbuseRadio = await GetSelectedRadioTextAsync("There is probable evidence of abuse, neglect or mistreatment:");
        Assert.That(actualAbuseRadio, Does.Contain(expectedAbuseRadio), "Состояние радиокнопки Evidence of abuse не совпадает.");

        // 4. Проверяем текстовый редактор Evidence Reason через ТОТ ЖЕ универсальный метод
        if (!string.IsNullOrEmpty(expected.EvidenceReason))
        {
            Log.Debug("[SUMMARY_TAB] Верификация Evidence Reason через универсальный метод...");

            // Передаем имя "evidenceReason", метод сам поймет как его прочитать и дождется текста
            var actualReasonRichText = await GetRichTextValueAsync("evidenceReason");

            // Очищаем от пробелов для надежности сравнения
            string cleanActual = actualReasonRichText.Replace("\r", "").Replace("\n", "").Trim();
            string cleanExpected = expected.EvidenceReason.Replace("\r", "").Replace("\n", "").Trim();

            Assert.That(cleanActual, Does.Contain(cleanExpected.Substring(0, 30)),
                "Текст в поле Evidence Reason не совпадает с ожидаемым черновиком.");
        }

        // 5. This will be reported to the DOH... (Radio)
        string expectedReportedRadio = expected.ReportedToAgency ? "Yes" : "No";
        var actualReportedRadio = await GetSelectedRadioTextAsync("This will be reported to the DOH, OHMS, or other agency to intervene?");
        Assert.That(actualReportedRadio, Does.Contain(expectedReportedRadio));

        // 6. Possible Contributing Factor (Мультиселект)
        if (expected.PossibleContributingFactor != null && expected.PossibleContributingFactor.Count > 0)
        {
            var actualFactors = await GetSelectedContributingFactorsAsync();
            foreach (var expectedFactor in expected.PossibleContributingFactor)
            {
                Assert.That(actualFactors, Contains.Item(expectedFactor),
                    $"Фактор '{expectedFactor}' не найден среди выбранных элементов на UI.");
            }
        }
    }
}