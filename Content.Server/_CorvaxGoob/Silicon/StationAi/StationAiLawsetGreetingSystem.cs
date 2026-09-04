// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxGoob.Silicon.StationAi;

public sealed class StationAiLawsetGreetingSystem : EntitySystem
{
    private const string StationAiJobId = "StationAi";

    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Only announce laws for a normal Station AI spawn with a valid law provider and lawset prototype.
        if (args.Silent ||
            args.JobId != StationAiJobId ||
            !TryComp(args.Mob, out SiliconLawProviderComponent? provider) ||
            !_prototype.TryIndex(provider.Laws, out SiliconLawsetPrototype? lawset))
            return;

        // Use the prototype ID as a fallback when the lawset has no localized display name.
        var lawsetName = lawset.Name is { } name
            ? Loc.GetString(name)
            : lawset.ID;

        // Report the lawset actually applied by the server, including the result of a random selection.
        var message = Loc.GetString("station-ai-lawset-greeting", ("lawset", lawsetName));
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

        _chat.ChatMessageToOne(ChatChannel.Server,
            message,
            wrappedMessage,
            default,
            false,
            args.Player.Channel);
    }
}
