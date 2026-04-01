// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Weapons.Parry.Components;

[RegisterComponent]
public sealed partial class CqcParryComponent : Component
{
    [ViewVariables]
    public bool HadBlockedState;

    [ViewVariables]
    public bool RememberedBlockedState;
}
