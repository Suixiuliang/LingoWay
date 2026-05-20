namespace LingoWay.Domain.Constants;

/// <summary>
/// 应用全局常量
/// </summary>
public static class AppConstants
{
    public const string AppName = "LingoWay";
    public const string AppVersion = "1.0.0";
    public const string DatabaseFileName = "lingoWay.db";

    // 最小化版本要求
    public static class MinimumVersions
    {
        public const int AndroidMinimum = 28; // Android 9
        public const string WindowsMinimum = "7";
        public const string MacOSMinimum = "12.7.6";
        public const int iOSMinimum = 13;
    }

    // 播放器配置
    public static class Playback
    {
        public const float MinPlaybackRate = 0.5f;
        public const float MaxPlaybackRate = 2.0f;
        public const float DefaultPlaybackRate = 1.0f;

        public const int SeekBackwardSeconds = 15;
        public const int SeekForwardSeconds = 30;
    }

    // 缓存配置
    public static class Cache
    {
        public const int MaxCacheSizeMB = 500;
        public const int ImageCacheSizeMB = 100;
        public const int ThumbnailCacheSizeMB = 50;
    }

    // 下载配置
    public static class Download
    {
        public const int MaxConcurrentDownloads = 3;
        public const int ConnectionTimeoutSeconds = 30;
        public const int MaxRetries = 3;
    }

    // 词汇难度
    public static class Vocabulary
    {
        public const int HighFrequencyThreshold = 1000; // 前1000个词为高频
        public const int CoreVocabularyThreshold = 5000; // 核心词汇
    }
}
