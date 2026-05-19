using System.Numerics;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


[Serializable, NetSerializable, DataDefinition]
public sealed partial class MedievalLightningSpellData : MedievalEntityAimingSpellData
{
    [DataField]
    public EntProtoId? SpawnedEffectPrototype;

    [DataField(serverOnly: true), NonSerialized]
    public EntityEffect[] LightningCollideEffects;

    [DataField]
    public float Speed = 0.0f;

    [DataField]
    public float Intensity = 2.0f;

    [DataField]
    public float? Seed;

    [DataField]
    public float Amplitude = 0.2f;

    [DataField]
    public float Frequency = 3.0f;

    [DataField]
    public Color LightningColor = Color.FromHex("#1A40F0");

    [DataField]
    public Vector2 Offset = Vector2.Zero;

    [DataField]
    public TimeSpan LifeTime = TimeSpan.FromSeconds(1);
}
