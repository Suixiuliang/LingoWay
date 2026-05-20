namespace LingoWay.Domain.Constants;

/// <summary>
/// 色彩常量定义
/// </summary>
public static class ColorConstants
{
    // 深色模式背景色
    public static class Dark
    {
        public const string BackgroundPrimary = "#0A0E27";      // 深紫黑
        public const string BackgroundSecondary = "#1A1F3A";    // 深蓝灰
        public const string CardBackground = "#242A45";         // 深灰蓝
        public const string SurfaceElevated = "#2A2F47";        // 稍微提升的表面

        public const string TextPrimary = "#E8E9ED";            // 亮灰
        public const string TextSecondary = "#A0A5B3";          // 中灰
        public const string TextTertiary = "#6B7280";           // 暗灰

        public const string PrimaryBrand = "#6366F1";           // 靛蓝 (MAUI主色)
        public const string PrimaryHover = "#4F46E5";           // 靛蓝深色

        public const string AccentGreen = "#10B981";            // 翠绿 (CTA)
        public const string AccentOrange = "#F59E0B";           // 琥珀色 (次级)
        public const string ErrorRed = "#EF4444";               // 柔和红
        public const string WarningYellow = "#FBBF24";          // 警告黄
        public const string SuccessGreen = "#34D399";           // 成功绿
    }

    // 浅色模式 (备用)
    public static class Light
    {
        public const string BackgroundPrimary = "#FFFFFF";
        public const string BackgroundSecondary = "#F3F4F6";
        public const string CardBackground = "#FFFFFF";

        public const string TextPrimary = "#111827";
        public const string TextSecondary = "#6B7280";
        public const string TextTertiary = "#9CA3AF";

        public const string PrimaryBrand = "#6366F1";
    }

    // 词汇难度颜色
    public static class Vocabulary
    {
        public const string HighFrequency = "Transparent";      // 隐藏
        public const string CoreWord = "#10B981";               // 绿色
        public const string DifficultWord = "#F59E0B";          // 橙色
        public const string VeryDifficultWord = "#EF4444";      // 红色
    }
}
