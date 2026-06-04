namespace LingoWay.Views;

using LingoWay.Presentation.ViewModels;
using LingoWay.Domain.Models;
using LingoWay.Domain.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;

public class PodcastDisplayItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public Podcast Podcast { get; init; } = null!;
    public string Title => Podcast.Title;
    public string Author => Podcast.Author;
    public string CoverUrl => Podcast.CoverUrl;
    public string RssUrl => Podcast.RssUrl;

    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set { _hasError = value; PropertyChanged?.Invoke(this, new(nameof(HasError))); }
    }
}

public partial class BrowsePage : ContentPage
{
    private readonly BrowseViewModel _viewModel;
    private readonly IContentProvider _contentProvider;
    private readonly ObservableCollection<PodcastDisplayItem> _mySubsItems = new();
    private enum PageTab { Recommend, MySubs }
    private PageTab _currentTab = PageTab.Recommend;

    public BrowsePage(BrowseViewModel viewModel, IContentProvider contentProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _contentProvider = contentProvider;
        MySubsList.ItemsSource = _mySubsItems;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        TrySeedDefaults();
        await RefreshRecommendAsync();
        await RefreshMySubsAsync();
    }

    // --- Tab 切换 ---
    private void OnTabRecommendTapped(object? sender, EventArgs e)
    {
        _currentTab = PageTab.Recommend;
        TabRecommend.BackgroundColor = Color.FromArgb("#005FB8"); TabRecommend.Stroke = Colors.Transparent;
        (TabRecommend.Content as Label)!.TextColor = Colors.White;
        TabMySubs.BackgroundColor = Color.FromArgb("#2D2D2D"); TabMySubs.Stroke = Color.FromArgb("#3D3D3D");
        (TabMySubs.Content as Label)!.TextColor = Color.FromArgb("#9CA3AF");
        RecommendPanel.IsVisible = true; MySubsPanel.IsVisible = false;
    }

    private void OnTabMySubsTapped(object? sender, EventArgs e)
    {
        _currentTab = PageTab.MySubs;
        TabMySubs.BackgroundColor = Color.FromArgb("#005FB8"); TabMySubs.Stroke = Colors.Transparent;
        (TabMySubs.Content as Label)!.TextColor = Colors.White;
        TabRecommend.BackgroundColor = Color.FromArgb("#2D2D2D"); TabRecommend.Stroke = Color.FromArgb("#3D3D3D");
        (TabRecommend.Content as Label)!.TextColor = Color.FromArgb("#9CA3AF");
        RecommendPanel.IsVisible = false; MySubsPanel.IsVisible = true;
    }

    // --- 数据加载 ---
    private async void TrySeedDefaults()
    {
        try { await _contentProvider.SeedDefaultPodcastsAsync(); }
        catch { }
    }

    private async Task RefreshRecommendAsync() =>
        await _viewModel.LoadPodcastsCommand.ExecuteAsync(null);

    private async Task RefreshMySubsAsync()
    {
        try
        {
            var podcasts = await _contentProvider.GetPodcastsAsync();
            var items = new List<PodcastDisplayItem>();
            foreach (var p in podcasts)
            {
                var item = new PodcastDisplayItem { Podcast = p };
                if (!string.IsNullOrEmpty(p.RssUrl))
                {
                    try
                    {
                        _ = await new HttpClient { Timeout = TimeSpan.FromSeconds(8) }
                            .GetStringAsync(p.RssUrl);
                        item.HasError = false;
                    }
                    catch { item.HasError = true; }
                }
                items.Add(item);
            }
            _mySubsItems.Clear();
            foreach (var it in items) _mySubsItems.Add(it);
        }
        catch { }
    }

    // --- 搜索 ---
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) =>
        _ = _viewModel.SearchCommand.ExecuteAsync(e.NewTextValue);

    private void OnSearchButtonPressed(object? sender, EventArgs e) =>
        _ = _viewModel.SearchCommand.ExecuteAsync(SearchBar.Text);

    // --- 添加 RSS ---
    private async void OnAddRssClicked(object? sender, EventArgs e)
    {
        var url = await DisplayPromptAsync("添加 RSS 源", "请输入播客 RSS 订阅地址：",
            "添加", "取消", placeholder: "https://feeds.example.com/podcast.xml",
            maxLength: 2048, keyboard: Keyboard.Url);
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            AddRssButton.IsEnabled = false; AddRssButton.Text = "⏳ 正在订阅...";
            await _contentProvider.AddCustomPodcastAsync(url);
            await RefreshMySubsAsync();
        }
        catch (Exception ex) { await DisplayAlert("订阅失败", ex.Message, "确定"); }
        finally { AddRssButton.IsEnabled = true; AddRssButton.Text = "+ 添加RSS源"; }
    }

    // --- 推荐列表选中 = 一键订阅 ---
    private async void OnPodcastSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Podcast podcast) return;
        RecommendList.SelectedItem = null;
        await _contentProvider.AddCustomPodcastAsync(podcast.RssUrl);
        await DisplayAlert("已订阅", $"已订阅「{podcast.Title}」", "确定");
        await RefreshMySubsAsync();
    }

    // --- 我的订阅：点击进入 ---
    private async void OnSubscribedPodcastSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PodcastDisplayItem item) return;
        MySubsList.SelectedItem = null;

        if (item.HasError)
        {
            var fix = await DisplayAlert("订阅失效",
                $"「{item.Title}」RSS 源无法连接。\n是否取消订阅？", "取消订阅", "暂不处理");
            if (fix) await UnsubscribeAsync(item);
            return;
        }

        var episodes = await _contentProvider.GetEpisodesAsync(item.Podcast.Id);
        if (episodes.Count == 0)
        {
            await DisplayAlert("提示", "该播客暂无剧集", "确定");
            return;
        }
        await Shell.Current.GoToAsync("//PlayerPage");
    }

    // --- 单行右侧 ✕ 取消订阅 ---
    private async void OnUnsubItemTapped(object? sender, TappedEventArgs e)
    {
        var item = (sender as BindableObject)?.BindingContext as PodcastDisplayItem;
        if (item == null) return;

        var confirm = await DisplayAlert("取消订阅", $"确定取消订阅「{item.Title}」？", "取消订阅", "保留");
        if (confirm) await UnsubscribeAsync(item);
    }

    // --- 核心取消订阅逻辑 ---
    private async Task UnsubscribeAsync(PodcastDisplayItem item)
    {
        var deleted = false;
        try
        {
            await _contentProvider.DeletePodcastAsync(item.Podcast.Id);
            deleted = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeletePodcast failed: {ex.Message}");
        }

        // 从列表中移除该项（无论 DB 删除成功与否，UI 上先移除）
        _mySubsItems.Remove(item);

        if (!deleted)
        {
            await DisplayAlert("提示", "订阅已从列表移除，但数据库清理可能未完成", "确定");
        }

        // 刷新推荐列表
        try { await _viewModel.LoadPodcastsCommand.ExecuteAsync(null); } catch { }
    }
}
