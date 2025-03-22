using Microsoft.Extensions.Logging;
using TouchMacro.Services;
using TouchMacro.ViewModels;
using SQLite;
using System.Reflection;
using System.IO;

namespace TouchMacro;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        // Configure logging
#if DEBUG
        builder.Logging.AddDebug();
#endif
        
        // Register core application services
        builder.Services.AddSingleton<ExceptionHandlerService>();
        
        // Register domain services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<MacroRecorderService>();
        builder.Services.AddSingleton<MacroPlayerService>();
        builder.Services.AddSingleton<PermissionService>();
        
        // Register view models
        builder.Services.AddSingleton<MacroListViewModel>();
        
        // Register pages
        builder.Services.AddSingleton<MainPage>();
        
        // Register app as a service to enable constructor injection
        builder.Services.AddSingleton<App>();
        
        // Configure app with dependency injection
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        return builder.Build();
    }
}