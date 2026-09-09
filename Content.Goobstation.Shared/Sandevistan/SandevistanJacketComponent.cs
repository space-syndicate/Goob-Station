// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Sandevistan;


[RegisterComponent]
public sealed partial class SandevistanJacketComponent : Component
{
    [DataField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(1);
}
