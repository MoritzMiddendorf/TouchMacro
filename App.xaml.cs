using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using TouchMacro.Services;

namespace TouchMacro
{
    public partial class App : Application
    {
        private readonly ILogger<App> _logger;
        private readonly ExceptionHandlerService _exceptionHandler;

        public App(ILogger<App> logger, ExceptionHandlerService exceptionHandler)
        {
            _logger = logger;
            _exceptionHandler = exceptionHandler;
            
            InitializeComponent();
            
            // Register global exception handlers
            _exceptionHandler.RegisterGlobalExceptionHandlers();
            
            _logger.LogInformation("TouchMacro application starting");
            
            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            Window window = base.CreateWindow(activationState);
            
            // Handle window-level exceptions
            window.Resumed += (s, e) => _logger.LogInformation("Window resumed");
            window.Stopped += (s, e) => _logger.LogInformation("Window stopped");
            window.Destroying += (s, e) => _logger.LogInformation("Window destroying");
            
            return window;
        }
    }
}
