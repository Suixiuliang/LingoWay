namespace LingoWay.Application.Services;

using LingoWay.Domain.Interfaces;
using LingoWay.Domain.Models;
using LingoWay.Infrastructure.Database;
using LingoWay.Infrastructure.Repositories;

/// <summary>
/// 内容提供商实现
/// </summary>
public class ContentProvider : IContentProvider
{
    private readonly PodcastRepository podcastRepo;
    private readonly EpisodeRepository episodeRepo;
    private readonly Infrastructure.Http.ContentClient httpClient;
    private readonly CacheService cacheService;

    public ContentProvider(AppDbContext dbContext)
    {
        podcastRepo = new PodcastRepository(dbContext);
        episodeRepo = new EpisodeRepository(dbContext);
        httpClient = new Infrastructure.Http.ContentClient();
        cacheService = new CacheService();
    }

    public async Task<List<Podcast>> GetPodcastsAsync()
    {
        return await podcastRepo.GetAllAsync();
    }

    public async Task<List<Episode>> GetEpisodesAsync(string podcastId)
    {
        return await episodeRepo.GetEpisodesByPodcastAsync(podcastId);
    }

    public async Task<Podcast?> GetPodcastAsync(string podcastId)
    {
        return await podcastRepo.GetByIdAsync(podcastId);
    }

    public async Task<Episode?> GetEpisodeAsync(string episodeId)
    {
        return await episodeRepo.GetByIdAsync(episodeId);
    }

    public async Task RefreshPodcastsAsync()
    {
        var podcasts = await podcastRepo.GetAllAsync();
        foreach (var podcast in podcasts)
        {
            if (string.IsNullOrEmpty(podcast.RssUrl))
                continue;

            try
            {
                var episodes = await httpClient.GetEpisodesFromRssAsync(podcast.RssUrl);

                // 检查并添加新剧集
                foreach (var episode in episodes)
                {
                    var existing = await episodeRepo.GetByIdAsync(episode.Id);
                    if (existing == null)
                    {
                        episode.PodcastId = podcast.Id;
                        await episodeRepo.AddAsync(episode);
                    }
                }

                podcast.LastUpdatedDate = DateTime.UtcNow;
                await podcastRepo.UpdateAsync(podcast);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新播客失败: {podcast.Title} - {ex.Message}");
            }
        }
    }

    public async Task AddCustomPodcastAsync(string rssUrl)
    {
        try
        {
            var episodes = await httpClient.GetEpisodesFromRssAsync(rssUrl);
            if (episodes.Count == 0)
                return;

            var podcast = new Podcast
            {
                Title = episodes.FirstOrDefault()?.Title ?? "Custom Podcast",
                RssUrl = rssUrl,
                CreatedDate = DateTime.UtcNow,
                Episodes = episodes
            };

            await podcastRepo.AddAsync(podcast);
        }
        catch
        {
            // 处理异常
        }
    }
}

/// <summary>
/// 搜索服务实现
/// </summary>
public class SearchService : ISearchService
{
    private readonly EpisodeRepository episodeRepo;
    private readonly PodcastRepository podcastRepo;
    private readonly VocabularyRepository vocabRepo;

    public SearchService(AppDbContext dbContext)
    {
        episodeRepo = new EpisodeRepository(dbContext);
        podcastRepo = new PodcastRepository(dbContext);
        vocabRepo = new VocabularyRepository(dbContext);
    }

