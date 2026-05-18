using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using NUnit.Framework;
using Serilog;
using static BaseIncidentTests;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.BaseIncidentTabs;
using static DetailsTab;
using static GeneralTab;
using static IncidentCreatePage;
using static IncidentDataFactory;
using static MedicationTab;
using static StateTab;
using static SummaryTab;
using static System.Net.WebRequestMethods;



    namespace CareAdminTestProject.Incidents.IncidentDetails.Steps
    {
        public class IncidentDetailsSteps
        {
            private readonly IPage _page;
            private readonly IncidentCreatePage _createPage;
            private readonly IncidentTrackerPage _trackerPage;
            public IncidentCreatePage CreatePage => _createPage;
            string fileName;

        public IncidentGeneralInfo CapturedGeneralData { get; private set; }

        public IncidentDetailsSteps(IPage page)
            {
                _page = page;
                _createPage = new IncidentCreatePage(page);
                _trackerPage = new IncidentTrackerPage(page);
            }

            public async Task OpenNewIncidentAsync()
            {
                await _trackerPage.ClickNewIncidentAsync();
                Log.Debug("New Incident form is opened");
            }

            public async Task<ResidentInfo> SelectResidentAsync(int i)
            {
            Log.Debug($"Try to select resident with the index {i} in the list");

            var info = await _createPage.SelectResidentAsyncByInd(i);
                // Можно сразу добавить проверку, чтобы не тащить её в тест
                await Assertions.Expect(_page.Locator("a.link.resident-name")).ToContainTextAsync(info.Name);
                Log.Debug($"Resident is selected {info.Name}");
                return info;
            }

        public async Task<string> GetCurrentUrlAsync()
        {
            return _page.Url;
        }

        public async Task GoBackBrowserAsync()
        {
            Log.Debug("Нажимаем браузерную кнопку 'Назад' для проверки Change Detection...");

            // Эмулирует нажатие стрелки назад в браузере
            await _page.GoBackAsync();
        }

        public async Task LeavePageViaMenuAsync()
        {
            Log.Debug("Кликаем по пункту меню 'Tracker' для ухода со страницы...");

            // Находим ссылку 'Tracker' внутри боковой панели Accident/Incident
            await _page.Locator("a:has-text('Tracker'), .sidebar-link:has-text('Tracker')").First.ClickAsync();
        }

        public async Task ReloadPageAndNavigateAsync(string url)
        {
            await _page.GotoAsync(url);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        public async Task SwitchToTab(string tabName)
        {
            Log.Debug($"Переключаемся на вкладку: {tabName}");

            // 1. Кликаем по вкладке
            await _createPage.ClickTabAsync(tabName, new() { Timeout = 30000 });

            // 2. Находим активную панель контента
            var tabContentPanel = _page.GetByRole(AriaRole.Tabpanel, new() { Name = tabName });
            await tabContentPanel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

            // 3. ЖДЕМ ЗАВЕРШЕНИЯ АНИМАЦИИ (ждем, пока opacity станет равен 1)
            Log.Debug("Ждем завершения CSS-анимации вкладки...");
            try
            {
                // Опрашиваем элемент, пока стили анимации не придут в финальное состояние
                await _page.WaitForFunctionAsync(
                    "el => window.getComputedStyle(el).opacity === '1'",
                    await tabContentPanel.ElementHandleAsync(),
                    new() { Timeout = 5000 }
                );
            }
            catch
            {
                Log.Warning("Анимация opacity не завершилась вовремя, пробуем сделать стабилизирующую паузу.");
                // Хэндшейк-задержка на случай жесткого лага рендеринга на QA-стенде
                await Task.Delay(1000);
            }

            Log.Debug($"Вкладка {tabName} готова к работе!");
        }

        public async Task FillAndSaveEntireIncident(IncidentTestData data)
        {
            await ClearGeneralForm();
            await FillGeneralTabAsync(data);
            await ClickCreateIncidentAsync();
            await FillDetailsTabAsync(data);
            await FillStateTabAsync(data);
            await FillMedicationTabAsync(data);
            await FillRNFormTabAsync(data);
            await FillSummaryTabAsync(data);
            await ClickSaveIncidentAsync();
            await SignSummaryAndVerifyAsync();
            await ClickSaveIncidentAsync(true);
            await UploadAttachmentTabAsync("Accident Report");
        }

        public Task SignDNS()
        {
            return _createPage.SignAsRoleAsync(RoleToSign.DNS);
        }

        public Task SignMD()
        {
            return _createPage.SignAsRoleAsync(RoleToSign.MD);
        }

        public Task SignAdministrator()
        {
            return _createPage.SignAsRoleAsync(RoleToSign.Administrator);
        }

        public async Task AssertIncidentIsLockedAsync()
        {
            Log.Debug("Проверяем, что инцидент успешно заблокирован после всех подписей...");

            // Ищем верхний баннер блокировки
            var lockBanner = _page.Locator("body");
            await Assertions.Expect(lockBanner).ToContainTextAsync("Incident Locked", new() { Timeout = 5000 });

            Log.Debug("[SUCCESS] Инцидент заблокирован, все подписи сохранены.");
        }

        public async Task AssertIncidentIsUnlockedAsync()
        {
            Log.Debug("Проверяем состояние тумблера блокировки (должен быть разблокирован)...");

            // Смотрим строго на кнопку внутри mat-slide-toggle
            var toggleButton = _page.Locator("mat-slide-toggle button[role='switch']").First;

            // Проверяем, что атрибут равен "false"
            await Assertions.Expect(toggleButton).ToHaveAttributeAsync("aria-checked", "false", new() { Timeout = 5000 });

            Log.Debug("[SUCCESS] Подтверждено атрибутом Angular: инцидент разблокирован.");
        }

        /// Устанавливает состояние блокировки инцидента (true - заблокировать, false - разблокировать)
        public async Task SetIncidentLockStateAsync(bool shouldBeLocked)
        {
            Log.Debug($"[Блокировка] Переводим форму в состояние 'Заблокировано = {shouldBeLocked}'...");

            // Точечный локатор переключателя
            var toggleButton = _page.Locator("mat-slide-toggle button[role='switch']").First;
            await toggleButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // Проверяем текущий статус ползунка
            var ariaChecked = await toggleButton.GetAttributeAsync("aria-checked");
            bool isCurrentlyLocked = ariaChecked == "true";

            if (isCurrentlyLocked == shouldBeLocked)
            {
                Log.Debug($"Форма уже в нужном состоянии (Locked = {isCurrentlyLocked}). Клик пропущен.");
                return;
            }

            Log.Debug($"Кликаем по тумблеру блокировки. Текущее: {isCurrentlyLocked}, Целевое: {shouldBeLocked}");

            // Кликаем по самому элементу кнопки внутри mat-slide-toggle
            await toggleButton.ClickAsync(new() { Force = true });

            // Ждем изменения Angular-атрибута кнопки (true/false) — это главный индикатор переключения
            string expectedValue = shouldBeLocked ? "true" : "false";
            await Assertions.Expect(toggleButton).ToHaveAttributeAsync("aria-checked", expectedValue, new() { Timeout = 7000 });

            // Небольшая пауза для завершения анимации стилей на странице
            await Task.Delay(1000);
            Log.Debug("[Блокировка] Состояние тумблера успешно изменилось в DOM.");
        }

        public async Task FillGeneralTabAsync(IncidentTestData data)
            {
                CapturedGeneralData = await _createPage.General.FillBasicInfoAsync(data.General);
                await _page.MakeScreenshotAsync("General_Filled");
                Log.Information("General Tab filled");
            }

            public async Task FillDetailsTabAsync(IncidentTestData data)
            {
                await _createPage.ClickTabAsync("Details");
                await _createPage.Details.FillDetailsInfoAsync(data.Details);
                await _page.MakeScreenshotAsync("Details_Filled");
                Log.Information("Details Tab filled");
            }

            public async Task FillStateTabAsync(IncidentTestData data)
            {
                await _createPage.ClickTabAsync("State");
                await _createPage.State.FillStateTabAsync(data.State);
                await _page.MakeScreenshotAsync("State_Filled");
                Log.Information("State Tab filled");
            }

            public async Task FillMedicationTabAsync(IncidentTestData data)
            {
                await _createPage.ClickTabAsync("Medication");
                await _createPage.Medication.FillMedicationTabAsync(data.Medications);
                await _page.MakeScreenshotAsync("Medication_Filled");
                Log.Information("Medication Tab filled");
            }

            public async Task ClearMedicationTabAsync()
            {
                await _createPage.ClickTabAsync("Medication");
                await _createPage.Medication.ClearAllMedicationsAsync();
                Log.Information("Medication Tab cleared");
            }

        public async Task FillRNFormTabAsync(IncidentTestData data)
            {
                await _createPage.ClickTabAsync("RN Supervisor Investigation Form\r\n");
                await _createPage.RNSupervisor.FillQuestionsAsync(data.RNSupervisor);
                await _page.MakeScreenshotAsync("RNSupervisorForm_Filled");
                Log.Information("RN Supervisor Investigation Form Tab filled");
            }

            public async Task FillSummaryTabAsync(IncidentTestData data)
            {
                await _createPage.ClickTabAsync("Summary");
                await _createPage.Summary.FillSummaryInfoAsync(data.Summary);
                await _page.MakeScreenshotAsync("Summary_Filled");
                Log.Information("Summary Tab filled");
            }
            public async Task UploadAttachmentTabAsync(string categoryName, string? note = null, string fileNameString = null, bool toScreenShot= true)
            {
                await _createPage.ClickTabAsync("Attachments");

                // Если имя файла передано (из прошлых шагов) — берем его, иначе используем наш дефолтный путь
                string fileToUpload;

                // Если имя файла НЕ передано — берем готовое значение/путь из свойства класса (fileName)
                if (string.IsNullOrEmpty(fileNameString))
                {
                    fileToUpload = fileName;
                }
                // Если передано конкретное имя файла (например, "test_1page.pdf")
                else
                {
                    // Если вдруг передан сразу полный путь — оставляем, иначе собираем из TestData
                    fileToUpload = Path.IsPathRooted(fileNameString)
                        ? fileNameString
                        : Path.Combine(AppContext.BaseDirectory, "TestData", "Files", fileNameString);
                }
            
            
                await _createPage.Attachments.UploadAttachmentAsync(fileToUpload);
                await _createPage.Attachments.AssignCategoriesToAllPagesAsync(categoryName, note);
                await _createPage.Attachments.VerifyAttachmentIsDisplayedAsync(categoryName);
                if (toScreenShot)
                {
                    await _page.MakeScreenshotAsync("Attachment_Filled");
                }
                Log.Information("Attachment file attached");
            }

        public async Task UploadAttachmentTabAsync(
        IReadOnlyList<string> categoryNames, // Принимает список категорий для каждой страницы
        string? note = null,
        string? fileNameString = null,
        bool toScreenShot = true)
        {
            await _createPage.ClickTabAsync("Attachments");

            string fileToUpload = string.IsNullOrEmpty(fileNameString)
                ? fileName
                : (Path.IsPathRooted(fileNameString)
                    ? fileNameString
                    : Path.Combine(AppContext.BaseDirectory, "TestData", "Files", fileNameString));

            await _createPage.Attachments.UploadAttachmentAsync(fileToUpload);

            // Вызываем ваш метод полистной разметки, передавая ему весь список категорий
            await _createPage.Attachments.AssignCategoriesToAllPagesAsync(categoryNames, note);

            // Проверяем отображение первой категории из списка в качестве базовой проверки
            if (categoryNames != null && categoryNames.Any())
            {
                await _createPage.Attachments.VerifyAttachmentIsDisplayedAsync(categoryNames[0]);
            }

            if (toScreenShot)
            {
                await _page.MakeScreenshotAsync("Attachment_Filled");
            }
            Log.Information($"Multi-page attachment file '{fileToUpload}' attached successfully.");
        }

        public async Task VerifyCreateButtonIsDisabledAsync()
        {
            // Получаем локатор кнопки через ваш класс страницы
            var createButton = await _createPage.GetCreateIncidentButtonLocator();

            // Проверяем стандартный HTML-атрибут disabled
            await Assertions.Expect(createButton).ToBeDisabledAsync();
        }


        public async Task VerifyDataRetainedAsync(object tabData)
        {
            switch (tabData)
            {
                case IncidentGeneralInfo generalData:
                    // Вызываем проверку у обработчика вкладки General
                    await _createPage.ClickTabAsync("General");
                    await _createPage.General.VerifyDataFieldsAsync(generalData);
                    Log.Debug("Tab General checked and is OK");
                    break;

                case IncidentDetailsInfo detailsData:
                    // Вызываем проверку у обработчика вкладки Details
                    await _createPage.ClickTabAsync("Details");
                    await _createPage.Details.VerifyDataFieldsAsync(detailsData);
                    Log.Debug("Tab Details checked and is OK");
                    break;

                case IncidentStateInfo stateData:
                    await _createPage.ClickTabAsync("State");
                    await _createPage.State.VerifyDataFieldsAsync(stateData);
                    Log.Debug("Tab State checked and is OK");
                    break;

                case List<MedicationInfo> medicationsList:
                    await _createPage.ClickTabAsync("Medication");
                    await _createPage.Medication.VerifyDataFieldsAsync(medicationsList);
                    Log.Debug("Tab Medications checked and is OK");
                    break; 

                case IncidentSummaryInfo summaryData:
                    await _createPage.ClickTabAsync("Summary");
                    await _createPage.Summary.VerifyDataFieldsAsync(summaryData);
                    Log.Debug("Tab Summary checked and is OK");
                    break;

                case RNSupervisorTab.RNSupervisorTabInfo rnFormData:
                    await _createPage.ClickTabAsync("RN Supervisor Investigation Form");
                    await _createPage.RNSupervisor.VerifyDataFieldsAsync(rnFormData);
                    Log.Debug("Tab RN Supervisor Investigation Form checked and is OK");
                    break;

                default:
                    throw new ArgumentException($"Универсальная проверка не настроена для типа данных: {tabData.GetType().Name}");
            }
        }


        public async Task ModifySingleFieldOnTabAsync(string tabName)
        {
            switch (tabName)
            {
                case "General":
                    await _createPage.General.GetFieldByLabel("SBARSummary").TypeAsync("Changed field");
                    break;
                case "Details":
                    await _createPage.Details.GetFieldByLabel("Describe Occurrence").TypeAsync("Changed field");
                    break;
                case "State":
                    // Для чекбоксов Angular Material/Kendo изменение состояния тоже триггерит dirty
                    await _page.Locator("mat-checkbox input, kendo-switch input").First.ClickAsync();
                    break;
                case "Medication":
                    var row = _page.Locator(".medication-row.ng-star-inserted").Nth(0);
                    await row.Locator("input").Nth(1).FillAsync("1000");
                    break;
                case "RN Supervisor Investigation Form":
                    await _page.Locator("kendo-timepicker[name='answerTime'] input:visible").First.ClickAsync();
                    // Просто фокус и изменение времени
                    await _page.Keyboard.PressAsync("ArrowUp");
                    break;
                case "Summary":
                    await _createPage.Summary.SelectQuestionRadioAsync("Based upon the collection and review of all attached information, the following conclusion has been reached:", "Undetermined");

                    break;
            }
        }

        public async Task VerifyUnsavedChangesAlertVisibleAsync(string sourceTabName)
        {
            Log.Debug($"[VALIDATION] Проверяем появление кастомной модалки Change Detection для вкладки: {sourceTabName}");

            // 1. Ищем текст заголовка или тела кастомного окна
            var customModalText = _page.GetByText("Do you want to leave this page?");

            // 2. Ожидаем появление модалки на UI
            await customModalText.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });

            // 3. Делаем ассерт
            bool isModalVisible = await customModalText.IsVisibleAsync();
            Assert.That(isModalVisible, Is.True,
                $"Кастомное предупреждение о несохраненных данных НЕ появилось при попытке ухода с вкладки '{sourceTabName}'!");
        }

        public async Task ClickCreateIncidentAsync()
            {
                // Сюда пихаем и клик, и ожидание, и скриншот
                // (Предположим, метод ClickCreateIncident доступен в контексте или через _createPage)
                await _createPage.ClickCreateIncident();
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

                public async Task ClickSaveIncidentAsync(bool shouldDownloadReport = false)
            {
                // Сюда пихаем и клик, и ожидание, и скриншот
                // (Предположим, метод ClickCreateIncident доступен в контексте или через _createPage)
           
                Log.Debug("Clicking Save button");
                await _createPage.ClickSaveIncident();

                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                if (shouldDownloadReport)
                {
                    Log.Information("Waiting for Summary report to download...");
                    fileName = await _createPage.Summary.DownloadSummaryReportAsync();
                }

                await _page.MakeScreenshotAsync(shouldDownloadReport ? "save_with_report" : "save_regular");

                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            public async Task SignSummaryAndVerifyAsync()
            {
                await _createPage.ClickTabAsync("Summary\r\n");
                await _createPage.Summary.SignAndConfirmIncident();
                await _createPage.Summary.VerifySignatureImageVisible();
            }


            public async Task NavigateToTrackerViaMenu()
            {
                // 1. Уточняем селектор родителя (используем класс навигации, если он есть)
                var parentMenu = _page.Locator("li").Filter(new() { HasText = "Accident/Incident" });

                // 2. Уточняем ссылку Tracker именно внутри бокового меню (sidebar)
                // Используем .First(), если их несколько, или более точный путь:
                var trackerLink = parentMenu.Locator("a").Filter(new() { HasText = "Tracker" });

                // Проверяем видимость (раскрываем меню если надо)
                if (!await trackerLink.IsVisibleAsync())
                {
                    await parentMenu.Locator(".k-icon, .arrow-icon, text=Accident/Incident").First.ClickAsync();
                    // Даем меню время раскрыться (анимация)
                    await trackerLink.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                }

                // 3. Кликаем. Если обычный клик не помогает, пробуем Force: true
                await trackerLink.ClickAsync();

                // 4. Ждем смены URL. Это критично! 
                // Если тест падает здесь по таймауту — значит клик физически не сработал.
                await _page.WaitForURLAsync("**/accident-incident/tracker/**", new() { Timeout = 10000 });

                Log.Debug("Navigated to Tracker menu successfully");
            }

        public async Task VerifyFieldsOneByOneWithFilling(object tabComponent, object tabData)
        {
            dynamic tab = tabComponent;
            var fieldsMap = tab.GetRequiredFieldsMap((dynamic)tabData);

            foreach (var field in fieldsMap)
            {
                // Распаковываем кортеж из dynamic в именованные переменные
                // Это восстанавливает типизацию внутри цикла
                var (action, isRequired) = ((Func<Task>, bool))field.Value;

                // Теперь снова можно использовать красивые имена
                await VerifyRedDotField(tabComponent, field.Key, isRequired);

                await action.Invoke();

                //await _page.Keyboard.PressAsync("Tab");
                await Task.Delay(300);

                await VerifyRedDotField(tabComponent, field.Key, false);
            }
        }

        public async Task VerifyAllFieldsDotsStateAsync<T>(object tabComponent, T data, bool shouldBeVisible)
        {
            // 1. Используем switch для определения мапы, приводя данные к нужному типу
            var fieldsMap = tabComponent switch
            {
                GeneralTab g when data is IncidentGeneralInfo gData => g.GetRequiredFieldsMap(gData),
                DetailsTab d when data is IncidentDetailsInfo dData => d.GetRequiredFieldsMap(dData),

                _ => throw new ArgumentException(
                    $"Combination of tab {tabComponent.GetType().Name} and data {typeof(T).Name} is not supported")
            };

            // 2. Итерируемся по словарю
            foreach (var field in fieldsMap)
            {
                if (field.Value.IsRequired)
                {
                    await VerifyRedDotField(tabComponent, field.Key, shouldBeVisible);
                }
            }
        }

        public async Task VerifyStateTabSpecificLogicAsync()
        {
            var fieldsMap = _createPage.State.GetStateRequiredFieldsMap();

            foreach (var fieldEntry in fieldsMap)
            {
                string fieldName = fieldEntry.Key;
                var (Action, Reset, _) = fieldEntry.Value;
                Log.Debug($"Checking field {fieldName}");

                // Скроллим обратно вверх к индикатору, чтобы он гарантированно попал в viewport
                await _createPage.State.GeneralPointLocator.ScrollIntoViewIfNeededAsync();

                // 1. Ждем появления точки перед началом
                await _createPage.State.GeneralPointLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible });

                // 2. ВКЛЮЧАЕМ поле
                await Action.Invoke();

                // 3. УМНОЕ ОЖИДАНИЕ: Ждем, пока точка ИСЧЕЗНЕТ
                await _createPage.State.GeneralPointLocator.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

                // Дополнительная проверка для надежности
                Assert.That(await _createPage.State.IsGeneralStatePointVisibleAsync(), Is.False,
                    $"Point did NOT disappear after filling {fieldName}");

                // 4. СБРОС (выключаем поле)
                await Reset.Invoke();

                // Перед проверкой возвращения точки снова возвращаем скролл наверх
                await _createPage.State.GeneralPointLocator.ScrollIntoViewIfNeededAsync();

                // 5. УМНОЕ ОЖИДАНИЕ: Ждем, пока точка ПОЯВИТСЯ
                await _createPage.State.GeneralPointLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible });

                Assert.That(await _createPage.State.IsGeneralStatePointVisibleAsync(), Is.True,
                    $"Point did NOT return after clearing {fieldName}");

                Log.Debug($"Field {fieldName} verified fast and successfully");
            }
        }


        public async Task VerifyRedDotField(object tabComponent, string fieldName, bool shouldBeVisible = true)
        {
            Log.Debug($"Red Dot validation for the field '{fieldName}': is expected {shouldBeVisible}");

            // Динамически определяем, есть ли у переданного компонента метод проверки
            // Предполагается, что ваши компоненты (General, Details и т.д.) имеют метод IsFieldMarkedRequiredAsync

            var tab = tabComponent as BaseIncidentTabs;
            
            bool isVisible = await tab.IsFieldMarkedRequiredAsync(fieldName);

            Assert.That(isVisible, Is.EqualTo(shouldBeVisible),
                shouldBeVisible
                    ? $"The field {fieldName} should have the Red Dot"
                    : $"The field {fieldName} should NOT have the Red Dot ");

            Log.Debug($"Red Dot validation for the field '{fieldName}': {(isVisible ? "true" : "false")}");
        }

        public async Task VerifyMedicationTabFullLifecycleAndIndicatorAsync()
        {
            const string tabName = "Medication";
            Log.Information($"--- START: Complex lifecycle test for '{tabName}' tab ---");

            // Step 1: Verify that the red dot is initially VISIBLE
            Log.Debug("[STEP 1] Checking the initial state of the indicator...");
            await VerifyRedDotTab(tabName, shouldBeVisible: true);

            // Step 2: Add empty rows. The dot must REMAIN VISIBLE
            Log.Debug("[STEP 2] Adding empty medication rows...");
            await _createPage.Medication.AddEmptyMedicationRowsAsync(2);

            // Remove focus to trigger Angular validation
            await _page.Mouse.ClickAsync(0, 0);
            await _page.WaitForTimeoutAsync(200);

            await VerifyRedDotTab(tabName, shouldBeVisible: true);
            Log.Debug("[SUCCESS] Empty rows successfully kept the indicator visible");

            // Step 3: Fill the previously created empty rows with data. The dot must DISAPPEAR
            Log.Debug("[STEP 3] Filling the created rows with test data...");
            var testMedications = new List<MedicationTab.MedicationInfo>
    {
        new("Aspirin", "100mg", "Once a day", "08:00"),
        new("Nurofen", "250mg", "Twice a day", "15:00")
    };

            for (int i = 0; i < testMedications.Count; i++)
            {
                var medication = testMedications[i];
                var row = _page.Locator(".medication-row.ng-star-inserted").Nth(i);

                await row.Locator("input").Nth(0).FillAsync(medication.Name);
                await row.Locator("input").Nth(1).FillAsync(medication.Dosage);
                await row.Locator("input").Nth(2).FillAsync(medication.Frequency);
                await row.Locator("input").Nth(3).FillAsync(medication.TimeReceived);
            }

            // Short delay to let Angular process the form validation state
            await _page.WaitForTimeoutAsync(300);

            Log.Debug("[STEP 3] Verifying indicator hiding after filling the fields...");
            await VerifyRedDotTab(tabName, shouldBeVisible: false);
            Log.Debug("[SUCCESS] The indicator successfully hid after form filling");

            // Step 4: Remove all medications. The dot must RETURN
            Log.Debug("[STEP 4] Completely clearing the medication table...");
            await _createPage.Medication.ClearAllMedicationsAsync();

            // Delay to allow DOM elements removal to register in the form state
            await _page.WaitForTimeoutAsync(300);

            Log.Debug("[STEP 4] Verifying the return of the indicator...");
            await VerifyRedDotTab(tabName, shouldBeVisible: true);

            Log.Information($"--- FINISH: Lifecycle test for '{tabName}' tab successfully passed! ---");

        }

        public async Task FillRNFormTabWithTabCheckAsync(IncidentTestData data)
        {
            var tab = "RN Supervisor Investigation Form";

            // Просто вызываем метод заполнения, но передаем туда лямбду-проверку, 
            // которая сработает на нужном шаге внутри одного потока Playwright!
            await _createPage.RNSupervisor.FillQuestionsAsync(data.RNSupervisor, async (currentStep) =>
            {
                if (currentStep == 27)
                {
                    // Как только заполнился 27 шаг — проверяем, что красная точка всё еще на месте
                    await VerifyRedDotTab(tab, shouldBeVisible: true);
                    Log.Debug("Verified inside flow: Red Dot is still visible on step 27.");
                }
            });

            Log.Information("RN Supervisor Investigation Form Tab filled with inline validation");
        }

        public async Task VerifyRedDotTab(string tabName, bool shouldBeVisible = true)
            {
                // Вкладки обычно проверяются через главный пейдж-объект, а не через текущую вкладку
                bool isVisible = await _createPage.IsTabMarkedIncompleteAsync(tabName);

                Assert.That(isVisible, Is.EqualTo(shouldBeVisible),
                    shouldBeVisible
                        ? $"The tab '{tabName}' should have the Red Dot "
                        : $"The tab '{tabName}' should NOT have the Red Dot ");

                Log.Debug($"Red Dot validation for the tab '{tabName}': {(isVisible ? "true" : "false")}");
            }

            public async Task ClearGeneralForm()
            {
                await _createPage.General.ClearPreFilledFieldsAsync();
            }
            public async Task ClearDetailsForm()
            {
            Log.Debug("Try to clear All Diagnoses");
                await _createPage.Details.ClearAndSaveDiagnosesAsync();
                await _page.MakeScreenshotAsync("AllDiagnoses_cleared");

            }
        public async Task SwitchFirstAid(bool answer)
        {
            await _createPage.Details.SelectFirstAdmitedAsync(answer, "");
        }
    }
}
