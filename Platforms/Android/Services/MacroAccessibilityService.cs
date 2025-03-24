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
using TouchMacro.Models;

// Using aliases to resolve ambiguous references
using AndroidView = global::Android.Views.View;

namespace TouchMacro.Platforms.Android.Services
{
    /// <summary>
    /// Android Accessibility Service for simulating taps and drags on the screen
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
            
            _logger?.LogInformation($"Simulating tap at ({x}, {y})");
            
            // Create a tap gesture using the GestureDescription API
            _gestureBuilder = new GestureDescription.Builder();
            var path = new global::Android.Graphics.Path();
            path.MoveTo(x, y);
            
            // Add the path to the gesture - duration 1ms, starting at 0ms
            _gestureBuilder.AddStroke(new GestureDescription.StrokeDescription(path, 0, 1));
            
            // Dispatch the gesture
            return DispatchGesture(_gestureBuilder.Build(), null, null);
        }
        
        /// <summary>
        /// Simulates a drag from one point to another
        /// </summary>
        public bool SimulateDrag(float startX, float startY, float endX, float endY, long durationMs)
        {
            if (!CanPerformGestures)
            {
                _logger?.LogWarning("Cannot perform gestures - permission not granted");
                return false;
            }
            
            _logger?.LogInformation($"Simulating drag from ({startX}, {startY}) to ({endX}, {endY}) with duration {durationMs}ms");
            
            // Ensure a minimum duration for the drag
            if (durationMs < 100) durationMs = 100;
            
            // Create a drag gesture using the GestureDescription API
            _gestureBuilder = new GestureDescription.Builder();
            var path = new global::Android.Graphics.Path();
            path.MoveTo(startX, startY);
            path.LineTo(endX, endY); // Create a straight line from start to end
            
            // Add the path to the gesture - with the specified duration
            _gestureBuilder.AddStroke(new GestureDescription.StrokeDescription(path, 0, durationMs));
            
            // Dispatch the gesture
            return DispatchGesture(_gestureBuilder.Build(), null, null);
        }
        
        /// <summary>
        /// Simulates a complex drag path
        /// </summary>
        public bool SimulateComplexDrag(MacroAction[] dragActions)
        {
            if (!CanPerformGestures || dragActions.Length < 2)
            {
                _logger?.LogWarning("Cannot perform complex drag - invalid state or actions");
                return false;
            }
            
            _logger?.LogInformation($"Simulating complex drag with {dragActions.Length} points");
            
            // Create a path from the drag actions
            var path = new global::Android.Graphics.Path();
            
            // Start at the first point
            path.MoveTo(dragActions[0].X, dragActions[0].Y);
            
            // Calculate total duration
            long totalDuration = 0;
            
            // Add all points to the path
            for (int i = 1; i < dragActions.Length; i++)
            {
                var action = dragActions[i];
                
                // Add each point to the path
                path.LineTo(action.X, action.Y);
                
                // Add the delay to the total duration
                totalDuration += action.DelayMs;
            }
            
            // Ensure a minimum duration
            if (totalDuration < 100) totalDuration = 100;
            
            // Create the gesture
            _gestureBuilder = new GestureDescription.Builder();
            _gestureBuilder.AddStroke(new GestureDescription.StrokeDescription(path, 0, totalDuration));
            
            // Dispatch the gesture
            return DispatchGesture(_gestureBuilder.Build(), null, null);
        }
        
        /// <summary>
        /// Executes a macro action
        /// </summary>
        public bool ExecuteAction(MacroAction action, MacroAction? previousAction = null)
        {
            switch (action.ActionType)
            {
                case ActionType.Tap:
                    return SimulateTap(action.X, action.Y);
                    
                case ActionType.DragStart:
                    // Just wait for the next action
                    return true;
                    
                case ActionType.DragMove:
                    // Only process if we have a previous action
                    if (previousAction != null && 
                        (previousAction.ActionType == ActionType.DragStart || 
                         previousAction.ActionType == ActionType.DragMove))
                    {
                        return SimulateDrag(previousAction.X, previousAction.Y, action.X, action.Y, action.DelayMs);
                    }
                    return false;
                    
                case ActionType.DragEnd:
                    // Only process if we have a previous action
                    if (previousAction != null && 
                        (previousAction.ActionType == ActionType.DragStart || 
                         previousAction.ActionType == ActionType.DragMove))
                    {
                        return SimulateDrag(previousAction.X, previousAction.Y, action.X, action.Y, action.DelayMs);
                    }
                    return false;
                    
                default:
                    _logger?.LogWarning($"Unknown action type: {action.ActionType}");
                    return false;
            }
        }
    }
}