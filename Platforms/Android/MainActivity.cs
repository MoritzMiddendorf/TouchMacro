using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;

namespace TouchMacro;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnResume()
    {
        base.OnResume();
        
        // Force permission check on resume
        MainThread.BeginInvokeOnMainThread(async () => 
        {
            var permissionService = MauiApplication.Current.Services.GetService<TouchMacro.Services.PermissionService>();
            if (permissionService != null)
            {
                await Task.Delay(500); // Short delay to ensure app is fully loaded
                await permissionService.CheckAllPermissionsAsync();
            }
        });
    }
}