namespace ICQ.Server.Models;

/// <summary>
/// Classic ICQ-style emoticon codes → emoji (and special markers).
/// Clients should replace these codes in message text before display.
/// </summary>
public static class SmileyPack
{
    public static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Classic faces
        [":)"] = "🙂",
        [":-)"] = "🙂",
        [":("] = "🙁",
        [":-("] = "🙁",
        [":D"] = "😀",
        [":-D"] = "😀",
        [";)"] = "😉",
        [";-)"] = "😉",
        [":P"] = "😛",
        [":-P"] = "😛",
        [":p"] = "😛",
        [":*"] = "😘",
        [":-*"] = "😘",
        ["8)"] = "😎",
        ["8-)"] = "😎",
        ["B)"] = "😎",
        [":O"] = "😮",
        [":-O"] = "😮",
        [":o"] = "😮",
        [":|"] = "😐",
        [":-|"] = "😐",
        [":$"] = "😳",
        [":-$"] = "😳",
        [":@"] = "😠",
        [":-@"] = "😠",
        [":'("] = "😢",
        ["XD"] = "😆",
        ["xD"] = "😆",
        [":/"] = "😕",
        [":-/"] = "😕",
        [":\\"] = "😕",
        ["<3"] = "❤️",
        ["</3"] = "💔",
        [":3"] = "😺",
        ["O:)"] = "😇",
        ["O:-)"] = "😇",
        [">:("] = "😡",
        [">:-("] = "😡",
        [":X"] = "🤐",
        [":-X"] = "🤐",

        // ICQ classics & fun
        ["*JOKINGLY*"] = "😜",
        ["*KISSING*"] = "💋",
        ["*STOP*"] = "🛑",
        ["*THUMBS UP*"] = "👍",
        ["*THUMBS DOWN*"] = "👎",
        ["*APPLAUD*"] = "👏",
        ["*OK*"] = "👌",
        ["*HELP*"] = "🆘",
        ["*PARTY*"] = "🥳",
        ["*DRINK*"] = "🍺",
        ["*COFFEE*"] = "☕",
        ["*ROSE*"] = "🌹",
        ["*SUN*"] = "☀️",
        ["*RAIN*"] = "🌧️",
        ["*MUSIC*"] = "🎵",
        ["*DANCE*"] = "💃",
        ["*ANGEL*"] = "😇",
        ["*DEVIL*"] = "😈",
        ["*LOVE*"] = "🥰",
        ["*CRAZY*"] = "🤪",
        ["*SICK*"] = "🤒",
        ["*YAWN*"] = "🥱",
        ["*SLEEP*"] = "😴",
        ["*THINK*"] = "🤔",
        ["*IDEA*"] = "💡",
        ["*BOMB*"] = "💣",
        ["*FIRE*"] = "🔥",

        // Legendary ICQ: smiley banging head against the wall
        // (Unicode approx. — classic was an animated .gif in old ICQ)
        ["*BANG*"] = "🤕🧱",
        ["*HEADBANG*"] = "🤕🧱",
        [":bang:"] = "🤕🧱",
        ["*WALL*"] = "🤕🧱",
        ["*FACEPALM*"] = "🤦",
        ["*PANIC*"] = "😱",
        ["*SIGH*"] = "😮‍💨",
        ["*SHRUG*"] = "🤷",
    };

    /// <summary>Replace emoticon codes in text with emoji (server-side helper / API).</summary>
    public static string Expand(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        var result = text;
        // Longer codes first so *HEADBANG* wins over partial matches
        foreach (var kv in Map.OrderByDescending(k => k.Key.Length))
            result = result.Replace(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    /// <summary>
    /// Codes for the picker. Prefer canonical ICQ codes; keep *BANG*/*HEADBANG* visible.
    /// </summary>
    public static IReadOnlyList<SmileyDto> List()
    {
        // Prefer longer / canonical codes when several map to the same glyph
        var preferred = new[]
        {
            "*HEADBANG*", "*BANG*", "*WALL*", ":bang:",
            "*FACEPALM*", "*JOKINGLY*", "*KISSING*", "*THUMBS UP*", "*THUMBS DOWN*",
            "*APPLAUD*", "*PARTY*", "*ROSE*", "*ANGEL*", "*DEVIL*", "*SHRUG*", "*PANIC*"
        };

        var byEmoji = new Dictionary<string, SmileyDto>(StringComparer.Ordinal);
        foreach (var code in preferred)
        {
            if (Map.TryGetValue(code, out var emoji) && !byEmoji.ContainsKey(emoji))
                byEmoji[emoji] = new SmileyDto(code, emoji);
        }

        foreach (var kv in Map.OrderByDescending(k => k.Key.Length))
        {
            if (!byEmoji.ContainsKey(kv.Value))
                byEmoji[kv.Value] = new SmileyDto(kv.Key, kv.Value);
        }

        return byEmoji.Values
            .OrderBy(s => s.Code.StartsWith('*') ? 1 : 0)
            .ThenBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public record SmileyDto(string Code, string Emoji);
