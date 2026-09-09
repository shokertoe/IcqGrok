namespace ICQ.Server.Models;

/// <summary>
/// Classic ICQ-style emoticon codes → emoji (and special markers).
/// Clients should replace these codes in message text before display.
/// </summary>
public static class SmileyPack
{
    public static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
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
        ["*BANG*"] = "🤦‍♂️",
        ["*HEADBANG*"] = "🤦‍♂️",
        [":bang:"] = "🤦‍♂️",
        ["*WALL*"] = "🤦‍♂️",
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
        foreach (var kv in Map.OrderByDescending(k => k.Key.Length))
            result = result.Replace(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    public static IReadOnlyList<SmileyDto> List() =>
        Map.Select(kv => new SmileyDto(kv.Key, kv.Value)).DistinctBy(s => s.Emoji).ToList();
}

public record SmileyDto(string Code, string Emoji);
