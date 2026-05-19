using System.Numerics;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Called to cast a spell with a projectile
/// </summary>
public sealed partial class MedievalLightningSpellEvent : MedievalEntityAimingSpellEvent
{
    /// <summary>
    /// A entity spawned after lightning was casted
    /// </summary>
    [DataField]
    public EntProtoId? SpawnedEffectPrototype;

    /// <summary>
    /// Entity effects aplyed after lightning collide
    /// </summary>
    [DataField(serverOnly: true)]
    public EntityEffect[] LightningCollideEffects = [];

    /// <summary>
    /// Speed of lightning
    /// </summary>
    [DataField]
    public float Speed = 0.0f;

    /// <summary>
    /// Lightning width
    /// </summary>
    [DataField]
    public float Intensity = 2.0f;

    /// <summary>
    /// Needed for procedural generation of lightning.
    /// Lightning with the same seed will look the same.
    /// Leave this value to null if you want different seeds every time.
    /// </summary>
    [DataField]
    public float? Seed;

    /// <summary>
    /// Lightning scatter at start and end.
    /// Increase this value to make the lightning more chaotic.
    /// </summary>
    [DataField]
    public float Amplitude = 0.2f;

    /// <summary>
    /// Lightning frequency.
    /// Responsible for the quality and randomness of lightning generation.
    /// </summary>
    [DataField]
    public float Frequency = 3.0f;

    /// <summary>
    /// Lightning color
    /// </summary>
    [DataField]
    public Color LightningColor = Color.FromHex("#1A40F0");

    /// <summary>
    /// Lightning offset relative to itself
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    /// <summary>
    /// Try to guess
    /// </summary>
    [DataField]
    public TimeSpan LifeTime = TimeSpan.FromSeconds(1);
}
