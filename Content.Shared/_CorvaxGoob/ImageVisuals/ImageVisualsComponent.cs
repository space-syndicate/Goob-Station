using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.ImageVisuals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImageVisualsComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string ImagePath = string.Empty;

    [DataField, AutoNetworkedField]
    public Vector2 ImageSize = new(600, 900);
}

[Serializable, NetSerializable]
public enum ImageVisualsUiKey : byte
{
    Key
}