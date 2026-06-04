namespace LingoWay.Infrastructure.Http;

using System.Net.Http;
using System.Xml.Linq;
using LingoWay.Domain.Models;

/// <summary>
/// RSS Feed 元数据
/// </summary>
public record RssFeedInfo
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Author { get; init; } = "";
    public string CoverUrl { get; init; } = "";
    public string Language { get; init; } = "en";
    public string Category { get; init; } = "";
}

/// <summary>
/// RSS解析器 — 完整支持 RSS 2.0 + iTunes 命名空间
/// </summary>
public class RssParser
{
    private static readonly XNamespace ITunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

    public async Task<(RssFeedInfo FeedInfo, List<Episode> Episodes)> ParseAsync(string rssUrl)
    {
        var feedInfo = new RssFeedInfo();
        var episodes = new List<Episode>();

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("LingoWay/1.0");
            var content = await client.GetStringAsync(rssUrl);

            var doc = XDocument.Parse(content);
            var channel = doc.Element("rss")?.Element("channel");

            if (channel == null)
                return (feedInfo, episodes);

            // --- 频道级元数据 ---
            feedInfo = new RssFeedInfo
            {
                Title = channel.Element("title")?.Value ?? "",
                Description = StripHtml(channel.Element("description")?.Value ?? ""),
                Author = channel.Element(ITunes + "author")?.Value
                      ?? channel.Element(ITunes + "owner")?.Element(ITunes + "name")?.Value
                      ?? "",
                Language = channel.Element("language")?.Value ?? "en",
                Category = channel.Element(ITunes + "category")?.Attribute("text")?.Value
                        ?? channel.Element("category")?.Value ?? "",
                CoverUrl = channel.Element(ITunes + "image")?.Attribute("href")?.Value
                        ?? channel.Element("image")?.Element("url")?.Value ?? ""
            };

            // --- 频道级封面回退到第一个剧集的封面 ---
            if (string.IsNullOrEmpty(feedInfo.CoverUrl))
            {
                var firstItemImage = channel.Elements("item").FirstOrDefault()
                    ?.Element(ITunes + "image")?.Attribute("href")?.Value;
                if (!string.IsNullOrEmpty(firstItemImage))
                    feedInfo = feedInfo with { CoverUrl = firstItemImage };
            }

            // --- 剧集列表 ---
            foreach (var item in channel.Elements("item"))
            {
                var episode = ParseEpisode(item);
                if (episode != null)
                    episodes.Add(episode);
            }

            return (feedInfo, episodes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RSS解析失败 [{rssUrl}]: {ex.Message}");
            return (feedInfo, episodes);
        }
    }

    private Episode? ParseEpisode(XElement item)
    {
        try
        {
            var audioUrl = "";
            var duration = TimeSpan.Zero;

            // 优先 enclosure，其次 media:content
            var enclosure = item.Element("enclosure");
            if (enclosure != null && enclosure.Attribute("type")?.Value?.StartsWith("audio/") == true)
            {
                audioUrl = enclosure.Attribute("url")?.Value ?? "";
            }

            // iTunes duration
            var itunesDuration = item.Element(ITunes + "duration")?.Value;
            if (!string.IsNullOrEmpty(itunesDuration))
            {
                if (int.TryParse(itunesDuration, out var secs))
                    duration = TimeSpan.FromSeconds(secs);
                else if (TimeSpan.TryParse(itunesDuration, out var ts))
                    duration = ts;
            }

            if (string.IsNullOrEmpty(audioUrl))
            {
                // 尝试从 media:content 或其他位置获取
                var mediaContent = item.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "content" && e.Attribute("type")?.Value?.StartsWith("audio/") == true);
                audioUrl = mediaContent?.Attribute("url")?.Value ?? "";
            }

            if (string.IsNullOrEmpty(audioUrl))
                return null; // 跳过没有音频的条目

            var pubDate = item.Element("pubDate")?.Value ?? "";
            var coverUrl = item.Element(ITunes + "image")?.Attribute("href")?.Value
                        ?? item.Element("image")?.Element("url")?.Value ?? "";

            return new Episode
            {
                Id = GenerateEpisodeId(item),
                Title = item.Element("title")?.Value ?? "Untitled",
                Description = StripHtml(item.Element("description")?.Value ?? ""),
                SourceUrl = item.Element("link")?.Value ?? "",
                PublishedDate = ParseDate(pubDate),
                Duration = duration,
                CoverUrl = coverUrl,
                AudioUrl = audioUrl
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"解析剧集失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 基于 enclosure URL 或 guid 生成稳定的 EpisodeId
    /// </summary>
    private static string GenerateEpisodeId(XElement item)
    {
        var enclosure = item.Element("enclosure")?.Attribute("url")?.Value;
        var guid = item.Element("guid")?.Value;
        var link = item.Element("link")?.Value;
        var raw = enclosure ?? guid ?? link ?? item.ToString();

        // 简单 hash 作为 ID
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hash)[..24];
    }

    private static DateTime ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return DateTime.UtcNow;
        if (DateTime.TryParse(dateStr, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal, out var date))
            return date;
        return DateTime.UtcNow;
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "").Trim();
    }
}

