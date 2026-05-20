namespace LingoWay.Domain.Interfaces;

using LingoWay.Domain.Models;

/// <summary>
/// 播放控制服务接口
/// </summary>
public interface IPlaybackService
{
    Task PlayAsync(Episode episode);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();

    Task SeekAsync(TimeSpan position);
    Task SetPlaybackRateAsync(float rate);

    bool IsPlaying { get; }
    TimeSpan CurrentPosition { get; }
    float PlaybackRate { get; }
    Episode? CurrentEpisode { get; }
}

/// <summary>
/// 下载管理服务接口
/// </summary>
public interface IDownloadService
{
    Task<Download> EnqueueAsync(Episode episode);
    IAsyncEnumerable<DownloadProgress> DownloadAsync(Download download);
    Task DeleteAsync(Download download);
    Task<List<Download>> GetActiveDownloadsAsync();
    Task<List<Download>> GetCompletedDownloadsAsync();
}

/// <summary>
/// 下载进度
/// </summary>
public class DownloadProgress
{
    public string DownloadId { get; set; } = "";
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
    public double ProgressPercentage => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;
}

/// <summary>
/// 字幕服务接口
/// </summary>
public interface ISubtitleService
{
    Task<List<Subtitle>> GetSubtitlesAsync(string episodeId);
    Task<string> GenerateSubtitlesAsync(string audioPath);
    Task<string> TranslateSubtitleAsync(string text, string targetLanguage);
    Task SaveSubtitlesAsync(Episode episode, List<Subtitle> subtitles);
}

/// <summary>
/// 词汇服务接口
/// </summary>
public interface IVocabularyService
{
    Task<Vocabulary?> GetVocabularyAsync(string word);
    Task<List<Vocabulary>> ExtractVocabularyFromTextAsync(string text);
    Task<List<Vocabulary>> GetUserVocabularyAsync();
    Task AddToUserVocabularyAsync(string word);
    Task RemoveFromUserVocabularyAsync(string word);
    Task UpdateMasteryLevelAsync(string word, int level);
}

/// <summary>
/// 翻译服务接口
/// </summary>
public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
    Task<string> TranslateBatchAsync(List<string> texts, string sourceLanguage, string targetLanguage);
}

/// <summary>
/// 内容提供商接口
/// </summary>
public interface IContentProvider
{
    Task<List<Podcast>> GetPodcastsAsync();
    Task<List<Episode>> GetEpisodesAsync(string podcastId);
    Task<Podcast?> GetPodcastAsync(string podcastId);
    Task<Episode?> GetEpisodeAsync(string episodeId);
    Task RefreshPodcastsAsync();
    Task AddCustomPodcastAsync(string rssUrl);
}

/// <summary>
/// 存储服务接口
/// </summary>
public interface IStorageService
{
    Task<string> GetAppDataDirectoryAsync();
    Task<string> GetCacheDirectoryAsync();
    Task<string> GetDownloadsDirectoryAsync();
    Task<bool> FileExistsAsync(string filePath);
    Task DeleteFileAsync(string filePath);
    Task<long> GetFileSizeAsync(string filePath);
}

/// <summary>
/// 学习记录服务接口
/// </summary>
public interface ILearningService
{
    Task RecordPlaybackAsync(Episode episode, TimeSpan listenedDuration, TimeSpan lastPosition);
    Task<LearningRecord?> GetLearningRecordAsync(string episodeId);
    Task<List<LearningRecord>> GetRecentLearningRecordsAsync(int days = 30);
    Task<double> GetTotalListeningTimeAsync();
    Task<int> GetConsecutiveDaysAsync();
}

/// <summary>
/// 搜索服务接口
/// </summary>
public interface ISearchService
{
    Task<List<Episode>> SearchEpisodesAsync(string query);
    Task<List<Podcast>> SearchPodcastsAsync(string query);
    Task<List<Vocabulary>> SearchVocabularyAsync(string query);
}

/// <summary>
/// 收藏服务接口
/// </summary>
public interface IFavoriteService
{
    Task AddFavoriteAsync(Episode episode);
    Task RemoveFavoriteAsync(Episode episode);
    Task<List<Episode>> GetFavoritesAsync();
    Task<bool> IsFavoriteAsync(string episodeId);
}
