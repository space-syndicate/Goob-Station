// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Sandevistan;

/// <summary>
/// Marker component for clothing that shortens the sandevistan toggle use delay while worn.
/// </summary>
[RegisterComponent]
public sealed partial class SandevistanJacketComponent : Component
{
    [DataField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(1);
}