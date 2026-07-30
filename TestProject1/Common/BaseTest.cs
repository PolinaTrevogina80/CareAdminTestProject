using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using Serilog;
using System.Collections.Concurrent;
using static System.Net.Mime.MediaTypeNames;

namespace CareAdminTestProject.Common
{
    /// <summary>
    /// Base test class providing isolated multi-threaded authentication states 
    /// and single-instance browser lifecycle management for Playwright tests.
    /// </summary>
    public class BaseTest : PageTest
    {
        /// <summary>
        /// Gets the dynamic path to the storage state file, unique for each parallel worker thread.
        /// </summary>
        public string StatePath => Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            $"state_{TestContext.CurrentContext.WorkerId}.json");

        /// <summary>
        /// Gets the target application base URL environment endpoint.
        /// </summary>
        public virtual string BaseUrl => "http://localhost:4200";
        //public virtual string BaseUrl => "https://stg.careadminplus.com";
        //public virtual string BaseUrl => "https://qa.careadminplus.com";

        /// <summary>
        /// Dedicated logger instance for tracking framework initialization steps and test lifecycle events.
        /// </summary>
        protected Serilog.ILogger Log;

        private static readonly SemaphoreSlim _authSemaphore = new SemaphoreSlim(1, 1);
        private static IPlaywright _playwright;
        private static IBrowser _sharedBrowser;

        private readonly List<string> _networkErrors = new List<string>();
        private const int MaxAuthRetries = 3;

        /// <summary>
        /// Initializes the shared heavy browser instance once before any tests in the fixture start execution.
        /// Prevents resource exhaustion and avoids cascading connection pool overflows on the server.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _sharedBrowser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            // Initialize global Serilog configuration once per run
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File($"logs/test-run-.txt", rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();
        }

