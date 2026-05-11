using Microsoft.Playwright;

public class ResidentSelector
{
    private readonly IPage _page;
    private ILocator SearchInput => _page.Locator("input[placeholder='Search...']");
    private ILocator ResidentRow(string name) => _page.Locator($"tr:has-text('{name}')");

    public ResidentSelector(IPage page) => _page = page;

    public async Task SelectResidentAsync(string name)
    {
        await _page.ClickAsync(".resident-dropdown"); // Селектор самого поля
        await SearchInput.FillAsync(name);
        await ResidentRow(name).ClickAsync();
    }
}