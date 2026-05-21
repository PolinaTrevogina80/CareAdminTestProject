using Microsoft.Playwright;

/// <summary>
/// Represents the Incident Tracker dashboard page.
/// Serves as the primary entry point to manage repository lists, view grids, and initialize new form creation workflows.
/// </summary>
public class IncidentTrackerPage
{
    private readonly IPage _page; // Declare the core browser automation page field string reference

    /// <summary>
    /// Initializes a new instance of the <see cref="IncidentTrackerPage"/> class.
    /// </summary>
    /// <param name="page">The active thread-isolated Playwright page context instance forwarded from the test suite layers.</param>
    public IncidentTrackerPage(IPage page) // Received directly out of test runtime contexts
    {
        _page = page;
    }

    /// <summary>
    /// Locates the primary workflow creation action button on the dashboard repository layout and dispatches a click trigger event.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClickNewIncidentAsync()
    {
        var newIncidentButton = _page.GetByRole(AriaRole.Button, new() { Name = "New Incident" });

        await newIncidentButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await newIncidentButton.ClickAsync();
    }
}
