namespace LingoWay.Presentation.Converters;

using System.Globalization;

/// <summary>
/// 布尔值转可见性转换器
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? true : false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? true : false;
    }
}

/// <summary>
/// TimeSpan转字符串转换器
/// </summary>
public class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timespan)
        {
            return timespan.ToString(@"hh\:mm\:ss");
        }
        return "00:00:00";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && TimeSpan.TryParse(str, out var result))
        {
            return result;
        }
        return TimeSpan.Zero;
    }
}

/// <summary>
/// 下载进度转换器
/// </summary>
public class DownloadProgressConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            return $"{progress:F1}%";
        }
        return "0%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 词汇难度颜色转换器
/// </summary>
public class VocabularyColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            0 => Colors.Transparent,           // HighFrequency - 隐藏
            1 => Color.FromHex("#10B981"),    // CoreWord - 绿色
            2 => Color.FromHex("#F59E0B"),    // DifficultWord - 橙色
            3 => Color.FromHex("#EF4444"),    // VeryDifficultWord - 红色
            _ => Colors.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 下载状态转颜色转换器
/// </summary>
public class DownloadStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Pending" => Color.FromHex("#6B7280"),      // 灰色
            "Downloading" => Color.FromHex("#6366F1"),  // 蓝色
            "Completed" => Color.FromHex("#10B981"),    // 绿色
            "Failed" => Color.FromHex("#EF4444"),       // 红色
            "Cancelled" => Color.FromHex("#F59E0B"),    // 橙色
            _ => Colors.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 倒计时转换器
/// </summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? false : true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? false : true;
    }
}

/// <summary>
/// 列表计数转可见性转换器
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0;
        }
        if (value is System.Collections.ICollection collection)
        {
            return collection.Count > 0;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
