using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LingoWay.Application.Services;

/// <summary>
/// 测试数据生成和调试助手
/// </summary>
public static class DebugHelper
{
    /// <summary>
    /// 创建测试 LRC 文件
    /// </summary>
    public static async Task<string> CreateTestLrcFileAsync()
    {
        var testLrcContent = @"[00:00.00]Welcome to LingoWay
[00:00.00]欢迎来到 LingoWay
[00:05.50]This is a test episode
[00:05.50]这是一个测试剧集
[00:10.00]Learn English words step by step
[00:10.00]一步步学习英文单词
[00:15.30]The quick brown fox jumps over the lazy dog
[00:15.30]快速的棕色狐狸跳过懒狗
[00:20.00]Technology is changing our lives
[00:20.00]技术正在改变我们的生活
[00:25.50]Machine learning and artificial intelligence are the future
[00:25.50]机器学习和人工智能是未来
[00:30.00]Let's practice pronunciation together
[00:30.00]让我们一起练习发音
[00:35.20]Every day brings new opportunities to learn
[00:35.20]每一天都带来学习的新机会
[00:40.00]Thank you for listening to this episode
[00:40.00]感谢您收听本集
[00:45.00]Good luck with your English learning journey
[00:45.00]祝您英语学习之旅顺利
";

        var fileName = $"test_{DateTime.Now:yyyyMMdd_HHmmss}.lrc";
        var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

        await File.WriteAllTextAsync(filePath, testLrcContent, Encoding.UTF8);
        System.Diagnostics.Debug.WriteLine($"Created test LRC file at: {filePath}");

        return filePath;
    }

    /// <summary>
    /// 创建测试音频文件（简单的 WAV 文件头，用于演示）
    /// </summary>
    public static async Task<string> CreateTestAudioFileAsync()
    {
        var fileName = $"test_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
        var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

        // 创建一个最小的 WAV 文件
        // 这只是一个演示，实际应该使用真实音频文件
        var waveData = CreateMinimalWaveFile(44100, 2); // 44100 Hz, 2 seconds

        await File.WriteAllBytesAsync(filePath, waveData);
        System.Diagnostics.Debug.WriteLine($"Created test audio file at: {filePath}");

        return filePath;
    }

    /// <summary>
    /// 创建一个最小的 WAV 文件（用于测试）
    /// </summary>
    private static byte[] CreateMinimalWaveFile(int sampleRate, int durationSeconds)
    {
        int channels = 2;
        int bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int dataSize = sampleRate * channels * bitsPerSample / 8 * durationSeconds;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // WAV header
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        // fmt subchunk
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // subchunk1Size
        writer.Write((ushort)1); // audioFormat (PCM)
        writer.Write((ushort)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((ushort)blockAlign);
        writer.Write((ushort)bitsPerSample);

        // data subchunk
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        // 填充数据（简单的正弦波）
        for (int i = 0; i < dataSize / blockAlign; i++)
        {
            // 生成一个简单的 440 Hz 正弦波
            double t = (double)i / sampleRate;
            short sample = (short)(30000 * Math.Sin(2 * Math.PI * 440 * t));
            for (int ch = 0; ch < channels; ch++)
            {
                writer.Write(sample);
            }
        }

        return stream.ToArray();
    }

    /// <summary>
    /// 获取示例 LRC 文本内容
    /// </summary>
    public static string GetSampleLrcContent() => @"[00:00.00]Welcome to LingoWay
[00:00.00]欢迎来到 LingoWay
[00:05.50]This is a test episode
[00:05.50]这是一个测试剧集
[00:10.00]Learn English words step by step
[00:10.00]一步步学习英文单词
[00:15.30]The quick brown fox jumps over the lazy dog
[00:15.30]快速的棕色狐狸跳过懒狗
[00:20.00]Technology is changing our lives
[00:20.00]技术正在改变我们的生活
[00:25.50]Machine learning and artificial intelligence are the future
[00:25.50]机器学习和人工智能是未来
[00:30.00]Let's practice pronunciation together
[00:30.00]让我们一起练习发音
[00:35.20]Every day brings new opportunities to learn
[00:35.20]每一天都带来学习的新机会
[00:40.00]Thank you for listening to this episode
[00:40.00]感谢您收听本集
[00:45.00]Good luck with your English learning journey
[00:45.00]祝您英语学习之旅顺利
";

    /// <summary>
    /// 记录调试信息
    /// </summary>
    public static void LogDebugInfo(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[LingoWay.Debug] {DateTime.Now:HH:mm:ss.fff} - {message}");
    }

    /// <summary>
    /// 获取应用数据目录中的所有测试文件
    /// </summary>
    public static List<string> GetTestFiles()
    {
        var testFiles = new List<string>();
        var appDataDir = FileSystem.AppDataDirectory;

        try
        {
            var files = Directory.GetFiles(appDataDir, "test_*");
            testFiles.AddRange(files);
            LogDebugInfo($"Found {files.Length} test files in {appDataDir}");
        }
        catch (Exception ex)
        {
            LogDebugInfo($"Error reading test files: {ex.Message}");
        }

        return testFiles;
    }

    /// <summary>
    /// 清理测试文件
    /// </summary>
    public static void CleanupTestFiles()
    {
        var testFiles = GetTestFiles();
        foreach (var file in testFiles)
        {
            try
            {
                File.Delete(file);
                LogDebugInfo($"Deleted test file: {file}");
            }
            catch (Exception ex)
            {
                LogDebugInfo($"Error deleting test file {file}: {ex.Message}");
            }
        }
    }
}
