using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    internal class IncidentDetailsStateTests : BaseIncidentTests
    {

        /// <summary>
        /// Verified that the All Diagnoses container correctly loads and displays 
        /// resident-specific diagnoses data on the Details tab when available.
        /// </summary>
        [Test]
        public async Task DetailsTabAllDiagnosesShouldLoadCorrectly()
        {
            var tab = "Details";

            // Переходим на нужную вкладку
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);

            // Изолированная проверка диагнозов
            // Метод внутри обрабатывает как наличие списка, так и его отсутствие (data.Diagnoses)
            await steps.VerifyResidentDiagnosesLoadedAsync(data.Details.AllDiagnoses);
        }

        /// <summary>
        /// Validates that activating the First Aid toggle dynamically enforces 
        /// the requirement indicator (red dot) for the adjacent Describe field.
        /// </summary>
        [Test]
        public async Task DetailsTab_FirstAidToggle_ShouldTriggerDescribeFieldRequirement()
        {
            var tab = "Details";
            data = data with
            {
                Details = data.Details with { FirstAidDescribe = "" }
            };

            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);
            await steps.FillDetailsTabAsync(data);

            // Шаг 1: Включаем тумблер первой помощи (Yes) -> проверяем, что красная точка ПОЯВИЛАСЬ
            await steps.SwitchFirstAid(true);
            await steps.VerifyDescribeFieldRedDotStateAsync(true);

            // Шаг 2: Выключаем тумблер первой помощи (No) -> проверяем, что красной точки НЕТ
            await steps.SwitchFirstAid(false);
            await steps.VerifyDescribeFieldRedDotStateAsync(false);

            // Шаг 3: Включаем обратно (Yes) -> проверяем, что красная точка СНОВА НА МЕСТЕ
            await steps.SwitchFirstAid(true);
            await steps.VerifyDescribeFieldRedDotStateAsync(true);
        }

        /// <summary>
        /// Validates that the input field below 'Other Type of Alarm' checkbox 
        /// becomes active and editable strictly when the checkbox is checked.
        /// </summary>
        [Test]
        public async Task StateTab_OtherTypeOfAlarmCheckbox_ShouldToggleInputFieldActivation()
        {
            var tab = "State";

            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);

            // Шаг 2: Включаем чекбокс -> проверяем, что поле активировалось (Enabled)
            await steps.CreatePage.State.SetCheckboxAsync("Other Type of Alarm",true);
            await steps.VerifyOtherAlarmInputFieldStateAsync(shouldBeActive: true);

            // Шаг 1: По умолчанию чекбокс снят -> проверяем, что поле заблокировано (Disabled)
            await steps.CreatePage.State.SetCheckboxAsync("Other Type of Alarm", false);
            await steps.VerifyOtherAlarmInputFieldStateAsync(shouldBeActive: false);

            // Шаг 3: Выключаем обратно -> проверяем, что поле снова заблокировано
            await steps.CreatePage.State.SetCheckboxAsync("Other Type of Alarm", true);
            await steps.VerifyOtherAlarmInputFieldStateAsync(shouldBeActive: true);
        }
    }
}
