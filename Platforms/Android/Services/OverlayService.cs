using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Extensions.Logging;
using TouchMacro.Services;
using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

// Using aliases to resolve ambiguous references
using AndroidView = global::Android.Views.View;
using AndroidImageButton = global::Android.Widget.ImageButton;
using AndroidButton = global::Android.Widget.Button;
using Android.Content.PM;

namespace TouchMacro.Platforms.Android.Services
{
    /// <summary>
    /// Android service for showing an overlay with macro controls
    /// </summary>
    [Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMediaPlayback)]
    public class OverlayService : Service
    {
        // Constants
        private const int NotificationId = 1001;
        private const string NotificationChannelId = "TouchMacroOverlay";
        
        // Dependencies
        private ILogger<OverlayService>? _logger;
        private MacroRecorderService? _recorderService;
        private MacroPlayerService? _playerService;
        
        // Android-specific fields
        private WindowManagerLayoutParams? _layoutParams;
        private WindowManagerLayoutParams? _touchCaptureLayoutParams;
        private IWindowManager? _windowManager;
        private AndroidView? _overlayView;
        private AndroidView? _touchCaptureView;
        private AndroidImageButton? _recordButton;
        private AndroidImageButton? _playButton;
        private AndroidImageButton? _settingsButton;
        private bool _isOverlayShown = false;
        private bool _isTouchCaptureShown = false;
        private GestureDetector? _gestureDetector;
        private ScreenTouchListener? _touchListener;
        
        // Static accessor
        private static OverlayService? _instance;
        public static OverlayService? Instance => _instance;
        
        /// <summary>
        /// Called when the service is created
        /// </summary>
        public override void OnCreate()
        {
            base.OnCreate();
            _instance = this;
            
            // Get the services from MauiApp
            // Use Microsoft.Maui.ApplicationModel
            var services = IPlatformApplication.Current.Services;
            _logger = services.GetService<ILogger<OverlayService>>();
            _recorderService = services.GetService<MacroRecorderService>();
            _playerService = services.GetService<MacroPlayerService>();
            
            _logger?.LogInformation("Overlay service created");
            
            // Set up overlay parameters
            SetupOverlayParams();
            
            // Create notification channel (required for foreground service)
            CreateNotificationChannel();
            
            // Subscribe to player events
            if (_playerService != null)
            {
                _playerService.PlaybackStarted += OnPlaybackStarted;
                _playerService.PlaybackStopped += OnPlaybackStopped;
                _playerService.OnActionRequested += OnActionRequested;
            }
        }
        
        /// <summary>
        /// Sets up the layout parameters for the overlay window
        /// </summary>
        private void SetupOverlayParams()
        {
            _windowManager = GetSystemService(WindowService).JavaCast<IWindowManager>();
            
            // Create layout parameters for the control overlay
            _layoutParams = new WindowManagerLayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent,
                WindowManagerTypes.ApplicationOverlay, // API 26+ (Android 8.0+) only uses ApplicationOverlay
                WindowManagerFlags.NotFocusable,
                Format.Translucent
            );
            
            // Set initial position
            _layoutParams.Gravity = GravityFlags.Top | GravityFlags.Left;
            _layoutParams.X = 100;
            _layoutParams.Y = 100;
            
            // Create layout parameters for the touch capture overlay
            _touchCaptureLayoutParams = new WindowManagerLayoutParams(
                ViewGroup.LayoutParams.MatchParent,  // Full screen width
                ViewGroup.LayoutParams.MatchParent,  // Full screen height
                WindowManagerTypes.ApplicationOverlay,
                WindowManagerFlags.NotFocusable | WindowManagerFlags.NotTouchModal | WindowManagerFlags.LayoutInScreen,
                Format.Translucent
            );
            
            // Position at top-left and full screen
            _touchCaptureLayoutParams.Gravity = GravityFlags.Top | GravityFlags.Left;
            _touchCaptureLayoutParams.X = 0;
            _touchCaptureLayoutParams.Y = 0;
        }
        
        /// <summary>
        /// Creates a notification channel for the foreground service
        /// </summary>
        private void CreateNotificationChannel()
        {
            // API 26+ (Android 8.0+) always requires a notification channel
            var channel = new NotificationChannel(
                NotificationChannelId,
                "TouchMacro Overlay",
                NotificationImportance.Low
            )
            {
                Description = "Notification channel for TouchMacro overlay service"
            };
            
            var notificationManager = GetSystemService(NotificationService).JavaCast<NotificationManager>();
            notificationManager.CreateNotificationChannel(channel);
        }
        
        /// <summary>
        /// Creates the foreground notification
        /// </summary>
        private Notification CreateNotification()
        {
            var pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                new Intent(this, typeof(MainActivity)),
                PendingIntentFlags.Immutable
            );
            
            // API 26+ (Android 8.0+) always requires a channel ID
            var notificationBuilder = new Notification.Builder(this, NotificationChannelId)
                .SetContentTitle("TouchMacro")
                .SetContentText("TouchMacro overlay is active")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetContentIntent(pendingIntent);
            
            return notificationBuilder.Build();
        }
        
        /// <summary>
        /// Called when the service is started
        /// </summary>
        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            _logger?.LogInformation("Overlay service started");
            
            // Start foreground service with notification and type
            // API 29+ requires foreground service type, but we target API 26, so we use this safely
            // because we have appropriate fallback behavior for API 26-28
            try
            {
                StartForeground(NotificationId, CreateNotification(), ForegroundService.TypeMediaPlayback);
            }
            catch (Java.Lang.NoSuchMethodError)
            {
                // Fallback for API 26-28
                StartForeground(NotificationId, CreateNotification());
            }
            
            // Show the overlay if it's not already showing
            if (!_isOverlayShown)
            {
                ShowOverlay();
            }
            
            return StartCommandResult.Sticky;
        }
        
        /// <summary>
        /// Shows the overlay on the screen
        /// </summary>
        private void ShowOverlay()
        {
            if (_isOverlayShown)
            {
                return;
            }
            
            try
            {
                // Inflate the overlay layout
                var inflater = LayoutInflater.From(this);
                _overlayView = inflater.Inflate(Resource.Layout.overlay_controls, null);
                
                // Set up the overlay buttons
                SetupOverlayButtons();
                
                // Set up touch handling for dragging
                SetupTouchHandling();
                
                // Add the view to the window manager
                // We're using AndroidView and not Microsoft.Maui.Controls.View here
                _windowManager.AddView(_overlayView, _layoutParams);
                _isOverlayShown = true;
                
                _logger?.LogInformation("Overlay shown");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing overlay");
            }
        }
        
        /// <summary>
        /// Sets up the overlay buttons and their click handlers
        /// </summary>
        private void SetupOverlayButtons()
        {
            // Get button references
            _recordButton = _overlayView.FindViewById<AndroidImageButton>(Resource.Id.recordButton);
            _playButton = _overlayView.FindViewById<AndroidImageButton>(Resource.Id.playButton);
            _settingsButton = _overlayView.FindViewById<AndroidImageButton>(Resource.Id.settingsButton);
            
            // Set up click listeners
            _recordButton.Click += OnRecordButtonClick;
            _playButton.Click += OnPlayButtonClick;
            _settingsButton.Click += OnSettingsButtonClick;
            
            // Update button states
            UpdateButtonStates();
        }
        
        /// <summary>
        /// Sets up touch handling for the overlay (for dragging)
        /// </summary>
        private void SetupTouchHandling()
        {
            // Create a gesture detector for handling drags
            _gestureDetector = new GestureDetector(this, new OverlayGestureListener(this, _logger));
            
            // Set the touch listener on the overlay view
            _overlayView.Touch += (sender, e) => {
                _gestureDetector.OnTouchEvent(e.Event);
                e.Handled = true;
            };
        }
        
        /// <summary>
        /// Updates the state of the buttons based on recording/playback state
        /// </summary>
        private void UpdateButtonStates()
        {
            if (_recordButton == null || _playButton == null)
                return;
                
            // Update record button state
            bool isRecording = _recorderService?.IsRecording ?? false;
            _recordButton.Selected = isRecording;
            _recordButton.ContentDescription = isRecording ? "Stop Recording" : "Start Recording";
            
            // Update play button state
            bool isPlaying = _playerService?.IsPlaying ?? false;
            _playButton.Selected = isPlaying;
            _playButton.Enabled = !isRecording && !isPlaying;
            _playButton.ContentDescription = isPlaying ? "Stop Playback" : "Start Playback";
        }
        
        /// <summary>
        /// Shows the touch capture overlay for recording touches
        /// </summary>
        private void ShowTouchCaptureOverlay()
        {
            if (_isTouchCaptureShown || _windowManager == null)
                return;
                
            // Create a fully transparent view to capture touches
            var context = this.ApplicationContext;
            _touchCaptureView = new AndroidView(context);
            
            // Make the view completely transparent
            _touchCaptureView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            
            // Create and set the touch listener that will record touches
            _touchListener = new ScreenTouchListener(this, _recorderService, _logger);
            _touchCaptureView.SetOnTouchListener(_touchListener);
            
            // Add the view to the window manager
            _windowManager.AddView(_touchCaptureView, _touchCaptureLayoutParams);
            _isTouchCaptureShown = true;
            
            _logger?.LogInformation("Touch capture overlay shown");
        }
        
        /// <summary>
        /// Hides the touch capture overlay
        /// </summary>
        private void HideTouchCaptureOverlay()
        {
            if (!_isTouchCaptureShown || _windowManager == null || _touchCaptureView == null)
                return;
                
            // Remove the overlay from the window manager
            _windowManager.RemoveViewImmediate(_touchCaptureView);
            _isTouchCaptureShown = false;
            _logger?.LogInformation("Touch capture overlay hidden");
        }
        
        /// <summary>
        /// Handles the record button click
        /// </summary>
        private void OnRecordButtonClick(object sender, EventArgs e)
        {
            var isRecording = _recorderService?.IsRecording ?? false;
            
            if (isRecording)
            {
                // Update button states immediately to provide visual feedback
                _recorderService.IsRecording = false;  // Temporarily set to false for visual update
                UpdateButtonStates();
                
                // Hide the touch capture overlay
                HideTouchCaptureOverlay();
                
                // Show dialog to name and save the macro
                ShowSaveMacroDialog();
            }
            else
            {
                // Start recording
                _recorderService?.StartRecording();
                UpdateButtonStates();
                
                // Show the touch capture overlay to capture touches
                ShowTouchCaptureOverlay();
            }
        }
        
        /// <summary>
        /// Shows a dialog to save the recorded macro
        /// </summary>
        public void ShowSaveMacroDialog()
        {
            // Get the current foreground activity context
            var activity = Platform.CurrentActivity;
            
            // If no activity is in foreground, launch the main activity
            if (activity == null)
            {
                // Launch the main app activity
                var intent = new Intent(this, typeof(MainActivity));
                intent.SetFlags(ActivityFlags.NewTask);
                intent.PutExtra("showSaveDialog", true); // Signal to show the save dialog when activity starts
                StartActivity(intent);
                return;
            }
            
            // Create dialog using the activity context
            var dialog = new Dialog(activity);
            dialog.SetContentView(Resource.Layout.save_macro_dialog);
            dialog.SetCancelable(false);
            dialog.SetTitle("Save Macro");
            
            // Get views
            var nameEditText = dialog.FindViewById<EditText>(Resource.Id.macroNameEditText);
            var saveButton = dialog.FindViewById<AndroidButton>(Resource.Id.saveButton);
            var cancelButton = dialog.FindViewById<AndroidButton>(Resource.Id.cancelButton);
            
            // Set up cancel button
            cancelButton.Click += (s, args) => {
                _recorderService?.CancelRecording();
                UpdateButtonStates(); // Ensure buttons are updated
                dialog.Dismiss();
            };
            
            // Set up save button
            saveButton.Click += async (s, args) => {
                var name = nameEditText.Text?.Trim() ?? "Unnamed Macro";
                if (string.IsNullOrEmpty(name))
                {
                    name = "Unnamed Macro";
                }
                
                var macroId = await _recorderService.StopRecordingAndSaveAsync(name);
                UpdateButtonStates();
                
                if (macroId > 0)
                {
                    Toast.MakeText(this, "Macro saved", ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(this, "No actions recorded", ToastLength.Short).Show();
                }
                
                dialog.Dismiss();
            };
            
            // Show the dialog
            dialog.Show();
        }
        
        /// <summary>
        /// Handles the play button click
        /// </summary>
        private void OnPlayButtonClick(object sender, EventArgs e)
        {
            var isPlaying = _playerService?.IsPlaying ?? false;
            
            if (isPlaying)
            {
                // Stop playback
                _playerService?.StopPlayback();
            }
            else
            {
                // Show macro selection dialog
                ShowSelectMacroDialog();
            }
        }
        
        /// <summary>
        /// Shows a dialog to select a macro to play
        /// </summary>
        private async void ShowSelectMacroDialog()
        {
            // Get the current foreground activity context
            var activity = Platform.CurrentActivity;
            if (activity == null)
            {
                throw new InvalidOperationException("Cannot show dialog: No foreground activity found");
            }
            
            // Get all macros from database service
            var databaseService = IPlatformApplication.Current.Services.GetService<DatabaseService>();
            var macros = await databaseService.GetAllMacrosAsync();
            
            if (macros == null || macros.Count == 0)
            {
                Toast.MakeText(this, "No macros found", ToastLength.Short).Show();
                return;
            }
            
            // Create adapter for the list
            var adapter = new ArrayAdapter<string>(
                activity,
                global::Android.Resource.Layout.SimpleListItem1,
                macros.ConvertAll(m => $"{m.Name} ({m.ActionCount} actions)")
            );
            
            // Create and show the dialog
            var builder = new AlertDialog.Builder(activity);
            builder.SetTitle("Select Macro");
            
            builder.SetAdapter(adapter, (sender, args) => {
                var selectedMacro = macros[args.Which];
                StartMacroPlayback(selectedMacro.Id);
            });
            
            builder.SetNegativeButton("Cancel", (sender, args) => { });
            builder.Show();
        }
        
        /// <summary>
        /// Starts playing the selected macro
        /// </summary>
        private async void StartMacroPlayback(int macroId)
        {
            if (_playerService == null || macroId <= 0)
                return;
                
            // Start the playback - any exceptions will be caught by global handler
            await _playerService.PlayMacroAsync(macroId);
        }
        
        /// <summary>
        /// Handles the settings button click
        /// </summary>
        private void OnSettingsButtonClick(object sender, EventArgs e)
        {
            // Launch the main app activity
            var intent = new Intent(this, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }
        
        /// <summary>
        /// Handles the playback started event
        /// </summary>
        private void OnPlaybackStarted(object sender, EventArgs e)
        {
            // Update button states on the UI thread
            new Handler(Looper.MainLooper).Post(() => {
                UpdateButtonStates();
                Toast.MakeText(this, "Playback started", ToastLength.Short).Show();
            });
        }
        
        /// <summary>
        /// Handles the playback stopped event
        /// </summary>
        private void OnPlaybackStopped(object sender, EventArgs e)
        {
            // Update button states on the UI thread
            new Handler(Looper.MainLooper).Post(() => {
                UpdateButtonStates();
                Toast.MakeText(this, "Playback complete", ToastLength.Short).Show();
            });
        }
        
        /// <summary>
        /// Handles action requested events from the player service
        /// </summary>
        private void OnActionRequested(object sender, (MacroAction Current, MacroAction? Previous) actionData)
        {
            // Get the accessibility service instance to perform the action
            var accessibilityService = MacroAccessibilityService.Instance;
            if (accessibilityService == null)
            {
                _logger?.LogWarning("Cannot execute action - accessibility service not available");
                return;
            }
            
            // Execute the action using the accessibility service
            accessibilityService.ExecuteAction(actionData.Current, actionData.Previous);
        }
        
        /// <summary>
        /// Moves the overlay to a new position
        /// </summary>
        public void MoveOverlay(int deltaX, int deltaY)
        {
            if (!_isOverlayShown)
                return;
                
            _layoutParams.X += deltaX;
            _layoutParams.Y += deltaY;
            
            // We're using AndroidView and not Microsoft.Maui.Controls.View here
            _windowManager.UpdateViewLayout(_overlayView, _layoutParams);
        }
        
        /// <summary>
        /// Removes the overlay from the screen
        /// </summary>
        private void HideOverlay()
        {
            if (!_isOverlayShown)
                return;
                
            // Also hide the touch capture overlay if it's shown
            HideTouchCaptureOverlay();
            
            // Remove the overlay from the window manager
            // We're using AndroidView and not Microsoft.Maui.Controls.View here
            _windowManager.RemoveViewImmediate(_overlayView);
            _isOverlayShown = false;
            
            // Clean up event handlers
            if (_recordButton != null)
                _recordButton.Click -= OnRecordButtonClick;
                
            if (_playButton != null)
                _playButton.Click -= OnPlayButtonClick;
                
            if (_settingsButton != null)
                _settingsButton.Click -= OnSettingsButtonClick;
                
            _logger?.LogInformation("Overlay hidden");
        }
        
        /// <summary>
        /// Called when the service is destroyed
        /// </summary>
        public override void OnDestroy()
        {
            _logger?.LogInformation("Overlay service destroyed");
            
            // Unsubscribe from events
            if (_playerService != null)
            {
                _playerService.PlaybackStarted -= OnPlaybackStarted;
                _playerService.PlaybackStopped -= OnPlaybackStopped;
                _playerService.OnActionRequested -= OnActionRequested;
            }
            
            // Hide the overlay
            HideOverlay();
            
            // Clear the instance reference
            if (_instance == this)
                _instance = null!;
                
            base.OnDestroy();
        }
        
        /// <summary>
        /// Returns the service binder (not used in this service)
        /// </summary>
        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }
        
        /// <summary>
        /// Gesture listener for handling overlay dragging
        /// </summary>
        private class OverlayGestureListener : GestureDetector.SimpleOnGestureListener
        {
            private readonly OverlayService _service;
            private readonly ILogger _logger;
            
            public OverlayGestureListener(OverlayService service, ILogger logger)
            {
                _service = service;
                _logger = logger;
            }
            
            public override bool OnScroll(MotionEvent? e1, MotionEvent? e2, float distanceX, float distanceY)
            {
                // Move the overlay in the opposite direction of the distance (for dragging)
                _service.MoveOverlay((int)(-distanceX), (int)(-distanceY));
                return true;
            }
        }
        
        /// <summary>
        /// Touch event listener for capturing screen touches when recording
        /// </summary>
        private class ScreenTouchListener : Java.Lang.Object, AndroidView.IOnTouchListener
        {
            private readonly OverlayService _service;
            private readonly MacroRecorderService _recorderService;
            private readonly ILogger _logger;
            
            public ScreenTouchListener(OverlayService service, MacroRecorderService recorderService, ILogger logger)
            {
                _service = service;
                _recorderService = recorderService;
                _logger = logger;
            }
            
            public bool OnTouch(AndroidView? v, MotionEvent? e)
            {
                if (e == null || !_recorderService.IsRecording)
                    return false;
                
                switch (e.Action & MotionEventActions.Mask)
                {
                    case MotionEventActions.Down:
                        _logger.LogInformation($"Touch DOWN at ({e.RawX}, {e.RawY})");
                        _recorderService.RecordTouchDown(e.RawX, e.RawY);
                        break;
                        
                    case MotionEventActions.Move:
                        _logger.LogInformation($"Touch MOVE to ({e.RawX}, {e.RawY})");
                        _recorderService.RecordTouchMove(e.RawX, e.RawY);
                        break;
                        
                    case MotionEventActions.Up:
                    case MotionEventActions.Cancel:
                        _logger.LogInformation($"Touch UP at ({e.RawX}, {e.RawY})");
                        _recorderService.RecordTouchUp(e.RawX, e.RawY);
                        break;
                }
                
                // Return false to allow the event to be passed to the system
                return false;
            }
        }
    }
}