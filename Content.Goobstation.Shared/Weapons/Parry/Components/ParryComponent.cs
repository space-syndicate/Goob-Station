// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Weapons.Parry.Components;

[RegisterComponent]
public sealed partial class ParryComponent : Component
{
    [DataField]
    public bool CanParryMelee = true;

    [DataField]
    public bool CanParryInjectables;

    [DataField]
    public bool CanParryThrown;

    [DataField]
    public bool CanParryProjectiles;

    [DataField]
    public float ParryChance = 1f;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.Zero;

    [DataField]
    public TimeSpan NextParry = TimeSpan.Zero;

    [DataField]
    public bool Active;

    [DataField]
    public bool AlwaysActive;

    [DataField]
    public bool RequireWielded;

    [DataField]
    public int RequireFreeHands;

    [DataField]
    public bool RequireStanding = true;

    [DataField]
    public bool BlockCombosWhileActive;

    [DataField]
    public bool DisableMartialArtsWhileActive;

    [DataField]
    public bool CounterAttack;

    [DataField]
    public TimeSpan CounterAttackStunTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan CounterAttackKnockdownTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan AttackerBlockTime = TimeSpan.Zero;

    [DataField]
    public EntProtoId? Action;

    [DataField]
    public SoundSpecifier? ParrySound = new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg");

    [DataField]
    public bool ReflectThrown;

    [DataField]
    public LocId? SelfPopup = "parry-popup-self";

    [DataField]
    public LocId? AttackerPopup = "parry-popup-attacker";

    [ViewVariables]
    public EntityUid? ActionEntity;
}
