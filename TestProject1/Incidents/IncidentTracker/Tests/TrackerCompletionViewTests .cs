using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace CareAdminTestProject.Incidents.IncidentTracker.Tests
{
    [TestFixture]
    public class TrackerCompletionViewTests : BaseTrackerTests
    {
        [SetUp]
        public async Task CompletionSetup()
        {
            // Перед каждым тестом гарантированно переключаемся на вкладку Completion View
            await trackerSteps.SwitchToCompletionViewAsync();
        }

        [Test]
        public async Task CompletionView_DynamicSearchResident_ShouldFilterGrid()
        {
            // 1. Динамически берем имя резидента из 1-й строки таблицы (индекс 0, колонка 0)
            var firstRow = trackerPage.CompletionView.DataRows.First;
            string fullResidentName = (await firstRow.Locator("td").First.InnerTextAsync()).Trim();

            // Берем только фамилию до запятой для проверки частичного поиска
            string searchCriteria = fullResidentName.Split(',')[0].Trim();

            // 2. Ищем на лету по фамилии
            await trackerSteps.SearchResidentInCompletionViewLetterByLetterAsync(searchCriteria);

            // 3. Проверяем, что в отфильтрованной таблице остался этот резидент
            var filteredText = await trackerPage.CompletionView.DataRows.First.InnerTextAsync();
            Assert.That(filteredText.Contains(searchCriteria), Is.True,
                $"Резидент {searchCriteria} не найден в первой строке грида после фильтрации.");
        }

        [Test]
        public async Task CompletionView_FilterByDate_ShouldShowTargetIncident()
        {
            // 1. Берем Дату и Время из 3-й строки таблицы (индекс 2)
            var targetIncident = await trackerSteps.GrabCompletionIncidentDateTimeAsync(targetRowIndex: 2);

            // 2. Заполняем Start Date и End Date в шапке этой датой и жмем GO
            await trackerSteps.FilterByDateRangeAsync(targetIncident.Date, targetIncident.Date);

            // 3. Так как фильтр дат перезагружает страницу, нам нужно снова вернуться на вкладку Completion View
            await trackerSteps.SwitchToCompletionViewAsync();

            // 4. Проверяем, что целевой инцидент со своим временем присутствует в результатах таблицы
            var wholeGridText = await trackerPage.CompletionView.DataRows.Locator("..").InnerTextAsync();
            Assert.That(wholeGridText.Contains(targetIncident.Time), Is.True,
                $"Инцидент со временем {targetIncident.Time} исчез после фильтрации по дате {targetIncident.Date}.");

            Log.Information($"[SUCCESS] Фильтрация по датам на Completion View успешно подтверждена для времени {targetIncident.Time}.");
        }
    }
}
