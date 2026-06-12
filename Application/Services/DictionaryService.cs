using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace LingoWay.Application.Services;

public class DictionaryService : IDisposable
{
    private readonly SqliteConnection? _conn;
    private static readonly ConcurrentDictionary<string, string?> FullCache = new();
    private static readonly ConcurrentDictionary<string, string?> QuickCache = new();
    private const int MaxCacheSize = 10000;
    public bool Available => _conn != null;

    public DictionaryService(string baseDir)
    {
        var dbPath = FindDbPath(baseDir);
        System.Diagnostics.Debug.WriteLine($"[Dict] baseDir={baseDir}, found={dbPath}");
        if (dbPath == null)
        {
            System.Diagnostics.Debug.WriteLine("[Dict] dict.db not found");
            return;
        }

        try
        {
            _conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
            _conn.Open();

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "PRAGMA mmap_size=268435456";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "PRAGMA cache_size=-50000";
            cmd.ExecuteNonQuery();

            System.Diagnostics.Debug.WriteLine($"[Dict] Connected to {dbPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dict] Open failed: {ex.Message}");
            _conn = null;
        }
    }

    private static string? FindDbPath(string baseDir)
    {
        foreach (var name in new[] { "dict.db", "dict_v2.db" })
        {
            var path = Path.Combine(baseDir, name);
            if (File.Exists(path)) return path;
        }

        var exeDir = AppContext.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            foreach (var name in new[] { "dict.db", "dict_v2.db" })
            {
                var candidate = Path.Combine(exeDir, "Resources", name);
                if (File.Exists(candidate)) return candidate;
            }
            var parent = Path.GetDirectoryName(exeDir);
            if (parent == null || parent == exeDir) break;
            exeDir = parent;
        }

        var hard = @"D:\Administrator\Documents\GitHub\LingoWay\Resources\dict.db";
        if (File.Exists(hard)) return hard;

