namespace ICQ.Server.Models;

public static class Smileys
{
    // Classic ICQ-style smileys + modern emoji + *BANG*
    public static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        { ":)", "😊" },
        { ":-)", "😊" },
        { ":D", "😃" },
        { ":-D", "😃" },
        { ":(", "😞" },
        { ":-(", "😞" },
        { ";)", "😉" },
        { ";-)", "😉" },
        { ":P", "😛" },
        { ":-P", "😛" },
        { ":p", "😛" },
        { "8)", "😎" },
        { "8-)", "😎" },
        { ":'", "😢" },
        { ":'(", "😢" },
        { ":*", "😘" },
        { ":-*", "😘" },
        { ":O", "😮" },
        { ":-O", "😮" },
        { ":/", "😕" },
        { ":-/", "😕" },
        { "<3", "❤️" },
        { "*BANG*", "🤦‍♂️" },
        { "*bang*", "🤦‍♂️" },
        { "BANG", "🤦‍♂️" },
        { ":beer:", "🍺" },
        { ":coffee:", "☕" },
        { ":thumbsup:", "👍" },
        { ":thumbsdown:", "👎" },
        { ":fire:", "🔥" },
        { ":100:", "💯" },
        { ":ok:", "👌" },
        { ":wave:", "👋" },
        { ":clap:", "👏" },
        { ":heart:", "❤️" },
        { ":broken_heart:", "💔" },
        { ":smile:", "😄" },
        { ":laugh:", "😂" },
        { ":wink:", "😉" },
        { ":cool:", "😎" },
        { ":angry:", "😠" },
        { ":cry:", "😢" },
        { ":sad:", "😔" },
        { ":surprised:", "😲" },
        { ":thinking:", "🤔" },
        { ":shrug:", "🤷" },
        { ":facepalm:", "🤦‍♂️" },
    };

    public static string Expand(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var result = text;
        // Longer codes first to avoid partial replaces
        foreach (var kv in Map.OrderByDescending(k => k.Key.Length))
        {
            result = result.Replace(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase);
        }
        return result;
    }

    public static object GetPack()
    {
        return Map.Select(kv => new { code = kv.Key, emoji = kv.Value }).ToList();
    }
}
