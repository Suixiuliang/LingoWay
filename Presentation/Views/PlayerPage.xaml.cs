using LingoWay.Presentation.ViewModels;
using LingoWay.Domain.Models;
using LingoWay.Application.Services;
using System.Collections.ObjectModel;
using System.Threading;
using SkiaSharp;
using TagLib;

namespace LingoWay.Views;

public class LyricDisplayItem : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private LrcLine _line = null!;
    public LrcLine Line
    {
        get => _line;
        set { _line = value; OnPropertyChanged(); }
    }
    public string TimeText => $"{(int)Line.StartTime.TotalMinutes:D2}:{Line.StartTime.Seconds:D2}";
    public string EnglishText => Line.EnglishText;
    public string ChinesePlaceholder => "_中文翻译_";

    private string _markedWordsText = "";
    public string MarkedWordsText
    {
        get => _markedWordsText;
        set { _markedWordsText = value; OnPropertyChanged(); }
    }

    private Color _textColor = Color.FromArgb("#D1D5DB");
    public Color TextColor
    {
        get => _textColor;
        set { _textColor = value; OnPropertyChanged(); }
    }

    private string _fontAttr = "None";
    public string FontAttr
    {
        get => _fontAttr;
        set { _fontAttr = value; OnPropertyChanged(); }
    }

    private Color _backgroundColor = Color.FromArgb("#15171C");
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; OnPropertyChanged(); }
    }

    private float _backgroundOpacity = 0f;
    public float BackgroundOpacity
    {
        get => _backgroundOpacity;
        set { _backgroundOpacity = value; OnPropertyChanged(); }
    }

    private Color _markedWordsColor = Color.FromArgb("#FACC15");
    public Color MarkedWordsColor
    {
        get => _markedWordsColor;
        set { _markedWordsColor = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}

public class MarkedWordItem : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private string _word = "";
    public string Word
    {
        get => _word;
        set { _word = value; OnPropertyChanged(); }
    }
    public string Translation { get; set; } = "_单词中译_";
    public string Source { get; set; } = "";
    public string DisplayText => $"{Word}  {Translation}";

    private Color _color = Color.FromArgb("#FACC15");
    public Color Color
    {
        get => _color;
        set { _color = value; OnPropertyChanged(); }
    }
    public string LevelLabel { get; set; } = "";

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}

public partial class PlayerPage : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    private readonly IAudioPlaybackService _audioService;
    private readonly ObservableCollection<LyricDisplayItem> _lyricItems = new();
    private readonly ObservableCollection<MarkedWordItem> _markedWords = new();
    private readonly ObservableCollection<MarkedWordItem> _masteredWords = new();
    private HashSet<string> _markedWordSet = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _masteredWordSet = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, int> _wordMasteryLevels = new(StringComparer.OrdinalIgnoreCase);
    private bool _audioLoaded;

    /// <summary>屏蔽词还原映射：归一化词 → 显示原形（fuck → f**k）</summary>
    private readonly Dictionary<string, string> _censoredForm = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 归一化词库词典：f**k → fuck，合并重复项取最高掌握度
    /// </summary>
    private static Dictionary<string, int> NormalizeVocabLevels(IReadOnlyDictionary<string, int> levels)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in levels)
        {
            var nk = LrcParserService.NormalizeCensoredWord(kv.Key);
            if (result.TryGetValue(nk, out var existing))
                result[nk] = Math.Max(existing, kv.Value);
            else
                result[nk] = kv.Value;
        }
        return result;
    }

    /// <summary>
    /// 英语缩略词映射 — 用于歌词单词高亮解析
    /// </summary>
    private static readonly Dictionary<string, string[]> Contractions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["you're"] = ["you", "are"],
        ["we're"] = ["we", "are"],
        ["they're"] = ["they", "are"],
        ["i'm"] = ["i", "am"],
        ["he's"] = ["he", "is"],
        ["she's"] = ["she", "is"],
        ["it's"] = ["it", "is"],
        ["that's"] = ["that", "is"],
        ["there's"] = ["there", "is"],
        ["we'll"] = ["we", "will"],
        ["i'll"] = ["i", "will"],
        ["you'll"] = ["you", "will"],
        ["he'll"] = ["he", "will"],
        ["she'll"] = ["she", "will"],
        ["they'll"] = ["they", "will"],
        ["it'll"] = ["it", "will"],
        ["don't"] = ["do", "not"],
        ["doesn't"] = ["does", "not"],
        ["didn't"] = ["did", "not"],
        ["won't"] = ["will", "not"],
        ["can't"] = ["can", "not"],
        ["couldn't"] = ["could", "not"],
        ["shouldn't"] = ["should", "not"],
        ["wouldn't"] = ["would", "not"],
        ["isn't"] = ["is", "not"],
        ["aren't"] = ["are", "not"],
        ["wasn't"] = ["was", "not"],
        ["weren't"] = ["were", "not"],
        ["hasn't"] = ["has", "not"],
        ["haven't"] = ["have", "not"],
        ["i've"] = ["i", "have"],
        ["you've"] = ["you", "have"],
        ["we've"] = ["we", "have"],
        ["they've"] = ["they", "have"],
        ["i'd"] = ["i", "would"],
        ["you'd"] = ["you", "would"],
        ["he'd"] = ["he", "would"],
        ["she'd"] = ["she", "would"],
        ["we'd"] = ["we", "would"],
        ["they'd"] = ["they", "would"],
        ["ain't"] = ["am", "not"],  // also "are not" / "is not"
    };

    /// <summary>
    /// 取词及其缩略展开的所有成分中最低的掌握度
    /// </summary>
    private int GetEffectiveMastery(string word)
    {
        // 屏蔽词还原：f**k → fuck
        word = LrcParserService.NormalizeCensoredWord(word);
        
        var best = -1;  // -1 = 完全未标记
        void Check(string w)
        {
            if (_wordMasteryLevels.TryGetValue(w, out var lv) && (best < 0 || lv < best))
                best = lv;
            else if (_markedWordSet.Contains(w) && best < 0)
                best = 0;
        }
        Check(word);
        if (Contractions.TryGetValue(word, out var parts))
            foreach (var p in parts) Check(p);
        return best;
    }
    private static Color GetWordColorByMastery(int level)
    {
        return level switch
        {
            >= 5 => Color.FromArgb("#22C55E"),
            0 => Color.FromArgb("#EF4444"),
            _ => Color.FromArgb("#F97316"),
        };
    }
    private bool _isUpdatingProgressFromPlayback;
    private bool _autoScrollEnabled = true;
    private bool _isDraggingSlider;
    private string? _currentLrcFilePath;
    private bool _isAndroidLayoutApplied;
    private readonly Dictionary<string, CancellationTokenSource> _hoverAnimTokens = new();

    public PlayerPage(PlayerViewModel viewModel, IAudioPlaybackService audioService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _audioService = audioService;
        BindingContext = viewModel;
        ApplyPlatformLayout();
#if WINDOWS
        SuppressSliderAnimations();
#endif
    }

