using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Views.Accessibility;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Maui.Platform;
using Microsoft.Maui.ApplicationModel;
using Android.OS;

// Using aliases to resolve ambiguous references
using AndroidView = global::Android.Views.View;

namespace TouchMacro.Platforms.Android.Services
{
    /// <summary>
    /// Android Accessibility Service for simulating taps on the screen
    /// </summary>
    [Service(Label = "TouchMacro", Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE", Exported = false)]
    [IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
    [MetaData("android.accessibilityservice", Resource = "@xml/accessibility_service_config")]
    public class MacroAccessibilityService : AccessibilityService
    {
        private static MacroAccessibilityService? _instance;
        private ILogger? _logger;
        private GestureDescription.Builder? _gestureBuilder;
        private bool CanPerformGestures = true;
        
        /// <summary>
        /// Gets the singleton instance of the service
        /// </summary>
        public static MacroAccessibilityService? Instance => _instance;
        
        public override void OnCreate()
        {
            base.OnCreate();
            _instance = this;
            
            // Get logger from MauiApp
            // Use Microsoft.Maui.ApplicationModel
            _logger = IPlatformApplication.Current.Services.GetService<ILogger<MacroAccessibilityService>>();
            _logger?.LogInformation("Accessibility Service created");
        }
        
        public override void OnDestroy()
        {
            _instance = null!;
            _logger?.LogInformation("Accessibility Service destroyed");
            base.OnDestroy();
        }
        
        public override void OnAccessibilityEvent(AccessibilityEvent? e)
        {
            // We don't need to process accessibility events for our use case
        }
        
        public override void OnInterrupt()
        {
            // Required by the AccessibilityService interface
        }
        
        protected override bool OnGesture(int gestureId)
        {
            // We don't use gesture detection in this service
            return base.OnGesture(gestureId);
        }
        
        protected override void OnServiceConnected()
        {
            base.OnServiceConnected();
            _logger?.LogInformation("Accessibility Service connected");
        }
        
        /// <summary>
        /// Simulates a tap at the specified coordinates
        /// </summary>
        public bool SimulateTap(float x, float y)
        {
            if (!CanPerformGestures)
            {
                _logger?.LogWarning("Cannot perform gestures - permission not granted");
                return false;
            }
            
            try
            {
                _logger?.LogInformation($"Simulating tap at ({x}, {y})");
                
                // Create a tap gesture using the GestureDescription API (Android 7.0+)
                // API 26+ (Android 8.0+) always supports GestureDescription
                _gestureBuilder = new GestureDescription.Builder();
                var path = new global::Android.Graphics.Path();
                path.MoveTo(x, y);
                
                // Add the path to the gesture - duration 1ms, starting at 0ms
                _gestureBuilder.AddStroke(new GestureDescription.StrokeDescription(path, 0, 1));
                
                // Dispatch the gesture
                return DispatchGesture(_gestureBuilder.Build(), null, null);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error simulating tap");
                return false;
            }
        }
    }
}