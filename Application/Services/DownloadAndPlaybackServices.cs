namespace LingoWay.Application.Services;

using LingoWay.Domain.Interfaces;
using LingoWay.Domain.Models;
using LingoWay.Infrastructure.Database;
using LingoWay.Infrastructure.Repositories;
using LingoWay.Infrastructure.Storage;

/// <summary>
/// 下载服务实现
/// </summary>
public class DownloadService : IDownloadService
{
    private readonly DownloadRepository downloadRepo;
    private readonly EpisodeRepository episodeRepo;
    private readonly FileStorageService fileStorage;
    private readonly Infrastructure.Http.ContentClient httpClient;

    public DownloadService(AppDbContext dbContext)
    {
        downloadRepo = new DownloadRepository(dbContext);
        episodeRepo = new EpisodeRepository(dbContext);
        fileStorage = new FileStorageService();
        httpClient = new Infrastructure.Http.ContentClient();
    }

    public async Task<Download> EnqueueAsync(Episode episode)
    {
        var download = new Download
        {
            EpisodeId = episode.Id,
            Status = DownloadStatus.Pending,
            TotalBytes = 0,
            CreatedDate = DateTime.UtcNow
        };

        await downloadRepo.AddAsync(download);
        return download;
    }

    public async IAsyncEnumerable<DownloadProgress> DownloadAsync(Download download)
    {
        var episode = await episodeRepo.GetByIdAsync(download.EpisodeId);
        if (episode == null || string.IsNullOrEmpty(episode.AudioUrl))
            yield break;

        download.Status = DownloadStatus.Downloading;
        await downloadRepo.UpdateAsync(download);

        // 下载文件
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(300) };
        using var response = await httpClient.GetAsync(episode.AudioUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        download.TotalBytes = totalBytes;

        var downloadsDir = await fileStorage.GetDownloadsDirectoryAsync();
        var fileName = $"{episode.Id}.mp3";
        var filePath = Path.Combine(downloadsDir, fileName);

        using var sourceStream = await response.Content.ReadAsStreamAsync();
        using var targetStream = File.Create(filePath);

        var buffer = new byte[8192];
        int bytesRead;
        long totalRead = 0;

        while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await targetStream.WriteAsync(buffer, 0, bytesRead);
            totalRead += bytesRead;
            download.DownloadedBytes = totalRead;

            yield return new DownloadProgress
            {
                DownloadId = download.Id,
                DownloadedBytes = totalRead,
                TotalBytes = totalBytes
            };
        }

        download.Status = DownloadStatus.Completed;
        download.LocalPath = filePath;
        download.CompletedDate = DateTime.UtcNow;
        await downloadRepo.UpdateAsync(download);
    }

    public async Task DeleteAsync(Download download)
    {
        if (!string.IsNullOrEmpty(download.LocalPath) && File.Exists(download.LocalPath))
        {
            await fileStorage.DeleteFileAsync(download.LocalPath);
        }
        await downloadRepo.DeleteAsync(download);
    }

    public async Task<List<Download>> GetActiveDownloadsAsync()
    {
        return await downloadRepo.GetActiveDownloadsAsync();
    }

    public async Task<List<Download>> GetCompletedDownloadsAsync()
    {
        return await downloadRepo.GetDownloadsByStatusAsync(DownloadStatus.Completed);
    }
}

/// <summary>
/// 播放服务实现
/// </summary>
public class PlaybackService : IPlaybackService
{
    private Episode? currentEpisode;
    private bool isPlaying;
    private TimeSpan currentPosition = TimeSpan.Zero;
    private float playbackRate = 1.0f;

    public bool IsPlaying => isPlaying;
    public TimeSpan CurrentPosition => currentPosition;
    public float PlaybackRate => playbackRate;
    public Episode? CurrentEpisode => currentEpisode;

    public async Task PlayAsync(Episode episode)
    {
        currentEpisode = episode;
        isPlaying = true;
        // 实际平台特定的播放逻辑
        await Task.CompletedTask;
    }

    public async Task PauseAsync()
    {
        isPlaying = false;
        await Task.CompletedTask;
    }

    public async Task ResumeAsync()
    {
        isPlaying = true;
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        isPlaying = false;
        currentEpisode = null;
        currentPosition = TimeSpan.Zero;
        await Task.CompletedTask;
    }

    public async Task SeekAsync(TimeSpan position)
    {
        currentPosition = position;
        await Task.CompletedTask;
    }

    public async Task SetPlaybackRateAsync(float rate)
    {
        playbackRate = Math.Max(0.5f, Math.Min(rate, 2.0f));
        await Task.CompletedTask;
    }
}

/// <summary>
/// CacheService 引用
/// </summary>
public class CacheService
{
    private readonly Dictionary<string, (object value, DateTime expiration)> memoryCache;
    private readonly FileStorageService fileStorage;

    public CacheService()
    {
        memoryCache = new Dictionary<string, (object, DateTime)>();
        fileStorage = new FileStorageService();
    }

    public void SetMemoryCache(string key, object value, TimeSpan? expiration = null)
    {
        var exp = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : DateTime.MaxValue;
        memoryCache[key] = (value, exp);
    }

    public object? GetMemoryCache(string key)
    {
        if (memoryCache.TryGetValue(key, out var cached))
        {
            if (cached.expiration > DateTime.UtcNow)
            {
                return cached.value;
            }
            else
            {
                memoryCache.Remove(key);
                return null;
            }
        }
        return null;
    }

    public async Task<bool> SetFileCache(string key, string content, TimeSpan? expiration = null)
    {
        try
        {
            var cacheDir = await fileStorage.GetCacheDirectoryAsync();
            var fileName = GetHashFileName(key);
            var filePath = Path.Combine(cacheDir, fileName);

            await File.WriteAllTextAsync(filePath, content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetFileCache(string key)
    {
        try
        {
            var cacheDir = await fileStorage.GetCacheDirectoryAsync();
            var fileName = GetHashFileName(key);
            var filePath = Path.Combine(cacheDir, fileName);

            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public void ClearMemoryCache()
    {
        memoryCache.Clear();
    }

    private static string GetHashFileName(string key)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash);
    }
}
