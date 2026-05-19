namespace Content.Server.Imperial.HandledCuffer;
using Robust.Shared.Prototypes;

[RegisterComponent]
public sealed partial class HandledCufferComponent : Component
{
    [DataField(required: true)]
    public EntProtoId SpawnedPrototype = "EnergyZipties";
}