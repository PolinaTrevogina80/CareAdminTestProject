using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Content;

namespace CareAdminTestProject.Incidents.IncidentTracker.PageConfig.Components
{
    public class DetailedViewTab : IncidentTrackerPage
    {
        public DetailedViewTab(IPage page) : base(page)
        {
        }

        // 1. Быстрые фильтры (карточки сверху)
        public ILocator QuickFilterCard(string filterName) =>
            _page.Locator("mat-card, .filter-card").Filter(new() { HasText = filterName });

        public ILocator TabButton =>
            _page.Locator("a.child-nav-item").GetByText("Detailed View", new() { Exact = false });


        // 2. Элементы управления над таблицей
        public ILocator SearchResidentInput =>
            _page.GetByLabel("Search Resident", new() { Exact = true });

        public ILocator GroupByResidentCheckbox => _page.GetByLabel("Group by Resident");
        private ILocator Grid => _page.Locator("kendo-grid, .k-grid").First;

        // Все строки таблицы

        // Ищем конкретную строку резидента по его имени
        public ILocator ResidentRow(string residentName) =>
            GridRows.Filter(new()
            {
                HasTextRegex = new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(residentName))
            });

        // Колонка "Total #" (вторая ячейка td в строке резидента)
        public ILocator TotalCountCell(string residentName) =>
            ResidentRow(residentName).Locator("td").Nth(1);

        public ILocator GroupHeaders => Grid.Locator("tr.k-grouping-row, tr.mat-row-group");

        public ILocator GroupExpandArrow(string residentName) =>
            ResidentRow(residentName).Locator(".toggle-button-wrapper, mat-icon").First;

        public ILocator DataRows => Grid.Locator("tbody tr").Filter(new() { HasNot = _page.Locator(".k-grouping-row") });


        // 3. Элементы таблицы (Grid)
        private ILocator GridTable => _page.Locator("kendo-grid, mat-table, table").First;

        public ILocator GridRows => GridTable.Locator("tbody tr");

        // Локатор для раскрытия группы резидента (стрелочка Кендо или матраса)
        public ILocator RowExpandArrow(string residentName) =>
            GridRows.Filter(new() { HasText = residentName }).Locator(".k-icon, .expand-arrow").First;



        // --- Низкоуровневые действия (Actions) ---

        public async Task FilterByQuickCardAsync(string filterName)
        {
            await QuickFilterCard(filterName).ClickAsync();
            // Здесь также заложим ожидание глобального лоадера, так как таблица будет перегружаться
        }
        /// <summary>
        /// Ожидание лоадера, аналогичное логике из базового класса IncidentTrackerPage.
        /// </summary>
        public async Task WaitForGridLoaderAsync()
        {
            await _page.WaitForTimeoutAsync(400); // Пауза для срабатывания дебаунса Angular/Kendo
            var loader = _page.Locator(".loader-wrapper, kendo-loading, .k-loading-overlay").First;
            if (await loader.IsVisibleAsync())
            {
                await loader.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });
            }
        }

        /// <summary>
        /// Извлекает текст из ячейки и очищает его от системных элементов интерфейса.
        /// </summary>
        public async Task<string> GetResidentNameFromRowAsync(int rowIndex, int columnIndex = 0)
        {
            var row = DataRows.Nth(rowIndex);
            var cell = row.Locator("td").Nth(columnIndex);
            string rawText = await cell.InnerTextAsync();

            // ИСПРАВЛЕНО: Срезаем системные имена иконок Material Design, которые Playwright стягивает вместе с текстом
            string cleanText = rawText
                .Replace("keyboard_arrow_right", "")
                .Replace("keyboard_arrow_down", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();

            return cleanText;
        }


        public async Task SearchResidentAsync(string name)
        {
            await SearchResidentInput.FillAsync(name);
            await SearchResidentInput.PressAsync("Enter"); // или дебаунс ожидания
        }

        public async Task ToggleGroupByResidentAsync(bool shouldGroup)
        {
            var isChecked = await GroupByResidentCheckbox.IsCheckedAsync();
            if (isChecked != shouldGroup)
            {
                await GroupByResidentCheckbox.ClickAsync();
            }
        }

        /// <summary>
        /// Ожидание завершения локального рендеринга интерфейса после фильтрации/кликов.
        /// </summary>
        public async Task WaitForInterfaceDebounceAsync()
        {
            // Не ищем лоадер, просто даем Angular/Kendo применить фильтр в UI
            await Task.Delay(600);
        }

        /// <summary>
        /// Извлекает текст из ячейки даты инцидента по индексу строки.
        /// </summary>
        public async Task<string> GetIncidentDateFromRowAsync(int rowIndex, int columnIndex = 1)
        {
            var row = DataRows.Nth(rowIndex);
            var cell = row.Locator("td").Nth(columnIndex);
            return (await cell.InnerTextAsync()).Trim();
        }

        /// <summary>
        /// Извлекает уникальный маркер инцидента (например, время или ID) для финальной проверки присутствия.
        /// </summary>
        public async Task<string> GetIncidentTimeFromRowAsync(int rowIndex, int columnIndex = 2)
        {
            var row = DataRows.Nth(rowIndex);
            var cell = row.Locator("td").Nth(columnIndex);
            return (await cell.InnerTextAsync()).Trim();
        }
    }
}
