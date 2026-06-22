using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    internal class IncidentRNFormTests : BaseIncidentTests
    {
        string tabName = "RN Supervisor Investigation Form";

        [Test]
        public async Task RNInvestigation_ProgressBarAndRedDot_Lifecycle()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            await steps.SwitchToTab(tabName);

            // Получаем инстанс таба через хелпер/свойство (предполагаем, что он доступен в шагах)
            var rnTab = steps.CreatePage.RNSupervisor;
            var testData = data.RNSupervisor; // Твои тестовые данные RNSupervisorTabInfo

            // Act & Assert
            await steps.VerifyRedDotTab(tabName, true);

            // Запускаем заполнение вопросов с хуком на каждый шаг
            await rnTab.FillQuestionsAsync(testData, async (stepNumber) =>
            {
                // Вычисляем ожидаемый процент
                double expectedPercentage = (double)stepNumber / 28 * 100;

                // ЗАМЕНА: Math.Round вместо Math.Floor для корректного математического округления
                int roundedPercentage = (int)Math.Round(expectedPercentage, MidpointRounding.AwayFromZero);

                // Метод проверки текста процента
                await rnTab.VerifyProgressBarPercentageAsync($"{roundedPercentage}%");

                if (stepNumber < 28)
                {
                    // До 28-го шага точка должна гореть
                    await steps.VerifyRedDotTab(tabName, true);
                }
            });

            // После заполнения 28-го шага (выход из цикла)
            await steps.VerifyRedDotTab(tabName, false);
            await rnTab.VerifyProgressBarPercentageAsync("100%");
        }

        [Test]
        public async Task RNInvestigation_Overview_FullCompletionVerification()
        {
            // Arrange
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tabName);
            var rnTab = steps.CreatePage.RNSupervisor;
            var expectedData = data.RNSupervisor; // Твоя полная модель с 28 вопросами

            // Act
            await rnTab.FillQuestionsAsync(expectedData);
            await rnTab.ClickToOverviewAsync();

            // Assert
            // Передаем всю структуру данных для построчной сверки таблицы
            await rnTab.VerifyOverviewAllQuestionsAsync(expectedData);
        }

        [Test]
        public async Task RNInvestigation_Overview_PartialCompletionGapsVerification()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            await steps.SwitchToTab(tabName);
            var rnTab = steps.CreatePage.RNSupervisor;

            // 1. Просто берем первые 5 заполненных вопросов.
            var partialQuestions = data.RNSupervisor.Questions.Take(5).ToList();

            // 2. Мутируем данные через 'with', отдавая ровно 5 элементов
            data = data with
            {
                RNSupervisor = data.RNSupervisor with
                {
                    Questions = partialQuestions.AsReadOnly()
                }
            };


            // 4. Act: Передаем измененный объект RNSupervisor, а не список вопросов
            await rnTab.FillQuestionsAsync(data.RNSupervisor);
            await rnTab.ClickToOverviewAsync();

            // Assert
            // 1. Проверяем базовые текстовые шаги (Шаг 1 и Шаг 2)
            await rnTab.VerifyOverviewAllQuestionsAsync(data.RNSupervisor);
        }

        [Test]
        public async Task RNInvestigation_Overview_EmptyFormGapsVerification()
        {
            // Arrange
            // Заполняем обязательную общую вкладку, чтобы получить доступ к форме исследования
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tabName);
            var rnTab = steps.CreatePage.RNSupervisor;

            // Создаем пустой объект данных, где все списки и строки пустые
            var emptyData = new RNSupervisorTabInfo(
                Locations: new List<string>().AsReadOnly(),
                LastSeen: new RNSupervisorTabInfo.LastSeenInfo(default, ""),
                DescribeExactly: new RNSupervisorTabInfo.DescribeExactlyInfo(""),
                Questions: new List<RNSupervisorTabInfo.QuestionWithDetails>().AsReadOnly() // 0 вопросов
            );

            // Act
            // Сразу кликаем переход на экран обзора, минуя пошаговое заполнение
            await rnTab.ClickToOverviewAsync();

            // Assert
            // Наш универсальный метод автоматически проверит пустоту для всех 28 шагов формы
            await rnTab.VerifyOverviewAllQuestionsAsync(emptyData);
        }


        [Test]
        public async Task RNInvestigation_FieldLogic_And_Persistence()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            await steps.SwitchToTab(tabName);
            var rnTab = steps.CreatePage.RNSupervisor;

            // 1. Тестируем ручной ввод времени (находясь на Шаге 1)
            var manualTime = new TimeOnly(15, 15); // 03:15 PM
            await rnTab.TypeTimeManuallyAsync(manualTime);
            await rnTab.VerifyTimeFieldValueAsync(manualTime);

            // 2. Тестируем перезапись времени через Picker
            var pickerTime = new TimeOnly(16, 30); // 04:30 PM
            await rnTab.SelectTimeInPickerAsync("answerTime", pickerTime);
            await rnTab.VerifyTimeFieldValueAsync(pickerTime);

            // 3. Тестируем Persistence (Сохранение данных при навигации)
            var sampleDetails = "The resident was sitting in a wheelchair near the room entrance.";
            // Используем FillFormStepAsync, но переопределяем вызов навигации, чтобы остаться на месте
            // Для этого передаем пустой колбэк, но сам метод в конце нажмет GoToNextStepAsync() и переведет нас на Шаг 2
            await rnTab.FillFormStepAsync(1, async () =>
            {
                var detailsArea = rnTab.Page.GetByPlaceholder("Enter details");
                await detailsArea.FillAsync(sampleDetails);
            });
            // МЫ СЕЙЧАС НАХОДИМСЯ НА ШАГЕ 2 (так как FillFormStepAsync автоматом кликнул Next)

            // Возвращаемся назад на Шаг 1 вручную
            await rnTab.GoBackStepAsync();

            // Assert: Проверяем, что введенный текст и время не сбросились
            // Передаем advanceToNextStep: false, чтобы после проверки робот остался стоять на Шаге 1
            await rnTab.VerifyFormStepAsync(1, async () =>
            {
                var detailsArea = rnTab.Page.GetByPlaceholder("Enter details");
                await Assertions.Expect(detailsArea).ToHaveValueAsync(sampleDetails);

                // Попутно проверяем и время
                await rnTab.VerifyTimeFieldValueAsync(pickerTime);
            }, advanceToNextStep: false);
        }


        [Test]
        public async Task RNInvestigation_MultiSelect_AddAndRemove()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            await steps.SwitchToTab(tabName);
            var rnTab = steps.CreatePage.RNSupervisor;

            // 1. Добавление нескольких зон
            var locationsToSelect = new List<string> { "Bathroom: Resident", "Hallway" };
            await rnTab.SelectLocationsAsync(locationsToSelect);

            // Assert-шаг: Проверяем, что обе зоны успешно добавились и отображаются на UI
            await rnTab.VerifySelectedLocationsAsync(locationsToSelect);

            // 2. Удаление одной зоны
            await rnTab.RemoveLocationAsync("Bathroom: Resident");

            // Assert-шаг: Проверяем, что Bathroom исчез, а Hallway остался на месте
            var expectedAfterRemove = new List<string> { "Hallway" };
            await rnTab.VerifySelectedLocationsAsync(expectedAfterRemove);
        }

        [Test]
        public async Task RNInvestigation_MultiSelect_SaveAndReopenPersistence()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            await steps.SwitchToTab(tabName);
            var rnTab = steps.CreatePage.RNSupervisor;

            // 1. Первый набор локаций
            var locationSet = new List<string> { "Bathroom: Resident", "Day Room" };
            await rnTab.SelectLocationsAsync(locationSet);

            // Проверяем "живьем" на форме, что они объединились, а не перезаписались
            await rnTab.VerifySelectedLocationsAsync(locationSet);

            // 3. Сохранение инцидента
            await steps.ClickCreateIncidentAsync();
            await Task.Delay(1000);
            string draftUrl = await steps.GetCurrentUrlAsync();
            await steps.ReloadPageAndNavigateAsync(draftUrl);

            // Возвращаемся на вкладку опросника
            await steps.SwitchToTab(tabName);

            // 5. Assert: Проверяем, что после вычитки из БД прилетели ВСЕ выбранные локации
            await rnTab.VerifySelectedLocationsAsync(locationSet);
        }

        [Test]
        public async Task RNInvestigation_MultiSelect_AddAfterSaving()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);
            await steps.SwitchToTab(tabName);

            var rnTab = steps.CreatePage.RNSupervisor;

            // 1. Первый набор локаций
            var firstSet = new List<string> { "Bathroom: Resident" };
            Console.WriteLine("[TEST] Добавляем первый набор локаций...");
            await rnTab.SelectLocationsAsync(firstSet);

            // 3. Сохранение инцидента
            await steps.ClickCreateIncidentAsync();
            await Task.Delay(1000);
            string incidentUrl = await steps.GetCurrentUrlAsync();
            await steps.ReloadPageAndNavigateAsync(incidentUrl);

            // Возвращаемся на вкладку опросника
            await steps.SwitchToTab(tabName); 
            
            // 2. Второй набор локаций (вызывается ЧЕСТНО второй раз)
            var secondSet = new List<string> { "Hallway", "Courtyard" };
            Console.WriteLine("[TEST] Открываем пикер повторно и добавляем второй набор...");
            await rnTab.SelectLocationsAsync(secondSet);

            // 3. Сохранение инцидента
            await steps.ClickSaveIncidentAsync();
            await Task.Delay(1000);
            await steps.ReloadPageAndNavigateAsync(incidentUrl);

            // Полный ожидаемый список
            var fullExpectedSet = firstSet.Concat(secondSet).ToList();

            // Проверяем накопление на UI
            await rnTab.VerifySelectedLocationsAsync(fullExpectedSet);



            // Финальная проверка сохранения структуры в базе данных
            await rnTab.VerifySelectedLocationsAsync(fullExpectedSet);
            Console.WriteLine("[TEST] Успех! Накопительное сохранение мультиселекта работает корректно.");

        }

        [Test]
        public async Task RNInvestigation_Pagination_Boundaries()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tabName);
            var rnTab = steps.CreatePage.RNSupervisor;

            // Мы на 1-м шаге. Проверяем, что "Назад" задизейблена
            await rnTab.VerifyBackButtonStateAsync(isEnabled: false);
            await rnTab.VerifyNextButtonStateAsync(isEnabled: true);

            // Прокликиваем/заполняем форму до самого конца (28 шаг)
            await rnTab.FillQuestionsAsync(data.RNSupervisor);
            await rnTab.VerifyCurrentStepNumberAsync(28);

            // Мы на 28-м шаге. Проверяем, что "Вперед" задизейблена
            await rnTab.VerifyNextButtonStateAsync(isEnabled: false);
            await rnTab.VerifyBackButtonStateAsync(isEnabled: true);
        }
    }
}
