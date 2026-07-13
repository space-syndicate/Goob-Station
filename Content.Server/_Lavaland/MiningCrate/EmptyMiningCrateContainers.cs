// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Storage.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server._Lavaland.MiningCrate;

/// <summary>
/// Dumps storage contents when the crate is deconstructed.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class EmptyMiningCrateContainers : IGraphAction
{
    [DataField]
    public float Scatter = 0.35f;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent(uid, out TransformComponent? xform))
            return;

        var containerSys = entityManager.System<SharedContainerSystem>();
        var transformSys = entityManager.System<SharedTransformSystem>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var coords = xform.Coordinates;

        if (entityManager.HasComponent<EntityStorageComponent>(uid))
            entityManager.System<EntityStorageSystem>().EmptyContents(uid);

        if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? manager))
            return;

        foreach (var container in containerSys.GetAllContainers(uid, manager))
        {
            foreach (var ent in containerSys.EmptyContainer(container, force: true, destination: coords, reparent: true))
            {
                if (Scatter <= 0f)
                    continue;

                transformSys.SetCoordinates(ent, coords.Offset(random.NextVector2(Scatter)));
            }
        }
    }
}
