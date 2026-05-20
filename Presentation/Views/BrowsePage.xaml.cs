namespace LingoWay.Views;

using LingoWay.Presentation.ViewModels;

public partial class BrowsePage : ContentPage
{
    public BrowsePage(BrowseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        // 处理搜索
    }
}