        /// <summary>
        /// Configures initial context options, automatically attaching pre-existing authentication cookies and tokens if available.
        /// </summary>
        /// <returns>A configured instance of <see cref="BrowserNewContextOptions"/>.</returns>
        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions
            {
                StorageStatePath = File.Exists(StatePath) ? StatePath : null,
                BaseURL = BaseUrl,
                IgnoreHTTPSErrors = true
            };
        }

        /// <summary>
        /// Runs before each test case. Sets up logging, thread-safe authentication states, 
        /// and navigates the page to a ready-to-test verified UI anchor state.
        /// </summary>
        [SetUp]
        public async Task BaseSetup()
        {
            // 1. Очередь для хранения истории последних 10 запросов (всех, включая успешные)
            var requestHistory = new ConcurrentQueue<string>();
            const int maxHistorySize = 10;

            // 2. Очередь для хранения последних 10 логов консоли браузера
            var consoleHistory = new ConcurrentQueue<string>();

            // Подписка на ВСЕ ответы для ведения истории
            Page.Response += (sender, response) =>
            {
                var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                var logLine = $"[{timestamp}] [{response.Status}] {response.Request.Method} -> {response.Url}";

                requestHistory.Enqueue(logLine);
                while (requestHistory.Count > maxHistorySize)
                    requestHistory.TryDequeue(out _);

                // Если это ошибка, обрабатываем её асинхронно, чтобы прочитать Body
                if (response.Status >= 400)
                {
                    Task.Run(async () =>
                    {
                        string responseBody = "Не удалось прочитать тело ответа";
                        try
                        {
                            // Пытаемся получить текст ответа (обработает и plain text, и JSON)
                            responseBody = await response.TextAsync();

                            // Опционально: если ответ слишком длинный, можно его обрезать
                            if (responseBody.Length > 2000)
                                responseBody = responseBody.Substring(0, 2000) + "... [ОБРЕЗАНО]";
                        }
                        catch (Exception ex)
                        {
                            responseBody = $"[Ошибка чтения Body]: {ex.Message}";
                        }

                        var errorTimestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                        var errorBuilder = new System.Text.StringBuilder();

                        errorBuilder.AppendLine($"==================================================");
                        errorBuilder.AppendLine($"[❗ API ERROR {response.Status}] AT {errorTimestamp}");
                        errorBuilder.AppendLine($"URL: {response.Url}");
                        errorBuilder.AppendLine($"Method: {response.Request.Method}");
                        errorBuilder.AppendLine($"==================================================");

                        // Сюда вставляем полученный ответ сервера
                        errorBuilder.AppendLine("--- ОТВЕТ СЕРВЕРА (RESPONSE BODY) ---");
                        errorBuilder.AppendLine(string.IsNullOrWhiteSpace(responseBody) ? "(Пустое тело ответа)" : responseBody);
                        errorBuilder.AppendLine("------------------------------------");

                        errorBuilder.AppendLine("--- ПОСЛЕДНИЕ ЗАПРОСЫ (КОНТЕКСТ) ---");
                        foreach (var hist in requestHistory) errorBuilder.AppendLine(hist);

                        errorBuilder.AppendLine("--- ПОСЛЕДНИЕ ЛОГИ КОНСОЛИ БРАУЗЕРА ---");
                        if (consoleHistory.IsEmpty) errorBuilder.AppendLine("(Консоль чиста)");
                        else foreach (var conLog in consoleHistory) errorBuilder.AppendLine(conLog);

                        errorBuilder.AppendLine($"==================================================\n");

                        Log.Error($"API ERROR {response.Status}: {response.Url}");

                        // Используем блокировку или потокобезопасный метод для записи в файл
                        lock (requestHistory)
                        {
                            File.AppendAllText("failed_api_requests.log", errorBuilder.ToString());
                        }
                    });
                }
            };

            Page.Console += (sender, msg) =>
            {
                var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                var consoleLine = $"[{timestamp}] [{msg.Type}] {msg.Text}";
                consoleHistory.Enqueue(consoleLine);
                while (consoleHistory.Count > maxHistorySize) consoleHistory.TryDequeue(out _);
            };
            Log = Serilog.Log.ForContext("WorkerId", TestContext.CurrentContext.WorkerId);
            Log.Information($"Initializing test context for Worker: {TestContext.CurrentContext.WorkerId}...");

            // 1. Пытаемся применить существующий state.json, если он есть
            if (File.Exists(StatePath))
            {
                Log.Information($"[AUTH] State file found. Loading context state...");
                try
                {
                    await RefreshTokenIfNeededInternal();
                }
                catch (Exception ex)
                {
                    Log.Warning($"[AUTH] Token refresh failed: {ex.Message}. Will attempt recovery on landing page.");
                }
            }
            else
            {
                Log.Information($"[AUTH] state.json missing. Triggering baseline auth...");
                await ExecuteAuthWithRetriesAsync("INITIAL AUTH");
            }

            // 2. Переходим на главную страницу
            Log.Information($"[NAVIGATE] Worker {TestContext.CurrentContext.WorkerId} navigating to home page...");
            await Page.GotoAsync("/", new() { Timeout = 60000, WaitUntil = WaitUntilState.Commit });

            // 3. ПРОВЕРКА: Куда мы попали?
            // Разделяем селекторы: один означает "мы внутри", второй — "нас выкинуло на форму логина"
            var dashboardAnchor = Page.Locator("span.title:has-text('My Tasks')").First;
            var loginFormAnchor = Page.Locator("button:has-text('SIGN IN'), input[placeholder='Username']").First;

            Log.Information($"[NAVIGATE] Verifying page landing state...");

            // Ждем появления хотя бы одного из этих признаков
            await Page.Locator("span.title:has-text('My Tasks'), button:has-text('SIGN IN')").First.WaitForAsync(new() { Timeout = 15000 });

            if (await loginFormAnchor.IsVisibleAsync())
            {
                Log.Warning($"[AUTH ALERT] Detached session detected! Worker was redirected to Sign In page. Executing emergency login sequence...");

                // Перезапускаем полную процедуру авторизации через UI (ввод логина, пароля, сохранение state.json)
                await ExecuteAuthWithRetriesAsync("EMERGENCY RE-AUTH");

                // Снова переходим на главную страницу после генерации свежего состояния
                await Page.GotoAsync("/", new() { Timeout = 30000, WaitUntil = WaitUntilState.Commit });
            }

            // 4. Окончательное подтверждение, что мы внутри системы
            try
            {
                await dashboardAnchor.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
                Log.Information($"[NAVIGATE SUCCESS] Worker {TestContext.CurrentContext.WorkerId} successfully reached target dashboard state.");
            }
            catch (Exception ex)
            {
                Log.Error($"[NAVIGATE FAILED] Worker {TestContext.CurrentContext.WorkerId} stuck. Current URL: {Page.Url}. Error: {ex.Message}");
                await Page.ScreenshotAsync(new() { Path = $"stuck_navigate_worker_{TestContext.CurrentContext.WorkerId}.png" });
                throw;
            }
        }
        /// <summary>
        /// Evaluates current token expiration thresholds and updates the storage session state 
        /// if the file age exceeds the designated 4-minute boundary.
        /// </summary>
        private async Task RefreshTokenIfNeededInternal()
        {
            bool needRefresh = File.Exists(StatePath)
                ? (DateTime.Now - File.GetLastWriteTime(StatePath)).TotalMinutes > 4
                : true;

            if (needRefresh)
            {
                Log.Information($"[REFRESH] Token expired or missing for Worker {TestContext.CurrentContext.WorkerId}. Initiating session refresh sequence...");
                _networkErrors.Clear();

                Log.Information($"[REFRESH] Navigating to root before refresh sequence on Worker {TestContext.CurrentContext.WorkerId}...");
                await Page.GotoAsync("/", new() { WaitUntil = WaitUntilState.Commit });

                await ExecuteAuthWithRetriesAsync("SESSION REFRESH");
            }
        }

        /// <summary>
        /// Executes the login sequence wrapper with automatic retry logic and extensive error logging.
        /// </summary>
        private async Task ExecuteAuthWithRetriesAsync(string contextName)
        {
            for (int attempt = 1; attempt <= MaxAuthRetries; attempt++)
            {
                try
                {
                    Log.Information($"[{contextName}] Attempt {attempt} of {MaxAuthRetries} for Worker {TestContext.CurrentContext.WorkerId}...");

                    await AuthSetup.CreateLogin(_sharedBrowser, Context, Page, _networkErrors, BaseUrl, StatePath);
                    File.SetLastWriteTime(StatePath, DateTime.Now);

                    Log.Information($"[{contextName}] Successfully completed on attempt {attempt} for Worker {TestContext.CurrentContext.WorkerId}.");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warning($"[{contextName} FAILED] Attempt {attempt} failed on Worker {TestContext.CurrentContext.WorkerId}. Current URL: {Page.Url}. Error: {ex.Message}");

                    if (_networkErrors.Any())
                    {
                        Log.Warning($"[{contextName} NET ERRORS] Intercepted network errors during attempt {attempt}: {string.Join(" | ", _networkErrors)}");
                        _networkErrors.Clear();
                    }

                    if (attempt == MaxAuthRetries)
                    {
                        Log.Error($"[{contextName} CRITICAL] All {MaxAuthRetries} authentication attempts failed for Worker {TestContext.CurrentContext.WorkerId}.");
                        throw;
                    }

                    // Small progressive delay between retries to let network/services stabilize
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                }
            }
        }

        /// <summary>
        /// Performs cleanup after each test. If the test fails, captures a page screenshot and attaches it to the report.
        /// </summary>
        /// <returns>A task representing the asynchronous cleanup operation.</returns>
        [TearDown]
        public async Task TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                // Get the test name
                var testName = TestContext.CurrentContext.Test.Name;

                // Replace invalid filename characters, spaces, quotes, and commas with underscores
                var invalidChars = new string(Path.GetInvalidFileNameChars()) + "\" ,";
                var safeName = System.Text.RegularExpressions.Regex.Replace(testName, $"[{System.Text.RegularExpressions.Regex.Escape(invalidChars)}]+", "_");

                var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{safeName}_failed.png");
                await Page.ScreenshotAsync(new() { Path = path });
                TestContext.AddTestAttachment(path, "Screenshot on Failure");
                Log.Error($"Failure Screenshot is saved {path}");
            }
        }

        /// <summary>
        /// Disposes system processes, releases active ports, and safely destroys thread synchronization locks 
        /// once all test executions within the current suite terminate.
        /// </summary>
        [OneTimeTearDown]
        public async Task OneTimeCleanup()
        {
            if (_sharedBrowser != null)
            {
                await _sharedBrowser.CloseAsync();
            }

            _playwright?.Dispose();
            _authSemaphore?.Dispose();
        }
    }
}