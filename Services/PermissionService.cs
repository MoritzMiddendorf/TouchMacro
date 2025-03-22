using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TouchMacro.Services
{
    /// <summary>
    /// Service for checking and requesting necessary permissions
    /// </summary>
    public class PermissionService
    {
        private readonly ILogger<PermissionService> _logger;
        
        public PermissionService(ILogger<PermissionService> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Checks if the overlay permission is granted
        /// </summary>
        public async Task<bool> CheckOverlayPermissionAsync()
        {
#if ANDROID
            return await CheckPlatformOverlayPermission();
#else
            _logger.LogWarning("Overlay permission check is only implemented for Android");
            return true;
#endif
        }
        
        /// <summary>
        /// Requests the overlay permission
        /// </summary>
        public async Task RequestOverlayPermissionAsync()
        {
#if ANDROID
            await RequestPlatformOverlayPermission();
#else
            _logger.LogWarning("Overlay permission request is only implemented for Android");
#endif
        }
        
        /// <summary>
        /// Checks if the accessibility service is enabled
        /// </summary>
        public async Task<bool> CheckAccessibilityServiceEnabledAsync()
        {
#if ANDROID
            return await CheckPlatformAccessibilityService();
#else
            _logger.LogWarning("Accessibility service check is only implemented for Android");
            return true;
#endif
        }
        
        /// <summary>
        /// Opens the accessibility settings page
        /// </summary>
        public async Task OpenAccessibilitySettingsAsync()
        {
#if ANDROID
            await OpenPlatformAccessibilitySettings();
#else
            _logger.LogWarning("Opening accessibility settings is only implemented for Android");
#endif
        }
        
        /// <summary>
        /// Checks if all required permissions are granted
        /// </summary>
        public async Task<bool> CheckAllPermissionsAsync()
        {
            var hasOverlay = await CheckOverlayPermissionAsync();
            var hasAccessibility = await CheckAccessibilityServiceEnabledAsync();
            
            _logger.LogInformation($"Permission status - Overlay: {hasOverlay}, Accessibility: {hasAccessibility}");
            
            return hasOverlay && hasAccessibility;
        }
        
#if ANDROID
        private async Task<bool> CheckPlatformOverlayPermission()
        {
            try
            {
                // Android-specific implementation in platform code
                return await TouchMacro.Platforms.Android.Services.PermissionHelper.CheckOverlayPermissionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking overlay permission");
                return false;
            }
        }
        
        private async Task RequestPlatformOverlayPermission()
        {
            try
            {
                // Android-specific implementation in platform code
                await TouchMacro.Platforms.Android.Services.PermissionHelper.RequestOverlayPermissionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting overlay permission");
            }
        }
        
        private async Task<bool> CheckPlatformAccessibilityService()
        {
            try
            {
                // Android-specific implementation in platform code
                return await TouchMacro.Platforms.Android.Services.PermissionHelper.CheckAccessibilityServiceEnabledAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking accessibility service");
                return false;
            }
        }
        
        private async Task OpenPlatformAccessibilitySettings()
        {
            try
            {
                // Android-specific implementation in platform code
                await TouchMacro.Platforms.Android.Services.PermissionHelper.OpenAccessibilitySettingsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening accessibility settings");
            }
        }
#endif
    }
}