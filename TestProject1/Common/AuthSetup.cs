using Microsoft.Playwright;
using Serilog;

namespace TestProject1.Common
{
    // Этот класс выполнится ОДИН РАЗ перед всеми тестами в пространстве имен CareAdminTests
    [SetUpFixture]
    public class AuthSetup
    {
        // Формируем надежный путь к файлу в папке запуска тестов
        public static string StatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "state.json");

        [OneTimeSetUp]
        public async Task RunBeforeAllTests()
        {
            Log.Information("--- STARTING AUTH SETUP ---");

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true // Можно поставить false для отладки
            });

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            try
            {
                // 1. Логин
                await page.GotoAsync("https://qa.careadminplus.com");
                await page.GetByPlaceholder("Username").FillAsync("polly@test.ts");
                await page.GetByPlaceholder("Password").FillAsync("Qwert1@#");
                await page.GetByRole(AriaRole.Button, new() { Name = "SIGN IN" }).ClickAsync();

                // 2. Ждем успешного входа (замените селектор на тот, что в вашем тесте)
                await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(".*home"), new() { Timeout = 15000 });
                await page.GetByText("My Tasks").WaitForAsync();

                // 3. Сохранение состояния по АБСОЛЮТНОМУ пути
                await context.StorageStateAsync(new() { Path = StatePath });

                Log.Information($"--- AUTH SUCCESSFUL. State saved to: {StatePath} ---");
            }
            catch (Exception ex)
            {
                Log.Information($"--- AUTH FAILED: {ex.Message} ---");
                throw; // Если логин упал, NUnit пометит остальные тесты как Inconclusive
            }
            finally
            {
                await browser.CloseAsync();
            }
        }
    }
}