using CareAdminTestProject.Incidents.IncidentDetails.Steps;
using Microsoft.Playwright;
using CareAdminTestProject.Common;
using static IncidentDataFactory;
using Log = CareAdminTestProject.Common.TestLog;
using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using CareAdminTestProject.Incidents.CommonIncidentTests;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    /// <summary>
    /// Serves as the functional base suite for incident automation tests.
    /// Orchestrates standard multi-tab setups, shared pre-requisites, and test execution workflows.
    /// </summary>
    [TestFixture]
    public class BaseIncidentTests : BaseIncidentHubTests
    {
        /// <summary> The main high-level BDD step layer manager scoped to the current test context execution loop. </summary>
        public IncidentDetailsSteps steps;

        /// <summary> The complex reference test data payload record model used to populate forms during execution. </summary>
        public IncidentTestData data;

        /// <summary> Biographical and room placement location constraints captured for the target resident profile. </summary>
        public IncidentCreatePage.ResidentInfo resident;

        private static int _globalResidentCounter = 0;

        /// <summary>
        /// Executes foundational test pre-requisites before each automated test case runs, 
        /// enforcing target facility selections, opening new blank forms, and caching model references.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SetUp]
        public async Task Setup()
        {
            // 1. Вызываем базовый выбор Facility из BaseIncidentHubTests
            await BaseHubSetup();
            steps = new IncidentDetailsSteps(Page);

            var tabsToReset = new[]
            {
                new { Name = "General", Count = (int?)null },
                new { Name = "Details", Count = (int?)null },
                new { Name = "State", Count = (int?)null },
                new { Name = "Medication", Count = (int?)null },
                new { Name = "RN Supervisor Investigation Form", Count = (int?)null },
                // Помним, что Summary выключить нельзя, но включить обратно (на всякий случай) бэкенд позволит
                new { Name = "Summary", Count = (int?)null },
                new { Name = "Attachments", Count = (int?)1 } // Передаем 5 для аттачей
            };

            // Цикл перебора и сброса конфигурации
            foreach (var tab in tabsToReset)
            {
                // Если Count заполнен, он передастся в attachmentCount, иначе уйдет null (дефолтное значение метода)
                await steps.UpdateIncidentConfigurationAsync(
                    sectionCode: tab.Name,
                    isEnabled: true,
                    attachmentCount: tab.Count
                );
            }


            // Instantiating the step runner, mapping it to the thread-isolated Page instance
            await steps.NavigateToTrackerViaMenu();
            await steps.OpenNewIncidentAsync();



            // ДИНАМИЧЕСКИЙ РАСЧЕТ ИНДЕКСА РЕЗИДЕНТА ДЛЯ ПОТОКА:
            // Берем хэш-код текущего потока (он уникален для каждого параллельного воркера)
            int threadId = Environment.CurrentManagedThreadId;

            // Оператор % гарантирует, что индекс ВСЕГДА будет в диапазоне от 0 до 4, независимо от ID потока
            int currentTestRunIndex = System.Threading.Interlocked.Increment(ref _globalResidentCounter);

            // Используем оператор %, чтобы если тестов станет больше 45, индексы плавно пошли по второму кругу
            // Начинаем с индекса 1 (пропуская 0, если первый элемент в дропдауне — это какой-то пустой плейсхолдер)
            int residentIndex = (currentTestRunIndex % 45) + 1;

            Log.Debug($"[PARALLEL ENGINE] Test '{TestContext.CurrentContext.Test.Name}' triggered. " +
                         $"Global Run Number: {currentTestRunIndex}. Selecting unique Resident Index: {residentIndex}");
            // =========================================================================


            Log.Debug($"[THREAD PARALLEL] Thread ID {threadId} evaluates to Resident Index: {residentIndex}");

            resident = await steps.SelectResidentAsync(residentIndex);
            data = IncidentDataFactory.CreateDefaultFall(resident);
        }

    }
}