using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Playwright;
using Serilog;

public class SummaryTab : BaseIncidentTabs
{
    public record SummaryInfo(
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

    public SummaryTab(IPage page) : base(page) { }

    public async Task FillSummaryInfoAsync(SummaryInfo info)
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
}