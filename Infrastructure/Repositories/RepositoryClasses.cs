namespace LingoWay.Infrastructure.Repositories;

using LingoWay.Domain.Models;
using LingoWay.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// 通用Repository基类
/// </summary>
public abstract class BaseRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    protected BaseRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(object id)
    {
        return await DbSet.FindAsync(id);
    }

    public virtual async Task<List<TEntity>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task<bool> AddAsync(TEntity entity)
    {
        await DbSet.AddAsync(entity);
        return await SaveChangesAsync();
    }

    public virtual async Task<bool> AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await DbSet.AddRangeAsync(entities);
        return await SaveChangesAsync();
    }

    public virtual async Task<bool> UpdateAsync(TEntity entity)
    {
        DbSet.Update(entity);
        return await SaveChangesAsync();
    }

    public virtual async Task<bool> DeleteAsync(TEntity entity)
    {
        DbSet.Remove(entity);
        return await SaveChangesAsync();
    }

    public virtual async Task<bool> DeleteRangeAsync(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
        return await SaveChangesAsync();
    }

    public virtual async Task<bool> SaveChangesAsync()
    {
        try
        {
            await DbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Episode Repository
/// </summary>
public class EpisodeRepository : BaseRepository<Episode>
{
    public EpisodeRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<List<Episode>> GetEpisodesByPodcastAsync(string podcastId)
    {
        return await DbSet
            .Where(e => e.PodcastId == podcastId)
            .OrderByDescending(e => e.PublishedDate)
            .ToListAsync();
    }

    public async Task<List<Episode>> GetRecentEpisodesAsync(int count = 20)
    {
        return await DbSet
            .OrderByDescending(e => e.PublishedDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Episode?> GetEpisodeWithSubtitlesAsync(string episodeId)
    {
        return await DbSet
            .Include(e => e.Subtitles)
            .FirstOrDefaultAsync(e => e.Id == episodeId);
    }
}

/// <summary>
/// Podcast Repository
/// </summary>
public class PodcastRepository : BaseRepository<Podcast>
{
    public PodcastRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<Podcast?> GetPodcastWithEpisodesAsync(string podcastId)
    {
        return await DbSet
            .Include(p => p.Episodes)
            .FirstOrDefaultAsync(p => p.Id == podcastId);
    }

    public async Task<List<Podcast>> GetPodcastsByCategoryAsync(string category)
    {
        return await DbSet
            .Where(p => p.Category == category)
            .ToListAsync();
    }

    public async Task<List<Podcast>> GetPodcastsByDifficultyAsync(DifficultyLevel difficulty)
    {
        return await DbSet
            .Where(p => p.DifficultyLevel == difficulty)
            .ToListAsync();
    }
}

/// <summary>
/// Vocabulary Repository
/// </summary>
public class VocabularyRepository : BaseRepository<Vocabulary>
{
    public VocabularyRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<Vocabulary?> GetByWordAsync(string word)
    {
        return await DbSet.FirstOrDefaultAsync(v => v.Word == word);
    }

    public async Task<List<Vocabulary>> SearchByWordAsync(string searchTerm)
    {
        return await DbSet
            .Where(v => v.Word.Contains(searchTerm))
            .ToListAsync();
    }

    public async Task<List<Vocabulary>> GetVocabulariesByDifficultyAsync(DifficultyLevel difficulty)
    {
        return await DbSet
            .Where(v => v.Difficulty == difficulty)
            .ToListAsync();
    }
}

/// <summary>
/// Download Repository
/// </summary>
public class DownloadRepository : BaseRepository<Download>
{
    public DownloadRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<List<Download>> GetDownloadsByStatusAsync(DownloadStatus status)
    {
        return await DbSet
            .Where(d => d.Status == status)
            .ToListAsync();
    }

    public async Task<List<Download>> GetActiveDownloadsAsync()
    {
        return await DbSet
            .Where(d => d.Status == DownloadStatus.Downloading || d.Status == DownloadStatus.Pending)
            .ToListAsync();
    }

    public async Task<Download?> GetDownloadByEpisodeAsync(string episodeId)
    {
        return await DbSet
            .FirstOrDefaultAsync(d => d.EpisodeId == episodeId);
    }
}

/// <summary>
/// LearningRecord Repository
/// </summary>
public class LearningRecordRepository : BaseRepository<LearningRecord>
{
    public LearningRecordRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<LearningRecord?> GetByEpisodeAsync(string episodeId)
    {
        return await DbSet.FirstOrDefaultAsync(l => l.EpisodeId == episodeId);
    }

    public async Task<List<LearningRecord>> GetRecentRecordsAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .Where(l => l.LastPlayedTime >= startDate)
            .OrderByDescending(l => l.LastPlayedTime)
            .ToListAsync();
    }

    public async Task<double> GetTotalListeningTimeAsync()
    {
        var records = await DbSet.ToListAsync();
        return records.Sum(r => r.ListenedDuration.TotalSeconds);
    }
}

/// <summary>
/// Favorite Repository
/// </summary>
public class FavoriteRepository : BaseRepository<Favorite>
{
    public FavoriteRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<bool> IsFavoriteAsync(string episodeId)
    {
        return await DbSet.AnyAsync(f => f.EpisodeId == episodeId);
    }

    public async Task<List<Episode>> GetFavoritesAsync()
    {
        return await DbSet
            .Include(f => f.Episode)
            .Select(f => f.Episode!)
            .ToListAsync();
    }

    public async Task<Favorite?> GetByEpisodeAsync(string episodeId)
    {
        return await DbSet.FirstOrDefaultAsync(f => f.EpisodeId == episodeId);
    }
}

/// <summary>
/// UserVocabulary Repository
/// </summary>
public class UserVocabularyRepository : BaseRepository<UserVocabulary>
{
    public UserVocabularyRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<UserVocabulary?> GetByWordAsync(string word)
    {
        return await DbSet.FirstOrDefaultAsync(uv => uv.Word == word);
    }

    public async Task<List<UserVocabulary>> GetMasteredVocabularyAsync()
    {
        return await DbSet
            .Where(uv => uv.MasteryLevel >= 5)
            .ToListAsync();
    }

    public async Task<List<UserVocabulary>> GetNeedReviewAsync(int daysThreshold = 7)
    {
        var reviewDate = DateTime.UtcNow.AddDays(-daysThreshold);
        return await DbSet
            .Where(uv => uv.LastReviewedDate < reviewDate && uv.MasteryLevel < 5)
            .ToListAsync();
    }
}

/// <summary>
/// UserSettings Repository
/// </summary>
public class UserSettingsRepository : BaseRepository<UserSettings>
{
    public UserSettingsRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<UserSettings> GetDefaultSettingsAsync()
    {
        var settings = await DbSet.FirstOrDefaultAsync(us => us.Id == "default");
        if (settings == null)
        {
            settings = new UserSettings();
            await AddAsync(settings);
        }
        return settings;
    }
}
