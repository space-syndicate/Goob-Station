using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Internals;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Hostage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HostageTakerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Hostage;

    [DataField, AutoNetworkedField]
    public EntityUid Weapon;

    [DataField]
    public float MinKillDistance = 2f;

    [DataField]
    public float MaxKillDistance = 3f;

    [DataField]
    public EntityUid? HostageTakerEffectUid = null;

    [DataField]
    public EntProtoId HostageTakerEffect = "HostageTakerEffect";

    [DataField]
    public ProtoId<AlertPrototype> ExecuteAlert = "ExecuteHostage";

    [DataField]
    public ProtoId<AlertPrototype> FreeAlert = "FreeHostage";

    [DataField]
    public ProtoId<AlertPrototype> ToggleKillAlert = "ToggleKillHostage";

    [DataField]
    public float MovementSpeedMod = 0.4f;

    [DataField]
    public bool AutoKill = true;
}

[Serializable, NetSerializable]
public sealed partial class StartTakeHostageDoAfterEvent : DoAfterEvent
{
    [NonSerialized]
    public EntityUid Gun;

    public StartTakeHostageDoAfterEvent(EntityUid gun)
    {
        Gun = gun;
    }

    public override DoAfterEvent Clone() => this;
}

public sealed partial class ExecuteHostageAlertEvent : BaseAlertEvent;
public sealed partial class FreeHostageAlertEvent : BaseAlertEvent;
public sealed partial class AutoKillAlertEvent : BaseAlertEvent;
