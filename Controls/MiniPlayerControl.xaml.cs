using LingoWay.Application.Services;
using LingoWay.Domain.Models;

namespace LingoWay.Controls;

public partial class MiniPlayerControl : ContentView
{
    private readonly IAudioPlaybackService? _audioService;
    private bool _isPlaying;

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
        if (source != null) { MiniCoverImage.Source = source; MiniCoverBorder.IsVisible = true; }
        else MiniCoverBorder.IsVisible = false;
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
