using Microsoft.Playwright;
using Serilog;
using System.Buffers.Text;

namespace TestProject1.Common
{
    public class AuthSetup
    {
        // Формируем надежный путь к файлу в папке запуска тестов
        public static async Task CreateLogin(IBrowser browser, IBrowserContext authContext, IPage authPage, List<string> networkErrors, string BaseUrl, string StatePath)
        {
            try
            {
                Log.Information($"Переход на базовый URL: {BaseUrl}");
                var mainResponse = await authPage.GotoAsync("/");

                if (mainResponse == null || mainResponse.Status >= 400)
                {
                    var status = mainResponse?.Status.ToString() ?? "Unknown";
                    throw new Exception($"Не удалось загрузить стартовую страницу. HTTP Status: {status}");
                }

                // ================= ВОЗВРАЩАЕМ И ИСПРАВЛЯЕМ ПРОВЕРКУ СЕССИИ =================
                var regexHome = new System.Text.RegularExpressions.Regex(@"\/home$");

                // Даем странице 1 секунду на автоматический редирект, если токен еще живой
                await authPage.WaitForTimeoutAsync(1000);

                // Проверяем, перекинуло ли нас на /home автоматически
                if (regexHome.IsMatch(authPage.Url))
                {
                    Log.Information("Обнаружена active session (/home). Пропускаем ввод логина и обновляем state.json...");
                    await authContext.StorageStateAsync(new() { Path = StatePath });
                    return;
                }

                Log.Debug("Проверяем доступность полей авторизации...");
                var usernameInput = authPage.GetByPlaceholder("Username");

                try
                {
                    // Ждем появления поля Username не более 5 секунд
                    await usernameInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                }
                catch (TimeoutException)
                {
                    Log.Warning("Поле ввода Username не появилось. Принудительно очищаем контекст сессии...");
                    await authContext.ClearCookiesAsync();
                    await authPage.EvaluateAsync("() => localStorage.clear()");
                    await authPage.EvaluateAsync("() => sessionStorage.clear()");

                    await authPage.GotoAsync("/");
                    await usernameInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
                }
                // ============================================================================

                Log.Debug("Заполнение полей авторизации...");
                await usernameInput.FillAsync("polly@test.ts");
                await authPage.GetByPlaceholder("Password").FillAsync("Qwert1@#");

                Log.Information("Нажатие кнопки SIGN IN...");
                await authPage.GetByRole(AriaRole.Button, new() { Name = "SIGN IN" }).ClickAsync();


                Log.Debug("Ожидание авторизации и проверка ошибок...");

                // Инициализируем локатор для плашки ошибки на форме
                var errorBanner = authPage.Locator(".mat-mdc-card, form, mat-error, .error-message, [role='alert']")
                                          .Filter(new() { HasText = "error" })
                                          .First;

                // Ждем либо успешного изменения URL, либо появления ошибки на форме (макс. 15 секунд)
                bool isSuccess = false;
                var stopWatch = System.Diagnostics.Stopwatch.StartNew();

                while (stopWatch.ElapsedMilliseconds < 15000)
                {
                    // Проверяем, ушли ли мы на /home
                    if (regexHome.IsMatch(authPage.Url))
                    {
                        isSuccess = true;
                        break;
                    }

                    // Проверяем, появилась ли ошибка на UI
                    if (await errorBanner.IsVisibleAsync())
                    {
                        var errorText = await errorBanner.InnerTextAsync();
                        var networkSummary = networkErrors.Any() ? string.Join(Environment.NewLine, networkErrors) : "Нет сетевых ошибок";
                        throw new Exception($"Авторизация прервана. На форме отображена ошибка: '{errorText.Trim()}'.{Environment.NewLine}Лог сети:{Environment.NewLine}{networkSummary}");
                    }

                    // Небольшая пауза между проверками, чтобы не нагружать поток
                    await Task.Delay(200);
                }

                if (!isSuccess)
                {
                    throw new TimeoutException($"Превышено время ожидания перехода на /home. Текущий URL: {authPage.Url}");
                }
                Log.Debug("Проверка видимости элемента 'My Tasks'...");
                // 1. Привязываемся к тегу span с текстом "My Tasks" (как в вашем DOM)
                var myTasksTitle = authPage.Locator("span.title").Filter(new() { HasText = "My Tasks" });
                await myTasksTitle.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

                Log.Debug("Ожидание появления токена in LocalStorage...");
                await authPage.WaitForFunctionAsync(@"() => {
                    for (let i = 0; i < localStorage.length; i++) {
                        if (localStorage.getItem(localStorage.key(i)).includes('accessToken')) return true;
                    }
                    return false;
                }", null, new PageWaitForFunctionOptions { Timeout = 10000 });

                await authPage.WaitForTimeoutAsync(1000);

                await authContext.StorageStateAsync(new() { Path = StatePath });
                Log.Information("Successful log in. Auth state saved.");
            }
            catch (Exception ex)
            {
                var networkSummary = networkErrors.Any() ? string.Join(Environment.NewLine, networkErrors) : "Лог сетевых запросов пуст";
                Log.Error(ex, $"[AUTH CRASH] Ошибка создания AuthState.{Environment.NewLine}Текущий URL: {authPage.Url}{Environment.NewLine}Сетевые ошибки:{Environment.NewLine}{networkSummary}");
                throw;
            }
            finally
            {
                // Этот блок должен остаться полностью пустым.
            }
        }
    }
}