using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Imperial.PhaseSpace;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Imperial.Medieval.Magic.MedievalSpellTeleportEffect;


public abstract partial class SharedMedievalSpellTeleportEffectSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalSpellTeleportEffectComponent, MedievalBeforeCastSpellEvent>(OnBeforeCast);
        SubscribeLocalEvent<MedievalSpellTeleportEffectComponent, MedievalAfterCastSpellEvent>(OnAfterCast);
    }

    protected virtual void OnBeforeCast(EntityUid uid, MedievalSpellTeleportEffectComponent component, ref MedievalBeforeCastSpellEvent args)
    {
        var origin = _transformSystem.GetMapCoordinates(args.Performer);
        var target = _transformSystem.ToMapCoordinates(args.Target);

        if (component.CheckOccluded && !_examineSystem.InRangeUnOccluded(origin, target, SharedInteractionSystem.MaxRaycastRange, null))
        {
            _popupSystem.PopupClient(Loc.GetString("dash-ability-cant-see"), args.Performer, args.Performer);
            _actionsSystem.ClearCooldown(uid);

            args.Cancelled = true;

            return;
        }

        foreach (var ent in _entityLookupSystem.GetEntitiesInRange(target, 1, LookupFlags.Static))
        {
            if (!TryComp<FixturesComponent>(ent, out var fixturesComponent))
                continue;

            var canDash = !fixturesComponent
                .Fixtures
                .Where(el => (el.Value.CollisionMask & (int)CollisionGroup.WallLayer) != 0)
                .Any();

            if (!canDash)
            {
                args.Cancelled = true;
                _popupSystem.PopupClient(Loc.GetString("dash-ability-tile-not-empty"), args.Performer, args.Performer);

                return;
            }
        }

        EnsureComp<PhaseSpaceFadeDistortionComponent>(args.Performer);
    }

    protected virtual void OnAfterCast(EntityUid uid, MedievalSpellTeleportEffectComponent component, ref MedievalAfterCastSpellEvent args)
    {
        EnsureComp<PhaseSpaceFadeDistortionComponent>(args.Performer);
    }
}
