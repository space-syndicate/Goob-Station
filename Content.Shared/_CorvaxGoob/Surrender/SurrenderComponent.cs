using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Surrender;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurrenderComponent : Component
{
    [DataField]
    public bool HadPacifismBefore = false;

    [DataField]
    public SoundSpecifier SurrendSound = new SoundPathSpecifier("/Audio/Effects/chime.ogg"); // Добавить

    [DataField]
    public EntProtoId SurrendEffect = "SurrendEffect";

    [DataField]
    public EntityUid? SurrenderEffect = null;

    [DataField]
    public ProtoId<AlertPrototype> Alert = "Surrend";

    [DataField]
    public float MovementSpeedMod = 0.4f;

    [DataField]
    public float InteractionWithUserMod = 0.4f;

    [DataField]
    public DoAfterId? StopSurrendDoAfter = null;

    [DataField]
    public TimeSpan StopSurrendTime = TimeSpan.FromSeconds(5);
}

[Serializable, NetSerializable]
public enum StopSurrendingStatus
{
    StopSurrending
}
public sealed partial class SurrenderAlertEvent : BaseAlertEvent;

[ByRefEvent]
public record struct TryStopSurrendingEvent(bool Cancelled = false);

[Serializable, NetSerializable]
public sealed partial class StopSurrendDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public enum SurrendVisualType
{
    IsHostaged
}
