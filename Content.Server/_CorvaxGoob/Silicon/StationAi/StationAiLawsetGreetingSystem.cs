// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxGoob.Silicon.StationAi;

public sealed class StationAiLawsetGreetingSystem : EntitySystem
{
    private const string StationAiJobId = "StationAi";

    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.Silent ||
            args.JobId != StationAiJobId ||
            !TryComp(args.Mob, out SiliconLawProviderComponent? provider) ||
            !_prototype.TryIndex(provider.Laws, out SiliconLawsetPrototype? lawset))
            return;

        var lawsetName = lawset.Name is { } name
            ? Loc.GetString(name)
            : lawset.ID;

        _chat.DispatchServerMessage(args.Player,
            Loc.GetString("station-ai-lawset-greeting", ("lawset", lawsetName)));
    }
}
