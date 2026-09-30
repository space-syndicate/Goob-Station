using Content.Server.Chat.Systems;
using Content.Server.Emoting.Components;
using Content.Shared._CorvaxGoob.Trigger.Components.Triggers;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Trigger;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxGoob.Trigger.Systems;

public sealed class EmoteOnTriggerSystem : XOnTriggerSystem<EmoteOnTriggerComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    protected override void OnTrigger(Entity<EmoteOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var emote = _prototypes.Index<EmotePrototype>(ent.Comp.Emote);
        foreach (var uid in _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Radius))
        {
            if (!HasComp<BodyEmotesComponent>(uid))
                continue;

            _chat.TryEmoteWithChat(uid, emote, ignoreActionBlocker: true, forceEmote: true);
        }

        args.Handled = true;
    }
}
