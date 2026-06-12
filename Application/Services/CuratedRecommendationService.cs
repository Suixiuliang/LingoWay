namespace LingoWay.Application.Services;

using LingoWay.Domain.Models;
using LingoWay.Infrastructure.Http;
using System.ComponentModel;

/// <summary>
/// 精选英语学习播客推荐服务
/// 
/// 数据来源:
/// 1. GitHub yvoronoy/awesome-english 仓库的精选播客列表
/// 2. Apple Podcast Search API 获取封面、RSS、描述等富数据
/// 
/// 首选使用 Apple API 获取实时元数据; 网络不可用时回退到内置种子数据
/// </summary>
public class CuratedRecommendationService
{
    private readonly ApplePodcastClient _appleClient;
    private List<Podcast>? _cachedRecommendations;

    public CuratedRecommendationService()
    {
        _appleClient = new ApplePodcastClient();
    }

    // ============ awesome-english 精选播客 (名称 + 难度 + 分类) ============

    /// <summary>
    /// 从 awesome-english 仓库提取的精选英语学习播客
    /// Category: ESL / Education / Language / News / Tech / Interview
    /// </summary>
    private static readonly (string Name, string Category, DifficultyLevel Level)[] CuratedPodcasts =
    [
        // ---- ESL / Education (英语教学类) ----
        ("6 Minute English",               "ESL",       DifficultyLevel.Elementary),
        ("The English We Speak",           "ESL",       DifficultyLevel.Elementary),
        ("All Ears English",               "ESL",       DifficultyLevel.Intermediate),
        ("Culips ESL Podcast",             "ESL",       DifficultyLevel.Intermediate),
        ("Luke's English Podcast",         "ESL",       DifficultyLevel.Intermediate),
        ("English Learning for Curious Minds","ESL",   DifficultyLevel.UpperIntermediate),
        ("RealLife English",               "ESL",       DifficultyLevel.Intermediate),
        ("Speak English with Tiffani",     "ESL",       DifficultyLevel.Intermediate),
        ("ESL Pod",                        "ESL",       DifficultyLevel.Beginner),
        ("A Way with Words",               "Language",  DifficultyLevel.Advanced),

        // ---- News (新闻类 - 日常听力) ----
        ("CNN 10",                         "News",      DifficultyLevel.Intermediate),
        ("NPR News Now",                   "News",      DifficultyLevel.UpperIntermediate),
        ("Global News Podcast",            "News",      DifficultyLevel.UpperIntermediate),

        // ---- Tech (科技类) ----
        ("Hard Fork",                      "Tech",      DifficultyLevel.UpperIntermediate),
        ("Bloomberg Technology",           "Tech",      DifficultyLevel.Advanced),
        ("Talk Python To Me",              "Tech",      DifficultyLevel.Advanced),

        // ---- Interview (访谈类 - 高级学习者) ----
        ("The Ezra Klein Show",            "Interview", DifficultyLevel.Advanced),
        ("The Tim Ferriss Show",           "Interview", DifficultyLevel.Advanced),
        ("How I Built This with Guy Raz",  "Interview", DifficultyLevel.UpperIntermediate),
        ("Conan O'Brien Needs A Friend",   "Interview", DifficultyLevel.Advanced),
        ("SmartLess",                      "Interview", DifficultyLevel.Advanced),
        ("Fresh Air",                      "Interview", DifficultyLevel.UpperIntermediate),
        ("Armchair Expert with Dax Shepard","Interview",DifficultyLevel.Advanced),
        ("Lex Fridman Podcast",            "Interview", DifficultyLevel.Expert),
    ];

    // ---- 难度到中文标签 ----
    private static readonly Dictionary<DifficultyLevel, string> LevelLabels = new()
    {
        [DifficultyLevel.Beginner]          = "入门",
        [DifficultyLevel.Elementary]        = "初级",
        [DifficultyLevel.Intermediate]      = "中级",
        [DifficultyLevel.UpperIntermediate] = "中高级",
        [DifficultyLevel.Advanced]          = "高级",
        [DifficultyLevel.Expert]            = "专家",
    };

