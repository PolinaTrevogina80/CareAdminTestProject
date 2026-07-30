using CareAdminTestProject.Common;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;

namespace CareAdminTestProject.Incidents.IncidentTracker.Tests
{
    [TestFixture]
    public class TrackerDetailedViewTests : BaseTrackerTests
    {

        [Test]
        public async Task DetailedView_DynamicSearchResidentOnTheFly_ShouldFilterGridImmediately()
        {
            // 1. Ждем загрузку страницы трекера
            await trackerPage.WaitForPageLoadAsync();

            // 2. Динамически берём имя 3-го резидента из списка
            string dynamicResidentName = await trackerSteps.GrabResidentNameFromRowAsync(targetRowIndex: 2, residentColumnIndex: 0);

            // Берем первые 4 буквы для проверки фильтрации ("ALBI")
            string searchCriteria = dynamicResidentName.Length > 4 ? dynamicResidentName.Substring(0, 4) : dynamicResidentName;

            // 3. Ищем побуквенно
            await trackerSteps.SearchResidentLetterByLetterAsync(searchCriteria);

            // 4. Финальная проверка: в первой строчке отфильтрованного грида действительно наш резидент
            var finalRowCount = await trackerPage.DetailedView.DataRows.CountAsync();
            Assert.That(finalRowCount, Is.GreaterThan(0), "Грид оказался полностью пуст после фильтрации");

            await Assertions.Expect(trackerPage.DetailedView.DataRows.First).ToContainTextAsync(dynamicResidentName);
        }

        [Test]
        public async Task DetailedView_DynamicGroupByResident_ShouldMatchTotalCountCounter()
        {
            // 1. Ждем загрузку страницы трекера
            await trackerPage.WaitForPageLoadAsync();

            // 2. Запоминаем имя резидента из 3-й строки исходного списка до группировки
            string dynamicResidentName = await trackerSteps.GrabResidentNameFromRowAsync(targetRowIndex: 2, residentColumnIndex: 0);

            // 3. Включаем группировку
            await trackerSteps.SetGroupByResidentAsync(true);

            // 4. Раскрываем группу динамически полученного резидента
            await trackerSteps.ExpandResidentGroupAsync(dynamicResidentName);

            // 5. Проверяем каунтер "Total #"
            await trackerSteps.VerifyResidentTotalIncidentsCountAsync(dynamicResidentName);
        }

        [Test]
        public async Task DetailedView_DynamicGroupByResident_ShouldCreateSingleUniqueGroup()
        {
            // 1. Дожидаемся инициализации страницы (откроется чистый трекер)
            await trackerPage.WaitForPageLoadAsync();

            // 2. Динамически берём имя 3-го резидента из исходной таблицы (пока группировка выключена)
            string dynamicResidentName = await trackerSteps.GrabResidentNameFromRowAsync(targetRowIndex: 2, residentColumnIndex: 0);

            // 3. Включаем группировку по резиденту
            await trackerSteps.SetGroupByResidentAsync(true);

            // 4. Проверяем, что этот резидент сгруппирован в единственную строку и у него корректный Total #
            await trackerSteps.VerifyGroupedResidentIsUniqueAndMatchesCountAsync(dynamicResidentName);

            // 5. Кликаем по стрелочке раскрытия (теперь локатор со скриншота отработает без таймаута)
            Log.Information($"[ACTION] Пробуем раскрыть группу резидента: {dynamicResidentName}");
            await trackerPage.DetailedView.GroupExpandArrow(dynamicResidentName).ClickAsync();

            await trackerPage.DetailedView.WaitForInterfaceDebounceAsync();
        }

        [Test]
        public async Task DetailedView_FilterByDate_ShouldShowTargetIncident()
        {
            // 1. Ждем загрузку страницы трекера (откроется исходный грид)
            await trackerPage.WaitForPageLoadAsync();

            // 2. Выбираем 3-й инцидент (индекс 2) и берем Дату из колонки 2, Время из колонки 3
            var targetIncident = await trackerSteps.GrabIncidentDateTimeAsync(targetRowIndex: 2, dateColumn: 2, timeColumn: 3);

            // 3. Устанавливаем Start Date и End Date равными дате этого инцидента
            await trackerSteps.FilterByDateRangeAsync(targetIncident.Date, targetIncident.Date);

            // 4. Проверяем, что в отфильтрованном списке есть хоть какие-то данные
            var finalRowsCount = await trackerPage.DetailedView.DataRows.CountAsync();
            Assert.That(finalRowsCount, Is.GreaterThan(0), "Грид оказался пуст после фильтрации по дате");

            // ИСПРАВЛЕНО: Стягиваем текст ВСЕГО тела таблицы грида
            var wholeGridText = await trackerPage.DetailedView.DataRows.Locator("..").InnerTextAsync();

            // Проверяем, что наше целевое время (11:18 AM) есть среди отфильтрованных данных
            Assert.That(wholeGridText.Contains(targetIncident.Time), Is.True,
                $"Целевой инцидент со временем {targetIncident.Time} не найден в отфильтрованном списке дат. Данные таблицы:\n{wholeGridText}");

            Log.Information($"[SUCCESS] Фильтрация по датам успешно выполнена. Инцидент со временем {targetIncident.Time} присутствует на странице.");
        }
    }
}