        return null;
    }

    /// <summary>Full lookup: all dictionaries, all definitions.</summary>
    public string? Lookup(string word)
    {
        if (_conn == null || string.IsNullOrWhiteSpace(word)) return null;
        var key = word.Trim().ToLowerInvariant();

        if (FullCache.TryGetValue(key, out var cached)) return cached;
        if (FullCache.Count > MaxCacheSize) FullCache.Clear();

        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT source, definition FROM entries WHERE word = @w COLLATE NOCASE ORDER BY CASE source WHEN 'oxford' THEN 0 WHEN 'collins' THEN 1 ELSE 2 END";
            cmd.Parameters.AddWithValue("@w", key);

            var parts = new List<string>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var source = reader.GetString(0);
                var html = reader.GetString(1);
                var text = ExtractFullText(html);
                if (string.IsNullOrWhiteSpace(text)) continue;

                var label = source switch
                {
                    "oxford" => "[牛津]",
                    "collins" => "[柯林斯]",
                    "urban" => "[俚语]",
                    _ => $"[{source}]"
                };
                parts.Add($"【{label}】\n{text}");
            }

            var result = parts.Count > 0 ? string.Join("\n────────\n", parts) : null;
            FullCache[key] = result;
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dict] Lookup({key}): {ex.Message}");
            return null;
        }
    }

    /// <summary>Quick lookup: Chinese-only, pos + 释义，不带例句。For sidebar tooltips.</summary>
    public string? LookupQuick(string word)
    {
        if (_conn == null || string.IsNullOrWhiteSpace(word)) return null;
        var key = word.Trim().ToLowerInvariant();

        if (QuickCache.TryGetValue(key, out var cached)) return cached;
        if (QuickCache.Count > MaxCacheSize) QuickCache.Clear();

        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT source, definition FROM entries WHERE word = @w COLLATE NOCASE ORDER BY CASE source WHEN 'oxford' THEN 0 WHEN 'collins' THEN 1 ELSE 2 END LIMIT 2";
            cmd.Parameters.AddWithValue("@w", key);

            var defs = new List<string>();
            string? lastSource = null;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var source = reader.GetString(0);
                var html = reader.GetString(1);
                var text = ExtractChineseDefs(html, maxDefs: 2 - defs.Count);
                if (string.IsNullOrWhiteSpace(text)) continue;

                var label = source switch
                {
                    "oxford" => "[牛]",
                    "collins" => "[柯]",
                    "urban" => "[俚]",
                    _ => $"[{source}]"
                };
                if (source != lastSource)
                {
                    defs.Add($"{label} {text}");
                    lastSource = source;
                }
                else
                {
                    defs.Add(text);
                }
                if (defs.Count >= 3) break;
            }

            var result = defs.Count > 0 ? string.Join("\n", defs) : null;
            if (result != null && result.Length > 180)
                result = result[..180] + "...";
            QuickCache[key] = result;
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dict] Quick({key}): {ex.Message}");
            return null;
        }
    }

    public string? LookupPlainText(string word) => LookupQuick(word);

    // ──── Chinese-only extraction: 词性 + 中文释义，不带例句 ────

    private static string ExtractChineseDefs(string html, int maxDefs)
    {
        html = Regex.Replace(html, @"<(head|script|style)[^>]*>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<(link|meta)\b[^>]*/?>", "", RegexOptions.IgnoreCase);

        var results = new List<string>();

        // Oxford: <span class="pos" pos="v">verb</span> ... <span class="chn">中文</span>
        var posMatches = Regex.Matches(html, @"<span\s+[^>]*class=""[^""]*pos[^""]*""[^>]*>\s*(\w+)\s*</span>", RegexOptions.IgnoreCase);
        for (int i = 0; i < posMatches.Count && results.Count < maxDefs; i++)
        {
            var m = posMatches[i];
            var pos = MapPos(m.Groups[1].Value.Trim().ToLower());
            if (string.IsNullOrEmpty(pos)) continue;

            // Find the Chinese translation span after this pos
            var searchStart = m.Index + m.Length;
            var searchEnd = i + 1 < posMatches.Count ? posMatches[i + 1].Index : html.Length;

            var cnMatch = Regex.Match(html[searchStart..searchEnd],
                @"<span\s+[^>]*class=""(?:chn|tx)""[^>]*>(.*?)</span>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (cnMatch.Success)
            {
                var cn = StripHtml(cnMatch.Groups[1].Value).Trim();
                if (cn.Length >= 1 && HasCJK(cn))
                    results.Add($"{pos} {cn}");
            }
        }

        // Oxford fallback: no pos tags, try <span class="chn"> directly
        if (results.Count == 0)
        {
            foreach (Match c in Regex.Matches(html, @"<span\s+[^>]*class=""(?:chn|tx)""[^>]*>(.*?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                var cn = StripHtml(c.Groups[1].Value).Trim();
                if (cn.Length >= 1 && HasCJK(cn))
                {
                    results.Add(cn);
                    if (results.Count >= maxDefs) break;
                }
            }
        }

        // Collins: <span class="additional" title="名词">N-VAR</span>
        if (results.Count == 0)
        {
            var addMatches = Regex.Matches(html, @"<span\s+class=""additional""\s+title=""[^""]*""[^>]*>([^<]+)</span>", RegexOptions.IgnoreCase);
            for (int i = 0; i < addMatches.Count && results.Count < maxDefs; i++)
            {
                var m = addMatches[i];
                var enPos = m.Groups[1].Value.Trim();
                if (enPos.Length > 6 || enPos.Contains('/') || enPos.Contains('(')) continue;
                var pos = MapPos(enPos.ToLower());
                if (string.IsNullOrEmpty(pos)) continue;

                var after = m.Index + m.Length;
                var end = i + 1 < addMatches.Count ? addMatches[i + 1].Index : html.Length;
                foreach (var tag in new[] { "<li", "<span class=\"additional\"", "<br", "</p>", "</div>", "<div " })
                {
                    var idx = html.IndexOf(tag, after, StringComparison.OrdinalIgnoreCase);
                    if (idx > after && idx < end) end = idx;
                }
                var stripped = StripHtml(html[after..end]).Trim();
                var cnOnly = PickChinese(stripped);
                if (!string.IsNullOrWhiteSpace(cnOnly))
                    results.Add($"{pos} {cnOnly}");
            }
        }

        // Urban
        if (results.Count == 0)
        {
            foreach (Match m in Regex.Matches(html, @"<span\s+class=""UD_explanation_content"">(.*?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                var defn = StripHtml(m.Groups[1].Value).Trim();
                if (defn.Length > 2)
                {
                    results.Add(defn.Length > 60 ? defn[..60] + "..." : defn);
                    if (results.Count >= 1) break;
                }
            }
        }

        // Fallback
        if (results.Count == 0)
        {
            var cnOnly = PickChinese(StripHtml(html));
            if (!string.IsNullOrWhiteSpace(cnOnly))
                results.Add(cnOnly);
        }

        return string.Join("\n", results.Distinct().Take(maxDefs));
    }

    // ──── Full text (definitions only, no examples) ────

    private static string ExtractFullText(string html)
    {
        html = Regex.Replace(html, @"<(head|script|style)[^>]*>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<(link|meta)\b[^>]*/?>", "", RegexOptions.IgnoreCase);

        var lines = new List<string>();

        // ── Oxford ──
        var posMatches = Regex.Matches(html, @"<span\s+[^>]*class=""[^""]*pos[^""]*""[^>]*>\s*([\w-]+)\s*</span>", RegexOptions.IgnoreCase);
        var oxfordDefs = Regex.Matches(html, @"<span\s+[^>]*class=""d""[^>]*>(.*?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        int nextDefIdx = 0;
        foreach (Match pm in posMatches)
        {
            var pos = MapPos(pm.Groups[1].Value.Trim().ToLower());
            if (string.IsNullOrEmpty(pos)) continue;
            string? defText = null;
            for (int i = nextDefIdx; i < oxfordDefs.Count; i++)
            {
                var dm = oxfordDefs[i];
                if (dm.Index > pm.Index)
                {
                    var fullDef = dm.Groups[1].Value;
                    var cnMatch = Regex.Match(fullDef, @"<span\s+[^>]*class=""(?:chn|tx)""[^>]*>(.*?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    defText = cnMatch.Success
                        ? StripHtml(cnMatch.Groups[1].Value).Trim()
                        : StripHtml(fullDef).Trim();
                    nextDefIdx = i + 1;
                    break;
                }
            }
            if (!string.IsNullOrWhiteSpace(defText))
                lines.Add($"{pos} {defText}");
        }

        if (lines.Count == 0)
        {
            foreach (Match dm in oxfordDefs)
            {
                var defnHtml = dm.Groups[1].Value;
                var cnMatch = Regex.Match(defnHtml, @"<span\s+[^>]*class=""(?:chn|tx)""[^>]*>(.*?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                var t = cnMatch.Success ? StripHtml(cnMatch.Groups[1].Value).Trim() : StripHtml(defnHtml).Trim();
                if (t.Length > 2) lines.Add(t);
            }
        }

        // ── Collins ──
        var addMatches = Regex.Matches(html, @"<span\s+class=""additional""\s+title=""[^""]*""[^>]*>([^<]+)</span>", RegexOptions.IgnoreCase);
        for (int i = 0; i < addMatches.Count; i++)
        {
            var m = addMatches[i];
            var enPos = m.Groups[1].Value.Trim().ToLower();
            if (enPos.Length > 6 || enPos.Contains('/') || enPos.Contains('(')) continue;

            var start = m.Index + m.Length;
            var endTag = html.IndexOf("</p>", start, StringComparison.OrdinalIgnoreCase);
            if (endTag < 0) endTag = html.Length;
            var nextLi = html.IndexOf("<li", start, StringComparison.OrdinalIgnoreCase);
            var nextEx = html.IndexOf("<div class=\"exampleLists\"", start, StringComparison.OrdinalIgnoreCase);
            if (nextLi > start && nextLi < endTag) endTag = nextLi;
            if (nextEx > start && nextEx < endTag) endTag = nextEx;

            var rawDef = StripHtml(html[start..endTag]).Trim();
            if (rawDef.Length < 2) continue;

            var cnMatch = Regex.Match(rawDef, @"[\u4e00-\u9fff][\u4e00-\u9fff—，、。！？；：…·\s]+", RegexOptions.RightToLeft);
            var pos = MapPos(enPos);
            lines.Add(cnMatch.Success ? $"{pos} {cnMatch.Value.Trim()}" : $"{pos} {rawDef[..Math.Min(80, rawDef.Length)]}");
        }

        // ── Urban ──
        foreach (Match m in Regex.Matches(html, @"<span\s+class=""UD_explanation_content"">(.*?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
        {
            var defn = StripHtml(m.Groups[1].Value).Trim();
            if (defn.Length > 2) lines.Add(defn);
        }

        // ── Fallbacks ──
        if (lines.Count == 0)
        {
            foreach (Match li in Regex.Matches(html, @"<li[^>]*>(.*?)</li>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                var t = StripHtml(li.Groups[1].Value).Trim();
                if (t.Length > 2) lines.Add(t);
            }
        }

        if (lines.Count == 0)
        {
            var t = StripHtml(html).Trim();
            if (!string.IsNullOrWhiteSpace(t))
                lines.Add(t.Length > 200 ? t[..200] + "..." : t);
        }

        return string.Join("\n", lines.Distinct().Take(20));
    }

    // ──── Helpers ────

    /// <summary>从混合文本中提取中文部分（末尾连续中文）</summary>
    private static string PickChinese(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        // 优先匹配末尾的连续中文片段
        var m = Regex.Match(text, @"[\u4e00-\u9fff\u3400-\u4dbf][\u4e00-\u9fff\u3400-\u4dbf。，、；：？！「」『』（）【】《》…—·\s]*[\u4e00-\u9fff\u3400-\u4dbf]", RegexOptions.RightToLeft);
        if (m.Success) return m.Value.Trim();
        // 回退：任意包含中文的非英文片段
        m = Regex.Match(text, @"[\u4e00-\u9fff\u3400-\u4dbf][^a-zA-Z]{2,}", RegexOptions.RightToLeft);
        return m.Success ? m.Value.Trim() : "";
    }

    private static bool HasCJK(string s) =>
        Regex.IsMatch(s, @"[\u4e00-\u9fff\u3400-\u4dbf]");

    /// <summary>词性缩写 → 缩写保留 + 句点</summary>
    private static string MapPos(string raw) => raw switch
    {
        "n" or "noun" or "n-var" or "n-count" or "n-uncount" or "n-proper" or "n-plural"
            or "n-sing" or "n-mass" => "n.",
        "v" or "verb" or "v-erg" or "v-link" or "v-pass" or "v-recip"
            or "v-t" or "vt" => "v.",
        "adj" or "adjective" or "adj-grad" or "adj-color" or "adj-compar" or "adj-superl" => "adj.",
        "adv" or "adverb" or "adv-brd-neg" or "adv-cl" or "adv-compar" or "adv-superl" => "adv.",
        "prep" or "preposition" or "prep-phrase" => "prep.",
        "conj" or "conjunction" or "conj-coord" or "conj-subord" => "conj.",
        "pron" or "pronoun" => "pron.",
        "det" or "determiner" => "det.",
        "interj" or "interjection" => "int.",
        "num" or "number" or "ord" or "ordinal" => "num.",
        "aux" or "auxiliary" or "modal" or "modal-verb" => "aux.",
        "art" or "article" or "def-art" or "indef-art" => "art.",
        "prefix" => "pref.",
        "suffix" => "suf.",
        "abbr" or "abbreviation" => "abbr.",
        "phrase" => "phr.",
        "quant" or "quantifier" => "quant.",
        _ => raw.Length <= 6 ? $"{raw}." : ""
    };

    private static string StripHtml(string html)
    {
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        text = System.Net.WebUtility.HtmlDecode(text);
        return text;
    }

    public void Dispose() => _conn?.Dispose();
}
