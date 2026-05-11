using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using NUnit.Framework;
using Serilog;
using static DetailsTab;
using static GeneralTab;
using static IncidentCreatePage;
using static IncidentDataFactory;
using static StateTab;



    namespace CareAdminTestProject.Incidents.IncidentDetails.Steps
    {
        public class IncidentDetailsSteps
        {
            private readonly IPage _page;
            private readonly IncidentCreatePage _createPage;
            private readonly IncidentTrackerPage _trackerPage;
            public IncidentCreatePage CreatePage => _createPage;
            string fileName;

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
            public async Task SwitchToTab(string tabName)
            {
                await _createPage.ClickTabAsync(tabName, new() { Timeout = 30000 });
                Log.Debug($"Switched to tab {tabName}");
            }
        public async Task FillGeneralTabAsync(IncidentTestData data)
            {
                await _createPage.General.FillBasicInfoAsync(data.General);
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
            public async Task UploadAttachmentTabAsync(string categoryName, string? note = null)
            {
                await _createPage.ClickTabAsync("Attachments");
                await _createPage.Attachments.UploadAttachmentAsync(fileName);
                await _createPage.Attachments.AssignCategoriesToAllPagesAsync(categoryName, note);
                await _createPage.Attachments.VerifyAttachmentIsDisplayedAsync(categoryName);
                await _page.MakeScreenshotAsync("Attachment_Filled");
                Log.Information("Attachment file attached");
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

                // 1. Ждем появления точки перед началом (если она вдруг не успела появиться от прошлого шага)
                await _createPage.State.GeneralPointLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible });

                // 2. ВКЛЮЧАЕМ поле
                await Action.Invoke();

                // 3. УМНОЕ ОЖИДАНИЕ: Ждем, пока точка ИСЧЕЗНЕТ
                // Тест пойдет дальше мгновенно, как только точка пропадет из DOM или станет невидимой
                await _createPage.State.GeneralPointLocator.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

                // Дополнительная проверка для надежности
                Assert.That(await _createPage.State.IsGeneralStatePointVisibleAsync(), Is.False,
                    $"Point did NOT disappear after filling {fieldName}");

                // 4. СБРОС (выключаем поле)
                await Reset.Invoke();

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