    public async Task<List<Episode>> SearchEpisodesAsync(string query)
    {
        var episodes = await episodeRepo.GetAllAsync();
        return episodes
            .Where(e => e.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       e.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<Podcast>> SearchPodcastsAsync(string query)
    {
        var podcasts = await podcastRepo.GetAllAsync();
        return podcasts
            .Where(p => p.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       p.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<Vocabulary>> SearchVocabularyAsync(string query)
    {
        return await vocabRepo.SearchByWordAsync(query);
    }
}

/// <summary>
/// 收藏服务实现
/// </summary>
public class FavoriteService : IFavoriteService
{
    private readonly FavoriteRepository favoriteRepo;
    private readonly EpisodeRepository episodeRepo;

    public FavoriteService(AppDbContext dbContext)
    {
        favoriteRepo = new FavoriteRepository(dbContext);
        episodeRepo = new EpisodeRepository(dbContext);
    }

    public async Task AddFavoriteAsync(Episode episode)
    {
        var existing = await favoriteRepo.GetByEpisodeAsync(episode.Id);
        if (existing == null)
        {
            var favorite = new Favorite { EpisodeId = episode.Id };
            await favoriteRepo.AddAsync(favorite);
        }
    }

    public async Task RemoveFavoriteAsync(Episode episode)
    {
        var existing = await favoriteRepo.GetByEpisodeAsync(episode.Id);
        if (existing != null)
        {
            await favoriteRepo.DeleteAsync(existing);
        }
    }

    public async Task<List<Episode>> GetFavoritesAsync()
    {
        return await favoriteRepo.GetFavoritesAsync();
    }

    public async Task<bool> IsFavoriteAsync(string episodeId)
    {
        return await favoriteRepo.IsFavoriteAsync(episodeId);
    }
}

/// <summary>
/// 学习记录服务实现
/// </summary>
public class LearningService : ILearningService
{
    private readonly LearningRecordRepository learningRepo;

    public LearningService(AppDbContext dbContext)
    {
        learningRepo = new LearningRecordRepository(dbContext);
    }

    public async Task RecordPlaybackAsync(Episode episode, TimeSpan listenedDuration, TimeSpan lastPosition)
    {
        var record = await learningRepo.GetByEpisodeAsync(episode.Id);

        if (record == null)
        {
            record = new LearningRecord
            {
                EpisodeId = episode.Id,
                ListenedDuration = listenedDuration,
                LastPosition = lastPosition,
                LastPlayedTime = DateTime.UtcNow,
                PlayCount = 1
            };
            await learningRepo.AddAsync(record);
        }
        else
        {
            record.ListenedDuration = record.ListenedDuration.Add(listenedDuration);
            record.LastPosition = lastPosition;
            record.LastPlayedTime = DateTime.UtcNow;
            record.PlayCount++;
            await learningRepo.UpdateAsync(record);
        }

        // 计算完成百分比
        if (episode.Duration.TotalSeconds > 0)
        {
            record.CompletionPercentage = (lastPosition.TotalSeconds / episode.Duration.TotalSeconds) * 100;
        }
    }

    public async Task<LearningRecord?> GetLearningRecordAsync(string episodeId)
    {
        return await learningRepo.GetByEpisodeAsync(episodeId);
    }

    public async Task<List<LearningRecord>> GetRecentLearningRecordsAsync(int days = 30)
    {
        return await learningRepo.GetRecentRecordsAsync(days);
    }

    public async Task<double> GetTotalListeningTimeAsync()
    {
        return await learningRepo.GetTotalListeningTimeAsync();
    }

    public async Task<int> GetConsecutiveDaysAsync()
    {
        var records = await learningRepo.GetRecentRecordsAsync(365);
        if (records.Count == 0)
            return 0;

        var dates = records
            .Select(r => r.LastPlayedTime.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int consecutiveDays = 1;
        for (int i = 0; i < dates.Count - 1; i++)
        {
            if ((dates[i] - dates[i + 1]).TotalDays == 1)
            {
                consecutiveDays++;
            }
            else
            {
                break;
            }
        }

        return consecutiveDays;
    }
}

/// <summary>
/// 词汇服务实现
/// </summary>
public class VocabularyService : IVocabularyService
{
    private readonly VocabularyRepository vocabRepo;
    private readonly UserVocabularyRepository userVocabRepo;

    public VocabularyService(AppDbContext dbContext)
    {
        vocabRepo = new VocabularyRepository(dbContext);
        userVocabRepo = new UserVocabularyRepository(dbContext);
    }

    public async Task<Vocabulary?> GetVocabularyAsync(string word)
    {
        return await vocabRepo.GetByWordAsync(word);
    }

    public async Task<List<Vocabulary>> ExtractVocabularyFromTextAsync(string text)
    {
        // 简单的单词分割实现
        var words = text.Split(new[] { ' ', ',', '.', '!', '?', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLower())
            .Distinct()
            .ToList();

        var vocabularies = new List<Vocabulary>();
        foreach (var word in words)
        {
            var vocab = await vocabRepo.GetByWordAsync(word);
            if (vocab != null)
            {
                vocabularies.Add(vocab);
            }
        }

        return vocabularies;
    }

    public async Task<List<Vocabulary>> GetUserVocabularyAsync()
    {
        var userVocabs = await userVocabRepo.GetAllAsync();
        var result = new List<Vocabulary>();

        foreach (var uv in userVocabs)
        {
            var vocab = await vocabRepo.GetByWordAsync(uv.Word);
            if (vocab != null)
            {
                result.Add(vocab);
            }
        }

        return result;
    }

    public async Task AddToUserVocabularyAsync(string word)
    {
        var existing = await userVocabRepo.GetByWordAsync(word);
        if (existing == null)
        {
            var userVocab = new UserVocabulary { Word = word };
            await userVocabRepo.AddAsync(userVocab);
        }
    }

    public async Task RemoveFromUserVocabularyAsync(string word)
    {
        var existing = await userVocabRepo.GetByWordAsync(word);
        if (existing != null)
        {
            await userVocabRepo.DeleteAsync(existing);
        }
    }

    public async Task UpdateMasteryLevelAsync(string word, int level)
    {
        var userVocab = await userVocabRepo.GetByWordAsync(word);
        if (userVocab != null)
        {
            userVocab.MasteryLevel = Math.Max(0, Math.Min(level, 5));
            userVocab.LastReviewedDate = DateTime.UtcNow;
            userVocab.ReviewCount++;
            await userVocabRepo.UpdateAsync(userVocab);
        }
    }
}

/// <summary>
/// 字幕服务实现
/// </summary>
public class SubtitleService : ISubtitleService
{
    private readonly AppDbContext dbContext;

    public SubtitleService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<List<Subtitle>> GetSubtitlesAsync(string episodeId)
    {
        var subtitles = dbContext.Subtitles
            .Where(s => s.EpisodeId == episodeId)
            .OrderBy(s => s.StartTime)
            .ToList();

        return await Task.FromResult(subtitles);
    }

    public async Task<string> GenerateSubtitlesAsync(string audioPath)
    {
        // 这里应该集成Whisper或其他STT服务
        // 目前返回占位符
        return await Task.FromResult("字幕生成功能即将推出");
    }

    public async Task<string> TranslateSubtitleAsync(string text, string targetLanguage)
    {
        // 集成翻译API
        return await Task.FromResult(text);
    }

    public async Task SaveSubtitlesAsync(Episode episode, List<Subtitle> subtitles)
    {
        foreach (var subtitle in subtitles)
        {
            subtitle.EpisodeId = episode.Id;
        }

        dbContext.Subtitles.AddRange(subtitles);
        await dbContext.SaveChangesAsync();
    }
}

/// <summary>
/// 翻译服务实现
/// </summary>
public class TranslationService : ITranslationService
{
    private readonly Infrastructure.Http.TranslationClient httpClient;

    public TranslationService()
    {
        httpClient = new Infrastructure.Http.TranslationClient();
    }

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        // 实现翻译逻辑
        return await Task.FromResult(text);
    }

    public async Task<string> TranslateBatchAsync(List<string> texts, string sourceLanguage, string targetLanguage)
    {
        var results = await httpClient.TranslateBatchAsync(texts, sourceLanguage, targetLanguage);
        return string.Join("\n", results);
    }
}
