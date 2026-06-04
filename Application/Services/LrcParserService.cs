using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LingoWay.Domain.Models;

namespace LingoWay.Application.Services;

/// <summary>
/// LRC 格式字幕文件解析服务
/// </summary>
public class LrcParserService
{
    private static readonly Regex LrcLineRegex = new(@"^\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)$", RegexOptions.Compiled);
    private static readonly Regex WordRegex = new(@"\b[\w*']+\b", RegexOptions.Compiled);
    private static readonly Regex ChineseRegex = new(@"[\u4e00-\u9fff]", RegexOptions.Compiled);

    /// <summary>屏蔽词 → 真实词映射表</summary>
    private static readonly Dictionary<string, string> CensoredWords = new(StringComparer.OrdinalIgnoreCase);

    static LrcParserService()
    {
        try { InitCensoredWords(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CensoredWords init failed: {ex}");
        }
    }

    private static void InitCensoredWords()
    {
        // America, Fuck Yeah!
        // 
        CensoredWords["f**k"] = "fuck";
        CensoredWords["f***"] = "fuck";
        CensoredWords["f*ck"] = "fuck";
        // shit
        CensoredWords["sh*t"] = "shit";
        CensoredWords["s**t"] = "shit";
        CensoredWords["sh**"] = "shit";
        CensoredWords["s*it"] = "shit";
        // ass
        CensoredWords["a**"] = "ass";
        CensoredWords["a*s"] = "ass";
        // bitch
        CensoredWords["b**ch"] = "bitch";
        CensoredWords["b*tch"] = "bitch";
        CensoredWords["bit*h"] = "bitch";
        // damn
        CensoredWords["d**n"] = "damn";
        CensoredWords["d*mn"] = "damn";
        CensoredWords["da*n"] = "damn";
        // hell
        CensoredWords["h**l"] = "hell";
        CensoredWords["h*ll"] = "hell";
        CensoredWords["he*l"] = "hell";
        // crap
        CensoredWords["cr*p"] = "crap";
        CensoredWords["c**p"] = "crap";
        // dick
        CensoredWords["d**k"] = "dick";
        CensoredWords["d*ck"] = "dick";
        // piss
        CensoredWords["p**s"] = "piss";
        CensoredWords["p*ss"] = "piss";
        // cock
        CensoredWords["c**k"] = "cock";
        CensoredWords["c*ck"] = "cock";
        // cunt
        CensoredWords["c**t"] = "cunt";
        CensoredWords["c*nt"] = "cunt";
        // whore
        CensoredWords["w***e"] = "whore";
        CensoredWords["wh**e"] = "whore";
        CensoredWords["w*ore"] = "whore";
        // slut
        CensoredWords["sl*t"] = "slut";
        // idiot
        CensoredWords["i***t"] = "idiot";
        CensoredWords["id**t"] = "idiot";
        CensoredWords["id*ot"] = "idiot";
        // stupid
        CensoredWords["st***d"] = "stupid";
        CensoredWords["stu**d"] = "stupid";
        CensoredWords["st*pid"] = "stupid";
        // moron
        CensoredWords["m***n"] = "moron";
        CensoredWords["mo**n"] = "moron";
        // bastard
        CensoredWords["b*****d"] = "bastard";
        CensoredWords["b**tard"] = "bastard";
        CensoredWords["bas**rd"] = "bastard";
        // motherfucker
        CensoredWords["m**********r"] = "motherfucker";
        CensoredWords["mother****er"] = "motherfucker";
        CensoredWords["motherf**ker"] = "motherfucker";
        // bullshit
        CensoredWords["b******t"] = "bullshit";
        CensoredWords["bull****"] = "bullshit";
        CensoredWords["bullsh*t"] = "bullshit";
        // screw
        CensoredWords["sc**w"] = "screw";
        CensoredWords["sc*ew"] = "screw";
        // suck
        CensoredWords["s**k"] = "suck";
        CensoredWords["su*k"] = "suck";
        // douche
        CensoredWords["d****e"] = "douche";
        CensoredWords["dou*he"] = "douche";
        // retard
        CensoredWords["r****d"] = "retard";
        CensoredWords["re**rd"] = "retard";
        CensoredWords["ret**d"] = "retard";
        // faggot
        CensoredWords["f****t"] = "faggot";
        CensoredWords["fa**ot"] = "faggot";
        // wanker
        CensoredWords["w****r"] = "wanker";
        CensoredWords["w**ker"] = "wanker";
        // bugger
        CensoredWords["b****r"] = "bugger";
        CensoredWords["bu**er"] = "bugger";
        // arse
        CensoredWords["a**e"] = "arse";
        CensoredWords["ar*e"] = "arse";
        // twat
        CensoredWords["t**t"] = "twat";
        // jerk
        CensoredWords["j**k"] = "jerk";
        CensoredWords["je*k"] = "jerk";
        // goddamn
        CensoredWords["g*****n"] = "goddamn";
        CensoredWords["god***n"] = "goddamn";
        CensoredWords["godd**n"] = "goddamn";
    }

    /// <summary>
    /// 屏蔽词还原 + 后缀变形：f**king → fucking, sh**ty → shitty
    /// </summary>
    public static string NormalizeCensoredWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return word;
        if (!word.Contains('*')) return word;

        if (CensoredWords.TryGetValue(word, out var real))
            return real;

        var suffixes = new[] { "ing", "ed", "ers", "est", "er", "ly", "y", "'s", "s" };
        foreach (var suffix in suffixes)
        {
            if (word.Length > suffix.Length + 1
                && word.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                var baseWord = word[..^suffix.Length];
                if (CensoredWords.TryGetValue(baseWord, out var baseReal))
                    return baseReal + suffix;
            }
        }
        return word;
    }

