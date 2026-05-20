namespace LingoWay.Presentation.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LingoWay.Application.Services;
using LingoWay.Domain.Interfaces;
using LingoWay.Domain.Models;

/// <summary>
/// 基础ViewModel
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    protected bool isBusy;

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }
}

/// <summary>
/// 播放页面ViewModel
/// </summary>
public partial class PlayerViewModel : BaseViewModel
{
    private readonly IPlaybackService playbackService;
    private readonly ILearningService learningService;
    private readonly ISubtitleService subtitleService;
    private readonly IVocabularyService vocabularyService;

    [ObservableProperty]
    private Episode? currentEpisode;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private TimeSpan currentPosition = TimeSpan.Zero;

    [ObservableProperty]
    private TimeSpan duration = TimeSpan.Zero;

    [ObservableProperty]
    private float playbackRate = 1.0f;

    [ObservableProperty]
    private List<Subtitle> subtitles = [];

    [ObservableProperty]
    private Subtitle? currentSubtitle;

    [ObservableProperty]
    private float volume = 1.0f;

    [ObservableProperty]
    private bool isSubtitleVisible = true;

    public PlayerViewModel(
        IPlaybackService playbackService,
        ILearningService learningService,
        ISubtitleService subtitleService,
        IVocabularyService vocabularyService)
    {
        this.playbackService = playbackService;
        this.learningService = learningService;
        this.subtitleService = subtitleService;
        this.vocabularyService = vocabularyService;
    }

    [RelayCommand]
    public async Task PlayAsync(Episode episode)
    {
        CurrentEpisode = episode;
        Duration = episode.Duration;

        await playbackService.PlayAsync(episode);
        IsPlaying = true;

        // 加载字幕
        Subtitles = await subtitleService.GetSubtitlesAsync(episode.Id);
    }

    [RelayCommand]
    public async Task PauseAsync()
    {
        await playbackService.PauseAsync();
        IsPlaying = false;
    }

    [RelayCommand]
    public async Task ResumeAsync()
    {
        await playbackService.ResumeAsync();
        IsPlaying = true;
    }

    [RelayCommand]
    public async Task StopAsync()
    {
        await playbackService.StopAsync();
        IsPlaying = false;
        CurrentEpisode = null;
    }

    [RelayCommand]
    public async Task SeekAsync(double seconds)
    {
        var position = TimeSpan.FromSeconds(seconds);
        CurrentPosition = position;
        await playbackService.SeekAsync(position);
    }

    [RelayCommand]
    public async Task ChangePlaybackRateAsync(float rate)
    {
        PlaybackRate = Math.Max(0.5f, Math.Min(rate, 2.0f));
        await playbackService.SetPlaybackRateAsync(PlaybackRate);
    }

    [RelayCommand]
    public async Task UpdateCurrentPositionAsync(double seconds)
    {
        CurrentPosition = TimeSpan.FromSeconds(seconds);

        // 更新当前字幕
        UpdateCurrentSubtitle();

        // 定期记录学习进度
        if (CurrentEpisode != null && CurrentPosition.TotalSeconds % 30 < 1)
        {
            await learningService.RecordPlaybackAsync(
                CurrentEpisode, 
                TimeSpan.FromSeconds(30),
                CurrentPosition);
        }
    }

    [RelayCommand]
    public void ToggleSubtitleAsync()
    {
        IsSubtitleVisible = !IsSubtitleVisible;
    }

    private void UpdateCurrentSubtitle()
    {
        CurrentSubtitle = Subtitles.FirstOrDefault(s =>
            s.StartTime <= CurrentPosition && CurrentPosition <= s.EndTime);
    }
}

/// <summary>
/// 浏览页面ViewModel
/// </summary>
public partial class BrowseViewModel : BaseViewModel
{
    private readonly IContentProvider contentProvider;
    private readonly IFavoriteService favoriteService;

    [ObservableProperty]
    private List<Podcast> podcasts = [];

    [ObservableProperty]
    private List<Episode> recentEpisodes = [];

    [ObservableProperty]
    private Podcast? selectedPodcast;

    [ObservableProperty]
    private string searchQuery = "";

    public BrowseViewModel(
        IContentProvider contentProvider,
        IFavoriteService favoriteService)
    {
        this.contentProvider = contentProvider;
        this.favoriteService = favoriteService;
    }

