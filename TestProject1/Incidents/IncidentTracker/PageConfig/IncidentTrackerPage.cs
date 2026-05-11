using Microsoft.Playwright;

public class IncidentTrackerPage
{
    private readonly IPage _page; // Объявляем поле

    public IncidentTrackerPage(IPage page) // Получаем из теста
    {
        _page = page;
    }

    public async Task ClickNewIncidentAsync()
    {
        // Предполагаемый селектор кнопки (обычно по тексту или роли)
        await _page.GetByRole(AriaRole.Button, new() { Name = "New Incident" }).ClickAsync();
    }
}