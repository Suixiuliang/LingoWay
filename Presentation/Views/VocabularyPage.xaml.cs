using LingoWay.Presentation.ViewModels;
using LingoWay.Domain.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using LingoWay.Application.Services;

namespace LingoWay.Views;

public class VocabDisplayItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Word { get; set; } = "";
    public int MasteryLevel { get; set; }
    public DateTime AddedDate { get; set; }
    public int ReviewCount { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); }
    }

    public string MasteryText => MasteryLevel switch
    {
        0 => "新词",
        1 => "认识",
        2 => "熟悉",
        3 => "掌握中",
        4 => "接近掌握",
        >= 5 => "已掌握",
        _ => "新词"
    };

    public Color MasteryColor => MasteryLevel switch
    {
        0 => Color.FromArgb("#EF4444"),
        1 => Color.FromArgb("#F97316"),
        2 => Color.FromArgb("#EAB308"),
        3 => Color.FromArgb("#84CC16"),
        4 => Color.FromArgb("#22C55E"),
        >= 5 => Color.FromArgb("#10B981"),
        _ => Color.FromArgb("#EF4444")
    };

    public string DisplayInfo => $"添加于 {AddedDate:MM-dd}  |  复习 {ReviewCount} 次";
}

public partial class VocabularyPage : ContentPage
{
    private enum Tab { New, Learning, Mastered }

    private readonly VocabularyViewModel _viewModel;
    private readonly WordTranslationService _translationService;
    private readonly ObservableCollection<VocabDisplayItem> _items = new();
    private List<VocabDisplayItem> _allItems = [];
    private Tab _currentTab = Tab.New;
    private bool _batchMode;

    // 词典弹窗通过模态页面 DictPopupPage 呈现

    public VocabularyPage(VocabularyViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _translationService = new WordTranslationService();
        VocabCollectionView.ItemsSource = _items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadWordsAsync();
    }

    private async Task LoadWordsAsync()
    {
        try
        {
            await _viewModel.LoadVocabularyAsync();

            _allItems = _viewModel.VocabularyList
                .Select(v => new VocabDisplayItem
                {
                    Word = v.Word,
                    MasteryLevel = v.UserVocabularies?.FirstOrDefault()?.MasteryLevel ?? 0,
                    AddedDate = v.UserVocabularies?.FirstOrDefault()?.AddedDate ?? v.CreatedDate,
                    ReviewCount = v.UserVocabularies?.FirstOrDefault()?.ReviewCount ?? 0
                })
                .OrderByDescending(x => x.AddedDate)
                .ToList();

            if (_batchMode) ExitBatchMode();
            ApplyTabFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vocabulary: {ex.Message}");
        }
    }

    private void ApplyTabFilter()
    {
        var filtered = _currentTab switch
        {
            Tab.New => _allItems.Where(x => x.MasteryLevel == 0),
            Tab.Learning => _allItems.Where(x => x.MasteryLevel >= 1 && x.MasteryLevel <= 4),
            Tab.Mastered => _allItems.Where(x => x.MasteryLevel >= 5),
            _ => _allItems.AsEnumerable()
        };
        var list = ApplySearchFilter(filtered.ToList());
        _items.Clear();
        foreach (var item in list) _items.Add(item);
    }

