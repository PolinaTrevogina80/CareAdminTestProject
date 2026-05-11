using Microsoft.Playwright;
using NUnit.Framework;

public static class PlaywrightExtensions
{
    public static async Task MakeScreenshotAsync(this IPage page, string stepName)
    {
        // Генерируем путь
        var path = Path.Combine(Path.GetTempPath(), $"{stepName}_{Guid.NewGuid()}.png");
        
        // Делаем скриншот
        await page.ScreenshotAsync(new() { Path = path });

        // Прикрепляем к NUnit (TestContext доступен глобально в потоке теста)
        TestContext.AddTestAttachment(path, stepName);
    }
}