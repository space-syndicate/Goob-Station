using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Imperial.MouseInput.Events;
using Content.Shared.Mind;
using Content.Shared.Movement.Systems;
using Robust.Shared.Map;

namespace Content.Shared.Imperial.Medieval.Magic;


public abstract partial class SharedMedievalMagicSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        #region Do Afters

        SubscribeLocalEvent<MedievalSpellCasterComponent, MedievalInstantSpawnDoAfterEvent>(OnSpellDoAfterCast);
        SubscribeLocalEvent<MedievalSpellCasterComponent, MedievalCastSpawnSpellDoAfterEvent>(OnSpellDoAfterCast);
        SubscribeLocalEvent<MedievalSpellCasterComponent, MedievalSpawnInHandSpellDoAfterEvent>(OnSpellDoAfterCast);
        SubscribeLocalEvent<MedievalSpellCasterComponent, MedievalCastTeleportSpellDoAfterEvent>(OnSpellDoAfterCast);
        SubscribeLocalEvent<MedievalSpellCasterComponent, MedievalSpawnAimingEntityDoAfterEvent>(OnSpellDoAfterCast);
        SubscribeLocalEvent<MedievalSpellCasterComponent, MedievalCastLightningSpellDoAfterEvent>(OnSpellDoAfterCast);
        SubscribeLocalEvent<MedievalSpellCasterComponent, MedievalCastProjectileSpellDoAfterEvent>(OnSpellDoAfterCast);
        SubscribeLocalEvent<MedievalSpellCasterComponent, MedievalCastHomingProjectilesSpellDoAfterEvent>(OnSpellDoAfterCast);
        SubscribeLocalEvent<MedievalSpellCasterComponent, MedievalCastEntityTargetProjectileSpellDoAfterEvent>(OnSpellDoAfterCast);

        #endregion

        #region Relay

        SubscribeLocalEvent<MedievalSpellCasterComponent, MousePositionRefreshEvent>(OnMousePositionResponse);

        #endregion

        SubscribeLocalEvent<MedievalSpellCasterComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);

        InitializeTargetSpells();
        InitializeInstantSpells();
        InitializeEntityAimingSpells();
    }

    private void OnSpellDoAfterCast(EntityUid uid, MedievalSpellCasterComponent component, MedievalSpellDoAfterEvent args)
    {
        if (args.Handled) return;

        var casterComponent = EnsureComp<MedievalSpellCasterComponent>(uid);
        var spellData = GetSpellData(args);
        casterComponent.SpeedModifiers.Remove(spellData.CastSpeedModifier);

        Dirty(uid, casterComponent);

        _speedModifierSystem.RefreshMovementSpeedModifiers(uid);

        if (args.Cancelled)
        {
            RaiseLocalEvent(GetEntity(spellData.Action), new MedievalFailCastSpellEvent()
            {
                Action = GetEntity(spellData.Action),
                Performer = uid
            });

            return;
        }

        CastSpell(args);
    }

    private void OnRefresh(EntityUid uid, MedievalSpellCasterComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var speedModifier = component.SpeedModifiers.Aggregate(1.0f, (acc, next) => next < acc ? next : acc);

        args.ModifySpeed(speedModifier, speedModifier);
    }

    private void OnMousePositionResponse(EntityUid uid, MedievalSpellCasterComponent component, MousePositionRefreshEvent args)
    {
        if (!_mindSystem.TryGetMind(uid, out var mind, out var _)) return;
        if (!TryComp<ActionsContainerComponent>(mind, out var actionsContainerComponent)) return;

        var ev = new MedievalSpellRelayEvent<MousePositionRefreshEvent>(args);

        foreach (var action in actionsContainerComponent.Container.ContainedEntities)
            RaiseLocalEvent(action, ev);
    }

    #region Helpers

    protected bool PassesSpellPrerequisites(EntityUid spell, EntityUid performer, EntityCoordinates target)
    {
        var ev = new MedievalBeforeCastSpellEvent(performer, target);
        RaiseLocalEvent(spell, ref ev);

        return !ev.Cancelled;
    }

    protected MedievalSpellData GetSpellData(MedievalSpellDoAfterEvent ev)
    {
        return ev switch
        {
            MedievalCastProjectileSpellDoAfterEvent projectileSpellDoAfterEvent => projectileSpellDoAfterEvent.SpellData,
            MedievalCastSpawnSpellDoAfterEvent spawnSpellDoAfterEvent => spawnSpellDoAfterEvent.SpellData,
            MedievalCastTeleportSpellDoAfterEvent teleportSpellDoAfterEvent => teleportSpellDoAfterEvent.SpellData,
            MedievalSpawnInHandSpellDoAfterEvent spawnInHandSpellDoAfterEvent => spawnInHandSpellDoAfterEvent.SpellData,
            MedievalInstantSpawnDoAfterEvent instantSpawnDoAfterEvent => instantSpawnDoAfterEvent.SpellData,
            MedievalCastHomingProjectilesSpellDoAfterEvent homingProjectilesSpellDoAfterEvent => homingProjectilesSpellDoAfterEvent.SpellData,
            MedievalCastEntityTargetProjectileSpellDoAfterEvent entityTargetProjectileSpellDoAfterEvent => entityTargetProjectileSpellDoAfterEvent.SpellData,
            MedievalCastLightningSpellDoAfterEvent lightningSpellDoAfterEvent => lightningSpellDoAfterEvent.SpellData,
            MedievalSpawnAimingEntityDoAfterEvent spawnAimingEntityDoAfterEvent => spawnAimingEntityDoAfterEvent.SpellData,
            _ => throw new ArgumentOutOfRangeException("Cannot find upcast method")
        };
    }

    #region Cast Spell

    protected void CastSpell(MedievalSpellDoAfterEvent args)
    {
        switch (args)
        {
            case MedievalCastProjectileSpellDoAfterEvent projectileSpell:
                CastProjectileSpell(projectileSpell.SpellData);
                break;
            case MedievalCastSpawnSpellDoAfterEvent spawnSpell:
                CastSpawnSpell(spawnSpell.SpellData);
                break;
            case MedievalCastTeleportSpellDoAfterEvent teleportSpell:
                CastTeleportSpell(teleportSpell.SpellData);
                break;
            case MedievalSpawnInHandSpellDoAfterEvent spawnInHandSpell:
                CastSpawnInHandSpell(spawnInHandSpell.SpellData);
                break;
            case MedievalInstantSpawnDoAfterEvent instantSpawnSpell:
                CastInstantSpawnSpell(instantSpawnSpell.SpellData);
                break;
            case MedievalCastHomingProjectilesSpellDoAfterEvent homingProjectilesEvent:
                CastHomingProjectilesSpell(homingProjectilesEvent.SpellData);
                break;
            case MedievalCastEntityTargetProjectileSpellDoAfterEvent entityTargetProjectileEvent:
                CastEntityTargetProjectileSpell(entityTargetProjectileEvent.SpellData);
                break;
            case MedievalCastLightningSpellDoAfterEvent lightningSpellEvent:
                CastLightningSpell(lightningSpellEvent.SpellData);
                break;
            case MedievalSpawnAimingEntityDoAfterEvent spawnAimingEntityDoAfterEvent:
                CastSpawnAimingEntitySpell(spawnAimingEntityDoAfterEvent.SpellData);
                break;
        }
    }

    protected void CastSpell(MedievalSpellData args)
    {
        switch (args)
        {
            case MedievalSpawnSpellData spawnSpellData:
                CastSpawnSpell(spawnSpellData);
                break;
            case MedievalSpawnInHandSpellData spawnInHandSpellData:
                CastSpawnInHandSpell(spawnInHandSpellData);
                break;
            case MedievalTeleportSpellData teleportSpellData:
                CastTeleportSpell(teleportSpellData);
                break;
            case MedievalProjectileSpellData projectileSpellData:
                CastProjectileSpell(projectileSpellData);
                break;
            case MedievalInstantSpawnData instantSpawnSpellData:
                CastInstantSpawnSpell(instantSpawnSpellData);
                break;
            case MedievalHomingProjectilesSpellData homingProjectilesSpellData:
                CastHomingProjectilesSpell(homingProjectilesSpellData);
                break;
            case MedievalEntityTargetProjectileSpellData entityTargetProjectileSpellData:
                CastEntityTargetProjectileSpell(entityTargetProjectileSpellData);
                break;
            case MedievalLightningSpellData lightningSpellData:
                CastLightningSpell(lightningSpellData);
                break;
            case MedievalSpawnAimingEntityData spawnAimingEntityData:
                CastSpawnAimingEntitySpell(spawnAimingEntityData);
                break;
        }
    }

    #endregion

    #region Server/Client implementation

    protected virtual void AddToStack(EntityUid uid, Dictionary<TimeSpan, MedievalSpellSpeech>? el)
    {
    }

    #endregion

    #endregion
}
