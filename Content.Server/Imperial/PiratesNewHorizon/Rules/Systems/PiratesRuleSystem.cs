using Content.Server.Antag;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Imperial.PiratesNewHorizon.Roles;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Maths;
using Content.Server.Imperial.PiratesNewHorizon.Rules.Components;
using Content.Server.Roles;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.Mind;
using Robust.Server.Player;

namespace Content.Server.Imperial.PiratesNewHorizon.Rules.Systems;

public sealed class PiratesRuleSystem : GameRuleSystem<PiratesRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PiratesRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);
        SubscribeLocalEvent<PiratesRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagEntSelected);
        SubscribeLocalEvent<PirateRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }
    
    private void OnGetBriefing(Entity<PirateRoleComponent> role, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("pirate-welcome"));
        args.Append(Loc.GetString("pirate-briefing"));
    }
    
    private void OnRuleLoadedGrids(Entity<PiratesRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        var query = EntityQueryEnumerator<PiratesShuttleComponent>();
        while (query.MoveNext(out var uid, out var shuttle))
        {
            var transform = Transform(uid);
            if (transform.MapID == args.Map)
            {
                shuttle.AssociatedRule = ent;
                var pirates = ent.Comp;
                pirates.InitialItems.Clear();
                pirates.PirateShuttle = uid;
                pirates.InitialShipValue = _pricingSystem.AppraiseGrid(uid, uid =>
                {
                    pirates.InitialItems.Add(uid);
                    return true;
                });
                break;
            }
        }
    }
    
    protected override void AppendRoundEndText(EntityUid uid, PiratesRuleComponent comp, GameRuleComponent gameRule, ref RoundEndTextAppendEvent ev)
    {
        var pirates = comp;
        if (Deleted(comp.PirateShuttle))
        {
            ev.AddLine(Loc.GetString("pirates-no-ship"));
            ev.AddLine(Loc.GetString("pirates-result-totalloss"));
        }
        else
        {
            List<(double, EntityUid)> mostValuableThefts = new();
            var shuttle = comp.PirateShuttle;
            var comp1 = comp;
            var finalValue = _pricingSystem.AppraiseGrid(shuttle, uid =>
            {
                foreach (var mindId in _antag.GetAntagMinds(uid))
                {
                    if (TryComp(mindId, out MindComponent? mind) && mind.CurrentEntity == uid)
                        return false;
                }
                return true;
            }, (uid, price) =>
            {
                if (comp1.InitialItems.Contains(uid))
                    return;

                mostValuableThefts.Add((price, uid));
                mostValuableThefts.Sort((i1, i2) => i2.Item1.CompareTo(i1.Item1));
                if (mostValuableThefts.Count > 5)
                    mostValuableThefts.Pop();
            });

            foreach (var mindId in _antag.GetAntagMinds(uid))
            {
                if (TryComp(mindId, out MindComponent? mind) && mind.CurrentEntity is not null)
                    finalValue += _pricingSystem.GetPrice(mind.CurrentEntity.Value);
            }

            var score = finalValue - pirates.InitialShipValue;
            ev.AddLine("");

            if (mostValuableThefts.Count != 0)
            {
                ev.AddLine(Loc.GetString("pirates-final-score", ("score", $"{score:F2}")));
                ev.AddLine(Loc.GetString("pirates-final-score-2", ("finalPrice", $"{finalValue:F2}")));
            }
            ev.AddLine(Loc.GetString("pirates-most-valuable"));
            foreach (var (price, obj) in mostValuableThefts)
            {
                ev.AddLine(Loc.GetString("pirates-stolen-item-entry", ("entity", obj), ("credits", $"{price:F2}")));
            }

            if (mostValuableThefts.Count == 0)
            {
                ev.AddLine(Loc.GetString("pirates-stole-nothing"));
                ev.AddLine(Loc.GetString("pirates-stole-nothing2"));
            }

            var winningscore = pirates.WinningScore;

            if (score >= winningscore)
            {
                ev.AddLine(Loc.GetString("pirates-result-victory"));
            }
            else if (mostValuableThefts.Count == 0)
            {
                ev.AddLine(Loc.GetString("pirates-result-totalloss"));
            }
            else
            {
                ev.AddLine(Loc.GetString("pirates-result-loss"));
            }
        }

        ev.AddLine("");
        ev.AddLine(Loc.GetString("pirates-list-start"));
        foreach (var pirate in _antag.GetAntagMinds(uid))
        {
            if (TryComp(pirate, out MindComponent? mind))
            {
                if (!_players.TryGetSessionById(mind.UserId, out var session))
                    return;
                ev.AddLine($"- {mind.CharacterName} ({session.Name})");
            }
        }
    }
    
    private void OnAfterAntagEntSelected(Entity<PiratesRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        _antag.SendBriefing(args.Session,
            Loc.GetString("pirate-welcome"),
            Color.Yellow,
            ent.Comp.PirateAlertSound);
    }
    
    private EntityUid? GetShuttle(Entity<PiratesRuleComponent> ent)
    {
        var query = EntityQueryEnumerator<PiratesShuttleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AssociatedRule == ent.Owner)
                return uid;
        }
        return null;
    }
}