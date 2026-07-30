using Microsoft.Playwright;
using System.Threading.Tasks;
using Log = CareAdminTestProject.Common.TestLog;

namespace CareAdminTestProject.Incidents.IncidentTracker.PageConfig.Components
{
    // Наследуемся напрямую от главной страницы трекера, как и в прошлых табах!
    public class ReferralsTab : IncidentTrackerPage
    {
        public ReferralsTab(IPage page) : base(page)
        {
        }

        // Кнопка переключения на вкладку Referrals
        public ILocator TabButton =>
            _page.Locator("a.child-nav-item").GetByText("Incident Referrals", new() { Exact = false });

        // Тот самый специфичный чекбокс из твоего плана тестов
        public ILocator DisplayWithoutMatchingCheckbox =>
            _page.GetByLabel("Display Referrals Without Matching Incident", new() { Exact = false });

        // Таблица рефералов
        private ILocator Grid => _page.Locator("kendo-grid, .k-grid, table").First;
        public ILocator DataRows => Grid.Locator("tbody tr");

        /// <summary>
        /// Переключается на вкладку Incident Referrals и дожидается загрузки данных.
        /// </summary>
        public async Task OpenAsync()
        {
            Log.Debug("[NAVIGATION] Кликаем по вкладке Incident Referrals...");
            await TabButton.ClickAsync();

            // Заложим небольшую паузу на прогрузку списка рефералов
            await Task.Delay(500);

            // Если там тоже будет свой локальный спиннер загрузки, мы добавим его ожидание сюда позже
        }
    }
}