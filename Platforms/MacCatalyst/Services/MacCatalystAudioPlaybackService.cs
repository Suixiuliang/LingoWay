using AVFoundation;
using Foundation;
using LingoWay.Application.Services;

namespace LingoWay.Application.Services.MacCatalyst
{
    /// <summary>
    /// MacCatalyst 平台音频播放服务实现（复用 iOS AVAudioPlayer）
    /// </summary>
    public class MacCatalystAudioPlaybackService : IAudioPlaybackService
    {
        private AVAudioPlayer? _audioPlayer;
        private PlaybackStateEnum _currentState = PlaybackStateEnum.Idle;
        private TimeSpan _currentPosition = TimeSpan.Zero;
        private TimeSpan _duration = TimeSpan.Zero;
        private float _playbackRate = 1.0f;
        private float _currentVolume = 1.0f;
        private NSTimer? _positionUpdateTimer;

        public TimeSpan CurrentPosition => _currentPosition;
        public TimeSpan Duration => _duration;
        public PlaybackStateEnum CurrentPlaybackState => _currentState;

        public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
        public event EventHandler<PlaybackPositionChangedEventArgs>? PositionChanged;
        public event EventHandler? PlaybackCompleted;
        public event EventHandler<PlaybackErrorEventArgs>? PlaybackError;

        public Task LoadAudioAsync(string audioPath)
        {
            return InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    ReleaseAudioPlayer();

                    var url = NSUrl.FromFilename(audioPath);
                    _audioPlayer = AVAudioPlayer.FromUrl(url);

                    if (_audioPlayer == null)
                    {
                        throw new InvalidOperationException("Failed to create AVAudioPlayer");
                    }

                    _audioPlayer.PrepareToPlay();
                    _duration = TimeSpan.FromSeconds(_audioPlayer.Duration);
                    _currentPosition = TimeSpan.Zero;

                    // 设置播放完成通知
                    NSNotificationCenter.DefaultCenter.AddObserver(
                        AVPlayerItem.DidPlayToEndTimeNotification,
                        _ =>
                        {
                            _currentState = PlaybackStateEnum.Stopped;
                            StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
                            {
                                OldState = PlaybackStateEnum.Playing,
                                NewState = PlaybackStateEnum.Stopped
                            });
                            PlaybackCompleted?.Invoke(this, EventArgs.Empty);
                            StopPositionUpdateTimer();
                        });

                    _currentState = PlaybackStateEnum.Idle;
                }
                catch (Exception ex)
                {
                    PlaybackError?.Invoke(this, new PlaybackErrorEventArgs
                    {
                        Message = $"Failed to load audio: {ex.Message}",
                        Exception = ex
                    });
                    throw;
                }
            });
        }

        public Task PlayAsync()
        {
            return InvokeOnMainThreadAsync(() =>
            {
                if (_audioPlayer == null)
                    return;

                var oldState = _currentState;
                _audioPlayer.Play();
                _currentState = PlaybackStateEnum.Playing;

                StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
                {
                    OldState = oldState,
                    NewState = PlaybackStateEnum.Playing
                });

                StartPositionUpdateTimer();
            });
        }

        public Task PauseAsync()
        {
            return InvokeOnMainThreadAsync(() =>
            {
                if (_audioPlayer == null)
                    return;

                var oldState = _currentState;
                _audioPlayer.Pause();
                _currentState = PlaybackStateEnum.Paused;

                StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
                {
                    OldState = oldState,
                    NewState = PlaybackStateEnum.Paused
                });

                StopPositionUpdateTimer();
            });
        }

        public Task StopAsync()
        {
            return InvokeOnMainThreadAsync(() =>
            {
                if (_audioPlayer == null)
                    return;

                var oldState = _currentState;
                _audioPlayer.Stop();
                _audioPlayer.CurrentTime = 0;
                _currentState = PlaybackStateEnum.Stopped;
                _currentPosition = TimeSpan.Zero;

                StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
                {
                    OldState = oldState,
                    NewState = PlaybackStateEnum.Stopped
                });

                StopPositionUpdateTimer();
            });
        }

        public Task SeekAsync(TimeSpan position)
        {
            return InvokeOnMainThreadAsync(() =>
            {
                if (_audioPlayer == null)
                    return;

                _audioPlayer.CurrentTime = position.TotalSeconds;
                _currentPosition = position;

                PositionChanged?.Invoke(this, new PlaybackPositionChangedEventArgs
                {
                    CurrentPosition = _currentPosition,
                    Duration = _duration
                });
            });
        }

        public void SetPlaybackRate(float rate)
        {
            _playbackRate = rate;

            if (_audioPlayer != null)
            {
                _audioPlayer.EnableRate = true;
                _audioPlayer.Rate = rate;
            }
        }

        public void SetVolume(float volume)
        {
            _currentVolume = Math.Max(0f, Math.Min(1f, volume));
            if (_audioPlayer != null)
            {
                _audioPlayer.Volume = _currentVolume;
            }
        }

        public async Task FadeVolumeAsync(float target, uint durationMs = 300)
        {
            var targetVol = Math.Max(0f, Math.Min(1f, target));
            var startVol = _currentVolume;
            var steps = Math.Max(1, (int)(durationMs / 16));
            var delayMs = (int)(durationMs / steps);

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                _currentVolume = startVol + (targetVol - startVol) * t;
                if (_audioPlayer != null)
                    _audioPlayer.Volume = _currentVolume;
                if (i < steps)
                    await Task.Delay(delayMs);
            }
        }

        public void Dispose()
        {
            StopPositionUpdateTimer();
            ReleaseAudioPlayer();
        }

        private void ReleaseAudioPlayer()
        {
            _audioPlayer?.Dispose();
            _audioPlayer = null;
        }

        private void StartPositionUpdateTimer()
        {
            StopPositionUpdateTimer();

            _positionUpdateTimer = NSTimer.CreateRepeatingScheduledTimer(
                TimeSpan.FromMilliseconds(200),
                _ =>
                {
                    if (_audioPlayer?.Playing == true)
                    {
                        _currentPosition = TimeSpan.FromSeconds(_audioPlayer.CurrentTime);

                        PositionChanged?.Invoke(this, new PlaybackPositionChangedEventArgs
                        {
                            CurrentPosition = _currentPosition,
                            Duration = _duration
                        });
                    }
                });
        }

        private void StopPositionUpdateTimer()
        {
            _positionUpdateTimer?.Invalidate();
            _positionUpdateTimer?.Dispose();
            _positionUpdateTimer = null;
        }

        private Task InvokeOnMainThreadAsync(Action action)
        {
            var tcs = new TaskCompletionSource();
            NSRunLoop.Main.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
