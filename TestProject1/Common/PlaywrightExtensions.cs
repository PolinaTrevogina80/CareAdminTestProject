using Microsoft.Playwright;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

/// <summary>
/// Provides utility extension methods for Playwright fluent interactions.
/// Houses core diagnostic capturing mechanisms configured for multi-threaded execution environments.
/// </summary>
namespace CareAdminTestProject.Common
{
    public static class PlaywrightExtensions
    {
        /// <summary>
        /// Captures a browser screenshot, automatically generating a unique system file path to prevent thread resource collisions, 
        /// and binds the resulting image asset directly into the active NUnit test report attachments repository.
        /// </summary>
        /// <param name="page">The current runtime fluid Playwright page context instance extended by this invocation.</param>
        /// <param name="stepName">The descriptive step string name prefix used to label the output image asset.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task MakeScreenshotAsync(this IPage page, string stepName)
        {
            // Generate a uniquely isolated local system file path destination pointer
            var path = Path.Combine(Path.GetTempPath(), $"{stepName}_{Guid.NewGuid()}.png");

            // Execute the automated native viewport screen capture action
            await page.ScreenshotAsync(new() { Path = path });

            // Bind the image reference directly to NUnit metadata (TestContext maps safely via localized execution threads)
            TestContext.AddTestAttachment(path, stepName);
        }

        /// <summary>
        /// Executes an authorized asynchronous HTTP POST request to retrieve available employee rosters.
        /// Extracts a secure session bearer token and passes the operational facility context parameters.
        /// Catch blocks safely encapsulate connection failures into structured fallback network error logs.
        /// </summary>
        /// <param name="page">The current Playwright page automation instance handling the execution context window.</param>
        /// <param name="apiName">The specific descriptive alias matching the routing gateway layout.</param>
        /// <returns>A string payload representing the response body or a safe formatted network exception text envelope.</returns>

        public static async Task<string> ApiPostRequest(this IPage page, string apiName)
        {

            string token = "";
            try
            {
                token = await GetTokenFromFile();

                // Делаем POST-запрос, явно передавая заголовок Authorization
                var apiResponse = await page.APIRequest.PostAsync("employees", new()
                {
                    DataObject = new { facilityId = "c1f80483-fd30-4327-814e-778ad171a67b" },
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? token : $"Bearer {token}" },
                        { "Accept", "application/json" }
                    }
                });

                if (!apiResponse.Ok)
                {
                    Assert.Fail($"API Error: Failed to fetch employees. Status: {apiResponse.Status}, Text: {await apiResponse.TextAsync()}");
                }

                return await apiResponse.TextAsync();
            }
            catch (Exception ex)
            {
                return($"Network Error: Exception occurred while requesting employee data: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts a valid authentication Bearer access token straight from the worker-isolated storage state environment file.
        /// Parses the underlying multi-origin localStorage schema layout tracking the explicit session key structures.
        /// </summary>
        /// <returns>A clean text value representation of the authorization access token data mapping string.</returns>
        /// <exception cref="NUnit.Framework.AssertionException">Thrown if the state path context is missing or token serialization steps return null.</exception>

        private static async Task<string> GetTokenFromFile()
        {
            Log.Debug("[STAFF_VALIDATION] Extracting Bearer token from storage state...");

            string token = "";
            string statePath = Path.Combine(
    TestContext.CurrentContext.TestDirectory,
    $"state_{TestContext.CurrentContext.WorkerId}.json");

            Log.Debug($"[STAFF_VALIDATION] Reading state file from: {statePath}");

            if (File.Exists(statePath))
            {
                var stateContent = File.ReadAllText(statePath);
                using var stateDoc = System.Text.Json.JsonDocument.Parse(stateContent);

                // 1. Берем массив "origins"
                var originsArray = stateDoc.RootElement.GetProperty("origins").EnumerateArray();

                // 2. Ищем нужный origin (наш BaseUrl) или берем первый попавшийся
                var currentOrigin = originsArray.FirstOrDefault(o =>
                    o.TryGetProperty("localStorage", out var ls) && ls.ValueKind == System.Text.Json.JsonValueKind.Array);

                if (currentOrigin.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                {
                    // 3. Теперь безопасно извлекаем массив localStorage из найденного объекта origin
                    var localStorageItems = currentOrigin.GetProperty("localStorage").EnumerateArray();

                    var sessionKeyItem = localStorageItems.FirstOrDefault(i =>
                        i.GetProperty("name").GetString() == "wc.sessionStorageKey");

                    if (sessionKeyItem.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                    {
                        string innerJsonStr = sessionKeyItem.GetProperty("value").GetString() ?? "";
                        using var innerDoc = System.Text.Json.JsonDocument.Parse(innerJsonStr);

                        if (innerDoc.RootElement.TryGetProperty("accessToken", out var accessTokenObj) &&
                            accessTokenObj.TryGetProperty("accessToken", out var tokenProp))
                        {
                            token = tokenProp.GetString() ?? "";
                        }
                    }
                }
            }
            else
            {
                Assert.Fail($"Validation Error: Target state file was not found at path: {statePath}");
            }

            if (string.IsNullOrEmpty(token))
            {
                Assert.Fail("Validation Error: Bearer token could not be extracted from the internal wc.sessionStorageKey structure.");
            }

            Log.Debug("[STAFF_VALIDATION] Token extracted successfully. Executing authorized POST request...");
            return token;
        }
    }

    /// <summary>
    /// A thread-safe logger bridge that routes code outputs to both disk files via Serilog 
    /// and to the active NUnit Test Explorer console view simultaneously.
    /// </summary>
    public static class TestLog
    {
        public static void Debug(string message)
        {
            Serilog.Log.Debug(message);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss} DBG] {message}");
        }

        public static void Information(string message)
        {
            Serilog.Log.Information(message);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss} INF] {message}");
        }

        public static void Warning(string message)
        {
            Serilog.Log.Warning(message);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss} WRN] {message}");
        }

        public static void Error(string message)
        {
            Serilog.Log.Error(message);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss} ERR] {message}");
        }

        public static void Error(Exception ex, string message)
        {
            Serilog.Log.Error(ex, message);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss} ERR] {message}{Environment.NewLine}{ex}");
        }

        public static void Fatal(Exception ex, string message)
        {
            Serilog.Log.Fatal(ex, message);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss} FTL] {message}{Environment.NewLine}{ex}");
        }
    }
}
