// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Weapons.Parry.Components;

[RegisterComponent]
public sealed partial class ParriableComponent : Component
{
    [DataField]
    public bool ParryableAsMelee = true;

    [DataField]
    public bool ParryableAsInjectable;

    [DataField]
    public bool ParryableAsThrown;

    [DataField]
    public bool BypassesParry;

    [DataField]
    public float ParryBypassChance;
}