    /// <summary>
    /// 获取推荐播客列表。
    /// 优先从 Apple API 获取实时元数据 (封面+RSS+描述);
    /// 网络不可用时使用缓存; 缓存也为空则使用内置种子数据。
    /// </summary>
    /// <param name="forceRefresh">忽略缓存，强制重新请求 Apple API</param>
    public async Task<List<RecommendedPodcast>> GetRecommendationsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedRecommendations != null)
            return ToRecommendedList(_cachedRecommendations);

        var podcasts = new List<Podcast>();

        try
        {
            // 批量并发搜索 Apple Podcast API
            var names = CuratedPodcasts.Select(c => c.Name);
            var lookup = await _appleClient.SearchBatchAsync(names);

            foreach (var (name, category, level) in CuratedPodcasts)
            {
                Podcast podcast;

                if (lookup.TryGetValue(name, out var result) && result != null)
                {
                    // 使用 Apple API 返回的元数据
                    podcast = new Podcast
                    {
                        Id = $"rec_{result.CollectionId}",
                        Title = result.CollectionName,
                        Author = result.ArtistName,
                        Description = $"{result.PrimaryGenreName} · {result.TrackCount} episodes",
                        CoverUrl = !string.IsNullOrEmpty(result.ArtworkUrl600)
                            ? result.ArtworkUrl600
                            : result.ArtworkUrl100,
                        RssUrl = result.FeedUrl,
                        Language = "en",
                        Category = category,
                        DifficultyLevel = level,
                    };
                }
                else
                {
                    // Apple API 未找到，使用内置种子数据 (没有封面和 RSS)
                    podcast = new Podcast
                    {
                        Id = $"rec_seed_{Guid.NewGuid():N}"[..16],
                        Title = name,
                        Author = "",
                        Description = $"精选英语播客 · {LevelLabels.GetValueOrDefault(level, "中级")}",
                        CoverUrl = "",
                        RssUrl = "",
                        Language = "en",
                        Category = category,
                        DifficultyLevel = level,
                    };
                }

                podcasts.Add(podcast);
            }

            _cachedRecommendations = podcasts;
        }
        catch
        {
            // 网络完全不可用：使用内置种子
            if (_cachedRecommendations != null)
                return ToRecommendedList(_cachedRecommendations);

            podcasts = CuratedPodcasts.Select(c => new Podcast
            {
                Id = $"rec_seed_{Guid.NewGuid():N}"[..16],
                Title = c.Name,
                Author = "",
                Description = $"精选英语播客 · {LevelLabels.GetValueOrDefault(c.Level, "中级")}",
                CoverUrl = "",
                RssUrl = "",
                Language = "en",
                Category = c.Category,
                DifficultyLevel = c.Level,
            }).ToList();

            _cachedRecommendations = podcasts;
        }

        return ToRecommendedList(podcasts);
    }

    /// <summary>
    /// 按分类获取推荐
    /// </summary>
    public async Task<List<RecommendedPodcast>> GetByCategoryAsync(string category)
    {
        var all = await GetRecommendationsAsync();
        return all.Where(r => r.Category == category).ToList();
    }

    /// <summary>
    /// 获取所有可用分类
    /// </summary>
    public static List<string> GetCategories() =>
        CuratedPodcasts.Select(c => c.Category).Distinct().ToList();

    private static List<RecommendedPodcast> ToRecommendedList(List<Podcast> podcasts)
    {
        return podcasts.Select(p => new RecommendedPodcast(p)).ToList();
    }
}

/// <summary>
/// UI 展示用的推荐播客包装
/// </summary>
public class RecommendedPodcast : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Podcast Podcast { get; }

    public string Id => Podcast.Id;
    public string Title => Podcast.Title;
    public string Author => Podcast.Author;
    public string Description => Podcast.Description;
    public string CoverUrl => Podcast.CoverUrl;
    public string RssUrl => Podcast.RssUrl;
    public string Language => Podcast.Language;
    public string Category => Podcast.Category;

    public DifficultyLevel Difficulty => Podcast.DifficultyLevel;

    /// <summary>封面是否可用</summary>
    public bool HasCover => !string.IsNullOrEmpty(CoverUrl);

    /// <summary>是否可一键订阅 (有 RSS 源)</summary>
    public bool HasRssFeed => !string.IsNullOrEmpty(RssUrl);

    /// <summary>难度中文标签</summary>
    public string LevelLabel => Podcast.DifficultyLevel switch
    {
        DifficultyLevel.Beginner          => "入门",
        DifficultyLevel.Elementary        => "初级",
        DifficultyLevel.Intermediate      => "中级",
        DifficultyLevel.UpperIntermediate => "中高级",
        DifficultyLevel.Advanced          => "高级",
        DifficultyLevel.Expert            => "专家",
        _ => "中级",
    };

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new(nameof(IsExpanded)));
        }
    }

    public RecommendedPodcast(Podcast podcast)
    {
        Podcast = podcast;
    }
}
