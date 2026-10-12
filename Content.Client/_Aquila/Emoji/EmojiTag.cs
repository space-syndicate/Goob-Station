using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.Emoji;

[UsedImplicitly]
public sealed class EmojiTag : IMarkupTagHandler
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public const string TagName = "emoji";

    public string Name => TagName;

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Value.TryGetString(out var name) || !EmojiList.TryGetPath(name, out var path))
            return false;

        control = new TextureRect
        {
            Texture = _resourceCache.GetResource<TextureResource>(path).Texture,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(18, 18),
            Margin = new Thickness(0, 0),
            ToolTip = $":{name}:",
        };
        return true;
    }
}
