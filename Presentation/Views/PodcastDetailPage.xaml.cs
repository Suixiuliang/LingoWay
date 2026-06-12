namespace LingoWay.Views;

using LingoWay.Domain.Models;
using System.Xml.Linq;

#nullable enable

[QueryProperty(nameof(PodcastTitle), "title")]
[QueryProperty(nameof(PodcastAuthor), "author")]
[QueryProperty(nameof(PodcastCover), "cover")]
[QueryProperty(nameof(PodcastRss), "rss")]
[QueryProperty(nameof(PodcastDesc), "desc")]
public partial class PodcastDetailPage : ContentPage
{
    private readonly System.Collections.ObjectModel.ObservableCollection<Episode> _episodes = [];

    public string PodcastTitle { get; set; } = "";
    public string PodcastAuthor { get; set; } = "";
    public string PodcastCover { get; set; } = "";
    public string PodcastRss { get; set; } = "";
    public string PodcastDesc { get; set; } = "";

    public PodcastDetailPage()
    {
        InitializeComponent();
        EpisodeList.ItemsSource = _episodes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        TitleLabel.Text = Uri.UnescapeDataString(PodcastTitle);
        AuthorLabel.Text = Uri.UnescapeDataString(PodcastAuthor);
        DescLabel.Text = Uri.UnescapeDataString(PodcastDesc);

        if (!string.IsNullOrEmpty(PodcastCover))
        {
            try
            {
                CoverImage.Source = Uri.UnescapeDataString(PodcastCover);
            }
            catch { }
        }

        await LoadEpisodesAsync();
    }

    // ==================== 加载 RSS 剧集 ====================

    private async Task LoadEpisodesAsync()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        EmptyText.Text = "正在加载剧集...";

        try
        {
            var rssUrl = Uri.UnescapeDataString(PodcastRss);
            if (string.IsNullOrEmpty(rssUrl))
            {
                EmptyText.Text = "该播客暂无 RSS 源";
                EpisodeCount.Text = "0 episodes";
                return;
            }

            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var xml = await http.GetStringAsync(rssUrl);

            var doc = XDocument.Parse(xml);
            var itunesNs = XNamespace.Get("http://www.itunes.com/dtds/podcast-1.0.dtd");

            _episodes.Clear();

            var items = doc.Descendants("item").ToList();
            foreach (var item in items)
            {
                var audioUrl = item.Element("enclosure")?.Attribute("url")?.Value ?? "";
                
                // 跳过无音频的条目
                if (string.IsNullOrEmpty(audioUrl)) continue;

                var title = item.Element("title")?.Value ?? "无标题";
                // 去除 CDATA 包装
                title = StripCdata(title);

                var episode = new Episode
                {
                    Title = title,
                    Description = StripCdata(item.Element("description")?.Value ?? ""),
                    AudioUrl = audioUrl,
                    PublishedDate = ParsePubDate(item.Element("pubDate")?.Value),
                    Duration = ParseDuration(item.Element(itunesNs + "duration")?.Value ?? ""),
                };

                _episodes.Add(episode);
            }

            EpisodeCount.Text = $"{_episodes.Count} episodes";
            EpisodeList.ItemsSource = null;
            EpisodeList.ItemsSource = _episodes;

            if (_episodes.Count == 0)
                EmptyText.Text = "该播客暂无剧集";
        }
        catch (Exception ex)
        {
            EmptyText.Text = $"加载失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[PodcastDetail] Load failed: {ex.Message}");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    // ==================== 操作 ====================

    private async void OnSubscribeTapped(object? sender, EventArgs e)
    {
        var rssUrl = Uri.UnescapeDataString(PodcastRss);
        if (string.IsNullOrEmpty(rssUrl))
        {
            await DisplayAlert("无法订阅", "该播客没有可用的 RSS 地址", "确定");
            return;
        }

        try
        {
            var contentProvider = Handler?.MauiContext?.Services
                .GetService<Domain.Interfaces.IContentProvider>();
            if (contentProvider != null)
            {
                await contentProvider.AddCustomPodcastAsync(rssUrl);
                SubscribeLabel.Text = "✅ 已订阅";
                await DisplayAlert("已订阅",
                    $"已将「{Uri.UnescapeDataString(PodcastTitle)}」添加到我的订阅", "确定");
            }
        }
        catch (InvalidOperationException)
        {
            SubscribeLabel.Text = "✅ 已订阅";
            await DisplayAlert("提示", "该播客已在订阅列表中", "确定");
        }
        catch (Exception ex)
        {
            await DisplayAlert("订阅失败", ex.Message, "确定");
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadEpisodesAsync();
        await DisplayAlert("刷新完成", $"共加载 {_episodes.Count} 个剧集", "确定");
    }

    // ==================== RSS 解析工具 ====================

    private static string StripCdata(string input)
    {
        if (input.StartsWith("<![CDATA[") && input.EndsWith("]]>"))
            return input[9..^3];
        return System.Net.WebUtility.HtmlDecode(input);
    }

    private static DateTime ParsePubDate(string? pubDate)
    {
        if (string.IsNullOrEmpty(pubDate)) return DateTime.MinValue;

        // 尝试多种 RFC 2822 格式
        var formats = new[]
        {
            "ddd, dd MMM yyyy HH:mm:ss zzz",
            "ddd, dd MMM yyyy HH:mm:ss Z",
            "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
            "ddd, dd MMM yyyy HH:mm:ss",
            "dd MMM yyyy HH:mm:ss zzz",
        };

        foreach (var fmt in formats)
        {
            if (DateTime.TryParseExact(pubDate, fmt,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
                return dt;
        }

        return DateTime.TryParse(pubDate, out var fallback) ? fallback : DateTime.MinValue;
    }

    private static TimeSpan ParseDuration(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return TimeSpan.Zero;

        // iTunes duration 格式: 纯秒数 或 HH:MM:SS 或 MM:SS
        if (int.TryParse(raw, out var seconds))
            return TimeSpan.FromSeconds(seconds);
        if (TimeSpan.TryParse(raw, out var ts))
            return ts;

        return TimeSpan.Zero;
    }
}