#if WINDOWS
    private void SuppressSliderAnimations()
    {
        void FixSlider(Slider slider)
        {
            slider.HandlerChanged += (_, _) =>
            {
                if (slider.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Slider nativeSlider)
                {
                    var empty = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection();
                    nativeSlider.Transitions = empty;
                }
            };
        }
        FixSlider(ProgressSlider);
        FixSlider(CustomSpeedSlider);
    }
#endif

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            BindableLayout.SetItemsSource(LyricsStackLayout, _lyricItems);
            MarkedWordsCollectionView.ItemsSource = _markedWords;
            MasteredWordsCollectionView.ItemsSource = _masteredWords;

            _viewModel.LrcLinesUpdated -= ViewModel_LrcLinesUpdated;
            _viewModel.CurrentLineChanged -= ViewModel_CurrentLineChanged;
            _viewModel.PlaybackPositionChanged -= ViewModel_PlaybackPositionChanged;
            _viewModel.PlaybackStateChanged -= ViewModel_PlaybackStateChanged;

            _viewModel.LrcLinesUpdated += ViewModel_LrcLinesUpdated;
            _viewModel.CurrentLineChanged += ViewModel_CurrentLineChanged;
            _viewModel.PlaybackPositionChanged += ViewModel_PlaybackPositionChanged;
            _viewModel.PlaybackStateChanged += ViewModel_PlaybackStateChanged;

            TitleLabel.Text = string.IsNullOrWhiteSpace(_viewModel.CurrentEpisode?.Title)
                ? "未加载音频"
                : _viewModel.CurrentEpisode.Title;

            CoverOverlay.IsVisible = _viewModel.CurrentEpisode != null;

            TimeLabel.Text = $"{FormatTime(_viewModel.CurrentPosition)} / {FormatTime(_viewModel.TotalDuration)}";

            _markedWordSet = await _viewModel.GetMarkedWordsAsync();
            var vocabLevels = await _viewModel.GetUserVocabularyWithLevelsAsync();
            // 归一化屏蔽词 → 合并重复项（取最高掌握度）
            _wordMasteryLevels = NormalizeVocabLevels(vocabLevels);
            _censoredForm.Clear();
            foreach (var kv in vocabLevels)
            {
                var nk = LrcParserService.NormalizeCensoredWord(kv.Key);
                if (nk != kv.Key) _censoredForm[nk] = kv.Key;
            }
            _markedWordSet = _wordMasteryLevels
                .Where(kv => kv.Value < 5)
                .Select(kv => kv.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            _masteredWordSet = _wordMasteryLevels
                .Where(kv => kv.Value >= 5)
                .Select(kv => kv.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            RefreshMarkedWordsDisplay();
            RefreshMasteredWordsDisplay();
            RebuildLyricsList();
            UpdateSubtitleDisplay(_viewModel.CurrentLrcLine);
            UpdateFavoriteButton();
            UpdatePlaybackRateButtons(_viewModel.PlaybackRate);
            UpdatePlayPauseIcon();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing PlayerPage: {ex.Message}");
        }
    }

    private void ApplyPlatformLayout()
    {
        if (_isAndroidLayoutApplied)
            return;

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // Top section: cover + subtitle/controls (vertical)
            TopSectionGrid.ColumnDefinitions.Clear();
            TopSectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            TopSectionGrid.RowDefinitions.Clear();
            TopSectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            TopSectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(CoverPanel, 0);
            Grid.SetRow(CoverPanel, 0);
            Grid.SetColumn(SubtitleAndControlsPanel, 0);
            Grid.SetRow(SubtitleAndControlsPanel, 1);

            CoverPanel.MaximumWidthRequest = 9999;

            // SubtitleAndControlsPanel: swap order - playback controls on top, subtitle display below
            var subtitleIndex = SubtitleAndControlsPanel.Children.IndexOf(SubtitleDisplayBorder);
            var controlsIndex = SubtitleAndControlsPanel.Children.IndexOf(PlaybackControlsBorder);
            if (subtitleIndex >= 0 && controlsIndex >= 0 && subtitleIndex < controlsIndex)
            {
                SubtitleAndControlsPanel.Children.RemoveAt(controlsIndex);
                SubtitleAndControlsPanel.Children.RemoveAt(subtitleIndex);
                SubtitleAndControlsPanel.Children.Insert(0, PlaybackControlsBorder);
                SubtitleAndControlsPanel.Children.Insert(1, SubtitleDisplayBorder);
            }

            // SubtitleDisplayBorder: change SubtitleInnerGrid from left-right to top-bottom
            SubtitleInnerGrid.ColumnDefinitions.Clear();
            SubtitleInnerGrid.RowDefinitions.Clear();
            SubtitleInnerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            SubtitleInnerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(SubtitleWordsColumn, 0);
            Grid.SetRow(SubtitleWordsColumn, 0);
            Grid.SetColumn(TranslationColumn, 0);
            Grid.SetRow(TranslationColumn, 1);

            // TranslationColumn: change translation items to horizontal layout
            var oldTranslationColumn = TranslationColumn;
            var parent = oldTranslationColumn.Parent as Grid;
            if (parent != null)
            {
                var newTranslationRow = new HorizontalStackLayout
                {
                    Spacing = 16,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Center
                };

                // 句子翻译
                var sentenceStack = new VerticalStackLayout { Spacing = 4 };
                sentenceStack.Children.Add(new Label { Text = "句子翻译", TextColor = Color.FromArgb("#6B7280"), FontSize = 11, FontAttributes = FontAttributes.Bold });
                sentenceStack.Children.Add(ChineseTextLabel);
                newTranslationRow.Children.Add(sentenceStack);

                // 单词翻译
                var wordStack = new VerticalStackLayout { Spacing = 4 };
                wordStack.Children.Add(new Label { Text = "单词翻译", TextColor = Color.FromArgb("#6B7280"), FontSize = 11, FontAttributes = FontAttributes.Bold });
                wordStack.Children.Add(WordTranslationLabel);
                newTranslationRow.Children.Add(wordStack);

                // 标记生词
                var markedStack = new VerticalStackLayout { Spacing = 4 };
                markedStack.Children.Add(new Label { Text = "标记生词", TextColor = Color.FromArgb("#6B7280"), FontSize = 11, FontAttributes = FontAttributes.Bold });
                markedStack.Children.Add(MarkedVocabLabel);
                newTranslationRow.Children.Add(markedStack);

                parent.Children.Remove(oldTranslationColumn);
                newTranslationRow.BindingContext = oldTranslationColumn.BindingContext;
                parent.Children.Add(newTranslationRow);
                Grid.SetColumn(newTranslationRow, 0);
                Grid.SetRow(newTranslationRow, 1);
            }

            // Bottom section: subtitles + vocabulary (vertical)
            BottomSectionGrid.ColumnDefinitions.Clear();
            BottomSectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            BottomSectionGrid.RowDefinitions.Clear();
            BottomSectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            BottomSectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(SubtitlesPanel, 0);
            Grid.SetRow(SubtitlesPanel, 0);
            Grid.SetColumn(VocabularyPanel, 0);
            Grid.SetRow(VocabularyPanel, 1);

            _isAndroidLayoutApplied = true;
        }
    }

    private async void OnImportAudioClicked(object? sender, EventArgs e)
    {
        try
        {
            var audioResult = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "选择音频文件 (MP3/WAV/M4A/FLAC)"
            });
            if (audioResult == null) return;

            var audioPath = audioResult.FullPath;

            await _viewModel.LoadAudioAndSubtitleAsync(audioPath, null);
            TitleLabel.Text = System.IO.Path.GetFileNameWithoutExtension(audioPath);
            CoverOverlay.IsVisible = true;
            LoadAudioCover(audioPath);
            BlurOverlay.IsVisible = true;
            _audioLoaded = true;

            _markedWordSet = await _viewModel.GetMarkedWordsAsync();
            var vocabLevels = await _viewModel.GetUserVocabularyWithLevelsAsync();
            // 归一化屏蔽词 → 合并重复项（取最高掌握度）
            _wordMasteryLevels = NormalizeVocabLevels(vocabLevels);
            _censoredForm.Clear();
            foreach (var kv in vocabLevels)
            {
                var nk = LrcParserService.NormalizeCensoredWord(kv.Key);
                if (nk != kv.Key) _censoredForm[nk] = kv.Key;
            }
            _markedWordSet = _wordMasteryLevels
                .Where(kv => kv.Value < 5)
                .Select(kv => kv.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            _masteredWordSet = _wordMasteryLevels
                .Where(kv => kv.Value >= 5)
                .Select(kv => kv.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            RefreshMarkedWordsDisplay();
            RefreshMasteredWordsDisplay();
            RebuildLyricsList();
            UpdateSubtitleDisplay(_viewModel.CurrentLrcLine);
            UpdateFavoriteButton();
            UpdatePlaybackRateButtons(_viewModel.PlaybackRate);
            UpdatePlayPauseIcon();
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"导入音频失败: {ex.Message}", "确定");
        }
    }

    private async void OnImportSubtitleClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!_audioLoaded)
            {
                await DisplayAlert("提示", "请先导入音频文件", "确定");
                return;
            }

            var subtitleResult = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "选择 LRC 字幕文件"
            });
            if (subtitleResult == null) return;

            _currentLrcFilePath = subtitleResult.FullPath;

            await _viewModel.LoadSubtitleAsync(_currentLrcFilePath);
            await LoadLrcMarkedWordsAsync(_currentLrcFilePath);

            RefreshMarkedWordsDisplay();
            RefreshMasteredWordsDisplay();
            RebuildLyricsList();
            UpdateSubtitleDisplay(_viewModel.CurrentLrcLine);
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"导入字幕失败: {ex.Message}", "确定");
        }
    }

    private async Task LoadLrcMarkedWordsAsync(string lrcPath)
    {
        try
        {
            var content = await System.IO.File.ReadAllTextAsync(lrcPath);
            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[marked:") && trimmed.EndsWith("]"))
                {
                    var word = trimmed[8..^1].Trim();
                    var normWord = LrcParserService.NormalizeCensoredWord(word);
                    if (word != normWord) _censoredForm[normWord] = word;
                    if (!string.IsNullOrWhiteSpace(normWord)
                        && !_markedWordSet.Contains(normWord)
                        && !_masteredWordSet.Contains(normWord))
                    {
                        _markedWordSet.Add(normWord);
                        _wordMasteryLevels[normWord] = 0;
                        await _viewModel.AddMarkedWordAsync(normWord);
                    }
                }
            }
        }
        catch
        {
        }
    }

    private void LoadAudioCover(string audioPath)
    {
        try
        {
            using var file = TagLib.File.Create(audioPath);
            var picture = file.Tag.Pictures?.FirstOrDefault();
            if (picture?.Data?.Data?.Length > 0)
            {
                var imageBytes = picture.Data.Data;
                CoverImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                // 生成 90px 高斯模糊背景
                var blurredBytes = GenerateBlurredCover(imageBytes, 90);
                if (blurredBytes != null)
                {
                    CoverBlurImage.Source = ImageSource.FromStream(() => new MemoryStream(blurredBytes));
                    BlurOverlay.IsVisible = true;
                }
                else
                {
                    CoverBlurImage.Source = "empty.png";
                    BlurOverlay.IsVisible = false;
                }
            }
            else
            {
                CoverImage.Source = "empty.png";
                CoverBlurImage.Source = "empty.png";
                BlurOverlay.IsVisible = false;
            }
        }
        catch
        {
            CoverImage.Source = "empty.png";
            CoverBlurImage.Source = "empty.png";
            BlurOverlay.IsVisible = false;
        }
    }

    private static byte[]? GenerateBlurredCover(byte[] imageBytes, int blurRadius)
    {
        try
        {
            using var original = SKBitmap.Decode(imageBytes);
            if (original == null) return null;

            // 缩小到合理的处理尺寸（先缩放再模糊，性能好）
            int maxDim = 1080;
            float scale = 1f;
            if (original.Width > maxDim || original.Height > maxDim)
            {
                scale = Math.Min((float)maxDim / original.Width, (float)maxDim / original.Height);
            }
            int w = (int)(original.Width * scale);
            int h = (int)(original.Height * scale);

            using var resized = original.Resize(new SKImageInfo(w, h), SKFilterQuality.Medium);
            if (resized == null) return null;

            // 90px 高斯模糊
            using var filter = SKImageFilter.CreateBlur(blurRadius, blurRadius);
            using var paint = new SKPaint { ImageFilter = filter };
            using var surface = SKSurface.Create(new SKImageInfo(w, h));
            var canvas = surface.Canvas;
            canvas.DrawBitmap(resized, 0, 0, paint);

            using var blurredImage = surface.Snapshot();
            using var data = blurredImage.Encode(SKEncodedImageFormat.Jpeg, 80);
            return data.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GenerateBlurredCover failed: {ex.Message}");
            return null;
        }
    }

    private async void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        await _viewModel.TogglePlayPauseAsync();
    }

    private async void OnPreviousClicked(object? sender, EventArgs e)
    {
        await _viewModel.SkipBackwardAsync();
    }

    private async void OnNextClicked(object? sender, EventArgs e)
    {
        await _viewModel.SkipForwardAsync();
    }

    private void OnPlaybackRateClicked(object? sender, EventArgs e)
    {
        if (sender is Button button &&
            !string.IsNullOrWhiteSpace(button.ClassId) &&
            float.TryParse(button.ClassId, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rate))
        {
            _audioService.SetPlaybackRate(rate);
            UpdatePlaybackRateButtons(rate);
            CustomSpeedSlider.Value = Math.Max(CustomSpeedSlider.Minimum, Math.Min(rate, CustomSpeedSlider.Maximum));
        }
    }

    private void OnCustomSpeedSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        var rate = (float)Math.Round(e.NewValue / 0.05) * 0.05f;
        if (rate < 0.5f) rate = 0.5f;
        if (rate > 3.0f) rate = 3.0f;
        CustomSpeedLabel.Text = $"{rate:F2}x";
    }

    private async void OnCustomSpeedDragCompleted(object? sender, EventArgs e)
    {
        var rate = (float)Math.Round(CustomSpeedSlider.Value / 0.05) * 0.05f;
        if (rate < 0.5f) rate = 0.5f;
        if (rate > 3.0f) rate = 3.0f;
        _audioService.SetPlaybackRate(rate);
        UpdatePlaybackRateButtons(rate);
        CustomSpeedSlider.Value = rate;
        CustomSpeedLabel.Text = $"{rate:F2}x";
    }

    private void OnProgressSliderDragStarted(object? sender, EventArgs e)
    {
        _isDraggingSlider = true;
    }

    private async void OnProgressSliderDragCompleted(object? sender, EventArgs e)
    {
        _isDraggingSlider = false;
        if (_viewModel.TotalDuration.TotalMilliseconds <= 0) return;
        var target = TimeSpan.FromMilliseconds(ProgressSlider.Value * _viewModel.TotalDuration.TotalMilliseconds / 100d);
        await _viewModel.SeekAsync(target);
    }

    private async void OnProgressSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_isUpdatingProgressFromPlayback || _isDraggingSlider) return;
        if (_viewModel.TotalDuration.TotalMilliseconds <= 0) return;
        var position = TimeSpan.FromMilliseconds(e.NewValue * _viewModel.TotalDuration.TotalMilliseconds / 100d);
        await _viewModel.SeekAsync(position);
    }

    private void ViewModel_LrcLinesUpdated(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RebuildLyricsList();
            UpdateSubtitleDisplay(_viewModel.CurrentLrcLine);
        });
    }

    private void ViewModel_CurrentLineChanged(object? sender, LrcLine? currentLine)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateSubtitleDisplay(currentLine);
            HighlightCurrentLyricLine(currentLine);
        });
    }

    private void ViewModel_PlaybackPositionChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var currentPos = _viewModel.CurrentPosition;
            var total = _viewModel.TotalDuration;
            TimeLabel.Text = $"{FormatTime(currentPos)} / {FormatTime(total)}";

            if (!_isDraggingSlider && total.TotalMilliseconds > 0)
            {
                _isUpdatingProgressFromPlayback = true;
                try
                {
                    ProgressSlider.Value = (currentPos.TotalMilliseconds / total.TotalMilliseconds) * 100d;
                }
                finally
                {
                    _isUpdatingProgressFromPlayback = false;
                }
            }
        });
    }

    private void ViewModel_PlaybackStateChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdatePlayPauseIcon();
            UpdateFavoriteButton();
        });
    }

    private void UpdatePlayPauseIcon()
    {
        PlayPauseLabel.Text = _viewModel.CurrentPlaybackState == PlaybackStateEnum.Playing ? "❚❚" : "▶";
        PlayPauseLabel.TextColor = Colors.White;
        PlayPauseButton.Stroke = Colors.White;
        PlayPauseButton.BackgroundColor = Colors.Transparent;
    }

    private void UpdateSubtitleDisplay(LrcLine? line)
    {
        var displayLine = line;
        if (displayLine == null && _viewModel.LrcLines.Count > 0)
            displayLine = _viewModel.LrcLines[0];

        SubtitleWordsLayout.Children.Clear();

        if (displayLine == null)
        {
            SubtitleWordsLayout.Children.Add(new Label
            {
                Text = "导入音频和 LRC 字幕后显示歌词",
                TextColor = Color.FromArgb("#6B7280"),
                FontSize = 28
            });
            ChineseTextLabel.Text = "_中文翻译_";
            WordTranslationLabel.Text = "";
            MarkedVocabLabel.Text = "";
            _hoverAnimTokens.Clear();
            return;
        }

        _hoverAnimTokens.Clear();

        ChineseTextLabel.Text = "_中文翻译_";
        WordTranslationLabel.Text = "";

        var markedInLine = GetMarkedWordsForLine(displayLine).ToList();
        MarkedVocabLabel.Text = markedInLine.Count > 0
            ? "★ " + string.Join("  ", markedInLine.Select(w => $"{w}(_单词中译_)"))
            : "";

        if (displayLine.Words == null || displayLine.Words.Count == 0)
        {
            SubtitleWordsLayout.Children.Add(new Label
            {
                Text = displayLine.EnglishText,
                TextColor = Colors.White,
                FontSize = 28,
                FontAttributes = FontAttributes.Bold
            });
            return;
        }

        foreach (var word in displayLine.Words.OrderBy(w => w.PositionInLine))
        {
            // 缩略词展开：you're → you + are，取最低掌握度
            var effLevel = GetEffectiveMastery(word.Word);
            var isMarked = effLevel >= 0 && effLevel < 5;
            var isMastered = effLevel >= 5;
            var wordColor = effLevel >= 0
                ? GetWordColorByMastery(effLevel)
                : Colors.White;
            var underlineColor = wordColor;

            // 存储展开信息（缩略词 → 完整形，普通词 → 自身）
            var expansion = Contractions.TryGetValue(word.Word, out var parts)
                ? string.Join(" ", parts) : word.Word;

            var container = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(new GridLength(3))
                },
                Padding = new Thickness(0, 0, 8, 6),
                BackgroundColor = Colors.Transparent,
                ClassId = word.Word,
                IsClippedToBounds = false
            };

            var wordLabel = new Label
            {
                Text = word.Word,
                FontSize = 28,
                FontAttributes = FontAttributes.Bold,
                TextColor = isMarked || isMastered ? wordColor : Colors.White,
                Padding = new Thickness(2, 0),
                BackgroundColor = Colors.Transparent,
                InputTransparent = true
            };
            Grid.SetRow(wordLabel, 0);

            var underline = new BoxView
            {
                Color = underlineColor,
                HeightRequest = 3,
                HorizontalOptions = LayoutOptions.Fill,
                ScaleX = isMarked ? 1.0 : 0.0,
                AnchorX = 0,
                Opacity = isMarked ? 1.0 : 0.85,
                InputTransparent = true
            };
            Grid.SetRow(underline, 1);

            container.Children.Add(wordLabel);
            container.Children.Add(underline);

            var tap = new TapGestureRecognizer();
            tap.Tapped += OnWordTapped;
            container.GestureRecognizers.Add(tap);

            void StartHover()
            {
                // 取消这个词上正在运行的任何旧动画
                if (_hoverAnimTokens.TryGetValue(word.Word, out var oldCts))
                    oldCts.Cancel();

                // 缩略词感知：标记以最低掌握度为准
                var effLv = GetEffectiveMastery(word.Word);
                var marked = effLv >= 0;
                if (marked)
                {
                    var exp = Contractions.TryGetValue(word.Word, out var p)
                        ? $" ({string.Join(" ", p)})" : "";
                    WordTranslationLabel.Text = $"{word.Word}{exp}  _单词中译_";
                    return;
                }

                var cts = new CancellationTokenSource();
                _hoverAnimTokens[word.Word] = cts;
                _ = AnimateUnderlineIn(underline, cts.Token);
                wordLabel.TextColor = Color.FromArgb("#FDE68A");
                var exp2 = Contractions.TryGetValue(word.Word, out var pp)
                    ? $" ({string.Join(" ", pp)})" : "";
                WordTranslationLabel.Text = $"{word.Word}{exp2}  _单词中译_";
            }

            void EndHover()
            {
                // 取消这个词上的动画（无论是 In 还是 Out）
                if (_hoverAnimTokens.TryGetValue(word.Word, out var oldCts))
                    oldCts.Cancel();

                var effLv2 = GetEffectiveMastery(word.Word);
                var marked2 = effLv2 >= 0;
                if (marked2)
                {
                    wordLabel.TextColor = wordColor;
                    WordTranslationLabel.Text = "";
                    return;
                }

                var cts = new CancellationTokenSource();
                _hoverAnimTokens[word.Word] = cts;
                _ = AnimateUnderlineOut(underline, cts.Token);
                wordLabel.TextColor = Colors.White;
                WordTranslationLabel.Text = "";
            }

            var pointer = new PointerGestureRecognizer();
            pointer.PointerEntered += (_, _) => StartHover();
            pointer.PointerExited += (_, _) => EndHover();
            container.GestureRecognizers.Add(pointer);

            // 右键取消标记 (Windows)
