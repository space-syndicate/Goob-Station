// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Construction;
using Content.Shared._Lavaland.MiningCrate;
using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server._Lavaland.MiningCrate;

/// <summary>
/// After a deconstruction step finishes, start self-destruct if security is still armed.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class MiningCrateDeconstructSecurityCheck : IGraphAction
{
    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent(uid, out MiningCrateSecurityComponent? security))
            return;

        if (!security.Armed || security.Detonating)
            return;

        entityManager.System<MiningCrateSecuritySystem>().StartDetonation((uid, security), userUid);
        entityManager.System<ConstructionSystem>().ResetEdge(uid);
    }
}
