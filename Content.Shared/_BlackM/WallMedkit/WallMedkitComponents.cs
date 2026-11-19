using System;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._BlackM.WallMedkit;

[RegisterComponent]
public sealed partial class WallMedkitComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Charges = 3;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxCharges = 3;

    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "Ambuzol";

    [DataField]
    public FixedPoint2 Quantity = FixedPoint2.New(15);

    [DataField]
    public DamageSpecifier? Heal;

    [DataField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(1.4);

    [DataField]
    public SoundSpecifier? InjectSound = new SoundPathSpecifier("/Audio/_BlackM/WallMedkit/inject.ogg");

    [DataField]
    public SoundSpecifier? RefillSound = new SoundPathSpecifier("/Audio/_BlackM/WallMedkit/refill.ogg");

    [DataField]
    public SoundSpecifier? EmptySound = new SoundPathSpecifier("/Audio/_BlackM/WallMedkit/empty.ogg");

    [ViewVariables]
    public TimeSpan NextUse;

    [ViewVariables]
    public TimeSpan? AnimationEnd;
}

[RegisterComponent]
public sealed partial class WallMedkitRefillComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Charges = 3;
}

[Serializable, NetSerializable]
public enum WallMedkitVisuals : byte
{
    Charged,
    Using,
}
