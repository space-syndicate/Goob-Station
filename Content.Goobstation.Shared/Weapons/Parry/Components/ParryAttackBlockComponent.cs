// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Weapons.Parry.Components;

[RegisterComponent]
public sealed partial class ParryAttackBlockComponent : Component
{
    [ViewVariables]
    public Dictionary<EntityUid, TimeSpan> BlockedTargets = new();
}
