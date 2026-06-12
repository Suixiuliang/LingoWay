using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;

namespace LingoWay.Application.Services;

/// <summary>
/// 单词悬停=本地牛津词典，句子翻译=MyMemory API。各自独立缓存。
/// </summary>
public class WordTranslationService
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly DictionaryService _dict;
    private readonly string _cacheFilePath;

    // API 翻译缓存（句子）
    private ConcurrentDictionary<string, string> _apiCache = new(StringComparer.OrdinalIgnoreCase);
    // 本地词典缓存（单词悬停）——不和 API 混用
    private static readonly ConcurrentDictionary<string, string?> _dictCache = new(StringComparer.OrdinalIgnoreCase);

    public WordTranslationService()
    {
        _dict = new DictionaryService(FindDictBase());
        _cacheFilePath = Path.Combine(FileSystem.AppDataDirectory, "word_translations.json");
        LoadCache();
    }

    private static string FindDictBase()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            var candidate = Path.Combine(dir, "Resources");
            if (Directory.Exists(candidate)) return candidate;
            var parent = Path.GetDirectoryName(dir);
            if (parent == null) break;
            dir = parent;
        }
        return Path.Combine(AppContext.BaseDirectory, "Resources");
    }

    // ── 悬停 tooltip：纯本地牛津词典，独立缓存，不沾 API ──

    public string? GetTranslation(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return null;
        var key = word.Trim();
        if (_dictCache.TryGetValue(key, out var cached)) return cached;

        var result = _dict.LookupQuick(key);
        _dictCache[key] = result;
        return result;
    }

    // ── 详细查询：弹出窗口用，返回完整释义 ──

    public string? LookupDetail(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return null;
        return _dict.Lookup(word.Trim());
    }

    // ── 全文翻译：MyMemory API ──

    public async Task<string?> TranslateSingleAsync(string text, string sourceLang = "en", string targetLang = "zh")
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var key = text.Trim();

        if (_apiCache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            var translation = await FetchTranslationAsync(key, sourceLang, targetLang);
            if (!string.IsNullOrWhiteSpace(translation))
            {
                _apiCache[key] = translation;
                SaveCache();
                return translation;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] TranslateSingle failed: {ex.Message}");
        }
        return null;
    }

    public async Task TranslateWordsAsync(IEnumerable<string> words, string sourceLang = "en", string targetLang = "zh")
    {
        var uniqueWords = words
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(w => !_apiCache.ContainsKey(w))
            .ToList();

        if (uniqueWords.Count == 0) return;

        foreach (var word in uniqueWords)
        {
            try
            {
                await Task.Delay(350);
                var translation = await FetchTranslationAsync(word, sourceLang, targetLang);
                if (!string.IsNullOrWhiteSpace(translation))
                    _apiCache[word] = translation;
            }
            catch { }
        }

        SaveCache();
    }

    // ── MyMemory API ──

    private async Task<string?> FetchTranslationAsync(string text, string sourceLang, string targetLang)
    {
        var langPair = $"{sourceLang}|{targetLang}";
        var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={langPair}&mt=1";
        var response = await _httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(response);

        if (doc.RootElement.TryGetProperty("responseData", out var rd) &&
            rd.TryGetProperty("translatedText", out var tt))
        {
            var translation = tt.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(translation) &&
                !string.Equals(translation, text, StringComparison.OrdinalIgnoreCase))
            {
                return translation;
            }
        }
        return null;
    }

    // ── 缓存持久化（仅 API 缓存落盘，词典缓存常驻内存） ──

    private void LoadCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                    _apiCache = new ConcurrentDictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
            }
        }
        catch { }
    }

    private void SaveCache()
    {
        try
        {
            var d = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(d) && !Directory.Exists(d))
                Directory.CreateDirectory(d);
            var json = JsonSerializer.Serialize(_apiCache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_cacheFilePath, json);
        }
        catch { }
    }
}