    public List<LrcLine> ParseLrc(string content, string episodeId)
    {
        var parsedEntries = new List<(TimeSpan Time, string Text)>();
        if (string.IsNullOrWhiteSpace(content)) return [];

        var rawLines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n', StringSplitOptions.None);
        foreach (var rawLine in rawLines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("{") && line.EndsWith("}")) continue;

            var match = LrcLineRegex.Match(line);
            if (!match.Success) continue;

            var minutes = int.Parse(match.Groups[1].Value);
            var seconds = int.Parse(match.Groups[2].Value);
            var fraction = match.Groups[3].Value;
            var text = match.Groups[4].Value.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            var ms = fraction.Length switch { 2 => int.Parse(fraction) * 10, 3 => int.Parse(fraction), _ => 0 };
            parsedEntries.Add((new TimeSpan(0, 0, minutes, seconds, ms), text));
        }

        if (parsedEntries.Count == 0) return [];

        var timeGroups = parsedEntries.GroupBy(x => x.Time).OrderBy(g => g.Key).ToList();
        var lines = new List<LrcLine>();
        var lineNumber = 0;

        for (var i = 0; i < timeGroups.Count; i++)
        {
            var group = timeGroups[i].ToList();
            var startTime = timeGroups[i].Key;
            TimeSpan? endTime = i + 1 < timeGroups.Count ? timeGroups[i + 1].Key : null;

            string englishText = string.Empty, chineseText = string.Empty;
            foreach (var item in group)
            {
                if (string.IsNullOrWhiteSpace(item.Text)) continue;
                if (IsChinese(item.Text)) chineseText = item.Text;
                else englishText = item.Text;
            }

            if (string.IsNullOrWhiteSpace(englishText) && !string.IsNullOrWhiteSpace(chineseText))
            { englishText = chineseText; chineseText = string.Empty; }
            if (string.IsNullOrWhiteSpace(englishText)) continue;

            var lrcLine = new LrcLine
            {
                EpisodeId = episodeId, StartTime = startTime, EndTime = endTime,
                EnglishText = englishText, ChineseText = chineseText, LineNumber = lineNumber++
            };
            lrcLine.Words = ParseWords(englishText, lrcLine);
            lines.Add(lrcLine);
        }
        return lines;
    }

    private List<LrcWord> ParseWords(string text, LrcLine lrcLine)
    {
        var words = new List<LrcWord>();
        if (string.IsNullOrWhiteSpace(text)) return words;
        var matches = WordRegex.Matches(text);
        var position = 0;
        foreach (var match in matches.Cast<Match>())
        {
            words.Add(new LrcWord { LrcLine = lrcLine, Word = match.Value, PositionInLine = position++ });
        }
        return words;
    }

    private bool IsChinese(string text) => ChineseRegex.IsMatch(text);

    public LrcLine? GetCurrentLine(List<LrcLine> lines, TimeSpan currentTime)
    {
        return lines.Where(l => l.StartTime <= currentTime && (l.EndTime == null || currentTime < l.EndTime)).FirstOrDefault();
    }

    public async Task<List<LrcLine>> LoadLrcFileAsync(string filePath, string episodeId)
    {
        try { var content = await File.ReadAllTextAsync(filePath); return ParseLrc(content, episodeId); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading LRC: {ex.Message}"); return []; }
    }
}
