using Microsoft.Extensions.Logging;
using TouchMacro.Services;
using TouchMacro.ViewModels;
using SQLite;

namespace TouchMacro;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<MacroRecorderService>();
        builder.Services.AddSingleton<MacroPlayerService>();
        builder.Services.AddSingleton<PermissionService>();
        
        // Register view models
        builder.Services.AddSingleton<MacroListViewModel>();
        
        // Register pages
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}