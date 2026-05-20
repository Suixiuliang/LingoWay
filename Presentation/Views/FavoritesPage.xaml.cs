namespace LingoWay.Views;

using LingoWay.Presentation.ViewModels;

public partial class FavoritesPage : ContentPage
{
    public FavoritesPage(FavoriteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