/// <summary>
/// 内容HTTP客户端
/// </summary>
public class ContentClient
{
    private readonly HttpClient httpClient;
    private readonly RssParser rssParser;

    public ContentClient()
    {
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        rssParser = new RssParser();
    }

    /// <summary>
    /// 从RSS源获取剧集
    /// </summary>
    public async Task<List<Episode>> GetEpisodesFromRssAsync(string rssUrl)
    {
        var (_, episodes) = await rssParser.ParseAsync(rssUrl);
        return episodes;
    }

    /// <summary>
    /// 从RSS源获取频道信息+剧集
    /// </summary>
    public async Task<(RssFeedInfo FeedInfo, List<Episode> Episodes)> GetFeedWithEpisodesAsync(string rssUrl)
    {
        return await rssParser.ParseAsync(rssUrl);
    }

    /// <summary>
    /// 下载音频文件
    /// </summary>
    public async Task<Stream> DownloadAudioAsync(string audioUrl)
    {
        var response = await httpClient.GetAsync(audioUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    /// <summary>
    /// 获取字幕内容
    /// </summary>
    public async Task<string> GetSubtitleContentAsync(string subtitleUrl)
    {
        return await httpClient.GetStringAsync(subtitleUrl);
    }
}

/// <summary>
/// 翻译API客户端
/// </summary>
public class TranslationClient
{
    private readonly HttpClient httpClient;

    public TranslationClient()
    {
        httpClient = new HttpClient 
        { 
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    /// <summary>
    /// 使用腾讯翻译API (示例实现)
    /// 实际使用时需要配置API密钥
    /// </summary>
    public async Task<string> TranslateWithTencentAsync(
        string text, 
        string sourceLanguage, 
        string targetLanguage,
        string secretId = "",
        string secretKey = "")
    {
        // 实现腾讯翻译API调用
        // 这里只是框架，实际需要实现腾讯云SDK集成

        return await Task.FromResult(text); // 占位符
    }

    /// <summary>
    /// 使用Azure翻译API (示例实现)
    /// </summary>
    public async Task<string> TranslateWithAzureAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        string apiKey = "",
        string region = "")
    {
        // 实现Azure Translator API调用
        // 这里只是框架，实际需要实现Azure SDK集成

        return await Task.FromResult(text); // 占位符
    }

    /// <summary>
    /// 批量翻译
    /// </summary>
    public async Task<List<string>> TranslateBatchAsync(
        List<string> texts,
        string sourceLanguage,
        string targetLanguage)
    {
        var results = new List<string>();
        foreach (var text in texts)
        {
            // 调用翻译API
            results.Add(text); // 占位符
        }
        return await Task.FromResult(results);
    }
}