#if WINDOWS
            container.Loaded += OnWordContainerLoaded;
#endif

            SubtitleWordsLayout.Children.Add(container);
        }
    }

#if WINDOWS
    private void OnWordContainerLoaded(object? sender, EventArgs e)
    {
        if (sender is not Grid g) return;
        g.Loaded -= OnWordContainerLoaded;
        if (g.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement uie)
        {
            uie.RightTapped += async (_, _) =>
            {
                var word = g.ClassId ?? "";
                var label = g.Children.OfType<Label>().FirstOrDefault();
                var uline = g.Children.OfType<BoxView>().FirstOrDefault();
                if (label != null && uline != null)
                    await UnmarkWordAsync(word, label, uline);
            };
        }
    }
#endif

    /// <summary>
    /// 右键：取消标记并清理 LRC
    /// </summary>
    private async Task UnmarkWordAsync(string word, Label wordLabel, BoxView underline)
    {
        var normWord = LrcParserService.NormalizeCensoredWord(word);

        if (!_markedWordSet.Contains(normWord) && !_masteredWordSet.Contains(normWord))
            return;

        _markedWordSet.Remove(normWord);
        _masteredWordSet.Remove(normWord);
        _wordMasteryLevels.Remove(normWord);
        await _viewModel.RemoveMarkedWordAsync(normWord);

        underline.ScaleX = 1.0;
        await FlashReverse(wordLabel, underline);
        await AnimateUnderlineOut(underline);
        wordLabel.TextColor = Colors.White;

        if (!string.IsNullOrEmpty(_currentLrcFilePath))
            await RemoveMarkFromLrcFileAsync(_currentLrcFilePath, word);

        RefreshMarkedWordsDisplay();
        RefreshMasteredWordsDisplay();
        RebuildLyricsList();
        UpdateSubtitleDisplay(_viewModel.CurrentLrcLine);
    }

    private IEnumerable<string> GetMarkedWordsForLine(LrcLine line)
    {
        if (line.Words == null)
            return Enumerable.Empty<string>();

        return line.Words
            .Where(w => _markedWordSet.Contains(LrcParserService.NormalizeCensoredWord(w.Word)))
            .OrderBy(w => w.PositionInLine)
            .Select(w => w.Word);
    }

    private async void OnWordTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Grid container)
            return;

        var word = container.ClassId ?? "";
        if (string.IsNullOrWhiteSpace(word))
            return;

        var wordLabel = container.Children.OfType<Label>().FirstOrDefault();
        var underline = container.Children.OfType<BoxView>().FirstOrDefault();
        if (wordLabel == null || underline == null)
            return;

        // 屏蔽词还原：f**k → fuck（用于词库查询/标记），保留原词用于 LRC 文件
        var normWord = LrcParserService.NormalizeCensoredWord(word);

        // 获取缩略词所有成分（自身 + 展开成分）
        var allParts = new List<string> { word };
        if (Contractions.TryGetValue(word, out var parts))
            allParts.AddRange(parts);

        // 取成分中最低优先度的状态（已归一化内部查询）
        var effLevel = GetEffectiveMastery(word);

        if (effLevel >= 0 && effLevel < 5)
        {
            // 红色(0)或橙色(1-4) → 所有成分直接变已掌握(5, 绿色)
            foreach (var p in allParts)
            {
                var np = LrcParserService.NormalizeCensoredWord(p);
                if (p != np) _censoredForm[np] = p;
                _markedWordSet.Remove(np);
                _masteredWordSet.Add(np);
                _wordMasteryLevels[np] = 5;
                await _viewModel.UpdateMasteryLevelAsync(np, 5);
            }

            var green = GetWordColorByMastery(5);
            wordLabel.TextColor = green;
            underline.Color = green;
            underline.ScaleX = 1.0;
            await FlashOnce(wordLabel, underline);

            if (!string.IsNullOrEmpty(_currentLrcFilePath))
                foreach (var p in allParts)
                    await RemoveMarkFromLrcFileAsync(_currentLrcFilePath, p);
        }
        else if (effLevel >= 5)
        {
            // 绿色(已掌握) → 单击无视（只有右键才能取消标记）
            return;
        }
        else
        {
            // 未标记 → 标记所有成分为新词(红色)
            foreach (var p in allParts)
            {
                var np = LrcParserService.NormalizeCensoredWord(p);
                if (p != np) _censoredForm[np] = p;
                _markedWordSet.Add(np);
                _wordMasteryLevels[np] = 0;
                await _viewModel.AddMarkedWordAsync(np);
            }
            await FlashOnce(wordLabel, underline);
            var red = GetWordColorByMastery(0);
            wordLabel.TextColor = red;
            underline.Color = red;
            underline.ScaleX = 1.0;
            var exp = allParts.Count > 1 ? $" ({string.Join(" ", allParts.Skip(1))})" : "";
            WordTranslationLabel.Text = $"{word}{exp}  _单词中译_";

            if (!string.IsNullOrEmpty(_currentLrcFilePath))
                foreach (var p in allParts)
                    await MarkWordInLrcFileAsync(_currentLrcFilePath, p);
        }

        RefreshMarkedWordsDisplay();
        RefreshMasteredWordsDisplay();
        RebuildLyricsList();
        UpdateSubtitleDisplay(_viewModel.CurrentLrcLine);
    }

    private static async Task FlashOnce(Label label, BoxView underline)
    {
        underline.ScaleX = 1.0;
        label.TextColor = Color.FromArgb("#111827");
        underline.Opacity = 1.0;
        await Task.Delay(120);
        label.TextColor = Color.FromArgb("#FACC15");
        underline.Opacity = 0.85;
        await Task.Delay(100);
    }

    private static async Task FlashReverse(Label label, BoxView underline)
    {
        label.TextColor = Color.FromArgb("#111827");
        await Task.Delay(100);
        label.TextColor = Colors.White;
        label.BackgroundColor = Colors.Transparent;
        await Task.Delay(80);
    }

    /// <summary>
    /// 手动插值下划线淡入动画（避开 WinUI3 CompositionAnimation 裁剪 Bug）
    /// </summary>
    private static async Task AnimateUnderlineIn(BoxView underline, uint durationMs = 180)
    {
        double start = underline.ScaleX;
        const double target = 1.0;
        const int steps = 12;
        int delay = (int)(durationMs / steps);
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            double eased = t * t * t; // CubicOut easing
            underline.ScaleX = start + (target - start) * eased;
            await Task.Delay(delay);
        }
        underline.ScaleX = target;
    }

    /// <summary>
    /// 带取消的淡入动画（用于 hover 避免并发竞争）
    /// </summary>
    private static async Task AnimateUnderlineIn(BoxView underline, CancellationToken ct, uint durationMs = 180)
    {
        double start = underline.ScaleX;
        const double target = 1.0;
        const int steps = 12;
        int delay = (int)(durationMs / steps);
        for (int i = 1; i <= steps; i++)
        {
            if (ct.IsCancellationRequested) return;
            double t = (double)i / steps;
            double eased = t * t * t;
            underline.ScaleX = start + (target - start) * eased;
            try { await Task.Delay(delay, ct); }
            catch (OperationCanceledException) { return; }
        }
        if (!ct.IsCancellationRequested) underline.ScaleX = target;
    }

    /// <summary>
    /// 手动插值下划线淡出动画
    /// </summary>
    private static async Task AnimateUnderlineOut(BoxView underline, uint durationMs = 120)
    {
        double start = underline.ScaleX;
        const double target = 0.0;
        const int steps = 10;
        int delay = (int)(durationMs / steps);
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            double eased = 1.0 - (1.0 - t) * (1.0 - t) * (1.0 - t); // CubicIn
            underline.ScaleX = start + (target - start) * eased;
            await Task.Delay(delay);
        }
        underline.ScaleX = target;
    }

    /// <summary>
    /// 带取消的淡出动画（用于 hover 避免并发竞争）
    /// </summary>
    private static async Task AnimateUnderlineOut(BoxView underline, CancellationToken ct, uint durationMs = 120)
    {
        double start = underline.ScaleX;
        const double target = 0.0;
        const int steps = 10;
        int delay = (int)(durationMs / steps);
        for (int i = 1; i <= steps; i++)
        {
            if (ct.IsCancellationRequested) { underline.ScaleX = 0; return; }
            double t = (double)i / steps;
            double eased = 1.0 - (1.0 - t) * (1.0 - t) * (1.0 - t);
            underline.ScaleX = start + (target - start) * eased;
            try { await Task.Delay(delay, ct); }
            catch (OperationCanceledException) { underline.ScaleX = 0; return; }
        }
        if (!ct.IsCancellationRequested) underline.ScaleX = target;
        else underline.ScaleX = 0;
    }

    private static async Task MarkWordInLrcFileAsync(string lrcFilePath, string word)
    {
        try
        {
            var marker = $"[marked:{word}]";
            var content = await System.IO.File.ReadAllTextAsync(lrcFilePath);
            if (content.Contains(marker))
                return;
            await System.IO.File.AppendAllTextAsync(lrcFilePath, Environment.NewLine + marker);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marking word in LRC: {ex.Message}");
        }
    }

    private static async Task RemoveMarkFromLrcFileAsync(string lrcFilePath, string word)
    {
        try
        {
            var marker = $"[marked:{word}]";
            var content = await System.IO.File.ReadAllTextAsync(lrcFilePath);
            if (!content.Contains(marker))
                return;
            // Remove the marker line (handling both \n and \r\n)
            content = content.Replace(Environment.NewLine + marker, "");
            content = content.Replace(marker + Environment.NewLine, "");
            content = content.Replace(marker, "");
            await System.IO.File.WriteAllTextAsync(lrcFilePath, content);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing mark from LRC: {ex.Message}");
        }
    }

    private void RebuildLyricsList()
    {
        _lyricItems.Clear();
        foreach (var line in _viewModel.LrcLines.OrderBy(l => l.LineNumber))
        {
            var markedWords = GetMarkedWordsForLine(line).ToList();
            // 根据词的最高等级决定标记词颜色
            int bestLevel = -1;
            foreach (var w in markedWords)
            {
                if (_wordMasteryLevels.TryGetValue(w, out var lv) && lv > bestLevel)
                    bestLevel = lv;
            }
            if (bestLevel < 0 && markedWords.Count > 0)
                bestLevel = 0; // 刚从 LRC 来的新词
            var lineMarkColor = bestLevel >= 0
                ? GetWordColorByMastery(bestLevel)
                : Color.FromArgb("#FACC15");

            _lyricItems.Add(new LyricDisplayItem
            {
                Line = line,
                MarkedWordsText = markedWords.Count > 0
                    ? string.Join("  ", markedWords.Select(w => $"{w}(_单词中译_)"))
                    : "",
                MarkedWordsColor = lineMarkColor,
                TextColor = Color.FromArgb("#D1D5DB"),
                FontAttr = "None",
                BackgroundColor = Color.FromArgb("#15171C")
            });
        }
    }

    private void HighlightCurrentLyricLine(LrcLine? currentLine)
    {
        if (currentLine == null) return;

        // 找到当前行索引
        var currentIndex = -1;
        for (var i = 0; i < _lyricItems.Count; i++)
        {
            if (_lyricItems[i].Line == currentLine)
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex < 0) return;

        // 三向着色 + 触发 UI 刷新
        for (var i = 0; i < _lyricItems.Count; i++)
        {
            var item = _lyricItems[i];
            if (i == currentIndex)
            {
                item.TextColor = Colors.White;
                item.FontAttr = "Bold";
                item.BackgroundColor = Color.FromArgb("#1E2A4A");
            }
            else if (i < currentIndex)
            {
                item.TextColor = Color.FromArgb("#6B7280");
                item.FontAttr = "None";
                item.BackgroundColor = Color.FromArgb("#15171C");
            }
            else
            {
                item.TextColor = Color.FromArgb("#D1D5DB");
                item.FontAttr = "None";
                item.BackgroundColor = Color.FromArgb("#15171C");
            }
        }

        // 自动滚动——只滚歌词区 ScrollView，不滚整页
        if (_autoScrollEnabled)
        {
            var children = LyricsStackLayout.Children;
            if (currentIndex < children.Count)
            {
                View? targetChild = children[currentIndex] as View;
                if (targetChild != null)
                {
                    _isAutoScrollingProgrammatic = true;
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Task.Delay(80);
                        try
                        {
                            // 计算 targetChild 在 LyricsScrollView 内的相对 Y
                            double y = 0;
                            Element? walk = targetChild;
                            while (walk != null && walk != LyricsScrollView.Content)
                            {
                                if (walk is VisualElement ve)
                                    y += ve.Bounds.Y;
                                walk = walk.Parent;
                            }
                            await LyricsScrollView.ScrollToAsync(0, y - 10, true);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"LyricsScrollView scroll: {ex.Message}");
                        }
                        finally
                        {
                            _isAutoScrollingProgrammatic = false;
                        }
                    });
                }
            }
        }
    }

    private async void OnLyricLineTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: LyricDisplayItem selected })
        {
            await _viewModel.SeekAsync(selected.Line.StartTime);
        }
    }

    private void OnAutoScrollChanged(object? sender, CheckedChangedEventArgs e)
    {
        _autoScrollEnabled = e.Value;
        // 重新启用自动滚动时，立即滚到当前行
        if (_autoScrollEnabled && _viewModel.CurrentLrcLine != null)
        {
            HighlightCurrentLyricLine(_viewModel.CurrentLrcLine);
        }
    }

    private bool _isAutoScrollingProgrammatic;

    private void OnLyricsScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isAutoScrollingProgrammatic) return;
        if (_autoScrollEnabled)
        {
            _autoScrollEnabled = false;
            AutoScrollCheckBox.IsChecked = false;
        }
    }

    private HashSet<string> GetCurrentFileWords()
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lrcLines = _viewModel.LrcLines;
        if (lrcLines == null) return words;

        foreach (var line in lrcLines)
        {
            if (line.Words == null) continue;
            foreach (var w in line.Words)
            {
                if (!string.IsNullOrWhiteSpace(w.Word))
                    words.Add(LrcParserService.NormalizeCensoredWord(w.Word));
            }
        }
        return words;
    }

    private void RefreshMarkedWordsDisplay()
    {
        var currentFileWords = GetCurrentFileWords();
        _markedWords.Clear();
        foreach (var word in _markedWordSet
            .Where(w => currentFileWords.Contains(w))
            .OrderBy(w => w))
        {
            var level = _wordMasteryLevels.GetValueOrDefault(word, 0);
            _markedWords.Add(new MarkedWordItem
            {
                Word = word,
                Translation = "_单词中译_",
                Source = _viewModel.CurrentEpisode?.Title ?? "",
                Color = GetWordColorByMastery(level),
                LevelLabel = level == 0 ? "新词" : $"Lv.{level}"
            });
        }
    }

    private void RefreshMasteredWordsDisplay()
    {
        var currentFileWords = GetCurrentFileWords();
        _masteredWords.Clear();
        foreach (var word in _masteredWordSet
            .Where(w => currentFileWords.Contains(w))
            .OrderBy(w => w))
        {
            _masteredWords.Add(new MarkedWordItem
            {
                Word = word,
                Translation = "已掌握",
                Source = _viewModel.CurrentEpisode?.Title ?? "",
                Color = GetWordColorByMastery(5),
                LevelLabel = "已掌握"
            });
        }
    }

    private async void OnMarkedWordSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MarkedWordItem selected) return;
        MarkedWordsCollectionView.SelectedItem = null;

        var action = await DisplayActionSheet(
            selected.Word,
            "取消",
            "删除单词",
            "查询", "复习", "已掌握");

        switch (action)
        {
            case "已掌握":
                _markedWordSet.Remove(selected.Word);
                _masteredWordSet.Add(selected.Word);
                _wordMasteryLevels[selected.Word] = 5;
                await _viewModel.UpdateMasteryLevelAsync(selected.Word, 5);
                // 从 LRC 文件中移除标记（使用显示原形）
                if (!string.IsNullOrEmpty(_currentLrcFilePath))
                    await RemoveMarkFromLrcFileAsync(_currentLrcFilePath,
                        _censoredForm.GetValueOrDefault(selected.Word, selected.Word));
                RefreshMarkedWordsDisplay();
                RefreshMasteredWordsDisplay();
                RebuildLyricsList();
                UpdateSubtitleDisplay(_viewModel.CurrentLrcLine);
                break;
            case "删除单词":
                _markedWordSet.Remove(selected.Word);
                _wordMasteryLevels.Remove(selected.Word);
                await _viewModel.RemoveMarkedWordAsync(selected.Word);
                // 从 LRC 文件中移除标记（使用显示原形）
                if (!string.IsNullOrEmpty(_currentLrcFilePath))
                    await RemoveMarkFromLrcFileAsync(_currentLrcFilePath,
                        _censoredForm.GetValueOrDefault(selected.Word, selected.Word));
                RefreshMarkedWordsDisplay();
                RefreshMasteredWordsDisplay();
                RebuildLyricsList();
                UpdateSubtitleDisplay(_viewModel.CurrentLrcLine);
                break;
            case "复习":
                await _viewModel.UpdateMasteryLevelAsync(selected.Word, 1);
                _wordMasteryLevels[selected.Word] = 1;
                RefreshMarkedWordsDisplay();
                break;
            case "查询":
                // 功能暂空
                break;
        }
    }

    private async void OnMasteredWordSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MarkedWordItem selected) return;
        MasteredWordsCollectionView.SelectedItem = null;

        var action = await DisplayActionSheet(
            selected.Word,
            "取消",
            "删除单词",
            "查询", "移回生词本", null);

        switch (action)
        {
            case "移回生词本":
                _masteredWordSet.Remove(selected.Word);
                _markedWordSet.Add(selected.Word);
                _wordMasteryLevels[selected.Word] = 0;
                await _viewModel.UpdateMasteryLevelAsync(selected.Word, 0);
                RefreshMarkedWordsDisplay();
                RefreshMasteredWordsDisplay();
                RebuildLyricsList();
                UpdateSubtitleDisplay(_viewModel.CurrentLrcLine);
                break;
            case "删除单词":
                _masteredWordSet.Remove(selected.Word);
                _wordMasteryLevels.Remove(selected.Word);
                await _viewModel.RemoveMarkedWordAsync(selected.Word);
                // 从 LRC 文件中移除标记
                if (!string.IsNullOrEmpty(_currentLrcFilePath))
                    await RemoveMarkFromLrcFileAsync(_currentLrcFilePath,
                        _censoredForm.GetValueOrDefault(selected.Word, selected.Word));
                RefreshMasteredWordsDisplay();
                break;
            case "查询":
                // 功能暂空
                break;
        }
    }

    private async void OnToggleFavoriteClicked(object? sender, EventArgs e)
    {
        await _viewModel.ToggleFavoriteAsync();
        UpdateFavoriteButton();

        var key = $"fav_{_viewModel.CurrentEpisode?.Id}";
        if (_viewModel.IsFavorite)
            Preferences.Set(key, true);
        else
            Preferences.Remove(key);
    }

    private void UpdateFavoriteButton()
    {
        if (_viewModel.IsFavorite)
        {
            FavoriteButton.Text = "★";
            FavoriteButton.TextColor = Color.FromArgb("#FACC15");
            FavoriteButton.BackgroundColor = Color.FromArgb("#2B2400");
        }
        else
        {
            FavoriteButton.Text = "☆";
            FavoriteButton.TextColor = Color.FromArgb("#9CA3AF");
            FavoriteButton.BackgroundColor = Color.FromArgb("#22252B");
        }
    }

    private void UpdatePlaybackRateButtons(float rate)
    {
        foreach (var child in PlaybackRateButtons.Children)
        {
            if (child is not Button button || string.IsNullOrWhiteSpace(button.ClassId))
                continue;

            var isActive = float.TryParse(button.ClassId, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var buttonRate)
                           && Math.Abs(buttonRate - rate) < 0.01f;
            button.BackgroundColor = isActive ? Color.FromArgb("#2D7DFF") : Color.FromArgb("#1A1D24");
            button.TextColor = isActive ? Colors.White : Color.FromArgb("#9CA3AF");
        }
    }

    private static string FormatTime(TimeSpan time) => $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.LrcLinesUpdated -= ViewModel_LrcLinesUpdated;
        _viewModel.CurrentLineChanged -= ViewModel_CurrentLineChanged;
        _viewModel.PlaybackPositionChanged -= ViewModel_PlaybackPositionChanged;
        _viewModel.PlaybackStateChanged -= ViewModel_PlaybackStateChanged;
    }
}
