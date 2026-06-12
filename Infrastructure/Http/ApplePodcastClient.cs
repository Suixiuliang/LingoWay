namespace LingoWay.Infrastructure.Http;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

#nullable enable

// ==================== Apple Podcast Search API 响应模型 ====================

/// <summary>
/// iTunes Search API 完整响应
/// </summary>
public class AppleSearchResponse
{
    [JsonPropertyName("resultCount")]
    public int ResultCount { get; set; }

    [JsonPropertyName("results")]
    public List<ApplePodcastResult> Results { get; set; } = [];
}

/// <summary>
/// 单个播客搜索结果
/// </summary>
public class ApplePodcastResult
{
    [JsonPropertyName("collectionId")]
    public long CollectionId { get; set; }

    [JsonPropertyName("collectionName")]
    public string CollectionName { get; set; } = "";

    [JsonPropertyName("artistName")]
    public string ArtistName { get; set; } = "";

    [JsonPropertyName("feedUrl")]
    public string FeedUrl { get; set; } = "";

    [JsonPropertyName("artworkUrl100")]
    public string ArtworkUrl100 { get; set; } = "";

    [JsonPropertyName("artworkUrl600")]
    public string ArtworkUrl600 { get; set; } = "";

    [JsonPropertyName("primaryGenreName")]
    public string PrimaryGenreName { get; set; } = "";

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = [];

    [JsonPropertyName("collectionViewUrl")]
    public string CollectionViewUrl { get; set; } = "";

    [JsonPropertyName("trackCount")]
    public int TrackCount { get; set; }
}

/// <summary>
/// Apple Podcast Search API 客户端
/// 
/// 支持:
/// - term 搜索: 按播客名称搜索
/// - lookup: 按 Apple collectionId 精确查找
/// - 自动获取高清封面 (600x600)
/// </summary>
public class ApplePodcastClient
{
    private readonly HttpClient _http;

    public ApplePodcastClient()
    {
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
            BaseAddress = new Uri("https://itunes.apple.com/")
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("LingoWay/1.0");
    }

    /// <summary>
    /// 按关键词搜索播客
    /// </summary>
    /// <param name="term">搜索词 (播客名称)</param>
    /// <param name="limit">返回数量 (默认5, 最大200)</param>
    /// <param name="country">国家代码 (默认US)</param>
    /// <returns>匹配的播客列表</returns>
    public async Task<List<ApplePodcastResult>> SearchAsync(
        string term, int limit = 5, string country = "US")
    {
        try
        {
            var encoded = Uri.EscapeDataString(term);
            var url = $"search?term={encoded}&media=podcast&entity=podcast&limit={limit}&country={country}";

            var response = await _http.GetFromJsonAsync<AppleSearchResponse>(url);
            return response?.Results ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ApplePodcast] Search failed for '{term}': {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// 按 Apple collectionId 精确查找单个播客
    /// </summary>
    public async Task<ApplePodcastResult?> LookupAsync(long collectionId, string country = "US")
    {
        try
        {
            var url = $"lookup?id={collectionId}&entity=podcast&country={country}";

            var response = await _http.GetFromJsonAsync<AppleSearchResponse>(url);
            return response?.Results?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ApplePodcast] Lookup failed for id={collectionId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 批量搜索播客：并发调用，返回去重结果。
    /// 每个名称取第一个匹配结果。
    /// </summary>
    /// <param name="names">播客名称列表</param>
    /// <returns>搜索结果字典 (名称 -> 结果)</returns>
    public async Task<Dictionary<string, ApplePodcastResult?>> SearchBatchAsync(
        IEnumerable<string> names)
    {
        var results = new Dictionary<string, ApplePodcastResult?>();

        var tasks = names.Select(async name =>
        {
            var list = await SearchAsync(name, limit: 1);
            return (Name: name, Result: list.FirstOrDefault());
        });

        var all = await Task.WhenAll(tasks);
        foreach (var (name, result) in all)
        {
            results[name] = result;
        }

        return results;
    }
}
