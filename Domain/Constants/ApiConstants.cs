namespace LingoWay.Domain.Constants;

/// <summary>
/// API端点常量
/// </summary>
public static class ApiConstants
{
    // RSS源
    public static class RssSources
    {
        public const string VoaSlowEnglish = "https://www.voaspecialenglish.com/rss";
        public const string BbcLearningEnglish = "https://www.bbc.co.uk/learningenglish/feed";
    }

    // 翻译API
    public static class Translation
    {
        // 腾讯翻君
        public const string TencentTranslatorBaseUrl = "https://api.tencentcloudapi.com";

        // Azure
        public const string AzureTranslatorBaseUrl = "https://api.cognitive.microsofttranslator.com";
    }

    // 超时配置
    public static class Timeouts
    {
        public const int HttpClientTimeoutSeconds = 30;
        public const int RssParserTimeoutSeconds = 20;
        public const int TranslationApiTimeoutSeconds = 15;
    }
}
