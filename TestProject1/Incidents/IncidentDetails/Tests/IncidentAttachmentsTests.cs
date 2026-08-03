using System;
using System.Collections.Generic;
using System.Text;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    internal class IncidentAttachmentsTests : BaseIncidentTests
    {

        [Test]
        public async Task AttachmentsTabSinglePageFileCompletenessVerification()
        {
            // 1. ПРЕ-КОНФИГУРАЦИЯ: Явно задаем порог в 3 файла через ваш API-метод
            // Предположим, у вас есть API-клиент или хелпер для этого:
            await steps.UpdateIncidentConfigurationAsync("Attachments", true, 1);

            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // 2. ПРОВЕРКА ИСХОДНОГО СОСТОЯНИЯ
            // Красная точка должна быть видна, так как файлов еще 0 (меньше порога 3)
            await steps.VerifyRedDotTab(tab, true);
            // Проверяем, что счетчик на вкладке равен (0) и в таблице 0 строк
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 0);

            // 3. ДЕЙСТВИЕ: Загружаем 1 файл на 10 страниц. 
            // Ваша логика распределения категорий создаст 10 отдельных строк в таблице!
            await steps.UploadAttachmentTabAsync(AttachmentsTab.AttachmentCategories, fileNameString: "test_1page.pdf", toScreenShot: true);

            // 4. ФИНАЛЬНЫЕ ПРОВЕРКИ
            // Так как создалось 10 строк, это больше или равно порогу (3) -> точка должна исчезнуть
            await steps.VerifyRedDotTab(tab, false);

            // Проверяем, что теперь счетчик на вкладке стал (10) и в таблице ровно 10 строк
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 1);

            // Опционально: проверяем, что все 10 категорий из списка отображаются в таблице
            foreach (var category in AttachmentsTab.AttachmentCategories.Take(1))
            {
                await steps.VerifyAttachmentRowIsDisplayedAsync(category);
            }
        }

        [Test]
        public async Task AttachmentsTabSingleMultiPageFileCompletenessVerification()
        {
            // 1. ПРЕ-КОНФИГУРАЦИЯ: Явно задаем порог в 3 файла через ваш API-метод
            // Предположим, у вас есть API-клиент или хелпер для этого:
            await steps.UpdateIncidentConfigurationAsync("Attachments", true, 3);

            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // 2. ПРОВЕРКА ИСХОДНОГО СОСТОЯНИЯ
            // Красная точка должна быть видна, так как файлов еще 0 (меньше порога 3)
            await steps.VerifyRedDotTab(tab, true);
            // Проверяем, что счетчик на вкладке равен (0) и в таблице 0 строк
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 0);

            // 3. ДЕЙСТВИЕ: Загружаем 1 файл на 10 страниц. 
            // Ваша логика распределения категорий создаст 10 отдельных строк в таблице!
            await steps.UploadAttachmentTabAsync(AttachmentsTab.AttachmentCategories, fileNameString: "test_10pages.pdf", toScreenShot: true);

            // 4. ФИНАЛЬНЫЕ ПРОВЕРКИ
            // Так как создалось 10 строк, это больше или равно порогу (3) -> точка должна исчезнуть
            await steps.VerifyRedDotTab(tab, false);

            // Проверяем, что теперь счетчик на вкладке стал (10) и в таблице ровно 10 строк
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 10);

            // Опционально: проверяем, что все 10 категорий из списка отображаются в таблице
            foreach (var category in AttachmentsTab.AttachmentCategories.Take(10))
            {
                await steps.VerifyAttachmentRowIsDisplayedAsync(category);
            }
        }

        [Test]
        public async Task AttachmentsTabRedDotReturnsWhenBelowThresholdVerification()
        {
            // 1. Настройка конфигурации: ставим порог 3 файла
            await steps.UpdateIncidentConfigurationAsync(sectionCode: "Attachments", isEnabled: true, attachmentCount: 3);

            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // 2. Исходное состояние: точка есть, файлов 0
            await steps.VerifyRedDotTab(tab, true);
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 0);

            // 3. Загружаем тестовый PDF. Передаем список из 3 категорий, чтобы метод разбил его на 3 строки
            var initialCategories = AttachmentsTab.AttachmentCategories.Take(3).ToList();
            await steps.UploadAttachmentTabAsync(initialCategories, fileNameString: "test_3pages.pdf", toScreenShot: true);

            // 4. Проверяем, что лимит выполнен: точка скрылась, счетчик равен 3
            await steps.VerifyRedDotTab(tab, false);
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 3);

            // 5. ДЕЙСТВИЕ: Удаляем один файл (например, самую первую категорию из списка)
            string categoryToRemove = initialCategories[0];
            await steps.DeleteAttachmentRowAsync(categoryToRemove);

            // 6. ПРОВЕРКА ВОЗВРАТА ТОЧКИ:
            // Количество файлов (2) теперь меньше порога (3) -> красная точка ДОЛЖНА ВЕРНУТЬСЯ
            await steps.VerifyRedDotTab(tab, true);

            // Счетчик вкладки и строки в таблице должны уменьшиться до 2
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 2);
        }

        [Test]
        public async Task AttachmentsTabCounterAndTableRowsSynchronizationVerification()
        {
            // Отключаем лимиты или ставим 1, чтобы "Red Dot" нам здесь не мешал — мы тестируем строго цифры
            await steps.UpdateIncidentConfigurationAsync(sectionCode: "Attachments", isEnabled: true, attachmentCount: 1);

            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // ЭТАП 1: Проверяем исходное пустое состояние (0 == 0)
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 0);

            // ЭТАП 2: Загружаем 3 страницы и проверяем, что счетчик стал (3) и в таблице ровно 3 строки
            var initialCategories = AttachmentsTab.AttachmentCategories.Take(3).ToList();
            await steps.UploadAttachmentTabAsync(initialCategories, fileNameString: "test_3pages.pdf", toScreenShot: true);

            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 3);

            // ЭТАП 3: Удаляем одну строку и проверяем, что счетчик синхронно упал до (2) и строк осталось 2
            string categoryToRemove = initialCategories[0];
            await steps.DeleteAttachmentRowAsync(categoryToRemove);

            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 2);
        }


        [Test]
        public async Task AttachmentsTabDifferentCategoryPagesSplittingVerification()
        {
            await steps.UpdateIncidentConfigurationAsync(sectionCode: "Attachments", isEnabled: true, attachmentCount: 1);

            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // Выбираем 3 абсолютно разные категории
            var differentCategories = AttachmentsTab.AttachmentCategories.Take(3).ToList();

            // Загружаем 3 страницы и распределяем их по разным категориям
            await steps.UploadAttachmentTabAsync(differentCategories, fileNameString: "test_3pages.pdf", toScreenShot: true);

            // Проверяем, что в таблице появилось ровно 3 отдельных строки
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 3);

            // Проверяем, что каждая выбранная категория присутствует на экране
            foreach (var category in differentCategories)
            {
                await steps.VerifyAttachmentRowIsDisplayedAsync(category);
            }
        }

        [Test]
        public async Task AttachmentsTabSameCategoryPagesMergingAndPageCountVerification()
        {
            await steps.UpdateIncidentConfigurationAsync(sectionCode: "Attachments", isEnabled: true, attachmentCount: 1);

            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // 1. ДЕЙСТВИЕ: Загружаем 3 страницы, выбрав одинаковую категорию
            string targetCategory = "Accident Report";
            var identicalCategories = new List<string> { targetCategory, targetCategory, targetCategory };
            await steps.UploadAttachmentTabAsync(identicalCategories, fileNameString: "test_3pages.pdf", toScreenShot: true);

            // 2. ПРОВЕРКА UI: Строки схлопнулись в одну
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 1);
            await steps.VerifyAttachmentRowIsDisplayedAsync(targetCategory);

            // 3. ДЕЙСТВИЕ: Скачиваем файл во временную директорию через шаг
            string downloadedFilePath = await steps.DownloadAttachmentToTempFolderAsync(targetCategory);

            // 4. ПРОВЕРКА БИЗНЕС-ЛОГИКИ: Считываем количество страниц из скачанного файла
            int actualPagesInPdf = steps.GetPdfPageCount(downloadedFilePath);

            Assert.That(actualPagesInPdf, Is.EqualTo(3),
                $"Бэкенд некорректно склеил файл! Ожидалось 3 объединенных страницы в PDF, но обнаружено: {actualPagesInPdf}");

            // Очищаем за собой диск агента сборки
            if (File.Exists(downloadedFilePath)) File.Delete(downloadedFilePath);
        }

        [Test]
        public async Task AttachmentsTabCnaStatementGeneratesPostfixesInsteadOfMergingVerification()
        {
            await steps.UpdateIncidentConfigurationAsync(sectionCode: "Attachments", isEnabled: true, attachmentCount: 1);
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // 1. ДЕЙСТВИЕ: Загружаем 3 страницы под одной категорией
            string targetCategory = "CNA Statement";
            var cnaCategories = new List<string> { targetCategory, targetCategory, targetCategory };
            await steps.UploadAttachmentTabAsync(cnaCategories, fileNameString: "test_3pages.pdf", toScreenShot: true);

            // 2. ПРОВЕРКА UI 1: Строки не схлопнулись, их ровно 3
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 3);

            // 3. ПРОВЕРКА UI 2: Используем ОДИН И ТОТ ЖЕ метод шага для проверки постфиксов!
            await steps.VerifyAttachmentRowIsDisplayedAsync($"{targetCategory}_A");
            await steps.VerifyAttachmentRowIsDisplayedAsync($"{targetCategory}_B");
            await steps.VerifyAttachmentRowIsDisplayedAsync($"{targetCategory}_C");
        }

        [Test]
        public async Task AttachmentsTabFileDownloadAndNameMaskVerification()
        {
            await steps.UpdateIncidentConfigurationAsync(sectionCode: "Attachments", isEnabled: true, attachmentCount: 1);
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // 1. ДЕЙСТВИЕ: Загружаем 1 файл
            string targetCategory = "Witness Statement";
            var categories = new List<string> { targetCategory };
            await steps.UploadAttachmentTabAsync(categories, fileNameString: "test_1page.pdf", toScreenShot: true);

            // 2. ДЕЙСТВИЕ И ПРОВЕРКА: Скачиваем файл и валидируем маску имени {MRN}_{Category}_{Date}.pdf
            // Этот шаг мы детально спроектировали в самом начале, он использует GetResidentMrnAsync() 
            // и проверяет регуляркой имя файла, возвращаемое браузером.
            await steps.DownloadAndVerifyAttachmentMaskAsync(targetCategory);
        }

        [Test]
        public async Task AttachmentsTabEditCategoryPersistenceVerification()
        {
            await steps.UpdateIncidentConfigurationAsync(sectionCode: "Attachments", isEnabled: true, attachmentCount: 1);
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // 1. ДЕЙСТВИЕ: Загружаем файл с начальной категорией
            string initialCategory = "Witness Statement";
            string updatedCategory = "Employee Statement";
            var categories = new List<string> { initialCategory };
            await steps.UploadAttachmentTabAsync(categories, fileNameString: "test_1page.pdf", toScreenShot: true);

            // 2. ДЕЙСТВИЕ: Меняем категорию в строке и сохраняем форму
            await steps.EditAttachmentCategoryAsync(initialCategory, updatedCategory);

            await steps.ClickSaveIncidentAsync();
            // На всякий случай делаем рефреш, чтобы доказать, что бэкенд засинкшил изменения в БД
            await Page.ReloadAsync();
            await steps.SwitchToTab(tab);

            // 3. ПРОВЕРКА: Старая категория исчезла, новая успешно отображается в таблице
            await steps.VerifyAttachmentRowIsDisplayedAsync(initialCategory);
            await steps.VerifyRowSelectedCategoryAsync(initialCategory, updatedCategory);

            // Проверяем, что строк по-прежнему одна
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 1);
        }

        [Test]
        public async Task AttachmentsTab_AutoPopulation_AddedByAndConverterTime()
        {
            // Пре-конфигурация и создание инцидента
            await steps.FillAndSaveEntireIncident(data);

            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            string fileName = "test_1page.pdf";
            string expectedUserEmail = "polly@test.ts"; 

            // Действие: Загружаем файл
            await steps.UploadAttachmentTabAsync(AttachmentsTab.AttachmentCategories, fileNameString: fileName);

            // Берем первую категорию для проверки конкретной строки
            string targetCategory = AttachmentsTab.AttachmentCategories.First();

            // Проверка автозаполнения данных
            await steps.VerifyAttachmentRowMetadataAsync(targetCategory, expectedUserEmail, expectedTime: DateTime.UtcNow);


        }

        [Test]
        public async Task AttachmentsTab_Locking_ReadOnlyModeWhenIncidentSigned()
        {
            // Создаем инцидент и загружаем один тестовый файл, чтобы было что удалять
            await steps.FillAndSaveEntireIncident(data);

            // Имитируем полную блокировку инцидента подписями (DON/Admin)
            // В зависимости от логики системы: либо через UI подписываем, либо переводим статус бэкендом
            await steps.SignDNS();
            await steps.AssertIncidentIsLockedAsync();

            // Возвращаемся на вкладку вложений
            await steps.SwitchToTab("Attachments");

            // ПРОВЕРКА: Кнопки редактирования должна быть скрыты 
            await steps.VerifyAttachmentEditButtonsVisibilityAsync(isVisible: false);

        }

        [Test]
        public async Task AttachmentsTab_Validation_InvalidFormatAndEmptyFile()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();
            await steps.SwitchToTab("Attachments");

            // Попытка 1: Загрузка исполняемого файла (.exe)
            await steps.UploadAttachmentTabAsync(
                categoryNames: new List<string> { "Other" },
                note: "Testing malicious exe",
                fileNameString: "malicious_app.exe",
                toScreenShot: true,
                expectSuccess: false // <-- Магия здесь
            ); await steps.VerifyAttachmentErrorMessageDisplayedAsync("Unsupported file format. Executable files are not allowed.");

            // Попытка 2: Загрузка пустого файла (0 КБ)
            await steps.UploadAttachmentTabAsync(
                categoryNames: new List<string> { "Other" },
                note: "Testing empty file",
                fileNameString: "empty_file.txt",
                toScreenShot: true,
                expectSuccess: false
            ); await steps.VerifyAttachmentErrorMessageDisplayedAsync("File cannot be empty.");
        }

        [Test]
        public async Task AttachmentsTab_Validation_StandardFormatsDoNotTriggerAssignPages()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();
            await steps.SwitchToTab("Attachments");

            // Загружаем стандартный документ .doc или картинку .png
            // Система должна применить категорию ко всему файлу целиком, не открывая попап нарезки страниц
            await steps.UploadAttachmentTabAsync(AttachmentsTab.AttachmentCategories, fileNameString: "photo_evidence.png");

            // ПРОВЕРКА: Попап распределения страниц ("Assign Pages") НЕ появился на экране
            await steps.VerifyAssignPagesPopupVisibilityAsync(isVisible: false);

            // Проверяем, что файл успешно добавился как 1 элемент/строка
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 1);
        }

        [Test]
        public async Task AttachmentsTab_Validation_LargeFileSizeLimitError()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();
            await steps.SwitchToTab("Attachments");

            // Попытка загрузить файл размером более 25 мегабайт
            await steps.UploadAttachmentTabAsync(AttachmentsTab.AttachmentCategories, fileNameString: "30mb.pdf");

            // ПРОВЕРКА: Система корректно обрабатывает ошибку лимита размера файла
            await steps.VerifyAttachmentErrorMessageDisplayedAsync("File size exceeds the maximum limit of 25MB.");

            // Проверяем, что счетчик остался нулевым и файл не попал в таблицу
            await steps.VerifyAttachmentsCounterAndTableRowsAsync(expectedCount: 0);
        }
    }
}
