using LingoWay.Presentation.ViewModels;
using Microsoft.Maui.Controls;

namespace LingoWay.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnLightThemeClicked(object sender, EventArgs e)
    {
        (Microsoft.Maui.Controls.Application.Current as App)?.LoadTheme("Light");
    }

    private void OnDarkThemeClicked(object sender, EventArgs e)
    {
        (Microsoft.Maui.Controls.Application.Current as App)?.LoadTheme("Dark");
    }

    private void OnSystemThemeClicked(object sender, EventArgs e)
    {
        // 跟随系统主题：先将 UserAppTheme 设为 Unspecified，再根据系统重新加载
        Microsoft.Maui.Controls.Application.Current.UserAppTheme = AppTheme.Unspecified;
        var currentTheme = Microsoft.Maui.Controls.Application.Current.RequestedTheme == AppTheme.Light ? "Light" : "Dark";
        (Microsoft.Maui.Controls.Application.Current as App)?.LoadTheme(currentTheme);
    }
}
