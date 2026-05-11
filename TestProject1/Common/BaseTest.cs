using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Testing.Platform.Logging;
using Serilog;


namespace TestProject1.Common
    {
        public class BaseTest : PageTest
        {
            public readonly string StatePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "state.json");
            public virtual string BaseUrl => "https://qa.careadminplus.com";
            protected Microsoft.Extensions.Logging.ILogger<BaseTest> Log;

        public override BrowserNewContextOptions ContextOptions()
            {
                return new BrowserNewContextOptions
                {
                    StorageStatePath = File.Exists(StatePath) ? StatePath : null,
                    
                    BaseURL = BaseUrl

                };
            }


        [OneTimeSetUp]
        public async Task CreateAuthState()
        {
            // Настройка Serilog через код (или можно через appsettings.json)
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // Уровень по умолчанию
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("logs/test-log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Создаем фабрику для Microsoft Logging
            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
            Log = loggerFactory.CreateLogger<BaseTest>();


            // 1. Создаем отдельный инстанс Playwright и браузера, так как базовый Browser еще null
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            var authContext = await browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = BaseUrl });

            var authPage = await authContext.NewPageAsync();

            await authPage.GotoAsync("/");
            await authPage.GetByPlaceholder("Username").FillAsync("polly@test.ts");
            await authPage.GetByPlaceholder("Password").FillAsync("Qwert1@#");
            await authPage.GetByRole(AriaRole.Button, new() { Name = "SIGN IN" }).ClickAsync();

            await authPage.WaitForURLAsync(new System.Text.RegularExpressions.Regex(".*home"), new() { Timeout = 30000 });
            await Expect(authPage.GetByText("My Tasks")).ToBeVisibleAsync(new() { Timeout = 30000 });

            // Ожидаем появления токена в LocalStorage
            await authPage.WaitForFunctionAsync(@"() => {
        for (let i = 0; i < localStorage.length; i++) {
            if (localStorage.getItem(localStorage.key(i)).includes('accessToken')) return true;
        }
        return false;
    }", null, new PageWaitForFunctionOptions { Timeout = 10000 });

            await authPage.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Сохраняем состояние
            await authContext.StorageStateAsync(new() { Path = StatePath });
            Log.LogInformation("Successful log in");

            // Закрываем временный браузер
            await authContext.CloseAsync();
            await browser.CloseAsync();
        }


        [TearDown]
        public async Task TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                // Берем имя теста
                var testName = TestContext.CurrentContext.Test.Name;

                // Заменяем все плохие символы на подчеркивание
                var safeName = string.Join("_", testName.Split(Path.GetInvalidFileNameChars()));
                // Дополнительно убираем кавычки и запятые, которые NUnit добавляет от параметров
                safeName = safeName.Replace("\"", "").Replace(",", "").Replace(" ", "_");

                string fileName = TestContext.CurrentContext.Test.Name;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    safeName = safeName.Replace(c, '_');
                }

                var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{safeName}_failed.png");
                await Page.ScreenshotAsync(new() { Path = path });
                TestContext.AddTestAttachment(path, "Screenshot on Failure");
                Log.LogError($"Failure Screenshot is saved {path}");
            }
        }
    }
    
}