    [RelayCommand]
    public async Task LoadPodcastsAsync()
    {
        IsBusy = true;
        try
        {
            Podcasts = await contentProvider.GetPodcastsAsync();
            // 加载最近的剧集
            var allEpisodes = new List<Episode>();
            foreach (var podcast in Podcasts)
            {
                var episodes = await contentProvider.GetEpisodesAsync(podcast.Id);
                allEpisodes.AddRange(episodes);
            }
            RecentEpisodes = allEpisodes
                .OrderByDescending(e => e.PublishedDate)
                .Take(20)
                .ToList();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            await contentProvider.RefreshPodcastsAsync();
            await LoadPodcastsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SelectPodcastAsync(Podcast podcast)
    {
        SelectedPodcast = podcast;
    }

    [RelayCommand]
    public async Task AddCustomPodcastAsync(string rssUrl)
    {
        if (string.IsNullOrWhiteSpace(rssUrl))
            return;

        IsBusy = true;
        try
        {
            await contentProvider.AddCustomPodcastAsync(rssUrl);
            await LoadPodcastsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// 下载页面ViewModel
/// </summary>
public partial class DownloadViewModel : BaseViewModel
{
    private readonly IDownloadService downloadService;
    private readonly IContentProvider contentProvider;

    [ObservableProperty]
    private List<Download> downloads = [];

    [ObservableProperty]
    private List<Episode> downloadedEpisodes = [];

    [ObservableProperty]
    private double totalDownloadProgress;

    public DownloadViewModel(
        IDownloadService downloadService,
        IContentProvider contentProvider)
    {
        this.downloadService = downloadService;
        this.contentProvider = contentProvider;
    }

    [RelayCommand]
    public async Task LoadDownloadsAsync()
    {
        IsBusy = true;
        try
        {
            var completed = await downloadService.GetCompletedDownloadsAsync();
            Downloads = completed;

            // 加载已下载的剧集
            var episodes = new List<Episode>();
            foreach (var download in Downloads)
            {
                var episode = await contentProvider.GetEpisodeAsync(download.EpisodeId);
                if (episode != null)
                {
                    episodes.Add(episode);
                }
            }
            DownloadedEpisodes = episodes;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DownloadEpisodeAsync(Episode episode)
    {
        var download = await downloadService.EnqueueAsync(episode);

        await foreach (var progress in downloadService.DownloadAsync(download))
        {
            TotalDownloadProgress = progress.ProgressPercentage;
        }

        await LoadDownloadsAsync();
    }

    [RelayCommand]
    public async Task DeleteDownloadAsync(Download download)
    {
        await downloadService.DeleteAsync(download);
        await LoadDownloadsAsync();
    }
}

/// <summary>
/// 收藏页面ViewModel
/// </summary>
public partial class FavoriteViewModel : BaseViewModel
{
    private readonly IFavoriteService favoriteService;

    [ObservableProperty]
    private List<Episode> favorites = [];

    public FavoriteViewModel(IFavoriteService favoriteService)
    {
        this.favoriteService = favoriteService;
    }

    [RelayCommand]
    public async Task LoadFavoritesAsync()
    {
        IsBusy = true;
        try
        {
            Favorites = await favoriteService.GetFavoritesAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RemoveFavoriteAsync(Episode episode)
    {
        await favoriteService.RemoveFavoriteAsync(episode);
        await LoadFavoritesAsync();
    }
}

/// <summary>
/// 搜索页面ViewModel
/// </summary>
public partial class SearchViewModel : BaseViewModel
{
    private readonly ISearchService searchService;

    [ObservableProperty]
    private string searchQuery = "";

    [ObservableProperty]
    private List<Episode> searchResults = [];

    [ObservableProperty]
    private List<Podcast> podcastResults = [];

    [ObservableProperty]
    private List<Vocabulary> vocabularyResults = [];

    public SearchViewModel(ISearchService searchService)
    {
        this.searchService = searchService;
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        IsBusy = true;
        try
        {
            SearchResults = await searchService.SearchEpisodesAsync(SearchQuery);
            PodcastResults = await searchService.SearchPodcastsAsync(SearchQuery);
            VocabularyResults = await searchService.SearchVocabularyAsync(SearchQuery);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// 设置页面ViewModel
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty]
    private float playbackRate = 1.0f;

    [ObservableProperty]
    private bool isBackgroundPlayEnabled = true;

    [ObservableProperty]
    private bool isSubtitleEnabled = true;

    [ObservableProperty]
    private string subtitleLanguage = "en";

    [ObservableProperty]
    private bool isDarkModeEnabled = true;

    [ObservableProperty]
    private int subtitleFontSize = 16;

    [ObservableProperty]
    private bool isWifiOnlyDownloadEnabled = false;

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        IsBusy = true;
        try
        {
            // 保存设置到本地存储
            await Task.Delay(500); // 模拟保存
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ResetToDefaultAsync()
    {
        PlaybackRate = 1.0f;
        IsBackgroundPlayEnabled = true;
        IsSubtitleEnabled = true;
        SubtitleLanguage = "en";
        IsDarkModeEnabled = true;
        SubtitleFontSize = 16;
        IsWifiOnlyDownloadEnabled = false;
        await SaveSettingsAsync();
    }
}
