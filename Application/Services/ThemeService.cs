using Microsoft.Maui.Controls;

namespace LingoWay.Application.Services;

public interface IThemeService
{
    void SetLightTheme();
    void SetDarkTheme();
    void SetSystemTheme();
}

public class ThemeService : IThemeService
{
    public void SetLightTheme()
    {
        global::Microsoft.Maui.Controls.Application.Current.UserAppTheme = AppTheme.Light;
    }

    public void SetDarkTheme()
    {
        global::Microsoft.Maui.Controls.Application.Current.UserAppTheme = AppTheme.Dark;
    }

    public void SetSystemTheme()
    {
        global::Microsoft.Maui.Controls.Application.Current.UserAppTheme = AppTheme.Unspecified;
    }
}