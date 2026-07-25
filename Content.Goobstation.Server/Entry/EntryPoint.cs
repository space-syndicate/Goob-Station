
using Content.Goobstation.Server.Voice;
using Content.Goobstation.Common.ServerCurrency;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Entry;

public sealed class EntryPoint : GameServer
{
    // private IVoiceChatServerManager _voiceManager = default!; // deleted by CorvaxGoob
    // private ICommonCurrencyManager _curr = default!; // deleted by CorvaxGoob
    // private IJoinQueueManager _joinQueue = default!; // deleted by CorvaxGoob

    public override void Init()
    {
        base.Init();

        // ServerGoobContentIoC.Register(); // deleted by CorvaxGoob

        IoCManager.BuildGraph();

        /* deleted by CorvaxGoob
        _voiceManager = IoCManager.Resolve<IVoiceChatServerManager>();

        _joinQueue = IoCManager.Resolve<IJoinQueueManager>();
        _joinQueue.Initialize();

        _curr = IoCManager.Resolve<ICommonCurrencyManager>(); // Goobstation
        _curr.Initialize(); // Goobstation
        */
    }

    /* // deleted by CorvaxGoob
    public override void PostInit()
    {
        base.PostInit();
    }

    public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
    {
        base.Update(level, frameEventArgs);

        switch (level)
        {
            case ModUpdateLevel.PreEngine:
                _voiceManager.Update();
                _joinQueue.Update(frameEventArgs.DeltaSeconds);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _curr.Shutdown(); // Goobstation
        _voiceManager.Shutdown(); // Goobstation
    }*/
}
