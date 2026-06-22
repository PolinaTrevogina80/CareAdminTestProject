using CareAdminTestProject.Common;
using Microsoft.Playwright;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Log = CareAdminTestProject.Common.TestLog;


namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    internal class IncidentSummaryTests : BaseIncidentTests
    {
        /// <summary>
        /// Проверяет "No" сценарий для секции Evidence: выбор шаблона причины из выпадающего списка
        /// и валидацию исчезновения красной точки (обязательности поля).
        /// </summary>
        [Test]
        public async Task Summary_EvidenceSection_NoScenario_DropdownSelection()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);
            
            await steps.SwitchToTab("Summary");

            var summaryPage = steps.CreatePage.Summary;

            // Включаем режим "No" (probable evidence = false)
            await summaryPage.FillEvidenceSectionAsync(choice: false, reasonOrText: "This was unanticipated event");

            // Проверяем, что красная точка (валидация) уходит после выбора шаблона
            await steps.VerifyRedDotField(steps.CreatePage.Summary, "Evidence Reason", false);

        }

        /// <summary>
        /// Проверяет "Yes" сценарий для секции Evidence: скрытие дропдауна, ручной ввод текста в Rich Text
        /// и валидацию исчезновения красной точки.
        /// </summary>
        [Test]
        public async Task Summary_EvidenceSection_YesScenario_ManualTextInput()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            await steps.SwitchToTab("Summary");

            var summaryPage = steps.CreatePage.Summary;

            // Включаем режим "Yes" (probable evidence = true) и пишем кастомный текст в редактор
            await summaryPage.FillEvidenceSectionAsync(choice: true, reasonOrText: "Bruising observed on the left forearm area.");

            // Проверяем, что красная точка исчезает
            await steps.VerifyRedDotField(steps.CreatePage.Summary, "Evidence Reason", false);
        }

        /// <summary>
        /// Проверяет логику сброса (Reset on Toggle): при переключении тумблера Yes -> No 
        /// введенный ранее текст должен очищаться, а красная точка — появляться снова.
        /// </summary>
        [Test]
        public async Task Summary_EvidenceSection_Toggle_ResetsPreviousInput()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            await steps.SwitchToTab("Summary");

            var summaryPage = steps.CreatePage.Summary;

            // Шаг 1: Инициализируем режим "Yes" и вводим кастомный текст
            await summaryPage.FillEvidenceSectionAsync(choice: true, reasonOrText: "Temporary manual text input");

            // Шаг 2: Переключаем тумблер обратно на "No"
            await summaryPage.SelectSummaryRadioOptionAsync("There is probable evidence of abuse", "No");

            // Проверяем, что текст стерся, а поле снова требует заполнения (появилась красная точка)
            await summaryPage.VerifyRichTextFieldIsEmptyAsync("evidenceReason");
            await steps.VerifyRedDotField(steps.CreatePage.Summary, "Evidence Reason", true);

            // Шаг 3: Заполняем поле режим "No" и выбирает текст из списка
            await summaryPage.FillEvidenceSectionAsync(choice: false, reasonOrText: "Event was unavoidable as safety interventions are all in place or resident was in a supervised area");
            await steps.VerifyRedDotField(steps.CreatePage.Summary, "Evidence Reason", false);

            // Шаг 4: Переключается опять на режим "Yes" и вводим кастомный текст, проверяем что поле пустое
            await summaryPage.SelectSummaryRadioOptionAsync("There is probable evidence of abuse", "Yes");
            await summaryPage.VerifyRichTextFieldIsEmptyAsync("evidenceReason");
            await steps.VerifyRedDotField(steps.CreatePage.Summary, "Evidence Reason", true);

            await summaryPage.FillEvidenceSectionAsync(choice: true, reasonOrText: "Temporary manual text input one more time");

            await steps.VerifyRedDotField(steps.CreatePage.Summary, "Evidence Reason", false);
        }

        /// <summary>
        /// Проверяет, что кнопка "Sign Here" скрыта от пользователя, пока форма не сохранена 
        /// и не заполнены все обязательные поля с красными точками.
        /// </summary>
        [Test]
        public async Task Summary_SignButtonAvailability_HiddenUntilFormIsValid()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            await steps.ClickCreateIncidentAsync();
            await steps.SwitchToTab("Summary");

            var summaryPage = steps.CreatePage.Summary;

            // Форма пустая -> Проверяем, что кнопки "Sign Here" нет на экране или она задизейблена
            bool isSignVisibleBefore = await Page.GetByRole(AriaRole.Button, new() { Name = "Sign Here" }).IsVisibleAsync();
            Assert.That(isSignVisibleBefore, Is.False, "Кнопка 'Sign Here' доступна на пустой, несохраненной форме.");

            // Заполняем вкладку и сохраняем
            await steps.FillSummaryTabAsync(data);
            await steps.ClickSaveIncidentAsync();

            // Теперь кнопка обязана появиться
            bool isSignVisibleAfter = await Page.GetByRole(AriaRole.Button, new() { Name = "Sign Here" }).IsVisibleAsync();
            Assert.That(isSignVisibleAfter, Is.True, "Кнопка 'Sign Here' не появилась после заполнения обязательных полей и сейва.");
        }

        /// <summary>
        /// Проверяет блокировку полей (Read-only): после наложения подписи и сохранения 
        /// все поля ввода, редакторы и чекбоксы должны заблокироваться для редактирования.
        /// </summary>
        [Test]
        public async Task Summary_FieldLocking_FieldsBecomeReadOnlyPostSignature()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);
            
            await steps.ClickCreateIncidentAsync();
            await steps.SwitchToTab("Summary");

            var summaryPage = steps.CreatePage.Summary;

            // Заполняем, подписываем и сохраняем
            await steps.FillSummaryTabAsync(data);
            await steps.ClickSaveIncidentAsync(false);
            await steps.SignSummaryAndVerifyAsync();

            // Проверяем стейт блокировки полей (метод должен возвращать true, если контролы disabled)
            // Для этого можно использовать проверку атрибута disabled или класса mdc-button--disabled на чекбоксах
            bool isCheckboxLocked = await summaryPage.IsCheckboxDisabledAsync("Care Plan Updated");
            Assert.That(isCheckboxLocked, Is.True, "Чекбокс остался доступен для редактирования после подписания формы.");
        }

        /// <summary>
        /// Проверяет разблокировку формы: при нажатии "Remove Signature" поля должны снова стать активными.
        /// </summary>
        [Test]
        public async Task Summary_FieldUnlocking_FieldsBecomeEditableAfterSignatureRemoval()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);
            
            await steps.ClickCreateIncidentAsync();
            await steps.SwitchToTab("Summary");

            var summaryPage = steps.CreatePage.Summary;

            // Подписываем и фиксируем замок
            await steps.FillSummaryTabAsync(data);
            await steps.ClickSaveIncidentAsync(false);
            await steps.SignSummaryAndVerifyAsync();
            await steps.ClickSaveIncidentAsync(true);

            // Снимаем подпись
            await steps.RemoveSignatireAsync();

            // Проверяем, что чекбокс снова разлочен
            bool isCheckboxLocked = await summaryPage.IsCheckboxDisabledAsync("Care Plan Updated");
            Assert.That(isCheckboxLocked, Is.False, "Чекбокс остался заблокирован после удаления цифровой подписи.");
        }
        /// <summary>
        /// Проверяет интеграцию: активация чекбокса "Set as reportable" приводит к тому,
        /// что API бэкенда при сохранении возвращает признак успешного создания связанной сущности отчета.
        /// </summary>
        [Test]
        public async Task Summary_Integration_SetAsReportable_TriggersReportCreation()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            await steps.ClickCreateIncidentAsync();
            await steps.SwitchToTab("Summary");

            var summaryPage = steps.CreatePage.Summary;
            await steps.FillSummaryTabAsync(data);

            // Активируем чекбокс и сохраняем инцидент
            await summaryPage.SetCheckboxAsync("Set as reportable", true);
            await steps.ClickSaveIncidentAsync();

            // 1. Извлекаем ID инцидента из текущего URL
            string draftUrl = await steps.GetCurrentUrlAsync();
            string incidentId = await steps.GetIndicentId(draftUrl);

            await steps.VerifyReportableLogContainsCurrentIncidentAsync(incidentId);

        }

        /// <summary>
        /// Проверяет принудительное подписание (Forced Signing): 
        /// Полное подписание блокирует форму -> разблокировка -> удаление подписи Summary делает сохранение неактивным ->
        /// повторное подписание возвращает активность кнопке сохранения.
        /// </summary>
        [Test]
        public async Task Summary_ForcedSigning_PreventSaveWhenSignatureIsRemoved()
        {
            // 1. Создаем, заполняем, подписываем и проверяем блокировку
            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.AssertIncidentIsLockedAsync();

            // 2. Разлочиваем документ через админ-панель/контроли
            await steps.SetIncidentLockStateAsync(false);
            await steps.AssertIncidentIsUnlockedAsync();

            // 3. Переходим на Summary и удаляем подпись
            await steps.SwitchToTab("Summary");
            await steps.RemoveSignatireAsync();

            // 4. Проверяем, что кнопка сохранения заблокирована (Save is disabled)
            await steps.VerifySaveButtonEnabledStateAsync(false);

            // 5. Возвращаем подпись назад (подписываем заново кнопку "Sign Here")
            await steps.SignSummaryAndVerifyAsync();

            // 6. Проверяем, что кнопка сохранения снова активна (Save is enabled)
            await steps.VerifySaveButtonEnabledStateAsync(true);

            // 7. Сохраняем, проверяем, что подпись на месте и документ автоматически залочился
            await steps.ClickSaveIncidentAsync();
            await steps.AssertIncidentIsLockedAsync();
        }

        [Test]
        public async Task Summary_PdfGenerationAndExport_TriggerDownloadAfterSigning()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            var tab = "Summary";
            await steps.SwitchToTab(tab);

            // 1. Заполняем вкладку Summary данными
            await steps.FillSummaryTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            // 2. Подписываем форму
            await steps.SignSummaryAndVerifyAsync();

            // 3. Сохраняем инцидент и ожидаем появление кастомного попапа скачивания (true)
            // Если попап не появится, таймаут упадет внутри ClickSaveIncidentAsync -> DownloadSummaryReportAsync
            await steps.ClickSaveIncidentAsync(shouldDownloadReport: true);
        }

        [Test]
        public async Task Summary_PdfGeneration_DoNotTriggerDownloadWhenDataChanged()
        {

            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);
            await steps.ClickCreateIncidentAsync();

            var summaryTab = "Summary";

            // Шаг 1: Подготавливаем подписанную форму
            await steps.SwitchToTab(summaryTab);
            await steps.FillSummaryTabAsync(data);
            // Здесь попап не появится это подтвердит, что изначально триггер не сработал.
            await steps.ClickSaveIncidentAsync(shouldDownloadReport: false);

            await steps.SignSummaryAndVerifyAsync();

            // Сохраняем в первый раз (режим создания -> режим редактирования). 
            // Здесь попап появится и скачается, это подтвердит, что триггер работал.
            await steps.ClickSaveIncidentAsync(shouldDownloadReport: true);

            // Шаг 2: Переходим на вкладку General и меняем поле
            await steps.SwitchToTab("General");
            await steps.ModifySingleFieldOnTabAsync("General");

            // Шаг 3: Возвращаемся на вкладку Summary, как требуют условия
            await steps.SwitchToTab(summaryTab);

            // Шаг 4: Нажимаем Save и ожидаем, что попапа скачивания НЕ будет (false)
            await steps.ClickSaveIncidentAsync(shouldDownloadReport: false);
        }

        [Test]
        public async Task Summary_AuthorTracking_VerifyLastModifiedUpdates()
        {

            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            var tab = "Summary";
            var currentUser = "Test, Polly"; // Можно брать динамически из data.User
            await steps.SwitchToTab(tab);

            // --- Шаг 1: Проверка до заполнения (опционально, что таблицы еще нет или она пуста) ---
            var table = Page.Locator("table").Filter(new() { HasText = "Last Modified By" });
            await Assertions.Expect(table).Not.ToContainTextAsync(currentUser);
            await steps.ClickCreateIncidentAsync();

            // --- Шаг 2: Заполнение и первое сохранение ---
            await steps.FillSummaryTabAsync(data);
            await steps.ClickSaveIncidentAsync(shouldDownloadReport: false);

            // Вызываем наш новый шаг
            await steps.VerifyLastModifiedFooterAsync(currentUser);

            // --- Шаг 3: Подписание и второе сохранение ---
            await steps.SignSummaryAndVerifyAsync();
            await steps.ClickSaveIncidentAsync(shouldDownloadReport: true);

            // Вызываем шаг повторно
            await steps.VerifyLastModifiedFooterAsync(currentUser);
        }
    }
}
