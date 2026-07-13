// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.MiningCrate;

/// <summary>
/// Core mining crate: power gate, open state, loot, shared visuals/sounds.
/// Unlock methods are separate optional components (points, free timer, ...).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(MiningCrateSystem), typeof(MiningCrateUnlockTimerSystem), typeof(MiningCratePointsUnlockSystem))]
public sealed partial class MiningCrateComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool RequirePower = true;

    [DataField, AutoNetworkedField]
    public bool StartUnlocked;

    [DataField, AutoNetworkedField]
    public bool SpawnLoot = true;

    [DataField, AutoNetworkedField]
    public bool EnableDenySiren = true;

    [DataField, AutoNetworkedField]
    public bool Unlocked;

    [DataField]
    public ProtoId<EntityTablePrototype>? LootTable;

    [DataField]
    public SoundSpecifier UnlockSound = new SoundPathSpecifier("/Audio/Machines/door_lock_off.ogg");

    [DataField]
    public SoundSpecifier LockSound = new SoundPathSpecifier("/Audio/Machines/door_lock_on.ogg");

    [DataField]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");

    [DataField]
    public SoundSpecifier PowerOffSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public SoundSpecifier ShockSound = new SoundPathSpecifier("/Audio/Effects/sparks1.ogg");

    [DataField]
    public SoundSpecifier PurchaseSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public SoundSpecifier TamperAlarmSound = new SoundPathSpecifier("/Audio/Machines/alarm.ogg");

    [DataField]
    public TimeSpan DenySoundDelay = TimeSpan.FromSeconds(0.45);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDenySound = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTamperBlink = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public bool TamperShowLock = true;

    [DataField]
    public TimeSpan TamperBlinkInterval = TimeSpan.FromSeconds(0.2);
}
