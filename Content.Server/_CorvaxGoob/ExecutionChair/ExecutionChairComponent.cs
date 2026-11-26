// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CorvaxGoob.ExecutionChair;

[RegisterComponent]
public sealed partial class ExecutionChairComponent : Component;

/// <summary>
/// Marks an execution chair that is waiting for the power network to process its load.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ExecutionChairSystem))]
public sealed partial class ExecutionChairPowerPendingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan CheckAt;
}

/// <summary>
/// Marks an armed execution chair that currently has an occupant.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ExecutionChairSystem))]
public sealed partial class ActiveExecutionChairComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextShockTime;
}
