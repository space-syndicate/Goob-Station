// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Sara Aldrete's Top Guy <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// using Content.Goobstation.Client.VoiceChat; // CorvaxGoob
using Content.Goobstation.Client.JoinQueue;
using Content.Goobstation.Common.ServerCurrency;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.Entry;

public sealed partial class EntryPoint : GameClient
{
    /* CorvaxGoob
    [Dependency] private VoiceChatManager _voiceManager = default!;
    [Dependency] private JoinQueueManager _joinQueue = default!;
    [Dependency] private PollManager _pollManager = default!;
    [Dependency] private ICommonCurrencyManager _currMan = default!;
    */
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override void Init()
    {
        // ContentGoobClientIoC.Register(); CorvaxGoob

        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);

        _prototypeManager.RegisterIgnore("achievement");
    }

    public override void PostInit()
    {
        base.PostInit();

        /* CorvaxGoob
        _voiceManager.Initialize();
        _joinQueue.Initialize();
        _pollManager.Initialize();
        _currMan.Initialize();
        */
    }

    /*
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _currMan.Shutdown();
        _voiceManager.Shutdown();
    }
    */
}
