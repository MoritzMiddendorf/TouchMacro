using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using System.Threading.Tasks;
using AndroidX.Core.Content;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace TouchMacro.Platforms.Android.Services
{
    /// <summary>
    /// Helper class for Android-specific permission handling
    /// </summary>
    public static class PermissionHelper
    {
        /// <summary>
        /// Checks if the overlay permission is granted
        /// </summary>
        public static Task<bool> CheckOverlayPermissionAsync()
        {
            // On Android 6.0+ (API level 23+), check for overlay permission
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                return Task.FromResult(Settings.CanDrawOverlays(Platform.CurrentActivity));
            }
            
            // On older versions, this permission is granted by default
            return Task.FromResult(true);
        }
        
        /// <summary>
        /// Requests the overlay permission by opening the system settings
        /// </summary>
        public static Task RequestOverlayPermissionAsync()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var intent = new Intent(Settings.ActionManageOverlayPermission);
                intent.SetData(global::Android.Net.Uri.Parse($"package:{Platform.CurrentActivity.PackageName}"));
                Platform.CurrentActivity.StartActivity(intent);
            }
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Checks if our accessibility service is enabled
        /// </summary>
        public static Task<bool> CheckAccessibilityServiceEnabledAsync()
        {
            var context = Platform.CurrentActivity;
            var packageName = context.PackageName;
            var serviceName = $"{packageName}.Platforms.Android.Services.MacroAccessibilityService";
            
            var accessibilityEnabled = 0;
            try
            {
                accessibilityEnabled = Settings.Secure.GetInt(
                    context.ContentResolver,
                    Settings.Secure.AccessibilityEnabled
                );
            }
            catch (global::Android.Database.Sqlite.SQLiteException)
            {
                // Setting not found, assume not enabled
                return Task.FromResult(false);
            }
            
            if (accessibilityEnabled == 1)
            {
                // Check if our service is in the enabled services list
                string settingValue = Settings.Secure.GetString(
                    context.ContentResolver,
                    Settings.Secure.EnabledAccessibilityServices
                );
                
                return Task.FromResult(settingValue != null && settingValue.Contains(serviceName));
            }
            
            return Task.FromResult(false);
        }
        
        /// <summary>
        /// Opens the accessibility settings page
        /// </summary>
        public static Task OpenAccessibilitySettingsAsync()
        {
            var intent = new Intent(Settings.ActionAccessibilitySettings);
            Platform.CurrentActivity.StartActivity(intent);
            
            return Task.CompletedTask;
        }
    }
}