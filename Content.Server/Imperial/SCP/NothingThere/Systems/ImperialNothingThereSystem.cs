using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Content.Server.Mind;
using Content.Server.Imperial.SCP.NothingThere.Components;
using Robust.Shared.Timing;
using Content.Server.Polymorph.Systems;
using Content.Shared.Destructible;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Server.Player;
using Content.Server.Chat.Managers;
using System.Linq.Expressions;
namespace Content.Server.Imperial.SCP.NothingThere.Systems;

public sealed partial class ImperialNothingThereSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IChatManager _chatM = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        InitializeMap();
        InitializeArsenal();
        InitializeBodyControl();
        InitializeChaseMusic();
        InitializeEgg();
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateItemProvide();
        UpdateEgg();
    }
    private void OnInit(EntityUid uid, ImperialNothingThereComponent comp, MapInitEvent args)
    {
        StartChaseMusic(uid, comp);
        switch (comp.Phase)
        {
            case NothingTherePhase.Original:
                if (comp.EnterBodyEntity == null)
                {
                    _actions.AddAction(uid,
                        ref comp.EnterBodyEntity,
                        comp.EnterBodyAction);
                    _actions.AddAction(uid,
                        ref comp.TransformEggEntity,
                        comp.TransformEggAction);
                }
                break;
            case NothingTherePhase.Egg:
                var curTime = _gameTiming.CurTime;
                comp.EggTransformEnd = curTime + comp.EggTransformDuration;
                break;
            case NothingTherePhase.True:
                _actions.AddAction(uid,
                    ref comp.EmpowerEntity,
                    comp.EmpowerAction);
                _actions.AddAction(uid,
                    ref comp.ProjectileEntity,
                    comp.ProjectileAction);
                var hands = EnsureComp<HandsComponent>(uid);
                if (_hands.TryGetEmptyHand((uid, hands), out var emptyHand) && comp.NeedItems == true)
                {
                    var hit = Spawn(comp.HitProto, Transform(uid).Coordinates);
                    if (!_hands.TryForcePickup(uid, hit, emptyHand, checkActionBlocker: false, handsComp: hands))
                    {
                        QueueDel(hit);
                        return;
                    }
                    else
                        comp.NeedItems = false;
                }
                break;
        }
    }
}
