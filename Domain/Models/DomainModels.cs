namespace LingoWay.Domain.Models;

/// <summary>
/// 播客/内容源
/// </summary>
public class Podcast
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public string CoverUrl { get; set; } = "";
    public string RssUrl { get; set; } = "";

    public string Language { get; set; } = "en";
    public string Category { get; set; } = "";
    public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Intermediate;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedDate { get; set; }

    public virtual ICollection<Episode> Episodes { get; set; } = [];
}

/// <summary>
/// 剧集/资源单元
/// </summary>
public class Episode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string SourceUrl { get; set; } = "";

    public DateTime PublishedDate { get; set; }
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;

    public string CoverUrl { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    public string VideoUrl { get; set; } = "";

    // 关系
    public string PodcastId { get; set; } = "";
    public virtual Podcast? Podcast { get; set; }

    // 导航属性
    public virtual ICollection<Subtitle> Subtitles { get; set; } = [];
    public virtual ICollection<LrcLine> LrcLines { get; set; } = [];
    public virtual ICollection<Download> Downloads { get; set; } = [];
    public virtual ICollection<LearningRecord> LearningRecords { get; set; } = [];
    public virtual ICollection<Favorite> Favorites { get; set; } = [];
    public virtual ICollection<PlaybackState> PlaybackStates { get; set; } = [];
}

/// <summary>
/// 字幕条目
/// </summary>
public class Subtitle
{
    public int Id { get; set; }

    public string EpisodeId { get; set; } = "";
    public virtual Episode? Episode { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public string EnglishText { get; set; } = "";
    public string ChineseText { get; set; } = "";

    // 词汇提及
    public virtual ICollection<VocabularyMention> VocabularyMentions { get; set; } = [];
}

/// <summary>
/// 词汇条目
/// </summary>
public class Vocabulary
{
    public string Word { get; set; } = "";

    public string PartOfSpeech { get; set; } = ""; // noun, verb, adj, adv, etc.
    public string Definition { get; set; } = "";
    public string ChineseTranslation { get; set; } = "";
    public string Phonetic { get; set; } = "";     // /fə'nɛtɪk/

    public string WordRoot { get; set; } = "";     // 词根分解
    public string Etymology { get; set; } = "";    // 词源

    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Intermediate;

    // 示例句子
    public string ExampleSentence { get; set; } = "";
    public string ExampleTranslation { get; set; } = "";

    // 关系词
    public string Synonyms { get; set; } = "";     // JSON array
    public string Antonyms { get; set; } = "";     // JSON array

    // 元数据
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<VocabularyMention> Mentions { get; set; } = [];
    public virtual ICollection<UserVocabulary> UserVocabularies { get; set; } = [];
}

/// <summary>
/// 词汇在字幕中的提及
/// </summary>
public class VocabularyMention
{
    public int Id { get; set; }

    public string Word { get; set; } = "";
    public virtual Vocabulary? Vocabulary { get; set; }

    public int SubtitleId { get; set; }
    public virtual Subtitle? Subtitle { get; set; }

    public int CharacterPosition { get; set; }  // 在字幕文本中的位置
}

/// <summary>
/// 下载记录
/// </summary>
public class Download
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string EpisodeId { get; set; } = "";
    public virtual Episode? Episode { get; set; }

    public string LocalPath { get; set; } = "";
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }

    public DownloadStatus Status { get; set; } = DownloadStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedDate { get; set; }
}

/// <summary>
/// 学习记录
/// </summary>
public class LearningRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string EpisodeId { get; set; } = "";
    public virtual Episode? Episode { get; set; }

    public TimeSpan ListenedDuration { get; set; }
    public TimeSpan LastPosition { get; set; }

    public DateTime LastPlayedTime { get; set; }
    public int PlayCount { get; set; }

    public double CompletionPercentage { get; set; } // 0-100
}

/// <summary>
/// 收藏
/// </summary>
public class Favorite
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string EpisodeId { get; set; } = "";
    public virtual Episode? Episode { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = "";
}

/// <summary>
/// 用户词汇本
/// </summary>
public class UserVocabulary
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Word { get; set; } = "";
    public virtual Vocabulary? Vocabulary { get; set; }

    public int ReviewCount { get; set; }
    public DateTime LastReviewedDate { get; set; } = DateTime.UtcNow;
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;

    public int MasteryLevel { get; set; } // 0-5, 5为已掌握
}

/// <summary>
/// 用户设置
/// </summary>
public class UserSettings
{
    public string Id { get; set; } = "default";

    // 播放设置
    public float PlaybackRate { get; set; } = 1.0f;
    public bool IsBackgroundPlayEnabled { get; set; } = true;
    public bool IsAutoPlayNextEnabled { get; set; } = true;

    // 字幕设置
    public bool IsSubtitleEnabled { get; set; } = true;
    public string SubtitleLanguage { get; set; } = "en"; // "en", "zh", "dual"
    public int SubtitleFontSize { get; set; } = 16;

    // UI设置
    public string ThemeMode { get; set; } = "Dark"; // "Dark", "Light", "System"
    public bool IsHighContrastEnabled { get; set; } = false;

    // 下载设置
    public bool IsWifiOnlyDownloadEnabled { get; set; } = false;
    public int MaxConcurrentDownloads { get; set; } = 3;

    // 隐私设置
    public bool IsAnalyticsEnabled { get; set; } = false;
    public bool IsCrashReportingEnabled { get; set; } = true;

    public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// LRC 字幕条目 - 用于解析和显示 LRC 格式字幕
/// </summary>
public class LrcLine
{
    public int Id { get; set; }

    public string EpisodeId { get; set; } = "";
    public virtual Episode? Episode { get; set; }

    // 时间相关
    public TimeSpan StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }  // 可选，用于计算下一行的结束时间

    // 内容
    public string EnglishText { get; set; } = "";
    public string ChineseText { get; set; } = "";

    // 单词分解
    public virtual ICollection<LrcWord> Words { get; set; } = [];

    public int LineNumber { get; set; }  // 在解析后歌词中的行号
}

/// <summary>
/// LRC 行中的单词
/// </summary>
public class LrcWord
{
    public int Id { get; set; }

    public int LrcLineId { get; set; }
    public virtual LrcLine? LrcLine { get; set; }

    public string Word { get; set; } = "";
    public int PositionInLine { get; set; }  // 单词在行中的位置（0-based）
    public bool IsMarked { get; set; }

    // 关联到生词本
    public string? VocabularyWord { get; set; }
    public virtual Vocabulary? Vocabulary { get; set; }
}

/// <summary>
/// 播放状态
/// </summary>
public class PlaybackState
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string EpisodeId { get; set; } = "";
    public virtual Episode? Episode { get; set; }

    // 当前播放位置
    public TimeSpan CurrentPosition { get; set; } = TimeSpan.Zero;

    // 当前高亮的 LRC 行
    public int? CurrentLrcLineId { get; set; }

    // 当前高亮的单词
    public int? CurrentHighlightedWordId { get; set; }

    public DateTime LastUpdatedTime { get; set; } = DateTime.UtcNow;
}

// ============ Enums ============

public enum DifficultyLevel
{
    Beginner = 0,
    Elementary = 1,
    Intermediate = 2,
    UpperIntermediate = 3,
    Advanced = 4,
    Expert = 5
}

public enum DownloadStatus
{
    Pending = 0,
    Downloading = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