    private List<VocabDisplayItem> ApplySearchFilter(List<VocabDisplayItem> source)
    {
        var query = SearchBar.Text?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(query)) return source;
        return source.Where(x => x.Word.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // --- Tab 切换 ---
    private void SetActiveTab(Border active, Border inactive1, Border inactive2)
    {
        active.BackgroundColor = Color.FromArgb("#005FB8"); active.Stroke = Colors.Transparent;
        SetTabLabelColor(active, Colors.White);
        inactive1.BackgroundColor = Color.FromArgb("#2D2D2D"); inactive1.Stroke = Color.FromArgb("#3D3D3D");
        SetTabLabelColor(inactive1, Color.FromArgb("#9CA3AF"));
        inactive2.BackgroundColor = Color.FromArgb("#2D2D2D"); inactive2.Stroke = Color.FromArgb("#3D3D3D");
        SetTabLabelColor(inactive2, Color.FromArgb("#9CA3AF"));
    }

    private static void SetTabLabelColor(Border tab, Color color)
    {
        if (tab.Content is HorizontalStackLayout hsl && hsl.Children.Count >= 2 && hsl.Children[1] is Label label)
            label.TextColor = color;
    }

    private void OnTabNewTapped(object? sender, EventArgs e)
    {
        _currentTab = Tab.New;
        SetActiveTab(TabNew, TabLearning, TabMastered);
        EmptyLabel.Text = "还没有新词";
        if (_batchMode) ExitBatchMode();
        ApplyTabFilter();
    }

    private void OnTabLearningTapped(object? sender, EventArgs e)
    {
        _currentTab = Tab.Learning;
        SetActiveTab(TabLearning, TabNew, TabMastered);
        EmptyLabel.Text = "学习中的单词会出现在这里";
        if (_batchMode) ExitBatchMode();
        ApplyTabFilter();
    }

    private void OnTabMasteredTapped(object? sender, EventArgs e)
    {
        _currentTab = Tab.Mastered;
        SetActiveTab(TabMastered, TabNew, TabLearning);
        EmptyLabel.Text = "还没有已掌握的单词";
        if (_batchMode) ExitBatchMode();
        ApplyTabFilter();
    }

    private void OnSearch(object? sender, EventArgs e) => ApplyTabFilter();
    private void OnClearSearch(object? sender, EventArgs e) { SearchBar.Text = ""; ApplyTabFilter(); }

    // --- 批量管理 ---
    private void OnToggleBatchMode(object? sender, EventArgs e)
    {
        _batchMode = !_batchMode;
        BatchToggleBtn.Text = _batchMode ? "退出管理" : "批量管理";
        BatchToggleBtn.BackgroundColor = _batchMode ? Color.FromArgb("#C42B1C") : Color.FromArgb("#005FB8");
        BatchBar.IsVisible = _batchMode;
        UpdateBatchCount();

        // 切换 CheckBox 可见性
        foreach (var item in _items) item.IsSelected = false;
    }

    private void ExitBatchMode()
    {
        _batchMode = false;
        BatchToggleBtn.Text = "批量管理";
        BatchToggleBtn.BackgroundColor = Color.FromArgb("#005FB8");
        BatchBar.IsVisible = false;
        foreach (var item in _items) item.IsSelected = false;
    }

    private void OnSelectAll(object? sender, EventArgs e)
    {
        var allDeselected = _items.Count > 0 && _items.All(i => i.IsSelected);
        foreach (var item in _items) item.IsSelected = !allDeselected;
        UpdateBatchCount();
    }

    private void UpdateBatchCount() =>
        BatchCountLabel.Text = $"已选 {_items.Count(i => i.IsSelected)} 项";

    private async void OnBatchMaster(object? sender, EventArgs e)
    {
        var selected = _items.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0) { await DisplayAlert("提示", "请先选择单词", "确定"); return; }
        var ok = await DisplayAlert("批量标记", $"将 {selected.Count} 个单词标记为已掌握？", "确定", "取消");
        if (!ok) return;
        foreach (var item in selected)
            await _viewModel.UpdateMasteryLevelAsync(item.Word, 5);
        await LoadWordsAsync();
    }

    private async void OnBatchDelete(object? sender, EventArgs e)
    {
        var selected = _items.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0) { await DisplayAlert("提示", "请先选择单词", "确定"); return; }
        var ok = await DisplayAlert("批量删除", $"确定删除 {selected.Count} 个单词？", "删除", "取消");
        if (!ok) return;
        foreach (var item in selected)
        {
            var v = _viewModel.VocabularyList.FirstOrDefault(x => x.Word == item.Word);
            if (v != null) await _viewModel.SelectVocabularyCommand.ExecuteAsync(v);
        }
        await LoadWordsAsync();
    }

    // --- 单词操作 ---
    private async void OnWordTapped(object? sender, EventArgs e)
    {
        if (sender is not Border border || border.BindingContext is not VocabDisplayItem selected)
            return;

        if (_batchMode)
        {
            selected.IsSelected = !selected.IsSelected;
            UpdateBatchCount();
            return;
        }

        // 普通模式弹窗
        var action = await DisplayActionSheet(
            $"{selected.Word} ({selected.MasteryText})",
            "取消",
            "删除单词",
            "查询", "复习", "已掌握");

        switch (action)
        {
            case "已掌握":
                await _viewModel.UpdateMasteryLevelAsync(selected.Word, 5);
                await LoadWordsAsync();
                break;
            case "删除单词":
                var w = _viewModel.VocabularyList.FirstOrDefault(v => v.Word == selected.Word);
                if (w != null) await _viewModel.SelectVocabularyCommand.ExecuteAsync(w);
                await LoadWordsAsync();
                break;
            case "复习":
                var nl = Math.Min(selected.MasteryLevel + 1, 4);
                await _viewModel.UpdateMasteryLevelAsync(selected.Word, nl);
                await LoadWordsAsync();
                break;
            case "查询":
                ShowDetailPopup(selected.Word);
                break;
        }
    }

    // ──── 查询弹窗 ────

    private async void ShowDetailPopup(string word)
    {
        var detail = _translationService.LookupDetail(word);
        await DictPopupPage.ShowAsync(this, word, detail);
    }
}
