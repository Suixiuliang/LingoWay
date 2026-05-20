namespace LingoWay.Views;

using LingoWay.Presentation.ViewModels;

public partial class DownloadsPage : ContentPage
{
    public DownloadsPage(DownloadViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
