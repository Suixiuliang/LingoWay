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
/// 播放页面ViewModel - 支持 LRC 字幕和本地音频播放
/// </summary>
public partial class PlayerViewModel : BaseViewModel
{
    private readonly IPlaybackService playbackService;
    private readonly ILearningService learningService;
    private readonly ISubtitleService subtitleService;
    private readonly IVocabularyService vocabularyService;
    private readonly IFavoriteService favoriteService;
    private readonly IAudioPlaybackService audioPlaybackService;
    private readonly LrcParserService lrcParserService;

    private CancellationTokenSource? _positionUpdateCts;
    private Task? _positionUpdateTask;

    [ObservableProperty]
    private Episode? currentEpisode;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private TimeSpan currentPosition = TimeSpan.Zero;

    [ObservableProperty]
    private TimeSpan totalDuration = TimeSpan.Zero;

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

    [ObservableProperty]
    private List<LrcLine> lrcLines = [];

    [ObservableProperty]
    private LrcLine? currentLrcLine;

    [ObservableProperty]
    private PlaybackStateEnum currentPlaybackState = PlaybackStateEnum.Idle;

    [ObservableProperty]
    private bool isUserSeeking = false;

    [ObservableProperty]
    private bool isFavorite;

    // 事件
    public event EventHandler? LrcLinesUpdated;
    public event EventHandler<LrcLine?>? CurrentLineChanged;
    public event EventHandler? PlaybackPositionChanged;
    public event EventHandler? PlaybackStateChanged;

    public PlayerViewModel(
        IPlaybackService playbackService,
        ILearningService learningService,
        ISubtitleService subtitleService,
        IVocabularyService vocabularyService,
        IFavoriteService favoriteService,
        IAudioPlaybackService audioPlaybackService)
    {
        this.playbackService = playbackService;
        this.learningService = learningService;
        this.subtitleService = subtitleService;
        this.vocabularyService = vocabularyService;
        this.favoriteService = favoriteService;
        this.audioPlaybackService = audioPlaybackService;
        this.lrcParserService = new LrcParserService();

        // 订阅音频播放服务事件
        audioPlaybackService.StateChanged += AudioPlaybackService_StateChanged;
        audioPlaybackService.PositionChanged += AudioPlaybackService_PositionChanged;
        audioPlaybackService.PlaybackCompleted += AudioPlaybackService_PlaybackCompleted;
        audioPlaybackService.PlaybackError += AudioPlaybackService_PlaybackError;
    }

