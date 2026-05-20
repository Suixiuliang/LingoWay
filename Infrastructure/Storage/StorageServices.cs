namespace LingoWay.Infrastructure.Storage;

/// <summary>
/// 文件存储服务
/// </summary>
public class FileStorageService
{
    private string AppDataDirectory => FileSystem.AppDataDirectory;

    public async Task<string> GetAppDataDirectoryAsync()
    {
        return await Task.FromResult(AppDataDirectory);
    }

    public async Task<string> GetCacheDirectoryAsync()
    {
        var cacheDir = Path.Combine(AppDataDirectory, "Cache");
        EnsureDirectoryExists(cacheDir);
        return await Task.FromResult(cacheDir);
    }

    public async Task<string> GetDownloadsDirectoryAsync()
    {
        var downloadsDir = Path.Combine(AppDataDirectory, "Downloads");
        EnsureDirectoryExists(downloadsDir);
        return await Task.FromResult(downloadsDir);
    }

    public async Task<string> GetEpisodesDirectoryAsync()
    {
        var episodesDir = Path.Combine(AppDataDirectory, "Episodes");
        EnsureDirectoryExists(episodesDir);
        return await Task.FromResult(episodesDir);
    }

    public async Task<string> GetSubtitlesDirectoryAsync()
    {
        var subtitlesDir = Path.Combine(AppDataDirectory, "Subtitles");
        EnsureDirectoryExists(subtitlesDir);
        return await Task.FromResult(subtitlesDir);
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        return await Task.FromResult(File.Exists(filePath));
    }

    public async Task DeleteFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        await Task.CompletedTask;
    }

    public async Task<long> GetFileSizeAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return 0;

        var info = new FileInfo(filePath);
        return await Task.FromResult(info.Length);
    }

    public async Task<bool> SaveFileAsync(string fileName, Stream stream)
    {
        try
        {
            var filePath = Path.Combine(AppDataDirectory, fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                EnsureDirectoryExists(directory);
            }

            using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Stream?> ReadFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var stream = File.OpenRead(filePath);
            return await Task.FromResult(stream);
        }
        catch
        {
            return null;
        }
    }

    public async Task<long> GetDirectorySizeAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return 0;

        var dir = new DirectoryInfo(directoryPath);
        long size = dir.EnumerateFiles().Sum(f => f.Length);
        foreach (var subdir in dir.EnumerateDirectories())
        {
            size += await GetDirectorySizeAsync(subdir.FullName);
        }
        return size;
    }

    public async Task ClearCacheAsync()
    {
        var cacheDir = await GetCacheDirectoryAsync();
        if (Directory.Exists(cacheDir))
        {
            Directory.Delete(cacheDir, true);
            EnsureDirectoryExists(cacheDir);
        }
    }

    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}

/// <summary>
/// 缓存服务
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
