using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.Imperial.SyndieBorg;

namespace Content.Server.Silicons.Laws;

public sealed class SyndieBorgSystem : EntitySystem
{
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SyndieBorgComponent, ComponentStartup>(OnComponentStartup);
    }
    private void OnComponentStartup(EntityUid uid, SyndieBorgComponent component, ComponentStartup args)
    {
        var lawBound = EnsureComp<SiliconLawBoundComponent>(uid);
        var laws = _siliconLaw.GetLaws(uid, lawBound);

        laws.Laws.Insert(0, new SiliconLaw
        {
            LawString = Loc.GetString("syndie-borg-laws"),
            Order = -1,
            LawIdentifierOverride = Loc.GetString("ion-storm-law-scrambled-number", ("length", _robustRandom.Next(5, 10)))
        });
    }
}

