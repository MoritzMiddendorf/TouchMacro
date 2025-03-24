using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;
using TouchMacro.Platforms.Android.Services;

namespace TouchMacro;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleIntent(Intent);
    }
    
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleIntent(intent);
    }
    
    private void HandleIntent(Intent? intent)
    {
        if (intent?.GetBooleanExtra("showSaveDialog", false) == true)
        {
            MainThread.BeginInvokeOnMainThread(() => 
            {
                TouchMacro.Platforms.Android.Services.OverlayService.Instance?.ShowSaveMacroDialog();
            });
        }
    }
    
    protected override void OnResume()
    {
        base.OnResume();
        
        // Force permission check on resume
        MainThread.BeginInvokeOnMainThread(async () => 
        {
            var permissionService = MauiApplication.Current.Services.GetService<TouchMacro.Services.PermissionService>();
            if (permissionService != null)
            {
                await permissionService.CheckAllPermissionsAsync();
            }
        });
    }
}