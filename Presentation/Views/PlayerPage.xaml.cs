namespace LingoWay.Views;

using LingoWay.Presentation.ViewModels;

public partial class PlayerPage : ContentPage
{
    public PlayerPage(PlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
