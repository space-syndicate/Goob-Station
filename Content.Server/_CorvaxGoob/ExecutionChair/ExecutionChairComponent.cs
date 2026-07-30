// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._CorvaxGoob.ExecutionChair;

[RegisterComponent]
public sealed partial class ExecutionChairComponent : Component;

/// <summary>
/// Marks an execution chair that is waiting for the power network to process its load.
/// </summary>
[RegisterComponent, UnsavedComponent, AutoGenerateComponentPause]
[Access(typeof(ExecutionChairSystem))]
public sealed partial class ExecutionChairPowerPendingComponent : Component
{
    [AutoPausedField]
    public TimeSpan CheckAt;
}

/// <summary>
/// Marks an armed execution chair that currently has an occupant.
/// </summary>
[RegisterComponent, UnsavedComponent, AutoGenerateComponentPause]
[Access(typeof(ExecutionChairSystem))]
public sealed partial class ActiveExecutionChairComponent : Component
{
    [AutoPausedField]
    public TimeSpan NextShockTime;
}
