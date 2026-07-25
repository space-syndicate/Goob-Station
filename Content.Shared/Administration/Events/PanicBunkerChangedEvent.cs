// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events;

[Serializable, NetSerializable]
public sealed class PanicBunkerStatus
{
    public bool Enabled;
    public bool DisableWithAdmins;
    public bool EnableWithoutAdmins;
    public bool CountDeadminnedAdmins;
    public bool ShowReason;
    public int MinAccountAgeMinutes;
    public int MinOverallMinutes;
    public bool DenyVpn; // CorvaxGoob-VPNGuard
}

[Serializable, NetSerializable]
public sealed class PanicBunkerChangedEvent : EntityEventArgs
{
    public PanicBunkerStatus Status;

    public PanicBunkerChangedEvent(PanicBunkerStatus status)
    {
        Status = status;
    }
}