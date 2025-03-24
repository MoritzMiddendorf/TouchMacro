using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using System.Threading.Tasks;
using AndroidX.Core.Content;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Android.Content.PM;
using System;

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
            // For API 26+ (Android 8.0+), we always check overlay permission
            return Task.FromResult(Settings.CanDrawOverlays(Platform.CurrentActivity));
        }
        
        /// <summary>
        /// Requests the overlay permission by opening the system settings
        /// </summary>
        public static Task RequestOverlayPermissionAsync()
        {
            // For API 26+ (Android 8.0+), we always need to request this permission explicitly
            var intent = new Intent(Settings.ActionManageOverlayPermission);
            intent.SetData(global::Android.Net.Uri.Parse($"package:{Platform.CurrentActivity.PackageName}"));
            Platform.CurrentActivity.StartActivity(intent);
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Checks if our accessibility service is enabled
        /// </summary>
        public static Task<bool> CheckAccessibilityServiceEnabledAsync()
        {
            try
            {
                var context = Platform.CurrentActivity;
                var packageName = context.PackageName;
                var serviceName = Java.Lang.Class.FromType(typeof(TouchMacro.Platforms.Android.Services.MacroAccessibilityService)).CanonicalName;
                
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
                    System.Diagnostics.Debug.WriteLine($"Failed to check accessibility enabled setting");
                    return Task.FromResult(false);
                }
                
                if (accessibilityEnabled == 1)
                {
                    // Check if our service is in the enabled services list
                    string settingValue = Settings.Secure.GetString(
                        context.ContentResolver,
                        Settings.Secure.EnabledAccessibilityServices
                    );
                    
                    System.Diagnostics.Debug.WriteLine($"Checking if service is enabled: {serviceName}");
                    System.Diagnostics.Debug.WriteLine($"Enabled services: {settingValue}");
                    
                    var fullyQualifiedServiceName = packageName + "/" + serviceName;
                    var result = settingValue != null && (
                        settingValue.Contains(serviceName) || 
                        settingValue.Contains(fullyQualifiedServiceName));
                    return Task.FromResult(result);
                }
                
                System.Diagnostics.Debug.WriteLine("Accessibility services not enabled");
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking accessibility service: {ex}");
                return Task.FromResult(false);
            }
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
        
        /// <summary>
        /// Checks if the foreground service permission is granted
        /// </summary>
        public static Task<bool> CheckForegroundServicePermissionAsync()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.P) // Android 9 (API 28) or higher
                {
                    var context = Platform.CurrentActivity;
                    
                    // Check if the permission is granted
                    var permissionResult = context.CheckSelfPermission("android.permission.FOREGROUND_SERVICE");
                    bool isGranted = permissionResult == Permission.Granted;
                    
                    System.Diagnostics.Debug.WriteLine($"FOREGROUND_SERVICE permission check result: {isGranted}");
                    
                    // For app installed from Play Store, this permission is automatically granted if declared in manifest
                    // During development, we'll assume it's granted since it's in the manifest
                    // This is because FOREGROUND_SERVICE is a "normal" permission that's automatically granted if declared
                    #if DEBUG
                    isGranted = true;
                    #endif
                    
                    return Task.FromResult(isGranted);
                }
                
                // For Android versions below 9, foreground service permission is not required
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking foreground service permission: {ex}");
                return Task.FromResult(false);
            }
        }
        
        /// <summary>
        /// Requests the foreground service permission by opening the app permissions settings
        /// </summary>
        public static Task RequestForegroundServicePermissionAsync()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.P) // Android 9 (API 28) or higher
                {
                    // Open app settings page since foreground permission can't be requested directly
                    var intent = new Intent(global::Android.Provider.Settings.ActionApplicationDetailsSettings);
                    var uri = global::Android.Net.Uri.FromParts("package", Platform.CurrentActivity.PackageName, null);
                    intent.SetData(uri);
                    Platform.CurrentActivity.StartActivity(intent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error requesting foreground service permission: {ex}");
            }
            
            return Task.CompletedTask;
        }
    }
}