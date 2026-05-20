namespace LingoWay.Views;

using LingoWay.Presentation.ViewModels;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnLightThemeClicked(object sender, EventArgs e)
    {
        var svc = App.Current.Handler.MauiContext.Services.GetService(typeof(LingoWay.Application.Services.IThemeService)) as LingoWay.Application.Services.IThemeService;
        svc?.SetLightTheme();
    }

    private void OnDarkThemeClicked(object sender, EventArgs e)
    {
        var svc = App.Current.Handler.MauiContext.Services.GetService(typeof(LingoWay.Application.Services.IThemeService)) as LingoWay.Application.Services.IThemeService;
        svc?.SetDarkTheme();
    }

    private void OnSystemThemeClicked(object sender, EventArgs e)
    {
        var svc = App.Current.Handler.MauiContext.Services.GetService(typeof(LingoWay.Application.Services.IThemeService)) as LingoWay.Application.Services.IThemeService;
        svc?.SetSystemTheme();
    }
}
