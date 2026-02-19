using Content.Shared.Doors.Components;
using Content.Shared.Imperial.XxRaay.Nda079;
using Content.Shared.Verbs;

namespace Content.Shared.Imperial.XxRaay.Nda079;

public abstract class SharedNDA079AirlockAbilitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AirlockComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAirlockVerbs);
    }

    private void OnGetAirlockVerbs(Entity<AirlockComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (!TryComp<NDA079Component>(user, out var nda079Comp))
            return;

        if (!nda079Comp.InAIVisionMode)
            return;

        if (!HasComp<NDA079AirlockAbilityComponent>(user))
            return;

        var target = ent.Owner;

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("nda079-ability-airlock-verb"),
            Priority = 10,
            Act = () => OnAirlockVerbAct(user, target)
        };
        args.Verbs.Add(verb);
    }

    protected virtual void OnAirlockVerbAct(EntityUid user, EntityUid target)
    {
    }
}
