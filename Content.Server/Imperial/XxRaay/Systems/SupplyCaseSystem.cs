using System.Linq;
using Content.Server.Imperial.XxRaay.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Systems;

/// <summary>
/// System for handling supply case - container that can send items via supplypod.
/// </summary>
public sealed class SupplyCaseSystem : EntitySystem
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
            Text = "Отправить",
            Message = isEmpty
                ? "Чемодан пуст"
                : "Отправка предметов через supplypod"
        });
    }

    private void SendItems(EntityUid caseUid, EntityUid user, StorageComponent storage)
    {
        var containedEntities = storage.Container.ContainedEntities.ToList();
        
        if (containedEntities.Count == 0)
        {
            _popup.PopupEntity("Чемодан пуст", caseUid, user);
            return;
        }

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
        }

        if (itemPrototypes.Count == 0)
        {
            _popup.PopupEntity("Нет подходящих предметов для отправки", caseUid, user);
            return;
        }

        foreach (var item in containedEntities)
        {
            if (_containerSystem.Remove(item, storage.Container))
            {
                storage.StoredItems.Remove(item);
                Del(item);
            }
        }

        Dirty(caseUid, storage);

        EntityCoordinates spawnCoordinates;
        if (_handsSystem.IsHolding(user, caseUid, out _))
        {
            if (!TryComp<TransformComponent>(user, out var userXform))
                return;
            spawnCoordinates = userXform.Coordinates;
        }
        else
        {
            if (!TryComp<TransformComponent>(caseUid, out var caseXform))
                return;
            spawnCoordinates = caseXform.Coordinates;
        }

        var podEntity = Spawn("supplypod_spawn", spawnCoordinates);

        var spawnItemsComp = EnsureComp<SpawnItemsOnDespawnComponent>(podEntity);
        var spawnItemsSystem = EntitySystem.Get<SpawnItemsOnDespawnSystem>();
        spawnItemsSystem.SetItems((podEntity, spawnItemsComp), itemPrototypes);

        _popup.PopupEntity($"Отправлено предметов: {itemPrototypes.Count}", caseUid, user);
    }
}

