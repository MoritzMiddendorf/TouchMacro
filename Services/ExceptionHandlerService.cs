using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace TouchMacro.Services
{
    /// <summary>
    /// Service for handling unhandled exceptions globally
    /// </summary>
    public class ExceptionHandlerService
    {
        private readonly ILogger<ExceptionHandlerService> _logger;

        public ExceptionHandlerService(ILogger<ExceptionHandlerService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registers global exception handlers for the app
        /// </summary>
        public void RegisterGlobalExceptionHandlers()
        {
            // Handle exceptions from the .NET runtime
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            // Handle exceptions from tasks
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// Handles unhandled exceptions from the AppDomain
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            _logger.LogCritical(exception, "Unhandled AppDomain exception");
            ShowCrashDialog(exception, "AppDomain.UnhandledException");
        }

        /// <summary>
        /// Handles unobserved task exceptions
        /// </summary>
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogCritical(e.Exception, "Unobserved Task exception");
            ShowCrashDialog(e.Exception, "TaskScheduler.UnobservedTaskException");
            
            // Mark as observed to prevent the app from crashing
            e.SetObserved();
        }

        /// <summary>
        /// Shows a crash dialog to the user and terminates the app
        /// </summary>
        public async void ShowCrashDialog(Exception exception, string source)
        {
            try
            {
                // Make sure we're on the UI thread
                if (!MainThread.IsMainThread)
                {
                    MainThread.BeginInvokeOnMainThread(() => ShowCrashDialog(exception, source));
                    return;
                }

                // Create error details for potential reporting
                string errorDetails = $"Source: {source}\n" +
                                    $"Exception Type: {exception?.GetType().FullName}\n" +
                                    $"Message: {exception?.Message}\n" +
                                    $"Stack Trace: {exception?.StackTrace}";

                // Log the complete error details
                _logger.LogCritical(errorDetails);

                // Make sure we have a MainPage to show the dialog
                if (Application.Current?.MainPage == null)
                {
                    _logger.LogCritical("Cannot show error dialog: MainPage is null");
                    return;
                }

                // Show error dialog to the user
                bool report = await Application.Current.MainPage.DisplayAlert(
                    "TouchMacro Has Crashed",
                    "We're sorry, but TouchMacro has encountered an unexpected error and needs to close.\n\n" +
                    "Would you like to report this issue on GitHub?",
                    "Report on GitHub",
                    "Close App");

                if (report)
                {
                    // Open GitHub issue page with error details
                    string issueTitle = Uri.EscapeDataString($"App crash: {exception?.GetType().Name}");
                    string issueBody = Uri.EscapeDataString(
                        $"## Error Details\n" +
                        $"- App Version: {AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})\n" +
                        $"- Device: {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}\n" +
                        $"- OS: {DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}\n\n" +
                        $"## Exception Information\n" +
                        $"```\n{errorDetails}\n```\n\n" +
                        $"## Steps to Reproduce\n" +
                        $"1. [Please describe what you were doing when the crash occurred]\n\n" +
                        $"## Additional Information\n" +
                        $"[Any other information that might be helpful]");

                    string issueUrl = $"https://github.com/MoritzMiddendorf/TouchMacro/issues/new?title={issueTitle}&body={issueBody}";
                    await Browser.OpenAsync(issueUrl, BrowserLaunchMode.SystemPreferred);
                }

                // Terminate the app
                Application.Current.Quit();
            }
            catch (Exception ex)
            {
                // Last resort if our error handler itself crashes
                _logger.LogCritical(ex, "Exception in crash handler");
                Application.Current.Quit();
            }
        }
    }
}