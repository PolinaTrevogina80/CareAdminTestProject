using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
        public virtual string BaseUrl => "https://localhost:60254";

        /// <summary>
        /// Dedicated logger instance for tracking framework initialization steps and test lifecycle events.
        /// </summary>
        protected Microsoft.Extensions.Logging.ILogger<BaseTest> Log;

        private static readonly SemaphoreSlim _authSemaphore = new SemaphoreSlim(1, 1);
        private static IPlaywright _playwright;
        private static IBrowser _sharedBrowser;

        private readonly List<string> _networkErrors = new List<string>();

        /// <summary>
        /// Initializes the shared heavy browser instance once before any tests in the fixture start execution.
        /// Prevents resource exhaustion and avoids cascading connection pool overflows on the server.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _sharedBrowser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
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
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File($"logs/test-run-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
            Log = loggerFactory.CreateLogger<BaseTest>();

            Log.LogInformation($"Initializing test context for Worker: {TestContext.CurrentContext.WorkerId}...");

            await _authSemaphore.WaitAsync();
            try
            {
                if (!File.Exists(StatePath))
                {
                    Log.LogInformation($"[AUTH] state.json missing for Worker {TestContext.CurrentContext.WorkerId}. Triggering baseline authentication sequence...");
                    await AuthSetup.CreateLogin(_sharedBrowser, Context, Page, _networkErrors, BaseUrl, StatePath);
                    File.SetLastWriteTime(StatePath, DateTime.Now);
                }

                await RefreshTokenIfNeededInternal();
            }
            finally
            {
                _authSemaphore.Release();
            }

            // Direct page viewport towards the target system landing pathway
            await Page.GotoAsync("/", new() { Timeout = 60000, WaitUntil = WaitUntilState.Commit });

            var rootAppAnchor = Page.Locator("input[placeholder='Username'], span.title:has-text('My Tasks')").First;
            await rootAppAnchor.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });
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
                Log.LogInformation($"[REFRESH] Token expired for Worker {TestContext.CurrentContext.WorkerId}. Initiating session refresh sequence...");
                _networkErrors.Clear();

                await Page.GotoAsync("/", new() { WaitUntil = WaitUntilState.Commit });

                try
                {
                    await AuthSetup.CreateLogin(_sharedBrowser, Context, Page, _networkErrors, BaseUrl, StatePath);
                    File.SetLastWriteTime(StatePath, DateTime.Now);
                    Log.LogInformation($"[REFRESH] Session updated successfully for Worker {TestContext.CurrentContext.WorkerId}.");
                }
                catch (Exception ex)
                {
                    Log.LogError($"[REFRESH FAILED] Authentication sequence crashed on Worker {TestContext.CurrentContext.WorkerId}: {ex.Message}");
                    throw;
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
                Log.LogError($"Failure Screenshot is saved {path}");
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
