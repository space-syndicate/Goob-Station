using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Localization;

namespace Content.Shared.Imperial.XxRaay.Systems;

/// <summary>
/// Shared system for supply case - container that can send items via supplypod.
/// </summary>
public abstract class SupplyCaseSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _storageSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupplyCaseComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    private void OnGetInteractionVerbs(Entity<SupplyCaseComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!TryComp<StorageComponent>(entity, out var storage))
            return;

        var user = args.User;
        var isEmpty = storage.Container.ContainedEntities.Count == 0;
        
        args.Verbs.Add(new InteractionVerb
        {
            Act = () =>
            {
                SendItems(entity.Owner, user, storage);
            },
            Text = Loc.GetString(entity.Comp.SendVerbLoc),
            Message = isEmpty
                ? Loc.GetString(entity.Comp.EmptyCaseLoc)
                : Loc.GetString(entity.Comp.SendDescLoc)
        });
    }

    private void SendItems(EntityUid caseUid, EntityUid user, StorageComponent storage)
    {
        var containedEntities = storage.Container.ContainedEntities.ToArray();
        if (containedEntities.Length == 0)
        {
            _popup.PopupEntity(Loc.GetString(Comp<SupplyCaseComponent>(caseUid).EmptyCaseLoc), caseUid, user);
            return;
        }

        var comp = Comp<SupplyCaseComponent>(caseUid);

        var itemPrototypes = new List<EntProtoId>();
        foreach (var item in containedEntities)
        {
            if (!TryComp<MetaDataComponent>(item, out var meta))
                continue;

            var protoId = meta.EntityPrototype?.ID;
            if (string.IsNullOrEmpty(protoId))
                continue;

            if (!_prototypeManager.HasIndex<EntityPrototype>(protoId))
                continue;

            itemPrototypes.Add(protoId);

            if (_containerSystem.Remove(item, storage.Container))
            {
                storage.StoredItems.Remove(item);
                Del(item);
            }
        }

        if (itemPrototypes.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString(comp.EmptyCaseLoc), caseUid, user);
            return;
        }

        var spawnCoordinates = _handsSystem.IsHolding(user, caseUid, out _)
            ? Transform(user).Coordinates
            : Transform(caseUid).Coordinates;

        var podEntity = Spawn(comp.PodPrototype, spawnCoordinates);

        var spawnItemsComp = EnsureComp<SpawnItemsOnDespawnComponent>(podEntity);
        spawnItemsComp.Items.Clear();
        spawnItemsComp.Items.AddRange(itemPrototypes);

        _popup.PopupEntity(Loc.GetString(comp.PopupSentLoc, ("count", itemPrototypes.Count)), caseUid, user);
    }
}

