namespace LingoWay.Views;

public partial class SplashPage : ContentPage
{
    private readonly TaskCompletionSource _completion = new();

    public Task Completion => _completion.Task;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            // 1. 三点淡入
            await LoadingDots.FadeTo(1, 200);

            // 2. 光条从中心展开
            await Task.WhenAll(
                GlowLine.FadeTo(1, 300),
                GlowLine.ScaleXTo(1, 400, Easing.Linear));

            // 3. 标题弹入
            await Task.WhenAll(
                AppTitleLabel.FadeTo(1, 400, Easing.CubicOut),
                AppTitleLabel.ScaleTo(1, 500, Easing.SpringOut));

            // 4. 标语
            await SloganLabel.FadeTo(1, 300, Easing.CubicOut);

            await Task.Delay(1400);

            // 5. 淡出
            await this.FadeTo(0, 450, Easing.CubicIn);
            _completion.TrySetResult();
        }
        catch
        {
            _completion.TrySetResult();
        }
    }
}
