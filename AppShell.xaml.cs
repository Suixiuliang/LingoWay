using LingoWay.Views;

namespace LingoWay
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("PodcastDetail", typeof(PodcastDetailPage));
        }
    }
}
