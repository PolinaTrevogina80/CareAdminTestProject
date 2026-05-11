using Microsoft.Playwright;

public class IncidentDetailsPage
{
    private readonly IPage _page;
    // Компоненты вкладок
    public GeneralTab General { get; }

    public IncidentDetailsPage(IPage page)
    {
        _page = page;
        General = new GeneralTab(_page);
    }

    // Общие методы, например, переключение вкладок
    public async Task GoToTabAsync(string tabName) =>
        await _page.ClickAsync($"text={tabName}");
}