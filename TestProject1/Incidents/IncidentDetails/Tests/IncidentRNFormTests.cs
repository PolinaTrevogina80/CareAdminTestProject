using System;
using System.Collections.Generic;
using System.Text;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    internal class IncidentRNFormTests : BaseIncidentTests
    {

        [Test]
        public async Task RNInvestigation_ProgressBarAndRedDot_Lifecycle()
        {
            // Arrange
            await steps.FillGeneralTabAsync(data);
            var tabName = "RN Supervisor Investigation Form";
            await steps.SwitchToTab(tabName);

            // Получаем инстанс таба через хелпер/свойство (предполагаем, что он доступен в шагах)
            var rnTab = steps.CreatePage.RNSupervisor;
            var testData = data.RNSupervisor; // Твои тестовые данные RNSupervisorTabInfo

            // Act & Assert
            await steps.VerifyRedDotTab(tabName, true);

            // Запускаем заполнение вопросов с хуком на каждый шаг
            await rnTab.FillQuestionsAsync(testData, async (stepNumber) =>
            {
                // Вычисляем ожидаемый процент. Шаги 4-28 (всего 25 вопросов) + первые поля.
                // Или завязываемся на логику шагов (всего 28 шагов пагинации).
                double expectedPercentage = (double)stepNumber / 28 * 100;

                // Метод проверки текста процента (например, "100%")
                await rnTab.VerifyProgressBarPercentageAsync($"{Math.Floor(expectedPercentage)}%");

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
            await steps.SwitchToTab("RN Supervisor Investigation Form");
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
            // Arrange
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab("RN Supervisor Investigation Form");
            var rnTab = steps.CreatePage.RNSupervisor;

            // 1. Берем первые 5 заполненных вопросов
            var partialQuestions = data.RNSupervisor.Questions.Take(5).ToList();

            // 2. Дозаполняем оставшиеся шаги до максимума (всего в цикле должно быть 26 вопросов)
            while (partialQuestions.Count < 26)
            {
                partialQuestions.Add(new RNSupervisorTabInfo.QuestionWithDetails(false, ""));
            }

            // 3. Мутируем данные через 'with'
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


        //[Test]
        //public async Task RNInvestigation_FieldLogic_And_Persistence()
        //{
        //    await steps.FillGeneralTabAsync(data);
        //    await steps.SwitchToTab("RN Supervisor Investigation Form");
        //    var rnTab = steps.RNSupervisorTab;

        //    // 1. Проверка Time Picker
        //    var timeToType = new TimeOnly(15, 15); // 03:15 PM
        //    await rnTab.TypeTimeManuallyAsync(timeToType);
        //    await rnTab.VerifyTimeFieldValueAsync(timeToType);

        //    // Проверка выбора через иконку часов (открытие поп-апа и выбор)
        //    await rnTab.SelectTimeViaClockIconAsync(new TimeOnly(16, 30));

        //    // 2. Проверка Persistence (Сохранение при навигации)
        //    // Шаг 1 (на скриншоте) — вводим текст
        //    var sampleDetails = "Sitting in wheelchair in room";
        //    await rnTab.FillLastSeenDetailsAsync(sampleDetails);

        //    // Идем вперед, затем возвращаемся
        //    await rnTab.GoToNextStepAsync(); // На шаг 2
        //    await rnTab.GoBackStepAsync();   // Назад на шаг 1

        //    // Проверяем, что текст остался на месте
        //    await rnTab.VerifyLastSeenDetailsValueAsync(sampleDetails);
        //}
        //[Test]
        //public async Task RNInvestigation_MultiSelect_AddAndRemove()
        //{
        //    await steps.FillGeneralTabAsync(data);
        //    await steps.SwitchToTab("RN Supervisor Investigation Form");
        //    var rnTab = steps.RNSupervisorTab;

        //    // 1. Добавление нескольких зон
        //    var locationsToSelect = new List<string> { "Bathroom", "Hallway" };
        //    await rnTab.SelectLocationsAsync(locationsToSelect);

        //    // Проверяем, что они выбрались на UI
        //    var selected = await rnTab.GetSelectedLocationsAsync();
        //    Assert.That(selected, Is.EquivalentTo(locationsToSelect));

        //    // 2. Удаление одной зоны
        //    await rnTab.RemoveLocationAsync("Bathroom");

        //    // Проверяем, что остался только Hallway и валидация не сломалась
        //    var updatedSelected = await rnTab.GetSelectedLocationsAsync();
        //    Assert.That(updatedSelected, Does.Contain("Hallway"));
        //    Assert.That(updatedSelected, Does.Not.Contain("Bathroom"));

        //    // Проверяем, что поле не подсвечено ошибкой
        //    await rnTab.VerifyLocationFieldValidationErrorVisibleAsync(false);
        //}
        //[Test]
        //public async Task RNInvestigation_Pagination_Boundaries()
        //{
        //    await steps.FillGeneralTabAsync(data);
        //    await steps.SwitchToTab("RN Supervisor Investigation Form");
        //    var rnTab = steps.RNSupervisorTab;

        //    // Мы на 1-м шаге. Проверяем, что "Назад" задизейблена
        //    await rnTab.VerifyBackButtonStateAsync(isEnabled: false);
        //    await rnTab.VerifyNextButtonStateAsync(isEnabled: true);

        //    // Прокликиваем/заполняем форму до самого конца (28 шаг)
        //    await rnTab.FillQuestionsAsync(data.RNSupervisor);
        //    await rnTab.VerifyCurrentStepNumberAsync(28);

        //    // Мы на 28-м шаге. Проверяем, что "Вперед" задизейблена
        //    await rnTab.VerifyNextButtonStateAsync(isEnabled: false);
        //    await rnTab.VerifyBackButtonStateAsync(isEnabled: true);
    }

}
