using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Media.Core;
using LingoWay.Domain.Models;

namespace LingoWay.Application.Services.Windows;

/// <summary>
/// Windows 平台音频播放服务实现
/// 使用 MediaPlayer 进行音频播放
/// </summary>
public class WindowsAudioPlaybackService : IAudioPlaybackService
{
    private MediaPlayer? _mediaPlayer;
    private PlaybackStateEnum _currentState = PlaybackStateEnum.Idle;
    private string _currentAudioPath = "";
    private bool _isDisposed = false;

    public TimeSpan CurrentPosition => _mediaPlayer?.Position ?? TimeSpan.Zero;
    public TimeSpan Duration => _mediaPlayer?.NaturalDuration ?? TimeSpan.Zero;
    public PlaybackStateEnum CurrentPlaybackState => _currentState;

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<PlaybackPositionChangedEventArgs>? PositionChanged;
    public event EventHandler<PlaybackErrorEventArgs>? PlaybackError;
    public event EventHandler? PlaybackCompleted;

    public WindowsAudioPlaybackService()
    {
        InitializeMediaPlayer();
    }

    private void InitializeMediaPlayer()
    {
        _mediaPlayer = new MediaPlayer
        {
            AutoPlay = false
        };

        _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        _mediaPlayer.PlaybackSession.PlaybackRateChanged += PlaybackSession_PlaybackRateChanged;
    }

    public async Task LoadAudioAsync(string audioPath)
    {
        try
        {
            if (string.IsNullOrEmpty(audioPath))
                throw new ArgumentException("Audio path cannot be empty");

            if (!File.Exists(audioPath))
                throw new FileNotFoundException($"Audio file not found: {audioPath}");

            _currentAudioPath = audioPath;

            // 创建 MediaSource
            var mediaSource = MediaSource.CreateFromUri(new Uri($"file:///{audioPath}"));
            _mediaPlayer?.Source = mediaSource;

            OnStateChanged(PlaybackStateEnum.Idle);
        }
        catch (Exception ex)
        {
            OnPlaybackError($"Failed to load audio: {ex.Message}", ex);
            throw;
        }
    }

    public async Task PlayAsync()
    {
        try
        {
            if (_mediaPlayer == null)
                throw new InvalidOperationException("MediaPlayer not initialized");

            if (string.IsNullOrEmpty(_currentAudioPath))
                throw new InvalidOperationException("No audio loaded");

            _mediaPlayer.Play();
            OnStateChanged(PlaybackStateEnum.Playing);
        }
        catch (Exception ex)
        {
            OnPlaybackError($"Failed to play audio: {ex.Message}", ex);
            throw;
        }
    }

    public async Task PauseAsync()
    {
        try
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Pause();
                OnStateChanged(PlaybackStateEnum.Paused);
            }
        }
        catch (Exception ex)
        {
            OnPlaybackError($"Failed to pause audio: {ex.Message}", ex);
            throw;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Pause();
                _mediaPlayer.Position = TimeSpan.Zero;
                _currentAudioPath = "";
                OnStateChanged(PlaybackStateEnum.Stopped);
            }
        }
        catch (Exception ex)
        {
            OnPlaybackError($"Failed to stop audio: {ex.Message}", ex);
            throw;
        }
    }

    public async Task SeekAsync(TimeSpan position)
    {
        try
        {
            if (_mediaPlayer == null)
                throw new InvalidOperationException("MediaPlayer not initialized");

            // 确保位置在有效范围内
            var validPosition = TimeSpan.FromMilliseconds(
                Math.Max(0, Math.Min(position.TotalMilliseconds, Duration.TotalMilliseconds))
            );

            _mediaPlayer.Position = validPosition;
        }
        catch (Exception ex)
        {
            OnPlaybackError($"Failed to seek: {ex.Message}", ex);
            throw;
        }
    }

    public void SetPlaybackRate(float rate)
    {
        try
        {
            if (_mediaPlayer?.PlaybackSession != null)
            {
                var validRate = Math.Max(0.5f, Math.Min(3.0f, rate));
                _mediaPlayer.PlaybackSession.PlaybackRate = validRate;
            }
        }
        catch (Exception ex)
        {
            OnPlaybackError($"Failed to set playback rate: {ex.Message}", ex);
        }
    }

    public void SetVolume(float volume)
    {
        try
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = Math.Max(0f, Math.Min(1f, volume));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set volume: {ex.Message}");
        }
    }

    /// <summary>
    /// 淡入/淡出音量到目标值，用于暂停/恢复的平滑过渡
    /// </summary>
    public async Task FadeVolumeAsync(float target, uint durationMs = 300)
    {
        try
        {
            if (_mediaPlayer == null) return;

            var start = _mediaPlayer.Volume;
            var steps = 15;
            var delayMs = (int)(durationMs / steps);

            for (int i = 1; i <= steps; i++)
            {
                var t = (double)i / steps;
                var eased = t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
                var vol = start + (target - start) * eased;
                _mediaPlayer.Volume = Math.Max(0, Math.Min(1, vol));
                await Task.Delay(delayMs);
            }

            _mediaPlayer.Volume = Math.Max(0, Math.Min(1, target));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FadeVolume failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        try
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
                _mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                _mediaPlayer.PlaybackSession.PlaybackRateChanged -= PlaybackSession_PlaybackRateChanged;
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }
        }
        finally
        {
            _isDisposed = true;
        }
    }

    // ========== Private Event Handlers ==========

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        OnPositionChanged(sender.Position, sender.NaturalDuration);
    }

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        OnPlaybackCompleted();
    }

    private void PlaybackSession_PlaybackRateChanged(MediaPlaybackSession sender, object args)
    {
        System.Diagnostics.Debug.WriteLine($"Playback rate changed to: {sender.PlaybackRate}");
    }

    // ========== Protected Event Raising Methods ==========

    protected virtual void OnStateChanged(PlaybackStateEnum newState)
    {
        var oldState = _currentState;
        _currentState = newState;
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
        {
            OldState = oldState,
            NewState = newState
        });
    }

    protected virtual void OnPositionChanged(TimeSpan currentPosition, TimeSpan duration)
    {
        PositionChanged?.Invoke(this, new PlaybackPositionChangedEventArgs
        {
            CurrentPosition = currentPosition,
            Duration = duration
        });
    }

    protected virtual void OnPlaybackError(string message, Exception? exception)
    {
        OnStateChanged(PlaybackStateEnum.Error);
        PlaybackError?.Invoke(this, new PlaybackErrorEventArgs
        {
            Message = message,
            Exception = exception
        });
    }

    protected virtual void OnPlaybackCompleted()
    {
        OnStateChanged(PlaybackStateEnum.Stopped);
        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
    }
}
