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
        Application.Current.UserAppTheme = AppTheme.Light;
    }

    public void SetDarkTheme()
    {
        Application.Current.UserAppTheme = AppTheme.Dark;
    }

    public void SetSystemTheme()
    {
        Application.Current.UserAppTheme = AppTheme.Unspecified;
    }
}
