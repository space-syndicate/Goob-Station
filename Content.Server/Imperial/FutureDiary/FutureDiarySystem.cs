using Content.Shared.Paper;
using static Content.Shared.Paper.PaperComponent;
using Content.Shared.Interaction;

namespace Content.Server.Imperial.FutureDiary;

public sealed class FutureDiarySystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paperSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FutureDiaryComponent, PaperInputTextMessage>(OnPaperTextChanged);
        SubscribeLocalEvent<FutureDiaryComponent, InteractUsingEvent>(OnPaperInteractUsing);
    }

    private void OnPaperTextChanged(EntityUid uid, FutureDiaryComponent component, PaperInputTextMessage args)
    {
        if (!HasComp<FutureDiaryComponent>(uid))
            return;

        SyncAllDiaries(uid, args.Text);
    }

    private void OnPaperInteractUsing(EntityUid uid, FutureDiaryComponent component, InteractUsingEvent args)
    {
        if (!HasComp<FutureDiaryComponent>(uid))
            return;

        if (!TryComp<PaperComponent>(uid, out var paperComp))
            return;

        SyncAllDiaries(uid, paperComp.Content);
    }

    private void SyncAllDiaries(EntityUid changedUid, string content)
    {
        var query = EntityQueryEnumerator<PaperComponent, FutureDiaryComponent>();
        while (query.MoveNext(out var uid, out var paper, out _))
        {
            if (uid == changedUid)
                continue;

            _paperSystem.SetContent(uid, content);
        }
    }
}
