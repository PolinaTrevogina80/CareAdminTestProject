using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Serilog;
using static TestProject1.Common.AuthSetup;


namespace TestProject1.Common
{
    public class BaseTest : PageTest
    {
        public readonly string StatePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "state.json");
        public virtual string BaseUrl => "https://qa.careadminplus.com";
        protected Microsoft.Extensions.Logging.ILogger<BaseTest> Log;
        private static readonly object _refreshLock = new object();

        // РЕШЕНИЕ ПРОБЛЕМЫ 1: Сразу создаем пустой список, чтобы он никогда не был null
        private List<string> networkErrors = new List<string>();

        // Эти поля больше не нужны на уровне класса, так как повторно использовать одну страницу в параллельных тестах нельзя
        private IBrowser _sharedBrowser;

        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions
            {
                StorageStatePath = File.Exists(StatePath) ? StatePath : null,
                BaseURL = BaseUrl,
                IgnoreHTTPSErrors = true
            };
        }

        [OneTimeSetUp]
        public async Task CreateAuthState()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("logs/test-log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
            Log = loggerFactory.CreateLogger<BaseTest>();

            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            _sharedBrowser = browser; 

            var authContext = await browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = BaseUrl, IgnoreHTTPSErrors = true });
            var authPage = await authContext.NewPageAsync();

            authPage.Response += (sender, response) =>
            {
                if (response.Status >= 400)
                {
                    lock (networkErrors) { networkErrors.Add($"[HTTP {response.Status}] {response.Request.Method} {response.Url}"); }
                }
            };

            try
            {
                // Вызываем логин для первичного создания файла
                await CreateLogin(browser, authContext, authPage, networkErrors, BaseUrl, StatePath);
            }
            finally
            {
                // Закрываем технические окна ЗДЕСЬ, так как мы их тут и создали
                if (authContext != null) await authContext.CloseAsync();
                if (browser != null) await browser.CloseAsync();
            }
        }

        [SetUp]
        public async Task RefreshTokenIfNeeded()
        {
            bool needRefresh = false;

            lock (_refreshLock)
            {
                if (File.Exists(StatePath))
                {
                    var lastWrite = File.GetLastWriteTime(StatePath);
                    if ((DateTime.Now - lastWrite).TotalMinutes > 4)
                    {
                        needRefresh = true;
                        // УБРАЛИ отсюда моментальный сдвиг времени изменения файла
                    }
                }
                else
                {
                    needRefresh = true;
                }
            }

            if (needRefresh)
            {
                Log.LogInformation("Токен устарел (прошло > 4 минут). Запускаем обновление сессии...");
                lock (networkErrors) { networkErrors.Clear(); }

                await Page.GotoAsync("/");

                // Вызываем обновленный метод. Если он упадет — время файла не изменится,
                // и следующий тест честно попробует перелогиниться заново, не ломая всю очередь
                await CreateLogin(_sharedBrowser, Context, Page, networkErrors, BaseUrl, StatePath);

                lock (_refreshLock)
                {
                    // Фиксируем успех: теперь файл официально обновлен
                    File.SetLastWriteTime(StatePath, DateTime.Now);
                }
                Log.LogInformation("Сессия текущего теста успешно обновлена.");
            }
        }
        [TearDown]
        public async Task TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                var testName = TestContext.CurrentContext.Test.Name;
                var safeName = string.Join("_", testName.Split(Path.GetInvalidFileNameChars()));
                safeName = safeName.Replace("\"", "").Replace(",", "").Replace(" ", "_");

                // Формирования уникального пути для разных потоков:
                var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 5);
                var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{safeName}_{uniqueId}_failed.png");

                await Page.ScreenshotAsync(new() { Path = path });
                TestContext.AddTestAttachment(path, "Screenshot on Failure");
                Log.LogError($"Failure Screenshot is saved {path}");
            }
        }
    }
}
