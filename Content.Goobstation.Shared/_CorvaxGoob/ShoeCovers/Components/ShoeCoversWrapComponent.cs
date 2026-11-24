using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._CorvaxGoob.ShoeCovers.Components;

[RegisterComponent]
public sealed partial class ShoeCoversWrapComponent : Component
{
    [DataField]
    public TimeSpan WrapDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier? WrapSound;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    // Этот список загружается из addedComponents в YAML и добавляется к ботинкам после завершения действия.
    [DataField(required: true)]
    public ComponentRegistry AddedComponents = new();
}
