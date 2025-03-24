using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using TouchMacro.Models;
using TouchMacro.Services;

namespace TouchMacro.ViewModels
{
    /// <summary>
    /// ViewModel for displaying and managing the list of macros
    /// </summary>
    public class MacroListViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly PermissionService _permissionService;
        private readonly ILogger<MacroListViewModel> _logger;
        
        public ObservableCollection<Macro> Macros { get; private set; } = new ObservableCollection<Macro>();
        
        public ICommand RefreshCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand StartOverlayCommand { get; private set; }
        
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }
        
        private bool _hasOverlayPermission;
        public bool HasOverlayPermission
        {
            get => _hasOverlayPermission;
            set
            {
                SetProperty(ref _hasOverlayPermission, value);
                UpdateAllPermissionsGranted();
            }
        }
        
        private bool _hasAccessibilityPermission;
        public bool HasAccessibilityPermission
        {
            get => _hasAccessibilityPermission;
            set
            {
                SetProperty(ref _hasAccessibilityPermission, value);
                UpdateAllPermissionsGranted();
            }
        }
        
        private bool _hasForegroundServicePermission;
        public bool HasForegroundServicePermission
        {
            get => _hasForegroundServicePermission;
            set
            {
                SetProperty(ref _hasForegroundServicePermission, value);
                UpdateAllPermissionsGranted();
            }
        }
        
        private bool _allPermissionsGranted;
        public bool AllPermissionsGranted
        {
            get => _allPermissionsGranted;
            set => SetProperty(ref _allPermissionsGranted, value);
        }
        
        private void UpdateAllPermissionsGranted()
        {
            AllPermissionsGranted = _hasOverlayPermission && _hasAccessibilityPermission && _hasForegroundServicePermission;
        }
        
        public MacroListViewModel(
            DatabaseService databaseService,
            PermissionService permissionService,
            ILogger<MacroListViewModel> logger)
        {
            _databaseService = databaseService;
            _permissionService = permissionService;
            _logger = logger;
            
            RefreshCommand = new Command(async () => await LoadMacrosAsync());
            DeleteCommand = new Command<Macro>(async (macro) => await DeleteMacroAsync(macro));
            StartOverlayCommand = new Command(async () => await StartOverlayAsync());
            
            // Initial load
            Task.Run(async () => 
            {
                await CheckPermissionsAsync();
                await LoadMacrosAsync();
            });
        }
        
        /// <summary>
        /// Loads all macros from the database
        /// </summary>
        public async Task LoadMacrosAsync()
        {
            if (IsRefreshing)
                return;
                
            try
            {
                IsRefreshing = true;
                
                // Get macros from database
                var macros = await _databaseService.GetAllMacrosAsync();
                
                // Update the observable collection
                Macros.Clear();
                foreach (var macro in macros)
                {
                    Macros.Add(macro);
                }
                
                _logger.LogInformation($"Loaded {Macros.Count} macros");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading macros");
            }
            finally
            {
                IsRefreshing = false;
            }
        }
        
        /// <summary>
        /// Deletes a macro
        /// </summary>
        public async Task DeleteMacroAsync(Macro macro)
        {
            if (macro == null)
                return;
                
            try
            {
                var confirmed = await Shell.Current.DisplayAlert(
                    "Delete Macro", 
                    $"Are you sure you want to delete '{macro.Name}'?", 
                    "Delete", "Cancel");
                    
                if (confirmed)
                {
                    await _databaseService.DeleteMacroAsync(macro.Id);
                    Macros.Remove(macro);
                    _logger.LogInformation($"Deleted macro {macro.Id} ({macro.Name})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting macro {macro.Id}");
                await Shell.Current.DisplayAlert("Error", "Failed to delete macro", "OK");
            }
        }
        
        /// <summary>
        /// Checks if all required permissions are granted
        /// </summary>
        public async Task CheckPermissionsAsync()
        {
            try
            {
                // Check overlay permission
                HasOverlayPermission = await _permissionService.CheckOverlayPermissionAsync();
                
                // Check accessibility service
                HasAccessibilityPermission = await _permissionService.CheckAccessibilityServiceEnabledAsync();
                
                // Check foreground service permission
                HasForegroundServicePermission = await _permissionService.CheckForegroundServicePermissionAsync();
                
                // UpdateAllPermissionsGranted is called automatically when setting the individual permissions
                
                _logger.LogInformation($"Permission check - Overlay: {HasOverlayPermission}, Accessibility: {HasAccessibilityPermission}, ForegroundService: {HasForegroundServicePermission}, All Granted: {AllPermissionsGranted}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permissions");
                
                // Reset permissions on error
                HasOverlayPermission = false;
                HasAccessibilityPermission = false;
                HasForegroundServicePermission = false;
                // AllPermissionsGranted is updated by the property setters
            }
        }
        
        /// <summary>
        /// Requests the foreground service permission
        /// </summary>
        public async Task RequestForegroundServicePermissionAsync()
        {
            try
            {
                await _permissionService.RequestForegroundServicePermissionAsync();
                
                // Start polling for permission changes
                _ = PollForPermissionChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting foreground service permission");
            }
        }
        
        /// <summary>
        /// Requests the overlay permission and continuously polls for changes
        /// </summary>
        public async Task RequestOverlayPermissionAsync()
        {
            try
            {
                await _permissionService.RequestOverlayPermissionAsync();
                
                // Start polling for permission changes
                _ = PollForPermissionChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting overlay permission");
            }
        }
        
        /// <summary>
        /// Opens the accessibility settings and continuously polls for changes
        /// </summary>
        public async Task OpenAccessibilitySettingsAsync()
        {
            try
            {
                await _permissionService.OpenAccessibilitySettingsAsync();
                
                // Start polling for permission changes
                _ = PollForPermissionChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening accessibility settings");
            }
        }
        
        /// <summary>
        /// Polls for permission changes after the user has been directed to system settings
        /// </summary>
        private async Task PollForPermissionChangesAsync()
        {
            try
            {
                // Initial state
                bool initialOverlayState = HasOverlayPermission;
                bool initialAccessibilityState = HasAccessibilityPermission;
                
                // Poll for changes every second for 30 seconds
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(1000);
                    
                    // Recheck permissions
                    await CheckPermissionsAsync();
                    
                    // If permissions have changed, stop polling
                    if (HasOverlayPermission != initialOverlayState || 
                        HasAccessibilityPermission != initialAccessibilityState)
                    {
                        _logger.LogInformation("Permission state changed, stopping polling");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling for permission changes");
            }
        }
        
        /// <summary>
        /// Starts the overlay service if permissions are granted
        /// </summary>
        public async Task StartOverlayAsync()
        {
            // First check if we have permissions
            await CheckPermissionsAsync();
            
            if (!HasOverlayPermission || !HasAccessibilityPermission || !HasForegroundServicePermission)
            {
                await Shell.Current.DisplayAlert(
                    "Permissions Required",
                    "To use the overlay, you need to grant all required permissions: overlay, accessibility, and foreground service.",
                    "OK");
                return;
            }
            
            try
            {
#if ANDROID
                // Start the overlay service on Android
                var context = Android.App.Application.Context;
                var intent = new Android.Content.Intent(context, typeof(TouchMacro.Platforms.Android.Services.OverlayService));
                
                // For API 26+ (Android 8.0+), we always use StartForegroundService
                context.StartForegroundService(intent);
                
                _logger.LogInformation("Started overlay service");
#else
                _logger.LogWarning("Overlay service is only available on Android");
                await Shell.Current.DisplayAlert("Not Supported", "Overlay is only available on Android devices.", "OK");
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting overlay service");
                await Shell.Current.DisplayAlert("Error", "Failed to start overlay: " + ex.Message, "OK");
            }
        }
    }
}