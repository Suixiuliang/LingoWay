namespace LingoWay.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnPlayCenterClicked(object sender, EventArgs e)
    {
        // 导航到播放页面（Shell route: PlayerPage）
        await Shell.Current.GoToAsync("PlayerPage");
    }
}
