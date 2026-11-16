using System.Numerics;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Pulling.Events;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._BlackM.Cargo;

[RegisterComponent, NetworkedComponent]
public sealed partial class CargoTransitComponent : Component { }

// the package and everything inside it for the whole cycle.
[RegisterComponent, NetworkedComponent]
public sealed partial class CargoSealedComponent : Component { }

[Serializable, NetSerializable]
public enum CargoVisuals : byte { State, Phase, Export, Paused }

[Serializable, NetSerializable]
public enum CargoState : byte { Idle, Preparing, Moving, Blocked }

[Serializable, NetSerializable]
public enum CargoPhase : byte { Idle, Warning, Opening, Out, Jaws, Return, Closing }

public sealed class CargoTransitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CargoTransitComponent, GettingPickedUpAttemptEvent>((_, _, ev) => ev.Cancel());
        SubscribeLocalEvent<CargoTransitComponent, BeingPulledAttemptEvent>((_, _, ev) => ev.Cancel());

        SubscribeLocalEvent<CargoSealedComponent, GettingInteractedWithAttemptEvent>(OnInteract);
        SubscribeLocalEvent<CargoSealedComponent, StorageOpenAttemptEvent>(OnCrateOpen);
        SubscribeLocalEvent<CargoSealedComponent, StorageInteractAttemptEvent>(OnStorageInteract);
        SubscribeLocalEvent<CargoSealedComponent, LockToggleAttemptEvent>(OnLockToggle);
        SubscribeLocalEvent<CargoSealedComponent, ItemSlotEjectAttemptEvent>(OnSlotEject);
        SubscribeLocalEvent<CargoSealedComponent, ActivatableUIOpenAttemptEvent>((_, _, ev) => ev.Cancel());
        SubscribeLocalEvent<CargoSealedComponent, ContainerIsRemovingAttemptEvent>(OnContainerRemoving);
        SubscribeLocalEvent<CargoSealedComponent, ContainerIsInsertingAttemptEvent>(OnContainerInserting);
        SubscribeLocalEvent<CargoSealedComponent, ContainerGettingRemovedAttemptEvent>(OnGettingRemoved);
        SubscribeLocalEvent<CargoSealedComponent, ContainerGettingInsertedAttemptEvent>(OnGettingInserted);
    }

    private void OnInteract(EntityUid uid, CargoSealedComponent comp, ref GettingInteractedWithAttemptEvent args) => args.Cancelled = true;
    private void OnCrateOpen(EntityUid uid, CargoSealedComponent comp, ref StorageOpenAttemptEvent args) => args.Cancelled = true;
    private void OnStorageInteract(EntityUid uid, CargoSealedComponent comp, ref StorageInteractAttemptEvent args) => args.Cancelled = true;
    private void OnLockToggle(EntityUid uid, CargoSealedComponent comp, ref LockToggleAttemptEvent args) => args.Cancelled = true;
    private void OnSlotEject(EntityUid uid, CargoSealedComponent comp, ref ItemSlotEjectAttemptEvent args) => args.Cancelled = true;
    private void OnContainerRemoving(EntityUid uid, CargoSealedComponent comp, ref ContainerIsRemovingAttemptEvent args) => args.Cancel();
    private void OnContainerInserting(EntityUid uid, CargoSealedComponent comp, ref ContainerIsInsertingAttemptEvent args) => args.Cancel();
    private void OnGettingRemoved(EntityUid uid, CargoSealedComponent comp, ref ContainerGettingRemovedAttemptEvent args) => args.Cancel();
    private void OnGettingInserted(EntityUid uid, CargoSealedComponent comp, ref ContainerGettingInsertedAttemptEvent args) => args.Cancel();
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CargoMachineVisualComponent : Component
{
    [DataField] public int PadTiles = 1;

    // Must equal the Sprite offset in the prototype.
    [DataField] public Vector2 PadOffset = Vector2.Zero;

    public float PadHalf => PadTiles * 0.5f - 0.05f;

    [ViewVariables] public string? AppliedState;
    [ViewVariables] public CargoPhase Phase;
    [ViewVariables] public bool Export;
    [ViewVariables] public bool Paused;
    [ViewVariables] public float PhaseTime;
}

public static class HeavyCargoTiming
{
    public const float OpenStart = 1f;
    public const float OutStart = 2f;
    public const float OutEnd = 5f;
    public const float Release = 6f;
    public const float InEnd = 9f;
    public const float Complete = 10f;

    public const float BottomScale = 0.55f;

    public static CargoPhase PhaseAt(float elapsed) => elapsed switch
    {
        < 0f => CargoPhase.Idle,
        < OpenStart => CargoPhase.Warning,
        < OutStart => CargoPhase.Opening,
        < OutEnd => CargoPhase.Out,
        < Release => CargoPhase.Jaws,
        < InEnd => CargoPhase.Return,
        < Complete => CargoPhase.Closing,
        _ => CargoPhase.Idle,
    };

    public static bool GateClosed(CargoPhase phase) =>
        phase is CargoPhase.Idle or CargoPhase.Warning or CargoPhase.Opening or CargoPhase.Closing;

    public static string StateName(CargoPhase phase, bool export) =>
        (export ? "export_" : "import_") + phase switch
        {
            CargoPhase.Warning => "warning",
            CargoPhase.Opening => "opening",
            CargoPhase.Out => "out",
            CargoPhase.Jaws => "jaws",
            CargoPhase.Return => "return",
            CargoPhase.Closing => "closing",
            _ => "idle",
        };
}
