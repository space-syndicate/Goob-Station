using System.Diagnostics.CodeAnalysis;
using System.Text;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.Emoji;

public static class EmojiList
{
    private static readonly ResPath Root = new("/Textures/_Aquila/Interface/Emoji");

    public static readonly string[] Names =
    [
        "smile", "grin", "joy", "wink", "love", "cool",
        "wow", "sad", "cry", "angry", "heart", "clown",
    ];

    private static readonly HashSet<string> NameSet = new(Names);

    private static readonly (string Text, string Name)[] Emoticons =
    [
        (":)", "smile"),
        (":D", "grin"),
        (";)", "wink"),
        (":(", "sad"),
        (":O", "wow"),
        (":o", "wow"),
        ("<3", "heart"),
    ];

    public static bool TryGetPath(string name, out ResPath path)
    {
        path = default;
        if (!NameSet.Contains(name))
            return false;

        path = Root / $"{name}.png";
        return true;
    }

    public static FormattedMessage Parse(string text)
    {
        var message = new FormattedMessage();
        var buffer = new StringBuilder();
        var i = 0;
        var lastWasEmoji = false;

        while (i < text.Length)
        {
            if (TryMatch(text, i, out var name, out var length))
            {
                if (buffer.Length > 0)
                {
                    message.AddText(buffer.ToString());
                    buffer.Clear();
                }
                else if (lastWasEmoji)
                {
                    message.AddText(" ");
                }

                message.PushTag(new MarkupNode(EmojiTag.TagName, new MarkupParameter(name), null), selfClosing: true);
                i += length;
                lastWasEmoji = true;
                continue;
            }

            buffer.Append(text[i]);
            lastWasEmoji = false;
            i++;
        }

        if (buffer.Length > 0)
            message.AddText(buffer.ToString());

        return message;
    }

    private static bool TryMatch(string text, int index, [NotNullWhen(true)] out string? name, out int length)
    {
        name = null;
        length = 0;

        if (text[index] == ':')
        {
            var end = text.IndexOf(':', index + 1);
            if (end > index + 1)
            {
                var candidate = text.Substring(index + 1, end - index - 1);
                if (NameSet.Contains(candidate))
                {
                    name = candidate;
                    length = end - index + 1;
                    return true;
                }
            }
        }

        foreach (var (emoticon, emoticonName) in Emoticons)
        {
            if (string.CompareOrdinal(text, index, emoticon, 0, emoticon.Length) != 0)
                continue;

            var before = index == 0 || char.IsWhiteSpace(text[index - 1]);
            var afterIndex = index + emoticon.Length;
            var after = afterIndex >= text.Length || char.IsWhiteSpace(text[afterIndex]) || char.IsPunctuation(text[afterIndex]);
            if (!before || !after)
                continue;

            name = emoticonName;
            length = emoticon.Length;
            return true;
        }

        return false;
    }
}