    /// <summary>
    /// 加载音频和字幕文件
    /// </summary>
    public async Task LoadAudioAndSubtitleAsync(string audioPath, string? subtitlePath)
    {
        try
        {
            IsBusy = true;

            // 加载音频
            await audioPlaybackService.LoadAudioAsync(audioPath);
            TotalDuration = audioPlaybackService.Duration;

            // 创建临时 Episode
            var episodeId = Guid.NewGuid().ToString();
            CurrentEpisode = new Episode
            {
                Id = episodeId,
                Title = System.IO.Path.GetFileNameWithoutExtension(audioPath),
                AudioUrl = audioPath,
                Duration = TotalDuration
            };

            // 加载字幕
            if (!string.IsNullOrEmpty(subtitlePath) && File.Exists(subtitlePath))
            {
                var lrcLines = await lrcParserService.LoadLrcFileAsync(subtitlePath, episodeId);
                LrcLines = lrcLines;
                LrcLinesUpdated?.Invoke(this, EventArgs.Empty);

                // 计算每行的结束时间
                for (int i = 0; i < LrcLines.Count; i++)
                {
                    if (LrcLines[i].EndTime == null && i + 1 < LrcLines.Count)
                    {
                        LrcLines[i].EndTime = LrcLines[i + 1].StartTime;
                    }
                    else if (LrcLines[i].EndTime == null)
                    {
                        LrcLines[i].EndTime = TotalDuration;
                    }
                }
            }

            // 检查收藏状态
            try
            {
                IsFavorite = await favoriteService.IsFavoriteAsync(episodeId);
            }
            catch
            {
                IsFavorite = Preferences.Get($"fav_{episodeId}", false);
            }

            // 更新当前字幕行
            UpdateCurrentLrcLine();

            System.Diagnostics.Debug.WriteLine($"Loaded audio: {audioPath}, LRC lines: {LrcLines.Count}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 仅为已加载的音频加载字幕（单独导入 LRC）
    /// </summary>
    public async Task LoadSubtitleAsync(string subtitlePath)
    {
        try
        {
            IsBusy = true;

            if (CurrentEpisode == null) return;

            var lrcLines = await lrcParserService.LoadLrcFileAsync(subtitlePath, CurrentEpisode.Id);
            LrcLines = lrcLines;
            LrcLinesUpdated?.Invoke(this, EventArgs.Empty);

            for (int i = 0; i < LrcLines.Count; i++)
            {
                if (LrcLines[i].EndTime == null && i + 1 < LrcLines.Count)
                    LrcLines[i].EndTime = LrcLines[i + 1].StartTime;
                else if (LrcLines[i].EndTime == null)
                    LrcLines[i].EndTime = TotalDuration;
            }

            UpdateCurrentLrcLine();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 切换收藏状态
    /// </summary>
    public async Task ToggleFavoriteAsync()
    {
        if (CurrentEpisode == null)
            return;

        try
        {
            if (IsFavorite)
            {
                await favoriteService.RemoveFavoriteAsync(CurrentEpisode);
                IsFavorite = false;
            }
            else
            {
                await favoriteService.AddFavoriteAsync(CurrentEpisode);
                IsFavorite = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling favorite via DB: {ex.Message}");
            // 回退到 Preferences
            var key = $"fav_{CurrentEpisode.Id}";
            if (IsFavorite)
            {
                Preferences.Remove(key);
                IsFavorite = false;
            }
            else
            {
                Preferences.Set(key, true);
                IsFavorite = true;
            }
        }
    }

    /// <summary>
    /// 切换播放/暂停
    /// </summary>
    public async Task TogglePlayPauseAsync()
    {
        try
        {
            if (CurrentPlaybackState == PlaybackStateEnum.Playing)
            {
                // 淡出：先渐弱再暂停
                await audioPlaybackService.FadeVolumeAsync(0f, 200);
                await audioPlaybackService.PauseAsync();
            }
            else if (CurrentPlaybackState == PlaybackStateEnum.Paused || CurrentPlaybackState == PlaybackStateEnum.Idle)
            {
                // 淡入：从静音开始播放，再渐强
                audioPlaybackService.SetVolume(0f);
                await audioPlaybackService.PlayAsync();
                await audioPlaybackService.FadeVolumeAsync(1f, 250);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling playback: {ex.Message}");
        }
    }

    /// <summary>
    /// 平滑调整音量（淡入/淡出）—— 已迁移到 IAudioPlaybackService
    /// </summary>
    [Obsolete]
    private async Task FadeVolumeAsync(float from, float to, int durationMs)
    {
        await audioPlaybackService.FadeVolumeAsync(to, (uint)durationMs);
    }

    /// <summary>
    /// 跳到上一句字幕
    /// </summary>
    public async Task SkipBackwardAsync()
    {
        // 找到当前字幕行索引
        var currentIndex = LrcLines.FindIndex(line =>
            CurrentPosition >= line.StartTime &&
            (line.EndTime == null || CurrentPosition < line.EndTime));

        // 找到上一句
        var targetIndex = currentIndex > 0 ? currentIndex - 1 : 0;
        if (targetIndex >= 0 && targetIndex < LrcLines.Count)
        {
            await SeekAsync(LrcLines[targetIndex].StartTime);
        }
        else
        {
            await SeekAsync(TimeSpan.Zero);
        }
    }

    /// <summary>
    /// 跳到下一句字幕
    /// </summary>
    public async Task SkipForwardAsync()
    {
        // 找到当前字幕行索引
        var currentIndex = LrcLines.FindIndex(line =>
            CurrentPosition >= line.StartTime &&
            (line.EndTime == null || CurrentPosition < line.EndTime));

        // 找到下一句（跳过当前行之后的那一句）
        var targetIndex = currentIndex + 1;
        if (targetIndex < LrcLines.Count)
        {
            await SeekAsync(LrcLines[targetIndex].StartTime);
        }
        else
        {
            // 没有更多字幕，保持当前位置或跳到最后
            await SeekAsync(TotalDuration);
        }
    }

    /// <summary>
    /// 定位到指定位置
    /// </summary>
    public async Task SeekAsync(TimeSpan position)
    {
        try
        {
            IsUserSeeking = true;
            CurrentPosition = position;
            await audioPlaybackService.SeekAsync(position);
            UpdateCurrentLrcLine();
        }
        finally
        {
            IsUserSeeking = false;
        }
    }

    /// <summary>
    /// 设置播放速度
    /// </summary>
    public void SetPlaybackRate(float rate)
    {
        try
        {
            var validRate = Math.Max(0.5f, Math.Min(rate, 3.0f));
            PlaybackRate = validRate;
            audioPlaybackService.SetPlaybackRate(validRate);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting playback rate: {ex.Message}");
        }
    }

    /// <summary>
    /// 添加单词到生词本
    /// </summary>
    public async Task AddWordToVocabularyAsync(string word)
    {
        await vocabularyService.AddToUserVocabularyAsync(word);
    }

    /// <summary>
    /// 获取已标记的生词集合
    /// </summary>
    public async Task<HashSet<string>> GetMarkedWordsAsync()
    {
        return await vocabularyService.GetMarkedWordsAsync();
    }

    /// <summary>
    /// 标记一个生词
    /// </summary>
    public async Task AddMarkedWordAsync(string word)
    {
        await vocabularyService.AddToUserVocabularyAsync(word, CurrentEpisode?.Id);
    }

    /// <summary>
    /// 取消标记一个生词
    /// </summary>
    public async Task RemoveMarkedWordAsync(string word)
    {
        await vocabularyService.RemoveFromUserVocabularyAsync(word);
    }

    /// <summary>
    /// 获取用户词汇及掌握度
    /// </summary>
    public async Task<Dictionary<string, int>> GetUserVocabularyWithLevelsAsync()
    {
        return await vocabularyService.GetUserVocabularyWithLevelsAsync();
    }

    /// <summary>
    /// 更新词汇掌握度
    /// </summary>
    public async Task UpdateMasteryLevelAsync(string word, int level)
    {
        await vocabularyService.UpdateMasteryLevelAsync(word, level);
    }

    /// <summary>
    /// 更新当前高亮的 LRC 行
    /// </summary>
    private void UpdateCurrentLrcLine()
    {
        LrcLine? newLine = null;

        if (LrcLines.Count > 0)
        {
            newLine = lrcParserService.GetCurrentLine(LrcLines, CurrentPosition);

            if (newLine == null && CurrentPosition <= LrcLines[0].StartTime)
            {
                newLine = LrcLines[0];
            }
        }

        if (newLine != CurrentLrcLine)
        {
            CurrentLrcLine = newLine;
            CurrentLineChanged?.Invoke(this, newLine);
        }
    }

    // ========== Audio Playback Service Event Handlers ==========

    private void AudioPlaybackService_StateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentPlaybackState = e.NewState;
            IsPlaying = e.NewState == PlaybackStateEnum.Playing;
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private void AudioPlaybackService_PositionChanged(object? sender, PlaybackPositionChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!IsUserSeeking)
            {
                CurrentPosition = e.CurrentPosition;
                TotalDuration = e.Duration;
                UpdateCurrentLrcLine();
                PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    private void AudioPlaybackService_PlaybackCompleted(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPlaying = false;
            CurrentPlaybackState = PlaybackStateEnum.Stopped;
        });
    }

    private void AudioPlaybackService_PlaybackError(object? sender, PlaybackErrorEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            System.Diagnostics.Debug.WriteLine($"Playback error: {e.Message}");
            CurrentPlaybackState = PlaybackStateEnum.Error;
        });
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        try
        {
            audioPlaybackService.StateChanged -= AudioPlaybackService_StateChanged;
            audioPlaybackService.PositionChanged -= AudioPlaybackService_PositionChanged;
            audioPlaybackService.PlaybackCompleted -= AudioPlaybackService_PlaybackCompleted;
            audioPlaybackService.PlaybackError -= AudioPlaybackService_PlaybackError;

            _positionUpdateCts?.Cancel();
            _positionUpdateCts?.Dispose();

            audioPlaybackService.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
        }
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
    public async Task SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            await LoadPodcastsAsync();
            return;
        }
        IsBusy = true;
        try
        {
            var all = await contentProvider.GetPodcastsAsync();
            Podcasts = all
                .Where(p => p.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || p.Author.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DeletePodcastAsync(Podcast podcast)
    {
        await contentProvider.DeletePodcastAsync(podcast.Id);
        await LoadPodcastsAsync();
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
    private List<string> subtitleLanguages = ["English (英文)", "Chinese (中文)", "Spanish (西班牙文)", "French (法文)", "German (德文)"];

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
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert("成功", "设置已保存", "确定");
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert("错误", $"保存失败: {ex.Message}", "确定");
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ResetToDefaultAsync()
    {
        var mainPage = Microsoft.Maui.Controls.Application.Current?.MainPage;
        if (mainPage == null) return;

        bool confirm = await mainPage.DisplayAlert(
            "确认", 
            "确定要恢复所有设置到默认值吗？", 
            "是", 
            "否");

        if (confirm)
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
}

/// <summary>
/// 单词本页面ViewModel
/// </summary>
public partial class VocabularyViewModel : BaseViewModel
{
    private readonly IVocabularyService vocabularyService;
    private List<Vocabulary> allVocabularies = [];

    [ObservableProperty]
    private List<Vocabulary> vocabularyList = [];

    [ObservableProperty]
    private string searchText = "";

    public VocabularyViewModel(IVocabularyService vocabularyService)
    {
        this.vocabularyService = vocabularyService;
    }

    [RelayCommand]
    public async Task LoadVocabularyAsync()
    {
        IsBusy = true;
        try
        {
            // 从服务加载用户的单词列表
            allVocabularies = await vocabularyService.GetUserVocabularyAsync();
            VocabularyList = allVocabularies;
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert("错误", $"加载单词本失败: {ex.Message}", "确定");
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadVocabularyAsync();
            return;
        }

        IsBusy = true;
        try
        {
            var searchTerm = SearchText.ToLower();
            var filtered = allVocabularies
                .Where(v => v.Word.ToLower().Contains(searchTerm) || 
                           v.Definition.ToLower().Contains(searchTerm))
                .ToList();
            VocabularyList = filtered;
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert("错误", $"搜索失败: {ex.Message}", "确定");
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ClearSearchAsync()
    {
        SearchText = "";
        await LoadVocabularyAsync();
    }

    [RelayCommand]
    public async Task SelectVocabularyAsync(Vocabulary item)
    {
        if (item == null) return;

        var updatedList = VocabularyList.Where(v => v.Word != item.Word).ToList();
        VocabularyList = updatedList;
        await vocabularyService.RemoveFromUserVocabularyAsync(item.Word);
    }

    public async Task UpdateMasteryLevelAsync(string word, int level)
    {
        await vocabularyService.UpdateMasteryLevelAsync(word, level);
    }
}
