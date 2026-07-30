using CareAdminTestProject.Common;
using CareAdminTestProject.Incidents.CommonIncidentTests;
using System;
using System.Collections.Generic;
using System.Text;

namespace CareAdminTestProject.Incidents.IncidentTracker.Tests
{
    public class BaseTrackerTests : BaseIncidentHubTests
    {

        public IncidentTrackerPage trackerPage;
        public IncidentTrackerSteps trackerSteps;

        [SetUp]
        public async Task TestSetup() // Меняем void на async Task, чтобы делать переходы
        {
            trackerPage = new IncidentTrackerPage(Page);
            trackerSteps = new IncidentTrackerSteps(Page);

            // 1. Вызываем базовый выбор Facility (он приедет из родительского BaseIncidentHubTests)
            await BaseHubSetup();

            // 2. Переходим на страницу трекера через меню
            await trackerSteps.NavigateToTrackerViaMenu();

            // 3. Ждем, пока отработает стартовый loader-wrapper страницы трекера
            await trackerPage.WaitForPageLoadAsync();
        }
    }
}
