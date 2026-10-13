using Content.Shared._Aquila.Heretic;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client._Aquila.Heretic;

public sealed class MansusFadeOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public MansusFadeOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 100;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent(_player.LocalEntity, out MansusVictimComponent? victim))
            return;

        var now = _timing.CurTime;
        var fadeIn = 1f - Ease((float) ((now - victim.ArrivalTime) / victim.FadeInDuration));
        var fadeOut = victim.LeaveTime is { } leave
            ? Ease((float) ((now - leave) / victim.FadeOutDuration))
            : 0f;

        var alpha = MathF.Max(fadeIn, fadeOut);
        if (alpha <= 0f)
            return;

        args.ScreenHandle.DrawRect(args.ViewportBounds, Color.Black.WithAlpha(alpha));
    }

    private static float Ease(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
