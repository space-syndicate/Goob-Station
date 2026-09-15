using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CorvaxGoob.ImageVisuals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImageVisualsComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ResPath ImagePath;

    [DataField, AutoNetworkedField]
    public Vector2 ImageSize = new Vector2(100, 100);
}

[Serializable, NetSerializable]
public enum ImageVisualsUiKey : byte
{
    Key
}