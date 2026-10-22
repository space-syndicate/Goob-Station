using Content.Shared.PDA;
using Content.Shared.Silicons.StationAi;
using Robust.Server.Containers;
using Robust.Server.GameObjects;

namespace Content.Shared._CorvaxGoob.Silicon.StationAi;

public sealed class StationAiSystem : EntitySystem
{
    [Dependency] private readonly SharedStationAiSystem _stationAiSystem = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiHeldComponent, OpenPDAUIEvent>(OnOpenPDAUIEvent);
    }

    private void OnOpenPDAUIEvent(Entity<StationAiHeldComponent> entity, ref OpenPDAUIEvent args)
    {
        if (!_stationAiSystem.TryGetCore(entity.Owner, out var core))
            return;

        if (!_containers.TryGetContainer(core, StationAiCoreComponent.PdaContainer, out var container))
            return;

        if (container.ContainedEntities.Count == 0)
            return;

        if (!TryComp<PdaComponent>(container.ContainedEntities[0], out var pda)
            || !TryComp<UserInterfaceComponent>(container.ContainedEntities[0], out var userInterface))
            return;

        _userInterface.OpenUi((container.ContainedEntities[0], userInterface), PdaUiKey.Key, entity);
    }
}
