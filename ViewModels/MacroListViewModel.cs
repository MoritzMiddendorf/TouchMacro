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
            set => SetProperty(ref _hasOverlayPermission, value);
        }
        
        private bool _hasAccessibilityPermission;
        public bool HasAccessibilityPermission
        {
            get => _hasAccessibilityPermission;
            set => SetProperty(ref _hasAccessibilityPermission, value);
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
                
                _logger.LogInformation($"Permission check - Overlay: {HasOverlayPermission}, Accessibility: {HasAccessibilityPermission}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permissions");
            }
        }
        
        /// <summary>
        /// Requests the overlay permission
        /// </summary>
        public async Task RequestOverlayPermissionAsync()
        {
            try
            {
                await _permissionService.RequestOverlayPermissionAsync();
                await Task.Delay(500); // Short delay before checking again
                await CheckPermissionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting overlay permission");
            }
        }
        
        /// <summary>
        /// Opens the accessibility settings
        /// </summary>
        public async Task OpenAccessibilitySettingsAsync()
        {
            try
            {
                await _permissionService.OpenAccessibilitySettingsAsync();
                await Task.Delay(500); // Short delay before checking again
                await CheckPermissionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening accessibility settings");
            }
        }
        
        /// <summary>
        /// Starts the overlay service if permissions are granted
        /// </summary>
        public async Task StartOverlayAsync()
        {
            // First check if we have permissions
            await CheckPermissionsAsync();
            
            if (!HasOverlayPermission || !HasAccessibilityPermission)
            {
                await Shell.Current.DisplayAlert(
                    "Permissions Required",
                    "To use the overlay, you need to grant both overlay and accessibility permissions.",
                    "OK");
                return;
            }
            
            try
            {
#if ANDROID
                // Start the overlay service on Android
                var context = Android.App.Application.Context;
                var intent = new Android.Content.Intent(context, typeof(TouchMacro.Platforms.Android.Services.OverlayService));
                
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    context.StartForegroundService(intent);
                }
                else
                {
                    context.StartService(intent);
                }
                
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