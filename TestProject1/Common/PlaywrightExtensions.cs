using Microsoft.Playwright;

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
