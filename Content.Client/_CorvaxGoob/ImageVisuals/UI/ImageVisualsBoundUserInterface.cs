using Content.Shared._CorvaxGoob.ImageVisuals;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._CorvaxGoob.ImageVisuals.UI;

[UsedImplicitly]
public sealed class ImageVisualsBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ImageVisualsWindow? _window;

    public ImageVisualsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ImageVisualsWindow>();

        if (EntMan.TryGetComponent<ImageVisualsComponent>(Owner, out var visuals))
        {
            _window.SetImage(visuals.ImagePath, visuals.ImageSize);
        }
    }
}