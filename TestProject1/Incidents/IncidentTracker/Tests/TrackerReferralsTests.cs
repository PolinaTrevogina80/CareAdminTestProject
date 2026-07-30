using CareAdminTestProject.Incidents.IncidentTracker.PageConfig.Components;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CareAdminTestProject.Incidents.IncidentTracker.Tests
{
    [TestFixture]
    public class TrackerReferralsTests : BaseTrackerTests
    {
        private ReferralDataFactory _dataFactory;
        private string _targetResidentName;

        [SetUp]
        public async Task ReferralsSetup()
        {
            _dataFactory = new ReferralDataFactory(Page);

            // 1. Генерируем полную связку данных через API
            _targetResidentName = await _dataFactory.PrepareReferralDataAsync();

            // 2. Открываем вкладку рефералов в интерфейсе
            await trackerSteps.SwitchToReferralsViewAsync();
        }

        //[Test]
        //public async Task ReferralsView_CreateReferralViaApi_ShouldDisplayInGrid()
        //{
        //    // 1. Включаем чекбокс отображения рефералов без инцидента
        //    await trackerPage.Referral.DisplayWithoutMatchingCheckbox.CheckAsync();
        //    await trackerPage.WaitForPageLoadAsync();

        //    // 2. Проверяем, что в таблице появились строки
        //    var rowCount = await trackerPage.Referral.DataRows.CountAsync();
        //    Assert.That(rowCount, Is.GreaterThan(0), "Грид рефералов остался пустым после генерации данных!");

        //    // 3. Валидируем, что наш созданный резидент отображается в списке рефералов
        //    var wholeGridText = await trackerPage.Referral.DataRows.Locator("..").InnerTextAsync();
        //    Assert.That(wholeGridText.Contains(_targetResidentName), Is.True,
        //        $"Сгенерированный резидент '{_targetResidentName}' не найден в таблице рефералов.");

        //    Log.Information($"[SUCCESS] Реферал для резидента {_targetResidentName} успешно провалидирован в UI!");
        //}
    }
}