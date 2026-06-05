using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LingoWay.Application.Services;

/// <summary>
/// MyMemory API 单词翻译缓存服务
/// 翻译结果存入本地 JSON 文件，重复单词不重复请求
/// </summary>
public class WordTranslationService
{
    private static readonly HttpClient _httpClient = new() 
    { 
        Timeout = TimeSpan.FromSeconds(15) 
    };
    
    private readonly string _cacheFilePath;
    private Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private bool _dirty;

    public WordTranslationService()
    {
        _cacheFilePath = Path.Combine(FileSystem.AppDataDirectory, "word_translations.json");
        LoadCache();
    }

    /// <summary>
    /// 获取单个单词的翻译（从缓存）
    /// </summary>
    public string? GetTranslation(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return null;
        return _cache.TryGetValue(word.Trim(), out var t) ? t : null;
    }

    /// <summary>
    /// 翻译单个文本（单词或句子），自动缓存
    /// </summary>
    public async Task<string?> TranslateSingleAsync(string text, string sourceLang = "en", string targetLang = "zh")
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var key = text.Trim();
        
        // 先查缓存
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            var translation = await FetchTranslationAsync(key, sourceLang, targetLang);
            if (!string.IsNullOrWhiteSpace(translation))
            {
                _cache[key] = translation;
                SaveCache();
                return translation;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WordTranslation] TranslateSingle failed for '{key}': {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// 批量翻译单词（跳过已缓存的，只翻译新词）
    /// </summary>
    public async Task TranslateWordsAsync(IEnumerable<string> words, string sourceLang = "en", string targetLang = "zh")
    {
        var uniqueWords = words
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(w => !_cache.ContainsKey(w))
            .ToList();

        if (uniqueWords.Count == 0) return;

        foreach (var word in uniqueWords)
        {
            try
            {
                await _rateLimiter.WaitAsync();
                
                // MyMemory 免费版频率限制：约 1 req/sec
                await Task.Delay(350); // throttle

                var translation = await FetchTranslationAsync(word, sourceLang, targetLang);
                if (!string.IsNullOrWhiteSpace(translation))
                {
                    _cache[word] = translation;
                    _dirty = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WordTranslation] Failed for '{word}': {ex.Message}");
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        if (_dirty)
        {
            SaveCache();
            _dirty = false;
        }
    }

    /// <summary>
    /// 从 MyMemory API 获取翻译
    /// </summary>
    private async Task<string?> FetchTranslationAsync(string word, string sourceLang, string targetLang)
    {
        var langPair = $"{sourceLang}|{targetLang}";
        var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(word)}&langpair={langPair}&mt=1";

        var response = await _httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        // 取 translatedText
        if (root.TryGetProperty("responseData", out var rd) &&
            rd.TryGetProperty("translatedText", out var tt))
        {
            var translation = tt.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(translation) && 
                !string.Equals(translation, word, StringComparison.OrdinalIgnoreCase))
            {
                return translation;
            }
        }

        return null;
    }

    private void LoadCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    _cache = new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WordTranslation] LoadCache failed: {ex.Message}");
        }
    }

    private void SaveCache()
    {
        try
        {
            var dir = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WordTranslation] SaveCache failed: {ex.Message}");
        }
    }
}
