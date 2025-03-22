using TouchMacro.ViewModels;

namespace TouchMacro
{
    public partial class MainPage : ContentPage
    {
        private readonly MacroListViewModel _viewModel;
        
        public MainPage(MacroListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
        
        private async void OnRequestOverlayPermissionClicked(object sender, EventArgs e)
        {
            await _viewModel.RequestOverlayPermissionAsync();
        }
        
        private async void OnOpenAccessibilitySettingsClicked(object sender, EventArgs e)
        {
            await _viewModel.OpenAccessibilitySettingsAsync();
        }
    }
}