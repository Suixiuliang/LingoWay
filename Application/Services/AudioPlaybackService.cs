using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LingoWay.Domain.Models;

namespace LingoWay.Application.Services;

/// <summary>
/// 音频播放服务接口
/// </summary>
public interface IAudioPlaybackService
{
    /// <summary>
    /// 加载音频文件
    /// </summary>
    Task LoadAudioAsync(string audioPath);

    /// <summary>
    /// 开始播放
    /// </summary>
    Task PlayAsync();

    /// <summary>
    /// 暂停播放
    /// </summary>
    Task PauseAsync();

    /// <summary>
    /// 停止播放
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 设置播放位置
    /// </summary>
    Task SeekAsync(TimeSpan position);

    /// <summary>
    /// 设置播放速率
    /// </summary>
    void SetPlaybackRate(float rate);

    /// <summary>
    /// 设置音量 (0.0 - 1.0)
    /// </summary>
    void SetVolume(float volume);

    /// <summary>
    /// 淡入/淡出音量到目标值（用于暂停/恢复的平滑过渡）
    /// </summary>
    Task FadeVolumeAsync(float target, uint durationMs = 300);

    /// <summary>
    /// 获取当前播放位置
    /// </summary>
    TimeSpan CurrentPosition { get; }

    /// <summary>
    /// 获取音频总时长
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// 播放状态改变事件
    /// </summary>
    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// 播放位置改变事件
    /// </summary>
    event EventHandler<PlaybackPositionChangedEventArgs>? PositionChanged;

    /// <summary>
    /// 播放错误事件
    /// </summary>
    event EventHandler<PlaybackErrorEventArgs>? PlaybackError;

    /// <summary>
    /// 播放完成事件
    /// </summary>
    event EventHandler? PlaybackCompleted;

    /// <summary>
    /// 获取当前播放状态
    /// </summary>
    PlaybackStateEnum CurrentPlaybackState { get; }

    /// <summary>
    /// 释放资源
    /// </summary>
    void Dispose();
}

/// <summary>
/// 播放状态枚举
/// </summary>
public enum PlaybackStateEnum
{
    Idle,
    Playing,
    Paused,
    Stopped,
    Error
}

/// <summary>
/// 播放状态改变事件参数
/// </summary>
public class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackStateEnum OldState { get; set; }
    public PlaybackStateEnum NewState { get; set; }
}

/// <summary>
/// 播放位置改变事件参数
/// </summary>
public class PlaybackPositionChangedEventArgs : EventArgs
{
    public TimeSpan CurrentPosition { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// 播放错误事件参数
/// </summary>
public class PlaybackErrorEventArgs : EventArgs
{
    public string Message { get; set; } = "";
    public Exception? Exception { get; set; }
}

/// <summary>
/// 默认播放服务实现（无操作，用于非 Windows 平台）
/// </summary>
public class DefaultAudioPlaybackService : IAudioPlaybackService
{
    public TimeSpan CurrentPosition => TimeSpan.Zero;
    public TimeSpan Duration => TimeSpan.Zero;
    public PlaybackStateEnum CurrentPlaybackState => PlaybackStateEnum.Idle;

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<PlaybackPositionChangedEventArgs>? PositionChanged;
    public event EventHandler<PlaybackErrorEventArgs>? PlaybackError;
    public event EventHandler? PlaybackCompleted;

    public void Dispose() { }
    public Task LoadAudioAsync(string audioPath) => Task.CompletedTask;
    public Task PlayAsync() => Task.CompletedTask;
    public Task PauseAsync() => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public Task SeekAsync(TimeSpan position) => Task.CompletedTask;
    public void SetPlaybackRate(float rate) { }
    public void SetVolume(float volume) { }
    public Task FadeVolumeAsync(float target, uint durationMs = 300) => Task.CompletedTask;
}
