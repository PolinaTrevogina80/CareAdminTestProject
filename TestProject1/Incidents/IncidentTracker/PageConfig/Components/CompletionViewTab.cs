using Microsoft.Playwright;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace CareAdminTestProject.Incidents.IncidentTracker.PageConfig.Components
{
    public class CompletionViewTab : IncidentTrackerPage
    {
        public CompletionViewTab(IPage page) : base(page)
        {
        }
        
        // Кнопка-переключатель на саму вкладку Completion View
        public ILocator TabButton =>
            _page.Locator("a.child-nav-item").GetByText("Completion View", new() { Exact = false });

        // Поле поиска конкретно на этой вкладке (используем наш проверенный GetByLabel)
        public ILocator SearchResidentInput => _page.GetByLabel("Search Resident", new() { Exact = true });

        // Строки именно этой таблицы
        private ILocator Grid => _page.Locator("kendo-grid, .k-grid, table").First;
        public ILocator DataRows => Grid.Locator("tbody tr");

        /// <summary>
        /// Локатор для буквы 'C' / спиннера загрузки Completion View.
        /// </summary>
        private ILocator CompletionSpinner =>
            _page.Locator(".completion-loader, [class*='loader'], [class*='spinner'], svg").First;


        /// <summary>
        /// Переключается на вкладку Completion View.
        /// </summary>
        public async Task OpenAsync()
        {
            Log.Debug("[NAVIGATION] Кликаем по вкладке Completion View...");
            await TabButton.ClickAsync();

            // Микро-пауза, чтобы Angular успел вставить спиннер 'C' в DOM
            await Task.Delay(300);

            // Если спиннер появился на экране, ждем его полного исчезновения (до 30 секунд)
            if (await CompletionSpinner.IsVisibleAsync())
            {
                Log.Debug("[NAVIGATION] Обнаружен локальный лоадер матрицы 'C'. Ждем стабилизации данных...");
                try
                {
                    await CompletionSpinner.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 30000 });
                    Log.Debug("[NAVIGATION] Лоадер Completion View успешно скрылся.");
                }
                catch (TimeoutException)
                {
                    Log.Warning("[NAVIGATION] Спиннер матрицы не исчез за 30с, пробуем продолжить тест.");
                }
            }

            // Дополнительная пауза, чтобы строки грида окончательно отрендерились в браузере
            await Task.Delay(300);
        }


    }
}
