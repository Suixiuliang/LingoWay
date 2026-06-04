using Android.Content;
using Android.Media;
using LingoWay.Application.Services;

namespace LingoWay.Application.Services.Android
{
    /// <summary>
    /// Android 平台音频播放服务实现
    /// </summary>
    public class AndroidAudioPlaybackService : IAudioPlaybackService
    {
        private MediaPlayer? _mediaPlayer;
        private PlaybackStateEnum _currentState = PlaybackStateEnum.Idle;
        private TimeSpan _currentPosition = TimeSpan.Zero;
        private TimeSpan _duration = TimeSpan.Zero;
        private float _playbackRate = 1.0f;
        private float _currentVolume = 1.0f;
        private System.Timers.Timer? _positionUpdateTimer;
        private readonly object _lock = new();

        public TimeSpan CurrentPosition => _currentPosition;
        public TimeSpan Duration => _duration;
        public PlaybackStateEnum CurrentPlaybackState => _currentState;

        public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
        public event EventHandler<PlaybackPositionChangedEventArgs>? PositionChanged;
        public event EventHandler? PlaybackCompleted;
        public event EventHandler<PlaybackErrorEventArgs>? PlaybackError;

        public Task LoadAudioAsync(string audioPath)
        {
            var tcs = new TaskCompletionSource();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    ReleaseMediaPlayer();

                    _mediaPlayer = new MediaPlayer();
                    _mediaPlayer.SetAudioAttributes(new AudioAttributes.Builder()
                        .SetUsage(AudioUsageKind.Media)!
                        .SetContentType(AudioContentType.Music)!
                        .Build());
                    _mediaPlayer.SetDataSource(audioPath);
                    _mediaPlayer.Prepare();

                    _duration = TimeSpan.FromMilliseconds(_mediaPlayer.Duration);
                    _currentPosition = TimeSpan.Zero;

                    _mediaPlayer.Completion += (sender, e) =>
                    {
                        _currentState = PlaybackStateEnum.Stopped;
                        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
                        {
                            OldState = PlaybackStateEnum.Playing,
                            NewState = PlaybackStateEnum.Stopped
                        });
                        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
                        StopPositionUpdateTimer();
                    };

                    _mediaPlayer.Error += OnMediaPlayerError;

                    _currentState = PlaybackStateEnum.Idle;
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    PlaybackError?.Invoke(this, new PlaybackErrorEventArgs
                    {
                        Message = $"Failed to load audio: {ex.Message}",
                        Exception = ex
                    });
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public Task PlayAsync()
        {
            var tcs = new TaskCompletionSource();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_mediaPlayer == null)
                    {
                        tcs.SetResult();
                        return;
                    }

                    var oldState = _currentState;
                    _mediaPlayer.Start();
                    _currentState = PlaybackStateEnum.Playing;

                    StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
                    {
                        OldState = oldState,
                        NewState = PlaybackStateEnum.Playing
                    });

                    StartPositionUpdateTimer();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public Task PauseAsync()
        {
            var tcs = new TaskCompletionSource();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_mediaPlayer == null)
                    {
                        tcs.SetResult();
                        return;
                    }

                    var oldState = _currentState;
                    _mediaPlayer.Pause();
                    _currentState = PlaybackStateEnum.Paused;

                    StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
                    {
                        OldState = oldState,
                        NewState = PlaybackStateEnum.Paused
                    });

                    StopPositionUpdateTimer();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public Task StopAsync()
        {
            var tcs = new TaskCompletionSource();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_mediaPlayer == null)
                    {
                        tcs.SetResult();
                        return;
                    }

                    var oldState = _currentState;
                    _mediaPlayer.Stop();
                    _mediaPlayer.Reset();
                    _currentState = PlaybackStateEnum.Stopped;
                    _currentPosition = TimeSpan.Zero;

                    StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
                    {
                        OldState = oldState,
                        NewState = PlaybackStateEnum.Stopped
                    });

                    StopPositionUpdateTimer();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public Task SeekAsync(TimeSpan position)
        {
            var tcs = new TaskCompletionSource();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_mediaPlayer == null)
                    {
                        tcs.SetResult();
                        return;
                    }

                    _mediaPlayer.SeekTo((int)position.TotalMilliseconds);
                    _currentPosition = position;

                    PositionChanged?.Invoke(this, new PlaybackPositionChangedEventArgs
                    {
                        CurrentPosition = _currentPosition,
                        Duration = _duration
                    });
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public void SetPlaybackRate(float rate)
        {
            _playbackRate = rate;

            if (_mediaPlayer != null && global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.M)
            {
                var paramsObj = _mediaPlayer.PlaybackParams;
                paramsObj.SetSpeed(rate);
                _mediaPlayer.PlaybackParams = paramsObj;
            }
        }

        public void SetVolume(float volume)
        {
            _currentVolume = Math.Max(0f, Math.Min(1f, volume));
            try
            {
                _mediaPlayer?.SetVolume(_currentVolume, _currentVolume);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set volume: {ex.Message}");
            }
        }

        public async Task FadeVolumeAsync(float target, uint durationMs = 300)
        {
            var targetVol = Math.Max(0f, Math.Min(1f, target));
            var startVol = _currentVolume;
            var steps = Math.Max(1, (int)(durationMs / 16));
            var delayMs = (int)(durationMs / steps);

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    _currentVolume = startVol + (targetVol - startVol) * t;
                    _mediaPlayer?.SetVolume(_currentVolume, _currentVolume);
                    if (i < steps)
                        await Task.Delay(delayMs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FadeVolumeAsync failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopPositionUpdateTimer();
            ReleaseMediaPlayer();
        }

        private void ReleaseMediaPlayer()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Release();
                _mediaPlayer = null;
            }
        }

        private void StartPositionUpdateTimer()
        {
            StopPositionUpdateTimer();

            _positionUpdateTimer = new System.Timers.Timer(200); // 200ms 更新一次
            _positionUpdateTimer.Elapsed += (sender, e) =>
            {
                if (_mediaPlayer?.IsPlaying == true)
                {
                    _currentPosition = TimeSpan.FromMilliseconds(_mediaPlayer.CurrentPosition);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PositionChanged?.Invoke(this, new PlaybackPositionChangedEventArgs
                        {
                            CurrentPosition = _currentPosition,
                            Duration = _duration
                        });
                    });
                }
            };
            _positionUpdateTimer.AutoReset = true;
            _positionUpdateTimer.Start();
        }

        private void StopPositionUpdateTimer()
        {
            _positionUpdateTimer?.Stop();
            _positionUpdateTimer?.Dispose();
            _positionUpdateTimer = null;
        }

        private void OnMediaPlayerError(object? sender, MediaPlayer.ErrorEventArgs e)
        {
            _currentState = PlaybackStateEnum.Error;
            PlaybackError?.Invoke(this, new PlaybackErrorEventArgs
            {
                Message = $"MediaPlayer error: what={e.What}, extra={e.Extra}"
            });
            StopPositionUpdateTimer();
        }
    }
}
