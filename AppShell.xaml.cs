using LingoWay.Controls;
using LingoWay.Views;

namespace LingoWay
{
    public partial class AppShell : Shell
    {
        private MiniPlayerControl? _miniPlayer;
        private View? _originalContent;
        private ContentPage? _currentOverlayPage;

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
            var currentPage = CurrentPage;
            if (currentPage is PlayerPage)
            {
                RemoveMiniPlayerOverlay();
                _miniPlayer?.Detach();
                _miniPlayer = null;
            }
            else if (currentPage != null)
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
                AddMiniPlayerOverlay(currentPage);
            }
        }

        private void AddMiniPlayerOverlay(Page page)
        {
            if (_miniPlayer == null) return;
            if (_currentOverlayPage == page) return;
            if (page is not ContentPage cp) return;

            RemoveMiniPlayerOverlay();

            var content = cp.Content;
            if (content == null) return;

            _originalContent = content;
            _currentOverlayPage = cp;

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

            Grid.SetRow(_originalContent, 1);
            wrapper.Children.Add(_originalContent);

            cp.Content = wrapper;
        }

        private void RemoveMiniPlayerOverlay()
        {
            if (_currentOverlayPage == null || _originalContent == null) return;

            _currentOverlayPage.Content = _originalContent;
            _originalContent = null;
            _currentOverlayPage = null;
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
