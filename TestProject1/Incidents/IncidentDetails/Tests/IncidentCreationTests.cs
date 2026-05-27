using Microsoft.Playwright;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    /// <summary>
    /// Encapsulates functional regression and validation tests for the incident creation wizard flow.
    /// Covers end-to-end multi-tab field inputs, minimal required field evaluations, and negative validation data boundary checks.
    /// </summary>
    [TestFixture]
    public class IncidentCreationTests : BaseIncidentTests
    {
        /// <summary>
        /// Validates the full sequential user path for successfully creating an incident report from scratch.
        /// Fills out all multi-tab sheets, applies digital signatures, downloads compiled summaries, and streams document attachments.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateIncident_StartWithResidentSelection()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();

            await steps.FillDetailsTabAsync(data);
            await steps.FillStateTabAsync(data);
            await steps.FillMedicationTabAsync(data);
            await steps.FillRNFormTabAsync(data);
            await steps.FillSummaryTabAsync(data);

            // 6. Save and Sign workflows execution loops
            await steps.ClickSaveIncidentAsync();
            await steps.SignSummaryAndVerifyAsync();
            await steps.ClickSaveIncidentAsync(true);

            // 7. Dynamic attachments injection
            await steps.UploadAttachmentTabAsync("Other", "This is a test note");
            //await steps.SaveIncidentAsync();
        }

        /// <summary>
        /// Validates that an incident record successfully submits when populating strictly mandatory marked fields on the General form layout.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateIncident_WithOnlyRequiredFields_ShouldPass()
        {
            // Extract and slice away non-mandatory parameters from the General object layout, keeping only fields containing validation asterisks
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };

            await steps.FillGeneralTabAsync(minimalData);
            await steps.ClickCreateIncidentAsync();
        }

        /// <summary>
        /// Negative boundary test case routing: Evaluates form validation behaviors by sequentially purging mandatory field parameters one by one, 
        /// verifying the wizard locks data submission if critical information is absent.
        /// </summary>
        /// <param name="missingField">The data model parameter property name targeted to evaluate as empty or null.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if test inputs specify unknown fields outside mapped switch parameter limits.</exception>
        [TestCase("Room")]
        [TestCase("Bed")]
        [TestCase("Date")]
        [TestCase("Time")]
        [TestCase("Location")]
        [TestCase("Type")]
        public async Task CreateIncident_MissingRequiredField_ShouldShowValidationError(string missingField)
        {
            // Clear baseline form entries
            await steps.ClearGeneralForm();

            // 1. Retrieve a clean baseline collection containing only mandatory field metrics
            var baseRequiredGeneral = data.General.GetOnlyRequiredFields();

            // 2. Intentionally modify a designated target property to map as a null value state
            var invalidGeneral = missingField switch
            {
                "Room" => baseRequiredGeneral with { room = null },
                "Bed" => baseRequiredGeneral with { bed = null },
                "Date" => baseRequiredGeneral with { date = null },     // Will fail to populate the date picker control
                "Time" => baseRequiredGeneral with { time = null },     // Will fail to populate the time picker control
                "Location" => baseRequiredGeneral with { location = null },
                "Type" => baseRequiredGeneral with { type = null },
                _ => throw new ArgumentException($"Unknown validation field configuration targeted: {missingField}")
            };

            // 3. Re-assemble the complex IncidentTestData payload model mapping the modified invalid General data branch
            var invalidData = data with { General = invalidGeneral };

            // 4. Fill out the UI form inputs using mismatched invalid parameters
            await steps.FillGeneralTabAsync(invalidData);

            // 5. Enforce strict assertions verifying that the structural 'Create' wizard action button registers as disabled (Approach A)
            await steps.VerifyCreateButtonStateAsync(shouldBeEnabled: false);

            // Alternative Evaluation Approach B:
            // await steps.ClickCreateIncidentAsync(shouldBeEnabled: false);
        }


        /// <summary>
        /// Verifies that strictly mandatory field inputs on the General tab successfully persist 
        /// and restore into form fields after navigating away and reloading the saved draft URL.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateIncident_RequieredFields_ShouldRetainDataAfterReload()
        {
            // 1. Isolate only required fields and assemble the structured data representation
            var requiredGeneral = data.General.GetOnlyRequiredFields();
            var partialData = data with { General = requiredGeneral };

            // 2. Populate input fields on the General form layout tab view
            await steps.FillGeneralTabAsync(partialData);

            // 3. Commit creation persistence workflows
            await steps.ClickCreateIncidentAsync();
            await Task.Delay(1000);

            // 4. Capture the active window layout location destination path URL for the saved draft incident
            string draftUrl = await steps.GetCurrentUrlAsync();

            // 5. Force a browser reload navigating directly back to the saved draft URL
            await steps.ReloadPageAndNavigateAsync(draftUrl);

            // 6. Execute polymorphic verification checking that all parameters restore properly out of database storage records
            await steps.VerifyDataRetainedAsync(requiredGeneral);
        }

        /// <summary>
        /// Verifies that a fully populated dataset on the General tab successfully persists 
        /// and restores into form fields after navigating away and reloading the saved draft URL.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateIncident_FullGeneral_ShouldRetainDataAfterReload()
        {
            // 2. Populate all input fields on the General form layout tab view
            await steps.FillGeneralTabAsync(data);

            // 3. Commit creation persistence workflows
            await steps.ClickCreateIncidentAsync();
            await Task.Delay(1000);

            // 4. Capture the active window layout location destination path URL for the saved draft incident
            string draftUrl = await steps.GetCurrentUrlAsync();

            // 5. Force a browser reload navigating directly back to the saved draft URL
            await steps.ReloadPageAndNavigateAsync(draftUrl);

            // 6. Execute polymorphic verification checking that all parameters restore properly out of database storage records
            await steps.VerifyDataRetainedAsync(data.General);
        }

        /// <summary>
        /// Comprehensive data integration test: verifies that a full multi-tab incident form 
        /// completely saves into draft repositories and successfully restores every field parameter layout post browser reload.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateIncident_FullIncident_ShouldRetainDataAfterReload()
        {
            // 2. Populate input datasets across every separate wizard form sheet sequentially
            await steps.FillGeneralTabAsync(data);
            await steps.FillDetailsTabAsync(data);
            await steps.FillStateTabAsync(data);
            await steps.FillMedicationTabAsync(data);
            await steps.FillRNFormTabAsync(data);
            await steps.FillSummaryTabAsync(data);

            // 3. Commit creation persistence workflows
            await steps.ClickCreateIncidentAsync();
            await Task.Delay(1000);

            // 4. Capture the active window layout location destination path URL for the saved draft incident
            string draftUrl = await steps.GetCurrentUrlAsync();

            // 5. Force a browser reload navigating directly back to the saved draft URL
            await steps.ReloadPageAndNavigateAsync(draftUrl);

            // 6. Execute multi-tab validations checking that all separate sheet inputs successfully restored out of draft references
            await steps.VerifyDataRetainedAsync(data.General);
            await steps.VerifyDataRetainedAsync(data.Details);
            await steps.VerifyDataRetainedAsync(data.State);
            await steps.VerifyDataRetainedAsync(data.Medications);
            await steps.VerifyDataRetainedAsync(data.RNSupervisor);
            await steps.VerifyDataRetainedAsync(data.Summary);
        }

        /// <summary>
        /// Verifies that modifying an existing saved incident draft with a brand new updated dataset 
        /// successfully overwrites the older parameters and retains the modifications post browser reload.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task EditIncident_OverwriteWithNewDataSet_ShouldRetainUpdatedData()
        {
            // --- DATA PREPARATION ---
            // Dataset 1 (Retrieve baseline values directly from the data factory)
            var data1 = IncidentDataFactory.CreateDefaultFall(resident);

            // Dataset 2 (Clone Dataset 1, altering only specific target fields slated for overwriting)
            var data2 = data1 with
            {
                // Overwrite the specific questionnaire parameters on the RNSupervisor sheet branch
                RNSupervisor = data1.RNSupervisor with
                {
                    Locations = new[] { "Lobby" }, // Was: "ADL Suite", "Dining Room"
                    LastSeen = new RNSupervisorTabInfo.LastSeenInfo(
                        Time: new TimeOnly(14, 30), // Was: 09:00
                        Details: "Resident was walking down the hallway safely."
                    )
                },
                // Intentionally overwrite the baseline description narrative string within the General tab data branch
                General = data1.General with { summary = "Updated summary after second investigation" }
            };

            // --- STEP 1: POPULATE FORM FIELD LAYOUTS WITH DATASET 1 ---
            await steps.FillGeneralTabAsync(data1);
            await steps.FillDetailsTabAsync(data1);
            await steps.FillStateTabAsync(data1);
            await steps.FillMedicationTabAsync(data1);
            await steps.FillRNFormTabAsync(data1);
            await steps.FillSummaryTabAsync(data1);

            // Commit initial creation data persistence pipelines
            await steps.ClickCreateIncidentAsync();
            await Task.Delay(1000);
            string draftUrl = await steps.GetCurrentUrlAsync();

            // --- STEP 2: RE-OPEN FORM WORKSPACE AND OVERWRITE ENTRIES WITH DATASET 2 ---
            // Navigate straight back into the saved incident draft using the captured destination path URL
            await steps.ReloadPageAndNavigateAsync(draftUrl);

            // Inject the modified updated parameter configurations from data2 over the top of existing historical text entries
            // Note: your dynamic Fill...TabAsync interaction methods must accommodate clearing/overwriting input fields smoothly
            await steps.FillGeneralTabAsync(data2);
            await steps.FillRNFormTabAsync(data2);

            // Commit modifications back to system draft repositories via the Save execution controls
            await steps.ClickSaveIncidentAsync();
            await Task.Delay(1000);

            // --- STEP 3: RELOAD BROWSER CONTEXT AND EXECUTE FINAL DATA INTEGRATION VALIDATION (Expect Dataset 2) ---
            await steps.ReloadPageAndNavigateAsync(draftUrl);

            // Verify the modified tab field data sets against the data2 reference criteria models
            await steps.VerifyDataRetainedAsync(data2.General);
            await steps.VerifyDataRetainedAsync(data2.RNSupervisor);

            // Verify the historical untouched tab data sheets against the data1 reference criteria models (or data2, since they remain identical)
            await steps.VerifyDataRetainedAsync(data1.Details);
            await steps.VerifyDataRetainedAsync(data1.State);
            await steps.VerifyDataRetainedAsync(data1.Medications);
            await steps.VerifyDataRetainedAsync(data1.Summary);
        }


        /// <summary>
        /// Data-driven navigation test: loops through individual tab names dynamically, 
        /// modifying a field entry to trigger form dirtiness states, and validates that 
        /// navigating away via the main menu successfully prompts custom Change Detection warning alert modals.
        /// </summary>
        /// <param name="tabName">The descriptive string text name of the destination tab layout sheet being evaluated.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [TestCase("General")]
        [TestCase("Details")]
        [TestCase("State")]
        [TestCase("Medication")]
        [TestCase("RN Supervisor Investigation Form")]
        [TestCase("Summary")]
        public async Task EveryTab_WhenDataChanged_ShouldTriggerChangeDetectionOnMenuLeave(string tabName)
        {
            // --- STEP 1: POPULATE INITIAL MASTER REFERENCE DATASET ---
            await steps.FillGeneralTabAsync(data);
            await steps.FillDetailsTabAsync(data);
            await steps.FillStateTabAsync(data);
            await steps.FillMedicationTabAsync(data);
            await steps.FillRNFormTabAsync(data);
            await steps.FillSummaryTabAsync(data);
            await steps.ClickCreateIncidentAsync();
            await Task.Delay(1000);
            string draftUrl = await steps.GetCurrentUrlAsync();
            await steps.ReloadPageAndNavigateAsync(draftUrl);

            // --- STEP 2: SWITCH TO SPECIFIC TESTCASE TAB AND FORCE FIELD DIRTINESS STATES ---
            await steps.SwitchToTab(tabName);
            await steps.ModifySingleFieldOnTabAsync(tabName);

            // --- STEP 3: ROUTE AWAY VIA MENU CONTROLS AND ASSERT DISCARD WARNING MODALS POPULATE ---
            await steps.LeavePageViaMenuAsync();
            await steps.VerifyUnsavedChangesAlertVisibleAsync(tabName);

            // Dismiss the active warnings and navigate back into the verified draft container safely
            await Page.GetByRole(AriaRole.Button, new() { Name = "Discard Changes" }).ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await steps.ReloadPageAndNavigateAsync(draftUrl);
        }

        [Test]
        [Description("Verify that deleting an incident transitions it to a read-only state with accurate deletion metadata.")]
        public async Task Incident_Deletion_ShouldBecomeReadOnlyWithMetadata()
        {
            // 1. Создаем инцидент (минимальный валидный набор)
            var minimalData = data with { General = data.General.GetOnlyRequiredFields() };
            await steps.FillGeneralTabAsync(minimalData);
            await steps.ClickCreateIncidentAsync();



            string incidentUrl = await steps.GetCurrentUrlAsync();

            // 2. Действие: Нажимаем кнопку Delete для самого инцидента
            await steps.CreatePage.GetButtonByText("Delete Incident").ClickAsync();

            // ВЗАИМОДЕЙСТВИЕ С ПОПАПОМ УДАЛЕНИЯ

            // 1. Изолируем контейнер самого активного попапа
            var popup = Page.Locator("div[role='dialog'], mat-dialog-container, dialog").First;
            await Assertions.Expect(popup).ToBeVisibleAsync();

            // 2. Ищем и кликаем на триггер выпадающего списка СТРОГО внутри этого попапа
            var dropdownInPopup = popup.Locator(".mat-mdc-select-trigger, mat-select").First;
            await dropdownInPopup.ClickAsync();

            // Даем оверлею списка открыться
            await Task.Delay(500);

            // 3. Выбираем первую опцию (опции Angular выносятся за пределы попапа в корень cdk-overlay)
            var reasonOption = Page.Locator("mat-mdc-option, mat-option, [role='option']").Nth(0);
            await reasonOption.ClickAsync();

            // Даем форме обновиться (Required field должно исчезнуть, кнопка OK стать активной)
            await Task.Delay(300);

            // 4. Кликаем по кнопке OK СТРОГО внутри этого попапа
            var okButton = popup.Locator("button").Filter(new() { HasText = "OK" }).First;
            await okButton.ClickAsync();

            // Небольшая пауза после выбора опции
            await Task.Delay(300);

            // 3. Форсированно возвращаемся по URL удаленного инцидента
            await steps.ReloadPageAndNavigateAsync(incidentUrl);

            // 5. АССЕРТЫ: Проверка состояния формы (Read-Only) и меток удаления

            // Проверяем, что поля формы заблокированы (Read-Only режим)
            var firstInputField = Page.Locator("mat-select, input, textarea").First;
            await Assertions.Expect(firstInputField).ToBeDisabledAsync();

            // Проверяем метаданные удаления на странице
            var deletedByLocator = Page.Locator("text=Deleted By:");
            var deletedAtLocator = Page.Locator("text=Deleted At:");

            await Assertions.Expect(deletedByLocator).ToBeVisibleAsync();
            await Assertions.Expect(deletedAtLocator).ToBeVisibleAsync();

            // Проверяем конкретный текст меток (как на вашем скриншоте)
            await Assertions.Expect(Page.Locator("body")).ToContainTextAsync("Deleted By: Test, Polly");
            await Assertions.Expect(Page.Locator("body")).ToContainTextAsync("Deleted At:");
        }

        [Test]
        [Description("Verify that creating a duplicate incident for the same resident at the same time is blocked by the system validation.")]
        public async Task Incident_DuplicateCreation_ShouldBeBlockedByValidationTable()
        {
            // Получаем точный индекс выбранного резидента
            int savedResidentIndex = await steps.GetSelectedResidentIndexAsync();

            // 1. Готовим данные для первого инцидента
            var now = DateTime.Now;
            var generatedTime = new TimeOnly(now.Hour, now.Minute);

            // 1. Готовим данные для первого инцидента с динамическим временем
            var incidentData = data with
            {
                General = data.General.GetOnlyRequiredFields() with
                {
                    time = generatedTime
                }
            };

            // Заполняем первый инцидент (выбирается случайный резидент)
            await steps.FillGeneralTabAsync(incidentData);


            string addUrl = await steps.GetCurrentUrlAsync();

            // Создаем первый инцидент
            await steps.ClickCreateIncidentAsync();
            await Task.Delay(1000);

            // Возвращаемся на пустую форму создания
            await steps.ReloadPageAndNavigateAsync(addUrl);

            // 2. Заполняем второй инцидент для ТОГО ЖЕ резидента по сохраненному индексу
            await steps.SelectResidentAsync(savedResidentIndex);
            await steps.FillGeneralTabAsync(incidentData);
            await steps.VerifyCreateButtonStateAsync(false);

            // 3. АССЕРТЫ: Проверяем появление таблицы дубликатов на экране
            var duplicatesPanel = Page.Locator("div.same-incidents-line");
            await Assertions.Expect(duplicatesPanel).ToBeVisibleAsync();

            var duplicateWarningText = duplicatesPanel.Locator("text=Duplicate date/time for resident");
            await Assertions.Expect(duplicateWarningText).ToBeVisibleAsync();
        }

        /// <summary>
        /// Verifies that entering a different time on the same date displays existing incidents in the daily panel without duplicate errors, allowing successful creation.
        /// </summary>
        [Test]
        [Description("Verify that creating a second incident for the same resident on the same day but at a different time shows the history panel without duplicate errors and allows saving.")]
        public async Task Incident_SameDayDifferentTime_ShouldShowPanelWithoutErrorsAndAllowCreation()
        {
            // 1. Генерируем базовое уникальное время для первого инцидента
            var now = DateTime.Now;
            var firstIncidentTime = new TimeOnly(now.Hour, now.Minute);

            // Готовим данные для первого инцидента
            var firstIncidentData = data with
            {
                General = data.General.GetOnlyRequiredFields() with
                {
                    time = firstIncidentTime
                }
            };

            // Заполняем и сохраняем первый инцидент
            await steps.FillGeneralTabAsync(firstIncidentData);
            int savedResidentIndex = await steps.GetSelectedResidentIndexAsync();
            string addUrl = await steps.GetCurrentUrlAsync();

            await steps.ClickCreateIncidentAsync();
            await Task.Delay(1000);

            // 2. Готовим данные для второго инцидента (сдвигаем время на -2 часа вперед, чтобы избежать дубликата)
            var secondIncidentTime = firstIncidentTime.AddHours(-2);
            var secondIncidentData = data with
            {
                General = data.General.GetOnlyRequiredFields() with
                {
                    time = secondIncidentTime
                }
            };

            // Возвращаемся на пустую форму создания
            await steps.ReloadPageAndNavigateAsync(addUrl);

            // Заполняем форму для ТОГО ЖЕ резидента, но с НОВЫМ временем
            await steps.SelectResidentAsync(savedResidentIndex);
            await steps.FillGeneralTabAsync(secondIncidentData);

            // 3. АССЕРТЫ: Проверяем панель инцидентов за этот день
            var incidentsPanel = Page.Locator("div.same-incidents-line");

            // Панель должна быть на экране, так как у резидента уже есть инцидент в эти сутки
            await Assertions.Expect(incidentsPanel).ToBeVisibleAsync();

            // КРИТИЧЕСКИЙ АССЕРТ: Красного сообщения об ошибке времени быть НЕ должно
            var duplicateWarningText = incidentsPanel.Locator("text=Duplicate date/time for resident");
            await Assertions.Expect(duplicateWarningText).Not.ToBeVisibleAsync();

            // Кнопка Create должна быть активна, так как время не пересекается
            await steps.VerifyCreateButtonStateAsync(shouldBeEnabled: true);

            // 4. ДЕЙСТВИЕ: Успешно сохраняем второй инцидент
            await steps.ClickCreateIncidentAsync();
            await Task.Delay(1000);
        }
    }
}