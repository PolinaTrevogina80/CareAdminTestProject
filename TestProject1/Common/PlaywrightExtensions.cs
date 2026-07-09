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

        public static async Task<string> ApiPostRequest(
            this IPage page,
            string relativeUrl,
            object? requestBody = null,
            Dictionary<string, string>? customHeaders = null)
        {
            string token = "";
            string rawContent = "";
            string fullUrl = "";
            try
            {
                fullUrl = relativeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? relativeUrl
                    : await GetFullURL(relativeUrl, page);

                try
                {
                    // Исправлено: если JS возвращает null, C# запишет пустую строку, а не null
                    token = await page.EvaluateAsync<string>("() => localStorage.getItem('access_token') || sessionStorage.getItem('access_token') || ''");
                }
                catch (Exception)
                {
                    token = "";
                }

                // Если из браузера токен получить не удалось (вернулась пустая строка), берем из файла
                if (string.IsNullOrEmpty(token))
                {
                    token = await GetTokenFromFile() ?? "";
                }

                // Если токен все еще пустой, подстрахуемся, чтобы не передать null в заголовок
                if (string.IsNullOrEmpty(token))
                {
                    token = "NoTokenFound";
                }

                // Если токен достали без приставки Bearer, форматируем его
                if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) && token != "NoTokenFound")
                {
                    token = $"Bearer {token}";
                }

                var currentUri = new Uri(page.Url);
                string baseFrontendUrl = currentUri.GetLeftPart(UriPartial.Authority);

                // Формируем словарь заголовков. Ни одно значение здесь теперь гарантированно не будет null
                var headers = new Dictionary<string, string>
                {
                    { "Authorization", token },
                    { "Accept", "application/json" },
                    { "Content-Type", "application/json" }, // Возвращаем обязательный Content-Type
                    { "X-App-Id", "AccidentIncident" },
                    { "X-Context-Id", "c1f80483-fd30-4327-814e-778ad171a67b" },
                    { "X-Context-Type", "Facility" },
                    { "X-Tenant-Id", "CassenaCare" },
                    { "Origin", baseFrontendUrl },
                    { "Referer", baseFrontendUrl + "/" }
                };

                if (customHeaders != null)
                {
                    foreach (var header in customHeaders)
                    {
                        // Дополнительная защита: игнорируем null/undefined из кастомных заголовков
                        if (header.Value != null)
                        {
                            headers[header.Key] = header.Value;
                        }
                    }
                }

                var options = new APIRequestContextOptions { Headers = headers };

                if (requestBody != null)
                {
                    options.DataObject = requestBody;
                }

                var apiResponse = await page.APIRequest.PostAsync(fullUrl, options);
                rawContent = await apiResponse.TextAsync();

                if (!apiResponse.Ok)
                {
                    Assert.Fail($"API Error: Failed to POST data to {fullUrl}. Status: {apiResponse.Status}, Content: {rawContent}");
                }

                if (rawContent.TrimStart().StartsWith("<"))
                {
                    string snippet = rawContent.Length > 500 ? rawContent.Substring(0, 500) : rawContent;
                    Assert.Fail($"API Error: Expected JSON response from {fullUrl}, but received HTML instead. Snippet: {snippet}");
                }

                return rawContent;
            }
            catch (Exception ex) when (!(ex is AssertionException))
            {
                string snippet = rawContent.Length > 200 ? rawContent.Substring(0, 200) : rawContent;
                Log.Error($"Network Error: Exception occurred while POSTing to {fullUrl}: {ex.Message}. Raw content snippet: {snippet}");

                throw;
            }
        }


        /// <summary>
        /// Executes an authorized asynchronous HTTP GET request to retrieve available employee rosters.
        /// Extracts a secure session bearer token and passes the operational facility context parameters.
        /// Catch blocks safely encapsulate connection failures into structured fallback network error logs.
        /// </summary>
        /// <param name="page">The current Playwright page automation instance handling the execution context window.</param>
        /// <param name="apiName">The specific descriptive alias matching the routing gateway layout.</param>
        /// <returns>A string payload representing the response body or a safe formatted network exception text envelope.</returns>

        public static async Task<string> ApiGetRequest(this IPage page, string relativeUrl, Dictionary<string, object>? queryParams = null, Dictionary<string, string>? customHeaders = null)
        {
            string token = "";
            string rawContent = "";
            try
            {
                // 1. Формируем полный URL (если передан относительный путь/хвост)
                string fullUrl = relativeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? relativeUrl
                    : await GetFullURL(relativeUrl, page);

                token = await GetTokenFromFile();

                var headers = new Dictionary<string, string>
        {
            { "Authorization", token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? token : $"Bearer {token}" },
            { "Accept", "application/json" }
        };

                if (customHeaders != null)
                {
                    foreach (var header in customHeaders)
                    {
                        headers[header.Key] = header.Value;
                    }
                }

                var options = new APIRequestContextOptions { Headers = headers };

                if (queryParams != null)
                {
                    options.Params = queryParams.ToDictionary(k => k.Key, v => v.Value);
                }

                // 2. ИССПРАВЛЕНО: Теперь отправляем запрос именно на сформированный fullUrl
                var apiResponse = await page.APIRequest.GetAsync(fullUrl, options);
                rawContent = await apiResponse.TextAsync();

                // 3. ИСПРАВЛЕНО: В логи ошибок пишем fullUrl для точной диагностики
                if (!apiResponse.Ok)
                {
                    Assert.Fail($"API Error: Failed to fetch data from {fullUrl}. Status: {apiResponse.Status}, Content: {rawContent}");
                }

                // Проверяем на случай, если сервер вернул 200 OK, но внутри HTML (например, страница логина IIS/Nginx)
                if (rawContent.TrimStart().StartsWith("<"))
                {
                    string snippet = rawContent.Length > 500 ? rawContent.Substring(0, 500) : rawContent;
                    Assert.Fail($"API Error: Expected JSON from {fullUrl}, but received HTML instead. Snippet: {snippet}");
                }

                return rawContent;
            }
            catch (Exception ex) when (!(ex is AssertionException))
            {
                // 4. ИСПРАВЛЕНО: В лог перехвата исключений пишем fullUrl
                string snippet = rawContent.Length > 200 ? rawContent.Substring(0, 200) : rawContent;
                Log.Error($"Network Error: Exception occurred while requesting {relativeUrl} (Full URL: {relativeUrl}): {ex.Message}. Raw content snippet: {snippet}");

                throw; // Критично: пробрасываем ошибку вверх, чтобы тест упал с понятным логом
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

        public static async Task<String> GetFullURL(String method, IPage _page)
        {
            // 1. Динамически получаем базовый URL текущего стенда (локального или серверного)
            var currentUri = new Uri(_page.Url);
            string fullApiUrl;
            string cleanMethod = method.StartsWith("/") ? method.Substring(1) : method;

            // 2. Проверяем, локальный ли это запуск (localhost или 127.0.0.1)
            if (currentUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                currentUri.Host.Equals("127.0.0.1"))
            {
                // ИСПРАВЛЕНО: Для локального запуска жестко прописываем порт бэкенда 60254
                // вместо порта фронтенда (4200), который открыт в браузере
                string localApiPort = ":60254";
                fullApiUrl = $"https://{currentUri.Host}{localApiPort}/{cleanMethod}";
            }
            else
            {
                // 3. Если серверный стенд — извлекаем окружение (qa, stg, dev и т.д.) из хоста страницы
                // Например: из "qa.careadminplus.com" получаем первое слово до точки — "qa"
                string environment = currentUri.Host.Split('.')[0].ToLower();

                // На случай, если в URL страницы вообще нет поддомена (просто careadminplus.com), ставим дефолт "qa"
                if (environment == "careadminplus" || string.IsNullOrEmpty(environment))
                {
                    environment = "qa";
                }

                // 4. Собираем целевой URL к Azure API с динамической подстановкой окружения
                fullApiUrl = $"https://cad-api-eu-{environment}.azurewebsites.net/{method}";
            }

            // Logging для отладки в консоли тестов
            Log.Information($"[API GET] Сформирован целевой URL: {fullApiUrl}");
            return fullApiUrl;
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
