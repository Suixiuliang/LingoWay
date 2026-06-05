using LingoWay.Controls;
using LingoWay.Views;

namespace LingoWay
{
    public partial class AppShell : Shell
    {
        private MiniPlayerControl? _miniPlayer;
        private readonly Dictionary<ContentPage, View> _wrappedOriginalContents = new();

        private string _cachedTitle = "未播放";
        private ImageSource? _cachedCover;
        private bool _cachedIsPlaying;
        private TimeSpan _cachedCurrentPos;
        private TimeSpan _cachedTotalDur;

        public AppShell()
        {
            InitializeComponent();
            Navigated += OnShellNavigated;
        }

        private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            if (_miniPlayer == null)
            {
                var audioService = IPlatformApplication.Current?.Services
                    .GetService(typeof(Application.Services.IAudioPlaybackService))
                    as Application.Services.IAudioPlaybackService;
                _miniPlayer = new MiniPlayerControl(audioService!);
                _miniPlayer.SetTitle(_cachedTitle);
                _miniPlayer.SetCover(_cachedCover);
                _miniPlayer.SetPlayState(_cachedIsPlaying);
                _miniPlayer.SetTime(_cachedCurrentPos, _cachedTotalDur);
            }

            var currentPage = CurrentPage;
            _miniPlayer.IsVisible = currentPage is not PlayerPage;

            if (currentPage is ContentPage cp && currentPage is not PlayerPage)
                EnsureOverlayWrapped(cp);
        }

        private void EnsureOverlayWrapped(ContentPage cp)
        {
            if (_wrappedOriginalContents.ContainsKey(cp)) return;
            if (cp.Content is not View original || _miniPlayer == null) return;

            _wrappedOriginalContents[cp] = original;

            var wrapper = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star)
                }
            };

            Grid.SetRow(_miniPlayer, 0);
            if (_miniPlayer.Parent is Layout p) p.Children.Remove(_miniPlayer);
            wrapper.Children.Add(_miniPlayer);

            Grid.SetRow(original, 1);
            wrapper.Children.Add(original);

            cp.Content = wrapper;
        }

        public void UpdateMiniPlayer(string? title, ImageSource? cover, bool isPlaying, TimeSpan current, TimeSpan total)
        {
            _cachedTitle = title ?? "未播放";
            _cachedCover = cover;
            _cachedIsPlaying = isPlaying;
            _cachedCurrentPos = current;
            _cachedTotalDur = total;

            _miniPlayer?.SetTitle(_cachedTitle);
            _miniPlayer?.SetCover(_cachedCover);
            _miniPlayer?.SetPlayState(_cachedIsPlaying);
            _miniPlayer?.SetTime(_cachedCurrentPos, _cachedTotalDur);
        }

        public void UpdateMiniPlayerTime(TimeSpan current, TimeSpan total)
        {
            _cachedCurrentPos = current;
            _cachedTotalDur = total;
            _miniPlayer?.SetTime(current, total);
        }
    }
}
