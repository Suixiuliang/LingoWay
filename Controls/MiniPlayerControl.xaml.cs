using LingoWay.Application.Services;
using LingoWay.Domain.Models;
using SkiaSharp;

namespace LingoWay.Controls;

public partial class MiniPlayerControl : ContentView
{
    private readonly IAudioPlaybackService? _audioService;
    private bool _isPlaying;
    private ImageSource? _rawSource; // 保存原始来源，用于对比避免重复处理

    private const int ThumbSize = 72; // 缩略图尺寸 (36px 显示 × 2x DPI = 72px)

    public MiniPlayerControl()
    {
        InitializeComponent();
    }

    public MiniPlayerControl(IAudioPlaybackService audioService) : this()
    {
        _audioService = audioService;
        if (_audioService != null)
            _audioService.StateChanged += OnPlaybackStateChanged;
    }

    public void SetTitle(string? title)
        => MiniTitleLabel.Text = string.IsNullOrWhiteSpace(title) ? "未播放" : title;

    public void SetTime(TimeSpan current, TimeSpan total)
        => MiniTimeLabel.Text = $"{FormatTime(current)} / {FormatTime(total)}";

    public void SetCover(ImageSource? source)
    {
        if (source == null)
        {
            MiniCoverBorder.IsVisible = false;
            _rawSource = null;
            return;
        }

        if (ReferenceEquals(source, _rawSource)) return;
        _rawSource = source;

        var thumb = CreateThumbnail(source);
        if (thumb == null)
        {
            MiniCoverBorder.IsVisible = false;
            return;
        }

        MiniCoverImage.Source = thumb;
        MiniCoverBorder.IsVisible = true;
    }

    private static ImageSource? CreateThumbnail(ImageSource source)
    {
        try
        {
            byte[]? bytes = null;

            if (source is FileImageSource fileSrc)
                bytes = File.ReadAllBytes(fileSrc.File);
            else if (source is StreamImageSource streamSrc)
            {
                var cancelToken = new System.Threading.CancellationTokenSource(3000).Token;
                var stream = streamSrc.Stream(cancelToken).GetAwaiter().GetResult();
                if (stream != null)
                {
                    using (stream)
                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        bytes = ms.ToArray();
                    }
                }
            }

            if (bytes == null) return null;

            using var original = SKBitmap.Decode(bytes);
            if (original == null) return null;

            // 等比例缩放到 ThumbSize×ThumbSize 画布中，居中显示
            float scale = Math.Min((float)ThumbSize / original.Width, (float)ThumbSize / original.Height);
            float w = original.Width * scale;
            float h = original.Height * scale;
            float x = (ThumbSize - w) / 2f;
            float y = (ThumbSize - h) / 2f;

            using var surface = SKSurface.Create(new SKImageInfo(ThumbSize, ThumbSize));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(original, new SKRect(x, y, x + w, y + h),
                new SKSamplingOptions(SKCubicResampler.Mitchell));

            using var snapshot = surface.Snapshot();
            using var data = snapshot.Encode(SKEncodedImageFormat.Png, 85);

            return ImageSource.FromStream(() => new MemoryStream(data.ToArray()));
        }
        catch
        {
            return null;
        }
    }

    public void SetPlayState(bool isPlaying)
    {
        _isPlaying = isPlaying;
        MiniPlayIcon.Text = isPlaying ? "❚❚" : "▶";
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        try { MainThread.BeginInvokeOnMainThread(() => SetPlayState(e.NewState == PlaybackStateEnum.Playing)); }
        catch (InvalidOperationException) { }
    }

    private async void OnPlayPauseTapped(object? sender, TappedEventArgs e)
    {
        if (_audioService == null) return;
        if (_isPlaying) await _audioService.PauseAsync();
        else await _audioService.PlayAsync();
    }

    public void Detach()
    {
        if (_audioService != null)
            _audioService.StateChanged -= OnPlaybackStateChanged;
    }

    private static string FormatTime(TimeSpan t) => $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
}
